using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Project.Application.Poker;
using Project.Config;
using Project.Domain.Cards;
using Project.Domain.Poker;
using Project.UI.Components;
using Project.UI.Views;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Project.Dev
{
    public sealed class TexasHoldemOfflineGraphicDemo : MonoBehaviour
    {
        private const string HumanPlayerId = "Player";
        private const string BotPlayerId = "Bot";

        [Header("Config")]
        [SerializeField] private CardSpriteDatabase cardSpriteDatabase;
        [SerializeField] private CardView cardViewPrefab;

        [Header("Scene References")]
        [SerializeField] private RectTransform cardLayer;
        [SerializeField] private RectTransform deckAnchor;
        [SerializeField] private Image deckImage;
        [SerializeField] private TMP_Text tableStatusText;

        [Header("Slots")]
        [FormerlySerializedAs("player1Slots")]
        [SerializeField] private RectTransform[] playerSlots;
        [FormerlySerializedAs("player2Slots")]
        [SerializeField] private RectTransform[] botSlots;
        [SerializeField] private RectTransform[] boardSlots;

        [Header("Player HUD")]
        [SerializeField] private Image playerProfileImage;
        [SerializeField] private Image botProfileImage;
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text botNameText;
        [SerializeField] private TMP_Text playerChipsText;
        [SerializeField] private TMP_Text botChipsText;
        [SerializeField] private TMP_Text playerActionText;
        [SerializeField] private TMP_Text botActionText;
        [SerializeField] private TMP_Text playerTimerText;
        [SerializeField] private TMP_Text botTimerText;
        [SerializeField] private GameObject playerTimerBackground;
        [SerializeField] private GameObject botTimerBackground;
        [SerializeField] private TMP_Text playerHandRankText;
        [SerializeField] private TMP_Text botHandRankText;
        [SerializeField] private GameObject playerHandRankBackground;
        [SerializeField] private GameObject botHandRankBackground;

        [Header("Pot And Bets")]
        [SerializeField] private TMP_Text livePotText;
        [SerializeField] private TMP_Text settledPotText;
        [SerializeField] private TMP_Text playerStreetBetText;
        [SerializeField] private TMP_Text botStreetBetText;
        [SerializeField] private RectTransform playerProfileAnchor;
        [SerializeField] private RectTransform botProfileAnchor;
        [SerializeField] private RectTransform playerBetAnchor;
        [SerializeField] private RectTransform botBetAnchor;
        [SerializeField] private RectTransform potAnchor;
        [SerializeField] private RectTransform settledPotChipAnchor;
        [SerializeField] private RectTransform settledPotChipVisual;

        [Header("Actions")]
        [SerializeField] private Button leftActionButton;
        [SerializeField] private Button middleActionButton;
        [SerializeField] private Button rightActionButton;
        [SerializeField] private TMP_Text leftActionButtonText;
        [SerializeField] private TMP_Text middleActionButtonText;
        [SerializeField] private TMP_Text rightActionButtonText;

        [Header("Bet Slider")]
        [SerializeField] private GameObject betSliderContainer;
        [SerializeField] private Slider betAmountSlider;
        [SerializeField] private GameObject selectedBetAmountContainer;
        [SerializeField] private TMP_Text selectedBetAmountText;
        [SerializeField] private Button decreaseBetButton;
        [SerializeField] private Button increaseBetButton;

        [Header("Chip Animation")]
        [SerializeField] private RectTransform chipAnimationLayer;
        [SerializeField] private RectTransform chipMoveVisualPrefab;
        [SerializeField] private RectTransform playerTableChipVisual;
        [SerializeField] private RectTransform botTableChipVisual;
        [SerializeField] private float chipMoveStartDelay = 0f;
        [SerializeField] private float chipMoveDuration = 0.25f;
        [SerializeField] private AnimationCurve chipMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private float collectStreetBetsDelaySeconds = 2f;
        [SerializeField] private float awardPotDelaySeconds = 2f;
        [SerializeField] private float winnerBetHoldSeconds = 2f;

        [Header("Winner Visuals")]
        [SerializeField] private GameObject playerWinnerVisual;
        [SerializeField] private GameObject botWinnerVisual;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [FormerlySerializedAs("dealCardClip")]
        [SerializeField] private AudioClip drawCardClip;
        [SerializeField] private AudioClip drawCardClip2;
        [SerializeField] private AudioClip checkVoiceClip;
        [SerializeField] private AudioClip checkKnockClip;
        [SerializeField] private AudioClip callVoiceClip;
        [SerializeField] private AudioClip betVoiceClip;
        [SerializeField] private AudioClip raiseVoiceClip;
        [SerializeField] private AudioClip foldVoiceClip;
        [SerializeField] private AudioClip allInVoiceClip;
        [SerializeField] private AudioClip chipsEarnedClip;
        [SerializeField] private AudioClip placeChipsClip;
        [SerializeField] private AudioClip collectChipsClip;
        [SerializeField] private AudioClip foldCardsClip;
        [SerializeField] private AudioClip winnerClip;

        [Header("Hand Rank Colors")]
        [SerializeField] private Color highCardRankColor = Color.white;
        [SerializeField] private Color pairRankColor = new Color(1f, 0.84f, 0f);
        [SerializeField] private Color threeKindRankColor = new Color(0.55f, 0.85f, 1f);
        [SerializeField] private Color straightPlusRankColor = new Color(0.25f, 0.95f, 0.35f);
        [SerializeField] private Color fourKindRankColor = new Color(0.72f, 0.35f, 1f);
        [SerializeField] private Color straightFlushRankColor = new Color(1f, 0.16f, 0.16f);

        [Header("Game Rules")]
        [SerializeField] private int startingChips = 100000;
        [SerializeField] private int smallBlind = 400;
        [SerializeField] private int bigBlind = 800;
        [SerializeField] private bool anteEnabled = true;
        [SerializeField] private int anteAmount = 80;
        [SerializeField] private float turnTimeLimitSeconds = 15f;
        [SerializeField] private float autoNewHandDelaySeconds = 5f;
        [SerializeField] private Vector2 botThinkDelayRangeSeconds = new Vector2(3f, 5f);

        [Header("Animation")]
        [SerializeField] private float dealDelay = 0.12f;
        [SerializeField] private float moveDuration = 0.22f;
        [SerializeField] private float cardFlipDuration = 0.24f;

        [Header("Debug")]
        [SerializeField] private bool useFixedSeed = false;
        [SerializeField] private int seed = 12345;

        private readonly List<CardView> _spawnedCards = new();
        private readonly List<CardView> _playerHoleCardViews = new();
        private readonly Dictionary<Card, CardView> _cardViewsByCard = new();
        private readonly Dictionary<string, string> _lastActionByPlayerId = new();

        private CardAnimationRunner _animationRunner;
        private TexasHoldemRoundStateMachine _round;
        private SimpleTexasHoldemBot _bot;
        private BestFiveCardPokerHandEvaluator _previewBestHandEvaluator;
        private int _dealtBoardCardCount;
        private Coroutine _runningCoroutine;
        private Coroutine _autoNewHandCoroutine;
        private bool _controlsInitialized;
        private bool _inputLocked;
        private bool _isBotThinking;
        private string _activeTimerPlayerId;
        private TexasHoldemStreet _activeTimerStreet = TexasHoldemStreet.NotStarted;
        private float _turnTimeRemaining;
        private int _selectedBetOrRaiseTo;
        private int _playerStack;
        private int _botStack;
        private bool _hasPersistentStacks;
        private int _dealerIndex;
        private int _visualPlayerStreetBet;
        private int _visualBotStreetBet;
        private int _visualSettledPot;
        private int _displayedPlayerStack;
        private int _displayedBotStack;
        private bool _playerHoleCardsFaceUp = true;
        private bool _playerHoleCardsFlipInProgress;

        private void Awake()
        {
            _animationRunner = GetComponent<CardAnimationRunner>();

            if (_animationRunner == null)
                _animationRunner = gameObject.AddComponent<CardAnimationRunner>();

            if (audioSource == null)
                TryGetComponent<AudioSource>(out audioSource);
        }

        private void Update()
        {
            UpdateTurnTimer();
        }

        private void Start()
        {
            StartGame();
        }

        public void StartGame()
        {
            if (_runningCoroutine != null)
                StopCoroutine(_runningCoroutine);

            if (_autoNewHandCoroutine != null)
                StopCoroutine(_autoNewHandCoroutine);

            _inputLocked = true;
            _runningCoroutine = StartCoroutine(RunInteractiveHand());
        }

        private IEnumerator RunInteractiveHand()
        {
            ValidateReferences();
            InitializeControls();
            ClearTable();

            if (deckImage != null)
                deckImage.sprite = cardSpriteDatabase.CardBackSprite;

            _round = CreateRoundStateMachine();
            _bot = new SimpleTexasHoldemBot();
            _dealtBoardCardCount = 0;
            _lastActionByPlayerId.Clear();
            EnsurePersistentStacks();

            _round.StartHand(
                new[]
                {
                    HumanPlayerId,
                    BotPlayerId
                },
                new[]
                {
                    _playerStack,
                    _botStack
                },
                dealerIndex: _dealerIndex,
                botPlayerIds: new[] { BotPlayerId }
            );
            ResetVisualPotStateFromRound();
            ResetDisplayedStacksFromRound();

            _lastActionByPlayerId[HumanPlayerId] = FormatPostedBlindLabel(HumanPlayerId);
            _lastActionByPlayerId[BotPlayerId] = FormatPostedBlindLabel(BotPlayerId);

            TexasHoldemRoundPlayerState player = _round.Players[0];
            TexasHoldemRoundPlayerState bot = _round.Players[1];

            SetStatus("Dealing...");
            RefreshHud();

            yield return DealCard(player.HoleCards[0], playerSlots[0], true);
            yield return WaitDealDelay();

            yield return DealCard(bot.HoleCards[0], botSlots[0], false);
            yield return WaitDealDelay();

            yield return DealCard(player.HoleCards[1], playerSlots[1], true);
            yield return WaitDealDelay();

            yield return DealCard(bot.HoleCards[1], botSlots[1], false);
            yield return WaitDealDelay();

            _inputLocked = false;
            RefreshHud();
            yield return ResolveBotTurns();
        }

        private TexasHoldemRoundStateMachine CreateRoundStateMachine()
        {
            ICardShuffler shuffler = useFixedSeed
                ? new SystemRandomCardShuffler(seed)
                : new SecureRandomCardShuffler();

            var handComparer = new PokerHandComparer();

            _previewBestHandEvaluator = new BestFiveCardPokerHandEvaluator(
                new PokerHandEvaluator(),
                handComparer
            );

            return new TexasHoldemRoundStateMachine(
                shuffler,
                _previewBestHandEvaluator,
                handComparer,
                smallBlind,
                bigBlind,
                anteEnabled,
                anteAmount
            );
        }

        public void OnLeftActionPressed()
        {
            if (CanHumanAct())
                ApplyHumanAction(TexasHoldemPlayerAction.Fold());
        }

        public void OnMiddleActionPressed()
        {
            if (!CanHumanAct())
                return;

            if (_round.AmountToCall == 0)
                ApplyHumanAction(TexasHoldemPlayerAction.Check());
            else if (_round.AmountToCall <= _round.CurrentPlayer.Chips)
                ApplyHumanAction(TexasHoldemPlayerAction.Call());
        }

        public void OnRightActionPressed()
        {
            if (!CanHumanAct())
                return;

            TexasHoldemRoundPlayerState player = _round.CurrentPlayer;

            if (_round.AmountToCall >= player.Chips)
            {
                ApplyHumanAction(TexasHoldemPlayerAction.AllIn());
                return;
            }

            if (_round.CurrentStreetBet == 0)
            {
                if (_selectedBetOrRaiseTo >= player.Chips)
                    ApplyHumanAction(TexasHoldemPlayerAction.AllIn());
                else
                    ApplyHumanAction(TexasHoldemPlayerAction.Bet(_selectedBetOrRaiseTo));

                return;
            }

            if (player.StreetBet + player.Chips < _round.GetMinimumLegalRaiseTo(HumanPlayerId))
            {
                ApplyHumanAction(TexasHoldemPlayerAction.AllIn());
                return;
            }

            int additionalAmount = _selectedBetOrRaiseTo - player.StreetBet;

            if (additionalAmount >= player.Chips)
                ApplyHumanAction(TexasHoldemPlayerAction.AllIn());
            else
                ApplyHumanAction(TexasHoldemPlayerAction.RaiseTo(_selectedBetOrRaiseTo));
        }

        public void DecreaseBetAmount()
        {
            StepBetAmount(-bigBlind);
        }

        public void IncreaseBetAmount()
        {
            StepBetAmount(bigBlind);
        }

        private void StepBetAmount(int delta)
        {
            if (betAmountSlider == null || !betAmountSlider.gameObject.activeInHierarchy)
                return;

            betAmountSlider.value = Mathf.Clamp(
                betAmountSlider.value + delta,
                betAmountSlider.minValue,
                betAmountSlider.maxValue
            );
        }

        private IEnumerator DealCard(Card card, RectTransform targetSlot, bool faceUp)
        {
            Sprite frontSprite = cardSpriteDatabase.GetSprite(card);
            Sprite backSprite = cardSpriteDatabase.CardBackSprite;

            CardView cardView = Instantiate(cardViewPrefab, cardLayer);
            PlayRandomDrawCardSound();
            cardView.RectTransform.position = deckAnchor.position;
            cardView.RectTransform.localScale = Vector3.one;
            cardView.Setup(card, frontSprite, backSprite, faceUp);

            _spawnedCards.Add(cardView);
            _cardViewsByCard[card] = cardView;
            RegisterPlayerHoleCardToggle(cardView);

            yield return _animationRunner.MoveToSlot(cardView, targetSlot, moveDuration);
        }

        private void RegisterPlayerHoleCardToggle(CardView cardView)
        {
            if (_round == null || _round.Players.Count == 0)
                return;

            TexasHoldemRoundPlayerState player = _round.Players[0];

            if (!player.HoleCards.Contains(cardView.Card))
                return;

            _playerHoleCardViews.Add(cardView);

            CardClickHandler clickHandler = cardView.GetComponent<CardClickHandler>();

            if (clickHandler == null)
                clickHandler = cardView.gameObject.AddComponent<CardClickHandler>();

            clickHandler.Initialize(OnPlayerHoleCardClicked);
        }

        private void OnPlayerHoleCardClicked(CardView cardView)
        {
            if (_playerHoleCardsFlipInProgress || _playerHoleCardViews.Count < 2)
                return;

            TexasHoldemRoundPlayerState player = _round != null && _round.Players.Count > 0
                ? _round.Players[0]
                : null;

            if (player != null && player.HasFolded && !_round.IsHandComplete)
                return;

            StartCoroutine(TogglePlayerHoleCardsFaceUp());
        }

        private IEnumerator TogglePlayerHoleCardsFaceUp()
        {
            _playerHoleCardsFlipInProgress = true;
            _playerHoleCardsFaceUp = !_playerHoleCardsFaceUp;
            PlayRandomDrawCardSound();

            yield return _animationRunner.FlipFaces(
                _playerHoleCardViews,
                _playerHoleCardsFaceUp,
                cardFlipDuration
            );

            _playerHoleCardsFlipInProgress = false;
            RefreshFoldedCardDimStates();
            RefreshCurrentHighlights();
        }

        private void ApplyHumanAction(TexasHoldemPlayerAction action)
        {
            if (_runningCoroutine != null)
                StopCoroutine(_runningCoroutine);

            _inputLocked = true;
            StopTurnTimer();
            RefreshHud();
            _runningCoroutine = StartCoroutine(ApplyActionAndContinue(HumanPlayerId, action));
        }

        private IEnumerator ApplyActionAndContinue(string playerId, TexasHoldemPlayerAction action)
        {
            Dictionary<string, int> streetBetsBeforeAction = CaptureStreetBets();
            TexasHoldemRoundPlayerState actingPlayer = GetPlayer(playerId);
            int committedBeforeAction = actingPlayer.TotalCommitted;
            int boardCountBeforeAction = _round.BoardCards.Count;
            bool completedBeforeAction = _round.IsHandComplete;

            _round.ApplyAction(playerId, action);

            int committedAmount = actingPlayer.TotalCommitted - committedBeforeAction;
            _lastActionByPlayerId[playerId] = FormatActionLabel(action, committedAmount);
            SetVisualStreetBet(playerId, streetBetsBeforeAction[playerId] + committedAmount);
            PlayActionSound(action);

            if (action.ActionType == TexasHoldemPlayerActionType.Fold)
            {
                RefreshFoldedCardDimStates();
                yield return SetHoleCardsFaceUp(actingPlayer, false, playDrawSound: false);
            }

            if (committedAmount > 0)
                SetDisplayedStack(playerId, GetDisplayedStack(playerId) - committedAmount);

            RefreshHud();

            if (committedAmount > 0)
            {
                PlaySound(placeChipsClip);
                yield return AnimateChips(GetProfileAnchor(playerId), GetBetAnchor(playerId), committedAmount);
            }

            bool streetAdvanced = !completedBeforeAction && _round.BoardCards.Count > boardCountBeforeAction;
            bool shouldMoveStreetBetsToPot = streetAdvanced || (!completedBeforeAction && _round.IsHandComplete);

            if (shouldMoveStreetBetsToPot)
                yield return AnimateStreetBetsToPot(streetBetsBeforeAction, playerId, committedAmount);

            yield return RefreshTableAfterAction();
            yield return ResolveBotTurns();
        }

        private IEnumerator ResolveBotTurns()
        {
            while (_round != null && !_round.IsHandComplete && _round.CurrentPlayer != null && _round.CurrentPlayer.IsBot)
            {
                _inputLocked = true;
                _isBotThinking = true;
                RefreshHud();
                yield return WaitBotThinkDelay();
                _isBotThinking = false;
                StopTurnTimer();

                string botPlayerId = _round.CurrentPlayer.PlayerId;
                TexasHoldemPlayerAction botAction = _bot.ChooseAction(_round);

                yield return ApplyActionAndContinue(botPlayerId, botAction);
                yield break;
            }

            _inputLocked = false;
            _isBotThinking = false;
            RefreshHud();
        }

        private IEnumerator RefreshTableAfterAction()
        {
            while (_round != null && _dealtBoardCardCount < _round.BoardCards.Count)
            {
                Card boardCard = _round.BoardCards[_dealtBoardCardCount];
                yield return DealCard(boardCard, boardSlots[_dealtBoardCardCount], true);
                _dealtBoardCardCount++;
                RefreshHud();
                yield return WaitDealDelay();
            }

            if (_round != null && _round.IsHandComplete)
            {
                _inputLocked = true;
                yield return RevealCardsForHandComplete();
                RefreshHud();
                ShowResult(_round.RoundResult);
                HighlightWinnerCards(_round.RoundResult);
                yield return AnimateSettledPotToWinners(_round.RoundResult);
                CaptureStacksForNextHand();
                RefreshHud();

                if (autoNewHandDelaySeconds > 0f)
                    _autoNewHandCoroutine = StartCoroutine(StartNextHandAfterDelay());
            }
            else
            {
                RefreshHandPreviewHighlights();
                RefreshHud();
            }
        }

        private IEnumerator StartNextHandAfterDelay()
        {
            yield return new WaitForSeconds(autoNewHandDelaySeconds);
            StartGame();
        }

        private void EnsurePersistentStacks()
        {
            if (_hasPersistentStacks)
                return;

            _playerStack = startingChips;
            _botStack = startingChips;
            _hasPersistentStacks = true;
        }

        private void CaptureStacksForNextHand()
        {
            if (_round == null || _round.Players.Count < 2)
                return;

            _playerStack = Mathf.Max(0, _round.Players[0].Chips);
            _botStack = Mathf.Max(0, _round.Players[1].Chips);
            _dealerIndex = _dealerIndex == 0 ? 1 : 0;

            if (_playerStack <= 0 || _botStack <= 0)
            {
                _playerStack = startingChips;
                _botStack = startingChips;
                _dealerIndex = 0;
            }
        }

        private void ResetVisualPotStateFromRound()
        {
            if (_round == null || _round.Players.Count < 2)
                return;

            _visualPlayerStreetBet = _round.Players[0].StreetBet;
            _visualBotStreetBet = _round.Players[1].StreetBet;
            _visualSettledPot = _round.SettledPot;
        }

        private void ResetDisplayedStacksFromRound()
        {
            if (_round == null || _round.Players.Count < 2)
                return;

            _displayedPlayerStack = _round.Players[0].Chips;
            _displayedBotStack = _round.Players[1].Chips;
        }

        private int GetDisplayedStack(string playerId)
        {
            return playerId == HumanPlayerId ? _displayedPlayerStack : _displayedBotStack;
        }

        private void SetDisplayedStack(string playerId, int amount)
        {
            if (playerId == HumanPlayerId)
                _displayedPlayerStack = Mathf.Max(0, amount);
            else
                _displayedBotStack = Mathf.Max(0, amount);
        }

        private int GetVisualStreetBet(string playerId)
        {
            return playerId == HumanPlayerId ? _visualPlayerStreetBet : _visualBotStreetBet;
        }

        private void SetVisualStreetBet(string playerId, int amount)
        {
            if (playerId == HumanPlayerId)
                _visualPlayerStreetBet = Mathf.Max(0, amount);
            else
                _visualBotStreetBet = Mathf.Max(0, amount);
        }

        private RectTransform GetSettledPotAnchor()
        {
            return settledPotChipAnchor != null ? settledPotChipAnchor : potAnchor;
        }

        private IEnumerator AnimateStreetBetsToPot(
            IReadOnlyDictionary<string, int> streetBetsBeforeAction,
            string actingPlayerId,
            int actingCommittedAmount)
        {
            if (collectStreetBetsDelaySeconds > 0f)
                yield return new WaitForSeconds(collectStreetBetsDelaySeconds);

            bool playedCollectSound = false;

            foreach (KeyValuePair<string, int> pair in streetBetsBeforeAction)
            {
                int amount = pair.Value;

                if (pair.Key == actingPlayerId)
                    amount += actingCommittedAmount;

                if (amount > 0)
                {
                    if (!playedCollectSound)
                    {
                        PlaySound(collectChipsClip);
                        playedCollectSound = true;
                    }

                    yield return AnimateChips(GetBetAnchor(pair.Key), GetSettledPotAnchor(), amount);
                    SetVisualStreetBet(pair.Key, 0);
                    _visualSettledPot += amount;
                    RefreshHud();
                }
            }
        }

        private IEnumerator AnimateSettledPotToWinners(TexasHoldemRoundResult result)
        {
            TexasHoldemPlayerResult[] winners = result.Players.Where(player => player.IsWinner).ToArray();

            if (winners.Length == 0 || _visualSettledPot <= 0)
                yield break;

            if (awardPotDelaySeconds > 0f)
                yield return new WaitForSeconds(awardPotDelaySeconds);

            int potToAward = _visualSettledPot;
            int share = potToAward / winners.Length;
            int remainder = potToAward % winners.Length;

            for (int i = 0; i < winners.Length; i++)
            {
                TexasHoldemPlayerResult winner = winners[i];
                int awardAmount = share + (i < remainder ? 1 : 0);

                if (awardAmount <= 0)
                    continue;

                RectTransform winnerBetAnchor = GetBetAnchor(winner.PlayerId);

                PlaySound(placeChipsClip);
                yield return AnimateChips(GetSettledPotAnchor(), winnerBetAnchor, awardAmount);
                _visualSettledPot -= awardAmount;
                SetVisualStreetBet(winner.PlayerId, GetVisualStreetBet(winner.PlayerId) + awardAmount);
                RefreshHud();

                if (winnerBetHoldSeconds > 0f)
                    yield return new WaitForSeconds(winnerBetHoldSeconds);

                yield return AnimateChips(winnerBetAnchor, GetProfileAnchor(winner.PlayerId), awardAmount);
                SetVisualStreetBet(winner.PlayerId, Mathf.Max(0, GetVisualStreetBet(winner.PlayerId) - awardAmount));
                SetDisplayedStack(winner.PlayerId, GetDisplayedStack(winner.PlayerId) + awardAmount);
                PlaySound(chipsEarnedClip);
                RefreshHud();
            }
        }

        private IEnumerator AnimateChips(RectTransform from, RectTransform to, int amount)
        {
            if (chipMoveVisualPrefab == null || from == null || to == null)
                yield break;

            if (chipMoveStartDelay > 0f)
                yield return new WaitForSeconds(chipMoveStartDelay);

            RectTransform parent = chipAnimationLayer != null ? chipAnimationLayer : cardLayer;
            RectTransform chip = Instantiate(chipMoveVisualPrefab, parent);
            chip.position = from.position;
            chip.gameObject.SetActive(true);

            TMP_Text text = chip.GetComponentInChildren<TMP_Text>(includeInactive: true);

            if (text != null)
                text.text = FormatChips(amount);

            Vector3 start = from.position;
            Vector3 end = to.position;

            if (chipMoveDuration <= 0f)
            {
                chip.position = end;
                Destroy(chip.gameObject);
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < chipMoveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / chipMoveDuration);
                t = chipMoveCurve != null ? chipMoveCurve.Evaluate(t) : 1f - Mathf.Pow(1f - t, 3f);
                chip.position = Vector3.Lerp(start, end, t);
                yield return null;
            }

            Destroy(chip.gameObject);
        }

        private void RefreshHud()
        {
            UpdateTurnOwner();
            UpdateTableText();
            UpdatePlayerHud();
            UpdateActionControls();
            RefreshHandPreviewHighlights();
        }

        private void UpdateTableText()
        {
            if (_round == null)
                return;

            SetText(livePotText, $"Pot: {FormatChips(_round.LivePot)}");
            SetText(settledPotText, $"Main: {FormatChips(_visualSettledPot)}");

            SetText(playerStreetBetText, FormatBetText(_visualPlayerStreetBet));
            SetText(botStreetBetText, FormatBetText(_visualBotStreetBet));
            UpdatePersistentTableChips();

            IReadOnlyList<Card> visibleBoardCards = GetVisibleBoardCards();
            string boardText = visibleBoardCards.Count == 0
                ? "Board: -"
                : $"Board: {string.Join(", ", visibleBoardCards)}";

            if (_round.IsHandComplete)
                return;

            string turnText = _round.CurrentPlayer == null
                ? "Resolving..."
                : _round.CurrentPlayer.IsBot
                    ? "Bot is thinking..."
                    : _round.AmountToCall > 0
                        ? $"Your turn: call {FormatChips(_round.AmountToCall)}, raise, all-in, or fold."
                        : "Your turn: check, bet, all-in, or fold.";

            SetStatus($"{_round.Street} | {boardText}\n{turnText}");
        }

        private void UpdatePlayerHud()
        {
            if (_round == null)
                return;

            TexasHoldemRoundPlayerState player = _round.Players[0];
            TexasHoldemRoundPlayerState bot = _round.Players[1];

            SetText(playerNameText, HumanPlayerId);
            SetText(botNameText, BotPlayerId);
            SetText(playerChipsText, FormatChips(_displayedPlayerStack));
            SetText(botChipsText, FormatChips(_displayedBotStack));
            SetText(playerActionText, GetActionText(HumanPlayerId));
            SetText(botActionText, GetActionText(BotPlayerId));

            UpdateHandRankDisplay(playerHandRankText, playerHandRankBackground, player, revealCards: true);
            UpdateHandRankDisplay(
                botHandRankText,
                botHandRankBackground,
                bot,
                revealCards: ShouldShowHandRank(bot)
            );
        }

        private void UpdateActionControls()
        {
            bool canAct = CanHumanAct();

            if (!canAct)
            {
                SetButton(leftActionButton, leftActionButtonText, false, "Fold");
                SetButton(middleActionButton, middleActionButtonText, false, "Check");
                SetButton(rightActionButton, rightActionButtonText, false, "Bet");
                SetBetSliderVisible(false);
                return;
            }

            TexasHoldemRoundPlayerState player = _round.CurrentPlayer;
            int amountToCall = _round.AmountToCall;
            bool facingUncallableAllIn = amountToCall > player.Chips;
            bool isBetAction = _round.CurrentStreetBet == 0;

            SetButton(leftActionButton, leftActionButtonText, true, "Fold");

            if (isBetAction)
            {
                SetButton(middleActionButton, middleActionButtonText, true, "Check");
                ConfigureBetSlider(_round.GetMinimumLegalBet(HumanPlayerId), player.Chips);
                SetButton(rightActionButton, rightActionButtonText, player.Chips > 0, FormatBetOrAllInLabel("Bet", player.Chips));
                return;
            }

            SetButton(
                middleActionButton,
                middleActionButtonText,
                !facingUncallableAllIn,
                amountToCall == 0 ? "Check" : $"Call\n{FormatChips(amountToCall)}"
            );

            if (facingUncallableAllIn)
            {
                SetBetSliderVisible(false);
                SetButton(rightActionButton, rightActionButtonText, player.Chips > 0, "All In");
                return;
            }

            int minimumRaiseTo = _round.GetMinimumLegalRaiseTo(HumanPlayerId);
            int maximumRaiseTo = player.StreetBet + player.Chips;
            bool canRaise = maximumRaiseTo >= minimumRaiseTo;

            if (!canRaise)
            {
                SetBetSliderVisible(false);
                SetButton(rightActionButton, rightActionButtonText, player.Chips > amountToCall, "All In");
                return;
            }

            ConfigureBetSlider(minimumRaiseTo, maximumRaiseTo);
            SetButton(rightActionButton, rightActionButtonText, true, FormatBetOrAllInLabel("Raise", maximumRaiseTo));
        }

        private void ConfigureBetSlider(int minAmount, int maxAmount)
        {
            if (betAmountSlider == null)
                return;

            minAmount = Mathf.Max(0, minAmount);
            maxAmount = Mathf.Max(minAmount, maxAmount);

            SetGameObjectActive(betSliderContainer, true);
            betAmountSlider.gameObject.SetActive(true);
            SetGameObjectActive(selectedBetAmountContainer, true);
            SetObjectActive(selectedBetAmountText, true);
            SetObjectActive(decreaseBetButton, true);
            SetObjectActive(increaseBetButton, true);

            betAmountSlider.wholeNumbers = true;
            betAmountSlider.minValue = minAmount;
            betAmountSlider.maxValue = maxAmount;

            if (_selectedBetOrRaiseTo < minAmount || _selectedBetOrRaiseTo > maxAmount)
                _selectedBetOrRaiseTo = minAmount;

            betAmountSlider.SetValueWithoutNotify(_selectedBetOrRaiseTo);
            UpdateSelectedBetText();
        }

        private void SetBetSliderVisible(bool visible)
        {
            SetGameObjectActive(betSliderContainer, visible);

            if (betAmountSlider != null)
                betAmountSlider.gameObject.SetActive(visible);

            SetGameObjectActive(selectedBetAmountContainer, visible);
            SetObjectActive(selectedBetAmountText, visible);
            SetObjectActive(decreaseBetButton, visible);
            SetObjectActive(increaseBetButton, visible);
        }

        private void OnBetSliderChanged(float value)
        {
            _selectedBetOrRaiseTo = Mathf.RoundToInt(value);
            UpdateSelectedBetText();
        }

        private void UpdateSelectedBetText()
        {
            SetText(selectedBetAmountText, FormatChips(_selectedBetOrRaiseTo));

            if (!CanHumanAct())
                return;

            TexasHoldemRoundPlayerState player = _round.CurrentPlayer;
            int maximumTotal = player.StreetBet + player.Chips;

            if (_round.CurrentStreetBet == 0)
                SetButton(rightActionButton, rightActionButtonText, player.Chips > 0, FormatBetOrAllInLabel("Bet", player.Chips));
            else
                SetButton(rightActionButton, rightActionButtonText, true, FormatBetOrAllInLabel("Raise", maximumTotal));
        }

        private string FormatBetOrAllInLabel(string actionName, int allInThreshold)
        {
            return _selectedBetOrRaiseTo >= allInThreshold
                ? "All In"
                : $"{actionName}\n{FormatChips(_selectedBetOrRaiseTo)}";
        }

        private void UpdatePersistentTableChips()
        {
            UpdatePersistentTableChip(playerTableChipVisual, playerBetAnchor, _visualPlayerStreetBet);
            UpdatePersistentTableChip(botTableChipVisual, botBetAnchor, _visualBotStreetBet);
            UpdatePersistentTableChip(settledPotChipVisual, GetSettledPotAnchor(), _visualSettledPot);
        }

        private static void UpdatePersistentTableChip(RectTransform chipVisual, RectTransform anchor, int amount)
        {
            if (chipVisual == null)
                return;

            if (anchor != null)
                chipVisual.position = anchor.position;

            bool visible = amount > 0;
            chipVisual.gameObject.SetActive(visible);

            if (!visible)
                return;

            TMP_Text text = chipVisual.GetComponentInChildren<TMP_Text>(includeInactive: true);

            if (text != null)
                text.text = FormatChips(amount);
        }

        private bool CanHumanAct()
        {
            return _round != null
                && !_inputLocked
                && !_round.IsHandComplete
                && _round.CurrentPlayer != null
                && _round.CurrentPlayer.PlayerId == HumanPlayerId;
        }

        private void RefreshHandPreviewHighlights()
        {
            if (_round != null && _round.IsHandComplete)
                return;

            foreach (CardView cardView in _spawnedCards)
            {
                cardView.SetHighlighted(false);
            }

            if (_round == null)
                return;

            if (_round.IsHandComplete)
                return;

            TexasHoldemRoundPlayerState player = _round.Players[0];
            TexasHoldemHandPreview preview = EvaluatePreview(player);

            foreach (Card card in preview.HighlightCards)
            {
                if (_cardViewsByCard.TryGetValue(card, out CardView cardView))
                {
                    if (!ShouldHighlightShownCard(cardView))
                        continue;

                    cardView.SetHighlighted(true);
                }
            }
        }

        private void RefreshCurrentHighlights()
        {
            if (_round != null && _round.IsHandComplete && _round.RoundResult != null)
            {
                HighlightWinnerCards(_round.RoundResult);
                return;
            }

            RefreshHandPreviewHighlights();
        }

        private void UpdateHandRankDisplay(
            TMP_Text rankText,
            GameObject rankBackground,
            TexasHoldemRoundPlayerState player,
            bool revealCards)
        {
            if (!revealCards)
            {
                SetText(rankText, string.Empty);
                SetObjectActive(rankText, false);
                SetGameObjectActive(rankBackground, false);
                return;
            }

            TexasHoldemHandPreview preview = EvaluatePreview(player);
            SetObjectActive(rankText, true);
            SetGameObjectActive(rankBackground, true);
            SetText(rankText, FormatRank(preview.Rank));

            if (rankText != null)
                rankText.color = GetRankColor(preview.Rank);
        }

        private TexasHoldemHandPreview EvaluatePreview(TexasHoldemRoundPlayerState player)
        {
            var cards = new List<Card>(7);
            cards.AddRange(player.HoleCards);
            cards.AddRange(GetVisibleBoardCards());

            return EvaluatePreview(cards);
        }

        private TexasHoldemHandPreview EvaluatePreview(IReadOnlyList<Card> cards)
        {
            if (cards.Count >= 5)
                return EvaluateFiveToSevenCardPreview(cards);

            return EvaluatePartialPreview(cards);
        }

        private IReadOnlyList<Card> GetVisibleBoardCards()
        {
            if (_round == null || _round.BoardCards.Count == 0 || _dealtBoardCardCount <= 0)
                return new Card[0];

            int visibleCount = Mathf.Min(_dealtBoardCardCount, _round.BoardCards.Count);
            return _round.BoardCards.Take(visibleCount).ToArray();
        }

        private TexasHoldemHandPreview EvaluatePartialPreview(IReadOnlyList<Card> cards)
        {
            var groupedCards = cards
                .GroupBy(card => card.Rank)
                .OrderByDescending(group => group.Count())
                .ThenByDescending(group => group.Key)
                .ToArray();

            if (groupedCards.Length > 0 && groupedCards[0].Count() == 4)
                return new TexasHoldemHandPreview(PokerHandRank.FourOfAKind, groupedCards[0].ToArray());

            if (groupedCards.Length > 0 && groupedCards[0].Count() == 3)
                return new TexasHoldemHandPreview(PokerHandRank.ThreeOfAKind, groupedCards[0].ToArray());

            Card[] pairCards = groupedCards
                .Where(group => group.Count() == 2)
                .SelectMany(group => group)
                .ToArray();

            if (pairCards.Length >= 4)
                return new TexasHoldemHandPreview(PokerHandRank.TwoPair, pairCards);

            if (pairCards.Length == 2)
                return new TexasHoldemHandPreview(PokerHandRank.OnePair, pairCards);

            return new TexasHoldemHandPreview(PokerHandRank.HighCard, new Card[0]);
        }

        private TexasHoldemHandPreview EvaluateFiveToSevenCardPreview(IReadOnlyList<Card> cards)
        {
            BestPokerHandResult bestHand = _previewBestHandEvaluator.EvaluateBestHand(cards);
            PokerHandRank rank = bestHand.HandResult.Rank;
            IReadOnlyList<int> tieBreakers = bestHand.HandResult.TieBreakers;

            if (rank == PokerHandRank.HighCard)
                return new TexasHoldemHandPreview(rank, new Card[0]);

            if (rank == PokerHandRank.OnePair ||
                rank == PokerHandRank.ThreeOfAKind ||
                rank == PokerHandRank.FourOfAKind)
            {
                Rank madeRank = (Rank)tieBreakers[0];
                return new TexasHoldemHandPreview(rank, cards.Where(card => card.Rank == madeRank).ToArray());
            }

            if (rank == PokerHandRank.TwoPair || rank == PokerHandRank.FullHouse)
            {
                var madeRanks = new HashSet<Rank>
                {
                    (Rank)tieBreakers[0],
                    (Rank)tieBreakers[1]
                };

                return new TexasHoldemHandPreview(rank, cards.Where(card => madeRanks.Contains(card.Rank)).ToArray());
            }

            return new TexasHoldemHandPreview(rank, bestHand.BestCards);
        }

        private void HighlightWinnerCards(TexasHoldemRoundResult result)
        {
            foreach (CardView cardView in _spawnedCards)
            {
                cardView.SetHighlighted(false);
            }

            HashSet<Card> winningCards = result.Players
                .Where(player => player.IsWinner)
                .Where(player => player.BestHand != null)
                .SelectMany(player => player.BestHand.BestCards)
                .ToHashSet();

            if (winningCards.Count > 0)
            {
                foreach (CardView cardView in _spawnedCards)
                {
                    cardView.SetHighlighted(winningCards.Contains(cardView.Card) && ShouldHighlightShownCard(cardView));
                }

                return;
            }

            HighlightShownMadeRankCards();
        }

        private void HighlightShownMadeRankCards()
        {
            if (_round == null)
                return;

            foreach (TexasHoldemRoundPlayerState player in _round.Players)
            {
                TexasHoldemHandPreview preview = EvaluatePreview(GetShownCardsForHighlight(player));

                if (preview.Rank == PokerHandRank.HighCard)
                    continue;

                foreach (Card card in preview.HighlightCards)
                {
                    if (_cardViewsByCard.TryGetValue(card, out CardView cardView) && ShouldHighlightShownCard(cardView))
                        cardView.SetHighlighted(true);
                }
            }
        }

        private IReadOnlyList<Card> GetShownCardsForHighlight(TexasHoldemRoundPlayerState player)
        {
            var cards = new List<Card>(7);

            foreach (Card card in player.HoleCards)
            {
                if (IsCardFaceUp(card))
                    cards.Add(card);
            }

            foreach (Card card in GetVisibleBoardCards())
            {
                if (IsCardFaceUp(card))
                    cards.Add(card);
            }

            return cards;
        }

        private IEnumerator RevealCardsForHandComplete()
        {
            if (_round == null || _round.RoundResult == null)
                yield break;

            foreach (TexasHoldemRoundPlayerState player in _round.Players)
            {
                if (!ShouldRevealHoleCardsAtCompletion(player))
                    continue;

                yield return SetHoleCardsFaceUp(player, true, playDrawSound: true);
            }

            RefreshFoldedCardDimStates();
        }

        private bool ShouldRevealHoleCardsAtCompletion(TexasHoldemRoundPlayerState player)
        {
            TexasHoldemPlayerResult result = _round.RoundResult.Players.FirstOrDefault(candidate => candidate.PlayerId == player.PlayerId);

            if (result != null && result.BestHand != null)
                return true;

            return player.IsBot && player.HasFolded && HasMadeRank(player);
        }

        private bool HasMadeRank(TexasHoldemRoundPlayerState player)
        {
            return EvaluatePreview(player).Rank != PokerHandRank.HighCard;
        }

        private IEnumerator SetHoleCardsFaceUp(
            TexasHoldemRoundPlayerState player,
            bool faceUp,
            bool playDrawSound)
        {
            CardView[] holeCardViews = GetHoleCardViews(player)
                .Where(cardView => cardView != null && cardView.IsFaceUp != faceUp)
                .ToArray();

            if (holeCardViews.Length == 0)
            {
                UpdateHumanHoleCardFaceState(player, faceUp);
                yield break;
            }

            if (playDrawSound)
                PlayRandomDrawCardSound();

            yield return _animationRunner.FlipFaces(
                holeCardViews,
                faceUp,
                cardFlipDuration
            );

            UpdateHumanHoleCardFaceState(player, faceUp);
            RefreshCurrentHighlights();
        }

        private void UpdateHumanHoleCardFaceState(TexasHoldemRoundPlayerState player, bool faceUp)
        {
            if (player.PlayerId == HumanPlayerId)
                _playerHoleCardsFaceUp = faceUp;
        }

        private CardView[] GetHoleCardViews(TexasHoldemRoundPlayerState player)
        {
            return player.HoleCards
                .Select(card => _cardViewsByCard.TryGetValue(card, out CardView cardView) ? cardView : null)
                .Where(cardView => cardView != null)
                .ToArray();
        }

        private bool ShouldHighlightShownCard(CardView cardView)
        {
            return cardView != null && cardView.IsFaceUp;
        }

        private bool IsCardFaceUp(Card card)
        {
            return _cardViewsByCard.TryGetValue(card, out CardView cardView) && cardView.IsFaceUp;
        }

        private bool ShouldShowHandRank(TexasHoldemRoundPlayerState player)
        {
            if (player.PlayerId == HumanPlayerId)
                return true;

            CardView[] holeCardViews = GetHoleCardViews(player);
            return holeCardViews.Length == player.HoleCards.Count && holeCardViews.All(cardView => cardView.IsFaceUp);
        }

        private void RefreshFoldedCardDimStates()
        {
            if (_round == null)
                return;

            foreach (TexasHoldemRoundPlayerState player in _round.Players)
            {
                bool folded = player.HasFolded;

                foreach (CardView cardView in GetHoleCardViews(player))
                {
                    cardView.SetDimmed(folded);

                    if (folded && !cardView.IsFaceUp)
                        cardView.SetHighlighted(false);
                }
            }
        }

        private void ShowResult(TexasHoldemRoundResult result)
        {
            TexasHoldemPlayerResult[] winners = result.Players
                .Where(player => player.IsWinner)
                .ToArray();

            string winnerText = string.Join(", ", winners.Select(winner => winner.PlayerId));
            SetStatus($"Winner: {winnerText}");
            SetWinnerVisuals(winners);
            PlaySound(winnerClip);
        }

        private void UpdateTurnOwner()
        {
            string currentPlayerId = ShouldShowTurnTimer()
                ? _round.CurrentPlayer.PlayerId
                : null;
            TexasHoldemStreet currentStreet = _round != null ? _round.Street : TexasHoldemStreet.NotStarted;

            if (_activeTimerPlayerId == currentPlayerId && _activeTimerStreet == currentStreet)
                return;

            _activeTimerPlayerId = currentPlayerId;
            _activeTimerStreet = currentStreet;
            _turnTimeRemaining = turnTimeLimitSeconds;
            UpdateTimerTexts();
        }

        private void UpdateTurnTimer()
        {
            if (!IsTurnTimerRunning())
            {
                StopTurnTimer();
                return;
            }

            _turnTimeRemaining = Mathf.Max(0f, _turnTimeRemaining - Time.deltaTime);
            UpdateTimerTexts();

            if (_turnTimeRemaining > 0f || _inputLocked || _round.CurrentPlayer.PlayerId != HumanPlayerId)
                return;

            if (_round.AmountToCall == 0)
                ApplyHumanAction(TexasHoldemPlayerAction.Check());
            else
                ApplyHumanAction(TexasHoldemPlayerAction.Fold());
        }

        private void UpdateTimerTexts()
        {
            SetTimerVisible(HumanPlayerId, _activeTimerPlayerId == HumanPlayerId);
            SetTimerVisible(BotPlayerId, _activeTimerPlayerId == BotPlayerId);

            TMP_Text timerText = _activeTimerPlayerId == HumanPlayerId ? playerTimerText : botTimerText;
            SetText(timerText, Mathf.CeilToInt(_turnTimeRemaining).ToString());
        }

        private void SetTimerVisible(string playerId, bool visible)
        {
            TMP_Text timerText = playerId == HumanPlayerId ? playerTimerText : botTimerText;
            GameObject timerBackground = playerId == HumanPlayerId ? playerTimerBackground : botTimerBackground;

            SetObjectActive(timerText, visible);
            SetGameObjectActive(timerBackground, visible);
        }

        private bool IsTurnTimerRunning()
        {
            return ShouldShowTurnTimer()
                && !string.IsNullOrEmpty(_activeTimerPlayerId)
                && _round.CurrentPlayer.PlayerId == _activeTimerPlayerId;
        }

        private bool ShouldShowTurnTimer()
        {
            if (_round == null || _round.IsHandComplete || _round.CurrentPlayer == null)
                return false;

            if (_round.CurrentPlayer.PlayerId == HumanPlayerId)
                return !_inputLocked;

            return _round.CurrentPlayer.IsBot && _isBotThinking;
        }

        private void StopTurnTimer()
        {
            _activeTimerPlayerId = null;
            _activeTimerStreet = TexasHoldemStreet.NotStarted;

            SetTimerVisible(HumanPlayerId, false);
            SetTimerVisible(BotPlayerId, false);
        }

        private Dictionary<string, int> CaptureStreetBets()
        {
            return new Dictionary<string, int>
            {
                [HumanPlayerId] = _visualPlayerStreetBet,
                [BotPlayerId] = _visualBotStreetBet
            };
        }

        private TexasHoldemRoundPlayerState GetPlayer(string playerId)
        {
            return _round.Players.First(player => player.PlayerId == playerId);
        }

        private RectTransform GetProfileAnchor(string playerId)
        {
            if (playerId == HumanPlayerId)
                return playerProfileAnchor != null ? playerProfileAnchor : playerProfileImage != null ? playerProfileImage.rectTransform : null;

            return botProfileAnchor != null ? botProfileAnchor : botProfileImage != null ? botProfileImage.rectTransform : null;
        }

        private RectTransform GetBetAnchor(string playerId)
        {
            return playerId == HumanPlayerId ? playerBetAnchor : botBetAnchor;
        }

        private void SetWinnerVisuals(IReadOnlyList<TexasHoldemPlayerResult> winners)
        {
            bool playerWon = winners.Any(winner => winner.PlayerId == HumanPlayerId);
            bool botWon = winners.Any(winner => winner.PlayerId == BotPlayerId);

            SetGameObjectActive(playerWinnerVisual, playerWon);
            SetGameObjectActive(botWinnerVisual, botWon);
        }

        private string GetActionText(string playerId)
        {
            if (ShouldShowSeatPositionForTurn(playerId))
                return GetSeatPositionLabel(playerId);

            return _lastActionByPlayerId.TryGetValue(playerId, out string actionText)
                ? actionText
                : string.Empty;
        }

        private bool ShouldShowSeatPositionForTurn(string playerId)
        {
            return _round != null
                && !_round.IsHandComplete
                && _round.CurrentPlayer != null
                && _round.CurrentPlayer.PlayerId == playerId
                && _activeTimerPlayerId == playerId;
        }

        private string FormatPostedBlindLabel(string playerId)
        {
            TexasHoldemRoundPlayerState player = GetPlayer(playerId);
            string seatPosition = GetSeatPositionLabel(playerId);

            if (player.SeatIndex == _round.SmallBlindIndex)
                return $"{seatPosition} {FormatChips(smallBlind)}";

            if (player.SeatIndex == _round.BigBlindIndex)
                return $"{seatPosition} {FormatChips(bigBlind)}";

            return seatPosition;
        }

        private string GetSeatPositionLabel(string playerId)
        {
            return _round != null
                ? _round.GetSeatPositionText(playerId)
                : string.Empty;
        }

        private void PlayActionSound(TexasHoldemPlayerAction action)
        {
            switch (action.ActionType)
            {
                case TexasHoldemPlayerActionType.Check:
                    PlaySound(checkVoiceClip);
                    PlaySound(checkKnockClip);
                    break;
                case TexasHoldemPlayerActionType.Call:
                    PlaySound(callVoiceClip);
                    break;
                case TexasHoldemPlayerActionType.Bet:
                    PlaySound(betVoiceClip);
                    break;
                case TexasHoldemPlayerActionType.Raise:
                    PlaySound(raiseVoiceClip);
                    break;
                case TexasHoldemPlayerActionType.Fold:
                    PlaySound(foldVoiceClip);
                    PlaySound(foldCardsClip);
                    break;
                case TexasHoldemPlayerActionType.AllIn:
                    PlaySound(allInVoiceClip);
                    break;
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip == null || audioSource == null)
                return;

            audioSource.PlayOneShot(clip);
        }

        private void PlayRandomDrawCardSound()
        {
            if (drawCardClip == null)
            {
                PlaySound(drawCardClip2);
                return;
            }

            if (drawCardClip2 == null)
            {
                PlaySound(drawCardClip);
                return;
            }

            PlaySound(UnityEngine.Random.value < 0.5f ? drawCardClip : drawCardClip2);
        }

        private string FormatActionLabel(TexasHoldemPlayerAction action, int committedAmount)
        {
            switch (action.ActionType)
            {
                case TexasHoldemPlayerActionType.Fold:
                    return "Fold";
                case TexasHoldemPlayerActionType.Check:
                    return "Check";
                case TexasHoldemPlayerActionType.Call:
                    return $"Call {FormatChips(committedAmount)}";
                case TexasHoldemPlayerActionType.Bet:
                    return $"Bet {FormatChips(committedAmount)}";
                case TexasHoldemPlayerActionType.Raise:
                    return $"Raise {FormatChips(action.Amount)}";
                case TexasHoldemPlayerActionType.AllIn:
                    return "All in";
                default:
                    return action.ActionType.ToString();
            }
        }

        private static string FormatRank(PokerHandRank rank)
        {
            switch (rank)
            {
                case PokerHandRank.RoyalFlush:
                    return "Royal Flush";
                case PokerHandRank.StraightFlush:
                    return "Straight Flush";
                case PokerHandRank.FourOfAKind:
                    return "Four of a Kind";
                case PokerHandRank.FullHouse:
                    return "Full House";
                case PokerHandRank.Flush:
                    return "Flush";
                case PokerHandRank.Straight:
                    return "Straight";
                case PokerHandRank.ThreeOfAKind:
                    return "Three of a Kind";
                case PokerHandRank.TwoPair:
                    return "Two Pair";
                case PokerHandRank.OnePair:
                    return "Pair";
                default:
                    return "High Card";
            }
        }

        private Color GetRankColor(PokerHandRank rank)
        {
            switch (rank)
            {
                case PokerHandRank.OnePair:
                case PokerHandRank.TwoPair:
                    return pairRankColor;
                case PokerHandRank.ThreeOfAKind:
                    return threeKindRankColor;
                case PokerHandRank.Straight:
                case PokerHandRank.Flush:
                case PokerHandRank.FullHouse:
                    return straightPlusRankColor;
                case PokerHandRank.FourOfAKind:
                    return fourKindRankColor;
                case PokerHandRank.StraightFlush:
                case PokerHandRank.RoyalFlush:
                    return straightFlushRankColor;
                default:
                    return highCardRankColor;
            }
        }

        private static string FormatBetText(int amount)
        {
            return amount > 0 ? FormatChips(amount) : string.Empty;
        }

        private static string FormatChips(int amount)
        {
            return amount.ToString("N0");
        }

        private static void SetButton(Button button, TMP_Text label, bool interactable, string text)
        {
            if (button != null)
                button.interactable = interactable;

            SetText(label, text);
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
                text.text = value;
        }

        private void SetStatus(string value)
        {
            SetText(tableStatusText, value);
        }

        private static void SetObjectActive(Component component, bool active)
        {
            if (component != null)
                component.gameObject.SetActive(active);
        }

        private static void SetGameObjectActive(GameObject target, bool active)
        {
            if (target != null)
                target.SetActive(active);
        }

        private void InitializeControls()
        {
            if (_controlsInitialized)
                return;

            AddButtonListener(leftActionButton, OnLeftActionPressed);
            AddButtonListener(middleActionButton, OnMiddleActionPressed);
            AddButtonListener(rightActionButton, OnRightActionPressed);
            AddButtonListener(decreaseBetButton, DecreaseBetAmount);
            AddButtonListener(increaseBetButton, IncreaseBetAmount);

            if (betAmountSlider != null)
            {
                betAmountSlider.onValueChanged.RemoveListener(OnBetSliderChanged);
                betAmountSlider.onValueChanged.AddListener(OnBetSliderChanged);
            }

            _controlsInitialized = true;
        }

        private static void AddButtonListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private IEnumerator WaitDealDelay()
        {
            if (dealDelay > 0f)
                yield return new WaitForSeconds(dealDelay);
        }

        private IEnumerator WaitBotThinkDelay()
        {
            float minDelay = Mathf.Max(0f, botThinkDelayRangeSeconds.x);
            float maxDelay = Mathf.Max(minDelay, botThinkDelayRangeSeconds.y);
            float delay = Random.Range(minDelay, maxDelay);

            if (delay > 0f)
                yield return new WaitForSeconds(delay);
        }

        private void ClearTable()
        {
            foreach (CardView cardView in _spawnedCards)
            {
                if (cardView != null)
                    Destroy(cardView.gameObject);
            }

            _spawnedCards.Clear();
            _playerHoleCardViews.Clear();
            _cardViewsByCard.Clear();
            _dealtBoardCardCount = 0;
            _selectedBetOrRaiseTo = 0;
            _playerHoleCardsFaceUp = true;
            _playerHoleCardsFlipInProgress = false;
            _isBotThinking = false;
            _activeTimerPlayerId = null;
            _activeTimerStreet = TexasHoldemStreet.NotStarted;
            _visualPlayerStreetBet = 0;
            _visualBotStreetBet = 0;
            _visualSettledPot = 0;
            SetStatus(string.Empty);
            SetText(playerHandRankText, string.Empty);
            SetText(botHandRankText, string.Empty);
            SetObjectActive(playerHandRankText, false);
            SetObjectActive(botHandRankText, false);
            SetGameObjectActive(playerHandRankBackground, false);
            SetGameObjectActive(botHandRankBackground, false);
            SetTimerVisible(HumanPlayerId, false);
            SetTimerVisible(BotPlayerId, false);
            SetText(playerActionText, string.Empty);
            SetText(botActionText, string.Empty);
            SetGameObjectActive(playerWinnerVisual, false);
            SetGameObjectActive(botWinnerVisual, false);
            SetObjectActive(playerTableChipVisual, false);
            SetObjectActive(botTableChipVisual, false);
            SetObjectActive(settledPotChipVisual, false);
        }

        private void ValidateReferences()
        {
            if (cardSpriteDatabase == null)
                throw new MissingReferenceException("CardSpriteDatabase is missing.");

            if (cardViewPrefab == null)
                throw new MissingReferenceException("CardView prefab is missing.");

            if (cardLayer == null)
                throw new MissingReferenceException("CardLayer is missing.");

            if (deckAnchor == null)
                throw new MissingReferenceException("DeckAnchor is missing.");

            if (tableStatusText == null)
                throw new MissingReferenceException("TableStatusText is missing. Use TMP_Text.");

            if (playerSlots == null || playerSlots.Length != 2)
                throw new MissingReferenceException("PlayerSlots must contain exactly 2 slots.");

            if (botSlots == null || botSlots.Length != 2)
                throw new MissingReferenceException("BotSlots must contain exactly 2 slots.");

            if (boardSlots == null || boardSlots.Length != 5)
                throw new MissingReferenceException("BoardSlots must contain exactly 5 slots.");

            ValidateButton(leftActionButton, leftActionButtonText, nameof(leftActionButton));
            ValidateButton(middleActionButton, middleActionButtonText, nameof(middleActionButton));
            ValidateButton(rightActionButton, rightActionButtonText, nameof(rightActionButton));

            if (betAmountSlider == null)
                throw new MissingReferenceException("BetAmountSlider is missing.");

            if (selectedBetAmountText == null)
                throw new MissingReferenceException("SelectedBetAmountText is missing. Use TMP_Text.");
        }

        private static void ValidateButton(Button button, TMP_Text label, string fieldName)
        {
            if (button == null)
                throw new MissingReferenceException($"{fieldName} is missing.");

            if (label == null)
                throw new MissingReferenceException($"{fieldName} label is missing. Use TMP_Text.");
        }

        private readonly struct TexasHoldemHandPreview
        {
            public PokerHandRank Rank { get; }
            public IReadOnlyList<Card> HighlightCards { get; }

            public TexasHoldemHandPreview(PokerHandRank rank, IEnumerable<Card> highlightCards)
            {
                Rank = rank;
                HighlightCards = highlightCards.ToArray();
            }
        }
    }
}
