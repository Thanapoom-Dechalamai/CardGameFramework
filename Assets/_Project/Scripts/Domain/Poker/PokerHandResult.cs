using System;
using System.Collections.Generic;
using System.Linq;

namespace Project.Domain.Poker
{
    public sealed class PokerHandResult
    {
        public PokerHandRank Rank { get; }
        public IReadOnlyList<int> TieBreakers { get; }

        public PokerHandResult(PokerHandRank rank, IEnumerable<int> tieBreakers)
        {
            if (tieBreakers == null)
                throw new ArgumentNullException(nameof(tieBreakers));

            Rank = rank;
            TieBreakers = tieBreakers.ToArray();
        }

        public override string ToString()
        {
            return $"{Rank} [{string.Join(", ", TieBreakers)}]";
        }
    }
}