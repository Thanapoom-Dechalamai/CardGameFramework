using System;
using System.Collections.Generic;
using System.Linq;
using Project.Domain.Cards;
using Project.Domain.Poker;

namespace Project.Application.Poker
{
    public sealed class TexasHoldemOfflineRoundSimulator
    {
        private const int HoleCardCount = 2;
        private const int BoardCardCount = 5;

        private readonly ICardShuffler _cardShuffler;
        private readonly BestFiveCardPokerHandEvaluator _bestHandEvaluator;
        private readonly PokerHandComparer _handComparer;

        public TexasHoldemOfflineRoundSimulator(
            ICardShuffler cardShuffler,
            BestFiveCardPokerHandEvaluator bestHandEvaluator,
            PokerHandComparer handComparer)
        {
            _cardShuffler = cardShuffler ?? throw new ArgumentNullException(nameof(cardShuffler));
            _bestHandEvaluator = bestHandEvaluator ?? throw new ArgumentNullException(nameof(bestHandEvaluator));
            _handComparer = handComparer ?? throw new ArgumentNullException(nameof(handComparer));
        }

        public TexasHoldemRoundResult PlayRound(IReadOnlyList<string> playerIds)
        {
            if (playerIds == null)
                throw new ArgumentNullException(nameof(playerIds));

            if (playerIds.Count < 2)
                throw new ArgumentException("Texas Hold'em requires at least 2 players.", nameof(playerIds));

            if (playerIds.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("Player id cannot be null or empty.", nameof(playerIds));

            if (playerIds.Distinct().Count() != playerIds.Count)
                throw new ArgumentException("Player ids must be unique.", nameof(playerIds));

            Deck deck = DeckFactory.CreateStandard52CardDeck();
            deck.Shuffle(_cardShuffler);

            var holeCardsByPlayer = new Dictionary<string, List<Card>>();

            foreach (string playerId in playerIds)
            {
                holeCardsByPlayer[playerId] = new List<Card>(HoleCardCount);
            }

            // Deal one card at a time, like real poker.
            for (int cardIndex = 0; cardIndex < HoleCardCount; cardIndex++)
            {
                foreach (string playerId in playerIds)
                {
                    holeCardsByPlayer[playerId].Add(deck.Draw());
                }
            }

            IReadOnlyList<Card> boardCards = deck.Draw(BoardCardCount);

            var temporaryResults = new List<TemporaryPlayerResult>();

            foreach (string playerId in playerIds)
            {
                List<Card> sevenCards = new List<Card>(7);
                sevenCards.AddRange(holeCardsByPlayer[playerId]);
                sevenCards.AddRange(boardCards);

                BestPokerHandResult bestHand = _bestHandEvaluator.EvaluateBestHand(sevenCards);

                temporaryResults.Add(new TemporaryPlayerResult(
                    playerId,
                    holeCardsByPlayer[playerId],
                    bestHand
                ));
            }

            BestPokerHandResult winningHand = temporaryResults[0].BestHand;

            for (int i = 1; i < temporaryResults.Count; i++)
            {
                if (_handComparer.Compare(temporaryResults[i].BestHand.HandResult, winningHand.HandResult) > 0)
                {
                    winningHand = temporaryResults[i].BestHand;
                }
            }

            List<TexasHoldemPlayerResult> finalResults = temporaryResults
                .Select(result =>
                {
                    bool isWinner = _handComparer.Compare(
                        result.BestHand.HandResult,
                        winningHand.HandResult
                    ) == 0;

                    return new TexasHoldemPlayerResult(
                        result.PlayerId,
                        result.HoleCards,
                        result.BestHand,
                        isWinner
                    );
                })
                .ToList();

            return new TexasHoldemRoundResult(boardCards, finalResults);
        }

        private sealed class TemporaryPlayerResult
        {
            public string PlayerId { get; }
            public IReadOnlyList<Card> HoleCards { get; }
            public BestPokerHandResult BestHand { get; }

            public TemporaryPlayerResult(
                string playerId,
                IReadOnlyList<Card> holeCards,
                BestPokerHandResult bestHand)
            {
                PlayerId = playerId;
                HoleCards = holeCards;
                BestHand = bestHand;
            }
        }
    }
}