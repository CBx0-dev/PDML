namespace PDML.Generator.Abstractions;

public sealed record OutputFile
{
    public string LocalName { get; }
    public string Content { get; }

    public OutputFile(string localName, string content)
    {
        LocalName = localName;
        Content = content;
    }

    public static OutputFile From(string localName, StringWriter writer) 
        => new OutputFile(localName, writer.ToString());

    public static OutputFile From(string localName, string content)
        => new OutputFile(localName, content);
}