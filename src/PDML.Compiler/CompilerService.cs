using PDML.Compiler.Binding;
using PDML.Compiler.Binding.Nodes;
using PDML.Compiler.Common;
using PDML.Compiler.Diagnostics;
using PDML.Compiler.Lexing;
using PDML.Compiler.Parsing;
using PDML.Compiler.Parsing.Nodes;
using PDML.Compiler.Parsing.Nodes.Members;

namespace PDML.Compiler;

public class CompilerService
{
    private readonly List<SourceFile> _sourceFiles;
    private readonly DiagnosticBag _diagnostics;
    private Dictionary<string, ModuleInformation?> _modules;

    private CompilerService(List<SourceFile> sourceFiles)
    {
        _sourceFiles = sourceFiles;
        _diagnostics = new DiagnosticBag();
        _modules = new Dictionary<string, ModuleInformation?>();
    }

    public Compilation Compile()
    {
        CollectModules();
        string? projectRoot = GetProjectRoot();
        if (projectRoot is null)
        {
            throw new InvalidOperationException("Could not extract project root out of source files");
        }

        IReadOnlyList<BoundModule>? modules = BindModules(projectRoot);

        return new Compilation(_diagnostics, modules);
    }

    private void CollectModules()
    {
        foreach (SourceFile sourceFile in _sourceFiles)
        {
            CollectModule(sourceFile);
        }
    }

    private ModuleInformation? CollectModule(string filePath)
    {
        if (_modules.TryGetValue(filePath, out ModuleInformation? existingInformation))
        {
            return existingInformation;
        }

        SourceFile? loadedSourceFile = _sourceFiles.FirstOrDefault(source => source.FilePath == filePath);
        if (loadedSourceFile is null)
        {
            loadedSourceFile = SourceFile.Read(filePath);
        }

        return CollectModule(loadedSourceFile);
    }

    private ModuleInformation? CollectModule(SourceFile sourceFile)
    {
        if (_modules.TryGetValue(sourceFile.FilePath, out ModuleInformation? existingInformation))
        {
            // Module was parsed indirectly
            // through a dependency
            return existingInformation;
        }

        Module? module = ParseSource(sourceFile);

        if (module is null)
        {
            // We add even when null, to prevent
            // parsing and producing the same errors
            // of this file multiple times
            _modules.Add(sourceFile.FilePath, null);
            return null;
        }

        List<ModuleInformation> dependencies = new List<ModuleInformation>();
        ModuleInformation information = new ModuleInformation
        {
            Module = module,
            Dependencies = dependencies
        };
        _modules.Add(sourceFile.FilePath, information);

        foreach (string dependency in ResolveDependencies(module))
        {
            ModuleInformation? dependencyInformation = CollectModule(dependency);
            if (dependencyInformation is not null)
            {
                dependencies.Add(dependencyInformation);
            }
        }

        return information;
    }

    private Module? ParseSource(SourceFile sourceFile)
    {
        DiagnosticBag diagnostics = new DiagnosticBag();

        List<Token> tokens = Lexer.TokenizeAll(diagnostics, sourceFile);
        if (diagnostics.HasErrors)
        {
            _diagnostics.Consume(diagnostics);
            return null;
        }

        Parser parser = new Parser(diagnostics, tokens);

        Module module = parser.ParseModule();
        _diagnostics.Consume(diagnostics);

        if (diagnostics.HasErrors)
        {
            return null;
        }

        return module;
    }

    private IEnumerable<string> ResolveDependencies(Module module)
    {
        foreach (ImportMember member in module.Imports)
        {
            string relativePath = TokenUtils.NormalizeString(member.Locator);
            string fullPath = Path.GetFullPath(Path.Combine(module.Location.SourceFile.Directory, relativePath) + ".pdml");

            yield return fullPath;
        }
    }

    private string? GetProjectRoot()
    {
        string[] paths = _modules.Keys.ToArray();

        if (paths.Length == 0)
        {
            return null;
        }

        string? common = Path.GetDirectoryName(paths[0]);
        if (common is null)
        {
            return null;
        } 

        foreach (string path in paths.Skip(1))
        {
            while (!path.StartsWith(common))
            {
                common = Path.GetDirectoryName(common);
                if (common is null)
                {
                    return null;
                }
            }
        }

        return common;
    }

    private IReadOnlyList<BoundModule>? BindModules(string projectRoot)
    {
        if (_diagnostics.HasErrors)
        {
            return null;
        }

        // Must be not null, because if they are
        // then at least one diagnostic error has
        // been reported
        ModuleGraph graph = new ModuleGraph(_modules.Values!);
        Dictionary<Module, BoundModule> modules = new Dictionary<Module, BoundModule>();

        foreach (ModuleInformation information in graph.WalkDependencies())
        {
            if (modules.ContainsKey(information.Module))
            {
                continue;
            }

            BoundModule[] dependencies = information.Dependencies
                .Select(dependency => modules[dependency.Module])
                .ToArray();

            BoundModule module = Binder.BindModule(projectRoot, _diagnostics, information.Module, dependencies);
            modules.Add(information.Module, module);
        }

        return modules.Values.ToList();
    }

    public static Compilation CompileEntryPoints(IEnumerable<SourceFile> entryPoints)
    {
        List<SourceFile> sourceFiles = entryPoints.ToList();
        if (sourceFiles.Count == 0)
        {
            throw new ArgumentException("At least one source file is required", nameof(entryPoints));
        }

        CompilerService compiler = new CompilerService(sourceFiles);


        return compiler.Compile();
    }
}
