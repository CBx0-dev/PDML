using PDML.Commands;
using Spectre.Console.Cli;

namespace PDML;

public static class Program
{
    public static int Main(string[] args)
    {
        CommandApp app = new CommandApp();
        app.Configure(config =>
        {
            config.AddCommand<CompileCommand>("compile");
        });
        
        return app.Run(args);
    }
}