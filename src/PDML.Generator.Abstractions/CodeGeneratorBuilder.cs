namespace PDML.Generator.Abstractions;

public abstract class CodeGeneratorBuilder
{
    public abstract string Name { get; }
    public abstract string Key { get; }
    public abstract Type SettingsType { get; }
    public abstract Type CodeGeneratorType { get; }

    public CodeGenerator CreateInstance(GeneratorSettings settings)
    {
        if (!SettingsType.IsAssignableFrom(settings.GetType()))
        {
            throw new ArgumentException(
                $"The given settings cannot be used for this generator. '{settings.GetType().FullName}' not assigable to '{SettingsType.FullName}'"
            );
        }

        CodeGenerator? generator = (CodeGenerator?)Activator.CreateInstance(CodeGeneratorType, settings);
        if (generator is null)
        {
            throw new InvalidOperationException("Unable to instantiate generator");
        }

        return generator;
    }
}

public abstract class CodeGeneratorBuilder<TSettings, TCodeGenerator> : CodeGeneratorBuilder
    where TSettings : GeneratorSettings
    where TCodeGenerator : CodeGenerator
{
    public override sealed Type SettingsType => typeof(TSettings);
    public override sealed Type CodeGeneratorType => typeof(TCodeGenerator);
}