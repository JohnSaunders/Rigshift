using Rigshift.Settings;

namespace Rigshift.Tests;

public class LoadoutEditorTests
{
    [Fact]
    public void ShowsTheLoadoutNamesAndSelectFillsTheNameField()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft
            {
                Loadouts = new List<LoadoutDraft>
                {
                    new() { Name = "AI" },
                    new() { Name = "Dev" }
                }
            };
            var editor = new LoadoutEditor(draft);
            editor.ConfirmDelete = false;

            Assert.Equal(new[] { "AI", "Dev" }, editor.LoadoutNames);

            editor.Select(1);

            Assert.Equal(1, editor.SelectedIndex);
            Assert.Equal("Dev", editor.NameText);
        });
    }

    [Fact]
    public void AddAppendsAndSelectsTheNewToggleLoadout()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft
            {
                Loadouts = new List<LoadoutDraft> { new() { Name = "AI" } }
            };
            var editor = new LoadoutEditor(draft);
            editor.ConfirmDelete = false;

            editor.Add();

            Assert.Equal(2, draft.Loadouts.Count);
            Assert.Equal("New loadout", draft.Loadouts[1].Name);
            Assert.True(draft.Loadouts[1].Toggle);
            Assert.Equal("#8A8A8E", draft.Loadouts[1].Color);
            Assert.Equal(new[] { "AI", "New loadout" }, editor.LoadoutNames);
            Assert.Equal(1, editor.SelectedIndex);
            Assert.True(editor.ToggleChecked);
        });
    }

    [Fact]
    public void DuplicateInsertsASelectCopyAfterTheOriginalWithItsOwnStartList()
    {
        StaThread.Run(() =>
        {
            var ai = new LoadoutDraft { Name = "AI", Start = new List<string> { "wsl" } };
            var draft = new SettingsDraft
            {
                Definitions = new List<DefinitionDraft>
                {
                    new() { Key = "wsl" },
                    new() { Key = "node" }
                },
                Loadouts = new List<LoadoutDraft> { ai }
            };
            var editor = new LoadoutEditor(draft);
            editor.ConfirmDelete = false;

            editor.Select(0);
            editor.Duplicate();

            Assert.Equal(2, draft.Loadouts.Count);
            Assert.Equal("AI copy", draft.Loadouts[1].Name);
            Assert.Equal(new[] { "AI", "AI copy" }, editor.LoadoutNames);
            Assert.Equal(1, editor.SelectedIndex);

            editor.StartList.AddKey("node");

            Assert.Equal(new[] { "wsl" }, ai.Start);
            Assert.Equal(new[] { "wsl", "node" }, draft.Loadouts[1].Start);
        });
    }

    [Fact]
    public void DeleteRemovesOnlyTheSelectedRowAndNameTextUpdatesTheDraft()
    {
        StaThread.Run(() =>
        {
            var ai = new LoadoutDraft { Name = "AI" };
            var dev = new LoadoutDraft { Name = "Dev" };
            var draft = new SettingsDraft { Loadouts = new List<LoadoutDraft> { ai, dev } };
            var editor = new LoadoutEditor(draft);
            editor.ConfirmDelete = false;

            editor.Select(0);
            editor.Delete();

            Assert.Single(draft.Loadouts);
            Assert.Same(dev, draft.Loadouts[0]);
            Assert.Equal(new[] { "Dev" }, editor.LoadoutNames);

            editor.NameText = "AI 2";

            Assert.Equal("AI 2", dev.Name);
            Assert.Equal(new[] { "AI 2" }, editor.LoadoutNames);
        });
    }

    [Fact]
    public void ABlankNameShowsAsUnnamedUntilItIsSet()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft
            {
                Loadouts = new List<LoadoutDraft> { new() { Name = "   " } }
            };
            var editor = new LoadoutEditor(draft);
            editor.ConfirmDelete = false;

            Assert.Equal(new[] { "(unnamed)" }, editor.LoadoutNames);

            editor.Select(0);
            editor.NameText = "Fixed";

            Assert.Equal("Fixed", draft.Loadouts[0].Name);
            Assert.Equal(new[] { "Fixed" }, editor.LoadoutNames);
        });
    }

    [Fact]
    public void DuplicateAndDeleteDoNothingWhenNothingIsSelected()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft
            {
                Loadouts = new List<LoadoutDraft> { new() { Name = "AI" } }
            };
            var editor = new LoadoutEditor(draft);
            editor.ConfirmDelete = false;

            editor.Duplicate();
            editor.Delete();

            Assert.Single(draft.Loadouts);
            Assert.Equal("AI", draft.Loadouts[0].Name);
            Assert.Equal(new[] { "AI" }, editor.LoadoutNames);
        });
    }

    [Fact]
    public void TheFieldPanelScrollsWhenTheTabIsShorterThanTheFields()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft
            {
                Loadouts = new List<LoadoutDraft> { new() { Name = "AI" } }
            };
            var editor = new LoadoutEditor(draft);
            editor.Size = new Size(560, 300);
            editor.Select(0);
            editor.PerformLayout();

            var flushBox = FindBox(editor, "Flush DNS");
            var scrollable = ScrollingAncestor(flushBox, editor);
            Assert.NotNull(scrollable);
            Assert.True(scrollable!.AutoScroll);
            Assert.False(scrollable.AutoSize);
            Assert.True(scrollable.Bottom <= editor.ClientSize.Height);
        });
    }

    [Fact]
    public void DeleteAndAddKeepTheFieldPanelScrolling()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft
            {
                Loadouts = new List<LoadoutDraft> { new() { Name = "AI" } }
            };
            var editor = new LoadoutEditor(draft);
            editor.ConfirmDelete = false;

            editor.Select(0);
            editor.Delete();

            Assert.Empty(draft.Loadouts);
            editor.Add();

            var flushBox = FindBox(editor, "Flush DNS");
            var scrollable = ScrollingAncestor(flushBox, editor);
            Assert.NotNull(scrollable);
            Assert.True(scrollable!.AutoScroll);
        });
    }

    [Fact]
    public void SelectFillsTheColorFieldAndTheSwatchShowsTheRowColor()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft
            {
                Loadouts = new List<LoadoutDraft>
                {
                    new() { Name = "AI", Color = "#2E6BFF" },
                    new() { Name = "Dev" }
                }
            };
            var editor = new LoadoutEditor(draft);
            editor.ConfirmDelete = false;

            editor.Select(0);

            Assert.Equal("#2E6BFF", editor.ColorText);
            var swatch = editor.ColorField.SwatchColor;
            Assert.Equal(0x2E, swatch.R);
            Assert.Equal(0x6B, swatch.G);
            Assert.Equal(0xFF, swatch.B);
        });
    }

    [Fact]
    public void PickWritesThePickedColorToTheSelectedDraftRow()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft
            {
                Loadouts = new List<LoadoutDraft>
                {
                    new() { Name = "AI", Color = "#2E6BFF" },
                    new() { Name = "Dev", Color = "#8A8A8E" }
                }
            };
            var editor = new LoadoutEditor(draft);
            editor.ConfirmDelete = false;
            editor.Select(0);
            editor.ColorField.PickColor = _ => Color.FromArgb(0x11, 0x22, 0x33);

            editor.ColorField.Pick();

            Assert.Equal("#112233", draft.Loadouts[0].Color);
            Assert.Equal("#8A8A8E", draft.Loadouts[1].Color);
        });
    }

    [Fact]
    public void ColorTextWritesTheDraftRowAndSelectingAnotherRowDoesNotWriteBack()
    {
        StaThread.Run(() =>
        {
            var first = new LoadoutDraft { Name = "AI" };
            var draft = new SettingsDraft
            {
                Loadouts = new List<LoadoutDraft>
                {
                    first,
                    new() { Name = "Dev", Color = "#8A8A8E" }
                }
            };
            var editor = new LoadoutEditor(draft);
            editor.ConfirmDelete = false;

            editor.Select(0);
            editor.ColorText = "#00FF00";

            Assert.Equal("#00FF00", first.Color);

            editor.Select(1);
            editor.Select(0);

            Assert.Equal("#00FF00", first.Color);
            Assert.Equal("#8A8A8E", draft.Loadouts[1].Color);
        });
    }

    [Fact]
    public void TheEmptyHintShowsWhenNothingIsSelectedAndHidesOnSelection()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft
            {
                Loadouts = new List<LoadoutDraft> { new() { Name = "AI" } }
            };
            var editor = new LoadoutEditor(draft);

            Assert.True(editor.HintVisible);

            editor.Select(0);

            Assert.False(editor.HintVisible);

            editor.Select(-1);

            Assert.True(editor.HintVisible);
        });
    }

    [Fact]
    public void AddSelectsTheNewRowAndPutsTheCursorInItsNameBox()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft
            {
                Loadouts = new List<LoadoutDraft> { new() { Name = "AI" } }
            };
            var editor = new LoadoutEditor(draft);

            editor.Add();

            Assert.False(editor.HintVisible);
            Assert.Equal(draft.Loadouts.Count - 1, editor.SelectedIndex);
            var nameBox = Assert.IsAssignableFrom<TextBox>(editor.ActiveControl);
            Assert.Equal(editor.NameText, nameBox.Text);
        });
    }

    [Fact]
    public void DeletingEveryRowShowsTheEmptyHintAgain()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft
            {
                Loadouts = new List<LoadoutDraft>
                {
                    new() { Name = "AI" },
                    new() { Name = "Dev" }
                }
            };
            var editor = new LoadoutEditor(draft);
            editor.ConfirmDelete = false;

            editor.Select(0);
            editor.Delete();
            editor.Delete();

            Assert.Empty(draft.Loadouts);
            Assert.True(editor.HintVisible);
        });
    }

    [Fact]
    public void TypingTheNameWithHandlesKeepsTheSelectionAndStartList()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft
            {
                Loadouts = new List<LoadoutDraft> { new() { Name = "AI" } }
            };
            var editor = new LoadoutEditor(draft);
            ForceHandles(editor);
            editor.Select(0);
            ForceHandles(editor);

            var startList = editor.StartList;

            editor.NameText = "AIx";

            Assert.Equal(0, editor.SelectedIndex);
            Assert.False(editor.HintVisible);
            Assert.Same(startList, editor.StartList);
        });
    }

    [Fact]
    public void TypingTheNameInTurnKeepsWritingTheDraftAndListText()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft
            {
                Loadouts = new List<LoadoutDraft> { new() { Name = "" } }
            };
            var editor = new LoadoutEditor(draft);
            ForceHandles(editor);
            editor.Select(0);
            ForceHandles(editor);

            editor.NameText = "A";
            editor.NameText = "AI";
            editor.NameText = "AIx";

            Assert.Equal("AIx", draft.Loadouts[0].Name);
            Assert.Equal("AIx", editor.LoadoutNames[0]);
        });
    }

    [Fact]
    public void TheToggleFieldUsesSwitchWordingWithAnOffHint()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft
            {
                Loadouts = new List<LoadoutDraft> { new() { Name = "AI" } }
            };
            var editor = new LoadoutEditor(draft);

            Assert.Equal("Show as a switch in the tray", editor.ToggleText);
            Assert.Equal("Off: a button that runs once.", editor.ToggleHintText);
            Assert.DoesNotContain("one-shot", editor.ToggleText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("one-shot", editor.ToggleHintText, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void TurningTheSwitchOffWritesTheDraftRow()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft
            {
                Loadouts = new List<LoadoutDraft> { new() { Name = "AI", Toggle = true } }
            };
            var editor = new LoadoutEditor(draft);

            editor.Select(0);

            Assert.True(editor.ToggleChecked);

            editor.ToggleChecked = false;

            Assert.False(draft.Loadouts[0].Toggle);
        });
    }

    // Forces a window handle on the control and every descendant so event
    // paths that only fire with native handles are exercised.
    [Fact]
    public void The_three_reference_lists_are_siblings_in_one_horizontal_row()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft
            {
                Loadouts = new List<LoadoutDraft>
                {
                    new() { Name = "AI", Start = new List<string> { "ollama" } },
                    new() { Name = "Dev" }
                }
            };
            var editor = new LoadoutEditor(draft);
            editor.Select(0);
            editor.PerformLayout();

            Assert.NotNull(editor.StartList.Parent);
            Assert.Same(editor.StartList.Parent, editor.KillList.Parent);
            Assert.Same(editor.StartList.Parent, editor.KillOnDisableList.Parent);
            var row = Assert.IsType<FlowLayoutPanel>(editor.StartList.Parent);
            Assert.Equal(FlowDirection.LeftToRight, row.FlowDirection);
            Assert.True(editor.StartList.Left < editor.KillList.Left);
            Assert.True(editor.KillList.Left < editor.KillOnDisableList.Left);
        });
    }

    [Fact]
    public void The_loadout_list_pip_follows_the_row_colour_and_rings_when_none()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft
            {
                Loadouts = new List<LoadoutDraft>
                {
                    new() { Name = "AI", Color = "#2E6BFF" },
                    new() { Name = "Game" }
                }
            };
            var editor = new LoadoutEditor(draft);
            var list = Descendants(editor).OfType<ThemedListBox>().Single(l => l.Parent is Panel);

            var ai = list.PipColor!(0);
            Assert.True(ai.HasValue);
            Assert.Equal(Color.FromArgb(0x2E, 0x6B, 0xFF), ai!.Value);
            Assert.Null(list.PipColor!(1));
        });
    }

    [Fact]
    public void The_field_rows_show_Mode_and_keep_the_switch_wording()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft { Loadouts = new List<LoadoutDraft> { new() { Name = "AI" } } };
            var editor = new LoadoutEditor(draft);
            editor.Select(0);

            var labels = Descendants(editor).OfType<Label>().Select(l => l.Text).ToList();
            Assert.Contains("Name", labels);
            Assert.Contains("Mode", labels);
            Assert.Contains("Color", labels);
            Assert.Equal("Show as a switch in the tray", editor.ToggleText);
            Assert.Equal("Off: a button that runs once.", editor.ToggleHintText);
        });
    }


    [Fact]
    public void The_reference_cards_say_when_each_list_runs()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft { Loadouts = new List<LoadoutDraft> { new() { Name = "AI" } } };
            var editor = new LoadoutEditor(draft);
            editor.Select(0);

            Assert.Equal("Start", editor.StartList.Caption);
            Assert.Equal("Kill", editor.KillList.Caption);
            Assert.Equal("Kill on disable", editor.KillOnDisableList.Caption);
            Assert.Equal("Started when it turns on", editor.StartList.Description);
            Assert.Equal("Closed when it runs", editor.KillList.Description);
            Assert.Equal("Stopped when it turns off", editor.KillOnDisableList.Description);
        });
    }

    [Fact]
    public void The_three_reference_cards_share_one_width_and_fill_the_field_panel()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft { Loadouts = new List<LoadoutDraft> { new() { Name = "AI" } } };
            var editor = new LoadoutEditor(draft) { Size = new Size(764, 520) };
            editor.Select(0);
            editor.PerformLayout();

            var row = Assert.IsType<FlowLayoutPanel>(editor.StartList.Parent);
            var fieldPanel = Assert.IsType<FlowLayoutPanel>(row.Parent);
            fieldPanel.PerformLayout();

            Assert.Equal(editor.StartList.Width, editor.KillList.Width);
            Assert.Equal(editor.StartList.Width, editor.KillOnDisableList.Width);
            var usable = fieldPanel.ClientSize.Width - fieldPanel.Padding.Horizontal;
            Assert.InRange(row.Width, usable - 3, usable);
        });
    }

    [Fact]
    public void The_checkboxes_paint_on_the_field_panel_colour()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft { Loadouts = new List<LoadoutDraft> { new() { Name = "AI" } } };
            var editor = new LoadoutEditor(draft);

            foreach (var box in Descendants(editor).OfType<ThemedCheckBox>())
                Assert.Equal(Theme.Surface, box.BackColor);
        });
    }

    [Fact]
    public void Every_field_label_is_as_tall_as_its_input_and_centred_on_it()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft { Loadouts = new List<LoadoutDraft> { new() { Name = "AI", Color = "#2E6BFF" } } };
            var loadouts = new LoadoutEditor(draft) { Size = new Size(764, 520) };
            loadouts.Select(0);
            loadouts.PerformLayout();
            var definitions = new DefinitionEditor(new SettingsDraft
            {
                Definitions = new List<DefinitionDraft> { new() { Key = "ollama", Kind = "app" } }
            }) { Size = new Size(764, 520) };
            definitions.Select(0);
            definitions.PerformLayout();

            var rows = Descendants(loadouts).Concat(Descendants(definitions))
                .OfType<FlowLayoutPanel>()
                .Where(row => row.Controls.Count > 1 && row.Controls[0] is Label { Width: Theme.LabelColumnWidth })
                .ToList();
            Assert.True(rows.Count >= 10, $"Expected the field rows, found {rows.Count}.");

            foreach (var row in rows)
            {
                row.PerformLayout();
                var label = row.Controls[0];
                var input = row.Controls[1];
                Assert.True(label.Height <= input.Height, $"The {label.Text} label is taller than its input.");
                var labelCentre = label.Top + label.Height / 2;
                var inputCentre = input.Top + input.Height / 2;
                Assert.InRange(labelCentre, inputCentre - 1, inputCentre + 1);
            }
        });
    }

    [Fact]
    public void Each_reference_card_takes_its_own_topic_colour_from_the_site()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft { Loadouts = new List<LoadoutDraft> { new() { Name = "AI" } } };
            var editor = new LoadoutEditor(draft);
            editor.Select(0);
            var definitions = new DefinitionEditor(new SettingsDraft
            {
                Definitions = new List<DefinitionDraft> { new() { Key = "ollama", Kind = "app" } }
            });
            definitions.Select(0);

            Assert.Equal(Theme.Teal, editor.StartList.TopicColor);
            Assert.Equal(Theme.Pink, editor.KillList.TopicColor);
            Assert.Equal(Theme.Amber, editor.KillOnDisableList.TopicColor);
            Assert.Equal(Theme.Blue, definitions.RequiresList.TopicColor);
        });
    }

    [Fact]
    public void Field_labels_are_muted_eyebrows_painted_in_capitals()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft { Loadouts = new List<LoadoutDraft> { new() { Name = "AI" } } };
            var editor = new LoadoutEditor(draft);
            editor.Select(0);

            var labels = Descendants(editor).OfType<EyebrowLabel>().ToList();
            Assert.Contains(labels, l => l.Text == "Mode" && l.PaintedText == "MODE");
            foreach (var label in labels)
            {
                Assert.Equal(Theme.Muted, label.ForeColor);
                Assert.Equal(Theme.EyebrowFont.Name, label.Font.Name);
            }
        });
    }

    private static ScrollableControl? ScrollingAncestor(Control control, Control root)
    {
        for (var current = control.Parent; current != null && current != root; current = current.Parent)
        {
            if (current is ScrollableControl scrollable && scrollable.AutoScroll)
                return scrollable;
        }
        return null;
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
    private static void ForceHandles(Control control)
    {
        _ = control.Handle;
        foreach (Control child in control.Controls)
            ForceHandles(child);
    }

    private static CheckBox FindBox(Control root, string text)
    {
        foreach (Control child in root.Controls)
        {
            if (child is CheckBox box && box.Text == text)
                return box;
            var found = FindBox(child, text);
            if (found is not null)
                return found;
        }
        return null!;
    }
}