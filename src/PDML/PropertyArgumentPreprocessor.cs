namespace PDML;

internal sealed class PropertyArgumentPreprocessor : ICommandAppPreprocessor
{
    public IEnumerable<string> ProcessAsync(
        IEnumerable<string> args,
        CancellationToken cancellationToken = default
    )
    {
        using IEnumerator<string> enumerator = args.GetEnumerator();

        while (enumerator.MoveNext())
        {
            cancellationToken.ThrowIfCancellationRequested();

            string argument = enumerator.Current;
            int separator = argument.IndexOf(':');

            // Match -<key>:<item>[=<value>]
            if (separator <= 1 || argument[0] != '-')
            {
                yield return argument;
                continue;
            }

            string option = argument.Substring(0, separator);
            string item = argument.Substring(separator + 1);

            if (string.IsNullOrWhiteSpace(item))
            {
                yield return argument;
                continue;
            }

            if (item.Contains('='))
            {
                yield return option;
                yield return item;
                continue;
            }

            if (!enumerator.MoveNext())
            {
                throw new ArgumentException(
                    $"Missing value for argument '{argument}'.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            string value = enumerator.Current;

            yield return option;
            yield return $"{item}={value}";
        }
    }
}