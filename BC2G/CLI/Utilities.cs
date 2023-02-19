namespace BC2G.CLI;

public static class Utilities
{
    public static bool Prompt(CancellationToken cT)
    {
        do
        {
            Console.Write("Do you want to retry? [Y/N] ");
            var keyInfo = Console.ReadKey();

            cT.ThrowIfCancellationRequested();

            switch (keyInfo.Key.ToString().ToUpper())
            {
                case "Y": Console.WriteLine(); return true;
                case "N": Console.WriteLine(); return false;
                default: Console.WriteLine($"\tInvalid choice; please retry."); break;
            }
        }
        while (true);
    }

    public static bool ContinueTerminatePrompt(string message, CancellationToken cT)
    {
        do
        {
            Console.Write($"{message} [Continue [C] or Terminate [T]] ");
            var keyInfo = Console.ReadKey();

            cT.ThrowIfCancellationRequested();

            switch (keyInfo.Key.ToString().ToUpper())
            {
                case "C": Console.WriteLine(); return true;
                case "T": Console.WriteLine(); return false;
                default: Console.WriteLine($"\tInvalid choice; please retry."); break;
            }
        }
        while (true);
    }
}
