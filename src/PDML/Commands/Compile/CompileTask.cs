using PDML.Common;
using PDML.Compiler;
using PDML.Compiler.Common;
using PDML.Compiler.Diagnostics;
using PDML.Generator.Abstractions;
using PDML.Output;

namespace PDML.Commands.Compile;

internal class CompileTask : ITask<CompileResult, CompileSettings>
{
    public Task<CompileResult> ExecuteAsync(CompileSettings settings)
    {
        CodeGenerator generator = InitGenerator(settings.Generator, settings.Properties, settings.DllFolders);

        SourceFile[] sourceFiles = ReadFiles(settings.RootPath, settings.UseStdin);
        Compilation compilation = CompilerService.CompileEntryPoints(
            new DefaultDiagnosticService(),
            settings.UseStdin
                ? Directory.GetCurrentDirectory()
                : settings.RootPath!,
            sourceFiles
        );

        if (compilation.HasErrors)
        {
            return Task.FromResult(new CompileResult
            {
                ExitCode = 1,
                Diagnostics = ((IEnumerable<Diagnostic>)compilation.Diagnostics).ToList(),
                OutputFiles = []
            });
        }

        OutputBuilder output = generator.Generate(compilation.Modules);
        if (settings.OutputDirectory is not null)
        {
            output.SaveTo(settings.OutputDirectory);
        }

        return Task.FromResult(new CompileResult
        {
            ExitCode = 0,
            Diagnostics = ((IEnumerable<Diagnostic>)compilation.Diagnostics).ToList(),
            OutputFiles = output.GetFiles()
        });
    }


    private CodeGenerator InitGenerator(string generatorKey, ILookup<string, string>? properties, string[]? dllFolders)
    {
        GeneratorLoader.LookupPaths.Add(AppContext.BaseDirectory);

        string? pathEnv = Environment.GetEnvironmentVariable("PDML_EXTENSIONS_PATH");
        if (pathEnv is not null)
        {
            GeneratorLoader.LookupPaths.AddRange(
                pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            );
        }

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

    private SourceFile[] ReadFiles(string? rootPath, bool useStdin)
    {
        if (useStdin)
        {
            if (rootPath is not null)
            {
                throw new ArgumentException("Standard input cannot be combined with other input files.");
            }

            string input = Console.In.ReadToEnd();

            return
            [
                SourceFile.Create(
                    Directory.GetCurrentDirectory(),
                    "stdin",
                    input
                )
            ];
        }

        if (rootPath is null)
        {
            throw new ArgumentException("At least one source path is required");
        }

        List<SourceFile> sourceFiles = new List<SourceFile>();
        foreach (string file in Directory.EnumerateFiles(rootPath, "*.pdml", SearchOption.AllDirectories))
        {
            sourceFiles.Add(SourceFile.Read(file));
        }

        return sourceFiles.ToArray();
    }

    public IOutputHandler<CompileResult>? ResolveOutputHandler(OutputFormat format, CompileSettings settings) => format switch
    {
        OutputFormat.Plain => new PlainCompileOutputHandler(),
        OutputFormat.Json => new JsonCompileOutputHandler(),
        _ => null
    };
}