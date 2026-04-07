using System;
using Unity.Netcode;
using UnityEngine;

namespace Minimos.PowerUps
{
    /// <summary>
    /// Single-slot power-up inventory attached to each player. Handles pickup,
    /// storage, and activation of power-ups. Networked so all clients see
    /// what each player is holding.
    /// </summary>
    public class PowerUpInventory : NetworkBehaviour
    {
        [Header("Power-Up Registry")]
        [Tooltip("All power-up configs indexed by their position in this array.")]
        [SerializeField] private PowerUpConfig[] allPowerUps;

        /// <summary>
        /// Index into allPowerUps array. -1 = empty slot.
        /// </summary>
        private NetworkVariable<int> currentPowerUpIndex = new NetworkVariable<int>(
            -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        // Local state
        private bool isStunned;
        private bool isGrabbed;

        // --- Events ---
        /// <summary>Fires when a power-up is picked up, with the config.</summary>
        public event Action<PowerUpConfig> OnPowerUpPickedUp;

        /// <summary>Fires when the held power-up is used.</summary>
        public event Action<PowerUpConfig> OnPowerUpUsed;

        // --- Public accessors ---

        /// <summary>True if the player is holding a power-up.</summary>
        public bool HasPowerUp => currentPowerUpIndex.Value >= 0;

        /// <summary>The currently held power-up config, or null.</summary>
        public PowerUpConfig CurrentPowerUp
        {
            get
            {
                int idx = currentPowerUpIndex.Value;
                if (idx >= 0 && idx < allPowerUps.Length)
                    return allPowerUps[idx];
                return null;
            }
        }

        /// <summary>
        /// Picks up a power-up, replacing any currently held one.
        /// </summary>
        /// <param name="config">The power-up config to store.</param>
        public void PickUp(PowerUpConfig config)
        {
            if (!IsOwner) return;
            if (config == null) return;

            int index = FindConfigIndex(config);
            if (index < 0)
            {
                Debug.LogWarning($"[PowerUpInventory] Config '{config.PowerUpName}' not found in registry.");
                return;
            }

            currentPowerUpIndex.Value = index;
            BroadcastPickupClientRpc(index);
        }

        [ClientRpc]
        private void BroadcastPickupClientRpc(int configIndex)
        {
            if (configIndex >= 0 && configIndex < allPowerUps.Length)
                OnPowerUpPickedUp?.Invoke(allPowerUps[configIndex]);
        }

        /// <summary>
        /// Attempts to use the held power-up. Fails if stunned, grabbed, or empty.
        /// </summary>
        public void UseItem()
        {
            if (!IsOwner) return;
            if (!HasPowerUp) return;
            if (isStunned || isGrabbed) return;

            var config = CurrentPowerUp;
            if (config == null) return;

            // Clear the slot
            int usedIndex = currentPowerUpIndex.Value;
            currentPowerUpIndex.Value = -1;

            // Request the host to apply the effect
            UseItemServerRpc(usedIndex);
        }

        [ServerRpc]
        private void UseItemServerRpc(int configIndex)
        {
            if (configIndex < 0 || configIndex >= allPowerUps.Length) return;

            var config = allPowerUps[configIndex];

            // Spawn VFX
            if (config.VfxPrefab != null)
            {
                GameObject vfx = Instantiate(config.VfxPrefab, transform.position, Quaternion.identity);
                var vfxNetwork = vfx.GetComponent<NetworkObject>();
                if (vfxNetwork != null)
                {
                    vfxNetwork.Spawn();
                    // Auto-destroy VFX after duration or a default
                    float lifetime = config.Duration > 0 ? config.Duration + 1f : 3f;
                    Destroy(vfx, lifetime);
                }
                else
                {
                    Destroy(vfx, config.Duration > 0 ? config.Duration + 1f : 3f);
                }
            }

            // Play activation SFX
            if (config.ActivateSfx != null)
                PlayActivateSfxClientRpc();

            BroadcastUsedClientRpc(configIndex);
        }

        [ClientRpc]
        private void PlayActivateSfxClientRpc()
        {
            var config = CurrentPowerUp;
            if (config != null && config.ActivateSfx != null)
                AudioSource.PlayClipAtPoint(config.ActivateSfx, transform.position);
        }

        [ClientRpc]
        private void BroadcastUsedClientRpc(int configIndex)
        {
            if (configIndex >= 0 && configIndex < allPowerUps.Length)
                OnPowerUpUsed?.Invoke(allPowerUps[configIndex]);
        }

        /// <summary>
        /// Set whether the player is currently stunned (prevents power-up use).
        /// </summary>
        public void SetStunned(bool stunned)
        {
            isStunned = stunned;
        }

        /// <summary>
        /// Set whether the player is currently grabbed (prevents power-up use).
        /// </summary>
        public void SetGrabbed(bool grabbed)
        {
            isGrabbed = grabbed;
        }

        /// <summary>
        /// Clears the current power-up slot without triggering the effect.
        /// </summary>
        public void ClearSlot()
        {
            if (!IsOwner) return;
            currentPowerUpIndex.Value = -1;
        }

        private int FindConfigIndex(PowerUpConfig config)
        {
            for (int i = 0; i < allPowerUps.Length; i++)
            {
                if (allPowerUps[i] == config)
                    return i;
            }
            return -1;
        }
    }
}
