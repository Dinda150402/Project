using UnoGame.Core.Enums;
using UnoGame.Core.Interfaces;
using UnoGame.Core.Models;

internal static class DeckFactory
{
    public static List<ICard> GenerateUnoDeck()
    {
        List<ICard> deck = new List<ICard>();
        CardColor[] colors = { CardColor.Red, CardColor.Blue, CardColor.Green, CardColor.Yellow };

        foreach (CardColor color in colors)
        {
            deck.Add(new Card(color, CardValue.Zero, 0));

            for (CardValue val = CardValue.One; val <= CardValue.Nine; val++)
            {
                int points = (int)val;
                deck.Add(new Card(color, val, points));
                deck.Add(new Card(color, val, points));
            }

            CardValue[] actionCards = { CardValue.Skip, CardValue.Reverse, CardValue.DrawTwo };
            foreach (CardValue action in actionCards)
            {
                deck.Add(new Card(color, action, 20));
                deck.Add(new Card(color, action, 20));
            }
        }

        for (int i = 0; i < 4; i++)
        {
            deck.Add(new Card(null, CardValue.Wild, 50));
            deck.Add(new Card(null, CardValue.WildDrawFour, 50));
        }

        return deck;
    }
}