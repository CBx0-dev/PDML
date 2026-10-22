using PDML.Compiler.Binding.Nodes.Members;
using PDML.Compiler.Binding.Types;
using PDML.Generator.Abstractions;

namespace PDML.Generator.TypeScript;

public sealed class TypeScriptGenerator : CodeGenerator
{
    public override string Name => "TypeScript";
    public override string Key => "typescript";

    protected override void GenerateTypeDefinition(TextWriter writer, BoundTypeDefinition type)
    {
        writer.Write("export interface ");
        writer.Write(type.Name);
        if (type.InheritingType is not null)
        {
            writer.Write(" extends ");
            writer.Write(type.InheritingType.Definition.Name);
        }

        writer.WriteLine(" {");

        foreach ((string name, BoundTypeDefinitionProperty property) in type.Properties)
        {
            writer.Write("    ");
            writer.Write(name);
            writer.Write(": ");
            GenerateTypeRef(writer, property.Type);
            writer.WriteLine(';');
        }

        writer.WriteLine('}');
    }

    protected override void GenerateTypeRef(TextWriter writer, TypeRef typeRef)
    {
        switch (typeRef)
        {
            case IntrinsicTypeRef intrinsicTypeRef:
                switch (intrinsicTypeRef.Type)
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
                        writer.Write("number");
                        break;
                    case IntrinsicType.Uint64:
                    case IntrinsicType.Int64:
                    case IntrinsicType.Char:
                    case IntrinsicType.String:
                    case IntrinsicType.Date:
                    case IntrinsicType.Time:
                    case IntrinsicType.DateTime:
                        writer.Write("string");
                        break;
                    case IntrinsicType.Bool:
                        writer.Write("boolean");
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
}