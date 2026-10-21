using PDML.Compiler.Diagnostics;

namespace PDML.Compiler.Lexing;

public static class LexingError
{
    public static readonly DiagnosticDescriptor UnexpectedCharacter = new DiagnosticDescriptor
    {
        Code = (int)Code.UnexpectedCharacter,
        Step = CompilationStep.Lexing,
        Severity = DiagnosticSeverity.Error,
        MessageFormat = "Unexpected character '{0}'"
    };

    private enum Code
    {
        UnexpectedCharacter = 1000
    }
}