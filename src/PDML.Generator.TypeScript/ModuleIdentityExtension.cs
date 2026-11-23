using PDML.Compiler.Binding;
using PDML.Generator.Abstractions;

namespace PDML.Generator.TypeScript;

public static class ModuleIdentityExtension
{
    public static string ToGeneratorString(this ModuleIdentity identity, string extension, bool isImport = false)
    {
        if (isImport)
        {
            if (extension.EndsWith(".d.ts"))
            {
                extension = extension.Substring(0, extension.Length - ".d.ts".Length);
            }
            else if (extension.EndsWith(".ts"))
            {
                extension = extension.Substring(0, extension.Length - ".ts".Length);
            }
        }

        IEnumerable<string> segments = identity.Segments.Select(segment => segment.ToCamelCase());

        return string.Join(Path.PathSeparator.ToString(), segments) + extension;
    }
}