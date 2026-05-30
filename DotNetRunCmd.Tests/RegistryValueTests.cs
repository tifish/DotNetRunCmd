namespace DotNetRunCmd.Tests;

using DotNetRun;
using Microsoft.Win32;
using DotNetRegistryValue = DotNetRun.RegistryValue;

public class RegistryValueTests
{
    [Fact]
    public void RegistryValue_ReadsWritesAndDeletesNamedValues()
    {
        using var key = new TemporaryRegistryKey();
        var value = new DotNetRegistryValue(key.KeyName, "Setting");

        Assert.False(value.HasKey());
        Assert.False(value.HasValue());

        value.SetValue("abc");

        Assert.True(value.HasKey());
        Assert.True(value.HasValue());
        Assert.Equal("abc", value.GetValue("fallback"));

        value.SetValue(12);
        Assert.Equal(12, value.GetValue(0));

        value.SetValue("expanded", RegistryValueKind.ExpandString);
        Assert.Equal("expanded", value.GetValue("fallback"));

        value.DeleteValue();
        Assert.False(value.HasValue());
    }

    [Fact]
    public void RegistryValue_EmptyValueNameTargetsDefaultValue()
    {
        using var key = new TemporaryRegistryKey();
        var value = new DotNetRegistryValue(key.KeyName, "");

        value.SetValue("default");

        Assert.Null(value.ValueName);
        Assert.Equal("default", value.GetValue("fallback"));
    }

    [Fact]
    public void RegistryValue_BinaryHelpers_ReadAndUpdateBits()
    {
        using var key = new TemporaryRegistryKey();
        var value = new DotNetRegistryValue(key.KeyName, "Flags");

        Assert.True(value.GetBitFromBinary(9, defaultValue: true));
        Assert.False(value.GetBitFromBinary(9, defaultValue: false));

        value.SetBitToBinary(9, true);

        Assert.True(value.GetBitFromBinary(9, defaultValue: false));
        Assert.False(value.GetBitFromBinary(8, defaultValue: false));
        Assert.Equal(new byte[] { 0, 2 }, value.GetBinaryValue(null));

        value.SetBitToBinary(9, false);

        Assert.False(value.GetBitFromBinary(9, defaultValue: true));
        Assert.Equal(new byte[] { 0, 0 }, value.GetBinaryValue(null));
    }

    [Fact]
    public void RegistryValue_DeleteKey_RemovesTheContainingKey()
    {
        using var key = new TemporaryRegistryKey();
        var value = new DotNetRegistryValue(key.KeyName, "Setting");
        value.SetValue("abc");

        value.DeleteKey();

        Assert.False(value.HasKey());
    }
}
