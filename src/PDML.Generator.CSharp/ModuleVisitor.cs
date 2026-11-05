using PDML.Compiler.Binding.Nodes;
using PDML.Compiler.Binding.Nodes.Members;
using PDML.Compiler.Binding.Types;
using PDML.Generator.Abstractions;

namespace PDML.Generator.CSharp;

public sealed class ModuleVisitor : ModuleVisitorBase
{
    private readonly StringWriter _writer = new StringWriter();
    private readonly CSharpGeneratorSettings _settings;

    public ModuleVisitor(CSharpGeneratorSettings settings)
    {
        _settings = settings;
    }

    public string Generate(BoundModule module)
    {
        _writer.GetStringBuilder().Clear();
        Visit(module);

        return _writer.ToString();
    }

    protected override void Visit(BoundModule module)
    {
        foreach (BoundModule dependency in module.Dependencies)
        {
            _writer.Write("using ");
            _writer.Write(_settings.RootNamespace);
            _writer.Write('.');
            _writer.Write(dependency.Identity.ToGeneratorString());
            _writer.WriteLine(';');
        }

        _writer.WriteLine();

        _writer.Write("namespace ");
        _writer.Write(_settings.RootNamespace);
        _writer.Write('.');
        _writer.Write(module.Identity.ToGeneratorString());
        _writer.WriteLine(';');

        _writer.WriteLine();

        foreach (BoundTypeDefinition type in module.Types)
        {
            Visit(type);
        }
    }

    protected override void Visit(BoundTypeDefinition type)
    {
        _writer.Write("public record ");
        _writer.Write(type.Name);
        if (type.InheritingType is not null)
        {
            _writer.Write(" : ");
            _writer.Write(type.InheritingType.Definition.Name);
        }

        _writer.WriteLine();
        _writer.WriteLine('{');

        foreach ((string name, BoundTypeDefinitionProperty property) in type.Properties)
        {
            _writer.Write("    public required ");
            Visit(property.Type);
            _writer.Write(' ');
            _writer.Write(name.ToPascalCase());
            _writer.WriteLine(" { get; set; }");
        }

        _writer.WriteLine('}');
        _writer.WriteLine();
    }

    protected override void Visit(BoundAliasTypeDefinition type)
    {
        // Doing nothing here, because C# has no aliases.
    }

    protected override void Visit(IntrinsicTypeRef type)
    {
        switch (type.Type)
        {
            case IntrinsicType.Uint8:
                _writer.Write("byte");
                break;
            case IntrinsicType.Uint16:
                _writer.Write("ushort");
                break;
            case IntrinsicType.Uint32:
                _writer.Write("uint");
                break;
            case IntrinsicType.Uint64:
                _writer.Write("ulong");
                break;
            case IntrinsicType.Int8:
                _writer.Write("sbyte");
                break;
            case IntrinsicType.Int16:
                _writer.Write("short");
                break;
            case IntrinsicType.Int32:
                _writer.Write("int");
                break;
            case IntrinsicType.Int64:
                _writer.Write("long");
                break;
            case IntrinsicType.Float32:
                _writer.Write("float");
                break;
            case IntrinsicType.Float64:
                _writer.Write("double");
                break;
            case IntrinsicType.Decimal:
                _writer.Write("decimal");
                break;
            case IntrinsicType.Char:
                _writer.Write("char");
                break;
            case IntrinsicType.String:
                _writer.Write("string");
                break;
            case IntrinsicType.Date:
                _writer.Write("System.DateOnly");
                break;
            case IntrinsicType.Time:
                _writer.Write("System.TimeOnly");
                break;
            case IntrinsicType.DateTime:
                _writer.Write("System.DateTime");
                break;
            case IntrinsicType.Bool:
                _writer.Write("bool");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    protected override void Visit(TypeDefinitionTypeRef type)
    {
        _writer.Write(type.Definition.Name);
    }
    
    protected override void Visit(AliasDefintionTypeRef alias)
    {
        // We just emit the aliasing type,
        // because C# has no real alias system
        Visit(alias.Definition.AliasingType);
    }

    protected override void Visit(ArrayTypeRef type)
    {
        Visit(type.ElementType);
        _writer.Write("[]");
    }

    protected override void Visit(RecordTypeRef type)
    {
        const string TypeNamespace = "System.Collections.Generic";

        _writer.Write(TypeNamespace);
        _writer.Write("Dictionary<");
        Visit(type.KeyType);
        _writer.Write(", ");
        Visit(type.ValueType);
        _writer.Write('>');
    }
}