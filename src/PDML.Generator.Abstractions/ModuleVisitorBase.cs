using PDML.Compiler.Binding.Nodes;
using PDML.Compiler.Binding.Nodes.Members;
using PDML.Compiler.Binding.Types;

namespace PDML.Generator.Abstractions;

public abstract class ModuleVisitorBase
{
    protected abstract void Visit(BoundModule module);

    protected abstract void Visit(BoundTypeDefinition type);

    protected void Visit(TypeRef type)
    {
        switch (type)
        {
            case IntrinsicTypeRef intrinsicTypeRef:
                Visit(intrinsicTypeRef);
                break;

            case TypeDefinitionTypeRef typeDefinitionTypeRef:
                Visit(typeDefinitionTypeRef);
                break;

            default:
                throw new InvalidOperationException("Unknown type reference kind");
        }
    }

    protected abstract void Visit(IntrinsicTypeRef type);

    protected abstract void Visit(TypeDefinitionTypeRef type);
}