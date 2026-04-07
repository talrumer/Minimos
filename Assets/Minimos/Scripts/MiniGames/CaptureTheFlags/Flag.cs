using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Minimos.MiniGames.CTF
{
    /// <summary>
    /// Current state of a flag on the field.
    /// </summary>
    public enum FlagState : byte
    {
        Available,
        BeingGrabbed,
        Carried,
        Dropped
    }

    /// <summary>
    /// NetworkBehaviour representing a capturable flag. Handles pickup, drop,
    /// respawn logic, and carrier tracking. Host-authoritative.
    /// </summary>
    public class Flag : NetworkBehaviour
    {
        [Header("Visuals")]
        [SerializeField] private GameObject flagModel;
        [SerializeField] private Vector3 carriedOffset = new Vector3(0f, 2.5f, 0f);

        [Header("Timings")]
        [SerializeField] private float grabDuration = 0.5f;
        [SerializeField] private float dropProtectionDuration = 3f;
        [SerializeField] private float carrierSpeedPenalty = 0.2f;

        // --- Network state ---
        private NetworkVariable<ulong> carrierClientId = new NetworkVariable<ulong>(
            ulong.MaxValue, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private NetworkVariable<int> carrierTeamIndex = new NetworkVariable<int>(
            -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private NetworkVariable<FlagState> flagState = new NetworkVariable<FlagState>(
            FlagState.Available, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        // --- Local tracking ---
        private Transform[] spawnPoints;
        private float idleTimer;
        private float dropTimer;
        private float grabTimer;
        private ulong grabbingClientId;
        private int grabbingTeamIndex;
        private Transform carrierTransform;

        // MVP tracking: carrier ID -> total hold time
        private readonly Dictionary<ulong, float> holdTimes = new Dictionary<ulong, float>();
        private float currentHoldStart;

        // --- Events ---
        /// <summary>Fires when a player picks up this flag.</summary>
        public event Action<ulong> OnFlagPickedUp;

        /// <summary>Fires when the flag is dropped at a position.</summary>
        public event Action<Vector3> OnFlagDropped;

        /// <summary>Fires when the flag respawns at a new location.</summary>
        public event Action OnFlagRespawned;

        // --- Public accessors ---
        public bool IsCarried => flagState.Value == FlagState.Carried;
        public bool IsDropped => flagState.Value == FlagState.Dropped;
        public bool IsAvailable => flagState.Value == FlagState.Available;
        public int CarrierTeamIndex => carrierTeamIndex.Value;
        public ulong CarrierClientId => carrierClientId.Value;
        public float CarrierSpeedPenalty => carrierSpeedPenalty;

        /// <summary>How long the flag has been idle (Available state) without interaction.</summary>
        public float IdleTime => idleTimer;

        /// <summary>
        /// Initializes the flag with available spawn points. Called by CTFGameMode after spawning.
        /// </summary>
        public void Initialize(Transform[] availableSpawnPoints)
        {
            spawnPoints = availableSpawnPoints;
            idleTimer = 0f;
        }

        private void Update()
        {
            if (IsOwner)
                UpdateHost();

            UpdateVisuals();
        }

        private void UpdateHost()
        {
            switch (flagState.Value)
            {
                case FlagState.Available:
                    idleTimer += Time.deltaTime;
                    break;

                case FlagState.BeingGrabbed:
                    grabTimer += Time.deltaTime;
                    if (grabTimer >= grabDuration)
                        CompleteGrab();
                    break;

                case FlagState.Carried:
                    TrackHoldTime();
                    break;

                case FlagState.Dropped:
                    dropTimer += Time.deltaTime;
                    if (dropTimer >= dropProtectionDuration)
                    {
                        flagState.Value = FlagState.Available;
                        idleTimer = 0f;
                    }
                    break;
            }
        }

        private void UpdateVisuals()
        {
            if (flagModel == null) return;

            if (IsCarried && carrierTransform != null)
            {
                // Float above carrier's head
                flagModel.transform.position = carrierTransform.position + carriedOffset;
                flagModel.transform.rotation = carrierTransform.rotation;
            }
            else
            {
                flagModel.transform.localPosition = Vector3.zero;
            }
        }

        // --- Pickup ---

        private void OnTriggerEnter(Collider other)
        {
            if (!IsOwner) return;
            if (flagState.Value != FlagState.Available) return;

            // Expect player to have a component that provides team info
            var playerTeam = other.GetComponentInParent<ITeamMember>();
            if (playerTeam == null) return;

            var networkObj = other.GetComponentInParent<NetworkObject>();
            if (networkObj == null) return;

            BeginGrab(networkObj.OwnerClientId, playerTeam.TeamIndex, other.transform.root);
        }

        private void BeginGrab(ulong clientId, int teamIndex, Transform playerTransform)
        {
            flagState.Value = FlagState.BeingGrabbed;
            grabbingClientId = clientId;
            grabbingTeamIndex = teamIndex;
            carrierTransform = playerTransform;
            grabTimer = 0f;
        }

        private void CompleteGrab()
        {
            carrierClientId.Value = grabbingClientId;
            carrierTeamIndex.Value = grabbingTeamIndex;
            flagState.Value = FlagState.Carried;
            idleTimer = 0f;
            currentHoldStart = Time.time;

            BroadcastPickupClientRpc(grabbingClientId);
        }

        [ClientRpc]
        private void BroadcastPickupClientRpc(ulong carrierId)
        {
            OnFlagPickedUp?.Invoke(carrierId);
        }

        // --- Drop ---

        /// <summary>
        /// Forces the carrier to drop the flag (e.g., on hit). Host only.
        /// </summary>
        public void DropFlag()
        {
            if (!IsOwner) return;
            if (flagState.Value != FlagState.Carried) return;

            RecordHoldTime();

            Vector3 dropPos = carrierTransform != null
                ? carrierTransform.position
                : transform.position;

            transform.position = dropPos;
            carrierClientId.Value = ulong.MaxValue;
            carrierTeamIndex.Value = -1;
            flagState.Value = FlagState.Dropped;
            dropTimer = 0f;
            carrierTransform = null;

            BroadcastDropClientRpc(dropPos);
        }

        [ClientRpc]
        private void BroadcastDropClientRpc(Vector3 position)
        {
            OnFlagDropped?.Invoke(position);
        }

        // --- Respawn ---

        /// <summary>
        /// Teleports the flag to a new position. Host only.
        /// </summary>
        public void Respawn(Vector3 newPosition)
        {
            if (!IsOwner) return;

            transform.position = newPosition;
            flagState.Value = FlagState.Available;
            idleTimer = 0f;
            carrierClientId.Value = ulong.MaxValue;
            carrierTeamIndex.Value = -1;
            carrierTransform = null;

            BroadcastRespawnClientRpc();
        }

        [ClientRpc]
        private void BroadcastRespawnClientRpc()
        {
            OnFlagRespawned?.Invoke();
        }

        // --- Hold time tracking (MVP) ---

        private void TrackHoldTime()
        {
            // Continuous tracking happens in RecordHoldTime on drop/end
        }

        private void RecordHoldTime()
        {
            if (carrierClientId.Value == ulong.MaxValue) return;

            float elapsed = Time.time - currentHoldStart;
            if (!holdTimes.ContainsKey(carrierClientId.Value))
                holdTimes[carrierClientId.Value] = 0f;
            holdTimes[carrierClientId.Value] += elapsed;
        }

        /// <summary>
        /// Returns the client ID and total hold time of the player who held this flag longest.
        /// </summary>
        public (ulong clientId, float holdTime) GetLongestCarrier()
        {
            // Record any in-progress hold
            if (flagState.Value == FlagState.Carried)
                RecordHoldTime();

            ulong bestId = ulong.MaxValue;
            float bestTime = 0f;

            foreach (var kvp in holdTimes)
            {
                if (kvp.Value > bestTime)
                {
                    bestTime = kvp.Value;
                    bestId = kvp.Key;
                }
            }

            return (bestId, bestTime);
        }

        /// <summary>
        /// Cancels any ongoing grab if the grabbing player leaves the trigger.
        /// </summary>
        private void OnTriggerExit(Collider other)
        {
            if (!IsOwner) return;
            if (flagState.Value != FlagState.BeingGrabbed) return;

            var networkObj = other.GetComponentInParent<NetworkObject>();
            if (networkObj != null && networkObj.OwnerClientId == grabbingClientId)
            {
                flagState.Value = FlagState.Available;
                grabTimer = 0f;
            }
        }
    }

    /// <summary>
    /// Interface for components that identify a player's team membership.
    /// Implement on your player controller or a dedicated TeamMember component.
    /// </summary>
    public interface ITeamMember
    {
        int TeamIndex { get; }
    }
}
