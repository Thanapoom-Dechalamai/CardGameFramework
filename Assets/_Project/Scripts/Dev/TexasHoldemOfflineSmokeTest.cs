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

            var round = new TexasHoldemRoundStateMachine(
                shuffler,
                bestHandEvaluator,
                handComparer,
                smallBlind: 10,
                bigBlind: 20
            );

            round.StartHand(
                new[]
                {
                    "Player",
                    "Bot"
                },
                startingChips: 1000,
                dealerIndex: 0,
                botPlayerIds: new[] { "Bot" }
            );

            var bot = new SimpleTexasHoldemBot();

            while (!round.IsHandComplete)
            {
                TexasHoldemRoundPlayerState currentPlayer = round.CurrentPlayer;
                TexasHoldemPlayerAction action = currentPlayer.IsBot
                    ? bot.ChooseAction(round)
                    : ChooseSmokeTestPlayerAction(round);

                round.ApplyAction(currentPlayer.PlayerId, action);
            }

            TexasHoldemRoundResult result = round.RoundResult;

            Debug.Log($"Board: {string.Join(", ", result.BoardCards)}");
            Debug.Log($"Pot: {round.Pot}");
            Debug.Log($"Hand Log:\n{string.Join("\n", round.HandLog)}");

            foreach (TexasHoldemPlayerResult player in result.Players)
            {
                string winnerText = player.IsWinner ? "WINNER" : "LOSE";
                string bestText = player.BestHand == null ? "No showdown hand" : player.BestHand.HandResult.ToString();
                string bestCardsText = player.BestHand == null ? "-" : string.Join(", ", player.BestHand.BestCards);

                Debug.Log(
                    $"{winnerText} | {player.PlayerId} | " +
                    $"Hole: {string.Join(", ", player.HoleCards)} | " +
                    $"Best: {bestText} | " +
                    $"Best Cards: {bestCardsText}"
                );
            }
        }

        private static TexasHoldemPlayerAction ChooseSmokeTestPlayerAction(TexasHoldemRoundStateMachine round)
        {
            if (round.AmountToCall > 0)
                return round.AmountToCall >= round.CurrentPlayer.Chips
                    ? TexasHoldemPlayerAction.AllIn()
                    : TexasHoldemPlayerAction.Call();

            return TexasHoldemPlayerAction.Check();
        }
    }
}
