using Rigshift.Config;
using Rigshift.Execution;

namespace Rigshift.Tests;

public class OneShotIntentAnalyzerTests
{
    private static RigshiftConfig Config() => ConfigLoader.Default();

    [Fact]
    public void GameClearsAiAndDevWhenBothAreEnabled()
    {
        var result = OneShotIntentAnalyzer.Cleared(Config(), "Game", new[] { "AI", "Dev" });

        Assert.Equal(new[] { "AI", "Dev" }, result);
    }

    [Fact]
    public void OneShotKillingOnlyRiderClearsNothing()
    {
        var config = Config();
        config.Loadouts.Add(new Loadout
        {
            Name = "RiderOnly", Toggle = false,
            Kill = new List<string> { "rider" }
        });

        var result = OneShotIntentAnalyzer.Cleared(config, "RiderOnly", new[] { "AI", "Dev" });

        Assert.Empty(result);
    }

    [Fact]
    public void ToggleStartingOllamaAndVscodeIsClearedWhenOnlyOllamaIsKilled()
    {
        var config = new RigshiftConfig
        {
            Definitions = new Dictionary<string, Definition>
            {
                ["ollama"] = new() { Kind = "app", ProcessName = "ollama app" },
                ["vscode"] = new() { Kind = "app", ProcessName = "Code" },
                ["rider"] = new() { Kind = "app", ProcessName = "rider64" }
            },
            Loadouts = new List<Loadout>
            {
                new() { Name = "Mixed", Toggle = true, Start = new List<string> { "ollama", "vscode" } },
                new() { Name = "Editor", Toggle = true, Start = new List<string> { "rider" } },
                new() { Name = "KillOllama", Toggle = false, Kill = new List<string> { "ollama" } }
            }
        };

        var result = OneShotIntentAnalyzer.Cleared(config, "KillOllama", new[] { "Mixed", "Editor" });

        Assert.Equal(new[] { "Mixed" }, result);
    }

    [Fact]
    public void KillingOnlyWslClearsAiAndDevInWslHostedConfig()
    {
        var config = WslHostedOllama.Config();
        config.Loadouts.Add(new Loadout
        {
            Name = "KillWsl", Toggle = false,
            Kill = new List<string> { "wsl" }
        });

        var result = OneShotIntentAnalyzer.Cleared(config, "KillWsl", new[] { "AI", "Dev" });

        Assert.Equal(new[] { "AI", "Dev" }, result);
    }

    [Fact]
    public void HardShutdownKillingOnlyNodeClearsAiAndDevButKeepsAppToggle()
    {
        var config = WslHostedOllama.Config();
        config.Loadouts.Add(new Loadout
        {
            Name = "RiderOnly", Toggle = true,
            Start = new List<string> { "rider" }
        });
        config.Loadouts.Add(new Loadout
        {
            Name = "KillNode", Toggle = false,
            Kill = new List<string> { "node" },
            HardShutdown = true
        });

        var result = OneShotIntentAnalyzer.Cleared(config, "KillNode", new[] { "AI", "Dev", "RiderOnly" });

        Assert.Equal(new[] { "AI", "Dev" }, result);
    }

    [Fact]
    public void UnknownOneShotNameClearsNothing()
    {
        var result = OneShotIntentAnalyzer.Cleared(Config(), "NoSuchOneShot", new[] { "AI", "Dev" });

        Assert.Empty(result);
    }

    [Fact]
    public void ToggleNamePassedAsOneShotClearsNothing()
    {
        var result = OneShotIntentAnalyzer.Cleared(Config(), "AI", new[] { "AI", "Dev" });

        Assert.Empty(result);
    }

    [Fact]
    public void EnabledNameMatchingNoLoadoutIsSkipped()
    {
        var result = OneShotIntentAnalyzer.Cleared(Config(), "Game", new[] { "AI", "Dev", "NoSuchToggle" });

        Assert.Equal(new[] { "AI", "Dev" }, result);
    }
}
