#!/usr/bin/env dotnet
#:project DotNetRunCmd/DotNetRunCmd.csproj
#:property TargetFramework=net10.0-windows

using System.Diagnostics;
using DotNetRun;
using static DotNetRun.Cmd;

try
{
    FileAssociation.SetRunCsFile();
    return 0;
}
catch (Exception ex)
{
    EchoError(ex.Message);
    if (!Debugger.IsAttached)
        Pause("Press any key to exit...");
    return 1;
}
