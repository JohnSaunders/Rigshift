using System.Drawing.Imaging;
using System.Reflection;
using Rigshift.Settings;

namespace Rigshift.Tests;

/// <summary>
/// The text tab strip paints the selected tab with an accent underline over a border rule,
/// selects by click and arrow / Ctrl+Tab keys, and reparents nothing itself (the form does).
/// </summary>
public class TabStripTests
{
    [Fact]
    public void Paints_two_tabs_with_the_selected_in_Text_and_the_other_in_Muted()
    {
        StaThread.Run(() =>
        {
            using var strip = new TabStrip("Loadouts", "Definitions") { Size = new Size(200, 22) };
            using var bmp = RenderPaint(strip);

            var first = strip.TabBounds(0);
            var second = strip.TabBounds(1);
            Assert.True(AnyPixel(bmp, TextArea(first), Theme.Text), "Selected tab is not Text.");
            Assert.False(AnyPixel(bmp, TextArea(second), Theme.Text), "Unselected tab drew Text.");
            Assert.True(AnyPixel(bmp, TextArea(second), Theme.Muted), "Unselected tab is not Muted.");
            Assert.False(AnyPixel(bmp, TextArea(first), Theme.Muted), "Selected tab drew Muted.");
        });
    }

    [Fact]
    public void The_selected_tab_draws_an_accent_underline_and_the_unselected_does_not()
    {
        StaThread.Run(() =>
        {
            using var strip = new TabStrip("Loadouts", "Definitions") { Size = new Size(200, 22) };
            using (var bmp = RenderPaint(strip))
            {
                Assert.True(AnyPixel(bmp, new Rectangle(strip.TabBounds(0).Left, 16, strip.TabBounds(0).Width, 4), Theme.Accent),
                    "Selected tab has no accent underline.");
            }

            strip.SelectedIndex = 1;
            using (var bmp = RenderPaint(strip))
            {
                Assert.False(AnyPixel(bmp, new Rectangle(strip.TabBounds(0).Left, 16, strip.TabBounds(0).Width, 4), Theme.Accent),
                    "A now-unselected tab still drew its accent underline.");
            }
        });
    }

    [Fact]
    public void The_tab_strip_ends_in_a_border_rule()
    {
        StaThread.Run(() =>
        {
            using var strip = new TabStrip("Loadouts", "Definitions") { Size = new Size(200, 22) };
            using var bmp = RenderPaint(strip);

            var row = new Rectangle(0, 20, 200, 2);
            Assert.True(AnyPixel(bmp, row, Theme.Border), "No border rule at the bottom of the tab strip.");
        });
    }

    [Fact]
    public void Clicking_a_tab_selects_it()
    {
        StaThread.Run(() =>
        {
            using var strip = new TabStrip("Loadouts", "Definitions") { Size = new Size(200, 22) };
            var centre = new Point(strip.TabBounds(1).Left + 4, 8);
            Click(strip, centre);
            Assert.Equal(1, strip.SelectedIndex);
        });
    }

    [Fact]
    public void Arrow_keys_and_Ctrl_Tab_move_the_selection()
    {
        StaThread.Run(() =>
        {
            using var strip = new TabStrip("Loadouts", "Definitions") { Size = new Size(200, 22) };
            Press(strip, Keys.Right);
            Assert.Equal(1, strip.SelectedIndex);
            Press(strip, Keys.Left);
            Assert.Equal(0, strip.SelectedIndex);

            // Ctrl+Tab advances (wraps), Ctrl+Shift+Tab retreats.
            Press(strip, Keys.Tab | Keys.Control);
            Assert.Equal(1, strip.SelectedIndex);
            Press(strip, Keys.Tab | Keys.Control);
            Assert.Equal(0, strip.SelectedIndex);
            Press(strip, Keys.Tab | Keys.Control | Keys.Shift);
            Assert.Equal(1, strip.SelectedIndex);
        });
    }

    [Fact]
    public void Selection_changes_raise_SelectedIndexChanged()
    {
        StaThread.Run(() =>
        {
            using var strip = new TabStrip("Loadouts", "Definitions") { Size = new Size(200, 22) };
            var changes = 0;
            strip.SelectedIndexChanged += (_, _) => changes++;
            strip.SelectedIndex = 0;   // no change
            Assert.Equal(0, changes);
            strip.SelectedIndex = 1;
            Assert.Equal(1, changes);
            strip.SelectedIndex = 1;   // no change
            Assert.Equal(1, changes);
        });
    }

    [Fact]
    public void TabBounds_follow_the_layout_columns()
    {
        StaThread.Run(() =>
        {
            using var strip = new TabStrip("Loadouts", "Definitions") { Size = new Size(200, 22) };
            var a = strip.TabBounds(0);
            var b = strip.TabBounds(1);
            Assert.True(a.Left > 0, "First tab does not start at the left padding.");
            Assert.True(b.Left > a.Right, "Second tab does not follow the first.");
            Assert.Equal(22, a.Height);
            Assert.Equal(strip.Titles, new[] { "Loadouts", "Definitions" });
        });
    }

    private static Rectangle TextArea(Rectangle bounds) => new(bounds.Left + 1, 2, Math.Max(1, bounds.Width - 2), 16);

    private static Bitmap RenderPaint(Control control)
    {
        var bmp = new Bitmap(control.Width, control.Height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        using var args = new PaintEventArgs(g, new Rectangle(Point.Empty, control.Size));
        Invoke(control, "OnPaint", args);
        return bmp;
    }

    private static void Click(Control control, Point location)
    {
        var e = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);
        Invoke(control, "OnMouseClick", e);
    }

    private static void Press(Control control, Keys keys)
    {
        var e = new KeyEventArgs(keys);
        Invoke(control, "OnKeyDown", e);
    }

    private static void Invoke(Control control, string method, object arg)
    {
        var info = control.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { arg.GetType() }, null)
                   ?? throw new MissingMethodException(control.GetType().Name, method);
        info.Invoke(control, new[] { arg });
    }

    private static bool AnyPixel(Bitmap bmp, Rectangle area, Color color)
    {
        var target = color.ToArgb();
        for (var x = Math.Max(0, area.Left); x < Math.Min(bmp.Width, area.Right); x++)
            for (var y = Math.Max(0, area.Top); y < Math.Min(bmp.Height, area.Bottom); y++)
                if (bmp.GetPixel(x, y).ToArgb() == target)
                    return true;
        return false;
    }
}
