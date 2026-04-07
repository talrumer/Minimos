using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Minimos.Core;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace Minimos.Networking
{
    /// <summary>
    /// Data about a connected player in the current session.
    /// </summary>
    [Serializable]
    public struct PlayerNetworkData
    {
        /// <summary>Netcode client ID.</summary>
        public ulong ClientId;

        /// <summary>Player display name.</summary>
        public string DisplayName;

        /// <summary>Assigned team index (0-based).</summary>
        public int TeamIndex;

        /// <summary>Whether this player has readied up.</summary>
        public bool IsReady;
    }

    /// <summary>
    /// Singleton that manages multiplayer sessions using Unity's Multiplayer Services SDK (Sessions API).
    /// Replaces the old Relay + Lobby separate packages with a unified approach.
    /// Handles session creation, joining, leaving, querying, and player tracking.
    /// </summary>
    public class NetworkGameManager : Singleton<NetworkGameManager>
    {
        #region Constants

        /// <summary>Maximum players allowed in a single session.</summary>
        public const int MaxPlayers = 12;

        private const string PlayerNamePropertyKey = "playerName";
        private const string TeamIndexPropertyKey = "teamIndex";
        private const string ReadyPropertyKey = "isReady";

        #endregion

        #region Fields

        [Header("References")]
        [SerializeField] private NetworkManager networkManager;

        private ISession activeSession;
        private readonly Dictionary<ulong, PlayerNetworkData> connectedPlayers = new();
        private bool servicesInitialized;

        #endregion

        #region Properties

        /// <summary>The active multiplayer session, or null.</summary>
        public ISession ActiveSession => activeSession;

        /// <summary>Read-only view of all connected players keyed by client ID.</summary>
        public IReadOnlyDictionary<ulong, PlayerNetworkData> ConnectedPlayers => connectedPlayers;

        /// <summary>The session join code, or null if not in a session.</summary>
        public string JoinCode => activeSession?.Code;

        /// <summary>Whether this instance is the session host.</summary>
        public bool IsHost => activeSession?.IsHost ?? false;

        /// <summary>Whether we are connected to a session.</summary>
        public bool IsConnected => networkManager != null && networkManager.IsConnectedClient;

        /// <summary>Whether we are in an active session.</summary>
        public bool IsInSession => activeSession != null;

        #endregion

        #region Events

        /// <summary>Fired when a session is created or joined.</summary>
        public event Action<ISession> OnSessionStarted;

        /// <summary>Fired when we leave a session.</summary>
        public event Action OnSessionLeft;

        /// <summary>Fired when a new player connects. Passes their client ID.</summary>
        public event Action<ulong> OnPlayerConnected;

        /// <summary>Fired when a player disconnects. Passes their client ID.</summary>
        public event Action<ulong> OnPlayerDisconnected;

        /// <summary>Fired when all connected players have set IsReady to true.</summary>
        public event Action OnAllPlayersReady;

        /// <summary>Fired when the host disconnects unexpectedly.</summary>
        public event Action OnHostDisconnected;

        #endregion

        #region Unity Lifecycle

        protected override void OnSingletonAwake()
        {
            if (networkManager == null)
            {
                networkManager = FindAnyObjectByType<NetworkManager>();
            }
        }

        private void OnEnable()
        {
            if (networkManager != null)
            {
                networkManager.OnClientConnectedCallback += HandleClientConnected;
                networkManager.OnClientDisconnectCallback += HandleClientDisconnected;
            }
        }

        private void OnDisable()
        {
            if (networkManager != null)
            {
                networkManager.OnClientConnectedCallback -= HandleClientConnected;
                networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes Unity Gaming Services (Authentication).
        /// Must be called before any session operation.
        /// </summary>
        public async Task InitializeServices()
        {
            if (servicesInitialized) return;

            try
            {
                await UnityServices.InitializeAsync();

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    Debug.Log($"[NetworkGameManager] Signed in. PlayerId: {AuthenticationService.Instance.PlayerId}");
                }

                servicesInitialized = true;
                Debug.Log("[NetworkGameManager] Unity Services initialized.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkGameManager] Failed to initialize services: {e.Message}");
                throw;
            }
        }

        #endregion

        #region Session Creation

        /// <summary>
        /// Creates a new session as host using Relay networking.
        /// The Sessions API handles Relay allocation + lobby creation automatically.
        /// </summary>
        /// <param name="maxPlayers">Max players for this session (clamped to 12).</param>
        /// <param name="isPrivate">If true, session won't appear in public queries.</param>
        /// <param name="sessionName">Optional display name for the session.</param>
        /// <returns>The session join code.</returns>
        public async Task<string> CreateSession(int maxPlayers = MaxPlayers, bool isPrivate = false, string sessionName = null)
        {
            await EnsureServicesInitialized();

            try
            {
                int clamped = Mathf.Clamp(maxPlayers, 2, MaxPlayers);

                var playerName = await GetPlayerName();
                var playerProperties = new Dictionary<string, PlayerProperty>
                {
                    [PlayerNamePropertyKey] = new PlayerProperty(playerName, VisibilityPropertyOptions.Member)
                };

                var options = new SessionOptions
                {
                    MaxPlayers = clamped,
                    IsLocked = false,
                    IsPrivate = isPrivate,
                    Name = sessionName ?? $"Minimos_{playerName}",
                    PlayerProperties = playerProperties
                }.WithRelayNetwork();

                connectedPlayers.Clear();
                activeSession = await MultiplayerService.Instance.CreateSessionAsync(options);

                Debug.Log($"[NetworkGameManager] Session created! Id: {activeSession.Id} | Code: {activeSession.Code} | Max: {clamped}");
                OnSessionStarted?.Invoke(activeSession);

                return activeSession.Code;
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkGameManager] CreateSession failed: {e.Message}");
                throw;
            }
        }

        #endregion

        #region Join Session

        /// <summary>
        /// Joins an existing session using a join code.
        /// </summary>
        /// <param name="joinCode">The session code shared by the host.</param>
        public async Task JoinSessionByCode(string joinCode)
        {
            await EnsureServicesInitialized();

            try
            {
                var playerName = await GetPlayerName();
                var playerProperties = new Dictionary<string, PlayerProperty>
                {
                    [PlayerNamePropertyKey] = new PlayerProperty(playerName, VisibilityPropertyOptions.Member)
                };

                connectedPlayers.Clear();
                activeSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(joinCode);

                Debug.Log($"[NetworkGameManager] Joined session by code: {joinCode} | Id: {activeSession.Id}");
                OnSessionStarted?.Invoke(activeSession);
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkGameManager] JoinSessionByCode failed: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Joins a session by its unique ID.
        /// </summary>
        /// <param name="sessionId">The session ID from a query result.</param>
        public async Task JoinSessionById(string sessionId)
        {
            await EnsureServicesInitialized();

            try
            {
                connectedPlayers.Clear();
                activeSession = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId);

                Debug.Log($"[NetworkGameManager] Joined session by ID: {sessionId}");
                OnSessionStarted?.Invoke(activeSession);
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkGameManager] JoinSessionById failed: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Quick-joins any available public session.
        /// </summary>
        public async Task QuickJoinSession()
        {
            await EnsureServicesInitialized();

            try
            {
                connectedPlayers.Clear();
                var options = new QuickJoinSessionOptions();
                activeSession = await MultiplayerService.Instance.QuickJoinSessionAsync(options);

                Debug.Log($"[NetworkGameManager] Quick-joined session: {activeSession.Id} | Code: {activeSession.Code}");
                OnSessionStarted?.Invoke(activeSession);
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkGameManager] QuickJoinSession failed: {e.Message}");
                throw;
            }
        }

        #endregion

        #region Query Sessions

        /// <summary>
        /// Queries for available public sessions (for session browser).
        /// </summary>
        /// <returns>List of session info for display.</returns>
        public async Task<IList<ISessionInfo>> QuerySessions()
        {
            await EnsureServicesInitialized();

            try
            {
                var queryOptions = new QuerySessionsOptions();
                QuerySessionsResults results = await MultiplayerService.Instance.QuerySessionsAsync(queryOptions);
                Debug.Log($"[NetworkGameManager] Found {results.Sessions.Count} sessions.");
                return results.Sessions;
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkGameManager] QuerySessions failed: {e.Message}");
                throw;
            }
        }

        #endregion

        #region Leave / Disconnect

        /// <summary>
        /// Leaves the current session and shuts down networking.
        /// </summary>
        public async Task LeaveSession()
        {
            if (activeSession != null)
            {
                try
                {
                    await activeSession.LeaveAsync();
                    Debug.Log("[NetworkGameManager] Left session.");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[NetworkGameManager] LeaveSession error (may already be disconnected): {e.Message}");
                }
                finally
                {
                    activeSession = null;
                    connectedPlayers.Clear();
                    OnSessionLeft?.Invoke();
                }
            }

            // Also shut down Netcode if still running
            if (networkManager != null && (networkManager.IsHost || networkManager.IsClient))
            {
                networkManager.Shutdown();
            }
        }

        /// <summary>
        /// Host only: kicks a player from the session.
        /// </summary>
        /// <param name="playerId">The UGS player ID to remove.</param>
        public async Task KickPlayer(string playerId)
        {
            if (!IsHost || activeSession == null) return;

            try
            {
                await activeSession.AsHost().RemovePlayerAsync(playerId);
                Debug.Log($"[NetworkGameManager] Kicked player: {playerId}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkGameManager] KickPlayer failed: {e.Message}");
            }
        }

        #endregion

        #region Player Data

        /// <summary>
        /// Updates the network data for a connected player.
        /// Fires OnAllPlayersReady if all players are now ready.
        /// </summary>
        public void UpdatePlayerData(ulong clientId, PlayerNetworkData data)
        {
            connectedPlayers[clientId] = data;
            CheckAllReady();
        }

        /// <summary>
        /// Sets the ready state for a specific player.
        /// </summary>
        public void SetPlayerReady(ulong clientId, bool isReady)
        {
            if (connectedPlayers.TryGetValue(clientId, out var data))
            {
                data.IsReady = isReady;
                connectedPlayers[clientId] = data;
                CheckAllReady();
            }
        }

        #endregion

        #region Callbacks

        private void HandleClientConnected(ulong clientId)
        {
            if (!connectedPlayers.ContainsKey(clientId))
            {
                connectedPlayers[clientId] = new PlayerNetworkData
                {
                    ClientId = clientId,
                    DisplayName = $"Player_{clientId}",
                    TeamIndex = -1,
                    IsReady = false
                };
            }

            Debug.Log($"[NetworkGameManager] Player connected: {clientId} ({connectedPlayers.Count}/{MaxPlayers})");
            OnPlayerConnected?.Invoke(clientId);
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            connectedPlayers.Remove(clientId);
            Debug.Log($"[NetworkGameManager] Player disconnected: {clientId}");
            OnPlayerDisconnected?.Invoke(clientId);

            // Detect host disconnection on client side
            if (!IsHost && clientId == NetworkManager.ServerClientId)
            {
                Debug.LogWarning("[NetworkGameManager] Host disconnected!");
                OnHostDisconnected?.Invoke();
            }
        }

        #endregion

        #region Helpers

        private async Task EnsureServicesInitialized()
        {
            if (!servicesInitialized)
            {
                await InitializeServices();
            }
        }

        private async Task<string> GetPlayerName()
        {
            try
            {
                return await AuthenticationService.Instance.GetPlayerNameAsync();
            }
            catch
            {
                return $"Player_{UnityEngine.Random.Range(1000, 9999)}";
            }
        }

        private void CheckAllReady()
        {
            if (connectedPlayers.Count == 0) return;

            foreach (var kvp in connectedPlayers)
            {
                if (!kvp.Value.IsReady) return;
            }

            Debug.Log("[NetworkGameManager] All players ready!");
            OnAllPlayersReady?.Invoke();
        }

        #endregion
    }
}
