using PDML.Compiler.Diagnostics.Abstractions;

namespace PDML.Plugin.LSP.Diagnostics;

public sealed class LspDiagnosticService : IDiagnosticService
{
    public DiagnosticBag CreateDiagnosticBag() => new LspDiagnosticBag();
}