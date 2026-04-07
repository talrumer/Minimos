using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Minimos.Player
{
    /// <summary>
    /// Runs on network spawn to configure the player based on ownership.
    /// Enables input and camera for the local player, disables for remotes.
    /// Sets display name and team color from network variables.
    /// </summary>
    public class PlayerSetup : NetworkBehaviour
    {
        #region Serialized Fields

        [Header("Components")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerCombat playerCombat;
        [SerializeField] private PlayerVisuals playerVisuals;
        [SerializeField] private PlayerAnimator playerAnimator;

        [Header("Input")]
        [SerializeField] private PlayerInput playerInput;

        [Header("Camera")]
        [SerializeField] private GameObject cameraTarget;

        #endregion

        #region Network Variables

        private NetworkVariable<FixedPlayerName> networkDisplayName = new(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private NetworkVariable<Color> networkTeamColor = new(
            Color.white, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private NetworkVariable<int> networkTeamId = new(
            -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        #endregion

        #region Public Properties

        /// <summary>The player's display name.</summary>
        public string DisplayName => networkDisplayName.Value.ToString();

        /// <summary>The player's team color.</summary>
        public Color TeamColor => networkTeamColor.Value;

        /// <summary>The player's team ID.</summary>
        public int TeamId => networkTeamId.Value;

        #endregion

        #region Private State

        private MinimosInputActions inputActions;

        #endregion

        #region Network Spawn

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            networkDisplayName.OnValueChanged += OnDisplayNameChanged;
            networkTeamColor.OnValueChanged += OnTeamColorChanged;

            // Apply current values
            ApplyDisplayName(networkDisplayName.Value.ToString());
            ApplyTeamColor(networkTeamColor.Value);

            if (IsOwner)
            {
                SetupLocalPlayer();
            }
            else
            {
                SetupRemotePlayer();
            }
        }

        public override void OnNetworkDespawn()
        {
            networkDisplayName.OnValueChanged -= OnDisplayNameChanged;
            networkTeamColor.OnValueChanged -= OnTeamColorChanged;

            if (IsOwner && inputActions != null)
            {
                UnbindInput();
                inputActions.Dispose();
                inputActions = null;
            }

            base.OnNetworkDespawn();
        }

        #endregion

        #region Setup

        private void SetupLocalPlayer()
        {
            // Enable input
            inputActions = new MinimosInputActions();
            inputActions.Player.Enable();
            BindInput();

            // Enable PlayerInput component if present
            if (playerInput != null) playerInput.enabled = true;

            // Setup camera follow via Cinemachine
            if (cameraTarget != null)
            {
                cameraTarget.SetActive(true);

                // Find Cinemachine virtual camera and set follow target
                var vcam = Object.FindAnyObjectByType<Unity.Cinemachine.CinemachineCamera>();
                if (vcam != null)
                {
                    vcam.Follow = cameraTarget.transform;
                    vcam.LookAt = cameraTarget.transform;
                }
            }
        }

        private void SetupRemotePlayer()
        {
            // Disable input for remote players
            if (playerInput != null) playerInput.enabled = false;

            // Disable camera target
            if (cameraTarget != null) cameraTarget.SetActive(false);
        }

        #endregion

        #region Input Binding

        private void BindInput()
        {
            var player = inputActions.Player;

            player.Move.performed += OnMovePerformed;
            player.Move.canceled += OnMoveCanceled;
            player.Jump.performed += OnJumpPerformed;
            player.Run.performed += OnRunPerformed;
            player.Run.canceled += OnRunCanceled;
            player.DodgeRoll.performed += OnDodgeRollPerformed;
            player.Slide.performed += OnSlidePerformed;
            player.Slide.canceled += OnSlideCanceled;
            player.MeleeAttack.performed += OnMeleePerformed;
            player.MeleeAttack.canceled += OnMeleeReleased;
            player.RangedAttack.performed += OnRangedPerformed;
            player.Grab.performed += OnGrabPerformed;
            player.UseItem.performed += OnUseItemPerformed;
        }

        private void UnbindInput()
        {
            var player = inputActions.Player;

            player.Move.performed -= OnMovePerformed;
            player.Move.canceled -= OnMoveCanceled;
            player.Jump.performed -= OnJumpPerformed;
            player.Run.performed -= OnRunPerformed;
            player.Run.canceled -= OnRunCanceled;
            player.DodgeRoll.performed -= OnDodgeRollPerformed;
            player.Slide.performed -= OnSlidePerformed;
            player.Slide.canceled -= OnSlideCanceled;
            player.MeleeAttack.performed -= OnMeleePerformed;
            player.MeleeAttack.canceled -= OnMeleeReleased;
            player.RangedAttack.performed -= OnRangedPerformed;
            player.Grab.performed -= OnGrabPerformed;
            player.UseItem.performed -= OnUseItemPerformed;
        }

        #endregion

        #region Input Callbacks

        private void OnMovePerformed(InputAction.CallbackContext ctx) =>
            playerController?.SetMoveInput(ctx.ReadValue<Vector2>());

        private void OnMoveCanceled(InputAction.CallbackContext ctx) =>
            playerController?.SetMoveInput(Vector2.zero);

        private void OnJumpPerformed(InputAction.CallbackContext ctx) =>
            playerController?.RequestJump();

        private void OnRunPerformed(InputAction.CallbackContext ctx) =>
            playerController?.SetRunInput(true);

        private void OnRunCanceled(InputAction.CallbackContext ctx) =>
            playerController?.SetRunInput(false);

        private void OnDodgeRollPerformed(InputAction.CallbackContext ctx) =>
            playerController?.RequestDodgeRoll();

        private void OnSlidePerformed(InputAction.CallbackContext ctx) =>
            playerController?.SetSlideInput(true);

        private void OnSlideCanceled(InputAction.CallbackContext ctx) =>
            playerController?.SetSlideInput(false);

        private void OnMeleePerformed(InputAction.CallbackContext ctx) =>
            playerCombat?.OnMeleePressed();

        private void OnMeleeReleased(InputAction.CallbackContext ctx) =>
            playerCombat?.OnMeleeReleased();

        private void OnRangedPerformed(InputAction.CallbackContext ctx) =>
            playerCombat?.OnRangedAttack();

        private void OnGrabPerformed(InputAction.CallbackContext ctx) =>
            playerCombat?.OnGrabPressed();

        private void OnUseItemPerformed(InputAction.CallbackContext ctx)
        {
            // 💡 Item system not yet implemented - hook up here
        }

        #endregion

        #region Server Configuration

        /// <summary>
        /// Set the player's display name. Must be called on the server.
        /// </summary>
        /// <param name="name">Display name string.</param>
        public void SetDisplayName(string name)
        {
            if (!IsServer) return;
            networkDisplayName.Value = new FixedPlayerName(name);
        }

        /// <summary>
        /// Set the player's team. Must be called on the server.
        /// </summary>
        /// <param name="teamId">Team index.</param>
        /// <param name="color">Team color.</param>
        public void SetTeam(int teamId, Color color)
        {
            if (!IsServer) return;
            networkTeamId.Value = teamId;
            networkTeamColor.Value = color;
        }

        #endregion

        #region Value Changed Callbacks

        private void OnDisplayNameChanged(FixedPlayerName previous, FixedPlayerName current)
        {
            ApplyDisplayName(current.ToString());
        }

        private void OnTeamColorChanged(Color previous, Color current)
        {
            ApplyTeamColor(current);
        }

        private void ApplyDisplayName(string name)
        {
            if (playerVisuals != null)
                playerVisuals.SetNameplate(name, networkTeamColor.Value);
        }

        private void ApplyTeamColor(Color color)
        {
            if (playerVisuals != null)
            {
                playerVisuals.SetTeamColor(color);
                playerVisuals.SetNameplate(networkDisplayName.Value.ToString(), color);
            }
        }

        #endregion
    }

    /// <summary>
    /// Fixed-size string struct for network serialization of player names.
    /// Supports up to 32 characters.
    /// </summary>
    public struct FixedPlayerName : INetworkSerializable
    {
        private const int MaxLength = 32;
        private byte length;
        private FixedString32 data;

        public FixedPlayerName(string name)
        {
            data = default;
            if (string.IsNullOrEmpty(name))
            {
                length = 0;
                return;
            }

            length = (byte)Mathf.Min(name.Length, MaxLength);
            data = new FixedString32(name);
        }

        public override string ToString()
        {
            return data.ToString();
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref length);
            serializer.SerializeValue(ref data);
        }
    }

    /// <summary>
    /// Fixed-size buffer for network serialization of short strings.
    /// </summary>
    public struct FixedString32 : INetworkSerializable
    {
        // Store as a Unity FixedString (uses fixed buffer internally)
        private Unity.Collections.FixedString64Bytes value;

        public FixedString32(string str)
        {
            value = new Unity.Collections.FixedString64Bytes();
            if (!string.IsNullOrEmpty(str))
            {
                // Truncate to 32 chars
                string truncated = str.Length > 32 ? str[..32] : str;
                value = new Unity.Collections.FixedString64Bytes(truncated);
            }
        }

        public override string ToString() => value.ToString();

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref value);
        }
    }
}
