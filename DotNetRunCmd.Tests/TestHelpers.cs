namespace DotNetRunCmd.Tests;

using DotNetRun;

internal sealed class TemporaryDirectory : IDisposable
{
    public string DirectoryPath { get; } = Path.Combine(
        Path.GetTempPath(),
        "DotNetRunCmd.Tests",
        Guid.NewGuid().ToString("N")
    );

    public TemporaryDirectory()
    {
        Directory.CreateDirectory(DirectoryPath);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(DirectoryPath))
                Directory.Delete(DirectoryPath, true);
        }
        catch
        {
            // Best-effort cleanup for files that may still be held by child processes.
        }
    }
}

internal sealed class ProcessEnvironmentVariableScope : IDisposable
{
    private readonly string _name;
    private readonly string? _originalValue;

    public ProcessEnvironmentVariableScope(string name, string? value = null)
    {
        _name = name;
        _originalValue = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        if (value is not null)
            Set(value);
    }

    public void Set(string? value)
    {
        Environment.SetEnvironmentVariable(_name, value, EnvironmentVariableTarget.Process);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(
            _name,
            _originalValue,
            EnvironmentVariableTarget.Process
        );
    }
}

internal sealed class TemporaryRegistryKey : IDisposable
{
    public string KeyName { get; } =
        $@"HKEY_CURRENT_USER\Software\DotNetRunCmd.Tests\{Guid.NewGuid():N}";

    public TemporaryRegistryKey()
    {
        Reg.DeleteKey(KeyName);
    }

    public void Dispose()
    {
        try
        {
            Reg.DeleteKey(KeyName);
        }
        catch
        {
            // Best-effort cleanup for registry handles owned by the current process.
        }
    }
}
