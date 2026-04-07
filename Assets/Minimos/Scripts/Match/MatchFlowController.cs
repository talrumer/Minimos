using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Minimos.MiniGames;

namespace Minimos.Match
{
    /// <summary>
    /// Phases of the overall party match flow.
    /// </summary>
    public enum MatchPhase : byte
    {
        Lobby,
        MiniGameIntro,
        Playing,
        RoundResults,
        FinalResults
    }

    /// <summary>
    /// How the next mini-game is selected.
    /// </summary>
    public enum SelectionMode : byte
    {
        Random,
        Vote,
        HostPick
    }

    /// <summary>
    /// Manages the full party match flow: round progression, mini-game loading,
    /// scoring, voting, and final results. Host-authoritative.
    /// </summary>
    public class MatchFlowController : NetworkBehaviour
    {
        [Header("Match Settings")]
        [SerializeField] private int totalRounds = 5;
        [SerializeField] private SelectionMode selectionMode = SelectionMode.Random;

        [Header("Phase Durations")]
        [SerializeField] private float introDuration = 3f;
        [SerializeField] private float countdownDuration = 3;
        [SerializeField] private float resultsDuration = 8f;

        [Header("Scoring")]
        [SerializeField] private int[] placementPoints = { 10, 7, 5, 3, 2, 1 };

        // --- Network state ---
        private NetworkVariable<int> currentRound = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private NetworkVariable<MatchPhase> matchPhase = new NetworkVariable<MatchPhase>(
            MatchPhase.Lobby, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private NetworkList<int> partyScores;

        // --- Local state ---
        private float phaseTimer;
        private MiniGameConfig currentConfig;
        private int teamCount;
        private readonly List<MiniGameConfig> playedGames = new List<MiniGameConfig>();

        // Voting
        private List<MiniGameConfig> voteOptions = new List<MiniGameConfig>();
        private readonly Dictionary<ulong, int> clientVotes = new Dictionary<ulong, int>();

        // --- Events ---
        /// <summary>Fires when a new round begins with the round number (1-based).</summary>
        public event Action<int> OnRoundStarted;

        /// <summary>Fires when a round ends with team results for that round.</summary>
        public event Action<MiniGameResults> OnRoundEnded;

        /// <summary>Fires when the entire match ends with final standings.</summary>
        public event Action OnMatchEnded;

        /// <summary>Fires when party scores change.</summary>
        public event Action<IReadOnlyList<int>> OnPartyScoresUpdated;

        /// <summary>Fires to present vote options to clients.</summary>
        public event Action<List<MiniGameConfig>> OnVoteOptionsPresented;

        // --- Public accessors ---
        public int CurrentRound => currentRound.Value;
        public int TotalRounds => totalRounds;
        public MatchPhase CurrentPhase => matchPhase.Value;

        private void Awake()
        {
            partyScores = new NetworkList<int>();
        }

        /// <summary>
        /// Starts the party match with the given number of teams. Host only.
        /// </summary>
        public void StartMatch(int teams)
        {
            if (!IsOwner) return;

            teamCount = teams;
            partyScores.Clear();
            for (int i = 0; i < teams; i++)
                partyScores.Add(0);

            playedGames.Clear();
            currentRound.Value = 0;

            MiniGameManager.Instance?.ResetPlayedHistory();
            AdvanceToNextRound();
        }

        private void Update()
        {
            if (!IsOwner) return;

            switch (matchPhase.Value)
            {
                case MatchPhase.MiniGameIntro:
                    phaseTimer -= Time.deltaTime;
                    if (phaseTimer <= 0f)
                        StartCurrentGame();
                    break;

                case MatchPhase.RoundResults:
                    phaseTimer -= Time.deltaTime;
                    if (phaseTimer <= 0f)
                    {
                        if (currentRound.Value >= totalRounds)
                            EnterFinalResults();
                        else
                            AdvanceToNextRound();
                    }
                    break;
            }
        }

        // --- Round progression ---

        private void AdvanceToNextRound()
        {
            currentRound.Value++;

            switch (selectionMode)
            {
                case SelectionMode.Random:
                    SelectRandomGame();
                    break;
                case SelectionMode.Vote:
                    StartVote();
                    break;
                case SelectionMode.HostPick:
                    // Host picks via HostPickGame()
                    break;
            }
        }

        private void SelectRandomGame()
        {
            var manager = MiniGameManager.Instance;
            if (manager == null) return;

            var available = manager.GetAvailableGames(teamCount);
            if (available.Count == 0)
                available = manager.GetAllSupportedGames(teamCount);

            currentConfig = MiniGameSelector.SelectRandom(available, playedGames);
            if (currentConfig != null)
                BeginIntroPhase();
        }

        /// <summary>
        /// Host picks a specific game for the next round.
        /// </summary>
        public void HostPickGame(MiniGameConfig config)
        {
            if (!IsOwner) return;
            currentConfig = config;
            BeginIntroPhase();
        }

        private void BeginIntroPhase()
        {
            matchPhase.Value = MatchPhase.MiniGameIntro;
            phaseTimer = introDuration;
            BroadcastRoundStartedClientRpc(currentRound.Value);
        }

        [ClientRpc]
        private void BroadcastRoundStartedClientRpc(int round)
        {
            OnRoundStarted?.Invoke(round);
        }

        private void StartCurrentGame()
        {
            matchPhase.Value = MatchPhase.Playing;

            var manager = MiniGameManager.Instance;
            if (manager == null) return;

            manager.LoadMiniGame(currentConfig);
            playedGames.Add(currentConfig);

            var game = manager.CurrentMiniGame;
            if (game != null)
            {
                game.SetTeamCount(teamCount);
                game.OnGameEnded += HandleGameEnded;
                game.StartCountdown((int)countdownDuration);
            }
        }

        private void HandleGameEnded(MiniGameResults results)
        {
            // Unsubscribe
            var game = MiniGameManager.Instance?.CurrentMiniGame;
            if (game != null)
                game.OnGameEnded -= HandleGameEnded;

            AwardPartyPoints(results);
            MiniGameManager.Instance?.UnloadMiniGame();

            matchPhase.Value = MatchPhase.RoundResults;
            phaseTimer = resultsDuration;

            BroadcastRoundEndedClientRpc();
        }

        // --- Scoring ---

        private void AwardPartyPoints(MiniGameResults results)
        {
            bool isFinalRound = currentRound.Value >= totalRounds;
            float multiplier = isFinalRound ? ComebackMechanics.GetFinalRoundMultiplier() : 1f;

            foreach (var teamResult in results.Rankings)
            {
                int pointIndex = Mathf.Clamp(teamResult.Rank - 1, 0, placementPoints.Length - 1);
                int points = Mathf.RoundToInt(placementPoints[pointIndex] * multiplier);

                if (teamResult.TeamIndex >= 0 && teamResult.TeamIndex < partyScores.Count)
                    partyScores[teamResult.TeamIndex] += points;
            }

            NotifyScoresUpdatedClientRpc();
        }

        [ClientRpc]
        private void NotifyScoresUpdatedClientRpc()
        {
            var scores = new List<int>();
            for (int i = 0; i < partyScores.Count; i++)
                scores.Add(partyScores[i]);
            OnPartyScoresUpdated?.Invoke(scores);
        }

        [ClientRpc]
        private void BroadcastRoundEndedClientRpc()
        {
            // Clients rebuild results from synced scores
            OnRoundEnded?.Invoke(null);
        }

        // --- Final results ---

        private void EnterFinalResults()
        {
            matchPhase.Value = MatchPhase.FinalResults;
            ResolveTiebreakers();
            BroadcastMatchEndedClientRpc();
        }

        [ClientRpc]
        private void BroadcastMatchEndedClientRpc()
        {
            OnMatchEnded?.Invoke();
        }

        private void ResolveTiebreakers()
        {
            // Tiebreaker is informational — final scores are what matter.
            // UI layer handles displaying tiebreaker info.
            // Priority: most 1st places > head-to-head > sudden death (manual trigger).
        }

        // --- Voting ---

        private void StartVote()
        {
            var manager = MiniGameManager.Instance;
            if (manager == null) return;

            var available = manager.GetAvailableGames(teamCount);
            if (available.Count == 0)
                available = manager.GetAllSupportedGames(teamCount);

            voteOptions = MiniGameSelector.GetVoteOptions(available, 3);
            clientVotes.Clear();

            PresentVoteOptionsClientRpc(
                voteOptions.Select(v => v.GameName).ToArray());
        }

        [ClientRpc]
        private void PresentVoteOptionsClientRpc(string[] optionNames)
        {
            // Client-side: match names back to configs for UI display
            OnVoteOptionsPresented?.Invoke(voteOptions);
        }

        /// <summary>
        /// Submit a vote from a client. Called via ServerRpc.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SubmitVoteServerRpc(int optionIndex, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (optionIndex < 0 || optionIndex >= voteOptions.Count) return;

            clientVotes[clientId] = optionIndex;

            // Check if all clients have voted
            if (clientVotes.Count >= NetworkManager.Singleton.ConnectedClientsIds.Count)
                ResolveVote();
        }

        /// <summary>
        /// Force-resolve the vote. Host can call this to end voting early.
        /// </summary>
        public void ForceResolveVote()
        {
            if (!IsOwner) return;
            ResolveVote();
        }

        private void ResolveVote()
        {
            var voteCounts = new Dictionary<MiniGameConfig, int>();
            foreach (var option in voteOptions)
                voteCounts[option] = 0;

            foreach (var vote in clientVotes.Values)
            {
                if (vote >= 0 && vote < voteOptions.Count)
                    voteCounts[voteOptions[vote]]++;
            }

            currentConfig = MiniGameSelector.ResolveVote(voteCounts);
            if (currentConfig != null)
                BeginIntroPhase();
        }

        /// <summary>
        /// Returns the current party score for a team.
        /// </summary>
        public int GetPartyScore(int teamIndex)
        {
            if (teamIndex >= 0 && teamIndex < partyScores.Count)
                return partyScores[teamIndex];
            return 0;
        }

        /// <summary>
        /// Returns all party scores as a list.
        /// </summary>
        public List<int> GetAllPartyScores()
        {
            var scores = new List<int>();
            for (int i = 0; i < partyScores.Count; i++)
                scores.Add(partyScores[i]);
            return scores;
        }
    }
}
