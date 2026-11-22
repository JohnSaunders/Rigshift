namespace Rigshift.Config;

/// <summary>A single controllable thing that loadouts reference by name.</summary>
public sealed class Definition
{
    /// <summary>"app", "wsl", "wslService", or "herdr".</summary>
    public string Kind { get; set; } = "";
    public string? ProcessName { get; set; }
    public string? Path { get; set; }
    public string? WindowTitle { get; set; }
    public int? Port { get; set; }
    public string? WslCommand { get; set; }
    public bool PortProxy { get; set; }
    public List<string> Requires { get; set; } = new();
}
