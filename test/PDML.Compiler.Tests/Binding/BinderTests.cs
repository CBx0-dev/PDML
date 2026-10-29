using PDML.Compiler.Binding;
using PDML.Compiler.Binding.Nodes;
using PDML.Compiler.Binding.Nodes.Members;
using PDML.Compiler.Binding.Types;
using PDML.Compiler.Common;
using PDML.Compiler.Diagnostics;
using PDML.Compiler.Lexing;
using PDML.Compiler.Parsing;
using PDML.Compiler.Parsing.Nodes;

namespace PDML.Compiler.Tests.Binding;

public sealed class BinderTests
{
    private static (BoundModule BoundModule, DiagnosticBag Diagnostics) Bind(string content)
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
        BoundModule boundModule = Binder.BindModule(diagnostics, module);
        return (boundModule, diagnostics);
    }

    [Fact]
    public void BindModule_WithEmptyModule_ProducesEmptyBoundModule()
    {
        const string source = "";
        (BoundModule boundModule, DiagnosticBag diagnostics) = Bind(source);

        Assert.False(diagnostics.HasErrors);
        Assert.Empty(boundModule.Types);
        Assert.NotNull(boundModule.Location);
    }

    [Fact]
    public void BindModule_WithSingleEmptyTypeDefinition_BindsSuccessfully()
    {
        const string source = "type User = {};";
        (BoundModule boundModule, DiagnosticBag diagnostics) = Bind(source);

        Assert.False(diagnostics.HasErrors);
        BoundTypeDefinition type = Assert.Single(boundModule.Types);
        Assert.Equal("User", type.Name);
        Assert.Null(type.InheritingType);
        Assert.Empty(type.Properties);
        Assert.NotNull(type.Location);
    }

    [Fact]
    public void BindModule_WithAllIntrinsicTypes_BindsPropertiesWithCorrectIntrinsicTypeRefs()
    {
        const string source = """
                              type AllIntrinsics = {
                                  u8: uint8;
                                  u16: uint16;
                                  u32: uint32;
                                  u64: uint64;
                                  i8: int8;
                                  i16: int16;
                                  i32: int32;
                                  i64: int64;
                                  c: char;
                                  b: bool;
                                  s: string;
                              };
                              """;
        (BoundModule boundModule, DiagnosticBag diagnostics) = Bind(source);

        Assert.False(diagnostics.HasErrors);
        BoundTypeDefinition type = Assert.Single(boundModule.Types);
        Assert.Equal(11, type.Properties.Count);

        Assert.Equal(IntrinsicTypeRef.Uint8, type.Properties["u8"].Type);
        Assert.Equal(IntrinsicType.Uint8, ((IntrinsicTypeRef)type.Properties["u8"].Type).Type);

        Assert.Equal(IntrinsicTypeRef.Uint16, type.Properties["u16"].Type);
        Assert.Equal(IntrinsicType.Uint16, ((IntrinsicTypeRef)type.Properties["u16"].Type).Type);

        Assert.Equal(IntrinsicTypeRef.Uint32, type.Properties["u32"].Type);
        Assert.Equal(IntrinsicType.Uint32, ((IntrinsicTypeRef)type.Properties["u32"].Type).Type);

        Assert.Equal(IntrinsicTypeRef.Uint64, type.Properties["u64"].Type);
        Assert.Equal(IntrinsicType.Uint64, ((IntrinsicTypeRef)type.Properties["u64"].Type).Type);

        Assert.Equal(IntrinsicTypeRef.Int8, type.Properties["i8"].Type);
        Assert.Equal(IntrinsicType.Int8, ((IntrinsicTypeRef)type.Properties["i8"].Type).Type);

        Assert.Equal(IntrinsicTypeRef.Int16, type.Properties["i16"].Type);
        Assert.Equal(IntrinsicType.Int16, ((IntrinsicTypeRef)type.Properties["i16"].Type).Type);

        Assert.Equal(IntrinsicTypeRef.Int32, type.Properties["i32"].Type);
        Assert.Equal(IntrinsicType.Int32, ((IntrinsicTypeRef)type.Properties["i32"].Type).Type);

        Assert.Equal(IntrinsicTypeRef.Int64, type.Properties["i64"].Type);
        Assert.Equal(IntrinsicType.Int64, ((IntrinsicTypeRef)type.Properties["i64"].Type).Type);

        Assert.Equal(IntrinsicTypeRef.Char, type.Properties["c"].Type);
        Assert.Equal(IntrinsicType.Char, ((IntrinsicTypeRef)type.Properties["c"].Type).Type);

        Assert.Equal(IntrinsicTypeRef.Bool, type.Properties["b"].Type);
        Assert.Equal(IntrinsicType.Bool, ((IntrinsicTypeRef)type.Properties["b"].Type).Type);

        Assert.Equal(IntrinsicTypeRef.String, type.Properties["s"].Type);
        Assert.Equal(IntrinsicType.String, ((IntrinsicTypeRef)type.Properties["s"].Type).Type);
    }

    [Fact]
    public void BindModule_WithCustomTypeReferenceProperty_BindsTypeDefinitionTypeRef()
    {
        const string source = """
                              type Role = { name: string; };
                              type User = { role: Role; };
                              """;
        (BoundModule boundModule, DiagnosticBag diagnostics) = Bind(source);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal(2, boundModule.Types.Count);

        BoundTypeDefinition roleType = boundModule.Types[0];
        BoundTypeDefinition userType = boundModule.Types[1];

        Assert.Single(userType.Properties);
        BoundTypeDefinitionProperty roleProp = userType.Properties["role"];
        TypeDefinitionTypeRef typeRef = Assert.IsType<TypeDefinitionTypeRef>(roleProp.Type);
        Assert.Same(roleType, typeRef.Definition);
    }

    [Fact]
    public void BindModule_WithForwardTypeReferenceProperty_BindsTypeDefinitionTypeRef()
    {
        const string source = """
                              type User = { role: Role; };
                              type Role = { name: string; };
                              """;
        (BoundModule boundModule, DiagnosticBag diagnostics) = Bind(source);

        Assert.False(diagnostics.HasErrors);
        BoundTypeDefinition userType = boundModule.Types[0];
        BoundTypeDefinition roleType = boundModule.Types[1];

        BoundTypeDefinitionProperty roleProp = userType.Properties["role"];
        TypeDefinitionTypeRef typeRef = Assert.IsType<TypeDefinitionTypeRef>(roleProp.Type);
        Assert.Same(roleType, typeRef.Definition);
    }

    [Fact]
    public void BindModule_WithInheritance_BindsInheritingType()
    {
        const string source = """
                              type Animal = { age: int32; };
                              type Dog : Animal = { barkSound: string; };
                              """;
        (BoundModule boundModule, DiagnosticBag diagnostics) = Bind(source);

        Assert.False(diagnostics.HasErrors);
        BoundTypeDefinition animalType = boundModule.Types[0];
        BoundTypeDefinition dogType = boundModule.Types[1];

        Assert.NotNull(dogType.InheritingType);
        Assert.Same(animalType, dogType.InheritingType.Definition);
        Assert.Single(dogType.Properties);
        Assert.True(dogType.Properties.ContainsKey("barkSound"));
    }

    [Fact]
    public void BindModule_WithMultiLevelInheritance_BindsInheritanceChain()
    {
        const string source = """
                              type Base = { id: int64; };
                              type Middle : Base = { middleProp: string; };
                              type Leaf : Middle = { leafProp: bool; };
                              """;
        (BoundModule boundModule, DiagnosticBag diagnostics) = Bind(source);

        Assert.False(diagnostics.HasErrors);
        BoundTypeDefinition baseType = boundModule.Types[0];
        BoundTypeDefinition middleType = boundModule.Types[1];
        BoundTypeDefinition leafType = boundModule.Types[2];

        Assert.Null(baseType.InheritingType);
        Assert.NotNull(middleType.InheritingType);
        Assert.Same(baseType, middleType.InheritingType.Definition);
        Assert.NotNull(leafType.InheritingType);
        Assert.Same(middleType, leafType.InheritingType.Definition);
    }

    [Fact]
    public void BindModule_WithComposition_CopiesPropertiesFromTargetTypes()
    {
        const string source = """
                              type Identifiable = { id: int64; };
                              type Timestamped = { createdAt: int64; };
                              type Entity = Identifiable & Timestamped & { name: string; };
                              """;
        (BoundModule boundModule, DiagnosticBag diagnostics) = Bind(source);

        Assert.False(diagnostics.HasErrors);
        BoundTypeDefinition entityType = boundModule.Types[2];

        Assert.Equal(3, entityType.Properties.Count);
        Assert.Equal(IntrinsicTypeRef.Int64, entityType.Properties["id"].Type);
        Assert.Equal(IntrinsicTypeRef.Int64, entityType.Properties["createdAt"].Type);
        Assert.Equal(IntrinsicTypeRef.String, entityType.Properties["name"].Type);
    }

    [Fact]
    public void BindModule_WithMultipleBodyCompositions_MergesAllProperties()
    {
        const string source = "type Combined = { a: int32; } & { b: string; } & { c: bool; };";
        (BoundModule boundModule, DiagnosticBag diagnostics) = Bind(source);

        Assert.False(diagnostics.HasErrors);
        BoundTypeDefinition type = Assert.Single(boundModule.Types);

        Assert.Equal(3, type.Properties.Count);
        Assert.Equal(IntrinsicTypeRef.Int32, type.Properties["a"].Type);
        Assert.Equal(IntrinsicTypeRef.String, type.Properties["b"].Type);
        Assert.Equal(IntrinsicTypeRef.Bool, type.Properties["c"].Type);
    }

    [Fact]
    public void BindModule_WithInheritanceAndComposition_BindsBothCorrectly()
    {
        const string source = """
                              type Parent = { parentProp: int32; };
                              type Mixin = { mixinProp: string; };
                              type Child : Parent = Mixin & { ownProp: bool; };
                              """;
        (BoundModule boundModule, DiagnosticBag diagnostics) = Bind(source);

        Assert.False(diagnostics.HasErrors);
        BoundTypeDefinition parentType = boundModule.Types[0];
        BoundTypeDefinition childType = boundModule.Types[2];

        Assert.NotNull(childType.InheritingType);
        Assert.Same(parentType, childType.InheritingType.Definition);
        Assert.Equal(2, childType.Properties.Count);
        Assert.True(childType.Properties.ContainsKey("mixinProp"));
        Assert.True(childType.Properties.ContainsKey("ownProp"));
    }

    [Fact]
    public void BindModule_PreservesLocationInformation()
    {
        const string source = "type User = { id: int64; };";
        (BoundModule boundModule, DiagnosticBag diagnostics) = Bind(source);

        Assert.False(diagnostics.HasErrors);
        BoundTypeDefinition type = Assert.Single(boundModule.Types);
        Assert.NotNull(type.Location);
        Assert.Equal(1, type.Location.Start.Line);
        Assert.Equal(6, type.Location.Start.Column);

        BoundTypeDefinitionProperty prop = type.Properties["id"];
        Assert.NotNull(prop.Location);
        Assert.Equal(1, prop.Location.Start.Line);
        Assert.Equal(15, prop.Location.Start.Column);
    }

    [Fact]
    public void TypeRef_IntrinsicTypeRef_EqualsAndHashCode()
    {
        IntrinsicTypeRef ref1 = IntrinsicTypeRef.Int32;
        IntrinsicTypeRef ref2 = IntrinsicTypeRef.Int32;
        IntrinsicTypeRef ref3 = IntrinsicTypeRef.Int64;

        Assert.Equal(ref1, ref2);
        Assert.True(ref1 == ref2);
        Assert.False(ref1 != ref2);
        Assert.NotEqual(ref1, ref3);
        Assert.True(ref1 != ref3);
        Assert.False(ref1 == ref3);
        Assert.Equal(ref1.GetHashCode(), ref2.GetHashCode());
        Assert.False(ref1.Equals(null));
        Assert.False(ref1.Equals("other"));
    }

    [Fact]
    public void TypeRef_TypeDefinitionTypeRef_EqualsAndHashCode()
    {
        BoundTypeDefinition def1 = new BoundTypeDefinition(null, "Foo", null, new Dictionary<string, BoundTypeDefinitionProperty>());
        BoundTypeDefinition def2 = new BoundTypeDefinition(null, "Foo", null, new Dictionary<string, BoundTypeDefinitionProperty>());

        TypeDefinitionTypeRef ref1a = new TypeDefinitionTypeRef(def1);
        TypeDefinitionTypeRef ref1b = new TypeDefinitionTypeRef(def1);
        TypeDefinitionTypeRef ref2a = new TypeDefinitionTypeRef(def2);

        Assert.Equal(ref1a, ref1a);
        Assert.Equal(ref1a, ref1b);
        Assert.True(ref1a == ref1b);
        Assert.False(ref1a != ref1b);
        Assert.NotEqual(ref1a, ref2a);
        Assert.True(ref1a != ref2a);
        Assert.Equal(ref1a.GetHashCode(), ref1b.GetHashCode());
        Assert.False(ref1a.Equals(null));
        Assert.False(ref1a.Equals("other"));
    }
}