using PDML.Compiler.Common;
using PDML.Compiler.Lexing;

namespace PDML.Compiler.Parsing.Nodes.Members;

public record TypeDefinitionMember : Member
{
    public override required Location Location { get; init; }

    public required Token TypeKeyword { get; init; }
    public required Token Name { get; init; }
    public required Token? Colon { get; init; }
    public required TypeSignature? InheritingType { get; init; }
    public required Token Equal { get; init; }
    public required DelimitedList<TypeDefinitionItem, Token> Items { get; init; }
    public required Token Semicolon { get; init; }
}