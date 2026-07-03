using NUnit.Framework;
using UnoGame.Core.Models;

namespace UnoGame.Tests.Models;

[TestFixture]
public class PlayerTests
{
    [Test]
    public void Constructor_SetsNameAndDefaultsScoreToZero()
    {
        Player player = new Player("Alice");

        Assert.Multiple(() =>
        {
            Assert.That(player.Name, Is.EqualTo("Alice"));
            Assert.That(player.Score, Is.EqualTo(0));
        });
    }

    [Test]
    public void Score_CanBeUpdatedAfterConstruction()
    {
        Player player = new Player("Bob");

        player.Score += 25;

        Assert.That(player.Score, Is.EqualTo(25));
    }
}
