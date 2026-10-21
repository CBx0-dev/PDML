using PDML.Compiler.Common;
using PDML.Compiler.Lexing;

namespace PDML.Compiler.Parsing.Nodes.Members;

public record TypeDefinitionBodyProperty : Node
{
    public override required Location Location { get; init; }

    public required Token Name { get; init; }
    public required TypeClause TypeClause { get; init; }
    public required Token Semicolon { get; init; }
}