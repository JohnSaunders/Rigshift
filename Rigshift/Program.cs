using Rigshift.Config;
using Rigshift.Execution;

namespace Rigshift;

internal static class Program
{
    public static ConfigLoadResult LoadConfig() =>
        ConfigLoader.LoadFromCandidates(ConfigLoader.CandidatePaths());

    /// <summary>The file currently in use, or null when the built-in default applies.</summary>
    public static string? ActiveConfigPath =>
        ConfigLoader.ResolveExisting(ConfigLoader.CandidatePaths());

    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        // Any arguments at all mean this is the short-lived elevated helper. It runs only
        // the closed --admin payload and never reads the config; anything else is rejected
        // with exit code 64 before a step runs. Falling through instead would leave a
        // second tray icon running as administrator.
        if (args.Length > 0)
        {
            var executor = new WindowsStepExecutor();
            return AdminHelper.Run(args, executor, () => executor.Errors, Report);
        }

        // Guards the tray path only; the elevated helper above is deliberately
        // short-lived and must always be allowed to run.
        using var single = new Mutex(initiallyOwned: true, "Rigshift.SingleInstance", out var isFirst);
        if (!isFirst) return 0;

        // The tray path only: the elevated helper above returns before this. Dark mode is
        // the base for the Settings window's scroll bars and drop-downs; the window paints
        // its own colours from Theme on top. It also turns the tray menu stock dark.
        Application.SetColorMode(SystemColorMode.Dark);

        Application.Run(new TrayApplication());
        return 0;
    }

    /// <summary>
    /// The elevated helper's only UI. DefaultDesktopOnly keeps the dialog on top: the
    /// helper has no owner window, so an ordinary MessageBox can hide behind others.
    /// </summary>
    private static void Report(string message) =>
        MessageBox.Show(message, "Rigshift — administrator steps", MessageBoxButtons.OK,
            MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
}
