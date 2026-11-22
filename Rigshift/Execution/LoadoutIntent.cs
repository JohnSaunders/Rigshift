namespace Rigshift.Execution;

/// <summary>
/// Which toggle loadouts the user has turned on, this session. Session-scoped by
/// design: nothing here is persisted, so a Rigshift restart resets everything to off.
/// </summary>
public sealed class LoadoutIntent
{
    private readonly HashSet<string> _enabled = new(StringComparer.Ordinal);

    public IReadOnlyList<string> Enabled => _enabled.ToList();

    public void Enable(string name) => _enabled.Add(name);
    public void Disable(string name) => _enabled.Remove(name);
    public void Clear() => _enabled.Clear();
}
