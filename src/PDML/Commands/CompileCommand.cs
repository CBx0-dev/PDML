using PDML.Compiler.Binding;
using PDML.Compiler.Binding.Nodes;
using PDML.Compiler.Common;
using PDML.Compiler.Diagnostics;
using PDML.Compiler.Lexing;
using PDML.Compiler.Parsing;
using PDML.Compiler.Parsing.Nodes;
using PDML.Generator.TypeScript;
using Spectre.Console.Cli;

namespace PDML.Commands;

internal sealed class CompileSettings : CommandSettings
{
}

internal sealed class CompileCommand : AsyncCommand<CompileSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, CompileSettings settings, CancellationToken cancellationToken)
    {
        SourceFile sourceFile = SourceFile.Create(
            ".",
            "test.pdml",
            """
            @type Test = {
                foo: int32;
            };

            @type Foo = Test & {
                bar: int32;
            };
            """
        );
        DiagnosticBag diagnostics = new DiagnosticBag();
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
            return 1;
        }

        Parser parser = new Parser(diagnostics, tokens);
        Module module = parser.ParseModule();

        if (diagnostics.HasErrors)
        {
            return 1;
        }

        BoundModule boundModule = Binder.BindModule(diagnostics, module);

        if (diagnostics.HasErrors)
        {
            return 1;
        }

        TypeScriptGenerator generator = new TypeScriptGenerator();
        generator.GenerateModule(Console.Out, boundModule);

        return 0;
    }
}