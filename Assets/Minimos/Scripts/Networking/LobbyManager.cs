using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Minimos.Core;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace Minimos.Networking
{
    /// <summary>
    /// Singleton manager for Unity Lobby service.
    /// Handles lobby creation, joining, heartbeat, polling, and
    /// converting a lobby into a Relay-backed game session.
    /// </summary>
    public class LobbyManager : Singleton<LobbyManager>
    {
        #region Constants

        private const float HEARTBEAT_INTERVAL = 15f;
        private const float POLL_INTERVAL = 2f;
        private const string KEY_RELAY_JOIN_CODE = "relayJoinCode";
        private const string KEY_GAME_STARTED = "gameStarted";

        #endregion

        #region Fields

        private Lobby currentLobby;
        private float heartbeatTimer;
        private float pollTimer;
        private bool isPolling;

        #endregion

        #region Properties

        /// <summary>The lobby we are currently in, or null.</summary>
        public Lobby CurrentLobby => currentLobby;

        /// <summary>Whether we are currently in a lobby.</summary>
        public bool IsInLobby => currentLobby != null;

        /// <summary>Whether the local player is the lobby host.</summary>
        public bool IsLobbyHost =>
            currentLobby != null &&
            currentLobby.HostId == AuthenticationService.Instance.PlayerId;

        /// <summary>Room code for the current lobby (shorthand).</summary>
        public string RoomCode => currentLobby?.LobbyCode;

        #endregion

        #region Events

        /// <summary>Fired after a lobby is created by this player.</summary>
        public event Action<Lobby> OnLobbyCreated;

        /// <summary>Fired after joining a lobby (by code or quick join).</summary>
        public event Action<Lobby> OnLobbyJoined;

        /// <summary>Fired when lobby data is refreshed via polling.</summary>
        public event Action<Lobby> OnLobbyUpdated;

        /// <summary>Fired when another player joins the lobby.</summary>
        public event Action OnPlayerJoinedLobby;

        /// <summary>Fired when another player leaves the lobby.</summary>
        public event Action OnPlayerLeftLobby;

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            if (currentLobby == null) return;

            HandleHeartbeat();
            HandlePolling();
        }

        #endregion

        #region Create / Join / Leave

        /// <summary>
        /// Creates a new lobby and becomes the host.
        /// </summary>
        /// <param name="lobbyName">Display name for the lobby.</param>
        /// <param name="maxPlayers">Max players allowed (clamped to 12).</param>
        /// <param name="isPrivate">If true, lobby won't appear in public listings.</param>
        /// <returns>The created Lobby with its room code.</returns>
        public async Task<Lobby> CreateLobby(string lobbyName, int maxPlayers, bool isPrivate)
        {
            await EnsureServicesInitialized();

            try
            {
                int clamped = Mathf.Clamp(maxPlayers, 2, NetworkGameManager.MaxPlayers);

                var options = new CreateLobbyOptions
                {
                    IsPrivate = isPrivate,
                    Data = new Dictionary<string, DataObject>
                    {
                        [KEY_RELAY_JOIN_CODE] = new DataObject(DataObject.VisibilityOptions.Member, ""),
                        [KEY_GAME_STARTED] = new DataObject(DataObject.VisibilityOptions.Member, "false")
                    }
                };

                currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, clamped, options);

                Debug.Log($"[LobbyManager] Created lobby '{lobbyName}' | Code: {currentLobby.LobbyCode} | Max: {clamped}");
                OnLobbyCreated?.Invoke(currentLobby);

                return currentLobby;
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"[LobbyManager] CreateLobby failed: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Joins a lobby using a short room code.
        /// </summary>
        /// <param name="roomCode">The alphanumeric code shown to players.</param>
        public async Task<Lobby> JoinLobbyByCode(string roomCode)
        {
            await EnsureServicesInitialized();

            try
            {
                currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(roomCode);
                Debug.Log($"[LobbyManager] Joined lobby via code: {roomCode}");
                OnLobbyJoined?.Invoke(currentLobby);
                return currentLobby;
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"[LobbyManager] JoinLobbyByCode failed: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Quick-joins any available public lobby, optionally filtered by region.
        /// </summary>
        /// <param name="region">Optional region filter (e.g., "us-east"). Null for any.</param>
        public async Task<Lobby> QuickJoinLobby(string region = null)
        {
            await EnsureServicesInitialized();

            try
            {
                var options = new QuickJoinLobbyOptions();

                if (!string.IsNullOrEmpty(region))
                {
                    options.Filter = new List<QueryFilter>
                    {
                        new(QueryFilter.FieldOptions.S1, region, QueryFilter.OpOptions.EQ)
                    };
                }

                currentLobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);
                Debug.Log($"[LobbyManager] Quick-joined lobby: {currentLobby.Id}");
                OnLobbyJoined?.Invoke(currentLobby);
                return currentLobby;
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"[LobbyManager] QuickJoinLobby failed: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Leaves the current lobby. If host, the lobby service assigns a new host automatically.
        /// </summary>
        public async Task LeaveLobby()
        {
            if (currentLobby == null) return;

            try
            {
                string lobbyId = currentLobby.Id;
                string playerId = AuthenticationService.Instance.PlayerId;

                await LobbyService.Instance.RemovePlayerAsync(lobbyId, playerId);
                Debug.Log($"[LobbyManager] Left lobby: {lobbyId}");

                currentLobby = null;
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"[LobbyManager] LeaveLobby failed: {e.Message}");
                currentLobby = null;
            }
        }

        #endregion

        #region Lobby Settings (Host Only)

        /// <summary>
        /// Updates lobby data fields. Host only.
        /// </summary>
        /// <param name="data">Key-value pairs to set on the lobby.</param>
        public async Task UpdateLobbySettings(Dictionary<string, string> data)
        {
            if (currentLobby == null || !IsLobbyHost)
            {
                Debug.LogWarning("[LobbyManager] UpdateLobbySettings: not host or no lobby.");
                return;
            }

            try
            {
                var lobbyData = new Dictionary<string, DataObject>();
                foreach (var kvp in data)
                {
                    lobbyData[kvp.Key] = new DataObject(DataObject.VisibilityOptions.Member, kvp.Value);
                }

                var options = new UpdateLobbyOptions { Data = lobbyData };
                currentLobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, options);

                Debug.Log("[LobbyManager] Lobby settings updated.");
                OnLobbyUpdated?.Invoke(currentLobby);
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"[LobbyManager] UpdateLobbySettings failed: {e.Message}");
            }
        }

        #endregion

        #region Start Game Session

        /// <summary>
        /// Host only: allocates a Relay server, shares the join code via lobby data,
        /// and signals clients that the game is starting.
        /// </summary>
        /// <returns>The Relay join code.</returns>
        public async Task<string> StartGameSession()
        {
            if (!IsLobbyHost)
            {
                Debug.LogWarning("[LobbyManager] Only the host can start a game session.");
                return null;
            }

            try
            {
                int maxPlayers = currentLobby.MaxPlayers;
                string joinCode = await NetworkGameManager.Instance.StartHost(maxPlayers);

                if (string.IsNullOrEmpty(joinCode))
                {
                    Debug.LogError("[LobbyManager] Failed to get Relay join code.");
                    return null;
                }

                // Share the join code with all lobby members.
                await UpdateLobbySettings(new Dictionary<string, string>
                {
                    [KEY_RELAY_JOIN_CODE] = joinCode,
                    [KEY_GAME_STARTED] = "true"
                });

                Debug.Log($"[LobbyManager] Game session started. Relay code: {joinCode}");
                return joinCode;
            }
            catch (Exception e)
            {
                Debug.LogError($"[LobbyManager] StartGameSession failed: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Client only: reads the Relay join code from lobby data and connects.
        /// Call after detecting that KEY_GAME_STARTED is "true" via lobby poll.
        /// </summary>
        public async Task JoinGameSession()
        {
            if (currentLobby == null)
            {
                Debug.LogWarning("[LobbyManager] No lobby to join game session from.");
                return;
            }

            if (!currentLobby.Data.TryGetValue(KEY_RELAY_JOIN_CODE, out var joinCodeData) ||
                string.IsNullOrEmpty(joinCodeData.Value))
            {
                Debug.LogWarning("[LobbyManager] Relay join code not available yet.");
                return;
            }

            await NetworkGameManager.Instance.StartClient(joinCodeData.Value);
            Debug.Log("[LobbyManager] Joined game session as client.");
        }

        /// <summary>
        /// Checks lobby data to see if the host has started the game.
        /// </summary>
        public bool HasGameStarted()
        {
            if (currentLobby?.Data == null) return false;
            return currentLobby.Data.TryGetValue(KEY_GAME_STARTED, out var val) && val.Value == "true";
        }

        #endregion

        #region Heartbeat & Polling

        private async void HandleHeartbeat()
        {
            if (!IsLobbyHost) return;

            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer > 0f) return;

            heartbeatTimer = HEARTBEAT_INTERVAL;

            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
            }
            catch (LobbyServiceException e)
            {
                Debug.LogWarning($"[LobbyManager] Heartbeat failed: {e.Message}");
            }
        }

        private async void HandlePolling()
        {
            if (isPolling) return;

            pollTimer -= Time.deltaTime;
            if (pollTimer > 0f) return;

            pollTimer = POLL_INTERVAL;
            isPolling = true;

            try
            {
                int previousPlayerCount = currentLobby.Players.Count;
                currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);

                int newPlayerCount = currentLobby.Players.Count;
                if (newPlayerCount > previousPlayerCount)
                    OnPlayerJoinedLobby?.Invoke();
                else if (newPlayerCount < previousPlayerCount)
                    OnPlayerLeftLobby?.Invoke();

                OnLobbyUpdated?.Invoke(currentLobby);
            }
            catch (LobbyServiceException e)
            {
                Debug.LogWarning($"[LobbyManager] Poll failed: {e.Message}");
                // Lobby may have been deleted.
                if (e.Reason == LobbyExceptionReason.LobbyNotFound)
                {
                    Debug.LogWarning("[LobbyManager] Lobby no longer exists.");
                    currentLobby = null;
                }
            }
            finally
            {
                isPolling = false;
            }
        }

        #endregion

        #region Helpers

        private async Task EnsureServicesInitialized()
        {
            if (!UnityServices.State.Equals(ServicesInitializationState.Initialized))
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }

        #endregion
    }
}
