using Rigshift.Config;
using Rigshift.Settings;

namespace Rigshift.Tests;

/// <summary>
/// Regression facts for the settings window at its default size: every button has a
/// label, no input is clipped on the right by a parent that cannot scroll, and the
/// fields at the bottom of each tab sit inside a panel that scrolls.
/// </summary>
public class SettingsLayoutTests
{
    [Fact]
    public void Every_button_in_the_settings_window_has_a_label()
    {
        StaThread.Run(() =>
        {
            using var form = BuildForm();

            var buttons = Descendants(form).OfType<Button>().ToList();

            Assert.NotEmpty(buttons);
            foreach (var button in buttons)
                Assert.False(string.IsNullOrWhiteSpace(button.Text), $"A button under {Describe(button.Parent)} has no label.");
        });
    }

    [Fact]
    public void No_button_or_input_is_clipped_on_the_right_by_a_parent_that_cannot_scroll()
    {
        StaThread.Run(() =>
        {
            using var form = BuildForm();

            var checkedCount = 0;
            foreach (var control in Descendants(form))
            {
                if (control is not (Button or ComboBox or TextBox or ColorField))
                    continue;
                var parent = control.Parent;
                if (parent is null)
                    continue;
                if (parent is ScrollableControl { AutoScroll: true })
                    continue;

                checkedCount++;
                Assert.True(
                    control.Right <= parent.ClientSize.Width,
                    $"{Describe(control)} ends at x={control.Right} but its parent {Describe(parent)} is {parent.ClientSize.Width} wide.");
            }

            Assert.True(checkedCount > 0);
        });
    }

    [Fact]
    public void The_bottom_loadout_fields_are_inside_a_scrolling_panel()
    {
        StaThread.Run(() =>
        {
            using var form = BuildForm();
            var editor = form.LoadoutEditor;

            foreach (var text in new[] { "Hard shutdown", "Flush DNS" })
            {
                var box = Descendants(editor).OfType<CheckBox>().Single(c => c.Text == text);
                Assert.True(HasScrollingAncestorBelow(box, editor), $"'{text}' has no auto-scrolling ancestor inside LoadoutEditor.");
            }
        });
    }

    [Fact]
    public void The_bottom_definition_fields_are_inside_a_scrolling_panel()
    {
        StaThread.Run(() =>
        {
            using var form = BuildForm();
            var editor = form.DefinitionEditor;

            var portProxy = Descendants(editor).OfType<CheckBox>().Single(c => c.Text == "Port proxy");
            Assert.True(HasScrollingAncestorBelow(portProxy, editor), "'Port proxy' has no auto-scrolling ancestor inside DefinitionEditor.");
            Assert.True(HasScrollingAncestorBelow(editor.RequiresList, editor), "RequiresList has no auto-scrolling ancestor inside DefinitionEditor.");
        });
    }

    [Fact]
    public void The_three_reference_lists_fit_within_the_field_panel_at_the_default_size()
    {
        StaThread.Run(() =>
        {
            using var form = BuildForm();
            var strip = Descendants(form).OfType<TabStrip>().Single();
            strip.SelectedIndex = 0;   // Loadouts
            form.LoadoutEditor.Select(0);
            form.PerformLayout();

            var row = form.LoadoutEditor.StartList.Parent as FlowLayoutPanel
                      ?? throw new Xunit.Sdk.XunitException("StartList has no horizontal reference row parent.");
            var fieldPanel = row.Parent as FlowLayoutPanel;
            Assert.NotNull(fieldPanel);

            // The side-by-side lists must fit the field panel's client width at the
            // approved 780x680 default: no horizontal overflow (which would show a
            // scrollbar or clip the third list).
            Assert.True(row.Right <= fieldPanel!.ClientSize.Width,
                $"The three reference lists span {row.Right}px but the field panel is only {fieldPanel.ClientSize.Width}px wide.");
        });
    }

    [Fact]
    public void Add_Duplicate_and_Delete_fit_inside_the_list_column_on_both_tabs()
    {
        StaThread.Run(() =>
        {
            using var form = BuildForm();

            foreach (Control editor in new Control[] { form.LoadoutEditor, form.DefinitionEditor })
            {
                var listPanel = editor.Controls.OfType<Panel>().Single(p => p.Dock == DockStyle.Left);
                listPanel.PerformLayout();
                var buttons = Descendants(listPanel).OfType<ThemedButton>().ToList();
                Assert.Equal(new[] { "Add", "Duplicate", "Delete" }, buttons.Select(b => b.Text));

                foreach (var button in buttons)
                {
                    var right = RightEdgeIn(listPanel, button);
                    Assert.True(button.Width > 0, $"'{button.Text}' has no width.");
                    Assert.True(right <= listPanel.ClientSize.Width,
                        $"'{button.Text}' ends at x={right} but the list column is {listPanel.ClientSize.Width} wide.");
                    var text = TextRenderer.MeasureText(button.Text, button.Font).Width;
                    Assert.True(button.Width >= text, $"'{button.Text}' is {button.Width} wide, narrower than its {text}px label.");
                }
            }
        });
    }

    [Fact]
    public void The_top_row_centres_the_save_path_and_the_idle_colour_on_one_line()
    {
        StaThread.Run(() =>
        {
            using var form = BuildForm();
            var top = form.Controls.OfType<TableLayoutPanel>().Single(p => p.Dock == DockStyle.Top);
            top.PerformLayout();

            var labels = top.Controls.OfType<Label>().ToList();
            Assert.Equal(3, labels.Count);
            var field = top.Controls.OfType<ColorField>().Single();
            var centre = field.Top + field.Height / 2;
            foreach (var label in labels)
            {
                var labelCentre = label.Top + label.Height / 2;
                Assert.InRange(labelCentre, centre - 1, centre + 1);
            }

            // The idle colour sits at the right edge, not straight after the path.
            Assert.True(field.Right >= top.ClientSize.Width - top.Padding.Right - 1,
                $"The idle colour ends at {field.Right}; the band is {top.ClientSize.Width} wide.");
        });
    }

    // Builds the window from the built-in default config at its default size, selects
    // the first loadout and the first definition, and lays both tabs out. The window is
    // never shown, so the tab that is not selected is sized explicitly to the selected
    // tab page's client area.
    private static SettingsForm BuildForm()
    {
        var form = new SettingsForm(
            SettingsMapper.FromConfig(ConfigLoader.Default()),
            new List<string>(),
            @"C:\x\rigshift.json");
        form.PerformLayout();

        // The host is the fill-docked panel holding the active editor. A plain panel
        // fills with its real size without a window handle, so no manual page sizing is
        // needed (unlike the old TabControl, whose pages default to 200x100 pre-handle).
        var host = form.Controls.OfType<Panel>().Single(p => p.Dock == DockStyle.Fill);
        Assert.True(host.ClientSize.Width >= 700, $"The tab area is only {host.ClientSize.Width} wide; the form did not lay out at its default size.");
        host.PerformLayout();

        Assert.Same(form.LoadoutEditor, host.Controls[0]);
        Assert.Equal(host.ClientSize, form.LoadoutEditor.Size);

        var strip = Descendants(form).OfType<TabStrip>().Single();
        strip.SelectedIndex = 1;
        host.PerformLayout();
        Assert.Same(form.DefinitionEditor, host.Controls[0]);
        Assert.Equal(host.ClientSize, form.DefinitionEditor.Size);

        return form;
    }

    private static bool HasScrollingAncestorBelow(Control control, Control root)
    {
        for (var current = control.Parent; current is not null && current != root; current = current.Parent)
        {
            if (current is ScrollableControl { AutoScroll: true })
                return true;
        }
        return false;
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

    private static int RightEdgeIn(Control root, Control control)
    {
        var left = 0;
        for (var current = control; current is not null && current != root; current = current.Parent)
            left += current.Left;
        return left + control.Width;
    }

    private static string Describe(Control? control)
    {
        if (control is null)
            return "(none)";
        var text = string.IsNullOrEmpty(control.Text) ? "" : $" '{control.Text}'";
        return $"{control.GetType().Name}{text}";
    }
}
