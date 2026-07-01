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
            // 0 hanya ada satu per warna, bernilai 0 poin
            deck.Add(new Card(color, CardValue.Zero, 0));

            // 1-9 ada dua per warna, bernilai sesuai angkanya
            for (CardValue val = CardValue.One; val <= CardValue.Nine; val++)
            {
                int points = (int)val;
                deck.Add(new Card(color, val, points));
                deck.Add(new Card(color, val, points));
            }

            // Skip, Reverse, DrawTwo ada dua per warna, bernilai 20 poin
            CardValue[] actionCards = { CardValue.Skip, CardValue.Reverse, CardValue.DrawTwo };
            foreach (CardValue action in actionCards)
            {
                deck.Add(new Card(color, action, 20));
                deck.Add(new Card(color, action, 20));
            }
        }

        // Wild & WildDrawFour ada 4 masing-masing, bernilai 50 poin, tanpa warna
        for (int i = 0; i < 4; i++)
        {
            deck.Add(new Card(null, CardValue.Wild, 50));
            deck.Add(new Card(null, CardValue.WildDrawFour, 50));
        }

        return deck;
    }
}