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
        StringWriter writer = new StringWriter();

        generator.GenerateModule(writer, boundModule, targetNamespace);

        csharpCode = writer.ToString();
        roslynDiagnostics = null;
        return true;
    }
}