using Rigshift.Settings;

namespace Rigshift.Tests;

public class ColorFieldTests
{
    [Fact]
    public void TryParseHexAcceptsFullHexWithOptionalWhitespace()
    {
        Assert.True(ColorField.TryParseHex("#2E6BFF", out Color upper));
        Assert.Equal(Color.FromArgb(0x2E, 0x6B, 0xFF), upper);

        Assert.True(ColorField.TryParseHex("#2e6bff", out Color lower));
        Assert.Equal(Color.FromArgb(0x2E, 0x6B, 0xFF), lower);

        Assert.True(ColorField.TryParseHex(" #2E6BFF ", out Color padded));
        Assert.Equal(Color.FromArgb(0x2E, 0x6B, 0xFF), padded);
    }

    [Fact]
    public void TryParseHexRejectsBlankMissingHashShortAndNonHexInput()
    {
        string?[] rejected = { "", null, "2E6BFF", "#2E6BF", "#GGGGGG" };
        foreach (string? text in rejected)
        {
            Assert.False(ColorField.TryParseHex(text, out Color color));
            Assert.Equal(Color.Empty, color);
        }
    }

    [Fact]
    public void FormatHexReturnsUpperCaseHexBytes()
    {
        Assert.Equal("#12ABEF", ColorField.FormatHex(Color.FromArgb(0x12, 0xAB, 0xEF)));
    }

    [Fact]
    public void SettingValidHexTextColorsTheFieldAndRaisesHexChanged()
    {
        StaThread.Run(() =>
        {
            var field = new ColorField();
            int raised = 0;
            field.HexChanged += (_, _) => raised++;

            field.HexText = "#FF0000";

            Assert.Equal(Theme.Text, field.TextColor);
            Assert.Equal(Color.FromArgb(255, 0, 0), field.SwatchColor);
            Assert.Equal(1, raised);
        });
    }

    [Fact]
    public void SettingInvalidHexTextRestoresTheDefaultColors()
    {
        StaThread.Run(() =>
        {
            var field = new ColorField();
            field.HexText = "#FF0000";
            int raised = 0;
            field.HexChanged += (_, _) => raised++;

            field.HexText = "nope";

            Assert.Equal(Theme.Text, field.TextColor);
            Assert.Equal(Theme.Surface, field.SwatchColor);
            Assert.Equal(1, raised);
        });
    }

    [Fact]
    public void PickWritesUpperHexAndPassesTheCurrentParsedColor()
    {
        StaThread.Run(() =>
        {
            var field = new ColorField { HexText = "#AABBCC" };
            Color? received = null;
            field.PickColor = current =>
            {
                received = current;
                return Color.FromArgb(0x12, 0x34, 0x56);
            };

            field.Pick();

            Assert.Equal("#123456", field.HexText);
            Assert.Equal(Color.FromArgb(0xAA, 0xBB, 0xCC), received);
        });
    }

    [Fact]
    public void PickLeavesHexTextUnchangedWhenTheDialogReturnsNull()
    {
        StaThread.Run(() =>
        {
            var field = new ColorField { HexText = "#AABBCC" };
            field.PickColor = current => null;

            field.Pick();

            Assert.Equal("#AABBCC", field.HexText);
        });
    }

    [Fact]
    public void PickPassesBlackWhenTheTextIsNotHex()
    {
        StaThread.Run(() =>
        {
            var field = new ColorField { HexText = "nope" };
            Color? received = null;
            field.PickColor = current =>
            {
                received = current;
                return null;
            };

            field.Pick();

            Assert.Equal(Color.Black, received);
        });
    }

    [Fact]
    public void The_swatch_is_the_focusable_picker_and_the_row_fits_field_and_swatch()
    {
        StaThread.Run(() =>
        {
            var field = new ColorField();
            field.PerformLayout();

            // The visible Pick… button is gone; no Button remains in the field.
            var buttons = Descendants(field).Where(control => control is Button).ToList();
            Assert.Empty(buttons);

            var textBox = Descendants(field).OfType<ThemedTextBox>().Single();
            var swatch = Descendants(field).OfType<ColorField.Swatch>().Single();

            Assert.Equal(90, textBox.Width);
            Assert.Equal(new Size(23, 23), swatch.Size);
            Assert.True(swatch.TabStop, "The swatch is not focusable.");
            Assert.Equal("Pick", swatch.AccessibleName);

            int childrenSum = textBox.Width + swatch.Width;
            Assert.True(
                field.PreferredSize.Width >= childrenSum,
                $"Preferred width {field.PreferredSize.Width} must fit the {childrenSum}px of children.");

            // Clicking the swatch opens the picker through the seam.
            var clicks = 0;
            field.PickColor = current => { clicks++; return null; };
            swatch.PerformClick();
            Assert.Equal(1, clicks);
        });
    }

    [Fact]
    public void PerformLayoutShrinksTheFieldToItsOneRow()
    {
        StaThread.Run(() =>
        {
            var field = new ColorField();
            field.PerformLayout();

            // A bare auto-sized container on a worker thread has no parent to sync
            // its own size to its preferred, so measure the control the way the app
            // measures it: inside the flow row it is always placed in, laid out.
            var host = new FlowLayoutPanel { AutoSize = false };
            host.ClientSize = new Size(400, 400);
            host.Controls.Add(field);
            host.PerformLayout();

            var row = field.Controls.OfType<FlowLayoutPanel>().Single();

            Assert.Equal(row.PreferredSize.Height, row.Height);
            Assert.True(
                field.PreferredSize.Height <= 40 && field.Height <= 40,
                $"Preferred height {field.PreferredSize.Height} and actual height {field.Height} must each be at most 40.");
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
