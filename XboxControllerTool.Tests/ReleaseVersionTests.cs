using System.Text.RegularExpressions;
using XboxControllerTool.ConsoleUi;

namespace XboxControllerTool.Tests;

/// <summary>
/// The version is now incremented per release, which makes a stale download link a real risk: the
/// installer is named after the version and the build script deletes the previous one, so a README
/// still pointing at the old name would be a dead link the moment it is pushed.
/// </summary>
public class ReleaseVersionTests
{
    private static readonly string Version =
        typeof(AppShell).Assembly.GetName().Version?.ToString(3)
        ?? throw new InvalidOperationException("The assembly has no version.");

    [Fact]
    public void TheReadmeDownloadLinkPointsAtTheVersionThatIsActuallyBuilt()
    {
        var readme = ReadReadme();
        var expected = $"XboxControllerTool-{Version}.msi";

        Assert.Contains(expected, readme);
    }

    [Fact]
    public void TheReadmeNeverMentionsAnInstallerThatWillNotExist()
    {
        var readme = ReadReadme();
        var expected = $"XboxControllerTool-{Version}.msi";

        var referenced = Regex.Matches(readme, @"XboxControllerTool-\d+\.\d+\.\d+\.msi")
            .Select(match => match.Value)
            .Distinct()
            .ToList();

        // The build script removes every other installer from Releases, so any other name is dead.
        Assert.All(referenced, name => Assert.Equal(expected, name));
    }

    [Fact]
    public void TheDownloadHeadingNamesTheSameVersionAsTheLink()
    {
        var readme = ReadReadme();

        Assert.Contains($"Download XboxControllerTool {Version} (Windows Installer)", readme);
    }

    [Fact]
    public void TheShippedInstallerMatchesTheVersionBeingBuilt()
    {
        var releases = Path.Combine(RepositoryRoot(), "Releases");

        Assert.True(Directory.Exists(releases), "The Releases folder is missing.");

        var installers = Directory.GetFiles(releases, "XboxControllerTool-*.msi")
            .Select(Path.GetFileName)
            .ToList();

        // Exactly one installer is ever kept, and it has to be the current one.
        Assert.Equal([$"XboxControllerTool-{Version}.msi"], installers);
    }

    private static string ReadReadme() => File.ReadAllText(Path.Combine(RepositoryRoot(), "README.md"));

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "README.md")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not find the repository root.");
    }
}
