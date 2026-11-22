using Rigshift.Config;

namespace Rigshift.Execution;

/// <summary>Whether disabling a toggle loadout right now would do nothing, and if so, who is holding it on.</summary>
public sealed record HeldLoadoutResult(bool Held, IReadOnlyList<string> Holders);

/// <summary>
/// Pure pre-check for the disable path. Loadouts stack, so disabling one can produce
/// an empty <see cref="ActionRunner.PlanDisable"/> plan when another enabled loadout
/// still needs the same definition (for example a service that requires "wsl", so
/// disabling Dev while that loadout is on leaves WSL up). That is correct behavior,
/// but firing a UAC prompt for an elevated child that will do nothing is a dead end. This
/// lets the tray detect the no-op *before* elevating, and name who is holding it on.
/// </summary>
public static class HeldLoadoutAnalyzer
{
    public static HeldLoadoutResult Analyze(
        RigshiftConfig config, string loadoutName, IReadOnlyList<string> enabled)
    {
        // ActionRunner.PlanDisable excludes loadoutName itself via its own "excluding"
        // guard, so it is correct to pass the full enabled list, including loadoutName.
        var plan = ActionRunner.PlanDisable(config, loadoutName, enabled);
        if (plan.Steps.Count > 0)
            return new HeldLoadoutResult(Held: false, Holders: Array.Empty<string>());

        var loadout = config.Loadouts.FirstOrDefault(l => l.Name == loadoutName);
        if (loadout is null)
            return new HeldLoadoutResult(Held: true, Holders: Array.Empty<string>());

        var killKeys = new HashSet<string>(loadout.KillOnDisable, StringComparer.Ordinal);
        var holders = new List<string>();
        foreach (var other in enabled)
        {
            if (other == loadoutName) continue;
            var otherLoadout = config.Loadouts.FirstOrDefault(l => l.Name == other);
            if (otherLoadout is null) continue;

            var required = new HashSet<string>(StringComparer.Ordinal);
            foreach (var key in otherLoadout.Start)
                AddWithRequires(config, key, required);

            if (killKeys.Overlaps(required))
                holders.Add(other);
        }

        return new HeldLoadoutResult(Held: true, Holders: holders);
    }

    private static void AddWithRequires(RigshiftConfig config, string key, HashSet<string> into)
    {
        if (!into.Add(key)) return;
        var def = config.Find(key);
        if (def is null) return;
        foreach (var required in def.Requires)
            AddWithRequires(config, required, into);
    }
}
