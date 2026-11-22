using Rigshift.Ui;

namespace Rigshift.Tests;

public class TrayWarningTests
{
    [Fact]
    public void TheSameWarningListNotifiesOnceAndThenStaysSilent()
    {
        var notifier = new WarningNotifier();
        Assert.True(notifier.ShouldNotify(new[] { "unknown definition 'x'" }));
        Assert.False(notifier.ShouldNotify(new[] { "unknown definition 'x'" }));
    }

    [Fact]
    public void AnEmptyWarningListNeverNotifies()
    {
        var notifier = new WarningNotifier();
        Assert.False(notifier.ShouldNotify(Array.Empty<string>()));
        Assert.False(notifier.ShouldNotify(Array.Empty<string>()));
    }

    [Fact]
    public void AListOfOnlyBlankEntriesNeverNotifies()
    {
        var notifier = new WarningNotifier();
        Assert.False(notifier.ShouldNotify(new[] { "", "   ", "\t" }));
    }

    [Fact]
    public void OrderAndDuplicatesDoNotChangeWhichSetWasSeen()
    {
        var notifier = new WarningNotifier();
        Assert.True(notifier.ShouldNotify(new[] { "a", "b" }));
        Assert.False(notifier.ShouldNotify(new[] { "b", "a" }));
        Assert.False(notifier.ShouldNotify(new[] { "a", "b", "a" }));
        Assert.True(notifier.ShouldNotify(new[] { "a", "c" }));
    }

    [Fact]
    public void ABlankListInBetweenStillSuppressesTheRepeat()
    {
        var notifier = new WarningNotifier();
        Assert.True(notifier.ShouldNotify(new[] { "a" }));
        Assert.False(notifier.ShouldNotify(Array.Empty<string>()));
        Assert.False(notifier.ShouldNotify(new[] { "a" }));
    }

    [Fact]
    public void ANullWarningListThrows()
    {
        var notifier = new WarningNotifier();
        Assert.Throws<ArgumentNullException>(() => notifier.ShouldNotify(null!));
    }
}
