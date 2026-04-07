using UnityEngine;

namespace Minimos.Announcer
{
    /// <summary>
    /// Singleton announcer system that plays context-sensitive voice lines
    /// with cooldowns and anti-repeat logic. Uses a dedicated AudioSource
    /// at the Announcer volume level.
    /// </summary>
    public class AnnouncerManager : Core.Singleton<AnnouncerManager>
    {
        #region Fields

        [Header("Config")]
        [SerializeField] private AnnouncerConfig config;

        [Header("Audio")]
        [SerializeField] private AudioSource announcerSource;

        [Header("Cooldowns")]
        [Tooltip("Minimum time between any two announcements.")]
        [SerializeField] private float globalCooldown = 0.5f;

        private float lastPlayTime = -10f;
        private AudioClip lastPlayedClip;

        #endregion

        #region Unity Lifecycle

        protected override void OnSingletonAwake()
        {
            if (announcerSource == null)
            {
                var go = new GameObject("AnnouncerSource_Dedicated");
                go.transform.SetParent(transform);
                announcerSource = go.AddComponent<AudioSource>();
                announcerSource.playOnAwake = false;
                announcerSource.spatialBlend = 0f;
            }

            ApplyVolume();
        }

        private void OnEnable()
        {
            SubscribeToGameEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromGameEvents();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Plays an announcer voice line for the given event type.
        /// Respects cooldown and won't repeat the same clip back-to-back.
        /// </summary>
        /// <param name="eventType">The announcer event to trigger.</param>
        public void Play(AnnouncerEvent eventType)
        {
            if (config == null)
            {
                Debug.LogWarning("[AnnouncerManager] No AnnouncerConfig assigned.");
                return;
            }

            // Global cooldown check.
            if (Time.unscaledTime - lastPlayTime < globalCooldown) return;

            // Don't overlap current announcement.
            if (announcerSource != null && announcerSource.isPlaying) return;

            // Pick a clip avoiding back-to-back repeats.
            AudioClip clip = config.GetRandomClipExcluding(eventType, lastPlayedClip);
            if (clip == null) return;

            PlayClip(clip);
        }

        /// <summary>
        /// Refreshes the announcer volume from AudioManager.
        /// </summary>
        public void ApplyVolume()
        {
            if (announcerSource == null) return;

            if (Audio.AudioManager.HasInstance)
            {
                announcerSource.volume = Audio.AudioManager.Instance.AnnouncerVolume *
                                         Audio.AudioManager.Instance.MasterVolume;
            }
        }

        #endregion

        #region Private Methods

        private void PlayClip(AudioClip clip)
        {
            if (announcerSource == null || clip == null) return;

            ApplyVolume();
            announcerSource.clip = clip;
            announcerSource.Play();

            lastPlayedClip = clip;
            lastPlayTime = Time.unscaledTime;
        }

        #endregion

        #region Game Event Subscriptions

        private void SubscribeToGameEvents()
        {
            // Subscribe to GameManager events.
            if (Core.GameManager.HasInstance)
            {
                Core.GameManager.Instance.OnPartyStarted += OnPartyStarted;
                Core.GameManager.Instance.OnRoundStarted += OnRoundStarted;
            }

            // Subscribe to game state changes for final seconds, round win, etc.
            Core.GameStateEvents.OnGameStateChanged += OnGameStateChanged;
        }

        private void UnsubscribeFromGameEvents()
        {
            if (Core.GameManager.HasInstance)
            {
                Core.GameManager.Instance.OnPartyStarted -= OnPartyStarted;
                Core.GameManager.Instance.OnRoundStarted -= OnRoundStarted;
            }

            Core.GameStateEvents.OnGameStateChanged -= OnGameStateChanged;
        }

        private void OnPartyStarted()
        {
            Play(AnnouncerEvent.MatchStart);
        }

        private void OnRoundStarted(int roundNumber)
        {
            Play(AnnouncerEvent.MatchStart);
        }

        private void OnGameStateChanged(Core.GameStateChangedEventArgs args)
        {
            switch (args.NewState)
            {
                case Core.GameState.RoundResults:
                    Play(AnnouncerEvent.RoundWin);
                    break;
                case Core.GameState.FinalResults:
                    Play(AnnouncerEvent.PartyWin);
                    break;
            }
        }

        #endregion

        #region External Trigger Helpers

        /// <summary>
        /// Call from game logic when a flag is picked up.
        /// </summary>
        public void OnFlagPickedUp() => Play(AnnouncerEvent.FlagGrabbed);

        /// <summary>
        /// Call from game logic when a flag is dropped.
        /// </summary>
        public void OnFlagDropped() => Play(AnnouncerEvent.FlagDropped);

        /// <summary>
        /// Call when a big knockback hit occurs.
        /// </summary>
        public void OnBigKnockback() => Play(AnnouncerEvent.BigKnockback);

        /// <summary>
        /// Call when a team hits a score milestone.
        /// </summary>
        public void OnScoreMilestone() => Play(AnnouncerEvent.ScoreMilestone);

        /// <summary>
        /// Call when a trailing team overtakes the leader.
        /// </summary>
        public void OnComeback() => Play(AnnouncerEvent.Comeback);

        /// <summary>
        /// Call when the timer is in the final seconds.
        /// </summary>
        public void OnFinalSeconds() => Play(AnnouncerEvent.FinalSeconds);

        /// <summary>
        /// Call when the final score difference is very close.
        /// </summary>
        public void OnCloseFinish() => Play(AnnouncerEvent.CloseFinish);

        #endregion
    }
}
