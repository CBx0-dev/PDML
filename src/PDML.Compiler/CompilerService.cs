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
    private readonly IDiagnosticService _diagnosticService;
    private readonly List<SourceFile> _sourceFiles;
    private Dictionary<string, ModuleInformation?> _modules;

    private CompilerService(IDiagnosticService diagnosticService, List<SourceFile> sourceFiles)
    {
        _diagnosticService = diagnosticService;
        _sourceFiles = sourceFiles;
        _modules = new Dictionary<string, ModuleInformation?>();
    }

    public Compilation Compile()
    {
        DiagnosticBag globalDiagnostics = _diagnosticService.CreateDiagnosticBag();
        CollectModules(globalDiagnostics);

        string? projectRoot = GetProjectRoot();
        if (projectRoot is null)
        {
            throw new InvalidOperationException("Could not extract project root out of source files");
        }

        IReadOnlyList<BoundModule>? modules = BindModules(globalDiagnostics, projectRoot);

        return new Compilation(globalDiagnostics, modules);
    }

    private void CollectModules(DiagnosticBag globalDiagnostics)
    {
        foreach (SourceFile sourceFile in _sourceFiles)
        {
            CollectModule(globalDiagnostics, sourceFile);
        }
    }

    private ModuleInformation? CollectModule(DiagnosticBag globalDiagnostics, string filePath)
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

        return CollectModule(globalDiagnostics, loadedSourceFile);
    }

    private ModuleInformation? CollectModule(DiagnosticBag globalDiagnostics, SourceFile sourceFile)
    {
        if (_modules.TryGetValue(sourceFile.FilePath, out ModuleInformation? existingInformation))
        {
            // Module was parsed indirectly
            // through a dependency
            return existingInformation;
        }

        Module? module = ParseSource(sourceFile, out DiagnosticBag diagnostics);
        globalDiagnostics.Consume(diagnostics);

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
            ModuleInformation? dependencyInformation = CollectModule(globalDiagnostics, dependency);
            if (dependencyInformation is not null)
            {
                dependencies.Add(dependencyInformation);
            }
        }

        return information;
    }

    private Module? ParseSource(SourceFile sourceFile, out DiagnosticBag diagnostics)
    {
        diagnostics = _diagnosticService.CreateDiagnosticBag();

        List<Token> tokens = Lexer.TokenizeAll(diagnostics, sourceFile);
        if (diagnostics.HasErrors)
        {
            return null;
        }

        Parser parser = new Parser(diagnostics, tokens);

        Module module = parser.ParseModule();
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

    private IReadOnlyList<BoundModule>? BindModules(DiagnosticBag diagnostics, string projectRoot)
    {
        if (diagnostics.HasErrors)
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

            BoundModule module = Binder.BindModule(projectRoot, diagnostics, information.Module, dependencies);
            modules.Add(information.Module, module);
        }

        return modules.Values.ToList();
    }

    public static Compilation CompileEntryPoints(IDiagnosticService diagnosticService, IEnumerable<SourceFile> entryPoints)
    {
        List<SourceFile> sourceFiles = entryPoints.ToList();
        if (sourceFiles.Count == 0)
        {
            throw new ArgumentException("At least one source file is required", nameof(entryPoints));
        }

        CompilerService compiler = new CompilerService(diagnosticService, sourceFiles);
        return compiler.Compile();
    }
}
