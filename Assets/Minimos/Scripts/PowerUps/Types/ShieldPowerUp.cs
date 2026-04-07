using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Minimos.PowerUps.Types
{
    /// <summary>
    /// Buddy Shield: grants a 1-hit shield to the user AND their teammate(s).
    /// The shield absorbs one incoming hit, then breaks. Instant activation.
    /// </summary>
    public class ShieldPowerUp : PowerUpBase
    {
        [Header("Shield")]
        [SerializeField] private GameObject shieldVfxPrefab;

        private readonly List<GameObject> shieldedPlayers = new List<GameObject>();
        private readonly Dictionary<GameObject, GameObject> activeShieldVfx = new Dictionary<GameObject, GameObject>();

        /// <summary>
        /// Applies a 1-hit shield to the player and all teammates.
        /// </summary>
        public override void ApplyEffect(GameObject player)
        {
            if (player == null) return;

            // Find teammates via ITeamProvider
            var teamProvider = player.GetComponent<ITeamProvider>();
            if (teamProvider == null)
            {
                // Solo shield if no team info available
                ApplyShieldToPlayer(player);
                return;
            }

            var teammates = teamProvider.GetTeammates();
            ApplyShieldToPlayer(player);

            foreach (var mate in teammates)
            {
                if (mate != null)
                    ApplyShieldToPlayer(mate);
            }
        }

        /// <summary>
        /// Removes the shield from all affected players.
        /// </summary>
        public override void RemoveEffect(GameObject player)
        {
            foreach (var shielded in shieldedPlayers)
            {
                if (shielded == null) continue;

                var damageable = shielded.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.RemoveShield();
                    damageable.OnShieldBroken -= HandleShieldBroken;
                }

                // Remove VFX
                if (activeShieldVfx.TryGetValue(shielded, out var vfx))
                {
                    if (vfx != null)
                    {
                        var nobj = vfx.GetComponent<NetworkObject>();
                        if (nobj != null && nobj.IsSpawned)
                            nobj.Despawn();
                        Object.Destroy(vfx);
                    }
                }
            }

            shieldedPlayers.Clear();
            activeShieldVfx.Clear();
        }

        private void ApplyShieldToPlayer(GameObject player)
        {
            var damageable = player.GetComponent<IDamageable>();
            if (damageable == null) return;

            damageable.ApplyShield(1);
            damageable.OnShieldBroken += HandleShieldBroken;
            shieldedPlayers.Add(player);

            // Spawn shield VFX parented to player
            if (shieldVfxPrefab != null)
            {
                var vfx = Instantiate(shieldVfxPrefab, player.transform);
                var nobj = vfx.GetComponent<NetworkObject>();
                if (nobj != null)
                    nobj.Spawn();
                activeShieldVfx[player] = vfx;
            }

            NotifyShieldAppliedClientRpc();
        }

        private void HandleShieldBroken(GameObject player)
        {
            if (player == null) return;

            var damageable = player.GetComponent<IDamageable>();
            if (damageable != null)
                damageable.OnShieldBroken -= HandleShieldBroken;

            // Destroy VFX for this player
            if (activeShieldVfx.TryGetValue(player, out var vfx))
            {
                if (vfx != null)
                {
                    var nobj = vfx.GetComponent<NetworkObject>();
                    if (nobj != null && nobj.IsSpawned)
                        nobj.Despawn();
                    Object.Destroy(vfx);
                }
                activeShieldVfx.Remove(player);
            }

            shieldedPlayers.Remove(player);
            NotifyShieldBrokenClientRpc();
        }

        [ClientRpc]
        private void NotifyShieldAppliedClientRpc()
        {
            // Visual/audio feedback for shield activation
        }

        [ClientRpc]
        private void NotifyShieldBrokenClientRpc()
        {
            // Visual/audio feedback for shield breaking
        }
    }

    /// <summary>
    /// Interface for team membership that can provide teammate references.
    /// Implement on your player controller.
    /// </summary>
    public interface ITeamProvider
    {
        /// <summary>Returns GameObjects of all teammates (excluding self).</summary>
        List<GameObject> GetTeammates();
    }

    /// <summary>
    /// Interface for player components that can receive and lose shields.
    /// Implement on your health/damage component.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>Applies a shield that absorbs the given number of hits.</summary>
        void ApplyShield(int hitCount);

        /// <summary>Removes any active shield.</summary>
        void RemoveShield();

        /// <summary>Fires when the shield is broken by damage, passing the player GameObject.</summary>
        event System.Action<GameObject> OnShieldBroken;
    }
}
