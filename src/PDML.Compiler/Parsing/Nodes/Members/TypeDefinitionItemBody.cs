using PDML.Compiler.Common;
using PDML.Compiler.Lexing;

namespace PDML.Compiler.Parsing.Nodes.Members;

public record TypeDefinitionItemBody : TypeDefinitionItem
{
    public override required Location Location { get; init; }

    public required Token LBrace { get; init; }
    public required List<TypeDefinitionBodyProperty> Properties { get; init; }
    public required Token RBrace { get; init; }
}