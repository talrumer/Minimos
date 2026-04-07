using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;

namespace Minimos.Camera
{
    /// <summary>
    /// Free-orbit spectator camera for eliminated players.
    /// Cycles between alive players with Tab, shows a "Spectating: [Name]" overlay,
    /// and prevents communication to alive teammates.
    /// </summary>
    public class SpectatorCamera : MonoBehaviour
    {
        #region Fields

        [Header("Orbit Settings")]
        [SerializeField] private float orbitDistance = 8f;
        [SerializeField] private float orbitSpeed = 120f;
        [SerializeField] private float verticalSpeed = 60f;
        [SerializeField] private float minVerticalAngle = -30f;
        [SerializeField] private float maxVerticalAngle = 60f;
        [SerializeField] private float heightOffset = 2f;

        [Header("UI")]
        [SerializeField] private GameObject spectatorOverlay;
        [SerializeField] private TMP_Text spectatingNameText;

        [Header("Input")]
        [SerializeField] private KeyCode cycleKey = KeyCode.Tab;

        private readonly List<Transform> aliveTargets = new();
        private int currentTargetIndex;
        private float horizontalAngle;
        private float verticalAngle = 20f;
        private bool isActive;

        #endregion

        #region Properties

        /// <summary>Whether the spectator camera is currently active.</summary>
        public bool IsActive => isActive;

        /// <summary>The transform currently being spectated, or null.</summary>
        public Transform CurrentTarget =>
            currentTargetIndex >= 0 && currentTargetIndex < aliveTargets.Count
                ? aliveTargets[currentTargetIndex]
                : null;

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            if (!isActive) return;

            HandleCycleInput();
            UpdateOrbit();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Activates spectator mode. Call when the local player is eliminated.
        /// </summary>
        public void Activate()
        {
            isActive = true;
            RefreshTargets();

            if (spectatorOverlay != null) spectatorOverlay.SetActive(true);

            if (aliveTargets.Count > 0)
            {
                currentTargetIndex = 0;
                UpdateSpectatingLabel();
            }

            Debug.Log("[SpectatorCamera] Activated.");
        }

        /// <summary>
        /// Deactivates spectator mode and hides the overlay.
        /// </summary>
        public void Deactivate()
        {
            isActive = false;
            if (spectatorOverlay != null) spectatorOverlay.SetActive(false);
            Debug.Log("[SpectatorCamera] Deactivated.");
        }

        /// <summary>
        /// Refreshes the list of alive players that can be spectated.
        /// Call when a player is eliminated or respawns.
        /// </summary>
        public void RefreshTargets()
        {
            aliveTargets.Clear();

            // Find all active PlayerControllers that are alive.
            var players = Object.FindObjectsByType<Player.PlayerController>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                var combat = player.GetComponent<Player.PlayerCombat>();
                // Include players that exist and are not the local eliminated player.
                var netObj = player.GetComponent<NetworkObject>();
                if (netObj != null && !netObj.IsOwner)
                {
                    aliveTargets.Add(player.transform);
                }
            }

            // Clamp index.
            if (aliveTargets.Count > 0)
            {
                currentTargetIndex = Mathf.Clamp(currentTargetIndex, 0, aliveTargets.Count - 1);
                UpdateSpectatingLabel();
            }
            else
            {
                currentTargetIndex = -1;
                if (spectatingNameText != null) spectatingNameText.text = "No players to spectate";
            }
        }

        /// <summary>
        /// Manually sets the spectated target by index.
        /// </summary>
        /// <param name="index">Index into the alive targets list.</param>
        public void SetTarget(int index)
        {
            if (index < 0 || index >= aliveTargets.Count) return;
            currentTargetIndex = index;
            UpdateSpectatingLabel();
        }

        #endregion

        #region Private Methods

        private void HandleCycleInput()
        {
            if (Input.GetKeyDown(cycleKey) && aliveTargets.Count > 0)
            {
                currentTargetIndex = (currentTargetIndex + 1) % aliveTargets.Count;
                UpdateSpectatingLabel();
            }
        }

        private void UpdateOrbit()
        {
            Transform target = CurrentTarget;
            if (target == null) return;

            // Mouse orbit.
            horizontalAngle += Input.GetAxis("Mouse X") * orbitSpeed * Time.deltaTime;
            verticalAngle -= Input.GetAxis("Mouse Y") * verticalSpeed * Time.deltaTime;
            verticalAngle = Mathf.Clamp(verticalAngle, minVerticalAngle, maxVerticalAngle);

            // Calculate orbit position.
            Quaternion rotation = Quaternion.Euler(verticalAngle, horizontalAngle, 0f);
            Vector3 offset = rotation * (Vector3.back * orbitDistance);
            Vector3 targetPos = target.position + Vector3.up * heightOffset;

            transform.position = targetPos + offset;
            transform.LookAt(targetPos);
        }

        private void UpdateSpectatingLabel()
        {
            if (spectatingNameText == null) return;

            Transform target = CurrentTarget;
            if (target == null)
            {
                spectatingNameText.text = "Spectating: ---";
                return;
            }

            // Try to get player name from NetworkObject.
            var netObj = target.GetComponent<NetworkObject>();
            string playerName = netObj != null ? $"Player {netObj.OwnerClientId}" : target.name;

            spectatingNameText.text = $"Spectating: {playerName}";
        }

        #endregion
    }
}
