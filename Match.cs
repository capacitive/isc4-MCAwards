using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace isc4_MCAwards
{
    //only for storing raw API data
    public class Match
    {
        public string MatchID { get; set; }

        [JsonPropertyName("MatchDetails")]
        public MatchDetails MatchDetails { get; set; }

        [JsonPropertyName("UserDetails")]
        public List<UserDetail> UserDetails { get; set; }
    }
}
