using PDML.Compiler.Diagnostics.Abstractions;

namespace PDML.Generator.CSharp.Roslyn;

internal sealed class RoslynDiagnosticService : IDiagnosticService
{
    public DiagnosticBag CreateDiagnosticBag() => new RoslynDiagnosticBag();
}
