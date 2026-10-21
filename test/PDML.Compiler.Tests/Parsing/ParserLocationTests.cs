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
        const string source = "@type User = {\n  id: Guid;\n};";
        (Module module, DiagnosticBag diagnostics) = Parse(source);

        Assert.False(diagnostics.HasErrors);
        TypeDefinitionMember member = Assert.IsType<TypeDefinitionMember>(Assert.Single(module.Members));

        // Module location: 0 (Line 1, Col 1) to 29 (Line 3, Col 3)
        Assert.Equal(new Coordinate { Position = 0, Line = 1, Column = 1 }, module.Location.Start);
        Assert.Equal(new Coordinate { Position = 29, Line = 3, Column = 3 }, module.Location.End);

        // Member location: from "@type" (0) to ";" (29)
        Assert.Equal(new Coordinate { Position = 0, Line = 1, Column = 1 }, member.Location.Start);
        Assert.Equal(new Coordinate { Position = 29, Line = 3, Column = 3 }, member.Location.End);

        // Body location: from "{" (13) to "}" (28)
        TypeDefinitionItemBody body = Assert.IsType<TypeDefinitionItemBody>(member.Items.First());
        Assert.Equal(new Coordinate { Position = 13, Line = 1, Column = 14 }, body.Location.Start);
        Assert.Equal(new Coordinate { Position = 28, Line = 3, Column = 2 }, body.Location.End);

        // Property location: from "id" (17) to ";" (26)
        TypeDefinitionBodyProperty prop = Assert.Single(body.Properties);
        Assert.Equal(new Coordinate { Position = 17, Line = 2, Column = 3 }, prop.Location.Start);
        Assert.Equal(new Coordinate { Position = 26, Line = 2, Column = 12 }, prop.Location.End);

        // Type clause location: from ":" (19) to "Guid" (25)
        Assert.Equal(new Coordinate { Position = 19, Line = 2, Column = 5 }, prop.TypeClause.Location.Start);
        Assert.Equal(new Coordinate { Position = 25, Line = 2, Column = 11 }, prop.TypeClause.Location.End);

        // Type signature location: "Guid" (21..25)
        Assert.Equal(new Coordinate { Position = 21, Line = 2, Column = 7 }, prop.TypeClause.TypeSignature.Location.Start);
        Assert.Equal(new Coordinate { Position = 25, Line = 2, Column = 11 }, prop.TypeClause.TypeSignature.Location.End);
    }

    [Fact]
    public void ParseModule_WithGenericSignatureLocation_SpansFromNameThroughClosingAngle()
    {
        const string source = "@type Generic = Wrapper<T, U>;";
        (Module module, DiagnosticBag diagnostics) = Parse(source);

        Assert.False(diagnostics.HasErrors);
        TypeDefinitionMember member = Assert.IsType<TypeDefinitionMember>(Assert.Single(module.Members));
        TypeDefinitionItemTarget target = Assert.IsType<TypeDefinitionItemTarget>(member.Items.First());

        // "Wrapper<T, U>" starts at "Wrapper" (16) and ends after ">" (29)
        Assert.Equal(new Coordinate { Position = 16, Line = 1, Column = 17 }, target.Type.Location.Start);
        Assert.Equal(new Coordinate { Position = 29, Line = 1, Column = 30 }, target.Type.Location.End);
    }
}
