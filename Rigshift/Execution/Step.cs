namespace Rigshift.Execution;

/// <summary>
/// A single OS-level action. Closed set by design: config can only ever compose
/// these, never supply an arbitrary command line.
/// </summary>
public abstract record Step;

public sealed record WslPkill(string Pattern) : Step;
public sealed record Wait(int Milliseconds) : Step;
public sealed record StartWslService(string WindowTitle, string WslCommand) : Step;

/// <summary>Port proxy steps carry only the port; the executor resolves the WSL IP.</summary>
public sealed record PortProxyAdd(int Port) : Step;
public sealed record PortProxyRemove(int Port) : Step;

public sealed record KillProcess(string ProcessName) : Step;
public sealed record CloseWindow(string WindowTitle) : Step;
public sealed record StartProcess(string Path) : Step;

public sealed record EnsureWsl : Step;
public sealed record ShutdownWsl : Step;
public sealed record KillWslService : Step;
public sealed record FlushDns : Step;

/// <summary>
/// Kills the current user's herdr.exe processes and their pane trees. The process
/// name is fixed here and never comes from config.
/// </summary>
public sealed record KillHerdr : Step;
