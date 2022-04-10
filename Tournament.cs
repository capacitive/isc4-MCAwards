using System;

namespace isc4_MCAwards
{
    /*
    Tournament --> Teams --> Matches --> MatchDetails + UserDetails
    */
    public class Tournament 
    {
        public string Name {get; set;}
        public string Year {get; set;}
        public DateTime StartDate {get; set;}
        public DateTime EndDate {get; set;}
        public ScoreKeeper ScoreKeeper;
    }
}
