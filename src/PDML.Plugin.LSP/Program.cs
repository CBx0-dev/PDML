using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Window;
using OmniSharp.Extensions.LanguageServer.Server;
using PDML.Plugin.LSP.Handlers;
using PDML.Plugin.LSP.Services;

namespace PDML.Plugin.LSP;

internal static class Program
{
    public static async Task Main()
    {

        LanguageServer server = await LanguageServer.From(static options =>
            {
                options.Services.AddSingleton<DocumentStoreService>();
                options.Services.AddSingleton<CompilationService>();

                options
                    .WithInput(Console.OpenStandardInput())
                    .WithOutput(Console.OpenStandardOutput())
                    .WithHandler<TextDocumentSyncHandler>()
                    .WithHandler<SemanticTokensHandler>();

                options.ConfigureLogging(logging => logging
                    .AddLanguageProtocolLogging()
                    .SetMinimumLevel(LogLevel.Trace)
                );

                options.OnStarted((server, cancellationToken) =>
                {
                    server.Window.LogInfo("PDML language started");
                    return Task.CompletedTask;
                });

                //.WithHandler<SemanticTokensHandler>()
                //.WithHandler<DefinitionHandler>();
            }
        );

        await server.WaitForExit;
    }
}