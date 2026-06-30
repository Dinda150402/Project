using UnoGame.Core.Interfaces;

namespace UnoGame.Core.Models;

public class DiscardPile : IDiscardPile
{
    public List<ICard> Cards { get; }

    public DiscardPile()
    {
        Cards = new List<ICard> { };
    }
}