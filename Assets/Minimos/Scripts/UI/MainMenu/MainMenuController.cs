using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Minimos.UI
{
    /// <summary>
    /// Controls the main menu scene: button navigation, player profile display,
    /// and entrance animations.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        #region Fields

        [Header("Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button characterStudioButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Button RectTransforms (for animations)")]
        [SerializeField] private RectTransform playButtonRect;
        [SerializeField] private RectTransform characterStudioButtonRect;
        [SerializeField] private RectTransform settingsButtonRect;
        [SerializeField] private RectTransform quitButtonRect;

        [Header("Profile Display")]
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text playerLevelText;
        [SerializeField] private TMP_Text playerCoinsText;

        [Header("Animation")]
        [SerializeField] private float slideOffset = 600f;
        [SerializeField] private float slideDuration = 0.4f;
        [SerializeField] private float slideStagger = 0.1f;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            BindButtons();
            LoadPlayerProfile();
            StartCoroutine(AnimateButtonsIn());
        }

        private void OnDisable()
        {
            UnbindButtons();
        }

        #endregion

        #region Button Binding

        private void BindButtons()
        {
            if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
            if (characterStudioButton != null) characterStudioButton.onClick.AddListener(OnCharacterStudioClicked);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);
        }

        private void UnbindButtons()
        {
            if (playButton != null) playButton.onClick.RemoveListener(OnPlayClicked);
            if (characterStudioButton != null) characterStudioButton.onClick.RemoveListener(OnCharacterStudioClicked);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettingsClicked);
            if (quitButton != null) quitButton.onClick.RemoveListener(OnQuitClicked);
        }

        #endregion

        #region Button Handlers

        /// <summary>
        /// Transitions to the lobby scene for matchmaking.
        /// </summary>
        public void OnPlayClicked()
        {
            Debug.Log("[MainMenu] Play clicked.");

            if (UIManager.HasInstance)
                UIManager.Instance.FadeTransition(0.3f, () =>
                {
                    Core.SceneLoader.Instance?.LoadSceneWithState(
                        Core.SceneLoader.SCENE_LOBBY, Core.GameState.Lobby);
                });
            else
                Core.SceneLoader.Instance?.LoadSceneWithState(
                    Core.SceneLoader.SCENE_LOBBY, Core.GameState.Lobby);
        }

        /// <summary>
        /// Transitions to the character studio for customization.
        /// </summary>
        public void OnCharacterStudioClicked()
        {
            Debug.Log("[MainMenu] Character Studio clicked.");

            if (UIManager.HasInstance)
                UIManager.Instance.FadeTransition(0.3f, () =>
                {
                    Core.SceneLoader.Instance?.LoadSceneWithState(
                        Core.SceneLoader.SCENE_CHARACTER_STUDIO, Core.GameState.CharacterStudio);
                });
            else
                Core.SceneLoader.Instance?.LoadSceneWithState(
                    Core.SceneLoader.SCENE_CHARACTER_STUDIO, Core.GameState.CharacterStudio);
        }

        /// <summary>
        /// Shows the settings overlay panel.
        /// </summary>
        public void OnSettingsClicked()
        {
            Debug.Log("[MainMenu] Settings clicked.");
            UIManager.Instance?.ShowPanel(UIPanel.Settings);
        }

        /// <summary>
        /// Quits the application (no-op in the editor).
        /// </summary>
        public void OnQuitClicked()
        {
            Debug.Log("[MainMenu] Quit clicked.");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region Profile Display

        private async void LoadPlayerProfile()
        {
            // Attempt to load from Firebase; fall back to defaults.
            var firebase = FindAnyObjectByType<Firebase.MockFirebaseService>();
            if (firebase == null || !firebase.IsSignedIn())
            {
                SetProfileDisplay("Player", 1, 0);
                return;
            }

            string userId = firebase.GetCurrentUserId();
            var profile = await firebase.GetPlayerProfile(userId);

            if (profile != null)
            {
                SetProfileDisplay(profile.DisplayName, profile.Level, profile.Coins);
            }
            else
            {
                SetProfileDisplay("Player", 1, 0);
            }
        }

        private void SetProfileDisplay(string displayName, int level, int coins)
        {
            if (playerNameText != null) playerNameText.text = displayName;
            if (playerLevelText != null) playerLevelText.text = $"Lv. {level}";
            if (playerCoinsText != null) playerCoinsText.text = coins.ToString("N0");
        }

        #endregion

        #region Animations

        private IEnumerator AnimateButtonsIn()
        {
            RectTransform[] buttons = { playButtonRect, characterStudioButtonRect, settingsButtonRect, quitButtonRect };

            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null) continue;

                // Alternate sliding from left and right.
                float offset = (i % 2 == 0) ? -slideOffset : slideOffset;
                StartCoroutine(UIAnimations.SlideIn(buttons[i], new Vector2(offset, 0f), slideDuration));

                yield return new WaitForSecondsRealtime(slideStagger);
            }
        }

        #endregion
    }
}
