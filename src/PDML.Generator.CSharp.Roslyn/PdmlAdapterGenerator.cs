using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using PDML.Compiler;
using PDML.Compiler.Binding.Nodes;
using PDML.Compiler.Common;
using PdmlCompilation = PDML.Compiler.Compilation;

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

        IncrementalValueProvider<ImmutableArray<InputFile>> projectFiles = inputs.Collect();

        context.RegisterSourceOutput(
            projectFiles,
            static (productionContext, files) =>
            {
                InputFile[] validFiles = files
                    .Where(static file => file.Content is not null)
                    .ToArray();

                if (validFiles.Length == 0)
                {
                    return;
                }

                PdmlCompilation compilation = CompilerService.CompileEntryPoints(
                    new RoslynDiagnosticService(),
                    validFiles.Select(file => SourceFile.Create(file.Path, file.Content!))
                );

                foreach (Diagnostic diagnostic in (RoslynDiagnosticBag)compilation.Diagnostics)
                {
                    productionContext.ReportDiagnostic(diagnostic);   
                }

                CSharpGenerator generator = new CSharpGenerator();
                CSharpGeneratorSettings settings = new CSharpGeneratorSettings
                {
                    RootNamespace = files[0].Namespace
                };

                StringWriter writer = new StringWriter();
                foreach (BoundModule module in compilation.Modules)
                {
                    writer.GetStringBuilder().Clear();
                    generator.GenerateModule(writer, settings, module);

                    string hintName = module.Identity.ToString() + ".g.cs";

                    productionContext.AddSource(
                        hintName,
                        SourceText.From(writer.ToString(), Encoding.UTF8)
                    );
                }
            }
        );
    }
}
