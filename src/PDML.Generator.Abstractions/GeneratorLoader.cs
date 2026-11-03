using System.Reflection;

namespace PDML.Generator.Abstractions;

public static class GeneratorLoader
{
    private static readonly Dictionary<string, CodeGeneratorBuilder> _generators = new Dictionary<string, CodeGeneratorBuilder>();

    public static readonly List<string> LookupPaths = new List<string>();

    public static void LoadAssemblies()
    {
        _generators.Clear();

        HashSet<string> dlls = new HashSet<string>();

        foreach (string path in LookupPaths)
        {
            if (!Directory.Exists(path))
            {
                continue;
            }

            foreach (string file in Directory.GetFiles(path, "PDML.Generator.*.dll"))
            {
                dlls.Add(file);
            }
        }

        foreach (string dll in dlls)
        {
            try
            {
                Assembly assembly = Assembly.LoadFile(dll);
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.IsAbstract || !typeof(CodeGeneratorBuilder).IsAssignableFrom(type))
                    {
                        continue;
                    }

                    CodeGeneratorBuilder? generator = (CodeGeneratorBuilder?)Activator.CreateInstance(type);
                    if (generator is null)
                    {
                        continue;
                    }

                    _generators[generator.Key] = generator;
                }
            }
            catch
            {
                // TODO thing about exceptions
            }
        }
    }

    public static CodeGeneratorBuilder? LoadGenerator(string key)
    {
        if (_generators.TryGetValue(key, out CodeGeneratorBuilder? generator))
        {
            return generator;
        }

        return null;
    }
}