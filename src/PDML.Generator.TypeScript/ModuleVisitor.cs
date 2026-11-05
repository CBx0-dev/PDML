using PDML.Compiler.Binding.Nodes;
using PDML.Compiler.Binding.Nodes.Members;
using PDML.Compiler.Binding.Types;
using PDML.Generator.Abstractions;

namespace PDML.Generator.TypeScript;

public sealed class ModuleVisitor : ModuleVisitorBase
{
    private readonly StringWriter _writer = new StringWriter();

    public string Generate(BoundModule module)
    {
        _writer.GetStringBuilder().Clear();
        Visit(module);

        return _writer.ToString();
    }

    protected override void Visit(BoundModule module)
    {
        foreach ((BoundModule source, IReadOnlyList<BoundTypeDefinition> symbols) in module.Imports)
        {
            _writer.Write("import { ");
            bool first = true;
            foreach (BoundTypeDefinition symbol in symbols)
            {
                if (!first)
                {
                    _writer.Write(", ");
                }

                first = false;
                _writer.Write(symbol.Name.ToPascalCase());
            }

            _writer.Write(" } from \"./");
            _writer.Write(source.Identity.ToGeneratorString());
            _writer.WriteLine("\";");
        }

        _writer.WriteLine();

        foreach (BoundTypeDefinition type in module.Types)
        {
            Visit(type);
        }
    }

    protected override void Visit(BoundTypeDefinition type)
    {
        _writer.Write("export interface ");
        _writer.Write(type.Name.ToPascalCase());
        if (type.InheritingType is not null)
        {
            _writer.Write(" extends ");
            Visit(type.InheritingType);
        }

        _writer.WriteLine(" {");

        foreach ((string name, BoundTypeDefinitionProperty property) in type.Properties)
        {
            _writer.Write("    ");
            _writer.Write(name.ToCamelCase());
            _writer.Write(": ");
            Visit(property.Type);
            _writer.WriteLine(';');
        }

        _writer.WriteLine('}');
        _writer.WriteLine();
    }

    protected override void Visit(IntrinsicTypeRef type)
    {
        switch (type.Type)
        {
            case IntrinsicType.Uint8:
            case IntrinsicType.Uint16:
            case IntrinsicType.Uint32:
            case IntrinsicType.Int8:
            case IntrinsicType.Int16:
            case IntrinsicType.Int32:
            case IntrinsicType.Float32:
            case IntrinsicType.Float64:
            case IntrinsicType.Decimal:
                _writer.Write("number");
                break;
            case IntrinsicType.Uint64:
            case IntrinsicType.Int64:
            case IntrinsicType.Char:
            case IntrinsicType.String:
            case IntrinsicType.Date:
            case IntrinsicType.Time:
            case IntrinsicType.DateTime:
                _writer.Write("string");
                break;
            case IntrinsicType.Bool:
                _writer.Write("boolean");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    protected override void Visit(TypeDefinitionTypeRef type)
    {
        _writer.Write(type.Definition.Name.ToPascalCase());
    }

    protected override void Visit(ArrayTypeRef type)
    {
        Visit(type.ElementType);
        _writer.Write("[]");
    }

    protected override void Visit(RecordTypeRef type)
    {
        _writer.Write("Record<");
        Visit(type.KeyType);
        _writer.Write(", ");
        Visit(type.ValueType);
        _writer.Write('>');
    }
}