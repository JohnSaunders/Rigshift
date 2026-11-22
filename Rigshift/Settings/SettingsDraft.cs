namespace Rigshift.Settings;

/// <summary>
/// The mutable copy of a <see cref="Rigshift.Config.RigshiftConfig"/> the settings window
/// edits. Definition ports are strings, so invalid text can sit in the form until
/// <see cref="SettingsMapper.ToConfig"/> or validation rejects it.
/// </summary>
public sealed class SettingsDraft
{
    public string IdleColor { get; set; } = "#8A8A8E";
    public List<DefinitionDraft> Definitions { get; set; } = new();
    public List<LoadoutDraft> Loadouts { get; set; } = new();
}

/// <summary>Draft of one entry in the config's definitions map; <see cref="Key"/> is the object key.</summary>
public sealed class DefinitionDraft
{
    public string Key { get; set; } = "";
    public string Kind { get; set; } = "";
    public string? ProcessName { get; set; }
    public string? Path { get; set; }
    public string? WindowTitle { get; set; }
    public string? Port { get; set; }
    public string? WslCommand { get; set; }
    public bool PortProxy { get; set; }
    public List<string> Requires { get; set; } = new();
}

/// <summary>Draft of one entry in the config's loadouts list.</summary>
public sealed class LoadoutDraft
{
    public string Name { get; set; } = "";
    public bool Toggle { get; set; }
    public string? Color { get; set; }
    public List<string> Start { get; set; } = new();
    public List<string> Kill { get; set; } = new();
    public List<string> KillOnDisable { get; set; } = new();
    public bool HardShutdown { get; set; }
    public bool FlushDns { get; set; }
}
