using PDML.Common;
using PDML.Output;
using Spectre.Console;

namespace PDML.Commands.Compile;

internal class PlainCompileOutputHandler : IOutputHandler<CompileResult>
{
    public void OnResult(CompileResult result)
    {
        DiagnosticPrinter.Print(result.Diagnostics);
    }

    public void OnException(Exception exception)
    {
        AnsiConsole.WriteException(exception, ExceptionFormats.NoStackTrace);
    }
}