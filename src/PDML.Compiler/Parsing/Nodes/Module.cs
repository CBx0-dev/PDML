using PDML.Compiler.Common;
using PDML.Compiler.Lexing;
using PDML.Compiler.Parsing.Nodes.Members;

namespace PDML.Compiler.Parsing.Nodes;

public record Module : Node
{
    public IEnumerable<TypeDefinitionMember> TypeDefinitions => Members.OfType<TypeDefinitionMember>();
    public IEnumerable<VirtualTypeDefinitionMember> VirtualTypeDefinitions => Members.OfType<VirtualTypeDefinitionMember>();

    public override required Location Location { get; init; }

    public required List<Member> Members { get; init; }

    public required Token Eof { get; init; }
}