using UnityEngine;

namespace Minimos.MiniGames
{
    /// <summary>
    /// Categories of mini-games that determine gameplay style.
    /// </summary>
    public enum MiniGameCategory
    {
        Objective,
        Combat,
        Race,
        Puzzle,
        Survival
    }

    /// <summary>
    /// Camera behavior modes available during mini-games.
    /// </summary>
    public enum CameraMode
    {
        Follow,
        Arena,
        SideScroll,
        Sports,
        SplitZone
    }

    /// <summary>
    /// ScriptableObject defining a mini-game's configuration and metadata.
    /// Create via Assets > Create > Minimos > Mini Game Config.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMiniGameConfig", menuName = "Minimos/Mini Game Config")]
    public class MiniGameConfig : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string gameName;
        [SerializeField][TextArea(2, 4)] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private MiniGameCategory category;

        [Header("Teams")]
        [SerializeField] private int minTeams = 2;
        [SerializeField] private int maxTeams = 6;

        [Header("Rules")]
        [Tooltip("Game duration in seconds. 0 = no time limit.")]
        [SerializeField] private float duration = 120f;
        [Tooltip("Score required to win. 0 = time-based only.")]
        [SerializeField] private int scoreToWin;

        [Header("Presentation")]
        [SerializeField] private CameraMode cameraMode = CameraMode.Arena;
        [SerializeField] private string environmentSceneName;
        [SerializeField] private bool powerUpsEnabled = true;
        [SerializeField][TextArea(3, 6)] private string rulesText;

        [Header("Prefab")]
        [Tooltip("The prefab containing the MiniGameBase component for this game mode.")]
        [SerializeField] private GameObject gameModePrefab;

        // --- Public accessors ---
        public string GameName => gameName;
        public string Description => description;
        public Sprite Icon => icon;
        public MiniGameCategory Category => category;
        public int MinTeams => minTeams;
        public int MaxTeams => maxTeams;
        public float Duration => duration;
        public int ScoreToWin => scoreToWin;
        public CameraMode CameraMode => cameraMode;
        public string EnvironmentSceneName => environmentSceneName;
        public bool PowerUpsEnabled => powerUpsEnabled;
        public string RulesText => rulesText;
        public GameObject GameModePrefab => gameModePrefab;

        /// <summary>
        /// Returns true if the given team count is within this game's supported range.
        /// </summary>
        public bool SupportsTeamCount(int teamCount)
        {
            return teamCount >= minTeams && teamCount <= maxTeams;
        }
    }
}
