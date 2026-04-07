using UnityEngine;

namespace Minimos.Audio
{
    /// <summary>
    /// ScriptableObject containing categorized SFX clips.
    /// Create via Assets > Create > Minimos > Audio > SFX Library.
    /// </summary>
    [CreateAssetMenu(fileName = "SFXLibrary", menuName = "Minimos/Audio/SFX Library")]
    public class SFXLibrary : ScriptableObject
    {
        #region Fields

        [Header("Hit Sounds")]
        [SerializeField] private AudioClip[] hitSounds;

        [Header("Pickup Sounds")]
        [SerializeField] private AudioClip[] pickupSounds;

        [Header("UI Sounds")]
        [SerializeField] private AudioClip[] uiSounds;

        [Header("Movement Sounds")]
        [SerializeField] private AudioClip[] movementSounds;

        [Header("Celebration Sounds")]
        [SerializeField] private AudioClip[] celebrationSounds;

        [Header("Countdown Sounds")]
        [SerializeField] private AudioClip countdownTick;
        [SerializeField] private AudioClip countdownGo;

        #endregion

        #region Properties

        /// <summary>The countdown tick sound (3, 2, 1...).</summary>
        public AudioClip CountdownTick => countdownTick;

        /// <summary>The "Go!" sound after countdown.</summary>
        public AudioClip CountdownGo => countdownGo;

        #endregion

        #region Public Methods

        /// <summary>Returns a random hit sound, or null if none exist.</summary>
        public AudioClip GetRandomHitSound() => GetRandom(hitSounds);

        /// <summary>Returns a random pickup sound, or null if none exist.</summary>
        public AudioClip GetRandomPickupSound() => GetRandom(pickupSounds);

        /// <summary>Returns a random UI sound, or null if none exist.</summary>
        public AudioClip GetRandomUISound() => GetRandom(uiSounds);

        /// <summary>Returns a random movement sound, or null if none exist.</summary>
        public AudioClip GetRandomMovementSound() => GetRandom(movementSounds);

        /// <summary>Returns a random celebration sound, or null if none exist.</summary>
        public AudioClip GetRandomCelebrationSound() => GetRandom(celebrationSounds);

        #endregion

        #region Helpers

        private AudioClip GetRandom(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[Random.Range(0, clips.Length)];
        }

        #endregion
    }
}
