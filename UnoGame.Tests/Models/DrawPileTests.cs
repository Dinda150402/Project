using NUnit.Framework;
using UnoGame.Core.Enums;
using UnoGame.Core.Interfaces;
using UnoGame.Core.Models;

namespace UnoGame.Tests.Models;

[TestFixture]
public class DrawPileTests
{
    [Test]
    public void Constructor_StoresProvidedCardsDirectly()
    {
        List<ICard> cards = new List<ICard> { new Card(CardColor.Red, CardValue.One, 1) };

        DrawPile drawPile = new DrawPile(cards);

        Assert.That(drawPile.Cards, Is.SameAs(cards));
    }
}
