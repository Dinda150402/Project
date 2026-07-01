using UnoGame.Core.Controllers;
using UnoGame.Core.Interfaces;
using UnoGame.Core.Models;

List<IPlayer> players = PlayerSetup.Setup();
List<ICard> deck = DeckFactory.GenerateUnoDeck();
IDrawPile drawPile = new DrawPile(deck);
IDiscardPile discardPile = new DiscardPile();
GameController game = new GameController(players, drawPile, discardPile);
GamePlay gamePlay = new GamePlay(game);
gamePlay.Run();