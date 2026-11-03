using PDML.Compiler.Binding.Nodes.Members;
using PDML.Compiler.Common;
using System.Diagnostics.CodeAnalysis;

namespace PDML.Compiler.Binding.Nodes;

public sealed class BoundModule : BoundNode
{
    public ModuleIdentity Identity { get; internal set; }
    public IReadOnlyDictionary<BoundModule, IReadOnlyList<BoundTypeDefinition>> Imports { get; internal set; }
    public IReadOnlyList<BoundTypeDefinition> Types { get; internal set; }

    public IReadOnlyList<BoundModule> Dependencies => Imports.Keys.ToList();

    public BoundModule(
        Location? location,
        ModuleIdentity identity,
        IReadOnlyDictionary<BoundModule,
            IReadOnlyList<BoundTypeDefinition>> imports,
        IEnumerable<BoundTypeDefinition> types
    ) : base(location)
    {
        Identity = identity;
        Types = types.ToList();
        Imports = imports;
    }

    public bool TryGetSymbol(string symbolName, [NotNullWhen(true)] out BoundTypeDefinition? type)
    {
        type = Types.FirstOrDefault(type => type.Name == symbolName);
        return type is not null;
    }
}