using System;
using System.Collections.Generic;
using System.Linq;
using Project.Domain.Cards;
using Project.Domain.Poker;

namespace Project.Application.Poker
{
    public sealed class TexasHoldemPlayerResult
    {
        public string PlayerId { get; }
        public IReadOnlyList<Card> HoleCards { get; }
        public BestPokerHandResult BestHand { get; }
        public bool IsWinner { get; }

        public TexasHoldemPlayerResult(
            string playerId,
            IEnumerable<Card> holeCards,
            BestPokerHandResult bestHand,
            bool isWinner)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                throw new ArgumentException("Player id cannot be null or empty.", nameof(playerId));

            PlayerId = playerId;
            HoleCards = holeCards?.ToArray() ?? throw new ArgumentNullException(nameof(holeCards));
            BestHand = bestHand;
            IsWinner = isWinner;
        }
    }
}
