namespace PDML.Generator.Abstractions;

public sealed class SettingsMappingException : Exception
{
    public SettingsMappingException(string message) : base(message)
    {
    }

    public SettingsMappingException(string message, Exception innerException) : base(message, innerException)
    {
    }
}