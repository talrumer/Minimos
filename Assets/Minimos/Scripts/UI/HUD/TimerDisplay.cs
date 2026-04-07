using UnityEngine;
using TMPro;

namespace Minimos.UI
{
    /// <summary>
    /// Displays the game timer as M:SS with urgency effects
    /// (color pulse under 30s, flash under 10s, hidden under 5s for Hot Potato).
    /// </summary>
    public class TimerDisplay : MonoBehaviour
    {
        #region Fields

        [Header("References")]
        [SerializeField] private TMP_Text timeText;

        [Header("Urgency Thresholds")]
        [SerializeField] private float pulseThreshold = 30f;
        [SerializeField] private float flashThreshold = 10f;
        [SerializeField] private float hideThreshold = 5f;

        [Header("Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color urgentColor = new(0.9f, 0.2f, 0.2f);

        [Header("Flash Settings")]
        [SerializeField] private float flashFrequency = 4f;

        [Header("Behavior")]
        [Tooltip("When true, hides the timer below hideThreshold (for Hot Potato).")]
        [SerializeField] private bool hideTimerWhenLow;

        private float displayedTime;

        #endregion

        #region Public API

        /// <summary>
        /// Updates the timer display.
        /// </summary>
        /// <param name="seconds">Remaining time in seconds.</param>
        public void UpdateTime(float seconds)
        {
            displayedTime = Mathf.Max(0f, seconds);

            if (timeText == null) return;

            // Hidden timer mechanic.
            if (hideTimerWhenLow && displayedTime <= hideThreshold && displayedTime > 0f)
            {
                timeText.text = "?:??";
                timeText.color = urgentColor;
                return;
            }

            // Format as M:SS.
            int totalSeconds = Mathf.CeilToInt(displayedTime);
            int minutes = totalSeconds / 60;
            int secs = totalSeconds % 60;
            timeText.text = $"{minutes}:{secs:D2}";

            // Color and flash effects.
            if (displayedTime <= flashThreshold)
            {
                // Flash between white and red.
                float flash = Mathf.Sin(Time.unscaledTime * flashFrequency * Mathf.PI * 2f);
                timeText.color = Color.Lerp(urgentColor, normalColor, (flash + 1f) * 0.5f);

                // Scale pulse.
                float scalePulse = 1f + Mathf.Abs(flash) * 0.1f;
                timeText.transform.localScale = Vector3.one * scalePulse;
            }
            else if (displayedTime <= pulseThreshold)
            {
                // Slow pulse to red.
                float pulse = Mathf.Sin(Time.unscaledTime * 2f * Mathf.PI);
                timeText.color = Color.Lerp(normalColor, urgentColor, (pulse + 1f) * 0.3f);
                timeText.transform.localScale = Vector3.one;
            }
            else
            {
                timeText.color = normalColor;
                timeText.transform.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// Enables or disables the hidden-timer mechanic (for Hot Potato).
        /// </summary>
        /// <param name="hide">True to hide timer when below threshold.</param>
        public void SetHideTimerWhenLow(bool hide)
        {
            hideTimerWhenLow = hide;
        }

        #endregion
    }
}
