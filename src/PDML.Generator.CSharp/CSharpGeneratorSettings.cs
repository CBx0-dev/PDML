using PDML.Generator.Abstractions;

namespace PDML.Generator.CSharp;

public sealed class CSharpGeneratorSettings : GeneratorSettings
{
    public required string RootNamespace { get; init; }
}
