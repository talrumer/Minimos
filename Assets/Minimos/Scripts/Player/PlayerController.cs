using System;
using Unity.Netcode;
using UnityEngine;

namespace Minimos.Player
{
    /// <summary>
    /// Networked player movement controller handling walk, run, jump, double jump,
    /// dodge roll, slide, and gravity. Uses momentum-based acceleration with
    /// squash-and-stretch landing visuals.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : NetworkBehaviour
    {
        #region Serialized Fields

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float runSpeedMultiplier = 1.5f;
        [SerializeField] private float acceleration = 12f;
        [SerializeField] private float deceleration = 10f;
        [SerializeField] private float airAcceleration = 6f;
        [SerializeField] private float turnSpeed = 15f;

        [Header("Jump")]
        [SerializeField] private float jumpForce = 10f;
        [SerializeField] private float doubleJumpForce = 8.5f;
        [SerializeField] private float coyoteTime = 0.15f;
        [SerializeField] private float jumpBufferTime = 0.1f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float maxFallSpeed = -30f;

        [Header("Dodge Roll")]
        [SerializeField] private float dodgeRollSpeed = 12f;
        [SerializeField] private float dodgeRollDuration = 0.4f;
        [SerializeField] private float dodgeRollCooldown = 2f;
        [SerializeField] private float dodgeIFrameDuration = 0.3f;

        [Header("Slide")]
        [SerializeField] private float slideSpeed = 8f;
        [SerializeField] private float slideDuration = 0.6f;
        [SerializeField] private float slideDeceleration = 4f;

        [Header("Ground Check")]
        [SerializeField] private float groundCheckRadius = 0.3f;
        [SerializeField] private float groundCheckDistance = 0.2f;
        [SerializeField] private LayerMask groundLayer;

        [Header("Squash & Stretch")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private float squashAmount = 0.7f;
        [SerializeField] private float stretchAmount = 1.15f;
        [SerializeField] private float squashStretchSpeed = 8f;

        [Header("References")]
        [SerializeField] private PlayerCombat playerCombat;
        [SerializeField] private PlayerAnimator playerAnimator;
        [SerializeField] private PlayerVisuals playerVisuals;

        #endregion

        #region Network Variables

        private NetworkVariable<Vector3> networkPosition = new(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private NetworkVariable<float> networkYRotation = new(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private NetworkVariable<int> jumpCount = new(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        #endregion

        #region Public Properties

        /// <summary>Whether the player is currently on the ground.</summary>
        public bool IsGrounded { get; private set; }

        /// <summary>Whether the player is holding the run input.</summary>
        public bool IsRunning { get; private set; }

        /// <summary>Whether the player is in a dodge roll.</summary>
        public bool IsDodging { get; private set; }

        /// <summary>Whether the player is sliding.</summary>
        public bool IsSliding { get; private set; }

        /// <summary>Current horizontal speed magnitude.</summary>
        public float CurrentSpeed { get; private set; }

        /// <summary>
        /// External speed multiplier for power-ups, flag carry, etc.
        /// 1.0 = normal speed.
        /// </summary>
        public float SpeedMultiplier
        {
            get => speedMultiplier;
            set => speedMultiplier = Mathf.Max(0f, value);
        }

        /// <summary>Whether dodge roll i-frames are active.</summary>
        public bool IsInvulnerable { get; private set; }

        #endregion

        #region Private State

        private CharacterController characterController;
        private Vector3 velocity;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        private float speedMultiplier = 1f;

        // Input
        private Vector2 moveInput;
        private bool runInput;
        private bool jumpRequested;
        private bool dodgeRequested;
        private bool slideRequested;

        // Jump state
        private float lastGroundedTime;
        private float jumpBufferTimer;
        private bool wasGrounded;

        // Dodge roll state
        private float dodgeRollTimer;
        private float dodgeRollCooldownTimer;
        private float dodgeIFrameTimer;
        private Vector3 dodgeDirection;

        // Slide state
        private float slideTimer;
        private float currentSlideSpeed;
        private Vector3 slideDirection;

        // Squash & stretch
        private Vector3 targetScale = Vector3.one;
        private bool isSquashStretching;

        // External knockback
        private Vector3 knockbackVelocity;
        private float knockbackDecay = 8f;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (!IsOwner)
            {
                // Interpolate toward network position for remote players
                transform.position = Vector3.Lerp(transform.position, networkPosition.Value, Time.deltaTime * 15f);
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.Euler(0f, networkYRotation.Value, 0f), Time.deltaTime * 15f);
                return;
            }

            if (playerCombat != null && playerCombat.IsStunned)
            {
                // While stunned, only apply gravity
                ApplyGravity();
                ApplyKnockbackDecay();
                characterController.Move((Vector3.up * verticalVelocity + knockbackVelocity) * Time.deltaTime);
                UpdateAnimator();
                SyncPositionServerRpc(transform.position, transform.eulerAngles.y);
                return;
            }

            UpdateGroundCheck();
            UpdateTimers();

            if (IsDodging)
            {
                UpdateDodgeRoll();
            }
            else if (IsSliding)
            {
                UpdateSlide();
            }
            else
            {
                UpdateMovement();
                HandleJump();
                HandleDodgeRoll();
                HandleSlide();
            }

            ApplyGravity();
            ApplyKnockbackDecay();

            Vector3 finalVelocity = horizontalVelocity + Vector3.up * verticalVelocity + knockbackVelocity;
            characterController.Move(finalVelocity * Time.deltaTime);

            CurrentSpeed = new Vector3(horizontalVelocity.x, 0f, horizontalVelocity.z).magnitude;

            UpdateSquashStretch();
            UpdateAnimator();
            SyncPositionServerRpc(transform.position, transform.eulerAngles.y);
        }

        #endregion

        #region Input Setters (called by PlayerSetup / Input System)

        /// <summary>Set movement input from Input System.</summary>
        public void SetMoveInput(Vector2 input) => moveInput = input;

        /// <summary>Set run input state.</summary>
        public void SetRunInput(bool running) => runInput = running;

        /// <summary>Request a jump (buffered).</summary>
        public void RequestJump()
        {
            jumpRequested = true;
            jumpBufferTimer = jumpBufferTime;
        }

        /// <summary>Request a dodge roll.</summary>
        public void RequestDodgeRoll() => dodgeRequested = true;

        /// <summary>Set slide input state.</summary>
        public void SetSlideInput(bool sliding) => slideRequested = sliding;

        #endregion

        #region Movement

        private void UpdateMovement()
        {
            IsRunning = runInput && moveInput.magnitude > 0.1f;
            float targetSpeed = walkSpeed * (IsRunning ? runSpeedMultiplier : 1f) * speedMultiplier;

            Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

            // Camera-relative movement
            if (UnityEngine.Camera.main != null)
            {
                Vector3 camForward = UnityEngine.Camera.main.transform.forward;
                Vector3 camRight = UnityEngine.Camera.main.transform.right;
                camForward.y = 0f;
                camRight.y = 0f;
                camForward.Normalize();
                camRight.Normalize();
                inputDirection = (camForward * moveInput.y + camRight * moveInput.x).normalized;
            }

            float currentAccel = IsGrounded ? acceleration : airAcceleration;

            if (inputDirection.magnitude > 0.1f)
            {
                Vector3 targetVelocity = inputDirection * targetSpeed;
                horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity,
                    currentAccel * Time.deltaTime);

                // Rotate toward movement direction
                Quaternion targetRotation = Quaternion.LookRotation(inputDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                    turnSpeed * Time.deltaTime);
            }
            else
            {
                // Decelerate to stop (momentum-based, not instant)
                horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero,
                    deceleration * Time.deltaTime);
            }
        }

        #endregion

        #region Jump

        private void HandleJump()
        {
            if (jumpBufferTimer <= 0f)
            {
                jumpRequested = false;
                return;
            }

            bool canCoyoteJump = (Time.time - lastGroundedTime) <= coyoteTime && jumpCount.Value == 0;
            bool canDoubleJump = jumpCount.Value == 1;

            if (jumpRequested && (canCoyoteJump || IsGrounded))
            {
                PerformJump(jumpForce);
                SetJumpCountServerRpc(1);
                jumpRequested = false;
                jumpBufferTimer = 0f;
            }
            else if (jumpRequested && canDoubleJump)
            {
                PerformJump(doubleJumpForce);
                SetJumpCountServerRpc(2);
                jumpRequested = false;
                jumpBufferTimer = 0f;
                // 💡 Double jump burst VFX triggered here
                if (playerVisuals != null) playerVisuals.PlayDoubleJumpVFX();
            }
        }

        private void PerformJump(float force)
        {
            verticalVelocity = force;
            IsGrounded = false;

            // Stretch on jump (elongate Y)
            if (visualRoot != null)
            {
                targetScale = new Vector3(0.85f, stretchAmount, 0.85f);
                isSquashStretching = true;
            }
        }

        #endregion

        #region Dodge Roll

        private void HandleDodgeRoll()
        {
            if (!dodgeRequested || dodgeRollCooldownTimer > 0f || IsDodging) return;

            dodgeRequested = false;
            IsDodging = true;
            IsInvulnerable = true;
            dodgeRollTimer = dodgeRollDuration;
            dodgeIFrameTimer = dodgeIFrameDuration;
            dodgeRollCooldownTimer = dodgeRollCooldown;

            dodgeDirection = horizontalVelocity.magnitude > 0.1f
                ? horizontalVelocity.normalized
                : transform.forward;
        }

        private void UpdateDodgeRoll()
        {
            dodgeRollTimer -= Time.deltaTime;
            dodgeIFrameTimer -= Time.deltaTime;

            if (dodgeIFrameTimer <= 0f) IsInvulnerable = false;

            horizontalVelocity = dodgeDirection * dodgeRollSpeed * speedMultiplier;

            if (dodgeRollTimer <= 0f)
            {
                IsDodging = false;
                if (dodgeIFrameTimer > 0f) IsInvulnerable = false;
            }
        }

        #endregion

        #region Slide

        private void HandleSlide()
        {
            if (!slideRequested || !IsRunning || !IsGrounded || IsSliding) return;

            IsSliding = true;
            slideTimer = slideDuration;
            currentSlideSpeed = CurrentSpeed;
            slideDirection = horizontalVelocity.normalized;
        }

        private void UpdateSlide()
        {
            slideTimer -= Time.deltaTime;
            currentSlideSpeed = Mathf.Max(0f, currentSlideSpeed - slideDeceleration * Time.deltaTime);
            horizontalVelocity = slideDirection * currentSlideSpeed;

            if (slideTimer <= 0f || currentSlideSpeed <= 0.5f)
            {
                IsSliding = false;
            }
        }

        #endregion

        #region Gravity & Ground Check

        private void ApplyGravity()
        {
            if (IsGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f; // Small downward force to stay grounded
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
                verticalVelocity = Mathf.Max(verticalVelocity, maxFallSpeed);
            }
        }

        private void UpdateGroundCheck()
        {
            wasGrounded = IsGrounded;
            Vector3 origin = transform.position + Vector3.up * (groundCheckRadius + 0.05f);
            IsGrounded = Physics.SphereCast(origin, groundCheckRadius, Vector3.down,
                out _, groundCheckDistance + 0.05f, groundLayer, QueryTriggerInteraction.Ignore);

            if (IsGrounded)
            {
                lastGroundedTime = Time.time;

                if (jumpCount.Value > 0) SetJumpCountServerRpc(0);

                // Landing squash
                if (!wasGrounded)
                {
                    OnLanded();
                }
            }
        }

        private void OnLanded()
        {
            if (visualRoot != null)
            {
                targetScale = new Vector3(stretchAmount, squashAmount, stretchAmount);
                isSquashStretching = true;
            }

            if (playerVisuals != null) playerVisuals.PlaySquashStretch();
        }

        #endregion

        #region Squash & Stretch

        private void UpdateSquashStretch()
        {
            if (visualRoot == null) return;

            if (isSquashStretching)
            {
                visualRoot.localScale = Vector3.Lerp(visualRoot.localScale, targetScale,
                    squashStretchSpeed * Time.deltaTime);

                if (Vector3.Distance(visualRoot.localScale, targetScale) < 0.02f)
                {
                    targetScale = Vector3.one;
                    if (Vector3.Distance(visualRoot.localScale, Vector3.one) < 0.02f)
                    {
                        visualRoot.localScale = Vector3.one;
                        isSquashStretching = false;
                    }
                }
            }
        }

        #endregion

        #region Knockback (called externally by PlayerCombat)

        /// <summary>
        /// Apply external knockback velocity. Decays over time.
        /// </summary>
        public void AddKnockback(Vector3 force)
        {
            knockbackVelocity += force;
        }

        private void ApplyKnockbackDecay()
        {
            knockbackVelocity = Vector3.MoveTowards(knockbackVelocity, Vector3.zero,
                knockbackDecay * Time.deltaTime);
        }

        #endregion

        #region Timers

        private void UpdateTimers()
        {
            if (jumpBufferTimer > 0f) jumpBufferTimer -= Time.deltaTime;
            if (dodgeRollCooldownTimer > 0f) dodgeRollCooldownTimer -= Time.deltaTime;
            dodgeRequested = false;
        }

        #endregion

        #region Animator Bridge

        private void UpdateAnimator()
        {
            if (playerAnimator == null) return;
            playerAnimator.UpdateAnimator();
        }

        #endregion

        #region Network RPCs

        [ServerRpc]
        private void SyncPositionServerRpc(Vector3 position, float yRotation)
        {
            networkPosition.Value = position;
            networkYRotation.Value = yRotation;
        }

        [ServerRpc]
        private void SetJumpCountServerRpc(int count)
        {
            jumpCount.Value = count;
        }

        #endregion
    }
}
