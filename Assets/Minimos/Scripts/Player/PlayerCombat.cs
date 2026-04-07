using System;
using Unity.Netcode;
using UnityEngine;

namespace Minimos.Player
{
    /// <summary>
    /// Networked combat system handling melee, charged melee, ranged attacks,
    /// grab/throw, stun, slow, knockback, and invulnerability.
    /// </summary>
    public class PlayerCombat : NetworkBehaviour
    {
        #region Serialized Fields

        [Header("Melee Attack")]
        [SerializeField] private float meleeRange = 2f;
        [SerializeField] private float meleeRadius = 0.5f;
        [SerializeField] private float meleeWindUp = 0.3f;
        [SerializeField] private float meleeCooldown = 0.5f;
        [SerializeField] private float meleeStunDuration = 1f;
        [SerializeField] private float meleeKnockbackForce = 6f;

        [Header("Charged Melee")]
        [SerializeField] private float chargeTime = 1f;
        [SerializeField] private float chargedStunDuration = 2f;
        [SerializeField] private float chargedKnockbackForce = 12f;

        [Header("Ranged Attack")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform projectileSpawnPoint;
        [SerializeField] private int maxAmmo = 3;
        [SerializeField] private float ammoRechargeTime = 5f;
        [SerializeField] private float rangedCooldown = 3f;

        [Header("Grab & Throw")]
        [SerializeField] private float grabRange = 2f;
        [SerializeField] private float grabBreakTime = 1f;
        [SerializeField] private float throwForce = 15f;
        [SerializeField] private float throwStunDuration = 1f;
        [SerializeField] private Transform grabHoldPoint;

        [Header("Diminishing Returns")]
        [SerializeField] private float diminishingReturnWindow = 5f;
        [SerializeField] private float maxStunDuration = 1.7f;
        [SerializeField] private float[] stunMultipliers = { 1f, 0.75f, 0.56f };

        [Header("Anti-Juggle")]
        [SerializeField] private float hitProtectionDuration = 0.5f;
        [SerializeField] private float hitProtectionKnockbackReduction = 0.5f;

        [Header("References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerAnimator playerAnimator;
        [SerializeField] private PlayerVisuals playerVisuals;
        [SerializeField] private LayerMask playerLayer;

        #endregion

        #region Network Variables

        private NetworkVariable<bool> networkIsStunned = new(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private NetworkVariable<float> networkStunEndTime = new(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private NetworkVariable<bool> networkIsSlowed = new(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private NetworkVariable<float> networkSlowEndTime = new(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private NetworkVariable<int> networkCurrentAmmo = new(
            3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private NetworkVariable<ulong> networkGrabbedPlayerId = new(
            ulong.MaxValue, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        #endregion

        #region Events

        /// <summary>Fired when this player becomes stunned.</summary>
        public event Action OnStunned;

        /// <summary>Fired when this player recovers from stun.</summary>
        public event Action OnStunRecovered;

        /// <summary>Fired when this player performs an attack.</summary>
        public event Action<string> OnAttack;

        /// <summary>Fired when this player is hit.</summary>
        public event Action OnHit;

        #endregion

        #region Public Properties

        /// <summary>Whether this player is currently stunned.</summary>
        public bool IsStunned => networkIsStunned.Value;

        /// <summary>Whether this player is currently slowed.</summary>
        public bool IsSlowed => networkIsSlowed.Value;

        /// <summary>Current ammo count for ranged attacks.</summary>
        public int CurrentAmmo => networkCurrentAmmo.Value;

        /// <summary>Whether this player is currently grabbing another player.</summary>
        public bool IsGrabbing => networkGrabbedPlayerId.Value != ulong.MaxValue;

        #endregion

        #region Private State

        // Melee
        private float meleeCooldownTimer;
        private bool isMeleeWindingUp;
        private float meleeWindUpTimer;

        // Charged melee
        private bool isCharging;
        private float chargeTimer;
        private bool meleeHeld;

        // Ranged
        private float rangedCooldownTimer;
        private float ammoRechargeTimer;

        // Grab
        private PlayerCombat grabbedTarget;
        private float grabBreakTimer;
        private int grabMashCount;
        private const int MashCountToBreakFree = 5;

        // Diminishing returns
        private int recentStunCount;
        private float lastStunTime;

        // Anti-juggle
        private float hitProtectionTimer;
        private bool hasHitProtection;

        // Slow
        private float currentSlowAmount;

        private bool wasStunned;

        #endregion

        #region Unity Lifecycle

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                networkCurrentAmmo.Value = maxAmmo;
            }

            networkIsStunned.OnValueChanged += OnStunnedChanged;
        }

        public override void OnNetworkDespawn()
        {
            networkIsStunned.OnValueChanged -= OnStunnedChanged;
            base.OnNetworkDespawn();
        }

        private void Update()
        {
            if (IsServer) ServerUpdate();
            if (!IsOwner) return;

            UpdateTimers();
            UpdateCharging();
        }

        #endregion

        #region Server Update

        private void ServerUpdate()
        {
            // Stun expiry
            if (networkIsStunned.Value && Time.time >= networkStunEndTime.Value)
            {
                networkIsStunned.Value = false;
            }

            // Slow expiry
            if (networkIsSlowed.Value && Time.time >= networkSlowEndTime.Value)
            {
                networkIsSlowed.Value = false;
                if (playerController != null) playerController.SpeedMultiplier = 1f;
            }

            // Ammo recharge
            if (networkCurrentAmmo.Value < maxAmmo)
            {
                ammoRechargeTimer += Time.deltaTime;
                if (ammoRechargeTimer >= ammoRechargeTime)
                {
                    ammoRechargeTimer = 0f;
                    networkCurrentAmmo.Value = Mathf.Min(networkCurrentAmmo.Value + 1, maxAmmo);
                }
            }

            // Hit protection decay
            if (hasHitProtection)
            {
                hitProtectionTimer -= Time.deltaTime;
                if (hitProtectionTimer <= 0f) hasHitProtection = false;
            }
        }

        #endregion

        #region Input (called by PlayerSetup / Input System)

        /// <summary>Called when melee attack button is pressed.</summary>
        public void OnMeleePressed()
        {
            meleeHeld = true;
            chargeTimer = 0f;
        }

        /// <summary>Called when melee attack button is released.</summary>
        public void OnMeleeReleased()
        {
            if (!meleeHeld) return;
            meleeHeld = false;

            if (IsStunned || meleeCooldownTimer > 0f) return;

            if (chargeTimer >= chargeTime)
            {
                // Charged melee
                isCharging = false;
                StartCoroutine(PerformMeleeCoroutine(true));
            }
            else
            {
                // Normal melee
                StartCoroutine(PerformMeleeCoroutine(false));
            }
        }

        /// <summary>Fire a ranged attack.</summary>
        public void OnRangedAttack()
        {
            if (IsStunned || rangedCooldownTimer > 0f) return;
            if (networkCurrentAmmo.Value <= 0) return;

            rangedCooldownTimer = rangedCooldown;
            FireProjectileServerRpc(projectileSpawnPoint.position, transform.forward);
            OnAttack?.Invoke("ranged");
            if (playerAnimator != null) playerAnimator.TriggerAttack();
        }

        /// <summary>Attempt to grab a nearby player.</summary>
        public void OnGrabPressed()
        {
            if (IsStunned) return;

            if (IsGrabbing)
            {
                // Throw the grabbed player
                ThrowGrabbedPlayerServerRpc();
                OnAttack?.Invoke("throw");
                if (playerAnimator != null) playerAnimator.TriggerThrow();
            }
            else
            {
                // Try to grab
                AttemptGrabServerRpc();
                if (playerAnimator != null) playerAnimator.TriggerGrab();
            }
        }

        /// <summary>Called by grabbed player mashing jump to escape.</summary>
        public void OnGrabMash()
        {
            if (!IsOwner) return;
            GrabMashServerRpc();
        }

        #endregion

        #region Melee Coroutine

        private System.Collections.IEnumerator PerformMeleeCoroutine(bool charged)
        {
            isMeleeWindingUp = true;
            meleeCooldownTimer = meleeCooldown;

            string attackType = charged ? "charged_melee" : "melee";
            OnAttack?.Invoke(attackType);

            if (playerAnimator != null)
            {
                if (charged) playerAnimator.TriggerChargedAttack();
                else playerAnimator.TriggerAttack();
            }

            // Wind-up
            yield return new WaitForSeconds(meleeWindUp);
            isMeleeWindingUp = false;

            // If we got stunned during wind-up, cancel the attack
            if (IsStunned) yield break;

            // SphereCast forward to detect hit
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            float force = charged ? chargedKnockbackForce : meleeKnockbackForce;
            float stunDur = charged ? chargedStunDuration : meleeStunDuration;

            MeleeHitServerRpc(origin, transform.forward, stunDur, force);
        }

        #endregion

        #region Charging

        private void UpdateCharging()
        {
            if (!meleeHeld || IsStunned) return;

            chargeTimer += Time.deltaTime;
            if (chargeTimer >= chargeTime && !isCharging)
            {
                isCharging = true;
                // 💡 Visual/audio feedback for fully charged
            }
        }

        /// <summary>Interrupt a charge-up (e.g., when hit).</summary>
        public void InterruptCharge()
        {
            isCharging = false;
            meleeHeld = false;
            chargeTimer = 0f;
        }

        #endregion

        #region Public Methods (called by server / other systems)

        /// <summary>
        /// Apply stun to this player with diminishing returns.
        /// </summary>
        /// <param name="duration">Base stun duration in seconds.</param>
        public void ApplyStun(float duration)
        {
            if (!IsServer) return;
            if (playerController != null && playerController.IsInvulnerable) return;

            // Diminishing returns
            if (Time.time - lastStunTime < diminishingReturnWindow)
            {
                recentStunCount = Mathf.Min(recentStunCount + 1, stunMultipliers.Length - 1);
            }
            else
            {
                recentStunCount = 0;
            }

            float multiplier = stunMultipliers[Mathf.Min(recentStunCount, stunMultipliers.Length - 1)];
            float finalDuration = Mathf.Min(duration * multiplier, maxStunDuration);

            lastStunTime = Time.time;
            networkStunEndTime.Value = Time.time + finalDuration;
            networkIsStunned.Value = true;

            // Start hit protection after stun
            hasHitProtection = true;
            hitProtectionTimer = hitProtectionDuration;

            InterruptChargeClientRpc();
        }

        /// <summary>
        /// Apply a movement slow to this player.
        /// </summary>
        /// <param name="duration">Slow duration in seconds.</param>
        /// <param name="slowAmount">Multiplier (0.5 = 50% speed).</param>
        public void ApplySlow(float duration, float slowAmount)
        {
            if (!IsServer) return;

            networkSlowEndTime.Value = Time.time + duration;
            networkIsSlowed.Value = true;
            currentSlowAmount = slowAmount;

            if (playerController != null)
                playerController.SpeedMultiplier = slowAmount;
        }

        /// <summary>
        /// Apply knockback force to this player.
        /// </summary>
        /// <param name="direction">Knockback direction (normalized).</param>
        /// <param name="force">Knockback force magnitude.</param>
        public void ApplyKnockback(Vector3 direction, float force)
        {
            if (!IsServer) return;
            if (playerController != null && playerController.IsInvulnerable) return;

            float finalForce = force;
            if (hasHitProtection)
            {
                finalForce *= hitProtectionKnockbackReduction;
            }

            ApplyKnockbackClientRpc(direction.normalized * finalForce);
        }

        #endregion

        #region Server RPCs

        [ServerRpc]
        private void MeleeHitServerRpc(Vector3 origin, Vector3 forward, float stunDuration, float knockback)
        {
            if (Physics.SphereCast(origin, meleeRadius, forward, out RaycastHit hit,
                meleeRange, playerLayer, QueryTriggerInteraction.Ignore))
            {
                var targetCombat = hit.collider.GetComponentInParent<PlayerCombat>();
                if (targetCombat != null && targetCombat != this)
                {
                    Vector3 knockDir = (hit.transform.position - transform.position).normalized;
                    knockDir.y = 0.3f; // Slight upward knockback
                    knockDir.Normalize();

                    targetCombat.ApplyStun(stunDuration);
                    targetCombat.ApplyKnockback(knockDir, knockback);
                    targetCombat.NotifyHitClientRpc();
                }
            }
        }

        [ServerRpc]
        private void FireProjectileServerRpc(Vector3 spawnPos, Vector3 forward)
        {
            if (networkCurrentAmmo.Value <= 0) return;
            networkCurrentAmmo.Value--;
            ammoRechargeTimer = 0f; // Reset recharge timer on fire

            if (projectilePrefab == null) return;

            GameObject proj = Instantiate(projectilePrefab, spawnPos,
                Quaternion.LookRotation(forward));
            var netObj = proj.GetComponent<NetworkObject>();
            if (netObj != null) netObj.Spawn();

            var projectile = proj.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.Initialize(OwnerClientId, forward);
            }
        }

        [ServerRpc]
        private void AttemptGrabServerRpc()
        {
            if (IsGrabbing) return;

            Collider[] hits = Physics.OverlapSphere(transform.position, grabRange, playerLayer);
            foreach (var hit in hits)
            {
                var targetCombat = hit.GetComponentInParent<PlayerCombat>();
                if (targetCombat == null || targetCombat == this) continue;
                if (targetCombat.IsStunned) continue; // Can't grab stunned (or change logic)

                // Grab this player
                networkGrabbedPlayerId.Value = targetCombat.OwnerClientId;
                grabbedTarget = targetCombat;
                grabMashCount = 0;
                return;
            }
        }

        [ServerRpc]
        private void ThrowGrabbedPlayerServerRpc()
        {
            if (!IsGrabbing || grabbedTarget == null) return;

            Vector3 throwDir = transform.forward + Vector3.up * 0.4f;
            throwDir.Normalize();

            grabbedTarget.ApplyKnockback(throwDir, throwForce);
            grabbedTarget.ApplyStun(throwStunDuration);

            networkGrabbedPlayerId.Value = ulong.MaxValue;
            grabbedTarget = null;
        }

        [ServerRpc]
        private void GrabMashServerRpc()
        {
            // Only the grabbed player should call this
            grabMashCount++;
            if (grabMashCount >= MashCountToBreakFree)
            {
                // Break free
                networkGrabbedPlayerId.Value = ulong.MaxValue;
                grabbedTarget = null;
            }
        }

        #endregion

        #region Client RPCs

        [ClientRpc]
        private void ApplyKnockbackClientRpc(Vector3 force)
        {
            if (playerController != null)
                playerController.AddKnockback(force);
        }

        [ClientRpc]
        private void InterruptChargeClientRpc()
        {
            InterruptCharge();
        }

        [ClientRpc]
        private void NotifyHitClientRpc()
        {
            OnHit?.Invoke();
            if (playerVisuals != null) playerVisuals.PlayHitFlash();
            if (playerAnimator != null) playerAnimator.TriggerHit();
        }

        #endregion

        #region Callbacks

        private void OnStunnedChanged(bool previous, bool current)
        {
            if (current)
            {
                OnStunned?.Invoke();
                if (playerVisuals != null) playerVisuals.SetInvulnerableVisual(false);
            }
            else
            {
                OnStunRecovered?.Invoke();
            }
        }

        #endregion

        #region Timers

        private void UpdateTimers()
        {
            if (meleeCooldownTimer > 0f) meleeCooldownTimer -= Time.deltaTime;
            if (rangedCooldownTimer > 0f) rangedCooldownTimer -= Time.deltaTime;
        }

        #endregion
    }
}
