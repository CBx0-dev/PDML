namespace PDML.Compiler.Common;

public sealed record Location
{
    public required SourceFile SourceFile { get; init; }
    public required Coordinate Start { get; init; }
    public required Coordinate End { get; init; }

    public int Length => End.Position - Start.Position;

    public bool InBounds(int line, int column) => (
        line >= Start.Line &&
        line <= End.Line &&
        column >= Start.Column &&
        column <= End.Column
    );

    public static Location Merge(Location left, Location right) => new Location
    {
        SourceFile = left.SourceFile,
        Start = left.Start,
        End = right.End
    };
}