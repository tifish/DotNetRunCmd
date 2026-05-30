namespace DotNetRunCmd.Tests;

using DotNetRun;

public class EnvVarTests
{
    [Fact]
    public void SetGetAndRemoveEnvVar_UseProcessTarget()
    {
        var name = $"DNRUN_TEST_{Guid.NewGuid():N}";
        using var scope = new ProcessEnvironmentVariableScope(name);

        Cmd.SetEnvVar(name, "value", EnvironmentVariableTarget.Process);

        Assert.Equal("value", Cmd.GetEnvVar(name));
        Assert.Equal("value", Cmd.GetEnvVar(name, EnvironmentVariableTarget.Process));

        Cmd.RemoveEnvVar(name, EnvironmentVariableTarget.Process);

        Assert.Equal("", Cmd.GetEnvVar(name, EnvironmentVariableTarget.Process));
    }

    [Fact]
    public void ExpandEnvVar_ExpandsNestedEnvironmentVariables()
    {
        var inner = $"DNRUN_INNER_{Guid.NewGuid():N}";
        var outer = $"DNRUN_OUTER_{Guid.NewGuid():N}";
        using var innerScope = new ProcessEnvironmentVariableScope(inner, "expanded");
        using var outerScope = new ProcessEnvironmentVariableScope(outer, $"%{inner}%");

        var expanded = Cmd.ExpandEnvVar($"before-%{outer}%-after");

        Assert.Equal("before-expanded-after", expanded);
    }

    [Fact]
    public void UpdateEnvVarFromSystem_WritesErrorWhenVariableIsMissing()
    {
        var name = $"DNRUN_MISSING_{Guid.NewGuid():N}";
        using var scope = new ProcessEnvironmentVariableScope(name);
        var originalError = Console.Error;
        using var writer = new StringWriter();

        try
        {
            Console.SetError(writer);
            Cmd.UpdateEnvVarFromSystem(name);
        }
        finally
        {
            Console.SetError(originalError);
        }

        Assert.Contains($"Environment variable {name} not found", writer.ToString());
        Assert.Null(Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process));
    }

    [Fact]
    public void BroadcastEnvChange_DoesNotThrow()
    {
        Cmd.BroadcastEnvChange(1);
    }
}
