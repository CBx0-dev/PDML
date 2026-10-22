using PDML.Commands;
using Spectre.Console.Cli;
using System.Globalization;

namespace PDML;

public static class Program
{
    public static int Main(string[] args)
    {
        CommandApp app = new CommandApp();
        app.Configure(config =>
        {
            config.Settings.Culture = CultureInfo.InvariantCulture;
            config.AddCommand<CompileCommand>("compile");
        });
        
        return app.Run(args);
    }
}