namespace Rigshift.Ui;

/// <summary>
/// Tracks which distinct sets of config warnings have already been shown this
/// run, so the tray warning balloon does not repeat on every right-click.
/// </summary>
public sealed class WarningNotifier
{
    private readonly HashSet<string> _seen = new(StringComparer.Ordinal);

    public bool ShouldNotify(IEnumerable<string> warnings)
    {
        ArgumentNullException.ThrowIfNull(warnings);

        // Drop blank entries, remove duplicates, and sort so the key does not
        // depend on order. When nothing is left there is nothing to notify.
        var key = string.Join('\n', warnings
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(w => w, StringComparer.Ordinal));
        if (key.Length == 0) return false;

        return _seen.Add(key);
    }
}
