using Rigshift.Settings;

namespace Rigshift.Tests;

public class DefinitionEditorTests
{
    [Fact]
    public void ShowsTheDefinitionKeysAndSelectFillsTheKindField()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft
            {
                Definitions = new List<DefinitionDraft>
                {
                    new() { Key = "ollama", Kind = "app", ProcessName = "ollama" },
                    new() { Key = "wsl", Kind = "wsl", WslCommand = "rigshift" }
                }
            };
            var editor = new DefinitionEditor(draft);

            Assert.Equal(new[] { "ollama", "wsl" }, editor.DefinitionKeys);

            editor.Select(1);

            Assert.Equal(1, editor.SelectedIndex);
            Assert.Equal("wsl", editor.KindText);
            Assert.Equal("wsl", draft.Definitions[1].Kind);
            Assert.Equal("rigshift", editor.WslCommandText);
        });
    }

    [Fact]
    public void AddAppendsAndSelectsTheNewAppDefinition()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft
            {
                Definitions = new List<DefinitionDraft> { new() { Key = "ollama", Kind = "app" } }
            };
            var editor = new DefinitionEditor(draft);
            var changed = 0;
            editor.DefinitionsChanged += (_, _) => changed++;

            editor.Add();

            Assert.Equal(2, draft.Definitions.Count);
            Assert.Equal("newDefinition", draft.Definitions[1].Key);
            Assert.Equal("app", draft.Definitions[1].Kind);
            Assert.Equal("process", draft.Definitions[1].ProcessName);
            Assert.Equal(new[] { "ollama", "newDefinition" }, editor.DefinitionKeys);
            Assert.Equal(1, editor.SelectedIndex);
            Assert.Equal("newDefinition", editor.KeyText);
            Assert.Equal("process", editor.ProcessNameText);
            Assert.Equal(1, changed);
        });
    }

    [Fact]
    public void CommitPendingRenamesTheKeyAndRewritesTheLoadoutStart()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft
            {
                Definitions = new List<DefinitionDraft>
                {
                    new() { Key = "ollama", Kind = "app", ProcessName = "ollama" },
                    new() { Key = "wsl", Kind = "wsl" }
                },
                Loadouts = new List<LoadoutDraft> { new() { Name = "AI", Start = new List<string> { "wsl" } } }
            };
            var editor = new DefinitionEditor(draft);
            var changed = 0;
            editor.DefinitionsChanged += (_, _) => changed++;

            editor.Select(1);
            editor.KeyText = "wsl2";
            var error = editor.CommitPending();

            Assert.Null(error);
            Assert.Equal("wsl2", draft.Definitions[1].Key);
            Assert.Equal(new[] { "wsl2" }, draft.Loadouts[0].Start);
            Assert.Equal(new[] { "ollama", "wsl2" }, editor.DefinitionKeys);
            Assert.Equal("wsl2", editor.KeyText);
            Assert.Equal("", editor.KeyErrorText);
            Assert.Equal(1, changed);
        });
    }

    [Fact]
    public void CommitPendingRejectsADuplicateKeyAndRestoresTheBox()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft
            {
                Definitions = new List<DefinitionDraft>
                {
                    new() { Key = "ollama", Kind = "app" },
                    new() { Key = "wsl", Kind = "wsl" }
                }
            };
            var editor = new DefinitionEditor(draft);
            var changed = 0;
            editor.DefinitionsChanged += (_, _) => changed++;

            editor.Select(1);
            editor.KeyText = "ollama";
            var error = editor.CommitPending();

            Assert.Equal("Definition key 'ollama' is duplicated.", error);
            Assert.Equal("Definition key 'ollama' is duplicated.", editor.KeyErrorText);
            Assert.Equal(Theme.Caution, editor.KeyErrorColor);
            Assert.Equal("wsl", editor.KeyText);
            Assert.Equal("wsl", draft.Definitions[1].Key);
            Assert.Equal(0, changed);
        });
    }

    [Fact]
    public void DeleteRemovesTheSelectedUnreferencedRowAndRaisesChanged()
    {
        StaThread.Run(() =>
        {
            var ollama = new DefinitionDraft { Key = "ollama", Kind = "app", ProcessName = "ollama" };
            var dev = new DefinitionDraft { Key = "dev", Kind = "app" };
            var draft = new SettingsDraft { Definitions = new List<DefinitionDraft> { ollama, dev } };
            var editor = new DefinitionEditor(draft);
            editor.ConfirmDelete = false;
            var changed = 0;
            editor.DefinitionsChanged += (_, _) => changed++;

            editor.Select(1);
            editor.Delete();

            Assert.Single(draft.Definitions);
            Assert.Same(ollama, draft.Definitions[0]);
            Assert.Equal(new[] { "ollama" }, editor.DefinitionKeys);
            Assert.Equal(1, changed);
        });
    }

    [Fact]
    public void PortTextWritesThroughAsEntered()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft
            {
                Definitions = new List<DefinitionDraft> { new() { Key = "ollama", Kind = "wslService" } }
            };
            var editor = new DefinitionEditor(draft);

            editor.Select(0);
            editor.PortText = "11434";

            Assert.Equal("11434", draft.Definitions[0].Port);

            editor.PortText = "not-a-port";

            Assert.Equal("not-a-port", draft.Definitions[0].Port);

            editor.PortText = "   ";

            Assert.Null(draft.Definitions[0].Port);
        });
    }

    [Fact]
    public void ChangingKindLeavesTheProcessNameInPlace()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft
            {
                Definitions = new List<DefinitionDraft> { new() { Key = "ollama", Kind = "app", ProcessName = "process" } }
            };
            var editor = new DefinitionEditor(draft);

            editor.Select(0);
            editor.KindText = "wsl";

            Assert.Equal("wsl", draft.Definitions[0].Kind);
            Assert.Equal("process", editor.ProcessNameText);
            Assert.Equal("process", draft.Definitions[0].ProcessName);
        });
    }

    [Fact]
    public void AnUnknownLoadedKindIsInsertedAndLeftSelected()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft
            {
                Definitions = new List<DefinitionDraft> { new() { Key = "legacy", Kind = "local", ProcessName = "legacy" } }
            };
            var editor = new DefinitionEditor(draft);

            editor.Select(0);

            Assert.Equal("local", editor.KindText);
            Assert.Equal("local", draft.Definitions[0].Kind);
        });
    }

    [Fact]
    public void DuplicateAndDeleteDoNothingWhenNothingIsSelected()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft
            {
                Definitions = new List<DefinitionDraft> { new() { Key = "ollama", Kind = "app" } }
            };
            var editor = new DefinitionEditor(draft);
            editor.ConfirmDelete = false;

            editor.Duplicate();
            editor.Delete();

            Assert.Single(draft.Definitions);
            Assert.Equal(new[] { "ollama" }, editor.DefinitionKeys);
        });
    }

    [Fact]
    public void TheFieldPanelScrollsAndReachesTheRequiresListAtTheMinimumSize()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft
            {
                Definitions = new List<DefinitionDraft> { new() { Key = "ollama", Kind = "app" } }
            };
            var editor = new DefinitionEditor(draft);
            editor.Size = new Size(560, 300);
            editor.Select(0);
            editor.PerformLayout();

            var portProxyBox = FindBox(editor, "Port proxy");
            var panel = editor.RequiresList.Parent;
            Assert.NotNull(panel);
            Assert.True(panel.Contains(portProxyBox));
            var scrollable = Assert.IsAssignableFrom<ScrollableControl>(panel);
            Assert.True(scrollable.AutoScroll);
            Assert.False(scrollable.AutoSize);
            Assert.True(panel.Bottom <= editor.ClientSize.Height);
        });
    }

    [Fact]
    public void The_requires_card_stays_directly_under_the_options_row_after_a_reselect()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft
            {
                Definitions = new List<DefinitionDraft>
                {
                    new() { Key = "ollama", Kind = "app" },
                    new() { Key = "wsl", Kind = "app" }
                }
            };
            var editor = new DefinitionEditor(draft);
            editor.Select(0);
            editor.Select(1);

            var panel = editor.RequiresList.Parent!;
            var portProxyRow = FindBox(editor, "Port proxy").Parent!;
            Assert.Equal(panel.Controls.GetChildIndex(portProxyRow) + 1, panel.Controls.GetChildIndex(editor.RequiresList));
        });
    }

    [Fact]
    public void TheEmptyHintShowsWhenNothingIsSelectedAndHidesOnSelection()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft
            {
                Definitions = new List<DefinitionDraft> { new() { Key = "ollama", Kind = "app" } }
            };
            var editor = new DefinitionEditor(draft);

            Assert.True(editor.HintVisible);

            editor.Select(0);

            Assert.False(editor.HintVisible);

            editor.Select(-1);

            Assert.True(editor.HintVisible);
        });
    }

    [Fact]
    public void AddSelectsTheNewRowAndPutsTheCursorInItsKeyBox()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft
            {
                Definitions = new List<DefinitionDraft> { new() { Key = "ollama", Kind = "app" } }
            };
            var editor = new DefinitionEditor(draft);

            editor.Add();

            Assert.False(editor.HintVisible);
            Assert.Equal(draft.Definitions.Count - 1, editor.SelectedIndex);
            var keyBox = Assert.IsAssignableFrom<TextBox>(editor.ActiveControl);
            Assert.Equal(draft.Definitions[1].Key, keyBox.Text);
        });
    }

    [Fact]
    public void DeletingEveryRowShowsTheEmptyHintAgain()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft
            {
                Definitions = new List<DefinitionDraft>
                {
                    new() { Key = "ollama", Kind = "app" },
                    new() { Key = "wsl", Kind = "wsl" }
                }
            };
            var editor = new DefinitionEditor(draft);
            editor.ConfirmDelete = false;

            editor.Select(0);
            editor.Delete();
            editor.Delete();

            Assert.Empty(draft.Definitions);
            Assert.True(editor.HintVisible);
        });
    }


    [Fact]
    public void The_themed_list_shows_None_and_port_proxy_writes_through()
    {
        StaThread.Run(() =>
        {
            var draft = new SettingsDraft
            {
                Definitions = new List<DefinitionDraft> { new() { Key = "ollama", Kind = "app" } }
            };
            var editor = new DefinitionEditor(draft);
            editor.Select(0);

            var list = Descendants(editor).OfType<ThemedListBox>().Single(l => l.Parent is Panel);
            Assert.Equal("None", list.EmptyText);

            Assert.False(editor.PortProxyChecked);
            editor.PortProxyChecked = true;
            Assert.True(draft.Definitions[0].PortProxy);
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