using System.Collections.Generic;
using System.Linq;
using Project.Application.Poker;
using Project.Domain.Cards;
using Project.Domain.Poker;
using UnityEngine;

namespace Project.Dev
{
    public sealed class TexasHoldemOfflineSmokeTest : MonoBehaviour
    {
        [SerializeField] private bool useFixedSeed = true;
        [SerializeField] private int seed = 12345;

        private void Start()
        {
            ICardShuffler shuffler = useFixedSeed
                ? new SystemRandomCardShuffler(seed)
                : new SecureRandomCardShuffler();

            var handComparer = new PokerHandComparer();

            var bestHandEvaluator = new BestFiveCardPokerHandEvaluator(
                new PokerHandEvaluator(),
                handComparer
            );

            var simulator = new TexasHoldemOfflineRoundSimulator(
                shuffler,
                bestHandEvaluator,
                handComparer
            );

            TexasHoldemRoundResult result = simulator.PlayRound(new[]
            {
                "Player 1",
                "Player 2"
            });

            Debug.Log($"Board: {string.Join(", ", result.BoardCards)}");

            foreach (TexasHoldemPlayerResult player in result.Players)
            {
                string winnerText = player.IsWinner ? "WINNER" : "LOSE";

                Debug.Log(
                    $"{winnerText} | {player.PlayerId} | " +
                    $"Hole: {string.Join(", ", player.HoleCards)} | " +
                    $"Best: {player.BestHand.HandResult} | " +
                    $"Best Cards: {string.Join(", ", player.BestHand.BestCards)}"
                );
            }
        }
    }
}