using System;
using System.Collections.Generic;
using System.Linq;
using Project.Domain.Cards;

namespace Project.Application.Poker
{
    public sealed class TexasHoldemRoundPlayerState
    {
        private readonly List<Card> _holeCards = new();

        public string PlayerId { get; }
        public int SeatIndex { get; }
        public bool IsBot { get; }
        public int Chips { get; private set; }
        public int StreetBet { get; private set; }
        public int TotalCommitted { get; private set; }
        public bool HasFolded { get; private set; }
        public bool IsAllIn => !HasFolded && Chips == 0;
        public bool HasActedThisStreet { get; private set; }
        public IReadOnlyList<Card> HoleCards => _holeCards;

        internal TexasHoldemRoundPlayerState(string playerId, int seatIndex, bool isBot, int chips)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                throw new ArgumentException("Player id cannot be null or empty.", nameof(playerId));

            if (chips <= 0)
                throw new ArgumentOutOfRangeException(nameof(chips), chips, "Starting chips must be positive.");

            PlayerId = playerId;
            SeatIndex = seatIndex;
            IsBot = isBot;
            Chips = chips;
        }

        internal void DealHoleCard(Card card)
        {
            if (_holeCards.Count >= 2)
                throw new InvalidOperationException("A Texas Hold'em player cannot have more than two hole cards.");

            _holeCards.Add(card);
        }

        internal int CommitChips(int requestedAmount)
        {
            if (requestedAmount < 0)
                throw new ArgumentOutOfRangeException(nameof(requestedAmount), requestedAmount, "Commit amount cannot be negative.");

            int committedAmount = Math.Min(requestedAmount, Chips);

            Chips -= committedAmount;
            StreetBet += committedAmount;
            TotalCommitted += committedAmount;

            return committedAmount;
        }

        internal int CommitToSettledPot(int requestedAmount)
        {
            if (requestedAmount < 0)
                throw new ArgumentOutOfRangeException(nameof(requestedAmount), requestedAmount, "Commit amount cannot be negative.");

            int committedAmount = Math.Min(requestedAmount, Chips);

            Chips -= committedAmount;
            TotalCommitted += committedAmount;

            return committedAmount;
        }

        internal void AwardChips(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Award amount cannot be negative.");

            Chips += amount;
        }

        internal void Fold()
        {
            HasFolded = true;
            HasActedThisStreet = true;
        }

        internal void MarkActed()
        {
            HasActedThisStreet = true;
        }

        internal void ResetForNextStreet()
        {
            StreetBet = 0;
            HasActedThisStreet = IsAllIn || HasFolded;
        }

        internal void RequireResponseToAggression()
        {
            if (!HasFolded && !IsAllIn)
                HasActedThisStreet = false;
        }

        internal TexasHoldemRoundPlayerState Copy()
        {
            var copy = new TexasHoldemRoundPlayerState(PlayerId, SeatIndex, IsBot, Chips)
            {
                StreetBet = StreetBet,
                TotalCommitted = TotalCommitted,
                HasFolded = HasFolded,
                HasActedThisStreet = HasActedThisStreet
            };

            foreach (Card card in _holeCards)
            {
                copy._holeCards.Add(card);
            }

            return copy;
        }

        public override string ToString()
        {
            string cards = _holeCards.Count == 0 ? "No cards" : string.Join(", ", _holeCards.Select(card => card.ToString()));
            return $"{PlayerId} | Chips: {Chips} | Bet: {StreetBet} | Cards: {cards}";
        }
    }
}
