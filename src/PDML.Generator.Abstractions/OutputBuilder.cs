namespace PDML.Generator.Abstractions;

public sealed class OutputBuilder
{
    private readonly List<OutputFile> _outputFiles = new List<OutputFile>();

    public void AddOutput(OutputFile file)
        => _outputFiles.Add(file);

    public void SaveTo(string rootDir)
    {
        foreach (OutputFile file in _outputFiles)
        {
            string path = Path.Combine(rootDir, file.LocalName);
            File.WriteAllText(path, file.Content);
        }
    }

    public IReadOnlyList<OutputFile> GetFiles() => _outputFiles;
}