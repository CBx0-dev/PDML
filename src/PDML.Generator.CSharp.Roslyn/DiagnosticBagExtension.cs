using Microsoft.CodeAnalysis.Text;
using PDML.Compiler.Common;
using PDML.Compiler.Diagnostics;

namespace PDML.Generator.CSharp.Roslyn;

internal static class DiagnosticBagExtension
{
    public static Microsoft.CodeAnalysis.Diagnostic[] ToRoslynDiagnostics(this DiagnosticBag diagnostics)
    {
        List<Microsoft.CodeAnalysis.Diagnostic> roslynDiagnostics = new List<Microsoft.CodeAnalysis.Diagnostic>();

        foreach (Diagnostic diagnostic in diagnostics)
        {
            roslynDiagnostics.Add(MapDiagnostic(diagnostic));
        }

        return roslynDiagnostics.ToArray();
    }

    private static Microsoft.CodeAnalysis.Diagnostic MapDiagnostic(Diagnostic diagnostic)
    {
        string id = "PDML" + diagnostic.Descriptor.Code;

        Microsoft.CodeAnalysis.DiagnosticSeverity severity = diagnostic.Descriptor.Severity switch
        {
            DiagnosticSeverity.Info => Microsoft.CodeAnalysis.DiagnosticSeverity.Info,
            DiagnosticSeverity.Warning => Microsoft.CodeAnalysis.DiagnosticSeverity.Warning,
            DiagnosticSeverity.Error => Microsoft.CodeAnalysis.DiagnosticSeverity.Error,
            _ => throw new ArgumentException("Unknown severity", nameof(diagnostic.Descriptor.Severity))
        };

        Microsoft.CodeAnalysis.Location location = MapLocation(diagnostic.Location);
        Microsoft.CodeAnalysis.Location[] additonalLocation = diagnostic.AdditionalLocations
            .Select(MapLocation)
            .ToArray();

        Microsoft.CodeAnalysis.DiagnosticDescriptor roslynDescriptor = new Microsoft.CodeAnalysis.DiagnosticDescriptor(
            id: id,
            title: id,
            messageFormat: "{0}",
            category: diagnostic.Descriptor.Step.ToString(),
            defaultSeverity: severity,
            isEnabledByDefault: true
        );

        return Microsoft.CodeAnalysis.Diagnostic.Create(
            descriptor: roslynDescriptor,
            location: location,
            additionalLocations: additonalLocation,
            messageArgs: [diagnostic.Message]
        );
    }

    private static Microsoft.CodeAnalysis.Location MapLocation(Location location)
    {
        SourceFile sourceFile = location.SourceFile;

        string filePath = sourceFile.FilePath;
        SourceText sourceText = SourceText.From(sourceFile.Content);

        int start = location.Start.Position;
        int end = location.End.Position;

        if (start < 0 ||
            end < start ||
            end > sourceText.Length
        )
        {
            throw new ArgumentOutOfRangeException(nameof(location), "The diagnostic span is outside the source text.");
        }

        TextSpan span = TextSpan.FromBounds(start, end);

        // Roslyn uses zero-based line and character positions.
        LinePositionSpan lineSpan = sourceText.Lines.GetLinePositionSpan(span);

        return Microsoft.CodeAnalysis.Location.Create(
            filePath,
            span,
            lineSpan
        );
    }
}