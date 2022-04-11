using System.Collections.Generic;

namespace isc4_MCAwards
{
    public class DamageComparer : IComparer<TeamMember>
    {
        public int Compare(TeamMember x, TeamMember y)
        {
            int result = x.Stats.Damage.CompareTo(y.Stats.Damage);
            return result;
        }
    }

    public class ComponentsDestroyedComparer : IComparer<TeamMember>
    {
        public int Compare(TeamMember x, TeamMember y)
        {
            int result = x.Stats.ComponentsDestroyed.CompareTo(y.Stats.ComponentsDestroyed);
            return result;
        }
    }

    public class KillsComparer : IComparer<TeamMember>
    {
        public int Compare(TeamMember x, TeamMember y)
        {
            int result = x.Stats.Kills.CompareTo(y.Stats.Kills);
            return result;
        }
    }

    public class KillAssistsComparer : IComparer<TeamMember>
    {
        public int Compare(TeamMember x, TeamMember y)
        {
            int result = x.Stats.KillAssists.CompareTo(y.Stats.KillAssists);
            return result;
        }
    }

    public class KMDDComparer : IComparer<TeamMember>
    {
        public int Compare(TeamMember x, TeamMember y)
        {
            int result = x.Stats.KMDD.CompareTo(y.Stats.KMDD);
            return result;
        }
    }

    public class TeamDamageComparer : IComparer<TeamMember>
    {
        public int Compare(TeamMember x, TeamMember y)
        {
            int result = x.Stats.TeamDamage.CompareTo(y.Stats.TeamDamage);
            return result;
        }
    }
}
