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
                if (
                    !provider.GlobalOptions.TryGetValue("build_property.PDMLNamespace", out string? ns) ||
                    string.IsNullOrWhiteSpace(ns)
                )
                {
                    throw new InvalidOperationException("PDMLNamespace is not set or is empty. Add one in your PropertyGroup");
                }

                return ns;
            });

        IncrementalValueProvider<string> projectRoot = context.AnalyzerConfigOptionsProvider
            .Select(static (provider, _) =>
            {
                if (provider.GlobalOptions.TryGetValue("build_property.MSBuildProjectDirectory", out string? projectRoot))
                {
                    return projectRoot;
                }

                if (
                    !provider.GlobalOptions.TryGetValue("build_property.PDMLRoot", out projectRoot) ||
                    string.IsNullOrWhiteSpace(projectRoot)
                )
                {
                    throw new InvalidOperationException("A project root is required. MSBuildProjectDirectory and PDMLRoot returned null or whitespace");
                }

                return projectRoot;
            });

        IncrementalValueProvider<ProjectInfo> project = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".pdml"))
            .Select(static (file, cancellationToken) => new InputFile
            {
                Path = file.Path,
                Content = file.GetText(cancellationToken)?.ToString()
            })
            .Collect()
            .CombinedSelect(targetNamespace, projectRoot, (files, rootNamespace, projectRoot, _) =>
                new ProjectInfo
                {
                    RootNamespace = rootNamespace,
                    ProjectRoot = projectRoot,
                    Files = files
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
                    project.ProjectRoot,
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