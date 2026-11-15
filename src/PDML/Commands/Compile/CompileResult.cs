using PDML.Compiler.Diagnostics;
using PDML.Generator.Abstractions;

namespace PDML.Commands.Compile;

internal sealed class CompileResult : TaskResult
{
    public required IReadOnlyList<Diagnostic> Diagnostics { get; init; }
    public required IReadOnlyList<OutputFile> OutputFiles { get; init; }
}