using PDML.Commands;
using Spectre.Console.Cli;

namespace PDML;

public static class ConfiguratorExtension
{
    public static ICommandConfigurator AddCommand<TSettings, TTask, TResult>(this IConfigurator configurator, string name)
        where TSettings : BaseCommandSettings
        where TTask : ITask<TResult, TSettings>, new()
        where TResult : TaskResult
        => configurator.AddCommand<TaskCommand<TSettings, TTask, TResult>>(name);
}