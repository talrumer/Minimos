using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Minimos.UI
{
    /// <summary>
    /// Identifies each UI panel for programmatic show/hide.
    /// </summary>
    public enum UIPanel
    {
        MainMenu,
        Lobby,
        HUD,
        Results,
        Loading,
        Settings
    }

    /// <summary>
    /// Singleton UI manager that owns references to all top-level panels and provides
    /// fade transitions, loading screens, and modal popups via standard Canvas UI.
    /// </summary>
    public class UIManager : Core.Singleton<UIManager>
    {
        #region Fields

        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject resultsPanel;
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private GameObject settingsPanel;

        [Header("Fade")]
        [Tooltip("Full-screen Image used for fade-to-black transitions.")]
        [SerializeField] private CanvasGroup fadeCanvasGroup;

        [Header("Popup")]
        [SerializeField] private GameObject popupPanel;
        [SerializeField] private TMP_Text popupTitleText;
        [SerializeField] private TMP_Text popupMessageText;
        [SerializeField] private Button popupConfirmButton;

        [Header("Loading Screen")]
        [SerializeField] private TMP_Text loadingMessageText;

        private readonly Dictionary<UIPanel, GameObject> panelMap = new();
        private Coroutine fadeRoutine;
        private Action pendingPopupCallback;

        #endregion

        #region Unity Lifecycle

        protected override void OnSingletonAwake()
        {
            panelMap[UIPanel.MainMenu] = mainMenuPanel;
            panelMap[UIPanel.Lobby] = lobbyPanel;
            panelMap[UIPanel.HUD] = hudPanel;
            panelMap[UIPanel.Results] = resultsPanel;
            panelMap[UIPanel.Loading] = loadingPanel;
            panelMap[UIPanel.Settings] = settingsPanel;

            if (popupConfirmButton != null)
                popupConfirmButton.onClick.AddListener(OnPopupConfirmClicked);

            // Start with everything hidden.
            HideAllPanels();
            if (popupPanel != null) popupPanel.SetActive(false);
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 0f;
                fadeCanvasGroup.blocksRaycasts = false;
            }
        }

        #endregion

        #region Panel Management

        /// <summary>
        /// Shows the specified UI panel. Optionally hides all others first.
        /// </summary>
        /// <param name="panel">The panel to show.</param>
        /// <param name="hideOthers">If true, all other panels are hidden first.</param>
        public void ShowPanel(UIPanel panel, bool hideOthers = false)
        {
            if (hideOthers) HideAllPanels();

            if (panelMap.TryGetValue(panel, out GameObject go) && go != null)
            {
                go.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"[UIManager] Panel '{panel}' is not assigned.");
            }
        }

        /// <summary>
        /// Hides the specified UI panel.
        /// </summary>
        /// <param name="panel">The panel to hide.</param>
        public void HidePanel(UIPanel panel)
        {
            if (panelMap.TryGetValue(panel, out GameObject go) && go != null)
            {
                go.SetActive(false);
            }
        }

        /// <summary>
        /// Hides all registered UI panels.
        /// </summary>
        public void HideAllPanels()
        {
            foreach (var kvp in panelMap)
            {
                if (kvp.Value != null)
                    kvp.Value.SetActive(false);
            }
        }

        /// <summary>
        /// Returns whether the given panel is currently active.
        /// </summary>
        /// <param name="panel">The panel to query.</param>
        public bool IsPanelVisible(UIPanel panel)
        {
            return panelMap.TryGetValue(panel, out GameObject go) && go != null && go.activeSelf;
        }

        #endregion

        #region Loading Screen

        /// <summary>
        /// Shows the loading panel with a custom message.
        /// </summary>
        /// <param name="message">Text to display on the loading screen.</param>
        public void ShowLoadingScreen(string message)
        {
            ShowPanel(UIPanel.Loading);

            if (loadingMessageText != null)
                loadingMessageText.text = message;
        }

        /// <summary>
        /// Hides the loading panel.
        /// </summary>
        public void HideLoadingScreen()
        {
            HidePanel(UIPanel.Loading);
        }

        #endregion

        #region Popup

        /// <summary>
        /// Shows a modal popup with a title, message, and confirm button.
        /// </summary>
        /// <param name="title">Popup title.</param>
        /// <param name="message">Popup body text.</param>
        /// <param name="onConfirm">Callback invoked when the user clicks Confirm.</param>
        public void ShowPopup(string title, string message, Action onConfirm = null)
        {
            if (popupPanel == null)
            {
                Debug.LogWarning("[UIManager] Popup panel is not assigned.");
                onConfirm?.Invoke();
                return;
            }

            if (popupTitleText != null) popupTitleText.text = title;
            if (popupMessageText != null) popupMessageText.text = message;

            pendingPopupCallback = onConfirm;
            popupPanel.SetActive(true);
        }

        private void OnPopupConfirmClicked()
        {
            if (popupPanel != null) popupPanel.SetActive(false);

            pendingPopupCallback?.Invoke();
            pendingPopupCallback = null;
        }

        #endregion

        #region Fade Transitions

        /// <summary>
        /// Fades the screen to black over the given duration.
        /// </summary>
        /// <param name="duration">Fade duration in seconds.</param>
        public void FadeToBlack(float duration)
        {
            if (fadeCanvasGroup == null) return;

            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeRoutine(fadeCanvasGroup.alpha, 1f, duration));
        }

        /// <summary>
        /// Fades the screen from black to transparent over the given duration.
        /// </summary>
        /// <param name="duration">Fade duration in seconds.</param>
        public void FadeFromBlack(float duration)
        {
            if (fadeCanvasGroup == null) return;

            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeRoutine(fadeCanvasGroup.alpha, 0f, duration));
        }

        /// <summary>
        /// Fades to black, invokes a callback, then fades back in.
        /// Useful for scene transitions.
        /// </summary>
        /// <param name="fadeDuration">Duration of each fade half.</param>
        /// <param name="onBlack">Callback invoked while the screen is fully black.</param>
        public void FadeTransition(float fadeDuration, Action onBlack)
        {
            if (fadeCanvasGroup == null)
            {
                onBlack?.Invoke();
                return;
            }

            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeTransitionRoutine(fadeDuration, onBlack));
        }

        private IEnumerator FadeRoutine(float from, float to, float duration)
        {
            fadeCanvasGroup.blocksRaycasts = true;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            fadeCanvasGroup.alpha = to;
            fadeCanvasGroup.blocksRaycasts = to > 0.01f;
            fadeRoutine = null;
        }

        private IEnumerator FadeTransitionRoutine(float fadeDuration, Action onBlack)
        {
            yield return FadeRoutine(0f, 1f, fadeDuration);
            onBlack?.Invoke();
            yield return FadeRoutine(1f, 0f, fadeDuration);
        }

        #endregion
    }
}
