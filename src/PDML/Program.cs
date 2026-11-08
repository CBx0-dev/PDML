using Spectre.Console.Cli;
using System.Globalization;
using PDML.Commands.Compile;

namespace PDML;

public static class Program
{
    public static int Main(string[] args)
    {
        CommandApp app = new CommandApp();
        app.Configure(config =>
        {
            config.Settings.Culture = CultureInfo.InvariantCulture;
            config.AddCommand<CompileSettings, CompileTask, CompileResult>("compile");
        });

        return app.Run(args, [new PropertyArgumentPreprocessor()]);
    }
}