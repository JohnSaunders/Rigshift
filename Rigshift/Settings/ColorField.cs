using System.ComponentModel;
using System.Text.RegularExpressions;

namespace Rigshift.Settings;

/// <summary>
/// Hex colour field: the hex code is a themed mono field drawn in <see cref="Theme.Text"/>,
/// with the swatch as the focusable picker (accessible name and tooltip "Pick…"), replacing
/// the visible text button. Holds no draft of its own; the caller wires it into the draft.
/// </summary>
public partial class ColorField : UserControl
{
    private readonly ThemedTextBox _field = new() { Mono = true };
    private readonly Swatch _swatch = new();

    public ColorField()
    {
        SuspendLayout();

        _field.Width = 90;
        _field.Margin = Padding.Empty;

        // The swatch is centred on the hex field, a small gap to its right.
        _swatch.Size = new Size(23, 23);
        var swatchTop = Math.Max(0, (_field.Height - _swatch.Height) / 2);
        _swatch.Margin = new Padding(ThemePainter.Px(6, DeviceDpi / 96f), swatchTop, 0, 0);

        var row = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = Padding.Empty,
            WrapContents = false
        };
        row.Controls.Add(_field);
        row.Controls.Add(_swatch);

        Controls.Add(row);

        _field.TextChanged += (_, _) =>
        {
            ApplyColors();
            HexChanged?.Invoke(this, EventArgs.Empty);
        };
        _swatch.Click += (_, _) => Pick();

        // The control fits its one row: the panel sizes to the two children and the
        // control grows to the panel, so no caller sizing is needed.
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        ApplyColors();
        ResumeLayout(false);
    }

    /// <summary>
    /// Parses a trimmed hex string of the form <c>#RRGGBB</c> (the
    /// <c>TintSelector</c> pattern). On success <paramref name="color"/> is an
    /// opaque colour with those red, green, and blue bytes; on failure it is
    /// <see cref="Color.Empty"/>.
    /// </summary>
    public static bool TryParseHex(string? text, out Color color)
    {
        color = Color.Empty;
        if (text is null)
            return false;

        string candidate = text.Trim();
        if (!HexPattern().IsMatch(candidate))
            return false;

        color = Color.FromArgb(
            Convert.ToInt32(candidate.Substring(1, 2), 16),
            Convert.ToInt32(candidate.Substring(3, 2), 16),
            Convert.ToInt32(candidate.Substring(5, 2), 16));
        return true;
    }

    /// <summary>Formats the red, green, and blue bytes as <c>#RRGGBB</c> with uppercase hex digits.</summary>
    public static string FormatHex(Color color) =>
        $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    /// <summary>The hex text in the field. A null write is stored as <c>""</c>.</summary>
    [DefaultValue("")]
    public string HexText
    {
        get => _field.Text;
        set => _field.Text = value ?? string.Empty;
    }

    /// <summary>Raised after the text changes, whether typed, set through <see cref="HexText"/>, or picked.</summary>
    public event EventHandler? HexChanged;

    /// <summary>The hex field's text colour: always <see cref="Theme.Text"/>. A test seam.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color TextColor => Theme.Text;

    /// <summary>The swatch's current <see cref="Control.BackColor"/>: the parsed colour, else <see cref="Theme.Surface"/>.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color SwatchColor => _swatch.BackColor;

    /// <summary>
    /// The colour dialog seam, given the colour to start from and returning the
    /// picked colour or null when the dialog was cancelled. Tests replace this.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<Color, Color?> PickColor { get; set; } = (Color current) =>
    {
        using var dialog = new ColorDialog { Color = current, FullOpen = true };
        return dialog.ShowDialog() == DialogResult.OK ? dialog.Color : null;
    };

    /// <summary>
    /// Opens the colour dialog through <see cref="PickColor"/> with the parsed
    /// current colour, or <see cref="Color.Black"/> when the text is not hex.
    /// A non-null result is written back as uppercase <c>#RRGGBB</c>; a null
    /// result changes nothing.
    /// </summary>
    public void Pick()
    {
        Color initial = TryParseHex(_field.Text, out Color parsed) ? parsed : Color.Black;
        Color? picked = PickColor(initial);
        if (picked is { } value)
            HexText = FormatHex(value);
    }

    /// <summary>
    /// Sets the swatch to the parsed colour, or back to <see cref="Theme.Surface"/> when the
    /// text is not valid hex. The hex text itself stays <see cref="Theme.Text"/>.
    /// </summary>
    private void ApplyColors()
    {
        _swatch.BackColor = TryParseHex(_field.Text, out Color color) ? color : Theme.Surface;
    }

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex HexPattern();

    /// <summary>A small focusable colour square that acts as the picker button.</summary>
    public sealed class Swatch : Panel
    {
        public Swatch()
        {
            TabStop = true;
            AccessibleName = "Pick";
            AccessibleDescription = "Pick…";
            BackColor = Theme.Surface;
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.Selectable, true);
            SetStyle(
                ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
                true);
        }

        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e);
            Invalidate();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }

        // The colour square with a one-pixel frame so a dark colour still reads against
        // the panel; the frame turns accent while the swatch has focus.
        protected override void OnPaint(PaintEventArgs e)
        {
            var scale = DeviceDpi / 96f;
            var border = Focused ? Theme.Accent : Enabled ? Theme.InputBorder : Theme.Border;
            ThemePainter.FillFrame(e.Graphics, ClientRectangle, BackColor, border, scale);
        }

        /// <summary>Activates the picker, for the button's Enter/Space and for tests.</summary>
        public void PerformClick() => OnClick(EventArgs.Empty);

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode is Keys.Space or Keys.Enter)
            {
                PerformClick();
                e.Handled = true;
                return;
            }
            base.OnKeyDown(e);
        }
    }
}
