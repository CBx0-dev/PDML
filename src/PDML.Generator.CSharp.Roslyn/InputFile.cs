namespace PDML.Generator.CSharp.Roslyn;

internal sealed record InputFile
{
    public required string Namespace { get; init; }
    public required string Path { get; init; }
    public required string? Content { get; init; }
}
