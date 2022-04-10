using System.Collections.Generic;

namespace isc4_MCAwards
{
    public class ScoreKeeper
    {
        public List<MatchEvent> MatchEvents { get; set; }

        public KeyValuePair<Team, int> SingleMatchMostKills { get; set; }
        public KeyValuePair<Team, int> SingleMatchMostKillAssists { get; set; }
        public KeyValuePair<Team, int> SingleMatchMostDamage { get; set; }
        public KeyValuePair<Team, int> SingleMatchMostKMDD { get; set; }
        public KeyValuePair<Team, int> SingleMatchMostComponentsDestroyed { get; set; }

        public KeyValuePair<Team, int> SingleDropMostTeamDamage { get; set; } 
        public KeyValuePair<Team, int> SingleDropMostDamage { get; set; }
        public KeyValuePair<Team, int> SingleDropMostComponentsDestroyed { get; set; }

        public ScoreKeeper()
        {
            MatchEvents = new List<MatchEvent>();
        }
        
        public void AddScoresForSingleMatch()
        {

        }

        public void AddScoresForSingleDrop()
        {

        }
    }
}
