using System;
using System.Collections.Generic;

namespace Minimos.Firebase.Models
{
    /// <summary>
    /// Placement result for a single team in a round.
    /// </summary>
    [Serializable]
    public class TeamPlacement
    {
        /// <summary>Team index (0-based).</summary>
        public int TeamIndex;

        /// <summary>Points earned this round.</summary>
        public int Points;

        public TeamPlacement() { }

        public TeamPlacement(int teamIndex, int points)
        {
            TeamIndex = teamIndex;
            Points = points;
        }
    }

    /// <summary>
    /// Result data for a single round within a match.
    /// </summary>
    [Serializable]
    public class RoundResult
    {
        /// <summary>Name of the mini-game played (e.g., "CaptureTheFlag").</summary>
        public string MiniGameName;

        /// <summary>Ordered list of team placements for this round.</summary>
        public List<TeamPlacement> Rankings = new();

        /// <summary>Optional MVP statistic description (e.g., "3 captures by Player1").</summary>
        public string MvpStat;

        public RoundResult() { }
    }

    /// <summary>
    /// Complete match history record stored in Firestore.
    /// One record per completed party session.
    /// </summary>
    [Serializable]
    public class MatchHistoryData
    {
        /// <summary>Unique match identifier.</summary>
        public string MatchId;

        /// <summary>ISO 8601 timestamp when the match started.</summary>
        public string StartedAt;

        /// <summary>ISO 8601 timestamp when the match ended.</summary>
        public string EndedAt;

        /// <summary>Number of teams that competed.</summary>
        public int TeamCount;

        /// <summary>Per-round results in order.</summary>
        public List<RoundResult> Rounds = new();

        /// <summary>Final team rankings by total points (team index list, first = winner).</summary>
        public List<int> FinalRankings = new();

        /// <summary>User IDs of all players who participated.</summary>
        public List<string> PlayerIds = new();

        public MatchHistoryData() { }
    }
}
