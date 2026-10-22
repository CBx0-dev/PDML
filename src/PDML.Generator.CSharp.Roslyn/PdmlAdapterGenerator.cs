using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace PDML.Generator.CSharp.Roslyn;

[Generator]
public sealed class PdmlAdapterGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<InputFile> inputs = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".pdml"))
            .Select(static (file, cancellationToken) => new InputFile
            {
                Path = file.Path,
                Content = file.GetText(cancellationToken)?.ToString()
            });

        context.RegisterSourceOutput(
            inputs,
            static (productionContext, input) =>
            {
                if (input.Content is null)
                {
                    return;
                }

                if (PdmlCompiler.Compile(
                    input.Path,
                    input.Content,
                    out string? csharpCode,
                    out Diagnostic[]? diagnostics)
                )
                {
                    string hintName = Path.GetFileName(input.Path) + ".g.cs";

                    productionContext.AddSource(
                        hintName,
                        SourceText.From(csharpCode, Encoding.UTF8)
                    );

                    return;
                }

                foreach (Diagnostic diagnostic in diagnostics)
                {
                    productionContext.ReportDiagnostic(diagnostic);
                }
            }
        );


    }
}
