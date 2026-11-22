using Rigshift.Config;

namespace Rigshift.Execution;

/// <summary>
/// Pure rule that decides which enabled toggles a one-shot loadout actually hit.
/// After a one-shot runs, the tray should clear the checkmark of every toggle the
/// one-shot touched — and only those. This is the "which toggles were hit" half of
/// that; the tray applies it (settings-usability/024). No I/O, no UI types, and it
/// never decides failure: an accepted UAC prompt is completion until
/// elevation-trust/005 adds exit codes.
/// </summary>
public static class OneShotIntentAnalyzer
{
    public static IReadOnlyList<string> Cleared(
        RigshiftConfig config, string oneShotName, IReadOnlyList<string> enabled)
    {
        var oneShot = config.Loadouts.FirstOrDefault(l => l.Name == oneShotName);
        if (oneShot is null || oneShot.Toggle)
            return Array.Empty<string>();

        var hit = new HashSet<string>(StringComparer.Ordinal);
        foreach (var key in oneShot.Kill)
        {
            if (config.Find(key) is not null)
                hit.Add(key);
        }
        if (oneShot.HardShutdown)
        {
            foreach (var (key, def) in config.Definitions)
            {
                if (def.Kind == "wsl" || def.Kind == "wslService")
                    hit.Add(key);
            }
        }

        var cleared = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in enabled)
        {
            if (!seen.Add(name)) continue;
            var toggle = config.Loadouts.FirstOrDefault(l => l.Name == name);
            if (toggle is null || !toggle.Toggle) continue;

            var reach = new HashSet<string>(StringComparer.Ordinal);
            foreach (var key in toggle.Start)
                AddWithRequires(config, key, reach);

            if (reach.Overlaps(hit))
                cleared.Add(name);
        }
        return cleared;
    }

    // Same walk as HeldLoadoutAnalyzer: a key plus everything it requires, transitively.
    private static void AddWithRequires(RigshiftConfig config, string key, HashSet<string> into)
    {
        if (!into.Add(key)) return;
        var def = config.Find(key);
        if (def is null) return;
        foreach (var required in def.Requires)
            AddWithRequires(config, required, into);
    }
}
