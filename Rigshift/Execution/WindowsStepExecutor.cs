using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;

namespace Rigshift.Execution;

/// <summary>
/// The only place that shells out. Holds no decisions — it just performs steps.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsStepExecutor : IStepExecutor
{
    private readonly List<string> _errors = new();
    public IReadOnlyList<string> Errors => _errors;

    public void Execute(Step step)
    {
        switch (step)
        {
            case Wait w:
                Thread.Sleep(w.Milliseconds);
                break;

            case WslPkill p:
                if (!ShellText.IsSafe(p.Pattern)) { Refuse("wsl pkill pattern", p.Pattern, "a quote or percent sign"); break; }
                // Absent process exits non-zero; that is the normal case.
                Run("wsl.exe", $"pkill -f \"{p.Pattern}\"", ignoreExitCode: true);
                break;

            case StartWslService s:
                StartDetachedWsl(s.WindowTitle, s.WslCommand);
                break;

            case PortProxyRemove r:
                Run("netsh.exe",
                    $"interface portproxy delete v4tov4 listenport={r.Port} listenaddress=127.0.0.1",
                    ignoreExitCode: true);
                break;

            case PortProxyAdd a:
                AddPortProxy(a.Port);
                break;

            case KillProcess k:
                if (!ShellText.IsSafe(k.ProcessName)) { Refuse("process name", k.ProcessName, "a quote or percent sign"); break; }
                Run("taskkill.exe", $"/f /im \"{k.ProcessName}.exe\"", ignoreExitCode: true);
                break;

            case CloseWindow c:
                if (!ShellText.IsSafe(c.WindowTitle)) { Refuse("window title", c.WindowTitle, "a quote or percent sign"); break; }
                Run("taskkill.exe", $"/f /fi \"WINDOWTITLE eq {c.WindowTitle}*\"", ignoreExitCode: true);
                break;

            case StartProcess sp:
                var expandedPath = Environment.ExpandEnvironmentVariables(sp.Path);
                try { Process.Start(new ProcessStartInfo(expandedPath) { UseShellExecute = true }); }
                catch (Exception ex) { _errors.Add($"Could not start '{expandedPath}': {ex.Message}"); }
                break;

            case EnsureWsl:
                Run("wsl.exe", "-e true", ignoreExitCode: true);
                break;

            case ShutdownWsl:
                Run("wsl.exe", "--shutdown", ignoreExitCode: true);
                break;

            case KillWslService:
                Run("taskkill.exe", "/f /im \"wslservice.exe\"", ignoreExitCode: true);
                break;

            case FlushDns:
                Run("ipconfig.exe", "/flushdns", ignoreExitCode: true);
                break;

            case KillHerdr:
                // Scoped to this account so the elevated kill never reaches another user's herdr.
                Run("taskkill.exe",
                    $"/f /t /fi \"USERNAME eq {Environment.UserDomainName}\\{Environment.UserName}\" /im \"herdr.exe\"",
                    ignoreExitCode: true);
                break;

            default:
                _errors.Add($"Unhandled step {step.GetType().Name}.");
                break;
        }
    }

    /// <summary>Records a refused step so the reason reaches the user, not just the log.</summary>
    private void Refuse(string what, string? value, string reason) =>
        _errors.Add($"Refused {what} '{Short(value)}': {reason} would change the assembled command line.");

    private static string Short(string? value) =>
        value is null ? "" : value.Length <= 60 ? value : value[..60] + "...";

    /// <summary>Reproduces: start "&lt;title&gt;" /min wsl -e bash -c "&lt;command&gt;"</summary>
    private void StartDetachedWsl(string windowTitle, string command)
    {
        if (!ShellText.IsSafe(windowTitle)) { Refuse("window title", windowTitle, "a quote or percent sign"); return; }
        if (!ShellText.IsSafeCommand(command)) { Refuse("WSL command", command, "a quote"); return; }
        var psi = new ProcessStartInfo("cmd.exe",
            $"/c start \"{windowTitle}\" /min wsl -e bash -c \"{command}\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true
        };
        try { Process.Start(psi); }
        catch (Exception ex) { _errors.Add($"Could not start '{windowTitle}': {ex.Message}"); }
    }

    private void AddPortProxy(int port)
    {
        var ip = ResolveWslIp();
        if (ip is null)
        {
            _errors.Add("Could not resolve the WSL IP address; port proxy not created.");
            return;
        }
        Run("netsh.exe",
            $"interface portproxy add v4tov4 listenport={port} listenaddress=127.0.0.1 " +
            $"connectport={port} connectaddress={ip}",
            ignoreExitCode: false);
    }

    /// <summary>First token of `wsl hostname -I`; it must be a strict IPv4 address before netsh sees it.</summary>
    private string? ResolveWslIp()
    {
        var output = Capture("wsl.exe", "hostname -I");
        return WslIp.TryParseFirst(output, out var ip) ? ip : null;
    }

    private void Run(string exe, string args, bool ignoreExitCode)
    {
        try
        {
            var psi = new ProcessStartInfo(exe, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var process = Process.Start(psi);
            if (process is null) return;
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            process.WaitForExit(30_000);
            Task.WaitAll(stdoutTask, stderrTask);
            var stderr = stderrTask.Result;
            if (!ignoreExitCode && process.ExitCode != 0)
                _errors.Add($"{exe} {args} exited {process.ExitCode}. {stderr.Trim()}");
        }
        catch (Exception ex)
        {
            _errors.Add($"{exe} failed: {ex.Message}");
        }
    }

    private string? Capture(string exe, string args)
    {
        try
        {
            var psi = new ProcessStartInfo(exe, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.UTF8
            };
            using var process = Process.Start(psi);
            if (process is null) return null;
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(15_000);
            return output;
        }
        catch (Exception ex)
        {
            _errors.Add($"{exe} failed: {ex.Message}");
            return null;
        }
    }
}
