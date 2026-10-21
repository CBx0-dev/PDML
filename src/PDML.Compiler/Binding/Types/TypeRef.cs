namespace PDML.Compiler.Binding.Types;

public abstract class TypeRef
{
    public static bool operator ==(TypeRef a, TypeRef b) => a.Equals(b);
    public static bool operator !=(TypeRef a, TypeRef b) => !a.Equals(b);

    public override abstract bool Equals(object? obj);

    public override abstract int GetHashCode();
}