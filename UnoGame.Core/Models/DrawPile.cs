using UnoGame.Core.Interfaces;

namespace UnoGame.Core.Models;

public class DrawPile : IDrawPile
{
    public List<ICard> Cards { get; }
    public DrawPile(List<ICard> cards){
        Cards = cards;
    }
}