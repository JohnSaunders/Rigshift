using Rigshift.Config;

namespace Rigshift.Tests;

/// <summary>
/// A config that hosts Ollama inside WSL. The built-in default is the native
/// Windows app; these tests keep coverage for the wslService kind.
/// </summary>
internal static class WslHostedOllama
{
    public static RigshiftConfig Config() => new()
    {
        IdleColor = "#8A8A8E",
        Definitions = new Dictionary<string, Definition>
        {
            ["ollama"] = new()
            {
                Kind = "wslService",
                ProcessName = "ollama",
                WindowTitle = "Ollama WSL",
                Port = 11435,
                WslCommand = "OLLAMA_HOST=0.0.0.0:11435 OLLAMA_MODELS=/usr/share/ollama/.ollama/models ollama serve",
                PortProxy = true,
                Requires = new List<string> { "wsl" }
            },
            ["wsl"] = new() { Kind = "wsl" },
            ["rider"] = new() { Kind = "app", ProcessName = "rider64" },
            ["webstorm"] = new() { Kind = "app", ProcessName = "webstorm64" },
            ["vscode"] = new() { Kind = "app", ProcessName = "Code" },
            ["node"] = new() { Kind = "app", ProcessName = "node" }
        },
        Loadouts = new List<Loadout>
        {
            new()
            {
                Name = "AI", Toggle = true, Color = "#2E6BFF",
                Start = new List<string> { "ollama" },
                KillOnDisable = new List<string> { "ollama" }
            },
            new()
            {
                Name = "Dev", Toggle = true, Color = "#FF3B30",
                Start = new List<string> { "wsl" },
                KillOnDisable = new List<string> { "wsl" }
            },
            new()
            {
                Name = "Game", Toggle = false,
                Kill = new List<string> { "rider", "webstorm", "vscode", "node", "ollama", "wsl" },
                HardShutdown = true, FlushDns = true
            }
        }
    };
}
