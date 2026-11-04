using System.Collections.Immutable;

namespace PDML.Generator.CSharp.Roslyn;

internal sealed record ProjectInfo
{
    public required string RootNamespace { get; init; }

    public required ImmutableArray<InputFile> Files { get; init; }
}