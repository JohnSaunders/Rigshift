using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Rigshift.Settings;

/// <summary>
/// Validation messages for a <see cref="SettingsDraft"/>. An empty list means the draft
/// can be saved. It never returns null, never mutates the draft, and never touches disk.
/// Comparison is ordinal on trimmed text, with the same hex pattern as
/// <see cref="Rigshift.Ui.TintSelector"/>.
/// </summary>
public static partial class SettingsValidator
{
    public static IReadOnlyList<string> Validate(SettingsDraft draft)
    {
        var errors = new List<string>();
        var knownKeys = KnownDefinitionKeys(draft.Definitions);

        // 1. Idle color.
        if (IsBlank(draft.IdleColor) || !IsHex(draft.IdleColor))
            errors.Add("Idle color must be #RRGGBB.");

        // 2/3. Loadouts, then duplicate loadout names.
        var nameFirstSeen = new List<string>();
        var nameCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < draft.Loadouts.Count; i++)
        {
            var loadout = draft.Loadouts[i];
            var position = i + 1;
            var name = loadout.Name?.Trim();
            var who = IsBlank(name) ? $"Loadout at position {position}" : $"Loadout '{name}'";

            if (IsBlank(name))
                errors.Add($"Loadout at position {position}: name is required.");

            if (!IsBlank(name) && (name!.Contains(',') || name.Contains('"')))
                errors.Add($"Loadout '{name}': name cannot contain a comma or a double quote.");

            var color = loadout.Color?.Trim();
            var colorBlank = IsBlank(color);
            if (loadout.Toggle && colorBlank)
                errors.Add($"{who}: color is required when toggle is on.");
            else if (!colorBlank && !IsHex(loadout.Color))
                errors.Add($"{who}: color must be #RRGGBB.");

            ValidateReferenceList(loadout.Start, who, "start", knownKeys, errors);
            ValidateReferenceList(loadout.Kill, who, "kill", knownKeys, errors);
            ValidateReferenceList(loadout.KillOnDisable, who, "killOnDisable", knownKeys, errors);

            if (!IsBlank(name))
            {
                if (!nameCounts.ContainsKey(name))
                {
                    nameCounts[name] = 0;
                    nameFirstSeen.Add(name);
                }
                nameCounts[name]++;
            }
        }

        foreach (var name in nameFirstSeen)
        {
            if (nameCounts[name] > 1)
                errors.Add($"Loadout name '{name}' is duplicated.");
        }

        // 4/5. Definitions, then duplicate definition keys.
        var keyFirstSeen = new List<string>();
        var keyCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < draft.Definitions.Count; i++)
        {
            var def = draft.Definitions[i];
            var position = i + 1;
            var key = def.Key?.Trim();
            var who = IsBlank(key) ? $"Definition at position {position}" : $"Definition '{key}'";
            var kind = def.Kind?.Trim();

            if (IsBlank(key))
                errors.Add($"Definition at position {position}: key is required.");

            if (kind is not ("app" or "wsl" or "wslService" or "herdr"))
                errors.Add($"{who}: kind must be app, wsl, wslService, or herdr.");

            if ((kind == "app" || kind == "wslService" || kind == "herdr") && IsBlank(def.ProcessName))
                errors.Add($"{who}: process name is required.");

            var portBlank = IsBlank(def.Port);
            var portValid = !portBlank && TryPort(def.Port, out _);
            var portRangeEmitted = false;
            if (!portBlank && !portValid)
            {
                errors.Add($"{who}: port must be an integer from 1 to 65535.");
                portRangeEmitted = true;
            }
            if (def.PortProxy && (portBlank || !portValid) && !portRangeEmitted)
                errors.Add($"{who}: port is required when port proxy is on.");
            if (def.PortProxy && kind != "wslService")
                errors.Add($"{who}: port proxy applies to wslService only.");

            ValidateReferenceList(def.Requires, who, "requires", knownKeys, errors);

            if (!IsBlank(key))
            {
                if (!keyCounts.ContainsKey(key))
                {
                    keyCounts[key] = 0;
                    keyFirstSeen.Add(key);
                }
                keyCounts[key]++;
            }
        }

        foreach (var key in keyFirstSeen)
        {
            if (keyCounts[key] > 1)
                errors.Add($"Definition key '{key}' is duplicated.");
        }

        // 6. Require-cycles.
        FindCycles(draft.Definitions, errors);

        return errors;
    }

    /// <summary>The set of distinct, non-blank trimmed definition keys.</summary>
    private static HashSet<string> KnownDefinitionKeys(List<DefinitionDraft> definitions)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var def in definitions)
        {
            var key = def.Key?.Trim();
            if (!IsBlank(key))
                keys.Add(key);
        }
        return keys;
    }

    /// <summary>Blank entry, then duplicates (first-seen), then unknown (first-seen).</summary>
    private static void ValidateReferenceList(
        List<string>? entries,
        string who,
        string listName,
        HashSet<string> knownKeys,
        List<string> errors)
    {
        if (entries is null)
            return;

        var hasBlank = false;
        var firstSeen = new List<string>();
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            var trimmed = entry?.Trim();
            if (IsBlank(trimmed))
            {
                hasBlank = true;
                continue;
            }
            if (!counts.ContainsKey(trimmed))
            {
                counts[trimmed] = 0;
                firstSeen.Add(trimmed);
            }
            counts[trimmed]++;
        }

        if (hasBlank)
            errors.Add($"{who}: {listName} contains a blank entry.");

        foreach (var value in firstSeen)
        {
            if (counts[value] > 1)
                errors.Add($"{who}: {listName} lists '{value}' more than once.");
        }

        foreach (var value in firstSeen)
        {
            if (!knownKeys.Contains(value))
                errors.Add($"{who}: {listName} references unknown definition '{value}'.");
        }
    }

    /// <summary>
    /// Depth-first walk over definitions with a non-blank trimmed key (first occurrence
    /// wins). A <see cref="DefinitionDraft.Requires"/> edge whose target is already on the
    /// visit stack is a cycle, reported once and not followed.
    /// </summary>
    private static void FindCycles(List<DefinitionDraft> definitions, List<string> errors)
    {
        var nodes = new List<(string Key, DefinitionDraft Row)>();
        var nodeSet = new HashSet<string>(StringComparer.Ordinal);
        foreach (var def in definitions)
        {
            var key = def.Key?.Trim();
            if (IsBlank(key) || !nodeSet.Add(key))
                continue; // blank, or a repeated trimmed key: keep the first
            nodes.Add((key, def));
        }

        var adjacency = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var (key, row) in nodes)
        {
            var edges = new List<string>();
            foreach (var entry in row.Requires ?? new List<string>())
            {
                var target = entry?.Trim();
                if (!IsBlank(target) && nodeSet.Contains(target))
                    edges.Add(target);
            }
            adjacency[key] = edges;
        }

        var done = new HashSet<string>(StringComparer.Ordinal);
        var stack = new List<string>();

        void Visit(string key)
        {
            if (done.Contains(key))
                return;

            stack.Add(key);
            foreach (var edge in adjacency[key])
            {
                var index = stack.IndexOf(edge);
                if (index >= 0)
                {
                    var parts = new List<string>(stack.Count - index + 1);
                    for (var j = index; j < stack.Count; j++)
                        parts.Add(stack[j]);
                    parts.Add(edge);
                    var path = string.Join(" -> ", parts);
                    errors.Add($"Definition '{edge}': requires contains a cycle ({path}).");
                    continue; // do not follow a back edge
                }

                Visit(edge);
            }

            stack.RemoveAt(stack.Count - 1);
            done.Add(key);
        }

        foreach (var (key, _) in nodes)
        {
            if (!done.Contains(key))
                Visit(key);
        }
    }

    private static bool IsBlank([NotNullWhen(false)] string? value) =>
        string.IsNullOrWhiteSpace(value);

    private static bool IsHex(string? value) =>
        value is not null && HexPattern().IsMatch(value.Trim());

    private static bool TryPort(string? raw, out int port)
    {
        var trimmed = raw?.Trim();
        if (trimmed is null || trimmed.Length == 0)
        {
            port = 0;
            return false;
        }
        if (!int.TryParse(trimmed, NumberStyles.None, CultureInfo.InvariantCulture, out port))
            return false;
        return port is >= 1 and <= 65535;
    }

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex HexPattern();
}
