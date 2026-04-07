using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Minimos.UI
{
    /// <summary>
    /// In-game HUD displaying timer, scores, ability cooldowns, minimap,
    /// event popups, and the pre-game countdown overlay.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        #region Fields

        [Header("Timer & Game Info")]
        [SerializeField] private TimerDisplay timerDisplay;
        [SerializeField] private TMP_Text miniGameNameText;

        [Header("Scores")]
        [SerializeField] private ScoreDisplay[] scoreDisplays;

        [Header("Abilities / Cooldowns")]
        [SerializeField] private Image[] cooldownOverlays;

        [Header("Minimap")]
        [SerializeField] private RawImage minimapImage;

        [Header("Event Popup")]
        [SerializeField] private TMP_Text eventPopupText;
        [SerializeField] private CanvasGroup eventPopupCanvasGroup;
        [SerializeField] private RectTransform eventPopupRect;

        [Header("Countdown Overlay")]
        [SerializeField] private GameObject countdownPanel;
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private RectTransform countdownRect;

        [Header("Root")]
        [SerializeField] private CanvasGroup hudCanvasGroup;

        private Coroutine eventPopupRoutine;
        private Coroutine countdownRoutine;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (eventPopupCanvasGroup != null) eventPopupCanvasGroup.alpha = 0f;
            if (countdownPanel != null) countdownPanel.SetActive(false);
        }

        #endregion

        #region Public API — Game Info

        /// <summary>
        /// Sets the mini-game name displayed above the timer.
        /// </summary>
        /// <param name="gameName">Mini-game display name.</param>
        public void SetMiniGameName(string gameName)
        {
            if (miniGameNameText != null) miniGameNameText.text = gameName;
        }

        /// <summary>
        /// Updates the timer display.
        /// </summary>
        /// <param name="timeRemaining">Seconds remaining.</param>
        public void UpdateTimer(float timeRemaining)
        {
            if (timerDisplay != null) timerDisplay.UpdateTime(timeRemaining);
        }

        #endregion

        #region Public API — Scores

        /// <summary>
        /// Initializes score displays for each active team.
        /// </summary>
        /// <param name="teamColors">Color per team, indexed by team.</param>
        /// <param name="teamNames">Display name per team.</param>
        public void InitializeScores(Color[] teamColors, string[] teamNames)
        {
            for (int i = 0; i < scoreDisplays.Length; i++)
            {
                if (i < teamColors.Length)
                {
                    scoreDisplays[i].gameObject.SetActive(true);
                    scoreDisplays[i].Setup(teamColors[i], teamNames[i]);
                }
                else
                {
                    scoreDisplays[i].gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Updates all team score bars.
        /// </summary>
        /// <param name="teamScores">Current score per team.</param>
        /// <param name="maxScore">Score cap for fill calculation (0 = use highest).</param>
        public void UpdateScores(int[] teamScores, int maxScore = 0)
        {
            int effectiveMax = maxScore > 0 ? maxScore : GetMaxScore(teamScores);

            for (int i = 0; i < scoreDisplays.Length && i < teamScores.Length; i++)
            {
                scoreDisplays[i].UpdateScore(teamScores[i], effectiveMax);
            }
        }

        private int GetMaxScore(int[] scores)
        {
            int max = 1;
            foreach (int s in scores)
            {
                if (s > max) max = s;
            }
            return max;
        }

        #endregion

        #region Public API — Cooldowns

        /// <summary>
        /// Sets the fill amount on an ability cooldown overlay.
        /// </summary>
        /// <param name="slotIndex">Ability slot (0-based).</param>
        /// <param name="fillAmount">Fill from 0 (ready) to 1 (full cooldown).</param>
        public void UpdateCooldown(int slotIndex, float fillAmount)
        {
            if (cooldownOverlays == null || slotIndex < 0 || slotIndex >= cooldownOverlays.Length) return;
            if (cooldownOverlays[slotIndex] != null)
                cooldownOverlays[slotIndex].fillAmount = Mathf.Clamp01(fillAmount);
        }

        #endregion

        #region Public API — Event Popup

        /// <summary>
        /// Shows a center-screen event popup that bounces in and fades out.
        /// </summary>
        /// <param name="text">Popup text (e.g., "+1 Point!").</param>
        /// <param name="color">Text color.</param>
        /// <param name="duration">Total display duration in seconds.</param>
        public void ShowEventPopup(string text, Color color, float duration = 1.5f)
        {
            if (eventPopupText == null || eventPopupCanvasGroup == null) return;

            if (eventPopupRoutine != null) StopCoroutine(eventPopupRoutine);
            eventPopupRoutine = StartCoroutine(EventPopupRoutine(text, color, duration));
        }

        private IEnumerator EventPopupRoutine(string text, Color color, float duration)
        {
            eventPopupText.text = text;
            eventPopupText.color = color;
            eventPopupCanvasGroup.alpha = 1f;

            // Bounce in
            if (eventPopupRect != null)
                yield return UIAnimations.BounceIn(eventPopupRect, 0.3f);

            // Hold
            yield return new WaitForSeconds(duration - 0.6f);

            // Fade out
            yield return UIAnimations.FadeOut(eventPopupCanvasGroup, 0.3f);

            eventPopupRoutine = null;
        }

        #endregion

        #region Public API — Countdown

        /// <summary>
        /// Displays a single countdown number with a bounce animation.
        /// </summary>
        /// <param name="number">Number to display (3, 2, 1).</param>
        public void ShowCountdownNumber(int number)
        {
            if (countdownText == null || countdownPanel == null) return;

            countdownPanel.SetActive(true);
            countdownText.text = number.ToString();

            if (countdownRoutine != null) StopCoroutine(countdownRoutine);
            countdownRoutine = StartCoroutine(CountdownNumberRoutine());
        }

        /// <summary>
        /// Shows the "GO!" text with a bounce and then hides the countdown.
        /// </summary>
        public void ShowCountdownGo()
        {
            if (countdownText == null || countdownPanel == null) return;

            countdownPanel.SetActive(true);
            countdownText.text = "GO!";
            countdownText.color = new Color(0.2f, 0.9f, 0.2f);

            if (countdownRoutine != null) StopCoroutine(countdownRoutine);
            countdownRoutine = StartCoroutine(CountdownGoRoutine());
        }

        private IEnumerator CountdownNumberRoutine()
        {
            countdownText.color = Color.white;

            if (countdownRect != null)
                yield return UIAnimations.BounceIn(countdownRect, 0.4f);
            else
                yield return new WaitForSeconds(0.4f);

            yield return new WaitForSeconds(0.5f);
            countdownRoutine = null;
        }

        private IEnumerator CountdownGoRoutine()
        {
            if (countdownRect != null)
                yield return UIAnimations.BounceIn(countdownRect, 0.3f);

            yield return new WaitForSeconds(0.5f);

            countdownPanel.SetActive(false);
            countdownRoutine = null;
        }

        #endregion

        #region Public API — Visibility

        /// <summary>
        /// Shows or hides the entire HUD.
        /// </summary>
        /// <param name="visible">Whether the HUD should be visible.</param>
        public void SetVisible(bool visible)
        {
            if (hudCanvasGroup != null)
            {
                hudCanvasGroup.alpha = visible ? 1f : 0f;
                hudCanvasGroup.interactable = visible;
                hudCanvasGroup.blocksRaycasts = visible;
            }
        }

        #endregion
    }
}
