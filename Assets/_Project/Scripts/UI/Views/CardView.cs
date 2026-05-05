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

        private CanvasGroup _canvasGroup;
        private Sprite _frontSprite;
        private Sprite _backSprite;

        public Card Card { get; private set; }
        public RectTransform RectTransform { get; private set; }

        private void Awake()
        {
            RectTransform = (RectTransform)transform;

            if (image == null)
                image = GetComponent<Image>();

            _canvasGroup = GetComponent<CanvasGroup>();

            if (outline != null)
                outline.enabled = false;
        }

        public void Setup(Card card, Sprite frontSprite, Sprite backSprite, bool faceUp)
        {
            Card = card;
            _frontSprite = frontSprite;
            _backSprite = backSprite;

            SetFaceUp(faceUp);
            SetHighlighted(false);
            SetVisible(true);
        }

        public void SetFaceUp(bool faceUp)
        {
            image.sprite = faceUp ? _frontSprite : _backSprite;
        }

        public void SetHighlighted(bool highlighted)
        {
            if (outline != null)
                outline.enabled = highlighted;
        }

        public void SetVisible(bool visible)
        {
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.blocksRaycasts = visible;
            _canvasGroup.interactable = visible;
        }
    }
}