using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rigshift.Config;

public sealed record ConfigLoadResult(RigshiftConfig Config, IReadOnlyList<string> Warnings);

public static class ConfigLoader
{
    public const string FileName = "rigshift.json";
    public const string ExampleFileName = "rigshift.example.json";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Per-user config: <c>%APPDATA%\Rigshift\rigshift.json</c>
    /// (<see cref="Environment.SpecialFolder.ApplicationData"/>).
    /// </summary>
    public static string UserConfigPath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Rigshift",
            FileName);

    public static string BesideExecutablePath(string baseDirectory) =>
        Path.Combine(baseDirectory, FileName);

    /// <summary>
    /// Files consulted, in order. The first one that exists is the config.
    /// When none exist, <see cref="LoadFromCandidates"/> uses <see cref="Default"/>.
    /// </summary>
    public static IReadOnlyList<string> CandidatePaths(string baseDirectory) =>
        new[] { UserConfigPath(), BesideExecutablePath(baseDirectory) };

    public static IReadOnlyList<string> CandidatePaths() =>
        CandidatePaths(AppContext.BaseDirectory);

    /// <summary>The first candidate that exists on disk, or null.</summary>
    public static string? ResolveExisting(IEnumerable<string> candidates)
    {
        foreach (var path in candidates)
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                return path;
        }
        return null;
    }

    /// <summary>
    /// Loads the first existing candidate. A missing file is skipped. The first
    /// existing file is used even when it cannot be parsed — lookup does not
    /// continue to a later file. When every candidate is missing, the built-in
    /// default is returned with no warning.
    /// </summary>
    public static ConfigLoadResult LoadFromCandidates(IReadOnlyList<string> candidates)
    {
        var path = ResolveExisting(candidates);
        return path is null ? new ConfigLoadResult(Default(), Array.Empty<string>()) : Load(path);
    }

    public static string ToJson(RigshiftConfig config) =>
        JsonSerializer.Serialize(config, WriteOptions);

    public static ConfigLoadResult Load(string path)
    {
        if (!File.Exists(path))
            return new ConfigLoadResult(Default(), new[] { $"Config not found at {path}; using defaults." });

        RigshiftConfig config;
        try
        {
            config = JsonSerializer.Deserialize<RigshiftConfig>(File.ReadAllText(path), Options)
                     ?? throw new JsonException("Config deserialized to null.");
        }
        // Broad by design: Load runs from ContextMenuStrip.Opening and from the poll
        // tick, so anything that escapes here (UnauthorizedAccessException, etc.)
        // becomes a WinForms crash dialog every 30 seconds and a menu that never opens.
        catch (Exception ex)
        {
            return new ConfigLoadResult(Default(), new[] { $"Could not read {path}: {ex.Message}. Using defaults." });
        }

        return new ConfigLoadResult(config, Validate(config));
    }

    /// <summary>Names every loadout reference that has no matching definition.</summary>
    private static List<string> Validate(RigshiftConfig config)
    {
        var warnings = new List<string>();
        foreach (var loadout in config.Loadouts)
        {
            foreach (var key in loadout.Start.Concat(loadout.Kill).Concat(loadout.KillOnDisable).Distinct())
            {
                if (!config.Definitions.ContainsKey(key))
                    warnings.Add($"Loadout '{loadout.Name}' references unknown definition '{key}'.");
            }
        }
        return warnings;
    }

    /// <summary>
    /// Native Windows Ollama, a WSL dev session, and a game purge.
    /// Same loadouts as <c>rigshift.example.json</c>.
    /// </summary>
    public static RigshiftConfig Default() => new()
    {
        IdleColor = "#8A8A8E",
        Definitions = new Dictionary<string, Definition>
        {
            ["ollama"] = new()
            {
                Kind = "app",
                ProcessName = "ollama app",
                Path = @"%LOCALAPPDATA%\Programs\Ollama\ollama app.exe"
            },
            ["ollamaServer"] = new() { Kind = "app", ProcessName = "ollama" },
            ["ollamaRunner"] = new() { Kind = "app", ProcessName = "llama-server" },
            ["wsl"] = new() { Kind = "wsl" },
            ["rider"] = new() { Kind = "app", ProcessName = "rider64" },
            ["webstorm"] = new() { Kind = "app", ProcessName = "webstorm64" },
            ["vscode"] = new() { Kind = "app", ProcessName = "Code" },
            ["node"] = new() { Kind = "app", ProcessName = "node" }
        },
        Loadouts = new List<Loadout>
        {
            new()
            {
                Name = "AI", Toggle = true, Color = "#2E6BFF",
                Start = new List<string> { "ollama" },
                KillOnDisable = new List<string> { "ollama", "ollamaServer", "ollamaRunner" }
            },
            new()
            {
                Name = "Dev", Toggle = true, Color = "#FF3B30",
                Start = new List<string> { "wsl" },
                KillOnDisable = new List<string> { "wsl" }
            },
            new()
            {
                Name = "Game", Toggle = false,
                // Deliberately leaves Rider, WebStorm and VS Code alone: those hold
                // unsaved work, and a one-shot from the tray must not end it. What is
                // freed here is Node and the local-model stack.
                Kill = new List<string>
                {
                    "node", "ollama", "ollamaServer", "ollamaRunner", "wsl"
                },
                HardShutdown = true,
                FlushDns = true
            }
        }
    };
}
