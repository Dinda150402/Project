using NUnit.Framework;
using UnoGame.Core.Enums;
using UnoGame.Core.Interfaces;
using UnoGame.Core.Models;
using UnoGame.Core.Services;

namespace UnoGame.Tests.Services;

[TestFixture]
public class RandomCardShufflerTests
{
    [Test]
    public void Shuffle_PreservesCardCountAndAllOriginalCards()
    {
        List<ICard> cards = Enumerable.Range(0, 10)
            .Select(v => (ICard)new Card(CardColor.Red, (CardValue)v, v))
            .ToList();
        List<ICard> originalCards = new List<ICard>(cards);
        RandomCardShuffler shuffler = new RandomCardShuffler();

        shuffler.Shuffle(cards);

        Assert.Multiple(() =>
        {
            Assert.That(cards, Has.Count.EqualTo(originalCards.Count));
            Assert.That(cards, Is.EquivalentTo(originalCards));
        });
    }

    [Test]
    public void Shuffle_WithSingleCard_DoesNotThrow()
    {
        List<ICard> cards = new List<ICard> { new Card(CardColor.Red, CardValue.One, 1) };
        RandomCardShuffler shuffler = new RandomCardShuffler();

        Assert.DoesNotThrow(() => shuffler.Shuffle(cards));
    }

    [Test]
    public void Shuffle_WithEmptyList_DoesNotThrow()
    {
        List<ICard> cards = new List<ICard>();
        RandomCardShuffler shuffler = new RandomCardShuffler();

        Assert.DoesNotThrow(() => shuffler.Shuffle(cards));
    }
}
