using Rigshift.Settings;

namespace Rigshift.Tests;

public class ThemeTests
{
    [Fact]
    public void The_colour_tokens_are_the_site_values()
    {
        Assert.Equal(Color.FromArgb(0x05, 0x10, 0x0F), Theme.Bg);
        Assert.Equal(Color.FromArgb(0x0B, 0x1A, 0x19), Theme.Surface);
        Assert.Equal(Color.FromArgb(0xE9, 0xF5, 0xF0), Theme.Text);
        Assert.Equal(Color.FromArgb(0x93, 0xAA, 0xA6), Theme.Muted);
        Assert.Equal(Color.FromArgb(0x1B, 0x2E, 0x2D), Theme.Border);
        Assert.Equal(Color.FromArgb(0x62, 0x80, 0x7C), Theme.InputBorder);
        Assert.Equal(Color.FromArgb(0x35, 0xC9, 0xE8), Theme.Accent);
        Assert.Equal(Color.FromArgb(0x02, 0x10, 0x14), Theme.AccentContrast);

        Assert.Equal(255, Theme.Bg.A);
        Assert.Equal(255, Theme.Surface.A);
        Assert.Equal(255, Theme.Text.A);
        Assert.Equal(255, Theme.Muted.A);
        Assert.Equal(255, Theme.Border.A);
        Assert.Equal(255, Theme.InputBorder.A);
        Assert.Equal(255, Theme.Accent.A);
        Assert.Equal(255, Theme.AccentContrast.A);
    }

    [Fact]
    public void Blend_returns_the_bottom_at_zero_and_the_top_at_one()
    {
        Assert.Equal(Color.FromArgb(0x0B, 0x1A, 0x19), Theme.Blend(Theme.Accent, Theme.Surface, 0));
        Assert.Equal(Color.FromArgb(0x35, 0xC9, 0xE8), Theme.Blend(Theme.Accent, Theme.Surface, 1));
    }

    [Fact]
    public void The_selected_fill_is_eleven_percent_accent_over_the_surface()
    {
        Assert.Equal(Color.FromArgb(0x10, 0x2D, 0x30), Theme.SelectedFill);
    }

    [Fact]
    public void The_fonts_are_Segoe_UI_and_Consolas_at_nine_points()
    {
        Assert.Equal("Segoe UI", Theme.UiFont.Name);
        Assert.Equal("Segoe UI Semibold", Theme.UiSemibold.Name);
        Assert.Equal("Consolas", Theme.MonoFont.Name);
        Assert.Equal(9F, Theme.UiFont.SizeInPoints);
        Assert.Equal(9F, Theme.UiSemibold.SizeInPoints);
        Assert.Equal(9F, Theme.MonoFont.SizeInPoints);
    }
}
