using System.Runtime.Versioning;

namespace DotNetRun;

[SupportedOSPlatform("windows")]
public static class FileAssociation
{
    public static void SetFileOpenCommand(string extension, string progId, string command)
    {
        // OpenWithProgids, {extension}\shell\open\command, {progId}\shell\open\command must be set at the same time.
        Reg.SetValue(
            $@"HKEY_CURRENT_USER\Software\Classes\{extension}\OpenWithProgids",
            progId,
            ""
        );
        Reg.SetValue(
            $@"HKEY_CURRENT_USER\Software\Classes\{extension}\shell\open\command",
            "",
            command
        );
        Reg.SetValue(
            $@"HKEY_CURRENT_USER\Software\Classes\{progId}\shell\open\command",
            "",
            command
        );
        // Delete the user-selected association
        Reg.DeleteKey(
            $@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{extension}"
        );
    }

    public static void SetRunCsFile()
    {
        SetFileOpenCommand(
            ".cs",
            "csfile",
            """
            "C:\Program Files\dotnet\dotnet.exe" run "%1"
            """
        );
    }

    public static void SetRunShFile()
    {
        SetFileOpenCommand(
            ".sh",
            "bashfile",
            """
            cmd /s /c ""C:\Program Files\Git\bin\bash.exe" "%1" || pause"
            """
        );
    }
}
