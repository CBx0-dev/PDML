using OmniSharp.Extensions.LanguageServer.Protocol;
using PDML.Compiler.Binding;
using PDML.Compiler.Binding.Nodes;
using PDML.Compiler.Common;
using PDML.Compiler.Diagnostics;
using PDML.Compiler.Lexing;
using PDML.Compiler.Parsing;
using PDML.Compiler.Parsing.Nodes;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using LspDiagnostic = OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic;

namespace PDML.Plugin.LSP;

internal static class PdmlCompiler
{
    public static LspDiagnostic[] Compile(DocumentUri uri, string content)
    {
        string path = uri.GetFileSystemPath();
        Compile(path, content, uri, out _, out LspDiagnostic[] diagnostics);
        return diagnostics;
    }

    public static LspDiagnostic[] Compile(string file, string content)
    {
        Compile(file, content, null, out _, out LspDiagnostic[] diagnostics);
        return diagnostics;
    }

    public static bool Compile(
        DocumentUri uri,
        string content,
        [NotNullWhen(true)] out BoundModule? boundModule,
        out LspDiagnostic[] lspDiagnostics
    )
    {
        string path = uri.GetFileSystemPath();
        return Compile(path, content, uri, out boundModule, out lspDiagnostics);
    }

    public static bool Compile(
        string file,
        string content,
        [NotNullWhen(true)] out BoundModule? boundModule,
        out LspDiagnostic[] lspDiagnostics
    )
    {
        return Compile(file, content, null, out boundModule, out lspDiagnostics);
    }

    private static bool Compile(
        string file,
        string content,
        DocumentUri? uri,
        [NotNullWhen(true)] out BoundModule? boundModule,
        out LspDiagnostic[] lspDiagnostics
    )
    {
        DiagnosticBag diagnostics = new DiagnosticBag();

        string fileDirectory = Path.GetDirectoryName(file) ?? ".";
        string fileName = Path.GetFileName(file);
        if (string.IsNullOrEmpty(fileName))
        {
            fileName = "document.pdml";
        }

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
            boundModule = null;
            lspDiagnostics = diagnostics.ToLspDiagnostics(uri);
            return false;
        }

        Parser parser = new Parser(diagnostics, tokens);
        Module module = parser.ParseModule();
        if (diagnostics.HasErrors)
        {
            boundModule = null;
            lspDiagnostics = diagnostics.ToLspDiagnostics(uri);
            return false;
        }

        boundModule = Binder.BindModule(diagnostics, module);
        lspDiagnostics = diagnostics.ToLspDiagnostics(uri);
        return !diagnostics.HasErrors;
    }
}
