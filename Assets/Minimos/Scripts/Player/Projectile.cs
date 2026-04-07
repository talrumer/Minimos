using Unity.Netcode;
using UnityEngine;

namespace Minimos.Player
{
    /// <summary>
    /// Networked projectile with parabolic arc trajectory.
    /// Spawned by the server via PlayerCombat. Applies slow on player hit.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class Projectile : NetworkBehaviour
    {
        #region Serialized Fields

        [Header("Trajectory")]
        [SerializeField] private float speed = 10f;
        [SerializeField] private float arcHeight = 3f;
        [SerializeField] private float lifetime = 4f;

        [Header("Combat")]
        [SerializeField] private float slowDuration = 1.5f;
        [SerializeField] private float slowAmount = 0.5f;
        [SerializeField] private float hitRadius = 0.3f;

        [Header("Visuals")]
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private ParticleSystem impactVFX;
        [SerializeField] private Renderer projectileRenderer;

        #endregion

        #region Private State

        private Vector3 startPosition;
        private Vector3 targetDirection;
        private float elapsedTime;
        private float totalFlightTime;
        private bool isInitialized;
        private ulong ownerClientId;
        private bool hasHit;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the projectile with a direction. Called server-side after spawn.
        /// </summary>
        /// <param name="shooterClientId">Client ID of the player who fired this.</param>
        /// <param name="forward">Forward direction of the shooter.</param>
        public void Initialize(ulong shooterClientId, Vector3 forward)
        {
            ownerClientId = shooterClientId;
            startPosition = transform.position;
            targetDirection = forward.normalized;
            totalFlightTime = lifetime;
            isInitialized = true;

            InitializeClientRpc(startPosition, targetDirection, shooterClientId);
        }

        [ClientRpc]
        private void InitializeClientRpc(Vector3 startPos, Vector3 direction, ulong shooterId)
        {
            startPosition = startPos;
            targetDirection = direction;
            ownerClientId = shooterId;
            isInitialized = true;
        }

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            if (!isInitialized || hasHit) return;

            elapsedTime += Time.deltaTime;

            if (elapsedTime >= lifetime)
            {
                DestroyProjectile();
                return;
            }

            // Parabolic arc: move forward + arc up then down
            float t = elapsedTime / totalFlightTime;
            float horizontalDistance = speed * elapsedTime;

            // Horizontal position
            Vector3 horizontalPos = startPosition + targetDirection * horizontalDistance;

            // Vertical arc: parabola peaking at arcHeight at t=0.5
            float verticalOffset = arcHeight * 4f * t * (1f - t);

            transform.position = new Vector3(horizontalPos.x, startPosition.y + verticalOffset, horizontalPos.z);

            // Face movement direction
            Vector3 nextPos = CalculatePositionAtTime(elapsedTime + 0.01f);
            Vector3 moveDir = (nextPos - transform.position).normalized;
            if (moveDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(moveDir);
            }

            // Server collision check
            if (IsServer)
            {
                CheckCollision();
            }
        }

        #endregion

        #region Trajectory

        private Vector3 CalculatePositionAtTime(float time)
        {
            float t = time / totalFlightTime;
            float dist = speed * time;
            Vector3 hPos = startPosition + targetDirection * dist;
            float vOffset = arcHeight * 4f * t * (1f - t);
            return new Vector3(hPos.x, startPosition.y + vOffset, hPos.z);
        }

        #endregion

        #region Collision

        private void CheckCollision()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, hitRadius);

            foreach (var hit in hits)
            {
                // Check for player hit
                var targetCombat = hit.GetComponentInParent<PlayerCombat>();
                if (targetCombat != null && targetCombat.OwnerClientId != ownerClientId)
                {
                    targetCombat.ApplySlow(slowDuration, slowAmount);
                    hasHit = true;
                    SpawnImpactClientRpc(transform.position);
                    DestroyProjectile();
                    return;
                }

                // Check for environment hit (not the shooter, not another projectile)
                if (hit.GetComponent<Projectile>() == null &&
                    hit.GetComponentInParent<PlayerController>() == null)
                {
                    if (!hit.isTrigger)
                    {
                        hasHit = true;
                        SpawnImpactClientRpc(transform.position);
                        DestroyProjectile();
                        return;
                    }
                }
            }
        }

        #endregion

        #region Destruction

        private void DestroyProjectile()
        {
            if (!IsServer) return;

            // Detach trail so it fades naturally
            DetachTrailClientRpc();

            GetComponent<NetworkObject>().Despawn(true);
        }

        [ClientRpc]
        private void DetachTrailClientRpc()
        {
            if (trailRenderer != null)
            {
                trailRenderer.transform.SetParent(null);
                trailRenderer.autodestruct = true;
            }
        }

        [ClientRpc]
        private void SpawnImpactClientRpc(Vector3 position)
        {
            if (impactVFX != null)
            {
                var vfx = Instantiate(impactVFX, position, Quaternion.identity);
                vfx.Play();
                Destroy(vfx.gameObject, vfx.main.duration + 0.5f);
            }
        }

        #endregion
    }
}
