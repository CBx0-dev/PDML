using PDML.Compiler.Common;

namespace PDML.Compiler.Binding.Nodes.Members;

public sealed class BoundEnumDefinition : BoundNode
{
    public string Name { get; init; }
    public IReadOnlyDictionary<string, BoundEnumProperty> Properties { get; internal set; }

    public BoundEnumDefinition(
        Location? location,
        string name,
        IReadOnlyDictionary<string, BoundEnumProperty> properties
    ) : base(location)
    {
        Name = name;
        Properties = properties;
    }
}