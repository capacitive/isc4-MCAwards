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

namespace isc4_MCAwards
{
    class Program
    {
        static string[] Scopes = { SheetsService.Scope.Spreadsheets, DriveService.Scope.Drive, DriveService.Scope.DriveFile };
        static string ApplicationName = "ISCCompTeamStats";

        static void Main(string[] args)
        {
            var service = AuthorizeGoogleApp();

            // 1. this code block populates a list of MatchEvents by transforming MWO lobby 'matches' into 'drops' - 5 per MatchEvent 
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
                    // if (idx == 4)
                    // {
                    //     break;
                    // }

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

                        using(var db = new LiteDatabase(@"E:\_databases\LiteDB\ISC4-MatchStats.db"))
                        {
                            var collection = db.GetCollection<Match>("MatchEvents");
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
                    }

                    matchEvents.Add(matchEvent);

                    idx++;
                }
            }
            else
            {
                Console.WriteLine("No data found.");
            }

            SortedSet<TeamMember> _pilotTopKillsPerMatch = new SortedSet<TeamMember>(new KillsComparer());
            SortedSet<TeamMember> _pilotTopKillAssistsPerMatch = new SortedSet<TeamMember>(new KillAssistsComparer());
            SortedSet<TeamMember> _pilotTopKMDDPerMatch = new SortedSet<TeamMember>(new KMDDComparer());
            SortedSet<TeamMember> _pilotTopComponentsDestroyedPerMatch  = new SortedSet<TeamMember>(new ComponentsDestroyedComparer());
            SortedSet<TeamMember> _pilotTopDamagePerMatch = new SortedSet<TeamMember>(new DamageComparer());

            SortedSet<TeamMember> _pilotTeamDamagePerDrop = new SortedSet<TeamMember>(new TeamDamageComparer());
            SortedSet<TeamMember> _pilotTopComponentsDestroyedPerDrop  = new SortedSet<TeamMember>(new ComponentsDestroyedComparer());
            SortedSet<TeamMember> _pilotTopDamagePerDrop = new SortedSet<TeamMember>(new DamageComparer());

            List<TeamMember> matchPilotForCSV = new List<TeamMember>();

            Console.WriteLine("Drop Pilot Name,Components Destroyed,Damage,Team Damage");
                
            foreach (var match in matchEvents)
            {
                // 2. this function call populates top pilot stats within each match:       
                match.CalculatePerMatchStats();

                //add latest top pilot to the global match list (will auto-sort):
                _pilotTopKillsPerMatch.Add(match.pilotTopKills.Max);
                _pilotTopKillAssistsPerMatch.Add(match.pilotTopKillAssists.Max);
                _pilotTopKMDDPerMatch.Add(match.pilotTopKMDD.Max);
                _pilotTopComponentsDestroyedPerMatch.Add(match.pilotTopComponentsDestroyed.Max);
                _pilotTopDamagePerMatch.Add(match.pilotTopDamage.Max);

                matchPilotForCSV.AddRange(match.BothTeamsPerMatch.MembersC.Values);

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
                }
            }

            Console.WriteLine("Match Pilot Name,Kills,Kill Assists,KMDD,Components Destroyed,Damage");
            foreach (var matchPilot in matchPilotForCSV)
            {
                Console.WriteLine($"{matchPilot.Name},{matchPilot.Stats.Kills},{matchPilot.Stats.KillAssists},{matchPilot.Stats.KMDD},{matchPilot.Stats.ComponentsDestroyed},{matchPilot.Stats.Damage}");
            }

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

            #region commented
            // Console.WriteLine("------------------------------------------------------------------------");
            // Console.WriteLine("match stats");
            // Console.WriteLine("------------------------------------------------------------------------");
            // Console.WriteLine("Name,Kills");
            // foreach (var item in _pilotTopKillsPerMatch)
            // {
            //     Console.WriteLine($"{item.Name},{item.Stats.Kills}");
            // }
            // Console.WriteLine("Name,Assists");
            // foreach (var item in _pilotTopKillAssistsPerMatch)
            // {
            //     Console.WriteLine($"{item.Name},{item.Stats.Kills},{item.Stats.KillAssists}");
            // }
            // Console.WriteLine("Name,KMDD");
            // foreach (var item in _pilotTopKMDDPerMatch)
            // {
            //     Console.WriteLine($"{item.Name},{item.Stats.KMDD}");
            // }
            // Console.WriteLine("Name,CD");
            // foreach (var item in _pilotTopComponentsDestroyedPerMatch)
            // {
            //     Console.WriteLine($"{item.Name},{item.Stats.ComponentsDestroyed}");
            // }
            // Console.WriteLine("Name,Damage");
            // foreach (var item in _pilotTopDamagePerMatch)
            // {
            //     Console.WriteLine($"{item.Name},{item.Stats.Damage}");
            // }

            // var killsAndKillAssists = 
            // _pilotTopKillsPerMatch.Zip(_pilotTopKillAssistsPerMatch, (kills, killAssists) => 
            // "k,"+ kills.Name + "," + kills.Stats.Kills + ",ka," + killAssists.Name + "," + killAssists.Stats.KillAssists
            // );

            //  var KMDDAndCD = 
            // _pilotTopKMDDPerMatch.Zip(_pilotTopComponentsDestroyedPerMatch, (kmdd, cd) => 
            // "kmdd," + kmdd.Name + "," + kmdd.Stats.KMDD + ",cd," + cd.Name + "," + cd.Stats.ComponentsDestroyed
            // );

            // var killsAndKillAssistsAndKMDDAndCD = 
            // killsAndKillAssists.Zip(KMDDAndCD, (kk, kc) => 
            // kk + "," + kc);

            // var killsAndKillAssistsAndKMDDAndCDAndDamage = 
            // _pilotTopDamagePerMatch.Zip(killsAndKillAssistsAndKMDDAndCD, (dmg, rest) => 
            // "dmg," + dmg.Name + "," + dmg.Stats.Damage + "," + rest);
            
            // foreach (var printcsv in killsAndKillAssistsAndKMDDAndCDAndDamage)
            // {
            //     Console.WriteLine($"{printcsv}");
            // }
            #endregion commented
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
    }
}
