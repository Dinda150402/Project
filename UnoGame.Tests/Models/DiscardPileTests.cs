using NUnit.Framework;
using UnoGame.Core.Models;

namespace UnoGame.Tests.Models;

[TestFixture]
public class DiscardPileTests
{
    [Test]
    public void Constructor_StartsEmpty()
    {
        DiscardPile discardPile = new DiscardPile();

        Assert.That(discardPile.Cards, Is.Empty);
    }
}
