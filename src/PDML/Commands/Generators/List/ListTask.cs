using PDML.Generator.Abstractions;
using PDML.Output;

namespace PDML.Commands.Generators.List;

internal sealed class ListTask : ITask<ListResult, ListSettings>
{
    public Task<ListResult> ExecuteAsync(ListSettings settings)
    {
        GeneratorLoader.LookupPaths.Add(AppContext.BaseDirectory);

        string? pathEnv = Environment.GetEnvironmentVariable("PDML_EXTENSIONS_PATH");
        if (pathEnv is not null)
        {
            GeneratorLoader.LookupPaths.AddRange(
                pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            );
        }

        if (settings.DllFolders is not null)
        {
            GeneratorLoader.LookupPaths.AddRange(settings.DllFolders);
        }

        GeneratorLoader.LoadAssemblies();

        return Task.FromResult(new ListResult
        {
            ExitCode = 0,
            Generators = GeneratorLoader.Generators.ToList()
        });
    }

    public IOutputHandler<ListResult>? ResolveOutputHandler(OutputFormat format, ListSettings settings) => format switch
    {
        OutputFormat.Plain => new PlainListOutputHandler(),
        OutputFormat.Json => new JsonListOutputHandler(),
        _ => null
    };
}