using Rigshift.Execution;

namespace Rigshift.Tests;

public class StepClassTests
{
    [Fact]
    public void PortProxyAddRequiresElevation()
    {
        Assert.True(StepClass.RequiresElevation(new PortProxyAdd(1)));
    }

    [Fact]
    public void PortProxyRemoveRequiresElevation()
    {
        Assert.True(StepClass.RequiresElevation(new PortProxyRemove(1)));
    }

    [Fact]
    public void KillWslServiceRequiresElevation()
    {
        Assert.True(StepClass.RequiresElevation(new KillWslService()));
    }

    [Fact]
    public void FlushDnsRequiresElevation()
    {
        Assert.True(StepClass.RequiresElevation(new FlushDns()));
    }

    [Fact]
    public void StartProcessDoesNotRequireElevation()
    {
        Assert.False(StepClass.RequiresElevation(new StartProcess("x.exe")));
    }

    [Fact]
    public void KillProcessDoesNotRequireElevation()
    {
        Assert.False(StepClass.RequiresElevation(new KillProcess("x")));
    }

    [Fact]
    public void CloseWindowDoesNotRequireElevation()
    {
        Assert.False(StepClass.RequiresElevation(new CloseWindow("T")));
    }

    [Fact]
    public void WaitDoesNotRequireElevation()
    {
        Assert.False(StepClass.RequiresElevation(new Wait(1)));
    }

    [Fact]
    public void EnsureWslDoesNotRequireElevation()
    {
        Assert.False(StepClass.RequiresElevation(new EnsureWsl()));
    }

    [Fact]
    public void WslPkillDoesNotRequireElevation()
    {
        Assert.False(StepClass.RequiresElevation(new WslPkill("x serve")));
    }

    [Fact]
    public void StartWslServiceDoesNotRequireElevation()
    {
        Assert.False(StepClass.RequiresElevation(new StartWslService("T", "cmd")));
    }

    [Fact]
    public void ShutdownWslDoesNotRequireElevation()
    {
        Assert.False(StepClass.RequiresElevation(new ShutdownWsl()));
    }

    [Fact]
    public void EveryStepTypeIsClassifiedWithoutThrowing()
    {
        var stepTypes = typeof(Step).Assembly
            .GetTypes()
            .Where(t => t.IsSealed && typeof(Step).IsAssignableFrom(t))
            .ToList();

        Assert.Equal(13, stepTypes.Count);

        foreach (var step in AllSteps())
            StepClass.RequiresElevation(step);
    }

    [Fact]
    public void NullStepThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => StepClass.RequiresElevation(null!));
    }

    private static IEnumerable<Step> AllSteps() => new Step[]
    {
        new WslPkill("x serve"), new Wait(1), new StartWslService("T", "cmd"),
        new PortProxyAdd(1), new PortProxyRemove(1), new KillProcess("x"),
        new CloseWindow("T"), new StartProcess("x.exe"),
        new EnsureWsl(), new ShutdownWsl(), new KillWslService(), new FlushDns(),
        new KillHerdr()
    };
}
