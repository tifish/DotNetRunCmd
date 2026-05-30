namespace DotNetRunCmd.Tests;

using DotNetRun;
using System.Runtime.InteropServices;

public class PathTests
{
    [Fact]
    public void PathSeparator_MatchesCurrentPlatform()
    {
        var expected = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ';' : ':';

        Assert.Equal(expected, Cmd.PathSeparator);
    }

    [Fact]
    public void GetFile_ReturnsExactFileAndFirstWildcardMatch()
    {
        using var temp = new TemporaryDirectory();
        var file = Path.Combine(temp.DirectoryPath, "sample.txt");
        File.WriteAllText(file, "content");

        Assert.Equal(file, Cmd.GetFile(file));
        Assert.Equal(file, Cmd.GetFile(Path.Combine(temp.DirectoryPath, "*.txt")));
    }

    [Fact]
    public void GetFile_ThrowsWhenNoFileMatches()
    {
        using var temp = new TemporaryDirectory();

        Assert.Throws<Exception>(() => Cmd.GetFile(Path.Combine(temp.DirectoryPath, "*.missing")));
        Assert.Throws<Exception>(() => Cmd.GetFile(Path.Combine(temp.DirectoryPath, "missing.txt")));
    }

    [Fact]
    public void AddToPathAndRemoveFromPath_UpdateProcessPathOnlyOnce()
    {
        using var temp = new TemporaryDirectory();
        using var pathScope = new ProcessEnvironmentVariableScope(
            "PATH",
            string.Join(Cmd.PathSeparator, "C:\\ExistingA", "C:\\ExistingB")
        );

        Cmd.AddToPath(temp.DirectoryPath, EnvironmentVariableTarget.Process);
        Cmd.AddToPath(temp.DirectoryPath, EnvironmentVariableTarget.Process);

        var pathsAfterAdd = (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Cmd.PathSeparator)
            .Where(path => path.Equals(temp.DirectoryPath, StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.Single(pathsAfterAdd);

        Cmd.RemoveFromPath(temp.DirectoryPath, EnvironmentVariableTarget.Process);

        Assert.DoesNotContain(
            temp.DirectoryPath,
            (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Cmd.PathSeparator)
        );
    }

    [Fact]
    public void FindInPathAndInPath_FindExecutableFromProcessPath()
    {
        using var temp = new TemporaryDirectory();
        using var pathScope = new ProcessEnvironmentVariableScope("PATH", temp.DirectoryPath);
        var commandPath = Path.Combine(temp.DirectoryPath, "dnrun-test-tool.cmd");
        File.WriteAllText(commandPath, "@echo off\r\nexit /b 0\r\n");

        var foundPath = Cmd.FindInPath("dnrun-test-tool");

        Assert.Equal(commandPath, foundPath, ignoreCase: true);
        Assert.True(Cmd.InPath("dnrun-test-tool"));
    }

    [Fact]
    public void GetAllPaths_ReturnsDistinctNonEmptyPaths()
    {
        using var temp = new TemporaryDirectory();
        using var pathScope = new ProcessEnvironmentVariableScope(
            "PATH",
            string.Join(Cmd.PathSeparator, temp.DirectoryPath, temp.DirectoryPath, "")
        );

        var paths = Cmd.GetAllPaths();

        Assert.Contains(temp.DirectoryPath, paths, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain(paths, string.IsNullOrWhiteSpace);
        Assert.Equal(paths.Count, paths.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }
}
