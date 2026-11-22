using Rigshift.Config;
using Rigshift.Execution;

namespace Rigshift.Tests;

public class HeldLoadoutAnalyzerTests
{
    private static RigshiftConfig Config() => ConfigLoader.Default();

    [Fact]
    public void NativeAiDoesNotHoldDev()
    {
        var result = HeldLoadoutAnalyzer.Analyze(Config(), "Dev", new[] { "Dev", "AI" });

        Assert.False(result.Held);
        Assert.Empty(result.Holders);
    }

    [Fact]
    public void DevIsHeldWhenAnotherLoadoutRequiresWsl()
    {
        // The WSL-hosted "ollama" requires "wsl", so disabling Dev alone leaves WSL up.
        var result = HeldLoadoutAnalyzer.Analyze(WslHostedOllama.Config(), "Dev", new[] { "Dev", "AI" });

        Assert.True(result.Held);
        Assert.Equal(new[] { "AI" }, result.Holders);
    }

    [Fact]
    public void DevIsNotHeldWhenNothingElseIsEnabled()
    {
        var result = HeldLoadoutAnalyzer.Analyze(Config(), "Dev", new[] { "Dev" });

        Assert.False(result.Held);
        Assert.Empty(result.Holders);
    }

    [Fact]
    public void AiIsNeverHeldSinceNothingElseNeedsOllama()
    {
        // Disabling AI always has a non-empty plan, regardless of what else is enabled,
        // because nothing else's Start needs "ollama".
        var result = HeldLoadoutAnalyzer.Analyze(Config(), "AI", new[] { "Dev", "AI" });

        Assert.False(result.Held);
        Assert.Empty(result.Holders);
    }

    [Fact]
    public void OnlyLoadoutsThatActuallyRequireTheSameDefinitionAreNamedAsHolders()
    {
        var config = WslHostedOllama.Config();
        // "Editor" needs "rider" only — it has nothing to do with wsl, so it must
        // not be reported as a holder when Dev is disabled.
        config.Loadouts.Add(new Loadout
        {
            Name = "Editor", Toggle = true,
            Start = { "rider" }, KillOnDisable = { "rider" }
        });

        var result = HeldLoadoutAnalyzer.Analyze(config, "Dev", new[] { "Dev", "AI", "Editor" });

        Assert.True(result.Held);
        Assert.Equal(new[] { "AI" }, result.Holders);
    }

    [Fact]
    public void UnknownLoadoutNameIsTreatedAsHeldWithNoHolders()
    {
        var result = HeldLoadoutAnalyzer.Analyze(Config(), "NoSuchLoadout", new[] { "NoSuchLoadout" });

        Assert.True(result.Held);
        Assert.Empty(result.Holders);
    }
}
