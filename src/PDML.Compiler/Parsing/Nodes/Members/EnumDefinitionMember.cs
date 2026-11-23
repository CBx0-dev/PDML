using PDML.Compiler.Common;
using PDML.Compiler.Lexing;

namespace PDML.Compiler.Parsing.Nodes.Members;

public record EnumDefinitionMember : Member
{
    public override required Location Location { get; init; }

    public required Token EnumKeyword { get; init; }
    public required Token Name { get; init; }
    public required Token LBrace { get; init; }
    public required DelimitedList<EnumDefinitionItem, Token> Items { get; init; }
    public required Token RBrace { get; init; }
    public required Token Semicolon { get; init; }
}