using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Minimos.MiniGames
{
    /// <summary>
    /// Singleton manager responsible for loading, tracking, and unloading mini-games.
    /// Maintains the pool of available configs and tracks play history to avoid repeats.
    /// </summary>
    public class MiniGameManager : NetworkBehaviour
    {
        public static MiniGameManager Instance { get; private set; }

        [Header("Mini-Game Configs")]
        [SerializeField] private List<MiniGameConfig> allConfigs = new List<MiniGameConfig>();

        private MiniGameBase currentMiniGame;
        private readonly HashSet<MiniGameConfig> playedThisParty = new HashSet<MiniGameConfig>();

        // --- Events ---
        /// <summary>Fires after a mini-game prefab is instantiated and spawned.</summary>
        public event Action<MiniGameBase> OnMiniGameLoaded;

        /// <summary>Fires after the active mini-game is destroyed.</summary>
        public event Action OnMiniGameUnloaded;

        /// <summary>The currently active mini-game instance, or null.</summary>
        public MiniGameBase CurrentMiniGame => currentMiniGame;

        /// <summary>Read-only list of all registered configs.</summary>
        public IReadOnlyList<MiniGameConfig> AllConfigs => allConfigs;

        /// <summary>Set of configs already played this party session.</summary>
        public IReadOnlyCollection<MiniGameConfig> PlayedThisParty => playedThisParty;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public override void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            base.OnDestroy();
        }

        /// <summary>
        /// Returns configs that support the given team count and haven't been played yet.
        /// </summary>
        public List<MiniGameConfig> GetAvailableGames(int teamCount)
        {
            return allConfigs
                .Where(c => c.SupportsTeamCount(teamCount) && !playedThisParty.Contains(c))
                .ToList();
        }

        /// <summary>
        /// Returns all configs supporting the team count, including already-played ones.
        /// Used when the unplayed pool is exhausted.
        /// </summary>
        public List<MiniGameConfig> GetAllSupportedGames(int teamCount)
        {
            return allConfigs.Where(c => c.SupportsTeamCount(teamCount)).ToList();
        }

        /// <summary>
        /// Instantiates and network-spawns the mini-game prefab from the config. Host only.
        /// </summary>
        public void LoadMiniGame(MiniGameConfig config)
        {
            if (!IsOwner)
            {
                Debug.LogWarning("[MiniGameManager] Only the host can load mini-games.");
                return;
            }

            if (config.GameModePrefab == null)
            {
                Debug.LogError($"[MiniGameManager] Config '{config.GameName}' has no GameModePrefab assigned.");
                return;
            }

            // Clean up any existing game first
            if (currentMiniGame != null)
                UnloadMiniGame();

            GameObject instance = Instantiate(config.GameModePrefab);
            var networkObj = instance.GetComponent<NetworkObject>();
            if (networkObj == null)
            {
                Debug.LogError("[MiniGameManager] GameModePrefab must have a NetworkObject component.");
                Destroy(instance);
                return;
            }

            networkObj.Spawn();
            currentMiniGame = instance.GetComponent<MiniGameBase>();

            if (currentMiniGame == null)
            {
                Debug.LogError("[MiniGameManager] GameModePrefab must have a MiniGameBase component.");
                networkObj.Despawn();
                Destroy(instance);
                return;
            }

            playedThisParty.Add(config);
            OnMiniGameLoaded?.Invoke(currentMiniGame);
        }

        /// <summary>
        /// Despawns and destroys the current mini-game instance. Host only.
        /// </summary>
        public void UnloadMiniGame()
        {
            if (!IsOwner) return;
            if (currentMiniGame == null) return;

            var networkObj = currentMiniGame.GetComponent<NetworkObject>();
            if (networkObj != null && networkObj.IsSpawned)
                networkObj.Despawn();

            Destroy(currentMiniGame.gameObject);
            currentMiniGame = null;
            OnMiniGameUnloaded?.Invoke();
        }

        /// <summary>
        /// Resets the played history. Call at the start of a new party.
        /// </summary>
        public void ResetPlayedHistory()
        {
            playedThisParty.Clear();
        }
    }
}
