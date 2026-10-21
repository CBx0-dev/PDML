using PDML.Compiler.Common;

namespace PDML.Compiler.Parsing.Nodes;

public abstract record Node
{
    public abstract required Location Location { get; init; }
}