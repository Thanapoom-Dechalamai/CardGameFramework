using System;
using System.Collections.Generic;
using Project.Domain.Cards;

namespace Project.Domain.Poker
{
    public sealed class BestFiveCardPokerHandEvaluator
    {
        private const int MinimumCardCount = 5;
        private const int MaximumCardCount = 7;
        private const int HandCardCount = 5;

        private readonly PokerHandEvaluator _handEvaluator;
        private readonly PokerHandComparer _handComparer;

        public BestFiveCardPokerHandEvaluator(
            PokerHandEvaluator handEvaluator,
            PokerHandComparer handComparer)
        {
            _handEvaluator = handEvaluator ?? throw new ArgumentNullException(nameof(handEvaluator));
            _handComparer = handComparer ?? throw new ArgumentNullException(nameof(handComparer));
        }

        public BestPokerHandResult EvaluateBestHand(IReadOnlyList<Card> cards)
        {
            if (cards == null)
                throw new ArgumentNullException(nameof(cards));

            if (cards.Count < MinimumCardCount || cards.Count > MaximumCardCount)
                throw new ArgumentException("Best hand evaluation requires 5 to 7 cards.", nameof(cards));

            CardCollectionValidator.EnsureNoDuplicateCards(cards, nameof(cards));

            PokerHandResult bestResult = null;
            List<Card> bestCards = null;

            for (int a = 0; a < cards.Count - 4; a++)
                for (int b = a + 1; b < cards.Count - 3; b++)
                    for (int c = b + 1; c < cards.Count - 2; c++)
                        for (int d = c + 1; d < cards.Count - 1; d++)
                            for (int e = d + 1; e < cards.Count; e++)
                            {
                                var candidateCards = new List<Card>(HandCardCount)
                                {
                                    cards[a],
                                    cards[b],
                                    cards[c],
                                    cards[d],
                                    cards[e]
                                };

                                PokerHandResult candidateResult = _handEvaluator.Evaluate(candidateCards);

                                if (bestResult == null || _handComparer.Compare(candidateResult, bestResult) > 0)
                                {
                                    bestResult = candidateResult;
                                    bestCards = candidateCards;
                                }
                            }

            return new BestPokerHandResult(bestResult, bestCards);
        }
    }
}