using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Minimos.UI
{
    /// <summary>
    /// Displays a single team's score as a filled bar with animated score counting.
    /// </summary>
    public class ScoreDisplay : MonoBehaviour
    {
        #region Fields

        [Header("References")]
        [SerializeField] private Image teamColorBar;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private Image teamIcon;

        [Header("Animation")]
        [SerializeField] private float fillAnimDuration = 0.4f;
        [SerializeField] private float countAnimDuration = 0.5f;
        [SerializeField] private float punchScale = 0.15f;
        [SerializeField] private float punchDuration = 0.25f;

        private int currentDisplayScore;
        private int targetScore;
        private float currentFill;
        private float targetFill;
        private Coroutine fillRoutine;
        private Coroutine countRoutine;
        private RectTransform rectTransform;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Initializes the score display with a team color and name.
        /// </summary>
        /// <param name="teamColor">The team's primary color.</param>
        /// <param name="teamName">The team's display name.</param>
        public void Setup(Color teamColor, string teamName)
        {
            if (teamColorBar != null)
            {
                teamColorBar.color = teamColor;
                teamColorBar.fillAmount = 0f;
            }

            if (scoreText != null) scoreText.text = "0";
            if (teamIcon != null) teamIcon.color = teamColor;

            currentDisplayScore = 0;
            targetScore = 0;
            currentFill = 0f;
            targetFill = 0f;
        }

        /// <summary>
        /// Updates the displayed score with animated bar fill and number counting.
        /// Triggers a punch animation if the score changed.
        /// </summary>
        /// <param name="score">New score value.</param>
        /// <param name="maxScore">Max possible score (for fill calculation).</param>
        public void UpdateScore(int score, int maxScore)
        {
            if (score == targetScore) return;

            int previousTarget = targetScore;
            targetScore = score;
            targetFill = maxScore > 0 ? (float)score / maxScore : 0f;

            // Animate fill bar.
            if (fillRoutine != null) StopCoroutine(fillRoutine);
            fillRoutine = StartCoroutine(AnimateFill());

            // Animate score count.
            if (countRoutine != null) StopCoroutine(countRoutine);
            countRoutine = StartCoroutine(AnimateCount(currentDisplayScore, targetScore));

            // Punch on score increase.
            if (score > previousTarget)
            {
                PunchOnScoreChange();
            }
        }

        /// <summary>
        /// Plays a bounce/punch animation on the score widget.
        /// </summary>
        public void PunchOnScoreChange()
        {
            if (rectTransform != null)
                StartCoroutine(UIAnimations.PunchScale(rectTransform, punchScale, punchDuration));
        }

        #endregion

        #region Animations

        private IEnumerator AnimateFill()
        {
            float startFill = currentFill;
            float elapsed = 0f;

            while (elapsed < fillAnimDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fillAnimDuration);
                // Ease-out quad.
                float eased = 1f - (1f - t) * (1f - t);
                currentFill = Mathf.Lerp(startFill, targetFill, eased);

                if (teamColorBar != null)
                    teamColorBar.fillAmount = currentFill;

                yield return null;
            }

            currentFill = targetFill;
            if (teamColorBar != null) teamColorBar.fillAmount = currentFill;
            fillRoutine = null;
        }

        private IEnumerator AnimateCount(int from, int to)
        {
            float elapsed = 0f;

            while (elapsed < countAnimDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / countAnimDuration);
                currentDisplayScore = Mathf.RoundToInt(Mathf.Lerp(from, to, t));

                if (scoreText != null)
                    scoreText.text = currentDisplayScore.ToString();

                yield return null;
            }

            currentDisplayScore = to;
            if (scoreText != null) scoreText.text = currentDisplayScore.ToString();
            countRoutine = null;
        }

        #endregion
    }
}
