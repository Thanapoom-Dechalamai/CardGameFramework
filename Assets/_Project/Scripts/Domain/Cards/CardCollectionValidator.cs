using System;
using System.Collections.Generic;

namespace Project.Domain.Cards
{
    public static class CardCollectionValidator
    {
        public static void EnsureNoDuplicateCards(IEnumerable<Card> cards, string parameterName)
        {
            if (cards == null)
                throw new ArgumentNullException(parameterName);

            var seenCards = new HashSet<Card>();

            foreach (Card card in cards)
            {
                if (!seenCards.Add(card))
                    throw new ArgumentException($"Duplicate card detected: {card}", parameterName);
            }
        }
    }
}