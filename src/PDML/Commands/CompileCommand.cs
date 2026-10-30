using GlobExpressions;
using PDML.Compiler.Binding;
using PDML.Compiler.Binding.Nodes;
using PDML.Compiler.Common;
using PDML.Compiler.Diagnostics;
using PDML.Compiler.Lexing;
using PDML.Compiler.Parsing;
using PDML.Compiler.Parsing.Nodes;
using PDML.Generator.Abstractions;
using Spectre.Console.Cli;

namespace PDML.Commands;

internal sealed class CompileCommand : AsyncCommand<CompileSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, CompileSettings settings, CancellationToken cancellationToken)
    {
        CodeGenerator generator = InitGenerator(settings.Generator, settings.DllFolders);
        GeneratorSettings generatorSettings = GeneratorSettingsMapper.Map(settings.Properties, generator.SettingsType);

        SourceFile[] sourceFiles = ReadFiles(settings.Paths);

        DiagnosticBag diagnostics = new DiagnosticBag();
        List<BoundModule> modules = new List<BoundModule>();
          

        foreach (SourceFile sourceFile in sourceFiles)
        {
            BoundModule? module = ParseFile(sourceFile, out DiagnosticBag moduleDiagnostics);
            diagnostics.Consume(moduleDiagnostics);
            
            if (module is not null)
            {
                modules.Add(module);
            }
        }


        if (diagnostics.HasErrors)
        {
            DiagnosticPrinter.Print(diagnostics);
            return 1;
        }

        foreach (BoundModule module in modules)
        {
            generator.GenerateModule(Console.Out, generatorSettings, module);
        }

        return 0;
    }

    private BoundModule? ParseFile(SourceFile sourceFile, out DiagnosticBag diagnostics)
    {
        diagnostics = new DiagnosticBag();

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

        BoundModule boundModule = Binder.BindModule(diagnostics, module);
        if (diagnostics.HasErrors)
        {
            return null;
        }

        return boundModule;
    }

    private CodeGenerator InitGenerator(string generatorKey, string[]? dllFolders)
    {
        GeneratorLoader.LookupPaths.Add(AppContext.BaseDirectory);

        if (dllFolders is not null)
        {
            GeneratorLoader.LookupPaths.AddRange(dllFolders);
        }

        GeneratorLoader.LoadAssemblies();

        CodeGenerator? generator = GeneratorLoader.LoadGenerator(generatorKey);
        if (generator is null)
        {
            throw new ArgumentException($"Unknown generator '{generatorKey}'", nameof(generator));
        }

        return generator;
    }

    private SourceFile[] ReadFiles(string[] paths)
    {
        List<SourceFile> sourceFiles = new List<SourceFile>();

        foreach (string path in paths)
        {
            IEnumerable<string> matches;

            if (File.Exists(path))
            {
                matches = [path];
            }
            else if (Directory.Exists(path))
            {
                matches = Directory.GetFiles(path);
            }
            else if (path.IndexOfAny(['*', '?', '[', '{']) >= 0)
            {
                matches = Glob.Files(
                    Directory.GetCurrentDirectory(),
                    path.Replace('\\', '/')
                );
            }
            else
            {
                continue;
            }

            foreach (string file in matches)
            {
                if (Path.GetExtension(file) == ".pdml")
                {
                    sourceFiles.Add(SourceFile.Read(file));   
                }
            }
        }

        return sourceFiles.ToArray();
    }
}
