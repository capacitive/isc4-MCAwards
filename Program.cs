using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Net.Http;
using Newtonsoft.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.Sheets.v4;
using LiteDB;

/* how to query NoSQL LiteDB:

SELECT $.TeamMembers[@.Name = 'justcallme A S H'].Name, $.TeamMembers[@.Name = 'justcallme A S H'].Stats.Damage
FROM MatchEvents 
WHERE COUNT($.TeamMembers[@.Name = 'justcallme A S H']) > 0

SELECT $.TeamMembers[*].Name
FROM MatchEvents
WHERE COUNT($.TeamMembers[@.Name = 'justcallme A S H']) > 0

SELECT $.MatchDetails, $.UserDetails[@.Username = 'justcallme A S H']
FROM apidata 
WHERE 
--COUNT($.UserDetails[@.Username = 'justcallme A S H']) > 0
--AND ($.MatchDetails.Map = "MiningCollective" OR $.MatchDetails.Map = "AlpinePeaks")

*/
namespace isc4_MCAwards
{
    class Program
    {
        static List<TeamMember> matchPilotForCSV = new List<TeamMember>();
        static List<TeamMember> matchPilotForKillsCSV = new List<TeamMember>();
        static List<TeamMember> matchPilotForKillAssistsCSV = new List<TeamMember>();
        static List<TeamMember> matchPilotForKMDDCSV = new List<TeamMember>();
        static List<TeamMember> matchPilotForComponentsDestroyedCSV = new List<TeamMember>();
        static List<TeamMember> matchPilotForDamageCSV = new List<TeamMember>();
        static string[] Scopes = { SheetsService.Scope.Spreadsheets, DriveService.Scope.Drive, DriveService.Scope.DriveFile };
        static string ApplicationName = "ISCCompTeamStats";

        static void Main(string[] args)
        {
            var service = AuthorizeGoogleApp();

            if(service == null) {return;}

            // 1. this code block populates a list of MatchEvents by transforming MWO lobby 'matches' into 'drops' - 5 per MatchEvent 
            //   (MatchEvents can be put inside a Tournament class and stored in the LiteDB database):
            string spreadsheetId = "1YvnL79jnWfJLUeeffWjUkWqKL1Su02OT5-rb-30Jmeo";
            string range = "Matches!A2:J";
            SpreadsheetsResource.ValuesResource.GetRequest request = service.Spreadsheets.Values.Get(spreadsheetId, range);

            ValueRange response = null;
            IList<IList<Object>> values = null;
            try {
                 response = request.Execute();
                 values = response.Values;
            } catch (Exception exc) {
                Console.WriteLine($"Exception thrown during GoogleSheets GetRequest: {exc.InnerException}");
            }

            try
            {
                using(var db = new LiteDatabase(@"E:\_databases\LiteDB\ISC4-MatchStats.db"))
                {
                    try
                    {
                        List<MatchEvent> matchEvents = new List<MatchEvent>();

                        //PGI API or cached Match from LiteDB:
                        if (values != null && values.Count > 0)
                        {
                            int idx = 1;
                            foreach (var row in values)
                            {
                                MatchEvent matchEvent = new MatchEvent 
                                { 
                                    Id = idx,
                                    Date = DateTime.Parse(row[0].ToString()),
                                    Team1 = new Team { Name = row[1].ToString() },
                                    Team2 = new Team { Name = row[2].ToString() }
                                };

                                for (int i = 5; i < 10; i++)
                                {
                                    string matchID = row[i].ToString();          
                                    var collection = db.GetCollection<Match>("apidata");
                                    Match cachedMatch = collection.FindOne(x => x.MatchID == matchID);

                                    if(cachedMatch == null && matchID != "0")
                                    {
                                        Match match = GetMatchData(matchID);      
                                        if(match != null)
                                        {
                                            match.MatchID = matchID;
                                            matchEvent.Drops.Add(match);
                                            collection.Insert(match);
                                        } 
                                    }
                                    else
                                    {
                                        matchEvent.Drops.Add(cachedMatch);
                                    }
                                } 

                                matchEvents.Add(matchEvent);
                                idx++;            
                            }                  
                        }
                        else
                        {
                            Console.WriteLine("No spreadsheet data found.");
                        }

                        //global match and drop stats containers:
                        SortedSet<TeamMember> _pilotTopKillsPerMatch = new SortedSet<TeamMember>(new KillsComparer());
                        SortedSet<TeamMember> _pilotTopKillAssistsPerMatch = new SortedSet<TeamMember>(new KillAssistsComparer());
                        SortedSet<TeamMember> _pilotTopKMDDPerMatch = new SortedSet<TeamMember>(new KMDDComparer());
                        SortedSet<TeamMember> _pilotTopComponentsDestroyedPerMatch  = new SortedSet<TeamMember>(new ComponentsDestroyedComparer());
                        SortedSet<TeamMember> _pilotTopDamagePerMatch = new SortedSet<TeamMember>(new DamageComparer());

                        SortedSet<TeamMember> _pilotTeamDamagePerDrop = new SortedSet<TeamMember>(new TeamDamageComparer());
                        SortedSet<TeamMember> _pilotTopComponentsDestroyedPerDrop  = new SortedSet<TeamMember>(new ComponentsDestroyedComparer());
                        SortedSet<TeamMember> _pilotTopDamagePerDrop = new SortedSet<TeamMember>(new DamageComparer());

                        //CSV stuff:        
                        Console.WriteLine("Drop Pilot Name,Components Destroyed,Damage,Team Damage");

                        //for the document store:
                        Tournament isc4 = new Tournament
                        {
                            Name = "ISC 4 (2022)",
                            StartDate = new DateTime(2022,1,11),
                            EndDate = new DateTime(2022,3,28),
                            Year = 2022
                        };

                        var matchEventsLDB = db.GetCollection<MatchEvent>("MatchEvents");
                        var teamMembersLDB = db.GetCollection<TeamMember>("TeamMembers");
                        var pilotStatsLDB = db.GetCollection<PilotStats>("PilotStats");

                        // BsonMapper.Global.Entity<Tournament>()
                        // .DbRef(x => x.Matches, "MatchEvents");

                        // BsonMapper.Global.Entity<MatchEvent>()
                        // .DbRef(x => x.TeamMembers, "TeamMembers");
                            
                        //the Big Kahuna - iterates all MatchEvents and calculates stats:
                        foreach (var match in matchEvents)
                        {
                            // this function call populates top pilot stats within each match:       
                            match.CalculatePerMatchStats();

                            //add latest top pilot to the global match list (will auto-sort):
                            if(match.pilotTopKills.Max != null) 
                            {
                                _pilotTopKillsPerMatch.Add(match.pilotTopKills.Max);
                                InsertMatchPilotHighestScoreIntoCorrespondingList(match, "kills");
                            }
                            
                            
                            if(match.pilotTopKillAssists.Max != null) {
                                _pilotTopKillAssistsPerMatch.Add(match.pilotTopKillAssists.Max);
                                InsertMatchPilotHighestScoreIntoCorrespondingList(match, "assists");
                            }

                            if(match.pilotTopKMDD.Max != null) 
                            {
                                _pilotTopKMDDPerMatch.Add(match.pilotTopKMDD.Max);
                                InsertMatchPilotHighestScoreIntoCorrespondingList(match, "kmdd");
                            }

                            if(match.pilotTopComponentsDestroyed.Max != null) 
                            {
                                _pilotTopComponentsDestroyedPerMatch.Add(match.pilotTopComponentsDestroyed.Max);
                                InsertMatchPilotHighestScoreIntoCorrespondingList(match, "components");
                            }

                            if(match.pilotTopDamage.Max != null) {
                                _pilotTopDamagePerMatch.Add(match.pilotTopDamage.Max);
                                InsertMatchPilotHighestScoreIntoCorrespondingList(match, "damage");
                            }

                            // another 'batch' of pilots goes into the global list (matchPilotForCSV).
                            // for reference purposes, this will produce duplicates because top-ranked pilots span matches:

                            // Console.WriteLine("------------------------------------------------------------------------");
                            // Console.WriteLine($"MATCH ID: {match.ID} | Team 1 (won): {match.Team1.Name} vs. Team 2 (lost): {match.Team2.Name}");
                            // Console.WriteLine("------------------------------------------------------------------------");
                            // Console.WriteLine($"Top Pilot for Kills: {match.pilotTopKills.Max.Name} ({match.pilotTopKills.Max.Stats.Kills})");
                            // Console.WriteLine($"Top Pilot for Kill Assists: {match.pilotTopKillAssists.Max.Name} ({match.pilotTopKillAssists.Max.Stats.KillAssists})");
                            // Console.WriteLine($"Top Pilot for KMDD: {match.pilotTopKMDD.Max.Name} ({match.pilotTopKMDD.Max.Stats.KMDD})");
                            // Console.WriteLine($"Top Pilot for ComponentsDestroyed: {match.pilotTopComponentsDestroyed.Max.Name} ({match.pilotTopComponentsDestroyed.Max.Stats.ComponentsDestroyed})");
                            // Console.WriteLine($"Top Pilot for Damage: {match.pilotTopDamage.Max.Name} ({match.pilotTopDamage.Max.Stats.Damage})");

                            //3. this function call populates pilot stats for this match's drops:
                            match.PopulatePerDropStats();

                            //add pilot stats for this match's drops to the global drop list (will auto-sort):
                            foreach (var dropPilot in match.BothTeamsPerDrop.MembersG)
                            {
                                _pilotTeamDamagePerDrop.Add(dropPilot);
                                _pilotTopComponentsDestroyedPerDrop.Add(dropPilot);
                                _pilotTopDamagePerDrop.Add(dropPilot);
                                Console.WriteLine($"{dropPilot.Name},{dropPilot.Stats.ComponentsDestroyed},{dropPilot.Stats.Damage},{dropPilot.Stats.TeamDamage}");

                                match.TeamMembers.Add(dropPilot);
                                teamMembersLDB.Insert(dropPilot);
                            }  
                            
                            try {
                                matchEventsLDB.Insert(match);      
                            } catch (Exception exc) {
                                Console.WriteLine($"LiteDB INSERT exception thrown: {exc}");
                            }
                        }

                        /*finally, add Tournament with MatchEvents to LiteDB:
                        1. create the Bson mappings for LiteDB
                        2. add the matches to the tournament
                        3. insert tournament
                        */

                        isc4.Matches.AddRange(matchEvents);

                        // foreach (var item in isc4.Matches)
                        // {
                        //     foreach (var itemw in item.TeamMembers)
                        //     {
                        //         pilotStatsLDB.Insert(itemw.Stats);                  
                        //     }
                        // }

                        var tournaments = db.GetCollection<Tournament>("Tournaments");
                        if(!tournaments.Exists(t => t.Name == "ISC 4 (2022)"))
                        {
                            tournaments.Insert(isc4);
                        }              

                        #region CSV
                        Console.WriteLine("[START Kills CSV]");
                        Console.WriteLine("Name,Kills,Kill Assists,KMDD,Components Destroyed,Damage");
                        matchPilotForKillsCSV.Sort(new KillsComparer());
                        matchPilotForKillsCSV.Reverse();
                        foreach (var matchPilot in matchPilotForKillsCSV)
                        {
                            if(matchPilot == null) {
                                continue;
                            }

                            var matchPilotName = matchPilot.Name == null ? "NULL" : matchPilot.Name;
                            var matchPilotStatsKills = matchPilot.Stats == null ? 0 : matchPilot.Stats.Kills;
                            var matchPilotStatsKillAssists = matchPilot.Stats == null ? 0 : matchPilot.Stats.KillAssists;
                            var matchPilotStatsKMDD = matchPilot.Stats == null ? 0 : matchPilot.Stats.KMDD;
                            var matchPilotStatsComponentsDestroyed = matchPilot.Stats == null ? 0 : matchPilot.Stats.ComponentsDestroyed;
                            var matchPilotStatsDamage = matchPilot.Stats == null ? 0 : matchPilot.Stats.Damage;

                            Console.WriteLine($"{matchPilotName},{matchPilotStatsKills},{matchPilotStatsKillAssists},{matchPilot.Stats.KMDD},{matchPilotStatsComponentsDestroyed},{matchPilotStatsDamage}");
                        }

                        Console.WriteLine("[START Kill Assists CSV]");
                        Console.WriteLine("Name,Kills,Kill Assists,KMDD,Components Destroyed,Damage");
                        matchPilotForKillAssistsCSV.Sort(new KillAssistsComparer());
                        matchPilotForKillAssistsCSV.Reverse();
                        foreach (var matchPilot in matchPilotForKillAssistsCSV)
                        {
                            if(matchPilot == null) {
                                continue;
                            }

                            var matchPilotName = matchPilot.Name == null ? "NULL" : matchPilot.Name;
                            var matchPilotStatsKills = matchPilot.Stats == null ? 0 : matchPilot.Stats.Kills;
                            var matchPilotStatsKillAssists = matchPilot.Stats == null ? 0 : matchPilot.Stats.KillAssists;
                            var matchPilotStatsKMDD = matchPilot.Stats == null ? 0 : matchPilot.Stats.KMDD;
                            var matchPilotStatsComponentsDestroyed = matchPilot.Stats == null ? 0 : matchPilot.Stats.ComponentsDestroyed;
                            var matchPilotStatsDamage = matchPilot.Stats == null ? 0 : matchPilot.Stats.Damage;

                            Console.WriteLine($"{matchPilotName},{matchPilotStatsKills},{matchPilotStatsKillAssists},{matchPilot.Stats.KMDD},{matchPilotStatsComponentsDestroyed},{matchPilotStatsDamage}");
                        }

                        Console.WriteLine("[START KMDD CSV]");
                        Console.WriteLine("Name,Kills,Kill Assists,KMDD,Components Destroyed,Damage");
                        matchPilotForKMDDCSV.Sort(new KMDDComparer());
                        matchPilotForKMDDCSV.Reverse();
                        foreach (var matchPilot in matchPilotForKMDDCSV)
                        {
                            if(matchPilot == null) {
                                continue;
                            }

                            var matchPilotName = matchPilot.Name == null ? "NULL" : matchPilot.Name;
                            var matchPilotStatsKills = matchPilot.Stats == null ? 0 : matchPilot.Stats.Kills;
                            var matchPilotStatsKillAssists = matchPilot.Stats == null ? 0 : matchPilot.Stats.KillAssists;
                            var matchPilotStatsKMDD = matchPilot.Stats == null ? 0 : matchPilot.Stats.KMDD;
                            var matchPilotStatsComponentsDestroyed = matchPilot.Stats == null ? 0 : matchPilot.Stats.ComponentsDestroyed;
                            var matchPilotStatsDamage = matchPilot.Stats == null ? 0 : matchPilot.Stats.Damage;

                            Console.WriteLine($"{matchPilotName},{matchPilotStatsKills},{matchPilotStatsKillAssists},{matchPilot.Stats.KMDD},{matchPilotStatsComponentsDestroyed},{matchPilotStatsDamage}");
                        }

                        Console.WriteLine("[START Components CSV]");
                        Console.WriteLine("Name,Kills,Kill Assists,KMDD,Components Destroyed,Damage");
                        matchPilotForComponentsDestroyedCSV.Sort(new ComponentsDestroyedComparer());
                        matchPilotForComponentsDestroyedCSV.Reverse();
                        foreach (var matchPilot in matchPilotForComponentsDestroyedCSV)
                        {
                            if(matchPilot == null) {
                                continue;
                            }

                            var matchPilotName = matchPilot.Name == null ? "NULL" : matchPilot.Name;
                            var matchPilotStatsKills = matchPilot.Stats == null ? 0 : matchPilot.Stats.Kills;
                            var matchPilotStatsKillAssists = matchPilot.Stats == null ? 0 : matchPilot.Stats.KillAssists;
                            var matchPilotStatsKMDD = matchPilot.Stats == null ? 0 : matchPilot.Stats.KMDD;
                            var matchPilotStatsComponentsDestroyed = matchPilot.Stats == null ? 0 : matchPilot.Stats.ComponentsDestroyed;
                            var matchPilotStatsDamage = matchPilot.Stats == null ? 0 : matchPilot.Stats.Damage;

                            Console.WriteLine($"{matchPilotName},{matchPilotStatsKills},{matchPilotStatsKillAssists},{matchPilot.Stats.KMDD},{matchPilotStatsComponentsDestroyed},{matchPilotStatsDamage}");
                        }

                        Console.WriteLine("[START Damage CSV]");
                        Console.WriteLine("Name,Kills,Kill Assists,KMDD,Components Destroyed,Damage");
                        matchPilotForDamageCSV.Sort(new DamageComparer());
                        matchPilotForDamageCSV.Reverse();
                        foreach (var matchPilot in matchPilotForDamageCSV)
                        {
                            if(matchPilot == null) {
                                continue;
                            }

                            var matchPilotName = matchPilot.Name == null ? "NULL" : matchPilot.Name;
                            var matchPilotStatsKills = matchPilot.Stats == null ? 0 : matchPilot.Stats.Kills;
                            var matchPilotStatsKillAssists = matchPilot.Stats == null ? 0 : matchPilot.Stats.KillAssists;
                            var matchPilotStatsKMDD = matchPilot.Stats == null ? 0 : matchPilot.Stats.KMDD;
                            var matchPilotStatsComponentsDestroyed = matchPilot.Stats == null ? 0 : matchPilot.Stats.ComponentsDestroyed;
                            var matchPilotStatsDamage = matchPilot.Stats == null ? 0 : matchPilot.Stats.Damage;

                            Console.WriteLine($"{matchPilotName},{matchPilotStatsKills},{matchPilotStatsKillAssists},{matchPilot.Stats.KMDD},{matchPilotStatsComponentsDestroyed},{matchPilotStatsDamage}");
                        }
                        #endregion CSV

                        //print out the top pilots stats per drop:
                        Console.WriteLine("------------------------------------------------------------------------");
                        Console.WriteLine($"Top drop pilot: {_pilotTeamDamagePerDrop.Max.Name} for team damage: {_pilotTeamDamagePerDrop.Max.Stats.TeamDamage}");   
                        Console.WriteLine($"Top drop pilot: {_pilotTopComponentsDestroyedPerDrop.Max.Name} for components destroyed: {_pilotTopComponentsDestroyedPerDrop.Max.Stats.ComponentsDestroyed}");   
                        Console.WriteLine($"Top drop pilot: {_pilotTopDamagePerDrop.Max.Name} for damage: {_pilotTopDamagePerDrop.Max.Stats.Damage}");

                        //print out the top pilots stats per match:
                        Console.WriteLine("------------------------------------------------------------------------");
                        Console.WriteLine($"Top match pilot: {_pilotTopKillsPerMatch.Max.Name} for kills: {_pilotTopKillsPerMatch.Max.Stats.Kills}");   
                        Console.WriteLine($"Top match pilot: {_pilotTopKillAssistsPerMatch.Max.Name} for kill assists: {_pilotTopKillAssistsPerMatch.Max.Stats.KillAssists}");   
                        Console.WriteLine($"Top match pilot: {_pilotTopKMDDPerMatch.Max.Name} for KMDD: {_pilotTopKMDDPerMatch.Max.Stats.KMDD}");   
                        Console.WriteLine($"Top match pilot: {_pilotTopComponentsDestroyedPerMatch.Max.Name} for components destroyed: {_pilotTopComponentsDestroyedPerMatch.Max.Stats.ComponentsDestroyed}");   
                        Console.WriteLine($"Top match pilot: {_pilotTopDamagePerMatch.Max.Name} for damage: {_pilotTopDamagePerMatch.Max.Stats.Damage}");
                    }
                    catch (Exception exc)
                    {
                        Console.WriteLine($"Inner exception thrown: {exc}");
                    }
                    
                }
            }
            catch //(System.Exception exc)
            {      
                //Console.WriteLine($"Outer exception thrown: {exc}");
            }
        }

        private static void InsertMatchPilotHighestScoreIntoCorrespondingList(MatchEvent match, string statType)
        {
            switch(statType)
            {
                case "kills":
                    var resultK = matchPilotForKillsCSV.FindAll((mp) =>
                    {
                        if (mp.Name == match.pilotTopKills.Max.Name && mp.Stats.Kills < match.pilotTopKills.Max.Stats.Kills)
                        {
                            matchPilotForKillsCSV.Remove(mp);
                            return true;
                        }
                        return false;
                    });

                    matchPilotForKillsCSV.Add(match.pilotTopKills.Max);

                break;
                case "assists":
                    var resultKA = matchPilotForKillAssistsCSV.FindAll((mp) =>
                    {
                        if (mp.Name == match.pilotTopKillAssists.Max.Name && mp.Stats.KillAssists < match.pilotTopKillAssists.Max.Stats.KillAssists)
                        {
                            matchPilotForKillAssistsCSV.Remove(mp);
                            return true;
                        }
                        return false;
                    });

                    matchPilotForKillAssistsCSV.Add(match.pilotTopKillAssists.Max);

                break;
                case "kmdd":
                    var resultKM = matchPilotForKMDDCSV.FindAll((mp) =>
                    {
                        if (mp.Name == match.pilotTopKMDD.Max.Name && mp.Stats.KMDD < match.pilotTopKMDD.Max.Stats.KMDD)
                        {
                            matchPilotForKMDDCSV.Remove(mp);
                            return true;
                        }
                        return false;
                    }); 

                    matchPilotForKMDDCSV.Add(match.pilotTopKMDD.Max);

                break;
                case "components":
                    var resultC = matchPilotForComponentsDestroyedCSV.FindAll((mp) =>
                    {
                        if (mp.Name == match.pilotTopComponentsDestroyed.Max.Name && mp.Stats.ComponentsDestroyed < match.pilotTopComponentsDestroyed.Max.Stats.ComponentsDestroyed)
                        {
                            matchPilotForComponentsDestroyedCSV.Remove(mp);
                            return true;
                        }
                        return false;
                    }); 

                    matchPilotForComponentsDestroyedCSV.Add(match.pilotTopComponentsDestroyed.Max);
                
                break;
                case "damage":
                    var resultD = matchPilotForDamageCSV.FindAll((mp) =>
                    {
                        if (mp.Name == match.pilotTopDamage.Max.Name && mp.Stats.Damage < match.pilotTopDamage.Max.Stats.Damage)
                        {
                            matchPilotForDamageCSV.Remove(mp);
                            return true;
                        }
                        return false;
                    }); 

                    matchPilotForDamageCSV.Add(match.pilotTopDamage.Max);
                
                break;
            }
        }

        private static SheetsService AuthorizeGoogleApp()
        {
            UserCredential credential;

            try {
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
            } catch (Exception exc) {
                Console.WriteLine($"Exception thrown in AuthorizeGoogleApp(): {exc.InnerException}");
                return null;
            }
        }
    
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
            catch (Exception exc) 
            {
                Console.WriteLine($"Exception thrown in GetMatchData({matchID}): {exc.InnerException}");
                return null;
            }    
        }
    }
}
