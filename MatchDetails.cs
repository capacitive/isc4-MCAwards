using System;
using System.Text.Json.Serialization;

namespace isc4_MCAwards
{
    public class MatchDetails
    {
        [JsonPropertyName("Map")]
        public string Map { get; set; }

        [JsonPropertyName("ViewMode")]
        public string ViewMode { get; set; }

        [JsonPropertyName("TimeOfDay")]
        public string TimeOfDay { get; set; }

        [JsonPropertyName("GameMode")]
        public string GameMode { get; set; }

        [JsonPropertyName("Region")]
        public string Region { get; set; }

        [JsonPropertyName("MatchTimeMinutes")]
        public string MatchTimeMinutes { get; set; }

        [JsonPropertyName("UseStockLoadout")]
        public bool UseStockLoadout { get; set; }

        [JsonPropertyName("NoMechQuirks")]
        public bool NoMechQuirks { get; set; }

        [JsonPropertyName("NoMechEfficiencies")]
        public bool NoMechEfficiencies { get; set; }

        [JsonPropertyName("WinningTeam")]
        public string WinningTeam { get; set; }

        [JsonPropertyName("Team1Score")]
        public int Team1Score { get; set; }

        [JsonPropertyName("Team2Score")]
        public int Team2Score { get; set; }

        [JsonPropertyName("MatchDuration")]
        public string MatchDuration { get; set; }

        [JsonPropertyName("CompleteTime")]
        public DateTime CompleteTime { get; set; }
    }
}
