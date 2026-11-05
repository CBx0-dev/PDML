using PDML.Compiler.Binding.Nodes;
using PDML.Compiler.Binding.Nodes.Members;
using PDML.Compiler.Binding.Types;

namespace PDML.Generator.Abstractions;

public abstract class ModuleVisitorBase
{
    protected abstract void Visit(BoundModule module);

    protected abstract void Visit(BoundTypeDefinition type);

    protected abstract void Visit(BoundAliasTypeDefinition alias);

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

            case AliasDefintionTypeRef aliasDefintionTypeRef:
                Visit(aliasDefintionTypeRef);
                break;
            
            case ArrayTypeRef arrayTypeRef: 
                Visit(arrayTypeRef);
                break;

            case RecordTypeRef recordTypeRef:
                Visit(recordTypeRef);
                break;

            default:
                throw new InvalidOperationException("Unknown type reference kind");
        }
    }

    protected abstract void Visit(IntrinsicTypeRef type);

    protected abstract void Visit(TypeDefinitionTypeRef type);

    protected abstract void Visit(AliasDefintionTypeRef type);
  
    protected abstract void Visit(ArrayTypeRef type);

    protected abstract void Visit(RecordTypeRef type);
}