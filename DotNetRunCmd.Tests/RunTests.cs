namespace DotNetRunCmd.Tests;

using DotNetRun;

public class RunTests
{
    [Fact]
    public void RunWithOutput_CapturesStandardOutput()
    {
        var output = Cmd.RunWithOutput("cmd", "/c echo hello");

        Assert.Equal("hello", output.Trim());
    }

    [Fact]
    public void Run_ReturnsOrThrowsBasedOnExitCode()
    {
        Cmd.Run("cmd", "/c exit 0");

        var exception = Assert.Throws<Exception>(() => Cmd.Run("cmd", "/c exit 7"));
        Assert.Contains("Failed to run", exception.Message);
    }

    [Fact]
    public void RunFailedAndRunIgnoreExitCode_HandleNonZeroExitCode()
    {
        Assert.True(Cmd.RunFailed("cmd", "/c exit 7"));

        Cmd.RunIgnoreExitCode("cmd", "/c exit 7");
    }

    [Fact]
    public void RunWithOutput_CanChangeWorkingDirectoryToExecutableDirectory()
    {
        var cmdPath = Cmd.FindInPath("cmd");
        var expectedDirectory = Path.GetDirectoryName(cmdPath);

        var output = Cmd.RunWithOutput("cmd", "/c cd", changeCurrentDirectoryToExecutable: true);

        Assert.Equal(expectedDirectory, output.Trim(), ignoreCase: true);
    }

    [Fact]
    public void RunWithProcess_ThrowsWhenExecutableCannotBeFound()
    {
        var missingExecutable = $"dnrun-missing-{Guid.NewGuid():N}.exe";

        var exception = Assert.Throws<Exception>(() => Cmd.RunWithProcess(new(missingExecutable)));

        Assert.Contains($"Executable not found: {missingExecutable}", exception.Message);
    }

    [Fact]
    public void RunNoWait_StartsProcessWithoutWaitingForExit()
    {
        Cmd.RunNoWait("cmd", "/c exit 0");
    }

    [Fact]
    public void Robocopy_CopiesDirectoryContents()
    {
        using var source = new TemporaryDirectory();
        using var destination = new TemporaryDirectory();
        var sourceFile = Path.Combine(source.DirectoryPath, "item.txt");
        File.WriteAllText(sourceFile, "copied");

        Cmd.Robocopy(source.DirectoryPath, destination.DirectoryPath, "/NFL /NDL /NJH /NJS /NP");

        Assert.Equal("copied", File.ReadAllText(Path.Combine(destination.DirectoryPath, "item.txt")));
    }
}
