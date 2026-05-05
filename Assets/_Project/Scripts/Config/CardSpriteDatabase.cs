using System;
using System.Collections.Generic;
using Project.Domain.Cards;
using UnityEngine;

namespace Project.Config
{
    [CreateAssetMenu(
        fileName = "CardSpriteDatabase",
        menuName = "Project/Card Game/Card Sprite Database")]
    public sealed class CardSpriteDatabase : ScriptableObject
    {
        [SerializeField] private Sprite cardBackSprite;
        [SerializeField] private List<CardSpriteEntry> entries = new();

        private Dictionary<Card, Sprite> _spriteByCard;

        public Sprite CardBackSprite => cardBackSprite;

        public Sprite GetSprite(Card card)
        {
            EnsureCache();

            if (_spriteByCard.TryGetValue(card, out Sprite sprite) && sprite != null)
                return sprite;

            throw new InvalidOperationException($"Missing sprite for card: {card}");
        }

        private void EnsureCache()
        {
            if (_spriteByCard != null)
                return;

            _spriteByCard = new Dictionary<Card, Sprite>();

            foreach (CardSpriteEntry entry in entries)
            {
                if (entry == null || entry.sprite == null)
                    continue;

                var card = new Card(entry.rank, entry.suit);
                _spriteByCard[card] = entry.sprite;
            }
        }
    }
}