using PDML.Compiler.Binding.Nodes;
using PDML.Compiler.Binding.Nodes.Members;
using PDML.Compiler.Binding.Types;

namespace PDML.Generator.Abstractions;

public abstract class CodeGenerator
{
    public abstract string Name { get; }
    public abstract string Key { get; }
    public abstract Type SettingsType { get; }

    public abstract void GenerateModule(TextWriter writer, GeneratorSettings settings, BoundModule module);
}

public abstract class CodeGenerator<TSettings> : CodeGenerator
    where TSettings : GeneratorSettings
{
    public sealed override Type SettingsType { get; } = typeof(TSettings);

    public override void GenerateModule(TextWriter writer, GeneratorSettings settings, BoundModule module)
        => GenerateModule(writer, (TSettings)settings, module);

    public virtual void GenerateModule(TextWriter writer, TSettings settings, BoundModule module)
    {
        foreach (BoundTypeDefinition type in module.Types)
        {
            GenerateTypeDefinition(writer, settings, type);
            writer.WriteLine();
        }
    }

    protected abstract void GenerateTypeDefinition(TextWriter writer, TSettings settings, BoundTypeDefinition type);

    protected abstract void GenerateTypeRef(TextWriter writer, TSettings settings, TypeRef typeRef);
}