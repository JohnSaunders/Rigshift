using System.Text.RegularExpressions;
using Rigshift.Config;

namespace Rigshift.Ui;

/// <summary>
/// Picks the tray icon's color. Last enabled wins; on a cold start there is no
/// session history, so config order decides and the last-listed active loadout wins.
/// </summary>
public static partial class TintSelector
{
    public static string SelectHex(RigshiftConfig config, IReadOnlyList<string> enabledLoadouts, string? lastEnabled)
    {
        var idle = IsValidHex(config.IdleColor) ? config.IdleColor : "#8A8A8E";
        if (enabledLoadouts.Count == 0) return idle;

        var owner = lastEnabled is not null && enabledLoadouts.Contains(lastEnabled)
            ? lastEnabled
            : LastInConfigOrder(config, enabledLoadouts);

        var loadout = config.Loadouts.FirstOrDefault(l => l.Name == owner);
        return loadout?.Color is { } c && IsValidHex(c) ? c : idle;
    }

    private static string? LastInConfigOrder(RigshiftConfig config, IReadOnlyList<string> enabled) =>
        config.Loadouts.LastOrDefault(l => enabled.Contains(l.Name))?.Name;

    private static bool IsValidHex(string? value) =>
        value is not null && HexPattern().IsMatch(value);

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex HexPattern();
}
