namespace DotNetRunCmd.Tests;

using DotNetRun;
using System.Diagnostics;

public class KillTests
{
    [Fact]
    public void KillAndForceKill_IgnoreMissingProcessIds()
    {
        Cmd.Kill(int.MaxValue);
        Cmd.ForceKill(int.MaxValue);

        Assert.True(Cmd.TryKill(int.MaxValue));
        Assert.True(Cmd.TryForceKill(int.MaxValue));
    }

    [Fact]
    public void KillAndForceKill_IgnoreMissingProcessNames()
    {
        var missingProcessName = $"dnrun-missing-{Guid.NewGuid():N}";

        Cmd.Kill(missingProcessName);
        Cmd.ForceKill(missingProcessName);

        Assert.True(Cmd.TryKill(missingProcessName));
        Assert.True(Cmd.TryForceKill(missingProcessName));
    }

    [Fact]
    public void Kill_TerminatesProcessById()
    {
        using var process = StartLongRunningProcess();

        Cmd.Kill(process.Id);

        Assert.True(process.WaitForExit(5000));
    }

    [Fact]
    public void ForceKill_TerminatesProcessById()
    {
        using var process = StartLongRunningProcess();

        Cmd.ForceKill(process.Id);

        Assert.True(process.WaitForExit(5000));
    }

    private static Process StartLongRunningProcess()
    {
        var process =
            Process.Start(
                new ProcessStartInfo("powershell.exe", "-NoProfile -Command Start-Sleep -Seconds 60")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                }
            ) ?? throw new InvalidOperationException("Failed to start test process.");
        return process;
    }
}
