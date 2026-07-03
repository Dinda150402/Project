using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Formatting.Compact;
using UnoGame.Core.Controllers;
using UnoGame.Core.Interfaces;
using UnoGame.Core.Models;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "UnoGame.Console")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        new CompactJsonFormatter(),
        path: "logs/unogame-.log",
        rollingInterval: RollingInterval.Day)
    .CreateLogger();

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog(Log.Logger));
ILogger<GameController> gameLogger = loggerFactory.CreateLogger<GameController>();

try
{
    Log.Information("Aplikasi UnoGame.Console dimulai");

    List<IPlayer> players = PlayerSetup.Setup();
    List<ICard> deck = DeckFactory.GenerateUnoDeck();
    IDrawPile drawPile = new DrawPile(deck);
    IDiscardPile discardPile = new DiscardPile();
    GameController game = new GameController(players, drawPile, discardPile, logger: gameLogger);
    GamePlay gamePlay = new GamePlay(game);
    gamePlay.Run();

    Log.Information("Aplikasi UnoGame.Console selesai secara normal");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Aplikasi UnoGame.Console berhenti karena exception yang tidak tertangani");
}
finally
{
    Log.CloseAndFlush();
}
