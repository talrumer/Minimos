using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Minimos.UI
{
    /// <summary>
    /// Controls the final podium celebration scene: positions characters on podiums,
    /// triggers emotes/particles, shows party stats, and provides end-of-game navigation.
    /// </summary>
    public class PodiumController : MonoBehaviour
    {
        #region Fields

        [Header("Podium Positions")]
        [Tooltip("Transforms where winning characters are placed. Index 0=1st, 1=2nd, 2=3rd.")]
        [SerializeField] private Transform[] podiumSpots = new Transform[3];

        [Header("Loser Positions")]
        [SerializeField] private Transform[] loserSpots;

        [Header("Particles")]
        [SerializeField] private ParticleSystem confettiSystem;

        [Header("UI — Team Name Labels")]
        [SerializeField] private TMP_Text firstPlaceLabel;
        [SerializeField] private TMP_Text secondPlaceLabel;
        [SerializeField] private TMP_Text thirdPlaceLabel;

        [Header("Party Stats Panel")]
        [SerializeField] private GameObject statsPanel;
        [SerializeField] private TMP_Text totalStunsText;
        [SerializeField] private TMP_Text closestRoundText;
        [SerializeField] private TMP_Text totalGoalsScoredText;
        [SerializeField] private TMP_Text longestStreakText;

        [Header("Navigation Buttons")]
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button returnToMenuButton;
        [SerializeField] private Button changeTeamsButton;

        [Header("Animation")]
        [SerializeField] private float characterSpawnDelay = 0.5f;
        [SerializeField] private float confettiDelay = 1.5f;
        [SerializeField] private float statsRevealDelay = 3f;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (playAgainButton != null) playAgainButton.onClick.AddListener(OnPlayAgainClicked);
            if (returnToMenuButton != null) returnToMenuButton.onClick.AddListener(OnReturnToMenuClicked);
            if (changeTeamsButton != null) changeTeamsButton.onClick.AddListener(OnChangeTeamsClicked);

            if (statsPanel != null) statsPanel.SetActive(false);
        }

        private void OnDisable()
        {
            if (playAgainButton != null) playAgainButton.onClick.RemoveListener(OnPlayAgainClicked);
            if (returnToMenuButton != null) returnToMenuButton.onClick.RemoveListener(OnReturnToMenuClicked);
            if (changeTeamsButton != null) changeTeamsButton.onClick.RemoveListener(OnChangeTeamsClicked);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Sets up the podium scene with ranked team data.
        /// Call after transitioning to the final results scene.
        /// </summary>
        /// <param name="rankedTeamIndices">Team indices sorted by placement (index 0 = 1st place).</param>
        /// <param name="characterPrefabs">Prefabs to spawn on podiums (matched to rankedTeamIndices).</param>
        public void Setup(int[] rankedTeamIndices, GameObject[] characterPrefabs)
        {
            StartCoroutine(SetupRoutine(rankedTeamIndices, characterPrefabs));
        }

        /// <summary>
        /// Populates the party stats summary panel.
        /// </summary>
        /// <param name="totalStuns">Total stuns across all rounds.</param>
        /// <param name="closestRound">Description of the closest round.</param>
        /// <param name="totalGoals">Total objectives/goals scored.</param>
        /// <param name="longestStreak">Longest win streak description.</param>
        public void SetPartyStats(int totalStuns, string closestRound, int totalGoals, string longestStreak)
        {
            if (totalStunsText != null) totalStunsText.text = $"Total Stuns: {totalStuns}";
            if (closestRoundText != null) closestRoundText.text = $"Closest Round: {closestRound}";
            if (totalGoalsScoredText != null) totalGoalsScoredText.text = $"Total Goals: {totalGoals}";
            if (longestStreakText != null) longestStreakText.text = $"Longest Streak: {longestStreak}";
        }

        #endregion

        #region Coroutines

        private IEnumerator SetupRoutine(int[] rankedTeamIndices, GameObject[] characterPrefabs)
        {
            // Place top 3 on podiums.
            for (int i = 0; i < Mathf.Min(3, rankedTeamIndices.Length); i++)
            {
                yield return new WaitForSeconds(characterSpawnDelay);

                if (i < podiumSpots.Length && podiumSpots[i] != null && i < characterPrefabs.Length && characterPrefabs[i] != null)
                {
                    GameObject character = Instantiate(characterPrefabs[i], podiumSpots[i].position,
                        podiumSpots[i].rotation, podiumSpots[i]);

                    // Trigger victory emote on the placed character.
                    var animator = character.GetComponentInChildren<Animator>();
                    if (animator != null)
                    {
                        animator.SetTrigger(i == 0 ? "Victory" : "Celebrate");
                    }
                }

                // Set podium label.
                SetPodiumLabel(i, rankedTeamIndices[i]);
            }

            // Place losers (4th+) in background.
            for (int i = 3; i < rankedTeamIndices.Length; i++)
            {
                int loserSlot = i - 3;
                if (loserSpots != null && loserSlot < loserSpots.Length && loserSpots[loserSlot] != null &&
                    i < characterPrefabs.Length && characterPrefabs[i] != null)
                {
                    GameObject character = Instantiate(characterPrefabs[i], loserSpots[loserSlot].position,
                        loserSpots[loserSlot].rotation, loserSpots[loserSlot]);

                    var animator = character.GetComponentInChildren<Animator>();
                    if (animator != null)
                    {
                        animator.SetTrigger("Sad");
                    }
                }
            }

            // Confetti.
            yield return new WaitForSeconds(confettiDelay - characterSpawnDelay * Mathf.Min(3, rankedTeamIndices.Length));
            if (confettiSystem != null) confettiSystem.Play();

            // Reveal stats panel.
            yield return new WaitForSeconds(statsRevealDelay);
            if (statsPanel != null) statsPanel.SetActive(true);
        }

        #endregion

        #region Button Handlers

        private void OnPlayAgainClicked()
        {
            Debug.Log("[Podium] Play Again.");
            Core.GameManager.Instance?.StartParty();
            Core.SceneLoader.Instance?.LoadSceneWithState(
                Core.SceneLoader.SCENE_LOBBY, Core.GameState.Lobby);
        }

        private void OnReturnToMenuClicked()
        {
            Debug.Log("[Podium] Return to Menu.");
            Core.GameManager.Instance?.ReturnToMenu();
            Core.SceneLoader.Instance?.LoadSceneWithState(
                Core.SceneLoader.SCENE_MAIN_MENU, Core.GameState.MainMenu);
        }

        private void OnChangeTeamsClicked()
        {
            Debug.Log("[Podium] Change Teams.");
            Teams.TeamManager.Instance?.ClearAssignments();
            Core.SceneLoader.Instance?.LoadSceneWithState(
                Core.SceneLoader.SCENE_LOBBY, Core.GameState.TeamSelect);
        }

        #endregion

        #region Helpers

        private void SetPodiumLabel(int podiumIndex, int teamIndex)
        {
            string teamName = Teams.TeamManager.HasInstance
                ? Teams.TeamManager.Instance.GetTeamName(teamIndex)
                : $"Team {teamIndex + 1}";

            TMP_Text label = podiumIndex switch
            {
                0 => firstPlaceLabel,
                1 => secondPlaceLabel,
                2 => thirdPlaceLabel,
                _ => null
            };

            if (label != null) label.text = teamName;
        }

        #endregion
    }
}
