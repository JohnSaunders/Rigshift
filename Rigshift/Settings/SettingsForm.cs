using System.Reflection;
using System.Text.Json;
using Rigshift.Ui;

namespace Rigshift.Settings;

/// <summary>
/// The settings window shell. Holds the <see cref="SettingsDraft"/> the window edits
/// plus the source-note lines and the save path, and hosts the idle color, the Loadouts
/// and Definitions tabs, the error list, and Save/Cancel. It does no file I/O: Save stays
/// open with the validation messages when the draft is invalid, and closes with
/// <see cref="DialogResult.OK"/> only when it is valid.
/// </summary>
public class SettingsForm : Form
{
    /// <summary>
    /// The accent, <c>#35C9E8</c>, the window icon is drawn in. Fixed theme color,
    /// not read from the config.
    /// </summary>
    public static readonly Color IconColor = Theme.Accent;

    /// <summary>The warning box fill: 7% caution over the background, as the site's callouts.</summary>
    public static readonly Color WarningFill = Theme.Blend(Theme.Caution, Theme.Bg, 0.07);

    /// <summary>The warning box frame: 60% caution over the background.</summary>
    public static readonly Color WarningBorder = Theme.Blend(Theme.Caution, Theme.Bg, 0.6);

    private readonly SettingsDraft _draft;
    private readonly Icon _icon;
    private readonly string _baseline;
    private bool _warningsVisible;
    private bool _errorsVisible;
    private bool _discardConfirmed;
    private readonly Label _savePathPrefix = new EyebrowLabel { Text = "Saves to" };
    private readonly Label _savePathValue = new() { ForeColor = Theme.Text, Font = Theme.MonoFont };
    private readonly ThemedTextView _warningBox = new();
    private readonly ColorField _idleColorField = new();
    private readonly TabStrip _tabStrip = new("Loadouts", "Definitions");
    private readonly Panel _host = new() { BackColor = Theme.Surface };
    private readonly LoadoutEditor _loadoutEditor;
    private readonly DefinitionEditor _definitionEditor;
    private readonly ThemedListBox _errorBox = new();
    private readonly ThemedButton _saveButton = new() { Text = "Save", Primary = true, AutoSize = true };
    private readonly ThemedButton _cancelButton = new() { Text = "Cancel", AutoSize = true };

    public SettingsForm(SettingsDraft draft, IReadOnlyList<string> notes, string savePath)
    {
        if (draft is null)
            throw new ArgumentNullException(nameof(draft));
        if (notes is null)
            throw new ArgumentNullException(nameof(notes));
        if (savePath is null)
            throw new ArgumentNullException(nameof(savePath));
        _draft = draft;

        SuspendLayout();

        Text = WindowTitle;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(780, 680);
        MinimumSize = new Size(780, 680);
        MaximumSize = new Size(780, 680);

        // The native caption and border are recoloured to the theme (design §4). On
        // Windows 10, where the caption-colour attributes are not honoured, the caption
        // falls back to plain dark; .NET 10 sets the DWM attributes on Windows 11.
        FormCaptionBackColor = Theme.Bg;
        FormCaptionTextColor = Theme.Text;
        FormBorderColor = Theme.Border;

        // The title bar and taskbar show the same Rigshift glyph the tray uses, loaded
        // from the asset next to the executable and drawn in the accent — the shipped
        // white silhouette is hard to see on a light title bar or taskbar. No size is
        // passed so Windows picks the one it needs. The form does not dispose an icon
        // assigned to its Icon property, so this form owns _icon and releases it in
        // Dispose.
        using var source = new Icon(Path.Combine(AppContext.BaseDirectory, "Assets", "rigshift.ico"));
        _icon = IconTinter.Tint(source, IconColor);
        Icon = _icon;

        // The top band is one docked table: the save path on the left and the idle colour
        // on the right of one row, every cell centred on that row's height, and the
        // source-note box spanning the row below when there are notes. The middle column
        // takes the spare width, which pushes the idle colour to the right edge.
        var scale = DeviceDpi / 96f;
        var topBand = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Theme.Bg,
            ColumnCount = 5,
            RowCount = 2,
            Padding = new Padding(ThemePainter.Px(16, scale), ThemePainter.Px(10, scale), ThemePainter.Px(16, scale), ThemePainter.Px(8, scale))
        };
        topBand.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        topBand.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        topBand.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        topBand.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        topBand.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        topBand.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        topBand.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _savePathPrefix.AutoSize = true;
        _savePathPrefix.Anchor = AnchorStyles.Left;
        _savePathPrefix.Margin = new Padding(0, 0, ThemePainter.Px(10, scale), 0);
        _savePathValue.AutoSize = true;
        _savePathValue.Anchor = AnchorStyles.Left;
        _savePathValue.Margin = Padding.Empty;
        _savePathValue.Text = savePath;

        var idleLabel = new EyebrowLabel
        {
            Text = "Idle color",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(ThemePainter.Px(16, scale), 0, ThemePainter.Px(8, scale), 0)
        };
        _idleColorField.HexText = draft.IdleColor ?? "";
        _idleColorField.HexChanged += (_, _) => _draft.IdleColor = _idleColorField.HexText;
        _idleColorField.Anchor = AnchorStyles.Left;
        _idleColorField.Margin = Padding.Empty;

        _warningBox.Dock = DockStyle.Fill;
        _warningBox.BackColor = WarningFill;
        _warningBox.ForeColor = Theme.Caution;
        _warningBox.Font = Theme.UiFont;
        _warningsVisible = notes.Count > 0;
        _warningBox.Text = string.Join(Environment.NewLine, notes);

        // The site's caution callout: an amber frame around an amber-tinted box.
        var warningInset = ThemePainter.Px(8, scale);
        var warningFrame = new Panel
        {
            Dock = DockStyle.Fill,
            Height = ThemePainter.Px(80, scale),
            BackColor = WarningFill,
            Padding = new Padding(warningInset, ThemePainter.Px(6, scale), ThemePainter.Px(4, scale), ThemePainter.Px(6, scale)),
            Margin = new Padding(0, ThemePainter.Px(8, scale), 0, 0),
            Visible = _warningsVisible
        };
        warningFrame.Paint += (_, e) =>
            ThemePainter.FillFrame(e.Graphics, warningFrame.ClientRectangle, WarningFill, WarningBorder, scale);
        warningFrame.Controls.Add(_warningBox);

        topBand.Controls.Add(_savePathPrefix, 0, 0);
        topBand.Controls.Add(_savePathValue, 1, 0);
        topBand.Controls.Add(idleLabel, 3, 0);
        topBand.Controls.Add(_idleColorField, 4, 0);
        topBand.Controls.Add(warningFrame, 0, 1);
        topBand.SetColumnSpan(warningFrame, 5);

        _loadoutEditor = new LoadoutEditor(draft) { Dock = DockStyle.Fill };
        _definitionEditor = new DefinitionEditor(draft) { Dock = DockStyle.Fill };
        _definitionEditor.DefinitionsChanged += (_, _) => _loadoutEditor.RefreshReferenceKeys();

        _host.Dock = DockStyle.Fill;
        _tabStrip.Dock = DockStyle.Top;
        _tabStrip.SelectedIndexChanged += (_, _) => ShowSelectedTab();

        // The bottom band is one docked table: the error list spans the first row when
        // a Save finds problems, and Cancel and Save sit at the right of the second row,
        // Save last, the way Windows dialogs order them.
        _errorBox.Height = 80;
        _errorBox.Dock = DockStyle.Fill;
        _errorBox.HorizontalScrollbar = true;
        _errorBox.Margin = new Padding(0, 0, 0, ThemePainter.Px(8, scale));
        _errorsVisible = false;
        _errorBox.Visible = false;

        _cancelButton.Margin = new Padding(0, 0, ThemePainter.Px(8, scale), 0);
        _saveButton.Margin = Padding.Empty;

        var bottomBand = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Theme.Bg,
            ColumnCount = 3,
            RowCount = 2,
            Padding = new Padding(ThemePainter.Px(16, scale), ThemePainter.Px(10, scale), ThemePainter.Px(16, scale), ThemePainter.Px(10, scale))
        };
        bottomBand.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        bottomBand.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bottomBand.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bottomBand.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        bottomBand.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        bottomBand.Controls.Add(_errorBox, 0, 0);
        bottomBand.SetColumnSpan(_errorBox, 3);
        bottomBand.Controls.Add(_cancelButton, 1, 1);
        bottomBand.Controls.Add(_saveButton, 2, 1);

        // A one-pixel rule above the bottom band and below the tab strip frames the tab body.
        var bottomRule = new Panel { Dock = DockStyle.Bottom, Height = ThemePainter.Px(1, scale), BackColor = Theme.Border };

        // Dock order matters: last added claims its edge first, so topBand (a Top dock)
        // must be added after tabStrip to sit above it. Fill (the host) is added first.
        Controls.Add(_host);
        Controls.Add(bottomRule);
        Controls.Add(bottomBand);
        Controls.Add(_tabStrip);
        Controls.Add(topBand);

        _saveButton.Click += (_, _) => ClickSave();
        _cancelButton.Click += (_, _) => ClickCancel();
        AcceptButton = _saveButton;
        CancelButton = _cancelButton;

        ResumeLayout(false);

        ShowSelectedTab();

        // The mockup opens with the first loadout already chosen and its fields filled.
        if (_draft.Loadouts.Count > 0)
            _loadoutEditor.Select(0);
        if (_draft.Definitions.Count > 0)
            _definitionEditor.Select(0);

        // The draft is fully shown at this point, so it is a clean baseline to compare
        // against. ConfirmDiscard is assigned here because its default uses `this`.
        _baseline = JsonSerializer.Serialize(_draft);
        ConfirmDiscard = () => MessageBox.Show(
            this,
            "Discard changes?",
            "Rigshift Settings",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2) == DialogResult.Yes;
    }

    /// <summary>
    /// The Rigshift build version, read once from the assembly's informational
    /// version. The SDK may append build metadata after a <c>+</c>; only the part
    /// before it is kept. <c>0.0.0</c> when the attribute is missing.
    /// </summary>
    public new static string ProductVersion { get; } = TrimBuildMetadata(
        typeof(SettingsForm).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

    private static string TrimBuildMetadata(string? informationalVersion)
    {
        if (string.IsNullOrEmpty(informationalVersion))
            return "0.0.0";
        var plus = informationalVersion.IndexOf('+');
        return plus < 0 ? informationalVersion : informationalVersion[..plus];
    }

    /// <summary>The settings window title: <c>Rigshift {version} Settings</c>.</summary>
    public static string WindowTitle => $"Rigshift {ProductVersion} Settings";

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _icon.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>The save-path label text: <c>Saves to {savePath}</c>.</summary>
    public string SavePathText => $"{_savePathPrefix.Text} {_savePathValue.Text}";

    /// <summary>
    /// Whether the source-note warning box is visible: true when notes are present.
    /// Reported from the constructor's decision, not the box's live <c>Visible</c>
    /// property, which reports parent-relative state and reads false while the
    /// window is not shown.
    /// </summary>
    public bool WarningsVisible => _warningsVisible;

    /// <summary>
    /// Whether the error list is visible: true after a Save that found problems,
    /// false from the constructor and again after a valid Save. Reported from the
    /// <c>ClickSave</c> decision, not the box's live <c>Visible</c> property, which
    /// reports parent-relative state and reads false while the window is not shown.
    /// </summary>
    public bool ErrorsVisible => _errorsVisible;
    /// <summary>The text colour of the warning box.</summary>
    public Color WarningColor => _warningBox.ForeColor;

    /// <summary>The warning box: wrapped text with the themed scroll thumb.</summary>
    public ThemedTextView WarningBox => _warningBox;

    /// <summary>The joined source-note lines shown in the warning box.</summary>
    public string WarningText => _warningBox.Text;

    /// <summary>
    /// The idle color hex text and the draft's idle color. Setting it writes both.
    /// </summary>
    // A settable property on a Form does not participate in designer code
    // serialization, so WFO1000 is not applicable here.
    #pragma warning disable WFO1000
    public string IdleColorText
    {
        get => _idleColorField.HexText;
        set
        {
            _idleColorField.HexText = value ?? "";
            _draft.IdleColor = value ?? "";
        }
    }
    #pragma warning restore WFO1000

    /// <summary>The idle color <see cref="ColorField"/>.</summary>
    public ColorField IdleColorField => _idleColorField;

    /// <summary>The loadouts tab body.</summary>
    public LoadoutEditor LoadoutEditor => _loadoutEditor;

    /// <summary>The definitions tab body.</summary>
    public DefinitionEditor DefinitionEditor => _definitionEditor;

    /// <summary>
    /// Parents the editor of the selected tab into the host panel. The inactive editor
    /// keeps its own draft-backed state but is not parented, so only the active one paints.
    /// </summary>
    private void ShowSelectedTab()
    {
        _host.SuspendLayout();
        _host.Controls.Clear();
        _host.Controls.Add(_tabStrip.SelectedIndex == 0 ? _loadoutEditor : _definitionEditor);
        _host.ResumeLayout(true);
    }

    /// <summary>The message in the error list box, in order. Empty until Save is pressed.</summary>
    public IReadOnlyList<string> ErrorItems => _errorBox.Items.Cast<string>().ToList();

    /// <summary>
    /// Commits a pending definition rename, then runs validation, inserting a rename
    /// error at the front. Fills the error list when there are problems and leaves the
    /// window open with <see cref="DialogResult"/> as <see cref="DialogResult.None"/>;
    /// sets <see cref="DialogResult"/> to <see cref="DialogResult.OK"/> and closes only
    /// when the draft is valid.
    /// </summary>
    public void ClickSave()
    {
        var pending = _definitionEditor.CommitPending();
        var errors = new List<string>(SettingsValidator.Validate(_draft));
        if (pending is not null)
            errors.Insert(0, pending);

        _errorBox.BeginUpdate();
        _errorBox.Items.Clear();
        foreach (var message in errors)
            _errorBox.Items.Add(message);
        _errorBox.EndUpdate();
        _errorsVisible = errors.Count > 0;
        _errorBox.Visible = _errorsVisible;

        if (errors.Count > 0)
        {
            DialogResult = DialogResult.None;
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    /// <summary>
    /// Whether the draft differs from the state it was shown in. Selecting a row does
    /// not count; any write to a field does.
    /// </summary>
    public bool IsDirty => JsonSerializer.Serialize(_draft) != _baseline;

    /// <summary>
    /// Asks whether to discard uncommitted edits before the window closes. Replaceable
    /// so a test never opens the real dialog; returns true to discard, false to keep
    /// editing. Defaults to a Yes/No warning dialog with No as the default button.
    /// </summary>
    // A settable property on a Form does not participate in designer code
    // serialization, so WFO1000 is not applicable here.
    #pragma warning disable WFO1000
    public Func<bool> ConfirmDiscard { get; set; }
    #pragma warning restore WFO1000

    /// <summary>
    /// Decides whether the window may close. Returns true without asking when the result
    /// is already <see cref="DialogResult.OK"/>, when a discard was already accepted, or
    /// when the draft is not dirty. Otherwise asks via <see cref="ConfirmDiscard"/>
    /// exactly once; on yes it records the acceptance and returns true, on no it returns
    /// false.
    /// </summary>
    public bool AllowClose()
    {
        if (DialogResult == DialogResult.OK)
            return true;
        if (_discardConfirmed)
            return true;
        if (!IsDirty)
            return true;

        var yes = ConfirmDiscard();
        if (yes)
        {
            _discardConfirmed = true;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Closes with <see cref="DialogResult.Cancel"/> after asking when the draft changed.
    /// Stays open with <see cref="DialogResult.None"/> when the discard is declined.
    /// Does not validate and does not write.
    /// </summary>
    public void ClickCancel()
    {
        if (!AllowClose())
        {
            DialogResult = DialogResult.None;
            return;
        }

        DialogResult = DialogResult.Cancel;
        Close();
    }

    /// <summary>
    /// Catches the close button and Esc. When a discard is declined it cancels the
    /// close and leaves <see cref="DialogResult"/> as <see cref="DialogResult.None"/>
    /// so the window stays open for further edits.
    /// </summary>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!AllowClose())
        {
            e.Cancel = true;
            DialogResult = DialogResult.None;
        }

        base.OnFormClosing(e);
    }
}
