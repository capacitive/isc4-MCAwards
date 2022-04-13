using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Converters;
using LiteDB;

namespace isc4_MCAwards
{
    /*
    Tournament --> Teams --> Matches --> MatchDetails + UserDetails
    */
    public class Tournament 
    {
        [BsonId]
        public int ID { get; set; }    
        public string Name {get; set;}
        public int Year {get; set;}
        
        [JsonConverter(typeof(IsoDateTimeConverter))]
        public DateTime StartDate {get; set;}

        [JsonConverter(typeof(IsoDateTimeConverter))]
        public DateTime EndDate {get; set;}
        public List<MatchEvent> Matches { get; set; }

        public Tournament()
        {
            Matches = new List<MatchEvent>();            
        }
    }
}
