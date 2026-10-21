using PDML.Compiler.Common;

namespace PDML.Compiler.Diagnostics;

public sealed record Diagnostic
{
    public required Location Location { get; init; }
    public required string Message { get; init; }
    public required DiagnosticDescriptor Descriptor { get; init; }
}