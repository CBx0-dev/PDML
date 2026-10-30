using PDML.Compiler.Common;
using PDML.Compiler.Diagnostics;
using PDML.Compiler.Lexing;
using PDML.Compiler.Parsing;
using PDML.Compiler.Parsing.Nodes;
using PDML.Compiler.Parsing.Nodes.Members;

namespace PDML.Compiler.Tests.Parsing;

public sealed class ParserDiagnosticTests
{
    private static (Module Module, DiagnosticBag Diagnostics) Parse(string content)
    {
        DiagnosticBag diagnostics = new DiagnosticBag();
        SourceFile sourceFile = SourceFile.Create(".", "test.pdml", content);
        List<Token> tokens = Lexer.TokenizeAll(diagnostics, sourceFile);

        Parser parser = new Parser(diagnostics, tokens);
        Module module = parser.ParseModule();
        return (module, diagnostics);
    }

    [Fact]
    public void ParseModule_WithUnexpectedTokenAtModuleLevel_ReportsDiagnosticAndSkips()
    {
        const string source = "invalidToken @type Valid = Target;";
        (Module module, DiagnosticBag diagnostics) = Parse(source);

        Assert.True(diagnostics.HasErrors);
        Diagnostic diag = Assert.Single(diagnostics, d => d.Descriptor.Code == (int)ParsingError.Code.UnexpectedToken);
        Assert.Equal(CompilationStep.Parsing, diag.Descriptor.Step);
        Assert.Equal(DiagnosticSeverity.Error, diag.Descriptor.Severity);

        TypeDefinitionMember member = Assert.IsType<TypeDefinitionMember>(Assert.Single(module.Members));
        Assert.Equal("Valid", member.Name.Value);
    }

    [Fact]
    public void ParseModule_WhenMissingIdentifierInTypeDefinition_ReportsExpectedTokenDiagnostic()
    {
        const string source = "@type = Target;";
        (Module module, DiagnosticBag diagnostics) = Parse(source);

        Assert.True(diagnostics.HasErrors);
        Assert.Contains(diagnostics, d => d.Descriptor.Code == (int)ParsingError.Code.ExpectedToken && d.Descriptor.Step == CompilationStep.Parsing);

        TypeDefinitionMember member = Assert.IsType<TypeDefinitionMember>(Assert.Single(module.Members));
        Assert.Equal(TokenType.Identifier, member.Name.Type);
    }

    [Fact]
    public void ParseModule_WhenMissingEqualsInTypeDefinition_ReportsExpectedTokenDiagnostic()
    {
        const string source = "@type MyType Target;";
        (Module _, DiagnosticBag diagnostics) = Parse(source);

        Assert.True(diagnostics.HasErrors);
        Assert.Contains(diagnostics, d => d.Descriptor.Code == (int)ParsingError.Code.ExpectedToken && d.Descriptor.Step == CompilationStep.Parsing);
    }

    [Fact]
    public void ParseModule_WhenMissingSemicolonAtEndOfMember_ReportsExpectedTokenDiagnostic()
    {
        const string source = "@type MyType = Target";
        (Module _, DiagnosticBag diagnostics) = Parse(source);

        Assert.True(diagnostics.HasErrors);
        Assert.Contains(diagnostics, d => d.Descriptor.Code == (int)ParsingError.Code.ExpectedToken && d.Descriptor.Step == CompilationStep.Parsing);
    }

    [Fact]
    public void ParseModule_WhenMissingColonInProperty_ReportsExpectedTokenDiagnostic()
    {
        const string source = "@type MyType = { prop int32; };";
        (Module _, DiagnosticBag diagnostics) = Parse(source);

        Assert.True(diagnostics.HasErrors);
        Assert.Contains(diagnostics, d => d.Descriptor.Code == (int)ParsingError.Code.ExpectedToken && d.Descriptor.Step == CompilationStep.Parsing);
    }

    [Fact]
    public void ParseModule_WhenMissingSemicolonInProperty_ReportsExpectedTokenDiagnostic()
    {
        const string source = "@type MyType = { prop: int32 };";
        (Module _, DiagnosticBag diagnostics) = Parse(source);

        Assert.True(diagnostics.HasErrors);
        Assert.Contains(diagnostics, d => d.Descriptor.Code == (int)ParsingError.Code.ExpectedToken && d.Descriptor.Step == CompilationStep.Parsing);
    }

    [Fact]
    public void ParseModule_WhenMissingClosingBraceInBody_ReportsExpectedTokenDiagnostic()
    {
        const string source = "@type MyType = { prop: int32; ;";
        (Module _, DiagnosticBag diagnostics) = Parse(source);

        Assert.True(diagnostics.HasErrors);
        Assert.Contains(diagnostics, d => d.Descriptor.Code == (int)ParsingError.Code.ExpectedToken && d.Descriptor.Step == CompilationStep.Parsing);
    }

    [Fact]
    public void ParseModule_WhenMissingClosingAngleInGenerics_ReportsExpectedTokenDiagnostic()
    {
        const string source = "@type MyType = List<T;";
        (Module _, DiagnosticBag diagnostics) = Parse(source);

        Assert.True(diagnostics.HasErrors);
        Assert.Contains(diagnostics, d => d.Descriptor.Code == (int)ParsingError.Code.ExpectedToken && d.Descriptor.Step == CompilationStep.Parsing);
    }

    [Fact]
    public void ParseModule_WhenMissingCommaInGenericArguments_ReportsExpectedTokenDiagnostic()
    {
        const string source = "@type MyType = Map<Key Value>;";
        (Module _, DiagnosticBag diagnostics) = Parse(source);

        Assert.True(diagnostics.HasErrors);
        Assert.Contains(diagnostics, d => d.Descriptor.Code == (int)ParsingError.Code.ExpectedToken && d.Descriptor.Step == CompilationStep.Parsing);
    }

    [Fact]
    public void ParseModule_WhenMissingAmpersandInComposition_ReportsExpectedTokenDiagnostic()
    {
        const string source = "@type MyType = A B;";
        (Module _, DiagnosticBag diagnostics) = Parse(source);

        Assert.True(diagnostics.HasErrors);
        Assert.Contains(diagnostics, d => d.Descriptor.Code == (int)ParsingError.Code.ExpectedToken && d.Descriptor.Step == CompilationStep.Parsing);
    }
}
