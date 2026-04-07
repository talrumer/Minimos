using System;

namespace Minimos.Firebase.Models
{
    /// <summary>
    /// A snapshot of a public lobby for display in the lobby browser.
    /// Stored in Firestore and refreshed by the host.
    /// </summary>
    [Serializable]
    public class LobbyListingData
    {
        /// <summary>Unity Lobby service ID.</summary>
        public string LobbyId;

        /// <summary>Display name of the host player.</summary>
        public string HostName;

        /// <summary>Short alphanumeric code players can type to join.</summary>
        public string RoomCode;

        /// <summary>Number of players currently in the lobby.</summary>
        public int CurrentPlayers;

        /// <summary>Maximum players allowed.</summary>
        public int MaxPlayers;

        /// <summary>Server region (e.g., "us-east", "eu-west").</summary>
        public string Region;

        /// <summary>Active game mode name.</summary>
        public string GameMode;

        /// <summary>Current lobby status (e.g., "waiting", "in_game", "full").</summary>
        public string Status;

        public LobbyListingData() { }
    }
}
