using Rigshift.Execution;

namespace Rigshift.Tests;

public class AdminHelperTests
{
    [Fact]
    public void AValidPayloadRunsEveryStepInOrderAndReturnsZeroWithoutReporting()
    {
        var executor = new RecordingExecutor();
        var reports = new List<string>();

        var code = AdminHelper.Run(
            new[] { "--admin", "flush-dns", "kill-wslservice" }, executor, () => executor.Errors, reports.Add);

        Assert.Equal(AdminHelper.Succeeded, code);
        Assert.Equal(new Step[] { new FlushDns(), new KillWslService() }, executor.Executed);
        Assert.Empty(reports);
    }

    [Fact]
    public void AStepErrorStillRunsLaterStepsReportsOnceAndReturnsTwo()
    {
        var executor = new RecordingExecutor { FailOn = typeof(FlushDns) };
        var reports = new List<string>();

        var code = AdminHelper.Run(
            new[] { "--admin", "flush-dns", "kill-wslservice" }, executor, () => executor.Errors, reports.Add);

        Assert.Equal(AdminHelper.StepErrors, code);
        Assert.Equal(new Step[] { new FlushDns(), new KillWslService() }, executor.Executed);
        Assert.Equal(new[] { "FlushDns failed" }, reports);
    }

    [Fact]
    public void SeveralStepErrorsAreReportedOnceJoinedByNewlines()
    {
        var executor = new RecordingExecutor { FailAll = true };
        var reports = new List<string>();

        var code = AdminHelper.Run(
            new[] { "--admin", "flush-dns", "kill-wslservice" }, executor, () => executor.Errors, reports.Add);

        Assert.Equal(AdminHelper.StepErrors, code);
        Assert.Equal(new[] { $"FlushDns failed{Environment.NewLine}KillWslService failed" }, reports);
    }

    public static IEnumerable<object?[]> RejectedArgumentSets() =>
    [
        [null],
        [Array.Empty<string>()],
        [new[] { "--run", "AI", "enable", "" }],
        [new[] { "--admin", "start:calc.exe" }],
        [new[] { "--admin", "flush-dns", "start:calc.exe" }]
    ];

    [Theory]
    [MemberData(nameof(RejectedArgumentSets))]
    public void ARejectedPayloadRunsNothingReportsOnceAndReturnsSixtyFour(string[]? args)
    {
        var executor = new RecordingExecutor();
        var reports = new List<string>();

        var code = AdminHelper.Run(args!, executor, () => executor.Errors, reports.Add);

        Assert.Equal(AdminPayload.RejectedExitCode, code);
        Assert.Empty(executor.Executed);
        Assert.Equal(new[] { AdminHelper.RejectedMessage }, reports);
    }

    [Fact]
    public void NullSeamsThrowArgumentNullException()
    {
        var executor = new RecordingExecutor();
        var args = new[] { "--admin", "flush-dns" };

        Assert.Throws<ArgumentNullException>(() => AdminHelper.Run(args, null!, () => executor.Errors, _ => { }));
        Assert.Throws<ArgumentNullException>(() => AdminHelper.Run(args, executor, null!, _ => { }));
        Assert.Throws<ArgumentNullException>(() => AdminHelper.Run(args, executor, () => executor.Errors, null!));
        Assert.Empty(executor.Executed);
    }

    private sealed class RecordingExecutor : IStepExecutor
    {
        private readonly List<string> _errors = new();

        public List<Step> Executed { get; } = new();
        public IReadOnlyList<string> Errors => _errors;
        public Type? FailOn { get; init; }
        public bool FailAll { get; init; }

        public void Execute(Step step)
        {
            Executed.Add(step);
            if (FailAll || step.GetType() == FailOn)
                _errors.Add($"{step.GetType().Name} failed");
        }
    }
}
