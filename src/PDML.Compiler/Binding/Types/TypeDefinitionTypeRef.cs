using PDML.Compiler.Binding.Nodes.Members;

namespace PDML.Compiler.Binding.Types;

public sealed class TypeDefinitionTypeRef : TypeRef
{
    public BoundTypeDefinition Definition { get; }

    public TypeDefinitionTypeRef(BoundTypeDefinition definition)
    {
        Definition = definition;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not TypeDefinitionTypeRef other)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Definition == other.Definition;
    }

    public override int GetHashCode() => Definition.GetHashCode();
}