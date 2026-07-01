🎴 UnoGame
Implementasi permainan kartu UNO menggunakan C# (.NET 8), dengan tampilan CLI interaktif berbasis
Spectre.Console. Project ini dibangun dengan pemisahan arsitektur yang rapi antara logika game
(core), antarmuka console, dan skeleton REST API.
✨ Fitur
♟️ Aturan UNO lengkap: kartu angka, Skip, Reverse, Draw Two, Wild, dan Wild Draw Four
🔄 Arah permainan (searah/berlawanan jarum jam), otomatis berlaku seperti Skip saat hanya 2
pemain
️ Mekanisme panggil "UNO!" saat kartu tersisa satu — lupa memanggil bisa "ditangkap" pemain
lain dan kena penalti 2 kartu
🏆 Sistem skor multi-ronde hingga salah satu pemain mencapai 500 poin, dengan papan skor
berjalan
🎨 Tampilan CLI kaya lewat Spectre.Console: kartu berwarna, panel, tabel status pemain, dan
banner Figlet
👥 Mode hotseat lokal untuk 2-4 pemain, lengkap dengan layar "pergantian giliran" agar pemain lain
tidak mengintip kartu
‍‍ Setup pemain default (Alice, Bob, Charlie, Jack) atau nama custom
️ Struktur Project
Project-main/
├── UnoGame.Core/ # Class library — logika inti game (engine)
│ ├── Controllers/
│ │ └── GameControllers.cs # State machine utama permainan
│ ├── Enums/
│ │ ├── CardColor.cs
│ │ ├── CardValue.cs
│ │ └── GameDirection.cs
│ ├── Interfaces/
│ │ ├── ICard.cs
│ │ ├── IDiscardPile.cs
│ │ ├── IDrawPile.cs
│ │ └── IPlayer.cs
│ └── Models/
│ ├── Card.cs
│ ├── DiscardPile.cs

│ ├── DrawPile.cs
│ ├── GameResult.cs
│ └── Player.cs
│
├── UnoGame.Console/ # Aplikasi CLI (Spectre.Console)
│ ├── DeckFacory.cs # Generator 108 kartu standar UNO
│ ├── GamePlay.cs # Game loop & event handling
│ ├── GameRenderer.cs # Semua tampilan board/kartu di terminal
│ ├── PlayerSetup.cs # Setup jumlah & nama pemain
│ └── Program.cs

Alur ketergantungan: UnoGame.Console mereferensikan
UnoGame.Core , sehingga logika game terpusat dan bisa dipakai ulang di antarmuka mana pun.
️ Teknologi

Komponen Teknologi
Bahasa & Runtime C# / .NET 8
Tampilan CLI Spectre.Console 0.57.1
API (WIP) ASP.NET Core + Swashbuckle (Swagger)

🚀 Cara Menjalankan
Prasyarat
.NET SDK 8.0 atau lebih baru
Menjalankan versi Console 
cd UnoGame.Console
dotnet run

🎮 Cara Bermain
1. Saat aplikasi dijalankan, pilih mode pemain: default (Alice, Bob, Charlie, Jack) atau custom (2-4
pemain dengan nama sendiri).
2. Setiap pemain mendapat 7 kartu di awal ronde.
3. Pada giliran masing-masing, pilih "🃏 Mainkan Kartu" atau "🂠 Ambil Kartu".
4. Jika kartu di tangan tersisa satu, kamu wajib memanggil "UNO!" — jika lupa, pemain lain
berkesempatan menangkapmu dan kamu akan mendapat 2 kartu tambahan sebagai penalti.
5. Layar "pergantian giliran" akan muncul di antara giliran, memberi jeda agar device bisa diserahkan
tanpa pemain lain mengintip kartu.
6. Ronde berakhir saat kartu seorang pemain habis; skor ronde dihitung dari total nilai kartu yang
tersisa di tangan pemain lain.
7. Permainan berakhir saat skor total seorang pemain mencapai 500 poin.
🧮 Aturan Skor Kartu

Jenis Kartu Poin
Angka 0-9 sesuai nilai angkanya
Skip / Reverse / Draw Two 20
Wild / Wild Draw Four 50

Total satu deck berisi 108 kartu, mengikuti komposisi kartu UNO resmi (4 warna × 25 kartu + 8 kartu
Wild).
📸 Screenshot
![Alt text](images/screenshot1.png)
![Alt text](images/screenshot2.png)
![Alt text](images/screenshot3.png)
![Alt text](images/screenshot4.png)
![Alt text](images/screenshot5.png)
![Alt text](images/screenshot6.png)
