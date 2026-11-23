using PDML.Compiler.Binding.Nodes;
using PDML.Generator.Abstractions;

namespace PDML.Generator.TypeScript;

public sealed class TypeScriptGenerator : CodeGenerator<TypeScriptGeneratorSettings>
{
    private readonly ModuleVisitor _visitor;

    public TypeScriptGenerator(TypeScriptGeneratorSettings settings) : base(settings)
    {
        _visitor = new ModuleVisitor(settings);
    }

    protected override void Generate(OutputBuilder output, IReadOnlyList<BoundModule> modules)
    {
        foreach (BoundModule module in modules)
        {
            string filePath = module.Identity.ToGeneratorString(_settings.FileExtension);
            string content = _visitor.Generate(module);

            output.AddOutput(OutputFile.From(filePath, content));
        }
    }
}