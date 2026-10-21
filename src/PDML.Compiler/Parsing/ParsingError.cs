using PDML.Compiler.Diagnostics;

namespace PDML.Compiler.Parsing;

public static class ParsingError
{
    public static readonly DiagnosticDescriptor ExpectedToken = new DiagnosticDescriptor
    {
        Code = (int)Code.ExpectedToken,
        Step = CompilationStep.Parsing,
        Severity = DiagnosticSeverity.Error,
        MessageFormat = "Expected '{0}', but found '{1}'",
        ArgumentCount = 2
    };

    public static readonly DiagnosticDescriptor UnexpectedToken = new DiagnosticDescriptor
    {
        Code = (int)Code.UnexpectedToken,
        Step = CompilationStep.Parsing,
        Severity = DiagnosticSeverity.Error,
        MessageFormat = "Unexpected token '{0}'",
        ArgumentCount = 1
    };

    public enum Code
    {
        ExpectedToken = 2000,
        UnexpectedToken
    }
}