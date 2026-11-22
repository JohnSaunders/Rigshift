namespace Rigshift.Config;

/// <summary>A named set of definitions to start or kill.</summary>
public sealed class Loadout
{
    public string Name { get; set; } = "";

    /// <summary>true renders a checkbox; false renders a one-shot action item.</summary>
    public bool Toggle { get; set; }

    /// <summary>Hex tint for the tray icon. Only meaningful when Toggle is true.</summary>
    public string? Color { get; set; }

    public List<string> Start { get; set; } = new();
    public List<string> Kill { get; set; } = new();
    public List<string> KillOnDisable { get; set; } = new();

    /// <summary>After shutting WSL down, also kill wslservice.exe.</summary>
    public bool HardShutdown { get; set; }

    /// <summary>Run ipconfig /flushdns once at the end.</summary>
    public bool FlushDns { get; set; }
}
