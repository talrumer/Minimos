using UnityEngine;

namespace Minimos.Teams
{
    /// <summary>
    /// Static class with predefined team color constants matching the GDD palette.
    /// Each team has a pastel primary and a darker accent variant.
    /// </summary>
    public static class TeamColors
    {
        #region Primary Colors

        /// <summary>Coral Red — #FF6B6B</summary>
        public static readonly Color CoralRed = new(1f, 0.420f, 0.420f, 1f);

        /// <summary>Sky Blue — #74B9FF</summary>
        public static readonly Color SkyBlue = new(0.455f, 0.725f, 1f, 1f);

        /// <summary>Mint Green — #55EFC4</summary>
        public static readonly Color MintGreen = new(0.333f, 0.937f, 0.769f, 1f);

        /// <summary>Sunny Yellow — #FFEAA7</summary>
        public static readonly Color SunnyYellow = new(1f, 0.918f, 0.655f, 1f);

        /// <summary>Peach Orange — #FAB1A0</summary>
        public static readonly Color PeachOrange = new(0.980f, 0.694f, 0.627f, 1f);

        /// <summary>Lavender Purple — #A29BFE</summary>
        public static readonly Color LavenderPurple = new(0.635f, 0.608f, 0.996f, 1f);

        #endregion

        #region Accent Colors (Darker Variants)

        /// <summary>Coral Red Accent — #C0392B</summary>
        public static readonly Color CoralRedAccent = new(0.753f, 0.224f, 0.169f, 1f);

        /// <summary>Sky Blue Accent — #2980B9</summary>
        public static readonly Color SkyBlueAccent = new(0.161f, 0.502f, 0.725f, 1f);

        /// <summary>Mint Green Accent — #00B894</summary>
        public static readonly Color MintGreenAccent = new(0f, 0.722f, 0.580f, 1f);

        /// <summary>Sunny Yellow Accent — #F39C12</summary>
        public static readonly Color SunnyYellowAccent = new(0.953f, 0.612f, 0.071f, 1f);

        /// <summary>Peach Orange Accent — #E17055</summary>
        public static readonly Color PeachOrangeAccent = new(0.882f, 0.439f, 0.333f, 1f);

        /// <summary>Lavender Purple Accent — #6C5CE7</summary>
        public static readonly Color LavenderPurpleAccent = new(0.424f, 0.361f, 0.906f, 1f);

        #endregion

        #region Lookup

        /// <summary>All primary colors indexed 0-5.</summary>
        public static readonly Color[] PrimaryColors =
        {
            CoralRed, SkyBlue, MintGreen, SunnyYellow, PeachOrange, LavenderPurple
        };

        /// <summary>All accent colors indexed 0-5.</summary>
        public static readonly Color[] AccentColors =
        {
            CoralRedAccent, SkyBlueAccent, MintGreenAccent, SunnyYellowAccent, PeachOrangeAccent, LavenderPurpleAccent
        };

        /// <summary>All team names indexed 0-5.</summary>
        public static readonly string[] TeamNames =
        {
            "Coral Red", "Sky Blue", "Mint Green", "Sunny Yellow", "Peach Orange", "Lavender Purple"
        };

        /// <summary>
        /// Returns the primary color for a team index (0-5). White if out of range.
        /// </summary>
        public static Color GetPrimary(int teamIndex)
        {
            return (teamIndex >= 0 && teamIndex < PrimaryColors.Length) ? PrimaryColors[teamIndex] : Color.white;
        }

        /// <summary>
        /// Returns the accent color for a team index (0-5). Gray if out of range.
        /// </summary>
        public static Color GetAccent(int teamIndex)
        {
            return (teamIndex >= 0 && teamIndex < AccentColors.Length) ? AccentColors[teamIndex] : Color.gray;
        }

        #endregion
    }
}
