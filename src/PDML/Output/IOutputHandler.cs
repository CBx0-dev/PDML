using PDML.Commands;

namespace PDML.Output;

public interface IOutputHandler<in TResult> where TResult : TaskResult
{
    public void OnResult(TResult result);

    public void OnException(Exception exception);
}