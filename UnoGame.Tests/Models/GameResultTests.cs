using NUnit.Framework;
using UnoGame.Core.Models;

namespace UnoGame.Tests.Models;

[TestFixture]
public class GameResultTests
{
    [Test]
    public void Ok_CreatesSuccessfulResultWithoutErrorMessage()
    {
        GameResult result = GameResult.Ok();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ErrorMessage, Is.Null);
        });
    }

    [Test]
    public void Fail_CreatesFailedResultWithErrorMessage()
    {
        GameResult result = GameResult.Fail("Terjadi kesalahan");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("Terjadi kesalahan"));
        });
    }

    [Test]
    public void GenericOk_CreatesSuccessfulResultWithValue()
    {
        GameResult<int> result = GameResult<int>.Ok(42);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Value, Is.EqualTo(42));
            Assert.That(result.ErrorMessage, Is.Null);
        });
    }

    [Test]
    public void GenericFail_CreatesFailedResultWithDefaultValue()
    {
        GameResult<int> result = GameResult<int>.Fail("Gagal ambil kartu");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("Gagal ambil kartu"));
            Assert.That(result.Value, Is.EqualTo(0));
        });
    }
}
