using Project.Domain.Cards;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI.Components
{
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class CardView : MonoBehaviour
    {
        [SerializeField] private Image image;
        [SerializeField] private Outline outline;
        [SerializeField] private Graphic dimOverlay;
        [SerializeField] private Color fallbackDimmedColor = new Color(0.35f, 0.35f, 0.35f, 1f);

        private CanvasGroup _canvasGroup;
        private Sprite _frontSprite;
        private Sprite _backSprite;
        private Color _baseImageColor = Color.white;

        public Card Card { get; private set; }
        public RectTransform RectTransform { get; private set; }
        public bool IsFaceUp { get; private set; }
        public bool IsDimmed { get; private set; }

        private void Awake()
        {
            RectTransform = (RectTransform)transform;

            if (image == null)
                image = GetComponent<Image>();

            if (image != null)
                _baseImageColor = image.color;

            _canvasGroup = GetComponent<CanvasGroup>();

            if (outline != null)
                outline.enabled = false;

            if (dimOverlay != null)
            {
                dimOverlay.raycastTarget = false;
                dimOverlay.gameObject.SetActive(false);
            }
        }

        public void Setup(Card card, Sprite frontSprite, Sprite backSprite, bool faceUp)
        {
            Card = card;
            _frontSprite = frontSprite;
            _backSprite = backSprite;

            SetFaceUp(faceUp);
            SetHighlighted(false);
            SetDimmed(false);
            SetVisible(true);
        }

        public void SetFaceUp(bool faceUp)
        {
            IsFaceUp = faceUp;
            image.sprite = faceUp ? _frontSprite : _backSprite;
        }

        public void SetHighlighted(bool highlighted)
        {
            if (outline != null)
                outline.enabled = highlighted;
        }

        public void SetDimmed(bool dimmed)
        {
            IsDimmed = dimmed;

            if (dimOverlay != null)
            {
                dimOverlay.gameObject.SetActive(dimmed);
                return;
            }

            if (image != null)
                image.color = dimmed ? fallbackDimmedColor : _baseImageColor;
        }

        public void SetVisible(bool visible)
        {
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.blocksRaycasts = visible;
            _canvasGroup.interactable = visible;
        }
    }
}
