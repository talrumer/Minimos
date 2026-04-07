using System;
using UnityEngine;
using Minimos.Core;

namespace Minimos.Audio
{
    /// <summary>
    /// A named music entry mapping an environment to its AudioClip.
    /// </summary>
    [Serializable]
    public class EnvironmentMusic
    {
        [Tooltip("Name matching the mini-game or environment (e.g., 'CaptureTheFlag', 'Arena').")]
        public string environmentName;

        [Tooltip("The music clip to play in this environment.")]
        public AudioClip clip;
    }

    /// <summary>
    /// ScriptableObject containing music tracks mapped to game states and environments.
    /// Create via Assets > Create > Minimos > Audio > Music Library.
    /// </summary>
    [CreateAssetMenu(fileName = "MusicLibrary", menuName = "Minimos/Audio/Music Library")]
    public class MusicLibrary : ScriptableObject
    {
        #region Fields

        [Header("State Music")]
        [SerializeField] private AudioClip mainMenuMusic;
        [SerializeField] private AudioClip lobbyMusic;
        [SerializeField] private AudioClip characterStudioMusic;
        [SerializeField] private AudioClip teamSelectMusic;
        [SerializeField] private AudioClip miniGameIntroMusic;
        [SerializeField] private AudioClip roundResultsMusic;
        [SerializeField] private AudioClip finalResultsMusic;

        [Header("Environment Music")]
        [SerializeField] private EnvironmentMusic[] environmentTracks;

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns the music clip associated with the given game state.
        /// </summary>
        /// <param name="state">The current game state.</param>
        /// <returns>The matching AudioClip, or null if none is assigned.</returns>
        public AudioClip GetMusicForState(GameState state)
        {
            return state switch
            {
                GameState.MainMenu => mainMenuMusic,
                GameState.Lobby => lobbyMusic,
                GameState.CharacterStudio => characterStudioMusic,
                GameState.TeamSelect => teamSelectMusic,
                GameState.MiniGameIntro => miniGameIntroMusic,
                GameState.RoundResults => roundResultsMusic,
                GameState.FinalResults => finalResultsMusic,
                _ => null
            };
        }

        /// <summary>
        /// Returns the music clip for a named environment/mini-game.
        /// </summary>
        /// <param name="envName">Environment or mini-game name (case-insensitive).</param>
        /// <returns>The matching AudioClip, or null if not found.</returns>
        public AudioClip GetMusicForEnvironment(string envName)
        {
            if (string.IsNullOrEmpty(envName) || environmentTracks == null) return null;

            foreach (var entry in environmentTracks)
            {
                if (string.Equals(entry.environmentName, envName, StringComparison.OrdinalIgnoreCase))
                {
                    return entry.clip;
                }
            }

            Debug.LogWarning($"[MusicLibrary] No music found for environment '{envName}'.");
            return null;
        }

        #endregion
    }
}
