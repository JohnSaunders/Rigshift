namespace Rigshift.Settings;

/// <summary>
/// The note lines about where the loaded config came from, shown at the top of the
/// settings window. A single "no config file yet" line when nothing was loaded from
/// disk, or a "leaves the loaded file unchanged" line when the loaded file is not
/// already the per-user file, followed by each non-blank load warning in order. When
/// the loaded file is already the per-user file, only the load warnings are returned.
/// </summary>
public static partial class SettingsSource
{
    /// <summary>
    /// Builds a new list of note lines. <paramref name="activePath"/> is the file the
    /// draft was loaded from (or null when the built-in default was used), and
    /// <paramref name="userPath"/> is the per-user save destination. Path identity is
    /// ordinal-ignore-case; only here, per the spec.
    /// </summary>
    public static IReadOnlyList<string> Notes(string? activePath, string userPath, IReadOnlyList<string> loadWarnings)
    {
        var notes = new List<string>();
        if (string.IsNullOrWhiteSpace(activePath))
        {
            notes.Add($"No config file yet. Saving creates {userPath}.");
        }
        else if (!string.Equals(activePath, userPath, StringComparison.OrdinalIgnoreCase))
        {
            notes.Add($"Loaded {activePath}. Saving writes {userPath} and leaves the loaded file unchanged.");
        }
        if (loadWarnings is not null)
        {
            foreach (var warning in loadWarnings)
            {
                if (!string.IsNullOrWhiteSpace(warning))
                    notes.Add(warning);
            }
        }
        return notes;
    }
}
