using System;

namespace Project.Domain.Poker
{
    public sealed class PokerHandComparer
    {
        public int Compare(PokerHandResult left, PokerHandResult right)
        {
            if (left == null)
                throw new ArgumentNullException(nameof(left));

            if (right == null)
                throw new ArgumentNullException(nameof(right));

            int rankComparison = left.Rank.CompareTo(right.Rank);

            if (rankComparison != 0)
                return rankComparison;

            int tieBreakerCount = Math.Min(left.TieBreakers.Count, right.TieBreakers.Count);

            for (int i = 0; i < tieBreakerCount; i++)
            {
                int tieBreakerComparison = left.TieBreakers[i].CompareTo(right.TieBreakers[i]);

                if (tieBreakerComparison != 0)
                    return tieBreakerComparison;
            }

            return left.TieBreakers.Count.CompareTo(right.TieBreakers.Count);
        }
    }
}