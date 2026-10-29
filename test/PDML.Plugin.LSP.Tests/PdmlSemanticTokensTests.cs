using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using PDML.Plugin.LSP;

namespace PDML.Plugin.LSP.Tests;

public class PdmlSemanticTokensTests
{
    [Fact]
    public void CollectTokens_EmptyContent_ReturnsEmpty()
    {
        List<SemanticTokenInfo> tokens = PdmlSemanticTokens.CollectTokens("");
        Assert.Empty(tokens);
    }

    [Fact]
    public void CollectTokens_BasicTypeDefinitionWithProperties()
    {
        string content = "type User = {\n    id: int32;\n    name: string;\n};";
        List<SemanticTokenInfo> tokens = PdmlSemanticTokens.CollectTokens(content);

        // Expected tokens:
        // Line 0: type (Keyword)
        // Line 0: User (Type, Declaration)
        // Line 0: = (Operator)
        // Line 1: id (Property, Declaration)
        // Line 1: int32 (Type, DefaultLibrary)
        // Line 2: name (Property, Declaration)
        // Line 2: string (Type, DefaultLibrary)

        Assert.Equal(7, tokens.Count);

        Assert.Equal(new SemanticTokenInfo(0, 0, 4, SemanticTokenType.Keyword, Array.Empty<SemanticTokenModifier>()), tokens[0]);
        Assert.Equal(new SemanticTokenInfo(0, 5, 4, SemanticTokenType.Type, new[] { SemanticTokenModifier.Declaration }), tokens[1]);
        Assert.Equal(new SemanticTokenInfo(0, 10, 1, SemanticTokenType.Operator, Array.Empty<SemanticTokenModifier>()), tokens[2]);

        Assert.Equal(new SemanticTokenInfo(1, 4, 2, SemanticTokenType.Property, new[] { SemanticTokenModifier.Declaration }), tokens[3]);
        Assert.Equal(new SemanticTokenInfo(1, 8, 5, SemanticTokenType.Type, new[] { SemanticTokenModifier.DefaultLibrary }), tokens[4]);

        Assert.Equal(new SemanticTokenInfo(2, 4, 4, SemanticTokenType.Property, new[] { SemanticTokenModifier.Declaration }), tokens[5]);
        Assert.Equal(new SemanticTokenInfo(2, 10, 6, SemanticTokenType.Type, new[] { SemanticTokenModifier.DefaultLibrary }), tokens[6]);
    }

    [Fact]
    public void CollectTokens_InheritanceAndComposition()
    {
        string content = "type Admin : User = BaseUser & {\n    role: string;\n};";
        List<SemanticTokenInfo> tokens = PdmlSemanticTokens.CollectTokens(content);

        // Expected tokens:
        // Line 0: @type (Keyword)
        // Line 0: Admin (Type, Declaration)
        // Line 0: User (Type)
        // Line 0: = (Operator)
        // Line 0: BaseUser (Type)
        // Line 0: & (Operator)
        // Line 1: role (Property, Declaration)
        // Line 1: string (Type, DefaultLibrary)

        Assert.Equal(8, tokens.Count);

        Assert.Equal(new SemanticTokenInfo(0, 0, 4, SemanticTokenType.Keyword, Array.Empty<SemanticTokenModifier>()), tokens[0]);
        Assert.Equal(new SemanticTokenInfo(0, 5, 5, SemanticTokenType.Type, new[] { SemanticTokenModifier.Declaration }), tokens[1]);
        Assert.Equal(new SemanticTokenInfo(0, 13, 4, SemanticTokenType.Type, Array.Empty<SemanticTokenModifier>()), tokens[2]);
        Assert.Equal(new SemanticTokenInfo(0, 18, 1, SemanticTokenType.Operator, Array.Empty<SemanticTokenModifier>()), tokens[3]);
        Assert.Equal(new SemanticTokenInfo(0, 20, 8, SemanticTokenType.Type, Array.Empty<SemanticTokenModifier>()), tokens[4]);
        Assert.Equal(new SemanticTokenInfo(0, 29, 1, SemanticTokenType.Operator, Array.Empty<SemanticTokenModifier>()), tokens[5]);

        Assert.Equal(new SemanticTokenInfo(1, 4, 4, SemanticTokenType.Property, new[] { SemanticTokenModifier.Declaration }), tokens[6]);
        Assert.Equal(new SemanticTokenInfo(1, 10, 6, SemanticTokenType.Type, new[] { SemanticTokenModifier.DefaultLibrary }), tokens[7]);
    }

    [Fact]
    public void CollectTokens_GenericsInTypeSignatures()
    {
        string content = "type Response = {\n    data: List<int32>;\n};";
        List<SemanticTokenInfo> tokens = PdmlSemanticTokens.CollectTokens(content);

        // Expected tokens:
        // Line 0: @type (Keyword)
        // Line 0: Response (Type, Declaration)
        // Line 0: = (Operator)
        // Line 1: data (Property, Declaration)
        // Line 1: List (Type)
        // Line 1: int32 (Type, DefaultLibrary)

        Assert.Equal(6, tokens.Count);

        Assert.Equal(new SemanticTokenInfo(0, 0, 4, SemanticTokenType.Keyword, Array.Empty<SemanticTokenModifier>()), tokens[0]);
        Assert.Equal(new SemanticTokenInfo(0, 5, 8, SemanticTokenType.Type, new[] { SemanticTokenModifier.Declaration }), tokens[1]);
        Assert.Equal(new SemanticTokenInfo(0, 14, 1, SemanticTokenType.Operator, Array.Empty<SemanticTokenModifier>()), tokens[2]);

        Assert.Equal(new SemanticTokenInfo(1, 4, 4, SemanticTokenType.Property, new[] { SemanticTokenModifier.Declaration }), tokens[3]);
        Assert.Equal(new SemanticTokenInfo(1, 10, 4, SemanticTokenType.Type, Array.Empty<SemanticTokenModifier>()), tokens[4]);
        Assert.Equal(new SemanticTokenInfo(1, 15, 5, SemanticTokenType.Type, new[] { SemanticTokenModifier.DefaultLibrary }), tokens[5]);
    }

    [Fact]
    public void CollectTokens_MultipleTypeDefinitions()
    {
        string content = "type A = { id: int32; };\ntype B = A & { name: string; };";
        List<SemanticTokenInfo> tokens = PdmlSemanticTokens.CollectTokens(content);

        Assert.Equal(12, tokens.Count);

        // Type A
        Assert.Equal(0, tokens[0].Line);
        Assert.Equal(SemanticTokenType.Keyword, tokens[0].TokenType);
        Assert.Equal(0, tokens[1].Line);
        Assert.Equal(SemanticTokenType.Type, tokens[1].TokenType);
        Assert.Equal(0, tokens[2].Line);
        Assert.Equal(SemanticTokenType.Operator, tokens[2].TokenType);
        Assert.Equal(0, tokens[3].Line);
        Assert.Equal(SemanticTokenType.Property, tokens[3].TokenType);
        Assert.Equal(0, tokens[4].Line);
        Assert.Equal(SemanticTokenType.Type, tokens[4].TokenType);

        // Type B
        Assert.Equal(1, tokens[5].Line);
        Assert.Equal(SemanticTokenType.Keyword, tokens[5].TokenType);
        Assert.Equal(1, tokens[6].Line);
        Assert.Equal(SemanticTokenType.Type, tokens[6].TokenType);
        Assert.Equal(1, tokens[7].Line);
        Assert.Equal(SemanticTokenType.Operator, tokens[7].TokenType);
        Assert.Equal(1, tokens[8].Line);
        Assert.Equal(SemanticTokenType.Type, tokens[8].TokenType);
        Assert.Equal(1, tokens[9].Line);
        Assert.Equal(SemanticTokenType.Operator, tokens[9].TokenType);
        Assert.Equal(1, tokens[10].Line);
        Assert.Equal(SemanticTokenType.Property, tokens[10].TokenType);
        Assert.Equal(1, tokens[11].Line);
        Assert.Equal(SemanticTokenType.Type, tokens[11].TokenType);
    }
}
