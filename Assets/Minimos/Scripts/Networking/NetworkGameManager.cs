using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Minimos.Core;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
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
    /// Singleton that wraps Unity's NetworkManager with Relay-based P2P hosting.
    /// Manages connected player tracking, host/client lifecycle, and host migration events.
    /// </summary>
    public class NetworkGameManager : Singleton<NetworkGameManager>
    {
        #region Constants

        /// <summary>Maximum players allowed in a single session.</summary>
        public const int MaxPlayers = 12;

        #endregion

        #region Fields

        [Header("References")]
        [SerializeField] private NetworkManager networkManager;

        private readonly Dictionary<ulong, PlayerNetworkData> connectedPlayers = new();
        private string currentJoinCode;
        private bool servicesInitialized;

        #endregion

        #region Properties

        /// <summary>Read-only view of all connected players keyed by client ID.</summary>
        public IReadOnlyDictionary<ulong, PlayerNetworkData> ConnectedPlayers => connectedPlayers;

        /// <summary>The current Relay join code, if hosting. Null otherwise.</summary>
        public string CurrentJoinCode => currentJoinCode;

        /// <summary>Whether this instance is the host.</summary>
        public bool IsHost => networkManager != null && networkManager.IsHost;

        /// <summary>Whether we are connected as a client (including host-client).</summary>
        public bool IsConnected => networkManager != null && networkManager.IsConnectedClient;

        #endregion

        #region Events

        /// <summary>Fired when a new player connects. Passes their client ID.</summary>
        public event Action<ulong> OnPlayerConnected;

        /// <summary>Fired when a player disconnects. Passes their client ID.</summary>
        public event Action<ulong> OnPlayerDisconnected;

        /// <summary>Fired when all connected players have set IsReady to true.</summary>
        public event Action OnAllPlayersReady;

        /// <summary>Fired when the host disconnects unexpectedly (for migration handling).</summary>
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
        /// Initializes Unity Gaming Services (Authentication, Relay).
        /// Must be called before any networking operation.
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

        #region Relay

        /// <summary>
        /// Allocates a Relay server and configures the transport for hosting.
        /// </summary>
        /// <param name="maxPlayers">Max connections (clamped to <see cref="MaxPlayers"/>).</param>
        /// <returns>The Relay join code that clients use to connect.</returns>
        public async Task<string> AllocateRelay(int maxPlayers)
        {
            await EnsureServicesInitialized();

            int clampedMax = Mathf.Clamp(maxPlayers, 1, MaxPlayers);

            try
            {
                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(clampedMax);
                currentJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                var transport = networkManager.GetComponent<UnityTransport>();
                if (transport == null)
                {
                    Debug.LogError("[NetworkGameManager] UnityTransport component not found on NetworkManager.");
                    return null;
                }

                transport.SetRelayServerData(
                    allocation.RelayServer.IpV4,
                    (ushort)allocation.RelayServer.Port,
                    allocation.AllocationIdBytes,
                    allocation.Key,
                    allocation.ConnectionData
                );

                Debug.Log($"[NetworkGameManager] Relay allocated. Join code: {currentJoinCode}");
                return currentJoinCode;
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkGameManager] AllocateRelay failed: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Joins an existing Relay allocation using a join code and configures the transport.
        /// </summary>
        /// <param name="joinCode">The join code obtained from the host.</param>
        public async Task JoinRelay(string joinCode)
        {
            await EnsureServicesInitialized();

            try
            {
                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

                var transport = networkManager.GetComponent<UnityTransport>();
                if (transport == null)
                {
                    Debug.LogError("[NetworkGameManager] UnityTransport component not found on NetworkManager.");
                    return;
                }

                transport.SetRelayServerData(
                    joinAllocation.RelayServer.IpV4,
                    (ushort)joinAllocation.RelayServer.Port,
                    joinAllocation.AllocationIdBytes,
                    joinAllocation.Key,
                    joinAllocation.ConnectionData,
                    joinAllocation.HostConnectionData
                );

                Debug.Log($"[NetworkGameManager] Joined Relay with code: {joinCode}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkGameManager] JoinRelay failed: {e.Message}");
                throw;
            }
        }

        #endregion

        #region Host / Client

        /// <summary>
        /// Starts as host: allocates Relay and starts the NetworkManager as host.
        /// </summary>
        /// <param name="maxPlayers">Max players for this session.</param>
        /// <returns>The Relay join code.</returns>
        public async Task<string> StartHost(int maxPlayers = MaxPlayers)
        {
            string joinCode = await AllocateRelay(maxPlayers);
            connectedPlayers.Clear();

            bool success = networkManager.StartHost();
            if (!success)
            {
                Debug.LogError("[NetworkGameManager] NetworkManager.StartHost() failed.");
                return null;
            }

            Debug.Log("[NetworkGameManager] Host started.");
            return joinCode;
        }

        /// <summary>
        /// Starts as client: joins a Relay allocation and starts the NetworkManager as client.
        /// </summary>
        /// <param name="joinCode">The host's Relay join code.</param>
        public async Task StartClient(string joinCode)
        {
            await JoinRelay(joinCode);
            connectedPlayers.Clear();

            bool success = networkManager.StartClient();
            if (!success)
            {
                Debug.LogError("[NetworkGameManager] NetworkManager.StartClient() failed.");
            }
            else
            {
                Debug.Log("[NetworkGameManager] Client started.");
            }
        }

        /// <summary>
        /// Disconnects from the current session and shuts down networking.
        /// </summary>
        public void Disconnect()
        {
            if (networkManager.IsHost || networkManager.IsClient)
            {
                networkManager.Shutdown();
            }

            connectedPlayers.Clear();
            currentJoinCode = null;
            Debug.Log("[NetworkGameManager] Disconnected.");
        }

        #endregion

        #region Player Data

        /// <summary>
        /// Updates the network data for a connected player.
        /// Fires <see cref="OnAllPlayersReady"/> if all players are now ready.
        /// </summary>
        /// <param name="clientId">The client ID to update.</param>
        /// <param name="data">Updated player data.</param>
        public void UpdatePlayerData(ulong clientId, PlayerNetworkData data)
        {
            connectedPlayers[clientId] = data;
            CheckAllReady();
        }

        /// <summary>
        /// Sets the ready state for a specific player.
        /// </summary>
        /// <param name="clientId">Target client ID.</param>
        /// <param name="isReady">Ready state.</param>
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

            // Detect host disconnection on client side.
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
