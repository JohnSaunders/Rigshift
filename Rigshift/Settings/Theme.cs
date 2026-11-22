namespace Rigshift.Settings;

/// <summary>
/// The Settings window's colour tokens, fonts, and metrics. The colours are the
/// The project's design tokens. No other file names a colour literal for
/// window chrome; user loadout colours from the config are data and are not here.
/// </summary>
public static class Theme
{
    public static readonly Color Bg = Color.FromArgb(0x05, 0x10, 0x0F);
    public static readonly Color Surface = Color.FromArgb(0x0B, 0x1A, 0x19);
    public static readonly Color Text = Color.FromArgb(0xE9, 0xF5, 0xF0);
    public static readonly Color Muted = Color.FromArgb(0x93, 0xAA, 0xA6);
    public static readonly Color Border = Color.FromArgb(0x1B, 0x2E, 0x2D);
    public static readonly Color InputBorder = Color.FromArgb(0x62, 0x80, 0x7C);
    public static readonly Color Accent = Color.FromArgb(0x35, 0xC9, 0xE8);
    public static readonly Color AccentContrast = Color.FromArgb(0x02, 0x10, 0x14);

    /// <summary>The site's pink topic colour, used for the hovered remove glyph.</summary>
    public static readonly Color Danger = Color.FromArgb(0xFF, 0x5D, 0x8F);

    // The site's topic colours. Each reference card takes one, so the lists read apart.
    public static readonly Color Teal = Color.FromArgb(0x3F, 0xE0, 0xC5);
    public static readonly Color Pink = Danger;
    public static readonly Color Amber = Color.FromArgb(0xF6, 0xB3, 0x52);
    public static readonly Color Blue = Color.FromArgb(0x8E, 0xA8, 0xFF);

    /// <summary>The site's caution colour, for warnings and errors.</summary>
    public static readonly Color Caution = Amber;

    /// <summary>A card raised off the surface: 3.5% text over the surface.</summary>
    public static readonly Color Card = Blend(Text, Surface, 0.035);

    /// <summary>The count badge fill: 14% accent over a card.</summary>
    public static readonly Color AccentTint = Blend(Accent, Card, 0.14);

    /// <summary>The selected list row: 11% accent over the surface.</summary>
    public static readonly Color SelectedFill = Blend(Accent, Surface, 0.11);

    public static readonly Font UiFont = new("Segoe UI", 9F);
    public static readonly Font UiSemibold = new("Segoe UI Semibold", 9F);
    public static readonly Font MonoFont = new("Consolas", 9F);
    public static readonly Font SmallFont = new("Segoe UI", 8F);
    public static readonly Font HeadingFont = new("Segoe UI Semibold", 13F);

    /// <summary>The site's eyebrow: small mono capitals, tracked out, for field labels.</summary>
    public static readonly Font EyebrowFont = new("Consolas", 8F);

    /// <summary>Extra space between eyebrow letters, in pixels at 96 DPI.</summary>
    public const int EyebrowTracking = 1;

    public const int RowHeight = 30;
    public const int FieldHeight = 26;
    public const int ButtonHeight = 26;
    public const int Gap = 14;

    /// <summary>Width of the label column that lines up field rows in both editors.</summary>
    public const int LabelColumnWidth = 96;

    /// <summary>Width of the loadout and definition lists on the left of each tab.</summary>
    public const int ListPanelWidth = 208;

    /// <summary>
    /// Mixes <paramref name="top"/> over <paramref name="bottom"/>: each channel is
    /// <c>bottom + (top - bottom) * amount</c>, rounded half away from zero. The result is opaque.
    /// </summary>
    public static Color Blend(Color top, Color bottom, double amount)
    {
        static int Mix(int t, int b, double a) => (int)Math.Round(b + (t - b) * a, MidpointRounding.AwayFromZero);
        return Color.FromArgb(Mix(top.R, bottom.R, amount), Mix(top.G, bottom.G, amount), Mix(top.B, bottom.B, amount));
    }
}
