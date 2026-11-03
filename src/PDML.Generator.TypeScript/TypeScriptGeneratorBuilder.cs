using PDML.Generator.Abstractions;

namespace PDML.Generator.TypeScript;

public sealed class TypeScriptGeneratorBuilder : CodeGeneratorBuilder<TypeScriptGeneratorSettings, TypeScriptGenerator>
{
    public override string Name => "TypeScript";
    public override string Key => "typescript";
}