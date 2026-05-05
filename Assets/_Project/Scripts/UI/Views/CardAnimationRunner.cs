using System.Collections;
using Project.UI.Components;
using UnityEngine;

namespace Project.UI.Views
{
    public sealed class CardAnimationRunner : MonoBehaviour
    {
        public IEnumerator MoveToSlot(
            CardView cardView,
            RectTransform targetSlot,
            float duration)
        {
            RectTransform cardTransform = cardView.RectTransform;

            Vector3 startPosition = cardTransform.position;
            Vector3 targetPosition = targetSlot.position;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                float t = Mathf.Clamp01(elapsed / duration);
                t = EaseOutCubic(t);

                cardTransform.position = Vector3.Lerp(startPosition, targetPosition, t);

                yield return null;
            }

            cardTransform.SetParent(targetSlot, worldPositionStays: false);
            cardTransform.anchoredPosition = Vector2.zero;
            cardTransform.localScale = Vector3.one;
        }

        private static float EaseOutCubic(float t)
        {
            return 1f - Mathf.Pow(1f - t, 3f);
        }
    }
}