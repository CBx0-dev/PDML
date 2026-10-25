namespace System.Runtime.CompilerServices
{
    [AttributeUsage(
        AttributeTargets.Class |
        AttributeTargets.Struct |
        AttributeTargets.Field |
        AttributeTargets.Property,
        Inherited = false
    )]
    public sealed class RequiredMemberAttribute : Attribute
    {
    }
}