using Rigshift.Execution;

namespace Rigshift.Tests;

public class ShellTextTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("rider64")]
    [InlineData("My Editor")]
    [InlineData("ollama serve")]
    public void IsSafe_accepts_a_title_process_name_or_pattern_without_quotes_or_percent(string? value)
    {
        Assert.True(ShellText.IsSafe(value));
    }

    [Theory]
    [InlineData("Editor\" & calc.exe")]
    [InlineData("note%TEMP%")]
    [InlineData("\"")]
    [InlineData("%")]
    public void IsSafe_refuses_a_quote_or_a_percent_sign(string value)
    {
        Assert.False(ShellText.IsSafe(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("cd ~ && ollama serve")]
    [InlineData("date +%s")]
    public void IsSafeCommand_allows_bash_text_including_the_percent_sign(string? value)
    {
        Assert.True(ShellText.IsSafeCommand(value));
    }

    [Fact]
    public void IsSafeCommand_refuses_a_quote()
    {
        Assert.False(ShellText.IsSafeCommand("echo \"hi\""));
    }
}

/// <summary>
/// Each of these steps would otherwise spawn taskkill, wsl or cmd. The assertion
/// that no other error is present is what shows nothing ran: a spawned process
/// that failed would have added an "exited" or "failed" line instead.
/// </summary>
public class StepRefusalTests
{
    private static IReadOnlyList<string> ErrorsAfter(Step step)
    {
        var executor = new WindowsStepExecutor();
        executor.Execute(step);
        return executor.Errors;
    }

    private static void AssertRefusedAndNothingRan(IReadOnlyList<string> errors, string expected)
    {
        Assert.Contains(errors, e => e.StartsWith(expected, StringComparison.Ordinal));
        Assert.DoesNotContain(errors, e => e.Contains("exited", StringComparison.Ordinal)
                                        || e.Contains("failed", StringComparison.Ordinal));
    }

    [Fact]
    public void A_window_title_with_a_quote_is_refused_before_taskkill_runs()
    {
        AssertRefusedAndNothingRan(
            ErrorsAfter(new CloseWindow("Editor\" & calc.exe")), "Refused window title");
    }

    [Fact]
    public void A_process_name_with_a_quote_is_refused_before_taskkill_runs()
    {
        AssertRefusedAndNothingRan(
            ErrorsAfter(new KillProcess("Code\" & calc.exe")), "Refused process name");
    }

    [Fact]
    public void A_percent_sign_in_a_process_name_is_refused_before_taskkill_runs()
    {
        AssertRefusedAndNothingRan(
            ErrorsAfter(new KillProcess("Code%TEMP%")), "Refused process name");
    }

    [Fact]
    public void A_wsl_pkill_pattern_with_a_quote_is_refused_before_wsl_runs()
    {
        AssertRefusedAndNothingRan(
            ErrorsAfter(new WslPkill("ollama\" ; rm -rf /")), "Refused wsl pkill pattern");
    }

    [Fact]
    public void A_quoted_window_title_is_refused_before_cmd_runs()
    {
        var errors = ErrorsAfter(new StartWslService("Editor\" & calc.exe", "cd ~ && ollama serve"));

        AssertRefusedAndNothingRan(errors, "Refused window title");
        Assert.Single(errors);
    }

    [Fact]
    public void A_quote_in_the_wsl_command_is_refused_before_cmd_runs()
    {
        var errors = ErrorsAfter(new StartWslService("Ollama", "echo \"hi\""));

        AssertRefusedAndNothingRan(errors, "Refused WSL command");
        Assert.Single(errors);
    }

    [Fact]
    public void A_percent_sign_in_the_wsl_command_is_not_refused()
    {
        // Bash uses % freely, so the guard must not fire. Nothing is executed
        // here because a refused title stops the step first.
        var errors = ErrorsAfter(new StartWslService("Ollama\"", "date +%s"));

        Assert.Single(errors);
        Assert.StartsWith("Refused window title", errors[0], StringComparison.Ordinal);
    }
}
