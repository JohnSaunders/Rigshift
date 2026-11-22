using System.Diagnostics;
using System.Runtime.Versioning;
using Rigshift.Config;
using Rigshift.Execution;
using Rigshift.Settings;
using Rigshift.Ui;

namespace Rigshift;

[SupportedOSPlatform("windows")]
public sealed class TrayApplication : ApplicationContext
{
    private readonly NotifyIcon _tray;
    private readonly Icon _baseIcon;
    private readonly LoadoutIntent _intent = new();
    private readonly WarningNotifier _warningNotifier = new();
    private readonly ActionCoordinator _coordinator = new();
    private readonly Elevator _elevator = new();
    private readonly TimeSpan _adminTimeout = TimeSpan.FromSeconds(120);
    private Icon? _currentIcon;
    private AboutForm? _about;
    private string? _lastEnabled;
    private string? _busyLoadout;

    private bool IsBusy => _busyLoadout is not null;

    public TrayApplication()
    {
        _baseIcon = new Icon(Path.Combine(AppContext.BaseDirectory, "Assets", "rigshift.ico"), 16, 16);
        _tray = new NotifyIcon
        {
            Icon = _baseIcon,
            Visible = true,
            Text = "Rigshift",
            ContextMenuStrip = new ContextMenuStrip()
        };
        // Double-clicking the tray icon opens Settings, the same entry point and busy
        // guard as the Settings menu item, so the gesture never bypasses the busy state.
        _tray.DoubleClick += (_, _) =>
        {
            if (!IsBusy)
                OpenSettings();
        };
        // Rebuild on open so config edits land without a restart.
        _tray.ContextMenuStrip.Opening += (_, _) => BuildMenu();
        BuildMenu();
    }

    private void BuildMenu()
    {
        var (config, warnings) = Program.LoadConfig();
        var enabled = _intent.Enabled;

        var menu = _tray.ContextMenuStrip!;
        menu.Items.Clear();

        foreach (var loadout in config.Loadouts)
        {
            var name = loadout.Name;
            if (loadout.Toggle)
            {
                var isOn = enabled.Contains(name);
                var label = TrayMenuLabels.Toggle(name, isOn && HeldLoadoutAnalyzer.Analyze(config, name, enabled).Held);
                var item = new ToolStripMenuItem(label) { Checked = isOn, CheckOnClick = false, Enabled = !IsBusy };
                item.Click += (_, _) => Toggle(name, isOn);
                menu.Items.Add(item);
            }
            else
            {
                var item = new ToolStripMenuItem(TrayMenuLabels.OneShot(name)) { Enabled = !IsBusy };
                item.Click += (_, _) => OneShot(name);
                menu.Items.Add(item);
            }
        }

        menu.Items.Add(new ToolStripSeparator());
        foreach (var label in TrayMenuLabels.Fixed)
        {
            var item = new ToolStripMenuItem(label);
            switch (label)
            {
                case TrayMenuLabels.Settings:
                    item.Enabled = !IsBusy;
                    item.Click += (_, _) => OpenSettings();
                    break;
                case TrayMenuLabels.OpenConfig:
                    item.Click += (_, _) => OpenConfig();
                    break;
                case TrayMenuLabels.About:
                    item.Click += (_, _) => ShowAbout();
                    break;
                case TrayMenuLabels.Exit:
                    item.Click += (_, _) =>
                    {
                        if (MessageBox.Show(
                                TrayMenuLabels.ExitConfirmText,
                                TrayMenuLabels.ExitConfirmTitle,
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question,
                                MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                            ExitThread();
                    };
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected menu item '{label}'.");
            }
            menu.Items.Add(item);
        }

        ApplyTint(config, enabled);
        if (_warningNotifier.ShouldNotify(warnings)) Balloon("Rigshift config", string.Join(Environment.NewLine, warnings));
    }

    private void Toggle(string name, bool currentlyOn)
    {
        var config = Program.LoadConfig().Config;

        if (currentlyOn)
        {
            // Pure logic over the tray's own intent list — no admin rights needed — so
            // we can detect a no-op disable (another enabled loadout still needs the
            // same definition) before doing any work whose elevated child would then
            // do nothing.
            var held = HeldLoadoutAnalyzer.Analyze(config, name, _intent.Enabled);
            if (held.Held)
            {
                Balloon("Rigshift", HeldMessage(name, held.Holders));
                return;
            }
        }

        var plan = currentlyOn
            ? ActionRunner.PlanDisable(config, name, _intent.Enabled)
            : ActionRunner.PlanEnable(config, name);
        Execute(config, name, currentlyOn ? ActionKind.Disable : ActionKind.Enable, plan, Array.Empty<string>());
    }

    private static string HeldMessage(string name, IReadOnlyList<string> holders)
    {
        if (holders.Count == 0)
            return $"{name} stays on — still needed by another active loadout.";

        var who = string.Join(" + ", holders);
        return $"{name} stays on — still needed by {who}. Turn {who} off first.";
    }

    private void OneShot(string name)
    {
        var config = Program.LoadConfig().Config;
        var cleared = OneShotIntentAnalyzer.Cleared(config, name, _intent.Enabled);
        var plan = ActionRunner.PlanOneShot(config, name);
        Execute(config, name, ActionKind.OneShot, plan, cleared);
    }

    /// <summary>
    /// Runs one action as user steps then admin steps, off the UI thread, and applies
    /// the intent decision. Busy is set first and cleared in a finally, so the menu and
    /// Settings stay out of the way while the user slice sleeps or the admin prompt is up.
    /// </summary>
    private async void Execute(RigshiftConfig config, string name, ActionKind kind, ActionPlan plan, IReadOnlyList<string> oneShotCleared)
    {
        if (IsBusy)
            return;

        var part = PlanPartition.Split(plan.Steps);
        SetBusy(name);
        try
        {
            // async void invoked from a UI event handler resumes on the WinForms
            // SynchronizationContext, so intent changes and the balloon stay on the UI
            // thread while the steps themselves run on thread-pool threads.
            var userExec = new WindowsStepExecutor();
            var outcome = await Task.Run(() =>
                _coordinator.RunAsync(part, userExec, () => userExec.Errors, _elevator, _adminTimeout, CancellationToken.None));

            var decision = IntentDecision.Decide(kind, part.Admin.Count == 0, part.User.Count == 0, outcome.Elevation);

            ApplyIntent(kind, name, oneShotCleared, decision.Apply);

            string? message;
            if (outcome.UserStepErrors.Count > 0)
                message = string.Join(Environment.NewLine, outcome.UserStepErrors);
            else if (decision.Balloon is not null)
                message = decision.Balloon;
            else if (!decision.Apply && outcome.Elevation is ElevationResult.Declined)
                message = $"{name} unchanged — administrator approval was declined.";
            else
                message = null;

            if (message is not null)
                Balloon("Rigshift", message);

            ApplyTint(config, _intent.Enabled);
        }
        catch (Exception ex)
        {
            // Never let an async void leak: a failure here would otherwise crash the tray
            // on the WinForms SynchronizationContext. Busy still clears via finally.
            Balloon("Rigshift", $"Could not run {name}: {ex.Message}");
        }
        finally
        {
            SetBusy(null);
        }
    }

    private void ApplyIntent(ActionKind kind, string name, IReadOnlyList<string> oneShotCleared, bool apply)
    {
        if (!apply)
            return;

        switch (kind)
        {
            case ActionKind.Enable:
                _intent.Enable(name);
                _lastEnabled = name;
                break;
            case ActionKind.Disable:
                _intent.Disable(name);
                break;
            case ActionKind.OneShot:
                foreach (var hit in oneShotCleared)
                    _intent.Disable(hit);
                if (_lastEnabled is not null && oneShotCleared.Contains(_lastEnabled))
                    _lastEnabled = null;
                break;
        }
    }

    private void SetBusy(string? name)
    {
        _busyLoadout = name;
        _tray.Text = name is null
            ? (_intent.Enabled.Count == 0 ? "Rigshift" : $"Rigshift — {string.Join(" + ", _intent.Enabled)}")
            : $"Rigshift — working on {name}…";
    }

    private void ApplyTint(RigshiftConfig config, IReadOnlyList<string> enabled)
    {
        var hex = TintSelector.SelectHex(config, enabled, _lastEnabled);
        var colour = ColorTranslator.FromHtml(hex);

        var next = IconTinter.Tint(_baseIcon, colour);
        _tray.Icon = next;
        _currentIcon?.Dispose();
        _currentIcon = next;

        _tray.Text = enabled.Count == 0 ? "Rigshift" : $"Rigshift — {string.Join(" + ", enabled)}";
    }

    // One About window at a time: a second click brings the open one forward.
    private void ShowAbout()
    {
        if (_about is { IsDisposed: false })
        {
            _about.Activate();
            return;
        }

        try
        {
            using var about = new AboutForm();
            _about = about;
            about.ShowDialog();
        }
        catch (Exception ex)
        {
            Balloon("Rigshift", $"Could not open About: {ex.Message}");
        }
        finally
        {
            _about = null;
        }
    }

    private void OpenConfig()
    {
        try
        {
            var path = Program.ActiveConfigPath ?? CreateUserConfig();
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Balloon("Rigshift", $"Could not open the config: {ex.Message}");
        }
    }

    private void OpenSettings()
    {
        try
        {
            var loaded = Program.LoadConfig();
            var userPath = ConfigLoader.UserConfigPath();

            var notes = SettingsSource.Notes(Program.ActiveConfigPath, userPath, loaded.Warnings);
            var draft = SettingsMapper.FromConfig(loaded.Config);

            using var form = new SettingsForm(draft, notes, userPath);
            if (form.ShowDialog() != DialogResult.OK)
                return;

            var result = SettingsSave.TrySave(draft, userPath, _intent.Enabled, _lastEnabled);
            if (!result.Saved)
            {
                Balloon("Rigshift", result.Error ?? "Could not save the config.");
                return;
            }

            // Drop enabled names the saved config no longer has as toggles. Snapshot first
            // so the disables below are evaluated against the pre-save intent.
            foreach (var name in _intent.Enabled.ToList())
            {
                if (!result.Enabled.Contains(name, StringComparer.Ordinal))
                    _intent.Disable(name);
            }
            _lastEnabled = result.LastEnabled;
            ApplyTint(result.Config!, _intent.Enabled);
        }
        catch (Exception ex)
        {
            Balloon("Rigshift", $"Could not open settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Writes the example (or the built-in default) to the per-user path so
    /// Open Config has a file to edit when nothing was loaded from disk.
    /// </summary>
    private static string CreateUserConfig()
    {
        var path = ConfigLoader.UserConfigPath();
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        if (!File.Exists(path))
        {
            var example = Path.Combine(AppContext.BaseDirectory, ConfigLoader.ExampleFileName);
            if (File.Exists(example))
                File.Copy(example, path);
            else
                File.WriteAllText(path, ConfigLoader.ToJson(ConfigLoader.Default()));
        }

        return path;
    }

    private void Balloon(string title, string text)
    {
        _tray.BalloonTipTitle = title;
        _tray.BalloonTipText = text;
        _tray.ShowBalloonTip(5000);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tray.Visible = false;
            _tray.Dispose();
            _currentIcon?.Dispose();
            _baseIcon.Dispose();
        }
        base.Dispose(disposing);
    }
}

public static class TrayMenuLabels
{
    public const string Settings = "Settings…";
    public const string OpenConfig = "Open Config…";
    public const string About = "About Rigshift…";
    public const string Exit = "Exit";

    public const string ExitConfirmTitle = "Exit Rigshift";
    public const string ExitConfirmText =
        "Exit Rigshift? Programs it started keep running, and every switch shows off the next time Rigshift starts.";

    public static IReadOnlyList<string> Fixed { get; } =
        new[] { Settings, OpenConfig, About, Exit };

    public static string AboutText => $"Rigshift {SettingsForm.ProductVersion}";

    public static string Toggle(string name, bool held) => held ? $"{name} (in use)" : name;
    public static string OneShot(string name) => name;
}
