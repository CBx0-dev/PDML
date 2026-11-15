using PDML.Common;
using PDML.Output;
using Spectre.Console;

namespace PDML.Commands.Analyze;

internal sealed class PlainAnalyzeOutputHandler : IOutputHandler<AnalyzeResult>
{
    public void OnResult(AnalyzeResult result)
    {
        DiagnosticPrinter.Print(result.Diagnostics);
    }

    public void OnException(Exception exception)
    {
        AnsiConsole.WriteException(exception, ExceptionFormats.NoStackTrace);
    }
}