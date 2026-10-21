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
        (Lexer lexer, DiagnosticBag diagnostics) = CreateLexer("@type");

        Token token = lexer.NextToken();
        Token eof = lexer.NextToken();

        Assert.Equal(TokenType.TypeKeyword, token.Type);
        Assert.Equal("@type", token.Value);
        Assert.Equal(0, token.Location.Start.Position);
        Assert.Equal(1, token.Location.Start.Line);
        Assert.Equal(1, token.Location.Start.Column);
        Assert.Equal(5, token.Location.End.Position);
        Assert.Equal(1, token.Location.End.Line);
        Assert.Equal(6, token.Location.End.Column);
        Assert.Equal(TokenType.Eof, eof.Type);
        Assert.False(diagnostics.HasErrors);
    }

    [Theory]
    [InlineData("@module")]
    [InlineData("@import")]
    [InlineData("@custom123")]
    [InlineData("@typedef")]
    public void NextToken_WithUnexpectedKeyword_ReturnsErrorTokenAndReportsDiagnostic(string keyword)
    {
        (Lexer lexer, DiagnosticBag diagnostics) = CreateLexer(keyword);

        Token token = lexer.NextToken();

        Assert.Equal(TokenType.Error, token.Type);
        Assert.Equal(keyword, token.Value);
        Assert.Equal(0, token.Location.Start.Position);
        Assert.Equal(keyword.Length, token.Location.End.Position);
        Assert.True(diagnostics.HasErrors);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Descriptor.Severity);
        Assert.Equal(CompilationStep.Lexing, diagnostic.Descriptor.Step);
        Assert.Equal(1001, diagnostic.Descriptor.Code);
        Assert.Contains(keyword, diagnostic.Message);
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

    [Theory]
    [InlineData("$")]
    [InlineData("#")]
    [InlineData("!")]
    [InlineData("?")]
    [InlineData("+")]
    [InlineData("1")]
    [InlineData("@")]
    public void NextToken_WithUnexpectedCharacter_ReturnsErrorTokenAndReportsDiagnostic(string unexpected)
    {
        (Lexer lexer, DiagnosticBag diagnostics) = CreateLexer(unexpected);

        Token token = lexer.NextToken();

        Assert.Equal(TokenType.Error, token.Type);
        Assert.Equal(unexpected, token.Value);
        Assert.True(diagnostics.HasErrors);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Descriptor.Severity);
        Assert.Equal(CompilationStep.Lexing, diagnostic.Descriptor.Step);
        Assert.Equal(1000, diagnostic.Descriptor.Code);
        Assert.Contains(unexpected, diagnostic.Message);
    }

    [Fact]
    public void NextToken_TracksCoordinatesAndLocationsAccurately_AcrossNewlines()
    {
        const string source = "first\nsecond\n  third";
        List<Token> tokens = LexAll(source, out DiagnosticBag diagnostics);

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
        const string source = "@type Core {\n  val: Type;\n}";
        List<Token> tokens = LexAll(source, out DiagnosticBag diagnostics);

        Assert.False(diagnostics.HasErrors);

        TokenType[] expectedTokenTypes = new[]
        {
            TokenType.TypeKeyword, // @type
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
            "@type",
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
