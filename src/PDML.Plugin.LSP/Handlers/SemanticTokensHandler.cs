using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using PDML.Compiler.Common;
using PDML.Compiler.Diagnostics.Abstractions;
using PDML.Compiler.Parsing.Nodes;
using PDML.Plugin.LSP.SemanticTokens;
using PDML.Plugin.LSP.Services;

namespace PDML.Plugin.LSP.Handlers;

public sealed class SemanticTokensHandler : SemanticTokensHandlerBase
{
    private readonly IWindowLanguageServer _logger;
    private readonly CompilationService _compilationService;
    private readonly DocumentStoreService _documentStoreService;

    private readonly SemanticTokenGenerator _semanticTokenGenerator;

    private SemanticTokensLegend? _legend;

    public SemanticTokensHandler(
        IWindowLanguageServer logger,
        CompilationService compilationService,
        DocumentStoreService documentStoreService
    )
    {
        _logger = logger;
        _compilationService = compilationService;
        _documentStoreService = documentStoreService;

        _semanticTokenGenerator = new SemanticTokenGenerator();

        _legend = null;
    }

    protected override SemanticTokensRegistrationOptions CreateRegistrationOptions(
        SemanticTokensCapability capability,
        ClientCapabilities clientCapabilities)
    {
        _legend = new SemanticTokensLegend
        {
            TokenTypes = new Container<SemanticTokenType>(
                SemanticTokenType.Type,
                SemanticTokenType.Keyword,
                SemanticTokenType.Operator,
                SemanticTokenType.Property,
                SemanticTokenType.String
            ),
            TokenModifiers = new Container<SemanticTokenModifier>(
                SemanticTokenModifier.Declaration
            )
        };

        return new SemanticTokensRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("pdml"),
            Legend = _legend,
            Full = false,
            Range = true
        };
    }

    protected override Task Tokenize(SemanticTokensBuilder builder, ITextDocumentIdentifierParams identifier, CancellationToken cancellationToken)
    {
        SourceFile? source = _documentStoreService.Read(identifier.TextDocument.Uri);
        if (source is null)
        {
            return Task.CompletedTask;
        }

        Module? module = _compilationService.ParseModule(source, out DiagnosticBag diagnostics);
        if (module is null || diagnostics.HasErrors)
        {
            return Task.CompletedTask;
        }

        IReadOnlyList<SemanticToken> tokens = _semanticTokenGenerator.Generate(module);
        foreach (SemanticToken token in tokens)
        {
            builder.Push(
                token.Location.Start.Line - 1,
                token.Location.Start.Column - 1,
                token.Location.Length,
                token.Type,
                token.Modifiers
            );
        }

        return Task.CompletedTask;
    }

    protected override Task<SemanticTokensDocument> GetSemanticTokensDocument(ITextDocumentIdentifierParams @params, CancellationToken cancellationToken)
    {
        if (_legend is null)
        {
            throw new InvalidOperationException("Legend is not initialized");
        }

        return Task.FromResult(
            new SemanticTokensDocument(_legend)
        );
    }
}