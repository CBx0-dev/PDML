using PDML.Generator.Abstractions;

namespace PDML.Commands.Generators.List;

internal sealed class ListResult : TaskResult
{
    public required IReadOnlyList<CodeGeneratorBuilder> Generators { get; init; }
}