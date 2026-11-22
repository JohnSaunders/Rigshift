using Rigshift.Config;
using Rigshift.Execution;

namespace Rigshift.Tests;

public class PlanPartitionTests
{
    private const string WslCommand =
        "OLLAMA_HOST=0.0.0.0:11435 OLLAMA_MODELS=/usr/share/ollama/.ollama/models ollama serve";

    [Fact]
    public void SplittingAnEmptyPlanGivesEmptySlicesWithNoElevation()
    {
        var result = PlanPartition.Split(Array.Empty<Step>());

        Assert.Empty(result.User);
        Assert.Empty(result.Admin);
        Assert.False(result.NeedsElevation);
    }

    [Fact]
    public void EnablingNativeOllamaNeedsNoElevation()
    {
        var plan = ActionRunner.PlanEnable(ConfigLoader.Default(), "AI");
        var result = PlanPartition.Split(plan.Steps);

        Assert.False(result.NeedsElevation);
        Assert.Empty(result.Admin);
        Assert.Equal(plan.Steps, result.User);
    }

    [Fact]
    public void EnablingDevNeedsNoElevation()
    {
        var plan = ActionRunner.PlanEnable(ConfigLoader.Default(), "Dev");
        var result = PlanPartition.Split(plan.Steps);

        Assert.False(result.NeedsElevation);
        Assert.Empty(result.Admin);
        Assert.Equal(plan.Steps, result.User);
    }

    [Fact]
    public void EnablingWslHostedOllamaPutsTheProxyStepsInTheAdminSlice()
    {
        var plan = ActionRunner.PlanEnable(WslHostedOllama.Config(), "AI");
        var result = PlanPartition.Split(plan.Steps);

        // The admin slice is the two proxy steps, in planner order.
        Assert.Equal(new Step[]
        {
            new PortProxyRemove(11435),
            new PortProxyAdd(11435)
        }, result.Admin);

        // The user slice keeps the non-elevated steps in planner order. It leads
        // with EnsureWsl because the "ollama" definition requires "wsl".
        Assert.Equal(new Step[]
        {
            new EnsureWsl(),
            new WslPkill("ollama serve"),
            new Wait(1000),
            new StartWslService("Ollama WSL", WslCommand),
            new Wait(3000)
        }, result.User);
    }

    [Fact]
    public void TheGameOneShotPutsTheEscalationStepsInTheAdminSlice()
    {
        var plan = ActionRunner.PlanOneShot(ConfigLoader.Default(), "Game");
        var result = PlanPartition.Split(plan.Steps);

        Assert.Equal(new Step[]
        {
            new KillWslService(),
            new FlushDns()
        }, result.Admin);

        // Every non-elevated step stays in its original relative order, with
        // ShutdownWsl last.
        Assert.Equal(new Step[]
        {
            new KillProcess("node"),
            new KillProcess("ollama app"),
            new KillProcess("ollama"),
            new KillProcess("llama-server"),
            new ShutdownWsl()
        }, result.User);
        Assert.IsType<ShutdownWsl>(result.User[^1]);
    }

    [Fact]
    public void SplittingAMixedListKeepsRelativeOrderWithinEachSlice()
    {
        var result = PlanPartition.Split(new Step[]
        {
            new FlushDns(),
            new StartProcess("a"),
            new PortProxyAdd(1),
            new KillProcess("b")
        });

        Assert.Equal(new Step[]
        {
            new StartProcess("a"),
            new KillProcess("b")
        }, result.User);
        Assert.Equal(new Step[]
        {
            new FlushDns(),
            new PortProxyAdd(1)
        }, result.Admin);
    }

    [Fact]
    public void SplittingNullThrowsAnArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => PlanPartition.Split(null!));
    }
}
