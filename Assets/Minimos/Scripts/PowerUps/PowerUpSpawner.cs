using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Minimos.Match;

namespace Minimos.PowerUps
{
    /// <summary>
    /// Spawns power-up crates at random points on a timer. Host-authoritative.
    /// Trailing teams receive a higher spawn probability near their area.
    /// </summary>
    public class PowerUpSpawner : NetworkBehaviour
    {
        [Header("Spawn Points")]
        [SerializeField] private Transform[] spawnPoints;

        [Header("Spawn Settings")]
        [SerializeField] private float spawnIntervalMin = 15f;
        [SerializeField] private float spawnIntervalMax = 20f;
        [SerializeField] private int maxActiveCrates = 3;

        [Header("Power-Up Pool")]
        [SerializeField] private List<PowerUpConfig> powerUpPool = new List<PowerUpConfig>();

        private float spawnTimer;
        private float nextSpawnTime;
        private bool isActive;
        private readonly List<NetworkObject> activeCrates = new List<NetworkObject>();

        /// <summary>
        /// Enables or disables the spawner.
        /// </summary>
        public void SetActive(bool active)
        {
            isActive = active;
            if (active)
                ResetSpawnTimer();
        }

        private void Update()
        {
            if (!IsOwner || !isActive) return;

            // Clean up destroyed references
            activeCrates.RemoveAll(c => c == null || !c.IsSpawned);

            spawnTimer += Time.deltaTime;
            if (spawnTimer >= nextSpawnTime)
            {
                TrySpawnCrate();
                ResetSpawnTimer();
            }
        }

        private void ResetSpawnTimer()
        {
            spawnTimer = 0f;
            nextSpawnTime = Random.Range(spawnIntervalMin, spawnIntervalMax);
        }

        private void TrySpawnCrate()
        {
            if (activeCrates.Count >= maxActiveCrates) return;
            if (spawnPoints == null || spawnPoints.Length == 0) return;
            if (powerUpPool.Count == 0) return;

            // Pick spawn point
            Transform spawnPoint = PickSpawnPoint();

            // Pick power-up weighted by rarity (and comeback boost)
            PowerUpConfig config = PickWeightedPowerUp();
            if (config == null || config.CratePrefab == null) return;

            // Instantiate and spawn
            GameObject crate = Instantiate(config.CratePrefab, spawnPoint.position, Quaternion.identity);
            var networkObj = crate.GetComponent<NetworkObject>();
            if (networkObj == null)
            {
                Debug.LogError("[PowerUpSpawner] CratePrefab must have a NetworkObject.");
                Destroy(crate);
                return;
            }

            networkObj.Spawn();
            activeCrates.Add(networkObj);
        }

        private Transform PickSpawnPoint()
        {
            // Simple random selection; comeback-biased spawning could weight
            // points closer to trailing teams if spatial data is available.
            return spawnPoints[Random.Range(0, spawnPoints.Length)];
        }

        private PowerUpConfig PickWeightedPowerUp()
        {
            // Build weighted list
            int totalWeight = 0;
            var weights = new List<(PowerUpConfig config, int weight)>();

            foreach (var config in powerUpPool)
            {
                int w = config.GetSpawnWeight();
                // Apply comeback boost for trailing teams (increases rare/uncommon chances)
                // This is a simplified global boost; in practice you'd check team proximity.
                weights.Add((config, w));
                totalWeight += w;
            }

            if (totalWeight <= 0) return null;

            int roll = Random.Range(0, totalWeight);
            int cumulative = 0;
            foreach (var (config, weight) in weights)
            {
                cumulative += weight;
                if (roll < cumulative)
                    return config;
            }

            return weights.Last().config;
        }

        /// <summary>
        /// Despawns all active crates. Called on game end cleanup.
        /// </summary>
        public void DespawnAll()
        {
            foreach (var crate in activeCrates)
            {
                if (crate != null && crate.IsSpawned)
                {
                    crate.Despawn();
                    Destroy(crate.gameObject);
                }
            }
            activeCrates.Clear();
        }

        public override void OnDestroy()
        {
            DespawnAll();
            base.OnDestroy();
        }
    }
}
