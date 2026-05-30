namespace DotNetRunCmd.Tests;

using DotNetRun;
using System.Runtime.InteropServices;

public class PlatformTests
{
    [Fact]
    public void PlatformHelpers_MatchRuntimeInformation()
    {
        Assert.Equal(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), Cmd.IsWindows());
        Assert.Equal(RuntimeInformation.IsOSPlatform(OSPlatform.Linux), Cmd.IsLinux());
        Assert.Equal(RuntimeInformation.IsOSPlatform(OSPlatform.OSX), Cmd.IsMacOS());
        Assert.Equal(RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD), Cmd.IsFreeBSD());

        var isUnix =
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            || RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);
        Assert.Equal(isUnix, Cmd.IsUnix());
    }

    [Fact]
    public void IsWSL_IsFalseOnWindows()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Assert.False(Cmd.IsWSL);
    }
}
