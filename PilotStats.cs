namespace isc4_MCAwards
{
    /*
    Single Match 
    ---------------
    Most Damage, Most Component Destruction, Most Kills, Most Kill Assists, Most KMDD

    Single Drop
    -------------
    Most Damage, Most Component Destruction, Most Team Damage                       
    */
    public class PilotStats
    {
        public int Damage { get; set; }
        public int ComponentsDestroyed { get; set; }
        public int Kills { get; set; }
        public int KillAssists { get; set; }       
        public int KMDD { get; set; }   
        public int TeamDamage { get; set; } 
    }
}
