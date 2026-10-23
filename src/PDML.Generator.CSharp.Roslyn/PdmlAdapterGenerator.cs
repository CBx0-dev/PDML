using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace PDML.Generator.CSharp.Roslyn;

[Generator]
public sealed class PdmlAdapterGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<string> targetNamespace = context.AnalyzerConfigOptionsProvider
            .Select(static (provider, _) =>
            {
                provider.GlobalOptions.TryGetValue(
                    "build_property.PDMLNamespace",
                    out var ns);

                return ns ?? string.Empty;
            });

        IncrementalValuesProvider<InputFile> inputs = context.AdditionalTextsProvider
            .Combine(targetNamespace)
            .Where(static pair => pair.Left.Path.EndsWith(".pdml"))
            .Select(static (pair, cancellationToken) =>
            {
                AdditionalText file = pair.Left;
                string? targetNamespace = pair.Right;

                return new InputFile
                {
                    Namespace = targetNamespace,
                    Path = file.Path,
                    Content = file.GetText(cancellationToken)?.ToString()
                };
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
                    input.Namespace,
                    out string? csharpCode,
                    out Diagnostic[]? diagnostics)
                )
                {
                    string hintName = Path.GetFileNameWithoutExtension(input.Path) + ".g.cs";

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
