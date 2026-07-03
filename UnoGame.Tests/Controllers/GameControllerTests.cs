using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using UnoGame.Core.Controllers;
using UnoGame.Core.Enums;
using UnoGame.Core.Interfaces;
using UnoGame.Core.Models;
using UnoGame.Tests.TestHelpers;
using static UnoGame.Tests.TestHelpers.DeckBuilder;

namespace UnoGame.Tests.Controllers;

[TestFixture]
public class GameControllerTests
{
    private IPlayer _alice = null!;
    private IPlayer _bob = null!;

    [SetUp]
    public void SetUp()
    {
        _alice = new Player("Alice");
        _bob = new Player("Bob");
    }

    private static GameController CreateController(List<IPlayer> players, IDrawPile drawPile)
    {
        IDiscardPile discardPile = new DiscardPile();
        return new GameController(
            players,
            drawPile,
            discardPile,
            shuffler: new NoOpShuffler(),
            logger: NullLogger<GameController>.Instance);
    }

    [Test]
    public void StartGame_WithLessThanTwoPlayers_ReturnsFailure()
    {
        IDrawPile drawPile = BuildDrawPileForTwoPlayers(
            player1Hand: FlatCards(1, CardColor.Red, CardValue.Zero),
            player2Hand: new List<ICard>(),
            starterCard: Card(CardColor.Red, CardValue.One, 1));
        GameController controller = CreateController(new List<IPlayer> { _alice }, drawPile);

        GameResult result = controller.StartGame();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("Jumlah Player Minimal 2 orang"));
        });
    }

    [Test]
    public void StartGame_WithTwoPlayers_DealsSevenCardsToEachPlayer()
    {
        List<ICard> aliceHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        List<ICard> bobHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        ICard starter = Card(CardColor.Red, CardValue.Five, 5);
        IDrawPile drawPile = BuildDrawPileForTwoPlayers(aliceHand, bobHand, starter);
        GameController controller = CreateController(new List<IPlayer> { _alice, _bob }, drawPile);

        GameResult result = controller.StartGame();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(controller.GetPlayerHand(_alice).Value, Has.Count.EqualTo(7));
            Assert.That(controller.GetPlayerHand(_bob).Value, Has.Count.EqualTo(7));
            Assert.That(controller.GetTopDiscardCard(), Is.EqualTo(starter));
            Assert.That(controller.GetCurrentPlayer(), Is.EqualTo(_alice));
        });
    }

    [Test]
    public void StartGame_WithReverseStarterCardAndTwoPlayers_ActsLikeSkip()
    {
        List<ICard> aliceHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        List<ICard> bobHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        ICard starter = Card(CardColor.Red, CardValue.Reverse, 20);
        IDrawPile drawPile = BuildDrawPileForTwoPlayers(aliceHand, bobHand, starter);
        GameController controller = CreateController(new List<IPlayer> { _alice, _bob }, drawPile);

        controller.StartGame();

        Assert.That(controller.GetCurrentPlayer(), Is.EqualTo(_bob));
    }

    [Test]
    public void PlayCard_WhenNotPlayersTurn_ReturnsFailure()
    {
        List<ICard> aliceHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        List<ICard> bobHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        ICard starter = Card(CardColor.Red, CardValue.Five, 5);
        IDrawPile drawPile = BuildDrawPileForTwoPlayers(aliceHand, bobHand, starter);
        GameController controller = CreateController(new List<IPlayer> { _alice, _bob }, drawPile);
        controller.StartGame();

        ICard cardFromBobHand = controller.GetPlayerHand(_bob).Value[0];
        GameResult result = controller.PlayCard(_bob, cardFromBobHand, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("It's not your turn yet"));
        });
    }

    [Test]
    public void PlayCard_WhenCardNotInHand_ReturnsFailure()
    {
        List<ICard> aliceHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        List<ICard> bobHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        ICard starter = Card(CardColor.Red, CardValue.Five, 5);
        IDrawPile drawPile = BuildDrawPileForTwoPlayers(aliceHand, bobHand, starter);
        GameController controller = CreateController(new List<IPlayer> { _alice, _bob }, drawPile);
        controller.StartGame();

        ICard cardNotInHand = Card(CardColor.Green, CardValue.Seven, 7);
        GameResult result = controller.PlayCard(_alice, cardNotInHand, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("You don't have that card"));
        });
    }

    [Test]
    public void PlayCard_WhenColorAndValueDoNotMatch_ReturnsFailure()
    {
        List<ICard> aliceHand = new List<ICard> { Card(CardColor.Green, CardValue.Two, 2) };
        aliceHand.AddRange(FlatCards(6, CardColor.Green, CardValue.Two, 2));
        List<ICard> bobHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        ICard starter = Card(CardColor.Red, CardValue.Five, 5);
        IDrawPile drawPile = BuildDrawPileForTwoPlayers(aliceHand, bobHand, starter);
        GameController controller = CreateController(new List<IPlayer> { _alice, _bob }, drawPile);
        controller.StartGame();

        GameResult result = controller.PlayCard(_alice, aliceHand[0], null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("There is no match in your cards"));
        });
    }

    [Test]
    public void PlayCard_WhenColorMatches_SucceedsAndPassesTurn()
    {
        List<ICard> aliceHand = new List<ICard> { Card(CardColor.Red, CardValue.Seven, 7) };
        aliceHand.AddRange(FlatCards(6, CardColor.Blue, CardValue.One, 1));
        List<ICard> bobHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        ICard starter = Card(CardColor.Red, CardValue.Five, 5);
        IDrawPile drawPile = BuildDrawPileForTwoPlayers(aliceHand, bobHand, starter);
        GameController controller = CreateController(new List<IPlayer> { _alice, _bob }, drawPile);
        controller.StartGame();

        GameResult result = controller.PlayCard(_alice, aliceHand[0], null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(controller.GetTopDiscardCard(), Is.EqualTo(aliceHand[0]));
            Assert.That(controller.GetPlayerHand(_alice).Value, Has.Count.EqualTo(6));
            Assert.That(controller.GetCurrentPlayer(), Is.EqualTo(_bob));
        });
    }

    [Test]
    public void PlayCard_FullRound_EndsRoundAndAwardsRemainingCardPoints()
    {
        List<ICard> aliceHand = Enumerable.Range(0, 7)
            .Select(v => Card(CardColor.Red, (CardValue)v, v))
            .ToList();
        List<ICard> bobHand = Enumerable.Range(0, 7)
            .Select(v => Card(CardColor.Red, (CardValue)v, v))
            .ToList();
        ICard starter = Card(CardColor.Red, CardValue.Seven, 7);

        IDrawPile drawPile = BuildDrawPileForTwoPlayers(aliceHand, bobHand, starter);
        GameController controller = CreateController(new List<IPlayer> { _alice, _bob }, drawPile);
        controller.StartGame();

        IPlayer? roundWinner = null;
        int roundScore = -1;
        controller.OnRoundEnded += (player, score) =>
        {
            roundWinner = player;
            roundScore = score;
        };

        int aliceIndex = 0;
        int bobIndex = 0;
        while (roundWinner == null)
        {
            IPlayer current = controller.GetCurrentPlayer();
            if (current == _alice)
            {
                controller.PlayCard(_alice, aliceHand[aliceIndex], null);
                aliceIndex++;
            }
            else
            {
                controller.PlayCard(_bob, bobHand[bobIndex], null);
                bobIndex++;
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(roundWinner, Is.EqualTo(_alice));
            Assert.That(roundScore, Is.EqualTo(6));
            Assert.That(_alice.Score, Is.EqualTo(6));
        });
    }

    [Test]
    public void DrawCard_WhenNotPlayersTurn_ReturnsFailure()
    {
        List<ICard> aliceHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        List<ICard> bobHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        ICard starter = Card(CardColor.Red, CardValue.Five, 5);
        List<ICard> reserve = FlatCards(3, CardColor.Green, CardValue.Two, 2);
        IDrawPile drawPile = BuildDrawPileForTwoPlayers(aliceHand, bobHand, starter, reserve);
        GameController controller = CreateController(new List<IPlayer> { _alice, _bob }, drawPile);
        controller.StartGame();

        GameResult<ICard> result = controller.DrawCard(_bob);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("It's not your turn yet"));
        });
    }

    [Test]
    public void DrawCard_CalledTwiceInSameTurn_SecondCallFails()
    {
        List<ICard> aliceHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        List<ICard> bobHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        ICard starter = Card(CardColor.Red, CardValue.Five, 5);
        List<ICard> reserve = FlatCards(3, CardColor.Green, CardValue.Two, 2);
        IDrawPile drawPile = BuildDrawPileForTwoPlayers(aliceHand, bobHand, starter, reserve);
        GameController controller = CreateController(new List<IPlayer> { _alice, _bob }, drawPile);
        controller.StartGame();

        GameResult<ICard> first = controller.DrawCard(_alice);
        GameResult<ICard> second = controller.DrawCard(_alice);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(second.Success, Is.False);
            Assert.That(second.ErrorMessage, Is.EqualTo("You already drawn a card"));
            Assert.That(controller.GetPlayerHand(_alice).Value, Has.Count.EqualTo(8));
        });
    }

    [Test]
    public void CallUno_WhenNotEligible_ReturnsFailure()
    {
        List<ICard> aliceHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        List<ICard> bobHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        ICard starter = Card(CardColor.Red, CardValue.Five, 5);
        IDrawPile drawPile = BuildDrawPileForTwoPlayers(aliceHand, bobHand, starter);
        GameController controller = CreateController(new List<IPlayer> { _alice, _bob }, drawPile);
        controller.StartGame();

        GameResult result = controller.CallUno(_alice);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("You cannot call UNO now"));
        });
    }

    [Test]
    public void CallUno_WhenPlayerHasOneCardLeft_Succeeds()
    {
        GameController controller = PlayUntilAliceIsUnoPending(out _, out _);

        GameResult result = controller.CallUno(_alice);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(controller.GetUnoPendingPlayers(), Does.Not.Contain(_alice));
        });
    }

    [Test]
    public void CatchUnoViolation_WhenPlayerForgotToCallUno_AppliesTwoCardPenalty()
    {
        GameController controller = PlayUntilAliceIsUnoPending(out _, out _);
        int handCountBefore = controller.GetPlayerHand(_alice).Value.Count;

        bool caught = controller.CatchUnoViolation(_alice);
        int handCountAfter = controller.GetPlayerHand(_alice).Value.Count;

        Assert.Multiple(() =>
        {
            Assert.That(caught, Is.True);
            Assert.That(handCountBefore, Is.EqualTo(1));
            Assert.That(handCountAfter, Is.EqualTo(3));
            Assert.That(controller.GetUnoPendingPlayers(), Does.Not.Contain(_alice));
        });
    }

    private GameController PlayUntilAliceIsUnoPending(out List<ICard> aliceHand, out List<ICard> bobHand)
    {
        aliceHand = Enumerable.Range(0, 7).Select(v => Card(CardColor.Red, (CardValue)v, v)).ToList();
        bobHand = Enumerable.Range(0, 7).Select(v => Card(CardColor.Red, (CardValue)v, v)).ToList();
        ICard starter = Card(CardColor.Red, CardValue.Seven, 7);
        List<ICard> reserve = FlatCards(5, CardColor.Yellow, CardValue.Three, 3);

        IDrawPile drawPile = BuildDrawPileForTwoPlayers(aliceHand, bobHand, starter, reserve);
        GameController controller = CreateController(new List<IPlayer> { _alice, _bob }, drawPile);
        controller.StartGame();

        int aliceIndex = 0;
        int bobIndex = 0;
        while (!controller.GetUnoPendingPlayers().Contains(_alice))
        {
            IPlayer current = controller.GetCurrentPlayer();
            if (current == _alice)
            {
                controller.PlayCard(_alice, aliceHand[aliceIndex], null);
                aliceIndex++;
            }
            else
            {
                controller.PlayCard(_bob, bobHand[bobIndex], null);
                bobIndex++;
            }
        }

        return controller;
    }

    [Test]
    public void PassTurn_WhenNotPlayersTurn_ReturnsFailure()
    {
        List<ICard> aliceHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        List<ICard> bobHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        ICard starter = Card(CardColor.Red, CardValue.Five, 5);
        List<ICard> reserve = FlatCards(3, CardColor.Green, CardValue.Two, 2);
        IDrawPile drawPile = BuildDrawPileForTwoPlayers(aliceHand, bobHand, starter, reserve);
        GameController controller = CreateController(new List<IPlayer> { _alice, _bob }, drawPile);
        controller.StartGame();

        GameResult result = controller.PassTurn(_bob);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("It's not your turn yet"));
        });
    }

    [Test]
    public void PassTurn_WhenPlayerHasNotDrawnACard_ReturnsFailure()
    {
        List<ICard> aliceHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        List<ICard> bobHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        ICard starter = Card(CardColor.Red, CardValue.Five, 5);
        List<ICard> reserve = FlatCards(3, CardColor.Green, CardValue.Two, 2);
        IDrawPile drawPile = BuildDrawPileForTwoPlayers(aliceHand, bobHand, starter, reserve);
        GameController controller = CreateController(new List<IPlayer> { _alice, _bob }, drawPile);
        controller.StartGame();

        GameResult result = controller.PassTurn(_alice);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("You need to draw a card first"));
        });
    }

    [Test]
    public void PassTurn_AfterDrawingCard_SucceedsAndMovesToNextPlayer()
    {
        List<ICard> aliceHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        List<ICard> bobHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        ICard starter = Card(CardColor.Red, CardValue.Five, 5);
        List<ICard> reserve = FlatCards(3, CardColor.Green, CardValue.Two, 2);
        IDrawPile drawPile = BuildDrawPileForTwoPlayers(aliceHand, bobHand, starter, reserve);
        GameController controller = CreateController(new List<IPlayer> { _alice, _bob }, drawPile);
        controller.StartGame();
        controller.DrawCard(_alice);

        GameResult result = controller.PassTurn(_alice);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(controller.GetCurrentPlayer(), Is.EqualTo(_bob));
        });
    }

    [Test]
    public void StartNextRound_WithPreviousRoundWinner_SetsWinnerAsStartingPlayer()
    {
        List<ICard> aliceHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        List<ICard> bobHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        ICard starter = Card(CardColor.Red, CardValue.Five, 5);
        IDrawPile drawPile = BuildDrawPileForTwoPlayers(aliceHand, bobHand, starter);
        GameController controller = CreateController(new List<IPlayer> { _alice, _bob }, drawPile);
        controller.StartGame();

        GameResult result = controller.StartNextRound(_bob);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(controller.GetCurrentPlayer(), Is.EqualTo(_bob));
        });
    }

    [Test]
    public void StartNextRound_ReturnsAllCardsToDrawPileBeforeRedealing()
    {
        List<ICard> aliceHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        List<ICard> bobHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        ICard starter = Card(CardColor.Red, CardValue.Five, 5);
        IDrawPile drawPile = BuildDrawPileForTwoPlayers(aliceHand, bobHand, starter);
        GameController controller = CreateController(new List<IPlayer> { _alice, _bob }, drawPile);
        controller.StartGame();

        controller.StartNextRound(_alice);

        Assert.Multiple(() =>
        {
            Assert.That(controller.GetPlayerHand(_alice).Value, Has.Count.EqualTo(7));
            Assert.That(controller.GetPlayerHand(_bob).Value, Has.Count.EqualTo(7));
        });
    }

    [Test]
    public void GetCurrentColor_BeforeGameStarts_ReturnsFailure()
    {
        IDrawPile drawPile = BuildDrawPileForTwoPlayers(
            FlatCards(7, CardColor.Blue, CardValue.One, 1),
            FlatCards(7, CardColor.Blue, CardValue.One, 1),
            Card(CardColor.Red, CardValue.Five, 5));
        GameController controller = CreateController(new List<IPlayer> { _alice, _bob }, drawPile);

        GameResult<CardColor> result = controller.GetCurrentColor();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("Current color is not set"));
        });
    }

    [Test]
    public void GetCurrentColor_AfterGameStarts_ReturnsStarterCardColor()
    {
        List<ICard> aliceHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        List<ICard> bobHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        ICard starter = Card(CardColor.Red, CardValue.Five, 5);
        IDrawPile drawPile = BuildDrawPileForTwoPlayers(aliceHand, bobHand, starter);
        GameController controller = CreateController(new List<IPlayer> { _alice, _bob }, drawPile);
        controller.StartGame();

        GameResult<CardColor> result = controller.GetCurrentColor();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Value, Is.EqualTo(CardColor.Red));
        });
    }

    [Test]
    public void GetPlayerHand_WhenPlayerNotInGame_ReturnsFailure()
    {
        List<ICard> aliceHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        List<ICard> bobHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        ICard starter = Card(CardColor.Red, CardValue.Five, 5);
        IDrawPile drawPile = BuildDrawPileForTwoPlayers(aliceHand, bobHand, starter);
        GameController controller = CreateController(new List<IPlayer> { _alice, _bob }, drawPile);
        controller.StartGame();
        IPlayer strangerNotInGame = new Player("Charlie");

        GameResult<List<ICard>> result = controller.GetPlayerHand(strangerNotInGame);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("Player not found in the game"));
        });
    }

    [Test]
    public void GetValidCards_WhenPlayerNotInGame_ReturnsFailure()
    {
        List<ICard> aliceHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        List<ICard> bobHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        ICard starter = Card(CardColor.Red, CardValue.Five, 5);
        IDrawPile drawPile = BuildDrawPileForTwoPlayers(aliceHand, bobHand, starter);
        GameController controller = CreateController(new List<IPlayer> { _alice, _bob }, drawPile);
        controller.StartGame();
        IPlayer strangerNotInGame = new Player("Charlie");

        GameResult<List<ICard>> result = controller.GetValidCards(strangerNotInGame);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("Player not found in the game"));
        });
    }

    [Test]
    public void GetPlayers_ReturnsAllPlayersInGame()
    {
        List<ICard> aliceHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        List<ICard> bobHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        ICard starter = Card(CardColor.Red, CardValue.Five, 5);
        IDrawPile drawPile = BuildDrawPileForTwoPlayers(aliceHand, bobHand, starter);
        GameController controller = CreateController(new List<IPlayer> { _alice, _bob }, drawPile);

        List<IPlayer> players = controller.GetPlayers();

        Assert.That(players, Is.EquivalentTo(new List<IPlayer> { _alice, _bob }));
    }

    [Test]
    public void PlayCard_WhenOnlyValueMatches_Succeeds()
    {
        List<ICard> aliceHand = new List<ICard> { Card(CardColor.Blue, CardValue.Five, 5) };
        aliceHand.AddRange(FlatCards(6, CardColor.Blue, CardValue.One, 1));
        List<ICard> bobHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        ICard starter = Card(CardColor.Red, CardValue.Five, 5);
        IDrawPile drawPile = BuildDrawPileForTwoPlayers(aliceHand, bobHand, starter);
        GameController controller = CreateController(new List<IPlayer> { _alice, _bob }, drawPile);
        controller.StartGame();

        GameResult result = controller.PlayCard(_alice, aliceHand[0], null);

        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void PlayCard_WithSkipCard_ReturnsTurnToSamePlayerWithTwoPlayers()
    {
        List<ICard> aliceHand = new List<ICard> { Card(CardColor.Red, CardValue.Skip, 20) };
        aliceHand.AddRange(FlatCards(6, CardColor.Blue, CardValue.One, 1));
        List<ICard> bobHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        ICard starter = Card(CardColor.Red, CardValue.Five, 5);
        IDrawPile drawPile = BuildDrawPileForTwoPlayers(aliceHand, bobHand, starter);
        GameController controller = CreateController(new List<IPlayer> { _alice, _bob }, drawPile);
        controller.StartGame();

        controller.PlayCard(_alice, aliceHand[0], null);

        Assert.That(controller.GetCurrentPlayer(), Is.EqualTo(_alice));
    }

    [Test]
    public void PlayCard_WithReverseCardAndThreePlayers_ChangesDirectionAndSkipsToThirdPlayer()
    {
        IPlayer charlie = new Player("Charlie");
        List<ICard> aliceHand = new List<ICard> { Card(CardColor.Red, CardValue.Reverse, 20) };
        aliceHand.AddRange(FlatCards(6, CardColor.Blue, CardValue.One, 1));
        List<ICard> bobHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        List<ICard> charlieHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        ICard starter = Card(CardColor.Red, CardValue.Five, 5);
        IDrawPile drawPile = BuildDrawPileForPlayers(
            new List<List<ICard>> { aliceHand, bobHand, charlieHand }, starter);
        GameController controller = CreateController(new List<IPlayer> { _alice, _bob, charlie }, drawPile);
        controller.StartGame();

        controller.PlayCard(_alice, aliceHand[0], null);

        Assert.That(controller.GetCurrentPlayer(), Is.EqualTo(charlie));
    }

    [Test]
    public void PlayCard_WithDrawTwoCard_NextPlayerDrawsTwoCardsAndIsSkipped()
    {
        List<ICard> aliceHand = new List<ICard> { Card(CardColor.Red, CardValue.DrawTwo, 20) };
        aliceHand.AddRange(FlatCards(6, CardColor.Blue, CardValue.One, 1));
        List<ICard> bobHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        ICard starter = Card(CardColor.Red, CardValue.Five, 5);
        List<ICard> reserve = FlatCards(2, CardColor.Green, CardValue.Two, 2);
        IDrawPile drawPile = BuildDrawPileForTwoPlayers(aliceHand, bobHand, starter, reserve);
        GameController controller = CreateController(new List<IPlayer> { _alice, _bob }, drawPile);
        controller.StartGame();

        controller.PlayCard(_alice, aliceHand[0], null);

        Assert.Multiple(() =>
        {
            Assert.That(controller.GetPlayerHand(_bob).Value, Has.Count.EqualTo(9));
            Assert.That(controller.GetCurrentPlayer(), Is.EqualTo(_alice));
        });
    }

    [Test]
    public void PlayCard_WithWildCard_ChangesColorAndDoesNotSkipNextPlayer()
    {
        List<ICard> aliceHand = new List<ICard> { Card(null, CardValue.Wild, 50) };
        aliceHand.AddRange(FlatCards(6, CardColor.Blue, CardValue.One, 1));
        List<ICard> bobHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        ICard starter = Card(CardColor.Red, CardValue.Five, 5);
        IDrawPile drawPile = BuildDrawPileForTwoPlayers(aliceHand, bobHand, starter);
        GameController controller = CreateController(new List<IPlayer> { _alice, _bob }, drawPile);
        controller.StartGame();

        controller.PlayCard(_alice, aliceHand[0], CardColor.Green);

        Assert.Multiple(() =>
        {
            Assert.That(controller.GetCurrentColor().Value, Is.EqualTo(CardColor.Green));
            Assert.That(controller.GetCurrentPlayer(), Is.EqualTo(_bob));
        });
    }

    [Test]
    public void PlayCard_WithWildDrawFourCard_NextPlayerDrawsFourCardsChosenColorAppliedAndSkipped()
    {
        List<ICard> aliceHand = new List<ICard> { Card(null, CardValue.WildDrawFour, 50) };
        aliceHand.AddRange(FlatCards(6, CardColor.Blue, CardValue.One, 1));
        List<ICard> bobHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        ICard starter = Card(CardColor.Red, CardValue.Five, 5);
        List<ICard> reserve = FlatCards(4, CardColor.Green, CardValue.Two, 2);
        IDrawPile drawPile = BuildDrawPileForTwoPlayers(aliceHand, bobHand, starter, reserve);
        GameController controller = CreateController(new List<IPlayer> { _alice, _bob }, drawPile);
        controller.StartGame();

        controller.PlayCard(_alice, aliceHand[0], CardColor.Yellow);

        Assert.Multiple(() =>
        {
            Assert.That(controller.GetPlayerHand(_bob).Value, Has.Count.EqualTo(11));
            Assert.That(controller.GetCurrentColor().Value, Is.EqualTo(CardColor.Yellow));
            Assert.That(controller.GetCurrentPlayer(), Is.EqualTo(_alice));
        });
    }

    [Test]
    public void StartGame_WithWildDrawFourStarterCard_DrawsNewStarterAndUsesItsColor()
    {
        List<ICard> aliceHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        List<ICard> bobHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        ICard wildDrawFourStarter = Card(null, CardValue.WildDrawFour, 50);
        ICard newStarter = Card(CardColor.Green, CardValue.Three, 3);
        List<ICard> reserve = new List<ICard> { newStarter };
        IDrawPile drawPile = BuildDrawPileForTwoPlayers(aliceHand, bobHand, wildDrawFourStarter, reserve);
        GameController controller = CreateController(new List<IPlayer> { _alice, _bob }, drawPile);

        controller.StartGame();

        Assert.Multiple(() =>
        {
            Assert.That(controller.GetTopDiscardCard(), Is.EqualTo(newStarter));
            Assert.That(controller.GetCurrentColor().Value, Is.EqualTo(CardColor.Green));
            Assert.That(controller.GetCurrentPlayer(), Is.EqualTo(_alice));
        });
    }

    [Test]
    public void StartGame_WithDrawTwoStarterCard_FirstPlayerDrawsTwoCardsAndSecondPlayerStarts()
    {
        List<ICard> aliceHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        List<ICard> bobHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        ICard drawTwoStarter = Card(CardColor.Red, CardValue.DrawTwo, 20);
        List<ICard> reserve = FlatCards(2, CardColor.Green, CardValue.Two, 2);
        IDrawPile drawPile = BuildDrawPileForTwoPlayers(aliceHand, bobHand, drawTwoStarter, reserve);
        GameController controller = CreateController(new List<IPlayer> { _alice, _bob }, drawPile);

        controller.StartGame();

        Assert.Multiple(() =>
        {
            Assert.That(controller.GetPlayerHand(_alice).Value, Has.Count.EqualTo(9));
            Assert.That(controller.GetPlayerHand(_bob).Value, Has.Count.EqualTo(7));
            Assert.That(controller.GetCurrentPlayer(), Is.EqualTo(_bob));
        });
    }

    [Test]
    public void DrawCard_WhenDrawPileIsEmpty_RefillsFromDiscardPileAndSucceeds()
    {
        List<ICard> aliceHand = new List<ICard> { Card(CardColor.Red, CardValue.Seven, 7) };
        aliceHand.AddRange(FlatCards(6, CardColor.Blue, CardValue.One, 1));
        List<ICard> bobHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        ICard starter = Card(CardColor.Red, CardValue.Five, 5);
        IDrawPile drawPile = BuildDrawPileForTwoPlayers(aliceHand, bobHand, starter);
        GameController controller = CreateController(new List<IPlayer> { _alice, _bob }, drawPile);
        controller.StartGame();
        controller.PlayCard(_alice, aliceHand[0], null);

        GameResult<ICard> result = controller.DrawCard(_bob);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Value, Is.EqualTo(starter));
            Assert.That(controller.GetPlayerHand(_bob).Value, Has.Count.EqualTo(8));
            Assert.That(controller.GetTopDiscardCard(), Is.EqualTo(aliceHand[0]));
        });
    }

    [Test]
    public void DrawCard_WhenDrawPileEmptyAndDiscardPileHasOnlyOneCard_ReturnsFailure()
    {
        List<ICard> aliceHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        List<ICard> bobHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        ICard starter = Card(CardColor.Red, CardValue.Five, 5);
        IDrawPile drawPile = BuildDrawPileForTwoPlayers(aliceHand, bobHand, starter);
        GameController controller = CreateController(new List<IPlayer> { _alice, _bob }, drawPile);
        controller.StartGame();

        GameResult<ICard> result = controller.DrawCard(_alice);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("There is not enough cards to draw"));
        });
    }

    [Test]
    public void PlayCard_WhenPlayerReachesFiveHundredPoints_TriggersGameEnded()
    {
        _alice.Score = 494;

        List<ICard> aliceHand = Enumerable.Range(0, 7)
            .Select(v => Card(CardColor.Red, (CardValue)v, v))
            .ToList();
        List<ICard> bobHand = Enumerable.Range(0, 7)
            .Select(v => Card(CardColor.Red, (CardValue)v, v))
            .ToList();
        ICard starter = Card(CardColor.Red, CardValue.Seven, 7);

        IDrawPile drawPile = BuildDrawPileForTwoPlayers(aliceHand, bobHand, starter);
        GameController controller = CreateController(new List<IPlayer> { _alice, _bob }, drawPile);
        controller.StartGame();

        IPlayer? gameWinner = null;
        controller.OnGameEnded += (player) => gameWinner = player;

        int aliceIndex = 0;
        int bobIndex = 0;
        while (gameWinner == null)
        {
            IPlayer current = controller.GetCurrentPlayer();
            if (current == _alice)
            {
                controller.PlayCard(_alice, aliceHand[aliceIndex], null);
                aliceIndex++;
            }
            else
            {
                controller.PlayCard(_bob, bobHand[bobIndex], null);
                bobIndex++;
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(gameWinner, Is.EqualTo(_alice));
            Assert.That(_alice.Score, Is.EqualTo(500));
        });
    }

    [Test]
    public void OnTurnStarted_WhenGameStarts_FiresWithFirstPlayer()
    {
        List<ICard> aliceHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        List<ICard> bobHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        ICard starter = Card(CardColor.Red, CardValue.Five, 5);
        IDrawPile drawPile = BuildDrawPileForTwoPlayers(aliceHand, bobHand, starter);
        GameController controller = CreateController(new List<IPlayer> { _alice, _bob }, drawPile);
        IPlayer? notifiedPlayer = null;
        controller.OnTurnStarted += (player) => notifiedPlayer = player;

        controller.StartGame();

        Assert.That(notifiedPlayer, Is.EqualTo(_alice));
    }

    [Test]
    public void OnTurnStarted_WhenTurnPasses_FiresWithNextPlayer()
    {
        List<ICard> aliceHand = new List<ICard> { Card(CardColor.Red, CardValue.Seven, 7) };
        aliceHand.AddRange(FlatCards(6, CardColor.Blue, CardValue.One, 1));
        List<ICard> bobHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        ICard starter = Card(CardColor.Red, CardValue.Five, 5);
        IDrawPile drawPile = BuildDrawPileForTwoPlayers(aliceHand, bobHand, starter);
        GameController controller = CreateController(new List<IPlayer> { _alice, _bob }, drawPile);
        controller.StartGame();
        IPlayer? notifiedPlayer = null;
        controller.OnTurnStarted += (player) => notifiedPlayer = player;

        controller.PlayCard(_alice, aliceHand[0], null);

        Assert.That(notifiedPlayer, Is.EqualTo(_bob));
    }

    [Test]
    public void OnCardPlayed_WhenCardIsPlayed_FiresWithCorrectPlayerAndCard()
    {
        List<ICard> aliceHand = new List<ICard> { Card(CardColor.Red, CardValue.Seven, 7) };
        aliceHand.AddRange(FlatCards(6, CardColor.Blue, CardValue.One, 1));
        List<ICard> bobHand = FlatCards(7, CardColor.Blue, CardValue.One, 1);
        ICard starter = Card(CardColor.Red, CardValue.Five, 5);
        IDrawPile drawPile = BuildDrawPileForTwoPlayers(aliceHand, bobHand, starter);
        GameController controller = CreateController(new List<IPlayer> { _alice, _bob }, drawPile);
        controller.StartGame();
        IPlayer? notifiedPlayer = null;
        ICard? notifiedCard = null;
        controller.OnCardPlayed += (player, card) =>
        {
            notifiedPlayer = player;
            notifiedCard = card;
        };

        controller.PlayCard(_alice, aliceHand[0], null);

        Assert.Multiple(() =>
        {
            Assert.That(notifiedPlayer, Is.EqualTo(_alice));
            Assert.That(notifiedCard, Is.EqualTo(aliceHand[0]));
        });
    }

    [Test]
    public void OnUnoCalled_WhenCallUnoSucceeds_FiresWithCorrectPlayer()
    {
        GameController controller = PlayUntilAliceIsUnoPending(out _, out _);
        IPlayer? notifiedPlayer = null;
        controller.OnUnoCalled += (player) => notifiedPlayer = player;

        controller.CallUno(_alice);

        Assert.That(notifiedPlayer, Is.EqualTo(_alice));
    }

    [Test]
    public void OnUnoPenaltyApplied_WhenPlayerIsCaught_FiresWithCorrectPlayer()
    {
        GameController controller = PlayUntilAliceIsUnoPending(out _, out _);
        IPlayer? notifiedPlayer = null;
        controller.OnUnoPenaltyApplied += (player) => notifiedPlayer = player;

        controller.CatchUnoViolation(_alice);

        Assert.That(notifiedPlayer, Is.EqualTo(_alice));
    }
}
