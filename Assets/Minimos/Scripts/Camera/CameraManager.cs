using UnityEngine;
using Unity.Cinemachine;
using Minimos.MiniGames;

namespace Minimos.Camera
{
    /// <summary>
    /// Singleton camera manager that switches between Cinemachine virtual cameras
    /// for different mini-game camera modes and provides screen shake via impulse.
    /// Uses Cinemachine 3.x API (Unity 6+).
    /// </summary>
    public class CameraManager : Core.Singleton<CameraManager>
    {
        #region Fields

        [Header("Virtual Cameras")]
        [Tooltip("Follow camera (3rd person behind player).")]
        [SerializeField] private CinemachineCamera followCamera;

        [Tooltip("Arena camera (overhead or isometric for arena modes).")]
        [SerializeField] private CinemachineCamera arenaCamera;

        [Tooltip("Side-scroll camera (locked Z-axis, 2D feel).")]
        [SerializeField] private CinemachineCamera sideScrollCamera;

        [Tooltip("Sports camera (wide broadcast-style for sports mini-games).")]
        [SerializeField] private CinemachineCamera sportsCamera;

        [Tooltip("Split-zone camera (tracks mid-point between zones).")]
        [SerializeField] private CinemachineCamera splitZoneCamera;

        [Header("Impulse")]
        [SerializeField] private CinemachineImpulseSource impulseSource;

        [Header("Settings")]
        [Tooltip("Screen shake intensity multiplier from 0 (off) to 1 (full).")]
        [SerializeField] [Range(0f, 1f)] private float shakeIntensityMultiplier = 1f;

        private CameraMode currentMode = CameraMode.Follow;

        #endregion

        #region Properties

        /// <summary>The currently active camera mode.</summary>
        public CameraMode CurrentMode => currentMode;

        /// <summary>
        /// Screen shake intensity multiplier (0-1). Set from settings.
        /// </summary>
        public float ShakeIntensityMultiplier
        {
            get => shakeIntensityMultiplier;
            set => shakeIntensityMultiplier = Mathf.Clamp01(value);
        }

        #endregion

        #region Unity Lifecycle

        protected override void OnSingletonAwake()
        {
            // Load shake preference from PlayerPrefs.
            shakeIntensityMultiplier = PlayerPrefs.GetFloat("Settings_ScreenShake", 100f) / 100f;
            SetCameraMode(CameraMode.Follow);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Activates the virtual camera for the given mode. Cinemachine handles blending.
        /// </summary>
        /// <param name="mode">The camera mode to activate.</param>
        public void SetCameraMode(CameraMode mode)
        {
            currentMode = mode;

            // Deactivate all, then activate the requested one.
            SetAllCamerasPriority(0);

            CinemachineCamera target = GetCameraForMode(mode);
            if (target != null)
            {
                target.Priority = 10;
                Debug.Log($"[CameraManager] Mode set to {mode}.");
            }
            else
            {
                Debug.LogWarning($"[CameraManager] No camera assigned for mode {mode}.");
            }
        }

        /// <summary>
        /// Sets the follow target for the follow camera.
        /// </summary>
        /// <param name="target">Transform to follow (typically the local player).</param>
        public void SetFollowTarget(Transform target)
        {
            if (followCamera != null)
            {
                followCamera.Follow = target;
                followCamera.LookAt = target;
            }
        }

        /// <summary>
        /// Sets the look-at target for the arena camera (e.g., center of the arena).
        /// </summary>
        /// <param name="target">Transform to look at.</param>
        public void SetArenaTarget(Transform target)
        {
            if (arenaCamera != null)
            {
                arenaCamera.LookAt = target;
            }
        }

        /// <summary>
        /// Triggers a camera shake using Cinemachine Impulse.
        /// Intensity is scaled by ShakeIntensityMultiplier.
        /// </summary>
        /// <param name="intensity">Base shake intensity.</param>
        /// <param name="duration">Shake duration in seconds (approximate via impulse shape).</param>
        public void ShakeCamera(float intensity, float duration)
        {
            if (impulseSource == null || shakeIntensityMultiplier <= 0f) return;

            float scaledIntensity = intensity * shakeIntensityMultiplier;

            // CinemachineImpulseSource uses DefaultVelocity * force for the impulse.
            impulseSource.DefaultVelocity = Random.insideUnitSphere.normalized;
            impulseSource.GenerateImpulse(scaledIntensity);
        }

        #endregion

        #region Helpers

        private CinemachineCamera GetCameraForMode(CameraMode mode)
        {
            return mode switch
            {
                CameraMode.Follow => followCamera,
                CameraMode.Arena => arenaCamera,
                CameraMode.SideScroll => sideScrollCamera,
                CameraMode.Sports => sportsCamera,
                CameraMode.SplitZone => splitZoneCamera,
                _ => followCamera
            };
        }

        private void SetAllCamerasPriority(int priority)
        {
            if (followCamera != null) followCamera.Priority = priority;
            if (arenaCamera != null) arenaCamera.Priority = priority;
            if (sideScrollCamera != null) sideScrollCamera.Priority = priority;
            if (sportsCamera != null) sportsCamera.Priority = priority;
            if (splitZoneCamera != null) splitZoneCamera.Priority = priority;
        }

        #endregion
    }
}
