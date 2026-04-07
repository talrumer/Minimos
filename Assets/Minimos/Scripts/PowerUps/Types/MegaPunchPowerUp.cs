using Unity.Netcode;
using UnityEngine;

namespace Minimos.PowerUps.Types
{
    /// <summary>
    /// One-use power-up: the player's next melee attack deals 3x knockback.
    /// Consumed on the next hit, regardless of whether it connects.
    /// </summary>
    public class MegaPunchPowerUp : PowerUpBase
    {
        [Header("Mega Punch")]
        [SerializeField] private float knockbackMultiplier = 3f;

        private GameObject activePlayer;

        /// <summary>
        /// Buffs the player's next melee attack with 3x knockback.
        /// </summary>
        public override void ApplyEffect(GameObject player)
        {
            if (player == null) return;
            activePlayer = player;

            var combat = player.GetComponent<ICombatModifiable>();
            if (combat != null)
            {
                combat.SetNextAttackKnockbackMultiplier(knockbackMultiplier);
                combat.OnAttackLanded += HandleAttackLanded;
            }

            NotifyMegaPunchReadyClientRpc();
        }

        /// <summary>
        /// Removes the mega punch buff (if it hasn't been consumed yet).
        /// </summary>
        public override void RemoveEffect(GameObject player)
        {
            if (player == null) return;

            var combat = player.GetComponent<ICombatModifiable>();
            if (combat != null)
            {
                combat.SetNextAttackKnockbackMultiplier(1f);
                combat.OnAttackLanded -= HandleAttackLanded;
            }

            activePlayer = null;
            NotifyMegaPunchConsumedClientRpc();
        }

        private void HandleAttackLanded()
        {
            // Consume the buff after one attack
            RemoveEffect(activePlayer);
        }

        [ClientRpc]
        private void NotifyMegaPunchReadyClientRpc()
        {
            // Visual: glowing fists, charge-up particles
        }

        [ClientRpc]
        private void NotifyMegaPunchConsumedClientRpc()
        {
            // Remove glowing fists visual
        }
    }

    /// <summary>
    /// Interface for player combat components that support knockback modification.
    /// Implement on your combat/melee component.
    /// </summary>
    public interface ICombatModifiable
    {
        /// <summary>Sets a knockback multiplier for the next attack (resets to 1 after use).</summary>
        void SetNextAttackKnockbackMultiplier(float multiplier);

        /// <summary>Fires when the player lands an attack.</summary>
        event System.Action OnAttackLanded;
    }
}
