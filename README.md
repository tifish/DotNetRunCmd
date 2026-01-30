# DotNetRunCmd

A utility library for writing C# scripts as easily as batch (`.cmd` or `.bat`) files.

## Introduction

.NET 10 SDK introduces the ability to run a single `.cs` file directly, similar to how you run a `.cmd` or `.bat` script. This is an exciting feature, but writing scripts directly is still not convenient enough.

**DotNetRunCmd** provides a set of practical command-line helper methods so that you can write C# script files as conveniently as traditional batch scripts.

## Getting Started

Create a `.cs` file, for example `hello.cs`:

```cs
#!/usr/bin/env dotnet
#:package DotNetRunCmd@*
using static DotNetRun.Cmd;

try
{
    Echo("Hello, World!");

    if (!InPath("dotnet"))
    {
        EchoError("Please install .NET SDK first.");
        return 1;
    }

    Run("dotnet", "--info");

    return 0;
}
catch (Exception ex)
{
    EchoError(ex.Message);
    Pause();
    return 1;
}
```

Run the script:

```bash
dotnet hello.cs
```

## License

MIT
