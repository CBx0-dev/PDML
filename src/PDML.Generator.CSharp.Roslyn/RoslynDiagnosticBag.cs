using Microsoft.CodeAnalysis.Text;
using PDML.Compiler.Common;
using PDML.Compiler.Diagnostics;

namespace PDML.Generator.CSharp.Roslyn;

internal sealed class RoslynDiagnosticBag : DiagnosticBag<Microsoft.CodeAnalysis.Diagnostic>
{
    public override bool HasErrors => _diagnostics.Any(diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);

    public override bool HasWarnings => _diagnostics.Any(diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);

    public override bool HasInfos => _diagnostics.Any(diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Info);

    public override void Report(DiagnosticDescriptor descriptor, Location location, Location[] additionalLocations, params object[] args)
    {
        if (args.Length != descriptor.ArgumentCount)
        {
            throw new ArgumentException($"Expected {descriptor.ArgumentCount} arguments, but got {args.Length}.", nameof(args));
        }

        string id = "PDML" + descriptor.Code;
        Microsoft.CodeAnalysis.DiagnosticSeverity severity = descriptor.Severity switch
        {
            DiagnosticSeverity.Info => Microsoft.CodeAnalysis.DiagnosticSeverity.Info,
            DiagnosticSeverity.Warning => Microsoft.CodeAnalysis.DiagnosticSeverity.Warning,
            DiagnosticSeverity.Error => Microsoft.CodeAnalysis.DiagnosticSeverity.Error,
            _ => throw new ArgumentException("Unknown severity", nameof(descriptor.Severity))
        };

        Microsoft.CodeAnalysis.Location roslynLocation = MapLocation(location);
        Microsoft.CodeAnalysis.Location[] roslynAdditonalLocation = additionalLocations
            .Select(MapLocation)
            .ToArray();

        Microsoft.CodeAnalysis.DiagnosticDescriptor roslynDescriptor = new Microsoft.CodeAnalysis.DiagnosticDescriptor(
            id: id,
            title: id,
            messageFormat: "{0}",
            category: descriptor.Step.ToString(),
            defaultSeverity: severity,
            isEnabledByDefault: true
        );

        _diagnostics.Add(Microsoft.CodeAnalysis.Diagnostic.Create(
            descriptor: roslynDescriptor,
            location: roslynLocation,
            additionalLocations: roslynAdditonalLocation,
            messageArgs: [string.Format(descriptor.MessageFormat, args)]
        ));
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