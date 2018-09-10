using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainServer.Factions
{
    /// <summary>
    /// Simple faction data, could be mobs influence when killed or players current standing
    /// </summary>
    public class Faction
    {
        /// <summary>
        /// For mob templates, when mob has no faction level to contend with
        /// </summary>
        public static int nullLevel = -9999;

        public int Id { get; private set; }
        public int Points { get; set; }
        public int MobLevel { get; private set; }

        public Faction(int factionId, int factionPoints)
        {
            this.Id = factionId;
            this.Points = factionPoints;
        }

        public Faction(int factionId, int factionPoints, int mobLevel)
        {
            this.Id = factionId;
            this.Points = factionPoints;
            this.MobLevel = mobLevel;
        }

    }
}
