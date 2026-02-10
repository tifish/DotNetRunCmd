namespace DotNetRun;

public static partial class Cmd
{
    public static string ScriptPath
    {
        get
        {
            field ??= (string)AppContext.GetData("EntryPointFilePath")!;
            return field;
        }
    }

    public static string ScriptDir
    {
        get
        {
            field ??= (string)AppContext.GetData("EntryPointFileDirectoryPath")!;
            return field;
        }
    }

    public static void Echo(string message = "")
    {
        Console.WriteLine(message);
    }

    public static void EchoError(string message = "")
    {
        Console.Error.WriteLine(message);
    }

    public static void ChangeDir(string directory)
    {
        Environment.CurrentDirectory = Path.GetFullPath(directory);
    }

    public static void Exit(int exitCode = 0)
    {
        Environment.Exit(exitCode);
    }

    public static void Pause(string message = "")
    {
        if (!string.IsNullOrEmpty(message))
            Echo(message);

        Console.ReadKey();
    }
}
