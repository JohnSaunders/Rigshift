namespace Rigshift.Settings;

/// <summary>
/// A field label in the site's eyebrow style: the text in small mono capitals, tracked
/// out, in <see cref="Theme.Muted"/>. <see cref="Control.Text"/> keeps its own casing for
/// accessibility and tests; only the painting is upper case.
/// </summary>
public class EyebrowLabel : Label
{
    private const TextFormatFlags Flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix;

    public EyebrowLabel()
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
            true);
        Font = Theme.EyebrowFont;
        ForeColor = Theme.Muted;
        TextAlign = ContentAlignment.MiddleLeft;
    }

    /// <summary>The text as painted: upper case.</summary>
    public string PaintedText => Text.ToUpperInvariant();

    private int Tracking => ThemePainter.Px(Theme.EyebrowTracking, DeviceDpi / 96f);

    public override Size GetPreferredSize(Size proposedSize)
    {
        var width = 0;
        var height = TextRenderer.MeasureText("X", Font, Size.Empty, Flags).Height;
        foreach (var c in PaintedText)
            width += TextRenderer.MeasureText(c.ToString(), Font, Size.Empty, Flags).Width + Tracking;
        return new Size(Math.Max(0, width - Tracking) + Padding.Horizontal, height + Padding.Vertical);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        using (var back = new SolidBrush(BackColor))
            e.Graphics.FillRectangle(back, ClientRectangle);

        var color = Enabled ? ForeColor : Theme.Border;
        var lineHeight = TextRenderer.MeasureText("X", Font, Size.Empty, Flags).Height;
        var y = Padding.Top + Math.Max(0, (ClientSize.Height - Padding.Vertical - lineHeight) / 2);
        var x = Padding.Left;
        foreach (var c in PaintedText)
        {
            var glyph = c.ToString();
            TextRenderer.DrawText(e.Graphics, glyph, Font, new Point(x, y), color, Flags);
            x += TextRenderer.MeasureText(glyph, Font, Size.Empty, Flags).Width + Tracking;
        }
    }
}
