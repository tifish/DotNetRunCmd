namespace DotNetRunCmd.Tests;

using DotNetRun;
using Microsoft.Win32;

public class RegTests
{
    [Fact]
    public void SetAndGetValue_HandleStringIntAndBinaryValues()
    {
        using var key = new TemporaryRegistryKey();
        var binary = new byte[] { 1, 2, 3 };

        Reg.SetValue(key.KeyName, "Text", "abc");
        Reg.SetValue(key.KeyName, "Number", 42);
        Reg.SetValue(key.KeyName, "NumericText", "123", RegistryValueKind.String);
        Reg.SetBinaryValue(key.KeyName, "Bytes", binary);

        Assert.Equal("abc", Reg.GetValue(key.KeyName, "Text", "fallback"));
        Assert.Equal("fallback", Reg.GetValue(key.KeyName, "Missing", "fallback"));
        Assert.Equal(42, Reg.GetValue(key.KeyName, "Number", 0));
        Assert.Equal(123, Reg.GetValue(key.KeyName, "NumericText", 0));
        Assert.Equal(9, Reg.GetValue(key.KeyName, "MissingNumber", 9));
        Assert.Equal(binary, Reg.GetBinaryValue(key.KeyName, "Bytes", null));
        Assert.Equal(binary, Reg.GetBinaryValue(key.KeyName, "MissingBytes", binary));
    }

    [Fact]
    public void OpenKeyDeleteValueAndDeleteKey_UpdateRegistryTree()
    {
        using var key = new TemporaryRegistryKey();
        Reg.SetValue(key.KeyName, "Text", "abc");

        using (var opened = Reg.OpenKey(key.KeyName))
        {
            Assert.NotNull(opened);
        }

        Reg.DeleteValue(key.KeyName, "Text");

        Assert.Equal("fallback", Reg.GetValue(key.KeyName, "Text", "fallback"));

        Reg.SetValue($@"{key.KeyName}\Child", "Text", "abc");
        Reg.DeleteKey(key.KeyName);

        Assert.Null(Reg.OpenKey(key.KeyName));
    }

    [Fact]
    public void GetBaseKeyFromKeyName_ParsesSupportedRegistryRoots()
    {
        var root = Reg.GetBaseKeyFromKeyName(
            @"HKEY_CURRENT_USER\Software",
            out var subKeyName
        );

        Assert.Equal(Registry.CurrentUser.Name, root.Name);
        Assert.Equal("Software", subKeyName);
        Assert.Throws<ArgumentException>(() => Reg.GetBaseKeyFromKeyName("BADROOT\\Key", out _));
    }
}
