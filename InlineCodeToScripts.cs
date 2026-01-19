#!/usr/bin/env dotnet
#:project DotNetRunCmd/DotNetRunCmd.csproj
using System.Diagnostics;
using static DotNetRun.Cmd;

try
{
    ChangeDir(ScriptDir);

    Echo("Loading template...");
    LoadTemplateLines();

    const string scriptListPath = "ScriptList.txt";
    if (!File.Exists(scriptListPath))
    {
        File.Create(scriptListPath).Close();
        Pause($"Add .cs file or directory to {scriptListPath}. Press any key to exit...");
        return 1;
    }

    Echo("Updating common functions for all .cs script files...");
    var scriptList = File.ReadAllLines(scriptListPath).ToList();

    var failedCount = 0;
    foreach (var script in scriptList)
    {
        if (!UpdateCsFiles(script))
            failedCount++;
    }

    Echo("Update complete.");
    if (failedCount > 0)
    {
        EchoError($"Failed to update {failedCount} files.");
        return 1;
    }

    return 0;
}
catch (Exception ex)
{
    EchoError(ex.Message);
    if (!Debugger.IsAttached)
        Console.ReadKey();
    return 1;
}

partial class Program
{
    private static List<string> _templateUsingLines = null!;
    private static List<string> _templateProgramLines = null!;

    private const string CmdClassLine = "public static partial class Cmd";

    private static void LoadTemplateLines()
    {
        var templatePath = Path.Combine(ScriptDir, @"DotNetRunCmd/DotNetRunCmd.cs");
        var lines = File.ReadAllLines(templatePath).ToList();

        // Skip namespace and blank lines
        var usingBeginIndex = 0;
        while (usingBeginIndex < lines.Count && lines[usingBeginIndex].StartsWith("namespace "))
        {
            usingBeginIndex++;
        }
        while (usingBeginIndex < lines.Count && string.IsNullOrWhiteSpace(lines[usingBeginIndex]))
        {
            usingBeginIndex++;
        }

        // Extract using statements
        var usingEndIndex = usingBeginIndex;
        while (
            usingEndIndex < lines.Count
            && (lines[usingEndIndex].StartsWith("using ") || lines[usingEndIndex].StartsWith("//"))
        )
        {
            usingEndIndex++;
        }
        _templateUsingLines = lines
            .Skip(usingBeginIndex)
            .Take(usingEndIndex - usingBeginIndex)
            .ToList();

        _templateUsingLines.Add("using static Cmd;");

        // Extract content after the Cmd class
        var templateStartIndex = lines.LastIndexOf(CmdClassLine);
        _templateProgramLines = lines.Skip(templateStartIndex).ToList();

        // Ensure a blank line at the end file
        if (_templateProgramLines[^1] != "")
        {
            _templateProgramLines.Add("");
        }
    }

    private static bool UpdateCsFiles(string csFileOrDirectory)
    {
        var csFiles = new List<string>();
        csFileOrDirectory = Path.GetFullPath(csFileOrDirectory);
        if (File.Exists(csFileOrDirectory))
        {
            csFiles.Add(csFileOrDirectory);
        }
        else if (Directory.Exists(csFileOrDirectory))
        {
            csFiles.AddRange(
                Directory.GetFiles(csFileOrDirectory, "*.cs", SearchOption.AllDirectories)
            );
        }
        else
        {
            EchoError($"File or directory not found: {csFileOrDirectory}");
            return false;
        }

        foreach (var file in csFiles)
        {
            if (file.Equals(ScriptPath, StringComparison.OrdinalIgnoreCase))
                continue;

            ModifyCsFile(file);
        }

        return true;
    }

    private static void ModifyCsFile(string csFilePath)
    {
        var fileLines = File.ReadAllLines(csFilePath).ToList();

        // Extract preprocessor statements at the beginning
        var preprocessEndIndex = 0;
        while (
            preprocessEndIndex < fileLines.Count && fileLines[preprocessEndIndex].StartsWith('#')
        )
        {
            preprocessEndIndex++;
        }

        // Skip blank lines
        var usingBeginIndex = preprocessEndIndex;
        while (usingBeginIndex < fileLines.Count && fileLines[usingBeginIndex].Trim() == "")
        {
            usingBeginIndex++;
        }
        var preprocessLines = fileLines.Take(usingBeginIndex).ToList();

        // Extract using statements
        var usingEndIndex = usingBeginIndex;
        while (
            usingEndIndex < fileLines.Count
            && (
                fileLines[usingEndIndex].StartsWith("using ")
                || fileLines[usingEndIndex].StartsWith("//")
            )
        )
        {
            usingEndIndex++;
        }
        var usingLines = fileLines
            .Skip(usingBeginIndex)
            .Take(usingEndIndex - usingBeginIndex)
            .ToList();

        // Merge template using statements
        foreach (var templateUsing in _templateUsingLines)
        {
            if (!usingLines.Contains(templateUsing))
            {
                usingLines.Add(templateUsing);
            }
        }

        // Align with csharpier formatting rules
        // Sort
        usingLines.Sort();

        // Move "using static" lines to the end
        var staticUsingLines = new List<string>();
        var nonStaticUsingLines = new List<string>();
        foreach (var line in usingLines)
        {
            if (line.TrimStart().StartsWith("using static"))
                staticUsingLines.Add(line);
            else
                nonStaticUsingLines.Add(line);
        }
        usingLines.Clear();
        usingLines.AddRange(nonStaticUsingLines);
        usingLines.AddRange(staticUsingLines);

        // Extract content before the partial class Program
        var partialClassIndex = fileLines.LastIndexOf(CmdClassLine);
        if (partialClassIndex >= 0)
        {
            var programLines = fileLines
                .Skip(usingEndIndex)
                .Take(partialClassIndex - usingEndIndex);

            // Replace using statements
            // Keep content before the partial class Program, then replace the rest with templateLines
            var newLines = preprocessLines
                .Concat(usingLines)
                .Concat(programLines)
                .Concat(_templateProgramLines)
                .ToList();

            if (newLines.SequenceEqual(fileLines))
            {
                Echo($"No update needed: {csFilePath}");
            }
            else
            {
                File.WriteAllText(csFilePath, string.Join("\n", newLines));
                Echo($"Update complete: {csFilePath}");
            }
        }
        else
        {
            Echo($"Cmd class not found: {csFilePath}");
        }
    }
}
