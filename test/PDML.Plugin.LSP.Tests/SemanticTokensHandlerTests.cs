using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using PDML.Plugin.LSP.Handlers;

namespace PDML.Plugin.LSP.Tests;

public class SemanticTokensHandlerTests
{
    [Fact]
    public void Legend_ExposesExpectedTokenTypesAndModifiers()
    {
        Assert.Contains(SemanticTokenType.Keyword, SemanticTokensHandler.Legend.TokenTypes);
        Assert.Contains(SemanticTokenType.Type, SemanticTokensHandler.Legend.TokenTypes);
        Assert.Contains(SemanticTokenType.Property, SemanticTokensHandler.Legend.TokenTypes);
        Assert.Contains(SemanticTokenType.Operator, SemanticTokensHandler.Legend.TokenTypes);

        Assert.Contains(SemanticTokenModifier.Declaration, SemanticTokensHandler.Legend.TokenModifiers);
        Assert.Contains(SemanticTokenModifier.DefaultLibrary, SemanticTokensHandler.Legend.TokenModifiers);
    }

    [Fact]
    public async Task Handle_SemanticTokensFull_ReturnsSemanticTokens()
    {
        DocumentStore store = new DocumentStore();
        SemanticTokensHandler handler = new SemanticTokensHandler(store);

        DocumentUri uri = DocumentUri.From("file:///path/to/test.pdml");
        string content = "@type User = {\n    id: int32;\n};";
        store.Update(uri, content);

        SemanticTokensParams @params = new SemanticTokensParams
        {
            TextDocument = new TextDocumentIdentifier(uri)
        };

        SemanticTokens? result = await handler.Handle(@params, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Data);
    }

    [Fact]
    public async Task Handle_DocumentNotInStore_ReturnsEmptyOrNoTokens()
    {
        DocumentStore store = new DocumentStore();
        SemanticTokensHandler handler = new SemanticTokensHandler(store);

        DocumentUri uri = DocumentUri.From("file:///path/to/nonexistent.pdml");

        SemanticTokensParams @params = new SemanticTokensParams
        {
            TextDocument = new TextDocumentIdentifier(uri)
        };

        SemanticTokens? result = await handler.Handle(@params, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result.Data);
    }
}
