using PDML.Output;

namespace PDML.Commands;

public interface ITask<TResult, in TSettings>
    where TResult : TaskResult
    where TSettings : BaseCommandSettings
{
    public Task<TResult> ExecuteAsync(TSettings settings);

    public IOutputHandler<TResult>? ResolveOutputHandler(OutputFormat format, TSettings settings);
}