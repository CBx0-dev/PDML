using PDML.Compiler.Binding;
using PDML.Compiler.Binding.Nodes;
using PDML.Compiler.Common;
using PDML.Compiler.Diagnostics;
using PDML.Compiler.Lexing;
using PDML.Compiler.Parsing;
using PDML.Compiler.Parsing.Nodes;
using System.Diagnostics.CodeAnalysis;

namespace PDML.Generator.CSharp.Roslyn;

internal static class PdmlCompiler
{
    public static bool Compile(
        string file, 
        string content,
        string targetNamespace,
        [NotNullWhen(true)] out string? csharpCode,
        [NotNullWhen(false)] out Microsoft.CodeAnalysis.Diagnostic[]? roslynDiagnostics
    )
    {
        DiagnosticBag diagnostics = new DiagnosticBag();
        
        string fileDirectory = Path.GetDirectoryName(file) ?? ".";
        string fileName = Path.GetFileName(file);

        SourceFile sourceFile = SourceFile.Create(fileDirectory, fileName, content);
        List<Token> tokens = Lexer.TokenizeAll(diagnostics, sourceFile);

        if (diagnostics.HasErrors)
        {
            csharpCode = null;
            roslynDiagnostics = diagnostics.ToRoslynDiagnostics();
            return false;
        }

        Parser parser = new Parser(diagnostics, tokens);
        Module module = parser.ParseModule();
        if (diagnostics.HasErrors)
        {
            csharpCode = null;
            roslynDiagnostics = diagnostics.ToRoslynDiagnostics();
            return false;
        }

        BoundModule boundModule = Binder.BindModule(diagnostics, module);
        if (diagnostics.HasErrors)
        {
            csharpCode = null;
            roslynDiagnostics = diagnostics.ToRoslynDiagnostics();
            return false;
        }

        CSharpGenerator generator = new CSharpGenerator();
        CSharpGeneratorSettings settings = new CSharpGeneratorSettings
        {
            RootNamespace = targetNamespace
        };

        StringWriter writer = new StringWriter();
        generator.GenerateModule(writer, settings, boundModule);

        csharpCode = writer.ToString();
        roslynDiagnostics = null;
        return true;
    }
}