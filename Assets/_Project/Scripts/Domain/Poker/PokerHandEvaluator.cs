using System;
using System.Collections.Generic;
using System.Linq;
using Project.Domain.Cards;

namespace Project.Domain.Poker
{
    public sealed class PokerHandEvaluator
    {
        private const int RequiredCardCount = 5;

        public PokerHandResult Evaluate(IReadOnlyList<Card> cards)
        {
            if (cards == null)
                throw new ArgumentNullException(nameof(cards));

            if (cards.Count != RequiredCardCount)
                throw new ArgumentException("Poker hand evaluation requires exactly 5 cards.", nameof(cards));

            CardCollectionValidator.EnsureNoDuplicateCards(cards, nameof(cards));

            List<int> ranksDescending = cards
                .Select(card => (int)card.Rank)
                .OrderByDescending(rank => rank)
                .ToList();

            bool isFlush = cards.All(card => card.Suit == cards[0].Suit);
            bool isStraight = TryGetStraightHighCard(ranksDescending, out int straightHighCard);

            var rankGroups = ranksDescending
                .GroupBy(rank => rank)
                .Select(group => new RankGroup(group.Key, group.Count()))
                .OrderByDescending(group => group.Count)
                .ThenByDescending(group => group.Rank)
                .ToList();

            if (isStraight && isFlush)
            {
                if (straightHighCard == (int)Rank.Ace)
                    return new PokerHandResult(PokerHandRank.RoyalFlush, new[] { straightHighCard });

                return new PokerHandResult(PokerHandRank.StraightFlush, new[] { straightHighCard });
            }

            if (rankGroups[0].Count == 4)
            {
                int fourRank = rankGroups[0].Rank;
                int kicker = rankGroups[1].Rank;

                return new PokerHandResult(PokerHandRank.FourOfAKind, new[] { fourRank, kicker });
            }

            if (rankGroups[0].Count == 3 && rankGroups[1].Count == 2)
            {
                int threeRank = rankGroups[0].Rank;
                int pairRank = rankGroups[1].Rank;

                return new PokerHandResult(PokerHandRank.FullHouse, new[] { threeRank, pairRank });
            }

            if (isFlush)
                return new PokerHandResult(PokerHandRank.Flush, ranksDescending);

            if (isStraight)
                return new PokerHandResult(PokerHandRank.Straight, new[] { straightHighCard });

            if (rankGroups[0].Count == 3)
            {
                int threeRank = rankGroups[0].Rank;

                IEnumerable<int> kickers = rankGroups
                    .Where(group => group.Count == 1)
                    .Select(group => group.Rank)
                    .OrderByDescending(rank => rank);

                return new PokerHandResult(
                    PokerHandRank.ThreeOfAKind,
                    new[] { threeRank }.Concat(kickers)
                );
            }

            if (rankGroups[0].Count == 2 && rankGroups[1].Count == 2)
            {
                List<int> pairRanks = rankGroups
                    .Where(group => group.Count == 2)
                    .Select(group => group.Rank)
                    .OrderByDescending(rank => rank)
                    .ToList();

                int kicker = rankGroups.Single(group => group.Count == 1).Rank;

                return new PokerHandResult(
                    PokerHandRank.TwoPair,
                    new[] { pairRanks[0], pairRanks[1], kicker }
                );
            }

            if (rankGroups[0].Count == 2)
            {
                int pairRank = rankGroups[0].Rank;

                IEnumerable<int> kickers = rankGroups
                    .Where(group => group.Count == 1)
                    .Select(group => group.Rank)
                    .OrderByDescending(rank => rank);

                return new PokerHandResult(
                    PokerHandRank.OnePair,
                    new[] { pairRank }.Concat(kickers)
                );
            }

            return new PokerHandResult(PokerHandRank.HighCard, ranksDescending);
        }

        private static bool TryGetStraightHighCard(IReadOnlyList<int> ranksDescending, out int highCard)
        {
            List<int> distinctRanks = ranksDescending
                .Distinct()
                .OrderByDescending(rank => rank)
                .ToList();

            if (distinctRanks.Count != RequiredCardCount)
            {
                highCard = 0;
                return false;
            }

            bool isAceLowStraight =
                distinctRanks[0] == (int)Rank.Ace &&
                distinctRanks[1] == 5 &&
                distinctRanks[2] == 4 &&
                distinctRanks[3] == 3 &&
                distinctRanks[4] == 2;

            if (isAceLowStraight)
            {
                highCard = 5;
                return true;
            }

            bool isNormalStraight = distinctRanks[0] - distinctRanks[4] == 4;

            if (isNormalStraight)
            {
                highCard = distinctRanks[0];
                return true;
            }

            highCard = 0;
            return false;
        }

        private readonly struct RankGroup
        {
            public int Rank { get; }
            public int Count { get; }

            public RankGroup(int rank, int count)
            {
                Rank = rank;
                Count = count;
            }
        }
    }
}