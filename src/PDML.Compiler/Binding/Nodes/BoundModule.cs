using PDML.Compiler.Binding.Nodes.Members;
using PDML.Compiler.Common;
using System.Diagnostics.CodeAnalysis;

namespace PDML.Compiler.Binding.Nodes;

public sealed class BoundModule : BoundNode
{
    public ModuleIdentity Identity { get; internal set; }
    public IReadOnlyList<BoundModule> Dependencies { get; internal set; }
    public IReadOnlyList<BoundTypeDefinition> Types { get; internal set; }

    public BoundModule(Location? location, ModuleIdentity identity, IEnumerable<BoundModule> dependencies, IEnumerable<BoundTypeDefinition> types) : base(location)
    {
        Identity = identity;
        Types = types.ToList();
        Dependencies = dependencies.ToList();
    }

    public bool TryGetSymbol(string symbolName, [NotNullWhen(true)] out BoundTypeDefinition? type)
    {
        type = Types.FirstOrDefault(type => type.Name == symbolName);
        return type is not null;
    }
}