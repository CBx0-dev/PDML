using PDML.Generator.Abstractions;

namespace PDML.Generator.CSharp;

public sealed class CSharpGeneratorBuilder : CodeGeneratorBuilder<CSharpGeneratorSettings, CSharpGenerator>
{
    public override string Name => "C#";
    public override string Key => "csharp";
}