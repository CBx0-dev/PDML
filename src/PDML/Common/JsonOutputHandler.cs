using System.Text.Json;
using PDML.Commands;
using PDML.Output;

namespace PDML.Common;

internal abstract class JsonOutputHandler<TResult> : IOutputHandler<TResult> where TResult : TaskResult
{
    public abstract void OnResult(TResult result);

    public void OnException(Exception exception)
    {
        WriteJson(JsonExceptionDto.From(exception));
    }

    protected void WriteJson(object data)
    {
        using Utf8JsonWriter writer = new Utf8JsonWriter(
            Console.OpenStandardOutput()
        );

        JsonSerializer.Serialize(writer, data);
        Console.WriteLine();
    }
}