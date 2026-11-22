using Rigshift.Config;
using Rigshift.Settings;

namespace Rigshift.Tests;

public class ConfigFileStoreTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("rigshift").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void SavingCreatesTheParentDirectoryAndAFileTheLoaderReadsBack()
    {
        var path = Path.Combine(_dir, "does-not-exist-yet", "nested", "rigshift.json");

        ConfigFileStore.Save(path, new RigshiftConfig { IdleColor = "#010101" });

        Assert.True(File.Exists(path));
        Assert.Contains("\"idleColor\": \"#010101\"", File.ReadAllText(path));
        Assert.False(File.Exists(path + ".tmp"));
        Assert.False(File.Exists(path + ".bak"));

        var result = ConfigLoader.Load(path);

        Assert.Equal("#010101", result.Config.IdleColor);
    }

    [Fact]
    public void SavingOverAnExistingFileKeepsThePreviousBytesAtTheBackupPath()
    {
        var path = Path.Combine(_dir, "rigshift.json");

        ConfigFileStore.Save(path, new RigshiftConfig { IdleColor = "#010101" });
        var firstBytes = File.ReadAllBytes(path);

        ConfigFileStore.Save(path, new RigshiftConfig { IdleColor = "#020202" });

        Assert.Equal(firstBytes, File.ReadAllBytes(path + ".bak"));
        Assert.Equal("#010101", ConfigLoader.Load(path + ".bak").Config.IdleColor);
        Assert.Equal("#020202", ConfigLoader.Load(path).Config.IdleColor);
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void AThirdSaveReplacesTheBackupWithTheImmediatelyPreviousFile()
    {
        var path = Path.Combine(_dir, "rigshift.json");

        ConfigFileStore.Save(path, new RigshiftConfig { IdleColor = "#010101" });
        ConfigFileStore.Save(path, new RigshiftConfig { IdleColor = "#020202" });
        var secondBytes = File.ReadAllBytes(path);

        ConfigFileStore.Save(path, new RigshiftConfig { IdleColor = "#030303" });

        Assert.Equal(secondBytes, File.ReadAllBytes(path + ".bak"));
        Assert.Equal("#030303", ConfigLoader.Load(path).Config.IdleColor);
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void SavingWhenTheParentPathIsAFileThrowsAndLeavesNoBackupOrTempFile()
    {
        var parent = Path.Combine(_dir, "parent-file");
        File.WriteAllText(parent, "i am a file, not a directory");
        var path = Path.Combine(parent, "rigshift.json");

        Assert.ThrowsAny<IOException>(() => ConfigFileStore.Save(path, new RigshiftConfig { IdleColor = "#010101" }));

        Assert.True(File.Exists(parent));
        Assert.False(File.Exists(path));
        Assert.False(File.Exists(path + ".bak"));
        Assert.False(File.Exists(parent + ".bak"));
        Assert.False(File.Exists(path + ".tmp"));
    }
}
