using PDML.Compiler.Common;
using PDML.Compiler.Diagnostics;
using PDML.Compiler.Lexing;
using PDML.Compiler.Parsing;
using PDML.Compiler.Parsing.Nodes;
using PDML.Compiler.Parsing.Nodes.Members;

namespace PDML.Compiler.Tests.Parsing;

public sealed class ParserTests
{
    private static (Module Module, DiagnosticBag Diagnostics) Parse(string content)
    {
        DiagnosticBag diagnostics = new DiagnosticBag();
        SourceFile sourceFile = SourceFile.Create(".", "test.pdml", content);
        Lexer lexer = new Lexer(diagnostics, sourceFile);
        List<Token> tokens = Lexer.TokenizeAll(diagnostics, sourceFile);

        Parser parser = new Parser(diagnostics, tokens);
        Module module = parser.ParseModule();
        return (module, diagnostics);
    }

    [Fact]
    public void ParseModule_WithEmptySource_ReturnsEmptyModule()
    {
        (Module module, DiagnosticBag diagnostics) = Parse("");

        Assert.False(diagnostics.HasErrors);
        Assert.Empty(module.Members);
        Assert.Equal(TokenType.Eof, module.Eof.Type);
        Assert.Equal(0, module.Location.Start.Position);
        Assert.Equal(0, module.Location.End.Position);
    }

    [Fact]
    public void ParseModule_WithTypeAliasToTarget_ParsesTypeDefinitionMember()
    {
        const string source = "type MyType = Target;";
        (Module module, DiagnosticBag diagnostics) = Parse(source);

        Assert.False(diagnostics.HasErrors);
        TypeDefinitionMember member = Assert.IsType<TypeDefinitionMember>(Assert.Single(module.Members));

        Assert.Equal(TokenType.TypeKeyword, member.TypeKeyword.Type);
        Assert.Equal("type", member.TypeKeyword.Value);

        Assert.Equal(TokenType.Identifier, member.Name.Type);
        Assert.Equal("MyType", member.Name.Value);

        Assert.Null(member.Colon);
        Assert.Null(member.InheritingType);

        Assert.Equal(TokenType.Equals, member.Equal.Type);
        Assert.Equal("=", member.Equal.Value);

        Assert.Equal(1, member.Items.Count);
        TypeDefinitionItemTarget targetItem = Assert.IsType<TypeDefinitionItemTarget>(member.Items.First());
        Assert.Equal("Target", targetItem.Type.Name.Value);
        Assert.Null(targetItem.Type.Generics);

        Assert.Equal(TokenType.Semicolon, member.Semicolon.Type);
        Assert.Equal(";", member.Semicolon.Value);
        Assert.Equal(TokenType.Eof, module.Eof.Type);
    }

    [Fact]
    public void ParseModule_WithTypeInheritance_ParsesColonAndInheritingType()
    {
        const string source = "type Derived : Base = Target;";
        (Module module, DiagnosticBag diagnostics) = Parse(source);

        Assert.False(diagnostics.HasErrors);
        TypeDefinitionMember member = Assert.IsType<TypeDefinitionMember>(Assert.Single(module.Members));

        Assert.Equal("Derived", member.Name.Value);
        Assert.NotNull(member.Colon);
        Assert.Equal(TokenType.Colon, member.Colon.Type);
        Assert.NotNull(member.InheritingType);
        Assert.Equal("Base", member.InheritingType.Name.Value);
        Assert.Null(member.InheritingType.Generics);

        Assert.Equal(1, member.Items.Count);
        TypeDefinitionItemTarget targetItem = Assert.IsType<TypeDefinitionItemTarget>(member.Items.First());
        Assert.Equal("Target", targetItem.Type.Name.Value);
    }

    [Fact]
    public void ParseModule_WithGenericInheritingType_ParsesGenericTypeSignature()
    {
        const string source = "type Child : Parent<T, U> = Target;";
        (Module module, DiagnosticBag diagnostics) = Parse(source);

        Assert.False(diagnostics.HasErrors);
        TypeDefinitionMember member = Assert.IsType<TypeDefinitionMember>(Assert.Single(module.Members));

        Assert.NotNull(member.InheritingType);
        TypeSignature parentType = member.InheritingType;
        Assert.Equal("Parent", parentType.Name.Value);
        Assert.NotNull(parentType.LAngle);
        Assert.Equal("<", parentType.LAngle.Value);
        Assert.NotNull(parentType.RAngle);
        Assert.Equal(">", parentType.RAngle.Value);

        Assert.NotNull(parentType.Generics);
        Assert.Equal(2, parentType.Generics.Count);

        List<TypeSignature> typeArgs = parentType.Generics.ToList();
        Assert.Equal(2, typeArgs.Count);
        Assert.Equal("T", typeArgs[0].Name.Value);
        Assert.Equal("U", typeArgs[1].Name.Value);
    }

    [Fact]
    public void ParseModule_WithTypeDefinitionBody_ParsesPropertiesAndTypeClauses()
    {
        const string source = """
            type User = {
                id: Guid;
                name: String;
            };
            """;
        (Module module, DiagnosticBag diagnostics) = Parse(source);

        Assert.False(diagnostics.HasErrors);
        TypeDefinitionMember member = Assert.IsType<TypeDefinitionMember>(Assert.Single(module.Members));

        Assert.Equal("User", member.Name.Value);
        Assert.Equal(1, member.Items.Count);

        TypeDefinitionItemBody body = Assert.IsType<TypeDefinitionItemBody>(member.Items.First());
        Assert.Equal(TokenType.LBrace, body.LBrace.Type);
        Assert.Equal(TokenType.RBrace, body.RBrace.Type);
        Assert.Equal(2, body.Properties.Count);

        TypeDefinitionBodyProperty prop0 = body.Properties[0];
        Assert.Equal("id", prop0.Name.Value);
        Assert.Equal(TokenType.Colon, prop0.TypeClause.Colon.Type);
        Assert.Equal("Guid", prop0.TypeClause.TypeSignature.Name.Value);
        Assert.Equal(TokenType.Semicolon, prop0.Semicolon.Type);

        TypeDefinitionBodyProperty prop1 = body.Properties[1];
        Assert.Equal("name", prop1.Name.Value);
        Assert.Equal(TokenType.Colon, prop1.TypeClause.Colon.Type);
        Assert.Equal("String", prop1.TypeClause.TypeSignature.Name.Value);
        Assert.Equal(TokenType.Semicolon, prop1.Semicolon.Type);
    }

    [Fact]
    public void ParseModule_WithEmptyTypeDefinitionBody_ParsesEmptyProperties()
    {
        const string source = "type Empty = {};";
        (Module module, DiagnosticBag diagnostics) = Parse(source);

        Assert.False(diagnostics.HasErrors);
        TypeDefinitionMember member = Assert.IsType<TypeDefinitionMember>(Assert.Single(module.Members));

        TypeDefinitionItemBody body = Assert.IsType<TypeDefinitionItemBody>(member.Items.First());
        Assert.Empty(body.Properties);
        Assert.Equal("{", body.LBrace.Value);
        Assert.Equal("}", body.RBrace.Value);
    }

    [Fact]
    public void ParseModule_WithCompositionOfTargetAndBody_ParsesAllItemsAndAmpersands()
    {
        const string source = "type Entity = BaseType & { id: int64; } & Timestamped;";
        (Module module, DiagnosticBag diagnostics) = Parse(source);

        Assert.False(diagnostics.HasErrors);
        TypeDefinitionMember member = Assert.IsType<TypeDefinitionMember>(Assert.Single(module.Members));

        Assert.Equal(3, member.Items.Count);
        Assert.Equal(5, member.Items.TotalCount);

        List<TypeDefinitionItem> items = member.Items.ToList();
        TypeDefinitionItemTarget target0 = Assert.IsType<TypeDefinitionItemTarget>(items[0]);
        Assert.Equal("BaseType", target0.Type.Name.Value);

        TypeDefinitionItemBody body = Assert.IsType<TypeDefinitionItemBody>(items[1]);
        Assert.Single(body.Properties);
        Assert.Equal("id", body.Properties[0].Name.Value);
        Assert.Equal("int64", body.Properties[0].TypeClause.TypeSignature.Name.Value);

        TypeDefinitionItemTarget target1 = Assert.IsType<TypeDefinitionItemTarget>(items[2]);
        Assert.Equal("Timestamped", target1.Type.Name.Value);
    }

    [Fact]
    public void ParseModule_WithNestedGenericPropertyType_ParsesHierarchy()
    {
        const string source = "type Container = { data: Map<String, List<Result<int32, Error>>>; };";
        (Module module, DiagnosticBag diagnostics) = Parse(source);

        Assert.False(diagnostics.HasErrors);
        TypeDefinitionMember member = Assert.IsType<TypeDefinitionMember>(Assert.Single(module.Members));
        TypeDefinitionItemBody body = Assert.IsType<TypeDefinitionItemBody>(member.Items.First());
        TypeDefinitionBodyProperty prop = Assert.Single(body.Properties);

        TypeSignature mapType = prop.TypeClause.TypeSignature;
        Assert.Equal("Map", mapType.Name.Value);
        Assert.NotNull(mapType.Generics);
        Assert.Equal(2, mapType.Generics.Count);

        List<TypeSignature> mapArgs = mapType.Generics.ToList();
        Assert.Equal("String", mapArgs[0].Name.Value);
        Assert.Null(mapArgs[0].Generics);

        TypeSignature listType = mapArgs[1];
        Assert.Equal("List", listType.Name.Value);
        Assert.NotNull(listType.Generics);
        Assert.Equal(1, listType.Generics.Count);

        TypeSignature resultType = Assert.Single(listType.Generics);
        Assert.Equal("Result", resultType.Name.Value);
        Assert.NotNull(resultType.Generics);
        Assert.Equal(2, resultType.Generics.Count);

        List<TypeSignature> resultArgs = resultType.Generics.ToList();
        Assert.Equal("int32", resultArgs[0].Name.Value);
        Assert.Equal("Error", resultArgs[1].Name.Value);
    }

    [Fact]
    public void ParseModule_WithMultipleTypeDefinitions_ParsesAllMembers()
    {
        const string source = """
            type First = { a: int32; };
            type Second : First = { b: String; };
            type Third = First & Second;
            """;
        (Module module, DiagnosticBag diagnostics) = Parse(source);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal(3, module.Members.Count);

        TypeDefinitionMember first = Assert.IsType<TypeDefinitionMember>(module.Members[0]);
        Assert.Equal("First", first.Name.Value);

        TypeDefinitionMember second = Assert.IsType<TypeDefinitionMember>(module.Members[1]);
        Assert.Equal("Second", second.Name.Value);
        Assert.Equal("First", second.InheritingType?.Name.Value);

        TypeDefinitionMember third = Assert.IsType<TypeDefinitionMember>(module.Members[2]);
        Assert.Equal("Third", third.Name.Value);
        Assert.Equal(2, third.Items.Count);
    }
}
