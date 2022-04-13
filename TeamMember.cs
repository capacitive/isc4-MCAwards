using LiteDB;

namespace isc4_MCAwards
{
    public class TeamMember
    {
        [BsonId]
        public int Id { get; set; }      
        public string MatchID { get; set; } 
        public string Name { get; set; }   

        public PilotStats Stats {get;set;}

        public TeamMember()
        {
            Stats = new PilotStats();
        }
    }
}
