using NSubstitute;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using PDML.Plugin.LSP.Handlers;

namespace PDML.Plugin.LSP.Tests;

public class DiagnosticsHandlerTests
{
    private readonly ILanguageServerFacade _server;
    private readonly ITextDocumentLanguageServer _textDocumentServer;
    private readonly DiagnosticsHandler _handler;

    public DiagnosticsHandlerTests()
    {
        _server = Substitute.For<ILanguageServerFacade>();
        _textDocumentServer = Substitute.For<ITextDocumentLanguageServer>();
        _server.TextDocument.Returns(_textDocumentServer);
        _handler = new DiagnosticsHandler(_server);
    }

    [Fact]
    public void GetTextDocumentAttributes_ReturnsPdmlLanguage()
    {
        DocumentUri uri = DocumentUri.From("file:///path/to/test.pdml");
        TextDocumentAttributes attributes = _handler.GetTextDocumentAttributes(uri);

        Assert.Equal("pdml", attributes.LanguageId);
    }

    [Fact]
    public async Task HandleDidOpen_ValidPdml_PublishesEmptyDiagnostics()
    {
        DocumentUri uri = DocumentUri.From("file:///path/to/test.pdml");
        string content = "type Valid = { id: int32; };";

        PublishDiagnosticsParams? published = null;
        _textDocumentServer.When(x => x.PublishDiagnostics(Arg.Any<PublishDiagnosticsParams>()))
            .Do(x => published = x.Arg<PublishDiagnosticsParams>());

        await _handler.Handle(new DidOpenTextDocumentParams
        {
            TextDocument = new TextDocumentItem
            {
                Uri = uri,
                LanguageId = "pdml",
                Version = 1,
                Text = content
            }
        }, CancellationToken.None);

        Assert.NotNull(published);
        Assert.Equal(uri, published.Uri);
        Assert.Empty(published.Diagnostics);
    }

    [Fact]
    public async Task HandleDidOpen_SyntaxError_PublishesParsingDiagnosticWithCorrectRange()
    {
        DocumentUri uri = DocumentUri.From("file:///path/to/test.pdml");
        string content = "invalidToken type Valid = Target;";

        PublishDiagnosticsParams? published = null;
        _textDocumentServer.When(x => x.PublishDiagnostics(Arg.Any<PublishDiagnosticsParams>()))
            .Do(x => published = x.Arg<PublishDiagnosticsParams>());

        await _handler.Handle(new DidOpenTextDocumentParams
        {
            TextDocument = new TextDocumentItem
            {
                Uri = uri,
                LanguageId = "pdml",
                Version = 1,
                Text = content
            }
        }, CancellationToken.None);

        Assert.NotNull(published);
        Assert.NotEmpty(published.Diagnostics);
        Diagnostic diag = Assert.Single(published.Diagnostics, d => d.Code?.String == "PDML2001");
        Assert.Equal(DiagnosticSeverity.Error, diag.Severity);
        Assert.Equal("pdml", diag.Source);
        // Position should be 0-based
        Assert.Equal(0, diag.Range.Start.Line);
    }

    [Fact]
    public async Task HandleDidOpen_BindingError_PublishesBindingDiagnosticWithRelatedInformation()
    {
        DocumentUri uri = DocumentUri.From("file:///path/to/test.pdml");
        string content = """
                         type User = { id: int64; };
                         type User = { name: string; };
                         """;

        PublishDiagnosticsParams? published = null;
        _textDocumentServer.When(x => x.PublishDiagnostics(Arg.Any<PublishDiagnosticsParams>()))
            .Do(x => published = x.Arg<PublishDiagnosticsParams>());

        await _handler.Handle(new DidOpenTextDocumentParams
        {
            TextDocument = new TextDocumentItem
            {
                Uri = uri,
                LanguageId = "pdml",
                Version = 1,
                Text = content
            }
        }, CancellationToken.None);

        Assert.NotNull(published);
        Assert.NotEmpty(published.Diagnostics);
        Diagnostic diag = Assert.Single(published.Diagnostics);
        Assert.Equal(DiagnosticSeverity.Error, diag.Severity);
        Assert.Equal("PDML3000", diag.Code?.String);
        // Second User is on line 2 (0-based line 1)
        Assert.Equal(1, diag.Range.Start.Line);
        Assert.NotNull(diag.RelatedInformation);
        DiagnosticRelatedInformation related = Assert.Single(diag.RelatedInformation);
        Assert.Equal(0, related.Location.Range.Start.Line);
    }

    [Fact]
    public async Task HandleDidChange_PublishesUpdatedDiagnostics()
    {
        DocumentUri uri = DocumentUri.From("file:///path/to/test.pdml");
        string updatedContent = "type Incomplete";

        PublishDiagnosticsParams? published = null;
        _textDocumentServer.When(x => x.PublishDiagnostics(Arg.Any<PublishDiagnosticsParams>()))
            .Do(x => published = x.Arg<PublishDiagnosticsParams>());

        await _handler.Handle(new DidChangeTextDocumentParams
        {
            TextDocument = new OptionalVersionedTextDocumentIdentifier
            {
                Uri = uri,
                Version = 2
            },
            ContentChanges = new Container<TextDocumentContentChangeEvent>(
                new TextDocumentContentChangeEvent
                {
                    Text = updatedContent
                }
            )
        }, CancellationToken.None);

        Assert.NotNull(published);
        Assert.NotEmpty(published.Diagnostics);
    }

    [Fact]
    public async Task HandleDidClose_ClearsDiagnostics()
    {
        DocumentUri uri = DocumentUri.From("file:///path/to/test.pdml");

        PublishDiagnosticsParams? published = null;
        _textDocumentServer.When(x => x.PublishDiagnostics(Arg.Any<PublishDiagnosticsParams>()))
            .Do(x => published = x.Arg<PublishDiagnosticsParams>());

        await _handler.Handle(new DidCloseTextDocumentParams
        {
            TextDocument = new TextDocumentIdentifier
            {
                Uri = uri
            }
        }, CancellationToken.None);

        Assert.NotNull(published);
        Assert.Equal(uri, published.Uri);
        Assert.Empty(published.Diagnostics);
    }
}
