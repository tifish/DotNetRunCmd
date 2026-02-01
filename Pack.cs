#!/usr/bin/env dotnet
#:project DotNetRunCmd/DotNetRunCmd.csproj

using System.Diagnostics;
using static DotNetRun.Cmd;

try
{
    var projectName = "DotNetRunCmd";

    Echo($"Packing {projectName}");
    Directory.Delete(Path.Combine(projectName, "bin", "Release"), true);
    Run("dotnet", $"pack {projectName} -c Release");

    Echo($"Adding to local test NuGet source");
    var localNugetPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".nuget-local-test"
    );
    Directory.CreateDirectory(localNugetPath);
    RunIgnoreExitCode("dotnet", $"nuget add source {localNugetPath} -n local-test");
    if (OperatingSystem.IsWindows())
        Robocopy($@"{projectName}\bin\Release", localNugetPath, "*.nupkg");
    else
        Run("rsync", $"{projectName}/bin/Release/*.nupkg {localNugetPath}/");

    return 0;
}
catch (Exception ex)
{
    EchoError(ex.Message);
    if (!Debugger.IsAttached)
        Console.ReadKey();
    return 1;
}
