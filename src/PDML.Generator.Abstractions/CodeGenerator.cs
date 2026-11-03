using PDML.Compiler.Binding.Nodes;

namespace PDML.Generator.Abstractions;

public abstract class CodeGenerator
{
    public abstract Type SettingsType { get; }

    public abstract OutputBuilder Generate(IReadOnlyList<BoundModule> modules);
}

public abstract class CodeGenerator<TSettings> : CodeGenerator
    where TSettings : GeneratorSettings
{
    protected TSettings _settings;

    public override sealed Type SettingsType { get; } = typeof(TSettings);

    protected CodeGenerator(TSettings settings)
    {
        _settings = settings;
    }

    public override sealed OutputBuilder Generate(IReadOnlyList<BoundModule> modules)
    {
        OutputBuilder output = new OutputBuilder();
        Generate(output, modules);

        return output;
    }

    protected abstract void Generate(OutputBuilder output, IReadOnlyList<BoundModule> modules);
}