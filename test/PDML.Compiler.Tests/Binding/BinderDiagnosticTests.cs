using PDML.Compiler.Binding;
using PDML.Compiler.Binding.Nodes;
using PDML.Compiler.Common;
using PDML.Compiler.Diagnostics;
using PDML.Compiler.Lexing;
using PDML.Compiler.Parsing;
using PDML.Compiler.Parsing.Nodes;

namespace PDML.Compiler.Tests.Binding;

public sealed class BinderDiagnosticTests
{
    private static DiagnosticBag Bind(string content)
    {
        DiagnosticBag diagnostics = new DiagnosticBag();
        SourceFile sourceFile = SourceFile.Create(".", "test.pdml", content);
        List<Token> tokens = Lexer.TokenizeAll(diagnostics, sourceFile);

        Parser parser = new Parser(diagnostics, tokens);
        Module module = parser.ParseModule();
        Binder.BindModule(diagnostics, module);
        return diagnostics;
    }

    [Fact]
    public void BindModule_WhenDuplicateTypeName_ReportsDuplicateNameDiagnostic()
    {
        const string source = """
                              type User = { id: int64; };
                              type User = { name: string; };
                              """;
        DiagnosticBag diagnostics = Bind(source);

        Assert.True(diagnostics.HasErrors);
        Diagnostic diag = Assert.Single(diagnostics, d => d.Descriptor.Code == (int)BindingError.Code.DuplicateName);
        Assert.Equal(CompilationStep.Binding, diag.Descriptor.Step);
        Assert.Equal(DiagnosticSeverity.Error, diag.Descriptor.Severity);
        Assert.Equal(2, diag.Location.Start.Line);
        Assert.Single(diag.AdditionalLocations);
        Assert.Equal(1, diag.AdditionalLocations[0].Start.Line);
    }

    [Fact]
    public void BindModule_WhenGenericFunctionInInheritance_ReportsFunctionNotAllowedDiagnostic()
    {
        const string source = """
                              type Base = {};
                              type Derived : Base<string> = {};
                              """;
        DiagnosticBag diagnostics = Bind(source);

        Assert.True(diagnostics.HasErrors);
        Diagnostic diag = Assert.Single(diagnostics, d => d.Descriptor.Code == (int)BindingError.Code.FunctionNotAllowed);
        Assert.Equal(CompilationStep.Binding, diag.Descriptor.Step);
        Assert.Equal(DiagnosticSeverity.Error, diag.Descriptor.Severity);
        Assert.Equal(2, diag.Location.Start.Line);
    }

    [Fact]
    public void BindModule_WhenUnknownTypeInPropertyClause_ReportsUnknownTypeDiagnostic()
    {
        const string source = "type User = { role: NonExistentType; };";
        DiagnosticBag diagnostics = Bind(source);

        Assert.True(diagnostics.HasErrors);
        Diagnostic diag = Assert.Single(diagnostics, d => d.Descriptor.Code == (int)BindingError.Code.UnknownType);
        Assert.Equal(CompilationStep.Binding, diag.Descriptor.Step);
        Assert.Equal(DiagnosticSeverity.Error, diag.Descriptor.Severity);
    }

    [Fact]
    public void BindModule_WhenUnknownTypeInInheritance_ReportsUnknownTypeDiagnostic()
    {
        const string source = "type Dog : UnknownAnimal = {};";
        DiagnosticBag diagnostics = Bind(source);

        Assert.True(diagnostics.HasErrors);
        Diagnostic diag = Assert.Single(diagnostics, d => d.Descriptor.Code == (int)BindingError.Code.UnknownType);
        Assert.Equal(CompilationStep.Binding, diag.Descriptor.Step);
        Assert.Equal(DiagnosticSeverity.Error, diag.Descriptor.Severity);
    }

    [Fact]
    public void BindModule_WhenUnknownTypeInComposition_ReportsUnknownTypeDiagnostic()
    {
        const string source = "type Entity = UnknownMixin & { id: int64; };";
        DiagnosticBag diagnostics = Bind(source);

        Assert.True(diagnostics.HasErrors);
        Diagnostic diag = Assert.Single(diagnostics, d => d.Descriptor.Code == (int)BindingError.Code.UnknownType);
        Assert.Equal(CompilationStep.Binding, diag.Descriptor.Step);
        Assert.Equal(DiagnosticSeverity.Error, diag.Descriptor.Severity);
    }

    [Fact]
    public void BindModule_WhenIntrinsicTypeInInheritance_ReportsIntrinsicTypeNotAllowedDiagnostic()
    {
        const string source = "type SpecialInt : int32 = {};";
        DiagnosticBag diagnostics = Bind(source);

        Assert.True(diagnostics.HasErrors);
        Diagnostic diag = Assert.Single(diagnostics, d => d.Descriptor.Code == (int)BindingError.Code.IntrinsicTypeNotAllowed);
        Assert.Equal(CompilationStep.Binding, diag.Descriptor.Step);
        Assert.Equal(DiagnosticSeverity.Error, diag.Descriptor.Severity);
    }

    [Fact]
    public void BindModule_WhenIntrinsicTypeInComposition_ReportsIntrinsicTypeNotAllowedDiagnostic()
    {
        const string source = "type Combined = int32 & { id: int64; };";
        DiagnosticBag diagnostics = Bind(source);

        Assert.True(diagnostics.HasErrors);
        Diagnostic diag = Assert.Single(diagnostics, d => d.Descriptor.Code == (int)BindingError.Code.IntrinsicTypeNotAllowed);
        Assert.Equal(CompilationStep.Binding, diag.Descriptor.Step);
        Assert.Equal(DiagnosticSeverity.Error, diag.Descriptor.Severity);
    }

    [Fact]
    public void BindModule_WhenSelfCircularInheritance_ReportsCircularInheritanceDiagnostic()
    {
        const string source = "type SelfLoop : SelfLoop = {};";
        DiagnosticBag diagnostics = Bind(source);

        Assert.True(diagnostics.HasErrors);
        Diagnostic diag = Assert.Single(diagnostics, d => d.Descriptor.Code == (int)BindingError.Code.CircularInheritance);
        Assert.Equal(CompilationStep.Binding, diag.Descriptor.Step);
        Assert.Equal(DiagnosticSeverity.Error, diag.Descriptor.Severity);
        Assert.NotEmpty(diag.AdditionalLocations);
    }

    [Fact]
    public void BindModule_WhenDirectTwoTypeCircularInheritance_ReportsCircularInheritanceDiagnostic()
    {
        const string source = """
                              type A : B = {};
                              type B : A = {};
                              """;
        DiagnosticBag diagnostics = Bind(source);

        Assert.True(diagnostics.HasErrors);
        Diagnostic diag = Assert.Single(diagnostics, d => d.Descriptor.Code == (int)BindingError.Code.CircularInheritance);
        Assert.Equal(CompilationStep.Binding, diag.Descriptor.Step);
        Assert.Equal(DiagnosticSeverity.Error, diag.Descriptor.Severity);
        Assert.NotEmpty(diag.AdditionalLocations);
    }

    [Fact]
    public void BindModule_WhenThreeTypeCircularInheritance_ReportsCircularInheritanceDiagnostic()
    {
        const string source = """
                              type A : B = {};
                              type B : C = {};
                              type C : A = {};
                              """;
        DiagnosticBag diagnostics = Bind(source);

        Assert.True(diagnostics.HasErrors);
        Diagnostic diag = Assert.Single(diagnostics, d => d.Descriptor.Code == (int)BindingError.Code.CircularInheritance);
        Assert.Equal(CompilationStep.Binding, diag.Descriptor.Step);
        Assert.Equal(DiagnosticSeverity.Error, diag.Descriptor.Severity);
        Assert.NotEmpty(diag.AdditionalLocations);
    }

    [Fact]
    public void BindModule_WhenDuplicatePropertyInSameBody_ReportsDuplicatePropertyDiagnostic()
    {
        const string source = """
                              type User = {
                                  id: int32;
                                  id: int64;
                              };
                              """;
        DiagnosticBag diagnostics = Bind(source);

        Assert.True(diagnostics.HasErrors);
        Diagnostic diag = Assert.Single(diagnostics, d => d.Descriptor.Code == (int)BindingError.Code.DuplicateProperty);
        Assert.Equal(CompilationStep.Binding, diag.Descriptor.Step);
        Assert.Equal(DiagnosticSeverity.Error, diag.Descriptor.Severity);
        Assert.Single(diag.AdditionalLocations);
        Assert.Equal(2, diag.AdditionalLocations[0].Start.Line);
        Assert.Equal(3, diag.Location.Start.Line);
    }

    [Fact]
    public void BindModule_WhenDuplicatePropertyAcrossMultipleBodies_ReportsDuplicatePropertyDiagnostic()
    {
        const string source = "type User = { id: int32; } & { id: int64; };";
        DiagnosticBag diagnostics = Bind(source);

        Assert.True(diagnostics.HasErrors);
        Diagnostic diag = Assert.Single(diagnostics, d => d.Descriptor.Code == (int)BindingError.Code.DuplicateProperty);
        Assert.Equal(CompilationStep.Binding, diag.Descriptor.Step);
        Assert.Equal(DiagnosticSeverity.Error, diag.Descriptor.Severity);
        Assert.Single(diag.AdditionalLocations);
    }

    [Fact]
    public void BindModule_WhenDuplicatePropertyFromComposition_ReportsDuplicatePropertyCompositionDiagnostic()
    {
        const string source = """
                              type Base = { id: int64; };
                              type Derived = { id: string; } & Base;
                              """;
        DiagnosticBag diagnostics = Bind(source);

        Assert.True(diagnostics.HasErrors);
        Diagnostic diag = Assert.Single(diagnostics, d => d.Descriptor.Code == (int)BindingError.Code.DuplicatePropertyComposition);
        Assert.Equal(CompilationStep.Binding, diag.Descriptor.Step);
        Assert.Equal(DiagnosticSeverity.Error, diag.Descriptor.Severity);
        Assert.Single(diag.AdditionalLocations);
    }

    [Fact]
    public void BindModule_WhenMultipleErrorsOccur_ReportsAllDiagnostics()
    {
        const string source = """
                              type A = { x: UnknownType1; };
                              type B = { y: UnknownType2; };
                              """;
        DiagnosticBag diagnostics = Bind(source);

        Assert.True(diagnostics.HasErrors);
        Assert.Equal(2, diagnostics.Count(d => d.Descriptor.Code == (int)BindingError.Code.UnknownType));
    }

    [Fact]
    public void BindModule_WhenHeaderPhaseHasErrors_SubsequentBindingPhasesDoNotExecute()
    {
        // When duplicate name error occurs in header phase, inheritance/body/composition phases abort early
        const string source = """
                              type A = { x: int32; };
                              type A : NonExistent = { y: int32; };
                              """;
        DiagnosticBag diagnostics = Bind(source);

        Assert.True(diagnostics.HasErrors);
        // Only duplicate name error is reported; unknown type for inheritance is not evaluated
        Assert.Contains(diagnostics, d => d.Descriptor.Code == (int)BindingError.Code.DuplicateName);
        Assert.DoesNotContain(diagnostics, d => d.Descriptor.Code == (int)BindingError.Code.UnknownType);
    }
}