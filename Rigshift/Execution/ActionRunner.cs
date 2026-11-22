using Rigshift.Config;

namespace Rigshift.Execution;

public sealed record ActionPlan(IReadOnlyList<Step> Steps, IReadOnlyList<string> Warnings);

/// <summary>
/// Pure planning: turns a loadout plus current state into an ordered step list.
/// Performs no I/O, so every behavior here is unit-testable.
/// </summary>
public static class ActionRunner
{
    public static ActionPlan PlanEnable(RigshiftConfig config, string loadoutName)
    {
        var steps = new List<Step>();
        var warnings = new List<string>();
        var loadout = config.Loadouts.FirstOrDefault(l => l.Name == loadoutName);
        if (loadout is null)
            return new ActionPlan(steps, new[] { $"No loadout named '{loadoutName}'." });

        var started = new HashSet<string>(StringComparer.Ordinal);
        foreach (var key in loadout.Start)
            AppendStart(config, key, steps, warnings, started);

        return new ActionPlan(steps, warnings);
    }

    private static void AppendStart(
        RigshiftConfig config, string key, List<Step> steps, List<string> warnings, HashSet<string> started)
    {
        if (!started.Add(key)) return;

        var def = config.Find(key);
        if (def is null)
        {
            warnings.Add($"Unknown definition '{key}'; skipped.");
            return;
        }

        foreach (var required in def.Requires)
            AppendStart(config, required, steps, warnings, started);

        switch (def.Kind)
        {
            case "wsl":
                steps.Add(new EnsureWsl());
                break;

            case "app":
            case "herdr":
                if (!string.IsNullOrWhiteSpace(def.Path))
                    steps.Add(new StartProcess(def.Path));
                break;

            case "wslService":
                // Fixed order for starting a service inside WSL. Do not reorder.
                // Without a processName the pattern would be " serve", and
                // `pkill -f " serve"` inside the distro matches far more than intended.
                if (!string.IsNullOrWhiteSpace(def.ProcessName))
                    steps.Add(new WslPkill($"{def.ProcessName} serve"));
                else
                    warnings.Add($"Definition '{key}' has no processName; skipped the pre-start pkill.");
                steps.Add(new Wait(1000));
                steps.Add(new StartWslService(def.WindowTitle ?? key, def.WslCommand ?? ""));
                steps.Add(new Wait(3000));
                if (def.PortProxy && def.Port is { } port)
                {
                    steps.Add(new PortProxyRemove(port));
                    steps.Add(new PortProxyAdd(port));
                }
                break;

            default:
                warnings.Add($"Definition '{key}' has unknown kind '{def.Kind}'; skipped.");
                break;
        }
    }

    /// <summary>
    /// Plans a toggle loadout's teardown. Definitions that another still-enabled
    /// loadout lists in its Start are left alone — this is what lets loadouts stack.
    /// </summary>
    public static ActionPlan PlanDisable(
        RigshiftConfig config, string loadoutName, IReadOnlyCollection<string> otherEnabledLoadouts)
    {
        var loadout = config.Loadouts.FirstOrDefault(l => l.Name == loadoutName);
        if (loadout is null)
            return new ActionPlan(Array.Empty<Step>(), new[] { $"No loadout named '{loadoutName}'." });

        var spokenFor = StillNeeded(config, otherEnabledLoadouts, loadoutName);
        return PlanKills(config, loadout, loadout.KillOnDisable, spokenFor);
    }

    /// <summary>Plans a non-toggle loadout. Ignores reference counting: this is the purge.</summary>
    public static ActionPlan PlanOneShot(RigshiftConfig config, string loadoutName)
    {
        var loadout = config.Loadouts.FirstOrDefault(l => l.Name == loadoutName);
        if (loadout is null)
            return new ActionPlan(Array.Empty<Step>(), new[] { $"No loadout named '{loadoutName}'." });

        return PlanKills(config, loadout, loadout.Kill, spokenFor: new HashSet<string>(StringComparer.Ordinal));
    }

    /// <summary>Every definition (transitively) required by the other enabled loadouts.</summary>
    private static HashSet<string> StillNeeded(
        RigshiftConfig config, IReadOnlyCollection<string> enabled, string excluding)
    {
        var needed = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in enabled)
        {
            if (name == excluding) continue;
            var other = config.Loadouts.FirstOrDefault(l => l.Name == name);
            if (other is null) continue;
            foreach (var key in other.Start)
                AddWithRequires(config, key, needed);
        }
        return needed;
    }

    private static void AddWithRequires(RigshiftConfig config, string key, HashSet<string> into)
    {
        if (!into.Add(key)) return;
        var def = config.Find(key);
        if (def is null) return;
        foreach (var required in def.Requires)
            AddWithRequires(config, required, into);
    }

    private static ActionPlan PlanKills(
        RigshiftConfig config, Loadout loadout, IReadOnlyList<string> keys, HashSet<string> spokenFor)
    {
        var steps = new List<Step>();
        var warnings = new List<string>();
        var killedWsl = false;
        var killedHerdr = false;

        foreach (var key in keys)
        {
            if (spokenFor.Contains(key)) continue;

            var def = config.Find(key);
            if (def is null)
            {
                warnings.Add($"Unknown definition '{key}'; skipped.");
                continue;
            }

            switch (def.Kind)
            {
                case "wsl":
                    steps.Add(new ShutdownWsl());
                    killedWsl = true;
                    break;

                case "app":
                    if (!string.IsNullOrWhiteSpace(def.ProcessName))
                        steps.Add(new KillProcess(def.ProcessName));
                    break;

                case "herdr":
                    // herdr.exe servers started over SSH by an administrator account run
                    // elevated, so the unelevated KillProcess cannot reach them.
                    if (!string.IsNullOrWhiteSpace(def.ProcessName))
                        steps.Add(new KillProcess(def.ProcessName));
                    if (!killedHerdr)
                    {
                        steps.Add(new KillHerdr());
                        killedHerdr = true;
                    }
                    break;

                case "wslService":
                    if (!string.IsNullOrWhiteSpace(def.ProcessName))
                        steps.Add(new KillProcess(def.ProcessName));
                    if (!string.IsNullOrWhiteSpace(def.WindowTitle))
                        steps.Add(new CloseWindow(def.WindowTitle));
                    if (def.PortProxy && def.Port is { } port)
                        steps.Add(new PortProxyRemove(port));
                    break;

                default:
                    warnings.Add($"Definition '{key}' has unknown kind '{def.Kind}'; skipped.");
                    break;
            }
        }

        // Escalation applies to the teardown as a whole, so it lands once, at the end.
        if (loadout.HardShutdown && killedWsl) steps.Add(new KillWslService());
        if (loadout.FlushDns) steps.Add(new FlushDns());

        return new ActionPlan(steps, warnings);
    }
}
