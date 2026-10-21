using PDML.Compiler.Binding.Nodes.Members;
using PDML.Compiler.Common;

namespace PDML.Compiler.Binding.Nodes;

public sealed class BoundModule : BoundNode
{
    public IReadOnlyList<BoundTypeDefinition> Types { get; internal set; }

    public BoundModule(Location? location, IReadOnlyList<BoundTypeDefinition> types) : base(location)
    {
        Types = types;
    }
}