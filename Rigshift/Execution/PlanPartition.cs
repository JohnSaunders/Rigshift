namespace Rigshift.Execution;

/// <summary>
/// The result of splitting one ordered plan into its two execution slices.
/// <see cref="User"/> runs in the unelevated tray process, in planner order;
/// <see cref="Admin"/> runs in the elevated helper afterwards, in planner order.
/// The step instances are the same records the planner produced, not copies.
/// </summary>
public sealed record PartitionedPlan(IReadOnlyList<Step> User, IReadOnlyList<Step> Admin)
{
    public bool NeedsElevation => Admin.Count > 0;
}

/// <summary>
/// Splits an ordered plan into the user and admin slices.
///
/// This stable order — every user step first, then every admin step — is safe
/// only because no admin step in any current plan must run before a later user
/// step (elevation-trust design §3). A new step type that breaks that rule must
/// revisit design §3 before it is allowed to plan.
/// </summary>
public static class PlanPartition
{
    public static PartitionedPlan Split(IReadOnlyList<Step> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);

        var user = new List<Step>();
        var admin = new List<Step>();
        foreach (var step in steps)
        {
            if (StepClass.RequiresElevation(step))
                admin.Add(step);
            else
                user.Add(step);
        }

        return new PartitionedPlan(user, admin);
    }
}
