using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MainServer.Factions
{

    public class FactionTemplateManager
    {
        /// <summary>
        /// All faction templates
        /// </summary>
        public Dictionary<int,FactionTemplate> Factions { get; private set; }
        
        /// <summary>
        /// Will load all factions and their level information from the dataDb.
        /// </summary>
        public FactionTemplateManager()
        {                                    
            Factions = new Dictionary<int, FactionTemplate>();
            
            //read in faction info
            Initialize();
        }

        public void Initialize()
        {

            //all our factions
            SqlQuery factionQuery = new SqlQuery(Program.processor.m_dataDB, "select * from factions");
            while (factionQuery.Read())
            {
                int id = factionQuery.GetInt32("id");
                string name = factionQuery.GetString("name");

                FactionTemplate faction = new FactionTemplate(id, name);                
                Factions.Add(id,faction);
            }

            //all our faction levels
            factionQuery = new SqlQuery(Program.processor.m_dataDB, "select * from factions_levels");
            while (factionQuery.Read())
            {
                int factionID = factionQuery.GetInt32("faction_id");
                int factionLevel = factionQuery.GetInt32("faction_level");
                int factionPoints = factionQuery.GetInt32("faction_points");
                string factionTitle = factionQuery.GetString("faction_title");

                //add this level information into the faction
                FactionTemplate factionTemplate;
                if(Factions.TryGetValue(factionID, out factionTemplate))
                {
                    factionTemplate.AddLevelInformation(factionLevel, factionPoints, factionTitle);
                }
                else
                {
                    //note the error at startup
                    Program.Display(String.Format("Cannot find factionID.{0} when loading a level.{1}", factionID,factionLevel));
                }
            }           
        }

        /// <summary>
        /// Are these mobs allies? I.e. neither in a faction or both allied to the same faction?
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="entityToCheck"></param>
        /// <returns></returns>
        internal bool IsAlly(ServerControlledEntity entity, ServerControlledEntity entityToCheck)
        {
            int entityAllegiance = GetFactionAllegianceFromInfluences(entity);
            int entityToCheckAllegiance = GetFactionAllegianceFromInfluences(entityToCheck);
            
            return entityAllegiance == entityToCheckAllegiance;
        }

        /// <summary>
        /// Find the likely allegiance of this mob.  I.e. if killing it results in negative points with a faction
        /// we assume it's a member of that faction.  If positive we return the faction opposing number.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>-1 if no allegiance found, other wise the int of the faction id</returns>
        internal int GetFactionAllegianceFromInfluences(ServerControlledEntity entity)
        {
            if (entity.Template.FactionInfluences == null)
                return -1;

            //check through all of my current known factions against the influences
            foreach (Faction factionInfluence in entity.Template.FactionInfluences)
            {
                // if a negative influence assume they are a member of this faction
                if (factionInfluence.Points < 0)
                {
                    return factionInfluence.Id;
                }
                // else positive influence, i.e. this faction likes it when you I am killed
                else
                {
                    return EnemyOfMyEnemy(factionInfluence.Id);
                }
            }
                   
            // be default, no allegiance
            return -1;

        }

        /// <summary>
        /// Some factions have natural enemies, e.g. liches vs reavers. 
        /// </summary>
        /// <param name="factionId"></param>
        /// <returns>enemy of this faction id</returns>
        public int EnemyOfMyEnemy(int factionId)
        {
            //i'm going to hard code this for now but will fix a better solution later
            if (factionId == 4)
                return 5;
            if (factionId == 5)
                return 4;

            return -1;
        }
    }
}
