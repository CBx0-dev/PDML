using PDML.Compiler.Diagnostics;

namespace PDML.Commands.Analyze;

internal sealed class AnalyzeResult : TaskResult
{
    public required IReadOnlyList<Diagnostic> Diagnostics { get; init; }
}