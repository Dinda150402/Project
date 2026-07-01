using Spectre.Console;
using Spectre.Console.Rendering;
using UnoGame.Core.Controllers;
using UnoGame.Core.Enums;
using UnoGame.Core.Interfaces;
using UnoGame.Core.Models;

internal static class GameRenderer
{
    public static void RenderBoard(GameController game, IPlayer currentPlayer)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("UNO").Centered().Color(Color.Gold1));

        ICard topCard = game.GetTopDiscardCard();

        GameResult<CardColor> colorResult = game.GetCurrentColor();
        CardColor activeColor = colorResult.Success ? colorResult.Value : CardColor.Red;
        string activeColorName = ColorName(activeColor);

        Panel arena = new Panel(CardPanel(topCard, "Kartu Teratas"))
            .Border(BoxBorder.Double)
            .BorderColor(GetSpectreColor(activeColor))
            .Header($" Warna Aktif: [bold {activeColorName}]{activeColor}[/] ")
            .Padding(1, 0, 1, 0);

        arena.Width = 28;

        Table arenaWrapper = new Table()
            .NoBorder()
            .HideHeaders()
            .Collapse();
        arenaWrapper.AddColumn(new TableColumn(string.Empty));
        arenaWrapper.AddRow(arena);
        AnsiConsole.Write(Align.Center(arenaWrapper));
        AnsiConsole.WriteLine();

        Table statusTable = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold]Status Pemain[/]");
        statusTable.AddColumn("Pemain");
        statusTable.AddColumn("Sisa Kartu");
        statusTable.AddColumn("Skor");
        statusTable.AddColumn("Status");

        List<IPlayer> pending = game.GetUnoPendingPlayers();

        foreach (IPlayer p in game.GetPlayers())
        {
            GameResult<List<ICard>> handResult = game.GetPlayerHand(p);
            int cardCount = handResult.Success ? handResult.Value.Count : 0;

            string status = (p == currentPlayer) ? "[green]▶ Giliran[/]" : "";
            if (pending.Contains(p))
            {
                status += (status.Length > 0 ? " " : "") + "[red]⚠ Belum UNO[/]";
            }

            string nameCell = (p == currentPlayer)
                ? $"[bold]{Esc(p.Name)}[/]"
                : Esc(p.Name);

            statusTable.AddRow(nameCell, cardCount.ToString(), p.Score.ToString(), status);
        }

        AnsiConsole.Write(statusTable);
        AnsiConsole.WriteLine();
    }
    public static void RenderHand(List<ICard> hand)
    {
        AnsiConsole.MarkupLine("[bold]Kartu di tangan Anda:[/]");

        if (hand.Count == 0)
        {
            return;
        }

        Table handTable = new Table()
            .NoBorder()
            .HideHeaders()
            .Collapse();

        for (int i = 0; i < hand.Count; i++)
        {
            handTable.AddColumn(new TableColumn(string.Empty).NoWrap());
        }

        IRenderable[] cardPanels = hand
            .Select((c, i) => (IRenderable)CardPanel(c, $"#{i}"))
            .ToArray();

        handTable.AddRow(cardPanels);
        AnsiConsole.Write(handTable);
        AnsiConsole.WriteLine();
    }
    public static void PrintScoreboard(GameController game)
    {
        Table scoreTable = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold]Skor Sementara[/]");
        scoreTable.AddColumn("Pemain");
        scoreTable.AddColumn("Skor");

        List<IPlayer> sortedPlayers = game.GetPlayers()
            .OrderByDescending(p => p.Score)
            .ToList();

        foreach (IPlayer p in sortedPlayers)
        {
            scoreTable.AddRow(Esc(p.Name), p.Score.ToString());
        }

        AnsiConsole.Write(scoreTable);
    }
    public static void ShowIntermissionScreen(IPlayer nextPlayer)
    {
        AnsiConsole.MarkupLine("\n[grey]Tekan ENTER untuk melanjutkan...[/]");
        Console.ReadLine();

        AnsiConsole.Clear();

        Panel panel = new Panel(
            Align.Center(
                new Markup(
                    $"[bold yellow]Harap serahkan komputer kepada:[/]\n\n" +
                    $"[bold underline]{Esc(nextPlayer.Name)}[/]\n\n" +
                    $"[grey]Pemain sebelumnya dilarang mengintip layar![/]"
                )
            )
        )
        .Border(BoxBorder.Double)
        .BorderColor(Color.Yellow)
        .Header(" PERGANTIAN GILIRAN ")
        .Padding(4, 2, 4, 2)
        .Expand();

        AnsiConsole.Write(panel);
        AnsiConsole.MarkupLine(
            "\n[grey]Tekan [bold]ENTER[/] jika Anda sudah siap untuk melihat kartu...[/]");
        Console.ReadLine();
        AnsiConsole.Clear();

        AnsiConsole.MarkupLine(
            $"\n🔔 [bold]GILIRAN BARU:[/] Sekarang giliran [bold]{Esc(nextPlayer.Name)}[/]");
    }
    public static Panel CardPanel(ICard card, string footer)
    {
        string colorName = ColorName(card.Color);
        Markup content = new Markup(
            $"[bold {colorName}]{CardLabel(card)}[/]\n[grey]{footer}[/]");

        Panel panel = new Panel(Align.Center(content))
            .Border(BoxBorder.Rounded)
            .BorderColor(GetSpectreColor(card.Color))
            .Padding(2, 1, 2, 1);

        panel.Width = 14;
        return panel;
    }
    public static Color GetSpectreColor(CardColor? color) => color switch
    {
        CardColor.Red => Color.Red,
        CardColor.Blue => Color.Blue,
        CardColor.Green => Color.Green,
        CardColor.Yellow => Color.Yellow,
        _ => Color.Grey
    };
    public static string CardMarkup(ICard card)
    {
        string colorName = ColorName(card.Color);
        string markup = $"[bold {colorName}]{CardLabel(card)}[/]";
        return markup;
    }
    public static string ColorName(CardColor? color) => color switch
    {
        CardColor.Red => "red",
        CardColor.Blue => "blue",
        CardColor.Green => "green",
        CardColor.Yellow => "yellow",
        _ => "grey"
    };
    public static string CardLabel(ICard card) => card.Value switch
    {
        CardValue.Zero => "0",
        CardValue.One => "1",
        CardValue.Two => "2",
        CardValue.Three => "3",
        CardValue.Four => "4",
        CardValue.Five => "5",
        CardValue.Six => "6",
        CardValue.Seven => "7",
        CardValue.Eight => "8",
        CardValue.Nine => "9",
        CardValue.Skip => "SKIP",
        CardValue.Reverse => "REV",
        CardValue.DrawTwo => "+2",
        CardValue.Wild => "WILD",
        CardValue.WildDrawFour => "+4",
        _ => card.Value.ToString()
    };
    public static string Esc(string s) => Markup.Escape(s ?? "");
}