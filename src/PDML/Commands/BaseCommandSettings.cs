using PDML.Output;
using Spectre.Console.Cli;

namespace PDML.Commands;

public abstract class BaseCommandSettings : CommandSettings
{
    [CommandOption("-f|--format <VALUE>")]
    public OutputFormat OutputFormat { get; init; } = OutputFormat.Plain;
}