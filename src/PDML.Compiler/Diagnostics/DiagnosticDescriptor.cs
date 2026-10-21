namespace PDML.Compiler.Diagnostics;

public sealed class DiagnosticDescriptor
{
    public required string MessageFormat { get; init; }
    public required int Code { get; init; }
    public required CompilationStep Step { get; init; }
    public required DiagnosticSeverity Severity { get; init; }
}