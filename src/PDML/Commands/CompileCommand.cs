using GlobExpressions;
using PDML.Compiler;
using PDML.Compiler.Common;
using PDML.Compiler.Diagnostics;
using PDML.Generator.Abstractions;
using Spectre.Console.Cli;

namespace PDML.Commands;

internal sealed class CompileCommand : Command<CompileSettings>
{
    public override int Execute(CommandContext context, CompileSettings settings, CancellationToken cancellationToken)
    {
        CodeGenerator generator = InitGenerator(settings.Generator, settings.Properties, settings.DllFolders);

        SourceFile[] sourceFiles = ReadFiles(settings.Paths);
        Compilation compilation = CompilerService.CompileEntryPoints(
            new DefaultDiagnosticService(),
            sourceFiles
        );

        if (compilation.Diagnostics.HasItems)
        {
            DiagnosticPrinter.Print((DefaultDiagnosticBag)compilation.Diagnostics);
        }

        if (compilation.HasErrors)
        {
            return 1;
        }

        OutputBuilder output = generator.Generate(compilation.Modules);
        output.SaveTo(settings.OutputDirectory);

        return 0;
    }

    private CodeGenerator InitGenerator(string generatorKey, ILookup<string, string>? properties, string[]? dllFolders)
    {
        GeneratorLoader.LookupPaths.Add(AppContext.BaseDirectory);

        if (dllFolders is not null)
        {
            GeneratorLoader.LookupPaths.AddRange(dllFolders);
        }

        GeneratorLoader.LoadAssemblies();

        CodeGeneratorBuilder? builder = GeneratorLoader.LoadGenerator(generatorKey);
        if (builder is null)
        {
            throw new ArgumentException($"Unknown generator '{generatorKey}'", nameof(builder));
        }

        GeneratorSettings settings = GeneratorSettingsMapper.Map(properties, builder.SettingsType);

        return builder.CreateInstance(settings);
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