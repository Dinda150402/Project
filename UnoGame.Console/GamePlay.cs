using Spectre.Console;
using UnoGame.Core.Controllers;
using UnoGame.Core.Enums;
using UnoGame.Core.Interfaces;
using UnoGame.Core.Models;

internal class GamePlay
{
    private readonly GameController _game;
    private bool _isRunning;
    private IPlayer? _currentPlayer;

    public GamePlay(GameController game)
    {
        _game = game;
        _isRunning = true;
        _currentPlayer = null;
    }
    public void Run()
    {
        SubscribeEvents();

        GameResult startResult = _game.StartGame();
        if (!startResult.Success)
        {
            AnsiConsole.MarkupLine(
                $"[red] Game tidak bisa dimulai: {Markup.Escape(startResult.ErrorMessage ?? "")}[/]");
            return;
        }
        GameLoop();
    }
    private void SubscribeEvents()
    {
        _game.OnTurnStarted += (player) =>
        {
            GameRenderer.ShowIntermissionScreen(player);
            _currentPlayer = player;
        };

        _game.OnCardPlayed += (player, card) =>
        {
            AnsiConsole.MarkupLine(
                $" [bold]{GameRenderer.Esc(player.Name)}[/] memainkan kartu: {GameRenderer.CardMarkup(card)}");

            if (_game.GetUnoPendingPlayers().Contains(player))
            {
                bool calledUno = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title($"[yellow]{GameRenderer.Esc(player.Name)}, kartu kamu tersisa 1![/]")
                        .AddChoices(" UNO!", " Lewati")
                ) == " UNO!";

                if (calledUno)
                {
                    GameResult unoResult = _game.CallUno(player);
                    if (unoResult.Success)
                    {
                        AnsiConsole.MarkupLine("[green] UNO berhasil dipanggil![/]");
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine(
                        "[red] Kamu lupa teriak UNO! Pemain lain bisa menangkapmu nanti.[/]");
                }
            }
        };
        _game.OnRoundEnded += (player, score) =>
        {
            AnsiConsole.MarkupLine(
                $"\n[green] Ronde selesai! {GameRenderer.Esc(player.Name)} menang ronde ini (+{score} poin).[/]");
            AnsiConsole.MarkupLine(
                $"[bold]Skor total {GameRenderer.Esc(player.Name)}: {player.Score}[/]\n");
            GameRenderer.PrintScoreboard(_game);

            if (player.Score >= 500)
            {
                AnsiConsole.MarkupLine("\n[gold1] Skor 500 tercapai! Game akan segera berakhir...[/]");
                AnsiConsole.MarkupLine("[grey]Tekan ENTER untuk lihat hasil akhir...[/]");
                Console.ReadLine();
                return;
            }

            bool lanjut = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("\nLanjut ke ronde berikutnya?")
                    .AddChoices(" Ya, lanjut ronde berikutnya", " Stop di sini")
            ) == " Ya, lanjut ronde berikutnya";

            if (lanjut)
            {
                AnsiConsole.MarkupLine("[grey]Tekan ENTER kalau semua pemain udah siap...[/]");
                Console.ReadLine();

                GameResult nextRoundResult = _game.StartNextRound(player);
                if (!nextRoundResult.Success)
                {
                    AnsiConsole.MarkupLine(
                        $"[red] Gagal mulai ronde berikutnya: {Markup.Escape(nextRoundResult.ErrorMessage ?? "")}[/]");
                    _isRunning = false;
                }
            }
            else
            {
                IPlayer leader = _game.GetPlayers()
                    .OrderByDescending(p => p.Score)
                    .First();

                AnsiConsole.Write(new FigletText("STOP").Centered().Color(Color.Red));
                AnsiConsole.MarkupLine(
                    $"\n[bold yellow] Skor tertinggi saat dihentikan: " +
                    $"{GameRenderer.Esc(leader.Name)} dengan {leader.Score} poin![/]");
                _isRunning = false;
            }
        };
        _game.OnGameEnded += (winner) =>
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new FigletText("GAME OVER").Centered().Color(Color.Gold1));
            AnsiConsole.MarkupLine(
                $"\n[bold yellow] Pemenang akhir: " +
                $"{GameRenderer.Esc(winner.Name)} dengan {winner.Score} poin![/]");
            _isRunning = false;
        };
    }
    private void GameLoop()
    {
        while (_isRunning)
        {
            IPlayer? current = _currentPlayer;
            if (current == null)
            {
                continue;
            }

            GameRenderer.RenderBoard(_game, current);
            HandleUnoViolationCatch(current);

            GameResult<List<ICard>> handResult = _game.GetPlayerHand(current);
            if (!handResult.Success)
            {
                continue;
            }

            List<ICard> hand = handResult.Value;
            GameRenderer.RenderHand(hand);

            string actionChoice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .HighlightStyle(new Style(decoration: Decoration.Bold | Decoration.Underline))
                    .Title("Pilih tindakan:")
                    .AddChoices(" Mainkan Kartu", " Ambil Kartu")
            );

            if (actionChoice == " Mainkan Kartu")
            {
                HandlePlayCard(current, hand);
            }
            else
            {
                HandleDrawCard(current);
            }
        }
    }
    private void HandleUnoViolationCatch(IPlayer currentPlayer)
    {
        List<IPlayer> unoPending = _game.GetUnoPendingPlayers()
            .Where(p => p != currentPlayer)
            .ToList();

        if (unoPending.Count == 0)
        {
            return;
        }

        const string skipLabel = "Lewati, jangan tangkap siapa-siapa";
        List<string> options = unoPending.Select(p => p.Name).Append(skipLabel).ToList();

        string pick = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[red]Ada pemain yang bisa ditangkap karena lupa teriak UNO![/]")
                .AddChoices(options)
        );

        if (pick == skipLabel)
        {
            return;
        }

        IPlayer target = unoPending.First(p => p.Name == pick);
        AnsiConsole.MarkupLine($"[green]Sukses! Anda menangkap {GameRenderer.Esc(target.Name)}.[/]");
        _game.CatchUnoViolation(target);
    }
    private void HandlePlayCard(IPlayer currentPlayer, List<ICard> hand)
    {
        ICard cardToPlay = AnsiConsole.Prompt(
            new SelectionPrompt<ICard>()
                .HighlightStyle(new Style(decoration: Decoration.Bold | Decoration.Underline))
                .Title("Pilih kartu yang ingin dimainkan:")
                .AddChoices(hand)
                .UseConverter(c => $"{GameRenderer.CardLabel(c)} ({c.Color?.ToString() ?? "Wild"})")
        );

        CardColor? chosenColor = null;
        if (cardToPlay.Value == CardValue.Wild || cardToPlay.Value == CardValue.WildDrawFour)
        {
            chosenColor = AnsiConsole.Prompt(
                new SelectionPrompt<CardColor>()
                    .HighlightStyle(new Style(decoration: Decoration.Bold | Decoration.Underline))
                    .Title("Pilih warna baru:")
                    .AddChoices(CardColor.Red, CardColor.Blue, CardColor.Green, CardColor.Yellow)
                    .UseConverter(c => $"[{GameRenderer.ColorName(c)}]{c}[/]")
            );
        }

        GameResult playResult = _game.PlayCard(currentPlayer, cardToPlay, chosenColor);
        if (!playResult.Success)
        {
            AnsiConsole.MarkupLine(
                $"[red] {Markup.Escape(playResult.ErrorMessage ?? "")}[/]");
            AnsiConsole.MarkupLine("[grey]Tekan ENTER untuk lanjut...[/]");
            Console.ReadLine();
        }
    }
    private void HandleDrawCard(IPlayer currentPlayer)
    {
        GameResult<List<ICard>> validCardsResult = _game.GetValidCards(currentPlayer);
        if (!validCardsResult.Success)
        {
            return;
        }

        List<ICard> validCards = validCardsResult.Value;
        if (validCards.Count > 0)
        {
            AnsiConsole.MarkupLine(
                "[yellow] Anda masih punya kartu valid, tidak bisa mengambil kartu.[/]");
            AnsiConsole.MarkupLine("[grey]Tekan ENTER untuk lanjut...[/]");
            Console.ReadLine();
            return;
        }

        GameResult<ICard> drawResult = _game.DrawCard(currentPlayer);
        if (!drawResult.Success)
        {
            AnsiConsole.MarkupLine(
                $"[red] {Markup.Escape(drawResult.ErrorMessage ?? "")}[/]");
            AnsiConsole.MarkupLine("[grey]Tekan ENTER untuk lanjut...[/]");
            Console.ReadLine();
            return;
        }

        ICard drawnCard = drawResult.Value;
        AnsiConsole.MarkupLine($"\n Anda mengambil kartu: {GameRenderer.CardMarkup(drawnCard)}");

        GameResult<List<ICard>> validAfterDrawResult = _game.GetValidCards(currentPlayer);
        if (!validAfterDrawResult.Success)
        {
            return;
        }

        List<ICard> validCardsAfterDraw = validAfterDrawResult.Value;
        if (validCardsAfterDraw.Contains(drawnCard))
        {
            AnsiConsole.MarkupLine("[green] Kartu ini valid dan akan otomatis dimainkan![/]");

            CardColor? chosenColor = null;
            if (drawnCard.Value == CardValue.Wild || drawnCard.Value == CardValue.WildDrawFour)
            {
                chosenColor = AnsiConsole.Prompt(
                    new SelectionPrompt<CardColor>()
                        .HighlightStyle(new Style(decoration: Decoration.Bold | Decoration.Underline))
                        .Title("Pilih warna baru:")
                        .AddChoices(CardColor.Red, CardColor.Blue, CardColor.Green, CardColor.Yellow)
                        .UseConverter(c => $"[{GameRenderer.ColorName(c)}]{c}[/]")
                );
            }

            GameResult playResult = _game.PlayCard(currentPlayer, drawnCard, chosenColor);
            if (!playResult.Success)
            {
                AnsiConsole.MarkupLine(
                    $"[red] {Markup.Escape(playResult.ErrorMessage ?? "")}[/]");
                AnsiConsole.MarkupLine("[grey]Tekan ENTER untuk lanjut...[/]");
                Console.ReadLine();
            }
        }
        else
        {
            AnsiConsole.MarkupLine("[red] Kartu tidak valid. Giliran Anda dilewati otomatis.[/]");
            AnsiConsole.MarkupLine("[grey]Tekan ENTER untuk lanjut...[/]");
            Console.ReadLine();

            GameResult passResult = _game.PassTurn(currentPlayer);
            if (!passResult.Success)
            {
                AnsiConsole.MarkupLine(
                    $"[red] {Markup.Escape(passResult.ErrorMessage ?? "")}[/]");
            }
        }
    }
}