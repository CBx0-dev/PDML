namespace PDML.Compiler.Binding.Types;

public class IntrinsicTypeRef : TypeRef
{
    public static readonly IntrinsicTypeRef Uint8 = new IntrinsicTypeRef(IntrinsicType.Uint8);
    public static readonly IntrinsicTypeRef Uint16 = new IntrinsicTypeRef(IntrinsicType.Uint16);
    public static readonly IntrinsicTypeRef Uint32 = new IntrinsicTypeRef(IntrinsicType.Uint32);
    public static readonly IntrinsicTypeRef Uint64 = new IntrinsicTypeRef(IntrinsicType.Uint64);

    public static readonly IntrinsicTypeRef Int8 = new IntrinsicTypeRef(IntrinsicType.Int8);
    public static readonly IntrinsicTypeRef Int16 = new IntrinsicTypeRef(IntrinsicType.Int16);
    public static readonly IntrinsicTypeRef Int32 = new IntrinsicTypeRef(IntrinsicType.Int32);
    public static readonly IntrinsicTypeRef Int64 = new IntrinsicTypeRef(IntrinsicType.Int64);

    public static readonly IntrinsicTypeRef Float32 = new IntrinsicTypeRef(IntrinsicType.Float32);
    public static readonly IntrinsicTypeRef Float64 = new IntrinsicTypeRef(IntrinsicType.Float64);
    public static readonly IntrinsicTypeRef Decimal = new IntrinsicTypeRef(IntrinsicType.Decimal);

    public static readonly IntrinsicTypeRef Char = new IntrinsicTypeRef(IntrinsicType.Char);
    public static readonly IntrinsicTypeRef Bool = new IntrinsicTypeRef(IntrinsicType.Bool);
    public static readonly IntrinsicTypeRef String = new IntrinsicTypeRef(IntrinsicType.String);

    public static readonly IntrinsicTypeRef Date = new IntrinsicTypeRef(IntrinsicType.Date);
    public static readonly IntrinsicTypeRef Time = new IntrinsicTypeRef(IntrinsicType.Time);
    public static readonly IntrinsicTypeRef DateTime = new IntrinsicTypeRef(IntrinsicType.DateTime);

    public IntrinsicType Type { get; }

    private IntrinsicTypeRef(IntrinsicType type)
    {
        Type = type;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not IntrinsicTypeRef other)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        return Type == other.Type;
    }

    public override int GetHashCode() => Type.GetHashCode();
}