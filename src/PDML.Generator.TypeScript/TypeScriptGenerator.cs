using PDML.Compiler.Binding.Nodes;
using PDML.Generator.Abstractions;

namespace PDML.Generator.TypeScript;

public sealed class TypeScriptGenerator : CodeGenerator<TypeScriptGeneratorSettings>
{
    private readonly ModuleVisitor _visitor = new ModuleVisitor();

    public TypeScriptGenerator(TypeScriptGeneratorSettings settings) : base(settings)
    {
    }

    protected override void Generate(OutputBuilder output, IReadOnlyList<BoundModule> modules)
    {
        foreach (BoundModule module in modules)
        {
            string filePath = module.Identity.ToGeneratorString(".ts");
            string content = _visitor.Generate(module);

            output.AddOutput(OutputFile.From(filePath, content));
        }
    }
}