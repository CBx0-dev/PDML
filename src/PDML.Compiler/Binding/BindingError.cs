using PDML.Compiler.Diagnostics;

namespace PDML.Compiler.Binding;

public static class BindingError
{
    public static readonly DiagnosticDescriptor DuplicateName = new DiagnosticDescriptor
    {
        Code = (int)Code.DuplicateName,
        Step = CompilationStep.Binding,
        Severity = DiagnosticSeverity.Error,
        MessageFormat = "Member with the same name '{0}' was already declared",
        ArgumentCount = 1
    };

    public static readonly DiagnosticDescriptor FunctionNotAllowed = new DiagnosticDescriptor
    {
        Code = (int)Code.FunctionNotAllowed,
        Step = CompilationStep.Binding,
        Severity = DiagnosticSeverity.Error,
        MessageFormat = "Function '{0}' is not allowed in this context",
        ArgumentCount = 1
    };

    public static readonly DiagnosticDescriptor UnknownType = new DiagnosticDescriptor
    {
        Code = (int)Code.UnknownType,
        Step = CompilationStep.Binding,
        Severity = DiagnosticSeverity.Error,
        MessageFormat = "Unknown type '{0}'",
        ArgumentCount = 1
    };

    public static readonly DiagnosticDescriptor VirtualTypeNotAllowed = new DiagnosticDescriptor
    {
        Code = (int)Code.VirtualTypeNotAllowed,
        Step = CompilationStep.Binding,
        Severity = DiagnosticSeverity.Error,
        MessageFormat = "A virtual type is not allowed in this context",
        ArgumentCount = 0
    };

    public static readonly DiagnosticDescriptor IntrinsicTypeNotAllowed = new DiagnosticDescriptor
    {
        Code = (int)Code.IntrinsicTypeNotAllowed,
        Step = CompilationStep.Binding,
        Severity = DiagnosticSeverity.Error,
        MessageFormat = "Intrinsic type '{0}' is not allowed in this context",
        ArgumentCount = 1
    };

    public static readonly DiagnosticDescriptor CircularInheritance = new DiagnosticDescriptor
    {
        Code = (int)Code.CircularInheritance,
        Step = CompilationStep.Binding,
        Severity = DiagnosticSeverity.Error,
        MessageFormat = "Circular inheritance detected for type '{0}'",
        ArgumentCount = 1
    };

    public static readonly DiagnosticDescriptor DuplicateProperty = new DiagnosticDescriptor
    {
        Code = (int)Code.DuplicateProperty,
        Step = CompilationStep.Binding,
        Severity = DiagnosticSeverity.Error,
        MessageFormat = "A property with the name '{0}' is already defined",
        ArgumentCount = 1
    };

    public static readonly DiagnosticDescriptor DuplicatePropertyComposition = new DiagnosticDescriptor
    {
        Code = (int)Code.DuplicatePropertyComposition,
        Step = CompilationStep.Binding,
        Severity = DiagnosticSeverity.Error,
        MessageFormat = "The property '{0}' is already defined, and copied as composition from type '{1}'",
        ArgumentCount = 2
    };

    public static readonly DiagnosticDescriptor UnknownImport = new DiagnosticDescriptor
    {
        Code = (int)Code.UnknownImport,
        Step = CompilationStep.Binding,
        Severity = DiagnosticSeverity.Error,
        MessageFormat = "No symbol named '{0}' is exported from '{1}'",
        ArgumentCount = 2
    };

    public enum Code
    {
        DuplicateName = 3000,
        FunctionNotAllowed,
        UnknownType,
        VirtualTypeNotAllowed,
        IntrinsicTypeNotAllowed,
        CircularInheritance,
        DuplicateProperty,
        DuplicatePropertyComposition,
        UnknownImport
    }
}