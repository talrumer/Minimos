using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Minimos.MiniGames.CTF;

namespace Minimos.MiniGames
{
    /// <summary>
    /// NetworkBehaviour for the King of the Hill capture zone. Tracks player
    /// occupancy per team, handles zone movement and shrinking, and provides
    /// visual feedback based on the dominant team.
    /// </summary>
    public class CaptureZone : NetworkBehaviour
    {
        [Header("Zone Visuals")]
        [SerializeField] private Renderer zoneRenderer;
        [SerializeField] private float baseShrinkRadius = 5f;
        [SerializeField] private float minRadius = 2f;

        [Header("Movement")]
        [SerializeField] private float moveLerpSpeed = 2f;

        // --- Network state ---
        private NetworkVariable<int> currentPositionIndex = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private NetworkVariable<float> zoneRadius = new NetworkVariable<float>(
            5f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private NetworkVariable<int> dominantTeam = new NetworkVariable<int>(
            -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        // --- Local tracking ---
        private Transform[] positions;
        private readonly Dictionary<int, HashSet<ulong>> teamPlayersInZone = new Dictionary<int, HashSet<ulong>>();
        private Vector3 targetPosition;
        private bool isMoving;

        // MVP tracking: player ID -> seconds in zone
        private readonly Dictionary<ulong, float> playerZoneTimes = new Dictionary<ulong, float>();
        private readonly HashSet<ulong> allPlayersInZone = new HashSet<ulong>();

        // --- Team colors (fallback, can be overridden) ---
        private static readonly Color NeutralColor = new Color(1f, 1f, 1f, 0.3f);

        /// <summary>
        /// Initializes the zone with available positions. Called by KOTHGameMode.
        /// </summary>
        public void Initialize(Transform[] zonePositions, int startIndex)
        {
            positions = zonePositions;
            currentPositionIndex.Value = startIndex;
            zoneRadius.Value = baseShrinkRadius;
            targetPosition = positions[startIndex].position;
            transform.position = targetPosition;
            UpdateScale();
        }

        private void Update()
        {
            if (isMoving)
            {
                transform.position = Vector3.Lerp(transform.position, targetPosition, moveLerpSpeed * Time.deltaTime);
                if (Vector3.Distance(transform.position, targetPosition) < 0.05f)
                {
                    transform.position = targetPosition;
                    isMoving = false;
                }
            }

            if (IsOwner)
            {
                UpdateDominantTeam();
                TrackPlayerTime();
            }

            UpdateScale();
        }

        // --- Zone radius ---

        /// <summary>
        /// Reduces the zone radius by the given amount, clamped to minimum.
        /// </summary>
        public void Shrink(float amount)
        {
            if (!IsOwner) return;
            zoneRadius.Value = Mathf.Max(minRadius, zoneRadius.Value - amount);
        }

        /// <summary>
        /// Resets the zone radius to base size. Called when zone moves.
        /// </summary>
        public void ResetRadius()
        {
            if (!IsOwner) return;
            zoneRadius.Value = baseShrinkRadius;
        }

        private void UpdateScale()
        {
            float diameter = zoneRadius.Value * 2f;
            transform.localScale = new Vector3(diameter, transform.localScale.y, diameter);
        }

        // --- Zone movement ---

        /// <summary>
        /// Begins moving the zone to a new position index. Host only.
        /// </summary>
        public void MoveToPosition(int posIndex)
        {
            if (!IsOwner) return;
            if (positions == null || posIndex < 0 || posIndex >= positions.Length) return;

            currentPositionIndex.Value = posIndex;
            targetPosition = positions[posIndex].position;
            isMoving = true;

            // Reset radius when zone moves
            ResetRadius();

            // Clear all tracked players — they need to re-enter
            foreach (var kvp in teamPlayersInZone)
                kvp.Value.Clear();
            allPlayersInZone.Clear();
        }

        // --- Player tracking ---

        /// <summary>
        /// Returns how many players from a team are currently inside the zone.
        /// </summary>
        public int GetTeamPlayerCount(int teamIndex)
        {
            if (teamPlayersInZone.TryGetValue(teamIndex, out var players))
                return players.Count;
            return 0;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsOwner) return;

            var teamMember = other.GetComponentInParent<ITeamMember>();
            var networkObj = other.GetComponentInParent<NetworkObject>();
            if (teamMember == null || networkObj == null) return;

            int team = teamMember.TeamIndex;
            ulong clientId = networkObj.OwnerClientId;

            if (!teamPlayersInZone.ContainsKey(team))
                teamPlayersInZone[team] = new HashSet<ulong>();

            teamPlayersInZone[team].Add(clientId);
            allPlayersInZone.Add(clientId);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsOwner) return;

            var teamMember = other.GetComponentInParent<ITeamMember>();
            var networkObj = other.GetComponentInParent<NetworkObject>();
            if (teamMember == null || networkObj == null) return;

            int team = teamMember.TeamIndex;
            ulong clientId = networkObj.OwnerClientId;

            if (teamPlayersInZone.TryGetValue(team, out var players))
                players.Remove(clientId);

            allPlayersInZone.Remove(clientId);
        }

        // --- Dominant team ---

        private void UpdateDominantTeam()
        {
            int bestTeam = -1;
            int bestCount = 0;
            bool tied = false;

            foreach (var kvp in teamPlayersInZone)
            {
                if (kvp.Value.Count > bestCount)
                {
                    bestCount = kvp.Value.Count;
                    bestTeam = kvp.Key;
                    tied = false;
                }
                else if (kvp.Value.Count == bestCount && kvp.Value.Count > 0)
                {
                    tied = true;
                }
            }

            dominantTeam.Value = tied ? -1 : bestTeam;
        }

        // --- Time tracking (MVP) ---

        private void TrackPlayerTime()
        {
            foreach (ulong clientId in allPlayersInZone)
            {
                if (!playerZoneTimes.ContainsKey(clientId))
                    playerZoneTimes[clientId] = 0f;
                playerZoneTimes[clientId] += Time.deltaTime;
            }
        }

        /// <summary>
        /// Returns the player who spent the most total time inside the zone.
        /// </summary>
        public (ulong clientId, float time) GetMostTimeInZone()
        {
            ulong bestId = ulong.MaxValue;
            float bestTime = 0f;

            foreach (var kvp in playerZoneTimes)
            {
                if (kvp.Value > bestTime)
                {
                    bestTime = kvp.Value;
                    bestId = kvp.Key;
                }
            }

            return (bestId, bestTime);
        }
    }
}
