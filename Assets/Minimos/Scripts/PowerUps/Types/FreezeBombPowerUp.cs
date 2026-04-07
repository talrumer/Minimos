using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Minimos.MiniGames.CTF;

namespace Minimos.PowerUps.Types
{
    /// <summary>
    /// Throwable AoE freeze bomb. When used, the player throws a projectile
    /// that creates a freeze zone on impact, rooting all enemies inside for 2 seconds.
    /// </summary>
    public class FreezeBombPowerUp : PowerUpBase
    {
        [Header("Freeze Bomb")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float throwForce = 15f;
        [SerializeField] private float aoeRadius = 4f;
        [SerializeField] private float freezeDuration = 2f;
        [SerializeField] private GameObject freezeZoneVfxPrefab;

        private GameObject activePlayer;

        /// <summary>
        /// Throws a freeze bomb projectile in the player's facing direction.
        /// </summary>
        public override void ApplyEffect(GameObject player)
        {
            if (player == null) return;
            activePlayer = player;

            // Spawn the projectile on the host
            SpawnProjectile(player);
        }

        /// <summary>
        /// No ongoing effect to remove — freeze is self-expiring.
        /// </summary>
        public override void RemoveEffect(GameObject player)
        {
            activePlayer = null;
        }

        private void SpawnProjectile(GameObject player)
        {
            if (projectilePrefab == null)
            {
                // No projectile prefab — instant AoE at player's position
                DetonateAtPosition(player.transform.position);
                return;
            }

            Vector3 spawnPos = player.transform.position + player.transform.forward + Vector3.up;
            GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

            var rb = proj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = player.transform.forward * throwForce + Vector3.up * (throwForce * 0.3f);
            }

            var networkObj = proj.GetComponent<NetworkObject>();
            if (networkObj != null)
                networkObj.Spawn();

            // Set up collision callback
            var bombCollision = proj.GetComponent<FreezeBombProjectile>();
            if (bombCollision == null)
                bombCollision = proj.AddComponent<FreezeBombProjectile>();

            bombCollision.Initialize(this);

            // Auto-detonate after 3 seconds if no collision
            Destroy(proj, 3f);
        }

        /// <summary>
        /// Creates the freeze AoE at the given position. Called on impact.
        /// </summary>
        public void DetonateAtPosition(Vector3 position)
        {
            // Find all enemies in radius
            Collider[] hits = Physics.OverlapSphere(position, aoeRadius);
            var frozenPlayers = new List<GameObject>();

            int userTeam = -1;
            if (activePlayer != null)
            {
                var teamMember = activePlayer.GetComponent<ITeamMember>();
                if (teamMember != null) userTeam = teamMember.TeamIndex;
            }

            foreach (var hit in hits)
            {
                var teamMember = hit.GetComponentInParent<ITeamMember>();
                var networkObj = hit.GetComponentInParent<NetworkObject>();
                if (teamMember == null || networkObj == null) continue;

                // Don't freeze allies
                if (teamMember.TeamIndex == userTeam) continue;

                // Don't double-freeze the same player
                GameObject playerRoot = networkObj.gameObject;
                if (frozenPlayers.Contains(playerRoot)) continue;

                var freezable = playerRoot.GetComponent<IFreezable>();
                if (freezable != null)
                {
                    freezable.Freeze(freezeDuration);
                    frozenPlayers.Add(playerRoot);
                }
            }

            // Spawn freeze zone VFX
            if (freezeZoneVfxPrefab != null)
            {
                GameObject vfx = Instantiate(freezeZoneVfxPrefab, position, Quaternion.identity);
                vfx.transform.localScale = Vector3.one * (aoeRadius * 2f);
                var nobj = vfx.GetComponent<NetworkObject>();
                if (nobj != null)
                    nobj.Spawn();
                Destroy(vfx, freezeDuration + 0.5f);
            }

            NotifyFreezeDetonatedClientRpc(position, frozenPlayers.Count);
        }

        [ClientRpc]
        private void NotifyFreezeDetonatedClientRpc(Vector3 position, int playersHit)
        {
            // Visual/audio feedback: ice shatter effect, freeze sound
        }
    }

    /// <summary>
    /// Attached to the freeze bomb projectile to detect ground/wall collision.
    /// </summary>
    public class FreezeBombProjectile : MonoBehaviour
    {
        private FreezeBombPowerUp owner;
        private bool hasDetonated;

        /// <summary>
        /// Initializes the projectile with its owning power-up for detonation callback.
        /// </summary>
        public void Initialize(FreezeBombPowerUp powerUp)
        {
            owner = powerUp;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (hasDetonated) return;
            hasDetonated = true;

            owner?.DetonateAtPosition(transform.position);

            // Clean up projectile
            var nobj = GetComponent<NetworkObject>();
            if (nobj != null && nobj.IsSpawned)
                nobj.Despawn();
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Interface for player components that can be frozen (rooted) in place.
    /// Implement on your player controller or movement component.
    /// </summary>
    public interface IFreezable
    {
        /// <summary>Roots the player in place for the given duration in seconds.</summary>
        void Freeze(float duration);
    }
}
