using PDML.Compiler.Common;

namespace PDML.Compiler.Binding.Nodes;

public abstract class BoundNode
{
    public Location? Location { get; internal set; }

    protected BoundNode(Location? location)
    {
        Location = location;
    }
}