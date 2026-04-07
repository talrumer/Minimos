using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Minimos.Core
{
    /// <summary>
    /// Async scene loading utility with progress callbacks and loading screen support.
    /// </summary>
    public class SceneLoader : Singleton<SceneLoader>
    {
        #region Scene Name Constants

        public const string SCENE_SPLASH = "Splash";
        public const string SCENE_MAIN_MENU = "MainMenu";
        public const string SCENE_CHARACTER_STUDIO = "CharacterStudio";
        public const string SCENE_LOBBY = "Lobby";
        public const string SCENE_GAMEPLAY = "Gameplay";
        public const string SCENE_RESULTS = "Results";

        #endregion

        #region Fields

        [SerializeField] private float minimumLoadTime = 1f;

        private bool isLoading;

        #endregion

        #region Properties

        /// <summary>Whether a scene is currently being loaded.</summary>
        public bool IsLoading => isLoading;

        #endregion

        #region Events

        /// <summary>Fired when a scene load begins. Passes the scene name.</summary>
        public event Action<string> OnSceneLoadStarted;

        /// <summary>Fired during loading with the current progress (0-1).</summary>
        public event Action<float> OnSceneLoadProgress;

        /// <summary>Fired when the scene has finished loading. Passes the scene name.</summary>
        public event Action<string> OnSceneLoadCompleted;

        #endregion

        #region Public Methods

        /// <summary>
        /// Loads a scene asynchronously with progress reporting.
        /// </summary>
        /// <param name="sceneName">Name of the scene to load.</param>
        /// <param name="setActiveOnLoad">Whether to set the loaded scene as the active scene.</param>
        public void LoadScene(string sceneName, bool setActiveOnLoad = true)
        {
            if (isLoading)
            {
                Debug.LogWarning($"[SceneLoader] Already loading a scene. Ignoring request for '{sceneName}'.");
                return;
            }

            StartCoroutine(LoadSceneRoutine(sceneName, LoadSceneMode.Single, setActiveOnLoad));
        }

        /// <summary>
        /// Loads a scene additively (without unloading the current scene).
        /// </summary>
        /// <param name="sceneName">Name of the scene to load additively.</param>
        public void LoadSceneAdditive(string sceneName)
        {
            if (isLoading)
            {
                Debug.LogWarning($"[SceneLoader] Already loading a scene. Ignoring additive request for '{sceneName}'.");
                return;
            }

            StartCoroutine(LoadSceneRoutine(sceneName, LoadSceneMode.Additive, false));
        }

        /// <summary>
        /// Unloads an additively loaded scene.
        /// </summary>
        /// <param name="sceneName">Name of the scene to unload.</param>
        public void UnloadScene(string sceneName)
        {
            StartCoroutine(UnloadSceneRoutine(sceneName));
        }

        /// <summary>
        /// Loads a scene and updates the GameManager state to Loading during the transition.
        /// </summary>
        /// <param name="sceneName">Name of the scene to load.</param>
        /// <param name="targetState">The GameState to set after loading completes.</param>
        public void LoadSceneWithState(string sceneName, GameState targetState)
        {
            if (isLoading) return;

            StartCoroutine(LoadSceneWithStateRoutine(sceneName, targetState));
        }

        #endregion

        #region Coroutines

        private IEnumerator LoadSceneRoutine(string sceneName, LoadSceneMode mode, bool setActiveOnLoad)
        {
            isLoading = true;
            float startTime = Time.unscaledTime;

            Debug.Log($"[SceneLoader] Loading '{sceneName}'...");
            OnSceneLoadStarted?.Invoke(sceneName);
            OnSceneLoadProgress?.Invoke(0f);

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, mode);
            operation.allowSceneActivation = false;

            // Report progress until 0.9 (Unity holds at 0.9 until allowSceneActivation).
            while (operation.progress < 0.9f)
            {
                OnSceneLoadProgress?.Invoke(operation.progress);
                yield return null;
            }

            OnSceneLoadProgress?.Invoke(0.9f);

            // Enforce minimum load time so the loading screen doesn't flash.
            float elapsed = Time.unscaledTime - startTime;
            if (elapsed < minimumLoadTime)
            {
                float remaining = minimumLoadTime - elapsed;
                float timer = 0f;
                while (timer < remaining)
                {
                    timer += Time.unscaledDeltaTime;
                    float t = 0.9f + 0.1f * (timer / remaining);
                    OnSceneLoadProgress?.Invoke(Mathf.Min(t, 1f));
                    yield return null;
                }
            }

            OnSceneLoadProgress?.Invoke(1f);
            operation.allowSceneActivation = true;

            // Wait for activation to complete.
            yield return new WaitUntil(() => operation.isDone);

            if (setActiveOnLoad && mode == LoadSceneMode.Single)
            {
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
            }

            isLoading = false;
            Debug.Log($"[SceneLoader] '{sceneName}' loaded.");
            OnSceneLoadCompleted?.Invoke(sceneName);
        }

        private IEnumerator UnloadSceneRoutine(string sceneName)
        {
            AsyncOperation operation = SceneManager.UnloadSceneAsync(sceneName);
            if (operation == null)
            {
                Debug.LogWarning($"[SceneLoader] Could not unload '{sceneName}'.");
                yield break;
            }

            yield return operation;
            Debug.Log($"[SceneLoader] '{sceneName}' unloaded.");
        }

        private IEnumerator LoadSceneWithStateRoutine(string sceneName, GameState targetState)
        {
            GameManager.Instance?.SetState(GameState.Loading);
            yield return LoadSceneRoutine(sceneName, LoadSceneMode.Single, true);
            GameManager.Instance?.SetState(targetState);
        }

        #endregion
    }
}
