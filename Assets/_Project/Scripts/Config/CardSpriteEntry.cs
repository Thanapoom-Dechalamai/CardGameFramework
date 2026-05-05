using System;
using Project.Domain.Cards;
using UnityEngine;

namespace Project.Config
{
    [Serializable]
    public sealed class CardSpriteEntry
    {
        public Rank rank;
        public Suit suit;
        public Sprite sprite;
    }
}