using System;
using System.Collections.Generic;

namespace Project.Domain.Cards
{
    public sealed class SystemRandomCardShuffler : ICardShuffler
    {
        private readonly Random _random;

        public SystemRandomCardShuffler()
        {
            _random = new Random();
        }

        public SystemRandomCardShuffler(int seed)
        {
            _random = new Random(seed);
        }

        public IReadOnlyList<Card> Shuffle(IReadOnlyList<Card> cards)
        {
            if (cards == null)
                throw new ArgumentNullException(nameof(cards));

            var shuffledCards = new List<Card>(cards);

            for (int i = shuffledCards.Count - 1; i > 0; i--)
            {
                int randomIndex = _random.Next(i + 1);

                Card temp = shuffledCards[i];
                shuffledCards[i] = shuffledCards[randomIndex];
                shuffledCards[randomIndex] = temp;
            }

            return shuffledCards;
        }
    }
}