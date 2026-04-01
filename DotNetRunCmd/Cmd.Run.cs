namespace DotNetRun;

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

public static partial class Cmd
{
    public static void Run(
        string filePath,
        string arguments = "",
        bool changeCurrentDirectoryToExecutable = false
    )
    {
        var exitCode = RunWithExitCode(filePath, arguments, changeCurrentDirectoryToExecutable);
        if (exitCode != 0)
            throw new Exception($"Failed to run {filePath} with arguments {arguments}");
    }

    public static bool RunFailed(
        string filePath,
        string arguments = "",
        bool changeCurrentDirectoryToExecutable = false
    )
    {
        return RunWithExitCode(filePath, arguments, changeCurrentDirectoryToExecutable) != 0;
    }

    public static void RunIgnoreExitCode(
        string filePath,
        string arguments = "",
        bool changeCurrentDirectoryToExecutable = false
    )
    {
        RunWithExitCode(filePath, arguments, changeCurrentDirectoryToExecutable);
    }

    public static int RunWithExitCode(
        string filePath,
        string arguments = "",
        bool changeCurrentDirectoryToExecutable = false
    )
    {
        Process? process = null;
        try
        {
            process = RunWithProcess(
                new ProcessStartInfo(filePath, arguments),
                changeCurrentDirectoryToExecutable
            );

            process.WaitForExit();
            return process.ExitCode;
        }
        finally
        {
            // Restore the console encoding to default in case of ANSI encoding issues
            Console.InputEncoding = Encoding.Default;
            Console.OutputEncoding = Encoding.Default;
            process?.Dispose();
        }
    }

    public static string RunWithOutput(
        string filePath,
        string arguments = "",
        bool changeCurrentDirectoryToExecutable = false,
        Encoding? outputEncoding = null
    )
    {
        Process? process = null;
        try
        {
            var startInfo = new ProcessStartInfo(filePath, arguments)
            {
                RedirectStandardOutput = true,
            };
            if (outputEncoding is not null)
            {
                startInfo.StandardOutputEncoding = outputEncoding;
            }
            process = RunWithProcess(startInfo, changeCurrentDirectoryToExecutable);

            process.WaitForExit();

            return process.StandardOutput.ReadToEnd();
        }
        finally
        {
            // Restore the console encoding to default in case of ANSI encoding issues
            Console.OutputEncoding = Encoding.Default;
            Console.InputEncoding = Encoding.Default;

            process?.Dispose();
        }
    }

    public static void RunNoWait(
        string filePath,
        string arguments = "",
        bool changeCurrentDirectoryToExecutable = false
    )
    {
        using var _ = RunWithProcess(
            new ProcessStartInfo(filePath, arguments) { UseShellExecute = true },
            changeCurrentDirectoryToExecutable
        );
    }

    public static Process RunWithProcess(
        ProcessStartInfo startInfo,
        bool changeCurrentDirectoryToExecutable = false
    )
    {
        // Maybe a file other than an executable
        if (File.Exists(startInfo.FileName))
        {
            startInfo.FileName = Path.GetFullPath(startInfo.FileName);
        }
        else
        {
            var fullPath = FindInPath(startInfo.FileName);
            if (string.IsNullOrEmpty(fullPath))
                throw new Exception($"Executable not found: {startInfo.FileName}");

            startInfo.FileName = fullPath;
        }

        var realFilePath = startInfo.FileName;

        var isBatch = startInfo.FileName.EndsWith(".cmd") || startInfo.FileName.EndsWith(".bat");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Handle PowerShell scripts
            if (startInfo.FileName.EndsWith(".ps1"))
            {
                startInfo.Arguments =
                    $"""-ExecutionPolicy ByPass -File "{startInfo.FileName}" {startInfo.Arguments}""";
                startInfo.FileName = "powershell.exe";
            }
            // Handle C# scripts
            else if (startInfo.FileName.EndsWith(".cs"))
            {
                var dotnetPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    @"dotnet\dotnet.exe"
                );
                if (!File.Exists(dotnetPath))
                {
                    throw new Exception($"dotnet.exe not found in {dotnetPath}");
                }
                startInfo.Arguments = $"""run "{startInfo.FileName}" {startInfo.Arguments}""";
                startInfo.FileName = dotnetPath;
            }
            // Handle batch files
            else if (isBatch)
            {
                if (!startInfo.UseShellExecute)
                {
                    // Use system encoding to avoid ANSI encoding issues
                    var oemCp = GetOEMCP();
                    if (!SetConsoleOutputCP(oemCp))
                    {
                        EchoError($"Failed to set console output encoding to {oemCp}");
                    }
                    if (!SetConsoleCP(oemCp))
                    {
                        EchoError($"Failed to set console input encoding to {oemCp}");
                    }
                }
            }
            // Handle shell scripts with msys
            else if (startInfo.FileName.EndsWith(".sh"))
            {
                var bashPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    @"Git\bin\bash.exe"
                );
                if (!File.Exists(bashPath))
                {
                    throw new Exception($"bash.exe not found in {bashPath}");
                }
                startInfo.Arguments = $"\"{startInfo.FileName}\" {startInfo.Arguments}";
                startInfo.FileName = bashPath;
            }
        }
        else if (
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            || RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
        )
        {
            // Handle shell scripts and C# scripts on Linux/Unix
            if (startInfo.FileName.EndsWith(".sh"))
            {
                startInfo.Arguments = $"\"{startInfo.FileName}\" {startInfo.Arguments}";
                startInfo.FileName = "/bin/bash";
            }
            else if (startInfo.FileName.EndsWith(".cs"))
            {
                var dotnetPath = FindInPath("dotnet");
                if (string.IsNullOrEmpty(dotnetPath))
                {
                    throw new Exception("dotnet not found in PATH");
                }
                startInfo.Arguments = $"run \"{startInfo.FileName}\" {startInfo.Arguments}";
                startInfo.FileName = dotnetPath;
            }
        }

        if (changeCurrentDirectoryToExecutable)
        {
            startInfo.WorkingDirectory = Path.GetDirectoryName(realFilePath);
        }

        var process =
            Process.Start(startInfo)
            ?? throw new Exception($"Failed to start process {startInfo.FileName}");
        return process;
    }

    /// <summary>
    /// Must be called after RunWithProcess to restore the console encoding to the original value
    /// </summary>
    public static void RestoreAfterRunWithProcess(Process? process)
    {
        if (process == null)
            return;

        if (
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            && !process.StartInfo.UseShellExecute
        )
        {
            if (!process.HasExited)
            {
                process.WaitForExit();
            }

            SetConsoleOutputCP(65001);
            SetConsoleCP(65001);
        }
    }

    [SupportedOSPlatform("windows")]
    public static void Robocopy(string source, string destination, string arguments = "")
    {
        var exitCode = RunWithExitCode(
            "robocopy",
            $"""
            "{source}" "{destination}" {arguments} /UNICODE
            """
        );

        if (exitCode >= 8)
            throw new Exception(
                $"Failed to robocopy {source} to {destination} with arguments {arguments}"
            );
    }
}
