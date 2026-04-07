using System.Collections.Generic;
using System.Threading.Tasks;
using Minimos.Firebase.Models;

namespace Minimos.Firebase
{
    /// <summary>
    /// Abstraction over all Firebase backend operations.
    /// Swap between <see cref="MockFirebaseService"/> (offline dev) and
    /// <see cref="FirebaseService"/> (production) via <see cref="FirebaseManager"/>.
    /// </summary>
    public interface IFirebaseService
    {
        #region Authentication

        /// <summary>
        /// Signs in anonymously. Creates a new anonymous account if none exists.
        /// </summary>
        /// <returns>The user ID of the signed-in user.</returns>
        Task<string> SignInAnonymously();

        /// <summary>
        /// Signs in with Google credentials via Firebase Auth.
        /// </summary>
        /// <returns>The user ID of the signed-in user.</returns>
        Task<string> SignInWithGoogle();

        /// <summary>
        /// Links the current anonymous account to a Google account so progress is preserved.
        /// Must be called while signed in anonymously.
        /// </summary>
        Task LinkAnonymousToGoogle();

        /// <summary>
        /// Returns the current authenticated user's ID, or null if not signed in.
        /// </summary>
        string GetCurrentUserId();

        /// <summary>
        /// Whether a user is currently authenticated.
        /// </summary>
        bool IsSignedIn();

        #endregion

        #region Player Profile

        /// <summary>
        /// Fetches a player profile from the backend.
        /// </summary>
        /// <param name="userId">Target user ID.</param>
        /// <returns>The player profile, or null if not found.</returns>
        Task<PlayerProfile> GetPlayerProfile(string userId);

        /// <summary>
        /// Saves or overwrites a player profile on the backend.
        /// </summary>
        /// <param name="profile">The profile to persist.</param>
        Task SavePlayerProfile(PlayerProfile profile);

        /// <summary>
        /// Atomically updates a player's stats after a match.
        /// </summary>
        /// <param name="userId">Target user ID.</param>
        /// <param name="xpGain">XP earned this match.</param>
        /// <param name="coinsGain">Coins earned this match.</param>
        /// <param name="won">Whether the player's team won.</param>
        Task UpdatePlayerStats(string userId, int xpGain, int coinsGain, bool won);

        #endregion

        #region Inventory

        /// <summary>
        /// Retrieves all inventory items for a player.
        /// </summary>
        /// <param name="userId">Target user ID.</param>
        Task<List<InventoryItem>> GetInventory(string userId);

        /// <summary>
        /// Saves or updates a single inventory item for a player.
        /// </summary>
        /// <param name="userId">Target user ID.</param>
        /// <param name="item">The item to persist.</param>
        Task SaveInventoryItem(string userId, InventoryItem item);

        #endregion

        #region Friends

        /// <summary>
        /// Retrieves the friend list for a player.
        /// </summary>
        /// <param name="userId">Target user ID.</param>
        Task<List<FriendData>> GetFriends(string userId);

        /// <summary>
        /// Sends a friend request from one player to another.
        /// </summary>
        /// <param name="fromUserId">Sender user ID.</param>
        /// <param name="toUserId">Recipient user ID.</param>
        Task SendFriendRequest(string fromUserId, string toUserId);

        /// <summary>
        /// Accepts a pending friend request, upgrading both sides to Accepted.
        /// </summary>
        /// <param name="userId">The accepting user's ID.</param>
        /// <param name="friendId">The requester's user ID.</param>
        Task AcceptFriendRequest(string userId, string friendId);

        #endregion

        #region Lobbies

        /// <summary>
        /// Fetches a list of publicly visible lobbies from Firestore.
        /// </summary>
        Task<List<LobbyListingData>> GetActiveLobbies();

        #endregion

        #region Match History

        /// <summary>
        /// Persists a completed match record.
        /// </summary>
        /// <param name="match">The match data to store.</param>
        Task SaveMatchHistory(MatchHistoryData match);

        /// <summary>
        /// Retrieves recent match history for a player.
        /// </summary>
        /// <param name="userId">Target user ID.</param>
        /// <param name="limit">Maximum number of records to return.</param>
        Task<List<MatchHistoryData>> GetMatchHistory(string userId, int limit);

        #endregion

        #region Leaderboard

        /// <summary>
        /// Fetches the top leaderboard entries for a given season.
        /// </summary>
        /// <param name="season">Season identifier (e.g., "S1_2026").</param>
        /// <param name="limit">Maximum entries to return.</param>
        Task<List<LeaderboardEntry>> GetLeaderboard(string season, int limit);

        /// <summary>
        /// Updates or creates a player's leaderboard entry for a season.
        /// </summary>
        /// <param name="season">Season identifier.</param>
        /// <param name="userId">Target user ID.</param>
        /// <param name="points">Points to add.</param>
        /// <param name="gamesPlayed">Games played increment (typically 1).</param>
        Task UpdateLeaderboard(string season, string userId, int points, int gamesPlayed);

        #endregion
    }
}
