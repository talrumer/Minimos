using System;
using UnityEngine;

namespace Minimos.Firebase.Models
{
    /// <summary>
    /// Persistent player profile data stored in Firestore.
    /// Tracks identity, progression, and lifetime stats.
    /// </summary>
    [Serializable]
    public class PlayerProfile
    {
        #region Fields

        public string UserId;
        public string DisplayName;
        public int Level;
        public int XP;
        public int Coins;
        public string CreatedAt;
        public string LastOnline;
        public int TotalGamesPlayed;
        public int TotalWins;

        #endregion

        #region Constructors

        public PlayerProfile() { }

        public PlayerProfile(string userId, string displayName)
        {
            UserId = userId;
            DisplayName = displayName;
            Level = 1;
            XP = 0;
            Coins = 0;
            CreatedAt = DateTime.UtcNow.ToString("o");
            LastOnline = DateTime.UtcNow.ToString("o");
            TotalGamesPlayed = 0;
            TotalWins = 0;
        }

        #endregion

        #region Progression

        /// <summary>
        /// Returns the total XP required to reach the next level from the given level.
        /// Uses a gentle curve: 100 * level * (1 + level * 0.1).
        /// </summary>
        /// <param name="currentLevel">The level to calculate XP requirement for.</param>
        /// <returns>XP needed to advance from currentLevel to currentLevel + 1.</returns>
        public static int XPForNextLevel(int currentLevel)
        {
            return Mathf.RoundToInt(100f * currentLevel * (1f + currentLevel * 0.1f));
        }

        /// <summary>
        /// Adds XP to this profile. Automatically handles level-ups when XP
        /// exceeds the threshold for the current level.
        /// </summary>
        /// <param name="amount">XP to add (must be positive).</param>
        /// <returns>Number of levels gained.</returns>
        public int AddXP(int amount)
        {
            if (amount <= 0) return 0;

            XP += amount;
            int levelsGained = 0;

            while (XP >= XPForNextLevel(Level))
            {
                XP -= XPForNextLevel(Level);
                Level++;
                levelsGained++;
            }

            return levelsGained;
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Serializes this profile to a JSON string.
        /// </summary>
        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        /// <summary>
        /// Deserializes a PlayerProfile from a JSON string.
        /// </summary>
        /// <param name="json">Valid JSON representing a PlayerProfile.</param>
        /// <returns>Deserialized profile, or null on failure.</returns>
        public static PlayerProfile FromJson(string json)
        {
            try
            {
                return JsonUtility.FromJson<PlayerProfile>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PlayerProfile] Failed to deserialize: {e.Message}");
                return null;
            }
        }

        #endregion
    }
}
