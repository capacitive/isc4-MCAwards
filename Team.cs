using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace isc4_MCAwards
{
    public class Team 
    {
        public string Name { get; set; }

        [JsonIgnore]
        public ConcurrentDictionary<string, TeamMember> MembersC;
        
        [JsonIgnore]
        public List<TeamMember> MembersG;

        public Team()
        {
            MembersC = new ConcurrentDictionary<string, TeamMember>();
            MembersG = new List<TeamMember>();
        }
    }
}
