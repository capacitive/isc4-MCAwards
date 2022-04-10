using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

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

    public class Team 
    {
        public string Name { get; set; }
        public List<TeamMember> Members;
        public MatchStats Stats { get; set; }
    }

    public class TeamMember
    {
        public string Name { get; set; }
    }

    //this is the Match Day set of drops in the lobby (x5):
    public class MatchEvent
    {
        public int MatchID { get; set; }
        public DateTime Date {get; set;}
        public Team Team1 { get; set; }
        public Team Team2 { get; set; }
        public List<Match> Drops;
    }

    /*
    Single Match 
    ---------------
    Most Damage, Most Component Destruction, Most Kills, Most Kill Assists, Most KMDD

    Single Drop
    -------------
    Most Damage, Most Component Destruction, Most Team Damage                       
    */
    public class MatchStats
    {
        public int Damage { get; set; }
        public int ComponentsDestroyed { get; set; }
        public int Kills { get; set; }
        public int KillAssists { get; set; }       
        public int KMDD { get; set; }   
        public int TeamDamage { get; set; } 
    }

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
    
    public class MatchDetails
    {
        [JsonPropertyName("Map")]
        public string Map { get; set; }

        [JsonPropertyName("ViewMode")]
        public string ViewMode { get; set; }

        [JsonPropertyName("TimeOfDay")]
        public string TimeOfDay { get; set; }

        [JsonPropertyName("GameMode")]
        public string GameMode { get; set; }

        [JsonPropertyName("Region")]
        public string Region { get; set; }

        [JsonPropertyName("MatchTimeMinutes")]
        public string MatchTimeMinutes { get; set; }

        [JsonPropertyName("UseStockLoadout")]
        public bool UseStockLoadout { get; set; }

        [JsonPropertyName("NoMechQuirks")]
        public bool NoMechQuirks { get; set; }

        [JsonPropertyName("NoMechEfficiencies")]
        public bool NoMechEfficiencies { get; set; }

        [JsonPropertyName("WinningTeam")]
        public string WinningTeam { get; set; }

        [JsonPropertyName("Team1Score")]
        public int Team1Score { get; set; }

        [JsonPropertyName("Team2Score")]
        public int Team2Score { get; set; }

        [JsonPropertyName("MatchDuration")]
        public string MatchDuration { get; set; }

        [JsonPropertyName("CompleteTime")]
        public DateTime CompleteTime { get; set; }
    }

    public class UserDetail
    {
        [JsonPropertyName("Username")]
        public string Username { get; set; }

        [JsonPropertyName("IsSpectator")]
        public bool IsSpectator { get; set; }

        [JsonPropertyName("Team")]
        public string Team { get; set; }

        [JsonPropertyName("Lance")]
        public string Lance { get; set; }

        [JsonPropertyName("MechItemID")]
        public int MechItemID { get; set; }

        [JsonPropertyName("MechName")]
        public string MechName { get; set; }

        [JsonPropertyName("SkillTier")]
        public int? SkillTier { get; set; }

        [JsonPropertyName("HealthPercentage")]
        public int HealthPercentage { get; set; }

        [JsonPropertyName("Kills")]
        public int Kills { get; set; }

        [JsonPropertyName("KillsMostDamage")]
        public int KillsMostDamage { get; set; }

        [JsonPropertyName("Assists")]
        public int Assists { get; set; }

        [JsonPropertyName("ComponentsDestroyed")]
        public int ComponentsDestroyed { get; set; }

        [JsonPropertyName("MatchScore")]
        public int MatchScore { get; set; }

        [JsonPropertyName("Damage")]
        public int Damage { get; set; }

        [JsonPropertyName("TeamDamage")]
        public int TeamDamage { get; set; }

        [JsonPropertyName("UnitTag")]
        public string UnitTag { get; set; }
    }

    public class Match
    {
        [JsonPropertyName("MatchDetails")]
        public MatchDetails MatchDetails { get; set; }

        [JsonPropertyName("UserDetails")]
        public List<UserDetail> UserDetails { get; set; }
    }

    class Program
    {
        public static void GetMatchData(string matchID)
        {
            using (HttpClient client = new HttpClient())
            {
                string key = "8lUZPYygrzKCKsbxasAE7FQLuMs7HCdbXGMSlC5f9f3v65XyCN4YUvgT1qXI";
                string url = $"https://mwomercs.com/api/v1/matches/";
                string queryStringPart = $"?api_token={key}";

                string finalURL = url + matchID + queryStringPart;
                
                var response = client.GetAsync(finalURL).Result;
                var json = response.Content.ReadAsStringAsync().Result;
                Match MatchClass = JsonConvert.DeserializeObject<Match>(json);
                Console.WriteLine(MatchClass);
            }
        }

        static void Main(string[] args)
        {
            GetMatchData("309106145869071");
        }
    }
}
