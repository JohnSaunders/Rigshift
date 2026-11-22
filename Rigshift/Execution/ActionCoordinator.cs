namespace Rigshift.Execution;

/// <summary>
/// The two facts an action left behind, in the shape the intent decision wants.
/// <see cref="UserStepErrors"/> is whatever the user slice reported, read once
/// after it finished. <see cref="Elevation"/> is the admin slice's outcome, or
/// null when the admin slice was empty and nothing was launched.
/// </summary>
public sealed record ActionOutcome(IReadOnlyList<string> UserStepErrors, ElevationResult? Elevation)
{
    public bool AdminAttempted => Elevation is not null;
}

/// <summary>
/// Runs one action as two slices: every user step through the injected executor,
/// in order, then the admin slice through the injected launcher — but only when
/// the admin slice is not empty (no UAC prompt, no helper otherwise).
///
/// It makes no intent change and shows no UI; elevation-trust/009 applies the
/// result of <see cref="IntentDecision.Decide"/> on the UI thread.
/// elevation-trust design §5.
/// </summary>
public sealed class ActionCoordinator
{
    public async Task<ActionOutcome> RunAsync(
        PartitionedPlan plan,
        IStepExecutor userExecutor,
        Func<IReadOnlyList<string>> getUserErrors,
        IElevationLauncher admin,
        TimeSpan adminTimeout,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(userExecutor);
        ArgumentNullException.ThrowIfNull(getUserErrors);
        ArgumentNullException.ThrowIfNull(admin);

        // Every step is attempted, even after an earlier one produced an error:
        // the executor records its errors and the caller reads them once below.
        foreach (var step in plan.User)
            userExecutor.Execute(step);

        var userErrors = getUserErrors();

        if (!plan.NeedsElevation)
            return new ActionOutcome(userErrors, null);

        var elevation = await admin.RunAsync(plan.Admin, adminTimeout, cancellationToken).ConfigureAwait(false);
        return new ActionOutcome(userErrors, elevation);
    }
}
