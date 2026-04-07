using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Minimos.PowerUps.Types
{
    /// <summary>
    /// Grants the user 2x movement speed for 3 seconds.
    /// Duration-based effect that auto-expires.
    /// </summary>
    public class SpeedBoostPowerUp : PowerUpBase
    {
        [Header("Speed Boost")]
        [SerializeField] private float speedMultiplier = 2f;
        [SerializeField] private float boostDuration = 3f;

        private GameObject activePlayer;
        private Coroutine expiryCoroutine;

        /// <summary>
        /// Doubles the player's movement speed for the configured duration.
        /// </summary>
        public override void ApplyEffect(GameObject player)
        {
            if (player == null) return;
            activePlayer = player;

            // Apply speed modifier via the player's ISpeedModifiable interface
            var speedMod = player.GetComponent<ISpeedModifiable>();
            if (speedMod != null)
            {
                speedMod.ApplySpeedMultiplier(speedMultiplier);
            }

            // Schedule removal
            expiryCoroutine = StartCoroutine(ExpireAfterDuration());

            NotifyEffectAppliedClientRpc();
        }

        /// <summary>
        /// Restores the player's original movement speed.
        /// </summary>
        public override void RemoveEffect(GameObject player)
        {
            if (player == null) return;

            var speedMod = player.GetComponent<ISpeedModifiable>();
            if (speedMod != null)
            {
                speedMod.RemoveSpeedMultiplier(speedMultiplier);
            }

            if (expiryCoroutine != null)
            {
                StopCoroutine(expiryCoroutine);
                expiryCoroutine = null;
            }

            activePlayer = null;
            NotifyEffectRemovedClientRpc();
        }

        private IEnumerator ExpireAfterDuration()
        {
            yield return new WaitForSeconds(boostDuration);
            RemoveEffect(activePlayer);
        }

        [ClientRpc]
        private void NotifyEffectAppliedClientRpc()
        {
            // Visual feedback: speed trail, motion blur, etc.
            // Handled by subscribing to this event in a visual controller.
        }

        [ClientRpc]
        private void NotifyEffectRemovedClientRpc()
        {
            // Remove visual feedback
        }
    }

    /// <summary>
    /// Interface for player components that support speed modification.
    /// Implement on your PlayerController or movement component.
    /// </summary>
    public interface ISpeedModifiable
    {
        /// <summary>Applies a multiplicative speed modifier.</summary>
        void ApplySpeedMultiplier(float multiplier);

        /// <summary>Removes a previously applied speed modifier.</summary>
        void RemoveSpeedMultiplier(float multiplier);
    }
}
