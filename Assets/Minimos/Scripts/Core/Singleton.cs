using UnityEngine;

namespace Minimos.Core
{
    /// <summary>
    /// Generic singleton base class for MonoBehaviour managers.
    /// Ensures only one instance exists and optionally persists across scenes.
    /// </summary>
    /// <typeparam name="T">The concrete manager type inheriting from this class.</typeparam>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        #region Fields

        [SerializeField] private bool dontDestroyOnLoad = true;

        private static T instance;
        private static readonly object lockObj = new();
        private static bool isQuitting;

        #endregion

        #region Properties

        /// <summary>
        /// The singleton instance. Returns null if the application is quitting.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (isQuitting)
                {
                    Debug.LogWarning($"[Singleton] Instance of {typeof(T)} requested after application quit.");
                    return null;
                }

                lock (lockObj)
                {
                    if (instance == null)
                    {
                        instance = FindAnyObjectByType<T>();

                        if (instance == null)
                        {
                            Debug.LogWarning($"[Singleton] No instance of {typeof(T)} found in scene.");
                        }
                    }

                    return instance;
                }
            }
        }

        /// <summary>
        /// Whether a valid singleton instance currently exists.
        /// </summary>
        public static bool HasInstance => instance != null;

        #endregion

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning($"[Singleton] Duplicate {typeof(T)} detected on '{gameObject.name}'. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            instance = this as T;

            if (dontDestroyOnLoad)
            {
                // Ensure we're at the root before calling DontDestroyOnLoad.
                if (transform.parent != null)
                    transform.SetParent(null);

                DontDestroyOnLoad(gameObject);
            }

            OnSingletonAwake();
        }

        protected virtual void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        protected virtual void OnApplicationQuit()
        {
            isQuitting = true;
        }

        #endregion

        #region Virtual Methods

        /// <summary>
        /// Called after the singleton instance is established in Awake.
        /// Override this instead of Awake in derived classes.
        /// </summary>
        protected virtual void OnSingletonAwake() { }

        #endregion
    }
}
