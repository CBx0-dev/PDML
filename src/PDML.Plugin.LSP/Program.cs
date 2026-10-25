using OmniSharp.Extensions.LanguageServer.Server;
using PDML.Plugin.LSP.Handlers;

namespace PDML.Plugin.LSP;

internal class Program
{
    public static async Task Main()
    {
        LanguageServer server = await LanguageServer.From(options => options
            .WithInput(Console.OpenStandardInput())
            .WithOutput(Console.OpenStandardOutput())
            .WithHandler<DiagnosticsHandler>()
        );

        await server.WaitForExit;
    }
}
