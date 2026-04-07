using System;

namespace Minimos.Firebase.Models
{
    /// <summary>
    /// A single entry on a seasonal leaderboard.
    /// </summary>
    [Serializable]
    public class LeaderboardEntry
    {
        /// <summary>Player's user ID.</summary>
        public string UserId;

        /// <summary>Cached display name for UI.</summary>
        public string DisplayName;

        /// <summary>Total ranking points earned this season.</summary>
        public int TotalPoints;

        /// <summary>Number of match wins this season.</summary>
        public int Wins;

        /// <summary>Total games played this season.</summary>
        public int GamesPlayed;

        public LeaderboardEntry() { }
    }
}
