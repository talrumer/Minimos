using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Minimos.MiniGames;

namespace Minimos.Match
{
    /// <summary>
    /// Utility class for selecting mini-games via random pick or voting.
    /// </summary>
    public static class MiniGameSelector
    {
        /// <summary>
        /// Selects a random mini-game config that hasn't been played yet.
        /// Falls back to any available config if all have been played.
        /// </summary>
        /// <param name="available">All configs supporting the current team count.</param>
        /// <param name="alreadyPlayed">Configs already played this party.</param>
        /// <returns>A randomly selected config, or null if none available.</returns>
        public static MiniGameConfig SelectRandom(
            List<MiniGameConfig> available,
            List<MiniGameConfig> alreadyPlayed)
        {
            if (available == null || available.Count == 0) return null;

            var unplayed = available.Where(c => !alreadyPlayed.Contains(c)).ToList();

            if (unplayed.Count > 0)
                return unplayed[Random.Range(0, unplayed.Count)];

            // All games played — allow repeats
            return available[Random.Range(0, available.Count)];
        }

        /// <summary>
        /// Picks a set of random configs for a vote. Returns up to 'count' unique options.
        /// </summary>
        /// <param name="available">Pool of available configs.</param>
        /// <param name="count">Number of vote options to present (default 3).</param>
        /// <returns>A list of distinct configs for players to vote on.</returns>
        public static List<MiniGameConfig> GetVoteOptions(
            List<MiniGameConfig> available,
            int count = 3)
        {
            if (available == null || available.Count == 0)
                return new List<MiniGameConfig>();

            count = Mathf.Min(count, available.Count);

            var shuffled = new List<MiniGameConfig>(available);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            return shuffled.GetRange(0, count);
        }

        /// <summary>
        /// Resolves a vote by returning the config with the most votes.
        /// Ties are broken randomly among tied options.
        /// </summary>
        /// <param name="votes">Dictionary mapping each config to its vote count.</param>
        /// <returns>The winning config, or null if no votes were cast.</returns>
        public static MiniGameConfig ResolveVote(Dictionary<MiniGameConfig, int> votes)
        {
            if (votes == null || votes.Count == 0) return null;

            int maxVotes = votes.Values.Max();
            var tied = votes.Where(kvp => kvp.Value == maxVotes).Select(kvp => kvp.Key).ToList();

            return tied[Random.Range(0, tied.Count)];
        }
    }
}
