using Spectre.Console.Cli;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PDML.Commands;

internal sealed class CompileSettings : CommandSettings
{
    [CommandOption("--dll-path")]
    [Description("Add a folder path to lookup generator DLLs")]
    public string[]? DllFolders { get; init; }

    [MinLength(1)]
    [CommandArgument(0, "<path>")]
    [Description("The path to *.pdml a file or a glob pattern that resolves to multiple files")]
    public required string[] Paths { get; init; }

    [CommandOption("--generator", true)]
    [Description("The generator key that determines the output file format")]
    public required string Generator { get; init; }

    [CommandOption("-p <VALUE>", true)]
    [Description("Options to pass to the generator")]
    public required ILookup<string, string> Properties { get; init; }
}