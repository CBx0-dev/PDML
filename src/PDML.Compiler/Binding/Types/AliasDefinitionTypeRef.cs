using PDML.Compiler.Binding.Nodes.Members;

namespace PDML.Compiler.Binding.Types;

public sealed class AliasDefinitionTypeRef : TypeRef
{
    public BoundAliasTypeDefinition Definition { get; }

    public AliasDefinitionTypeRef(BoundAliasTypeDefinition definition)
    {
        Definition = definition;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not AliasDefinitionTypeRef other)
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