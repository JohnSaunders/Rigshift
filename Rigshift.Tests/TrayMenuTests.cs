using Rigshift.Settings;

namespace Rigshift.Tests;

public class TrayMenuTests
{
    [Fact]
    public void FixedMenuItemsAreSettingsThenOpenConfigThenAboutThenExit()
    {
        Assert.Equal(new[] { "Settings…", "Open Config…", "About Rigshift…", "Exit" }, TrayMenuLabels.Fixed);

        Assert.Contains('…', TrayMenuLabels.Settings);
        Assert.Contains('…', TrayMenuLabels.OpenConfig);
        Assert.Contains('…', TrayMenuLabels.About);
    }

    [Fact]
    public void AboutTextIsProductNameAndVersion()
    {
        Assert.Equal($"Rigshift {SettingsForm.ProductVersion}", TrayMenuLabels.AboutText);
        Assert.Equal("Rigshift 0.1.0", TrayMenuLabels.AboutText);
    }

    [Fact]
    public void OneShotShowsConfiguredNameAlone()
    {
        Assert.Equal("Game", TrayMenuLabels.OneShot("Game"));
        Assert.Equal("Close browser", TrayMenuLabels.OneShot("Close browser"));
        Assert.DoesNotContain("purge", TrayMenuLabels.OneShot("Game"));
        Assert.DoesNotContain("purge", TrayMenuLabels.OneShot("Close browser"));
    }

    [Fact]
    public void ExitConfirmAsksBeforeQuitting()
    {
        Assert.Equal("Exit Rigshift", TrayMenuLabels.ExitConfirmTitle);
        Assert.StartsWith("Exit Rigshift?", TrayMenuLabels.ExitConfirmText);
        Assert.Contains("keep running", TrayMenuLabels.ExitConfirmText);
    }

    [Fact]
    public void ToggleShowsInUseSuffixOnlyWhenHeld()
    {
        Assert.Equal("Dev (in use)", TrayMenuLabels.Toggle("Dev", true));
        Assert.Equal("Dev", TrayMenuLabels.Toggle("Dev", false));
    }
}
