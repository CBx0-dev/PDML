using PDML.Compiler.Common;
using PDML.Compiler.Diagnostics;
using PDML.Compiler.Lexing;
using PDML.Compiler.Parsing;
using PDML.Compiler.Parsing.Nodes;
using PDML.Compiler.Parsing.Nodes.Members;

namespace PDML.Compiler.Tests.Parsing;

public sealed class ParserLocationTests
{
    private static (Module Module, DiagnosticBag Diagnostics) Parse(string content)
    {
        DiagnosticBag diagnostics = new DiagnosticBag();
        SourceFile sourceFile = SourceFile.Create(".", "test.pdml", content);
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

        Parser parser = new Parser(diagnostics, tokens);
        Module module = parser.ParseModule();
        return (module, diagnostics);
    }

    [Fact]
    public void ParseModule_TracksAstNodeLocationsAccurately()
    {
        const string source = "type User = {\n  id: Guid;\n};";
        (Module module, DiagnosticBag diagnostics) = Parse(source);

        Assert.False(diagnostics.HasErrors);
        TypeDefinitionMember member = Assert.IsType<TypeDefinitionMember>(Assert.Single(module.Members));

        Assert.Equal(new Coordinate { Position = 0, Line = 1, Column = 1 }, module.Location.Start);
        Assert.Equal(new Coordinate { Position = 28, Line = 3, Column = 3 }, module.Location.End);

        Assert.Equal(new Coordinate { Position = 0, Line = 1, Column = 1 }, member.Location.Start);
        Assert.Equal(new Coordinate { Position = 28, Line = 3, Column = 3 }, member.Location.End);

        TypeDefinitionItemBody body = Assert.IsType<TypeDefinitionItemBody>(member.Items.First());
        Assert.Equal(new Coordinate { Position = 12, Line = 1, Column = 13 }, body.Location.Start);
        Assert.Equal(new Coordinate { Position = 27, Line = 3, Column = 2 }, body.Location.End);

        TypeDefinitionBodyProperty prop = Assert.Single(body.Properties);
        Assert.Equal(new Coordinate { Position = 16, Line = 2, Column = 3 }, prop.Location.Start);
        Assert.Equal(new Coordinate { Position = 25, Line = 2, Column = 12 }, prop.Location.End);

        Assert.Equal(new Coordinate { Position = 18, Line = 2, Column = 5 }, prop.TypeClause.Location.Start);
        Assert.Equal(new Coordinate { Position = 24, Line = 2, Column = 11 }, prop.TypeClause.Location.End);

        Assert.Equal(new Coordinate { Position = 20, Line = 2, Column = 7 }, prop.TypeClause.TypeSignature.Location.Start);
        Assert.Equal(new Coordinate { Position = 24, Line = 2, Column = 11 }, prop.TypeClause.TypeSignature.Location.End);
    }

    [Fact]
    public void ParseModule_WithGenericSignatureLocation_SpansFromNameThroughClosingAngle()
    {
        const string source = "type Generic = Wrapper<T, U>;";
        (Module module, DiagnosticBag diagnostics) = Parse(source);

        Assert.False(diagnostics.HasErrors);
        TypeDefinitionMember member = Assert.IsType<TypeDefinitionMember>(Assert.Single(module.Members));
        TypeDefinitionItemTarget target = Assert.IsType<TypeDefinitionItemTarget>(member.Items.First());

        Assert.Equal(new Coordinate { Position = 15, Line = 1, Column = 16 }, target.Type.Location.Start);
        Assert.Equal(new Coordinate { Position = 28, Line = 1, Column = 29 }, target.Type.Location.End);
    }
}
