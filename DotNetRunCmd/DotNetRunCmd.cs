namespace Tifish.DotNetRun;

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

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

    public static int RunWithExitCode(
        string filePath,
        string arguments = "",
        bool changeCurrentDirectoryToExecutable = false
    )
    {
        using var process = RunWithProcess(
            new ProcessStartInfo(filePath, arguments),
            changeCurrentDirectoryToExecutable
        );

        process.WaitForExit();
        return process.ExitCode;
    }

    public static string RunWithOutput(
        string filePath,
        string arguments = "",
        bool changeCurrentDirectoryToExecutable = false
    )
    {
        using var process = RunWithProcess(
            new ProcessStartInfo(filePath, arguments) { RedirectStandardOutput = true },
            changeCurrentDirectoryToExecutable
        );

        process.WaitForExit();
        return process.StandardOutput.ReadToEnd();
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

        // Handle PowerShell and C# scripts on Windows
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (startInfo.FileName.EndsWith(".ps1"))
            {
                startInfo.Arguments =
                    $"""-ExecutionPolicy ByPass -File "{startInfo.FileName}" {startInfo.Arguments}""";
                startInfo.FileName = "powershell.exe";
            }
            else if (startInfo.FileName.EndsWith(".cs"))
            {
                var dotnetPath = Path.Combine(
                    Environment.GetEnvironmentVariable("ProgramFiles")!,
                    "dotnet",
                    "dotnet.exe"
                );
                if (!File.Exists(dotnetPath))
                {
                    throw new Exception($"dotnet.exe not found in {dotnetPath}");
                }
                startInfo.Arguments = $"""run "{startInfo.FileName}" {startInfo.Arguments}""";
                startInfo.FileName = dotnetPath;
            }
            else if (startInfo.FileName.EndsWith(".cmd") || startInfo.FileName.EndsWith(".bat"))
            {
                // todo: should use local encoding
            }
        }
        else
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
            startInfo.WorkingDirectory = Path.GetDirectoryName(realFilePath);
        var process =
            Process.Start(startInfo)
            ?? throw new Exception($"Failed to start process {startInfo.FileName}");
        return process;
    }

    public static string GetFile(string wildcard)
    {
        if (wildcard.Contains('*'))
        {
            var dir = Path.GetDirectoryName(wildcard);
            if (string.IsNullOrEmpty(dir))
                dir = ".";

            var files = Directory.GetFiles(dir, Path.GetFileName(wildcard));
            if (files.Length == 0)
                throw new Exception($"No files found for {wildcard}");

            return files[0];
        }

        if (File.Exists(wildcard))
            return wildcard;

        throw new Exception($"File not found: {wildcard}");
    }

    public static bool InPath(string executable)
    {
        return FindInPath(executable) != "";
    }

    private static string[] GetExecutableExtensions()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return [".exe", ".cmd", ".bat", ".ps1", ".cs"];
        }
        else
        {
            return ["", ".sh", ".cs"]; // Unix executables usually have no extension
        }
    }

    public static string FindInPath(string fileName)
    {
        UpdatePathEnvVar();

        foreach (var extension in GetExecutableExtensions())
        {
            var path = FindInPath(fileName, extension);
            if (path != "")
                return path;
        }
        return "";
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint SearchPathW(
        string? lpPath,
        string lpFileName,
        string? lpExtension,
        uint nBufferLength,
        StringBuilder lpBuffer,
        out IntPtr lpFilePart
    );

    private static string FindInPath(string fileName, string? extension)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return FindInPathWindows(fileName, extension);
        }
        else
        {
            return FindInPathUnix(fileName, extension);
        }
    }

    private static string FindInPathWindows(string fileName, string? extension)
    {
        var sb = new StringBuilder(260);
        uint len = SearchPathW(null, fileName, extension, (uint)sb.Capacity, sb, out _);

        if (len == 0)
            return "";

        if (len >= sb.Capacity)
        {
            sb = new StringBuilder((int)len + 1);
            len = SearchPathW(null, fileName, extension, (uint)sb.Capacity, sb, out _);
            if (len == 0)
                return "";
        }

        return sb.ToString();
    }

    private static string FindInPathUnix(string fileName, string? extension)
    {
        // On Linux/Unix, find executables via the PATH environment variable
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv))
            return "";

        var paths = pathEnv.Split(':');
        var fileNameWithExt = string.IsNullOrEmpty(extension) ? fileName : fileName + extension;

        foreach (var path in paths)
        {
            if (string.IsNullOrWhiteSpace(path))
                continue;

            try
            {
                var fullPath = Path.Combine(path, fileNameWithExt);
                if (File.Exists(fullPath))
                {
                    // Check whether the file is executable
                    if (IsExecutableUnix(fullPath))
                        return fullPath;
                }
            }
            catch
            {
                // Ignore invalid paths
                continue;
            }
        }

        return "";
    }

    private static bool IsExecutableUnix(string filePath)
    {
        try
        {
            // Check execute permission on Unix systems
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
                return false;

            // Use stat to check permissions (simplified: assume exists means executable)
            // A more accurate approach uses P/Invoke to call stat() or Mono.Unix
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool? _isWSL = null;

    /// <summary>
    /// 判断当前是否运行在 WSL (Windows Subsystem for Linux) 环境中
    /// </summary>
    /// <returns>如果在 WSL 环境中返回 true,否则返回 false</returns>
    public static bool IsWSL
    {
        get
        {
            if (_isWSL != null)
                return _isWSL.Value;

            // Windows 环境不是 WSL
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _isWSL = false;
            }
            else
            {
                try
                {
                    _isWSL = false;

                    // 检查 /proc/version 文件
                    if (File.Exists("/proc/version"))
                    {
                        var content = File.ReadAllText("/proc/version").ToLower();
                        // WSL1 和 WSL2 都会在 /proc/version 中包含 "microsoft" 字符串
                        if (content.Contains("microsoft") || content.Contains("wsl"))
                        {
                            _isWSL = true;
                        }
                    }
                }
                catch
                {
                    // 如果读取文件失败，假设不是 WSL 环境
                    _isWSL = false;
                }
            }

            return _isWSL.Value;
        }
    }

    [GeneratedRegex(@"%\w+%", RegexOptions.Compiled)]
    private static partial Regex EnvVarRegex();

    public static char PathSeparator
    {
        get
        {
            if (field == ' ')
            {
                return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ';' : ':';
            }
            return field;
        }
    } = ' ';

    public static List<string> GetEnvPaths()
    {
        // Get PATH from system.
        string path;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            path =
                Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process)
                + ";"
                + Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User)
                + ";"
                + Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine);

            // Replace environment variables in PATH (Windows only).
            var envVarRegex = EnvVarRegex();

            const int MaxTryCount = 10;
            for (var i = 0; i < MaxTryCount; i++)
            {
                foreach (Match match in envVarRegex.Matches(path))
                {
                    var envVar = match.Value.Trim('%');
                    var envValue = GetEnvVar(envVar);
                }

                // Expand environment variables in PATH.
                var oldPath = path;
                path = Environment.ExpandEnvironmentVariables(path);
                if (path == oldPath)
                    break;
            }
        }
        else
        {
            // Unix/Linux: PATH is simpler, just get from Process environment
            path =
                Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process) ?? "";
        }

        // Split into paths.
        return path.Split(PathSeparator)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct()
            .ToList();
    }

    public static List<string> UpdatePathEnvVar()
    {
        var paths = GetEnvPaths();
        Environment.SetEnvironmentVariable("PATH", string.Join(PathSeparator, paths));
        return paths;
    }

    /// <summary>
    /// Update an environment variable in current process from user and machine environment variables.
    /// </summary>
    /// <param name="name">The name of the environment variable to update.</param>
    public static void UpdateEnvVar(string name)
    {
        // User environment variable is preferred.
        var value = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User);
        if (string.IsNullOrEmpty(value))
        {
            value = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Machine);
            if (string.IsNullOrEmpty(value))
            {
                EchoError($"Environment variable {name} not found");
                return;
            }
        }

        Environment.SetEnvironmentVariable(name, value);
    }

    /// <summary>
    /// Set an environment variable.
    /// Always set to process environment variable at the same time.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <param name="target"></param>
    public static void SetEnvVar(
        string name,
        string value,
        EnvironmentVariableTarget target = EnvironmentVariableTarget.Process
    )
    {
        Environment.SetEnvironmentVariable(name, value, target);
        if (target != EnvironmentVariableTarget.Process)
        {
            Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.Process);
        }
    }

    /// <summary>
    /// Get an environment variable from process, user and machine environment variables.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static string GetEnvVar(string name)
    {
        var envValue = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        if (!string.IsNullOrEmpty(envValue))
            return envValue;

        envValue = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User);
        if (string.IsNullOrEmpty(envValue))
        {
            envValue = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Machine);
            if (string.IsNullOrEmpty(envValue))
            {
                EchoError($"Environment variable {name} not found");
            }
        }

        if (!string.IsNullOrEmpty(envValue))
        {
            // Set environment variable for later use.
            Environment.SetEnvironmentVariable(name, envValue);
        }

        return envValue ?? "";
    }

    /// <summary>
    /// Add a path to the PATH environment variable (only once).
    /// </summary>
    /// <param name="path">The path to add.</param>
    /// <param name="target">The target environment variable to update.</param>
    public static void AddToPath(
        string path,
        EnvironmentVariableTarget target = EnvironmentVariableTarget.Machine
    )
    {
        // Should expand environment variables in path, or can't be found.
        path = ExpandEnvVar(path);

        // If path is in user's home directory, set to user's environment variable.
        if (path.ToLower().StartsWith(@"c:\users\"))
        {
            target = EnvironmentVariableTarget.User;
        }

        // Add only once.
        var pathEnvVar = Environment.GetEnvironmentVariable("PATH", target) ?? "";
        var paths = pathEnvVar.Split(PathSeparator).ToList();
        if (paths.Contains(path))
            return;

        paths.Add(path);
        Environment.SetEnvironmentVariable("PATH", string.Join(PathSeparator, paths), target);
    }

    /// <summary>
    /// Expand environment variables in a string recursively.
    /// </summary>
    /// <param name="str">The string to expand.</param>
    /// <returns>The expanded string.</returns>
    public static string ExpandEnvVar(string str)
    {
        // Can be never ending loop, so limit the number of tries.
        const int MaxTryCount = 10;
        // Deal with nested environment variables.
        for (var i = 0; i < MaxTryCount; i++)
        {
            var newStr = Environment.ExpandEnvironmentVariables(str);
            if (newStr == str)
                return str;
            str = newStr;
        }
        return str;
    }

    private static readonly IntPtr HWND_BROADCAST = new(0xffff);
    private const uint WM_SETTINGCHANGE = 0x001A;

    private const uint SMTO_ABORTIFHUNG = 0x0002; // Abort if the target window is hung
    private const uint SMTO_BLOCK = 0x0001; // Synchronous wait (with timeout)
    private const uint SMTO_NORMAL = 0x0000;

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hWnd,
        uint Msg,
        UIntPtr wParam,
        string lParam,
        uint fuFlags,
        uint uTimeout,
        out UIntPtr lpdwResult
    );

    /// <summary>
    /// Broadcast that environment variables changed (WM_SETTINGCHANGE, "Environment")
    /// Only works on Windows.
    /// </summary>
    public static void BroadcastEnvChange(uint timeoutMs = 500)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        SendMessageTimeout(
            HWND_BROADCAST,
            WM_SETTINGCHANGE,
            UIntPtr.Zero,
            "Environment",
            SMTO_ABORTIFHUNG | SMTO_BLOCK,
            timeoutMs,
            out _
        );
    }
}
