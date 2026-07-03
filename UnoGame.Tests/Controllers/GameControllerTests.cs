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
        ICard starter = Card(CardColor.Red, CardValue.Five, 5); // warna Red, nilai Five
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
}
