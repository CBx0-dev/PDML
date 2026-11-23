using PDML.Compiler.Binding.Nodes.Members;

namespace PDML.Compiler.Binding.Types;

public sealed class EnumDefinitionTypeRef : TypeRef
{
    public BoundEnumDefinition Definition { get; }

    public EnumDefinitionTypeRef(BoundEnumDefinition definition)
    {
        Definition = definition;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not EnumDefinitionTypeRef other)
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