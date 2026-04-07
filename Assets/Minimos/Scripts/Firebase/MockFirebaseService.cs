using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Minimos.Firebase.Models;
using UnityEngine;

namespace Minimos.Firebase
{
    /// <summary>
    /// Offline mock implementation of <see cref="IFirebaseService"/>.
    /// Stores everything in memory and PlayerPrefs so the game is fully
    /// playable during development without a Firebase project.
    /// </summary>
    public class MockFirebaseService : IFirebaseService
    {
        #region Constants

        private const string PREFS_USER_ID = "Mock_UserId";
        private const string PREFS_PROFILE = "Mock_Profile";
        private const string PREFS_INVENTORY = "Mock_Inventory";

        #endregion

        #region State

        private string currentUserId;
        private readonly Dictionary<string, PlayerProfile> profiles = new();
        private readonly Dictionary<string, List<InventoryItem>> inventories = new();
        private readonly Dictionary<string, List<FriendData>> friends = new();
        private readonly List<MatchHistoryData> matchHistory = new();
        private readonly Dictionary<string, List<LeaderboardEntry>> leaderboards = new();

        #endregion

        #region Constructor

        public MockFirebaseService()
        {
            // Restore persisted user ID if available.
            currentUserId = PlayerPrefs.GetString(PREFS_USER_ID, null);

            // Restore profile from PlayerPrefs.
            if (!string.IsNullOrEmpty(currentUserId))
            {
                string json = PlayerPrefs.GetString(PREFS_PROFILE, "");
                if (!string.IsNullOrEmpty(json))
                {
                    var profile = PlayerProfile.FromJson(json);
                    if (profile != null)
                    {
                        profiles[currentUserId] = profile;
                    }
                }

                string invJson = PlayerPrefs.GetString(PREFS_INVENTORY, "");
                if (!string.IsNullOrEmpty(invJson))
                {
                    var wrapper = JsonUtility.FromJson<InventoryWrapper>(invJson);
                    if (wrapper?.Items != null)
                    {
                        inventories[currentUserId] = wrapper.Items;
                    }
                }
            }

            Debug.Log("[MockFirebase] Initialized (offline mode).");
        }

        #endregion

        #region Authentication

        /// <inheritdoc />
        public Task<string> SignInAnonymously()
        {
            if (string.IsNullOrEmpty(currentUserId))
            {
                currentUserId = $"mock_{Guid.NewGuid():N}".Substring(0, 20);
                PlayerPrefs.SetString(PREFS_USER_ID, currentUserId);
                PlayerPrefs.Save();
            }

            Debug.Log($"[MockFirebase] Signed in anonymously: {currentUserId}");
            return Task.FromResult(currentUserId);
        }

        /// <inheritdoc />
        public Task<string> SignInWithGoogle()
        {
            // Mock: just reuse existing ID or create one.
            if (string.IsNullOrEmpty(currentUserId))
            {
                currentUserId = $"google_{Guid.NewGuid():N}".Substring(0, 20);
                PlayerPrefs.SetString(PREFS_USER_ID, currentUserId);
                PlayerPrefs.Save();
            }

            Debug.Log($"[MockFirebase] Signed in with Google (mock): {currentUserId}");
            return Task.FromResult(currentUserId);
        }

        /// <inheritdoc />
        public Task LinkAnonymousToGoogle()
        {
            Debug.Log("[MockFirebase] LinkAnonymousToGoogle (no-op in mock).");
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public string GetCurrentUserId() => currentUserId;

        /// <inheritdoc />
        public bool IsSignedIn() => !string.IsNullOrEmpty(currentUserId);

        #endregion

        #region Player Profile

        /// <inheritdoc />
        public Task<PlayerProfile> GetPlayerProfile(string userId)
        {
            profiles.TryGetValue(userId, out var profile);
            return Task.FromResult(profile);
        }

        /// <inheritdoc />
        public Task SavePlayerProfile(PlayerProfile profile)
        {
            profiles[profile.UserId] = profile;
            PersistProfile(profile);
            Debug.Log($"[MockFirebase] Saved profile for {profile.UserId}");
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task UpdatePlayerStats(string userId, int xpGain, int coinsGain, bool won)
        {
            if (!profiles.TryGetValue(userId, out var profile))
            {
                profile = new PlayerProfile(userId, $"Player_{userId[..6]}");
                profiles[userId] = profile;
            }

            profile.AddXP(xpGain);
            profile.Coins += coinsGain;
            profile.TotalGamesPlayed++;
            if (won) profile.TotalWins++;
            profile.LastOnline = DateTime.UtcNow.ToString("o");

            PersistProfile(profile);
            return Task.CompletedTask;
        }

        #endregion

        #region Inventory

        /// <inheritdoc />
        public Task<List<InventoryItem>> GetInventory(string userId)
        {
            inventories.TryGetValue(userId, out var items);
            return Task.FromResult(items ?? new List<InventoryItem>());
        }

        /// <inheritdoc />
        public Task SaveInventoryItem(string userId, InventoryItem item)
        {
            if (!inventories.ContainsKey(userId))
            {
                inventories[userId] = new List<InventoryItem>();
            }

            var list = inventories[userId];
            int idx = list.FindIndex(i => i.ItemId == item.ItemId);
            if (idx >= 0)
                list[idx] = item;
            else
                list.Add(item);

            PersistInventory(userId);
            return Task.CompletedTask;
        }

        #endregion

        #region Friends

        /// <inheritdoc />
        public Task<List<FriendData>> GetFriends(string userId)
        {
            friends.TryGetValue(userId, out var list);
            return Task.FromResult(list ?? new List<FriendData>());
        }

        /// <inheritdoc />
        public Task SendFriendRequest(string fromUserId, string toUserId)
        {
            if (!friends.ContainsKey(fromUserId))
                friends[fromUserId] = new List<FriendData>();
            if (!friends.ContainsKey(toUserId))
                friends[toUserId] = new List<FriendData>();

            friends[fromUserId].Add(new FriendData(toUserId, $"Player_{toUserId[..6]}", FriendStatus.Pending));
            friends[toUserId].Add(new FriendData(fromUserId, $"Player_{fromUserId[..6]}", FriendStatus.Pending));

            Debug.Log($"[MockFirebase] Friend request: {fromUserId} -> {toUserId}");
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task AcceptFriendRequest(string userId, string friendId)
        {
            SetFriendStatus(userId, friendId, FriendStatus.Accepted);
            SetFriendStatus(friendId, userId, FriendStatus.Accepted);
            Debug.Log($"[MockFirebase] Friend accepted: {userId} <-> {friendId}");
            return Task.CompletedTask;
        }

        #endregion

        #region Lobbies

        /// <inheritdoc />
        public Task<List<LobbyListingData>> GetActiveLobbies()
        {
            // Return empty list in mock — lobby browsing uses Unity Lobby service directly.
            return Task.FromResult(new List<LobbyListingData>());
        }

        #endregion

        #region Match History

        /// <inheritdoc />
        public Task SaveMatchHistory(MatchHistoryData match)
        {
            matchHistory.Add(match);
            Debug.Log($"[MockFirebase] Saved match history: {match.MatchId}");
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<List<MatchHistoryData>> GetMatchHistory(string userId, int limit)
        {
            var results = matchHistory
                .Where(m => m.PlayerIds.Contains(userId))
                .OrderByDescending(m => m.EndedAt)
                .Take(limit)
                .ToList();

            return Task.FromResult(results);
        }

        #endregion

        #region Leaderboard

        /// <inheritdoc />
        public Task<List<LeaderboardEntry>> GetLeaderboard(string season, int limit)
        {
            if (!leaderboards.TryGetValue(season, out var entries))
                return Task.FromResult(new List<LeaderboardEntry>());

            var top = entries.OrderByDescending(e => e.TotalPoints).Take(limit).ToList();
            return Task.FromResult(top);
        }

        /// <inheritdoc />
        public Task UpdateLeaderboard(string season, string userId, int points, int gamesPlayed)
        {
            if (!leaderboards.ContainsKey(season))
                leaderboards[season] = new List<LeaderboardEntry>();

            var list = leaderboards[season];
            var entry = list.Find(e => e.UserId == userId);

            if (entry == null)
            {
                entry = new LeaderboardEntry
                {
                    UserId = userId,
                    DisplayName = profiles.TryGetValue(userId, out var p) ? p.DisplayName : userId
                };
                list.Add(entry);
            }

            entry.TotalPoints += points;
            entry.GamesPlayed += gamesPlayed;
            if (points > 0) entry.Wins++;

            return Task.CompletedTask;
        }

        #endregion

        #region Helpers

        private void PersistProfile(PlayerProfile profile)
        {
            if (profile.UserId == currentUserId)
            {
                PlayerPrefs.SetString(PREFS_PROFILE, profile.ToJson());
                PlayerPrefs.Save();
            }
        }

        private void PersistInventory(string userId)
        {
            if (userId == currentUserId && inventories.TryGetValue(userId, out var items))
            {
                var wrapper = new InventoryWrapper { Items = items };
                PlayerPrefs.SetString(PREFS_INVENTORY, JsonUtility.ToJson(wrapper));
                PlayerPrefs.Save();
            }
        }

        private void SetFriendStatus(string userId, string friendId, FriendStatus status)
        {
            if (!friends.TryGetValue(userId, out var list)) return;
            var entry = list.Find(f => f.FriendUserId == friendId);
            if (entry != null) entry.Status = status;
        }

        /// <summary>
        /// Wrapper for serializing a list of inventory items with JsonUtility.
        /// </summary>
        [Serializable]
        private class InventoryWrapper
        {
            public List<InventoryItem> Items = new();
        }

        #endregion
    }
}
