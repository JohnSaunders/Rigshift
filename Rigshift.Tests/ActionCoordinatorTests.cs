using Rigshift.Execution;

namespace Rigshift.Tests;

public class ActionCoordinatorTests
{
    private sealed class RecordingExecutor : IStepExecutor
    {
        public List<Step> Executed { get; } = new();
        public void Execute(Step step) => Executed.Add(step);
    }

    private sealed class CountingLauncher : IElevationLauncher
    {
        public ElevationResult Result { get; set; } = new ElevationResult.Succeeded();
        public int Calls { get; private set; }
        public IReadOnlyList<Step>? LastSteps { get; private set; }
        public TimeSpan? LastTimeout { get; private set; }
        public CancellationToken? LastToken { get; private set; }

        public Task<ElevationResult> RunAsync(IReadOnlyList<Step> adminSteps, TimeSpan timeout, CancellationToken cancellationToken)
        {
            Calls++;
            LastSteps = adminSteps;
            LastTimeout = timeout;
            LastToken = cancellationToken;
            return Task.FromResult(Result);
        }
    }

    private sealed class ErrorReader
    {
        public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
        public int Reads { get; private set; }
        public IReadOnlyList<string> Read()
        {
            Reads++;
            return Errors;
        }
    }

    private static PartitionedPlan EmptyPlan() => new(Array.Empty<Step>(), Array.Empty<Step>());

    [Fact]
    public async Task EmptyPlanRunsNothingAndNeverCallsAdmin()
    {
        var executor = new RecordingExecutor();
        var launcher = new CountingLauncher();
        var reader = new ErrorReader();

        var outcome = await new ActionCoordinator().RunAsync(
            EmptyPlan(), executor, reader.Read, launcher,
            TimeSpan.FromSeconds(120), CancellationToken.None);

        Assert.Empty(outcome.UserStepErrors);
        Assert.Null(outcome.Elevation);
        Assert.False(outcome.AdminAttempted);
        Assert.Empty(executor.Executed);
        Assert.Equal(0, launcher.Calls);
    }

    [Fact]
    public async Task UserOnlyPlanRunsEveryStepInOrderAndReturnsItsErrors()
    {
        var executor = new RecordingExecutor();
        var launcher = new CountingLauncher();
        var reader = new ErrorReader { Errors = new[] { "first step failed" } };
        var plan = new PartitionedPlan(
            new Step[] { new StartProcess("a.exe"), new Wait(10), new StartProcess("b.exe") },
            Array.Empty<Step>());

        var outcome = await new ActionCoordinator().RunAsync(
            plan, executor, reader.Read, launcher,
            TimeSpan.FromSeconds(120), CancellationToken.None);

        Assert.Equal(3, executor.Executed.Count);
        Assert.IsType<StartProcess>(executor.Executed[0]);
        Assert.IsType<Wait>(executor.Executed[1]);
        Assert.IsType<StartProcess>(executor.Executed[2]);
        Assert.Equal(new[] { "first step failed" }, outcome.UserStepErrors);
        Assert.Null(outcome.Elevation);
        Assert.False(outcome.AdminAttempted);
        Assert.Equal(0, launcher.Calls);
        Assert.Equal(1, reader.Reads);
    }

    [Fact]
    public async Task PlanWithAdminSliceRunsUserStepsThenAwaitsAdminExactlyOnce()
    {
        var executor = new RecordingExecutor();
        var launcher = new CountingLauncher { Result = new ElevationResult.Failed(2) };
        var reader = new ErrorReader();
        var userSteps = new Step[] { new StartProcess("a.exe") };
        var adminSteps = new Step[] { new PortProxyRemove(8080) };
        var plan = new PartitionedPlan(userSteps, adminSteps);

        var outcome = await new ActionCoordinator().RunAsync(
            plan, executor, reader.Read, launcher,
            TimeSpan.FromSeconds(120), CancellationToken.None);

        Assert.Equal(userSteps, executor.Executed);
        Assert.Equal(1, launcher.Calls);
        Assert.Equal(adminSteps, launcher.LastSteps);
        Assert.Same(launcher.Result, outcome.Elevation);
        Assert.True(outcome.AdminAttempted);
    }

    [Fact]
    public async Task ErrorOnOneUserStepStillRunsLaterStepsAndReturnsErrors()
    {
        var executor = new RecordingExecutor();
        var launcher = new CountingLauncher();
        var reader = new ErrorReader { Errors = new[] { "second step failed" } };
        var steps = new Step[]
        {
            new StartProcess("a.exe"),
            new Wait(5),
            new StartProcess("b.exe"),
            new CloseWindow("Title")
        };
        var plan = new PartitionedPlan(steps, Array.Empty<Step>());

        var outcome = await new ActionCoordinator().RunAsync(
            plan, executor, reader.Read, launcher,
            TimeSpan.FromSeconds(120), CancellationToken.None);

        Assert.Equal(steps, executor.Executed);
        Assert.Equal(new[] { "second step failed" }, outcome.UserStepErrors);
        Assert.Null(outcome.Elevation);
        Assert.Equal(0, launcher.Calls);
    }

    [Fact]
    public async Task AdminTimeoutAndCancellationTokenPassThroughUnchanged()
    {
        var executor = new RecordingExecutor();
        var launcher = new CountingLauncher();
        var reader = new ErrorReader();
        var plan = new PartitionedPlan(
            new Step[] { new StartProcess("a.exe") },
            new Step[] { new FlushDns() });
        var timeout = TimeSpan.FromMilliseconds(1234);
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        await new ActionCoordinator().RunAsync(plan, executor, reader.Read, launcher, timeout, token);

        Assert.Equal(timeout, launcher.LastTimeout);
        Assert.Equal(token, launcher.LastToken);
    }

    [Fact]
    public async Task NullPlanThrows()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => new ActionCoordinator().RunAsync(
            null!, new RecordingExecutor(), new ErrorReader().Read, new CountingLauncher(),
            TimeSpan.FromSeconds(1), CancellationToken.None));
    }

    [Fact]
    public async Task NullUserExecutorThrows()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => new ActionCoordinator().RunAsync(
            EmptyPlan(), null!, new ErrorReader().Read, new CountingLauncher(),
            TimeSpan.FromSeconds(1), CancellationToken.None));
    }

    [Fact]
    public async Task NullGetUserErrorsThrows()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => new ActionCoordinator().RunAsync(
            EmptyPlan(), new RecordingExecutor(), null!, new CountingLauncher(),
            TimeSpan.FromSeconds(1), CancellationToken.None));
    }

    [Fact]
    public async Task NullAdminThrows()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => new ActionCoordinator().RunAsync(
            EmptyPlan(), new RecordingExecutor(), new ErrorReader().Read, null!,
            TimeSpan.FromSeconds(1), CancellationToken.None));
    }
}
