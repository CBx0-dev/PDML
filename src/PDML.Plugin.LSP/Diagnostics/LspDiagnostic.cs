using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace PDML.Plugin.LSP.Diagnostics;

public record LspDiagnostic : Diagnostic
{
    public required DocumentUri Document { get; init; }
}