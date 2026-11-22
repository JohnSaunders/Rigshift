using Rigshift.Config;
using Rigshift.Execution;

namespace Rigshift.Tests;

public class ActionRunnerDisableTests
{
    private static RigshiftConfig Config() => ConfigLoader.Default();

    [Fact]
    public void DisablingAiKillsTheTrayAppServerAndRunner()
    {
        var plan = ActionRunner.PlanDisable(Config(), "AI", Array.Empty<string>());

        Assert.Equal(new Step[]
        {
            new KillProcess("ollama app"),
            new KillProcess("ollama"),
            new KillProcess("llama-server")
        }, plan.Steps);
    }

    [Fact]
    public void DisablingWslHostedOllamaStopsTheServiceAndRemovesTheProxy()
    {
        var plan = ActionRunner.PlanDisable(WslHostedOllama.Config(), "AI", Array.Empty<string>());

        Assert.Equal(new Step[]
        {
            new KillProcess("ollama"),
            new CloseWindow("Ollama WSL"),
            new PortProxyRemove(11435)
        }, plan.Steps);
    }

    [Fact]
    public void DisablingDevShutsWslDownWhenNothingElseNeedsIt()
    {
        var plan = ActionRunner.PlanDisable(Config(), "Dev", Array.Empty<string>());

        Assert.Equal(new Step[] { new ShutdownWsl() }, plan.Steps);
    }

    [Fact]
    public void DisablingDevLeavesWslUpWhenAiIsStillEnabled()
    {
        // The WSL-hosted "ollama" requires "wsl", so WSL is still spoken for.
        var plan = ActionRunner.PlanDisable(WslHostedOllama.Config(), "Dev", new[] { "AI" });

        Assert.Empty(plan.Steps);
    }

    [Fact]
    public void DisablingDoesNotEscalateToHardShutdown()
    {
        var plan = ActionRunner.PlanDisable(Config(), "Dev", Array.Empty<string>());

        Assert.DoesNotContain(plan.Steps, s => s is KillWslService);
        Assert.DoesNotContain(plan.Steps, s => s is FlushDns);
    }

    [Fact]
    public void GameKillsEverythingRegardlessOfWhatIsEnabled()
    {
        var plan = ActionRunner.PlanOneShot(Config(), "Game");

        Assert.Equal(new Step[]
        {
            new KillProcess("node"),
            new KillProcess("ollama app"),
            new KillProcess("ollama"),
            new KillProcess("llama-server"),
            new ShutdownWsl(),
            new KillWslService(),
            new FlushDns()
        }, plan.Steps);
    }

    [Fact]
    public void EscalationStepsComeLastAndOnlyOnce()
    {
        var config = ConfigLoader.Default();
        config.Loadouts.Add(new Loadout
        {
            Name = "Double", Toggle = false,
            Kill = { "wsl", "ollama" }, HardShutdown = true, FlushDns = true
        });

        var plan = ActionRunner.PlanOneShot(config, "Double");

        Assert.Single(plan.Steps, s => s is KillWslService);
        Assert.Single(plan.Steps, s => s is FlushDns);
        Assert.IsType<FlushDns>(plan.Steps[^1]);
        Assert.IsType<KillWslService>(plan.Steps[^2]);
    }
}
