using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using PDML.Compiler.Common;
using PDML.Compiler.Diagnostics;
using LspDiagnostic = OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic;
using LspDiagnosticSeverity = OmniSharp.Extensions.LanguageServer.Protocol.Models.DiagnosticSeverity;
using LspLocation = OmniSharp.Extensions.LanguageServer.Protocol.Models.Location;
using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace PDML.Plugin.LSP;

internal static class DiagnosticBagExtension
{
    public static LspDiagnostic[] ToLspDiagnostics(this DiagnosticBag diagnostics, DocumentUri? uri = null)
    {
        List<LspDiagnostic> lspDiagnostics = new List<LspDiagnostic>();

        foreach (PDML.Compiler.Diagnostics.Diagnostic diagnostic in diagnostics)
        {
            lspDiagnostics.Add(MapDiagnostic(diagnostic, uri));
        }

        return lspDiagnostics.ToArray();
    }

    private static LspDiagnostic MapDiagnostic(PDML.Compiler.Diagnostics.Diagnostic diag, DocumentUri? uri)
    {
        LspDiagnosticSeverity severity = diag.Descriptor.Severity switch
        {
            PDML.Compiler.Diagnostics.DiagnosticSeverity.Info => LspDiagnosticSeverity.Information,
            PDML.Compiler.Diagnostics.DiagnosticSeverity.Warning => LspDiagnosticSeverity.Warning,
            PDML.Compiler.Diagnostics.DiagnosticSeverity.Error => LspDiagnosticSeverity.Error,
            _ => LspDiagnosticSeverity.Error
        };

        LspRange range = MapRange(diag.Location);

        Container<DiagnosticRelatedInformation>? relatedInformation = null;
        if (diag.AdditionalLocations.Length > 0)
        {
            List<DiagnosticRelatedInformation> relatedList = new();
            foreach (PDML.Compiler.Common.Location additionalLoc in diag.AdditionalLocations)
            {
                DocumentUri targetUri = uri ?? DocumentUri.FromFileSystemPath(additionalLoc.SourceFile.FilePath);
                relatedList.Add(new DiagnosticRelatedInformation
                {
                    Location = new LspLocation
                    {
                        Uri = targetUri,
                        Range = MapRange(additionalLoc)
                    },
                    Message = "Related location"
                });
            }
            relatedInformation = new Container<DiagnosticRelatedInformation>(relatedList);
        }

        return new LspDiagnostic
        {
            Range = range,
            Severity = severity,
            Code = new DiagnosticCode("PDML" + diag.Descriptor.Code),
            Source = "pdml",
            Message = diag.Message,
            RelatedInformation = relatedInformation
        };
    }

    private static LspRange MapRange(PDML.Compiler.Common.Location location)
    {
        int startLine = Math.Max(0, location.Start.Line - 1);
        int startCol = Math.Max(0, location.Start.Column - 1);
        int endLine = Math.Max(0, location.End.Line - 1);
        int endCol = Math.Max(0, location.End.Column - 1);

        return new LspRange(new Position(startLine, startCol), new Position(endLine, endCol));
    }
}
