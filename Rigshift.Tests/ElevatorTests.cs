using System.ComponentModel;
using System.Diagnostics;
using Rigshift.Execution;

namespace Rigshift.Tests;

public class ElevatorTests
{
    private const string ExePath = @"C:\Program Files\Rigshift\Rigshift.exe";
    private static readonly TimeSpan Long = TimeSpan.FromSeconds(30);
    private static readonly Step[] Admin = { new PortProxyRemove(11435), new FlushDns() };

    [Fact]
    public async Task TheHelperIsLaunchedWithRunasAndTheAdminPayload()
    {
        ProcessStartInfo? seen = null;
        var elevator = new Elevator(psi => { seen = psi; return Cmd("exit 0"); }, ExePath);

        await elevator.RunAsync(Admin, Long, CancellationToken.None);

        Assert.NotNull(seen);
        Assert.Equal(ExePath, seen!.FileName);
        Assert.Equal("runas", seen.Verb);
        Assert.True(seen.UseShellExecute);
        Assert.Equal("--admin proxy-remove:11435 flush-dns", seen.Arguments);
    }

    [Fact]
    public async Task ExitCodeZeroIsSucceeded()
    {
        var elevator = new Elevator(_ => Cmd("exit 0"), ExePath);

        var result = await elevator.RunAsync(Admin, Long, CancellationToken.None);

        Assert.IsType<ElevationResult.Succeeded>(result);
    }

    [Fact]
    public async Task ANonZeroExitCodeIsFailedWithThatCode()
    {
        var elevator = new Elevator(_ => Cmd("exit 2"), ExePath);

        var result = await elevator.RunAsync(Admin, Long, CancellationToken.None);

        Assert.Equal(new ElevationResult.Failed(2), result);
    }

    [Fact]
    public async Task ADeclinedPromptIsDeclined()
    {
        var elevator = new Elevator(_ => throw new Win32Exception(1223), ExePath);

        var result = await elevator.RunAsync(Admin, Long, CancellationToken.None);

        Assert.IsType<ElevationResult.Declined>(result);
    }

    [Fact]
    public async Task AnotherStartErrorIsStartFailed()
    {
        var elevator = new Elevator(_ => throw new Win32Exception(2), ExePath);

        var result = await elevator.RunAsync(Admin, Long, CancellationToken.None);

        Assert.IsType<ElevationResult.StartFailed>(result);
    }

    [Fact]
    public async Task ANullProcessIsStartFailed()
    {
        var elevator = new Elevator(_ => null, ExePath);

        var result = await elevator.RunAsync(Admin, Long, CancellationToken.None);

        Assert.IsType<ElevationResult.StartFailed>(result);
    }

    [Fact]
    public async Task AnExpiredWaitIsTimedOutAndLeavesTheHelperRunning()
    {
        int pid = 0;
        var elevator = new Elevator(_ => { var p = Cmd("ping -n 8 -w 100 127.0.0.1 >nul"); pid = p.Id; return p; }, ExePath);
        try
        {
            var result = await elevator.RunAsync(Admin, TimeSpan.FromMilliseconds(300), CancellationToken.None);

            Assert.IsType<ElevationResult.TimedOut>(result);
            Assert.NotEqual(0, pid);
            using var stillRunning = Process.GetProcessById(pid);
            Assert.False(stillRunning.HasExited);
        }
        finally
        {
            KillById(pid);
        }
    }

    [Fact]
    public async Task ACancelledCallerTokenPropagatesOperationCanceled()
    {
        int pid = 0;
        var elevator = new Elevator(_ => { var p = Cmd("ping -n 8 -w 100 127.0.0.1 >nul"); pid = p.Id; return p; }, ExePath);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        try
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => elevator.RunAsync(Admin, Long, cancelled.Token));
        }
        finally
        {
            KillById(pid);
        }
    }

    [Fact]
    public async Task AnEmptyOrUserStepListThrowsBeforeAnythingStarts()
    {
        var started = false;
        var elevator = new Elevator(_ => { started = true; return null; }, ExePath);

        await Assert.ThrowsAsync<ArgumentException>(
            () => elevator.RunAsync(Array.Empty<Step>(), Long, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(
            () => elevator.RunAsync(new Step[] { new StartProcess("x.exe") }, Long, CancellationToken.None));
        Assert.False(started);
    }

    /// <summary>Starts cmd.exe directly, never with runas, so no UAC prompt appears.</summary>
    private static Process Cmd(string command) =>
        Process.Start(new ProcessStartInfo("cmd.exe", $"/c {command}")
        {
            UseShellExecute = false,
            CreateNoWindow = true
        })!;

    private static void KillById(int pid)
    {
        if (pid == 0) return;
        try
        {
            using var live = Process.GetProcessById(pid);
            live.Kill(entireProcessTree: true);
        }
        catch (ArgumentException)
        {
            // Already exited.
        }
        catch (InvalidOperationException)
        {
            // Already exited.
        }
    }
}
