using System;
using System.Collections.Generic;
using System.Linq;
using Project.Domain.Cards;
using Project.Domain.Poker;

namespace Project.Application.Poker
{
    public sealed class SimpleTexasHoldemBot
    {
        private const int PreFlopSimulationCount = 180;
        private const int PostFlopSimulationCount = 140;
        private const int RiverSimulationCount = 90;

        private readonly Random _random;
        private readonly PokerHandComparer _handComparer;
        private readonly BestFiveCardPokerHandEvaluator _bestHandEvaluator;
        private readonly double _aggression;
        private readonly double _looseness;
        private readonly double _bluffRate;

        public SimpleTexasHoldemBot()
            : this(new Random())
        {
        }

        public SimpleTexasHoldemBot(int seed)
            : this(new Random(seed))
        {
        }

        private SimpleTexasHoldemBot(Random random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _handComparer = new PokerHandComparer();
            _bestHandEvaluator = new BestFiveCardPokerHandEvaluator(new PokerHandEvaluator(), _handComparer);

            _aggression = RandomRange(0.35, 0.9);
            _looseness = RandomRange(0.25, 0.8);
            _bluffRate = RandomRange(0.04, 0.16);
        }

        public TexasHoldemPlayerAction ChooseAction(TexasHoldemRoundStateMachine round)
        {
            if (round == null)
                throw new ArgumentNullException(nameof(round));

            TexasHoldemRoundPlayerState bot = round.CurrentPlayer;

            if (bot == null)
                throw new InvalidOperationException("The round has no current player.");

            BotDecisionContext context = BuildDecisionContext(bot, round);

            if (round.AmountToCall <= 0)
                return ChooseActionWithoutPressure(bot, round, context);

            return ChooseActionFacingPressure(bot, round, context);
        }

        private TexasHoldemPlayerAction ChooseActionWithoutPressure(
            TexasHoldemRoundPlayerState bot,
            TexasHoldemRoundStateMachine round,
            BotDecisionContext context)
        {
            if (bot.Chips <= 0)
                return TexasHoldemPlayerAction.Check();

            if (ShouldMoveAllIn(bot, round, context, isFacingBet: false))
                return TexasHoldemPlayerAction.AllIn();

            double attackThreshold = 0.62 - (_aggression * 0.12) - (_looseness * 0.04);
            bool shouldApplyPressure = context.PlayScore >= attackThreshold || ShouldBluff(round, context, pressureBonus: 0);

            if (!shouldApplyPressure)
                return TexasHoldemPlayerAction.Check();

            return CreateAggressiveAction(bot, round, context);
        }

        private TexasHoldemPlayerAction ChooseActionFacingPressure(
            TexasHoldemRoundPlayerState bot,
            TexasHoldemRoundStateMachine round,
            BotDecisionContext context)
        {
            int callCost = Math.Min(round.AmountToCall, bot.Chips);
            double potOdds = callCost <= 0
                ? 0
                : callCost / (double)Math.Max(1, round.LivePot + callCost);

            if (round.AmountToCall >= bot.Chips)
            {
                double requiredAllInEquity = Clamp01(potOdds + 0.08 - (_looseness * 0.05));
                return context.PlayScore >= requiredAllInEquity || ShouldBluff(round, context, pressureBonus: 0.03)
                    ? TexasHoldemPlayerAction.AllIn()
                    : TexasHoldemPlayerAction.Fold();
            }

            if (ShouldMoveAllIn(bot, round, context, isFacingBet: true))
                return TexasHoldemPlayerAction.AllIn();

            bool shouldRaiseForValue = context.PlayScore >= 0.72 && Roll(0.22 + (_aggression * 0.45));

            if (shouldRaiseForValue || ShouldBluff(round, context, pressureBonus: 0.015))
                return CreateAggressiveAction(bot, round, context);

            double callThreshold = Clamp01(potOdds + 0.04 - (_looseness * 0.08));

            if (round.AmountToCall <= round.BigBlind && context.PlayScore >= 0.28)
                return TexasHoldemPlayerAction.Call();

            return context.PlayScore >= callThreshold
                ? TexasHoldemPlayerAction.Call()
                : TexasHoldemPlayerAction.Fold();
        }

        private TexasHoldemPlayerAction CreateAggressiveAction(
            TexasHoldemRoundPlayerState bot,
            TexasHoldemRoundStateMachine round,
            BotDecisionContext context)
        {
            int maximumTotalStreetBet = bot.StreetBet + bot.Chips;

            if (maximumTotalStreetBet <= bot.StreetBet)
                return round.AmountToCall == 0 ? TexasHoldemPlayerAction.Check() : TexasHoldemPlayerAction.Fold();

            if (round.CurrentStreetBet == 0)
            {
                int betAmount = ChooseBetAmount(bot, round, context);

                return betAmount >= bot.Chips
                    ? TexasHoldemPlayerAction.AllIn()
                    : TexasHoldemPlayerAction.Bet(betAmount);
            }

            int minimumRaiseTo = round.GetMinimumLegalRaiseTo(bot.PlayerId);

            if (maximumTotalStreetBet <= minimumRaiseTo)
                return TexasHoldemPlayerAction.AllIn();

            int raiseTo = ChooseRaiseTo(bot, round, context, minimumRaiseTo, maximumTotalStreetBet);

            return raiseTo >= maximumTotalStreetBet
                ? TexasHoldemPlayerAction.AllIn()
                : TexasHoldemPlayerAction.RaiseTo(raiseTo);
        }

        private int ChooseBetAmount(TexasHoldemRoundPlayerState bot, TexasHoldemRoundStateMachine round, BotDecisionContext context)
        {
            int pot = Math.Max(round.LivePot, round.BigBlind);
            double potFraction = ChoosePotFraction(context);
            int amount = RoundToBetUnit((int)Math.Round(pot * potFraction), round);

            amount = Clamp(amount, round.BigBlind, bot.Chips);

            if (amount >= bot.Chips * 0.86 && context.PlayScore >= 0.68)
                return bot.Chips;

            return amount;
        }

        private int ChooseRaiseTo(
            TexasHoldemRoundPlayerState bot,
            TexasHoldemRoundStateMachine round,
            BotDecisionContext context,
            int minimumRaiseTo,
            int maximumTotalStreetBet)
        {
            int potAfterCall = Math.Max(round.LivePot + Math.Min(round.AmountToCall, bot.Chips), round.BigBlind);
            double potFraction = ChoosePotFraction(context);
            int raiseSize = RoundToBetUnit((int)Math.Round(potAfterCall * potFraction), round);
            int raiseTo = round.CurrentStreetBet + Math.Max(round.MinimumRaiseAmount, raiseSize);

            raiseTo = Clamp(raiseTo, minimumRaiseTo, maximumTotalStreetBet);

            if (raiseTo >= maximumTotalStreetBet * 0.86 && context.PlayScore >= 0.66)
                return maximumTotalStreetBet;

            return raiseTo;
        }

        private double ChoosePotFraction(BotDecisionContext context)
        {
            if (context.PlayScore >= 0.86)
                return RandomRange(0.85, 1.35);

            if (context.PlayScore >= 0.68)
                return RandomRange(0.58, 0.95);

            if (context.HasStrongDraw)
                return RandomRange(0.48, 0.82);

            return RandomRange(0.42, 0.72);
        }

        private bool ShouldMoveAllIn(
            TexasHoldemRoundPlayerState bot,
            TexasHoldemRoundStateMachine round,
            BotDecisionContext context,
            bool isFacingBet)
        {
            int pot = Math.Max(round.LivePot, round.BigBlind);
            double stackToPotRatio = bot.Chips / (double)Math.Max(1, pot);

            if (bot.Chips <= round.BigBlind * 3 && context.PlayScore >= 0.44)
                return Roll(0.65 + (_aggression * 0.2));

            if (context.PlayScore >= 0.9 && stackToPotRatio <= 3.4)
                return Roll(0.55 + (_aggression * 0.35));

            if (context.PlayScore >= 0.76 && stackToPotRatio <= 1.55)
                return Roll(0.35 + (_aggression * 0.35));

            if (round.Street == TexasHoldemStreet.PreFlop && context.PreFlopScore >= 0.86 && stackToPotRatio <= 4.8)
                return Roll(0.18 + (_aggression * 0.22));

            if (!isFacingBet && context.PlayScore < 0.42 && stackToPotRatio <= 1.1)
                return Roll(_bluffRate * 0.45);

            return false;
        }

        private bool ShouldBluff(TexasHoldemRoundStateMachine round, BotDecisionContext context, double pressureBonus)
        {
            if (context.PlayScore >= 0.5)
                return false;

            double streetMultiplier = round.Street == TexasHoldemStreet.PreFlop ? 0.55 : 1.0;
            double drawBonus = context.HasStrongDraw ? 0.08 : 0;
            double chance = (_bluffRate + pressureBonus + drawBonus) * streetMultiplier;

            return Roll(chance);
        }

        private BotDecisionContext BuildDecisionContext(TexasHoldemRoundPlayerState bot, TexasHoldemRoundStateMachine round)
        {
            double equity = EstimateEquity(bot, round);
            double madeHandScore = EvaluateMadeHandScore(bot, round.BoardCards);
            double preFlopScore = EvaluatePreFlopScore(bot.HoleCards);
            bool hasStrongDraw = HasStrongDraw(bot, round.BoardCards);

            if (round.Street == TexasHoldemStreet.PreFlop)
                equity = (equity * 0.68) + (preFlopScore * 0.32);

            double playScore = (equity * 0.76) + (madeHandScore * 0.18);

            if (hasStrongDraw)
                playScore += 0.05 + (_aggression * 0.03);

            playScore += RandomRange(-0.035, 0.035);

            return new BotDecisionContext(
                Clamp01(equity),
                Clamp01(madeHandScore),
                Clamp01(preFlopScore),
                Clamp01(playScore),
                hasStrongDraw
            );
        }

        private double EstimateEquity(TexasHoldemRoundPlayerState bot, TexasHoldemRoundStateMachine round)
        {
            int simulationCount = round.BoardCards.Count == 5
                ? RiverSimulationCount
                : round.BoardCards.Count == 0
                    ? PreFlopSimulationCount
                    : PostFlopSimulationCount;

            var knownCards = new HashSet<Card>(bot.HoleCards);

            foreach (Card boardCard in round.BoardCards)
            {
                knownCards.Add(boardCard);
            }

            List<Card> availableCards = DeckFactory
                .CreateStandard52Cards()
                .Where(card => !knownCards.Contains(card))
                .ToList();

            int wins = 0;
            int ties = 0;

            for (int i = 0; i < simulationCount; i++)
            {
                var sampleDeck = new List<Card>(availableCards);
                Shuffle(sampleDeck);

                Card opponentCard1 = sampleDeck[0];
                Card opponentCard2 = sampleDeck[1];

                var board = new List<Card>(round.BoardCards);
                int cardsNeeded = 5 - board.Count;

                for (int cardIndex = 0; cardIndex < cardsNeeded; cardIndex++)
                {
                    board.Add(sampleDeck[2 + cardIndex]);
                }

                BestPokerHandResult botBestHand = _bestHandEvaluator.EvaluateBestHand(bot.HoleCards.Concat(board).ToArray());
                BestPokerHandResult opponentBestHand = _bestHandEvaluator.EvaluateBestHand(new[] { opponentCard1, opponentCard2 }.Concat(board).ToArray());

                int comparison = _handComparer.Compare(botBestHand.HandResult, opponentBestHand.HandResult);

                if (comparison > 0)
                    wins++;
                else if (comparison == 0)
                    ties++;
            }

            return (wins + (ties * 0.5)) / simulationCount;
        }

        private double EvaluateMadeHandScore(TexasHoldemRoundPlayerState bot, IReadOnlyList<Card> boardCards)
        {
            if (boardCards.Count == 0)
                return EvaluatePreFlopScore(bot.HoleCards);

            var cards = new List<Card>(7);
            cards.AddRange(bot.HoleCards);
            cards.AddRange(boardCards);

            if (cards.Count < 5)
                return EvaluatePreFlopScore(bot.HoleCards);

            BestPokerHandResult bestHand = _bestHandEvaluator.EvaluateBestHand(cards);
            double score = RankToScore(bestHand.HandResult.Rank);

            if (bestHand.HandResult.TieBreakers.Count > 0)
                score += (bestHand.HandResult.TieBreakers[0] / 14.0) * 0.035;

            return Clamp01(score);
        }

        private static double EvaluatePreFlopScore(IReadOnlyList<Card> holeCards)
        {
            if (holeCards.Count < 2)
                return 0;

            int firstRank = (int)holeCards[0].Rank;
            int secondRank = (int)holeCards[1].Rank;
            int highRank = Math.Max(firstRank, secondRank);
            int lowRank = Math.Min(firstRank, secondRank);
            bool isPair = firstRank == secondRank;
            bool isSuited = holeCards[0].Suit == holeCards[1].Suit;
            int gap = highRank - lowRank;

            if (isPair)
                return Clamp01(0.48 + (highRank / 14.0 * 0.42));

            double score = 0.16 + ((highRank + lowRank) / 28.0 * 0.48);

            if (highRank >= (int)Rank.Ace)
                score += 0.06;
            else if (highRank >= (int)Rank.King)
                score += 0.04;

            if (isSuited)
                score += 0.045;

            if (gap == 1)
                score += 0.04;
            else if (gap == 2)
                score += 0.018;
            else if (gap >= 5)
                score -= 0.055;

            if (lowRank <= (int)Rank.Five && highRank < (int)Rank.Jack)
                score -= 0.035;

            return Clamp01(score);
        }

        private static double RankToScore(PokerHandRank rank)
        {
            switch (rank)
            {
                case PokerHandRank.RoyalFlush:
                    return 1.0;
                case PokerHandRank.StraightFlush:
                    return 0.98;
                case PokerHandRank.FourOfAKind:
                    return 0.95;
                case PokerHandRank.FullHouse:
                    return 0.9;
                case PokerHandRank.Flush:
                    return 0.82;
                case PokerHandRank.Straight:
                    return 0.79;
                case PokerHandRank.ThreeOfAKind:
                    return 0.72;
                case PokerHandRank.TwoPair:
                    return 0.62;
                case PokerHandRank.OnePair:
                    return 0.45;
                default:
                    return 0.26;
            }
        }

        private static bool HasStrongDraw(TexasHoldemRoundPlayerState bot, IReadOnlyList<Card> boardCards)
        {
            if (boardCards.Count >= 5)
                return false;

            var cards = new List<Card>(7);
            cards.AddRange(bot.HoleCards);
            cards.AddRange(boardCards);

            bool hasFlushDraw = cards
                .GroupBy(card => card.Suit)
                .Any(group => group.Count() >= 4);

            return hasFlushDraw || HasStraightDraw(cards);
        }

        private static bool HasStraightDraw(IReadOnlyList<Card> cards)
        {
            var ranks = new HashSet<int>(cards.Select(card => (int)card.Rank));

            if (ranks.Contains((int)Rank.Ace))
                ranks.Add(1);

            for (int startRank = 1; startRank <= 10; startRank++)
            {
                int matchingRanks = 0;

                for (int offset = 0; offset < 5; offset++)
                {
                    if (ranks.Contains(startRank + offset))
                        matchingRanks++;
                }

                if (matchingRanks >= 4)
                    return true;
            }

            return false;
        }

        private void Shuffle(IList<Card> cards)
        {
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int swapIndex = _random.Next(i + 1);
                Card temp = cards[i];
                cards[i] = cards[swapIndex];
                cards[swapIndex] = temp;
            }
        }

        private int RoundToBetUnit(int amount, TexasHoldemRoundStateMachine round)
        {
            int unit = Math.Max(1, round.SmallBlind);
            int rounded = (int)Math.Round(amount / (double)unit) * unit;
            return Math.Max(unit, rounded);
        }

        private double RandomRange(double min, double max)
        {
            return min + (_random.NextDouble() * (max - min));
        }

        private bool Roll(double chance)
        {
            return _random.NextDouble() < Clamp01(chance);
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
                return min;

            return value > max ? max : value;
        }

        private static double Clamp01(double value)
        {
            if (value < 0)
                return 0;

            return value > 1 ? 1 : value;
        }

        private readonly struct BotDecisionContext
        {
            public double Equity { get; }
            public double MadeHandScore { get; }
            public double PreFlopScore { get; }
            public double PlayScore { get; }
            public bool HasStrongDraw { get; }

            public BotDecisionContext(
                double equity,
                double madeHandScore,
                double preFlopScore,
                double playScore,
                bool hasStrongDraw)
            {
                Equity = equity;
                MadeHandScore = madeHandScore;
                PreFlopScore = preFlopScore;
                PlayScore = playScore;
                HasStrongDraw = hasStrongDraw;
            }
        }
    }
}
