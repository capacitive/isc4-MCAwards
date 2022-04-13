using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Converters;
using LiteDB;

namespace isc4_MCAwards
{
    //MatchEvent contains n drops in the MWO in-game lobby 
    // 1. List<Match> Drops: raw lobby data from API GET (MatchDetails and UserDetails)
    // 2. (Team)BothTeamsPerDrop typed data for n match drop stats
    // 3. (Team)BothTeamsPerMatch: typed data for cumulative match stats
    // 4. List<TeamMember> Pilots: simple list for NoSQL doc store (needs BSON mapping for refs)
    public class MatchEvent
    {
        #region props, fields and ctor
        
        [BsonId]
        public int Id { get; set; } 

        [BsonField]
        [JsonConverter(typeof(IsoDateTimeConverter))]
        public DateTime Date {get; set;}

        public List<TeamMember> TeamMembers {get; set;}

        [BsonField]
        public Team Team1;

        [BsonField]
        public Team Team2;
        
        #region ignored for NoSQL doc store

        [JsonIgnore]
        public List<Match> Drops;
        
        [JsonIgnore]
        public Team BothTeamsPerMatch;
        
        [JsonIgnore]
        public Team BothTeamsPerDrop; 

        [JsonIgnore]
        public SortedSet<TeamMember> pilotTopKills = new SortedSet<TeamMember>(new KillsComparer());
        
        [JsonIgnore]
        public SortedSet<TeamMember> pilotTopKillAssists = new SortedSet<TeamMember>(new KillAssistsComparer());
        
        [JsonIgnore]
        public SortedSet<TeamMember> pilotTopKMDD = new SortedSet<TeamMember>(new KMDDComparer());

        [JsonIgnore]
        public SortedSet<TeamMember> pilotTopComponentsDestroyed  = new SortedSet<TeamMember>(new ComponentsDestroyedComparer());
        
        [JsonIgnore]
        public SortedSet<TeamMember> pilotTopDamage = new SortedSet<TeamMember>(new DamageComparer());
        #endregion ignored for NoSQL doc store

        public MatchEvent()
        {
            Drops = new List<Match>();
            BothTeamsPerMatch = new Team();
            BothTeamsPerDrop = new Team();
            TeamMembers = new List<TeamMember>();
        }

        #endregion props, fields and ctor

        public bool? CalculatePerMatchStats()
        {
            if (Drops != null && Drops.Count == 0)
            {
                return null;
            }

            try
            {
                foreach (var item in Drops)
                {
                    foreach (var user in item.UserDetails)
                    {
                        if(!user.IsSpectator) {
                            TeamMember pilot = new TeamMember{ Name = user.Username };
                            pilot.Stats.Kills = user.Kills;
                            pilot.Stats.KMDD = user.KillsMostDamage;
                            pilot.Stats.ComponentsDestroyed = user.ComponentsDestroyed;
                            pilot.Stats.KillAssists = user.Assists;             
                            pilot.Stats.Damage = user.Damage;
                            pilot.Stats.TeamDamage = user.TeamDamage;

                            //calculate cumulative pilot scores within this match:
                            TeamMember teamMember;
                            if(BothTeamsPerMatch.MembersC.TryRemove(user.Username, out teamMember))
                            {
                                teamMember.Stats.Kills += user.Kills;
                                teamMember.Stats.KMDD += user.KillsMostDamage;
                                teamMember.Stats.ComponentsDestroyed += user.ComponentsDestroyed;
                                teamMember.Stats.KillAssists += user.Assists;             
                                teamMember.Stats.Damage += user.Damage;
                                teamMember.Stats.TeamDamage += user.TeamDamage;

                                var success = BothTeamsPerMatch.MembersC.TryAdd(user.Username, teamMember);
                                //string message = success == true ? $"{user.Username},{user.Damage}" : $"did not update {user.Username}";
                                //Console.WriteLine(message);
                            }
                            else
                            {
                                var success = BothTeamsPerMatch.MembersC.TryAdd(user.Username, pilot);
                                //string message = success == true ? $"{user.Username},{pilot.Stats.Damage}" : $"did not add {user.Username}";
                                //Console.WriteLine(message);
                            }
                        }
                    }
                }

                List<TeamMember> pilots = new List<TeamMember>();
                foreach (var member in BothTeamsPerMatch.MembersC)
                {
                    pilots.Add(member.Value);
                }

                //populate the SortedSets for each stat score:
                foreach (var pilot in pilots)
                {          
                    pilotTopKills.Add(pilot);
                    pilotTopKillAssists.Add(pilot);   
                    pilotTopKMDD.Add(pilot);
                    pilotTopComponentsDestroyed.Add(pilot);
                    pilotTopDamage.Add(pilot);    
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.InnerException);
                return false;
            }
            
            return true;
        }
        public bool? PopulatePerDropStats()
        {
            if (Drops != null && Drops.Count == 0)
            {
                return null;
            }

            try
            {
                foreach (var item in Drops)
                {
                    foreach (var user in item.UserDetails)
                    {
                        if(!user.IsSpectator) {
                            TeamMember pilot = new TeamMember{ Name = user.Username };
                            pilot.Stats.Kills = user.Kills;
                            pilot.Stats.KMDD = user.KillsMostDamage;
                            pilot.Stats.ComponentsDestroyed = user.ComponentsDestroyed;
                            pilot.Stats.KillAssists = user.Assists;             
                            pilot.Stats.Damage = user.Damage;
                            pilot.Stats.TeamDamage = user.TeamDamage;

                            //add pilot stats for each drop (will create duplicates):
                            BothTeamsPerDrop.MembersG.Add(pilot);
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.InnerException);
                return false;
            }
            
            return true;
        }
    
    }
}
