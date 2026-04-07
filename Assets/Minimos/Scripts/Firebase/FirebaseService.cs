#if FIREBASE_AVAILABLE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using Minimos.Firebase.Models;
using UnityEngine;

namespace Minimos.Firebase
{
    /// <summary>
    /// Production implementation of <see cref="IFirebaseService"/> backed by
    /// Firebase Auth and Cloud Firestore.
    /// Only compiles when the FIREBASE_AVAILABLE scripting define is set.
    /// </summary>
    public class FirebaseService : IFirebaseService
    {
        #region Fields

        private FirebaseAuth auth;
        private FirebaseFirestore db;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes Firebase Auth and Firestore references.
        /// Call once after FirebaseApp.CheckAndFixDependenciesAsync succeeds.
        /// </summary>
        public void Initialize()
        {
            auth = FirebaseAuth.DefaultInstance;
            db = FirebaseFirestore.DefaultInstance;
            Debug.Log("[FirebaseService] Initialized with live Firebase backend.");
        }

        #endregion

        #region Authentication

        /// <inheritdoc />
        public async Task<string> SignInAnonymously()
        {
            try
            {
                var result = await auth.SignInAnonymouslyAsync();
                Debug.Log($"[FirebaseService] Signed in anonymously: {result.User.UserId}");
                return result.User.UserId;
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseService] SignInAnonymously failed: {e.Message}");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<string> SignInWithGoogle()
        {
            // Note: Actual Google sign-in requires platform-specific UI flow.
            // This assumes a Google ID token has been obtained via a native plugin
            // (e.g., Google Sign-In Unity plugin) and is passed through a helper.
            // For now, this is a placeholder that demonstrates the Firebase call.
            try
            {
                // In production, obtain idToken from Google Sign-In plugin.
                // var credential = GoogleAuthProvider.GetCredential(idToken, accessToken);
                // var result = await auth.SignInWithCredentialAsync(credential);
                // return result.User.UserId;

                Debug.LogWarning("[FirebaseService] SignInWithGoogle requires platform-specific Google Sign-In setup.");
                throw new NotImplementedException(
                    "Google Sign-In requires native plugin integration. " +
                    "Set up the Google Sign-In Unity plugin and pass credentials here.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseService] SignInWithGoogle failed: {e.Message}");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task LinkAnonymousToGoogle()
        {
            try
            {
                if (auth.CurrentUser == null || !auth.CurrentUser.IsAnonymous)
                {
                    Debug.LogWarning("[FirebaseService] LinkAnonymousToGoogle: no anonymous user.");
                    return;
                }

                // Same as above — requires Google credential from native plugin.
                // var credential = GoogleAuthProvider.GetCredential(idToken, accessToken);
                // await auth.CurrentUser.LinkWithCredentialAsync(credential);

                Debug.LogWarning("[FirebaseService] LinkAnonymousToGoogle requires Google credential.");
                await Task.CompletedTask;
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseService] LinkAnonymousToGoogle failed: {e.Message}");
                throw;
            }
        }

        /// <inheritdoc />
        public string GetCurrentUserId() => auth?.CurrentUser?.UserId;

        /// <inheritdoc />
        public bool IsSignedIn() => auth?.CurrentUser != null;

        #endregion

        #region Player Profile

        /// <inheritdoc />
        public async Task<PlayerProfile> GetPlayerProfile(string userId)
        {
            try
            {
                var doc = await db.Collection("players").Document(userId).GetSnapshotAsync();
                if (!doc.Exists) return null;

                return new PlayerProfile
                {
                    UserId = doc.GetValue<string>("userId"),
                    DisplayName = doc.GetValue<string>("displayName"),
                    Level = doc.GetValue<int>("level"),
                    XP = doc.GetValue<int>("xp"),
                    Coins = doc.GetValue<int>("coins"),
                    CreatedAt = doc.GetValue<string>("createdAt"),
                    LastOnline = doc.GetValue<string>("lastOnline"),
                    TotalGamesPlayed = doc.GetValue<int>("totalGamesPlayed"),
                    TotalWins = doc.GetValue<int>("totalWins")
                };
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseService] GetPlayerProfile failed: {e.Message}");
                return null;
            }
        }

        /// <inheritdoc />
        public async Task SavePlayerProfile(PlayerProfile profile)
        {
            try
            {
                var data = new Dictionary<string, object>
                {
                    ["userId"] = profile.UserId,
                    ["displayName"] = profile.DisplayName,
                    ["level"] = profile.Level,
                    ["xp"] = profile.XP,
                    ["coins"] = profile.Coins,
                    ["createdAt"] = profile.CreatedAt,
                    ["lastOnline"] = DateTime.UtcNow.ToString("o"),
                    ["totalGamesPlayed"] = profile.TotalGamesPlayed,
                    ["totalWins"] = profile.TotalWins
                };

                await db.Collection("players").Document(profile.UserId).SetAsync(data);
                Debug.Log($"[FirebaseService] Saved profile: {profile.UserId}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseService] SavePlayerProfile failed: {e.Message}");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task UpdatePlayerStats(string userId, int xpGain, int coinsGain, bool won)
        {
            try
            {
                var docRef = db.Collection("players").Document(userId);

                await db.RunTransactionAsync(async transaction =>
                {
                    var snapshot = await transaction.GetSnapshotAsync(docRef);
                    if (!snapshot.Exists) return;

                    int currentXp = snapshot.GetValue<int>("xp");
                    int currentLevel = snapshot.GetValue<int>("level");
                    int currentCoins = snapshot.GetValue<int>("coins");
                    int gamesPlayed = snapshot.GetValue<int>("totalGamesPlayed");
                    int wins = snapshot.GetValue<int>("totalWins");

                    // Calculate level-ups.
                    currentXp += xpGain;
                    while (currentXp >= PlayerProfile.XPForNextLevel(currentLevel))
                    {
                        currentXp -= PlayerProfile.XPForNextLevel(currentLevel);
                        currentLevel++;
                    }

                    var updates = new Dictionary<string, object>
                    {
                        ["xp"] = currentXp,
                        ["level"] = currentLevel,
                        ["coins"] = currentCoins + coinsGain,
                        ["totalGamesPlayed"] = gamesPlayed + 1,
                        ["totalWins"] = wins + (won ? 1 : 0),
                        ["lastOnline"] = DateTime.UtcNow.ToString("o")
                    };

                    transaction.Update(docRef, updates);
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseService] UpdatePlayerStats failed: {e.Message}");
                throw;
            }
        }

        #endregion

        #region Inventory

        /// <inheritdoc />
        public async Task<List<InventoryItem>> GetInventory(string userId)
        {
            try
            {
                var snapshot = await db.Collection("players").Document(userId)
                    .Collection("inventory").GetSnapshotAsync();

                return snapshot.Documents.Select(doc => new InventoryItem
                {
                    ItemId = doc.GetValue<string>("itemId"),
                    Type = Enum.Parse<ItemType>(doc.GetValue<string>("type")),
                    ItemName = doc.GetValue<string>("itemName"),
                    IsEquipped = doc.GetValue<bool>("isEquipped"),
                    AcquiredAt = doc.GetValue<string>("acquiredAt")
                }).ToList();
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseService] GetInventory failed: {e.Message}");
                return new List<InventoryItem>();
            }
        }

        /// <inheritdoc />
        public async Task SaveInventoryItem(string userId, InventoryItem item)
        {
            try
            {
                var data = new Dictionary<string, object>
                {
                    ["itemId"] = item.ItemId,
                    ["type"] = item.Type.ToString(),
                    ["itemName"] = item.ItemName,
                    ["isEquipped"] = item.IsEquipped,
                    ["acquiredAt"] = item.AcquiredAt
                };

                await db.Collection("players").Document(userId)
                    .Collection("inventory").Document(item.ItemId).SetAsync(data);
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseService] SaveInventoryItem failed: {e.Message}");
                throw;
            }
        }

        #endregion

        #region Friends

        /// <inheritdoc />
        public async Task<List<FriendData>> GetFriends(string userId)
        {
            try
            {
                var snapshot = await db.Collection("players").Document(userId)
                    .Collection("friends").GetSnapshotAsync();

                return snapshot.Documents.Select(doc => new FriendData
                {
                    FriendUserId = doc.GetValue<string>("friendUserId"),
                    DisplayName = doc.GetValue<string>("displayName"),
                    Status = Enum.Parse<FriendStatus>(doc.GetValue<string>("status")),
                    Since = doc.GetValue<string>("since")
                }).ToList();
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseService] GetFriends failed: {e.Message}");
                return new List<FriendData>();
            }
        }

        /// <inheritdoc />
        public async Task SendFriendRequest(string fromUserId, string toUserId)
        {
            try
            {
                string now = DateTime.UtcNow.ToString("o");

                // Get display names for both sides.
                var fromProfile = await GetPlayerProfile(fromUserId);
                var toProfile = await GetPlayerProfile(toUserId);

                var batch = db.StartBatch();

                // Sender's record.
                batch.Set(
                    db.Collection("players").Document(fromUserId)
                        .Collection("friends").Document(toUserId),
                    new Dictionary<string, object>
                    {
                        ["friendUserId"] = toUserId,
                        ["displayName"] = toProfile?.DisplayName ?? toUserId,
                        ["status"] = FriendStatus.Pending.ToString(),
                        ["since"] = now
                    });

                // Recipient's record.
                batch.Set(
                    db.Collection("players").Document(toUserId)
                        .Collection("friends").Document(fromUserId),
                    new Dictionary<string, object>
                    {
                        ["friendUserId"] = fromUserId,
                        ["displayName"] = fromProfile?.DisplayName ?? fromUserId,
                        ["status"] = FriendStatus.Pending.ToString(),
                        ["since"] = now
                    });

                await batch.CommitAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseService] SendFriendRequest failed: {e.Message}");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task AcceptFriendRequest(string userId, string friendId)
        {
            try
            {
                var batch = db.StartBatch();

                batch.Update(
                    db.Collection("players").Document(userId)
                        .Collection("friends").Document(friendId),
                    new Dictionary<string, object> { ["status"] = FriendStatus.Accepted.ToString() });

                batch.Update(
                    db.Collection("players").Document(friendId)
                        .Collection("friends").Document(userId),
                    new Dictionary<string, object> { ["status"] = FriendStatus.Accepted.ToString() });

                await batch.CommitAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseService] AcceptFriendRequest failed: {e.Message}");
                throw;
            }
        }

        #endregion

        #region Lobbies

        /// <inheritdoc />
        public async Task<List<LobbyListingData>> GetActiveLobbies()
        {
            try
            {
                var snapshot = await db.Collection("lobbies")
                    .WhereEqualTo("status", "waiting")
                    .OrderByDescending("currentPlayers")
                    .Limit(50)
                    .GetSnapshotAsync();

                return snapshot.Documents.Select(doc => new LobbyListingData
                {
                    LobbyId = doc.GetValue<string>("lobbyId"),
                    HostName = doc.GetValue<string>("hostName"),
                    RoomCode = doc.GetValue<string>("roomCode"),
                    CurrentPlayers = doc.GetValue<int>("currentPlayers"),
                    MaxPlayers = doc.GetValue<int>("maxPlayers"),
                    Region = doc.GetValue<string>("region"),
                    GameMode = doc.GetValue<string>("gameMode"),
                    Status = doc.GetValue<string>("status")
                }).ToList();
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseService] GetActiveLobbies failed: {e.Message}");
                return new List<LobbyListingData>();
            }
        }

        #endregion

        #region Match History

        /// <inheritdoc />
        public async Task SaveMatchHistory(MatchHistoryData match)
        {
            try
            {
                var data = new Dictionary<string, object>
                {
                    ["matchId"] = match.MatchId,
                    ["startedAt"] = match.StartedAt,
                    ["endedAt"] = match.EndedAt,
                    ["teamCount"] = match.TeamCount,
                    ["finalRankings"] = match.FinalRankings,
                    ["playerIds"] = match.PlayerIds,
                    ["rounds"] = match.Rounds.Select(r => new Dictionary<string, object>
                    {
                        ["miniGameName"] = r.MiniGameName,
                        ["mvpStat"] = r.MvpStat ?? "",
                        ["rankings"] = r.Rankings.Select(tp => new Dictionary<string, object>
                        {
                            ["teamIndex"] = tp.TeamIndex,
                            ["points"] = tp.Points
                        }).ToList()
                    }).ToList()
                };

                await db.Collection("matches").Document(match.MatchId).SetAsync(data);
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseService] SaveMatchHistory failed: {e.Message}");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<List<MatchHistoryData>> GetMatchHistory(string userId, int limit)
        {
            try
            {
                var snapshot = await db.Collection("matches")
                    .WhereArrayContains("playerIds", userId)
                    .OrderByDescending("endedAt")
                    .Limit(limit)
                    .GetSnapshotAsync();

                var results = new List<MatchHistoryData>();

                foreach (var doc in snapshot.Documents)
                {
                    var match = new MatchHistoryData
                    {
                        MatchId = doc.GetValue<string>("matchId"),
                        StartedAt = doc.GetValue<string>("startedAt"),
                        EndedAt = doc.GetValue<string>("endedAt"),
                        TeamCount = doc.GetValue<int>("teamCount"),
                        FinalRankings = doc.GetValue<List<object>>("finalRankings")
                            .Select(o => Convert.ToInt32(o)).ToList(),
                        PlayerIds = doc.GetValue<List<object>>("playerIds")
                            .Select(o => o.ToString()).ToList()
                    };

                    results.Add(match);
                }

                return results;
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseService] GetMatchHistory failed: {e.Message}");
                return new List<MatchHistoryData>();
            }
        }

        #endregion

        #region Leaderboard

        /// <inheritdoc />
        public async Task<List<LeaderboardEntry>> GetLeaderboard(string season, int limit)
        {
            try
            {
                var snapshot = await db.Collection("leaderboards").Document(season)
                    .Collection("entries")
                    .OrderByDescending("totalPoints")
                    .Limit(limit)
                    .GetSnapshotAsync();

                return snapshot.Documents.Select(doc => new LeaderboardEntry
                {
                    UserId = doc.GetValue<string>("userId"),
                    DisplayName = doc.GetValue<string>("displayName"),
                    TotalPoints = doc.GetValue<int>("totalPoints"),
                    Wins = doc.GetValue<int>("wins"),
                    GamesPlayed = doc.GetValue<int>("gamesPlayed")
                }).ToList();
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseService] GetLeaderboard failed: {e.Message}");
                return new List<LeaderboardEntry>();
            }
        }

        /// <inheritdoc />
        public async Task UpdateLeaderboard(string season, string userId, int points, int gamesPlayed)
        {
            try
            {
                var docRef = db.Collection("leaderboards").Document(season)
                    .Collection("entries").Document(userId);

                await db.RunTransactionAsync(async transaction =>
                {
                    var snapshot = await transaction.GetSnapshotAsync(docRef);

                    if (snapshot.Exists)
                    {
                        transaction.Update(docRef, new Dictionary<string, object>
                        {
                            ["totalPoints"] = snapshot.GetValue<int>("totalPoints") + points,
                            ["gamesPlayed"] = snapshot.GetValue<int>("gamesPlayed") + gamesPlayed,
                            ["wins"] = snapshot.GetValue<int>("wins") + (points > 0 ? 1 : 0)
                        });
                    }
                    else
                    {
                        var profile = await GetPlayerProfile(userId);
                        transaction.Set(docRef, new Dictionary<string, object>
                        {
                            ["userId"] = userId,
                            ["displayName"] = profile?.DisplayName ?? userId,
                            ["totalPoints"] = points,
                            ["gamesPlayed"] = gamesPlayed,
                            ["wins"] = points > 0 ? 1 : 0
                        });
                    }
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseService] UpdateLeaderboard failed: {e.Message}");
                throw;
            }
        }

        #endregion
    }
}
#endif
