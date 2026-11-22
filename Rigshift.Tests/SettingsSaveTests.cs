using Rigshift.Config;
using Rigshift.Settings;

namespace Rigshift.Tests;

public class SettingsSaveTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("rigshift").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private string Destination => Path.Combine(_dir, "rigshift.json");

    [Fact]
    public void SavesTheDefaultWithAnEditedIdleColorAndReadsItBack()
    {
        var draft = SettingsMapper.FromConfig(ConfigLoader.Default());
        draft.IdleColor = "#112233";

        var result = SettingsSave.TrySave(draft, Destination, new List<string> { "AI" }, "AI");

        Assert.True(result.Saved);
        Assert.Null(result.Error);
        var loaded = ConfigLoader.Load(Destination);
        Assert.Empty(loaded.Warnings);
        Assert.Equal("#112233", loaded.Config.IdleColor);
        Assert.False(File.Exists(Destination + ".tmp"));
    }

    [Fact]
    public void InvalidIdleColorFailsWithTheValidationMessageAndWritesNothing()
    {
        var draft = SettingsMapper.FromConfig(ConfigLoader.Default());
        draft.IdleColor = "nope";
        var enabled = new List<string> { "AI" };

        var result = SettingsSave.TrySave(draft, Destination, enabled, "AI");

        Assert.False(result.Saved);
        Assert.Null(result.Config);
        Assert.Contains("Idle color must be #RRGGBB.", result.Error, StringComparison.Ordinal);
        Assert.Contains("AI", result.Enabled);
        Assert.NotSame(enabled, result.Enabled);
        Assert.False(File.Exists(Destination));
        Assert.False(File.Exists(Destination + ".bak"));
    }

    [Fact]
    public void SuccessReconcilesEnabledNamesAgainstTheSavedConfig()
    {
        var draft = SettingsMapper.FromConfig(ConfigLoader.Default());

        var result = SettingsSave.TrySave(
            draft, Destination, new List<string> { "AI", "Gone" }, "Gone");

        Assert.True(result.Saved);
        Assert.Equal(new[] { "AI" }, result.Enabled);
        Assert.Null(result.LastEnabled);
    }

    [Fact]
    public void FailsWhenTheSavePathParentIsAFileAndWritesNoBackup()
    {
        var blocker = Path.Combine(_dir, "blocker");
        File.WriteAllText(blocker, "not a directory");
        var draft = SettingsMapper.FromConfig(ConfigLoader.Default());
        var enabled = new List<string> { "AI" };

        var result = SettingsSave.TrySave(draft, Path.Combine(_dir, "blocker", "rigshift.json"), enabled, "AI");

        Assert.False(result.Saved);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
        Assert.Contains("AI", result.Enabled);
        Assert.NotSame(enabled, result.Enabled);
        Assert.False(File.Exists(blocker + ".bak"));
    }
}
