using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace isc4_MCAwards
{
    public class Match
    {
        public string MatchID { get; set; }

        [JsonPropertyName("MatchDetails")]
        public MatchDetails MatchDetails { get; set; }

        [JsonPropertyName("UserDetails")]
        public List<UserDetail> UserDetails { get; set; }
    }
}
