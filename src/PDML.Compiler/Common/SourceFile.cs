namespace PDML.Compiler.Common;

public sealed record SourceFile
{
    public required string Directory { get; init; }
    public required string FileName { get; init; }
    public required string Content { get; init; }

    public int Length => Content.Length;
    public string FilePath => Path.Combine(Directory, FileName);

    public char this[int index] => Content[index];

    public static SourceFile Read(string path)
    {
        path = Path.GetFullPath(path);

        string directory = Path.GetDirectoryName(path) ?? ".";
        string fileName = Path.GetFileName(path);
        string content = File.ReadAllText(path);

        return new SourceFile
        {
            Directory = directory,
            FileName = fileName,
            Content = content
        };
    }

    public static SourceFile Create(string directory, string fileName, string content) => new SourceFile
    {
        Directory = directory,
        FileName = fileName,
        Content = content
    };
}