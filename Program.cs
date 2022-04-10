﻿using System;
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
                    
                    Thread.Sleep(2000);
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

            String spreadsheetId = "1YvnL79jnWfJLUeeffWjUkWqKL1Su02OT5-rb-30Jmeo";
            String range = "Matches!A2:J";
            SpreadsheetsResource.ValuesResource.GetRequest request = service.Spreadsheets.Values.Get(spreadsheetId, range);

            ValueRange response = request.Execute();
            IList<IList<Object>> values = response.Values;

            List<MatchEvent> matchEvents = new List<MatchEvent>();

            if (values != null && values.Count > 0)
            {
                int idx = 0;
                foreach (var row in values)
                {
                    if (idx == 2)
                    {
                        break;
                    }
                    // Print columns B and C, which correspond to indices 1 and 2.
                    //Console.WriteLine("{0}, {1}", row[1], row[2]);
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
                        match.MatchID = matchID;
                        matchEvent.Drops.Add(match);
                    }

                    matchEvents.Add(matchEvent);

                    idx++;
                }
            }
            else
            {
                Console.WriteLine("No data found.");
            }

            foreach (var match in matchEvents)
            {
                Console.WriteLine("------------------------------------------------------------------------");
                Console.WriteLine($"MATCH ID: {match.ID} | Team 1 (won): {match.Team1.Name} vs. Team 2 (lost): {match.Team2.Name}");
                Console.WriteLine("------------------------------------------------------------------------");
                Console.WriteLine($"Match 1 ID: {match.Drops[0].MatchID}");
                Console.WriteLine($"Match 2 ID: {match.Drops[1].MatchID}");
                Console.WriteLine($"Match 3 ID: {match.Drops[2].MatchID}");
                Console.WriteLine($"Match 4 ID: {match.Drops[3].MatchID}");
                Console.WriteLine($"Match 5 ID: {match.Drops[4].MatchID}");
            }

            List<UserDetail> pilots = new List<UserDetail>();
            //string[] matchIDs = {"309106145869071", "6783397991574", "620119893309", "6246527077046", "19174378712799", "20286775246421", "18886615903009", "21957517530931", "21038394525869", "22262460210155", "22434258902707", "23812943410214", "21566675505501", "23963267266235", "19977537599968", "28395673540051", "32995583545997", "94959077180681", "95637682016183", "95337034304250", "96616934563289", "82898808927605", "107633525747387", "104768782548077", "107444547185476", "109175419014092", "108750217249690", "109987167837207", "110171851431815", "120157650460482", "110227686006980", "108342195354518", "82881629058306", "309106145869071", "6783397991574", "620119893309", "6246527077046", "19174378712799", "20286775246421", "18886615903009", "21957517530931", "21038394525869", "22262460210155", "22434258902707", "23812943410214", "21566675505501", "23963267266235", "19977537599968", "28395673540051", "32995583545997", "94959077180681", "95637682016183", "95337034304250", "96616934563289", "82898808927605", "107633525747387", "104768782548077", "107444547185476", "109175419014092", "108750217249690", "109987167837207", "110171851431815", "120157650460482", "110227686006980", "108342195354518", "82881629058306", "309106145869071", "6783397991574", "620119893309", "6246527077046", "19174378712799", "20286775246421", "18886615903009", "21957517530931", "21038394525869", "22262460210155", "22434258902707", "23812943410214", "21566675505501", "23963267266235", "19977537599968", "28395673540051", "32995583545997", "94959077180681", "95637682016183", "95337034304250", "96616934563289", "82898808927605", "107633525747387", "104768782548077", "107444547185476", "109175419014092", "108750217249690", "109987167837207", "110171851431815", "120157650460482", "110227686006980", "108342195354518", "82881629058306", "8119135329525", "8668891145644", "8346768597165", "11550814214672", "3295887120650", "3557880039710", "5323111606024", "86974735456672", "91475861195741", "93915402626389", "96131605757372", "95985576868906", "97944081961054", "105790987241564", "93468726026472", "115347289511372", "94478043343775", "44617768525562", "46786727018940", "56338734345433", "58932894602571", "60711011070471", "61746098192911", "60556392247256", "57506965454612", "309037426391915", "6959491651405", "6641664070213", "4996691587638", "20235235638686", "19041234726188", "20772106552390", "21532315766953", "20544473284913", "22850870732148", "22769266353137", "23349086940412", "21643984917172", "23035554326562", "20505818579163", "24534497919023", "33309116160031", "94787278488214", "95448703454329", "95461588356288", "96793028223082", "83186571737859", "108629958164771", "105722265292405", "106529719147619", "108947785746304", "110459614242216", "109424527118488", "109965693000548", "120819075427464", "110798916660172", "107508971695243", "83255291214909", "27605400566530", "27687004945569", "32024921951626", "38329933990390", "46675055508005", "48813949232529", "49587043349194", "47525459037688", "49230561062095", "50321482760211", "50171158904204", "51614267922390", "47881941325001", "50793929165011", "53096031646129", "54397406742859", "103441638711597", "105142445770762", "113521927043097", "113358718284808", "75086264370047", "122472638956922", "123061049479198", "124675957189994", "125238597908737", "123593625426251", "125384626797541", "126557152875066", "127665254442628", "126140541045266", "5632349174127", "1414691348159", "5245802194412", "5855687553048", "6680321278200", "7543609709154", "7719703369144", "8187854806519", "8754790491911", "8402603172194", "12916613825526", "3347426728454", "3549290105067", "5559334808349", "87352692580007", "91965487468866", "94276179880304", "95809483209307", "96105835953522", "98141650457168", "105297065999772", "93434366287956", "116395261534425", "95139468309272", "44265581205436", "46868331397927", "56673741796023", "58026656499401", "60874219828418", "62454767799316", "60921464468800", "57803318199224", "309677376521984", "6749038253030", "6285181782903", "6590124462390", "19646825117015", "20531588382999", "20608897794549", "19354767339792", "21686934590278", "22743496549277", "23258892626828", "24268209945634", "22558812954755", "24057756547190", "20583127990711", "28198105042385", "31707093350992", "95328444369644", "95826660577833", "95727876329679", "96874632602029", "83182276770518", "108793166922918", "105975668364055", "107886928818919", "109218368687275", "110493973980769", "110528333719353", "102492449867199", "120093225950796", "111370147312921", "108685792739916", "83444269776801", "27042759848286", "27540976056883", "32102231363887", "37999221506762", "46559091390319", "49148956683075", "49565568512672", "50038014917369", "50020835048069", "49028697598309", "50682260014806", "51438174262391", "50076669623139", "52091009294757", "53100326613449", "55264990141353", "104399416424405", "105322834398288", "113302883709530", "114849071944258", "113998668415401", "122834416211433", "123340222354670", "124860640784667", "124718906863165", "124813396144111", "125612260065209", "126256505162876", "128442643526813", "127016714377886", "5451960546707", "3729678732386", "4202125136939", "5335996508041", "7586559382273", "7225782127667", "8436962910787", "8316703825937", "8488502518510", "12190764346088", "3781218427072", "3721088797779", "43696228862179", "87210958658765", "92416459036122", "94460863474538", "96518152815000", "96483793076575", "98807370389740", "103613438810741", "93670589489844", "116661549507524", "94658431970649", "45206179047884", "46507554143603", "56660856894082", "59925032052226", "61007363815062", "61471220284973", "60934349370753", "58078196107193", "309789045672251", "7693931062029", "7440527990555", "405371528508", "21107114002947", "21257437858924", "21360517074394", "22253870275554", "21412056682198", "23177288247868", "23439281253960", "13552266493310", "22193740733137", "23864483017999", "21150063676037", "28541702429618", "33592584002821", "95646271950797", "95985574368429", "96101538485867", "97256884692783", "82885924025609", "108402324896942", "105881179083079", "108015777838443", "107173964244709", "110674362607995", "109450296922372", "111395917116774", "121209917453141", "112087406854330", "108780282020899", "83573118796275", "27463666645198", "27394947168151", "32256850188675", "38617696800705", "46610630998140", "49097417075340", "49655762826230", "50007950146034", "49677237662802", "50742389557248", "50407382106525", "51652922628316", "50437446877698", "52159728771841", "53108916548047", "53959320076364", "104382236555119", "104171783156441", "113079545408939", "114771762532409", "114398100375781", "123331632420062", "123413236798979", "125028144510164", "125436166405292", "125023849542867", "125071094183385", "125938677581366", "128425463657582", "127171333201267", "5920111984489", "3562175007007", "5628054285483", "6048961082357", "7771242976931", "7161357617857", "8209329643067", "8484207551198", "8810625066953", "8900819380481", "12491412059201", "3626599603799", "3880002588396", "6229349709648", "86910310947069", "92253250278443", "94838820597632", "96586872291922", "96324879286169", "99112313068579", "106074455084751", "94065726482186", "116665844474841", "95680634190065", "45270603557584", "47340777802284", "56699511599907", "59959391790716", "61312306494314", "62746825576570", "61209227278801", "57747483624119", "309990909135958", "7681046160086", "7470592761784", "7135585311287", "21528020799625", "21085639166403", "21875913152048", "22206625635110", "22013352105958", "23383446678920", "23709864194644", "24474368376670", "14248051200958", "23666914521525", "21545200668892", "26286844583324", "34103685113020", "95852430381727", "96372121426555", "95096516134739", "97282654496766", "83555938927035", "109145354242823", "105404437711148", "108784576988239", "109368692543353", "111202643587786", "110751672019687", "111838298750108", "121205622485819", "112177601167874", "108492519210622", "83895241344710", "27991947624961", "27833033834283", "32338454568364", "38493142748466", "45644263350190", "48788179428636", "50179748838851", "49904870930479", "50454626747020", "50540526093328", "50686554982126", "52430311712614", "50677965047505", "51605677987751", "51073102040625", "55191975697006", "104781668515860", "103690746816402", "113912769069005", "114849071944258", "114574194035749", "123177013596798", "123683819739848", "125032439477469", "124796216274873", "125135518693157", "125994512156407", "125212828104849", "128391103919043", "126436893790191", "6504227539391", "4296614417846", "5748313370213", "5155607880781", "8033235983217", "7917271865672", "8024646048602", "7775537944239", "8681776047572", "8698955916833", "12276663692572", "4305204439617", "4081866052152", "6143450636290", "87541671141701", "93335582039927", "94563942689926", "97213937518936", "96702836409280", "99485975224381", "105477454627289", "94495223213017", "116833348199931", "95852432882370", "44055127806812", "47757389631570", "57232087546631", "58773980812034", "60998773880438", "61557119631183", "60985888978503", "58331599178676"};
            
            // foreach (var item in matchIDs)
            // {
            //     Match match = GetMatchData(item);
            //     if(match != null)
            //     {
            //         foreach (var user in match.UserDetails)
            //         {
            //             if(!user.IsSpectator) 
            //             {
            //                 pilots.Add(user);
            //             }
            //         }
            //     }
            // }

            // foreach (var pilot in pilots)
            // {
            //     Console.WriteLine($"Pilot: {pilot.Username} | Unit: {pilot.UnitTag}"); 
            // }
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
