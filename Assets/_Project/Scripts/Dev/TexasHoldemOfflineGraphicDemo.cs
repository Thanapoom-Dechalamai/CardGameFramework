using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Project.Application.Poker;
using Project.Config;
using Project.Domain.Cards;
using Project.Domain.Poker;
using Project.UI.Components;
using Project.UI.Views;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Dev
{
    public sealed class TexasHoldemOfflineGraphicDemo : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private CardSpriteDatabase cardSpriteDatabase;
        [SerializeField] private CardView cardViewPrefab;

        [Header("Scene References")]
        [SerializeField] private RectTransform cardLayer;
        [SerializeField] private RectTransform deckAnchor;
        [SerializeField] private Image deckImage;
        [SerializeField] private Text resultText;

        [Header("Slots")]
        [SerializeField] private RectTransform[] player1Slots;
        [SerializeField] private RectTransform[] player2Slots;
        [SerializeField] private RectTransform[] boardSlots;

        [Header("Animation")]
        [SerializeField] private float dealDelay = 0.12f;
        [SerializeField] private float moveDuration = 0.22f;

        [Header("Debug")]
        [SerializeField] private bool useFixedSeed = false;
        [SerializeField] private int seed = 12345;
        [SerializeField] private bool showOpponentCards = true;

        private readonly List<CardView> _spawnedCards = new();
        private CardAnimationRunner _animationRunner;

        private void Awake()
        {
            _animationRunner = GetComponent<CardAnimationRunner>();

            if (_animationRunner == null)
                _animationRunner = gameObject.AddComponent<CardAnimationRunner>();
        }

        private void Start()
        {
            StartCoroutine(RunDemo());
        }

        private IEnumerator RunDemo()
        {
            ValidateReferences();
            ClearTable();

            if (deckImage != null)
                deckImage.sprite = cardSpriteDatabase.CardBackSprite;

            TexasHoldemRoundResult result = PlayOfflineRound();

            TexasHoldemPlayerResult player1 = result.Players[0];
            TexasHoldemPlayerResult player2 = result.Players[1];

            resultText.text = "Dealing...";

            // Deal hole cards one by one.
            yield return DealCard(player1.HoleCards[0], player1Slots[0], true);
            yield return WaitDealDelay();

            yield return DealCard(player2.HoleCards[0], player2Slots[0], showOpponentCards);
            yield return WaitDealDelay();

            yield return DealCard(player1.HoleCards[1], player1Slots[1], true);
            yield return WaitDealDelay();

            yield return DealCard(player2.HoleCards[1], player2Slots[1], showOpponentCards);
            yield return WaitDealDelay();

            // Deal board cards.
            for (int i = 0; i < result.BoardCards.Count; i++)
            {
                yield return DealCard(result.BoardCards[i], boardSlots[i], true);
                yield return WaitDealDelay();
            }

            ShowResult(result);
            HighlightWinnerCards(result);
        }

        private TexasHoldemRoundResult PlayOfflineRound()
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

            return simulator.PlayRound(new[]
            {
                "Player 1",
                "Player 2"
            });
        }

        private IEnumerator DealCard(Card card, RectTransform targetSlot, bool faceUp)
        {
            Sprite frontSprite = cardSpriteDatabase.GetSprite(card);
            Sprite backSprite = cardSpriteDatabase.CardBackSprite;

            CardView cardView = Instantiate(cardViewPrefab, cardLayer);
            cardView.RectTransform.position = deckAnchor.position;
            cardView.RectTransform.localScale = Vector3.one;
            cardView.Setup(card, frontSprite, backSprite, faceUp);

            _spawnedCards.Add(cardView);

            yield return _animationRunner.MoveToSlot(
                cardView,
                targetSlot,
                moveDuration
            );
        }

        private IEnumerator WaitDealDelay()
        {
            if (dealDelay > 0f)
                yield return new WaitForSeconds(dealDelay);
        }

        private void ShowResult(TexasHoldemRoundResult result)
        {
            TexasHoldemPlayerResult[] winners = result.Players
                .Where(player => player.IsWinner)
                .ToArray();

            string winnerText = string.Join(", ", winners.Select(winner => winner.PlayerId));

            string lines = $"Winner: {winnerText}\n\n";

            foreach (TexasHoldemPlayerResult player in result.Players)
            {
                lines += $"{player.PlayerId}: {player.BestHand.HandResult}\n";
            }

            resultText.text = lines;
        }

        private void HighlightWinnerCards(TexasHoldemRoundResult result)
        {
            HashSet<Card> winningCards = result.Players
                .Where(player => player.IsWinner)
                .SelectMany(player => player.BestHand.BestCards)
                .ToHashSet();

            foreach (CardView cardView in _spawnedCards)
            {
                cardView.SetHighlighted(winningCards.Contains(cardView.Card));
            }
        }

        private void ClearTable()
        {
            foreach (CardView cardView in _spawnedCards)
            {
                if (cardView != null)
                    Destroy(cardView.gameObject);
            }

            _spawnedCards.Clear();

            if (resultText != null)
                resultText.text = string.Empty;
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

            if (resultText == null)
                throw new MissingReferenceException("ResultText is missing.");

            if (player1Slots == null || player1Slots.Length != 2)
                throw new MissingReferenceException("Player1Slots must contain exactly 2 slots.");

            if (player2Slots == null || player2Slots.Length != 2)
                throw new MissingReferenceException("Player2Slots must contain exactly 2 slots.");

            if (boardSlots == null || boardSlots.Length != 5)
                throw new MissingReferenceException("BoardSlots must contain exactly 5 slots.");
        }
    }
}