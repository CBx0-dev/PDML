using PDML.Compiler.Common;
using PDML.Compiler.Diagnostics;
using Spectre.Console;
using System.Drawing;

namespace PDML;

internal static class DiagnosticPrinter
{
    public static void Print(DiagnosticBag diagnostics)
    {
        foreach (Diagnostic diagnostic in diagnostics)
        {
            Print(diagnostic);
        }
    }

    private static void Print(Diagnostic diagnostic)
    {
        Location location = diagnostic.Location;
        DiagnosticDescriptor descriptor = diagnostic.Descriptor;
        SourceFile sourceFile = location.SourceFile;

        string severity;
        string severityColor;

        switch (descriptor.Severity)
        {
            case DiagnosticSeverity.Info:
                severity = "Info";
                severityColor = "cyan";
                break;
            case DiagnosticSeverity.Warning:
                severity = "Warning";
                severityColor = "yellow";
                break;
            case DiagnosticSeverity.Error:
                severity = "Error";
                severityColor = "red";
                break;
            default:
                throw new ArgumentException("Unknown severity levle", nameof(descriptor.Severity));
        }

        AnsiConsole.MarkupLine(
            $"[grey]{Markup.Escape(sourceFile.FilePath)}" +
            $"({location.Start.Line}:{location.Start.Column}):[/] " +
            $"[{severityColor}]{severity}[/] " +
            $"[grey]PDML{descriptor.Code}:[/] " +
            $"{Markup.Escape(diagnostic.Message)}"
        );

        Print(location, severityColor);

        if (diagnostic.AdditionalLocations.Length > 0)
        {
            AnsiConsole.WriteLine();
            foreach (Location additonalLocation in diagnostic.AdditionalLocations)
            {
                AnsiConsole.MarkupLine(
                    $"  [grey]Related locations: " +
                    $"{Markup.Escape(additonalLocation.SourceFile.FilePath)}" +
                    $"({additonalLocation.Start.Line}:{additonalLocation.Start.Column})[/]"
                );
                Print(additonalLocation, severityColor);
            }
        } 

        AnsiConsole.WriteLine();
    }

    private static void Print(Location location, string severityColor)
    {
        SourceFile sourceFile = location.SourceFile;

        if (location.Start.Line <= 0 || location.Start.Column <= 0)
        {
            return;
        }

        string[] lines = sourceFile.Content.Split(["\r\n", "\n", "\r"], StringSplitOptions.None);
        if (lines.Length == 0)
        {
            return;
        }

        int startLine = Math.Clamp(location.Start.Line, 1, lines.Length);
        int endLine = Math.Clamp(location.End.Line, startLine, lines.Length);

        int firstLine = Math.Max(1, startLine - 1);
        int lastLine = Math.Min(lines.Length, endLine + 1);

        int lineNumberWidth = lastLine.ToString().Length;

        for (int lineNumber = firstLine; lineNumber <= lastLine; lineNumber++)
        {
            string sourceLine = lines[lineNumber - 1];
            bool isDiagnosticLine = lineNumber >= startLine && lineNumber <= endLine;

            string lineNumberText = lineNumber.ToString().PadLeft(lineNumberWidth);

            AnsiConsole.MarkupLine($"[grey]{lineNumberText}| [/] {Markup.Escape(sourceLine)}");
            
            if (!isDiagnosticLine)
            {
                continue;
            }

            int startColumn = lineNumber == startLine
                ? location.Start.Column
                : 1;
            int endColumn = lineNumber == endLine
                ? location.End.Column
                : sourceLine.Length + 1;

            startColumn = Math.Clamp(startColumn, 1, sourceLine.Length + 1);
            endColumn = Math.Clamp(endColumn, startColumn, sourceLine.Length + 1);

            int underlineLength = Math.Max(1, endColumn - startColumn);
          
            AnsiConsole.MarkupLine(
                $"[grey]{new string(' ', lineNumberWidth)}| " +
                $"{new string(' ', startColumn)}[/]" +
                $"[{severityColor}]{new string('^', underlineLength)}[/]"
            );
        }
    }
}
