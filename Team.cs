using System.Collections.Concurrent;
using System.Collections.Generic;

namespace isc4_MCAwards
{
    public class Team 
    {
        public string Name { get; set; }
        public ConcurrentDictionary<string, TeamMember> MembersC;
        public Dictionary<string, TeamMember> MembersG;

        public Team()
        {
            MembersC = new ConcurrentDictionary<string, TeamMember>();
            MembersG = new Dictionary<string, TeamMember>();
        }
    }
}
