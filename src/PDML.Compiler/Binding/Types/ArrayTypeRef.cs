namespace PDML.Compiler.Binding.Types;

public sealed class ArrayTypeRef : TypeRef
{
    public TypeRef ElementType { get; }

    public ArrayTypeRef(TypeRef elementType)
    {
        ElementType = elementType;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not ArrayTypeRef other)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return ElementType == other.ElementType;
    }

    public override int GetHashCode() => ElementType.GetHashCode();
}
