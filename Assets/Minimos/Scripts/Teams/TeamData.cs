using UnityEngine;

namespace Minimos.Teams
{
    /// <summary>
    /// ScriptableObject defining a single team's identity (name, colors).
    /// Create via Assets > Create > Minimos > Teams > Team Data.
    /// </summary>
    [CreateAssetMenu(fileName = "TeamData", menuName = "Minimos/Teams/Team Data")]
    public class TeamData : ScriptableObject
    {
        #region Fields

        [Header("Identity")]
        [SerializeField] private int teamIndex;
        [SerializeField] private string teamName;

        [Header("Colors")]
        [Tooltip("The primary pastel team color.")]
        [SerializeField] private Color teamColor = Color.white;

        [Tooltip("Hex string of the primary color (e.g., '#FF6B6B').")]
        [SerializeField] private string teamColorHex = "#FFFFFF";

        [Tooltip("Darker accent variant for outlines, shadows, and emphasis.")]
        [SerializeField] private Color accentColor = Color.gray;

        [Tooltip("Hex string of the accent color.")]
        [SerializeField] private string accentColorHex = "#808080";

        #endregion

        #region Properties

        /// <summary>Zero-based team index.</summary>
        public int TeamIndex => teamIndex;

        /// <summary>Display name of the team (e.g., "Coral Red").</summary>
        public string TeamName => teamName;

        /// <summary>Primary pastel team color.</summary>
        public Color TeamColor => teamColor;

        /// <summary>Primary color as a hex string.</summary>
        public string TeamColorHex => teamColorHex;

        /// <summary>Darker accent color for outlines and emphasis.</summary>
        public Color AccentColor => accentColor;

        /// <summary>Accent color as a hex string.</summary>
        public string AccentColorHex => accentColorHex;

        #endregion
    }
}
