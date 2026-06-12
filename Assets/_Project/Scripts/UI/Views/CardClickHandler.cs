using System;
using Project.UI.Components;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Project.UI.Views
{
    [RequireComponent(typeof(CardView))]
    public sealed class CardClickHandler : MonoBehaviour, IPointerClickHandler
    {
        private CardView _cardView;
        private Action<CardView> _onClick;

        private void Awake()
        {
            _cardView = GetComponent<CardView>();
        }

        public void Initialize(Action<CardView> onClick)
        {
            _onClick = onClick;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _onClick?.Invoke(_cardView);
        }
    }
}
