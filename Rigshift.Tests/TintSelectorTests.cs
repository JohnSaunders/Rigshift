using Rigshift.Config;
using Rigshift.Ui;

namespace Rigshift.Tests;

public class TintSelectorTests
{
    private static RigshiftConfig Config() => ConfigLoader.Default();

    [Fact]
    public void NothingEnabledReturnsIdleColor()
    {
        Assert.Equal("#8A8A8E", TintSelector.SelectHex(Config(), Array.Empty<string>(), null));
    }

    [Fact]
    public void SingleEnabledLoadoutReturnsItsOwnColor()
    {
        Assert.Equal("#2E6BFF", TintSelector.SelectHex(Config(), new[] { "AI" }, "AI"));
    }

    [Fact]
    public void LastEnabledWinsWhenTwoAreActive()
    {
        Assert.Equal("#FF3B30", TintSelector.SelectHex(Config(), new[] { "AI", "Dev" }, "Dev"));
        Assert.Equal("#2E6BFF", TintSelector.SelectHex(Config(), new[] { "AI", "Dev" }, "AI"));
    }

    [Fact]
    public void ColourFallsBackToTheOtherLoadoutWhenTheOwnerIsNoLongerEnabled()
    {
        // Dev was last enabled, then disabled; AI is still on.
        Assert.Equal("#2E6BFF", TintSelector.SelectHex(Config(), new[] { "AI" }, "Dev"));
    }

    [Fact]
    public void ColdStartWithTwoActiveFallsBackToConfigOrderLastListed()
    {
        Assert.Equal("#FF3B30", TintSelector.SelectHex(Config(), new[] { "AI", "Dev" }, null));
    }

    [Fact]
    public void MissingColourFallsBackToIdle()
    {
        var config = new RigshiftConfig
        {
            IdleColor = "#123456",
            Loadouts = { new Loadout { Name = "X", Toggle = true, Color = null } }
        };

        Assert.Equal("#123456", TintSelector.SelectHex(config, new[] { "X" }, "X"));
    }

    [Theory]
    [InlineData("not-a-colour")]
    [InlineData("#12345")]
    [InlineData("")]
    public void MalformedColourFallsBackToIdle(string bad)
    {
        var config = new RigshiftConfig
        {
            IdleColor = "#123456",
            Loadouts = { new Loadout { Name = "X", Toggle = true, Color = bad } }
        };

        Assert.Equal("#123456", TintSelector.SelectHex(config, new[] { "X" }, "X"));
    }
}
