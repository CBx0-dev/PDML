using PDML.Compiler.Parsing.Nodes;

namespace PDML.Compiler;

public record ModuleInformation
{
    public required Module Module { get; init; }

    public required IReadOnlyList<ModuleInformation> Dependencies { get; init; }
}
