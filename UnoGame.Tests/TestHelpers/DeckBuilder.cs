using UnoGame.Core.Enums;
using UnoGame.Core.Interfaces;
using UnoGame.Core.Models;

namespace UnoGame.Tests.TestHelpers;

internal static class DeckBuilder
{
    public static ICard Card(CardColor? color, CardValue value, int points = 0)
        => new Card(color, value, points);

    public static List<ICard> FlatCards(int count, CardColor color, CardValue value, int points = 0)
    {
        List<ICard> cards = new List<ICard>();
        for (int i = 0; i < count; i++)
        {
            cards.Add(Card(color, value, points));
        }
        return cards;
    }

    public static IDrawPile BuildDrawPileForPlayers(
        List<List<ICard>> hands,
        ICard starterCard,
        List<ICard>? reserve = null)
    {
        List<ICard> popOrder = new List<ICard>();
        foreach (List<ICard> hand in hands)
        {
            popOrder.AddRange(hand);
        }
        popOrder.Add(starterCard);

        List<ICard> deck = new List<ICard>(popOrder);
        deck.Reverse();

        if (reserve is { Count: > 0 })
        {
            deck.InsertRange(0, reserve);
        }

        return new DrawPile(deck);
    }

    public static IDrawPile BuildDrawPileForTwoPlayers(
        List<ICard> player1Hand,
        List<ICard> player2Hand,
        ICard starterCard,
        List<ICard>? reserve = null)
    {
        return BuildDrawPileForPlayers(
            new List<List<ICard>> { player1Hand, player2Hand },
            starterCard,
            reserve);
    }
}
