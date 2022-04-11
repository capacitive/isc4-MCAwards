using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Net.Http;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.Sheets.v4;

namespace isc4_MCAwards
{
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
        public string MatchID { get; set; }

        [JsonPropertyName("MatchDetails")]
        public MatchDetails MatchDetails { get; set; }

        [JsonPropertyName("UserDetails")]
        public List<UserDetail> UserDetails { get; set; }
    }
    
    class Program
    {
        static string[] Scopes = { SheetsService.Scope.Spreadsheets, DriveService.Scope.Drive, DriveService.Scope.DriveFile };
        static string ApplicationName = "ISCCompTeamStats";

        public static Match GetMatchData(string matchID)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string key = "8lUZPYygrzKCKsbxasAE7FQLuMs7HCdbXGMSlC5f9f3v65XyCN4YUvgT1qXI";
                    string url = $"https://mwomercs.com/api/v1/matches/";
                    string queryStringPart = $"?api_token={key}";

                    string finalURL = url + matchID + queryStringPart;
                    
                    Thread.Sleep(2500);
                    var response = client.GetAsync(finalURL).Result;
                    var json = response.Content.ReadAsStringAsync().Result;
                    return JsonConvert.DeserializeObject<Match>(json);
                }
            }
            catch 
            {
                return null;
            }    
        }

        static void Main(string[] args)
        {
            var service = AuthorizeGoogleApp();

            //1. populate a list of MatchEvents by transforming MWO lobby 'matches' into 'drops' - 5 per MatchEvent 
            //   (MatchEvents can be put inside a Tournament class and sent to a NoSQL document store for archival purposes):
            string spreadsheetId = "1YvnL79jnWfJLUeeffWjUkWqKL1Su02OT5-rb-30Jmeo";
            string range = "Matches!A2:J";
            SpreadsheetsResource.ValuesResource.GetRequest request = service.Spreadsheets.Values.Get(spreadsheetId, range);

            ValueRange response = request.Execute();
            IList<IList<Object>> values = response.Values;

            List<MatchEvent> matchEvents = new List<MatchEvent>();

            if (values != null && values.Count > 0)
            {
                int idx = 1;
                foreach (var row in values)
                {
                    if (idx == 2)
                    {
                        break;
                    }

                    MatchEvent matchEvent = new MatchEvent 
                    { 
                        ID = idx,
                        Date = DateTime.Parse(row[0].ToString()),
                        Team1 = new Team { Name = row[1].ToString() },
                        Team2 = new Team { Name = row[2].ToString() }
                    };

                    for (int i = 5; i < 10; i++)
                    {
                        string matchID = row[i].ToString();
                        Match match = GetMatchData(matchID);

                        if(match != null)
                        {
                            match.MatchID = matchID;
                            matchEvent.Drops.Add(match);
                        }              
                    }

                    matchEvents.Add(matchEvent);

                    idx++;
                }
            }
            else
            {
                Console.WriteLine("No data found.");
            }

            SortedSet<TeamMember> _pilotTopKills = new SortedSet<TeamMember>(new KillsComparer());
            SortedSet<TeamMember> _pilotTopKillAssists = new SortedSet<TeamMember>(new KillAssistsComparer());
            SortedSet<TeamMember> _pilotTopKMDD = new SortedSet<TeamMember>(new KMDDComparer());
            SortedSet<TeamMember> _pilotTopComponentsDestroyed  = new SortedSet<TeamMember>(new ComponentsDestroyedComparer());
            SortedSet<TeamMember> _pilotTopDamage = new SortedSet<TeamMember>(new DamageComparer());
                
            foreach (var match in matchEvents)
            {
                /*
                2. the function call below gets top pilot scores within each match for:       
                    pilot.Stats.Kills
                    pilot.Stats.KillAssists 
                    pilot.Stats.KMDD
                    pilot.Stats.ComponentsDestroyed          
                    pilot.Stats.Damage
                    pilot.Stats.TeamDamage
                */
                match.CalculatePerMatchStats();

                _pilotTopKills.Add(match.pilotTopKills.Max);
                _pilotTopKillAssists.Add(match.pilotTopKillAssists.Max);
                _pilotTopKMDD.Add(match.pilotTopKMDD.Max);
                _pilotTopComponentsDestroyed.Add(match.pilotTopComponentsDestroyed.Max);
                _pilotTopDamage.Add(match.pilotTopDamage.Max);

                Console.WriteLine("------------------------------------------------------------------------");
                Console.WriteLine($"MATCH ID: {match.ID} | Team 1 (won): {match.Team1.Name} vs. Team 2 (lost): {match.Team2.Name}");
                Console.WriteLine("------------------------------------------------------------------------");
                Console.WriteLine($"Top Pilot for Kills: {match.pilotTopKills.Max.Name} ({match.pilotTopKills.Max.Stats.Kills})");
                Console.WriteLine($"Top Pilot for Kill Assists: {match.pilotTopKillAssists.Max.Name} ({match.pilotTopKillAssists.Max.Stats.KillAssists})");
                Console.WriteLine($"Top Pilot for KMDD: {match.pilotTopKMDD.Max.Name} ({match.pilotTopKMDD.Max.Stats.KMDD})");
                Console.WriteLine($"Top Pilot for ComponentsDestroyed: {match.pilotTopComponentsDestroyed.Max.Name} ({match.pilotTopComponentsDestroyed.Max.Stats.ComponentsDestroyed})");
                Console.WriteLine($"Top Pilot for Damage: {match.pilotTopDamage.Max.Name} ({match.pilotTopDamage.Max.Stats.Damage})");
            }

            //_pilotTopDamage.Sort(new DamageComparer());
            Console.WriteLine("------------------------------------------------------------------------");
            Console.WriteLine($"Top match pilot: {_pilotTopKills.Max.Name} for kills: {_pilotTopKills.Max.Stats.Kills}");   
            Console.WriteLine($"Top match pilot: {_pilotTopKillAssists.Max.Name} for kill assists: {_pilotTopKillAssists.Max.Stats.KillAssists}");   
            Console.WriteLine($"Top match pilot: {_pilotTopKMDD.Max.Name} for KMDD: {_pilotTopKMDD.Max.Stats.KMDD}");   
            Console.WriteLine($"Top match pilot: {_pilotTopComponentsDestroyed.Max.Name} for components destroyed: {_pilotTopComponentsDestroyed.Max.Stats.ComponentsDestroyed}");   
            Console.WriteLine($"Top match pilot: {_pilotTopDamage.Max.Name} for damage: {_pilotTopDamage.Max.Stats.Damage}");         

            //3. compare cumulative pilot scores across all matches:
            // Console.WriteLine("-------------------------------------------------------------------------------------------------");
            // Console.WriteLine($"Top Pilot for Kills in a single match: {_pilotTopKills.Max.Name} ({_pilotTopKills.Max.Stats.Kills})");
            // Console.WriteLine($"Top Pilot for Kill Assists in a single match: {_pilotTopKillAssists.Max.Name} ({_pilotTopKillAssists.Max.Stats.KillAssists})");
            // Console.WriteLine($"Top Pilot for KMDD in a single match: {_pilotTopKMDD.Max.Name} ({_pilotTopKMDD.Max.Stats.KMDD})");
            // Console.WriteLine($"Top Pilot for ComponentsDestroyed in a single match: {_pilotTopComponentsDestroyed.Max.Name} ({_pilotTopComponentsDestroyed.Max.Stats.ComponentsDestroyed})");
            // Console.WriteLine($"Top Pilot for Damage in a single match: {_pilotTopDamage.Max.Name} ({_pilotTopDamage.Max.Stats.Damage})");
        }

        private static SheetsService AuthorizeGoogleApp()
        {
            UserCredential credential;

            using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
                credPath = Path.Combine(credPath, ".credentials/sheets.googleapis.com-dotnet-IscCompTeamStats.json");

                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine($"Credential file saved to: {credPath}");
            }

            var service = new SheetsService(new BaseClientService.Initializer() 
            { 
                HttpClientInitializer = credential, 
                ApplicationName = ApplicationName, 
            });

            return service;
        }
    }
}
