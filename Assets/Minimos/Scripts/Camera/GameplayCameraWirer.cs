using Unity.Cinemachine;
using UnityEngine;

namespace Minimos.Camera
{
    /// <summary>
    /// Runtime helper that wires Cinemachine cameras to CameraManager
    /// when the Gameplay scene loads. Needed because CameraManager lives
    /// on GameBootstrap (DontDestroyOnLoad in SplashScreen) while cameras
    /// live in the Gameplay scene.
    /// </summary>
    public class GameplayCameraWirer : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera followCamera;
        [SerializeField] private CinemachineCamera arenaCamera;
        [SerializeField] private CinemachineCamera sideScrollCamera;
        [SerializeField] private CinemachineCamera sportsCamera;
        [SerializeField] private CinemachineCamera splitZoneCamera;
        [SerializeField] private CinemachineImpulseSource impulseSource;

        private void Start()
        {
            var camManager = CameraManager.Instance;
            if (camManager == null)
            {
                Debug.LogWarning("[GameplayCameraWirer] CameraManager not found.");
                return;
            }

            camManager.WireCameras(followCamera, arenaCamera, sideScrollCamera, sportsCamera, splitZoneCamera, impulseSource);
            Debug.Log("✅ [GameplayCameraWirer] Cameras wired to CameraManager.");
        }
    }
}
