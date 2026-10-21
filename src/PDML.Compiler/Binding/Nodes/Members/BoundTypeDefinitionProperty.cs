using PDML.Compiler.Binding.Types;
using PDML.Compiler.Common;

namespace PDML.Compiler.Binding.Nodes.Members;

public sealed class BoundTypeDefinitionProperty : BoundNode
{
    public TypeRef Type { get; }

    public BoundTypeDefinitionProperty(Location? location, TypeRef type) : base(location)
    {
        Type = type;
    }
}