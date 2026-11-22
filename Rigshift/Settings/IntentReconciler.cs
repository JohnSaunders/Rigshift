using Rigshift.Config;

namespace Rigshift.Settings;

/// <summary>The enabled names and last-enabled name after a settings save reconciles them against the saved config.</summary>
public sealed record IntentSnapshot(IReadOnlyList<string> Enabled, string? LastEnabled);

/// <summary>
/// Pure session-intent cleanup for after a settings save. The tray keeps enabled loadout
/// names in memory; this drops names whose loadout no longer exists, whose remaining
/// loadout(s) with that name are all one-shots, and names that are blank. Names are
/// compared ordinal, without trimming either side: the save path has already stored
/// trimmed names, so a name that only differs by whitespace is not the same name. No
/// names are ever invented, and neither the config nor the input collection is mutated.
/// </summary>
public static class IntentReconciler
{
    public static IntentSnapshot Reconcile(
        IReadOnlyList<string> enabled, string? lastEnabled, RigshiftConfig config)
    {
        var kept = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in enabled)
        {
            if (string.IsNullOrWhiteSpace(name) || !seen.Add(name))
                continue;

            if (HasToggleNamed(config, name))
                kept.Add(name);
        }

        var last = string.IsNullOrWhiteSpace(lastEnabled)
            ? null
            : kept.Contains(lastEnabled, StringComparer.Ordinal) ? lastEnabled : null;

        return new IntentSnapshot(kept, last);
    }

    private static bool HasToggleNamed(RigshiftConfig config, string name)
    {
        foreach (var loadout in config.Loadouts)
        {
            if (loadout.Toggle && string.Equals(loadout.Name, name, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
