using Project.Domain.Cards;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Bootstrap
{
    public sealed class CardDomainSmokeTest : MonoBehaviour
    {
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private int shuffleSeed = 12345;

        private void Start()
        {
            if (!runOnStart)
                return;

            Deck deck = DeckFactory.CreateStandard52CardDeck();
            deck.Shuffle(new SystemRandomCardShuffler(shuffleSeed));

            IReadOnlyList<Card> hand = deck.Draw(5);

            Debug.Log($"Card Domain Smoke Test OK | Drawn: {hand.Count} | Remaining: {deck.RemainingCount}");
            Debug.Log($"First drawn card: {hand[0]}");
        }
    }
}