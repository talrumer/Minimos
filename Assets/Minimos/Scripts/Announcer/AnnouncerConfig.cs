using System;
using UnityEngine;

namespace Minimos.Announcer
{
    /// <summary>
    /// Types of announcer events that trigger voice lines.
    /// </summary>
    public enum AnnouncerEvent
    {
        MatchStart,
        FlagGrabbed,
        FlagDropped,
        BigKnockback,
        ScoreMilestone,
        Comeback,
        CloseFinish,
        FinalSeconds,
        RoundWin,
        PartyWin
    }

    /// <summary>
    /// Associates an announcer event type with a pool of audio clips.
    /// </summary>
    [Serializable]
    public struct AnnouncerEventEntry
    {
        [Tooltip("The event type this entry is for.")]
        public AnnouncerEvent EventType;

        [Tooltip("Pool of clips to randomly choose from.")]
        public AudioClip[] Clips;
    }

    /// <summary>
    /// ScriptableObject holding all announcer voice line pools.
    /// Create via Assets > Create > Minimos > Announcer Config.
    /// </summary>
    [CreateAssetMenu(fileName = "AnnouncerConfig", menuName = "Minimos/Announcer Config")]
    public class AnnouncerConfig : ScriptableObject
    {
        #region Fields

        [SerializeField] private AnnouncerEventEntry[] entries;

        #endregion

        #region Public API

        /// <summary>
        /// Returns a random clip for the given event type, or null if none are configured.
        /// </summary>
        /// <param name="eventType">The announcer event to look up.</param>
        /// <returns>A random AudioClip from the pool, or null.</returns>
        public AudioClip GetRandomClip(AnnouncerEvent eventType)
        {
            if (entries == null) return null;

            foreach (var entry in entries)
            {
                if (entry.EventType == eventType && entry.Clips is { Length: > 0 })
                {
                    return entry.Clips[UnityEngine.Random.Range(0, entry.Clips.Length)];
                }
            }

            return null;
        }

        /// <summary>
        /// Returns a random clip for the given event, avoiding the specified clip
        /// to prevent back-to-back repeats.
        /// </summary>
        /// <param name="eventType">The announcer event to look up.</param>
        /// <param name="exclude">Clip to avoid selecting.</param>
        /// <returns>A random AudioClip that is not the excluded one, or null.</returns>
        public AudioClip GetRandomClipExcluding(AnnouncerEvent eventType, AudioClip exclude)
        {
            if (entries == null) return null;

            foreach (var entry in entries)
            {
                if (entry.EventType != eventType || entry.Clips == null || entry.Clips.Length == 0)
                    continue;

                // If only one clip, return it even if it matches exclude.
                if (entry.Clips.Length == 1) return entry.Clips[0];

                // Try up to 10 times to pick a non-excluded clip.
                for (int attempt = 0; attempt < 10; attempt++)
                {
                    AudioClip candidate = entry.Clips[UnityEngine.Random.Range(0, entry.Clips.Length)];
                    if (candidate != exclude) return candidate;
                }

                // Fallback: return any clip.
                return entry.Clips[0];
            }

            return null;
        }

        #endregion
    }
}
