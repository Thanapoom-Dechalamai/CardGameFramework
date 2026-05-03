using System;
using System.Collections.Generic;

namespace Project.Domain.Cards
{
    public sealed class Deck
    {
        private readonly List<Card> _cards;

        public int RemainingCount => _cards.Count;
        public bool IsEmpty => _cards.Count == 0;
        public IReadOnlyList<Card> Cards => _cards;

        public Deck(IEnumerable<Card> cards)
        {
            if (cards == null)
                throw new ArgumentNullException(nameof(cards));

            _cards = new List<Card>(cards);
            CardCollectionValidator.EnsureNoDuplicateCards(_cards, nameof(cards));
        }

        public void Shuffle(ICardShuffler shuffler)
        {
            if (shuffler == null)
                throw new ArgumentNullException(nameof(shuffler));

            IReadOnlyList<Card> shuffledCards = shuffler.Shuffle(_cards);

            if (shuffledCards.Count != _cards.Count)
                throw new InvalidOperationException("Shuffler must return the same number of cards.");

            CardCollectionValidator.EnsureNoDuplicateCards(shuffledCards, nameof(shuffledCards));

            _cards.Clear();
            _cards.AddRange(shuffledCards);
        }

        public Card Draw()
        {
            if (_cards.Count == 0)
                throw new InvalidOperationException("Cannot draw from an empty deck.");

            int lastIndex = _cards.Count - 1;
            Card drawnCard = _cards[lastIndex];
            _cards.RemoveAt(lastIndex);

            return drawnCard;
        }

        public IReadOnlyList<Card> Draw(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), count, "Draw count cannot be negative.");

            if (count > _cards.Count)
                throw new InvalidOperationException($"Cannot draw {count} cards. Only {_cards.Count} cards remain.");

            var drawnCards = new List<Card>(count);

            for (int i = 0; i < count; i++)
            {
                drawnCards.Add(Draw());
            }

            return drawnCards;
        }
    }
}