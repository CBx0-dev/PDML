using System.Reflection;

namespace PDML.Generator.Abstractions;

public static class GeneratorLoader
{
    private static Dictionary<string, CodeGenerator> _generators = new Dictionary<string, CodeGenerator>();

    public static List<string> LookupPaths = new List<string>();

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
                    if (type.IsAbstract || !typeof(CodeGenerator).IsAssignableFrom(type))
                    {
                        continue;
                    }

                    CodeGenerator? generator = (CodeGenerator?)Activator.CreateInstance(type);
                    if (generator is null)
                    {
                        return;
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

    public static CodeGenerator? LoadGenerator(string key)
    {
        if (_generators.TryGetValue(key, out CodeGenerator? generator))
        {
            return generator;
        }

        return null;
    }

}
