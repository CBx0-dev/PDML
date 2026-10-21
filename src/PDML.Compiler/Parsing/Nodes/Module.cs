using PDML.Compiler.Common;
using PDML.Compiler.Lexing;

namespace PDML.Compiler.Parsing.Nodes;

public record Module : Node
{
    public override required Location Location { get; init; }

    public required List<Member> Members { get; init; }
    
    public required Token Eof { get; init; }
}