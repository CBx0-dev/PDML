namespace PDML.Generator.Abstractions;

public static class StringExtension
{
    public static string ToPascalCase(this string value)
    {
        string firstChar = char.ToUpperInvariant(value[0]).ToString();
        string rest = value.Substring(1).ToLowerInvariant();

        return firstChar + rest;
    }

    public static string ToCamelCase(this string value)
    {
        string firstChar = char.ToLowerInvariant(value[0]).ToString();
        string rest = value.Substring(1);

        return firstChar + rest;
    }
}