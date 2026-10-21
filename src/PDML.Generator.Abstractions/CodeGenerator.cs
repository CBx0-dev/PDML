using PDML.Compiler.Binding.Nodes;
using PDML.Compiler.Binding.Nodes.Members;
using PDML.Compiler.Binding.Types;

namespace PDML.Generator.Abstractions;

public abstract class CodeGenerator
{
    public abstract string Name { get; }
    public abstract string Key { get; }

    public void GenerateModule(TextWriter writer, BoundModule module)
    {
        foreach (BoundTypeDefinition type in module.Types)
        {
            GenerateTypeDefinition(writer, type);
            writer.WriteLine();
        }
    }

    protected abstract void GenerateTypeDefinition(TextWriter writer, BoundTypeDefinition type);

    protected abstract void GenerateTypeRef(TextWriter writer, TypeRef typeRef);
}