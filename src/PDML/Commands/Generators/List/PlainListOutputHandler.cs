using PDML.Generator.Abstractions;
using PDML.Output;
using Spectre.Console;

namespace PDML.Commands.Generators.List;

internal sealed class PlainListOutputHandler : IOutputHandler<ListResult>
{
    public void OnResult(ListResult result)
    {
        Table table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Gray)
            .Expand()
            .AddColumn(new TableColumn("[gray]Name[/]").NoWrap())
            .AddColumn(new TableColumn("[gray]Key[/]").NoWrap())
            .AddColumn(new TableColumn("[gray]Assembly[/]").NoWrap());

        foreach (CodeGeneratorBuilder generator in result.Generators)
        {
            table.AddRow(
                $"[cyan]{Markup.Escape(generator.Name)}[/]",
                $"[yellow]{Markup.Escape(generator.Key)}[/]",
                $"[gray]{Markup.Escape(generator.GetType().Assembly.ToString())}[/]"
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    public void OnException(Exception exception)
    {
        AnsiConsole.WriteException(exception, ExceptionFormats.NoStackTrace);
    }
}