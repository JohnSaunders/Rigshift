namespace Rigshift.Settings;

/// <summary>
/// Add, duplicate, remove, and definition key rename operations for
/// <see cref="SettingsDraft"/>. Identity is ordinal on trimmed text; blank means null,
/// empty, or whitespace-only.
/// </summary>
public static class DraftListEditor
{
    /// <summary>Appends a new toggle loadout with the first free "New loadout" name and returns it.</summary>
    public static LoadoutDraft AddLoadout(SettingsDraft draft)
    {
        var row = new LoadoutDraft
        {
            Name = FirstFree("New loadout", LoadoutNames(draft, -1), number => $"New loadout {number}"),
            Toggle = true,
            Color = "#8A8A8E"
        };
        draft.Loadouts.Add(row);
        return row;
    }

    /// <summary>Inserts a copy of the loadout at <paramref name="index"/> right after it and returns the copy.</summary>
    public static LoadoutDraft DuplicateLoadout(SettingsDraft draft, int index)
    {
        var source = GetLoadout(draft, index);
        var seed = Blank(source.Name) ? "New loadout" : source.Name.Trim() + " copy";
        var name = FirstFree(seed, LoadoutNames(draft, index), number => $"{seed} {number}");
        var copy = new LoadoutDraft
        {
            Name = name,
            Toggle = source.Toggle,
            Color = source.Color,
            Start = new(source.Start),
            Kill = new(source.Kill),
            KillOnDisable = new(source.KillOnDisable),
            HardShutdown = source.HardShutdown,
            FlushDns = source.FlushDns
        };
        draft.Loadouts.Insert(index + 1, copy);
        return copy;
    }

    /// <summary>Removes the loadout at <paramref name="index"/>.</summary>
    public static void DeleteLoadout(SettingsDraft draft, int index)
    {
        GetLoadout(draft, index);
        draft.Loadouts.RemoveAt(index);
    }

    /// <summary>Appends a new app definition with the first free "newDefinition" key and returns it.</summary>
    public static DefinitionDraft AddDefinition(SettingsDraft draft)
    {
        var row = new DefinitionDraft
        {
            Key = FirstFree("newDefinition", DefinitionKeys(draft, -1), number => $"newDefinition{number}"),
            Kind = "app",
            ProcessName = "process"
        };
        draft.Definitions.Add(row);
        return row;
    }

    /// <summary>Inserts a copy of the definition at <paramref name="index"/> right after it and returns the copy.</summary>
    public static DefinitionDraft DuplicateDefinition(SettingsDraft draft, int index)
    {
        var source = GetDefinition(draft, index);
        var seed = Blank(source.Key) ? "newDefinition" : source.Key.Trim() + "Copy";
        var key = FirstFree(seed, DefinitionKeys(draft, index), number => $"{seed}{number}");
        var copy = new DefinitionDraft
        {
            Key = key,
            Kind = source.Kind,
            ProcessName = source.ProcessName,
            Path = source.Path,
            WindowTitle = source.WindowTitle,
            Port = source.Port,
            WslCommand = source.WslCommand,
            PortProxy = source.PortProxy,
            Requires = new(source.Requires)
        };
        draft.Definitions.Insert(index + 1, copy);
        return copy;
    }

    /// <summary>
    /// Removes the definition at <paramref name="index"/> when its key is blank or unreferenced and
    /// returns null. Otherwise leaves the draft unchanged and returns the message naming the
    /// loadouts and definitions that use the key.
    /// </summary>
    public static string? TryDeleteDefinition(SettingsDraft draft, int index)
    {
        var row = GetDefinition(draft, index);
        var key = row.Key.Trim();
        if (Blank(key))
        {
            draft.Definitions.RemoveAt(index);
            return null;
        }

        var places = new List<string>();
        for (var i = 0; i < draft.Loadouts.Count; i++)
        {
            var loadout = draft.Loadouts[i];
            if (ContainsKey(loadout.Start, key) || ContainsKey(loadout.Kill, key) || ContainsKey(loadout.KillOnDisable, key))
            {
                places.Add(DescribeLoadout(i, loadout.Name));
            }
        }
        for (var i = 0; i < draft.Definitions.Count; i++)
        {
            if (i == index)
                continue;
            if (ContainsKey(draft.Definitions[i].Requires, key))
                places.Add(DescribeDefinition(i, draft.Definitions[i].Key));
        }

        if (places.Count == 0)
        {
            draft.Definitions.RemoveAt(index);
            return null;
        }

        return $"Definition '{key}' is used by {string.Join(", ", places)}.";
    }

    /// <summary>
    /// Sets the definition's key to the trimmed <paramref name="newKey"/> and rewrites every
    /// reference that named the old key. Returns null on success, or an error message with the
    /// draft unchanged.
    /// </summary>
    public static string? TryRenameDefinitionKey(SettingsDraft draft, int index, string newKey)
    {
        var row = GetDefinition(draft, index);
        var proposed = newKey.Trim();
        if (row.Key == proposed)
            return null;
        if (Blank(proposed))
            return "Definition key is required.";

        for (var i = 0; i < draft.Definitions.Count; i++)
        {
            if (i == index)
                continue;
            if (draft.Definitions[i].Key.Trim() == proposed)
                return $"Definition key '{proposed}' is duplicated.";
        }

        var old = row.Key.Trim();
        row.Key = proposed;
        if (Blank(old))
            return null;

        foreach (var loadout in draft.Loadouts)
        {
            ReplaceKey(loadout.Start, old, proposed);
            ReplaceKey(loadout.Kill, old, proposed);
            ReplaceKey(loadout.KillOnDisable, old, proposed);
        }
        foreach (var definition in draft.Definitions)
        {
            ReplaceKey(definition.Requires, old, proposed);
        }
        return null;
    }

    private static string FirstFree(string seed, HashSet<string> taken, Func<int, string> numbered)
    {
        if (!taken.Contains(seed))
            return seed;
        for (var i = 2; ; i++)
        {
            var candidate = numbered(i);
            if (!taken.Contains(candidate))
                return candidate;
        }
    }

    private static HashSet<string> LoadoutNames(SettingsDraft draft, int excludeIndex)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < draft.Loadouts.Count; i++)
        {
            if (i == excludeIndex)
                continue;
            var name = draft.Loadouts[i].Name;
            if (!Blank(name))
                names.Add(name.Trim());
        }
        return names;
    }

    private static HashSet<string> DefinitionKeys(SettingsDraft draft, int excludeIndex)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < draft.Definitions.Count; i++)
        {
            if (i == excludeIndex)
                continue;
            var key = draft.Definitions[i].Key;
            if (!Blank(key))
                keys.Add(key.Trim());
        }
        return keys;
    }

    private static bool ContainsKey(List<string> list, string key)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i] is not null && list[i].Trim() == key)
                return true;
        }
        return false;
    }

    private static void ReplaceKey(List<string> list, string oldKey, string newKey)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i] is not null && list[i].Trim() == oldKey)
                list[i] = newKey;
        }
    }

    private static string DescribeLoadout(int index, string? name)
    {
        var trimmed = name?.Trim();
        return string.IsNullOrEmpty(trimmed)
            ? $"loadout at position {index + 1}"
            : $"loadout '{trimmed}'";
    }

    private static string DescribeDefinition(int index, string? key)
    {
        var trimmed = key?.Trim();
        return string.IsNullOrEmpty(trimmed)
            ? $"definition at position {index + 1}"
            : $"definition '{trimmed}'";
    }

    private static bool Blank(string? value)
        => value is null || value.Length == 0 || value.Trim().Length == 0;

    private static LoadoutDraft GetLoadout(SettingsDraft draft, int index)
    {
        if (index < 0 || index >= draft.Loadouts.Count)
            throw new ArgumentOutOfRangeException(nameof(index), index, "Loadout index is outside the list.");
        return draft.Loadouts[index];
    }

    private static DefinitionDraft GetDefinition(SettingsDraft draft, int index)
    {
        if (index < 0 || index >= draft.Definitions.Count)
            throw new ArgumentOutOfRangeException(nameof(index), index, "Definition index is outside the list.");
        return draft.Definitions[index];
    }
}
