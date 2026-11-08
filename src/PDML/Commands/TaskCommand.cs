using PDML.Output;
using Spectre.Console.Cli;

namespace PDML.Commands;

public sealed class TaskCommand<TSettings, TTask, TResult> : AsyncCommand<TSettings>
    where TSettings : BaseCommandSettings
    where TTask : ITask<TResult, TSettings>, new()
    where TResult : TaskResult
{
    public override async Task<int> ExecuteAsync(CommandContext context, TSettings settings, CancellationToken cancellationToken)
    {
        TTask task = new TTask();
        IOutputHandler<TResult>? handler = task.ResolveOutputHandler(settings.OutputFormat, settings);
        if (handler is null)
        {
            throw new InvalidOperationException($"Command '{context.Name}' dont has a output pipeline for format '{settings.OutputFormat.ToNameString()}'");
        }

        try
        {
            TResult result = await task.ExecuteAsync(settings);
            handler.OnResult(result);

            return result.ExitCode;
        }
        catch (Exception exception)
        {
            handler.OnException(exception);
            return 1;
        }
    }
}