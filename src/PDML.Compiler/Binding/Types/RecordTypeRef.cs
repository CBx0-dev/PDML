namespace PDML.Compiler.Binding.Types;

public sealed class RecordTypeRef : TypeRef
{
    public TypeRef KeyType { get; }
    public TypeRef ValueType { get; }

    public RecordTypeRef(TypeRef keyType, TypeRef valueType)
    {
        KeyType = keyType;
        ValueType = valueType;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not RecordTypeRef other)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return KeyType == other.KeyType && ValueType == other.ValueType;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 29;
            hash = hash * 31 + KeyType.GetHashCode();
            hash = hash * 31 + ValueType.GetHashCode();

            return hash;
        }
    }
}