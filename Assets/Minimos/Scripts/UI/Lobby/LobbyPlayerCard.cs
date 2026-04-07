using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Minimos.UI
{
    /// <summary>
    /// Displays a single player entry in the lobby player list:
    /// name, team color indicator, ready status, and host badge.
    /// </summary>
    public class LobbyPlayerCard : MonoBehaviour
    {
        #region Fields

        [Header("Display")]
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private Image teamColorIndicator;
        [SerializeField] private Image readyStatusIcon;
        [SerializeField] private GameObject hostBadge;

        [Header("Ready Status Colors")]
        [SerializeField] private Color readyIconColor = new(0.3f, 0.9f, 0.3f);
        [SerializeField] private Color notReadyIconColor = new(0.6f, 0.6f, 0.6f);

        [Header("Animation")]
        [SerializeField] private float pulseDuration = 0.3f;
        [SerializeField] private float pulseScale = 1.3f;

        private RectTransform rectTransform;
        private bool currentReadyState;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Configures this card with player data.
        /// </summary>
        /// <param name="name">Player display name.</param>
        /// <param name="teamColor">Team color for the indicator bar.</param>
        /// <param name="isReady">Whether the player is ready.</param>
        /// <param name="isHost">Whether this player is the lobby host.</param>
        public void Setup(string name, Color teamColor, bool isReady, bool isHost)
        {
            if (playerNameText != null) playerNameText.text = name;
            if (teamColorIndicator != null) teamColorIndicator.color = teamColor;
            if (hostBadge != null) hostBadge.SetActive(isHost);

            currentReadyState = isReady;
            UpdateReadyVisual(isReady);
        }

        /// <summary>
        /// Updates the ready state with an optional pulse animation.
        /// </summary>
        /// <param name="isReady">New ready state.</param>
        public void SetReady(bool isReady)
        {
            if (isReady == currentReadyState) return;

            currentReadyState = isReady;
            UpdateReadyVisual(isReady);

            if (isReady && rectTransform != null)
            {
                StartCoroutine(PulseAnimation());
            }
        }

        #endregion

        #region Private Methods

        private void UpdateReadyVisual(bool isReady)
        {
            if (readyStatusIcon != null)
                readyStatusIcon.color = isReady ? readyIconColor : notReadyIconColor;
        }

        private IEnumerator PulseAnimation()
        {
            if (rectTransform == null) yield break;

            Vector3 originalScale = Vector3.one;
            Vector3 peakScale = Vector3.one * pulseScale;
            float half = pulseDuration * 0.5f;
            float elapsed = 0f;

            // Scale up
            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / half);
                rectTransform.localScale = Vector3.Lerp(originalScale, peakScale, t);
                yield return null;
            }

            // Scale down
            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / half);
                rectTransform.localScale = Vector3.Lerp(peakScale, originalScale, t);
                yield return null;
            }

            rectTransform.localScale = originalScale;
        }

        #endregion
    }
}
