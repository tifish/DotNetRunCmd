namespace DotNetRunCmd.Tests;

using DotNetRun;

public class FileAssociationTests
{
    [Fact]
    public void SetFileOpenCommand_WritesAssociationKeysAndClearsUserChoice()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var extension = $".dnrun{suffix}";
        var progId = $"dnrunfile{suffix}";
        var command = $@"""C:\Tools\dnrun-{suffix}.exe"" ""%1""";
        var extensionClassKey = $@"HKEY_CURRENT_USER\Software\Classes\{extension}";
        var progIdKey = $@"HKEY_CURRENT_USER\Software\Classes\{progId}";
        var fileExtsKey =
            $@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{extension}";

        try
        {
            Reg.SetValue(fileExtsKey, "UserChoice", "old");

            FileAssociation.SetFileOpenCommand(extension, progId, command);

            Assert.Equal(
                "",
                Reg.GetValue($@"{extensionClassKey}\OpenWithProgids", progId, "missing")
            );
            Assert.Equal(
                command,
                Reg.GetValue($@"{extensionClassKey}\shell\open\command", "", "missing")
            );
            Assert.Equal(
                command,
                Reg.GetValue($@"{progIdKey}\shell\open\command", "", "missing")
            );
            Assert.Null(Reg.OpenKey(fileExtsKey));
        }
        finally
        {
            Reg.DeleteKey(extensionClassKey);
            Reg.DeleteKey(progIdKey);
            Reg.DeleteKey(fileExtsKey);
        }
    }
}
