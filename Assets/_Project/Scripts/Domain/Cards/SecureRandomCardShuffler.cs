using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Project.Domain.Cards
{
    public sealed class SecureRandomCardShuffler : ICardShuffler
    {
        public IReadOnlyList<Card> Shuffle(IReadOnlyList<Card> cards)
        {
            if (cards == null)
                throw new ArgumentNullException(nameof(cards));

            var shuffledCards = new List<Card>(cards);

            for (int i = shuffledCards.Count - 1; i > 0; i--)
            {
                int randomIndex = RandomNumberGenerator.GetInt32(i + 1);

                Card temp = shuffledCards[i];
                shuffledCards[i] = shuffledCards[randomIndex];
                shuffledCards[randomIndex] = temp;
            }

            return shuffledCards;
        }
    }
}