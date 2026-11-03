namespace PDML.Compiler.Common;

public record struct Coordinate
{
    public required int Position { get; set; }
    public required int Line { get; set; }
    public required int Column { get; set; }
}