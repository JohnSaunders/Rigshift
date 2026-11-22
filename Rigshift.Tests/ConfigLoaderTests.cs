using Rigshift.Config;

namespace Rigshift.Tests;

public class ConfigLoaderTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("rigshift").FullName;

    private string WriteConfig(string json)
    {
        var path = Path.Combine(_dir, "rigshift.json");
        File.WriteAllText(path, json);
        return path;
    }

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void ParsesDefinitionsAndLoadouts()
    {
        var path = WriteConfig("""
        {
          "idleColor": "#111111",
          "definitions": {
            "ollama": {
              "kind": "wslService", "processName": "ollama", "windowTitle": "Ollama WSL",
              "port": 11435, "wslCommand": "ollama serve", "portProxy": true, "requires": ["wsl"]
            },
            "wsl": { "kind": "wsl" }
          },
          "loadouts": [
            { "name": "AI", "toggle": true, "color": "#2E6BFF", "start": ["ollama"], "killOnDisable": ["ollama"] }
          ]
        }
        """);

        var result = ConfigLoader.Load(path);

        Assert.Empty(result.Warnings);
        Assert.Equal("#111111", result.Config.IdleColor);
        Assert.Equal("wslService", result.Config.Definitions["ollama"].Kind);
        Assert.Equal(11435, result.Config.Definitions["ollama"].Port);
        Assert.True(result.Config.Definitions["ollama"].PortProxy);
        Assert.Equal(new[] { "wsl" }, result.Config.Definitions["ollama"].Requires);
        var ai = Assert.Single(result.Config.Loadouts);
        Assert.Equal("AI", ai.Name);
        Assert.True(ai.Toggle);
        Assert.Equal("#2E6BFF", ai.Color);
        Assert.Equal(new[] { "ollama" }, ai.Start);
    }

    [Fact]
    public void MissingFileFallsBackToDefaultWithWarning()
    {
        var result = ConfigLoader.Load(Path.Combine(_dir, "nope.json"));

        Assert.Contains(result.Warnings, w => w.Contains("not found", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(3, result.Config.Loadouts.Count);
    }

    [Fact]
    public void MalformedJsonFallsBackToDefaultWithWarning()
    {
        var path = WriteConfig("{ this is not json ");

        var result = ConfigLoader.Load(path);

        Assert.NotEmpty(result.Warnings);
        Assert.Equal(3, result.Config.Loadouts.Count);
    }

    [Fact]
    public void UnknownDefinitionReferenceIsWarnedNotFatal()
    {
        var path = WriteConfig("""
        {
          "definitions": { "wsl": { "kind": "wsl" } },
          "loadouts": [ { "name": "Dev", "toggle": true, "start": ["wsl", "ghost"] } ]
        }
        """);

        var result = ConfigLoader.Load(path);

        Assert.Contains(result.Warnings, w => w.Contains("ghost"));
        Assert.Single(result.Config.Loadouts);
    }

    [Fact]
    public void DefaultConfigIsNativeWindowsOllama()
    {
        var cfg = ConfigLoader.Default();

        Assert.Equal(new[] { "AI", "Dev", "Game" }, cfg.Loadouts.Select(l => l.Name));
        Assert.True(cfg.Loadouts[0].Toggle);
        Assert.True(cfg.Loadouts[1].Toggle);
        Assert.False(cfg.Loadouts[2].Toggle);
        Assert.True(cfg.Loadouts[2].HardShutdown);
        Assert.True(cfg.Loadouts[2].FlushDns);

        var ollama = cfg.Definitions["ollama"];
        Assert.Equal("app", ollama.Kind);
        Assert.Equal("ollama app", ollama.ProcessName);
        Assert.Equal(@"%LOCALAPPDATA%\Programs\Ollama\ollama app.exe", ollama.Path);
        Assert.Null(ollama.Port);
        Assert.False(ollama.PortProxy);
        Assert.Empty(ollama.Requires);

        Assert.Equal("ollama", cfg.Definitions["ollamaServer"].ProcessName);
        Assert.Equal("llama-server", cfg.Definitions["ollamaRunner"].ProcessName);
        Assert.Equal(new[] { "ollama", "ollamaServer", "ollamaRunner" }, cfg.Loadouts[0].KillOnDisable);
        Assert.Equal(
            new[] { "node", "ollama", "ollamaRunner", "ollamaServer", "rider", "vscode", "webstorm", "wsl" },
            cfg.Definitions.Keys.OrderBy(k => k, StringComparer.Ordinal));
    }

    [Fact]
    public void SerializedDefaultRoundTrips()
    {
        var path = WriteConfig(ConfigLoader.ToJson(ConfigLoader.Default()));

        var result = ConfigLoader.Load(path);

        Assert.Empty(result.Warnings);
        AssertSameConfig(ConfigLoader.Default(), result.Config);
    }

    [Fact]
    public void ExampleFileMatchesTheBuiltInDefault()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "rigshift.example.json");

        var result = ConfigLoader.Load(path);

        Assert.Empty(result.Warnings);
        AssertSameConfig(ConfigLoader.Default(), result.Config);
    }

    [Fact]
    public void CandidatePathsAreUserConfigThenBesideTheExecutable()
    {
        var baseDirectory = Path.Combine(_dir, "exe");

        var candidates = ConfigLoader.CandidatePaths(baseDirectory);

        Assert.Equal(ConfigLoader.UserConfigPath(), candidates[0]);
        Assert.EndsWith(Path.Combine("Rigshift", "rigshift.json"), candidates[0]);
        Assert.Equal(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Rigshift", "rigshift.json"),
            candidates[0]);
        Assert.Equal(Path.Combine(baseDirectory, "rigshift.json"), candidates[1]);
    }

    [Fact]
    public void UserConfigWinsOverTheFileBesideTheExecutable()
    {
        var user = WriteNamed("user", """{ "idleColor": "#111111", "loadouts": [ { "name": "FromUser", "toggle": true } ] }""");
        var beside = WriteNamed("exe", """{ "idleColor": "#222222", "loadouts": [ { "name": "FromExe", "toggle": true } ] }""");

        var result = ConfigLoader.LoadFromCandidates(new[] { user, beside });

        Assert.Empty(result.Warnings);
        Assert.Equal("#111111", result.Config.IdleColor);
        Assert.Equal("FromUser", Assert.Single(result.Config.Loadouts).Name);
    }

    [Fact]
    public void FileBesideTheExecutableIsUsedWhenTheUserConfigIsMissing()
    {
        var user = Path.Combine(_dir, "absent-user", "rigshift.json");
        var beside = WriteNamed("exe-only", """{ "idleColor": "#222222", "loadouts": [ { "name": "FromExe", "toggle": true } ] }""");

        var result = ConfigLoader.LoadFromCandidates(new[] { user, beside });

        Assert.Empty(result.Warnings);
        Assert.Equal("FromExe", Assert.Single(result.Config.Loadouts).Name);
    }

    [Fact]
    public void MissingUserAndBesideExecutableUsesTheNativeDefault()
    {
        var result = ConfigLoader.LoadFromCandidates(new[]
        {
            Path.Combine(_dir, "no-user", "rigshift.json"),
            Path.Combine(_dir, "no-exe", "rigshift.json")
        });

        Assert.Empty(result.Warnings);
        Assert.Equal("app", result.Config.Definitions["ollama"].Kind);
        Assert.Equal(new[] { "AI", "Dev", "Game" }, result.Config.Loadouts.Select(l => l.Name));
    }

    [Fact]
    public void ABrokenUserConfigDoesNotFallThroughToTheFileBesideTheExecutable()
    {
        var user = WriteNamed("broken-user", "{ this is not json ");
        var beside = WriteNamed("valid-exe", """{ "idleColor": "#222222", "loadouts": [ { "name": "FromExe", "toggle": true } ] }""");

        var result = ConfigLoader.LoadFromCandidates(new[] { user, beside });

        Assert.NotEmpty(result.Warnings);
        Assert.Equal(new[] { "AI", "Dev", "Game" }, result.Config.Loadouts.Select(l => l.Name));
        Assert.DoesNotContain(result.Config.Loadouts, l => l.Name == "FromExe");
    }

    [Fact]
    public void ResolveExistingSkipsMissingFiles()
    {
        var missing = Path.Combine(_dir, "missing.json");
        var present = WriteNamed("present", "{}");

        Assert.Equal(present, ConfigLoader.ResolveExisting(new[] { missing, present }));
        Assert.Null(ConfigLoader.ResolveExisting(new[] { missing }));
    }

    private string WriteNamed(string folder, string json)
    {
        var dir = Path.Combine(_dir, folder);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "rigshift.json");
        File.WriteAllText(path, json);
        return path;
    }

    private static void AssertSameConfig(RigshiftConfig expected, RigshiftConfig actual)
    {
        Assert.Equal(expected.IdleColor, actual.IdleColor);
        Assert.Equal(
            expected.Definitions.Keys.OrderBy(k => k, StringComparer.Ordinal),
            actual.Definitions.Keys.OrderBy(k => k, StringComparer.Ordinal));
        foreach (var key in expected.Definitions.Keys)
        {
            var left = expected.Definitions[key];
            var right = actual.Definitions[key];
            Assert.Equal(left.Kind, right.Kind);
            Assert.Equal(left.ProcessName, right.ProcessName);
            Assert.Equal(left.Path, right.Path);
            Assert.Equal(left.WindowTitle, right.WindowTitle);
            Assert.Equal(left.Port, right.Port);
            Assert.Equal(left.WslCommand, right.WslCommand);
            Assert.Equal(left.PortProxy, right.PortProxy);
            Assert.Equal(left.Requires, right.Requires);
        }

        Assert.Equal(expected.Loadouts.Count, actual.Loadouts.Count);
        for (var i = 0; i < expected.Loadouts.Count; i++)
        {
            var left = expected.Loadouts[i];
            var right = actual.Loadouts[i];
            Assert.Equal(left.Name, right.Name);
            Assert.Equal(left.Toggle, right.Toggle);
            Assert.Equal(left.Color, right.Color);
            Assert.Equal(left.Start, right.Start);
            Assert.Equal(left.Kill, right.Kill);
            Assert.Equal(left.KillOnDisable, right.KillOnDisable);
            Assert.Equal(left.HardShutdown, right.HardShutdown);
            Assert.Equal(left.FlushDns, right.FlushDns);
        }
    }
}
