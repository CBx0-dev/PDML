using PDML.Compiler.Diagnostics;

namespace PDML.Compiler.Lexing;

public static class LexingError
{
    public static readonly DiagnosticDescriptor UnexpectedCharacter = new DiagnosticDescriptor
    {
        Code = (int)Code.UnexpectedCharacter,
        Step = CompilationStep.Lexing,
        Severity = DiagnosticSeverity.Error,
        MessageFormat = "Unexpected character '{0}'",
        ArgumentCount = 1
    };

    public static readonly DiagnosticDescriptor UnexpectedKeyword = new DiagnosticDescriptor
    {
        Code = (int)Code.UnexpectedKeyword,
        Step = CompilationStep.Lexing,
        Severity = DiagnosticSeverity.Error,
        MessageFormat = "Unexpected keyword '{0}'",
        ArgumentCount = 1
    };

    public static readonly DiagnosticDescriptor UnterminatedString = new DiagnosticDescriptor
    {
        Code = (int)Code.UnterminatedString,
        Step = CompilationStep.Lexing,
        Severity =DiagnosticSeverity.Error,
        MessageFormat = "Unterminated string",
        ArgumentCount = 1
    };

    public enum Code
    {
        UnexpectedCharacter = 1000,
        UnexpectedKeyword,
        UnterminatedString
    }
}