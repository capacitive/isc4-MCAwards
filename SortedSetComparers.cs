using System.Collections.Generic;

namespace isc4_MCAwards
{
    public class DamageComparer : IComparer<TeamMember>
    {
        public int Compare(TeamMember x, TeamMember y)
        {
            try
            {
                int result = x.Stats.Damage.CompareTo(y.Stats.Damage);
                return result;
            }
            catch {}
            return 0;
        }
    }

    public class ComponentsDestroyedComparer : IComparer<TeamMember>
    {
        public int Compare(TeamMember x, TeamMember y)
        {
            try
            {
                int result = x.Stats.ComponentsDestroyed.CompareTo(y.Stats.ComponentsDestroyed);
                return result;
            }
            catch {}
            return 0;
        }
    }

    public class KillsComparer : IComparer<TeamMember>
    {
        public int Compare(TeamMember x, TeamMember y)
        {
            try
            {
                int result = x.Stats.Kills.CompareTo(y.Stats.Kills);
                return result;
            }
            catch {}
            return 0;
        }
    }

    public class KillAssistsComparer : IComparer<TeamMember>
    {
        public int Compare(TeamMember x, TeamMember y)
        {
            try
            {
                int result = x.Stats.KillAssists.CompareTo(y.Stats.KillAssists);
                return result;
            }
            catch {}
            return 0;
        }
    }

    public class KMDDComparer : IComparer<TeamMember>
    {
        public int Compare(TeamMember x, TeamMember y)
        {
            try 
            {
                int result = x.Stats.KMDD.CompareTo(y.Stats.KMDD);
                return result;
            }
            catch {}
            return 0;
        }
    }

    public class TeamDamageComparer : IComparer<TeamMember>
    {
        public int Compare(TeamMember x, TeamMember y)
        {
            try
            {
                int result = x.Stats.TeamDamage.CompareTo(y.Stats.TeamDamage);
                return result;
            }
            catch {}
            return 0;
        }
    }
}
