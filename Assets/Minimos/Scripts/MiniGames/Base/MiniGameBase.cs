using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Minimos.MiniGames
{
    /// <summary>
    /// Phases of a mini-game lifecycle.
    /// </summary>
    public enum MiniGamePhase : byte
    {
        WaitingToStart,
        Countdown,
        Playing,
        Overtime,
        Ended
    }

    /// <summary>
    /// Result data for a single team at game end.
    /// </summary>
    [Serializable]
    public class TeamResult
    {
        public int TeamIndex;
        public int Score;
        public int Rank;

        public TeamResult(int teamIndex, int score, int rank)
        {
            TeamIndex = teamIndex;
            Score = score;
            Rank = rank;
        }
    }

    /// <summary>
    /// Aggregated results from a completed mini-game.
    /// </summary>
    [Serializable]
    public class MiniGameResults
    {
        public List<TeamResult> Rankings;
        public ulong MvpPlayerId;
        public string MvpStatDescription;

        public MiniGameResults()
        {
            Rankings = new List<TeamResult>();
            MvpPlayerId = ulong.MaxValue;
            MvpStatDescription = string.Empty;
        }
    }

    /// <summary>
    /// Abstract base class for all mini-game modes. Manages timer, scoring,
    /// phase transitions, and the game lifecycle. Host-authoritative.
    /// </summary>
    public abstract class MiniGameBase : NetworkBehaviour
    {
        [Header("Config")]
        [SerializeField] protected MiniGameConfig config;

        // --- Network state ---
        protected NetworkList<int> teamScores;

        private NetworkVariable<float> gameTimer = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private NetworkVariable<bool> isGameActive = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private NetworkVariable<MiniGamePhase> currentPhase = new NetworkVariable<MiniGamePhase>(
            MiniGamePhase.WaitingToStart, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        // --- Countdown ---
        private float countdownTimer;
        private int lastCountdownTick;

        // --- Events ---
        /// <summary>Fires each second during countdown with seconds remaining.</summary>
        public event Action<int> OnCountdownTick;

        /// <summary>Fires when gameplay begins (after countdown).</summary>
        public event Action OnGameStarted;

        /// <summary>Fires when the game ends with final results.</summary>
        public event Action<MiniGameResults> OnGameEnded;

        // --- Public accessors ---
        public MiniGameConfig Config => config;
        public float GameTimer => gameTimer.Value;
        public bool IsGameActive => isGameActive.Value;
        public MiniGamePhase CurrentPhase => currentPhase.Value;

        /// <summary>
        /// Gets the current score for a team.
        /// </summary>
        public int GetTeamScore(int teamIndex)
        {
            if (teamIndex >= 0 && teamIndex < teamScores.Count)
                return teamScores[teamIndex];
            return 0;
        }

        // --- Lifecycle ---

        private void Awake()
        {
            teamScores = new NetworkList<int>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner && config != null)
            {
                InitializeScores();
                OnGameSetup();
            }
        }

        private void Update()
        {
            if (!IsOwner) return;

            switch (currentPhase.Value)
            {
                case MiniGamePhase.Countdown:
                    UpdateCountdown();
                    break;
                case MiniGamePhase.Playing:
                case MiniGamePhase.Overtime:
                    UpdatePlaying();
                    break;
            }
        }

        // --- Setup ---

        private void InitializeScores()
        {
            teamScores.Clear();
            int teamCount = config.MaxTeams; // Will be overridden by match controller
            for (int i = 0; i < teamCount; i++)
                teamScores.Add(0);
        }

        /// <summary>
        /// Reinitializes scores for a specific team count. Call before starting.
        /// </summary>
        public void SetTeamCount(int count)
        {
            if (!IsOwner) return;
            teamScores.Clear();
            for (int i = 0; i < count; i++)
                teamScores.Add(0);
        }

        // --- Countdown ---

        /// <summary>
        /// Begins the pre-game countdown. Host only.
        /// </summary>
        public void StartCountdown(int seconds = 3)
        {
            if (!IsOwner) return;
            countdownTimer = seconds;
            lastCountdownTick = seconds;
            currentPhase.Value = MiniGamePhase.Countdown;
            BroadcastCountdownTickClientRpc(seconds);
        }

        private void UpdateCountdown()
        {
            countdownTimer -= Time.deltaTime;
            int tick = Mathf.CeilToInt(countdownTimer);

            if (tick != lastCountdownTick && tick > 0)
            {
                lastCountdownTick = tick;
                BroadcastCountdownTickClientRpc(tick);
            }

            if (countdownTimer <= 0f)
            {
                BeginPlay();
            }
        }

        [ClientRpc]
        private void BroadcastCountdownTickClientRpc(int secondsLeft)
        {
            OnCountdownTick?.Invoke(secondsLeft);
        }

        // --- Play ---

        private void BeginPlay()
        {
            gameTimer.Value = config.Duration;
            isGameActive.Value = true;
            currentPhase.Value = MiniGamePhase.Playing;
            OnGameStart();
            BroadcastGameStartedClientRpc();
        }

        [ClientRpc]
        private void BroadcastGameStartedClientRpc()
        {
            OnGameStarted?.Invoke();
        }

        private void UpdatePlaying()
        {
            if (config.Duration > 0f)
            {
                gameTimer.Value -= Time.deltaTime;
                if (gameTimer.Value <= 0f)
                {
                    gameTimer.Value = 0f;
                    EndGame();
                    return;
                }
            }

            OnGameUpdate();

            // Check score-based win condition
            if (config.ScoreToWin > 0)
            {
                for (int i = 0; i < teamScores.Count; i++)
                {
                    if (teamScores[i] >= config.ScoreToWin)
                    {
                        EndGame();
                        return;
                    }
                }
            }
        }

        // --- Scoring ---

        /// <summary>
        /// Awards points to a team. Override for custom scoring behavior.
        /// </summary>
        public virtual void OnPlayerScored(int teamIndex, int points)
        {
            if (!IsOwner) return;
            if (teamIndex < 0 || teamIndex >= teamScores.Count) return;
            teamScores[teamIndex] += points;
        }

        /// <summary>
        /// Called when a player is eliminated. Override in elimination-based modes.
        /// </summary>
        public virtual void OnPlayerEliminated(ulong clientId)
        {
        }

        // --- End ---

        /// <summary>
        /// Ends the game, calculates results, and broadcasts them to all clients.
        /// </summary>
        public void EndGame()
        {
            if (!IsOwner) return;
            if (currentPhase.Value == MiniGamePhase.Ended) return;

            isGameActive.Value = false;
            currentPhase.Value = MiniGamePhase.Ended;
            OnGameEnd();

            MiniGameResults results = GetResults();
            BroadcastGameEndedClientRpc(
                results.MvpPlayerId,
                results.MvpStatDescription ?? string.Empty);
        }

        [ClientRpc]
        private void BroadcastGameEndedClientRpc(ulong mvpId, string mvpStat)
        {
            // Reconstruct results on client from synced NetworkList
            var results = BuildResultsFromScores();
            results.MvpPlayerId = mvpId;
            results.MvpStatDescription = mvpStat;
            OnGameEnded?.Invoke(results);
        }

        private MiniGameResults BuildResultsFromScores()
        {
            var results = new MiniGameResults();
            var indexed = new List<(int index, int score)>();
            for (int i = 0; i < teamScores.Count; i++)
                indexed.Add((i, teamScores[i]));

            indexed.Sort((a, b) => b.score.CompareTo(a.score));

            int rank = 1;
            for (int i = 0; i < indexed.Count; i++)
            {
                if (i > 0 && indexed[i].score < indexed[i - 1].score)
                    rank = i + 1;
                results.Rankings.Add(new TeamResult(indexed[i].index, indexed[i].score, rank));
            }

            return results;
        }

        // --- Abstract methods subclasses must implement ---

        /// <summary>Called once on spawn. Set up arena, spawn objects, etc.</summary>
        protected abstract void OnGameSetup();

        /// <summary>Called when gameplay begins (after countdown).</summary>
        protected abstract void OnGameStart();

        /// <summary>Called every frame during gameplay. Host only.</summary>
        protected abstract void OnGameUpdate();

        /// <summary>Called when the game ends. Clean up game-specific state.</summary>
        protected abstract void OnGameEnd();

        /// <summary>Build and return final results including MVP. Host only.</summary>
        protected abstract MiniGameResults GetResults();
    }
}
