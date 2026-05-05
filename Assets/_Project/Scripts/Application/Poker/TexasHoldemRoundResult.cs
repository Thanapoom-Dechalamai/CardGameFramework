using System;
using System.Collections.Generic;
using System.Linq;
using Project.Domain.Cards;

namespace Project.Application.Poker
{
    public sealed class TexasHoldemRoundResult
    {
        public IReadOnlyList<Card> BoardCards { get; }
        public IReadOnlyList<TexasHoldemPlayerResult> Players { get; }

        public TexasHoldemRoundResult(
            IEnumerable<Card> boardCards,
            IEnumerable<TexasHoldemPlayerResult> players)
        {
            BoardCards = boardCards?.ToArray() ?? throw new ArgumentNullException(nameof(boardCards));
            Players = players?.ToArray() ?? throw new ArgumentNullException(nameof(players));
        }
    }
}