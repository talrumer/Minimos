using System.Collections;
using UnityEngine;
using TMPro;

namespace Minimos.UI
{
    /// <summary>
    /// Static utility class providing reusable UI animation coroutines.
    /// All methods return a Coroutine-compatible IEnumerator for chaining.
    /// Start them via MonoBehaviour.StartCoroutine().
    /// </summary>
    public static class UIAnimations
    {
        #region Scale Animations

        /// <summary>
        /// Scales a RectTransform from zero to full size with an elastic overshoot.
        /// </summary>
        /// <param name="target">The RectTransform to animate.</param>
        /// <param name="duration">Total animation duration in seconds.</param>
        /// <returns>Coroutine enumerator.</returns>
        public static IEnumerator BounceIn(RectTransform target, float duration)
        {
            if (target == null) yield break;

            float elapsed = 0f;
            target.localScale = Vector3.zero;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Elastic ease-out: overshoot then settle
                float scale = 1f + Mathf.Sin(t * Mathf.PI * 2.5f) * (1f - t) * 0.3f;
                scale = Mathf.LerpUnclamped(0f, scale, t);

                target.localScale = Vector3.one * scale;
                yield return null;
            }

            target.localScale = Vector3.one;
        }

        /// <summary>
        /// Applies a quick punch-scale effect (enlarge then return to original size).
        /// </summary>
        /// <param name="target">The RectTransform to animate.</param>
        /// <param name="punch">How much to scale up (e.g., 0.2 = 120% peak).</param>
        /// <param name="duration">Total animation duration in seconds.</param>
        /// <returns>Coroutine enumerator.</returns>
        public static IEnumerator PunchScale(RectTransform target, float punch, float duration)
        {
            if (target == null) yield break;

            float elapsed = 0f;
            Vector3 originalScale = target.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Quick spike then decay
                float scale = 1f + punch * Mathf.Sin(t * Mathf.PI) * (1f - t * 0.5f);
                target.localScale = originalScale * scale;
                yield return null;
            }

            target.localScale = originalScale;
        }

        #endregion

        #region Position Animations

        /// <summary>
        /// Slides a RectTransform from an offset position to its current anchored position.
        /// </summary>
        /// <param name="target">The RectTransform to animate.</param>
        /// <param name="fromOffset">Offset from the final position (e.g., new Vector2(-500, 0) for slide from left).</param>
        /// <param name="duration">Total animation duration in seconds.</param>
        /// <returns>Coroutine enumerator.</returns>
        public static IEnumerator SlideIn(RectTransform target, Vector2 fromOffset, float duration)
        {
            if (target == null) yield break;

            Vector2 endPos = target.anchoredPosition;
            Vector2 startPos = endPos + fromOffset;
            float elapsed = 0f;

            target.anchoredPosition = startPos;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Ease-out cubic
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                target.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);
                yield return null;
            }

            target.anchoredPosition = endPos;
        }

        #endregion

        #region Alpha Animations

        /// <summary>
        /// Fades a CanvasGroup from transparent to fully opaque.
        /// </summary>
        /// <param name="group">The CanvasGroup to fade.</param>
        /// <param name="duration">Fade duration in seconds.</param>
        /// <returns>Coroutine enumerator.</returns>
        public static IEnumerator FadeIn(CanvasGroup group, float duration)
        {
            if (group == null) yield break;

            float elapsed = 0f;
            group.alpha = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }

            group.alpha = 1f;
        }

        /// <summary>
        /// Fades a CanvasGroup from fully opaque to transparent.
        /// </summary>
        /// <param name="group">The CanvasGroup to fade.</param>
        /// <param name="duration">Fade duration in seconds.</param>
        /// <returns>Coroutine enumerator.</returns>
        public static IEnumerator FadeOut(CanvasGroup group, float duration)
        {
            if (group == null) yield break;

            float elapsed = 0f;
            group.alpha = 1f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = 1f - Mathf.Clamp01(elapsed / duration);
                yield return null;
            }

            group.alpha = 0f;
        }

        #endregion

        #region Text Animations

        /// <summary>
        /// Reveals text one character at a time (typewriter effect).
        /// </summary>
        /// <param name="text">The TMP_Text component to animate.</param>
        /// <param name="content">The full string to reveal.</param>
        /// <param name="charsPerSecond">How many characters to reveal per second.</param>
        /// <returns>Coroutine enumerator.</returns>
        public static IEnumerator TypewriterText(TMP_Text text, string content, float charsPerSecond)
        {
            if (text == null) yield break;

            text.text = content;
            text.maxVisibleCharacters = 0;

            float interval = 1f / Mathf.Max(charsPerSecond, 1f);
            float timer = 0f;
            int revealed = 0;
            int totalChars = content.Length;

            while (revealed < totalChars)
            {
                timer += Time.unscaledDeltaTime;

                while (timer >= interval && revealed < totalChars)
                {
                    revealed++;
                    text.maxVisibleCharacters = revealed;
                    timer -= interval;
                }

                yield return null;
            }

            text.maxVisibleCharacters = totalChars;
        }

        #endregion
    }
}
