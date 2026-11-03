using PDML.Compiler.Parsing.Nodes;

namespace PDML.Compiler;

public sealed class ModuleGraph
{
    private readonly ModuleInformation[] _roots;

    public ModuleGraph(IEnumerable<ModuleInformation> roots)
    {
        _roots = roots.ToArray();
    }

    public IEnumerable<ModuleInformation> WalkDependencies()
    {
        Stack<Module> visitStack = new Stack<Module>();

        IEnumerable<ModuleInformation> Dfs(ModuleInformation information)
        {
            if (visitStack.Contains(information.Module))
            {
                string[] filePaths = visitStack.Select(module => module.Location.SourceFile.FilePath).ToArray();
                string list = string.Join("\n", filePaths);

                throw new InvalidOperationException($"Circluar dependencies detected:\n\n{list}");
            }

            visitStack.Push(information.Module);

            foreach (ModuleInformation dependency in information.Dependencies)
            {
                foreach (ModuleInformation dependencyInformation in Dfs(dependency))
                {
                    yield return dependencyInformation;
                }
            }

            yield return information;

            visitStack.Pop();
        }

        foreach (ModuleInformation root in _roots)
        {
            foreach (ModuleInformation information in Dfs(root))
            {
                yield return information;
            }
        }
    }
}
