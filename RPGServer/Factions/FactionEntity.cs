using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainServer.Factions
{
    class FactionEntity
    {
        public List<Faction> Influences { get; private set; }

        /// <summary>
        /// Load in this entities faction influence, so when it's killed we now how to alter the players faction
        /// </summary>
        public FactionEntity()
        {
            Influences = new List<Faction>();
        }

    }
}
