using PDML.Compiler.Binding.Types;
using PDML.Compiler.Common;

namespace PDML.Compiler.Binding.Nodes.Members;

public sealed class BoundTypeDefinition : BoundNode
{
    public string Name { get; internal set; }
    public TypeDefinitionTypeRef? InheritingType { get; internal set; }
    public IReadOnlyDictionary<string, BoundTypeDefinitionProperty> Properties { get; internal set; }

    public BoundTypeDefinition(
        Location? location,
        string name,
        TypeDefinitionTypeRef? inheritingType,
        IReadOnlyDictionary<string, BoundTypeDefinitionProperty> properties
    ) : base(location)
    {
        Name = name;
        InheritingType = inheritingType;
        Properties = properties;
    }
}