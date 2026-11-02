using PDML.Compiler.Diagnostics;

namespace PDML.Generator.CSharp.Roslyn;

internal sealed class RoslynDiagnosticService : IDiagnosticService
{
    public DiagnosticBag CreateDiagnosticBag() => new RoslynDiagnosticBag();
}
