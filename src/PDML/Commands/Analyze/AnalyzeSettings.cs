using System.ComponentModel;
using Spectre.Console.Cli;

namespace PDML.Commands.Analyze;

internal sealed class AnalyzeSettings : BaseCommandSettings
{
    [CommandArgument(0, "[<path>]")]
    [Description("The path to *.pdml a file or a glob pattern that resolves to multiple files")]
    public required string[]? Paths { get; init; }

    [CommandOption("--stdin")]
    [Description("Reads the <stdin> as input and tries to parse it")]
    public required bool UseStdin { get; init; }
}