using Spectre.Console;
using System.CommandLine.Help;

namespace EBA.CLI;

internal class CustomHelpAction(HelpAction action) : SynchronousCommandLineAction
{
    private readonly HelpAction _defaultHelp = action;

    public override int Invoke(ParseResult parseResult)
    {
        AnsiConsole.Write(new FigletText("EBA").Color(Color.Purple_1));

        int result = _defaultHelp.Invoke(parseResult);

        //AnsiConsole.WriteLine("Sample usage: --file input.txt");

        return result;

    }
}
