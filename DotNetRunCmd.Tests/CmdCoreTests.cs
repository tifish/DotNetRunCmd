namespace DotNetRunCmd.Tests;

using DotNetRun;

public class CmdCoreTests
{
    [Fact]
    public void ScriptPathAndDir_ReadFromAppContextData()
    {
        using var temp = new TemporaryDirectory();
        var scriptPath = Path.Combine(temp.DirectoryPath, "script.cs");

        AppContext.SetData("EntryPointFilePath", scriptPath);
        AppContext.SetData("EntryPointFileDirectoryPath", temp.DirectoryPath);

        Assert.Equal(scriptPath, Cmd.ScriptPath);
        Assert.Equal(temp.DirectoryPath, Cmd.ScriptDir);
    }

    [Fact]
    public void Echo_WritesMessageAndNewLine()
    {
        var originalOut = Console.Out;
        using var writer = new StringWriter();

        try
        {
            Console.SetOut(writer);
            Cmd.Echo("hello");
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        Assert.Equal($"hello{Environment.NewLine}", writer.ToString());
    }

    [Fact]
    public void EchoError_WritesMessageAndNewLine()
    {
        var originalError = Console.Error;
        using var writer = new StringWriter();

        try
        {
            Console.SetError(writer);
            Cmd.EchoError("problem");
        }
        finally
        {
            Console.SetError(originalError);
        }

        Assert.Equal($"problem{Environment.NewLine}", writer.ToString());
    }

    [Fact]
    public void ChangeDir_UsesFullPath()
    {
        var originalDirectory = Environment.CurrentDirectory;
        using var temp = new TemporaryDirectory();

        try
        {
            var relativePath = Path.GetRelativePath(originalDirectory, temp.DirectoryPath);
            Cmd.ChangeDir(relativePath);

            Assert.Equal(Path.GetFullPath(temp.DirectoryPath), Environment.CurrentDirectory);
        }
        finally
        {
            Environment.CurrentDirectory = originalDirectory;
        }
    }
}
