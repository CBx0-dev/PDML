using GlobExpressions;
using PDML.Compiler;
using PDML.Compiler.Binding.Nodes;
using PDML.Compiler.Common;
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
        Compilation compilation = CompilerService.CompileEntryPoints(sourceFiles);

        if (compilation.Diagnostics.HasItems)
        {
            DiagnosticPrinter.Print(compilation.Diagnostics);
        }

        if (compilation.HasErrors)
        {
            return 1;
        }

        foreach (BoundModule module in compilation.Modules)
        {
            generator.GenerateModule(Console.Out, generatorSettings, module);
        }

        return 0;
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
