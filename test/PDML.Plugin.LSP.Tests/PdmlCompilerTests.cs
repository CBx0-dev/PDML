using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace PDML.Plugin.LSP.Tests;

public class PdmlCompilerTests
{
    [Fact]
    public void Compile_ValidPdml_ReturnsEmptyDiagnosticsAndTrue()
    {
        DocumentUri uri = DocumentUri.From("file:///path/to/valid.pdml");
        string content = "type Valid = { id: int32; };";

        bool result = PdmlCompiler.Compile(uri, content, out var boundModule, out Diagnostic[] diagnostics);

        Assert.True(result);
        Assert.NotNull(boundModule);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Compile_SyntaxError_ReturnsDiagnosticsAndFalse()
    {
        DocumentUri uri = DocumentUri.From("file:///path/to/invalid.pdml");
        string content = "invalidToken type Valid = Target;";

        bool result = PdmlCompiler.Compile(uri, content, out var boundModule, out Diagnostic[] diagnostics);

        Assert.False(result);
        Assert.Null(boundModule);
        Assert.NotEmpty(diagnostics);
        Assert.Contains(diagnostics, d => d.Code?.String == "PDML2001");
    }

    [Fact]
    public void Compile_FilePathOverload_ValidPdml_ReturnsEmptyDiagnostics()
    {
        string filePath = "/path/to/valid.pdml";
        string content = "type Valid = { id: int32; };";

        Diagnostic[] diagnostics = PdmlCompiler.Compile(filePath, content);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Compile_FilePathOverload_Error_ReturnsDiagnostics()
    {
        string filePath = "/path/to/error.pdml";
        string content = "type User = { id: int64; };\ntype User = { name: string; };";

        Diagnostic[] diagnostics = PdmlCompiler.Compile(filePath, content);

        Assert.NotEmpty(diagnostics);
        Assert.Contains(diagnostics, d => d.Code?.String == "PDML3000");
    }
}
