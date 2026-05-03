using System.Collections.Generic;

namespace Project.Domain.Cards
{
    public static class DeckFactory
    {
        public static Deck CreateStandard52CardDeck()
        {
            return new Deck(CreateStandard52Cards());
        }

        public static IReadOnlyList<Card> CreateStandard52Cards()
        {
            var cards = new List<Card>(52);

            for (int suitValue = (int)Suit.Clubs; suitValue <= (int)Suit.Spades; suitValue++)
            {
                for (int rankValue = (int)Rank.Two; rankValue <= (int)Rank.Ace; rankValue++)
                {
                    cards.Add(new Card((Rank)rankValue, (Suit)suitValue));
                }
            }

            return cards;
        }
    }
}