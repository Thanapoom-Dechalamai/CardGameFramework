using System;
using System.Collections.Generic;
using System.Linq;
using Project.Domain.Cards;

namespace Project.Domain.Poker
{
    public sealed class BestPokerHandResult
    {
        public PokerHandResult HandResult { get; }
        public IReadOnlyList<Card> BestCards { get; }

        public BestPokerHandResult(PokerHandResult handResult, IEnumerable<Card> bestCards)
        {
            HandResult = handResult ?? throw new ArgumentNullException(nameof(handResult));

            if (bestCards == null)
                throw new ArgumentNullException(nameof(bestCards));

            BestCards = bestCards.ToArray();
        }

        public override string ToString()
        {
            return $"{HandResult} | Cards: {string.Join(", ", BestCards)}";
        }
    }
}