using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using PDML.Compiler;
using PDML.Compiler.Common;
using PDML.Generator.Abstractions;
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
                provider.GlobalOptions.TryGetValue("build_property.PDMLNamespace", out string? ns);
                if (string.IsNullOrWhiteSpace(ns))
                {
                    throw new InvalidOperationException("PDMLNamespace is not set or is empty. Add one in your PropertyGroup");
                }

                return ns;
            });

        IncrementalValueProvider<ProjectInfo> project = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".pdml"))
            .Select(static (file, cancellationToken) => new InputFile
            {
                Path = file.Path,
                Content = file.GetText(cancellationToken)?.ToString()
            })
            .Collect()
            .Combine(targetNamespace)
            .Select(static (pair, _) => new ProjectInfo
            {
                RootNamespace = pair.Right,
                Files = pair.Left
            });

        context.RegisterSourceOutput(
            project,
            static (productionContext, project) =>
            {
                InputFile[] validFiles = project.Files
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

                CSharpGenerator generator = new CSharpGenerator(
                    new CSharpGeneratorSettings
                    {
                        RootNamespace = project.RootNamespace
                    }
                );

                OutputBuilder output = generator.Generate(compilation.Modules);
                foreach (OutputFile file in output.GetFiles())
                {
                    productionContext.AddSource(
                        file.LocalName,
                        SourceText.From(file.Content, Encoding.UTF8)
                    );
                }
            }
        );
    }
}