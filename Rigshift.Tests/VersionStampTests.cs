using System.Xml.Linq;

namespace Rigshift.Tests;

/// <summary>
/// Pins the build-version stamping in <c>Rigshift/Rigshift.csproj</c>.
/// The SDK would otherwise append a +&lt;short-sha&gt; source-revision to the assembly
/// informational version, which shows up in File Properties, Task Manager and the
/// installer's uninstall entry as an odd version string. We deliberately keep the
/// stamp a clean "0.1.0".
/// </summary>
public class VersionStampTests
{
    [Fact]
    public void Assembly_version_is_stamped_clean_without_git_metadata()
    {
        var csproj = XDocument.Load(Path.Combine(FindRepoRoot(), "Rigshift", "Rigshift.csproj"));
        var version = csproj.Descendants("Version").Single()?.Value;
        var includeSource = csproj.Descendants("IncludeSourceRevisionInInformationalVersion").Single();

        Assert.Equal("0.1.0", version);
        Assert.Equal("false", includeSource?.Value.ToLowerInvariant());
    }

    private static string FindRepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Rigshift.slnx")))
                return dir.FullName;
        }
        throw new DirectoryNotFoundException("Could not find the repository root above the test output.");
    }
}
