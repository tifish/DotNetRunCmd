namespace DotNetRun;

using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

public static partial class Cmd
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint SearchPathW(
        string? lpPath,
        string lpFileName,
        string? lpExtension,
        uint nBufferLength,
        StringBuilder lpBuffer,
        out IntPtr lpFilePart
    );

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
            return [".com", ".exe", ".cmd", ".bat", ".ps1", ".cs"];
        }
        else
        {
            return ["", ".sh", ".cs"]; // Unix executables usually have no extension
        }
    }

    public static string FindInPath(string fileName)
    {
        UpdatePathFromSystem();

        foreach (var extension in GetExecutableExtensions())
        {
            var path = FindInPath(fileName, extension);
            if (path != "")
                return path;
        }
        return "";
    }

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

    public static List<string> GetAllPaths()
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

                    // Set environment variable for later use.
                    if (!string.IsNullOrEmpty(envValue))
                    {
                        Environment.SetEnvironmentVariable(envVar, envValue);
                    }
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

    public static List<string> UpdatePathFromSystem()
    {
        var paths = GetAllPaths();
        Environment.SetEnvironmentVariable("PATH", string.Join(PathSeparator, paths));
        return paths;
    }

    public static void AddToPath(string path)
    {
        var expandedPath = ExpandEnvVar(path);

        // If path is in user's home directory, set to user's environment variable.
        if (expandedPath.StartsWith(@"c:\users\", StringComparison.OrdinalIgnoreCase))
        {
            AddToPath(path, EnvironmentVariableTarget.User);
        }
        // Otherwise, set to machine environment variable.
        else
        {
            AddToPath(path, EnvironmentVariableTarget.Machine);
        }

        // Always set to process environment variable.
        AddToPath(path, EnvironmentVariableTarget.Process);
    }

    /// <summary>
    /// Add a path to the PATH environment variable (only once).
    /// </summary>
    /// <param name="path">The path to add.</param>
    /// <param name="target">The target environment variable to update.</param>
    public static void AddToPath(string path, EnvironmentVariableTarget target)
    {
        // Add only once.
        var pathEnvVar = Environment.GetEnvironmentVariable("PATH", target) ?? "";
        var paths = pathEnvVar.Split(PathSeparator).ToList();
        if (paths.Contains(path, StringComparer.OrdinalIgnoreCase))
            return;

        paths.Add(path);
        Environment.SetEnvironmentVariable("PATH", string.Join(PathSeparator, paths), target);
    }

    public static void RemoveFromPath(string path)
    {
        RemoveFromPath(path, EnvironmentVariableTarget.Machine);
        RemoveFromPath(path, EnvironmentVariableTarget.User);
        RemoveFromPath(path, EnvironmentVariableTarget.Process);
    }

    public static void RemoveFromPath(string path, EnvironmentVariableTarget target)
    {
        var pathEnvVar = Environment.GetEnvironmentVariable("PATH", target) ?? "";
        var paths = pathEnvVar.Split(PathSeparator).ToList();
        if (!paths.Contains(path, StringComparer.OrdinalIgnoreCase))
            return;

        paths.Remove(path);
        Environment.SetEnvironmentVariable("PATH", string.Join(PathSeparator, paths), target);
    }
}
