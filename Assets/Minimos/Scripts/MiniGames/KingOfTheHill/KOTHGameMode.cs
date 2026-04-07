using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Minimos.MiniGames
{
    /// <summary>
    /// King of the Hill game mode. Teams compete for control of a capture zone
    /// that moves every 45 seconds. Points are awarded per second based on
    /// team presence inside the zone, with a teamwork bonus.
    /// </summary>
    public class KOTHGameMode : MiniGameBase
    {
        [Header("KOTH Settings")]
        [SerializeField] private Transform[] zonePositions;
        [SerializeField] private GameObject captureZonePrefab;

        [Header("Scoring")]
        [SerializeField] private int pointsPerPlayerPerSecond = 2;
        [SerializeField] private int fullTeamBonusPerSecond = 5;
        [SerializeField] private int playersPerTeam = 2;

        [Header("Zone Movement")]
        [SerializeField] private float zoneMoveInterval = 45f;
        [SerializeField] private float zoneShrinkRate = 0.005f;

        private CaptureZone captureZone;
        private float scoreTicker;
        private float zoneTimer;
        private int currentZoneIndex;

        private const float ScoreTickInterval = 1f;

        protected override void OnGameSetup()
        {
            if (captureZonePrefab == null || zonePositions == null || zonePositions.Length == 0)
            {
                Debug.LogError("[KOTHGameMode] Missing captureZonePrefab or zonePositions.");
                return;
            }

            // Spawn zone at first position
            currentZoneIndex = Random.Range(0, zonePositions.Length);
            var pos = zonePositions[currentZoneIndex].position;

            GameObject zoneObj = Instantiate(captureZonePrefab, pos, Quaternion.identity);
            var networkObj = zoneObj.GetComponent<NetworkObject>();
            networkObj.Spawn();

            captureZone = zoneObj.GetComponent<CaptureZone>();
            captureZone.Initialize(zonePositions, currentZoneIndex);
        }

        protected override void OnGameStart()
        {
            scoreTicker = 0f;
            zoneTimer = 0f;
        }

        protected override void OnGameUpdate()
        {
            UpdateZoneMovement();
            UpdateZoneShrink();
            UpdateScoring();
        }

        protected override void OnGameEnd()
        {
            // Cleanup handled by OnDestroy
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

            // MVP: player who spent most time in the zone
            if (captureZone != null)
            {
                var (mvpId, time) = captureZone.GetMostTimeInZone();
                results.MvpPlayerId = mvpId;
                results.MvpStatDescription = time > 0f ? $"Zone time: {time:F1}s" : string.Empty;
            }

            return results;
        }

        // --- Zone movement ---

        private void UpdateZoneMovement()
        {
            zoneTimer += Time.deltaTime;
            if (zoneTimer < zoneMoveInterval) return;
            zoneTimer -= zoneMoveInterval;

            // Pick next position (avoid repeating)
            int nextIndex;
            do
            {
                nextIndex = Random.Range(0, zonePositions.Length);
            } while (nextIndex == currentZoneIndex && zonePositions.Length > 1);

            currentZoneIndex = nextIndex;

            if (captureZone != null)
                captureZone.MoveToPosition(currentZoneIndex);
        }

        private void UpdateZoneShrink()
        {
            if (captureZone != null)
                captureZone.Shrink(zoneShrinkRate * Time.deltaTime);
        }

        // --- Scoring ---

        private void UpdateScoring()
        {
            scoreTicker += Time.deltaTime;
            if (scoreTicker < ScoreTickInterval) return;
            scoreTicker -= ScoreTickInterval;

            if (captureZone == null) return;

            for (int i = 0; i < teamScores.Count; i++)
            {
                int playersInZone = captureZone.GetTeamPlayerCount(i);
                if (playersInZone <= 0) continue;

                int points = playersInZone * pointsPerPlayerPerSecond;

                // Full team bonus
                if (playersInZone >= playersPerTeam)
                    points += fullTeamBonusPerSecond;

                OnPlayerScored(i, points);
            }
        }

        public override void OnDestroy()
        {
            if (captureZone != null)
            {
                var nobj = captureZone.GetComponent<NetworkObject>();
                if (nobj != null && nobj.IsSpawned)
                    nobj.Despawn();
                Destroy(captureZone.gameObject);
            }
            base.OnDestroy();
        }
    }
}
