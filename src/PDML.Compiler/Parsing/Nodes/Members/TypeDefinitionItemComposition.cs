using PDML.Compiler.Common;
using PDML.Compiler.Lexing;

namespace PDML.Compiler.Parsing.Nodes.Members;

public record TypeDefinitionItemTarget : TypeDefinitionItem
{
    public override required Location Location { get; init; }

    public required TypeSignature Type { get; init; }
}