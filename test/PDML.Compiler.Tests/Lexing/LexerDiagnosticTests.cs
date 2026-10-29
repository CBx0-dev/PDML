using PDML.Compiler.Common;
using PDML.Compiler.Diagnostics;
using PDML.Compiler.Lexing;

namespace PDML.Compiler.Tests.Lexing;

public sealed class LexerDiagnosticTests
{
    private static (Lexer Lexer, DiagnosticBag Diagnostics) CreateLexer(string content)
    {
        DiagnosticBag diagnostics = new DiagnosticBag();
        SourceFile sourceFile = SourceFile.Create(".", "test.pdml", content);
        Lexer lexer = new Lexer(diagnostics, sourceFile);
        return (lexer, diagnostics);
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
        Assert.Equal((int)LexingError.Code.UnexpectedCharacter, diagnostic.Descriptor.Code);
    }
}
