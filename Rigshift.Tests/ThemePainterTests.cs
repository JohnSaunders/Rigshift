using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Rigshift.Settings;

namespace Rigshift.Tests;

/// <summary>
/// Pixel facts for <see cref="ThemePainter"/>: each primitive draws into a bitmap
/// pre-filled with a token and the test reads exact pixels back.
/// </summary>
public class ThemePainterTests
{
    [Fact]
    public void Px_rounds_half_away_from_zero_and_never_returns_less_than_one()
    {
        Assert.Equal(1, ThemePainter.Px(1, 1f));
        Assert.Equal(2, ThemePainter.Px(1, 1.5f));
        Assert.Equal(1, ThemePainter.Px(1, 1.25f));
        Assert.Equal(3, ThemePainter.Px(2, 1.5f));
        Assert.Equal(33, ThemePainter.Px(26, 1.25f));
        Assert.Equal(1, ThemePainter.Px(1, 0.5f));
        Assert.Equal(1, ThemePainter.Px(0, 1f));
    }

    [Fact]
    public void FillFrame_draws_a_one_pixel_border_around_the_fill_and_doubles_it_at_scale_two()
    {
        using (var bmp = Canvas(20, 10, Theme.Bg))
        {
            Draw(bmp, g => ThemePainter.FillFrame(g, new Rectangle(2, 2, 10, 6), Theme.Surface, Theme.Border, 1f));
            AssertPixel(bmp, 2, 2, Theme.Border);
            AssertPixel(bmp, 6, 2, Theme.Border);
            AssertPixel(bmp, 6, 7, Theme.Border);
            AssertPixel(bmp, 2, 5, Theme.Border);
            AssertPixel(bmp, 11, 4, Theme.Border);
            AssertPixel(bmp, 3, 3, Theme.Surface);
            AssertPixel(bmp, 6, 4, Theme.Surface);
            AssertPixel(bmp, 10, 6, Theme.Surface);
            AssertPixel(bmp, 1, 1, Theme.Bg);
            AssertPixel(bmp, 12, 4, Theme.Bg);
            AssertPixel(bmp, 6, 8, Theme.Bg);
        }

        using (var bmp = Canvas(30, 20, Theme.Bg))
        {
            Draw(bmp, g => ThemePainter.FillFrame(g, new Rectangle(2, 2, 20, 12), Theme.Surface, Theme.Border, 2f));
            AssertPixel(bmp, 3, 6, Theme.Border);
            AssertPixel(bmp, 4, 6, Theme.Surface);
            AssertPixel(bmp, 20, 6, Theme.Border);
            AssertPixel(bmp, 19, 6, Theme.Surface);
            AssertPixel(bmp, 8, 3, Theme.Border);
            AssertPixel(bmp, 8, 4, Theme.Surface);
            AssertPixel(bmp, 8, 12, Theme.Border);
            AssertPixel(bmp, 8, 11, Theme.Surface);
        }
    }

    [Fact]
    public void DrawField_has_an_input_border_bottom_unfocused_an_accent_bottom_focused_and_a_border_bottom_disabled()
    {
        var field = new Rectangle(0, 0, 20, 10);

        using (var bmp = Canvas(24, 12, Theme.Bg))
        {
            Draw(bmp, g => ThemePainter.DrawField(g, field, focused: false, enabled: true, 1f));
            AssertPixel(bmp, 5, 9, Theme.InputBorder);
            AssertPixel(bmp, 5, 8, Theme.Surface);
            AssertPixel(bmp, 0, 5, Theme.Border);
            AssertPixel(bmp, 19, 5, Theme.Border);
            AssertPixel(bmp, 5, 0, Theme.Border);
            AssertPixel(bmp, 5, 5, Theme.Surface);
            AssertPixel(bmp, 5, 10, Theme.Bg);
        }

        using (var bmp = Canvas(24, 12, Theme.Bg))
        {
            Draw(bmp, g => ThemePainter.DrawField(g, field, focused: true, enabled: true, 1f));
            AssertPixel(bmp, 5, 9, Theme.Accent);
            AssertPixel(bmp, 5, 8, Theme.Accent);
            AssertPixel(bmp, 5, 7, Theme.Surface);
            AssertPixel(bmp, 0, 5, Theme.Border);
        }

        using (var bmp = Canvas(24, 12, Theme.Bg))
        {
            Draw(bmp, g => ThemePainter.DrawField(g, field, focused: true, enabled: false, 1f));
            AssertPixel(bmp, 5, 9, Theme.Border);
            AssertPixel(bmp, 5, 8, Theme.Surface);
        }
    }

    [Fact]
    public void DrawButton_outlined_primary_hot_pressed_and_disabled_use_their_fills_and_outlines()
    {
        // (primary, hot, pressed, enabled) → (outline at the corner, fill just inside it)
        var cases = new (bool Primary, bool Hot, bool Pressed, bool Enabled, Color Outline, Color Fill)[]
        {
            (false, false, false, true, Theme.Border, Theme.Surface),
            (false, true, false, true, Theme.InputBorder, Theme.Surface),
            (false, false, true, true, Theme.InputBorder, Theme.SelectedFill),
            (true, false, false, true, Theme.Accent, Theme.Accent),
            (true, true, false, true, Theme.Blend(Theme.Text, Theme.Accent, 0.15), Theme.Blend(Theme.Text, Theme.Accent, 0.15)),
            (true, false, true, true, Theme.Blend(Theme.AccentContrast, Theme.Accent, 0.15), Theme.Blend(Theme.AccentContrast, Theme.Accent, 0.15)),
            (false, false, false, false, Theme.Border, Theme.Surface),
            (true, false, false, false, Theme.Border, Theme.Border),
        };

        foreach (var c in cases)
        {
            using var bmp = Canvas(44, 24, Theme.Bg);
            Draw(bmp, g => ThemePainter.DrawButton(
                g, new Rectangle(0, 0, 40, 20), "", Theme.UiFont, c.Primary, c.Hot, c.Pressed, c.Enabled, 1f));
            AssertPixel(bmp, 0, 0, c.Outline);
            AssertPixel(bmp, 39, 19, c.Outline);
            AssertPixel(bmp, 2, 2, c.Fill);
            AssertPixel(bmp, 20, 10, c.Fill);
            AssertPixel(bmp, 41, 10, Theme.Bg);
        }

        // With text: the corners keep the fill and some pixel in the middle is the text.
        using (var bmp = Canvas(90, 26, Theme.Bg))
        {
            Draw(bmp, g => ThemePainter.DrawButton(
                g, new Rectangle(0, 0, 87, 26), "Save", Theme.UiSemibold, primary: true, hot: false, pressed: false, enabled: true, 1f));
            AssertPixel(bmp, 1, 1, Theme.Accent);
            AssertPixel(bmp, 85, 24, Theme.Accent);
            // Glyph stems render in exactly the text colour; ClearType fringes do not, and can
            // fall outside both colours, so only an exact match proves the text colour.
            Assert.True(AnyPixel(bmp, new Rectangle(20, 4, 47, 18), p => p == Theme.AccentContrast.ToArgb()),
                "The primary button text is not AccentContrast.");
        }

        // Outlined: the glyph stems are exactly Text.
        using (var bmp = Canvas(90, 26, Theme.Bg))
        {
            Draw(bmp, g => ThemePainter.DrawButton(
                g, new Rectangle(0, 0, 87, 26), "Cancel", Theme.UiFont, primary: false, hot: false, pressed: false, enabled: true, 1f));
            Assert.True(AnyPixel(bmp, new Rectangle(20, 4, 47, 18), p => p == Theme.Text.ToArgb()),
                "The outlined button text is not Text.");
        }
    }

    [Fact]
    public void DrawCheck_unchecked_is_an_outlined_box_and_checked_is_an_accent_box_with_a_dark_tick()
    {
        var box = new Rectangle(2, 2, 16, 16);

        using (var bmp = Canvas(20, 20, Theme.Bg))
        {
            Draw(bmp, g => ThemePainter.DrawCheck(g, box, isChecked: false, enabled: true, 1f));
            AssertPixel(bmp, 2, 2, Theme.InputBorder);
            AssertPixel(bmp, 17, 17, Theme.InputBorder);
            AssertPixel(bmp, 10, 10, Theme.Surface);
            AssertPixel(bmp, 1, 1, Theme.Bg);
        }

        using (var bmp = Canvas(20, 20, Theme.Bg))
        {
            Draw(bmp, g => ThemePainter.DrawCheck(g, box, isChecked: false, enabled: false, 1f));
            AssertPixel(bmp, 2, 2, Theme.Border);
            AssertPixel(bmp, 10, 10, Theme.Surface);
        }

        using (var bmp = Canvas(20, 20, Theme.Bg))
        {
            Draw(bmp, g => ThemePainter.DrawCheck(g, box, isChecked: true, enabled: true, 1f));
            AssertPixel(bmp, 2, 2, Theme.Accent);
            AssertPixel(bmp, 3, 3, Theme.Accent);
            AssertPixel(bmp, 17, 17, Theme.Accent);
            AssertPixel(bmp, 17, 2, Theme.Accent);
            AssertPixel(bmp, 1, 1, Theme.Bg);
            Assert.True(AnyPixel(bmp, box, p => p == Theme.AccentContrast.ToArgb()), "No tick pixel is AccentContrast.");
        }

        using (var bmp = Canvas(20, 20, Theme.Bg))
        {
            Draw(bmp, g => ThemePainter.DrawCheck(g, box, isChecked: true, enabled: false, 1f));
            AssertPixel(bmp, 3, 3, Theme.Muted);
            Assert.True(AnyPixel(bmp, box, p => p == Theme.Surface.ToArgb()), "No disabled tick pixel is Surface.");
        }
    }

    [Fact]
    public void DrawPip_fills_its_centre_with_the_colour_and_a_null_colour_draws_a_ring_with_an_untouched_middle()
    {
        var blue = Color.FromArgb(0x2E, 0x6B, 0xFF);
        var square = new Rectangle(5, 5, 11, 11);

        using (var bmp = Canvas(20, 20, Theme.Surface))
        {
            Draw(bmp, g => ThemePainter.DrawPip(g, new Point(10, 10), blue, 1f));
            AssertPixel(bmp, 10, 10, blue);
            AssertPixel(bmp, 9, 9, blue);
            AssertPixel(bmp, 1, 1, Theme.Surface);
            AssertPixel(bmp, 18, 18, Theme.Surface);
        }

        using (var bmp = Canvas(20, 20, Theme.Surface))
        {
            Draw(bmp, g => ThemePainter.DrawPip(g, new Point(10, 10), null, 1f));
            AssertPixel(bmp, 10, 10, Theme.Surface);
            AssertPixel(bmp, 9, 9, Theme.Surface);
            AssertPixel(bmp, 1, 1, Theme.Surface);
            Assert.True(AnyPixel(bmp, square, p => p != Theme.Surface.ToArgb()), "The empty pip drew no ring.");
        }

        // At 2x the ring is two device pixels wide, so its core is exactly InputBorder.
        using (var bmp = Canvas(40, 40, Theme.Surface))
        {
            Draw(bmp, g => ThemePainter.DrawPip(g, new Point(20, 20), null, 2f));
            AssertPixel(bmp, 20, 20, Theme.Surface);
            Assert.True(AnyPixel(bmp, new Rectangle(10, 10, 21, 21), p => p == Theme.InputBorder.ToArgb()),
                "The empty pip ring is not InputBorder.");
        }
    }

    [Fact]
    public void DrawSelectedRow_fills_the_row_and_draws_a_centred_half_height_accent_bar()
    {
        using var bmp = Canvas(44, 34, Theme.Bg);
        Draw(bmp, g => ThemePainter.DrawSelectedRow(g, new Rectangle(0, 0, 40, 30), 1f));

        // Bar: 2 wide, 15 tall, rows 7..21.
        AssertPixel(bmp, 0, 7, Theme.Accent);
        AssertPixel(bmp, 1, 21, Theme.Accent);
        AssertPixel(bmp, 0, 6, Theme.SelectedFill);
        AssertPixel(bmp, 0, 22, Theme.SelectedFill);
        AssertPixel(bmp, 2, 15, Theme.SelectedFill);
        AssertPixel(bmp, 39, 29, Theme.SelectedFill);
        AssertPixel(bmp, 41, 15, Theme.Bg);
        AssertPixel(bmp, 20, 31, Theme.Bg);
    }

    [Fact]
    public void DrawUnderline_and_DrawRule_cover_exactly_their_rows()
    {
        using (var bmp = Canvas(30, 30, Theme.Bg))
        {
            Draw(bmp, g =>
            {
                ThemePainter.DrawUnderline(g, 5, 10, 20, 1f);
                ThemePainter.DrawRule(g, 0, 25, 30, 1f);
            });
            AssertPixel(bmp, 5, 18, Theme.Accent);
            AssertPixel(bmp, 14, 19, Theme.Accent);
            AssertPixel(bmp, 5, 17, Theme.Bg);
            AssertPixel(bmp, 5, 20, Theme.Bg);
            AssertPixel(bmp, 4, 18, Theme.Bg);
            AssertPixel(bmp, 15, 18, Theme.Bg);

            AssertPixel(bmp, 0, 25, Theme.Border);
            AssertPixel(bmp, 29, 25, Theme.Border);
            AssertPixel(bmp, 0, 24, Theme.Bg);
            AssertPixel(bmp, 0, 26, Theme.Bg);
        }

        using (var bmp = Canvas(30, 30, Theme.Bg))
        {
            Draw(bmp, g => ThemePainter.DrawUnderline(g, 5, 10, 20, 2f));
            AssertPixel(bmp, 5, 16, Theme.Accent);
            AssertPixel(bmp, 5, 19, Theme.Accent);
            AssertPixel(bmp, 5, 15, Theme.Bg);
        }
    }

    [Fact]
    public void A_primitive_restores_the_graphics_state_it_changed()
    {
        using var bmp = Canvas(40, 40, Theme.Bg);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.None;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        var smoothing = g.SmoothingMode;
        var offset = g.PixelOffsetMode;

        ThemePainter.DrawPip(g, new Point(10, 10), Theme.Accent, 1f);
        ThemePainter.DrawPip(g, new Point(20, 10), null, 1f);
        ThemePainter.DrawCheck(g, new Rectangle(2, 20, 16, 16), isChecked: true, enabled: true, 1f);
        ThemePainter.DrawField(g, new Rectangle(20, 20, 18, 10), focused: true, enabled: true, 1f);
        ThemePainter.DrawButton(g, new Rectangle(0, 0, 40, 20), "Save", Theme.UiFont, primary: true, hot: false, pressed: false, enabled: true, 1f);
        ThemePainter.DrawUnderline(g, 0, 20, 30, 1f);
        ThemePainter.DrawRule(g, 0, 32, 30, 1f);

        Assert.Equal(smoothing, g.SmoothingMode);
        Assert.Equal(offset, g.PixelOffsetMode);

        g.SmoothingMode = SmoothingMode.AntiAlias;
        ThemePainter.FillFrame(g, new Rectangle(0, 0, 10, 10), Theme.Surface, Theme.Border, 1f);
        ThemePainter.DrawSelectedRow(g, new Rectangle(0, 0, 20, 10), 1f);
        Assert.Equal(SmoothingMode.AntiAlias, g.SmoothingMode);
    }

    private static Bitmap Canvas(int width, int height, Color background)
    {
        var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.Clear(background);
        return bmp;
    }

    private static void Draw(Bitmap bmp, Action<Graphics> paint)
    {
        using var g = Graphics.FromImage(bmp);
        paint(g);
    }

    private static void AssertPixel(Bitmap bmp, int x, int y, Color expected)
    {
        var actual = bmp.GetPixel(x, y);
        Assert.True(
            actual.ToArgb() == expected.ToArgb(),
            $"Pixel ({x},{y}) is #{actual.A:X2}{actual.R:X2}{actual.G:X2}{actual.B:X2}, expected #{expected.A:X2}{expected.R:X2}{expected.G:X2}{expected.B:X2}.");
    }

    private static bool AnyPixel(Bitmap bmp, Rectangle area, Func<int, bool> match)
    {
        for (var x = area.Left; x < area.Right; x++)
        {
            for (var y = area.Top; y < area.Bottom; y++)
            {
                if (match(bmp.GetPixel(x, y).ToArgb()))
                    return true;
            }
        }
        return false;
    }
}
