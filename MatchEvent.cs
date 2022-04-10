using System;
using System.Collections.Generic;

namespace isc4_MCAwards
{
    //this is the Match Day set of drops in the lobby (x5):
    public class MatchEvent
    {
        public int ID { get; set; }
        public DateTime Date {get; set;}
        public Team Team1 { get; set; }
        public Team Team2 { get; set; }
        public List<Match> Drops;

        public MatchEvent()
        {
            Drops = new List<Match>();
        }

        public bool? AddPilotsAndCalculateStats()
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

                            Team1.Members.AddOrUpdate(user.Username, pilot, (key, current) => {
                                current.Stats.Kills += user.Kills;
                                current.Stats.KMDD += user.KillsMostDamage;
                                current.Stats.ComponentsDestroyed += user.ComponentsDestroyed;
                                current.Stats.KillAssists += user.Assists;             
                                current.Stats.Damage += user.Damage;
                                current.Stats.TeamDamage += user.TeamDamage;
                                return current;
                            });

                            Team2.Members.AddOrUpdate(user.Username, pilot, (key, current) => {
                                current.Stats.Kills += user.Kills;
                                current.Stats.KMDD += user.KillsMostDamage;
                                current.Stats.ComponentsDestroyed += user.ComponentsDestroyed;
                                current.Stats.KillAssists += user.Assists;             
                                current.Stats.Damage += user.Damage;
                                current.Stats.TeamDamage += user.TeamDamage;
                                return current;
                            });
                        }
                    }
                }
            }
            catch
            {
                return false;
            }
            
            return true;
        }
    }
}
