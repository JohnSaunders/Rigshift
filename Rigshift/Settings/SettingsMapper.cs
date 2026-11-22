using System.Globalization;
using Rigshift.Config;

namespace Rigshift.Settings;

/// <summary>
/// Copies a <see cref="RigshiftConfig"/> into a <see cref="SettingsDraft"/> and back.
/// Every list is copied, so editing the draft cannot change the config it came from,
/// and <see cref="ToConfig"/> never mutates the draft.
/// </summary>
public sealed class SettingsMapper
{
    private SettingsMapper()
    {
    }

    /// <summary>
    /// Maps a config to a fresh draft. Definitions follow the dictionary's order and
    /// loadouts keep list order. A null <see cref="Definition.Port"/> stays null.
    /// </summary>
    public static SettingsDraft FromConfig(RigshiftConfig config)
    {
        var draft = new SettingsDraft { IdleColor = config.IdleColor };
        foreach (var (key, definition) in config.Definitions)
        {
            draft.Definitions.Add(new DefinitionDraft
            {
                Key = key,
                Kind = definition.Kind,
                ProcessName = definition.ProcessName,
                Path = definition.Path,
                WindowTitle = definition.WindowTitle,
                Port = definition.Port?.ToString(CultureInfo.InvariantCulture),
                WslCommand = definition.WslCommand,
                PortProxy = definition.PortProxy,
                Requires = Copy(definition.Requires)
            });
        }
        foreach (var loadout in config.Loadouts)
        {
            draft.Loadouts.Add(new LoadoutDraft
            {
                Name = loadout.Name,
                Toggle = loadout.Toggle,
                Color = loadout.Color,
                Start = Copy(loadout.Start),
                Kill = Copy(loadout.Kill),
                KillOnDisable = Copy(loadout.KillOnDisable),
                HardShutdown = loadout.HardShutdown,
                FlushDns = loadout.FlushDns
            });
        }
        return draft;
    }

    /// <summary>
    /// Builds a fresh config from a draft without touching it. Trimmed keys, names,
    /// kinds, and reference entries are stored trimmed; blank optional strings become
    /// null; a blank port becomes null and any other port must parse as an integer.
    /// Throws <see cref="InvalidOperationException"/> for duplicate trimmed definition
    /// keys or an unparseable port. Definition order follows the draft list.
    /// </summary>
    public static RigshiftConfig ToConfig(SettingsDraft draft)
    {
        var config = new RigshiftConfig
        {
            IdleColor = draft.IdleColor?.Trim() ?? ""
        };

        var seenKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in draft.Definitions)
        {
            var key = row.Key?.Trim() ?? "";
            if (!seenKeys.Add(key))
                throw new InvalidOperationException($"Definition key '{key}' is duplicated.");
            config.Definitions[key] = new Definition
            {
                Kind = row.Kind?.Trim() ?? "",
                ProcessName = ToOptional(row.ProcessName),
                Path = ToOptional(row.Path),
                WindowTitle = ToOptional(row.WindowTitle),
                Port = ToPort(row.Port),
                WslCommand = ToOptional(row.WslCommand),
                PortProxy = row.PortProxy,
                Requires = ToReferences(row.Requires)
            };
        }

        foreach (var row in draft.Loadouts)
        {
            config.Loadouts.Add(new Loadout
            {
                Name = row.Name?.Trim() ?? "",
                Toggle = row.Toggle,
                Color = ToOptional(row.Color),
                Start = ToReferences(row.Start),
                Kill = ToReferences(row.Kill),
                KillOnDisable = ToReferences(row.KillOnDisable),
                HardShutdown = row.HardShutdown,
                FlushDns = row.FlushDns
            });
        }

        return config;
    }

    /// <summary>A new list with the same entries; a null source becomes an empty list.</summary>
    private static List<string> Copy(List<string>? source) =>
        source is null ? new List<string>() : new List<string>(source);

    /// <summary>Trims an optional string; blank (null, empty, whitespace) becomes null.</summary>
    private static string? ToOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    /// <summary>New list of trimmed entries; blank entries are omitted.</summary>
    private static List<string> ToReferences(List<string>? source)
    {
        var result = new List<string>();
        if (source is null)
            return result;
        foreach (var entry in source)
        {
            var trimmed = entry?.Trim();
            if (!string.IsNullOrEmpty(trimmed))
                result.Add(trimmed);
        }
        return result;
    }

    /// <summary>Blank port text becomes null; anything else must parse as an invariant integer.</summary>
    private static int? ToPort(string? raw)
    {
        var trimmed = raw?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;
        if (!int.TryParse(trimmed, NumberStyles.None, CultureInfo.InvariantCulture, out var port))
            throw new InvalidOperationException($"Port '{raw}' is not a valid integer.");
        return port;
    }
}
