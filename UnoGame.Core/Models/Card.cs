using UnoGame.Core.Enums;
using UnoGame.Core.Interfaces;

namespace UnoGame.Core.Models;

public class Card : ICard
{
    public CardValue Value { get; init; }
    public int Points { get; init; }
    public CardColor? Color { get; init; }

    public Card (CardColor? color, CardValue value, int points)
    {
        Color = color;
        Value = value;
        Points = points;
    }
}
