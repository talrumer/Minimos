using System;
using System.Collections.Generic;
using UnityEngine;

namespace Minimos.Core
{
    /// <summary>
    /// How mini-games are selected each round.
    /// </summary>
    public enum MiniGameSelectionMode
    {
        Random,
        Vote,
        Sequential
    }

    /// <summary>
    /// Configuration for the current party session.
    /// </summary>
    [Serializable]
    public class PartyConfig
    {
        [Tooltip("Total number of rounds in this party.")]
        public int totalRounds = 5;

        [Tooltip("Number of teams competing.")]
        [Range(2, 6)]
        public int teamCount = 2;

        [Tooltip("Players per team.")]
        [Range(1, 2)]
        public int teamSize = 2;

        [Tooltip("How mini-games are chosen each round.")]
        public MiniGameSelectionMode selectionMode = MiniGameSelectionMode.Random;
    }

    /// <summary>
    /// Central manager for game flow, state, rounds, and party scoring.
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        #region Fields

        [Header("Party Config")]
        [SerializeField] private PartyConfig defaultConfig = new();

        private GameState currentState = GameState.Splash;
        private PartyConfig activeConfig;
        private int currentRound;
        private Dictionary<int, int> teamScores = new(); // teamIndex -> score

        #endregion

        #region Properties

        /// <summary>The current game state.</summary>
        public GameState CurrentState => currentState;

        /// <summary>The active party configuration. Null if no party is active.</summary>
        public PartyConfig ActiveConfig => activeConfig;

        /// <summary>Current round number (1-based). 0 if no party is active.</summary>
        public int CurrentRound => currentRound;

        /// <summary>Whether a party session is currently in progress.</summary>
        public bool IsPartyActive => activeConfig != null && currentRound > 0;

        #endregion

        #region Events

        /// <summary>Fired when a new party starts.</summary>
        public event Action OnPartyStarted;

        /// <summary>Fired when the party ends.</summary>
        public event Action OnPartyEnded;

        /// <summary>Fired when a new round begins. Passes round number (1-based).</summary>
        public event Action<int> OnRoundStarted;

        /// <summary>Fired when team scores are updated.</summary>
        public event Action<Dictionary<int, int>> OnScoresUpdated;

        #endregion

        #region Unity Lifecycle

        protected override void OnSingletonAwake()
        {
            activeConfig = null;
            currentRound = 0;
        }

        #endregion

        #region State Management

        /// <summary>
        /// Transitions to a new game state, firing the global event.
        /// </summary>
        /// <param name="newState">The state to transition to.</param>
        public void SetState(GameState newState)
        {
            if (currentState == newState) return;

            GameState previous = currentState;
            currentState = newState;

            Debug.Log($"[GameManager] State: {previous} -> {newState}");
            GameStateEvents.RaiseGameStateChanged(previous, newState);
        }

        #endregion

        #region Party Flow

        /// <summary>
        /// Starts a new party session with the given config (or default if null).
        /// </summary>
        /// <param name="config">Optional party configuration override.</param>
        public void StartParty(PartyConfig config = null)
        {
            activeConfig = config ?? new PartyConfig
            {
                totalRounds = defaultConfig.totalRounds,
                teamCount = defaultConfig.teamCount,
                teamSize = defaultConfig.teamSize,
                selectionMode = defaultConfig.selectionMode
            };

            currentRound = 0;
            teamScores.Clear();

            for (int i = 0; i < activeConfig.teamCount; i++)
            {
                teamScores[i] = 0;
            }

            Debug.Log($"[GameManager] Party started — {activeConfig.totalRounds} rounds, {activeConfig.teamCount} teams.");
            OnPartyStarted?.Invoke();
            OnScoresUpdated?.Invoke(new Dictionary<int, int>(teamScores));
        }

        /// <summary>
        /// Advances to the next round, or triggers final results if all rounds are done.
        /// </summary>
        public void NextRound()
        {
            if (activeConfig == null)
            {
                Debug.LogWarning("[GameManager] NextRound called with no active party.");
                return;
            }

            currentRound++;

            if (currentRound > activeConfig.totalRounds)
            {
                Debug.Log("[GameManager] All rounds complete — showing final results.");
                SetState(GameState.FinalResults);
                return;
            }

            Debug.Log($"[GameManager] Round {currentRound}/{activeConfig.totalRounds}");
            OnRoundStarted?.Invoke(currentRound);
            SetState(GameState.MiniGameIntro);
        }

        /// <summary>
        /// Ends the current party and returns to the main menu state.
        /// </summary>
        public void EndParty()
        {
            Debug.Log("[GameManager] Party ended.");
            OnPartyEnded?.Invoke();
            activeConfig = null;
            currentRound = 0;
            teamScores.Clear();
            SetState(GameState.MainMenu);
        }

        /// <summary>
        /// Returns to the main menu, ending any active party.
        /// </summary>
        public void ReturnToMenu()
        {
            if (IsPartyActive)
            {
                EndParty();
            }
            else
            {
                SetState(GameState.MainMenu);
            }
        }

        #endregion

        #region Scoring

        /// <summary>
        /// Adds points to a team's total score for the party.
        /// </summary>
        /// <param name="teamIndex">The team to award points to.</param>
        /// <param name="points">Number of points to add.</param>
        public void AddTeamScore(int teamIndex, int points)
        {
            if (!teamScores.ContainsKey(teamIndex))
            {
                Debug.LogWarning($"[GameManager] Team {teamIndex} not found in scores.");
                return;
            }

            teamScores[teamIndex] += points;
            Debug.Log($"[GameManager] Team {teamIndex} score: {teamScores[teamIndex]}");
            OnScoresUpdated?.Invoke(new Dictionary<int, int>(teamScores));
        }

        /// <summary>
        /// Gets the current score for a specific team.
        /// </summary>
        /// <param name="teamIndex">The team index to query.</param>
        /// <returns>The team's current score, or 0 if not found.</returns>
        public int GetTeamScore(int teamIndex)
        {
            return teamScores.TryGetValue(teamIndex, out int score) ? score : 0;
        }

        /// <summary>
        /// Returns a copy of all team scores.
        /// </summary>
        public Dictionary<int, int> GetAllScores()
        {
            return new Dictionary<int, int>(teamScores);
        }

        /// <summary>
        /// Returns the team index with the highest score. -1 if no scores.
        /// </summary>
        public int GetLeadingTeam()
        {
            int bestTeam = -1;
            int bestScore = int.MinValue;

            foreach (var kvp in teamScores)
            {
                if (kvp.Value > bestScore)
                {
                    bestScore = kvp.Value;
                    bestTeam = kvp.Key;
                }
            }

            return bestTeam;
        }

        #endregion
    }
}
