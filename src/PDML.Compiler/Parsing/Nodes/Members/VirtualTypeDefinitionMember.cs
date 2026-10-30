using PDML.Compiler.Common;
using PDML.Compiler.Lexing;

namespace PDML.Compiler.Parsing.Nodes.Members;

public record VirtualTypeDefinitionMember : Member
{
    public override required Location Location { get; init; }
    
    public required Token VirtualKeyword { get; init; }
    public required Token TypeKeyword { get; init; }
    public required Token Name { get; init; }
    public required Token Equal { get; init; }
    public required DelimitedList<TypeDefinitionItem, Token> Items { get; init; }
    public required Token Semicolon { get; init; }
}