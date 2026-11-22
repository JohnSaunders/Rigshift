using Rigshift.Config;
using Rigshift.Settings;

namespace Rigshift.Tests;

public class SettingsMapperTests
{
    [Fact]
    public void TheBuiltInDefaultRoundTripsThroughDraftAndBack()
    {
        var source = ConfigLoader.Default();

        var draft = SettingsMapper.FromConfig(source);
        var roundTrip = SettingsMapper.ToConfig(draft);

        Assert.Equal("#8A8A8E", draft.IdleColor);
        Assert.Equal("#8A8A8E", roundTrip.IdleColor);

        var defaultKeys = new[] { "ollama", "ollamaServer", "ollamaRunner", "wsl", "rider", "webstorm", "vscode", "node" };
        Assert.Equal(defaultKeys, draft.Definitions.Select(d => d.Key));
        Assert.Equal(defaultKeys, roundTrip.Definitions.Keys);

        Assert.Null(source.Definitions["ollama"].Port);
        Assert.Null(draft.Definitions[0].Port);
        Assert.Null(roundTrip.Definitions["ollama"].Port);

        for (var i = 0; i < source.Definitions.Count; i++)
        {
            var expected = source.Definitions[defaultKeys[i]];
            var actual = roundTrip.Definitions[defaultKeys[i]];
            Assert.Equal(expected.Kind, actual.Kind);
            Assert.Equal(expected.ProcessName, actual.ProcessName);
            Assert.Equal(expected.Path, actual.Path);
            Assert.Equal(expected.WindowTitle, actual.WindowTitle);
            Assert.Equal(expected.Port, actual.Port);
            Assert.Equal(expected.WslCommand, actual.WslCommand);
            Assert.Equal(expected.PortProxy, actual.PortProxy);
            Assert.Equal(expected.Requires, actual.Requires);
        }

        Assert.Equal(new[] { "AI", "Dev", "Game" }, draft.Loadouts.Select(l => l.Name));
        Assert.Equal(new[] { "AI", "Dev", "Game" }, roundTrip.Loadouts.Select(l => l.Name));
        for (var i = 0; i < source.Loadouts.Count; i++)
        {
            var expected = source.Loadouts[i];
            var actual = roundTrip.Loadouts[i];
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.Toggle, actual.Toggle);
            Assert.Equal(expected.Color, actual.Color);
            Assert.Equal(expected.Start, actual.Start);
            Assert.Equal(expected.Kill, actual.Kill);
            Assert.Equal(expected.KillOnDisable, actual.KillOnDisable);
            Assert.Equal(expected.HardShutdown, actual.HardShutdown);
            Assert.Equal(expected.FlushDns, actual.FlushDns);
        }
    }

    [Fact]
    public void DefinitionOrderFollowsInsertedKeysAndPortBecomesInvariantText()
    {
        var source = new RigshiftConfig
        {
            IdleColor = "#123456",
            Definitions = new Dictionary<string, Definition>
            {
                ["c"] = new() { Kind = "app", ProcessName = "proc" },
                ["a"] = new() { Kind = "wslService", ProcessName = "ollama", Port = 11434, PortProxy = true },
                ["b"] = new() { Kind = "wsl", WslCommand = "wsl.exe -d Ubuntu" }
            },
            Loadouts = new List<Loadout> { new() { Name = "One", Toggle = true, Color = "#336699" } }
        };

        var draft = SettingsMapper.FromConfig(source);
        var roundTrip = SettingsMapper.ToConfig(draft);

        Assert.Equal(new[] { "c", "a", "b" }, draft.Definitions.Select(d => d.Key));
        Assert.Equal("11434", draft.Definitions[1].Port);
        Assert.Null(draft.Definitions[0].Port);
        Assert.Null(draft.Definitions[2].Port);
        Assert.Equal(new[] { "c", "a", "b" }, roundTrip.Definitions.Keys);
        Assert.Equal(11434, roundTrip.Definitions["a"].Port);
        Assert.Equal("wslService", roundTrip.Definitions["a"].Kind);
        Assert.True(roundTrip.Definitions["a"].PortProxy);
        Assert.Equal("wsl.exe -d Ubuntu", roundTrip.Definitions["b"].WslCommand);
        Assert.Equal("One", Assert.Single(roundTrip.Loadouts).Name);
    }

    [Fact]
    public void MutatingDraftListsLeavesTheSourceConfigUntouched()
    {
        var source = new RigshiftConfig
        {
            IdleColor = "#8A8A8E",
            Definitions = new Dictionary<string, Definition>
            {
                ["ollama"] = new()
                {
                    Kind = "wslService",
                    ProcessName = "ollama",
                    Requires = new List<string> { "ollamaRunner" }
                }
            },
            Loadouts = new List<Loadout>
            {
                new() { Name = "AI", Toggle = true, Color = "#2E6BFF", Start = new List<string> { "ollama" } },
                new() { Name = "Dev", Toggle = true, Color = "#FF3B30", Kill = new List<string> { "wsl" } }
            }
        };

        var draft = SettingsMapper.FromConfig(source);
        draft.Loadouts[0].Start.Add("ghost");
        draft.Loadouts[0].Start[0] = "someone";
        draft.Loadouts[1].Kill.Add("ollama");
        draft.Definitions[0].Requires.Add("ollamaServer");

        Assert.Equal(new[] { "ollama" }, source.Loadouts[0].Start);
        Assert.Equal(new[] { "wsl" }, source.Loadouts[1].Kill);
        Assert.Equal(new[] { "ollamaRunner" }, source.Definitions["ollama"].Requires);
    }

    [Fact]
    public void ToConfigTrimsValuesAndTurnsBlankOptionalsIntoNull()
    {
        var draft = new SettingsDraft
        {
            IdleColor = "   #112233   ",
            Definitions =
            {
                new DefinitionDraft
                {
                    Key = "  ollama  ",
                    Kind = "  app  ",
                    ProcessName = "   ",
                    Path = "  C:\\app.exe  ",
                    WindowTitle = "\t",
                    Port = "  11434  ",
                    WslCommand = "  ollama serve  "
                }
            },
            Loadouts =
            {
                new LoadoutDraft
                {
                    Name = "  AI  ",
                    Toggle = true,
                    Color = "    ",
                    Start = new List<string> { "  ollama ", "   " }
                }
            }
        };

        var config = SettingsMapper.ToConfig(draft);

        Assert.Equal("#112233", config.IdleColor);
        Assert.Equal("ollama", Assert.Single(config.Definitions.Keys));
        var definition = config.Definitions["ollama"];
        Assert.Equal("app", definition.Kind);
        Assert.Null(definition.ProcessName);
        Assert.Equal(@"C:\app.exe", definition.Path);
        Assert.Null(definition.WindowTitle);
        Assert.Equal(11434, definition.Port);
        Assert.Equal("ollama serve", definition.WslCommand);

        var loadout = Assert.Single(config.Loadouts);
        Assert.Equal("AI", loadout.Name);
        Assert.True(loadout.Toggle);
        Assert.Null(loadout.Color);
        Assert.Equal(new[] { "ollama" }, loadout.Start);

        // ToConfig must never mutate the draft.
        Assert.Equal("  ollama  ", draft.Definitions[0].Key);
        Assert.Equal("    ", draft.Loadouts[0].Color);
    }

    [Fact]
    public void ANonNumericPortThrowsInvalidOperation()
    {
        var draft = new SettingsDraft
        {
            Definitions =
            {
                new DefinitionDraft { Key = "wslService", Kind = "wslService", ProcessName = "ollama", Port = "not-a-port" }
            }
        };

        Assert.Throws<InvalidOperationException>(() => SettingsMapper.ToConfig(draft));
    }

    [Fact]
    public void TwoDefinitionRowsWhoseTrimmedKeysMatchThrowInvalidOperation()
    {
        var draft = new SettingsDraft
        {
            Definitions =
            {
                new DefinitionDraft { Key = "ollama", Kind = "app" },
                new DefinitionDraft { Key = "  ollama  ", Kind = "wsl" }
            }
        };

        Assert.Throws<InvalidOperationException>(() => SettingsMapper.ToConfig(draft));
    }
}
