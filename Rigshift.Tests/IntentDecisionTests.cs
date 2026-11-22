using Rigshift.Execution;

namespace Rigshift.Tests;

public class IntentDecisionTests
{
    // One fact per row of the elevation-trust design §6 table, for Enable.

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EmptyAdminSliceAppliesWithoutBalloon(bool userSliceWasEmpty)
    {
        Assert.Equal(
            new IntentPlan(true, null),
            IntentDecision.Decide(ActionKind.Enable, true, userSliceWasEmpty, null));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SucceededAppliesWithoutBalloon(bool userSliceWasEmpty)
    {
        Assert.Equal(
            new IntentPlan(true, null),
            IntentDecision.Decide(ActionKind.Enable, false, userSliceWasEmpty, new ElevationResult.Succeeded()));
    }

    [Fact]
    public void DeclinedWithoutUserSliceDoesNotApplyAndStaysQuiet()
    {
        Assert.Equal(
            new IntentPlan(false, null),
            IntentDecision.Decide(ActionKind.Enable, false, true, new ElevationResult.Declined()));
    }

    [Fact]
    public void DeclinedAfterUserSliceRanAppliesWithDeclinedBalloon()
    {
        var plan = IntentDecision.Decide(ActionKind.Enable, false, false, new ElevationResult.Declined());

        Assert.True(plan.Apply);
        Assert.Equal("Approval was declined; the Administrator part of Enable was not done.", plan.Balloon);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FailedWithStepErrorsAppliesWithoutBalloon(bool userSliceWasEmpty)
    {
        Assert.Equal(
            new IntentPlan(true, null),
            IntentDecision.Decide(ActionKind.Enable, false, userSliceWasEmpty, new ElevationResult.Failed(2)));
    }

    [Fact]
    public void FailedWithOtherExitWithoutUserSliceDoesNotApplyWithInternalErrorBalloon()
    {
        Assert.Equal(
            new IntentPlan(false, "The administrator helper stopped with an internal error."),
            IntentDecision.Decide(ActionKind.Enable, false, true, new ElevationResult.Failed(1)));
    }

    [Fact]
    public void FailedWithOtherExitAfterUserSliceAppliesWithInternalErrorBalloon()
    {
        Assert.Equal(
            new IntentPlan(true, "The administrator helper stopped with an internal error."),
            IntentDecision.Decide(ActionKind.Enable, false, false, new ElevationResult.Failed(1)));
    }

    [Fact]
    public void StartFailedWithoutUserSliceDoesNotApplyWithStartFailedBalloon()
    {
        Assert.Equal(
            new IntentPlan(false, "The administrator helper could not be started."),
            IntentDecision.Decide(ActionKind.Enable, false, true, new ElevationResult.StartFailed("boom")));
    }

    [Fact]
    public void StartFailedAfterUserSliceAppliesWithStartFailedBalloon()
    {
        Assert.Equal(
            new IntentPlan(true, "The administrator helper could not be started."),
            IntentDecision.Decide(ActionKind.Enable, false, false, new ElevationResult.StartFailed("boom")));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TimedOutAppliesWithBackgroundBalloon(bool userSliceWasEmpty)
    {
        Assert.Equal(
            new IntentPlan(true, "Administrator steps are still running in the background."),
            IntentDecision.Decide(ActionKind.Enable, false, userSliceWasEmpty, new ElevationResult.TimedOut()));
    }

    // Disable and OneShot match Enable on every row whose balloon is null.

    [Theory]
    [InlineData(ActionKind.Disable)]
    [InlineData(ActionKind.OneShot)]
    public void DisableAndOneShotMatchEnableOnEveryRowWithNullBalloon(ActionKind kind)
    {
        Assert.Equal(
            new IntentPlan(true, null),
            IntentDecision.Decide(kind, true, false, null));
        Assert.Equal(
            new IntentPlan(true, null),
            IntentDecision.Decide(kind, false, true, new ElevationResult.Succeeded()));
        Assert.Equal(
            new IntentPlan(false, null),
            IntentDecision.Decide(kind, false, true, new ElevationResult.Declined()));
        Assert.Equal(
            new IntentPlan(true, null),
            IntentDecision.Decide(kind, false, false, new ElevationResult.Failed(2)));
    }

    // Every kind is named in the declined-after-user-ran balloon.

    [Theory]
    [InlineData(ActionKind.Enable, "Enable")]
    [InlineData(ActionKind.Disable, "Disable")]
    [InlineData(ActionKind.OneShot, "OneShot")]
    public void DeclinedAfterUserSliceRanNamesTheKind(ActionKind kind, string kindName)
    {
        var plan = IntentDecision.Decide(kind, false, false, new ElevationResult.Declined());

        Assert.True(plan.Apply);
        Assert.Equal($"Approval was declined; the Administrator part of {kindName} was not done.", plan.Balloon);
    }
}
