using PDML.Compiler.Common;
using PDML.Compiler.Diagnostics;
using PDML.Compiler.Lexing;

namespace PDML.Compiler.Tests.Lexing;

public sealed class LexerLocationTests
{
    private static List<Token> LexAll(string content, out DiagnosticBag diagnostics)
    {
        DiagnosticBag diag = new DiagnosticBag();
        SourceFile sourceFile = SourceFile.Create(".", "test.pdml", content);
        Lexer lexer = new Lexer(diag, sourceFile);
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
}
