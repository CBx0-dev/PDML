using PDML.Compiler.Binding;
using PDML.Generator.Abstractions;

namespace PDML.Generator.CSharp;

public static class ModuleIdentityExtension
{
    public static string ToGeneratorString(this ModuleIdentity identity)
    {
        IEnumerable<string> segments = identity.Segments.Select(segment => segment.ToPascalCase());

        return string.Join(".", segments);
    }

    public static string ToGeneratorString(this ModuleIdentity identity, string extension)
    {
        IEnumerable<string> segments = identity.Segments.Select(segment => segment.ToPascalCase());

        return string.Join(".", segments) + extension;
    }
}