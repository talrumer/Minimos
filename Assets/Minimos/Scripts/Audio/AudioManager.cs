using System.Collections;
using UnityEngine;

namespace Minimos.Audio
{
    /// <summary>
    /// Central audio manager handling music, SFX, and volume control with PlayerPrefs persistence.
    /// </summary>
    public class AudioManager : Core.Singleton<AudioManager>
    {
        #region Constants

        private const string PREF_MASTER_VOLUME = "Audio_MasterVolume";
        private const string PREF_MUSIC_VOLUME = "Audio_MusicVolume";
        private const string PREF_SFX_VOLUME = "Audio_SFXVolume";
        private const string PREF_ANNOUNCER_VOLUME = "Audio_AnnouncerVolume";
        private const int SFX_POOL_SIZE = 8;

        #endregion

        #region Fields

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSourceA;
        [SerializeField] private AudioSource musicSourceB;
        [SerializeField] private AudioSource announcerSource;

        private AudioSource[] sfxPool;
        private int sfxPoolIndex;

        private float masterVolume;
        private float musicVolume;
        private float sfxVolume;
        private float announcerVolume;

        private Coroutine musicFadeRoutine;
        private Coroutine crossfadeRoutine;
        private AudioSource activeMusicSource;

        #endregion

        #region Properties

        /// <summary>Master volume (0-1). Affects all audio.</summary>
        public float MasterVolume
        {
            get => masterVolume;
            set
            {
                masterVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(PREF_MASTER_VOLUME, masterVolume);
                ApplyVolumes();
            }
        }

        /// <summary>Music volume (0-1).</summary>
        public float MusicVolume
        {
            get => musicVolume;
            set
            {
                musicVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(PREF_MUSIC_VOLUME, musicVolume);
                ApplyVolumes();
            }
        }

        /// <summary>SFX volume (0-1).</summary>
        public float SFXVolume
        {
            get => sfxVolume;
            set
            {
                sfxVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(PREF_SFX_VOLUME, sfxVolume);
                ApplyVolumes();
            }
        }

        /// <summary>Announcer volume (0-1).</summary>
        public float AnnouncerVolume
        {
            get => announcerVolume;
            set
            {
                announcerVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(PREF_ANNOUNCER_VOLUME, announcerVolume);
                ApplyAnnouncerVolume();
            }
        }

        /// <summary>The currently playing music clip, or null.</summary>
        public AudioClip CurrentMusicClip => activeMusicSource != null ? activeMusicSource.clip : null;

        #endregion

        #region Unity Lifecycle

        protected override void OnSingletonAwake()
        {
            LoadVolumePrefs();
            InitializeSources();
        }

        #endregion

        #region Initialization

        private void LoadVolumePrefs()
        {
            masterVolume = PlayerPrefs.GetFloat(PREF_MASTER_VOLUME, 1f);
            musicVolume = PlayerPrefs.GetFloat(PREF_MUSIC_VOLUME, 0.7f);
            sfxVolume = PlayerPrefs.GetFloat(PREF_SFX_VOLUME, 1f);
            announcerVolume = PlayerPrefs.GetFloat(PREF_ANNOUNCER_VOLUME, 1f);
        }

        private void InitializeSources()
        {
            // Create music sources if not assigned.
            if (musicSourceA == null)
            {
                var goA = new GameObject("MusicSource_A");
                goA.transform.SetParent(transform);
                musicSourceA = goA.AddComponent<AudioSource>();
                musicSourceA.loop = true;
                musicSourceA.playOnAwake = false;
            }

            if (musicSourceB == null)
            {
                var goB = new GameObject("MusicSource_B");
                goB.transform.SetParent(transform);
                musicSourceB = goB.AddComponent<AudioSource>();
                musicSourceB.loop = true;
                musicSourceB.playOnAwake = false;
            }

            activeMusicSource = musicSourceA;

            // Create announcer source if not assigned.
            if (announcerSource == null)
            {
                var goAnnouncer = new GameObject("AnnouncerSource");
                goAnnouncer.transform.SetParent(transform);
                announcerSource = goAnnouncer.AddComponent<AudioSource>();
                announcerSource.playOnAwake = false;
            }

            // Create SFX pool.
            sfxPool = new AudioSource[SFX_POOL_SIZE];
            for (int i = 0; i < SFX_POOL_SIZE; i++)
            {
                var goSfx = new GameObject($"SFXSource_{i}");
                goSfx.transform.SetParent(transform);
                sfxPool[i] = goSfx.AddComponent<AudioSource>();
                sfxPool[i].playOnAwake = false;
            }

            ApplyVolumes();
            ApplyAnnouncerVolume();
        }

        #endregion

        #region Music

        /// <summary>
        /// Plays a music clip, optionally fading in over the given duration.
        /// </summary>
        /// <param name="clip">The music clip to play.</param>
        /// <param name="fadeTime">Fade-in duration in seconds. 0 for instant.</param>
        public void PlayMusic(AudioClip clip, float fadeTime = 1f)
        {
            if (clip == null) return;

            StopAllMusicCoroutines();

            activeMusicSource.clip = clip;
            activeMusicSource.Play();

            if (fadeTime > 0f)
            {
                activeMusicSource.volume = 0f;
                musicFadeRoutine = StartCoroutine(FadeAudioSource(activeMusicSource, 0f, musicVolume * masterVolume, fadeTime));
            }
            else
            {
                activeMusicSource.volume = musicVolume * masterVolume;
            }
        }

        /// <summary>
        /// Stops the currently playing music with an optional fade-out.
        /// </summary>
        /// <param name="fadeTime">Fade-out duration in seconds. 0 for instant.</param>
        public void StopMusic(float fadeTime = 1f)
        {
            StopAllMusicCoroutines();

            if (fadeTime > 0f && activeMusicSource.isPlaying)
            {
                musicFadeRoutine = StartCoroutine(FadeOutAndStop(activeMusicSource, fadeTime));
            }
            else
            {
                activeMusicSource.Stop();
                activeMusicSource.clip = null;
            }
        }

        /// <summary>
        /// Crossfades from the current music to a new clip over the given duration.
        /// </summary>
        /// <param name="newClip">The new music clip to crossfade to.</param>
        /// <param name="duration">Crossfade duration in seconds.</param>
        public void CrossfadeMusic(AudioClip newClip, float duration = 2f)
        {
            if (newClip == null) return;

            StopAllMusicCoroutines();

            AudioSource outgoing = activeMusicSource;
            AudioSource incoming = (activeMusicSource == musicSourceA) ? musicSourceB : musicSourceA;
            activeMusicSource = incoming;

            incoming.clip = newClip;
            incoming.volume = 0f;
            incoming.Play();

            crossfadeRoutine = StartCoroutine(CrossfadeRoutine(outgoing, incoming, duration));
        }

        #endregion

        #region SFX

        /// <summary>
        /// Plays a 2D sound effect.
        /// </summary>
        /// <param name="clip">The SFX clip to play.</param>
        /// <param name="volume">Volume multiplier (0-1). Combined with SFXVolume and MasterVolume.</param>
        public void PlaySFX(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;

            AudioSource source = GetNextSFXSource();
            source.spatialBlend = 0f;
            source.clip = clip;
            source.volume = volume * sfxVolume * masterVolume;
            source.Play();
        }

        /// <summary>
        /// Plays a 3D positional sound effect.
        /// </summary>
        /// <param name="clip">The SFX clip to play.</param>
        /// <param name="position">World position for the sound.</param>
        /// <param name="volume">Volume multiplier (0-1).</param>
        public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null) return;

            AudioSource source = GetNextSFXSource();
            source.transform.position = position;
            source.spatialBlend = 1f;
            source.clip = clip;
            source.volume = volume * sfxVolume * masterVolume;
            source.Play();
        }

        /// <summary>
        /// Plays an announcer voice line.
        /// </summary>
        /// <param name="clip">The announcer clip to play.</param>
        public void PlayAnnouncer(AudioClip clip)
        {
            if (clip == null) return;

            announcerSource.clip = clip;
            announcerSource.volume = announcerVolume * masterVolume;
            announcerSource.Play();
        }

        #endregion

        #region Volume

        private void ApplyVolumes()
        {
            float musicVol = musicVolume * masterVolume;
            if (musicSourceA != null && musicSourceA.isPlaying) musicSourceA.volume = musicVol;
            if (musicSourceB != null && musicSourceB.isPlaying) musicSourceB.volume = musicVol;

            // SFX pool volumes are set at play time, no need to update live.
        }

        private void ApplyAnnouncerVolume()
        {
            if (announcerSource != null)
            {
                announcerSource.volume = announcerVolume * masterVolume;
            }
        }

        #endregion

        #region Helpers

        private AudioSource GetNextSFXSource()
        {
            AudioSource source = sfxPool[sfxPoolIndex];
            sfxPoolIndex = (sfxPoolIndex + 1) % SFX_POOL_SIZE;
            return source;
        }

        private void StopAllMusicCoroutines()
        {
            if (musicFadeRoutine != null) { StopCoroutine(musicFadeRoutine); musicFadeRoutine = null; }
            if (crossfadeRoutine != null) { StopCoroutine(crossfadeRoutine); crossfadeRoutine = null; }
        }

        #endregion

        #region Coroutines

        private IEnumerator FadeAudioSource(AudioSource source, float from, float to, float duration)
        {
            float elapsed = 0f;
            source.volume = from;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            source.volume = to;
        }

        private IEnumerator FadeOutAndStop(AudioSource source, float duration)
        {
            float startVol = source.volume;

            yield return FadeAudioSource(source, startVol, 0f, duration);

            source.Stop();
            source.clip = null;
        }

        private IEnumerator CrossfadeRoutine(AudioSource outgoing, AudioSource incoming, float duration)
        {
            float elapsed = 0f;
            float outStartVol = outgoing.volume;
            float targetVol = musicVolume * masterVolume;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                outgoing.volume = Mathf.Lerp(outStartVol, 0f, t);
                incoming.volume = Mathf.Lerp(0f, targetVol, t);
                yield return null;
            }

            outgoing.Stop();
            outgoing.clip = null;
            outgoing.volume = 0f;
            incoming.volume = targetVol;
        }

        #endregion
    }
}
