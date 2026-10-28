namespace PDML;

internal interface ICommandAppPreprocessor
{
    public IEnumerable<string> ProcessAsync(IEnumerable<string> args, CancellationToken cancellationToken = default);
}