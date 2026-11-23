using PDML.Compiler.Common;

namespace PDML.Compiler.Binding.Nodes.Members;

public sealed class BoundEnumProperty : BoundNode
{
    public int Value { get; internal set; }
    
    public BoundEnumProperty(Location? location, int value) : base(location)
    {
        Value = value;
    }
}