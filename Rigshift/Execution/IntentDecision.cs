namespace Rigshift.Execution;

/// <summary>Which action produced the outcome, so the balloon can name it.</summary>
public enum ActionKind { Enable, Disable, OneShot }

/// <summary>
/// The intent change an action outcome implies: <see cref="Apply"/> is whether
/// the switch intent moves, and <see cref="Balloon"/> is the user-facing
/// summary, or null for none.
/// </summary>
public sealed record IntentPlan(bool Apply, string? Balloon);

/// <summary>
/// Pure rule that turns an action's outcome into an intent change plus a balloon.
/// The user slice always runs first and cannot be declined, so intent follows
/// what was done to the user's programs; admin problems are reported, not rolled
/// back. <paramref name="adminSliceWasEmpty"/> and <paramref name="elevation"/>
/// are consistent: when the admin slice is empty, the elevation is null.
/// <paramref name="userSliceWasEmpty"/> is <c>plan.User.Count == 0</c>.
/// elevation-trust design §6.
/// </summary>
public static class IntentDecision
{
    private const string InternalError = "The administrator helper stopped with an internal error.";
    private const string StartFailedMessage = "The administrator helper could not be started.";
    private const string TimedOutMessage = "Administrator steps are still running in the background.";

    public static IntentPlan Decide(
        ActionKind kind,
        bool adminSliceWasEmpty,
        bool userSliceWasEmpty,
        ElevationResult? elevation)
    {
        if (adminSliceWasEmpty || elevation is null)
            return new IntentPlan(true, null);

        return elevation switch
        {
            ElevationResult.Succeeded => new IntentPlan(true, null),
            ElevationResult.Failed { ExitCode: 2 } => new IntentPlan(true, null),
            ElevationResult.TimedOut => new IntentPlan(true, TimedOutMessage),
            // Declined and helper failures: intent moves only when the user
            // slice ran; otherwise the action is unchanged (and a declined
            // prompt already announced itself).
            ElevationResult.Declined => new IntentPlan(
                !userSliceWasEmpty,
                userSliceWasEmpty ? null : $"Approval was declined; the Administrator part of {kind} was not done."),
            ElevationResult.Failed => new IntentPlan(!userSliceWasEmpty, InternalError),
            ElevationResult.StartFailed => new IntentPlan(!userSliceWasEmpty, StartFailedMessage),
            _ => throw new InvalidOperationException($"Unrecognized ElevationResult: {elevation.GetType().Name}")
        };
    }
}
