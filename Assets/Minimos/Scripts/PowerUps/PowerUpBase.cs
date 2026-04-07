using Unity.Netcode;
using UnityEngine;

namespace Minimos.PowerUps
{
    /// <summary>
    /// Abstract base class for spawned power-up crates on the field.
    /// Handles pickup detection and delegates effect logic to subclasses.
    /// Host-authoritative — only the host processes pickups.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public abstract class PowerUpBase : NetworkBehaviour
    {
        [Header("Power-Up")]
        [SerializeField] private PowerUpConfig config;

        [Header("Crate Visuals")]
        [SerializeField] private Renderer crateRenderer;
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseMin = 0.6f;
        [SerializeField] private float pulseMax = 1f;

        private bool isPickedUp;

        /// <summary>The config defining this power-up's properties.</summary>
        public PowerUpConfig Config => config;

        private void Update()
        {
            if (isPickedUp) return;
            AnimatePulse();
        }

        private void AnimatePulse()
        {
            if (crateRenderer == null) return;

            // Pulse emission white glow
            float t = Mathf.Lerp(pulseMin, pulseMax,
                (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);

            var material = crateRenderer.material;
            Color glow = Color.white * t;
            material.SetColor("_EmissionColor", glow);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsOwner || isPickedUp) return;

            // Check if the collider belongs to a player with an inventory
            var inventory = other.GetComponentInParent<PowerUpInventory>();
            if (inventory == null) return;

            isPickedUp = true;
            inventory.PickUp(config);

            // Play pickup SFX on all clients
            if (config.PickupSfx != null)
                PlayPickupSfxClientRpc();

            // Destroy the crate across the network
            var networkObj = GetComponent<NetworkObject>();
            if (networkObj != null && networkObj.IsSpawned)
                networkObj.Despawn();

            Destroy(gameObject);
        }

        [ClientRpc]
        private void PlayPickupSfxClientRpc()
        {
            if (config.PickupSfx != null)
                AudioSource.PlayClipAtPoint(config.PickupSfx, transform.position);
        }

        /// <summary>
        /// Apply this power-up's effect to the player. Called when the player uses the item.
        /// </summary>
        /// <param name="player">The player GameObject receiving the effect.</param>
        public abstract void ApplyEffect(GameObject player);

        /// <summary>
        /// Remove this power-up's effect from the player. Called when duration expires
        /// or the effect is consumed. No-op for instant power-ups.
        /// </summary>
        /// <param name="player">The player GameObject losing the effect.</param>
        public abstract void RemoveEffect(GameObject player);
    }
}
