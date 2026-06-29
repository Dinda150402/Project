using System;
using System.Collections.Generic;
using System.Linq;
using Spectre.Console;
using Spectre.Console.Rendering;
using UnoGame.Core.Enums;
using UnoGame.Core.Interfaces;
using UnoGame.Core.Models;
using UnoGame.Core.Controllers;

class Program
{
    static void Main(string[] args)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("UNO CLI").Centered().Color(Color.Red));
        AnsiConsole.MarkupLine("[grey]Board edition — powered by Spectre.Console[/]\n");

        // Menampilkan fitur opsional input nama pemain dinamis
        List<IPlayer> players = new List<IPlayer>();
        bool useDefault = AnsiConsole.Confirm("Gunakan pemain default (Alice, Bob, Charlie, Jack)?");

        if (!useDefault)
        {
            int playerCount = AnsiConsole.Prompt(
                new TextPrompt<int>("Masukkan jumlah pemain (2-4):")
                    .Validate(n => n >= 2 && n <= 4
                        ? ValidationResult.Success()
                        : ValidationResult.Error("[red]Jumlah pemain harus 2-4[/]"))
            );

            for (int i = 1; i <= playerCount; i++)
            {
                string name = AnsiConsole.Ask<string>($"Masukkan nama pemain {i}: ");
                players.Add(new Player(name));
            }
        }
        else
        {
            players.Add(new Player("Alice"));
            players.Add(new Player("Bob"));
            players.Add(new Player("Charlie"));
            players.Add(new Player("Jack"));
        }

        // 1. Generate list kartu standar dengan poin lengkap
        List<ICard> startingCards = GenerateUnoDeck();

        // 2. Bungkus ke dalam objek DrawPile sebelum dikirim ke controller
        IDrawPile drawPile = new DrawPile(startingCards);

        // 3. Buat Instance GameController menggunakan IDrawPile
        GameController game = new GameController(players, drawPile);

        // State pengendali loop lokal
        bool isGameRunning = true;
        IPlayer? currentPlayer = null;

        // 4. SUBSCRIBE EVENT SEBELUM STARTGAME()
        game.OnTurnStarted += (player) =>
        {
            ShowIntermissionScreen(player);
            currentPlayer = player;
        };

        // Cek & minta UNO di sini (sebelum NextTurn/OnTurnStarted dipanggil GameController),
        // jadi prompt-nya pasti nempel ke pemain yang baru aja main, bukan pemain berikutnya.
        game.OnCardPlayed += (player, card) =>
        {
            AnsiConsole.MarkupLine($"🃏 [bold]{Esc(player.Name)}[/] memainkan kartu: {CardMarkup(card)}");

            if (game.GetUnoPendingPlayers().Contains(player))
            {
                bool calledUno = AnsiConsole.Confirm(
                    $"[yellow]{Esc(player.Name)}, kartu kamu tersisa 1! Teriak UNO sekarang?[/]");
                if (calledUno)
                {
                    game.CallUno(player);
                    AnsiConsole.MarkupLine("[green]✅ UNO berhasil dipanggil![/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]❌ Kamu lupa teriak UNO! Pemain lain bisa menangkapmu nanti.[/]");
                }
            }
        };

        game.OnRoundEnded += (player, score) =>
        {
            AnsiConsole.MarkupLine($"\n[green]🎉 Ronde selesai! {Esc(player.Name)} menang ronde ini (+{score} poin).[/]");
            AnsiConsole.MarkupLine($"[bold]Skor total {Esc(player.Name)}: {player.Score}[/]\n");
            PrintScoreboard(game);

            if (player.Score >= 500)
            {
                AnsiConsole.MarkupLine("\n[gold1]🔥 Skor 500 tercapai! Game akan segera berakhir...[/]");
                AnsiConsole.MarkupLine("[grey]Tekan ENTER untuk lihat hasil akhir...[/]");
                Console.ReadLine();
                return;
            }

        bool lanjut = AnsiConsole.Confirm("\nLanjut ke ronde berikutnya?");

        if (lanjut)
        {
            AnsiConsole.MarkupLine("[grey]Tekan ENTER kalau semua pemain udah siap...[/]");
            Console.ReadLine();
            game.StartNextRound(player); 
        }
        else
        {
            var leader = game.GetPlayers().OrderByDescending(p => p.Score).First();
            AnsiConsole.Write(new FigletText("STOP").Centered().Color(Color.Red));
            AnsiConsole.MarkupLine($"\n[bold yellow]🏆 Skor tertinggi saat dihentikan: {Esc(leader.Name)} dengan {leader.Score} poin![/]");
            isGameRunning = false;
        }
    };

        game.OnGameEnded += (winner) =>
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new FigletText("GAME OVER").Centered().Color(Color.Gold1));
            AnsiConsole.MarkupLine($"\n[bold yellow]🏆 Pemenang akhir: {Esc(winner.Name)} dengan {winner.Score} poin![/]");
            isGameRunning = false;
        };

        // 5. MEMULAI GAME
        game.StartGame();

        // 6. MAIN GAME LOOP
        while (isGameRunning)
        {
            if (currentPlayer == null) continue;

            RenderBoard(game, currentPlayer);

            // Catch Uno Violation
            var unoPending = game.GetUnoPendingPlayers().Where(p => p != currentPlayer).ToList();
            if (unoPending.Count > 0)
            {
                const string skipLabel = "Lewati, jangan tangkap siapa-siapa";
                var options = unoPending.Select(p => p.Name).Append(skipLabel).ToList();

                string pick = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[red]Ada pemain yang bisa ditangkap karena lupa teriak UNO![/]")
                        .AddChoices(options)
                );

                if (pick != skipLabel)
                {
                    IPlayer target = unoPending.First(p => p.Name == pick);
                    AnsiConsole.MarkupLine($"[green]Sukses! Anda menangkap {Esc(target.Name)}.[/]");
                    game.CatchUnoViolation(target);
                }
            }

            List<ICard> hand = game.GetPlayerHand(currentPlayer);
            RenderHand(hand);

            string actionChoice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .HighlightStyle(new Style(decoration: Decoration.Bold | Decoration.Underline))
                    .Title("Pilih tindakan:")
                    .AddChoices("🃏 Mainkan Kartu", "🂠 Ambil Kartu")
            );

            try
            {
                if (actionChoice == "🃏 Mainkan Kartu")
                {
                    ICard cardToPlay = AnsiConsole.Prompt(
                        new SelectionPrompt<ICard>()
                            .HighlightStyle(new Style(decoration: Decoration.Bold | Decoration.Underline))
                            .Title("Pilih kartu yang ingin dimainkan:")
                            .AddChoices(hand)
                            .UseConverter(c => $"{CardLabel(c)}  [{ColorName(c.Color)}]({ColorName(c.Color)})[/]")
                    );

                    CardColor? chosenColor = null;
                    if (cardToPlay.Value == CardValue.Wild || cardToPlay.Value == CardValue.WildDrawFour)
                    {
                        chosenColor = AnsiConsole.Prompt(
                            new SelectionPrompt<CardColor>()
                                .HighlightStyle(new Style(decoration: Decoration.Bold | Decoration.Underline))
                                .Title("Pilih warna baru:")
                                .AddChoices(CardColor.Red, CardColor.Blue, CardColor.Green, CardColor.Yellow)
                                .UseConverter(c => $"[{ColorName(c)}]{c}[/]")
                        );
                    }

                    game.PlayCard(currentPlayer, cardToPlay, chosenColor);
                }
                else // Ambil Kartu
                {
                    var validCards = game.GetValidCards(currentPlayer);
                    if (validCards.Count > 0)
                    {
                        AnsiConsole.MarkupLine("[yellow]⚠️ Anda masih punya kartu valid, tidak bisa mengambil kartu.[/]");
                        AnsiConsole.MarkupLine("[grey]Tekan ENTER untuk lanjut...[/]");
                        Console.ReadLine();
                    }
                    else
                    {
                        ICard drawnCard = game.DrawCard(currentPlayer);
                        AnsiConsole.MarkupLine($"\n🎴 Anda mengambil kartu: {CardMarkup(drawnCard)}");

                        var validCardsAfterDraw = game.GetValidCards(currentPlayer);

                        if (validCardsAfterDraw.Contains(drawnCard))
                        {
                            AnsiConsole.MarkupLine("[green]✅ Kartu ini valid dan akan otomatis dimainkan![/]");

                            CardColor? chosenColor = null;
                            if (drawnCard.Value == CardValue.Wild || drawnCard.Value == CardValue.WildDrawFour)
                            {
                                chosenColor = AnsiConsole.Prompt(
                                    new SelectionPrompt<CardColor>()
                                        .HighlightStyle(new Style(decoration: Decoration.Bold | Decoration.Underline))
                                        .Title("Pilih warna baru:")
                                        .AddChoices(CardColor.Red, CardColor.Blue, CardColor.Green, CardColor.Yellow)
                                        .UseConverter(c => $"[{ColorName(c)}]{c}[/]")
                                );
                            }

                            game.PlayCard(currentPlayer, drawnCard, chosenColor);
                        }
                        else
                        {
                            AnsiConsole.MarkupLine("[red]❌ Kartu tidak valid. Giliran Anda dilewati otomatis.[/]");
                            AnsiConsole.MarkupLine("[grey]Tekan ENTER untuk lanjut...[/]");
                            Console.ReadLine();
                            game.PassTurn(currentPlayer);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]⚠️ ATURAN MELANGGAR: {Markup.Escape(ex.Message)}[/]");
                AnsiConsole.MarkupLine("[grey]Tekan ENTER untuk lanjut...[/]");
                Console.ReadLine();
            }
        }
    }

    // ============== RENDERING HELPERS (Spectre.Console) ==============

    private static void RenderBoard(GameController game, IPlayer currentPlayer)
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

    private static void RenderHand(List<ICard> hand)
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

    private static Panel CardPanel(ICard card, string footer)
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

    private static Color GetSpectreColor(CardColor? color) => color switch
    {
        CardColor.Red => Color.Red,
        CardColor.Blue => Color.Blue,
        CardColor.Green => Color.Green,
        CardColor.Yellow => Color.Yellow,
        _ => Color.Grey
    };

    private static string CardMarkup(ICard card)
    {
        string color = ColorName(card.Color);
        return $"[bold {color}]{CardLabel(card)}[/]";
    }

    private static string ColorName(CardColor? color) => color switch
    {
        CardColor.Red => "red",
        CardColor.Blue => "blue",
        CardColor.Green => "green",
        CardColor.Yellow => "yellow",
        _ => "grey"
    };

    private static string CardLabel(ICard card) => card.Value switch
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

    private static string Esc(string s) => Markup.Escape(s ?? "");


    private static List<ICard> GenerateUnoDeck()
    {
        List<ICard> deck = new List<ICard>();
        CardColor[] colors = { CardColor.Red, CardColor.Blue, CardColor.Green, CardColor.Yellow };

        foreach (CardColor color in colors)
        {
            deck.Add(new Card(color, CardValue.Zero, 0));

            for (CardValue val = CardValue.One; val <= CardValue.Nine; val++)
            {
                int points = (int)val;
                deck.Add(new Card(color, val, points));
                deck.Add(new Card(color, val, points));
            }

            CardValue[] actions = { CardValue.Skip, CardValue.Reverse, CardValue.DrawTwo };
            foreach (CardValue act in actions)
            {
                deck.Add(new Card(color, act, 20));
                deck.Add(new Card(color, act, 20));
            }
        }

        for (int i = 0; i < 4; i++)
        {
            deck.Add(new Card(null, CardValue.Wild, 50));
            deck.Add(new Card(null, CardValue.WildDrawFour, 50));
        }

        return deck;
    }

    private static void PrintScoreboard(GameController game)
    {
        var table = new Table().Border(TableBorder.Rounded).Title("[bold]Skor Sementara[/]");
        table.AddColumn("Pemain");
        table.AddColumn("Skor");
        foreach (var p in game.GetPlayers().OrderByDescending(x => x.Score))
        {
            table.AddRow(Esc(p.Name), p.Score.ToString());
        }
        AnsiConsole.Write(table);
    }

    private static void ShowIntermissionScreen(IPlayer nextPlayer)
    {
        AnsiConsole.MarkupLine("\n[grey]Tekan ENTER untuk melanjutkan...[/]");
        Console.ReadLine();

        AnsiConsole.Clear();

        var panel = new Panel(
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
        AnsiConsole.MarkupLine("\n[grey]Tekan [bold]ENTER[/] jika Anda sudah siap untuk melihat kartu...[/]");
        Console.ReadLine();
        AnsiConsole.Clear();

        AnsiConsole.MarkupLine($"\n🔔 [bold]GILIRAN BARU:[/] Sekarang giliran [bold]{Esc(nextPlayer.Name)}[/]");
    }
}