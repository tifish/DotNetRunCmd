namespace DotNetRun;

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

public static partial class Cmd
{
    [DllImport("libc", SetLastError = true)]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("freebsd")]
    private static extern int kill(int pid, int sig);

    /// <summary>
    /// Sends SIGKILL signal using kill() system call on Unix/Linux platforms
    /// </summary>
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("freebsd")]
    private static void KillProcessUnix(int processId)
    {
        const int SIGKILL = 9;
        var result = kill(processId, SIGKILL);
        if (result != 0)
        {
            throw new Exception($"kill() system call failed with return code {result}");
        }
    }

    /// <summary>
    /// Terminates the specified processes (attempts graceful shutdown)
    /// </summary>
    /// <param name="processIds">Array of process IDs to terminate</param>
    /// <exception cref="Exception">Thrown when process termination fails</exception>
    public static void Kill(params int[] processIds)
    {
        foreach (var processId in processIds)
        {
            Kill(processId);
        }
    }

    private static void Kill(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows: Attempt graceful shutdown of main window
                if (process.MainWindowHandle != IntPtr.Zero)
                {
                    if (!process.CloseMainWindow())
                    {
                        // If graceful shutdown fails, force termination
                        process.Kill();
                    }
                }
                else
                {
                    // No main window, terminate directly
                    process.Kill();
                }
            }
            else
            {
                // Unix/Linux: Send SIGTERM signal
                process.Kill();
            }
        }
        catch (ArgumentException)
        {
            // Process does not exist, silently return
            return;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to kill process {processId}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Attempts to terminate the specified processes (attempts graceful shutdown)
    /// </summary>
    /// <param name="processIds">Array of process IDs to terminate</param>
    /// <returns>True if all processes were successfully terminated, false otherwise</returns>
    public static bool TryKill(params int[] processIds)
    {
        try
        {
            Kill(processIds);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Terminates all processes with the specified names (attempts graceful shutdown)
    /// </summary>
    /// <param name="processNames">Array of process names to terminate</param>
    /// <exception cref="Exception">Thrown when process termination fails</exception>
    public static void Kill(params string[] processNames)
    {
        foreach (var processName in processNames)
        {
            Kill(processName);
        }
    }

    private static void Kill(string processName)
    {
        try
        {
            var processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0)
            {
                // No processes found, silently return
                return;
            }

            Exception? lastException = null;
            foreach (var process in processes)
            {
                try
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        // Windows: Attempt graceful shutdown of main window
                        if (process.MainWindowHandle != IntPtr.Zero)
                        {
                            if (!process.CloseMainWindow())
                            {
                                // If graceful shutdown fails, force termination
                                process.Kill();
                            }
                        }
                        else
                        {
                            // No main window, terminate directly
                            process.Kill();
                        }
                    }
                    else
                    {
                        // Unix/Linux: Send SIGTERM signal
                        process.Kill();
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }
                finally
                {
                    process.Dispose();
                }
            }

            if (lastException != null)
            {
                throw new Exception(
                    $"Failed to kill some processes named {processName}",
                    lastException
                );
            }
        }
        catch (Exception ex) when (!ex.Message.Contains("Failed to kill"))
        {
            throw new Exception($"Failed to kill processes named {processName}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Attempts to terminate all processes with the specified names (attempts graceful shutdown)
    /// </summary>
    /// <param name="processNames">Array of process names to terminate</param>
    /// <returns>True if all processes were successfully terminated, false otherwise</returns>
    public static bool TryKill(params string[] processNames)
    {
        try
        {
            Kill(processNames);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Force terminates the specified processes
    /// </summary>
    /// <param name="processIds">Array of process IDs to force terminate</param>
    /// <exception cref="Exception">Thrown when process termination fails</exception>
    public static void ForceKill(params int[] processIds)
    {
        foreach (var processId in processIds)
        {
            ForceKill(processId);
        }
    }

    private static void ForceKill(int processId)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using var process = Process.GetProcessById(processId);
                process.Kill();
            }
            else if (IsUnix())
            {
                // Unix/Linux: Use kill() system call to send SIGKILL (signal 9)
                KillProcessUnix(processId);
            }
            else
            {
                // Other platforms fall back to Process.Kill()
                using var process = Process.GetProcessById(processId);
                process.Kill();
            }
        }
        catch (ArgumentException)
        {
            // Process does not exist, silently return
            return;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to force kill process {processId}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Attempts to force terminate the specified processes
    /// </summary>
    /// <param name="processIds">Array of process IDs to force terminate</param>
    /// <returns>True if all processes were successfully terminated, false otherwise</returns>
    public static bool TryForceKill(params int[] processIds)
    {
        try
        {
            ForceKill(processIds);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Force terminates all processes with the specified names
    /// </summary>
    /// <param name="processNames">Array of process names to force terminate</param>
    /// <exception cref="Exception">Thrown when process termination fails</exception>
    public static void ForceKill(params string[] processNames)
    {
        foreach (var processName in processNames)
        {
            ForceKill(processName);
        }
    }

    private static void ForceKill(string processName)
    {
        try
        {
            var processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0)
            {
                // No processes found, silently return
                return;
            }

            Exception? lastException = null;
            foreach (var process in processes)
            {
                try
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        process.Kill();
                    }
                    else if (IsUnix())
                    {
                        // Unix/Linux: Use kill() system call to send SIGKILL (signal 9)
                        KillProcessUnix(process.Id);
                    }
                    else
                    {
                        // Other platforms fall back to Process.Kill()
                        process.Kill();
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }
                finally
                {
                    process.Dispose();
                }
            }

            if (lastException != null)
            {
                throw new Exception(
                    $"Failed to force kill some processes named {processName}",
                    lastException
                );
            }
        }
        catch (Exception ex) when (!ex.Message.Contains("Failed to force kill"))
        {
            throw new Exception(
                $"Failed to force kill processes named {processName}: {ex.Message}",
                ex
            );
        }
    }

    /// <summary>
    /// Attempts to force terminate all processes with the specified names
    /// </summary>
    /// <param name="processNames">Array of process names to force terminate</param>
    /// <returns>True if all processes were successfully terminated, false otherwise</returns>
    public static bool TryForceKill(params string[] processNames)
    {
        try
        {
            ForceKill(processNames);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
