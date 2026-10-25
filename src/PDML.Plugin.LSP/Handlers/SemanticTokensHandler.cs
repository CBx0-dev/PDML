using System.Collections.Concurrent;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace PDML.Plugin.LSP.Handlers;

public class SemanticTokensHandler : SemanticTokensHandlerBase
{
    public static readonly SemanticTokensLegend Legend = new SemanticTokensLegend
    {
        TokenTypes = new Container<SemanticTokenType>(
            SemanticTokenType.Keyword,
            SemanticTokenType.Type,
            SemanticTokenType.Property,
            SemanticTokenType.Operator
        ),
        TokenModifiers = new Container<SemanticTokenModifier>(
            SemanticTokenModifier.Declaration,
            SemanticTokenModifier.DefaultLibrary
        )
    };

    private readonly DocumentStore _documentStore;
    private readonly ConcurrentDictionary<DocumentUri, SemanticTokensDocument> _documents = new();

    public SemanticTokensHandler(DocumentStore? documentStore = null)
    {
        _documentStore = documentStore ?? new DocumentStore();
    }

    protected override SemanticTokensRegistrationOptions CreateRegistrationOptions(
        SemanticTokensCapability capability, 
        ClientCapabilities clientCapabilities)
    {
        return new SemanticTokensRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("pdml"),
            Legend = Legend,
            Full = new SemanticTokensCapabilityRequestFull
            {
                Delta = true
            },
            Range = true
        };
    }

    protected override Task<SemanticTokensDocument> GetSemanticTokensDocument(
        ITextDocumentIdentifierParams @params, 
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_documents.GetOrAdd(@params.TextDocument.Uri, _ => new SemanticTokensDocument(Legend)));
    }

    protected override Task Tokenize(
        SemanticTokensBuilder builder, 
        ITextDocumentIdentifierParams @params, 
        CancellationToken cancellationToken)
    {
        string? text = _documentStore.GetText(@params.TextDocument.Uri);
        if (text is not null)
        {
            string path = @params.TextDocument.Uri.GetFileSystemPath();
            List<SemanticTokenInfo> tokens = PdmlSemanticTokens.CollectTokens(text, path);
            foreach (SemanticTokenInfo token in tokens)
            {
                builder.Push(token.Line, token.Column, token.Length, token.TokenType, token.Modifiers);
            }
        }

        return Task.CompletedTask;
    }
}
