using System;
using System.Collections.Generic;
using System.Linq;
using Project.Domain.Cards;
using Project.Domain.Poker;

namespace Project.Application.Poker
{
    public sealed class TexasHoldemRoundStateMachine
    {
        private const int HoleCardCount = 2;
        private const int HeadsUpPlayerCount = 2;

        private readonly ICardShuffler _cardShuffler;
        private readonly BestFiveCardPokerHandEvaluator _bestHandEvaluator;
        private readonly PokerHandComparer _handComparer;
        private readonly List<TexasHoldemRoundPlayerState> _players = new();
        private readonly List<Card> _boardCards = new();
        private readonly List<string> _handLog = new();

        private Deck _deck;
        private int _dealerIndex;
        private int _smallBlindIndex;
        private int _bigBlindIndex;
        private int _currentPlayerIndex = -1;
        private int _currentStreetBet;
        private int _minimumRaiseAmount;
        private readonly bool _anteEnabled;
        private readonly int _anteAmount;
        private TexasHoldemRoundResult _roundResult;

        public TexasHoldemStreet Street { get; private set; } = TexasHoldemStreet.NotStarted;
        public int SmallBlind { get; }
        public int BigBlind { get; }
        public bool AnteEnabled => _anteEnabled;
        public int AnteAmount => _anteEnabled ? _anteAmount : 0;
        public int SettledPot => _players.Sum(player => player.TotalCommitted - player.StreetBet);
        public int LivePot => _players.Sum(player => player.TotalCommitted);
        public int Pot => LivePot;
        public int CurrentStreetBet => _currentStreetBet;
        public int MinimumBetAmount => BigBlind;
        public int MinimumRaiseAmount => _minimumRaiseAmount;
        public int DealerIndex => _dealerIndex;
        public int SmallBlindIndex => _smallBlindIndex;
        public int BigBlindIndex => _bigBlindIndex;
        public int AmountToCall => CurrentPlayer == null ? 0 : Math.Max(0, _currentStreetBet - CurrentPlayer.StreetBet);
        public bool IsHandComplete => Street == TexasHoldemStreet.HandComplete;
        public TexasHoldemRoundPlayerState CurrentPlayer => IsHandComplete || _currentPlayerIndex < 0 ? null : _players[_currentPlayerIndex];
        public IReadOnlyList<TexasHoldemRoundPlayerState> Players => _players;
        public IReadOnlyList<Card> BoardCards => _boardCards;
        public IReadOnlyList<string> HandLog => _handLog;
        public TexasHoldemRoundResult RoundResult => _roundResult;

        public TexasHoldemRoundStateMachine(
            ICardShuffler cardShuffler,
            BestFiveCardPokerHandEvaluator bestHandEvaluator,
            PokerHandComparer handComparer,
            int smallBlind = 10,
            int bigBlind = 20,
            bool anteEnabled = false,
            int anteAmount = 0)
        {
            if (smallBlind <= 0)
                throw new ArgumentOutOfRangeException(nameof(smallBlind), smallBlind, "Small blind must be positive.");

            if (bigBlind < smallBlind)
                throw new ArgumentOutOfRangeException(nameof(bigBlind), bigBlind, "Big blind must be greater than or equal to the small blind.");

            if (anteAmount < 0)
                throw new ArgumentOutOfRangeException(nameof(anteAmount), anteAmount, "Ante amount cannot be negative.");

            _cardShuffler = cardShuffler ?? throw new ArgumentNullException(nameof(cardShuffler));
            _bestHandEvaluator = bestHandEvaluator ?? throw new ArgumentNullException(nameof(bestHandEvaluator));
            _handComparer = handComparer ?? throw new ArgumentNullException(nameof(handComparer));
            SmallBlind = smallBlind;
            BigBlind = bigBlind;
            _anteEnabled = anteEnabled;
            _anteAmount = anteEnabled && anteAmount == 0 ? bigBlind / 10 : anteAmount;
        }

        public void StartHand(IReadOnlyList<string> playerIds, int startingChips, int dealerIndex = 0, IReadOnlyCollection<string> botPlayerIds = null)
        {
            if (playerIds == null)
                throw new ArgumentNullException(nameof(playerIds));

            if (startingChips <= 0)
                throw new ArgumentOutOfRangeException(nameof(startingChips), startingChips, "Starting chips must be positive.");

            StartHand(
                playerIds,
                Enumerable.Repeat(startingChips, playerIds.Count).ToArray(),
                dealerIndex,
                botPlayerIds
            );
        }

        public void StartHand(IReadOnlyList<string> playerIds, IReadOnlyList<int> startingChipsByPlayer, int dealerIndex = 0, IReadOnlyCollection<string> botPlayerIds = null)
        {
            if (playerIds == null)
                throw new ArgumentNullException(nameof(playerIds));

            if (startingChipsByPlayer == null)
                throw new ArgumentNullException(nameof(startingChipsByPlayer));

            if (playerIds.Count != HeadsUpPlayerCount)
                throw new ArgumentException("This offline round state machine currently supports heads-up play only.", nameof(playerIds));

            if (startingChipsByPlayer.Count != playerIds.Count)
                throw new ArgumentException("Starting chips count must match player count.", nameof(startingChipsByPlayer));

            if (startingChipsByPlayer.Any(chips => chips <= 0))
                throw new ArgumentOutOfRangeException(nameof(startingChipsByPlayer), "Every player must start the hand with positive chips.");

            if (dealerIndex < 0 || dealerIndex >= playerIds.Count)
                throw new ArgumentOutOfRangeException(nameof(dealerIndex), dealerIndex, "Dealer index is outside the player list.");

            if (playerIds.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("Player id cannot be null or empty.", nameof(playerIds));

            if (playerIds.Distinct().Count() != playerIds.Count)
                throw new ArgumentException("Player ids must be unique.", nameof(playerIds));

            ResetHand();

            _dealerIndex = dealerIndex;

            for (int i = 0; i < playerIds.Count; i++)
            {
                bool isBot = botPlayerIds != null && botPlayerIds.Contains(playerIds[i]);
                _players.Add(new TexasHoldemRoundPlayerState(playerIds[i], i, isBot, startingChipsByPlayer[i]));
            }

            _smallBlindIndex = playerIds.Count == 2 ? _dealerIndex : NextSeat(_dealerIndex);
            _bigBlindIndex = NextSeat(_smallBlindIndex);

            _deck = DeckFactory.CreateStandard52CardDeck();
            _deck.Shuffle(_cardShuffler);

            PostAntes();
            PostBlind(_smallBlindIndex, SmallBlind, "small blind");
            PostBlind(_bigBlindIndex, BigBlind, "big blind");
            DealHoleCards();

            Street = TexasHoldemStreet.PreFlop;
            _currentStreetBet = _players.Max(player => player.StreetBet);
            _minimumRaiseAmount = BigBlind;
            _currentPlayerIndex = playerIds.Count == 2 ? _smallBlindIndex : NextActiveSeat(_bigBlindIndex);

            _handLog.Add($"Hand started. Dealer: {_players[_dealerIndex].PlayerId}.");
        }

        public bool CanPlayerAct(string playerId)
        {
            return CurrentPlayer != null && CurrentPlayer.PlayerId == playerId;
        }

        public void ApplyAction(string playerId, TexasHoldemPlayerAction action)
        {
            EnsureHandInProgress();

            TexasHoldemRoundPlayerState player = CurrentPlayer;

            if (player.PlayerId != playerId)
                throw new InvalidOperationException($"It is {player.PlayerId}'s turn, not {playerId}'s.");

            switch (action.ActionType)
            {
                case TexasHoldemPlayerActionType.Fold:
                    ApplyFold(player);
                    break;
                case TexasHoldemPlayerActionType.Check:
                    ApplyCheck(player);
                    break;
                case TexasHoldemPlayerActionType.Call:
                    ApplyCall(player);
                    break;
                case TexasHoldemPlayerActionType.Bet:
                    ApplyBet(player, action.Amount);
                    break;
                case TexasHoldemPlayerActionType.Raise:
                    ApplyRaiseTo(player, action.Amount);
                    break;
                case TexasHoldemPlayerActionType.AllIn:
                    ApplyAllIn(player);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action.ActionType, "Unsupported action type.");
            }

            ResolveAfterAction();
        }

        public int GetAmountToCall(string playerId)
        {
            TexasHoldemRoundPlayerState player = FindPlayer(playerId);
            return Math.Max(0, _currentStreetBet - player.StreetBet);
        }

        public bool CanCheck(string playerId)
        {
            return GetAmountToCall(playerId) == 0;
        }

        public int GetMinimumLegalRaiseTo(string playerId)
        {
            FindPlayer(playerId);
            return _currentStreetBet + _minimumRaiseAmount;
        }

        public int GetMinimumLegalBet(string playerId)
        {
            FindPlayer(playerId);
            return MinimumBetAmount;
        }

        public IReadOnlyList<TexasHoldemSeatPosition> GetSeatPositions(string playerId)
        {
            TexasHoldemRoundPlayerState player = FindPlayer(playerId);
            return GetSeatPositions(player.SeatIndex).ToArray();
        }

        public string GetSeatPositionText(string playerId)
        {
            return string.Join("/", GetSeatPositions(playerId).Select(FormatSeatPosition));
        }

        public static string FormatSeatPosition(TexasHoldemSeatPosition position)
        {
            switch (position)
            {
                case TexasHoldemSeatPosition.Button:
                    return "BTN";
                case TexasHoldemSeatPosition.SmallBlind:
                    return "SB";
                case TexasHoldemSeatPosition.BigBlind:
                    return "BB";
                case TexasHoldemSeatPosition.UnderTheGun:
                    return "UTG";
                case TexasHoldemSeatPosition.Lojack:
                    return "LJ";
                case TexasHoldemSeatPosition.Hijack:
                    return "HJ";
                case TexasHoldemSeatPosition.Cutoff:
                    return "CO";
                case TexasHoldemSeatPosition.MiddlePosition:
                    return "MP";
                default:
                    return position.ToString();
            }
        }

        private void ResetHand()
        {
            _players.Clear();
            _boardCards.Clear();
            _handLog.Clear();
            _deck = null;
            _currentPlayerIndex = -1;
            _currentStreetBet = 0;
            _minimumRaiseAmount = BigBlind;
            _roundResult = null;
            Street = TexasHoldemStreet.NotStarted;
        }

        private void PostBlind(int playerIndex, int blindAmount, string blindName)
        {
            TexasHoldemRoundPlayerState player = _players[playerIndex];
            int committed = player.CommitChips(blindAmount);
            _handLog.Add($"{player.PlayerId} posts {blindName} {committed}.");
        }

        private void PostAntes()
        {
            if (!_anteEnabled || _anteAmount <= 0)
                return;

            foreach (TexasHoldemRoundPlayerState player in _players)
            {
                int committed = player.CommitToSettledPot(_anteAmount);
                _handLog.Add($"{player.PlayerId} posts ante {committed}.");
            }
        }

        private void DealHoleCards()
        {
            for (int cardIndex = 0; cardIndex < HoleCardCount; cardIndex++)
            {
                for (int i = 0; i < _players.Count; i++)
                {
                    int seatIndex = (_smallBlindIndex + i) % _players.Count;
                    _players[seatIndex].DealHoleCard(_deck.Draw());
                }
            }
        }

        private void ApplyFold(TexasHoldemRoundPlayerState player)
        {
            player.Fold();
            _handLog.Add($"{player.PlayerId} folds.");
        }

        private void ApplyCheck(TexasHoldemRoundPlayerState player)
        {
            int amountToCall = GetAmountToCall(player.PlayerId);

            if (amountToCall != 0)
                throw new InvalidOperationException($"{player.PlayerId} cannot check while facing {amountToCall}.");

            player.MarkActed();
            _handLog.Add($"{player.PlayerId} checks.");
        }

        private void ApplyCall(TexasHoldemRoundPlayerState player)
        {
            int amountToCall = GetAmountToCall(player.PlayerId);

            if (amountToCall <= 0)
                throw new InvalidOperationException($"{player.PlayerId} has nothing to call.");

            int committed = player.CommitChips(amountToCall);
            player.MarkActed();
            _handLog.Add($"{player.PlayerId} calls {committed}.");
        }

        private void ApplyBet(TexasHoldemRoundPlayerState player, int amount)
        {
            if (_currentStreetBet != 0)
                throw new InvalidOperationException("Cannot bet after a bet already exists. Use RaiseTo instead.");

            if (amount < BigBlind && amount < player.Chips)
                throw new InvalidOperationException($"Bet must be at least the big blind ({BigBlind}) unless all-in.");

            int committed = player.CommitChips(amount);
            _currentStreetBet = player.StreetBet;
            _minimumRaiseAmount = Math.Max(BigBlind, committed);
            MarkOtherPlayersUnacted(player);
            player.MarkActed();
            _handLog.Add($"{player.PlayerId} bets {committed}.");
        }

        private void ApplyRaiseTo(TexasHoldemRoundPlayerState player, int totalStreetBet)
        {
            if (_currentStreetBet == 0)
                throw new InvalidOperationException("Cannot raise when no bet exists. Use Bet instead.");

            int previousStreetBet = player.StreetBet;
            int raiseAmount = totalStreetBet - _currentStreetBet;
            int additionalAmount = totalStreetBet - previousStreetBet;

            if (totalStreetBet <= _currentStreetBet)
                throw new InvalidOperationException("Raise total must be greater than the current bet.");

            if (raiseAmount < _minimumRaiseAmount && additionalAmount < player.Chips)
                throw new InvalidOperationException($"Raise must increase the bet by at least {_minimumRaiseAmount} unless all-in.");

            int committed = player.CommitChips(additionalAmount);

            if (player.StreetBet > _currentStreetBet)
            {
                _minimumRaiseAmount = player.StreetBet - _currentStreetBet;
                _currentStreetBet = player.StreetBet;
                MarkOtherPlayersUnacted(player);
            }

            player.MarkActed();
            _handLog.Add($"{player.PlayerId} raises to {player.StreetBet} ({committed} more).");
        }

        private void ApplyAllIn(TexasHoldemRoundPlayerState player)
        {
            int previousCurrentBet = _currentStreetBet;
            int committed = player.CommitChips(player.Chips);

            if (committed <= 0)
                throw new InvalidOperationException($"{player.PlayerId} has no chips left to move all-in.");

            player.MarkActed();

            if (player.StreetBet > previousCurrentBet)
            {
                int raiseAmount = player.StreetBet - previousCurrentBet;
                _currentStreetBet = player.StreetBet;

                if (raiseAmount >= _minimumRaiseAmount || previousCurrentBet == 0)
                {
                    _minimumRaiseAmount = Math.Max(BigBlind, raiseAmount);
                    MarkOtherPlayersUnacted(player);
                }
            }

            string actionText = previousCurrentBet == 0
                ? "bets all-in"
                : player.StreetBet > previousCurrentBet
                    ? "raises all-in"
                    : "calls all-in";

            _handLog.Add($"{player.PlayerId} {actionText} for {committed} ({player.StreetBet} total).");
        }

        private void MarkOtherPlayersUnacted(TexasHoldemRoundPlayerState actingPlayer)
        {
            foreach (TexasHoldemRoundPlayerState player in _players)
            {
                if (player != actingPlayer && !player.HasFolded && !player.IsAllIn)
                    player.RequireResponseToAggression();
            }
        }

        private void ResolveAfterAction()
        {
            if (TryCompleteByFold())
                return;

            if (RemainingPlayersCanOnlyShowdown())
            {
                RunOutBoard();
                CompleteByShowdown();
                return;
            }

            if (IsBettingRoundComplete())
            {
                if (RemainingPlayersHaveNoFutureBetting())
                {
                    RunOutBoard();
                    CompleteByShowdown();
                    return;
                }

                AdvanceStreetOrShowdown();
                return;
            }

            _currentPlayerIndex = NextActiveSeat(_currentPlayerIndex);
        }

        private bool TryCompleteByFold()
        {
            TexasHoldemRoundPlayerState[] activePlayers = _players
                .Where(player => !player.HasFolded)
                .ToArray();

            if (activePlayers.Length != 1)
                return false;

            TexasHoldemRoundPlayerState winner = activePlayers[0];
            int pot = LivePot;
            winner.AwardChips(pot);
            _handLog.Add($"{winner.PlayerId} wins {pot}.");
            CompleteHand(CreateFoldWinResult(winner));
            return true;
        }

        private bool RemainingPlayersCanOnlyShowdown()
        {
            TexasHoldemRoundPlayerState[] activePlayers = _players
                .Where(player => !player.HasFolded)
                .ToArray();

            return activePlayers.Length > 1 && activePlayers.All(player => player.IsAllIn);
        }

        private bool RemainingPlayersHaveNoFutureBetting()
        {
            TexasHoldemRoundPlayerState[] activePlayers = _players
                .Where(player => !player.HasFolded)
                .ToArray();

            if (activePlayers.Length <= 1)
                return false;

            return activePlayers.Count(player => !player.IsAllIn) <= 1;
        }

        private bool IsBettingRoundComplete()
        {
            foreach (TexasHoldemRoundPlayerState player in _players)
            {
                if (player.HasFolded || player.IsAllIn)
                    continue;

                if (!player.HasActedThisStreet)
                    return false;

                if (player.StreetBet != _currentStreetBet)
                    return false;
            }

            return true;
        }

        private void AdvanceStreetOrShowdown()
        {
            switch (Street)
            {
                case TexasHoldemStreet.PreFlop:
                    DealBoardCards(TexasHoldemStreet.Flop, 3);
                    break;
                case TexasHoldemStreet.Flop:
                    DealBoardCards(TexasHoldemStreet.Turn, 1);
                    break;
                case TexasHoldemStreet.Turn:
                    DealBoardCards(TexasHoldemStreet.River, 1);
                    break;
                case TexasHoldemStreet.River:
                    CompleteByShowdown();
                    break;
                default:
                    throw new InvalidOperationException($"Cannot advance from {Street}.");
            }
        }

        private void DealBoardCards(TexasHoldemStreet nextStreet, int count)
        {
            _deck.Draw();

            for (int i = 0; i < count; i++)
            {
                _boardCards.Add(_deck.Draw());
            }

            Street = nextStreet;
            _currentStreetBet = 0;
            _minimumRaiseAmount = BigBlind;

            foreach (TexasHoldemRoundPlayerState player in _players)
            {
                player.ResetForNextStreet();
            }

            _currentPlayerIndex = FirstActiveSeatAfterDealer();
            _handLog.Add($"{Street} dealt: {string.Join(", ", _boardCards)}.");
        }

        private void RunOutBoard()
        {
            while (_boardCards.Count < 5)
            {
                if (_boardCards.Count == 0)
                    DealBoardCards(TexasHoldemStreet.Flop, 3);
                else if (_boardCards.Count == 3)
                    DealBoardCards(TexasHoldemStreet.Turn, 1);
                else
                    DealBoardCards(TexasHoldemStreet.River, 1);
            }
        }

        private void CompleteByShowdown()
        {
            Street = TexasHoldemStreet.Showdown;
            TexasHoldemRoundResult result = CreateShowdownResult();
            TexasHoldemPlayerResult[] winners = result.Players.Where(player => player.IsWinner).ToArray();
            AwardShowdownPot(winners);
            string winnersText = string.Join(", ", winners.Select(player => player.PlayerId));
            _handLog.Add($"Showdown. Winner: {winnersText}.");
            CompleteHand(result);
        }

        private void AwardShowdownPot(IReadOnlyList<TexasHoldemPlayerResult> winners)
        {
            if (winners.Count == 0)
                throw new InvalidOperationException("Cannot award showdown pot without a winner.");

            int share = LivePot / winners.Count;
            int remainder = LivePot % winners.Count;

            for (int i = 0; i < winners.Count; i++)
            {
                TexasHoldemRoundPlayerState winnerState = FindPlayer(winners[i].PlayerId);
                winnerState.AwardChips(share + (i < remainder ? 1 : 0));
            }
        }

        private TexasHoldemRoundResult CreateShowdownResult()
        {
            var temporaryResults = new List<TemporaryPlayerResult>();

            foreach (TexasHoldemRoundPlayerState player in _players)
            {
                if (player.HasFolded)
                    continue;

                var sevenCards = new List<Card>(7);
                sevenCards.AddRange(player.HoleCards);
                sevenCards.AddRange(_boardCards);

                temporaryResults.Add(new TemporaryPlayerResult(
                    player.PlayerId,
                    player.HoleCards,
                    _bestHandEvaluator.EvaluateBestHand(sevenCards)
                ));
            }

            BestPokerHandResult winningHand = temporaryResults[0].BestHand;

            for (int i = 1; i < temporaryResults.Count; i++)
            {
                if (_handComparer.Compare(temporaryResults[i].BestHand.HandResult, winningHand.HandResult) > 0)
                    winningHand = temporaryResults[i].BestHand;
            }

            List<TexasHoldemPlayerResult> finalResults = _players
                .Select(player =>
                {
                    TemporaryPlayerResult temporaryResult = temporaryResults.FirstOrDefault(result => result.PlayerId == player.PlayerId);

                    if (temporaryResult == null)
                        return new TexasHoldemPlayerResult(player.PlayerId, player.HoleCards, null, false);

                    bool isWinner = _handComparer.Compare(
                        temporaryResult.BestHand.HandResult,
                        winningHand.HandResult
                    ) == 0;

                    return new TexasHoldemPlayerResult(
                        temporaryResult.PlayerId,
                        temporaryResult.HoleCards,
                        temporaryResult.BestHand,
                        isWinner
                    );
                })
                .ToList();

            return new TexasHoldemRoundResult(_boardCards, finalResults);
        }

        private TexasHoldemRoundResult CreateFoldWinResult(TexasHoldemRoundPlayerState winner)
        {
            List<TexasHoldemPlayerResult> finalResults = _players
                .Select(player => new TexasHoldemPlayerResult(
                    player.PlayerId,
                    player.HoleCards,
                    null,
                    player == winner
                ))
                .ToList();

            return new TexasHoldemRoundResult(_boardCards, finalResults);
        }

        private void CompleteHand(TexasHoldemRoundResult result)
        {
            _roundResult = result;
            _currentPlayerIndex = -1;
            Street = TexasHoldemStreet.HandComplete;
        }

        private int NextSeat(int seatIndex)
        {
            return (seatIndex + 1) % _players.Count;
        }

        private IEnumerable<TexasHoldemSeatPosition> GetSeatPositions(int seatIndex)
        {
            if (_players.Count == 2)
            {
                if (seatIndex == _dealerIndex)
                {
                    yield return TexasHoldemSeatPosition.Button;
                    yield return TexasHoldemSeatPosition.SmallBlind;
                }
                else
                {
                    yield return TexasHoldemSeatPosition.BigBlind;
                }

                yield break;
            }

            if (seatIndex == _dealerIndex)
            {
                yield return TexasHoldemSeatPosition.Button;
                yield break;
            }

            if (seatIndex == _smallBlindIndex)
            {
                yield return TexasHoldemSeatPosition.SmallBlind;
                yield break;
            }

            if (seatIndex == _bigBlindIndex)
            {
                yield return TexasHoldemSeatPosition.BigBlind;
                yield break;
            }

            yield return GetNonBlindSeatPosition(seatIndex);
        }

        private TexasHoldemSeatPosition GetNonBlindSeatPosition(int seatIndex)
        {
            int seatsAfterBigBlind = (_players.Count + seatIndex - _bigBlindIndex) % _players.Count;
            int seatsBeforeButton = (_players.Count + _dealerIndex - seatIndex) % _players.Count;

            if (seatsAfterBigBlind == 1)
                return TexasHoldemSeatPosition.UnderTheGun;

            if (seatsBeforeButton == 1)
                return TexasHoldemSeatPosition.Cutoff;

            if (seatsBeforeButton == 2)
                return TexasHoldemSeatPosition.Hijack;

            if (seatsBeforeButton == 3)
                return TexasHoldemSeatPosition.Lojack;

            return TexasHoldemSeatPosition.MiddlePosition;
        }

        private int NextActiveSeat(int currentSeatIndex)
        {
            int nextSeat = currentSeatIndex;

            for (int i = 0; i < _players.Count; i++)
            {
                nextSeat = NextSeat(nextSeat);
                TexasHoldemRoundPlayerState player = _players[nextSeat];

                if (!player.HasFolded && !player.IsAllIn)
                    return nextSeat;
            }

            return -1;
        }

        private int FirstActiveSeatAfterDealer()
        {
            int nextSeat = _dealerIndex;

            for (int i = 0; i < _players.Count; i++)
            {
                nextSeat = NextSeat(nextSeat);
                TexasHoldemRoundPlayerState player = _players[nextSeat];

                if (!player.HasFolded && !player.IsAllIn)
                    return nextSeat;
            }

            return -1;
        }

        private TexasHoldemRoundPlayerState FindPlayer(string playerId)
        {
            TexasHoldemRoundPlayerState player = _players.FirstOrDefault(candidate => candidate.PlayerId == playerId);

            if (player == null)
                throw new ArgumentException($"Unknown player id: {playerId}", nameof(playerId));

            return player;
        }

        private void EnsureHandInProgress()
        {
            if (Street == TexasHoldemStreet.NotStarted || Street == TexasHoldemStreet.HandComplete)
                throw new InvalidOperationException("No hand is currently in progress.");

            if (CurrentPlayer == null)
                throw new InvalidOperationException("No player is currently allowed to act.");
        }

        private sealed class TemporaryPlayerResult
        {
            public string PlayerId { get; }
            public IReadOnlyList<Card> HoleCards { get; }
            public BestPokerHandResult BestHand { get; }

            public TemporaryPlayerResult(string playerId, IReadOnlyList<Card> holeCards, BestPokerHandResult bestHand)
            {
                PlayerId = playerId;
                HoleCards = holeCards;
                BestHand = bestHand;
            }
        }
    }
}
