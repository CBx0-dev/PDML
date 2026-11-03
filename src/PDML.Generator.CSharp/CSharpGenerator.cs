using PDML.Compiler.Binding.Nodes;
using PDML.Generator.Abstractions;

namespace PDML.Generator.CSharp;

public sealed class CSharpGenerator : CodeGenerator<CSharpGeneratorSettings>
{
    private readonly ModuleVisitor _visitor;

    public CSharpGenerator(CSharpGeneratorSettings settings) : base(settings)
    {
        _visitor = new ModuleVisitor(settings);
    }

    protected override void Generate(OutputBuilder output, IReadOnlyList<BoundModule> modules)
    {
        foreach (BoundModule module in modules)
        {
            string filePath = module.Identity.ToGeneratorString(".g.cs");
            string content = _visitor.Generate(module);
            output.AddOutput(OutputFile.From(filePath, content));
        }
    }
}