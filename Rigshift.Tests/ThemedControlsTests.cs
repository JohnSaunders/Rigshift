using System.Drawing.Imaging;
using System.Reflection;
using Rigshift.Settings;

namespace Rigshift.Tests;

/// <summary>
/// The themed button and checkbox paint from Theme through ThemePainter and keep their
/// stock behaviour. Rendering uses DrawToBitmap; no control is ever shown.
/// </summary>
public class ThemedControlsTests
{
    [Fact]
    public void A_button_is_outlined_by_default_and_primary_switches_its_font_and_colours()
    {
        StaThread.Run(() =>
        {
            using var button = new ThemedButton { Text = "Cancel" };

            Assert.False(button.Primary);
            Assert.Equal(FlatStyle.Flat, button.FlatStyle);
            Assert.Equal(Theme.UiFont.Name, button.Font.Name);
            Assert.Equal(Theme.Surface, button.BackColor);
            Assert.Equal(Theme.Text, button.ForeColor);

            button.Primary = true;
            Assert.Equal(Theme.UiSemibold.Name, button.Font.Name);
            Assert.Equal(Theme.Accent, button.BackColor);
            Assert.Equal(Theme.AccentContrast, button.ForeColor);

            button.Primary = false;
            Assert.Equal(Theme.UiFont.Name, button.Font.Name);
            Assert.Equal(Theme.Surface, button.BackColor);
            Assert.Equal(Theme.Text, button.ForeColor);
        });
    }

    [Fact]
    public void An_outlined_button_paints_a_surface_fill_with_a_border_outline()
    {
        StaThread.Run(() =>
        {
            using var button = new ThemedButton { Text = "", Size = new Size(80, 26) };
            using var bmp = Render(button);

            AssertPixel(bmp, 0, 0, Theme.Border);
            AssertPixel(bmp, 79, 25, Theme.Border);
            AssertPixel(bmp, 40, 13, Theme.Surface);
        });
    }

    [Fact]
    public void A_primary_button_paints_an_accent_fill()
    {
        StaThread.Run(() =>
        {
            using var button = new ThemedButton { Text = "", Primary = true, Size = new Size(80, 26) };
            using var bmp = Render(button);

            AssertPixel(bmp, 0, 0, Theme.Accent);
            AssertPixel(bmp, 40, 13, Theme.Accent);
        });
    }

    [Fact]
    public void Hot_and_pressed_follow_the_mouse_and_the_space_key_and_clear_on_leave()
    {
        StaThread.Run(() =>
        {
            using var button = new ThemedButton { Text = "Add" };

            Invoke(button, "OnMouseEnter", EventArgs.Empty);
            Assert.True(button.IsHot);
            Assert.False(button.IsPressed);

            Invoke(button, "OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));
            Assert.True(button.IsPressed);
            Invoke(button, "OnMouseUp", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));
            Assert.False(button.IsPressed);

            Invoke(button, "OnMouseDown", new MouseEventArgs(MouseButtons.Right, 1, 5, 5, 0));
            Assert.False(button.IsPressed);

            Invoke(button, "OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));
            Invoke(button, "OnMouseLeave", EventArgs.Empty);
            Assert.False(button.IsHot);
            Assert.False(button.IsPressed);

            Invoke(button, "OnKeyDown", new KeyEventArgs(Keys.Space));
            Assert.True(button.IsPressed);
            Invoke(button, "OnKeyUp", new KeyEventArgs(Keys.Space));
            Assert.False(button.IsPressed);

            Invoke(button, "OnKeyDown", new KeyEventArgs(Keys.Space));
            Invoke(button, "OnLostFocus", EventArgs.Empty);
            Assert.False(button.IsPressed);

            // Either input holds it down: releasing Space while the mouse is held keeps it pressed.
            Invoke(button, "OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));
            Invoke(button, "OnKeyDown", new KeyEventArgs(Keys.Space));
            Invoke(button, "OnKeyUp", new KeyEventArgs(Keys.Space));
            Assert.True(button.IsPressed);
            Invoke(button, "OnMouseUp", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));
            Assert.False(button.IsPressed);
        });
    }

    [Fact]
    public void A_hot_outlined_button_paints_an_input_border_outline()
    {
        StaThread.Run(() =>
        {
            using var button = new ThemedButton { Text = "", Size = new Size(80, 26) };
            Invoke(button, "OnMouseEnter", EventArgs.Empty);
            using var bmp = Render(button);

            AssertPixel(bmp, 0, 0, Theme.InputBorder);
            AssertPixel(bmp, 40, 13, Theme.Surface);
        });
    }

    [Fact]
    public void A_disabled_primary_button_paints_a_border_fill()
    {
        StaThread.Run(() =>
        {
            using var button = new ThemedButton { Text = "", Primary = true, Enabled = false, Size = new Size(80, 26) };
            using var bmp = Render(button);

            AssertPixel(bmp, 40, 13, Theme.Border);
        });
    }

    [Fact]
    public void The_button_drops_a_single_mnemonic_marker_from_its_preferred_width()
    {
        StaThread.Run(() =>
        {
            using var plain = new ThemedButton { Text = "Duplicate copy of a long name" };
            using var marked = new ThemedButton { Text = "&Duplicate copy of a long name" };

            Assert.Equal(plain.GetPreferredSize(Size.Empty), marked.GetPreferredSize(Size.Empty));
            Assert.Equal("Save & close", ThemedText.StripMnemonic("Save && close"));
            Assert.Equal("Save", ThemedText.StripMnemonic("&Save"));

            var scale = plain.DeviceDpi / 96f;
            var textWidth = TextRenderer.MeasureText("Duplicate copy of a long name", plain.Font).Width;
            Assert.Equal(textWidth + 2 * ThemePainter.Px(16, scale), plain.GetPreferredSize(Size.Empty).Width);
            Assert.Equal(ThemePainter.Px(Theme.ButtonHeight, scale), plain.GetPreferredSize(Size.Empty).Height);
            using var tiny = new ThemedButton { Text = "X" };
            Assert.Equal(ThemePainter.Px(64, scale), tiny.GetPreferredSize(Size.Empty).Width);
        });
    }

    [Fact]
    public void A_button_click_still_raises_Click()
    {
        StaThread.Run(() =>
        {
            using var button = new ThemedButton { Text = "Save", Primary = true };
            var clicks = 0;
            button.Click += (_, _) => clicks++;

            button.PerformClick();

            Assert.Equal(1, clicks);
        });
    }

    [Fact]
    public void A_checkbox_paints_an_outlined_box_unchecked_and_an_accent_box_checked()
    {
        StaThread.Run(() =>
        {
            using var box = new ThemedCheckBox { Text = "", AutoSize = false, Size = new Size(30, 20) };
            var square = box.BoxBounds;

            using (var bmp = Render(box))
            {
                AssertPixel(bmp, square.Left, square.Top, Theme.InputBorder);
                AssertPixel(bmp, square.Left + 3, square.Top + 3, Theme.Surface);
                AssertPixel(bmp, square.Right + 4, square.Top + 3, Theme.Bg);
            }

            box.Checked = true;
            using (var bmp = Render(box))
            {
                AssertPixel(bmp, square.Left + 1, square.Top + 1, Theme.Accent);
                AssertPixel(bmp, square.Right + 4, square.Top + 3, Theme.Bg);
            }
        });
    }

    [Fact]
    public void A_checkbox_paints_its_text_in_Text_enabled_and_Muted_disabled()
    {
        StaThread.Run(() =>
        {
            using var box = new ThemedCheckBox { Text = "Hard shutdown" };
            box.Size = box.GetPreferredSize(Size.Empty);
            var textArea = new Rectangle(box.BoxBounds.Right + 1, 0, box.Width - box.BoxBounds.Right - 1, box.Height);

            using (var bmp = Render(box))
            {
                Assert.True(AnyPixel(bmp, textArea, Theme.Text), "Enabled text is not Text.");
                Assert.False(AnyPixel(bmp, textArea, Theme.Muted), "Enabled text drew Muted.");
            }

            box.Enabled = false;
            using (var bmp = Render(box))
            {
                Assert.True(AnyPixel(bmp, textArea, Theme.Muted), "Disabled text is not Muted.");
                Assert.False(AnyPixel(bmp, textArea, Theme.Text), "Disabled text drew Text.");
            }
        });
    }

    [Fact]
    public void A_checkbox_preferred_size_fits_the_box_gap_and_text()
    {
        StaThread.Run(() =>
        {
            using var box = new ThemedCheckBox { Text = "Flush DNS" };
            var scale = box.DeviceDpi / 96f;
            var text = TextRenderer.MeasureText("Flush DNS", box.Font, Size.Empty, TextFormatFlags.SingleLine);

            var preferred = box.GetPreferredSize(Size.Empty);

            Assert.Equal(ThemePainter.Px(16, scale) + ThemePainter.Px(8, scale) + text.Width + 1, preferred.Width);
            Assert.True(preferred.Height >= ThemePainter.Px(16, scale) + ThemePainter.Px(4, scale));
            Assert.True(preferred.Height >= text.Height);
            Assert.True(box.AutoSize);

            // The mnemonic marker takes no width.
            using var marked = new ThemedCheckBox { Text = "&Flush DNS" };
            Assert.Equal(preferred, marked.GetPreferredSize(Size.Empty));
        });
    }

    [Fact]
    public void A_checkbox_click_still_toggles_Checked_and_raises_CheckedChanged()
    {
        StaThread.Run(() =>
        {
            using var box = new ThemedCheckBox { Text = "Flush DNS" };
            var changes = 0;
            box.CheckedChanged += (_, _) => changes++;

            Invoke(box, "OnClick", EventArgs.Empty);
            Assert.True(box.Checked);
            Invoke(box, "OnClick", EventArgs.Empty);
            Assert.False(box.Checked);

            Assert.Equal(2, changes);
        });
    }

    [Fact]
    public void A_list_box_is_owner_drawn_borderless_on_the_surface_with_theme_row_height()
    {
        StaThread.Run(() =>
        {
            using var list = new ThemedListBox();
            var scale = list.DeviceDpi / 96f;

            Assert.Equal(DrawMode.OwnerDrawFixed, list.DrawMode);
            Assert.Equal(BorderStyle.None, list.BorderStyle);
            Assert.False(list.IntegralHeight);
            Assert.Equal(Theme.Surface, list.BackColor);
            Assert.Equal(Theme.Text, list.ForeColor);
            Assert.Equal(Theme.UiFont.Name, list.Font.Name);
            Assert.Equal(ThemePainter.Px(Theme.RowHeight, scale), list.ItemHeight);
            Assert.Null(list.PipColor);
            Assert.Equal("", list.EmptyText);
        });
    }

    [Fact]
    public void An_unselected_row_paints_the_surface_and_its_text_in_Text()
    {
        StaThread.Run(() =>
        {
            using var list = new ThemedListBox();
            list.Items.Add("ollamaServer");
            using var bmp = PaintRow(list, 0, selected: false);

            AssertPixel(bmp, 1, 1, Theme.Surface);
            AssertPixel(bmp, 4, 15, Theme.Surface);
            Assert.True(AnyPixel(bmp, new Rectangle(11, 0, 160, 30), Theme.Text), "Row text is not Text.");
            Assert.False(AnyPixel(bmp, new Rectangle(0, 0, 11, 30), Theme.Text), "Row text starts before the text column.");
        });
    }

    [Fact]
    public void A_selected_row_paints_the_selected_fill_and_a_centred_accent_bar_inside_its_inset()
    {
        StaThread.Run(() =>
        {
            using var list = new ThemedListBox();
            list.Items.Add("AI");
            using var bmp = PaintRow(list, 0, selected: true);

            AssertPixel(bmp, 2, 15, Theme.Surface);
            AssertPixel(bmp, 3, 15, Theme.Accent);
            AssertPixel(bmp, 4, 15, Theme.Accent);
            // A 30-px row gets a 15-px bar from (30 - 15) / 2 = 7: rows 7 to 21.
            AssertPixel(bmp, 3, 6, Theme.SelectedFill);
            AssertPixel(bmp, 3, 7, Theme.Accent);
            AssertPixel(bmp, 3, 21, Theme.Accent);
            AssertPixel(bmp, 3, 22, Theme.SelectedFill);
            AssertPixel(bmp, 5, 15, Theme.SelectedFill);
            AssertPixel(bmp, 196, 15, Theme.SelectedFill);
            AssertPixel(bmp, 197, 15, Theme.Surface);
        });
    }

    [Fact]
    public void A_row_pip_uses_the_row_colour_or_an_empty_ring_and_no_pip_column_moves_the_text_left()
    {
        StaThread.Run(() =>
        {
            var blue = Color.FromArgb(0x2E, 0x6B, 0xFF);
            using var list = new ThemedListBox { PipColor = i => i == 0 ? blue : null };
            list.Items.Add("AI");
            list.Items.Add("Game");

            using (var bmp = PaintRow(list, 0, selected: false))
            {
                AssertPixel(bmp, 13, 15, blue);
                Assert.False(AnyPixel(bmp, new Rectangle(0, 0, 29, 30), Theme.Text), "Text overlaps the pip column.");
                Assert.True(AnyPixel(bmp, new Rectangle(29, 0, 160, 30), Theme.Text), "Text is not after the pip column.");
            }

            using (var bmp = PaintRow(list, 1, selected: false))
            {
                AssertPixel(bmp, 13, 15, Theme.Surface);
                Assert.True(AnyPixel(bmp, new Rectangle(8, 10, 11, 11), c => c != Theme.Surface.ToArgb()), "The empty ring was not drawn.");
                Assert.False(AnyPixel(bmp, new Rectangle(8, 10, 11, 11), blue), "The empty ring drew the colour.");
            }

            list.PipColor = null;
            using (var bmp = PaintRow(list, 0, selected: false))
            {
                Assert.False(AnyPixel(bmp, new Rectangle(8, 10, 11, 11), blue), "A pip was drawn with no pip source.");
                Assert.True(AnyPixel(bmp, new Rectangle(11, 0, 18, 30), Theme.Text), "Without pips the text does not start at the left column.");
            }
        });
    }

    [Fact]
    public void A_disabled_row_paints_its_text_in_Muted()
    {
        StaThread.Run(() =>
        {
            using var list = new ThemedListBox { Enabled = false };
            list.Items.Add("ollamaServer");
            using var bmp = PaintRow(list, 0, selected: false);

            Assert.True(AnyPixel(bmp, new Rectangle(11, 0, 160, 30), Theme.Muted), "Disabled row text is not Muted.");
            Assert.False(AnyPixel(bmp, new Rectangle(11, 0, 160, 30), Theme.Text), "Disabled row text drew Text.");
        });
    }

    [Fact]
    public void An_empty_list_paints_its_empty_text_centred_in_Muted_and_nothing_when_blank()
    {
        StaThread.Run(() =>
        {
            using var list = new ThemedListBox { EmptyText = "None" };
            var client = new Rectangle(0, 0, 170, 160);

            using (var bmp = Canvas(170, 160, Theme.Bg))
            {
                using (var g = Graphics.FromImage(bmp))
                    list.PaintEmpty(g, client);
                AssertPixel(bmp, 2, 2, Theme.Surface);
                Assert.True(AnyPixel(bmp, new Rectangle(60, 68, 50, 24), Theme.Muted), "Empty text is not centred in Muted.");
                Assert.False(AnyPixel(bmp, new Rectangle(0, 0, 170, 60), Theme.Muted), "Empty text is not vertically centred.");
                Assert.False(AnyPixel(bmp, new Rectangle(0, 0, 50, 160), Theme.Muted), "Empty text is not horizontally centred.");
            }

            list.EmptyText = "";
            using (var bmp = Canvas(170, 160, Theme.Bg))
            {
                using (var g = Graphics.FromImage(bmp))
                    list.PaintEmpty(g, client);
                Assert.False(AnyPixel(bmp, client, c => c != Theme.Surface.ToArgb()), "A blank empty text drew something.");
            }
        });
    }

    [Fact]
    public void Painting_a_row_outside_the_items_draws_nothing()
    {
        StaThread.Run(() =>
        {
            using var list = new ThemedListBox();
            list.Items.Add("AI");

            foreach (var index in new[] { -1, 1, 5 })
            {
                using var bmp = PaintRow(list, index, selected: true);
                Assert.False(AnyPixel(bmp, new Rectangle(0, 0, 200, 30), c => c != Theme.Bg.ToArgb()), $"Index {index} drew something.");
            }
        });
    }

    [Fact]
    public void A_frame_paints_a_border_around_the_surface_and_pads_its_children()
    {
        StaThread.Run(() =>
        {
            using var frame = new ThemedFrame { Size = new Size(60, 40) };
            var scale = frame.DeviceDpi / 96f;
            using var bmp = Render(frame);

            AssertPixel(bmp, 0, 0, Theme.Border);
            AssertPixel(bmp, 59, 39, Theme.Border);
            AssertPixel(bmp, 30, 0, Theme.Border);
            AssertPixel(bmp, 1, 1, Theme.Surface);
            AssertPixel(bmp, 30, 20, Theme.Surface);
            Assert.Equal(new Padding(ThemePainter.Px(4, scale)), frame.Padding);
        });
    }

    [Fact]
    public void A_text_field_hosts_a_borderless_surface_text_box_at_field_height()
    {
        StaThread.Run(() =>
        {
            using var field = new ThemedTextBox();
            var scale = field.DeviceDpi / 96f;

            Assert.Same(field.Inner, field.Controls.OfType<TextBox>().Single());
            Assert.Equal(BorderStyle.None, field.Inner.BorderStyle);
            Assert.Equal(Theme.Surface, field.Inner.BackColor);
            Assert.Equal(Theme.Text, field.Inner.ForeColor);
            Assert.Equal(Theme.UiFont.Name, field.Inner.Font.Name);
            Assert.Equal(ThemePainter.Px(Theme.FieldHeight, scale), field.Height);
            Assert.Equal(240, field.Width);
        });
    }

    [Fact]
    public void A_text_field_forwards_its_text_both_ways_and_raises_TextChanged()
    {
        StaThread.Run(() =>
        {
            using var field = new ThemedTextBox();
            var changes = 0;
            field.TextChanged += (_, _) => changes++;

            field.Text = "AI";
            Assert.Equal("AI", field.Inner.Text);

            field.Inner.Text = "Dev";
            Assert.Equal("Dev", field.Text);

            field.Text = null;
            Assert.Equal("", field.Inner.Text);

            Assert.Equal(3, changes);
        });
    }

    [Fact]
    public void A_mono_text_field_uses_Consolas()
    {
        StaThread.Run(() =>
        {
            using var field = new ThemedTextBox { Mono = true };
            Assert.Equal(Theme.MonoFont.Name, field.Inner.Font.Name);

            field.Mono = false;
            Assert.Equal(Theme.UiFont.Name, field.Inner.Font.Name);
        });
    }

    [Fact]
    public void A_text_field_lays_its_text_box_out_inside_the_padding()
    {
        StaThread.Run(() =>
        {
            using var field = new ThemedTextBox { Width = 200 };
            var scale = field.DeviceDpi / 96f;
            var inset = ThemePainter.Px(8, scale);
            field.PerformLayout();

            Assert.Equal(inset, field.Inner.Left);
            Assert.Equal(field.ClientSize.Width - 2 * inset, field.Inner.Width);
            Assert.True(field.Inner.Top >= 0 && field.Inner.Bottom <= field.ClientSize.Height,
                $"Text box rows {field.Inner.Top}-{field.Inner.Bottom} overflow the field height {field.ClientSize.Height}.");
            Assert.InRange(field.Inner.Top - (field.ClientSize.Height - field.Inner.Bottom), -1, 1);
        });
    }

    [Fact]
    public void A_text_field_disabling_disables_its_inner_text_box()
    {
        StaThread.Run(() =>
        {
            using var field = new ThemedTextBox();
            Assert.True(field.Inner.Enabled);
            field.Enabled = false;
            Assert.False(field.Inner.Enabled);
            field.Enabled = true;
            Assert.True(field.Inner.Enabled);
        });
    }

    [Fact]
    public void A_text_field_paints_a_border_frame_with_an_input_border_bottom_and_a_border_bottom_disabled()
    {
        StaThread.Run(() =>
        {
            using var field = new ThemedTextBox { Width = 120 };
            using (var bmp = RenderPaint(field))
            {
                AssertPixel(bmp, 0, 0, Theme.Border);
                AssertPixel(bmp, 3, 3, Theme.Surface);
                AssertPixel(bmp, 60, field.Height - 1, Theme.InputBorder);
            }

            field.Enabled = false;
            using (var bmp = RenderPaint(field))
            {
                AssertPixel(bmp, 60, field.Height - 1, Theme.Border);
            }
        });
    }

    [Fact]
    public void A_plus_button_keeps_its_text_and_is_wider_than_the_same_button_without_the_plus()
    {
        StaThread.Run(() =>
        {
            using var plain = new ThemedButton { Text = "Add a long label" };
            using var plus = new ThemedButton { Text = "Add a long label", ShowPlus = true };

            Assert.Equal("Add a long label", plus.Text);
            Assert.True(plus.GetPreferredSize(Size.Empty).Width > plain.GetPreferredSize(Size.Empty).Width);
        });
    }

    [Fact]
    public void A_row_with_the_remove_cross_paints_its_own_background_and_keeps_text_out_of_the_cross()
    {
        StaThread.Run(() =>
        {
            using var list = new ThemedListBox { ShowRemoveGlyph = true, BackColor = Theme.Card };
            list.Items.Add("a very long reference key that would run into the cross");
            using var bmp = PaintRow(list, 0, selected: false);
            var glyph = list.RemoveGlyphBounds(new Rectangle(0, 0, 200, 30));

            AssertPixel(bmp, 4, 15, Theme.Card);
            Assert.True(glyph.Right <= 200 && glyph.Left > 100, $"The cross sits at {glyph}.");
            Assert.True(AnyPixel(bmp, glyph, p => Near(Color.FromArgb(p), Theme.Border)), "The resting cross is not drawn in Border.");
            Assert.False(AnyPixel(bmp, glyph, Theme.Text), "Row text ran into the cross.");
        });
    }

    [Fact]
    public void A_list_has_no_native_scroll_bar_and_shows_a_thin_thumb_only_when_rows_overflow()
    {
        StaThread.Run(() =>
        {
            using var list = new ThemedListBox { Size = new Size(160, 100) };
            for (var i = 0; i < 3; i++)
                list.Items.Add($"row {i}");
            _ = list.Handle;

            Assert.Equal(0, GetWindowLong(list.Handle, GWL_STYLE) & WS_VSCROLL);
            Assert.False(list.ShowsScrollThumb);
            Assert.Equal(Rectangle.Empty, list.ScrollThumbBounds);

            for (var i = 3; i < 20; i++)
                list.Items.Add($"row {i}");

            Assert.True(list.ShowsScrollThumb);
            var thumb = list.ScrollThumbBounds;
            Assert.True(thumb.Width <= 6, $"The thumb is {thumb.Width}px wide.");
            Assert.True(thumb.Right <= list.ClientSize.Width && thumb.Right >= list.ClientSize.Width - 4, $"The thumb sits at {thumb}.");
            Assert.True(thumb.Top <= 4, $"The thumb starts at {thumb.Top} with the list at the top.");
            Assert.True(thumb.Height < list.ClientSize.Height / 2, $"The thumb is {thumb.Height}px for {list.VisibleRowCount} of 20 rows.");
        });
    }

    [Fact]
    public void Dragging_the_thumb_and_the_wheel_scroll_the_list_and_the_thumb_follows()
    {
        StaThread.Run(() =>
        {
            using var list = new ThemedListBox { Size = new Size(160, 100) };
            for (var i = 0; i < 20; i++)
                list.Items.Add($"row {i}");
            _ = list.Handle;
            var maxTop = list.Items.Count - list.VisibleRowCount;

            list.DragThumbTo(list.ClientSize.Height);
            Assert.Equal(maxTop, list.TopIndex);
            var bottom = list.ScrollThumbBounds;
            Assert.True(bottom.Bottom >= list.ClientSize.Height - 4, $"The thumb ends at {bottom.Bottom} with the list at the bottom.");

            list.DragThumbTo(0);
            Assert.Equal(0, list.TopIndex);

            SendMessage(list.Handle, WM_MOUSEWHEEL, (IntPtr)(-120 << 16), IntPtr.Zero);
            Assert.True(list.TopIndex > 0, "The wheel no longer scrolls the list.");
            Assert.True(list.ScrollThumbBounds.Top > 4, "The thumb did not follow the wheel.");

            list.TopIndex = 0;
            list.SelectedIndex = list.Items.Count - 1;
            Assert.Equal(maxTop, list.TopIndex);
        });
    }

    [Fact]
    public void The_thumb_paints_as_a_capsule_in_the_input_border_colour_and_keeps_row_text_clear()
    {
        StaThread.Run(() =>
        {
            using var list = new ThemedListBox { Size = new Size(160, 100), BackColor = Theme.Card, ShowRemoveGlyph = true };
            for (var i = 0; i < 20; i++)
                list.Items.Add(new string('W', 40));
            _ = list.Handle;

            var thumb = list.ScrollThumbBounds;
            using var bmp = Canvas(160, 100, Theme.Card);
            using (var g = Graphics.FromImage(bmp))
                list.PaintScrollThumb(g);
            AssertPixel(bmp, thumb.Left + thumb.Width / 2, thumb.Top + thumb.Height / 2, Theme.InputBorder);

            var row = list.GetItemRectangle(0);
            Assert.True(list.RemoveGlyphBounds(row).Right < thumb.Left, "The remove cross runs under the thumb.");
            using var rowBmp = Canvas(160, row.Height, Theme.Bg);
            using (var g = Graphics.FromImage(rowBmp))
                list.PaintRow(g, new Rectangle(0, 0, row.Width, row.Height), 0, false);
            Assert.False(AnyPixel(rowBmp, new Rectangle(thumb.Left - 1, 0, 160 - thumb.Left + 1, row.Height), Theme.Text), "Row text runs under the thumb.");
        });
    }

    [Fact]
    public void A_text_view_has_no_native_scroll_bar_and_its_thumb_wheel_and_drag_scroll_the_lines()
    {
        StaThread.Run(() =>
        {
            using var view = new ThemedTextView { Size = new Size(240, 60) };
            view.Text = string.Join(Environment.NewLine, Enumerable.Range(1, 20).Select(i => $"Warning {i}"));
            _ = view.Handle;
            _ = view.Inner.Handle;

            Assert.Equal(0, GetWindowLong(view.Inner.Handle, GWL_STYLE) & WS_VSCROLL);
            Assert.Equal(20, view.LineCount);
            Assert.True(view.ShowsScrollThumb);
            var thumb = view.ScrollThumbBounds;
            Assert.True(thumb.Width <= 6 && thumb.Right >= view.ClientSize.Width - 4, $"The thumb sits at {thumb}.");
            Assert.True(view.Inner.Right <= thumb.Left, "The text runs under the thumb.");

            var maxFirst = view.LineCount - view.VisibleLineCount;
            view.DragThumbTo(view.ClientSize.Height);
            Assert.Equal(maxFirst, view.FirstVisibleLine);

            view.ScrollToLine(0);
            Assert.Equal(0, view.FirstVisibleLine);
            SendMessage(view.Inner.Handle, WM_MOUSEWHEEL, (IntPtr)(-120 << 16), IntPtr.Zero);
            Assert.True(view.FirstVisibleLine > 0, "The wheel does not scroll the text.");
            Assert.True(view.ScrollThumbBounds.Top > 4, "The thumb did not follow the wheel.");
        });
    }

    [Fact]
    public void A_text_view_whose_text_fits_shows_no_thumb()
    {
        StaThread.Run(() =>
        {
            using var view = new ThemedTextView { Size = new Size(240, 60), Text = "One warning." };
            _ = view.Handle;
            _ = view.Inner.Handle;

            Assert.False(view.ShowsScrollThumb);
            Assert.Equal(Rectangle.Empty, view.ScrollThumbBounds);
        });
    }

    private const int GWL_STYLE = -16;
    private const int WS_VSCROLL = 0x00200000;
    private const int WM_MOUSEWHEEL = 0x020A;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private static bool Near(Color actual, Color expected) =>
        Math.Abs(actual.R - expected.R) <= 8 && Math.Abs(actual.G - expected.G) <= 8 && Math.Abs(actual.B - expected.B) <= 8;

    private static Bitmap PaintRow(ThemedListBox list, int index, bool selected)
    {
        var bmp = Canvas(200, 30, Theme.Bg);
        using var g = Graphics.FromImage(bmp);
        list.PaintRow(g, new Rectangle(0, 0, 200, 30), index, selected);
        return bmp;
    }

    // Calls the control's own OnPaint into a bitmap, so child windows are left out.
    private static Bitmap RenderPaint(Control control)
    {
        var bmp = Canvas(control.Width, control.Height, Theme.Bg);
        using var g = Graphics.FromImage(bmp);
        using var args = new PaintEventArgs(g, new Rectangle(Point.Empty, control.Size));
        var info = control.GetType().GetMethod("OnPaint", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(PaintEventArgs) }, null)!;
        info.Invoke(control, new object[] { args });
        return bmp;
    }

    private static Bitmap Canvas(int width, int height, Color background)
    {
        var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.Clear(background);
        return bmp;
    }

    private static bool AnyPixel(Bitmap bmp, Rectangle area, Func<int, bool> match)
    {
        for (var x = Math.Max(0, area.Left); x < Math.Min(bmp.Width, area.Right); x++)
            for (var y = Math.Max(0, area.Top); y < Math.Min(bmp.Height, area.Bottom); y++)
                if (match(bmp.GetPixel(x, y).ToArgb()))
                    return true;
        return false;
    }

    private static Bitmap Render(Control control)
    {
        var bmp = new Bitmap(control.Width, control.Height, PixelFormat.Format32bppArgb);
        control.DrawToBitmap(bmp, new Rectangle(Point.Empty, control.Size));
        return bmp;
    }

    private static void Invoke(Control control, string method, EventArgs args)
    {
        var info = control.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { args.GetType() }, null)
                   ?? throw new MissingMethodException(control.GetType().Name, method);
        info.Invoke(control, new object[] { args });
    }

    private static void AssertPixel(Bitmap bmp, int x, int y, Color expected)
    {
        var actual = bmp.GetPixel(x, y);
        Assert.True(actual.ToArgb() == expected.ToArgb(),
            $"Pixel ({x},{y}) is #{actual.R:X2}{actual.G:X2}{actual.B:X2}, expected #{expected.R:X2}{expected.G:X2}{expected.B:X2}.");
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
