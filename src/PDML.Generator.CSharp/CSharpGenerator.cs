using PDML.Compiler.Binding.Nodes;
using PDML.Compiler.Binding.Nodes.Members;
using PDML.Compiler.Binding.Types;
using PDML.Generator.Abstractions;

namespace PDML.Generator.CSharp;

public sealed class CSharpGenerator : CodeGenerator<CSharpGeneratorSettings>
{
    public override string Name => "C#";

    public override string Key => "csharp";

    public override void GenerateModule(TextWriter writer, CSharpGeneratorSettings settings, BoundModule module)
    {
        writer.Write("namespace ");
        writer.Write(settings.RootNamespace);
        writer.WriteLine(';');
        writer.WriteLine();

        base.GenerateModule(writer, settings, module);
    }

    protected override void GenerateTypeDefinition(TextWriter writer, CSharpGeneratorSettings settings, BoundTypeDefinition type)
    {
        writer.Write("public record ");
        writer.Write(type.Name);
        if (type.InheritingType is not null)
        {
            writer.Write(" : ");
            writer.Write(type.InheritingType.Definition.Name);
        }

        writer.WriteLine();
        writer.WriteLine('{');

        foreach ((string name, BoundTypeDefinitionProperty property) in type.Properties)
        {
            writer.Write("    public required ");
            GenerateTypeRef(writer, settings, property.Type);
            writer.Write(' ');
            writer.Write(ConvertToPascalCase(name));
            writer.WriteLine(" { get; set; }");
        }

        writer.WriteLine('}');
    }

    protected override void GenerateTypeRef(TextWriter writer, CSharpGeneratorSettings settings, TypeRef typeRef)
    {
        switch (typeRef)
        {
            case IntrinsicTypeRef intrinsicTypeRef:
                switch (intrinsicTypeRef.Type)
                {
                    case IntrinsicType.Uint8:
                        writer.Write("byte");
                        break;
                    case IntrinsicType.Uint16:
                        writer.Write("ushort");
                        break;
                    case IntrinsicType.Uint32:
                        writer.Write("uint");
                        break;
                    case IntrinsicType.Uint64:
                        writer.Write("ulong");
                        break;
                    case IntrinsicType.Int8:
                        writer.Write("sbyte");
                        break;
                    case IntrinsicType.Int16:
                        writer.Write("short");
                        break;
                    case IntrinsicType.Int32:
                        writer.Write("int");
                        break;
                    case IntrinsicType.Int64:
                        writer.Write("long");
                        break;
                    case IntrinsicType.Float32:
                        writer.Write("float");
                        break;
                    case IntrinsicType.Float64:
                        writer.Write("double");
                        break;
                    case IntrinsicType.Decimal:
                        writer.Write("decimal");
                        break;
                    case IntrinsicType.Char:
                        writer.Write("char");
                        break;
                    case IntrinsicType.String:
                        writer.Write("string");
                        break;
                    case IntrinsicType.Date:
                        writer.Write("System.DateOnly");
                        break;
                    case IntrinsicType.Time:
                        writer.Write("System.TimeOnly");
                        break;
                    case IntrinsicType.DateTime:
                        writer.Write("System.DateTime");
                        break;
                    case IntrinsicType.Bool:
                        writer.Write("bool");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                break;

            case TypeDefinitionTypeRef typeDefinitionTypeRef:
                writer.Write(typeDefinitionTypeRef.Definition.Name);
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static string ConvertToPascalCase(string identifier)
    {
        string c = identifier[0].ToString();
        return c.ToUpperInvariant() + identifier.Substring(1);
    }
}
