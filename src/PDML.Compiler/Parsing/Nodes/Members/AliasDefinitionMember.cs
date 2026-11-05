using PDML.Compiler.Common;
using PDML.Compiler.Lexing;

namespace PDML.Compiler.Parsing.Nodes.Members;

public record AliasDefinitionMember : Member
{
    public override required Location Location { get; init; }

    public required Token AliasKeyword { get; init; }
    public required Token Name { get; init; }
    public required Token Equal { get; init; }
    public required TypeSignature AliasingType { get; init; }
    public required Token Semicolon { get; init; }
}