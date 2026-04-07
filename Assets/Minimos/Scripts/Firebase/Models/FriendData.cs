using System;

namespace Minimos.Firebase.Models
{
    /// <summary>
    /// Friendship status between two players.
    /// </summary>
    public enum FriendStatus
    {
        Pending,
        Accepted,
        Blocked
    }

    /// <summary>
    /// Represents a friend relationship stored per-player in Firestore.
    /// </summary>
    [Serializable]
    public class FriendData
    {
        /// <summary>The other player's user ID.</summary>
        public string FriendUserId;

        /// <summary>Cached display name for UI (may be stale).</summary>
        public string DisplayName;

        /// <summary>Current relationship status.</summary>
        public FriendStatus Status;

        /// <summary>ISO 8601 timestamp of when the relationship was created.</summary>
        public string Since;

        public FriendData() { }

        public FriendData(string friendUserId, string displayName, FriendStatus status)
        {
            FriendUserId = friendUserId;
            DisplayName = displayName;
            Status = status;
            Since = DateTime.UtcNow.ToString("o");
        }
    }
}
