using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MainServer.Factions
{
    /// <summary>
    /// Character faction manager
    /// </summary>
    class FactionManager
    {
        #region fields and events

        /// <summary>
        /// Current list of factions and repuation in them
        /// </summary>
        public Dictionary<int,Faction>  Factions { get; private set; }
        
        Character Character { get;  set; }
        Database worldDB { get; set; }
        FactionTemplateManager FactionTemplateManager { get;  set; }

        public delegate void FactionLevelChangedHandler(Faction faction, bool repIncreased);

        /// <summary>
        /// Event if a players overall level with a faction has changed e.g. going from neutral to friendly
        /// </summary>
        public event FactionLevelChangedHandler FactionLevelChanged;

        #endregion

        #region constructor and setup

        public FactionManager(Database db, Character character)
        {
            this.Character = character;
            this.worldDB = db;
            this.FactionTemplateManager = Program.processor.FactionTemplateManager;
            
            Factions = new Dictionary<int, Faction>();
            LoadCharactersFactionData();


            FactionLevelChanged += MessageCharactersAboutFactionLevel;
            FactionLevelChanged += UpdateQuests;
        }

        internal void LoadCharactersFactionData()
        {
            //load in this characters faction data
            string select = "select * from character_factions where characted_id like " + Character.m_character_id;            
            SqlQuery query = new SqlQuery(worldDB, "select * from  character_factions where character_id like " + Character.m_character_id);
            if (query.HasRows)
            {
                while (query.Read())
                {
                    int factionId = query.GetInt32("faction_id");
                    int factionPoints = query.GetInt32("faction_points");
                                        
                    Factions.Add(factionId, new Faction(factionId,factionPoints));
                }
            }
            query.Close();

        }

        #endregion

        #region main methods

        public void AlterFactionPoints(int characterLevel, int mobLevel, int factionID, int factionPointsDelta, int mobFactionLevel)
        {

            //don't do anything if the delta is zero
            if (factionPointsDelta == 0)
                return;
            
            //if we've never met this faction, skip and we pass on as normal
            Faction faction;
            if (Factions.TryGetValue(factionID, out faction))
            {

                // if we're equal or below the mob faction level, normal delta applies
                // if the delta is -ve (losing faction points) don't adjust
                // if we're one above it, we only get half the points
                // if we're higher than that we get nothing

                int currentLevel = FactionTemplateManager.Factions[factionID].GetLevel(faction.Points).level;
                if (factionPointsDelta > 0)
                {
                    if (currentLevel == mobFactionLevel + 1)
                        factionPointsDelta = factionPointsDelta / 2;
                    else if (currentLevel > mobFactionLevel + 1)
                    {
                        //we gain nothing and can return
                        factionPointsDelta = 0;
                        return;
                    }
                }

            }
            //for now just take the values as is, but we'll want to alter based on the level 
            AlterFactionPoints(factionID, factionPointsDelta);
        }

        /// <summary>
        /// Faction points have changed for this character and faction. 
        /// Update sql and fire off any event related to level changing
        /// </summary>
        /// <param name="factionID">Unique faction id</param>
        /// <param name="factionPointsDelta">change in standing +ve or -ve</param>
        public void AlterFactionPoints(int factionID, int factionPointsDelta)
        {
            //check validity of faction id            
            if (FactionTemplateManager.Factions.ContainsKey(factionID) == false)
            {
                //no matching template found...don't do anything
                return;
            }

            // if we already have this faction - get it and the template
            Faction faction;
            if (Factions.TryGetValue(factionID, out faction))
            {
                //get the template and check if we're changing a level or if we're at the min/max
                FactionTemplate factionTemplate = FactionTemplateManager.Factions[factionID];
                FactionTemplate.Level prevLevel = factionTemplate.GetLevel(faction.Points);

                //clamp to min max range
                int adjustedPoints = faction.Points + factionPointsDelta;
                //if the delta puts us out of range (either -ve or +ve)
                if (factionPointsDelta < 0 && adjustedPoints < factionTemplate.Min || factionPointsDelta > 0 && adjustedPoints > factionTemplate.Max)
                {
                    int clampedDelta = 0;
                    //if -ve - use min value
                    if (factionPointsDelta < 0)
                    {
                        clampedDelta = factionTemplate.Min - faction.Points;
                    }
                    //else use max value
                    else
                    {
                        clampedDelta = factionTemplate.Max - faction.Points;
                    }

                    

                    //now set
                    factionPointsDelta = clampedDelta;
                }

                // normally break but I want to see the debug on this
                if (factionPointsDelta == 0)
                {
                    //Program.Display("Faction points are ZERO");
                    return;
                }


                faction.Points += factionPointsDelta;
                UpdateFaction(factionID, faction.Points);
                FactionTemplate.Level currLevel = FactionTemplateManager.Factions[factionID].GetLevel(faction.Points);

                //note the change in points
                Program.processor.FactionNetworkManager.SendFactionsPoints(this.Character.m_player, faction, factionPointsDelta);

                // if there has been a change in level
                if (prevLevel.level != currLevel.level)
                    FactionLevelChanged(faction, factionPointsDelta > 0);

            }
            else //else we don't have info on this faction, it's new so create the sql listing
            {
                CreateFaction(factionID);
                AlterFactionPoints(factionID, factionPointsDelta);
            }
        }

        /// <summary>
        /// Create sql data for this character and faction.
        /// Will start at 0 points.
        /// </summary>
        /// <param name="factionID">unique faction id</param>
        private void CreateFaction(int factionID)
        {
            // insert new faction info for this character            
            string insertionString = String.Format("insert into character_factions (character_id,faction_id,faction_points) VALUES ( {0}, {1}, 0)",
                    this.Character.m_character_id, factionID);

            // #faction #debug
            Program.Display(insertionString);

            worldDB.runCommand(insertionString);

            //and also create in the class
            Factions.Add(factionID, new Faction(factionID, 0));
        }

        /// <summary>
        /// Update sql data for this character and faction
        /// </summary>
        /// <param name="factionID">unique faction id</param>
        /// <param name="factionPoints">total current points</param>
        private void UpdateFaction(int factionID, int factionPoints)
        {

            // update character points for this faction
            string updateString = String.Format("update character_factions set faction_points = {0} where character_id = {1} and faction_id = {2}",
                    factionPoints, Character.m_character_id, factionID);

            // #faction #debug
            //Program.Display(updateString);

            worldDB.runCommandSync(updateString);
        }

        /// <summary>
        /// Simple chect, do we have any reputation points with this faction, i.e. have we met them
        /// </summary>
        /// <param name="factionId"></param>
        /// <returns></returns>
        internal bool HasMetFaction(int factionId)
        {
            return Factions.ContainsKey(factionId);
        }

        /// <summary>
        /// Our faction level has changed, either an increase or a decrease
        /// </summary>
        /// <param name="faction">faction in question</param>
        /// <param name="repIncreased">true if they have gone up in reputation, else false</param>
        private void MessageCharactersAboutFactionLevel(Faction faction, bool repIncreased)
        {
            
            // get the template for the new title
            FactionTemplate factionTemplate;
            if (FactionTemplateManager.Factions.TryGetValue(faction.Id, out factionTemplate))
            {
                // get the now current level
                FactionTemplate.Level currLevel = factionTemplate.GetLevel(faction.Points);

                // send message to the client
                Program.processor.FactionNetworkManager.SendFactionsLevel(this.Character.m_player, faction,
                    factionTemplate, currLevel, repIncreased);

                // if we've changed from hated to friendly - refresh mob info
                if (repIncreased && currLevel.level == 1)
                {                 
                    this.Character.m_zone.UpdateMobFactionReputation(this.Character.m_player, faction, true);
                }
                // or if we've gone from friendly to hated - refresh mob info
                if (!repIncreased && currLevel.level == 0)
                {                    
                    this.Character.m_zone.UpdateMobFactionReputation(this.Character.m_player, faction, false);
                }
            }

                       
        }

        /// <summary>
        /// Ask the quest manager to refresh what quests are available when we change faction level
        /// </summary>
        /// <param name="faction"></param>
        /// <param name="repIncreased"></param>
        private void UpdateQuests(Faction faction, bool repIncreased)
        {            
            Character.m_QuestManager.SendQuestsInZoneRefresh();
        }

        #endregion

        #region helper methods

        /// <summary>
        /// Does this character meet or exceed this faction level check.  
        /// </summary>
        /// <param name="factionID">faction id character must know</param>
        /// <param name="factionLevel">Greater than or equal to this level</param>
        /// <returns></returns>
        public bool HasFactionLevel(int factionID, int factionLevel)
        {
            Faction faction;
            if (Factions.TryGetValue(factionID, out faction))
            {
                 //get the template and check against level
                FactionTemplate factionTemplate = FactionTemplateManager.Factions[factionID];
                
                FactionTemplate.Level curLevel = factionTemplate.GetLevel(faction.Points);
                if (curLevel.level >= factionLevel)
                    return true;
            }

            return false;
        }
        

        /// <summary>
        /// For a given set of influences, i.e. if we killed this mob our reputation would alter by these points.  
        /// Deduce if we are neutral with this mob.  E.g. if this mob would give -ve points for a faction we assume it's a member of that faction
        /// </summary>
        /// <param name="factionInfluences"></param>
        /// <returns>true if we are allied, false if we are an enemy</returns>
        internal bool CheckReputationAgainstEntity(List<Faction> factionInfluences)
        {
            // no faction influences...so we are a normal enemy
            if (factionInfluences == null)
                return false;

            //check through all of my current known factions against the influences
            foreach(Faction factionInfluence in factionInfluences)
            {
                //if a negative influence assume they are a member of this faction
                if (factionInfluence.Points < 0)
                {
                    //are we friendly?
                    if (HasFactionLevel(factionInfluence.Id, 1))
                    {                        
                        return true;
                    }
                    else
                    {                     
                        return false;
                    }
                }
                else //positive influence for this factionid
                {
                    int enemyFactionId = this.FactionTemplateManager.EnemyOfMyEnemy(factionInfluence.Id);
                    // are we friendly?
                    if (HasFactionLevel(enemyFactionId, 1))
                    {                        
                        return true;
                    }
                    else
                    {                        
                        return false;
                    }
                }

            }

            //by default we're an enemy with all factions until we prove otherwise
            return false;
        }

       

        #endregion

    }
}
