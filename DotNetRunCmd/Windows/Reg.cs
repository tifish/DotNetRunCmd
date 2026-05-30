using System.Runtime.Versioning;
using Microsoft.Win32;

namespace DotNetRun;

[SupportedOSPlatform("windows")]
public static class Reg
{
    public static string? GetValue(string keyName, string? valueName, string? defaultValue)
    {
        var value = Registry.GetValue(keyName, valueName, defaultValue);
        return value as string ?? defaultValue;
    }

    public static int GetValue(string keyName, string? valueName, int defaultValue)
    {
        var value = Registry.GetValue(keyName, valueName, defaultValue);
        return value switch
        {
            int intValue => intValue,
            long longValue when longValue is >= int.MinValue and <= int.MaxValue => (int)longValue,
            string stringValue when int.TryParse(stringValue, out var parsedValue) => parsedValue,
            _ => defaultValue,
        };
    }

    public static byte[]? GetBinaryValue(string keyName, string? valueName, byte[]? defaultValue)
    {
        var value = Registry.GetValue(keyName, valueName, null);
        return value as byte[] ?? defaultValue;
    }

    public static void SetValue(string keyName, string? valueName, string value)
    {
        var currentValue = Registry.GetValue(keyName, valueName, null);
        if (currentValue is string currentString && currentString == value)
            return;

        Registry.SetValue(keyName, valueName, value, RegistryValueKind.String);
    }

    public static void SetValue(string keyName, string? valueName, int value)
    {
        var currentValue = Registry.GetValue(keyName, valueName, null);
        if (currentValue is int currentInt && currentInt == value)
            return;

        Registry.SetValue(keyName, valueName, value, RegistryValueKind.DWord);
    }

    public static void SetBinaryValue(string keyName, string? valueName, byte[] value)
    {
        var currentValue = Registry.GetValue(keyName, valueName, null);
        if (currentValue is byte[] currentBytes && currentBytes.SequenceEqual(value))
            return;

        Registry.SetValue(keyName, valueName, value, RegistryValueKind.Binary);
    }

    public static void SetValue(
        string keyName,
        string? valueName,
        object value,
        RegistryValueKind valueKind
    )
    {
        Registry.SetValue(keyName, valueName, value, valueKind);
    }

    public static RegistryKey GetBaseKeyFromKeyName(string keyName, out string subKeyName)
    {
        var num1 = keyName.IndexOf('\\');
        var num2 = num1 != -1 ? num1 : keyName.Length;
        var baseKeyFromKeyName = num2 switch
        {
            10 => Registry.Users,
            17 => char.ToUpperInvariant(keyName[6]) == 'L'
                ? Registry.ClassesRoot
                : Registry.CurrentUser,
            18 => Registry.LocalMachine,
            19 => Registry.CurrentConfig,
            21 => Registry.PerformanceData,
            _ => null,
        };

        if (
            baseKeyFromKeyName == null
            || !keyName.StartsWith(baseKeyFromKeyName.Name, StringComparison.OrdinalIgnoreCase)
        )
            throw new ArgumentException($"Invalid key name: {keyName}");
        subKeyName =
            num1 == -1 || num1 == keyName.Length
                ? string.Empty
                : keyName.Substring(num1 + 1, keyName.Length - num1 - 1);

        return baseKeyFromKeyName;
    }

    public static void DeleteKey(string keyName)
    {
        var rootKey = GetBaseKeyFromKeyName(keyName, out var subKeyPath);
        var subKey = rootKey.OpenSubKey(subKeyPath);
        if (subKey == null)
            return;

        subKey.Close();
        rootKey.DeleteSubKeyTree(subKeyPath, false);
    }

    public static void DeleteValue(string keyName, string valueName)
    {
        using var key = OpenKey(keyName, true);
        key?.DeleteValue(valueName, false);
    }

    public static RegistryKey? OpenKey(string keyName, bool writable = false)
    {
        var rootKey = GetBaseKeyFromKeyName(keyName, out var keyPath);
        return rootKey.OpenSubKey(keyPath, writable);
    }
}
