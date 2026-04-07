using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Minimos.UI
{
    /// <summary>
    /// Controls the lobby UI: matchmaking buttons, room code display,
    /// player list, team selection, host controls, and ready state.
    /// </summary>
    public class LobbyUIController : MonoBehaviour
    {
        #region Fields

        [Header("Matchmaking")]
        [SerializeField] private Button quickPlayButton;
        [SerializeField] private Button createPrivateButton;
        [SerializeField] private Button joinByCodeButton;
        [SerializeField] private TMP_InputField roomCodeInput;

        [Header("Room Code Display")]
        [SerializeField] private GameObject roomCodePanel;
        [SerializeField] private TMP_Text roomCodeText;
        [SerializeField] private Button copyCodeButton;

        [Header("Player List")]
        [SerializeField] private Transform playerListContent;
        [SerializeField] private GameObject playerCardPrefab;

        [Header("Ready / Team")]
        [SerializeField] private Button readyButton;
        [SerializeField] private TMP_Text readyButtonText;
        [SerializeField] private Image readyButtonImage;
        [SerializeField] private Color readyColor = new(0.4f, 0.9f, 0.4f);
        [SerializeField] private Color notReadyColor = new(0.9f, 0.4f, 0.4f);

        [Header("Team Selection")]
        [SerializeField] private Button[] teamButtons;

        [Header("Host Controls")]
        [SerializeField] private GameObject hostControlsPanel;
        [SerializeField] private Slider roundsSlider;
        [SerializeField] private TMP_Text roundsValueText;
        [SerializeField] private TMP_Dropdown miniGameDropdown;
        [SerializeField] private Button startGameButton;

        [Header("Navigation")]
        [SerializeField] private Button backButton;

        private readonly List<LobbyPlayerCard> activeCards = new();
        private bool isReady;
        private bool isHost;
        private int selectedTeamIndex = -1;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            BindButtons();
            SetReady(false);
            if (roomCodePanel != null) roomCodePanel.SetActive(false);
            if (hostControlsPanel != null) hostControlsPanel.SetActive(false);
        }

        private void OnDisable()
        {
            UnbindButtons();
        }

        #endregion

        #region Button Binding

        private void BindButtons()
        {
            if (quickPlayButton != null) quickPlayButton.onClick.AddListener(OnQuickPlayClicked);
            if (createPrivateButton != null) createPrivateButton.onClick.AddListener(OnCreatePrivateClicked);
            if (joinByCodeButton != null) joinByCodeButton.onClick.AddListener(OnJoinByCodeClicked);
            if (copyCodeButton != null) copyCodeButton.onClick.AddListener(OnCopyCodeClicked);
            if (readyButton != null) readyButton.onClick.AddListener(OnReadyClicked);
            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
            if (startGameButton != null) startGameButton.onClick.AddListener(OnStartGameClicked);

            if (roundsSlider != null)
                roundsSlider.onValueChanged.AddListener(OnRoundsSliderChanged);

            for (int i = 0; i < teamButtons.Length; i++)
            {
                int teamIndex = i;
                if (teamButtons[i] != null)
                    teamButtons[i].onClick.AddListener(() => OnTeamSelected(teamIndex));
            }
        }

        private void UnbindButtons()
        {
            if (quickPlayButton != null) quickPlayButton.onClick.RemoveAllListeners();
            if (createPrivateButton != null) createPrivateButton.onClick.RemoveAllListeners();
            if (joinByCodeButton != null) joinByCodeButton.onClick.RemoveAllListeners();
            if (copyCodeButton != null) copyCodeButton.onClick.RemoveAllListeners();
            if (readyButton != null) readyButton.onClick.RemoveAllListeners();
            if (backButton != null) backButton.onClick.RemoveAllListeners();
            if (startGameButton != null) startGameButton.onClick.RemoveAllListeners();
            if (roundsSlider != null) roundsSlider.onValueChanged.RemoveAllListeners();

            foreach (var btn in teamButtons)
            {
                if (btn != null) btn.onClick.RemoveAllListeners();
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Displays the room code prominently for private lobbies.
        /// </summary>
        /// <param name="code">The room code to display.</param>
        public void ShowRoomCode(string code)
        {
            if (roomCodePanel != null) roomCodePanel.SetActive(true);
            if (roomCodeText != null) roomCodeText.text = code;
        }

        /// <summary>
        /// Configures the lobby for host or client mode.
        /// </summary>
        /// <param name="host">True if this player is the lobby host.</param>
        public void SetHostMode(bool host)
        {
            isHost = host;
            if (hostControlsPanel != null) hostControlsPanel.SetActive(host);
        }

        /// <summary>
        /// Rebuilds the player list from the provided data.
        /// </summary>
        /// <param name="players">List of player data tuples.</param>
        public void UpdatePlayerList(List<(string name, Color teamColor, bool isReady, bool isHost)> players)
        {
            // Clear existing cards.
            foreach (var card in activeCards)
            {
                if (card != null) Destroy(card.gameObject);
            }
            activeCards.Clear();

            // Spawn new cards.
            foreach (var data in players)
            {
                if (playerCardPrefab == null || playerListContent == null) continue;

                GameObject cardGo = Instantiate(playerCardPrefab, playerListContent);
                var card = cardGo.GetComponent<LobbyPlayerCard>();
                if (card != null)
                {
                    card.Setup(data.name, data.teamColor, data.isReady, data.isHost);
                    activeCards.Add(card);
                }
            }
        }

        /// <summary>
        /// Enables or disables the start game button (host only, when all players are ready).
        /// </summary>
        /// <param name="canStart">Whether the game can be started.</param>
        public void SetStartButtonEnabled(bool canStart)
        {
            if (startGameButton != null) startGameButton.interactable = canStart;
        }

        #endregion

        #region Button Handlers

        private void OnQuickPlayClicked()
        {
            Debug.Log("[Lobby] Quick Play requested.");
            // TODO: Hook into networking matchmaking.
        }

        private void OnCreatePrivateClicked()
        {
            Debug.Log("[Lobby] Create Private Lobby requested.");
            // TODO: Create lobby via networking and show room code.
        }

        private void OnJoinByCodeClicked()
        {
            string code = roomCodeInput != null ? roomCodeInput.text.Trim().ToUpper() : string.Empty;
            if (string.IsNullOrEmpty(code))
            {
                Debug.LogWarning("[Lobby] No room code entered.");
                return;
            }

            Debug.Log($"[Lobby] Joining by code: {code}");
            // TODO: Join lobby via networking.
        }

        private void OnCopyCodeClicked()
        {
            if (roomCodeText != null)
            {
                GUIUtility.systemCopyBuffer = roomCodeText.text;
                Debug.Log("[Lobby] Room code copied to clipboard.");
            }
        }

        private void OnReadyClicked()
        {
            SetReady(!isReady);
            Debug.Log($"[Lobby] Ready: {isReady}");
            // TODO: Send ready state to server.
        }

        private void OnTeamSelected(int teamIndex)
        {
            selectedTeamIndex = teamIndex;
            Debug.Log($"[Lobby] Team selected: {teamIndex}");
            // TODO: Send team selection to server.
        }

        private void OnRoundsSliderChanged(float value)
        {
            int rounds = Mathf.RoundToInt(value);
            if (roundsValueText != null) roundsValueText.text = rounds.ToString();
        }

        private void OnStartGameClicked()
        {
            if (!isHost) return;

            Debug.Log("[Lobby] Host starting game.");
            // TODO: Tell server to start the party.
        }

        private void OnBackClicked()
        {
            Debug.Log("[Lobby] Leaving lobby.");
            // TODO: Disconnect from lobby.

            if (UIManager.HasInstance)
                UIManager.Instance.FadeTransition(0.3f, () =>
                {
                    Core.SceneLoader.Instance?.LoadSceneWithState(
                        Core.SceneLoader.SCENE_MAIN_MENU, Core.GameState.MainMenu);
                });
            else
                Core.SceneLoader.Instance?.LoadSceneWithState(
                    Core.SceneLoader.SCENE_MAIN_MENU, Core.GameState.MainMenu);
        }

        #endregion

        #region Helpers

        private void SetReady(bool ready)
        {
            isReady = ready;

            if (readyButtonText != null)
                readyButtonText.text = isReady ? "READY!" : "Ready Up";

            if (readyButtonImage != null)
                readyButtonImage.color = isReady ? readyColor : notReadyColor;
        }

        #endregion
    }
}
