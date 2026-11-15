using Spectre.Console.Cli;
using System.Globalization;
using PDML.Commands;
using PDML.Commands.Analyze;
using PDML.Commands.Compile;
using PDML.Commands.Generators.List;

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
            config.AddCommand<AnalyzeSettings, AnalyzeTask, AnalyzeResult>("analyze");
            config.AddBranch("generators", group =>
            {
                group.AddCommand<TaskCommand<ListSettings, ListTask, ListResult>>("list");
            });
        });

        return app.Run(args, [new PropertyArgumentPreprocessor()]);
    }
}