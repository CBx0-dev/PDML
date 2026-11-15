namespace PDML.Output;

public static class OutputFormatExtension
{
    public static string ToNameString(this OutputFormat format) => format switch
    {
        OutputFormat.Plain => "plain",
        OutputFormat.Json => "json",
        _ => throw new ArgumentException($"Invalid output format value '{format}'", nameof(format))
    };
}