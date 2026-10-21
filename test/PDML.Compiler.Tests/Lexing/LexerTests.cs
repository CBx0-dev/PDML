using PDML.Compiler.Common;
using PDML.Compiler.Diagnostics;
using PDML.Compiler.Lexing;

namespace PDML.Compiler.Tests.Lexing;

public sealed class LexerTests
{
    private static (Lexer Lexer, DiagnosticBag Diagnostics) CreateLexer(string content)
    {
        var diagnostics = new DiagnosticBag();
        var sourceFile = SourceFile.Create(".", "test.pdml", content);
        var lexer = new Lexer(diagnostics, sourceFile);
        return (lexer, diagnostics);
    }

    private static List<Token> LexAll(string content, out DiagnosticBag diagnostics)
    {
        var (lexer, diag) = CreateLexer(content);
        diagnostics = diag;

        var tokens = new List<Token>();
        while (true)
        {
            var token = lexer.NextToken();
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
        var (lexer, diagnostics) = CreateLexer("");

        var token = lexer.NextToken();

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
        var (lexer, _) = CreateLexer("");

        var first = lexer.NextToken();
        var second = lexer.NextToken();
        var third = lexer.NextToken();

        Assert.Equal(TokenType.Eof, first.Type);
        Assert.Equal(TokenType.Eof, second.Type);
        Assert.Equal(TokenType.Eof, third.Type);
    }

    [Theory]
    [InlineData("&", TokenType.Ampersand)]
    [InlineData(":", TokenType.Colon)]
    [InlineData(";", TokenType.Semicolon)]
    [InlineData("{", TokenType.LBrace)]
    [InlineData("}", TokenType.RBrace)]
    [InlineData("<", TokenType.LAngle)]
    [InlineData(">", TokenType.RAngle)]
    public void NextToken_WithSingleCharacterSymbol_ReturnsExpectedTokenTypeAndValue(string symbol, TokenType expectedType)
    {
        var (lexer, diagnostics) = CreateLexer(symbol);

        var token = lexer.NextToken();
        var eof = lexer.NextToken();

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
        var (lexer, diagnostics) = CreateLexer(identifier);

        var token = lexer.NextToken();
        var eof = lexer.NextToken();

        Assert.Equal(TokenType.Identifier, token.Type);
        Assert.Equal(identifier, token.Value);
        Assert.Equal(0, token.Location.Start.Position);
        Assert.Equal(identifier.Length, token.Location.End.Position);
        Assert.Equal(TokenType.Eof, eof.Type);
        Assert.False(diagnostics.HasErrors);
    }

    [Theory]
    [InlineData("@module")]
    [InlineData("@import")]
    [InlineData("@type")]
    [InlineData("@custom123")]
    [InlineData("@")]
    public void NextToken_WithKeyword_ReturnsKeywordToken(string keyword)
    {
        var (lexer, diagnostics) = CreateLexer(keyword);

        var token = lexer.NextToken();
        var eof = lexer.NextToken();

        Assert.Equal(TokenType.Keyword, token.Type);
        Assert.Equal(keyword, token.Value);
        Assert.Equal(0, token.Location.Start.Position);
        Assert.Equal(keyword.Length, token.Location.End.Position);
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
        var (lexer, diagnostics) = CreateLexer(whitespace);

        var token = lexer.NextToken();
        var eof = lexer.NextToken();

        Assert.Equal(TokenType.Whitespace, token.Type);
        Assert.Equal(whitespace, token.Value);
        Assert.Equal(0, token.Location.Start.Position);
        Assert.Equal(whitespace.Length, token.Location.End.Position);
        Assert.Equal(TokenType.Eof, eof.Type);
        Assert.False(diagnostics.HasErrors);
    }

    [Theory]
    [InlineData("$")]
    [InlineData("#")]
    [InlineData("!")]
    [InlineData("?")]
    [InlineData("+")]
    [InlineData("1")]
    public void NextToken_WithUnexpectedCharacter_ReturnsErrorTokenAndReportsDiagnostic(string unexpected)
    {
        var (lexer, diagnostics) = CreateLexer(unexpected);

        var token = lexer.NextToken();

        Assert.Equal(TokenType.Error, token.Type);
        Assert.Equal(unexpected, token.Value);
        Assert.True(diagnostics.HasErrors);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Descriptor.Severity);
        Assert.Equal(CompilationStep.Lexing, diagnostic.Descriptor.Step);
        Assert.Equal(1000, diagnostic.Descriptor.Code);
        Assert.Contains(unexpected, diagnostic.Message);
    }

    [Fact]
    public void NextToken_TracksCoordinatesAndLocationsAccurately_AcrossNewlines()
    {
        const string source = "first\nsecond\n  third";
        var tokens = LexAll(source, out var diagnostics);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal(6, tokens.Count);

        // "first" -> Line 1, Col 1..6
        Assert.Equal(TokenType.Identifier, tokens[0].Type);
        Assert.Equal("first", tokens[0].Value);
        Assert.Equal(new Coordinate { Position = 0, Line = 1, Column = 1 }, tokens[0].Location.Start);
        Assert.Equal(new Coordinate { Position = 5, Line = 1, Column = 6 }, tokens[0].Location.End);

        // "\n" -> Line 1, Col 6 to Line 2, Col 1
        Assert.Equal(TokenType.Whitespace, tokens[1].Type);
        Assert.Equal("\n", tokens[1].Value);
        Assert.Equal(new Coordinate { Position = 5, Line = 1, Column = 6 }, tokens[1].Location.Start);
        Assert.Equal(new Coordinate { Position = 6, Line = 2, Column = 1 }, tokens[1].Location.End);

        // "second" -> Line 2, Col 1..7
        Assert.Equal(TokenType.Identifier, tokens[2].Type);
        Assert.Equal("second", tokens[2].Value);
        Assert.Equal(new Coordinate { Position = 6, Line = 2, Column = 1 }, tokens[2].Location.Start);
        Assert.Equal(new Coordinate { Position = 12, Line = 2, Column = 7 }, tokens[2].Location.End);

        // "\n  " -> Line 2, Col 7 to Line 3, Col 3
        Assert.Equal(TokenType.Whitespace, tokens[3].Type);
        Assert.Equal("\n  ", tokens[3].Value);
        Assert.Equal(new Coordinate { Position = 12, Line = 2, Column = 7 }, tokens[3].Location.Start);
        Assert.Equal(new Coordinate { Position = 15, Line = 3, Column = 3 }, tokens[3].Location.End);

        // "third" -> Line 3, Col 3..8
        Assert.Equal(TokenType.Identifier, tokens[4].Type);
        Assert.Equal("third", tokens[4].Value);
        Assert.Equal(new Coordinate { Position = 15, Line = 3, Column = 3 }, tokens[4].Location.Start);
        Assert.Equal(new Coordinate { Position = 20, Line = 3, Column = 8 }, tokens[4].Location.End);

        // Eof
        Assert.Equal(TokenType.Eof, tokens[5].Type);
    }

    [Fact]
    public void NextToken_WithPdmlSourceSnippet_LexesAllTokensInExpectedOrder()
    {
        const string source = "@module Core {\n  val: Type;\n}";
        var tokens = LexAll(source, out var diagnostics);

        Assert.False(diagnostics.HasErrors);

        var expectedTokenTypes = new[]
        {
            TokenType.Keyword,    // @module
            TokenType.Whitespace, // " "
            TokenType.Identifier, // Core
            TokenType.Whitespace, // " "
            TokenType.LBrace,     // {
            TokenType.Whitespace, // "\n  "
            TokenType.Identifier, // val
            TokenType.Colon,      // :
            TokenType.Whitespace, // " "
            TokenType.Identifier, // Type
            TokenType.Semicolon,  // ;
            TokenType.Whitespace, // "\n"
            TokenType.RBrace,     // }
            TokenType.Eof
        };

        var actualTokenTypes = tokens.Select(t => t.Type).ToArray();
        Assert.Equal(expectedTokenTypes, actualTokenTypes);

        var expectedValues = new[]
        {
            "@module",
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

        var actualValues = tokens.Select(t => t.Value).ToArray();
        Assert.Equal(expectedValues, actualValues);
    }

    [Fact]
    public void NextToken_WithGenericAnglesAndAmpersand_LexesCorrectSymbols()
    {
        const string source = "Type<T & U>";
        var tokens = LexAll(source, out var diagnostics);

        Assert.False(diagnostics.HasErrors);

        var expectedTokens = new (TokenType Type, string Value)[]
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
}
