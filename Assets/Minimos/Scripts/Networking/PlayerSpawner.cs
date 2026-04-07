using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Minimos.Networking
{
    /// <summary>
    /// Handles spawning and respawning player characters at team-based spawn points.
    /// Attach to the same GameObject as NetworkManager (or a child).
    /// </summary>
    public class PlayerSpawner : NetworkBehaviour
    {
        #region Fields

        [Header("Spawn Configuration")]
        [SerializeField] private GameObject playerPrefab;

        [Tooltip("Outer array = team index, inner array = spawn points for that team.")]
        [SerializeField] private SpawnPointGroup[] teamSpawnPoints;

        [Header("Respawn Settings")]
        [SerializeField] private float respawnDelay = 2f;
        [SerializeField] private float invulnerabilityDuration = 2f;

        [Header("Team Colors")]
        [SerializeField] private Color[] teamColors = new Color[]
        {
            new(0.45f, 0.78f, 1f),   // Pastel blue
            new(1f, 0.55f, 0.55f),    // Pastel red
            new(0.55f, 1f, 0.6f),     // Pastel green
            new(1f, 0.85f, 0.45f),    // Pastel yellow
            new(0.8f, 0.55f, 1f),     // Pastel purple
            new(1f, 0.65f, 0.45f)     // Pastel orange
        };

        /// <summary>Tracks which spawn index to use next per team (round-robin).</summary>
        private int[] spawnIndices;

        #endregion

        #region Nested Types

        /// <summary>
        /// Groups spawn point transforms for a single team.
        /// Using a wrapper class so the inspector shows nested arrays cleanly.
        /// </summary>
        [System.Serializable]
        public class SpawnPointGroup
        {
            [Tooltip("Spawn transforms for this team.")]
            public Transform[] Points;
        }

        #endregion

        #region NetworkBehaviour Lifecycle

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            // Initialize round-robin indices.
            spawnIndices = new int[teamSpawnPoints.Length];

            // Subscribe to connection events for automatic spawning.
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            Debug.Log("[PlayerSpawner] Ready to spawn players (server).");
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer) return;

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            }
        }

        #endregion

        #region Spawning

        private void HandleClientConnected(ulong clientId)
        {
            if (!IsServer) return;

            // Determine team assignment from NetworkGameManager data.
            int teamIndex = 0;
            string displayName = $"Player_{clientId}";

            if (NetworkGameManager.HasInstance)
            {
                var players = NetworkGameManager.Instance.ConnectedPlayers;
                if (players.TryGetValue(clientId, out var data))
                {
                    teamIndex = Mathf.Max(0, data.TeamIndex);
                    displayName = data.DisplayName;
                }
            }

            SpawnPlayer(clientId, teamIndex, displayName);
        }

        /// <summary>
        /// Spawns a player at the next available team spawn point.
        /// Server only.
        /// </summary>
        /// <param name="clientId">The client to spawn for.</param>
        /// <param name="teamIndex">Team index for spawn point selection.</param>
        /// <param name="displayName">Player display name to apply.</param>
        public void SpawnPlayer(ulong clientId, int teamIndex, string displayName)
        {
            if (!IsServer)
            {
                Debug.LogWarning("[PlayerSpawner] SpawnPlayer called on client.");
                return;
            }

            if (playerPrefab == null)
            {
                Debug.LogError("[PlayerSpawner] Player prefab is not assigned!");
                return;
            }

            Vector3 position = GetNextSpawnPosition(teamIndex);
            Quaternion rotation = GetSpawnRotation(teamIndex);

            var playerObj = Instantiate(playerPrefab, position, rotation);
            var networkObj = playerObj.GetComponent<NetworkObject>();

            if (networkObj == null)
            {
                Debug.LogError("[PlayerSpawner] Player prefab is missing NetworkObject component!");
                Destroy(playerObj);
                return;
            }

            networkObj.SpawnAsPlayerObject(clientId);

            // Apply team visuals via ClientRpc.
            ApplyTeamDataClientRpc(networkObj.NetworkObjectId, teamIndex, displayName);

            Debug.Log($"[PlayerSpawner] Spawned {displayName} on team {teamIndex} at {position}");
        }

        /// <summary>
        /// Respawns a player after a knockout with a delay and temporary invulnerability.
        /// Server only.
        /// </summary>
        /// <param name="clientId">The client to respawn.</param>
        /// <param name="teamIndex">The player's team for spawn point selection.</param>
        public void RespawnPlayer(ulong clientId, int teamIndex)
        {
            if (!IsServer) return;
            StartCoroutine(RespawnCoroutine(clientId, teamIndex));
        }

        #endregion

        #region ClientRpc

        /// <summary>
        /// Applies team color and display name on all clients.
        /// </summary>
        [ClientRpc]
        private void ApplyTeamDataClientRpc(ulong networkObjectId, int teamIndex, string displayName)
        {
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var networkObj))
            {
                Debug.LogWarning($"[PlayerSpawner] NetworkObject {networkObjectId} not found for team data.");
                return;
            }

            var go = networkObj.gameObject;

            // Apply team color to all renderers.
            Color color = GetTeamColor(teamIndex);
            var renderers = go.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor("_BaseColor", color);
                renderer.SetPropertyBlock(block);
            }

            // Store display name if a name component exists.
            // (Game-specific component — left as a convention point.)
            go.name = $"Player_{displayName}_Team{teamIndex}";
        }

        /// <summary>
        /// Sets invulnerability state on a player across all clients.
        /// </summary>
        [ClientRpc]
        private void SetInvulnerableClientRpc(ulong networkObjectId, bool invulnerable)
        {
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var networkObj))
                return;

            // Toggle a visual indicator (e.g., blinking shader).
            var renderers = networkObj.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetFloat("_Invulnerable", invulnerable ? 1f : 0f);
                renderer.SetPropertyBlock(block);
            }
        }

        #endregion

        #region Helpers

        private Vector3 GetNextSpawnPosition(int teamIndex)
        {
            if (teamSpawnPoints == null || teamSpawnPoints.Length == 0)
            {
                Debug.LogWarning("[PlayerSpawner] No spawn points configured. Using origin.");
                return Vector3.zero;
            }

            int clampedTeam = Mathf.Clamp(teamIndex, 0, teamSpawnPoints.Length - 1);
            var group = teamSpawnPoints[clampedTeam];

            if (group.Points == null || group.Points.Length == 0)
            {
                Debug.LogWarning($"[PlayerSpawner] No spawn points for team {clampedTeam}. Using origin.");
                return Vector3.zero;
            }

            int idx = spawnIndices[clampedTeam] % group.Points.Length;
            spawnIndices[clampedTeam]++;

            return group.Points[idx] != null ? group.Points[idx].position : Vector3.zero;
        }

        private Quaternion GetSpawnRotation(int teamIndex)
        {
            if (teamSpawnPoints == null || teamSpawnPoints.Length == 0)
                return Quaternion.identity;

            int clampedTeam = Mathf.Clamp(teamIndex, 0, teamSpawnPoints.Length - 1);
            var group = teamSpawnPoints[clampedTeam];

            if (group.Points == null || group.Points.Length == 0)
                return Quaternion.identity;

            int idx = (spawnIndices[clampedTeam] - 1) % group.Points.Length;
            return group.Points[idx] != null ? group.Points[idx].rotation : Quaternion.identity;
        }

        private Color GetTeamColor(int teamIndex)
        {
            if (teamColors == null || teamColors.Length == 0)
                return Color.white;

            return teamColors[Mathf.Clamp(teamIndex, 0, teamColors.Length - 1)];
        }

        private IEnumerator RespawnCoroutine(ulong clientId, int teamIndex)
        {
            yield return new WaitForSeconds(respawnDelay);

            // Find the player's existing network object.
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                Debug.LogWarning($"[PlayerSpawner] Client {clientId} no longer connected for respawn.");
                yield break;
            }

            var playerObj = client.PlayerObject;
            if (playerObj == null)
            {
                Debug.LogWarning($"[PlayerSpawner] No player object for client {clientId}. Skipping respawn.");
                yield break;
            }

            // Teleport to new spawn point.
            Vector3 newPos = GetNextSpawnPosition(teamIndex);
            playerObj.transform.position = newPos;

            // Enable invulnerability.
            SetInvulnerableClientRpc(playerObj.NetworkObjectId, true);

            yield return new WaitForSeconds(invulnerabilityDuration);

            // Disable invulnerability (check still valid).
            if (playerObj != null && playerObj.IsSpawned)
            {
                SetInvulnerableClientRpc(playerObj.NetworkObjectId, false);
            }
        }

        #endregion
    }
}
