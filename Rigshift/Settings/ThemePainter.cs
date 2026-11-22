using System.Drawing.Drawing2D;

namespace Rigshift.Settings;

/// <summary>
/// Paint primitives for the themed Settings controls. Every member draws from
/// <see cref="Theme"/> into the caller's <see cref="Graphics"/>, takes <c>scale</c>
/// (device DPI ÷ 96), and restores any <see cref="Graphics"/> state it changes.
/// Rectangles and lines are pixel-exact; the pip and the check mark are anti-aliased.
/// </summary>
public static class ThemePainter
{
    /// <summary>
    /// <paramref name="logical"/> × <paramref name="scale"/>, rounded half away from
    /// zero, never below 1.
    /// </summary>
    public static int Px(int logical, float scale)
    {
        var device = (int)Math.Round(logical * (double)scale, MidpointRounding.AwayFromZero);
        return Math.Max(1, device);
    }

    /// <summary>Fills <paramref name="bounds"/> and draws a one-logical-pixel frame inside its edges.</summary>
    public static void FillFrame(Graphics g, Rectangle bounds, Color fill, Color border, float scale = 1f)
    {
        var state = g.Save();
        try
        {
            g.SmoothingMode = SmoothingMode.None;
            Fill(g, bounds, fill);
            Frame(g, bounds, border, Px(1, scale));
        }
        finally
        {
            g.Restore(state);
        }
    }

    /// <summary>
    /// A text field frame: surface fill, border frame, and a bottom edge that is two
    /// accent rows when focused and enabled, otherwise one row of input border (enabled)
    /// or border (disabled).
    /// </summary>
    public static void DrawField(Graphics g, Rectangle bounds, bool focused, bool enabled, float scale = 1f)
    {
        var state = g.Save();
        try
        {
            g.SmoothingMode = SmoothingMode.None;
            Fill(g, bounds, Theme.Surface);
            Frame(g, bounds, Theme.Border, Px(1, scale));

            var accent = focused && enabled;
            var rows = accent ? Px(2, scale) : Px(1, scale);
            var color = accent ? Theme.Accent : enabled ? Theme.InputBorder : Theme.Border;
            Fill(g, new Rectangle(bounds.Left, bounds.Bottom - rows, bounds.Width, rows), color);
        }
        finally
        {
            g.Restore(state);
        }
    }

    /// <summary>
    /// An outlined or primary button with its text centred on one line. Empty text draws
    /// no text.
    /// </summary>
    public static void DrawButton(
        Graphics g, Rectangle bounds, string? text, Font font,
        bool primary, bool hot, bool pressed, bool enabled, float scale = 1f)
    {
        Color fill, outline, textColor;
        if (!enabled)
        {
            fill = primary ? Theme.Border : Theme.Surface;
            outline = Theme.Border;
            textColor = Theme.Muted;
        }
        else if (primary)
        {
            fill = pressed ? Theme.Blend(Theme.AccentContrast, Theme.Accent, 0.15)
                : hot ? Theme.Blend(Theme.Text, Theme.Accent, 0.15)
                : Theme.Accent;
            outline = fill;
            textColor = Theme.AccentContrast;
        }
        else
        {
            fill = pressed ? Theme.SelectedFill : Theme.Surface;
            outline = hot || pressed ? Theme.InputBorder : Theme.Border;
            textColor = Theme.Text;
        }

        FillFrame(g, bounds, fill, outline, scale);

        if (string.IsNullOrEmpty(text))
            return;
        TextRenderer.DrawText(
            g, text, font, bounds, textColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
    }

    /// <summary>
    /// A checkbox square: an outlined surface box when unchecked, an accent box with a
    /// dark tick when checked. Disabled boxes use the border and muted tokens.
    /// </summary>
    public static void DrawCheck(Graphics g, Rectangle box, bool isChecked, bool enabled, float scale = 1f)
    {
        if (!isChecked)
        {
            FillFrame(g, box, Theme.Surface, enabled ? Theme.InputBorder : Theme.Border, scale);
            return;
        }

        var state = g.Save();
        try
        {
            g.SmoothingMode = SmoothingMode.None;
            Fill(g, box, enabled ? Theme.Accent : Theme.Muted);

            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(enabled ? Theme.AccentContrast : Theme.Surface, 2f * scale)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            g.DrawLines(pen, new[]
            {
                new PointF(box.Left + box.Width * 0.22f, box.Top + box.Height * 0.52f),
                new PointF(box.Left + box.Width * 0.42f, box.Top + box.Height * 0.70f),
                new PointF(box.Left + box.Width * 0.78f, box.Top + box.Height * 0.32f)
            });
        }
        finally
        {
            g.Restore(state);
        }
    }

    /// <summary>
    /// An 8-logical-pixel colour dot centred on <paramref name="center"/>, or an empty
    /// input-border ring when <paramref name="color"/> is null.
    /// </summary>
    public static void DrawPip(Graphics g, Point center, Color? color, float scale = 1f)
    {
        var diameter = 8f * scale;
        var circle = new RectangleF(center.X - diameter / 2f, center.Y - diameter / 2f, diameter, diameter);

        var state = g.Save();
        try
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (color is { } fill)
            {
                using var brush = new SolidBrush(fill);
                g.FillEllipse(brush, circle);
                return;
            }

            float width = Px(1, scale);
            using var pen = new Pen(Theme.InputBorder, width);
            circle.Inflate(-width / 2f, -width / 2f);
            g.DrawEllipse(pen, circle);
        }
        finally
        {
            g.Restore(state);
        }
    }

    /// <summary>
    /// A selected list row: the row filled with the selected fill, plus a two-logical-pixel
    /// accent bar at its left edge, half the row tall and vertically centred.
    /// </summary>
    public static void DrawSelectedRow(Graphics g, Rectangle row, float scale = 1f)
    {
        var state = g.Save();
        try
        {
            g.SmoothingMode = SmoothingMode.None;
            Fill(g, row, Theme.SelectedFill);
            var barHeight = row.Height / 2;
            var top = row.Top + (row.Height - barHeight) / 2;
            Fill(g, new Rectangle(row.Left, top, Px(2, scale), barHeight), Theme.Accent);
        }
        finally
        {
            g.Restore(state);
        }
    }

    /// <summary>Two logical rows of accent from <paramref name="left"/>, ending on the row above <paramref name="bottom"/>.</summary>
    public static void DrawUnderline(Graphics g, int left, int width, int bottom, float scale = 1f)
    {
        var rows = Px(2, scale);
        FillExact(g, new Rectangle(left, bottom - rows, width, rows), Theme.Accent);
    }

    /// <summary>One logical row of border starting at <paramref name="top"/>.</summary>
    public static void DrawRule(Graphics g, int left, int top, int width, float scale = 1f)
    {
        FillExact(g, new Rectangle(left, top, width, Px(1, scale)), Theme.Border);
    }

    /// <summary>
    /// A pill (fully rounded ends) filled with <paramref name="fill"/>, a one-logical-pixel
    /// <paramref name="border"/> outline, and <paramref name="text"/> centred on one line.
    /// The outline and fill are anti-aliased; the text is drawn with <see cref="TextRenderer"/>.
    /// </summary>
    public static void DrawPill(
        Graphics g, Rectangle bounds, string text, Font font, Color fill, Color border, Color textColor, float scale = 1f)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        var state = g.Save();
        try
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float width = Px(1, scale);
            var rect = new RectangleF(bounds.X + width / 2f, bounds.Y + width / 2f, bounds.Width - width, bounds.Height - width);
            using var path = PillPath(rect);
            using (var brush = new SolidBrush(fill))
                g.FillPath(brush, path);
            using (var pen = new Pen(border, width))
                g.DrawPath(pen, path);
        }
        finally
        {
            g.Restore(state);
        }

        TextRenderer.DrawText(
            g, text, font, bounds, textColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
    }

    /// <summary>
    /// A remove cross centred in <paramref name="bounds"/>: two anti-aliased strokes,
    /// <c>1.5</c> logical pixels wide, spanning 40% of the smaller side.
    /// </summary>
    public static void DrawCross(Graphics g, Rectangle bounds, Color color, float scale = 1f)
    {
        var state = g.Save();
        try
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var half = Math.Min(bounds.Width, bounds.Height) * 0.2f;
            var cx = bounds.Left + bounds.Width / 2f;
            var cy = bounds.Top + bounds.Height / 2f;
            using var pen = new Pen(color, 1.5f * scale) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(pen, cx - half, cy - half, cx + half, cy + half);
            g.DrawLine(pen, cx - half, cy + half, cx + half, cy - half);
        }
        finally
        {
            g.Restore(state);
        }
    }

    /// <summary>
    /// A plus sign centred in <paramref name="bounds"/>: two pixel-exact strokes one
    /// logical pixel thick and <paramref name="arm"/> logical pixels from the centre.
    /// </summary>
    public static void DrawPlus(Graphics g, Rectangle bounds, Color color, int arm, float scale = 1f)
    {
        var thickness = Px(1, scale);
        var reach = Px(arm, scale);
        var cx = bounds.Left + bounds.Width / 2;
        var cy = bounds.Top + bounds.Height / 2;
        FillExact(g, new Rectangle(cx - reach, cy - thickness / 2, 2 * reach + 1, thickness), color);
        FillExact(g, new Rectangle(cx - thickness / 2, cy - reach, thickness, 2 * reach + 1), color);
    }

    /// <summary>
    /// Fills <paramref name="bounds"/> as a capsule: fully rounded at its short ends,
    /// so a tall narrow rectangle becomes a scroll thumb.
    /// </summary>
    public static void FillCapsule(Graphics g, Rectangle bounds, Color color)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        var state = g.Save();
        try
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = new GraphicsPath();
            var d = Math.Min(bounds.Width, bounds.Height);
            if (bounds.Height >= bounds.Width)
            {
                path.AddArc(bounds.Left, bounds.Top, d, d, 180, 180);
                path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 0, 180);
            }
            else
            {
                path.AddArc(bounds.Left, bounds.Top, d, d, 90, 180);
                path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 180);
            }
            path.CloseFigure();
            using var brush = new SolidBrush(color);
            g.FillPath(brush, path);
        }
        finally
        {
            g.Restore(state);
        }
    }

    private static GraphicsPath PillPath(RectangleF rect)
    {
        var path = new GraphicsPath();
        var diameter = Math.Min(rect.Height, rect.Width);
        path.AddArc(rect.Left, rect.Top, diameter, diameter, 90, 180);
        path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 180);
        path.CloseFigure();
        return path;
    }

    private static void FillExact(Graphics g, Rectangle bounds, Color color)
    {
        var state = g.Save();
        try
        {
            g.SmoothingMode = SmoothingMode.None;
            Fill(g, bounds, color);
        }
        finally
        {
            g.Restore(state);
        }
    }

    private static void Fill(Graphics g, Rectangle bounds, Color color)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;
        using var brush = new SolidBrush(color);
        g.FillRectangle(brush, bounds);
    }

    private static void Frame(Graphics g, Rectangle bounds, Color color, int thickness)
    {
        Fill(g, new Rectangle(bounds.Left, bounds.Top, bounds.Width, thickness), color);
        Fill(g, new Rectangle(bounds.Left, bounds.Bottom - thickness, bounds.Width, thickness), color);
        Fill(g, new Rectangle(bounds.Left, bounds.Top, thickness, bounds.Height), color);
        Fill(g, new Rectangle(bounds.Right - thickness, bounds.Top, thickness, bounds.Height), color);
    }
}
