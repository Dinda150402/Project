using UnoGame.Core.Controllers;
using UnoGame.Core.Interfaces;
using UnoGame.Core.Enums;
using Spectre.Console;
using Spectre.Console.Rendering;

internal static class GameRenderer {
    public static void RenderBoard(GameController game, IPlayer currentPlayer)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("UNO").Centered().Color(Color.Gold1));

        ICard topCard = game.GetTopDiscardCard();
        CardColor activeColor = game.GetCurrentColor();
        string activeColorName = ColorName(activeColor);

        var arena = new Panel(CardPanel(topCard, "Kartu Teratas"))
            .Border(BoxBorder.Double)
            .BorderColor(GetSpectreColor(activeColor))
            .Header($" Warna Aktif: [bold {activeColorName}]{activeColor}[/] ")
            .Padding(1, 0, 1, 0);

        arena.Width = 28;  

        var arenaWrapper = new Table()
            .NoBorder()
            .HideHeaders()
            .Collapse();                             
        arenaWrapper.AddColumn(new TableColumn(string.Empty));
        arenaWrapper.AddRow(arena);
        AnsiConsole.Write(Align.Center(arenaWrapper));
        AnsiConsole.WriteLine();

        var table = new Table().Border(TableBorder.Rounded).Title("[bold]Status Pemain[/]");
        table.AddColumn("Pemain");
        table.AddColumn("Sisa Kartu");
        table.AddColumn("Skor");
        table.AddColumn("Status");

        var pending = game.GetUnoPendingPlayers();
        foreach (var p in game.GetPlayers())
        {
            string status = (p == currentPlayer) ? "[green]▶ Giliran[/]" : "";
            if (pending.Contains(p))
                status += (status.Length > 0 ? " " : "") + "[red]⚠ Belum UNO[/]";

            string nameCell = (p == currentPlayer) ? $"[bold]{Esc(p.Name)}[/]" : Esc(p.Name);

            table.AddRow(nameCell, game.GetPlayerHand(p).Count.ToString(), p.Score.ToString(), status);
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    public static void RenderHand(List<ICard> hand)
    {
        AnsiConsole.MarkupLine("[bold]Kartu di tangan Anda:[/]");
        if (hand.Count == 0) return;

        var table = new Table()
            .NoBorder()
            .HideHeaders()
            .Collapse(); 

        for (int i = 0; i < hand.Count; i++)
            table.AddColumn(new TableColumn(string.Empty).NoWrap());

        table.AddRow(hand.Select((c, i) => (IRenderable)CardPanel(c, $"#{i}")).ToArray());
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    static Panel CardPanel(ICard card, string footer)
    {
        string colorName = ColorName(card.Color);
        var content = new Markup($"[bold {colorName}]{CardLabel(card)}[/]\n[grey]{footer}[/]");

        var panel = new Panel(Align.Center(content)) 
        .Border(BoxBorder.Rounded)
        .BorderColor(GetSpectreColor(card.Color))
        .Padding(2, 1, 2, 1);
    
        panel.Width = 14;  
        return panel;
        
    }

    static Color GetSpectreColor(CardColor? color) => color switch
    {
        CardColor.Red => Color.Red,
        CardColor.Blue => Color.Blue,
        CardColor.Green => Color.Green,
        CardColor.Yellow => Color.Yellow,
        _ => Color.Grey
    };

    public static string CardMarkup(ICard card)
    {
        string color = ColorName(card.Color);
        return $"[bold {color}]{CardLabel(card)}[/]";
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

