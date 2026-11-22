namespace Rigshift.Execution;

/// <summary>
/// Classifies each step as user or admin. Admin steps write system networking
/// (port proxies), stop a LocalSystem service, or flush the DNS cache. Everything
/// else runs as the user, including <see cref="StartWslService"/>, whose command text
/// comes from the user-writable config and must never run elevated.
/// </summary>
public static class StepClass
{
    public static bool RequiresElevation(Step step) => step switch
    {
        PortProxyAdd => true,
        PortProxyRemove => true,
        KillWslService => true,
        FlushDns => true,
        KillHerdr => true,
        StartProcess => false,
        KillProcess => false,
        CloseWindow => false,
        Wait => false,
        EnsureWsl => false,
        WslPkill => false,
        StartWslService => false,
        ShutdownWsl => false,
        null => throw new ArgumentNullException(nameof(step)),
        _ => throw new ArgumentOutOfRangeException(nameof(step), step.GetType().Name, "Unclassified step type.")
    };
}
