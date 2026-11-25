using System.ComponentModel;
using Spectre.Console.Cli;

namespace PDML.Commands.Analyze;

internal sealed class AnalyzeSettings : BaseCommandSettings
{
    [CommandArgument(0, "[<path>]")]
    [Description("The path to the project root. All *.pdml files are recusivly collected")]
    public required string? RootPath { get; init; }

    [CommandOption("--stdin")]
    [Description("Reads the <stdin> as input and tries to parse it")]
    public required bool UseStdin { get; init; }
}