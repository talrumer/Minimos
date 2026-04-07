using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Minimos.PowerUps;

namespace Minimos.MiniGames.CTF
{
    /// <summary>
    /// Capture the Flags game mode. Teams compete to hold flags — each flag held
    /// awards +1 point per second. First to ScoreToWin (default 100) or highest
    /// score at time limit wins.
    /// </summary>
    public class CTFGameMode : MiniGameBase
    {
        [Header("CTF Settings")]
        [SerializeField] private int flagCount = 2;
        [SerializeField] private Transform[] flagSpawnPoints;
        [SerializeField] private GameObject flagPrefab;

        [Header("Power-Up Spawning")]
        [SerializeField] private PowerUpSpawner powerUpSpawner;

        private readonly List<Flag> activeFlags = new List<Flag>();
        private float scoreTickTimer;

        private const float ScoreTickInterval = 1f;
        private const float FlagIdleRespawnTime = 10f;

        protected override void OnGameSetup()
        {
            SpawnFlags();
        }

        protected override void OnGameStart()
        {
            scoreTickTimer = 0f;

            if (powerUpSpawner != null && config.PowerUpsEnabled)
                powerUpSpawner.SetActive(true);
        }

        protected override void OnGameUpdate()
        {
            UpdateScoring();
            UpdateFlagRespawns();
        }

        protected override void OnGameEnd()
        {
            if (powerUpSpawner != null)
                powerUpSpawner.SetActive(false);
        }

        protected override MiniGameResults GetResults()
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

            // MVP: player who held flags the longest (tracked by Flag component)
            ulong mvpId = ulong.MaxValue;
            float longestHold = 0f;
            foreach (var flag in activeFlags)
            {
                var (carrierId, holdTime) = flag.GetLongestCarrier();
                if (holdTime > longestHold)
                {
                    longestHold = holdTime;
                    mvpId = carrierId;
                }
            }

            results.MvpPlayerId = mvpId;
            results.MvpStatDescription = longestHold > 0f
                ? $"Held flags for {longestHold:F1}s"
                : string.Empty;

            return results;
        }

        // --- Flag spawning ---

        private void SpawnFlags()
        {
            if (flagPrefab == null || flagSpawnPoints == null || flagSpawnPoints.Length == 0)
            {
                Debug.LogError("[CTFGameMode] Missing flagPrefab or flagSpawnPoints.");
                return;
            }

            int count = Mathf.Min(flagCount, flagSpawnPoints.Length);
            var shuffled = ShuffleSpawnPoints();

            for (int i = 0; i < count; i++)
            {
                GameObject flagObj = Instantiate(flagPrefab, shuffled[i].position, Quaternion.identity);
                var networkObj = flagObj.GetComponent<NetworkObject>();
                networkObj.Spawn();

                var flag = flagObj.GetComponent<Flag>();
                flag.Initialize(flagSpawnPoints);
                activeFlags.Add(flag);
            }
        }

        // --- Scoring ---

        private void UpdateScoring()
        {
            scoreTickTimer += Time.deltaTime;
            if (scoreTickTimer < ScoreTickInterval) return;
            scoreTickTimer -= ScoreTickInterval;

            // Award +1 point per flag held per team
            for (int i = 0; i < teamScores.Count; i++)
            {
                int flagsHeld = 0;
                foreach (var flag in activeFlags)
                {
                    if (flag.IsCarried && flag.CarrierTeamIndex == i)
                        flagsHeld++;
                }

                if (flagsHeld > 0)
                    OnPlayerScored(i, flagsHeld);
            }
        }

        // --- Flag respawns ---

        private void UpdateFlagRespawns()
        {
            foreach (var flag in activeFlags)
            {
                if (flag.IsAvailable && flag.IdleTime >= FlagIdleRespawnTime)
                {
                    var point = GetRandomSpawnPoint();
                    flag.Respawn(point.position);
                }
            }
        }

        // --- Helpers ---

        private Transform[] ShuffleSpawnPoints()
        {
            var list = flagSpawnPoints.ToList();
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            return list.ToArray();
        }

        private Transform GetRandomSpawnPoint()
        {
            return flagSpawnPoints[Random.Range(0, flagSpawnPoints.Length)];
        }

        public override void OnDestroy()
        {
            foreach (var flag in activeFlags)
            {
                if (flag != null)
                {
                    var nobj = flag.GetComponent<NetworkObject>();
                    if (nobj != null && nobj.IsSpawned)
                        nobj.Despawn();
                    Destroy(flag.gameObject);
                }
            }
            activeFlags.Clear();
            base.OnDestroy();
        }
    }
}
