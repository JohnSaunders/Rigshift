using System.Runtime.ExceptionServices;

namespace Rigshift.Tests;

/// <summary>
/// Runs an action on an STA thread and joins it, rethrowing any failure through
/// <see cref="ExceptionDispatchInfo"/> so the original stack is kept.
/// </summary>
public static class StaThread
{
    public static void Run(Action action)
    {
        ExceptionDispatchInfo? failure = null;

        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                failure = ExceptionDispatchInfo.Capture(ex);
            }
        })
        {
            IsBackground = true
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        failure?.Throw();
    }
}
