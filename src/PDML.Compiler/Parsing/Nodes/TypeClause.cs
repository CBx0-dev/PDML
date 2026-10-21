using PDML.Compiler.Common;
using PDML.Compiler.Lexing;

namespace PDML.Compiler.Parsing.Nodes;

public record TypeClause : Node
{
    public override required Location Location { get; init; }

    public required Token Colon { get; init; }
    public required TypeSignature TypeSignature { get; init; }
}