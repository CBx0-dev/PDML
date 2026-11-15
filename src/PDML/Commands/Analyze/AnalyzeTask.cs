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
        SourceFile[] sourceFiles = ReadFiles(settings.Paths, settings.UseStdin);
        Compilation compilation = CompilerService.CompileEntryPoints(
            new DefaultDiagnosticService(),
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

    private SourceFile[] ReadFiles(string[]? paths, bool useStdin)
    {
        if (useStdin)
        {
            if (paths is not null)
            {
                throw new ArgumentException("Standard input ('-') cannot be combined with other input files.");
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

        if (paths is null)
        {
            throw new ArgumentException("At least one source path is required");
        }

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

    public IOutputHandler<AnalyzeResult>? ResolveOutputHandler(OutputFormat format, AnalyzeSettings settings) => format switch
    {
        OutputFormat.Plain => new PlainAnalyzeOutputHandler(),
        OutputFormat.Json => new JsonAnalyzeOutputHandler(),
        _ => null
    };
}