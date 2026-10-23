using OmniSharp.Extensions.LanguageServer.Server;

namespace PDML.Plugin.LSP;

internal class Program
{
    public static async Task Main()
    {
        LanguageServer server = await LanguageServer.From(options => options
            .WithInput(Console.OpenStandardInput())
            .WithOutput(Console.OpenStandardOutput())
        );

        await server.WaitForExit;
    }
}
