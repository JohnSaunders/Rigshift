using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace Rigshift.Execution;

/// <summary>What happened to one elevated helper run.</summary>
public abstract record ElevationResult
{
    /// <summary>The helper exited 0: every admin step succeeded.</summary>
    public sealed record Succeeded : ElevationResult;

    /// <summary>The user dismissed the UAC prompt. Nothing ran.</summary>
    public sealed record Declined : ElevationResult;

    /// <summary>The helper could not be started for a reason other than a declined prompt.</summary>
    public sealed record StartFailed(string Message) : ElevationResult;

    /// <summary>The helper exited non-zero: 2 for step errors, 64 for a rejected payload.</summary>
    public sealed record Failed(int ExitCode) : ElevationResult;

    /// <summary>The wait expired; the helper may still be running and was not killed.</summary>
    public sealed record TimedOut : ElevationResult;
}

/// <summary>Runs an admin slice in the elevated helper. A seam so callers can be tested without UAC.</summary>
public interface IElevationLauncher
{
    Task<ElevationResult> RunAsync(IReadOnlyList<Step> adminSteps, TimeSpan timeout, CancellationToken cancellationToken);
}

/// <summary>
/// The tray process stays unelevated. Only the admin slice of an action runs as
/// administrator, in a short-lived copy of this executable launched with <c>runas</c>
/// and the closed <c>--admin</c> payload (<see cref="AdminPayload"/>).
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class Elevator : IElevationLauncher
{
    private const int ErrorCancelled = 1223;

    private readonly Func<ProcessStartInfo, Process?> _start;
    private readonly string? _exePath;

    /// <param name="start">Process launcher, defaulting to <see cref="Process.Start"/>.</param>
    /// <param name="exePath">The helper executable, defaulting to <see cref="Environment.ProcessPath"/>.</param>
    public Elevator(Func<ProcessStartInfo, Process?>? start = null, string? exePath = null)
    {
        _start = start ?? Process.Start;
        _exePath = exePath ?? Environment.ProcessPath;
    }

    public async Task<ElevationResult> RunAsync(
        IReadOnlyList<Step> adminSteps, TimeSpan timeout, CancellationToken cancellationToken)
    {
        // Validates before anything starts: throws for an empty list or a user step.
        var tokens = AdminPayload.Format(adminSteps);

        if (_exePath is null)
            return new ElevationResult.StartFailed("Could not determine the executable path.");

        // AdminPayload tokens are ASCII letters, digits, '-' and ':', so no quoting is needed.
        var psi = new ProcessStartInfo(_exePath)
        {
            UseShellExecute = true,
            Verb = "runas",
            Arguments = string.Join(' ', tokens)
        };

        Process? process;
        try
        {
            process = _start(psi);
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == ErrorCancelled)
        {
            return new ElevationResult.Declined();
        }
        catch (Exception ex)
        {
            return new ElevationResult.StartFailed(ex.Message);
        }

        if (process is null)
            return new ElevationResult.StartFailed("The administrator helper did not start.");

        using (process)
        {
            using var timeoutSource = new CancellationTokenSource(timeout);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);
            try
            {
                await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Deliberately not killed: the helper may be mid-teardown, and stopping it
                // could leave WSL or the port proxy half changed.
                return new ElevationResult.TimedOut();
            }

            return process.ExitCode == 0
                ? new ElevationResult.Succeeded()
                : new ElevationResult.Failed(process.ExitCode);
        }
    }

}
