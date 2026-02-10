namespace DotNetRun;

using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

public static partial class Cmd
{
    [GeneratedRegex(@"%\w+%", RegexOptions.Compiled)]
    private static partial Regex EnvVarRegex();

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
    /// Update an environment variable in current process from user and machine environment variables.
    /// </summary>
    /// <param name="name">The name of the environment variable to update.</param>
    public static void UpdateEnvVarFromSystem(string name)
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
        var envValue = GetEnvVar(name, EnvironmentVariableTarget.Process);
        if (!string.IsNullOrEmpty(envValue))
            return envValue;

        envValue = GetEnvVar(name, EnvironmentVariableTarget.User);
        if (!string.IsNullOrEmpty(envValue))
            return envValue;

        envValue = GetEnvVar(name, EnvironmentVariableTarget.Machine);
        if (!string.IsNullOrEmpty(envValue))
            return envValue;

        EchoError($"Environment variable {name} not found");
        return "";
    }

    public static string GetEnvVar(string name, EnvironmentVariableTarget target)
    {
        var envValue = Environment.GetEnvironmentVariable(name, target);
        if (!string.IsNullOrEmpty(envValue))
            return envValue;

        return "";
    }

    public static void RemoveEnvVar(string name)
    {
        RemoveEnvVar(name, EnvironmentVariableTarget.Process);
        RemoveEnvVar(name, EnvironmentVariableTarget.User);
        RemoveEnvVar(name, EnvironmentVariableTarget.Machine);
    }

    public static void RemoveEnvVar(string name, EnvironmentVariableTarget target)
    {
        Environment.SetEnvironmentVariable(name, null, target);
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
