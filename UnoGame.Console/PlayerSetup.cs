using Spectre.Console;
using UnoGame.Core.Interfaces;
using UnoGame.Core.Models;

internal static class PlayerSetup
{
    public static List<IPlayer> Setup()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("UNO CLI").Centered().Color(Color.Red));
        AnsiConsole.MarkupLine("[grey]Board edition — powered by Spectre.Console[/]\n");

        bool useDefault = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Pilih mode pemain:")
                .AddChoices(
                    "  Gunakan pemain default (Alice, Bob, Charlie, Jack)",
                    "  Masukkan nama pemain sendiri"
                )
        ) == " Gunakan pemain default (Alice, Bob, Charlie, Jack)";

        List<IPlayer> players = new List<IPlayer>();

        if (useDefault)
        {
            players.Add(new Player("Alice"));
            players.Add(new Player("Bob"));
            players.Add(new Player("Charlie"));
            players.Add(new Player("Jack"));
        }
        else
        {
            int playerCount = AnsiConsole.Prompt(
                new TextPrompt<int>("Masukkan jumlah pemain (2-4):")
                    .Validate(n => n >= 2 && n <= 4
                        ? ValidationResult.Success()
                        : ValidationResult.Error("[red]Jumlah pemain harus antara 2 sampai 4[/]"))
            );

            for (int i = 1; i <= playerCount; i++)
            {
                string name = AnsiConsole.Ask<string>($"Masukkan nama pemain {i}:");
                players.Add(new Player(name));
            }
        }

        return players;
    }
}