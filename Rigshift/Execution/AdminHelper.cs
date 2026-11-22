namespace Rigshift.Execution;

/// <summary>
/// The elevated helper's whole job: run the closed <c>--admin</c> payload and report an
/// exit code. It reads no config and takes no path, process name, or command text, so
/// nothing in the user-writable config can reach the elevated process. See the
/// Elevation section of docs/configuration.md.
/// </summary>
public static class AdminHelper
{
    public const int Succeeded = 0;
    public const int StepErrors = 2;

    public const string RejectedMessage = "Rejected administrator request. Nothing was run.";

    /// <summary>
    /// Parses <paramref name="args"/> with <see cref="AdminPayload.TryParse"/>. A rejected
    /// payload runs nothing, reports once, and returns <see cref="AdminPayload.RejectedExitCode"/>.
    /// Otherwise every step runs in order, even after an earlier one reported an error; any
    /// errors are reported once, joined by newlines, and the result is <see cref="StepErrors"/>.
    /// </summary>
    public static int Run(
        string[] args, IStepExecutor executor, Func<IReadOnlyList<string>> errors, Action<string> report)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(errors);
        ArgumentNullException.ThrowIfNull(report);

        if (!AdminPayload.TryParse(args, out var steps))
        {
            report(RejectedMessage);
            return AdminPayload.RejectedExitCode;
        }

        foreach (var step in steps)
            executor.Execute(step);

        var problems = errors();
        if (problems.Count == 0)
            return Succeeded;

        report(string.Join(Environment.NewLine, problems));
        return StepErrors;
    }
}
