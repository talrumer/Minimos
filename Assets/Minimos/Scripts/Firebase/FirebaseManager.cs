using System;
using System.Threading.Tasks;
using Minimos.Core;
using Minimos.Firebase.Models;
using UnityEngine;

namespace Minimos.Firebase
{
    /// <summary>
    /// Singleton facade for all Firebase operations.
    /// Defaults to <see cref="MockFirebaseService"/> and switches to
    /// <see cref="FirebaseService"/> when the Firebase SDK is detected.
    /// Caches the local player profile and auto-saves with debounce.
    /// </summary>
    public class FirebaseManager : Singleton<FirebaseManager>
    {
        #region Fields

        [Header("Configuration")]
        [SerializeField] private float profileSaveDebounceSeconds = 2f;

        private IFirebaseService service;
        private PlayerProfile cachedProfile;
        private bool profileDirty;
        private float dirtySinceTime;

        #endregion

        #region Properties

        /// <summary>The active Firebase service implementation.</summary>
        public IFirebaseService Service => service;

        /// <summary>Locally cached player profile. Null until loaded.</summary>
        public PlayerProfile CachedProfile => cachedProfile;

        #endregion

        #region Events

        /// <summary>Fired after successful sign-in. Passes user ID.</summary>
        public event Action<string> OnSignedIn;

        /// <summary>Fired after the local player profile is loaded from the backend.</summary>
        public event Action<PlayerProfile> OnProfileLoaded;

        /// <summary>Fired after the cached profile is saved to the backend.</summary>
        public event Action OnProfileUpdated;

        #endregion

        #region Unity Lifecycle

        protected override void OnSingletonAwake()
        {
            // Default to mock until Initialize is called.
            service = new MockFirebaseService();
            Debug.Log("[FirebaseManager] Awake — using MockFirebaseService by default.");
        }

        private void Update()
        {
            // Auto-save dirty profile after debounce period.
            if (profileDirty && Time.time - dirtySinceTime >= profileSaveDebounceSeconds)
            {
                profileDirty = false;
                _ = SaveCachedProfileInternal();
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the Firebase backend. Attempts to use the real Firebase SDK;
        /// falls back to mock if unavailable.
        /// Call this early (e.g., from a bootstrap scene).
        /// </summary>
        public async Task Initialize()
        {
            Debug.Log("[FirebaseManager] Initializing...");

#if FIREBASE_AVAILABLE
            try
            {
                var dependencyStatus = await global::Firebase.FirebaseApp.CheckAndFixDependenciesAsync();
                if (dependencyStatus == global::Firebase.DependencyStatus.Available)
                {
                    var firebaseService = new FirebaseService();
                    firebaseService.Initialize();
                    service = firebaseService;
                    Debug.Log("[FirebaseManager] Using live FirebaseService.");
                }
                else
                {
                    Debug.LogWarning($"[FirebaseManager] Firebase dependencies unavailable ({dependencyStatus}). Using mock.");
                    service = new MockFirebaseService();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[FirebaseManager] Firebase init failed: {e.Message}. Using mock.");
                service = new MockFirebaseService();
            }
#else
            service = new MockFirebaseService();
            Debug.Log("[FirebaseManager] FIREBASE_AVAILABLE not defined. Using MockFirebaseService.");
            await Task.CompletedTask;
#endif
        }

        #endregion

        #region Authentication

        /// <summary>
        /// Signs in anonymously and loads the player profile.
        /// Creates a new profile if none exists.
        /// </summary>
        public async Task<string> SignInAnonymously()
        {
            string userId = await service.SignInAnonymously();
            await PostSignIn(userId);
            return userId;
        }

        /// <summary>
        /// Signs in with Google and loads the player profile.
        /// </summary>
        public async Task<string> SignInWithGoogle()
        {
            string userId = await service.SignInWithGoogle();
            await PostSignIn(userId);
            return userId;
        }

        /// <summary>
        /// Links the current anonymous session to a Google account.
        /// </summary>
        public async Task LinkAnonymousToGoogle()
        {
            await service.LinkAnonymousToGoogle();
        }

        /// <summary>Whether a user is currently signed in.</summary>
        public bool IsSignedIn => service.IsSignedIn();

        /// <summary>The current user ID, or null.</summary>
        public string CurrentUserId => service.GetCurrentUserId();

        /// <summary>Returns the underlying Firebase service for direct calls.</summary>
        public IFirebaseService GetService() => service;

        #endregion

        #region Profile

        /// <summary>
        /// Explicitly loads the current player's profile from the backend.
        /// </summary>
        public async Task<PlayerProfile> LoadCurrentProfile()
        {
            string userId = service.GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogWarning("[FirebaseManager] Cannot load profile — not signed in.");
                return null;
            }

            cachedProfile = await service.GetPlayerProfile(userId);

            if (cachedProfile == null)
            {
                cachedProfile = new PlayerProfile(userId, $"Minimo_{userId[..6]}");
                await service.SavePlayerProfile(cachedProfile);
                Debug.Log($"[FirebaseManager] Created new profile for {userId}.");
            }

            OnProfileLoaded?.Invoke(cachedProfile);
            return cachedProfile;
        }

        /// <summary>
        /// Marks the cached profile as dirty. It will be auto-saved
        /// after the debounce period elapses.
        /// </summary>
        public void MarkProfileDirty()
        {
            if (cachedProfile == null) return;
            profileDirty = true;
            dirtySinceTime = Time.time;
        }

        /// <summary>
        /// Immediately saves the cached profile to the backend (bypasses debounce).
        /// </summary>
        public async Task SaveProfileNow()
        {
            profileDirty = false;
            await SaveCachedProfileInternal();
        }

        #endregion

        #region Helpers

        private async Task PostSignIn(string userId)
        {
            Debug.Log($"[FirebaseManager] Signed in: {userId}");
            OnSignedIn?.Invoke(userId);
            await LoadCurrentProfile();
        }

        private async Task SaveCachedProfileInternal()
        {
            if (cachedProfile == null) return;

            try
            {
                cachedProfile.LastOnline = DateTime.UtcNow.ToString("o");
                await service.SavePlayerProfile(cachedProfile);
                OnProfileUpdated?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseManager] Failed to save profile: {e.Message}");
            }
        }

        #endregion
    }
}
