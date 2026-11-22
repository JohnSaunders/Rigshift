using Rigshift.Settings;

namespace Rigshift.Tests;

/// <summary>
/// The About window follows the Settings theme: dark caption, accent glyph, the name,
/// version and tagline, and a primary OK button that closes it.
/// </summary>
public class AboutFormTests
{
    [Fact]
    public void The_About_window_uses_the_theme_caption_and_surface()
    {
        StaThread.Run(() =>
        {
            using var form = new AboutForm();

            Assert.Equal("About Rigshift", form.Text);
            Assert.Equal(Theme.Bg, form.FormCaptionBackColor);
            Assert.Equal(Theme.Text, form.FormCaptionTextColor);
            Assert.Equal(Theme.Border, form.FormBorderColor);
            Assert.Equal(Theme.Surface, form.BackColor);
            Assert.False(form.MaximizeBox);
            Assert.False(form.MinimizeBox);
        });
    }

    [Fact]
    public void The_About_window_shows_the_name_version_and_tagline()
    {
        StaThread.Run(() =>
        {
            using var form = new AboutForm();
            var texts = Descendants(form).OfType<Label>().Select(l => l.Text).ToList();

            Assert.Contains("Rigshift", texts);
            Assert.Contains($"Version {SettingsForm.ProductVersion}", texts);
            Assert.Equal("Version 0.1.0", AboutForm.VersionText);
            Assert.Contains(AboutForm.Tagline, texts);
        });
    }

    [Fact]
    public void OK_is_the_primary_button_and_closes_the_window()
    {
        StaThread.Run(() =>
        {
            using var form = new AboutForm();
            var ok = Assert.IsType<ThemedButton>(form.OkButton);

            Assert.Equal("OK", ok.Text);
            Assert.True(ok.Primary);
            Assert.Equal(DialogResult.OK, ok.DialogResult);
            Assert.Same(ok, form.AcceptButton);
            Assert.Same(ok, form.CancelButton);
        });
    }

    [Fact]
    public void Every_fully_opaque_pixel_of_the_About_icon_is_the_accent()
    {
        StaThread.Run(() =>
        {
            using var form = new AboutForm();
            using var icon = form.Icon!.ToBitmap();
            var opaque = 0;
            for (var x = 0; x < icon.Width; x++)
            {
                for (var y = 0; y < icon.Height; y++)
                {
                    var pixel = icon.GetPixel(x, y);
                    if (pixel.A != 255)
                        continue;
                    opaque++;
                    Assert.Equal(Theme.Accent.ToArgb(), pixel.ToArgb());
                }
            }

            Assert.True(opaque > 0);
        });
    }

    private static IEnumerable<Control> Descendants(Control control)
    {
        foreach (Control child in control.Controls)
        {
            yield return child;
            foreach (var deeper in Descendants(child))
                yield return deeper;
        }
    }
}
