using System;

namespace Minimos.Core
{
    /// <summary>
    /// All possible states the game can be in.
    /// </summary>
    public enum GameState
    {
        Splash,
        MainMenu,
        CharacterStudio,
        Lobby,
        TeamSelect,
        MiniGameIntro,
        Playing,
        RoundResults,
        FinalResults,
        Loading
    }

    /// <summary>
    /// Event args passed when the game state changes.
    /// </summary>
    public class GameStateChangedEventArgs : EventArgs
    {
        /// <summary>The state we're transitioning from.</summary>
        public GameState PreviousState { get; }

        /// <summary>The state we're transitioning to.</summary>
        public GameState NewState { get; }

        public GameStateChangedEventArgs(GameState previous, GameState newState)
        {
            PreviousState = previous;
            NewState = newState;
        }
    }

    /// <summary>
    /// Static event hub for game state transitions.
    /// Subscribe from any system without needing a reference to GameManager.
    /// </summary>
    public static class GameStateEvents
    {
        /// <summary>
        /// Fired whenever the game state changes.
        /// </summary>
        public static event Action<GameStateChangedEventArgs> OnGameStateChanged;

        /// <summary>
        /// Invokes the game state changed event. Called internally by GameManager.
        /// </summary>
        internal static void RaiseGameStateChanged(GameState previous, GameState newState)
        {
            OnGameStateChanged?.Invoke(new GameStateChangedEventArgs(previous, newState));
        }
    }
}
