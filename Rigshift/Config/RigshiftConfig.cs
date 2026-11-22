namespace Rigshift.Config;

public sealed class RigshiftConfig
{
    /// <summary>Tray icon tint when no toggle loadout is enabled.</summary>
    public string IdleColor { get; set; } = "#8A8A8E";

    public Dictionary<string, Definition> Definitions { get; set; } = new();
    public List<Loadout> Loadouts { get; set; } = new();

    public Definition? Find(string key) =>
        Definitions.TryGetValue(key, out var d) ? d : null;
}
