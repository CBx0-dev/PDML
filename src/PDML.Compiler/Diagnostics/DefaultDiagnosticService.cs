namespace PDML.Compiler.Diagnostics;

public sealed class DefaultDiagnosticService : IDiagnosticService
{
    public DiagnosticBag CreateDiagnosticBag() => new DefaultDiagnosticBag();
}
