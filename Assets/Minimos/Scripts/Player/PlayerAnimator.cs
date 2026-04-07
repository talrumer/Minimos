using UnityEngine;

namespace Minimos.Player
{
    /// <summary>
    /// Wrapper around Unity's Animator for the player character.
    /// Exposes typed methods for setting parameters and triggering animations.
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerCombat playerCombat;

        #endregion

        #region Animator Hashes

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int IsJumpingHash = Animator.StringToHash("IsJumping");
        private static readonly int IsDodgingHash = Animator.StringToHash("IsDodging");
        private static readonly int IsSlidingHash = Animator.StringToHash("IsSliding");
        private static readonly int IsStunnedHash = Animator.StringToHash("IsStunned");
        private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");
        private static readonly int ChargedAttackTriggerHash = Animator.StringToHash("ChargedAttackTrigger");
        private static readonly int GrabTriggerHash = Animator.StringToHash("GrabTrigger");
        private static readonly int ThrowTriggerHash = Animator.StringToHash("ThrowTrigger");
        private static readonly int HitTriggerHash = Animator.StringToHash("HitTrigger");

        #endregion

        #region Public Methods

        /// <summary>
        /// Update all continuous animator parameters. Called each frame by PlayerController.
        /// </summary>
        public void UpdateAnimator()
        {
            if (animator == null) return;

            float normalizedSpeed = 0f;
            if (playerController != null)
            {
                // Normalize speed: 0 = idle, 0.5 = walk, 1.0 = full run
                float maxSpeed = 5f * 1.5f; // walkSpeed * runMultiplier (approximate)
                normalizedSpeed = Mathf.Clamp01(playerController.CurrentSpeed / maxSpeed);
            }

            animator.SetFloat(SpeedHash, normalizedSpeed, 0.1f, Time.deltaTime);
            animator.SetBool(IsGroundedHash, playerController != null && playerController.IsGrounded);
            animator.SetBool(IsJumpingHash, playerController != null && !playerController.IsGrounded);
            animator.SetBool(IsDodgingHash, playerController != null && playerController.IsDodging);
            animator.SetBool(IsSlidingHash, playerController != null && playerController.IsSliding);
            animator.SetBool(IsStunnedHash, playerCombat != null && playerCombat.IsStunned);
        }

        /// <summary>
        /// Set the blend tree speed parameter directly (0..1).
        /// </summary>
        /// <param name="normalizedSpeed">Speed from 0 (idle) to 1 (full run).</param>
        public void SetMoveSpeed(float normalizedSpeed)
        {
            if (animator == null) return;
            animator.SetFloat(SpeedHash, Mathf.Clamp01(normalizedSpeed));
        }

        /// <summary>Trigger a normal melee attack animation.</summary>
        public void TriggerAttack()
        {
            if (animator != null) animator.SetTrigger(AttackTriggerHash);
        }

        /// <summary>Trigger a charged melee attack animation.</summary>
        public void TriggerChargedAttack()
        {
            if (animator != null) animator.SetTrigger(ChargedAttackTriggerHash);
        }

        /// <summary>Trigger the grab animation.</summary>
        public void TriggerGrab()
        {
            if (animator != null) animator.SetTrigger(GrabTriggerHash);
        }

        /// <summary>Trigger the throw animation.</summary>
        public void TriggerThrow()
        {
            if (animator != null) animator.SetTrigger(ThrowTriggerHash);
        }

        /// <summary>Trigger the hit reaction animation.</summary>
        public void TriggerHit()
        {
            if (animator != null) animator.SetTrigger(HitTriggerHash);
        }

        /// <summary>
        /// Play a named emote animation via trigger.
        /// </summary>
        /// <param name="emoteName">Name of the emote trigger parameter (e.g., "Wave", "Dance").</param>
        public void PlayEmote(string emoteName)
        {
            if (animator == null || string.IsNullOrEmpty(emoteName)) return;
            animator.SetTrigger(Animator.StringToHash(emoteName));
        }

        #endregion
    }
}
