using PDML.Compiler.Binding.Types;
using PDML.Compiler.Common;

namespace PDML.Compiler.Binding.Nodes.Members;

public sealed class BoundAliasTypeDefinition : BoundNode
{
    public string Name { get; internal set; }
    public TypeRef AliasingType { get; internal set; }

    public BoundAliasTypeDefinition(Location? location, string name, TypeRef aliasingType) : base(location)
    {
        Name = name;
        AliasingType = aliasingType;
    }
}