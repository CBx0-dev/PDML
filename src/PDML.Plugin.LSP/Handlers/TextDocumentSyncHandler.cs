using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using PDML.Compiler;
using PDML.Plugin.LSP.Diagnostics;
using PDML.Plugin.LSP.Services;

namespace PDML.Plugin.LSP.Handlers;

public sealed class TextDocumentSyncHandler : TextDocumentSyncHandlerBase
{
    private readonly IWindowLanguageServer _logger;
    private readonly ILanguageServerFacade _server;
    private readonly DocumentStoreService _documentStoreService;
    private readonly CompilationService _compilationService;

    public TextDocumentSyncHandler(
        IWindowLanguageServer logger,
        ILanguageServerFacade server,
        DocumentStoreService documentStoreService,
        CompilationService compilationService
    )
    {
        _logger = logger;
        _server = server;
        _documentStoreService = documentStoreService;
        _compilationService = compilationService;
    }

    public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri)
    {
        return new TextDocumentAttributes(uri, "pdml");
    }

    protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(
        TextSynchronizationCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new TextDocumentSyncRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("pdml"),
            Change = TextDocumentSyncKind.Full
        };
    }

    public override Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
    {
        _documentStoreService.Update(request.TextDocument.Uri);
        Handle(request.TextDocument.Uri);

        return Unit.Task;
    }

    public override Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
    {
        TextDocumentContentChangeEvent[] changes = request.ContentChanges.ToArray();
        if (changes.Length != 1)
        {
            return Unit.Task;
        }

        _documentStoreService.Update(request.TextDocument.Uri, changes[0].Text);
        Handle(request.TextDocument.Uri);

        return Unit.Task;
    }

    public override Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
    {
        _documentStoreService.Update(request.TextDocument.Uri);
        Handle(request.TextDocument.Uri);

        return Unit.Task;
    }

    public override Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
    {
        return Unit.Task;
    }

    private void Handle(DocumentUri document)
    {
        _compilationService.NextCompilation();

        Compilation compilation = _compilationService.Current;
        LspDiagnosticBag diagnostics = (LspDiagnosticBag)compilation.Diagnostics;

        _server.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
        {
            Diagnostics = new Container<Diagnostic>(
                diagnostics.Where(diagnostic => diagnostic.Document == document)
            ),
            Uri = document,
        });

    }
}