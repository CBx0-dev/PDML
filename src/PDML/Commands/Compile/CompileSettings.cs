using System.ComponentModel;
using Spectre.Console.Cli;

namespace PDML.Commands.Compile;

internal sealed class CompileSettings : BaseCommandSettings
{
    [CommandOption("--dll-path")]
    [Description("Add a folder path to lookup generator DLLs")]
    public string[]? DllFolders { get; init; }

    [CommandArgument(0, "[<path>]")]
    [Description("The path to *.pdml a file or a glob pattern that resolves to multiple files")]
    public required string[]? Paths { get; init; }

    [CommandOption("--stdin")]
    [Description("Reads the <stdin> as input and tries to parse it")]
    public required bool UseStdin { get; init; }

    [CommandOption("-g|--generator", true)]
    [Description("The generator key that determines the output file format")]
    public required string Generator { get; init; }

    [CommandOption("-p <VALUE>")]
    [Description("Options to pass to the generator")]
    public ILookup<string, string>? Properties { get; init; }

    [CommandOption("-o|--output")]
    [Description("The output directory")]
    public required string? OutputDirectory { get; init; }
}