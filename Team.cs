using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace isc4_MCAwards
{
    public class Team 
    {
        public string Name { get; set; }
        public ConcurrentDictionary<string, TeamMember> MembersC;
        public List<TeamMember> MembersG;

        public Lookup<string, TeamMember> MembersLookup;

        public Team()
        {
            MembersC = new ConcurrentDictionary<string, TeamMember>();
            MembersG = new List<TeamMember>();
        }
    }
}
