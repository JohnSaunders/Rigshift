using Rigshift.Config;
using Rigshift.Settings;

namespace Rigshift.Tests;

public class SettingsValidatorTests
{
    [Fact]
    public void MappedBuiltInDefaultIsValid()
    {
        var draft = SettingsMapper.FromConfig(ConfigLoader.Default());

        Assert.Empty(SettingsValidator.Validate(draft));
    }

    [Fact]
    public void IdleColorWithSurroundingSpacesIsValid()
    {
        var draft = Draft(" #8A8A8E ");

        Assert.Empty(SettingsValidator.Validate(draft));
    }

    [Fact]
    public void IdleColorThatIsNotHexIsRejected()
    {
        var draft = Draft("red");

        Assert.Contains(
            "Idle color must be #RRGGBB.",
            SettingsValidator.Validate(draft));
    }

    [Fact]
    public void LoadoutMessagesCoverNameDuplicateAndColor()
    {
        var draft = Draft(loadouts: new[]
        {
            LO(name: "", toggle: true, color: ""),
            LO(name: " AI ", toggle: true, color: "#123456"),
            LO(name: "AI", toggle: false, color: "#12"),
            LO(name: "Solo", toggle: false, color: "")
        });

        var errors = SettingsValidator.Validate(draft);

        Assert.Contains("Loadout at position 1: name is required.", errors);
        Assert.Contains("Loadout at position 1: color is required when toggle is on.", errors);
        Assert.Contains("Loadout 'AI': color must be #RRGGBB.", errors);
        Assert.Contains("Loadout name 'AI' is duplicated.", errors);
        Assert.DoesNotContain("Loadout 'Solo': color is required when toggle is on.", errors);
        Assert.DoesNotContain("Loadout 'Solo': color must be #RRGGBB.", errors);
    }

    [Fact]
    public void LoadoutNameWithCommaOrQuoteIsRejected()
    {
        var withComma = SettingsMapper.FromConfig(ConfigLoader.Default());
        withComma.Loadouts[0].Name = "AI, local";

        Assert.Contains(
            "Loadout 'AI, local': name cannot contain a comma or a double quote.",
            SettingsValidator.Validate(withComma));

        var withQuote = SettingsMapper.FromConfig(ConfigLoader.Default());
        withQuote.Loadouts[1].Name = "\"Dev\"";

        Assert.Contains(
            "Loadout '\"Dev\"': name cannot contain a comma or a double quote.",
            SettingsValidator.Validate(withQuote));
    }

    [Fact]
    public void AddedLoadoutNameWithCommaIsRejected()
    {
        var draft = SettingsMapper.FromConfig(ConfigLoader.Default());
        draft.Loadouts.Add(LO(name: "a,b", toggle: false, color: "#123456"));

        Assert.Contains(
            "Loadout 'a,b': name cannot contain a comma or a double quote.",
            SettingsValidator.Validate(draft));
    }

    [Fact]
    public void LoadoutNamesWithoutCommaOrQuoteAreAccepted()
    {
        var draft = Draft(loadouts: new[]
        {
            LO(name: "AI", toggle: false, color: "#123456"),
            LO(name: "Dev 2", toggle: false, color: "#123456"),
            LO(name: "Game - full", toggle: false, color: "#123456"),
            LO(name: " AI ", toggle: false, color: "#123456")
        });

        var errors = SettingsValidator.Validate(draft);

        Assert.DoesNotContain("Loadout 'AI': name cannot contain a comma or a double quote.", errors);
        Assert.DoesNotContain("Loadout 'Dev 2': name cannot contain a comma or a double quote.", errors);
        Assert.DoesNotContain("Loadout 'Game - full': name cannot contain a comma or a double quote.", errors);
    }

    [Fact]
    public void DefinitionMessagesCoverKindProcessPortAndProxy()
    {
        var draft = Draft(definitions: new[]
        {
            DF(key: "a", kind: "App", processName: "process"),
            DF(key: "b", kind: "app", processName: null),
            DF(key: "c", kind: "wsl", processName: null),
            DF(key: "d", kind: "wslService", processName: "process", port: "0"),
            DF(key: "e", kind: "wslService", processName: "process", port: "65536"),
            DF(key: "f", kind: "wslService", processName: "process", port: "abc"),
            DF(key: "g", kind: "wslService", processName: "process", port: null, portProxy: true),
            DF(key: "h", kind: "app", processName: "process", port: "11434", portProxy: true),
            DF(key: "i", kind: "wslService", processName: "process", port: "11434", portProxy: true),
            DF(key: "j", kind: "herdr", processName: null),
            DF(key: "k", kind: "herdr", processName: "hc")
        });

        var errors = SettingsValidator.Validate(draft);

        Assert.Contains("Definition 'a': kind must be app, wsl, wslService, or herdr.", errors);
        Assert.Contains("Definition 'j': process name is required.", errors);
        Assert.DoesNotContain(errors, e => e.StartsWith("Definition 'k'"));
        Assert.Contains("Definition 'b': process name is required.", errors);
        Assert.Contains("Definition 'd': port must be an integer from 1 to 65535.", errors);
        Assert.Contains("Definition 'e': port must be an integer from 1 to 65535.", errors);
        Assert.Contains("Definition 'f': port must be an integer from 1 to 65535.", errors);
        Assert.Contains("Definition 'g': port is required when port proxy is on.", errors);
        Assert.Contains("Definition 'h': port proxy applies to wslService only.", errors);
        Assert.DoesNotContain("Definition 'c': process name is required.", errors);
        Assert.DoesNotContain("Definition 'i': port must be an integer from 1 to 65535.", errors);
        Assert.DoesNotContain("Definition 'i': port is required when port proxy is on.", errors);
        Assert.DoesNotContain("Definition 'i': port proxy applies to wslService only.", errors);
    }

    [Fact]
    public void ListsAndCyclesProduceUnknownDuplicateBlankAndCycleLines()
    {
        var draft = Draft(
            loadouts: new[]
            {
                LO(
                    name: "L",
                    toggle: false,
                    color: null,
                    start: new[] { "wsl", "wsl", "ghost" },
                    kill: new[] { "" })
            },
            definitions: new[]
            {
                DF(key: "wsl", kind: "wsl"),
                DF(key: "a", kind: "app", processName: "process", requires: new[] { "b" }),
                DF(key: "b", kind: "app", processName: "process", requires: new[] { "a" }),
                DF(key: "s", kind: "wsl", requires: new[] { "s" })
            });

        var errors = SettingsValidator.Validate(draft);

        Assert.Contains("Loadout 'L': start lists 'wsl' more than once.", errors);
        Assert.Contains("Loadout 'L': start references unknown definition 'ghost'.", errors);
        Assert.Contains("Loadout 'L': kill contains a blank entry.", errors);
        Assert.Contains("Definition 'a': requires contains a cycle (a -> b -> a).", errors);
        Assert.Contains("Definition 's': requires contains a cycle (s -> s).", errors);
        Assert.DoesNotContain("Loadout 'L': start contains a blank entry.", errors);
        Assert.DoesNotContain("Loadout 'L': kill lists '' more than once.", errors);
    }

    private static SettingsDraft Draft(
        string idleColor = "#8A8A8E",
        IEnumerable<LoadoutDraft>? loadouts = null,
        IEnumerable<DefinitionDraft>? definitions = null) => new()
    {
        IdleColor = idleColor,
        Loadouts = loadouts?.ToList() ?? new List<LoadoutDraft>(),
        Definitions = definitions?.ToList() ?? new List<DefinitionDraft>()
    };

    private static LoadoutDraft LO(
        string name,
        bool toggle,
        string? color = null,
        IEnumerable<string>? start = null,
        IEnumerable<string>? kill = null,
        IEnumerable<string>? killOnDisable = null) => new()
    {
        Name = name,
        Toggle = toggle,
        Color = color,
        Start = ToList(start),
        Kill = ToList(kill),
        KillOnDisable = ToList(killOnDisable)
    };

    private static DefinitionDraft DF(
        string key,
        string kind,
        string? processName = null,
        string? port = null,
        bool portProxy = false,
        IEnumerable<string>? requires = null) => new()
    {
        Key = key,
        Kind = kind,
        ProcessName = processName,
        Port = port,
        PortProxy = portProxy,
        Requires = ToList(requires)
    };

    private static List<string> ToList(IEnumerable<string>? values) =>
        values is null ? new List<string>() : values.ToList();
}
