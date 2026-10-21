namespace PDML.Compiler.Common;

public sealed record Coordinate
{
    public required int Position { get; set; }
    public required int Line { get; set; }
    public required int Column { get; set; }
}