using PDML.Compiler.Binding;
using PDML.Generator.Abstractions;

namespace PDML.Generator.TypeScript;

public static class ModuleIdentityExtension
{
    public static string ToGeneratorString(this ModuleIdentity identity)
    {
        IEnumerable<string> segments = identity.Segments.Select(segment => segment.ToCamelCase());

        return string.Join(Path.PathSeparator.ToString(), segments);
    }

    public static string ToGeneratorString(this ModuleIdentity identity, string extension)
    {
        IEnumerable<string> segments = identity.Segments.Select(segment => segment.ToCamelCase());

        return string.Join(Path.PathSeparator.ToString(), segments) + extension;
    }
}