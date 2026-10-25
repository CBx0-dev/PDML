using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using PDML.Compiler.Common;
using PDML.Compiler.Diagnostics;
using PDML.Compiler.Lexing;
using PDML.Compiler.Parsing;
using PDML.Compiler.Parsing.Nodes;
using PDML.Compiler.Parsing.Nodes.Members;
using System.IO;

namespace PDML.Plugin.LSP;

internal readonly struct SemanticTokenInfo : IEquatable<SemanticTokenInfo>
{
    public int Line { get; }
    public int Column { get; }
    public int Length { get; }
    public SemanticTokenType TokenType { get; }
    public SemanticTokenModifier[] Modifiers { get; }

    public SemanticTokenInfo(int line, int column, int length, SemanticTokenType tokenType, SemanticTokenModifier[] modifiers)
    {
        Line = line;
        Column = column;
        Length = length;
        TokenType = tokenType;
        Modifiers = modifiers;
    }

    public bool Equals(SemanticTokenInfo other)
    {
        return Line == other.Line &&
               Column == other.Column &&
               Length == other.Length &&
               TokenType == other.TokenType &&
               Modifiers.SequenceEqual(other.Modifiers);
    }

    public override bool Equals(object? obj) => obj is SemanticTokenInfo other && Equals(other);

    public override int GetHashCode()
    {
        HashCode hash = new HashCode();
        hash.Add(Line);
        hash.Add(Column);
        hash.Add(Length);
        hash.Add(TokenType);
        foreach (SemanticTokenModifier mod in Modifiers)
        {
            hash.Add(mod);
        }
        return hash.ToHashCode();
    }

    public override string ToString() =>
        $"SemanticTokenInfo(Line={Line}, Column={Column}, Length={Length}, Type={TokenType}, Modifiers=[{string.Join(", ", Modifiers.Select(m => m.ToString()))}])";
}

internal static class PdmlSemanticTokens
{
    private static readonly HashSet<string> IntrinsicTypes = new(StringComparer.Ordinal)
    {
        "uint8", "uint16", "uint32", "uint64",
        "int8", "int16", "int32", "int64",
        "float32", "float64", "decimal",
        "char", "bool", "string",
        "date", "time", "datetime"
    };

    public static List<SemanticTokenInfo> CollectTokens(string content, string? filePath = null)
    {
        List<SemanticTokenInfo> result = new();
        if (string.IsNullOrEmpty(content))
        {
            return result;
        }

        string fileDirectory = filePath is not null ? Path.GetDirectoryName(filePath) ?? "." : ".";
        string fileName = filePath is not null ? Path.GetFileName(filePath) : "document.pdml";
        if (string.IsNullOrEmpty(fileName))
        {
            fileName = "document.pdml";
        }

        SourceFile sourceFile = SourceFile.Create(fileDirectory, fileName, content);
        DiagnosticBag diagnostics = new DiagnosticBag();
        Lexer lexer = new Lexer(diagnostics, sourceFile);
        List<Token> tokens = new List<Token>();
        Token token;

        do
        {
            token = lexer.NextToken();
            if (token.Type is not TokenType.Error and not TokenType.Whitespace)
            {
                tokens.Add(token);
            }
        } while (token.Type is not TokenType.Eof);

        // 1. Lexer tokens: keywords and operators
        foreach (Token tok in tokens)
        {
            if (tok.Type == TokenType.TypeKeyword)
            {
                AddToken(result, tok, SemanticTokenType.Keyword);
            }
            else if (tok.Type is TokenType.Equals or TokenType.Ampersand)
            {
                AddToken(result, tok, SemanticTokenType.Operator);
            }
        }

        // 2. AST tokens: type definitions, inheritance, compositions, bodies, properties, and type signatures
        Parser parser = new Parser(diagnostics, tokens);
        Module module = parser.ParseModule();

        foreach (TypeDefinitionMember def in module.TypeDefinitions)
        {
            AddToken(result, def.Name, SemanticTokenType.Type, SemanticTokenModifier.Declaration);

            if (def.InheritingType is not null)
            {
                CollectTypeSignatureTokens(result, def.InheritingType);
            }

            foreach (TypeDefinitionItem item in def.Items)
            {
                if (item is TypeDefinitionItemTarget target)
                {
                    CollectTypeSignatureTokens(result, target.Type);
                }
                else if (item is TypeDefinitionItemBody body)
                {
                    foreach (TypeDefinitionBodyProperty prop in body.Properties)
                    {
                        AddToken(result, prop.Name, SemanticTokenType.Property, SemanticTokenModifier.Declaration);
                        CollectTypeSignatureTokens(result, prop.TypeClause.TypeSignature);
                    }
                }
            }
        }

        return result
            .OrderBy(t => t.Line)
            .ThenBy(t => t.Column)
            .ToList();
    }

    private static void CollectTypeSignatureTokens(List<SemanticTokenInfo> result, TypeSignature signature)
    {
        if (IntrinsicTypes.Contains(signature.Name.Value))
        {
            AddToken(result, signature.Name, SemanticTokenType.Type, SemanticTokenModifier.DefaultLibrary);
        }
        else
        {
            AddToken(result, signature.Name, SemanticTokenType.Type);
        }

        if (signature.Generics is not null)
        {
            foreach (TypeSignature generic in signature.Generics)
            {
                CollectTypeSignatureTokens(result, generic);
            }
        }
    }

    private static void AddToken(
        List<SemanticTokenInfo> result,
        Token tok,
        SemanticTokenType tokenType,
        params SemanticTokenModifier[] modifiers)
    {
        int line = Math.Max(0, tok.Location.Start.Line - 1);
        int col = Math.Max(0, tok.Location.Start.Column - 1);
        int length = tok.Value.Length;
        if (length > 0)
        {
            result.Add(new SemanticTokenInfo(line, col, length, tokenType, modifiers));
        }
    }
}
