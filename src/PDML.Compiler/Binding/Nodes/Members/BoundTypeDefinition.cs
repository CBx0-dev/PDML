using PDML.Compiler.Binding.Types;
using PDML.Compiler.Common;

namespace PDML.Compiler.Binding.Nodes.Members;

public sealed class BoundTypeDefinition : BoundNode
{
    public string Name { get; internal set; }
    public TypeDefinitionTypeRef? InheritingType { get; internal set; }
    public IReadOnlyDictionary<string, BoundTypeDefinitionProperty> Properties { get; internal set; }
    public bool IsVirtual { get; }

    public BoundTypeDefinition(
        Location? location,
        string name,
        TypeDefinitionTypeRef? inheritingType,
        IReadOnlyDictionary<string, BoundTypeDefinitionProperty> properties,
        bool isVirtual
    ) : base(location)
    {
        if (isVirtual && inheritingType is not null)
        {
            throw new ArgumentException("A virtual type cannot have inheriting type");
        }

        Name = name;
        InheritingType = inheritingType;
        Properties = properties;
        IsVirtual = isVirtual;
    }
}