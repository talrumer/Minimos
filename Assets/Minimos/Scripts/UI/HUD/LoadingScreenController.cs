using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Minimos.UI
{
    /// <summary>
    /// Controls the between-round loading screen: mini-game rules preview,
    /// current standings, idle animation, rotating tips, and progress bar.
    /// </summary>
    public class LoadingScreenController : MonoBehaviour
    {
        #region Fields

        [Header("Loading Message")]
        [SerializeField] private TMP_Text loadingMessageText;

        [Header("Mini-Game Rules Preview")]
        [SerializeField] private TMP_Text miniGameNameText;
        [SerializeField] private TMP_Text[] ruleBulletTexts;
        [SerializeField] private TMP_Text keyControlsText;

        [Header("Standings Scoreboard")]
        [SerializeField] private Transform standingsParent;
        [SerializeField] private GameObject standingsEntryPrefab;

        [Header("Idle Animation")]
        [SerializeField] private Animator minimoIdleAnimator;
        [SerializeField] private string[] idleAnimationTriggers;

        [Header("Tips")]
        [SerializeField] private TMP_Text tipText;
        [SerializeField] private float tipRotateInterval = 4f;
        [SerializeField] private string[] tips =
        {
            "Dodge rolling makes you invulnerable for a brief moment!",
            "Charged melee attacks deal more knockback.",
            "Teamwork wins games. Stick with your partner!",
            "Power-ups spawn at fixed locations on the map.",
            "Double-jump to reach higher platforms.",
            "Hold slide while running to cover distance quickly."
        };

        [Header("Progress Bar")]
        [SerializeField] private Slider progressBar;

        private Coroutine tipRoutine;
        private int currentTipIndex;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            // Play a random idle animation.
            if (minimoIdleAnimator != null && idleAnimationTriggers is { Length: > 0 })
            {
                string trigger = idleAnimationTriggers[Random.Range(0, idleAnimationTriggers.Length)];
                minimoIdleAnimator.SetTrigger(trigger);
            }

            // Start rotating tips.
            if (tips is { Length: > 0 })
            {
                currentTipIndex = Random.Range(0, tips.Length);
                ShowCurrentTip();
                tipRoutine = StartCoroutine(RotateTipsRoutine());
            }

            if (progressBar != null) progressBar.value = 0f;
        }

        private void OnDisable()
        {
            if (tipRoutine != null)
            {
                StopCoroutine(tipRoutine);
                tipRoutine = null;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Configures the loading screen with mini-game preview, rules, tip, and standings.
        /// </summary>
        /// <param name="miniGameName">Name of the next mini-game.</param>
        /// <param name="rules">Up to 3 rule bullet points.</param>
        /// <param name="tip">A single "Did You Know?" tip override (or null for random).</param>
        /// <param name="standings">Team scores indexed by team. Null to hide standings.</param>
        public void Setup(string miniGameName, string[] rules, string tip, int[] standings)
        {
            // Mini-game name.
            if (miniGameNameText != null)
                miniGameNameText.text = miniGameName;

            // Rule bullets.
            if (ruleBulletTexts != null)
            {
                for (int i = 0; i < ruleBulletTexts.Length; i++)
                {
                    if (rules != null && i < rules.Length)
                    {
                        ruleBulletTexts[i].gameObject.SetActive(true);
                        ruleBulletTexts[i].text = $"- {rules[i]}";
                    }
                    else
                    {
                        ruleBulletTexts[i].gameObject.SetActive(false);
                    }
                }
            }

            // Tip override.
            if (!string.IsNullOrEmpty(tip) && tipText != null)
                tipText.text = $"Did You Know? {tip}";

            // Standings scoreboard.
            PopulateStandings(standings);
        }

        /// <summary>
        /// Sets the loading message text (e.g., "Loading Arena...").
        /// </summary>
        /// <param name="message">Message to display.</param>
        public void SetMessage(string message)
        {
            if (loadingMessageText != null) loadingMessageText.text = message;
        }

        /// <summary>
        /// Sets the key controls hint text.
        /// </summary>
        /// <param name="controls">Controls description string.</param>
        public void SetKeyControls(string controls)
        {
            if (keyControlsText != null) keyControlsText.text = controls;
        }

        /// <summary>
        /// Updates the progress bar value.
        /// </summary>
        /// <param name="progress">Progress from 0 to 1.</param>
        public void SetProgress(float progress)
        {
            if (progressBar != null) progressBar.value = Mathf.Clamp01(progress);
        }

        #endregion

        #region Private Methods

        private void PopulateStandings(int[] standings)
        {
            // Clear existing entries.
            if (standingsParent != null)
            {
                foreach (Transform child in standingsParent)
                    Destroy(child.gameObject);
            }

            if (standings == null || standingsEntryPrefab == null || standingsParent == null) return;

            for (int i = 0; i < standings.Length; i++)
            {
                GameObject entry = Instantiate(standingsEntryPrefab, standingsParent);
                var texts = entry.GetComponentsInChildren<TMP_Text>();

                string teamName = Teams.TeamManager.HasInstance
                    ? Teams.TeamManager.Instance.GetTeamName(i)
                    : $"Team {i + 1}";

                if (texts.Length >= 2)
                {
                    texts[0].text = teamName;
                    texts[1].text = standings[i].ToString();
                }

                // Color by team.
                var bg = entry.GetComponent<Image>();
                if (bg != null && Teams.TeamManager.HasInstance)
                {
                    Color c = Teams.TeamManager.Instance.GetTeamColor(i);
                    c.a = 0.3f;
                    bg.color = c;
                }
            }
        }

        private void ShowCurrentTip()
        {
            if (tipText != null && tips.Length > 0)
                tipText.text = $"Did You Know? {tips[currentTipIndex]}";
        }

        private IEnumerator RotateTipsRoutine()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(tipRotateInterval);
                currentTipIndex = (currentTipIndex + 1) % tips.Length;
                ShowCurrentTip();
            }
        }

        #endregion
    }
}
