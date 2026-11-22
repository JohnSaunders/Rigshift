using Rigshift.Settings;

namespace Rigshift.Tests;

public class DraftListEditorTests
{
    [Fact]
    public void AddLoadoutUsesStubFieldsAndSequentialNames()
    {
        var draft = new SettingsDraft();

        var first = DraftListEditor.AddLoadout(draft);
        var second = DraftListEditor.AddLoadout(draft);

        Assert.Equal("New loadout", first.Name);
        Assert.True(first.Toggle);
        Assert.Equal("#8A8A8E", first.Color);
        Assert.Empty(first.Start);
        Assert.Empty(first.Kill);
        Assert.Empty(first.KillOnDisable);
        Assert.False(first.HardShutdown);
        Assert.False(first.FlushDns);

        Assert.Equal("New loadout 2", second.Name);
        Assert.Equal(2, draft.Loadouts.Count);
    }

    [Fact]
    public void DuplicateLoadoutInsertsCopyAfterSourceWithIndependentLists()
    {
        var source = new LoadoutDraft
        {
            Name = "Dev",
            Toggle = true,
            Color = "#111111",
            Start = new List<string> { "node" }
        };
        var draft = new SettingsDraft { Loadouts = { source } };

        var copy = DraftListEditor.DuplicateLoadout(draft, 0);

        Assert.Same(copy, draft.Loadouts[1]);
        Assert.Equal("Dev copy", copy.Name);
        Assert.True(copy.Toggle);
        Assert.Equal("#111111", copy.Color);
        Assert.Equal(new[] { "node" }, copy.Start);
        Assert.NotSame(source.Start, copy.Start);

        copy.Start.Add("ollama");
        Assert.Equal(new[] { "node" }, source.Start);
    }

    [Fact]
    public void DuplicateLoadoutNumbersCollidingSeeds()
    {
        var draft = new SettingsDraft
        {
            Loadouts =
            {
                new LoadoutDraft { Name = "Dev", Toggle = true, Color = "#111111" },
                new LoadoutDraft { Name = "Dev copy", Toggle = true, Color = "#222222" }
            }
        };

        var copy = DraftListEditor.DuplicateLoadout(draft, 0);

        Assert.Same(copy, draft.Loadouts[1]);
        Assert.Equal("Dev copy 2", copy.Name);
    }

    [Fact]
    public void DeleteLoadoutRemovesOnlyTheChosenIndex()
    {
        var a = new LoadoutDraft { Name = "A" };
        var b = new LoadoutDraft { Name = "B" };
        var c = new LoadoutDraft { Name = "C" };
        var draft = new SettingsDraft { Loadouts = { a, b, c } };

        DraftListEditor.DeleteLoadout(draft, 1);

        Assert.Equal(2, draft.Loadouts.Count);
        Assert.Same(a, draft.Loadouts[0]);
        Assert.Same(c, draft.Loadouts[1]);
    }

    [Fact]
    public void AddAndDuplicateDefinitionCreateCopiesWithIndependentRequires()
    {
        var loadout = new LoadoutDraft
        {
            Name = "Dev",
            Toggle = true,
            Color = "#111111",
            Start = new List<string> { "newDefinition" }
        };
        var draft = new SettingsDraft { Loadouts = { loadout } };

        var added = DraftListEditor.AddDefinition(draft);
        var copy = DraftListEditor.DuplicateDefinition(draft, 0);

        Assert.Equal("app", added.Kind);
        Assert.Equal("process", added.ProcessName);
        Assert.Null(added.Path);
        Assert.Null(added.WindowTitle);
        Assert.Null(added.Port);
        Assert.Null(added.WslCommand);
        Assert.False(added.PortProxy);
        Assert.Empty(added.Requires);

        Assert.Equal("newDefinition", draft.Definitions[0].Key);
        Assert.Same(copy, draft.Definitions[1]);
        Assert.Equal("newDefinitionCopy", copy.Key);
        Assert.NotSame(draft.Definitions[0].Requires, copy.Requires);
        Assert.DoesNotContain("newDefinitionCopy", loadout.Start);

        copy.Requires.Add("newDefinition");
        Assert.Empty(draft.Definitions[0].Requires);
    }

    [Fact]
    public void TryDeleteDefinitionNamesTheUsingLoadoutAndDefinition()
    {
        var target = new DefinitionDraft { Key = "game", Kind = "app", ProcessName = "game" };
        var user = new DefinitionDraft
        {
            Key = "ollama",
            Kind = "app",
            ProcessName = "ollama",
            Requires = new List<string> { "game" }
        };
        var loadout = new LoadoutDraft
        {
            Name = "Game",
            Toggle = true,
            Color = "#111111",
            Kill = new List<string> { "game" }
        };
        var draft = new SettingsDraft
        {
            Definitions = { target, user },
            Loadouts = { loadout }
        };

        var result = DraftListEditor.TryDeleteDefinition(draft, 0);

        Assert.Equal("Definition 'game' is used by loadout 'Game', definition 'ollama'.", result);
        Assert.Equal(2, draft.Definitions.Count);
        Assert.Same(target, draft.Definitions[0]);
    }

    [Fact]
    public void TryDeleteDefinitionUsesPositionsForBlankNamesAndKeys()
    {
        var target = new DefinitionDraft { Key = "x", Kind = "app" };
        var blankKeyUser = new DefinitionDraft
        {
            Key = "   ",
            Kind = "app",
            Requires = new List<string> { "x" }
        };
        var loadout = new LoadoutDraft
        {
            Name = "  ",
            Toggle = true,
            Color = "#111111",
            Start = new List<string> { "x" }
        };
        var draft = new SettingsDraft
        {
            Definitions = { target, blankKeyUser },
            Loadouts = { loadout }
        };

        var result = DraftListEditor.TryDeleteDefinition(draft, 0);

        Assert.Equal("Definition 'x' is used by loadout at position 1, definition at position 2.", result);
        Assert.Equal(2, draft.Definitions.Count);
    }

    [Fact]
    public void TryDeleteDefinitionRemovesUnreferencedAndSelfRequiringKeys()
    {
        var lonely = new SettingsDraft
        {
            Definitions = { new DefinitionDraft { Key = "orphan", Kind = "app" } }
        };
        var self = new DefinitionDraft
        {
            Key = "solo",
            Kind = "app",
            Requires = new List<string> { "solo" }
        };
        var selfRequiring = new SettingsDraft { Definitions = { self } };
        var blank = new SettingsDraft
        {
            Definitions = { new DefinitionDraft { Key = "  ", Kind = "app", Requires = new List<string> { "orphan" } } }
        };

        Assert.Null(DraftListEditor.TryDeleteDefinition(lonely, 0));
        Assert.Empty(lonely.Definitions);

        Assert.Null(DraftListEditor.TryDeleteDefinition(selfRequiring, 0));
        Assert.Empty(selfRequiring.Definitions);

        Assert.Null(DraftListEditor.TryDeleteDefinition(blank, 0));
        Assert.Empty(blank.Definitions);
    }

    [Fact]
    public void TryRenameDefinitionKeyRewritesReferencesIncludingSelfRequires()
    {
        var target = new DefinitionDraft
        {
            Key = "wsl",
            Kind = "wsl",
            WslCommand = "wsl.exe",
            Requires = new List<string> { "  wsl ", "ollama" }
        };
        var loadout = new LoadoutDraft
        {
            Name = "Dev",
            Toggle = true,
            Color = "#111111",
            Start = new List<string> { "wsl", "ollama" }
        };
        var draft = new SettingsDraft
        {
            Definitions = { target },
            Loadouts = { loadout }
        };

        var result = DraftListEditor.TryRenameDefinitionKey(draft, 0, "  wsl2 ");

        Assert.Null(result);
        Assert.Equal("wsl2", target.Key);
        Assert.Equal(new[] { "wsl2", "ollama" }, loadout.Start);
        Assert.Equal(new[] { "wsl2", "ollama" }, target.Requires);
    }

    [Fact]
    public void TryRenameDefinitionKeyStripsStoredWhitespaceAndRewrites()
    {
        var target = new DefinitionDraft
        {
            Key = " wsl",
            Kind = "wsl",
            Requires = new List<string> { "wsl" }
        };
        var loadout = new LoadoutDraft
        {
            Name = "Dev",
            Toggle = true,
            Color = "#111111",
            Start = new List<string> { " wsl " }
        };
        var draft = new SettingsDraft
        {
            Definitions = { target },
            Loadouts = { loadout }
        };

        var result = DraftListEditor.TryRenameDefinitionKey(draft, 0, "wsl");

        Assert.Null(result);
        Assert.Equal("wsl", target.Key);
        Assert.Equal(new[] { "wsl" }, loadout.Start);
        Assert.Equal(new[] { "wsl" }, target.Requires);
    }

    [Fact]
    public void TryRenameDefinitionKeyReturnsNullWithoutWritingWhenTheKeyIsUnchanged()
    {
        var target = new DefinitionDraft { Key = "wsl", Kind = "wsl" };
        var draft = new SettingsDraft { Definitions = { target } };

        var result = DraftListEditor.TryRenameDefinitionKey(draft, 0, "wsl");

        Assert.Null(result);
        Assert.Equal("wsl", target.Key);
    }

    [Fact]
    public void TryRenameDefinitionKeyRejectsBlankAndDuplicateKeysWithoutChanges()
    {
        var target = new DefinitionDraft
        {
            Key = "wsl",
            Kind = "wsl",
            Requires = new List<string> { "wsl" }
        };
        var other = new DefinitionDraft { Key = "ollama", Kind = "app" };
        var loadout = new LoadoutDraft
        {
            Name = "Dev",
            Toggle = true,
            Color = "#111111",
            Start = new List<string> { "wsl" }
        };
        var draft = new SettingsDraft
        {
            Definitions = { target, other },
            Loadouts = { loadout }
        };

        Assert.Equal("Definition key is required.", DraftListEditor.TryRenameDefinitionKey(draft, 0, "   "));
        Assert.Equal("Definition key 'ollama' is duplicated.", DraftListEditor.TryRenameDefinitionKey(draft, 0, "ollama"));

        Assert.Equal("wsl", target.Key);
        Assert.Equal(new[] { "wsl" }, loadout.Start);
        Assert.Equal(new[] { "wsl" }, target.Requires);
        Assert.Equal("ollama", other.Key);
    }

    [Fact]
    public void OutOfRangeIndexesThrowOnMutatingCalls()
    {
        var draft = new SettingsDraft();

        Assert.Throws<ArgumentOutOfRangeException>(() => DraftListEditor.DuplicateLoadout(draft, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => DraftListEditor.DeleteLoadout(draft, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => DraftListEditor.DuplicateDefinition(draft, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => DraftListEditor.TryDeleteDefinition(draft, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => DraftListEditor.TryRenameDefinitionKey(draft, 0, "wsl"));
    }
}
