using System.Linq;

namespace Minimos.Match
{
    /// <summary>
    /// Static utility class providing comeback bonuses for trailing teams.
    /// All methods are pure functions based on current standings.
    /// </summary>
    public static class ComebackMechanics
    {
        /// <summary>
        /// Returns a speed multiplier boost (0 to 0.1) for trailing teams.
        /// Last place gets the full boost; first place gets 0.
        /// </summary>
        /// <param name="teamRank">1-based rank of the team (1 = first place).</param>
        /// <param name="totalTeams">Total number of teams in the match.</param>
        public static float GetSpeedBoost(int teamRank, int totalTeams)
        {
            if (totalTeams <= 1) return 0f;
            float t = (float)(teamRank - 1) / (totalTeams - 1);
            return t * 0.1f;
        }

        /// <summary>
        /// Returns a cooldown reduction (0 to 0.15) for trailing teams.
        /// Applied as a multiplier reduction to ability cooldowns.
        /// </summary>
        /// <param name="teamRank">1-based rank of the team.</param>
        /// <param name="totalTeams">Total number of teams.</param>
        public static float GetCooldownReduction(int teamRank, int totalTeams)
        {
            if (totalTeams <= 1) return 0f;
            float t = (float)(teamRank - 1) / (totalTeams - 1);
            return t * 0.15f;
        }

        /// <summary>
        /// Returns an increased power-up spawn probability boost (0 to 0.2) for
        /// trailing teams. Spawn logic adds this to the base spawn chance near
        /// that team's area.
        /// </summary>
        /// <param name="teamRank">1-based rank of the team.</param>
        /// <param name="totalTeams">Total number of teams.</param>
        public static float GetPowerUpSpawnBoost(int teamRank, int totalTeams)
        {
            if (totalTeams <= 1) return 0f;
            float t = (float)(teamRank - 1) / (totalTeams - 1);
            return t * 0.2f;
        }

        /// <summary>
        /// Returns true if the leading team is ahead by a large enough margin
        /// to trigger a mercy bonus for trailing teams. Threshold: leading team
        /// has 3+ rounds worth of points (30+) more than the second place team.
        /// </summary>
        /// <param name="partyScores">Array of total party scores indexed by team.</param>
        public static bool ShouldApplyMercyBonus(int[] partyScores)
        {
            if (partyScores == null || partyScores.Length < 2) return false;

            var sorted = partyScores.OrderByDescending(s => s).ToArray();
            int gap = sorted[0] - sorted[1];

            // 3 rounds worth of 1st-place points (10 each) = 30
            return gap >= 30;
        }

        /// <summary>
        /// Returns the score multiplier for the final round (1.5x).
        /// </summary>
        public static float GetFinalRoundMultiplier()
        {
            return 1.5f;
        }

        /// <summary>
        /// Calculates the team's rank (1-based) from the party scores array.
        /// Tied scores share the same rank.
        /// </summary>
        /// <param name="teamIndex">Index of the team to rank.</param>
        /// <param name="partyScores">Array of all team scores.</param>
        public static int GetTeamRank(int teamIndex, int[] partyScores)
        {
            if (partyScores == null || teamIndex < 0 || teamIndex >= partyScores.Length)
                return 1;

            int teamScore = partyScores[teamIndex];
            int rank = 1;
            for (int i = 0; i < partyScores.Length; i++)
            {
                if (i != teamIndex && partyScores[i] > teamScore)
                    rank++;
            }
            return rank;
        }
    }
}
