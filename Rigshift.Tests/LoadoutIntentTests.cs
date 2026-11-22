using Rigshift.Execution;

namespace Rigshift.Tests;

public class LoadoutIntentTests
{
    [Fact]
    public void StartsEmpty()
    {
        Assert.Empty(new LoadoutIntent().Enabled);
    }

    [Fact]
    public void EnableAddsName()
    {
        var intent = new LoadoutIntent();
        intent.Enable("Dev");

        Assert.Equal(new[] { "Dev" }, intent.Enabled);
    }

    [Fact]
    public void EnableTwiceDoesNotDuplicate()
    {
        var intent = new LoadoutIntent();
        intent.Enable("Dev");
        intent.Enable("Dev");

        Assert.Equal(new[] { "Dev" }, intent.Enabled);
    }

    [Fact]
    public void DisableRemovesName()
    {
        var intent = new LoadoutIntent();
        intent.Enable("Dev");
        intent.Enable("AI");
        intent.Disable("Dev");

        Assert.Equal(new[] { "AI" }, intent.Enabled);
    }

    [Fact]
    public void DisablingSomethingNotEnabledIsANoOp()
    {
        var intent = new LoadoutIntent();
        intent.Enable("AI");
        intent.Disable("Dev");

        Assert.Equal(new[] { "AI" }, intent.Enabled);
    }

    [Fact]
    public void ClearRemovesEverything()
    {
        var intent = new LoadoutIntent();
        intent.Enable("Dev");
        intent.Enable("AI");
        intent.Clear();

        Assert.Empty(intent.Enabled);
    }
}
