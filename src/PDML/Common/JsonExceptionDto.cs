namespace PDML.Common;

public record JsonExceptionDto
{
    public required string Name { get; init; }
    public required string Message { get; init; }
    public required JsonExceptionDto? InnerException { get; init; }

    public static JsonExceptionDto From(Exception exception)
    {
        JsonExceptionDto? innerException = exception.InnerException is not null
            ? From(exception.InnerException)
            : null;

        return new JsonExceptionDto
        {
            Name = exception.GetType().Name,
            Message = exception.Message,
            InnerException = innerException
        };
    }
}