using UnoGame.Core.Interfaces;

namespace UnoGame.Tests.TestHelpers;

public class NoOpShuffler : ICardShuffler
{
    public void Shuffle(List<ICard> cards)
    {
        
    }
}
