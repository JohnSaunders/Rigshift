namespace Rigshift.Execution;

/// <summary>
/// Guards the configuration values that are interpolated into an assembled
/// command line, which is the one way a config file could change the meaning of
/// what runs.
///
/// Two characters matter, and neither can be represented safely. A double quote
/// ends the quoted region that cmd.exe is parsing, so whatever follows is re-read
/// as syntax. A percent sign starts a variable expansion that cmd.exe performs
/// even inside quotes. Values carrying them are refused rather than rewritten, so
/// the reason surfaces instead of the behaviour silently changing.
/// </summary>
public static class ShellText
{
    /// <summary>For titles, process names and patterns: quotes and percent signs are both refused.</summary>
    public static bool IsSafe(string? value) =>
        value is null || (!value.Contains('"') && !value.Contains('%'));

    /// <summary>
    /// For a WSL command, which is bash text. Bash uses <c>%</c> freely
    /// (<c>date +%s</c>), so only the quote is refused — and it is refused because
    /// the quote cannot be passed through cmd.exe by escaping it.
    /// </summary>
    public static bool IsSafeCommand(string? value) =>
        value is null || !value.Contains('"');
}
