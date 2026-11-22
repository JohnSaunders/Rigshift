using Rigshift.Settings;

namespace Rigshift.Tests;

public class ReferenceListEditorTests
{
    [Fact]
    public void AddKeyAppendsToBoundListAndItems()
    {
        StaThread.Run(() =>
        {
            var bound = new List<string> { "wsl" };
            var editor = new ReferenceListEditor("Keys");
            editor.Bind(bound, () => new[] { "ollama", "wsl", "node" });

            Assert.Equal(new[] { "wsl" }, editor.Items);

            editor.AddKey("ollama");

            Assert.Equal(new[] { "wsl", "ollama" }, bound);
            Assert.Equal(new[] { "wsl", "ollama" }, editor.Items);
        });
    }

    [Fact]
    public void AddKeyIgnoresBlankAndExistingKeys()
    {
        StaThread.Run(() =>
        {
            var bound = new List<string> { "wsl" };
            var editor = new ReferenceListEditor("Keys");
            editor.Bind(bound, () => new[] { "ollama", "wsl", "node" });

            editor.AddKey("");
            editor.AddKey(" ");
            editor.AddKey("wsl");

            Assert.Equal(new[] { "wsl" }, bound);
            Assert.Equal(new[] { "wsl" }, editor.Items);
        });
    }

    [Fact]
    public void RemoveAtRemovesEntryAndIgnoresOutOfRange()
    {
        StaThread.Run(() =>
        {
            var bound = new List<string> { "a", "b" };
            var editor = new ReferenceListEditor("Keys");
            editor.Bind(bound, () => new[] { "a", "b" });

            editor.RemoveAt(0);
            Assert.Equal(new[] { "b" }, bound);
            Assert.Equal(new[] { "b" }, editor.Items);

            editor.RemoveAt(3);
            editor.RemoveAt(-1);

            Assert.Equal(new[] { "b" }, bound);
            Assert.Equal(new[] { "b" }, editor.Items);
        });
    }

    [Fact]
    public void RefreshKeysExcludesAlreadyBoundKeys()
    {
        StaThread.Run(() =>
        {
            var bound = new List<string> { "wsl" };
            var editor = new ReferenceListEditor("Keys");
            editor.Bind(bound, () => new[] { "ollama", "wsl", "node" });

            editor.RefreshKeys();

            Assert.Equal(new[] { "ollama", "node" }, editor.AvailableChoices);
        });
    }

    [Fact]
    public void IsWideEnoughToShowItsAddLabelAndCountsItems()
    {
        StaThread.Run(() =>
        {
            var editor = new ReferenceListEditor("Kill") { Height = 120, Width = 220 };
            editor.Bind(new List<string> { "a", "b" }, () => new[] { "c" });
            editor.PerformLayout();

            // The caller-set height is preserved.
            Assert.Equal(120, editor.Height);

            var buttons = Descendants(editor)
                .Where(control => control is Button)
                .Cast<Button>()
                .ToList();
            var combos = Descendants(editor)
                .Where(control => control is ComboBox)
                .Cast<ComboBox>()
                .ToList();

            // The visible Remove button is gone; only the outlined Add remains, carried
            // by a ThemedButton, and it has a readable label.
            Assert.Single(buttons);
            Assert.IsType<ThemedButton>(buttons[0]);
            Assert.Equal("Add", buttons[0].Text);

            // After layout, the Add button and the combo stay inside the editor.
            int clientWidth = editor.ClientSize.Width;
            foreach (var button in buttons)
                Assert.InRange(RightEdgeIn(editor, button), 0, clientWidth);
            foreach (var combo in combos)
                Assert.InRange(RightEdgeIn(editor, combo), 0, clientWidth);

            Assert.True(clientWidth > 0);

            // The header shows the bound count.
            Assert.Equal("2", editor.CountText);
        });
    }

    [Fact]
    public void An_empty_reference_list_shows_the_muted_empty_state_text()
    {
        StaThread.Run(() =>
        {
            var editor = new ReferenceListEditor("Start");
            var list = Descendants(editor).OfType<ThemedListBox>().Single();
            Assert.Equal("Nothing added", list.EmptyText);
            Assert.Equal(ReferenceListEditor.EmptyListText, list.EmptyText);
        });
    }

    [Fact]
    public void The_card_shows_its_caption_and_description_and_rows_carry_a_remove_cross()
    {
        StaThread.Run(() =>
        {
            var editor = new ReferenceListEditor("Kill on disable", "Stopped when it turns off");
            var list = Descendants(editor).OfType<ThemedListBox>().Single();

            Assert.Equal("Kill on disable", editor.Caption);
            Assert.Equal("Stopped when it turns off", editor.Description);
            Assert.True(list.ShowRemoveGlyph);

            var plain = new ReferenceListEditor("Requires");
            Assert.Equal("", plain.Description);
        });
    }

    [Fact]
    public void The_Add_button_is_outlined_with_a_plus_and_its_menu_lists_the_unbound_keys()
    {
        StaThread.Run(() =>
        {
            var editor = new ReferenceListEditor("Start");
            editor.Bind(new List<string> { "wsl" }, () => new[] { "ollama", "wsl", "node" });

            var add = Descendants(editor).OfType<ThemedButton>().Single();
            Assert.Equal("Add", add.Text);
            Assert.True(add.ShowPlus);
            Assert.False(add.Primary);

            editor.ShowAddMenu();
            var items = editor.AddMenu.Items.OfType<ToolStripMenuItem>().ToList();
            Assert.Equal(new[] { "ollama", "node" }, items.Select(i => i.Text));
            Assert.All(items, i => Assert.True(i.Enabled));

            items[1].PerformClick();
            Assert.Equal(new[] { "wsl", "node" }, editor.Items);
            Assert.Equal(new[] { "ollama" }, editor.AvailableChoices);
        });
    }

    [Fact]
    public void With_nothing_left_to_add_the_menu_shows_one_disabled_line()
    {
        StaThread.Run(() =>
        {
            var editor = new ReferenceListEditor("Start");
            editor.Bind(new List<string> { "wsl" }, () => new[] { "wsl" });
            editor.ShowAddMenu();
            var only = Assert.Single(editor.AddMenu.Items.OfType<ToolStripMenuItem>());
            Assert.False(only.Enabled);
            Assert.Equal("Everything is already added", only.Text);

            var none = new ReferenceListEditor("Start");
            none.Bind(new List<string>(), Array.Empty<string>);
            none.ShowAddMenu();
            var line = Assert.Single(none.AddMenu.Items.OfType<ToolStripMenuItem>());
            Assert.False(line.Enabled);
            Assert.Equal("No definitions yet", line.Text);
        });
    }

    [Fact]
    public void Removing_a_row_offers_its_key_in_the_Add_menu_again()
    {
        StaThread.Run(() =>
        {
            var bound = new List<string> { "wsl", "node" };
            var editor = new ReferenceListEditor("Kill");
            editor.Bind(bound, () => new[] { "wsl", "node" });
            Assert.Empty(editor.AvailableChoices);

            editor.RemoveAt(0);

            Assert.Equal(new[] { "node" }, bound);
            Assert.Equal(new[] { "wsl" }, editor.AvailableChoices);
            Assert.Equal("1", editor.CountText);
        });
    }

    [Fact]
    public void The_header_count_follows_Bind_AddKey_and_RemoveAt()
    {
        StaThread.Run(() =>
        {
            var bound = new List<string> { "a" };
            var editor = new ReferenceListEditor("Keys");
            editor.Bind(bound, () => new[] { "a", "b", "c" });

            Assert.Equal("1", editor.CountText);

            editor.AddKey("b");
            Assert.Equal("2", editor.CountText);
            Assert.DoesNotContain("b", editor.AvailableChoices);

            editor.RemoveAt(0);
            Assert.Equal("1", editor.CountText);

            editor.Bind(new List<string> { "x", "y", "z" }, () => Array.Empty<string>());
            Assert.Equal("3", editor.CountText);
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

    private static int RightEdgeIn(Control target, Control control)
    {
        int left = 0;
        for (var current = control; current != null && current != target; current = current.Parent)
            left += current.Left;
        return left + control.Width;
    }
}
