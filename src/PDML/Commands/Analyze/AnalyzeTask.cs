using GlobExpressions;
using PDML.Compiler;
using PDML.Compiler.Common;
using PDML.Compiler.Diagnostics;
using PDML.Output;

namespace PDML.Commands.Analyze;

internal sealed class AnalyzeTask : ITask<AnalyzeResult, AnalyzeSettings>
{
    public Task<AnalyzeResult> ExecuteAsync(AnalyzeSettings settings)
    {
        SourceFile[] sourceFiles = ReadFiles(settings.RootPath, settings.UseStdin);
        Compilation compilation = CompilerService.CompileEntryPoints(
            new DefaultDiagnosticService(),
            settings.UseStdin
            ? Directory.GetCurrentDirectory()
            : settings.RootPath!,
            sourceFiles
        );

        return Task.FromResult(new AnalyzeResult
        {
            ExitCode = compilation.HasErrors
                ? 1
                : 0,
            Diagnostics = ((IEnumerable<Diagnostic>)compilation.Diagnostics).ToList()
        });
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

    public IOutputHandler<AnalyzeResult>? ResolveOutputHandler(OutputFormat format, AnalyzeSettings settings) => format switch
    {
        OutputFormat.Plain => new PlainAnalyzeOutputHandler(),
        OutputFormat.Json => new JsonAnalyzeOutputHandler(),
        _ => null
    };
}