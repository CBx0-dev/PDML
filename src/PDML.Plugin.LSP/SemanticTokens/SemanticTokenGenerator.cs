using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using PDML.Compiler.Common;
using PDML.Compiler.Lexing;
using PDML.Compiler.Parsing.Nodes;
using PDML.Compiler.Parsing.Nodes.Members;

namespace PDML.Plugin.LSP.SemanticTokens;

public sealed class SemanticTokenGenerator
{
    private readonly List<SemanticToken> _tokens = new List<SemanticToken>();

    public IReadOnlyList<SemanticToken> Generate(Module module)
    {
        _tokens.Clear();

        foreach (ImportMember import in module.Imports)
        {
            VisitImportMember(import);
        }

        foreach (TypeDefinitionMember definition in module.TypeDefinitions)
        {
            VisitTypeDefinitionMember(definition);
        }

        foreach (VirtualTypeDefinitionMember definition in module.VirtualTypeDefinitions)
        {
            VisitVirtualTypeDefinitionMember(definition);
        }

        return _tokens
            .OrderBy(token => token.Location.Start.Position)
            .ToList();
    }

    private void VisitImportMember(ImportMember import)
    {
        _tokens.Add(new SemanticToken(
            import.ImportKeyword.Location,
            SemanticTokenType.Keyword
        ));

        foreach (Token symbol in import.Symbols)
        {
            _tokens.Add(new SemanticToken(
                symbol.Location,
                SemanticTokenType.Type,
                SemanticTokenModifier.Declaration
            ));
        }

        _tokens.Add(new SemanticToken(
            import.FromKeyword.Location,
            SemanticTokenType.Keyword
        ));
        _tokens.Add(new SemanticToken(
            import.Locator.Location,
            SemanticTokenType.String
        ));
    }

    private void VisitTypeDefinitionMember(TypeDefinitionMember definition)
    {
        _tokens.Add(new SemanticToken(
            definition.TypeKeyword.Location,
            SemanticTokenType.Keyword
        ));
        _tokens.Add(new SemanticToken(
            definition.Name.Location,
            SemanticTokenType.Type,
            SemanticTokenModifier.Declaration
        ));
        if (definition.InheritingType is not null)
        {
            VisitTypeSignature(definition.InheritingType);
        }

        VisitTypeDefinitionItems(definition.Items);
    }

    private void VisitVirtualTypeDefinitionMember(VirtualTypeDefinitionMember definition)
    {
        _tokens.Add(new SemanticToken(
            definition.TypeKeyword.Location,
            SemanticTokenType.Keyword
        ));
        _tokens.Add(new SemanticToken(
            definition.Name.Location,
            SemanticTokenType.Type,
            SemanticTokenModifier.Declaration
        ));

        VisitTypeDefinitionItems(definition.Items);
    }

    private void VisitTypeDefinitionItems(DelimitedList<TypeDefinitionItem, Token> items)
    {
        foreach ((TypeDefinitionItem? item, Token? delimiter) in items.Entries)
        {
            if (delimiter is not null)
            {
                _tokens.Add(new SemanticToken(
                    delimiter.Location,
                    SemanticTokenType.Operator
                ));
                continue;
            }

            if (item is not null)
            {
                switch (item)
                {
                    case TypeDefinitionItemBody body:
                        VisitTypeDefinitionItemBody(body);
                        break;
                    case TypeDefinitionItemTarget target:
                        VisitTypeDefinitionItemTarget(target);
                        break;
                    default:
                        throw new InvalidOperationException("Unexpected type definition item");
                }
            }
        }
    }

    private void VisitTypeDefinitionItemBody(TypeDefinitionItemBody definition)
    {
        foreach (TypeDefinitionBodyProperty property in definition.Properties)
        {
            _tokens.Add(new SemanticToken(
                property.Name.Location,
                SemanticTokenType.Property,
                SemanticTokenModifier.Declaration
            ));
            _tokens.Add(new SemanticToken(
                property.TypeClause.Colon.Location,
                SemanticTokenType.Operator
            ));

            VisitTypeSignature(property.TypeClause.TypeSignature);
        }
    }

    private void VisitTypeDefinitionItemTarget(TypeDefinitionItemTarget definition)
    {
        VisitTypeSignature(definition.Type);
    }

    private void VisitTypeSignature(TypeSignature signature)
    {
        _tokens.Add(new SemanticToken(
            signature.Name.Location,
            SemanticTokenType.Type
        ));
    }
}