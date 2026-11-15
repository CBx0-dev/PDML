using System.ComponentModel;
using Spectre.Console.Cli;

namespace PDML.Commands.Generators.List;

internal sealed class ListSettings : BaseCommandSettings
{
    [CommandOption("--dll-path")]
    [Description("Add a folder path to lookup generator DLLs")]
    public string[]? DllFolders { get; init; }

}