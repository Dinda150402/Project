using System;
using System.Collections.Generic;
using System.Linq;
using Spectre.Console;
using UnoGame.Core.Enums;
using UnoGame.Core.Interfaces;
using UnoGame.Core.Models;
using UnoGame.Core.Controllers;

class Program
{
    static void Main(string[] args)
    {
        Console.Clear();
        Console.WriteLine("=======================================");
        Console.WriteLine("        SELAMAT DATANG DI UNO CLI      ");
        Console.WriteLine("=======================================");

        // Menampilkan fitur opsional input nama pemain dinamis
        List<IPlayer> players = new List<IPlayer>();
        Console.Write("Gunakan pemain default (Alice, Bob, Charlie, Jack)? (y/n): ");
        string useDefault = Console.ReadLine()?.Trim().ToLower() ?? "";

        if (useDefault == "n")
        {
            int playerCount = 0;
            while (playerCount < 2 || playerCount > 4)
            {
                Console.Write("Masukkan jumlah pemain (2-4): ");
                int.TryParse(Console.ReadLine(), out playerCount);
            }

            for (int i = 1; i <= playerCount; i++)
            {
                Console.Write($"Masukkan nama pemain {i}: ");
                string name = Console.ReadLine()?.Trim() ?? "";
                players.Add(new Player(string.IsNullOrEmpty(name) ? $"Player {i}" : name));
            }
        }
        /*AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("UNO CLI").Centered().Color(Color.Red));
        AnsiConsole.MarkupLine ("[grey]Board edition - powered by Spectre.Console[/]\n");

        List<IPlayer> players = new List<IPLayer>();
        bool useDefault = AnsiConsole.Confirm("Gunakan pemain default (Alice, Bob, Charlie, Jack)?")

        if(!useDefault)
        {
            
        }
        */
        else
        {
            players.Add(new Player("Alice"));
            players.Add(new Player("Bob"));
            players.Add(new Player("Charlie"));
            players.Add(new Player("Jack"));
        }

        // 1. Generate list kartu standar dengan poin lengkap (Fix Error 2)
        List<ICard> startingCards = GenerateUnoDeck();

        // 2. Bungkus ke dalam objek DrawPile sebelum dikirim ke controller (Fix Error 1)
        IDrawPile drawPile = new DrawPile(startingCards);

        // 3. Buat Instance GameController menggunakan IDrawPile
        GameController game = new GameController(players, drawPile);

        // State pengendali loop lokal
        bool isGameRunning = true;
        IPlayer? currentPlayer = null;

        // 4. SUBSCRIBE EVENT SEBELUM STARTGAME()
        game.OnTurnStarted += (player) => {
            ShowIntermissionScreen(player);
            
            currentPlayer = player;
            Console.WriteLine($"\n🔔 GILIRAN BARU: Sekarang giliran [{player.Name}]");
        };

        game.OnCardPlayed += (player, card) => {
            Console.WriteLine($"🃏 {player.Name} memainkan kartu: {GetCardName(card)}");

            if (game.GetUnoPendingPlayers().Contains(player))
                        {
                        Console.Write("⚠️ Kamu punya 1 kartu! Ketik UNO dan tekan Enter: ");
                        string unoInput = Console.ReadLine() ?? "";
                        if (unoInput.Trim().ToUpper() == "UNO")
                            game.CallUno(player);
                        else
                            Console.WriteLine("❌ Kamu lupa teriak UNO! Pemain lain bisa menangkapmu.");
                        }
        };

        game.OnGameEnded += (winner) => {
            Console.WriteLine($"\n🏆 GAME OVER! Pemenangnya adalah {winner.Name}!");
            isGameRunning = false;
        };

        game.OnRoundEnded += (player, score) => {
            Console.WriteLine($"\n🎉 Ronde selesai! {player.Name} menang ronde ini dengan {score} poin!");
            Console.WriteLine($"Skor total {player.Name}: {player.Score}");
            Console.WriteLine("Memulai ronde baru...");
            Console.ReadLine();
        };

        // 5. MEMULAI GAME
        game.StartGame();

        // 6. MAIN GAME LOOP
        while (isGameRunning)
        {
            if (currentPlayer == null) continue;

            Console.WriteLine("---------------------------------------");
            Console.WriteLine($"Kartu Atas Arena: {GetCardName(game.GetTopDiscardCard())}");
            Console.WriteLine($"Warna Aktif Saat Ini: {game.GetCurrentColor()}"); // Menggunakan method getter (Fix Error 3)
            Console.WriteLine("---------------------------------------");

            //Catch Uno Violation
            var unoPending = game.GetUnoPendingPlayers().Where(p => p != currentPlayer).ToList();

            if(unoPending.Count > 0)
                {
                    Console.WriteLine("Daftar Pemain yang Bisa Ditangkap: ");
                    for(int i = 0; i < unoPending.Count; i++)
                    {
                        Console.WriteLine($"{i + 1} . {unoPending[i].Name}");
                    }

                    Console.WriteLine("0. Lewati, jangan tangkap siapa-siapa");
                        
                    int pilihanIndex = -1;
                    bool inputValid = false;

                    while (!inputValid)
                    {
                        Console.Write($"Masukkan Index Pemain (1. {unoPending.Count})");
                        string userInput = (Console.ReadLine() ?? "").Trim();

                        if (int.TryParse(userInput, out pilihanIndex) && pilihanIndex >= 0 && pilihanIndex <= unoPending.Count)
                        {       
                            if (pilihanIndex == 0)
                            {
                                Console.WriteLine("Lewati...");
                                break;
                            }
                            inputValid = true;
                            IPlayer pemainTerpilih = unoPending[pilihanIndex -1];
                            Console.WriteLine($"\nSukses! Anda memilih untuk menangkap: {pemainTerpilih.Name}");
                            game.CatchUnoViolation(pemainTerpilih);
                        }
                        else
                        {
                            Console.WriteLine("Input tidak valid! Silakan masukkan nomor yang tertera pada daftar.");
                        }
                    }
                        
                }

            // Tampilkan kartu di tangan pemain aktif
            List<ICard> hand = game.GetPlayerHand(currentPlayer);
            Console.WriteLine("Kartu di tangan Anda:");
            for (int i = 0; i < hand.Count; i++)
            {
                Console.WriteLine($"[{i}] {GetCardName(hand[i])}");
            }

            // Tampilkan Menu Pilihan Aksi
            Console.WriteLine("\nPilih tindakan:");
            Console.WriteLine("1. Mainkan Kartu (Play)");
            Console.WriteLine("2. Ambil Kartu (Draw)");
            Console.Write("Masukkan nomor aksi (1, 2): ");
            string choice = Console.ReadLine() ?? "";

            try
            {
                if (choice == "1")
                {
                    
                    Console.Write("Masukkan nomor indeks kartu yang ingin dimainkan: ");
                    if (int.TryParse(Console.ReadLine(), out int cardIndex) && cardIndex >= 0 && cardIndex < hand.Count)
                    {
                        ICard cardToPlay = hand[cardIndex];

                        // Tangani penentuan warna jika mengeluarkan kartu Wild
                        CardColor? chosenColor = null;
                        if (cardToPlay.Value == CardValue.Wild || cardToPlay.Value == CardValue.WildDrawFour)
                        {
                            Console.WriteLine("Pilih warna baru (Red, Blue, Green, Yellow):");
                            string colorInput = Console.ReadLine() ?? "";
                            if (Enum.TryParse(colorInput, true, out CardColor parsedColor))
                            {
                                chosenColor = parsedColor;
                            }
                            else
                            {
                                chosenColor = CardColor.Red; // Default fallback aman
                            }
                        }

                        IPlayer playerYangMain = currentPlayer;

                        game.PlayCard(playerYangMain, cardToPlay, chosenColor);

                    }
                    else
                    {
                        Console.WriteLine("❌ Indeks kartu tidak valid!");
                    }
                }
                else if (choice == "2")
                {
                    var validCards = game.GetValidCards(currentPlayer);
                    if(validCards.Count > 0)
                    {
                        Console.WriteLine("⚠️ Anda masih memiliki kartu yang valid untuk dimainkan. Anda tidak dapat mengambil kartu.");
                    }
                    else
                    {
                        ICard drawnCard = game.DrawCard(currentPlayer);
                        Console.WriteLine($"🃏 Anda menarik kartu: {GetCardName(drawnCard)}");

                        var validCardsAfterDraw = game.GetValidCards(currentPlayer);

                        if(validCardsAfterDraw.Contains(drawnCard))
                        {
                            ICard cardToPlay = drawnCard;

                            //Tangani penentuan warna jika yang keluar kartu wild
                            CardColor? chosenColor = null;
                            if (cardToPlay.Value == CardValue.Wild || cardToPlay.Value == CardValue.WildDrawFour)
                            {
                                Console.WriteLine("Pilih warna baru (Red, Blue, Green, Yellow):");
                                string colorInput = Console.ReadLine() ?? "";
                                if (Enum.TryParse(colorInput, true, out CardColor parsedColor))
                                {
                                    chosenColor = parsedColor;
                                }
                                else
                                {
                                    chosenColor = CardColor.Red;
                                }   
                            }

                        IPlayer playerYangMain = currentPlayer;
                        
                        game.PlayCard(currentPlayer, cardToPlay, chosenColor);
                        }
                        

                        else
                        {
                            game.PassTurn(currentPlayer);
                        }
                    }
                }
                
                else
                {
                    Console.WriteLine("❌ Pilihan menu tidak valid!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ ATURAN MELANGGAR: {ex.Message}");
            }
        }
    }

    // Helper Generator Deck 108 Kartu dengan Parameter Poin (Fix Error 2)
    private static List<ICard> GenerateUnoDeck()
    {
        List<ICard> deck = new List<ICard>();
        CardColor[] colors = { CardColor.Red, CardColor.Blue, CardColor.Green, CardColor.Yellow };

        foreach (CardColor color in colors)
        {
            // Angka 0 bernilai 0 poin
            deck.Add(new Card(color, CardValue.Zero, 0));

            // Angka 1-9 bernilai sesuai angkanya
            for (CardValue val = CardValue.One; val <= CardValue.Nine; val++)
            {
                int points = (int)val;
                deck.Add(new Card(color, val, points));
                deck.Add(new Card(color, val, points));
            }
 
            // Kartu aksi berwarna bernilai 20 poin
            CardValue[] actions = { CardValue.Skip, CardValue.Reverse, CardValue.DrawTwo };
            foreach (CardValue act in actions)
            {
                deck.Add(new Card(color, act, 20));
                deck.Add(new Card(color, act, 20));
            }
        }

        // Kartu Wild bernilai 50 poin
        for (int i = 0; i < 4; i++)
        {
            deck.Add(new Card(null, CardValue.Wild, 50));
            deck.Add(new Card(null, CardValue.WildDrawFour, 50));
        }

        return deck;
    }

    private static string GetCardName(ICard card)
    {
        if (card == null) return "Kosong";
        string colorName = card.Color.HasValue ? card.Color.Value.ToString() : "Wild";
        return $"[{colorName} - {card.Value}]";
    }

    private static void ShowIntermissionScreen(IPlayer nextPlayer)
    {
        Console.WriteLine("Tekan ENTER: ");
        Console.ReadKey();
        
        // 1. Bersihkan seluruh layar dari kartu pemain sebelumnya
        Console.Clear();

        // 2. Tampilkan pesan penutup/pembatas yang besar
        Console.WriteLine("=================================================");
        Console.WriteLine("               PERGANTIAN GILIRAN                ");
        Console.WriteLine("=================================================");
        Console.WriteLine("\n\n");
        Console.WriteLine($"      HARAP SERAHKAN KOMPUTER KEPADA: [{nextPlayer.Name}]");
        Console.WriteLine("\n\n");
        Console.WriteLine("=================================================");
        Console.WriteLine("Pemain sebelumnya dilarang mengintip layar!");
        Console.Write("Tekan [ENTER] jika Anda sudah siap untuk melihat kartu...");
    
    // 3. Kunci program di sini sampai pemain berikutnya menekan tombol Enter
        Console.ReadLine(); 
    
    // 4. Bersihkan layar lagi sebelum kartu asli digambar
        Console.Clear();
    }

    
}