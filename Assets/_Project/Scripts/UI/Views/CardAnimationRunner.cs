using System.Collections;
using System.Collections.Generic;
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

        public IEnumerator FlipFace(
            CardView cardView,
            bool faceUp,
            float duration)
        {
            RectTransform cardTransform = cardView.RectTransform;
            Vector3 baseScale = cardTransform.localScale;
            float halfDuration = Mathf.Max(0.01f, duration * 0.5f);

            float elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                float scaleX = Mathf.Lerp(baseScale.x, 0f, EaseOutCubic(t));
                cardTransform.localScale = new Vector3(scaleX, baseScale.y, baseScale.z);
                yield return null;
            }

            cardView.SetFaceUp(faceUp);

            elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                float scaleX = Mathf.Lerp(0f, baseScale.x, EaseOutCubic(t));
                cardTransform.localScale = new Vector3(scaleX, baseScale.y, baseScale.z);
                yield return null;
            }

            cardTransform.localScale = baseScale;
        }

        public IEnumerator FlipFaces(
            IReadOnlyList<CardView> cardViews,
            bool faceUp,
            float duration)
        {
            if (cardViews == null || cardViews.Count == 0)
                yield break;

            var transforms = new List<RectTransform>(cardViews.Count);
            var baseScales = new List<Vector3>(cardViews.Count);

            foreach (CardView cardView in cardViews)
            {
                if (cardView == null)
                    continue;

                transforms.Add(cardView.RectTransform);
                baseScales.Add(cardView.RectTransform.localScale);
            }

            if (transforms.Count == 0)
                yield break;

            float halfDuration = Mathf.Max(0.01f, duration * 0.5f);
            float elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);

                for (int i = 0; i < transforms.Count; i++)
                {
                    Vector3 baseScale = baseScales[i];
                    float scaleX = Mathf.Lerp(baseScale.x, 0f, EaseOutCubic(t));
                    transforms[i].localScale = new Vector3(scaleX, baseScale.y, baseScale.z);
                }

                yield return null;
            }

            foreach (CardView cardView in cardViews)
            {
                if (cardView != null)
                    cardView.SetFaceUp(faceUp);
            }

            elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);

                for (int i = 0; i < transforms.Count; i++)
                {
                    Vector3 baseScale = baseScales[i];
                    float scaleX = Mathf.Lerp(0f, baseScale.x, EaseOutCubic(t));
                    transforms[i].localScale = new Vector3(scaleX, baseScale.y, baseScale.z);
                }

                yield return null;
            }

            for (int i = 0; i < transforms.Count; i++)
            {
                transforms[i].localScale = baseScales[i];
            }
        }

        private static float EaseOutCubic(float t)
        {
            return 1f - Mathf.Pow(1f - t, 3f);
        }
    }
}
