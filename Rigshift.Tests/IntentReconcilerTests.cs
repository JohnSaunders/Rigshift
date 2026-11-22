using Rigshift.Config;
using Rigshift.Settings;

namespace Rigshift.Tests;

public class IntentReconcilerTests
{
    [Fact]
    public void KeepsToggleNamesFromTheDefaultConfigInOrder()
    {
        var result = IntentReconciler.Reconcile(new[] { "AI", "Dev" }, "Dev", ConfigLoader.Default());

        Assert.Equal(new[] { "AI", "Dev" }, result.Enabled);
        Assert.Equal("Dev", result.LastEnabled);
    }

    [Fact]
    public void DropsDeletedOneShotBlankAndRepeatedNames()
    {
        var result = IntentReconciler.Reconcile(
            new[] { "Missing", "Game", "   ", "AI", "AI" }, "Game", ConfigLoader.Default());

        Assert.Equal(new[] { "AI" }, result.Enabled);
        Assert.Null(result.LastEnabled);
    }

    [Fact]
    public void KeepsLastEnabledThatSurvivesReconciliation()
    {
        var result = IntentReconciler.Reconcile(
            new[] { "Missing", "Game", "   ", "AI", "AI" }, "AI", ConfigLoader.Default());

        Assert.Equal(new[] { "AI" }, result.Enabled);
        Assert.Equal("AI", result.LastEnabled);
    }

    [Fact]
    public void EmptyEnabledReturnsEmptyListAndNullLastEnabled()
    {
        var result = IntentReconciler.Reconcile(Array.Empty<string>(), "AI", ConfigLoader.Default());

        Assert.Empty(result.Enabled);
        Assert.Null(result.LastEnabled);
    }

    [Fact]
    public void LeavesTheInputCollectionAndConfigUntouched()
    {
        var enabled = new List<string> { "AI", "Dev" };
        var config = ConfigLoader.Default();

        var result = IntentReconciler.Reconcile(enabled, "Dev", config);

        Assert.Equal(new[] { "AI", "Dev" }, enabled);
        Assert.Equal(3, config.Loadouts.Count);
        Assert.NotSame(enabled, result.Enabled);
    }

    [Fact]
    public void KeepsTheNameWhenAnyLoadoutWithItIsAToggle()
    {
        var config = new RigshiftConfig
        {
            Loadouts = new List<Loadout>
            {
                new() { Name = "Mix", Toggle = false },
                new() { Name = "Mix", Toggle = true }
            }
        };

        var result = IntentReconciler.Reconcile(new[] { "Mix" }, "Mix", config);

        Assert.Equal(new[] { "Mix" }, result.Enabled);
        Assert.Equal("Mix", result.LastEnabled);
    }

    [Fact]
    public void DropsANameThatOnlyMatchesOneShotLoadouts()
    {
        var config = new RigshiftConfig
        {
            Loadouts = new List<Loadout>
            {
                new() { Name = "Oncer", Toggle = false }
            }
        };

        var result = IntentReconciler.Reconcile(new[] { "Oncer" }, "Oncer", config);

        Assert.Empty(result.Enabled);
        Assert.Null(result.LastEnabled);
    }

    [Fact]
    public void ComparesNamesOrdinalAndWithoutTrimming()
    {
        var config = new RigshiftConfig
        {
            Loadouts = new List<Loadout>
            {
                new() { Name = " AI", Toggle = true }
            }
        };

        var result = IntentReconciler.Reconcile(new[] { "AI " }, "AI ", config);

        Assert.Empty(result.Enabled);
        Assert.Null(result.LastEnabled);
    }
}
