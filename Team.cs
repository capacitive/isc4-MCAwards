using System.Collections.Concurrent;

namespace isc4_MCAwards
{
    public class Team 
    {
        public string Name { get; set; }
        public ConcurrentDictionary<string, TeamMember> Members;

        public Team()
        {
            Members = new ConcurrentDictionary<string, TeamMember>();
        }
    }
}
