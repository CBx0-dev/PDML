using PDML.Compiler.Binding.Nodes.Members;
using PDML.Compiler.Common;
using System.Diagnostics.CodeAnalysis;

namespace PDML.Compiler.Binding.Nodes;

public sealed class BoundModule : BoundNode
{
    public ModuleIdentity Identity { get; internal set; }
    public IReadOnlyDictionary<BoundModule, IReadOnlyList<BoundTypeDefinition>> Imports { get; internal set; }
    public IReadOnlyList<BoundTypeDefinition> Types { get; internal set; }
    public IReadOnlyList<BoundAliasTypeDefinition> Aliases { get; internal set; }

    public IReadOnlyList<BoundModule> Dependencies => Imports.Keys.ToList();

    public BoundModule(
        Location? location,
        ModuleIdentity identity,
        IReadOnlyDictionary<BoundModule,
        IReadOnlyList<BoundTypeDefinition>> imports,
        IEnumerable<BoundTypeDefinition> types,
        IEnumerable<BoundAliasTypeDefinition> aliases
    ) : base(location)
    {
        Identity = identity;
        Imports = imports;
        Types = types.ToList();
        Aliases = aliases.ToList();
    }

    public bool TryGetSymbol(string symbolName, out BoundTypeDefinition? type, out BoundAliasTypeDefinition? alias)
    {
        type = null;
        alias = null;

        type = Types.FirstOrDefault(type => type.Name == symbolName);
        if (type is not null)
        {
            alias = Aliases.FirstOrDefault(alias => alias.Name == symbolName);
        }

        return type is not null || alias is not null;
    }
}