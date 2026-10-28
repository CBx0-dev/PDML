using Spectre.Console.Cli;

namespace PDML;

internal static class CommandAppExtension
{
    public static int Run(
        this CommandApp app,
        IEnumerable<string> args,
        IEnumerable<ICommandAppPreprocessor> preprocessors,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(preprocessors);

        List<string> processed = new List<string>(args);
        foreach (ICommandAppPreprocessor preprocessor in preprocessors)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string[] processing = processed.ToArray();
            processed.Clear();
            foreach (string arg in preprocessor.ProcessAsync(processing, cancellationToken))
            {
                processed.Add(arg);
            }
        }
        
        return app.Run(processed, cancellationToken);
    }
}