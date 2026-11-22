using System.Text.RegularExpressions;

namespace Rigshift.Tests;

/// <summary>
/// Pins the trust-relevant directives of <c>installer/rigshift.iss</c>. These prove the
/// directives are present, not that the script compiles (the CI installer job does that) or
/// that the folder check behaves at runtime (the owner install check does that).
/// </summary>
public class InstallerScriptTests
{
    private static readonly string RepoRoot = FindRepoRoot();
    private static readonly string[] Lines = ScriptLines();

    [Fact]
    public void Installs_per_machine_only()
    {
        Assert.Contains("PrivilegesRequired=admin", Lines);
        Assert.DoesNotContain(Lines, l => l.StartsWith("PrivilegesRequiredOverridesAllowed", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Installs_to_a_fixed_program_files_folder()
    {
        Assert.Contains(@"DefaultDirName={autopf}\Rigshift", Lines);
        Assert.Contains("DisableDirPage=yes", Lines);
        Assert.Contains("UsePreviousAppDir=no", Lines);
        Assert.Contains("ArchitecturesInstallIn64BitMode=x64compatible", Lines);
    }

    [Fact]
    public void Never_widens_folder_permissions()
    {
        Assert.DoesNotContain(Lines, l => l.Contains("Permissions:", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Mutex_matches_the_tray()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot, "Rigshift", "Program.cs"));
        var match = Regex.Match(program, "new Mutex\\([^\"]*\"([^\"]+)\"");
        Assert.True(match.Success, "Program.cs no longer creates a named Mutex.");

        var directive = Assert.Single(Lines, l => l.StartsWith("AppMutex=", StringComparison.Ordinal));
        Assert.Equal(match.Groups[1].Value, directive["AppMutex=".Length..]);
    }

    [Fact]
    public void AppId_is_fixed()
    {
        Assert.Contains("AppId={{365D421D-FC94-4CFB-9416-357C0F2C2BDC}", Lines);
    }

    [Fact]
    public void Launches_the_tray_unelevated()
    {
        var run = Assert.Single(Section("Run"), l => l.Contains(@"{app}\Rigshift.exe", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("runasoriginaluser", run, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("postinstall", run, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Never_ships_a_config()
    {
        var files = Assert.Single(Section("Files"), l => l.Contains("Excludes:", StringComparison.Ordinal));
        var excludes = Regex.Match(files, "Excludes:\\s*\"([^\"]*)\"");
        Assert.True(excludes.Success, "The [Files] entry has no quoted Excludes: value.");
        Assert.Contains("rigshift.json", excludes.Groups[1].Value, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rejects_any_other_install_folder()
    {
        var code = Section("Code");
        Assert.NotEmpty(code);
        Assert.Contains(code, l => l.Contains("function PrepareToInstall", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(code, l => l.Contains(@"{autopf}\Rigshift", StringComparison.Ordinal));
    }

    [Fact]
    public void Clears_stale_payload_but_keeps_config()
    {
        var delete = Section("InstallDelete");
        Assert.NotEmpty(delete);
        Assert.Contains(delete, l => l.Contains(@"""{app}\*.dll""", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(delete, l => l.Contains(@"""{app}\Assets""", StringComparison.OrdinalIgnoreCase));

        foreach (var line in delete)
        {
            Assert.DoesNotContain("rigshift.json", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(@"{app}\*""", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(@"{app}\*.*""", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(@"{app}\*.json""", line, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>Lines between the <c>[name]</c> header and the next line starting with <c>[</c>.</summary>
    private static List<string> Section(string name)
    {
        var result = new List<string>();
        var inside = false;
        foreach (var line in Lines)
        {
            if (line.StartsWith('['))
            {
                inside = string.Equals(line, $"[{name}]", StringComparison.OrdinalIgnoreCase);
                continue;
            }
            if (inside)
                result.Add(line);
        }
        return result;
    }

    /// <summary>The script's non-blank lines, trimmed, without <c>;</c> or <c>//</c> comments.</summary>
    private static string[] ScriptLines() =>
        File.ReadAllLines(Path.Combine(RepoRoot, "installer", "rigshift.iss"))
            .Select(l => l.Trim())
            .Where(l => l.Length > 0 && !l.StartsWith(';') && !l.StartsWith("//", StringComparison.Ordinal))
            .ToArray();

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
