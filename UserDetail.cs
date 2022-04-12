using System.Text.Json.Serialization;

namespace isc4_MCAwards
{
    public class UserDetail
    {
        [JsonPropertyName("Username")]
        public string Username { get; set; }

        [JsonPropertyName("IsSpectator")]
        public bool IsSpectator { get; set; }

        [JsonPropertyName("Team")]
        public string Team { get; set; }

        [JsonPropertyName("Lance")]
        public string Lance { get; set; }

        [JsonPropertyName("MechItemID")]
        public int MechItemID { get; set; }

        [JsonPropertyName("MechName")]
        public string MechName { get; set; }

        [JsonPropertyName("SkillTier")]
        public int? SkillTier { get; set; }

        [JsonPropertyName("HealthPercentage")]
        public int HealthPercentage { get; set; }

        [JsonPropertyName("Kills")]
        public int Kills { get; set; }

        [JsonPropertyName("KillsMostDamage")]
        public int KillsMostDamage { get; set; }

        [JsonPropertyName("Assists")]
        public int Assists { get; set; }

        [JsonPropertyName("ComponentsDestroyed")]
        public int ComponentsDestroyed { get; set; }

        [JsonPropertyName("MatchScore")]
        public int MatchScore { get; set; }

        [JsonPropertyName("Damage")]
        public int Damage { get; set; }

        [JsonPropertyName("TeamDamage")]
        public int TeamDamage { get; set; }

        [JsonPropertyName("UnitTag")]
        public string UnitTag { get; set; }
    }
}
