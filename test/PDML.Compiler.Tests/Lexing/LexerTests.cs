using PDML.Compiler.Common;
using PDML.Compiler.Diagnostics;
using PDML.Compiler.Lexing;

namespace PDML.Compiler.Tests.Lexing;

public sealed class LexerTests
{
    private static (Lexer Lexer, DiagnosticBag Diagnostics) CreateLexer(string content)
    {
        DiagnosticBag diagnostics = new DiagnosticBag();
        SourceFile sourceFile = SourceFile.Create(".", "test.pdml", content);
        Lexer lexer = new Lexer(diagnostics, sourceFile);
        return (lexer, diagnostics);
    }

    private static List<Token> LexAll(string content, out DiagnosticBag diagnostics)
    {
        (Lexer lexer, DiagnosticBag diag) = CreateLexer(content);
        diagnostics = diag;

        List<Token> tokens = new List<Token>();
        while (true)
        {
            Token token = lexer.NextToken();
            tokens.Add(token);
            if (token.Type == TokenType.Eof)
            {
                break;
            }
        }

        return tokens;
    }

    [Fact]
    public void NextToken_WithEmptySource_ReturnsEofToken()
    {
        (Lexer lexer, DiagnosticBag diagnostics) = CreateLexer("");

        Token token = lexer.NextToken();

        Assert.Equal(TokenType.Eof, token.Type);
        Assert.Equal("", token.Value);
        Assert.Equal(0, token.Location.Start.Position);
        Assert.Equal(1, token.Location.Start.Line);
        Assert.Equal(1, token.Location.Start.Column);
        Assert.Equal(0, token.Location.End.Position);
        Assert.Equal(1, token.Location.End.Line);
        Assert.Equal(1, token.Location.End.Column);
        Assert.False(diagnostics.HasErrors);
    }

    [Fact]
    public void NextToken_WhenCalledRepeatedlyAtEof_ConsistentlyReturnsEof()
    {
        (Lexer lexer, _) = CreateLexer("");

        Token first = lexer.NextToken();
        Token second = lexer.NextToken();
        Token third = lexer.NextToken();

        Assert.Equal(TokenType.Eof, first.Type);
        Assert.Equal(TokenType.Eof, second.Type);
        Assert.Equal(TokenType.Eof, third.Type);
    }

    [Theory]
    [InlineData("&", TokenType.Ampersand)]
    [InlineData(":", TokenType.Colon)]
    [InlineData(";", TokenType.Semicolon)]
    [InlineData(",", TokenType.Comma)]
    [InlineData("=", TokenType.Equals)]
    [InlineData("{", TokenType.LBrace)]
    [InlineData("}", TokenType.RBrace)]
    [InlineData("<", TokenType.LAngle)]
    [InlineData(">", TokenType.RAngle)]
    public void NextToken_WithSingleCharacterSymbol_ReturnsExpectedTokenTypeAndValue(string symbol, TokenType expectedType)
    {
        (Lexer lexer, DiagnosticBag diagnostics) = CreateLexer(symbol);

        Token token = lexer.NextToken();
        Token eof = lexer.NextToken();

        Assert.Equal(expectedType, token.Type);
        Assert.Equal(symbol, token.Value);
        Assert.Equal(0, token.Location.Start.Position);
        Assert.Equal(1, token.Location.Start.Line);
        Assert.Equal(1, token.Location.Start.Column);
        Assert.Equal(1, token.Location.End.Position);
        Assert.Equal(1, token.Location.End.Line);
        Assert.Equal(2, token.Location.End.Column);
        Assert.Equal(TokenType.Eof, eof.Type);
        Assert.False(diagnostics.HasErrors);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("hello")]
    [InlineData("identifier123")]
    [InlineData("PascalCase")]
    [InlineData("camelCase")]
    public void NextToken_WithIdentifier_ReturnsIdentifierToken(string identifier)
    {
        (Lexer lexer, DiagnosticBag diagnostics) = CreateLexer(identifier);

        Token token = lexer.NextToken();
        Token eof = lexer.NextToken();

        Assert.Equal(TokenType.Identifier, token.Type);
        Assert.Equal(identifier, token.Value);
        Assert.Equal(0, token.Location.Start.Position);
        Assert.Equal(identifier.Length, token.Location.End.Position);
        Assert.Equal(TokenType.Eof, eof.Type);
        Assert.False(diagnostics.HasErrors);
    }

    [Fact]
    public void NextToken_WithTypeKeyword_ReturnsTypeKeywordToken()
    {
        (Lexer lexer, DiagnosticBag diagnostics) = CreateLexer("type");

        Token token = lexer.NextToken();
        Token eof = lexer.NextToken();

        Assert.Equal(TokenType.TypeKeyword, token.Type);
        Assert.Equal("type", token.Value);
        Assert.Equal(0, token.Location.Start.Position);
        Assert.Equal(1, token.Location.Start.Line);
        Assert.Equal(1, token.Location.Start.Column);
        Assert.Equal(4, token.Location.End.Position);
        Assert.Equal(1, token.Location.End.Line);
        Assert.Equal(5, token.Location.End.Column);
        Assert.Equal(TokenType.Eof, eof.Type);
        Assert.False(diagnostics.HasErrors);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("    ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData(" \t\r\n  ")]
    public void NextToken_WithWhitespace_ReturnsWhitespaceToken(string whitespace)
    {
        (Lexer lexer, DiagnosticBag diagnostics) = CreateLexer(whitespace);

        Token token = lexer.NextToken();
        Token eof = lexer.NextToken();

        Assert.Equal(TokenType.Whitespace, token.Type);
        Assert.Equal(whitespace, token.Value);
        Assert.Equal(0, token.Location.Start.Position);
        Assert.Equal(whitespace.Length, token.Location.End.Position);
        Assert.Equal(TokenType.Eof, eof.Type);
        Assert.False(diagnostics.HasErrors);
    }

    [Fact]
    public void NextToken_WithPdmlSourceSnippet_LexesAllTokensInExpectedOrder()
    {
        const string source = "type Core {\n  val: Type;\n}";
        List<Token> tokens = LexAll(source, out DiagnosticBag diagnostics);

        Assert.False(diagnostics.HasErrors);

        TokenType[] expectedTokenTypes = new[]
        {
            TokenType.TypeKeyword, // type
            TokenType.Whitespace,  // " "
            TokenType.Identifier,  // Core
            TokenType.Whitespace,  // " "
            TokenType.LBrace,      // {
            TokenType.Whitespace,  // "\n  "
            TokenType.Identifier,  // val
            TokenType.Colon,       // :
            TokenType.Whitespace,  // " "
            TokenType.Identifier,  // Type
            TokenType.Semicolon,   // ;
            TokenType.Whitespace,  // "\n"
            TokenType.RBrace,      // }
            TokenType.Eof
        };

        TokenType[] actualTokenTypes = tokens.Select(t => t.Type).ToArray();
        Assert.Equal(expectedTokenTypes, actualTokenTypes);

        string[] expectedValues = new[]
        {
            "type",
            " ",
            "Core",
            " ",
            "{",
            "\n  ",
            "val",
            ":",
            " ",
            "Type",
            ";",
            "\n",
            "}",
            ""
        };

        string[] actualValues = tokens.Select(t => t.Value).ToArray();
        Assert.Equal(expectedValues, actualValues);
    }

    [Fact]
    public void NextToken_WithGenericAnglesAndAmpersand_LexesCorrectSymbols()
    {
        const string source = "Type<T & U>";
        List<Token> tokens = LexAll(source, out DiagnosticBag diagnostics);

        Assert.False(diagnostics.HasErrors);

        (TokenType Type, string Value)[] expectedTokens = new (TokenType Type, string Value)[]
        {
            (TokenType.Identifier, "Type"),
            (TokenType.LAngle, "<"),
            (TokenType.Identifier, "T"),
            (TokenType.Whitespace, " "),
            (TokenType.Ampersand, "&"),
            (TokenType.Whitespace, " "),
            (TokenType.Identifier, "U"),
            (TokenType.RAngle, ">"),
            (TokenType.Eof, "")
        };

        Assert.Equal(expectedTokens.Length, tokens.Count);
        for (int i = 0; i < expectedTokens.Length; i++)
        {
            Assert.Equal(expectedTokens[i].Type, tokens[i].Type);
            Assert.Equal(expectedTokens[i].Value, tokens[i].Value);
        }
    }

    [Fact]
    public void NextToken_WithGenericParametersAndComma_LexesCorrectTokens()
    {
        const string source = "Type<T, U>";
        List<Token> tokens = LexAll(source, out DiagnosticBag diagnostics);

        Assert.False(diagnostics.HasErrors);

        (TokenType Type, string Value)[] expectedTokens = new (TokenType Type, string Value)[]
        {
            (TokenType.Identifier, "Type"),
            (TokenType.LAngle, "<"),
            (TokenType.Identifier, "T"),
            (TokenType.Comma, ","),
            (TokenType.Whitespace, " "),
            (TokenType.Identifier, "U"),
            (TokenType.RAngle, ">"),
            (TokenType.Eof, "")
        };

        Assert.Equal(expectedTokens.Length, tokens.Count);
        for (int i = 0; i < expectedTokens.Length; i++)
        {
            Assert.Equal(expectedTokens[i].Type, tokens[i].Type);
            Assert.Equal(expectedTokens[i].Value, tokens[i].Value);
        }
    }
}
