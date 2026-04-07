using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Minimos.MiniGames;

namespace Minimos.UI
{
    /// <summary>
    /// Controls the round results screen: team rankings with sequential animation,
    /// point changes, MVP callout, fun stat, auto-advance, and next game preview.
    /// </summary>
    public class ResultsScreenController : MonoBehaviour
    {
        #region Fields

        [Header("Rankings")]
        [SerializeField] private Transform rankingsParent;
        [SerializeField] private GameObject rankEntryPrefab;

        [Header("MVP")]
        [SerializeField] private GameObject mvpPanel;
        [SerializeField] private Image mvpStarIcon;
        [SerializeField] private TMP_Text mvpNameText;
        [SerializeField] private TMP_Text mvpStatText;

        [Header("Fun Stat")]
        [SerializeField] private TMP_Text funStatText;

        [Header("Point Changes")]
        [SerializeField] private TMP_Text[] pointChangeTexts;

        [Header("Next Game Preview")]
        [SerializeField] private GameObject nextGamePanel;
        [SerializeField] private TMP_Text nextGameNameText;
        [SerializeField] private TMP_Text nextGameRuleText;

        [Header("Auto-Advance")]
        [SerializeField] private float autoAdvanceTime = 8f;
        [SerializeField] private Slider autoAdvanceSlider;

        [Header("Animation")]
        [SerializeField] private float entrySlideOffset = -800f;
        [SerializeField] private float entrySlideDuration = 0.35f;
        [SerializeField] private float entryStagger = 0.2f;

        private readonly List<GameObject> spawnedEntries = new();
        private Coroutine animationRoutine;
        private Coroutine autoAdvanceRoutine;

        private static readonly string[] funStats =
        {
            "Most time spent in the air: {0}",
            "Longest survival streak: {0}",
            "Most dodge rolls: {0}",
            "Closest near-miss: {0}",
            "Most slides performed: {0}",
            "Farthest distance traveled: {0}"
        };

        #endregion

        #region Public API

        /// <summary>
        /// Populates and animates the results screen with team rankings.
        /// </summary>
        /// <param name="results">Sorted list of team results (rank 1 first).</param>
        public void AnimateResults(List<TeamResult> results)
        {
            ClearEntries();

            if (animationRoutine != null) StopCoroutine(animationRoutine);
            animationRoutine = StartCoroutine(AnimateResultsRoutine(results));

            if (autoAdvanceRoutine != null) StopCoroutine(autoAdvanceRoutine);
            autoAdvanceRoutine = StartCoroutine(AutoAdvanceRoutine());
        }

        /// <summary>
        /// Sets the MVP callout.
        /// </summary>
        /// <param name="playerName">MVP player name.</param>
        /// <param name="statDescription">The stat that earned them MVP.</param>
        public void SetMVP(string playerName, string statDescription)
        {
            if (mvpPanel != null) mvpPanel.SetActive(true);
            if (mvpNameText != null) mvpNameText.text = playerName;
            if (mvpStatText != null) mvpStatText.text = statDescription;
        }

        /// <summary>
        /// Sets a random fun/silly stat for the round.
        /// </summary>
        /// <param name="playerName">Player name to feature.</param>
        public void SetFunStat(string playerName)
        {
            if (funStatText == null) return;

            string template = funStats[Random.Range(0, funStats.Length)];
            funStatText.text = string.Format(template, playerName);
        }

        /// <summary>
        /// Shows the next game preview panel.
        /// </summary>
        /// <param name="gameName">Name of the next mini-game.</param>
        /// <param name="oneLineRule">A single-line rule description.</param>
        public void SetNextGamePreview(string gameName, string oneLineRule)
        {
            if (nextGamePanel != null) nextGamePanel.SetActive(true);
            if (nextGameNameText != null) nextGameNameText.text = $"Next: {gameName}";
            if (nextGameRuleText != null) nextGameRuleText.text = oneLineRule;
        }

        #endregion

        #region Private Methods

        private void ClearEntries()
        {
            foreach (var entry in spawnedEntries)
            {
                if (entry != null) Destroy(entry);
            }
            spawnedEntries.Clear();

            if (mvpPanel != null) mvpPanel.SetActive(false);
            if (nextGamePanel != null) nextGamePanel.SetActive(false);
        }

        private IEnumerator AnimateResultsRoutine(List<TeamResult> results)
        {
            // Spawn and animate each rank entry sequentially.
            for (int i = 0; i < results.Count; i++)
            {
                TeamResult result = results[i];

                if (rankEntryPrefab == null || rankingsParent == null) continue;

                GameObject entry = Instantiate(rankEntryPrefab, rankingsParent);
                spawnedEntries.Add(entry);

                // Configure entry text (expects children named "RankText", "TeamText", "ScoreText").
                var texts = entry.GetComponentsInChildren<TMP_Text>();
                if (texts.Length >= 3)
                {
                    texts[0].text = $"#{result.Rank}";
                    texts[1].text = GetTeamName(result.TeamIndex);
                    texts[2].text = result.Score.ToString();
                }

                // Color the entry by team.
                var bg = entry.GetComponent<Image>();
                if (bg != null)
                {
                    Color teamColor = GetTeamColor(result.TeamIndex);
                    teamColor.a = 0.4f;
                    bg.color = teamColor;
                }

                // Animate slide-in from left.
                RectTransform rect = entry.GetComponent<RectTransform>();
                if (rect != null)
                    StartCoroutine(UIAnimations.SlideIn(rect, new Vector2(entrySlideOffset, 0f), entrySlideDuration));

                // Show point change.
                if (pointChangeTexts != null && i < pointChangeTexts.Length && pointChangeTexts[i] != null)
                {
                    int points = GetPointsForRank(result.Rank, results.Count);
                    pointChangeTexts[i].text = $"+{points}";
                    pointChangeTexts[i].color = new Color(1f, 0.85f, 0.2f);
                }

                yield return new WaitForSeconds(entryStagger);
            }

            animationRoutine = null;
        }

        private IEnumerator AutoAdvanceRoutine()
        {
            float elapsed = 0f;

            while (elapsed < autoAdvanceTime)
            {
                elapsed += Time.deltaTime;
                if (autoAdvanceSlider != null)
                    autoAdvanceSlider.value = elapsed / autoAdvanceTime;
                yield return null;
            }

            // Auto-advance to next round.
            Debug.Log("[Results] Auto-advancing to next round.");
            Core.GameManager.Instance?.NextRound();

            autoAdvanceRoutine = null;
        }

        private int GetPointsForRank(int rank, int totalTeams)
        {
            // Simple descending points: 1st=10, 2nd=7, 3rd=5, 4th=3, 5th=2, 6th=1.
            return rank switch
            {
                1 => 10,
                2 => 7,
                3 => 5,
                4 => 3,
                5 => 2,
                _ => 1
            };
        }

        private Color GetTeamColor(int teamIndex)
        {
            if (Teams.TeamManager.HasInstance)
                return Teams.TeamManager.Instance.GetTeamColor(teamIndex);
            return Color.white;
        }

        private string GetTeamName(int teamIndex)
        {
            if (Teams.TeamManager.HasInstance)
                return Teams.TeamManager.Instance.GetTeamName(teamIndex);
            return $"Team {teamIndex + 1}";
        }

        #endregion
    }
}
