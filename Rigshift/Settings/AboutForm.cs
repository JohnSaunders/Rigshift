using Rigshift.Ui;

namespace Rigshift.Settings;

/// <summary>
/// The About window, painted from <see cref="Theme"/> like the Settings window: the
/// dark caption, the Rigshift glyph in the accent, the name and version, the tagline,
/// and an accent OK button in a footer band.
/// </summary>
public class AboutForm : Form
{
    /// <summary>The tagline under the version, as the README opens.</summary>
    public const string Tagline = "Loadouts for your rig.";

    private readonly Icon _icon;
    private readonly Bitmap _glyph;
    private readonly ThemedButton _okButton = new() { Text = "OK", Primary = true, AutoSize = true, DialogResult = DialogResult.OK };

    public AboutForm()
    {
        SuspendLayout();

        var scale = DeviceDpi / 96f;
        Text = "About Rigshift";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(ThemePainter.Px(380, scale), ThemePainter.Px(176, scale));
        BackColor = Theme.Surface;
        ForeColor = Theme.Text;
        Font = Theme.UiFont;

        FormCaptionBackColor = Theme.Bg;
        FormCaptionTextColor = Theme.Text;
        FormBorderColor = Theme.Border;

        // The same accent glyph the Settings window shows, for the caption and taskbar,
        // and a larger copy beside the name.
        var assetPath = Path.Combine(AppContext.BaseDirectory, "Assets", "rigshift.ico");
        using (var source = new Icon(assetPath))
            _icon = IconTinter.Tint(source, SettingsForm.IconColor);
        Icon = _icon;
        var glyphSize = ThemePainter.Px(48, scale);
        using (var large = new Icon(assetPath, glyphSize, glyphSize))
        using (var tinted = IconTinter.Tint(large, SettingsForm.IconColor))
            _glyph = tinted.ToBitmap();

        var picture = new PictureBox
        {
            Image = _glyph,
            SizeMode = PictureBoxSizeMode.Zoom,
            Size = new Size(glyphSize, glyphSize),
            BackColor = Theme.Surface,
            Margin = new Padding(0, ThemePainter.Px(2, scale), ThemePainter.Px(16, scale), 0)
        };

        var name = new Label
        {
            Text = "Rigshift",
            AutoSize = true,
            Font = Theme.HeadingFont,
            ForeColor = Theme.Text,
            BackColor = Theme.Surface,
            Margin = Padding.Empty
        };
        var version = new Label
        {
            Text = VersionText,
            AutoSize = true,
            Font = Theme.MonoFont,
            ForeColor = Theme.Accent,
            BackColor = Theme.Surface,
            Margin = new Padding(0, ThemePainter.Px(2, scale), 0, 0)
        };
        var tagline = new Label
        {
            Text = Tagline,
            AutoSize = true,
            ForeColor = Theme.Muted,
            BackColor = Theme.Surface,
            Margin = new Padding(0, ThemePainter.Px(10, scale), 0, 0)
        };

        var text = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            BackColor = Theme.Surface,
            Margin = Padding.Empty
        };
        text.Controls.Add(name);
        text.Controls.Add(version);
        text.Controls.Add(tagline);

        var body = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Theme.Surface,
            Padding = new Padding(ThemePainter.Px(24, scale), ThemePainter.Px(22, scale), ThemePainter.Px(24, scale), 0)
        };
        body.Controls.Add(picture);
        body.Controls.Add(text);

        _okButton.Margin = Padding.Empty;
        _okButton.MinimumSize = new Size(ThemePainter.Px(80, scale), 0);
        // RightToLeft flow places the button inside the left padding, then mirrors
        // that position, which counts the left padding a second time on the right.
        // This band is empty on the left, so that padding stays 0 and the 16px right
        // padding is the gap beside OK.
        // The footer pins OK to its right edge by hand. Layout panels here have dropped
        // the right padding, leaving the button flush against the window edge.
        var footerInset = ThemePainter.Px(16, scale);
        var footerPadY = ThemePainter.Px(10, scale);
        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            BackColor = Theme.Bg,
            Height = _okButton.GetPreferredSize(Size.Empty).Height + footerPadY * 2
        };
        footer.Controls.Add(_okButton);
        footer.Layout += (_, _) =>
        {
            var size = _okButton.GetPreferredSize(Size.Empty);
            _okButton.Size = new Size(Math.Max(size.Width, _okButton.MinimumSize.Width), size.Height);
            _okButton.Location = new Point(footer.ClientSize.Width - footerInset - _okButton.Width, footerPadY);
        };

        var rule = new Panel { Dock = DockStyle.Bottom, Height = ThemePainter.Px(1, scale), BackColor = Theme.Border };

        // Docking resolves from the end of the collection: footer, then the rule above
        // it, then the body fills the rest.
        Controls.Add(body);
        Controls.Add(rule);
        Controls.Add(footer);

        AcceptButton = _okButton;
        CancelButton = _okButton;

        ResumeLayout(false);
    }

    /// <summary>The version line under the name, e.g. <c>Version 0.1.0</c>.</summary>
    public static string VersionText => $"Version {SettingsForm.ProductVersion}";

    /// <summary>The OK button that closes the window.</summary>
    public Button OkButton => _okButton;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _icon.Dispose();
            _glyph.Dispose();
        }

        base.Dispose(disposing);
    }
}
