using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using PDML.Compiler.Diagnostics.Abstractions;
using DiagnosticSeverity = PDML.Compiler.Diagnostics.Abstractions.DiagnosticSeverity;
using Location = PDML.Compiler.Common.Location;
using LspDiagnosticSeverity = OmniSharp.Extensions.LanguageServer.Protocol.Models.DiagnosticSeverity;
using LspLocation = OmniSharp.Extensions.LanguageServer.Protocol.Models.Location;
using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace PDML.Plugin.LSP.Diagnostics;

public sealed class LspDiagnosticBag : DiagnosticBag<LspDiagnostic>
{
    public override bool HasErrors => _diagnostics.Any(diagnostic => diagnostic.Severity == LspDiagnosticSeverity.Error);
    public override bool HasWarnings => _diagnostics.Any(diagnostic => diagnostic.Severity == LspDiagnosticSeverity.Warning);
    public override bool HasInfos => _diagnostics.Any(diagnostic => diagnostic.Severity == LspDiagnosticSeverity.Information);
    public int Count => _diagnostics.Count;

    public override void Report(DiagnosticDescriptor descriptor, Location location, Location[] additionalLocations, params object[] args)
    {
        LspDiagnosticSeverity severity = descriptor.Severity switch
        {
            DiagnosticSeverity.Info => LspDiagnosticSeverity.Information,
            DiagnosticSeverity.Warning => LspDiagnosticSeverity.Warning,
            DiagnosticSeverity.Error => LspDiagnosticSeverity.Error,
            _ => throw new ArgumentException("Unknown diagnostic severity")
        };

        LspRange range = ConvertLocation(location);
        Container<DiagnosticRelatedInformation>? relatedInformation = null;
        if (additionalLocations.Length > 0)
        {
            relatedInformation = new Container<DiagnosticRelatedInformation>(
                additionalLocations.Select(additionalLocation => new DiagnosticRelatedInformation
                {
                    Location = new LspLocation
                    {
                        Uri = DocumentUri.FromFileSystemPath(additionalLocation.SourceFile.FilePath),
                        Range = ConvertLocation(additionalLocation)
                    },
                    Message = "Related location"
                })
            );
        }

        _diagnostics.Add(new LspDiagnostic
        {
            Document = DocumentUri.FromFileSystemPath(location.SourceFile.FilePath),
            Range = range,
            Severity = severity,
            Code = new DiagnosticCode("PDML" + descriptor.Code),
            Source = "PDML",
            Message = string.Format(descriptor.MessageFormat, args),
            RelatedInformation = relatedInformation
        });
    }

    private LspRange ConvertLocation(Location location)
    {
        int startLine = Math.Max(0, location.Start.Line - 1);
        int startColumn = Math.Max(0, location.Start.Column - 1);
        int endLine = Math.Max(0, location.End.Line - 1);
        int endColumn = Math.Max(0, location.End.Column - 1);

        return new LspRange
        {
            Start = new Position(startLine, startColumn),
            End = new Position(endLine, endColumn)
        };
    }
}