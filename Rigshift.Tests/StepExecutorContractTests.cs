using Rigshift.Execution;

namespace Rigshift.Tests;

public class StepExecutorContractTests
{
    /// <summary>Every Step must be handled; an unhandled one would silently do nothing.</summary>
    [Fact]
    public void ExecutorHandlesEveryStepType()
    {
        var stepTypes = typeof(Step).Assembly
            .GetTypes()
            .Where(t => t.IsSealed && typeof(Step).IsAssignableFrom(t))
            .ToList();

        Assert.NotEmpty(stepTypes);

        var executor = new RecordingStepExecutor();
        foreach (var step in AllSteps())
            executor.Execute(step);

        Assert.Equal(stepTypes.Count, executor.Executed.Select(s => s.GetType()).Distinct().Count());
    }

    private static IEnumerable<Step> AllSteps() => new Step[]
    {
        new WslPkill("x serve"), new Wait(1), new StartWslService("T", "cmd"),
        new PortProxyAdd(1), new PortProxyRemove(1), new KillProcess("x"),
        new CloseWindow("T"), new StartProcess("x.exe"),
        new EnsureWsl(), new ShutdownWsl(), new KillWslService(), new FlushDns(),
        new KillHerdr()
    };

    private sealed class RecordingStepExecutor : IStepExecutor
    {
        public List<Step> Executed { get; } = new();
        public void Execute(Step step) => Executed.Add(step);
    }
}
