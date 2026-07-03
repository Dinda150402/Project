using UnoGame.Core.Interfaces;

namespace UnoGame.Core.Services;

public class RandomCardShuffler : ICardShuffler
{
    public void Shuffle(List<ICard> cards)
    {
        int n = cards.Count;

        while (n > 1)
        {
            n--;
            int k = Random.Shared.Next(n + 1);

            (cards[k], cards[n]) = (cards[n], cards[k]);
        }
    }
}
