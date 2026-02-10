namespace DotNetRun;

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

public static partial class Cmd
{
    [SupportedOSPlatform("windows")]
    [DllImport("kernel32.dll", EntryPoint = "GetOEMCP")]
    private static extern uint GetOEMCP();

    [SupportedOSPlatform("windows")]
    [DllImport("kernel32.dll", EntryPoint = "GetACP")]
    private static extern uint GetACP();

    private static bool? _isWSL = null;

    /// <summary>
    /// Determines whether the current process is running in WSL (Windows Subsystem for Linux) environment
    /// </summary>
    /// <returns>Returns true if running in WSL environment, otherwise returns false</returns>
    public static bool IsWSL
    {
        get
        {
            if (_isWSL != null)
                return _isWSL.Value;

            // Windows environment is not WSL
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _isWSL = false;
            }
            else
            {
                try
                {
                    _isWSL = false;

                    // Check /proc/version file
                    if (File.Exists("/proc/version"))
                    {
                        var content = File.ReadAllText("/proc/version").ToLower();
                        // Both WSL1 and WSL2 contain "microsoft" string in /proc/version
                        if (content.Contains("microsoft") || content.Contains("wsl"))
                        {
                            _isWSL = true;
                        }
                    }
                }
                catch
                {
                    // If file reading fails, assume it's not a WSL environment
                    _isWSL = false;
                }
            }

            return _isWSL.Value;
        }
    }

    [SupportedOSPlatformGuard("windows")]
    public static bool IsWindows()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }

    [SupportedOSPlatformGuard("linux")]
    [SupportedOSPlatformGuard("macos")]
    [SupportedOSPlatformGuard("freebsd")]
    public static bool IsUnix()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            || RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);
    }

    [SupportedOSPlatformGuard("linux")]
    public static bool IsLinux()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    }

    [SupportedOSPlatformGuard("macos")]
    public static bool IsMacOS()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    }

    [SupportedOSPlatformGuard("freebsd")]
    public static bool IsFreeBSD()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);
    }
}
