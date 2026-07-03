using NUnit.Framework;
using UnoGame.Core.Enums;
using UnoGame.Core.Models;

namespace UnoGame.Tests.Models;

[TestFixture]
public class CardTests
{
    [Test]
    public void Constructor_SetsColorValueAndPoints()
    {
        Card card = new Card(CardColor.Red, CardValue.Seven, 7);

        Assert.Multiple(() =>
        {
            Assert.That(card.Color, Is.EqualTo(CardColor.Red));
            Assert.That(card.Value, Is.EqualTo(CardValue.Seven));
            Assert.That(card.Points, Is.EqualTo(7));
        });
    }

    [Test]
    public void Constructor_WithNullColor_AllowsWildCards()
    {
        Card card = new Card(null, CardValue.Wild, 50);

        Assert.Multiple(() =>
        {
            Assert.That(card.Color, Is.Null);
            Assert.That(card.Value, Is.EqualTo(CardValue.Wild));
            Assert.That(card.Points, Is.EqualTo(50));
        });
    }
}
