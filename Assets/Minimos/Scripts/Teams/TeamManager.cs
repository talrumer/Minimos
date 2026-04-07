using System;
using System.Collections.Generic;
using UnityEngine;

namespace Minimos.Teams
{
    /// <summary>
    /// Manages team definitions, player-to-team assignments, and team queries.
    /// </summary>
    public class TeamManager : Core.Singleton<TeamManager>
    {
        #region Fields

        [Header("Team Definitions")]
        [Tooltip("All available TeamData assets (up to 6). Order matches team index.")]
        [SerializeField] private TeamData[] teamDataAssets;

        /// <summary>Maps clientId to teamIndex for the current match.</summary>
        private readonly Dictionary<ulong, int> playerTeamAssignments = new();

        /// <summary>Number of teams active in the current match.</summary>
        private int activeTeamCount;

        #endregion

        #region Events

        /// <summary>Fired after teams have been assigned for a match.</summary>
        public event Action OnTeamsAssigned;

        /// <summary>Fired when a specific player's team changes. (clientId, newTeamIndex)</summary>
        public event Action<ulong, int> OnPlayerTeamChanged;

        #endregion

        #region Properties

        /// <summary>Number of teams active in the current match.</summary>
        public int ActiveTeamCount => activeTeamCount;

        /// <summary>Read-only view of current player-to-team assignments.</summary>
        public IReadOnlyDictionary<ulong, int> PlayerAssignments => playerTeamAssignments;

        #endregion

        #region Team Data Access

        /// <summary>
        /// Returns the TeamData for the given team index, or null if out of range.
        /// </summary>
        /// <param name="teamIndex">Zero-based team index.</param>
        public TeamData GetTeamData(int teamIndex)
        {
            if (teamDataAssets == null || teamIndex < 0 || teamIndex >= teamDataAssets.Length)
            {
                Debug.LogWarning($"[TeamManager] Invalid team index: {teamIndex}");
                return null;
            }
            return teamDataAssets[teamIndex];
        }

        /// <summary>
        /// Returns the primary color for the given team index.
        /// Falls back to TeamColors static lookup if no TeamData asset is assigned.
        /// </summary>
        /// <param name="teamIndex">Zero-based team index.</param>
        public Color GetTeamColor(int teamIndex)
        {
            var data = GetTeamData(teamIndex);
            return data != null ? data.TeamColor : TeamColors.GetPrimary(teamIndex);
        }

        /// <summary>
        /// Returns the accent color for the given team index.
        /// </summary>
        /// <param name="teamIndex">Zero-based team index.</param>
        public Color GetTeamAccentColor(int teamIndex)
        {
            var data = GetTeamData(teamIndex);
            return data != null ? data.AccentColor : TeamColors.GetAccent(teamIndex);
        }

        /// <summary>
        /// Returns the display name for the given team index.
        /// </summary>
        /// <param name="teamIndex">Zero-based team index.</param>
        public string GetTeamName(int teamIndex)
        {
            var data = GetTeamData(teamIndex);
            return data != null ? data.TeamName : TeamColors.TeamNames[Mathf.Clamp(teamIndex, 0, 5)];
        }

        #endregion

        #region Team Assignment

        /// <summary>
        /// Auto-assigns a list of player client IDs to teams using round-robin distribution.
        /// </summary>
        /// <param name="clientIds">List of player client IDs to assign.</param>
        /// <param name="teamCount">Number of teams to distribute across (2-6).</param>
        public void AssignTeams(List<ulong> clientIds, int teamCount)
        {
            teamCount = Mathf.Clamp(teamCount, 2, 6);
            activeTeamCount = teamCount;
            playerTeamAssignments.Clear();

            // Shuffle for fairness.
            var shuffled = new List<ulong>(clientIds);
            ShuffleList(shuffled);

            for (int i = 0; i < shuffled.Count; i++)
            {
                int teamIndex = i % teamCount;
                playerTeamAssignments[shuffled[i]] = teamIndex;
            }

            Debug.Log($"[TeamManager] Assigned {shuffled.Count} players to {teamCount} teams.");
            OnTeamsAssigned?.Invoke();
        }

        /// <summary>
        /// Overload: auto-assigns based on total player count and team size.
        /// Creates as many teams as needed.
        /// </summary>
        /// <param name="playerCount">Total number of players.</param>
        /// <param name="teamSize">Players per team.</param>
        public void AssignTeams(int playerCount, int teamSize)
        {
            teamSize = Mathf.Max(1, teamSize);
            int teamCount = Mathf.Clamp(Mathf.CeilToInt((float)playerCount / teamSize), 2, 6);

            // Generate placeholder client IDs (0 through playerCount-1).
            var ids = new List<ulong>();
            for (int i = 0; i < playerCount; i++)
            {
                ids.Add((ulong)i);
            }

            AssignTeams(ids, teamCount);
        }

        /// <summary>
        /// Sets a specific player's team. Fires OnPlayerTeamChanged.
        /// </summary>
        /// <param name="clientId">The player's network client ID.</param>
        /// <param name="teamIndex">The team to assign them to.</param>
        public void SetPlayerTeam(ulong clientId, int teamIndex)
        {
            playerTeamAssignments[clientId] = teamIndex;
            Debug.Log($"[TeamManager] Player {clientId} -> Team {teamIndex} ({GetTeamName(teamIndex)})");
            OnPlayerTeamChanged?.Invoke(clientId, teamIndex);
        }

        /// <summary>
        /// Gets the team index for a player, or -1 if not assigned.
        /// </summary>
        /// <param name="clientId">The player's network client ID.</param>
        public int GetPlayerTeam(ulong clientId)
        {
            return playerTeamAssignments.TryGetValue(clientId, out int team) ? team : -1;
        }

        /// <summary>
        /// Returns all client IDs on the given team.
        /// </summary>
        /// <param name="teamIndex">The team to query.</param>
        public List<ulong> GetPlayersOnTeam(int teamIndex)
        {
            var result = new List<ulong>();
            foreach (var kvp in playerTeamAssignments)
            {
                if (kvp.Value == teamIndex) result.Add(kvp.Key);
            }
            return result;
        }

        /// <summary>
        /// Clears all team assignments.
        /// </summary>
        public void ClearAssignments()
        {
            playerTeamAssignments.Clear();
            activeTeamCount = 0;
        }

        #endregion

        #region Helpers

        private void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        #endregion
    }
}
