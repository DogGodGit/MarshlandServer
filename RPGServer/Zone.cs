using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using Lidgren.Network;
using System.Drawing;
using MainServer.Collisions;
using System.Threading;
using MainServer.Combat;
using MainServer.partitioning;
using MainServer.TokenVendors;
using XnaGeometry;
using MainServer.Localise;
using MainServer.Factions;

namespace MainServer
{

    internal class Zone
	{
		// #localisation
		public class ZoneOfferTextDB : TextEnumDB
		{
			public ZoneOfferTextDB() : base(nameof(Zone), typeof(TextID)) { }

			public enum TextID
			{
				CANNOT_USE_RESURRECTION_IDOL,			// "Cannot use a resurrection idol for at least another {time0} seconds."
				CANNOT_RESURRECT_ARENA,					// "Cannot resurrect in the arena."
				ROLLED_NUMBER,							// "{name0} rolled {randResult1} out of 100"
				OTHER_CLAIMED_FISHING_SPOT,				// "Fishing spot already claimed by another player"
				OTHER_CANCELLED_TRADE,					// "{name0} has cancelled the trade"
				LEVEL_5_SHOUT,							// "You must be level 5 or higher to shout"
				UNKNOWN_SKILL,							// "Unknown Skill"
				PLAYER_JOINED_GROUP,					// "you have joined {name0}'s group"
				TARGET_NOT_SAME_PVP_MODE,				// "The target is not in the same PVP mode as you"
				INVALID_SKILL_TARGET,					// "invalid target for {skillName0}"
				INVALID_SKILL_EQUIPMENT,				// "invalid equipment for {skillName0}"
				SKILL_OUT_OF_RANGE,						// "out of range for {skillName0}"
				NOT_ENOUGH_HEALTH_CAST_SKILL,			// "Not enough health to cast this skill"
				NOT_ENOUGHT_ENERGY_CAST_SKILL,			// "Not enough energy to cast this skill"
				CHALLENGED_OTHER_DUEL,					// "You have challenged {name0} to a duel."
				ALREADY_SENT_DUEL_REQUEST,				// "They have already sent you a duel request"
				GROUPING_BONUS,							// "Grouping Bonus: experience {expPercentUp0}%, gold {goldPercentUp1}%"
				GAINS_WISEXP,							// "{name0} gains {value1} wiseXP"
				GAINS_LUCKY_GOLD,                       // "{name0} gains {value1} lucky gold"
				DUEL_COULD_NOT_START,					// "The duel could not be started at this time"
				DUEL_OTHER_PLAYER_BUSY,					// "The other player is currently busy"
				DUEL_PLAYER_BUSY,						// "You are currently busy"
				PLAYER_CAN_NOT_DUEL_AREA,				// "You can not duel in this area"
				THEY_CAN_NOT_DUEL_AREA,					// "They cannot duel in this area"
				BLOCK_CHARACTER_DUEL,					// "You have blocked this character, please unblock them before trying to duel"
				CAN_NOT_PICK_UP_ITEM,					// "cannot pick up item"
				CHARACTER_GAIN_ITEM,					// "{name0} gained item: {lootItemName}"
				CHARACTER_PICK_UP_ITEM,                 // "{name0} picked up: {itemName}"
				PLAYER_DECLINED_DEUL,					// "{name0} declined the duel."
			}
		}
		public static ZoneOfferTextDB textDB = new ZoneOfferTextDB();

		#region enums and static constants

		public enum COMBAT_WINNER
        {
            ABANDONED = 0,
            PLAYER = 1,
            MOB = 2
        };

        public enum TARGET_TYPE
        {
            NONE = -1,
            SELF = 0,
            OTHER_PLAYER = 1,
            MOB = 2
        };
        /// <summary>
        /// Area Specific actions will not be allowed beyond this range
        /// The Partitions will not find entities past this
        /// </summary>
        const float MAX_ACTION_INTEREST_RANGE = 40;
        static float playerUpdateDistance = 40;
        static float ZONE_POINT_DISCOVERY_DISTANCE = 20;
        internal static float LOCAL_MESSAGE_RANGE = 20;
        static double TIME_BETWEEN_LAG_UPDATES = 10;
        internal static int MAX_ASSIST_DISTANCE= 5;
        internal static int MAX_PARTY_EXP_SHARE_DISTANCE_SQR = 400;
        internal static bool ENABLE_PVP = false;

		#endregion

		#region variables

		ASPathFinder m_pathFinder;
        ZonePartitionHolder m_partitionHolder=null;              
        CCollisions m_collisions = new CCollisions();

        public CCollisions Collison { get { return m_collisions; } }

        static public double m_timeBetweenPositionSends = 0.5f;

        /// <summary>
        /// A list of all players in the zone
        /// only public for testing
        /// </summary>
        public List<Player> m_players;
        public List<Shop> m_shops = new List<Shop>();
        public int m_zone_id;
        public string m_zone_name;
        public RectangleF m_zoneRect;
        public PointF m_zoneCentre;
         
        public int m_serverConfigID;

        public string m_mapfilename;
        public CombatManager m_combatManager = null;
        public bool m_threadWorking = false;
        public bool m_threadExit = false;
        double m_currentZoneTime=NetTime.Now;
        double m_TimeofLastUpdate = NetTime.Now;
        public Thread m_updateThread;
        public ServerControlledEntity[] m_theMobs;
        /// <summary>
        /// A list of points that link to other zones
        /// </summary>
        List<ZonePoint> m_zonePoints;
        /// <summary>
        /// A list of places characters can warp in from
        /// either thought zoning, teleporting or death 
        /// </summary>
        List<PlayerSpawnPoint> m_playerSpawnPoints;
        /// <summary>
        /// a list of all monster and npc spawnpoints
        /// </summary>
        List<SpawnPoint> m_spawnPoints;
        /// <summary>
        /// a list of all item spawn points
        /// </summary>
        List<ItemSpawnPoint> m_itemSpawnPoints;
        /// <summary>
        /// A List of all the players currently lagging out
        /// </summary>
        List<Player> m_laggingList = new List<Player>();
        double m_lastLagCheck = 0;
        /// <summary>
        /// A list of mobs wanting to broadcast their position reguardless of position
        /// </summary>
        List<ServerControlledEntity> m_mobsWantingToSendPositionUpdate = new List<ServerControlledEntity>();
        /// <summary>
        /// Characters and mobs that have died or been revived since the last death update
        /// </summary>
        List<CombatEntity> m_deathChangedThisFrame = new List<CombatEntity>();
        bool m_combatUpdateNeedsToBeSent = false;
        List<CombatDamageMessageData> m_cancelledDamages = new List<CombatDamageMessageData>();
        /// <summary>
        /// a List of entities that have done something (or had something done to them)since the last combat update
        /// </summary>
        List<CombatEntity> m_combatListChangedThisFrame = new List<CombatEntity>();
        /// <summary>
        /// a List of entities that have had their health changed since the last combat update
        /// </summary>
        List<CombatEntity> m_damagedThisFrame = new List<CombatEntity>();
        public List<EffectArea> m_effectAreas = new List<EffectArea>();

        #endregion

        #region Properties
        internal ZonePartitionHolder PartitionHolder
        {
            get { return m_partitionHolder; }
        }
        internal ASPathFinder PathFinder
        {
            get { return m_pathFinder; }
        }
        public ServerControlledEntity[] TheMobs
        {
            get { return m_theMobs; }
        }
        public double CurrentZoneTime
        {
            get{return m_currentZoneTime;}
        }
        #endregion

		#region constructor

		public Zone(Database db, SqlQuery query)
		{
			m_zone_id = query.GetInt32("zone_id");
			m_zone_name = query.GetString("zone_name");
			float minx = query.GetFloat("minx");
			float maxx = query.GetFloat("maxx");
			float minz = query.GetFloat("minz");
			float maxz = query.GetFloat("maxz");
			m_zoneCentre = new PointF((minx + maxx) / 2, (minz + maxz) / 2);
			m_zoneRect = new RectangleF(minx, minz, maxx - minx, maxz - minz);
			m_partitionHolder = new ZonePartitionHolder(this, new Vector2(minx, minz), new Vector2(maxx, maxz), 50, 10, 80);
            m_mapfilename = query.GetString("map_filename");
			m_serverConfigID = query.GetInt32("server_config_id");
			if (m_serverConfigID == Program.m_serverID)
			{
				m_players = new List<Player>();
				m_zonePoints = new List<ZonePoint>();
				m_playerSpawnPoints = new List<PlayerSpawnPoint>();
				m_spawnPoints = new List<SpawnPoint>();
				m_itemSpawnPoints = new List<ItemSpawnPoint>();
				m_currentZoneTime = NetTime.Now;//NetTime.Now;
				m_TimeofLastUpdate = m_currentZoneTime;

				setupShops(db);
				m_combatManager = new CombatManager(this);

				PopulateMobsFromDatabase(db);
				PopulatePlayerSpawnPointsFromDatabase(db);
				PopulateZonePointsFromDatabase(db);
				PopulateItemSpawnPointsFromDatabase(db);
				PopulateAreasFromDatabase(db);
			}

			m_collisions.loadCollisionObjects(ConfigurationManager.AppSettings["CollisionMapPath"] + "z" + m_zone_id + "_collisions.txt", minx, maxx, minz, maxz);

			m_pathFinder = new ASPathFinder(ConfigurationManager.AppSettings["CollisionMapPath"] + "z" + m_zone_id + "_aimap.aimap");
			if (Program.m_usingThreads)
			{
				m_updateThread = new Thread(new ThreadStart(threadLoop));
				m_updateThread.Name = "ZoneUpdateThread";
				m_updateThread.Start();
			}
		}

		#endregion

		/// <summary>
        /// Called if a combat entity has taken damage to allert the zone that something has taken Place
        /// </summary>
        /// <param name="theEntity"></param>
        internal void EntityDamaged(CombatEntity theEntity)
        {
           
            m_combatUpdateNeedsToBeSent = true;
            //add them to the list of people who's health has changed since the last update
            if (theEntity!=null && m_damagedThisFrame.Contains(theEntity) == false)
            {
                m_damagedThisFrame.Add(theEntity);
            }
        }

       

        /// <summary>
        /// Called if an action has taken place so this info can be sent to the client 
        /// </summary>
        /// <param name="caster"></param>
        /// <param name="target"></param>
        internal void EntityAffectedByAction(CombatEntity caster, CombatEntity target)
        {
            m_combatUpdateNeedsToBeSent = true;
            //add the caster and target to the list of entities taking action
            if (caster != null && m_combatListChangedThisFrame.Contains(caster) == false)
            {
                m_combatListChangedThisFrame.Add(caster);
            }
            if (target != null && m_combatListChangedThisFrame.Contains(target) == false)
            {
                m_combatListChangedThisFrame.Add(target);
            }
        }
        internal void QueuedDamageCancelled(CombatDamageMessageData theDamage)
        {
            if (m_cancelledDamages.Contains(theDamage) == false)
            {
                m_cancelledDamages.Add(theDamage);
            }
        }
        

        private void setupShops(Database db)
        {
            int num_shops = 0;
            SqlQuery rcquery = new SqlQuery(db, "select count(*) as rowcount from shop where zone_id=" + m_zone_id);
            if (rcquery.HasRows)
            {
                rcquery.Read();
                num_shops = rcquery.GetInt32("rowcount");
            }
            rcquery.Close();
            SqlQuery query = new SqlQuery(db, "select * from shop where zone_id=" + m_zone_id+" or zone_id=0");
            if (query.HasRows)
            {
                while (query.Read())
                {
                    int shop_id = query.GetInt32("shop_id");
                    string shop_name = query.GetString("shop_name");
                    int npc_id = query.GetInt32("npc_id");
                    int loot_table_id = query.GetInt32("loot_table_id");
                    int loot_table_quantity = query.GetInt32("loot_table_quantity");
                    int zone_id = query.GetInt32("zone_id");
                    int class_id = query.GetInt32("class_id");
                    int faction_id = query.GetInt32("faction_id");
                    int faction_level = query.GetInt32("faction_level");
                    m_shops.Add(new Shop(db, shop_id, zone_id, shop_name, npc_id, loot_table_id, loot_table_quantity, class_id, faction_id, faction_level));
                }
            }
            query.Close();
        }
        
        public void removePlayer(Player curPlayer, NetServer server)
        {

            uint removedPlayerID = curPlayer.m_activeCharacter.m_character_id;
            bool playerRemoved = false;
            playerRemoved = m_players.Remove(curPlayer);


            if (playerRemoved)
            {

                CheckLeavingPlayersPVPStatus(curPlayer);
                m_combatManager.RemoveAllReferenceToEntity(curPlayer.m_activeCharacter);
                for (int currentMobIndex = 0; currentMobIndex < m_theMobs.Length; currentMobIndex++)
                {
                    ServerControlledEntity currentMob = m_theMobs[currentMobIndex];
                    if (currentMob != null)
                    {
                        currentMob.RemoveFromAggroLists(curPlayer.m_activeCharacter);
                    }

                }
                int numberOfPlayers = m_players.Count;
                NetConnection[] connections = new NetConnection[m_players.Count];
                for (int i = 0; i < numberOfPlayers; i++)
                {
                    if (i < m_players.Count)
                    {
                        connections[i] = m_players[i].connection;
                    }
                    else
                    {
                        connections[i] = null;
                    }
                }
                NetOutgoingMessage zonemsg = server.CreateMessage();
                zonemsg.WriteVariableUInt32((uint)NetworkCommandType.CharacterZoningOut);
                zonemsg.WriteVariableUInt32((uint)removedPlayerID);
                Program.processor.SendMessage(zonemsg, connections.ToList<NetConnection>(), NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.CharacterZoningOut);

                curPlayer.m_activeCharacter.CurrentPartition = null;
            }
        }
        public void threadLoop()
        {
            while (m_threadExit == false)
            {
                if (m_threadWorking)
                {
                    Update();
                    m_threadWorking = false;
                }
                else
                {
                    Thread.Sleep(1);
                }
            }

        }
     
        public void Update()
        {
            if (m_serverConfigID == Program.m_serverID)
            {
                m_currentZoneTime = NetTime.Now;

                double startOfUpdate = m_currentZoneTime;
                m_pathFinder.Reset();
                m_pathFinder.StopPathingTime = DateTime.Now.AddMilliseconds(50);
                List<CombatEntity> deadPlayerList = new List<CombatEntity>();
                double timeSinceLastFrame = m_currentZoneTime - m_TimeofLastUpdate;//NetTime.Now-m_timeAtLastFrame;

                if (timeSinceLastFrame > 0.001)
                {
                    double combatManagerStart = NetTime.Now;
                    m_combatManager.Update();

                    double cmTime = (NetTime.Now - combatManagerStart)*1000;
                    Program.m_combatUpdateTime += cmTime;

                    if (cmTime > 100)
                    {
                        Program.Display("Combat manager took " + cmTime.ToString("N2") + " ms to update");
                    }

                    Program.MainForm.updatePanel(m_zone_id);
                    m_TimeofLastUpdate = m_currentZoneTime;// NetTime.Now;

                    double charactersUpdateStart = NetTime.Now;
                    
                    for (int i = 0; i < m_players.Count; i++)
                    {
                        Player currentPlayer = m_players[i];
                        Character currentCharacter = currentPlayer.m_activeCharacter;

                        //Update each character
                        if (currentCharacter != null)
                        {
                            
                            Double characterUpdateStart = NetTime.Now;

                            TestPlayerOutOfZone(currentCharacter);
                            
                            currentCharacter.Update(timeSinceLastFrame);

                            if ((DateTime.Now - currentCharacter.m_lastUpdatedNearby).TotalSeconds > m_timeBetweenPositionSends)
                            {
                                DateTime lastSendTime = currentCharacter.m_lastUpdatedNearby;
                                 currentCharacter.m_lastUpdatedNearby = DateTime.Now;
                                 SendPositionUpdates(Program.processor.m_server, currentPlayer, true, lastSendTime);                               
                            }

                            //check if the character has encountered any zone points
                            PlayerSpawnPoint encounteredSpawnPoint = FindSpawnPointInRangeOfPlayer(currentCharacter);

                            if (encounteredSpawnPoint != null)
                            {
                                int spawnPointID = encounteredSpawnPoint.SpawnPointID;
                                currentCharacter.AddTeleportLocation(spawnPointID);

                                //if it's a teleport location tell the Player about it
                                if (encounteredSpawnPoint.TeleportPoint == true)
                                {
                                    WritePlayerDiscoveredSpawnPoint(currentPlayer, spawnPointID);
                                }
                            }

                            if (currentCharacter.Dead == true)
                            {
                                deadPlayerList.Add(currentCharacter);
                            }

                            double timeTaken = NetTime.Now - charactersUpdateStart;
                            if (timeTaken > 0.05)
                            {
                                Program.Display("Character update for " + currentCharacter.Name + " took " + (timeTaken) * 1000 + "ms to update");
                            }
                        }
                    }

                    double mobUpdateStartTime = NetTime.Now;
                    double puTime = (mobUpdateStartTime - charactersUpdateStart)*1000;

                    Program.m_playerUpdateTime += puTime;
                    double mobUpdateStart = mobUpdateStartTime;

                    //update the mobs
                    for (int currentMob = 0; currentMob < m_theMobs.Length; currentMob++)
                    {
                        ServerControlledEntity mob = m_theMobs[currentMob];

                        if (mob != null)
                        {
                            if (mob.m_willDespawn)
                            {
                                m_spawnPoints[currentMob].updateDespawn(timeSinceLastFrame);
                            }

                            if (mob.m_JustDied)
                            {
                                MobJustDied(mob);
                            }

                            mob.Update(timeSinceLastFrame);

                            DateTime mobUpdateTime = DateTime.Now; // optimisation

                            if (mob.ToBeDestroyed() == true || Program.m_RemoveAllMobs)
                            {
                                mob.CurrentPartition = null;
                                int serverID = mob.ServerID;
                                mob.Destroyed = true;
                                mob.m_nearbyPlayers.Clear();
                                mob.m_PlayersToUpdate.Clear();
                                m_theMobs[currentMob] = null;

                                if (Program.m_RemoveAllMobs)
                                {
                                    m_spawnPoints[currentMob].m_timeTillNextRespawn = 0;
                                }

                                mob = null;
                                SendRemoveMob(Program.processor.m_server, serverID);
                            }
                            else if ((mobUpdateTime - mob.m_lastUpdatedNearby).TotalSeconds > 1)
                            {
                                UpdateNearbyListForMob(mob);
                                mob.m_lastUpdatedNearby = mobUpdateTime;
                            }
                        }
                        else if(!Program.m_RemoveAllMobs)//mob is currently despawned
                        {
                            if (m_spawnPoints[currentMob] != null)
                            {
                                m_theMobs[currentMob] = m_spawnPoints[currentMob].Update(timeSinceLastFrame);

                                //if a mob appeared tell everyone
                                if (m_theMobs[currentMob] != null)
                                {
                                    m_theMobs[currentMob].EntityPartitionCheck();
                                    SendAddMob(Program.processor.m_server, currentMob);
                                }
                            }
                        }                        
                    }

                    double playerLoopTimeNow = NetTime.Now;

                    double moTime = (playerLoopTimeNow - mobUpdateStart)*1000;
                    Program.m_mobUpdateTime += moTime;
                    double otherUpdateStart = playerLoopTimeNow;

                    for (int i = m_players.Count-1; i >=0; i--)
                    {
                        try
                        {
                            Player currentPlayer = m_players[i];
                            //tell them what mobs are doing
                            if (currentPlayer != null)
                            {
                                if (currentPlayer.m_activeCharacter != null)
                                {
                                    SendPatrolUpdate(currentPlayer);
                                }
                                //tell them who is dead
                                if (currentPlayer.m_activeCharacter != null && currentPlayer.m_activeCharacter.m_InLimbo == false)
                                {
                                    currentPlayer.m_activeCharacter.SendDeadPlayersList(deadPlayerList);
                                }
                            }
                            else
                            {
                                m_players.RemoveAt(i);
                            }
                        }
                        catch (Exception e)
                        {
                            Program.Display("handled exception in player loop:" + e.Message + " " + e.StackTrace);
                        }
                    }

                    for (int currentMobIndex = 0; currentMobIndex < m_theMobs.Length; currentMobIndex++)
                    {
                        ServerControlledEntity currentMob = m_theMobs[currentMobIndex];
                        if (currentMob != null)
                        {
                            currentMob.JustStopped = false;
                        }
                    }

                    for (int i = 0; i < m_itemSpawnPoints.Count; i++)
                    {
                        m_itemSpawnPoints[i].Update(timeSinceLastFrame);
                    }

                    Program.MainForm.updatePanel(m_zone_id);

                    double ouTime = (NetTime.Now - otherUpdateStart)*1000;
                    Program.m_otherUpdateTime += ouTime;
                }
                
                if ((CurrentZoneTime - m_lastLagCheck) > TIME_BETWEEN_LAG_UPDATES)
                {
                    PartitionHolder.Update();
                    sendLaggingList();
                    m_lastLagCheck = CurrentZoneTime;
                }

                double timeToUpdate = NetTime.Now - startOfUpdate;
                if (timeToUpdate > Program.m_longZoneUpdateThreshold)
                {
                    Program.Display( string.Format("{0} update time = {1}", m_zone_name, (timeToUpdate*1000).ToString("F2")) );
                }

                SendMobPositionsIgnoringDistance();
            }

        }
        
        private void UpdateNearbyListForMob(ServerControlledEntity mob)
        {
            mob.m_PlayersToUpdate.Clear();
            for (int i = 0; i < mob.m_nearbyPlayers.Count; i++)
            {
                if (!mob.m_nearbyPlayers[i].Destroyed)
                {
                    mob.m_PlayersToUpdate.Add(mob.m_nearbyPlayers[i]);
                }
            }
            mob.m_nearbyPlayers.Clear();
            for (int i = 0; i < m_players.Count; i++)
            {
                Character otherPlayer = m_players[i].m_activeCharacter;
                if (!otherPlayer.Destroyed && Utilities.Difference2DSquared(otherPlayer.CurrentPosition.m_position, mob.CurrentPosition.m_position) < Character.SQUARED_POSITION_SEND_DIST)
                {
                    mob.m_nearbyPlayers.Add(otherPlayer);
                    if (!mob.m_PlayersToUpdate.Contains(otherPlayer))
                    {
                        mob.m_PlayersToUpdate.Add(otherPlayer);
                    }
                }
            }
        }

        private void UpdateNearbyListForPlayer(Character character)
        {
            List<Character> oldNearByPlayers = new List<Character>(character.m_PlayersToUpdate.Count);
            oldNearByPlayers.AddRange(character.m_PlayersToUpdate);
            character.m_PlayersToUpdate.Clear();
            
            for(int i = 0; i < character.m_nearbyPlayers.Count; i++)
            {
                Character currentCharacter = character.m_nearbyPlayers[i];

                if (!currentCharacter.Destroyed && currentCharacter.AdminCloakedCharacter == false)
                {
                    character.m_PlayersToUpdate.Add(currentCharacter);
                   
                    if (oldNearByPlayers.Contains(currentCharacter))
                    {
                        oldNearByPlayers.Remove(currentCharacter);
                    }
                }
            }

            character.m_nearbyPlayers.Clear();
            List<Player> nearByPlayers = new List<Player>();

            if (character.CurrentPartition != null)
            {
                character.CurrentPartition.AddPlayersInRangeToList(character, character.CurrentPosition.m_position, Character.POSITION_SEND_DIST, nearByPlayers, ZonePartition.ENTITY_TYPE.ET_PLAYER, null);

                // Add characters in party regardless of distance.
                if (character.CharacterParty != null)
                {
                    List<Character> partyCharacters = character.CharacterParty.GetCharacters;

                   foreach (Character partyChar in partyCharacters)
                   {
                       if (partyChar == character)
                           continue;
                   
                       if (partyChar.Destroyed)
                           continue;
                   
                       if(character.CurrentZone != partyChar.CurrentZone)
                           continue;

                       if (character.m_nearbyPlayers.Contains(partyChar))
                           continue;

                       Player newPlayer = Program.processor.getPlayerFromActiveCharacterId((int)partyChar.m_character_id);
                       nearByPlayers.Add(newPlayer);
                   }
                }
            }

            bool listChanged = false;

            for (int i = 0; i < nearByPlayers.Count; i++)
            {
                Character otherPlayer = nearByPlayers[i].m_activeCharacter;

                if (character != otherPlayer && !otherPlayer.Destroyed && otherPlayer.AdminCloakedCharacter == false)
                {
                    character.m_nearbyPlayers.Add(otherPlayer);
                   
                    if (!character.m_PlayersToUpdate.Contains(otherPlayer))
                    {
                        character.m_PlayersToUpdate.Add(otherPlayer);
                        listChanged = true;
                    }

                    if (oldNearByPlayers.Contains(otherPlayer))
                    {
                        oldNearByPlayers.Remove(otherPlayer);
                    }
                }
            }

            if (oldNearByPlayers.Count > 0)
            {
                listChanged = true;
            }

            if (listChanged)
            {
                character.m_batchPositionRequired = true; 
            }
        }

        /// <summary>
        /// Get all the mobs that are a member of this faction, and update the players reputation
        /// </summary>
        /// <param name="curPlayer">player we want to update</param>
        /// <param name="faction">faction to match with</param>
        /// <param name="isFriendly">true if we've gone from hated to friendly...else the the reverse</param>
        internal void UpdateMobFactionReputation(Player curPlayer, Faction faction, bool isFriendly)
        {                        
            // gather all mobs in zone that belong to this faction
            List<ServerControlledEntity> factionMobsToUpdate = new List<ServerControlledEntity>();
            foreach(var mob in TheMobs)
            {
                if (mob == null)
                    continue;
                if (Program.processor.FactionTemplateManager.GetFactionAllegianceFromInfluences(mob) == faction.Id)                    
                    factionMobsToUpdate.Add(mob);                
            }

            // nothing to do
            if (factionMobsToUpdate.Count <= 0)
                return;

            // create a message
            NetOutgoingMessage mobmsg = Program.Server.CreateMessage();
            mobmsg.WriteVariableUInt32((uint)NetworkCommandType.MobsFactionReputationUpdate);
            
            // write the count out
            mobmsg.WriteVariableInt32(factionMobsToUpdate.Count);

            // all we need is the server id and the new aggro range
            foreach (var mob in factionMobsToUpdate)
            {
                mobmsg.Write(mob.ServerID);
                mobmsg.Write(isFriendly ? 0f : mob.AggroRange);
            }
            
            // all done, send message
            Program.processor.SendMessage(mobmsg, curPlayer.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.MobsFactionReputationUpdate);

        }

        /// <summary>
        /// When a mob is attacked, see who is nearby and ask them to join the fight.
        /// </summary>
        /// <param name="mobInNeed">mob being attacked</param>
        /// <param name="target">player</param>
        /// <param name="range">aggro range</param>
        internal void RequestAssistance(ServerControlledEntity mobInNeed, CombatEntity target, float range)
        {
            List<ServerControlledEntity> theMobs = new List<ServerControlledEntity>();
            
            //the range to get mobs from
            float maxAggroRange = range; // MAX_ASSIST_DISTANCE;
            if (mobInNeed.CurrentPartition != null)
            {
                mobInNeed.CurrentPartition.AddMobsInRangeToList(mobInNeed, mobInNeed.CurrentPosition.m_position, maxAggroRange, theMobs, ZonePartition.ENTITY_TYPE.ET_MOB, null);
            }
            
            
            // go through our list of nearby mobs and ask for assistance
            for (int currentMobIndex = 0; currentMobIndex < theMobs.Count; currentMobIndex++)
            {
                ServerControlledEntity currentMob = theMobs[currentMobIndex];

                //perform a faction check. Want to filter this list so that only mobs with matching faction will be called to arms                
                if (Program.processor.FactionTemplateManager.IsAlly(mobInNeed, currentMob) == false)
                {
                    continue;
                }


                if (currentMob != null && currentMob != mobInNeed)
                {
                    currentMob.RequestAssistance(mobInNeed, target);
                }

            }
        }

        public void SendPatrolUpdate(Player thePlayer)
        {
            NetOutgoingMessage rawMessage = Program.Server.CreateMessage();
            Character theCharacter = thePlayer.m_activeCharacter;

            List<ServerControlledEntity> mobsNearBy = new List<ServerControlledEntity>();
            
            PartitionHolder.AddMobsInRangeToList(theCharacter, theCharacter.CurrentPosition.m_position, 80, mobsNearBy, ZonePartition.ENTITY_TYPE.ET_MOB,null);
            int counter=0;
            for (int currentMobIndex = 0; currentMobIndex < mobsNearBy.Count; currentMobIndex++)
            {
                ServerControlledEntity currentMob = mobsNearBy[currentMobIndex];
                if (currentMob != null)
                {
                    if (currentMob.WriteDestinationToMessage(rawMessage, currentMob.ServerID, theCharacter))
                        counter++;
                }
            }
            if (counter > 0)
            {

                NetOutgoingMessage updateMobMsg = Program.Server.CreateMessage();
                updateMobMsg.WriteVariableUInt32((uint)NetworkCommandType.mobPatrolUpdate);
                updateMobMsg.WriteVariableInt32(counter);
                updateMobMsg.Write(rawMessage.PeekDataBuffer(), 0, (int)rawMessage.LengthBytes);
                //Program.Display("SendPatrolUpdate sent mobsNearBy count =" + mobsNearBy.Count);
                Program.processor.SendMessage(updateMobMsg, thePlayer.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.mobPatrolUpdate);
                //check if a mob should be removed
            }
        }

        private void MobJustDied(ServerControlledEntity serverControlledEntity)
        {
            serverControlledEntity.m_JustDied = false;
            Character lastAttacker = serverControlledEntity.GetKiller();
            Character killer = lastAttacker;
            if (lastAttacker != null)
            {
                Player player = lastAttacker.m_player;
                int killerLevel = lastAttacker.Level;
                if (serverControlledEntity.LockOwner != null)
                {
                    if (serverControlledEntity.Gathering == CombatEntity.LevelType.none)
                    {
                        List<Character> charList = serverControlledEntity.LockOwner.GetCharacters;

                        // Iterate thru all chars in list and get first available player.
                        foreach (Character t in charList)
                        {
                            lastAttacker = t;
                            player = lastAttacker.m_player;
                            if (null != player)
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        player = serverControlledEntity.GetKiller().m_player;
                    }

                }

                if (player != null)
                {
                    SendWonCombat(player, serverControlledEntity, killerLevel, killer.m_character_id);
                }
            }
        }

        public void recordCombatFinishStats(Character character, ServerControlledEntity mob, COMBAT_WINNER winner)
        {
            if (character != null)
            {
                if (winner == COMBAT_WINNER.MOB)
                {
                   // Program.processor.m_worldDB.runCommand("update character_details set player_deaths=player_deaths+1 where character_id=" + character.m_character_id);
                    character.increaseRanking(RankingsManager.RANKING_TYPE.NUMBER_OF_DEATHS, 1,false);
                    character.setPVEKillsToDeaths();
                }                
            }
            
        }

        #region networking
        void sendLaggingList()
        {
            List<NetConnection> connections = new List<NetConnection>();
            AddConnectionsOfPlayersToList(connections, m_players);
            NetOutgoingMessage lagmsg = Program.Server.CreateMessage();
            lagmsg.WriteVariableUInt32((uint)NetworkCommandType.ZoneLagList);
            lagmsg.WriteVariableInt32(m_laggingList.Count);
            for (int i = 0; i < m_laggingList.Count; i++)
            {
                lagmsg.WriteVariableInt32((int)m_laggingList[i].m_activeCharacter.m_character_id);
            }
            if (connections.Count > 0)
            {
                Program.processor.SendMessage(lagmsg, connections, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.ZoneLagList);
            }
        }

        internal void ResWithOptions(Player player, bool useItem, int itemID)
        {
            bool resOnSpot = false;
            int oldResItemID = ItemTemplate.OLD_RES_ITEM_ID; // 20;
            int resItemID = ItemTemplate.RES_ITEM_ID;        //36759;//20;
            int resItemNoTradeID = ItemTemplate.RES_ITEM_ID_NOTRADE;

            int resItemUsed = -1;

            Character currentCharacter = player.m_activeCharacter;
            if (currentCharacter == null || currentCharacter.Dead != true)
            {
                return;
            }
            // Old Resurrection Timer Condition
            /*if (useItem && (DateTime.Now - currentCharacter.m_lastResIdol).TotalSeconds < ItemTemplateManager.RES_IDOL_RECHARGE_TIME)//30)
            {
                Program.processor.sendSystemMessage("Cannot use a resurrection idol for at least another "+(ItemTemplateManager.RES_IDOL_RECHARGE_TIME-(DateTime.Now - currentCharacter.m_lastResIdol).TotalSeconds).ToString("F0")+ " seconds.", player, false, SYSTEM_MESSAGE_TYPE.NONE);
                return;
            }*/
            // New DEATH TIMER Condition
            if (useItem)
            {
                foreach (CharacterEffect characterEffect in player.m_activeCharacter.m_currentCharacterEffects)
                {
                    // If they have the death timer status effect
                    if (characterEffect.m_id == Character.DEATH_TIMER_ID)
                    {
                        // And its duration has not run out 
                        // Sorry for this bs...            ->                                                     <- the CharacterEffects duration value is always 0
                        if ((characterEffect.m_TimeStarted + characterEffect.StatusEffect.m_effectLevel.m_duration) > Program.MainUpdateLoopStartTime())
                        {
                            // Calculating time remaining on 'DEATH TIMER' status effect
                            int timeRemaining = (int)((characterEffect.StatusEffect.StartTime + characterEffect.StatusEffect.m_effectLevel.m_duration) - Program.MainUpdateLoopStartTime());

							// Send error message and dont allow to resurrect with an idol
							string locText = Localiser.GetString(textDB, player, (int)ZoneOfferTextDB.TextID.CANNOT_USE_RESURRECTION_IDOL);
							locText = String.Format(locText, timeRemaining);
							Program.processor.sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.NONE);
							return;
                        }
                    }
                }
            }
            if ((useItem == true) && (itemID > 0))
            {
                Item resItem = player.m_activeCharacter.m_inventory.findBagItemByInventoryID(itemID, resItemNoTradeID);
                if (resItem == null)
                {
                    resItem = player.m_activeCharacter.m_inventory.findBagItemByInventoryID(itemID, oldResItemID);
                }
                if (resItem == null)
                {
                    resItem = player.m_activeCharacter.m_inventory.findBagItemByInventoryID(itemID, resItemID);
                }
                if (resItem != null)
                {
                    resOnSpot = true;

                    //check we're not in the arena.
                    if (player.m_activeCharacter.m_zone.m_zone_id == 8)
                    {
						string locText = Localiser.GetString(textDB, player, (int)ZoneOfferTextDB.TextID.CANNOT_RESURRECT_ARENA);
						Program.processor.sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.NONE);
						return;
                    }

                    resItemUsed = resItem.m_template_id;
                    Item oldTeleportStones = new Item(resItem);
                    //currentCharacter.currentCharacter.m_inventory.consumeItem(teleportToken);
                    int numWilRemain = resItem.m_quantity - 1;
                    currentCharacter.m_inventory.ConsumeItem(resItem.m_template_id, resItem.m_inventory_id, 1);//
                    if (numWilRemain > 0)
                    {
                        currentCharacter.m_inventory.SendReplaceItem(oldTeleportStones, resItem);
                    }
                    else
                    {
                        currentCharacter.m_inventory.SendReplaceItem(oldTeleportStones, null);
                    }
                    //currentCharacter.m_lastResIdol = DateTime.Now;
                }
            }
            if (resOnSpot)
            {
                currentCharacter.increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.BACK_FROM_THE_DEAD, 1);
                currentCharacter.Respawn(currentCharacter.CurrentPosition.m_position, Character.Respawn_Type.ResIdol,resItemUsed);
            }
            else
            {

                //get the closest respawn point to the player
                PlayerSpawnPoint respawnPoint = GetClosestRespawnPoint(currentCharacter);
                //if this somehow failed pick the first respawn point
                if (respawnPoint == null)
                {
                    respawnPoint = m_playerSpawnPoints[0];
                }
                //respawn the character at this position
                currentCharacter.Respawn(respawnPoint.Position, Character.Respawn_Type.normal, -1);

            }
        }

        public void SendPlayerCorrectionMessage(NetServer server, Player player, Vector3 position, int zoneID, float angle)
        {
            Program.Display("moving player " + player.m_activeCharacter.m_name + " to " +Program.processor.getZone(zoneID).m_zone_name +" "+ position.X + "," + position.Y + "," + position.Z);
            NetOutgoingMessage movemsg = CreateCharacterCorrectionMessage(server, player.m_activeCharacter, position, zoneID, angle);
            Program.processor.SendMessage(movemsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.CorrectPlayerPosition);
        }
        internal NetOutgoingMessage CreateCharacterCorrectionMessage(NetServer server, Character theCharacter, Vector3 position, int zoneID, float angle)
        {
            NetOutgoingMessage movemsg = server.CreateMessage();
            movemsg.WriteVariableUInt32((uint)NetworkCommandType.CorrectPlayerPosition);
            movemsg.WriteVariableUInt32((uint)zoneID);
            movemsg.WriteVariableInt32((int)theCharacter.m_character_id);
            movemsg.Write((float)position.X);
            movemsg.Write((float)position.Y);
            movemsg.Write((float)position.Z);
            movemsg.Write(angle);
            return movemsg;
        }
        public void ReadEmoteMessage(NetIncomingMessage msg, NetServer server, Player player)
        {
            int emote = msg.ReadVariableInt32();
            bool isLooping = msg.ReadBoolean();
            int numberOfPlayers = m_players.Count;
            NetConnection[] connections = new NetConnection[m_players.Count];
            for (int i = 0; i < numberOfPlayers; i++)
            {
                //if (player != m_players[i])
                {
                    connections[i] = m_players[i].connection;
                }
            }
            NetOutgoingMessage zonemsg = server.CreateMessage();
            if(emote == 28){
                int randResult = Program.getRandomNumber(100) + 1;

				string locRandString = Localiser.GetString(textDB, player, (int)ZoneOfferTextDB.TextID.ROLLED_NUMBER);
				locRandString = String.Format(locRandString, player.m_activeCharacter.Name, randResult);
				int senderID = (int)player.m_activeCharacter.m_character_id;
                List<Player> localPlayers = new List<Player>();
                PartitionHolder.AddPlayersInRangeToList(null, player.m_activeCharacter.CurrentPosition.m_position, LOCAL_MESSAGE_RANGE, localPlayers, ZonePartition.ENTITY_TYPE.ET_PLAYER, null);
                for(int i = localPlayers.Count-1; i>=0;i--)
                {
                    if (localPlayers[i].m_activeCharacter.HasBlockedCharacter(senderID) == true)
                    {
                        localPlayers.RemoveAt(i);
                    }
                }

                List<NetConnection> localconnections = new List<NetConnection>();
                AddConnectionsOfPlayersToList(localconnections, localPlayers);

				Program.processor.sendSystemMessage(locRandString, localconnections, false, SYSTEM_MESSAGE_TYPE.NONE);
				//Program.processor.sendSystemMessage(randString, localconnections, false, SYSTEM_MESSAGE_TYPE.NONE);
				// SendLocalSystemMessage(player.m_activeCharacter.Name + " rolled " + randResult + " out of 100", player.m_activeCharacter.CurrentPosition.m_position, LOCAL_MESSAGE_RANGE, false, SYSTEM_MESSAGE_TYPE.NONE);
			}
            zonemsg.WriteVariableUInt32((uint)NetworkCommandType.CharacterPlayingEmote);
            zonemsg.WriteVariableUInt32(player.m_activeCharacter.m_character_id);
            zonemsg.WriteVariableInt32(emote);
            zonemsg.Write(isLooping);
            Program.processor.SendMessage(zonemsg, connections.ToList<NetConnection>(), NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.CharacterPlayingEmote);

            if (Program.m_LogAnalytics)
            {
                AnalyticsMain logAnalytics = new AnalyticsMain(false);
                logAnalytics.EmoteUsed(player, emote.ToString(), Program.m_ServerName);
            }
        }
        public void ReadPositionUpdate(NetIncomingMessage msg, NetServer server, Player player)
        {
            double timeSent = msg.ReadDouble();
            double timeSinceSent = NetTime.Now - timeSent;
            
            player.m_activeCharacter.readMovementInfofromMessage(msg, timeSinceSent);
        }

        public void ReadStartAttackingMessage(NetIncomingMessage msg, NetServer server, Player player)
        {
            double timeSent = msg.ReadDouble();
            int targetType = msg.ReadVariableInt32();
            int targetID = (int)msg.ReadVariableUInt32();
            
            //if the target is valid
            CombatEntity target = GetTargetFor(targetType, targetID, player.m_activeCharacter.CurrentPosition.m_position);

            if (target != null && player.m_activeCharacter.IsEnemyOf(target))
            {
                bool targetIsFish = target.Gathering == CombatEntity.LevelType.fish;

				//#FISH do a check to see if this player is trying to fish again when they haven't recouped any concentration
                if (targetIsFish && player.m_activeCharacter.ConcentrationFishDepleted)
	            {
                    if (player.m_activeCharacter.CurrentConcentrationFishing <= 0)
		            {
						player.m_activeCharacter.WriteMessageForConcentrationAtZero();
                        m_combatManager.StopAttacking(player.m_activeCharacter);
						return;
		            }
					//not working - the client seems to put itself into an attacking state
					//regardless of the server
					//else
					//{
					//	player.m_activeCharacter.WriteMessageWaitForConcentraionToRecoup();
					//	return;
					//}		                
	            }
                else if (targetIsFish)
                {
                    // already-claimed fishing-spot check
                    if (m_combatManager.IsFishingTargetAlreadyUnderAttack(target))
                    {
                        Program.Display("Fishing spot is already in use, cancelling");
						string locText = Localiser.GetString(textDB, player, (int)ZoneOfferTextDB.TextID.OTHER_CLAIMED_FISHING_SPOT);
						Program.processor.sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.NONE);
						m_combatManager.StopAttacking(player.m_activeCharacter);
                        return;
                    }
                }

                m_combatManager.StartAttackingEntity(player.m_activeCharacter, target);					
            }    
        }

        public void ReadStopAttackingMessage(NetIncomingMessage msg, NetServer server, Player player)
        {
            double timeSent = msg.ReadDouble();
            m_combatManager.StopAttacking(player.m_activeCharacter);

        }
        public void ReadZoneRequest(NetIncomingMessage msg, NetServer server, Player player)
        {
            int zonePointID = msg.ReadVariableInt32();
            ZonePoint desiredPoint = null;
            //find the spawn point
            for (int currentPoint = 0; currentPoint < m_zonePoints.Count; currentPoint++)
            {

                ZonePoint currentZonePoint = m_zonePoints[currentPoint];
                int currentPointID = currentZonePoint.ZonePointID;
                bool questCompleted = true;
                if(currentZonePoint.QuestCompleted!=-1 && !player.m_activeCharacter.m_QuestManager.IsQuestComplete(currentZonePoint.QuestCompleted))
                {
                    questCompleted=false;
                }
                if (zonePointID == currentPointID && player.m_activeCharacter.Level>=currentZonePoint.MinLevel && questCompleted)
                {
                    desiredPoint = currentZonePoint;
                    break;
                }
            }
            //if no point was found, deal with it
            if (desiredPoint == null)
            {
                return;
            }
            float x = msg.ReadFloat();
            float y = msg.ReadFloat();
            float z = msg.ReadFloat();
            player.m_activeCharacter.CurrentPosition.m_position = new Vector3(x, y, z);
            player.m_activeCharacter.m_ConfirmedPosition.m_position = player.m_activeCharacter.CurrentPosition.m_position;
            //check the player is close enough to the point to zone
            Vector3 playerPosition = player.m_activeCharacter.CurrentPosition.m_position;
            Vector3 playerToSpawnPoint = (desiredPoint.Position - playerPosition);
            playerToSpawnPoint.Y = 0;
            double distanceFromPoint = playerToSpawnPoint.Length();
            if (distanceFromPoint <= desiredPoint.Radius)
            {
                //zone the player
                //find out where it's zoning to
                Zone newZone = Program.processor.getZone(desiredPoint.AdjoiningZoneID);
                PlayerSpawnPoint spawnPoint = Program.processor.GetSpawnPointForID(desiredPoint.AdjoiningSpawnPointID, desiredPoint.AdjoiningZoneID);
                //if there's somewhere for them to go then send them there
                if (spawnPoint != null)
                {
                    //tell the player they are zoning
                    int spawnPointID = spawnPoint.SpawnPointID;
                    Character currentCharacter = player.m_activeCharacter;

                    bool zoneKnown = false;
                    if (newZone != null)
                    {
                        zoneKnown = newZone.HasBeenToZone(currentCharacter);
                    }

                    if (!zoneKnown && !currentCharacter.AddTeleportLocation(spawnPointID) && spawnPoint.TeleportPoint)
                    {
                        WritePlayerDiscoveredSpawnPoint(player, spawnPointID);
                    }

                    Vector3 newPosition = spawnPoint.RandomRespawnPosition;

                    currentCharacter.m_CharacterPosition.m_position = newPosition;
                    currentCharacter.m_CharacterPosition.m_yangle = spawnPoint.Angle;
                    float dirx = (float)-Math.Sin((double)(180 - currentCharacter.m_CharacterPosition.m_yangle) * Math.PI / 180);
                    float dirz = (float)Math.Cos((double)(180 - currentCharacter.m_CharacterPosition.m_yangle) * Math.PI / 180);
                    currentCharacter.m_CharacterPosition.m_direction = new Vector3(dirx, 0, dirz);

                    currentCharacter.m_CharacterPosition.m_currentSpeed = 0;
                    currentCharacter.m_ConfirmedPosition.m_position = newPosition;
                    SendPlayerCorrectionMessage(server, player, newPosition, spawnPoint.ZoneID, spawnPoint.Angle);
                    if (player.m_activeCharacter.CurrentDuelTarget != null)
                    {
                        player.m_activeCharacter.CurrentDuelTarget.ForceEndDuel(player.m_activeCharacter, "");
                    }
                    if (player.m_activeCharacter.CurrentRequest != null)
                    {
                        player.m_activeCharacter.CurrentRequest.CancelRequest(player, PendingRequest.CANCEL_CONDITION.CC_SELF_CANCEL);
                    }
                    if (player.m_activeCharacter.m_tradingWith != null)
                    {
                        Player otherPlayer = player.m_activeCharacter.m_tradingWith;
                        player.m_activeCharacter.cancelTrade();
                       
                        if (otherPlayer != null && otherPlayer.m_activeCharacter != null)
                        {
                            Program.Display(player.m_activeCharacter.m_name + " cancelling trade with " + otherPlayer.m_activeCharacter.m_name);

							string locText = Localiser.GetString(textDB, otherPlayer, (int)ZoneOfferTextDB.TextID.OTHER_CANCELLED_TRADE);
							locText = String.Format(locText, player.m_activeCharacter.Name);
							Program.processor.sendSystemMessage(locText, otherPlayer, false, SYSTEM_MESSAGE_TYPE.TRADE);
						}
                    }
                    //add the player to the next zone
                    if (spawnPoint.ZoneID != m_zone_id)
                    {
                        // Program.processor.AddPlayerToZone(player, spawnPoint.ZoneID);
                        //remove the player
                        player.m_activeCharacter.m_zone = Program.processor.getZone(spawnPoint.ZoneID);
                        player.m_activeCharacter.InLimbo = true;
                        player.m_activeCharacter.saveNewZone();
                        removePlayer(player, server);
                        player.m_activeCharacter.UpdateSocialLists();
                    }
                    if (Program.m_LogAnalytics)
                    {
                        int currentZoneID = m_zone_id;
                        string currentZoneName = m_zone_name;

                        int newZoneID = newZone.m_zone_id;
                        string newZoneName = newZone.m_zone_name;

                        if (zoneKnown)
                        {
                            AnalyticsMain logAnalytics = new AnalyticsMain(true);
                            logAnalytics.zoneTravel(player, currentZoneID.ToString(), currentZoneName, newZoneID.ToString(), newZoneName);
                        }
                        else
                        {
                            AnalyticsMain logAnalytics = new AnalyticsMain(true);
                            logAnalytics.zoneNew(player, newZoneID.ToString(), newZoneName);
                        }
                    }

                }
            }
        }
        private void checkSlayerAchievements(Character character, ServerControlledEntity mob)
        {
            character.increaseRanking(RankingsManager.RANKING_TYPE.ENEMIES_KILLED, 1, false);
            character.setPVEKillsToDeaths();
            character.increaseRanking((RankingsManager.RANKING_TYPE)(mob.Template.m_power_level + 7), 1,false);
            character.increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.SLAYER, 1);
            switch (mob.Template.m_mobRace)
            {
                case MobTemplate.MOB_RACE.GOBLIN:
                    {
                        character.increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.GOBLIN_BANE, 1);
                        break;
                    }
                case MobTemplate.MOB_RACE.PUPPY:
                    {
                        character.increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.PUPPY_SLAYER, 1);
                        break;
                    }
                case MobTemplate.MOB_RACE.SKELETON:
                    {
                        character.increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.SKELETON_BANE, 1);
                        break;
                    }
                case MobTemplate.MOB_RACE.WOLF:
                    {
                        character.increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.WOLF_BANE, 1);
                        break;
                    }
            }
            switch (mob.Template.m_templateID)
            {
                case 70037:
                    {

                        character.increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.LORD_OF_CROOKBACK, 1);
                        break;
                    }
                case 70201:
                    {
                        character.increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.LORD_OF_DUSTWITHER, 1);
                        break;
                    }
                case 73000:
                    {
                        character.increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.DRAGON_SLAYER, 1);
                        break;
                    }
            }
            switch (character.m_zone.m_zone_id)
            {
                case 2:
                    {
                        character.increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.MASTER_OF_LIRS, 1);
                        break;
                    }
                case 3:
                    {
                        character.increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.MASTER_OF_STONEVALE, 1);
                        break;
                    }
                case 4:
                    {
                        character.increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.MASTER_OF_CROOKBACK, 1);
                        break;
                    }
                case 5:
                    {
                        character.increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.MASTER_OF_DUSTWITHER, 1);
                        break;
                    }
                case 6:
                    {
                        character.increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.MASTER_OF_SHALEMONT, 1);
                        break;
                    }
                case 7:
                    {
                        character.increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.MASTER_OF_OTHERWORLD, 1);
                        break;
                    }
                case 10:
                    {
                        character.increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.MASTER_OF_CARROWMORE, 1);
                        break;
                    }
                case 11:
                    {
                        character.increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.MASTER_OF_FINGALS, 1);
                        break;
                    }
                case 12:
                    {
                        character.increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.MASTER_OF_SEWERS, 1);
                        break;
                    }
            }
        }

		#region won combat

        private void SendWonCombat(Player player, ServerControlledEntity mob, int killerLevel, uint killerID)
        {
            //branch winning here
            //FISH
            if (mob.Gathering != CombatEntity.LevelType.none)
            {
                switch (mob.Gathering)
                {
                    case (CombatEntity.LevelType.fish):
                        SendWonFishCombat(player, mob, player.m_activeCharacter.GetRelevantLevel(mob), killerID);
                        break;

                }
                return;
            }            

            // if the player is in a group, award to party
            if (player.m_activeCharacter.CharacterParty != null)
            {
                // send party won combat
                SendPartyWonCombat(player, mob, killerLevel, killerID);
                return;
            }


            //alter for all faction influences
            if (mob.Template.FactionInfluences != null)
            {
                foreach (var factioninfluence in mob.Template.FactionInfluences)
                {
                    player.m_activeCharacter.FactionManager.AlterFactionPoints(player.m_activeCharacter.Level, mob.Level,
                        factioninfluence.Id, factioninfluence.Points, factioninfluence.MobLevel);
                }
            }

            //else award individual
            checkSlayerAchievements(player.m_activeCharacter, mob);
            if (player.m_activeCharacter.Level > killerLevel)
            {
                killerLevel = player.m_activeCharacter.Level;
            }

            //get experience & coins
            int experienceWon = mob.getExperienceValue(killerLevel);            
            int coinsWon = mob.getCoinsDropped();
            List<LootDetails> details = mob.getLootDropped();
            experienceWon = (int)(experienceWon * player.m_activeCharacter.ExpRate);

            // send won combat
            SendWonCombat(player, mob, experienceWon, coinsWon, details, killerID);

            //log this
            if (Program.m_LogAnalytics)
            {
                AnalyticsMain logAnalytics = new AnalyticsMain(false);
                logAnalytics.opponentDefeated(player, mob.Template.m_templateID.ToString(), mob.Template.m_name, coinsWon, details);
            }            

        }

		private void SendWonFishCombat(Player player, ServerControlledEntity mob, int killerLevel, uint killerID)
		{
            

            //if (player.m_activeCharacter.CharacterParty != null)
            //{
            //    SendPartyWonCombat(player, mob, killerLevel, killerID);
            //    return;
            //}
            
            
            if (player.m_activeCharacter.LevelFishing > killerLevel)
            {
                killerLevel = player.m_activeCharacter.LevelFishing;
            }

            int experienceWon = mob.getExperienceValue(killerLevel);
			//experienceWon *= 10;
			//int experienceWon = mob.getExperienceValue(player.m_activeCharacter.m_level);
			int coinsWon = mob.getCoinsDropped();
			List<LootDetails> details = mob.getLootDropped();

			experienceWon = (int)(experienceWon * player.m_activeCharacter.ExpRateFish); //FISH todo
			SendWonCombat(player, mob, experienceWon, coinsWon, details, killerID);

            Program.Display("Fishing Experience Rate = " + player.m_activeCharacter.ExpRateFish);
            Program.Display("Experience gained = " + experienceWon);

			//if (player.m_activeCharacter.CharacterParty != null)
			//{
			//	SendPartyWonCombat(player, mob, killerLevel, killerID);
			//	return;
			//}
			//checkSlayerAchievements(player.m_activeCharacter, mob);
			//if (player.m_activeCharacter.m_level > killerLevel)
			//{
			//	killerLevel = player.m_activeCharacter.m_level;
			//}
			//int experienceWon = mob.getExperienceValue(killerLevel);
			////int experienceWon = mob.getExperienceValue(player.m_activeCharacter.m_level);
			//int coinsWon = mob.getCoinsDropped();
			//List<LootDetails> details = mob.getLootDropped();

			//experienceWon = (int)(experienceWon * player.m_activeCharacter.ExpRate);
			//SendWonCombat(player, mob, experienceWon, coinsWon, details, killerID);
			//if (Program.m_LogAnalytics)
			//{
			//	AnalyticsMain logAnalytics = new AnalyticsMain(false);
			//	logAnalytics.opponentDefeated(player, mob.Template.m_templateID.ToString(), mob.Template.m_name, coinsWon, details);
			//}
		}

        private void SendPartyWonCombat(Player player, ServerControlledEntity mob, int killerLevel, uint killerID)
        {
			//todo
			//if fishing give to locked player somehow
            
            Party theParty = player.m_activeCharacter.CharacterParty; 
            //get members in range
            List<Character> fullMembersList = theParty.CharacterList;
            List<Character> inRangeMembers = new List<Character>();
            
            int highestLevelParticipant = killerLevel;

            if (mob.Gathering != CombatEntity.LevelType.none)
            {
                inRangeMembers.Add(mob.GetKiller());
                Program.Display("We're in party won combat and we shouldnt be.");
            }
            else
            {
                for (int i = 0; i < fullMembersList.Count; i++)
                {
                    Character currentCharacter = fullMembersList[i];
                    //if they are in range
                    if ((mob.TheCombatManager == currentCharacter.TheCombatManager) && (Utilities.Difference2DSquared(currentCharacter.CurrentPosition.m_position, mob.CurrentPosition.m_position) < MAX_PARTY_EXP_SHARE_DISTANCE_SQR))
                    {
                        //add them to the list
                        inRangeMembers.Add(currentCharacter);
                        if (currentCharacter.Level > highestLevelParticipant)
                        {
                            highestLevelParticipant = currentCharacter.Level;
                        }
                        checkSlayerAchievements(currentCharacter, mob);
                    }
                }
            }

            
            int charactersToReward = inRangeMembers.Count;
            if (charactersToReward == 0)
            {
                return;
            }
            //get the highest lvl of the party
            int highestLevel = theParty.HighestLevel;
            //work out how much exp they get
            //int totalExp = mob.getExperienceValue(killerLevel);
            //add the party boost
            float expBoost = 1 + (charactersToReward - 1) * Party.EXPERIANCE_BOOST_PER_PLAYER;

            float goldBoost = 1 + (charactersToReward - 1) * Party.GOLD_BOOST_PER_PLAYER;
            int expPercentUp = (int)((charactersToReward - 1)*(100 * Party.EXPERIANCE_BOOST_PER_PLAYER));
            int goldPercentUp = (int)((charactersToReward - 1)*(100 * Party.GOLD_BOOST_PER_PLAYER));
            //totalExp = (int)(totalExp * expBoost);
            //int expPerPlayer = totalExp / charactersToReward;
            //make a loot list for each
            List<LootDetails> initialLootList = mob.getLootDropped();
            //split up any multiple items
            List<LootDetails> totalLootList = new List<LootDetails>();
            for (int i = 0; i < initialLootList.Count; i++)
            {
                LootDetails currentLoot = initialLootList[i];
                //if it's one of many then 
                //split it up
                if (currentLoot.m_quantity > 1)
                {
                    for (int currentSplit = 0; currentSplit < currentLoot.m_quantity; currentSplit++)
                    {
                        LootDetails newLoot = new LootDetails(currentLoot.m_templateID, 1);
                        totalLootList.Add(newLoot);
                    }
                }
                //add it as is
                else
                {
                    totalLootList.Add(currentLoot);
                }
            }
            List<LootDetails>[] lootLists = new List<LootDetails>[charactersToReward];
            for (int i = 0; i < lootLists.Length; i++)
            {
                lootLists[i] = new List<LootDetails>();
            }
            //split up the loot
            //create a list to check who gets what 
            List<List<LootDetails>> splitList = new List<List<LootDetails>>();

            for (int i = 0; i < totalLootList.Count; i++)
            {
                //if the list is empty fill it up again
                if (splitList.Count <= 0)
                {
                    for (int curChar = 0; curChar < charactersToReward; curChar++)
                    {
                        splitList.Add(lootLists[curChar]);
                    }
                }
                //pick someone randomly from the list
                LootDetails currentLoot = totalLootList[i];
                int recievingCharacterIndex = Program.getRandomNumber(splitList.Count);
                splitList[recievingCharacterIndex].Add(currentLoot);
                //then remove them
                splitList.RemoveAt(recievingCharacterIndex);
                //before sending the list it will need to be checked for adding stackables
                /* LootDetails currentLoot = totalLootList[i];
                 int recievingCharacterIndex = Program.getRandomNumber(charactersToReward);
                 lootLists[recievingCharacterIndex].Add(currentLoot);*/

            }
            //get the coins dropped
            int totalCoinsWon =(int)( mob.getCoinsDropped()*goldBoost);
            int individualCoins = totalCoinsWon / charactersToReward;

            //send out the messages
            for (int i = 0; i < inRangeMembers.Count; i++)
            {
                Character currentCharacter = inRangeMembers[i];


                // #faction - grouping
                // alter for all faction influences
                if (mob.Template.FactionInfluences != null)
                {
                    foreach (var factioninfluence in mob.Template.FactionInfluences)
                    {
                        currentCharacter.FactionManager.AlterFactionPoints(currentCharacter.Level, mob.Level,
                            factioninfluence.Id, factioninfluence.Points, factioninfluence.MobLevel);
                    }
                }

                List<LootDetails> currentLootList = lootLists[i];
                //tidy up the loot list
                if (currentLootList.Count > 0)
                {
                    //check up to the second last
                    for (int lootIndex = 0; lootIndex < currentLootList.Count - 1; lootIndex++)
                    {
                        LootDetails currentLoot = currentLootList[lootIndex];
                        int currentID = currentLoot.m_templateID;
                        int currentQuantity = currentLoot.m_quantity;
                        ItemTemplate currentLootTemplate = ItemTemplateManager.GetItemForID(currentID);

                        //if it can be stacked then check there's not more than one of them
                        if (currentLootTemplate != null && currentLootTemplate.m_stackable == true)
                        {
                            for (int secondLootIndex = (lootIndex + 1); secondLootIndex < currentLootList.Count; secondLootIndex++)
                            {
                                LootDetails comparingLoot = currentLootList[secondLootIndex];
                                if (comparingLoot.m_templateID == currentID)
                                {
                                    currentLoot.m_quantity += comparingLoot.m_quantity;
                                    currentLootList.RemoveAt(secondLootIndex);
                                    secondLootIndex--;
                                }
                            }
                        }
                    }
                }

                int totalExp = mob.getExperienceValue(killerLevel);

                // FISHING
                // When calculating fishing exp the killerLevel is their fishing level not combat
                if (mob.Gathering == CombatEntity.LevelType.fish)
                {
                    if (currentCharacter.LevelFishing > killerLevel)
                    {
                        totalExp = mob.getExperienceValue(currentCharacter.LevelFishing);
                    }
                }
                // Normal check against combat level
                else
                {
                    if (currentCharacter.Level > killerLevel)
                    {
                        totalExp = mob.getExperienceValue(currentCharacter.Level);
                    }
                }

                totalExp = (int)(totalExp * expBoost);
                // FISH-143
                int expForPlayer = (int)((totalExp / charactersToReward) * (mob.Gathering == CombatEntity.LevelType.fish ? currentCharacter.ExpRateFish : currentCharacter.ExpRate)); 

                if (currentCharacter.Dead == true)
                {
                    expForPlayer = 0;
                }

                SendWonCombat(currentCharacter.m_player, mob, expForPlayer, individualCoins, currentLootList, killerID);
                //send info to rest of the group
                if (currentLootList.Count > 0)
                {
                    string lootList = "";
                    for (int lootIndex = 0; lootIndex < currentLootList.Count; lootIndex++)
                    {
                        LootDetails currentLoot = currentLootList[lootIndex];
                        int currentID = currentLoot.m_templateID;
                        int currentQuantity = currentLoot.m_quantity;
                        ItemTemplate currentLootTemplate = ItemTemplateManager.GetItemForID(currentID);
                        if (currentLootTemplate != null)
                        {
                            lootList += currentLootTemplate.m_loc_item_name[player.m_languageIndex];
                            if (currentQuantity > 1)
                            {
                                lootList += " * " + currentQuantity;
                            }
                            if (lootIndex < (currentLootList.Count - 1))
                            {
                                lootList += ", ";
                            }
                        }

                    }
					string locText = Localiser.GetString(textDB, player, (int)ZoneOfferTextDB.TextID.CHARACTER_GAIN_ITEM);
					locText = String.Format(locText, currentCharacter.m_name, lootList);
					theParty.SendPartySystemMessage(locText, currentCharacter, true, SYSTEM_MESSAGE_TYPE.BATTLE, true);
				}
                if (Program.m_LogAnalytics)
                {
                    AnalyticsMain logAnalytics = new AnalyticsMain(false);
                    logAnalytics.opponentDefeated(currentCharacter.m_player, mob.Template.m_templateID.ToString(), mob.Template.m_name, individualCoins, currentLootList);
                }
            }
            if (expPercentUp>0)
            {
				string locText = Localiser.GetString(textDB, player, (int)ZoneOfferTextDB.TextID.GROUPING_BONUS);
				locText = String.Format(locText, expPercentUp, goldPercentUp);
				theParty.SendPartySystemMessage(locText, null, true, SYSTEM_MESSAGE_TYPE.BATTLE, true);
			}
        }
        
        void SendWonCombat(Player player, ServerControlledEntity mob, int experienceWon, int coinsWon, List<LootDetails> lootWon, uint killersID)
        {
            // FISHING SUMMER //
            // These two functions now accept an experience type based on GatheringType

            // Lucky Xp & Scholar //
            // Check if player has either or both abilities
            CharacterAbility scholarAbility        = player.m_activeCharacter.getAbilityById(ABILITY_TYPE.SCHOLAR);
            CharacterAbility treasureHunterAbility = player.m_activeCharacter.getAbilityById(ABILITY_TYPE.TREASURE_HUNTER);

            int luckyXp   = 0;
            int luckyGold = 0;

            // If the mod does not have a gathering type and they have the skill - (not null) test if they proc and increase the amount of exp or gold earned
            if (mob.Gathering == CombatEntity.LevelType.none)
            {
                if ((scholarAbility != null) && (experienceWon > 0))
                {
                    float newExperienceWon = CheckForLuckyGoldOrExp(scholarAbility, player, mob, experienceWon);
                    if (newExperienceWon > experienceWon)
                    {
                        luckyXp = 1;
                        experienceWon = (int)Math.Round(newExperienceWon, 0);
                    }
                }
                if ((treasureHunterAbility != null) && (coinsWon > 0))
                {
                    float newCoinsWon = CheckForLuckyGoldOrExp(treasureHunterAbility, player, mob, coinsWon);
                    if (newCoinsWon > coinsWon)
                    {
                        luckyGold = 1;
                        coinsWon = (int)Math.Round(newCoinsWon, 0);
                    }
                }
            }

            experienceWon = player.m_activeCharacter.updateCoinsAndXP(coinsWon, experienceWon, mob.Gathering);
            float currentPCExperience = player.m_activeCharacter.getVisibleExperience(mob.Gathering);

            player.m_activeCharacter.m_inventory.addLoot(lootWon,mob);

            foreach (LootDetails details in lootWon)
            {
                Program.processor.CompetitionManager.UpdateCompetition(player.m_activeCharacter, Competitions.CompetitionType.LOOT_ITEM, details.m_templateID);
            }

            if (player.m_activeCharacter.m_character_id == killersID)
            {
                Program.processor.CompetitionManager.UpdateCompetition(player.m_activeCharacter, Competitions.CompetitionType.MOB_KILLS, mob.Template.m_templateID);
            }

            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.WonCombat);
            outmsg.WriteVariableInt32(mob.Template.m_templateID);
            outmsg.WriteVariableInt32(mob.ServerID);
            outmsg.WriteVariableInt32((int)killersID);
            outmsg.WriteVariableInt32((int)mob.Gathering);
            outmsg.WriteVariableInt32(experienceWon);
            outmsg.WriteVariableInt32(luckyXp); // flag for lucky xp
            outmsg.Write(currentPCExperience);
            outmsg.WriteVariableInt32(coinsWon);
            outmsg.WriteVariableInt32(luckyGold); // flag for lucky gold
            outmsg.WriteVariableInt32(lootWon.Count);
            for (int i = 0; i < lootWon.Count; i++)
            {
                outmsg.WriteVariableInt32(lootWon[i].m_templateID);
                outmsg.WriteVariableInt32(lootWon[i].m_quantity);
            }
            player.m_activeCharacter.m_inventory.WriteInventoryWithMoneyToMessage(outmsg);
            Program.processor.SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, (int)NetMessageChannel.NMC_Normal, NetworkCommandType.WonCombat);
            player.m_activeCharacter.m_QuestManager.checkKillRequired(mob.Template.m_templateID);
            for (int i = 0; i < lootWon.Count; i++)
            {
                player.m_activeCharacter.m_QuestManager.checkIfItemAffectsStage(lootWon[i].m_templateID);
            }
        }

        /// <summary>
        /// CheckForLuckyGoldOrExp
        /// Pass either the Scholar or Treasure Hunter abilities (assuming the player has them)
        /// This function calculates the chance of the ability procccing then returns the modified value of exp or gold
        /// </summary>
        /// <param name="ability"> Scholar or Treasure Hunter </param>
        /// <param name="player"> The player with this ability </param>
        /// <param name="target"> The target </param>
        /// <param name="value"> Exp or Gold gained (initial/base value) </param>
        /// <returns> Modified exp or gold value (returns same number when ability doe not proc )</returns>
        private float CheckForLuckyGoldOrExp(CharacterAbility ability, Player player, ServerControlledEntity target, float value)
        {
            // Get the current level of the ability
            float bonusLevel = player.m_activeCharacter.getAbilityLevel(ability.m_ability_id);

            // Get the base ability, max chance and multiplier for either Scholar or Treasure Hunter
            float baseAbility = 0.0f;
            float maxChance   = 0.0f;
            float multiplier  = 0.0f;

            switch (ability.m_ability_id)
            {
                case ABILITY_TYPE.SCHOLAR:
                {
                    baseAbility = Program.processor.m_abilityVariables.LuckyXpBaseAbility;
                    maxChance   = Program.processor.m_abilityVariables.LuckyXpMaxChance;
                    multiplier  = Program.processor.m_abilityVariables.LuckyXpMultiplier;

                    break;
                }
                case ABILITY_TYPE.TREASURE_HUNTER:
                {
                    baseAbility = Program.processor.m_abilityVariables.LuckyGoldBaseAbility;
                    maxChance   = Program.processor.m_abilityVariables.LuckyGoldMaxChance;
                    multiplier  = Program.processor.m_abilityVariables.LuckyGoldMultiplier;

                    break;
                }
            }

            // Get the chance of success (based on the targets level)
            float finalChance = maxChance * ((bonusLevel + baseAbility) / ((bonusLevel + baseAbility) + (10 * (target.Level + 3))));
            finalChance *= 100;

            // Random number from 0 - 99
            float bonusThreshold = (float)(Program.getRandomDouble() * 100);

            // If its less than the chance
            if (bonusThreshold < finalChance)
            {
                // Increase the amount by the multiplier
                float floatValue = value;
                value = floatValue * multiplier;

                // Send message to local players
                string playerName    = string.Empty; // players name
                string messageString = string.Empty; // final message string

                // Get the players name
                playerName = player.m_activeCharacter.Name;

				// Create the message string - "PlayerName gains numXP/numGold wise XP/lucky gold!"
				string locText;
				if (ability.m_ability_id == ABILITY_TYPE.SCHOLAR)
				{
					locText = Localiser.GetString(textDB, player, (int)ZoneOfferTextDB.TextID.GAINS_WISEXP);
				}
				else
				{
					locText = Localiser.GetString(textDB, player, (int)ZoneOfferTextDB.TextID.GAINS_LUCKY_GOLD);
				}
				messageString = string.Format(locText, playerName, value);

                // Send the message to nearby players
                player.m_activeCharacter.CurrentZone.SendLocalAbilityMessage(messageString, player.m_activeCharacter.CurrentPosition.m_position, Zone.LOCAL_MESSAGE_RANGE);
            }

            // Chance of skilling up on any attack which results in damage - check nulls
            if (player.m_activeCharacter != null && target != null)
            {
                // Check if the no ability test flag is false on the target
                if (!target.Template.m_noAbilityTest)
                {
                    player.m_activeCharacter.testAbilityUpgrade(ability);
                }
            }

            // Return the modified value
            return value;
        }

		#endregion

		public void SendRemoveMob(NetServer server, int mobToRemove)
        {
            if(Program.m_LogSpawns)
                Program.Display("Sending Remove mob " + mobToRemove);
            NetOutgoingMessage mobmsg = server.CreateMessage();
            mobmsg.WriteVariableUInt32((uint)NetworkCommandType.ZoneMonsterDisappeared);
            mobmsg.WriteVariableUInt32((uint)mobToRemove);

            NetConnection[] connections = new NetConnection[m_players.Count];
            for (int i = 0; i < m_players.Count; i++)
            {
                connections[i] = m_players[i].connection;
            }
            Program.processor.SendMessage(mobmsg, connections.ToList<NetConnection>(), NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.ZoneMonsterDisappeared);

        }
        // SendLocalAbilityMessage                                    //
        // Send a message using the Abilties channel to local players //
		internal void SendLocalAbilityMessageLocalised(LocaliseParams param, Vector3 position, float range)
		{
			List<Player> nearbyPlayers = new List<Player>();
			AddPlayersInRangeToList(position, range, nearbyPlayers);
			foreach (Player player in nearbyPlayers)
			{
				if (player != null && player.connection != null)
				{
					string locText = Localiser.GetString(param.textDB, player, param.textID);
					locText = string.Format(locText, param.args);
					Program.processor.SendAbilityMessage(locText, player.connection);
				}
			}
		}
		// SendLocalAbilityMessage                                    //
		// Send a message using the Abilties channel to local players //
		internal void SendLocalSkillDamageMessageLocalised(LocaliseParams param, Vector3 position, float range)
		{
			List<Player> nearbyPlayers = new List<Player>();
			AddPlayersInRangeToList(position, range, nearbyPlayers);
			foreach (Player player in nearbyPlayers)
			{
				if (player != null && player.connection != null)
				{
					string skillName = String.Empty;
					string locText = Localiser.GetString(param.textDB, player, param.textID);

					if ((int)param.args[1] != -1)
					{
						skillName = SkillTemplateManager.GetLocaliseSkillName(player, (SKILL_TYPE)param.args[1]);
					}

					locText = string.Format(locText, param.args[0], skillName, param.args[2]);
					Program.processor.SendAbilityMessage(locText, player.connection);
				}
			}
		}
		
		internal void SendLocalStatusEffectNameLocalised(LocaliseParams param, Vector3 position, float range)
		{
			List<Player> nearbyPlayers = new List<Player>();
			AddPlayersInRangeToList(position, range, nearbyPlayers);
			foreach (Player player in nearbyPlayers)
			{
				if (player != null && player.connection != null)
				{
					string locText = Localiser.GetString(param.textDB, player, param.textID);

					string statusEffectName = string.Empty;
					int statusEffectID = (int)param.args[1];
					if (statusEffectID != -1)
					{
						statusEffectName = StatusEffectTemplateManager.GetLocaliseStatusEffectName(player, statusEffectID);
					}

					locText = string.Format(locText, param.args[0], statusEffectName, param.args[2]);
					Program.processor.SendAbilityMessage(locText, player.connection);
				}
			}
		}

		internal void SendLocalAbilityMessage(string msg, Vector3 position, float range)
        {
            List<Player> nearbyPlayers      = new List<Player>();
            List<NetConnection> connections = new List<NetConnection>();

            AddPlayersInRangeToList(position, range, nearbyPlayers);
            AddConnectionsOfPlayersToList(connections, nearbyPlayers);

            Program.processor.SendAbilityMessage(msg, connections);
        }
		internal void SendLocalSystemSkillMessageLocalised(LocaliseParams param, Vector3 position, float range, bool important, SYSTEM_MESSAGE_TYPE type)
		{
			List<Player> nearbyPlayers = new List<Player>();
			AddPlayersInRangeToList(position, range, nearbyPlayers);
			foreach (Player player in nearbyPlayers)
			{
				if (player != null && player.connection != null)
				{
					string locText = Localiser.GetString(param.textDB, player, param.textID);
					string skillName = SkillTemplateManager.GetLocaliseSkillName(player, (SKILL_TYPE)param.args[1]);
					locText = string.Format(locText, param.args[0], skillName);
					Program.processor.sendSystemMessage(locText, player, important, type);
				}
			}
		}

        internal void SendLocalSystemMessage(string systemMessage, Vector3 position, float range, bool important, SYSTEM_MESSAGE_TYPE type,Player playerToExclude)
        {
            List<Player> nearbyPlayers = new List<Player>();
            List<NetConnection> connections = new List<NetConnection>();
            AddPlayersInRangeToList(position, range, nearbyPlayers);
            nearbyPlayers.Remove(playerToExclude);
            AddConnectionsOfPlayersToList(connections, nearbyPlayers);

            Program.processor.sendSystemMessage(systemMessage, connections, important, type);
        }
        internal void SendZoneSystemMessage(string systemMessage, bool important, SYSTEM_MESSAGE_TYPE type)
        {
            
            List<NetConnection> connections = new List<NetConnection>();
            
            AddConnectionsOfPlayersToList(connections, m_players);

            Program.processor.sendSystemMessage(systemMessage, connections, important, type);
        }
        internal void SendLocalChatMessage(string chatMessage, Player sendingPlayer)
        {
            List<Player> nearbyPlayers = new List<Player>();
            List<NetConnection> connections = new List<NetConnection>();
            AddPlayersInRangeToList(sendingPlayer.m_activeCharacter.m_CharacterPosition.m_position, LOCAL_MESSAGE_RANGE, nearbyPlayers);
            int sendersID = (int)sendingPlayer.m_activeCharacter.m_character_id;
            for (int i = nearbyPlayers.Count - 1; i >= 0; i--)
            {
                if (nearbyPlayers[i].m_activeCharacter.HasBlockedCharacter(sendersID) == true)
                {
                    nearbyPlayers.RemoveAt(i);
                }
            }
            AddConnectionsOfPlayersToList(connections, nearbyPlayers);
            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            //LOCAL_MESSAGE_RANGE

            outmsg.WriteVariableUInt32((uint)NetworkCommandType.GeneralChat);
            outmsg.WriteVariableInt32((int)HW_CHAT_BOX_CHANNEL.HW_CHAT_BOX_CHANNEL_LOCAL);
            outmsg.Write(sendingPlayer.m_activeCharacter.m_name);
            outmsg.Write(chatMessage);
            outmsg.WriteVariableInt32((int)sendingPlayer.m_activeCharacter.m_character_id);
            Program.Display("got local chat message from " + sendingPlayer.m_activeCharacter.m_name + " : " + chatMessage);
            Program.processor.SendMessage(outmsg, connections, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Chat, NetworkCommandType.GeneralChat);
        }
        internal void SendZoneChatMessage(string chatMessage, Player sendingPlayer)
        {
            //first deduct the item
            Character currentCharacter = sendingPlayer.m_activeCharacter;

            if (currentCharacter.Level < 5)
            {
				string locText = Localiser.GetString(textDB, sendingPlayer, (int)ZoneOfferTextDB.TextID.LEVEL_5_SHOUT);
				Program.processor.sendSystemMessage(locText, sendingPlayer, false, SYSTEM_MESSAGE_TYPE.NONE);
				return;
            }
            List<Player> zonesPlayers = new List<Player>(m_players);
            int sendersID = (int)sendingPlayer.m_activeCharacter.m_character_id;
            for (int i = zonesPlayers.Count - 1; i >= 0; i--)
            {
                if (zonesPlayers[i].m_activeCharacter.HasBlockedCharacter(sendersID) == true)
                {
                    zonesPlayers.RemoveAt(i);
                }
            }
            List<NetConnection> connections = new List<NetConnection>();
            AddConnectionsOfPlayersToList(connections, zonesPlayers);
            NetOutgoingMessage outmsg = Program.Server.CreateMessage();

            outmsg.WriteVariableUInt32((uint)NetworkCommandType.GeneralChat);
            outmsg.WriteVariableInt32((int)HW_CHAT_BOX_CHANNEL.HW_CHAT_BOX_CHANNEL_ZONE);
            outmsg.Write(sendingPlayer.m_activeCharacter.m_name);
            outmsg.Write(chatMessage);
            outmsg.WriteVariableInt32((int)sendingPlayer.m_activeCharacter.m_character_id);
            Program.Display("got zone chat message from " + sendingPlayer.m_activeCharacter.m_name + " : " + chatMessage);
            Program.processor.SendMessage(outmsg, connections, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Chat, NetworkCommandType.GeneralChat);
        }
        internal void AddPlayersInRangeToList(Vector3 position, float range, List<Player> players)
        {
            for (int i = 0; i < m_players.Count; i++)
            {
                Player currentPlayer = m_players[i];
                Character currentCharacter = currentPlayer.m_activeCharacter;
                if (currentCharacter != null)
                {
                    if ((currentCharacter.m_CharacterPosition.m_position - position).Length() < range)
                    {
                        players.Add(currentPlayer);
                    }
                }
            }

        }
        void AddConnectionsOfPlayersToList(List<NetConnection> connections, List<Player> players)
        {
            for (int i = 0; i < players.Count; i++)
            {
                Player currentPlayer = players[i];
                if (currentPlayer != null && currentPlayer.connection!=null)
                {
                    connections.Add(currentPlayer.connection);
                }
            }
        }
        internal void AddConnectionsOfPlayersToList(List<NetConnection> connections, List<Player> players,Player excludePlayer)
        {
            for (int i = 0; i < players.Count; i++)
            {
                Player currentPlayer = players[i];
                if (currentPlayer != null && currentPlayer.connection != null && currentPlayer!=excludePlayer)
                {
                    connections.Add(currentPlayer.connection);
                }
            }
        }
        
        /// <summary>
        /// A new mob has been added to the server
        /// </summary>
        /// <param name="server"></param>
        /// <param name="mobToAdd"></param>
        public void SendAddMob(NetServer server, int mobToAdd)
        {
            if(Program.m_LogSpawns)
                Program.Display("Sending Add mob " + m_theMobs[mobToAdd].Name+"[" + mobToAdd + "] spawn point " + m_theMobs[mobToAdd].ServerID);

            // if no players in the zone we have no work todo
            if (m_players.Count <= 0)            
                return;

            // for each player write out a mob added message
            for (int i = 0; i < m_players.Count; i++)
            {
                NetOutgoingMessage mobmsg = server.CreateMessage();
                mobmsg.WriteVariableUInt32((uint)NetworkCommandType.ZoneMonsterAppeared);
                mobmsg.WriteVariableUInt32((uint)m_theMobs[mobToAdd].ServerID);

                // write the mob data
                WriteMobToMsg(mobmsg, m_theMobs[mobToAdd],m_players[i]);
                
                // send message to player
                Program.processor.SendMessage(mobmsg, m_players[i].connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.ZoneMonsterAppeared);
            }
        }

        /// <summary>
        /// sends a position update to a particular player
        /// </summary>
        /// <param name="server"></param>
        /// <param name="player"></param>
        public void SendPositionUpdates(NetServer server, Player player, bool onlySendIfChanged, DateTime timeAtLastSent)
        {
            UpdateNearbyListForPlayer(player.m_activeCharacter);
            bool needsSend = !onlySendIfChanged;
            if (player.m_activeCharacter.m_batchPositionRequired == true)
            {
                needsSend = true;
                player.m_activeCharacter.m_batchPositionRequired = false;
            }
            //check if anyone has moved
            if (needsSend == false)
            {
                for (int j = 0; j < player.m_activeCharacter.m_PlayersToUpdate.Count && needsSend==false; j++)
                {
                    Character currentCharacter = player.m_activeCharacter.m_PlayersToUpdate[j];
                    if (currentCharacter.HasMovedSince(timeAtLastSent) == true)
                    {
                        needsSend = true;
                    }
                }

            }
            
            if (needsSend == true)
            {
                //create the message
                NetOutgoingMessage zonemsg = server.CreateMessage();
                zonemsg.WriteVariableUInt32((uint)NetworkCommandType.BatchZonePositionUpdate);

                //write the Time
                //zonemsg.Write(NetTime.Now);
                double serverTime = NetTime.Now + Character.SENT_FORWARD_PROJECTION_LENGTH;
                zonemsg.Write(serverTime);
                zonemsg.WriteVariableInt32(player.m_activeCharacter.m_PlayersToUpdate.Count);
                for (int j = 0; j < player.m_activeCharacter.m_PlayersToUpdate.Count; j++)
                {
                    Character otherCharacter = player.m_activeCharacter.m_PlayersToUpdate[j];
                    otherCharacter.WriteMovementInfoToMessage(zonemsg);
                }

                Program.processor.SendMessage(zonemsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Movement, NetworkCommandType.BatchZonePositionUpdate);
            }
        }
        public void ReadTeleportRequestMessage(NetIncomingMessage msg, NetServer server, Player player)
        {
            int destinationPoint = (int)msg.ReadVariableUInt32();
            int destinationZone = (int)msg.ReadVariableUInt32();
            Character currentCharacter = player.m_activeCharacter;
            bool teleportPointDiscovered = currentCharacter.DiscoveredSpawnPoints.Contains(destinationPoint);
            //don't teleport them to a point they have not discovered
            if (teleportPointDiscovered == false)
            {
                return;
            }

            PlayerSpawnPoint spawnPoint = Program.processor.GetSpawnPointForID(destinationPoint, destinationZone);
            if (spawnPoint == null)
                return;

            string errorReport = "";
            currentCharacter.CanFastTravel(ref errorReport);
            if (errorReport.Length > 0)
            {
                Program.processor.sendSystemMessage(errorReport, player, false, SYSTEM_MESSAGE_TYPE.NONE);
                return;
            }
            if (currentCharacter.m_inventory.GetItemCountForTravel() > currentCharacter.CompiledStats.FastTravelItemLimit && !spawnPoint.FreeTravel)
            {
                int teleportTokenID = 63184;
                Item teleportToken = currentCharacter.m_inventory.GetItemFromTemplateID(teleportTokenID, false); //--> No Trade Teleport Token

                if (teleportToken == null)
                {
                    teleportTokenID = 5;
                    teleportToken = currentCharacter.m_inventory.GetItemFromTemplateID(teleportTokenID, false);
                }

                if (teleportToken != null)
                {
                    Item oldTeleportStones = new Item(teleportToken);
                    //currentCharacter.currentCharacter.m_inventory.consumeItem(teleportToken);
                    int numWilRemain = teleportToken.m_quantity - 1;
                    currentCharacter.m_inventory.ConsumeItem(teleportToken.m_template_id, teleportToken.m_inventory_id, 1);//
                    if (numWilRemain > 0)
                    {
                        currentCharacter.m_inventory.SendReplaceItem(oldTeleportStones, teleportToken);
                    }
                    else
                    {
                        currentCharacter.m_inventory.SendReplaceItem(oldTeleportStones, null);
                    }
                }
                else
                {
                    //can not teleport
                    return;

                }
            }

            //if the spawn point exists then try to teleport there
            if (spawnPoint != null)
            {
                //check it is a teleport point
                if (spawnPoint.TeleportPoint == false)
                {
                    return;
                }
                //note the old position to update required players
                Vector3 oldPosition = currentCharacter.m_CharacterPosition.m_position;
                //real message
                //set their position and
                //tell them where they are going
                Program.Display("respawn");

                Vector3 newPosition = spawnPoint.RandomRespawnPosition;

                currentCharacter.m_CharacterPosition.m_position = newPosition;
                currentCharacter.m_CharacterPosition.m_yangle = spawnPoint.Angle;
                float dirx = (float)-Math.Sin((double)(180 - currentCharacter.m_CharacterPosition.m_yangle) * Math.PI / 180);
                float dirz = (float)Math.Cos((double)(180 - currentCharacter.m_CharacterPosition.m_yangle) * Math.PI / 180);
                currentCharacter.m_CharacterPosition.m_direction = new Vector3(dirx, 0, dirz);
                currentCharacter.m_ConfirmedPosition.m_position = newPosition;
                currentCharacter.m_ConfirmedPosition.m_yangle = spawnPoint.Angle;
                currentCharacter.m_ConfirmedPosition.m_direction = new Vector3(dirx, 0, dirz);
                SendPlayerCorrectionMessage(server, player, newPosition, spawnPoint.ZoneID, spawnPoint.Angle);
                //reset their speed - so that it doesn't run off while they are loading
                currentCharacter.m_CharacterPosition.m_currentSpeed = 0;
                if (currentCharacter.StatusCancelConditions.Move)
                {
                    currentCharacter.CancelEffectsDueToMove();
                }
                player.m_activeCharacter.TheCharactersPath.ClearList();
                if (player.m_activeCharacter.CurrentDuelTarget != null)
                {
                    player.m_activeCharacter.CurrentDuelTarget.ForceEndDuel(player.m_activeCharacter,"");
                }
                if (player.m_activeCharacter.CurrentRequest != null)
                {
                    player.m_activeCharacter.CurrentRequest.CancelRequest(player, PendingRequest.CANCEL_CONDITION.CC_SELF_CANCEL);
                }
                if (player.m_activeCharacter.m_tradingWith != null)
                {
                    Player otherPlayer = player.m_activeCharacter.m_tradingWith;
                    player.m_activeCharacter.cancelTrade();
                    
                    if (otherPlayer != null && otherPlayer.m_activeCharacter != null)
                    {
                        Program.Display(player.m_activeCharacter.m_name + " cancelling trade with " + otherPlayer.m_activeCharacter.m_name);
						string locText = Localiser.GetString(textDB, otherPlayer, (int)ZoneOfferTextDB.TextID.OTHER_CANCELLED_TRADE);
						locText = String.Format(locText, player.m_activeCharacter.Name);
						Program.processor.sendSystemMessage(locText, otherPlayer, false, SYSTEM_MESSAGE_TYPE.TRADE);
                    }
                }
                player.m_activeCharacter.ForfeitPVP();

                if (Program.m_LogAnalytics)
                {
                    AnalyticsMain logAnalytics = new AnalyticsMain(true);
                    logAnalytics.fastTravelUsed(player, spawnPoint.SpawnPointID.ToString());
                }

                //if it's not in the same zone deal with it
                if (destinationZone != m_zone_id)
                {
                    //add the player to the next zone
                    //Program.processor.AddPlayerToZone(player, spawnPoint.ZoneID);
                    //remove the player
                    player.m_activeCharacter.m_zone = Program.processor.getZone(spawnPoint.ZoneID);
                    player.m_activeCharacter.InLimbo = true;
                    player.m_activeCharacter.saveNewZone();
                    removePlayer(player, server);
                    player.m_activeCharacter.UpdateSocialLists();
                    
                }
                else
                {
                    //change the current partition if required
                    currentCharacter.EntityPartitionCheck();
                    //tell him where all the near by mobs are now
                    sendDumbMobPatrolUpdate(player);
                    SendPositionUpdates(Program.Server, player, false, currentCharacter.m_lastUpdatedNearby);
                    //send everyone notification this is a teleport and not a move
                    List<NetConnection> connections = new List<NetConnection>();
                    AddConnectionsOfPlayersToList(connections, m_players,player);
                    NetOutgoingMessage teleportMessage = CreateCharacterCorrectionMessage(Program.Server, currentCharacter, spawnPoint.Position, spawnPoint.ZoneID, spawnPoint.Angle);
                    Program.processor.SendMessage(teleportMessage, connections, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.CorrectPlayerPosition);
                }
            }
        }

        internal void ForceMoveCharacterToLocation(Vector3 position, float angle, int zoneID, Player player)
        {
            angle = 360 - angle;
            NetServer server = Program.processor.m_server;
            Character currentCharacter = player.m_activeCharacter;
            Vector3 oldPosition = currentCharacter.m_CharacterPosition.m_position;
            //real message
            //set their position and
            //tell them where they are going
        
            currentCharacter.m_CharacterPosition.m_position = position;
            currentCharacter.m_CharacterPosition.m_yangle = angle;
            Vector3 direction = Utilities.GetDirectionFromYAngle(angle);
            //float dirx = (float)-Math.Sin((double)(180 - currentCharacter.m_CharacterPosition.m_yangle) * Math.PI / 180);
            //float dirz = (float)Math.Cos((double)(180 - currentCharacter.m_CharacterPosition.m_yangle) * Math.PI / 180);
            currentCharacter.m_CharacterPosition.m_direction = direction;
            currentCharacter.m_ConfirmedPosition.m_position = position;
            currentCharacter.m_ConfirmedPosition.m_yangle = angle;
            currentCharacter.m_ConfirmedPosition.m_direction = direction;
            SendPlayerCorrectionMessage(server, player, position, zoneID, angle);
            //reset their speed - so that it doesn't run off while they are loading
            currentCharacter.m_CharacterPosition.m_currentSpeed = 0;
            if (currentCharacter.StatusCancelConditions.Move)
            {
                currentCharacter.CancelEffectsDueToMove();
            }
            player.m_activeCharacter.TheCharactersPath.ClearList();
            if (player.m_activeCharacter.CurrentDuelTarget != null)
            {
                player.m_activeCharacter.CurrentDuelTarget.ForceEndDuel(player.m_activeCharacter, "");
            }
            if (player.m_activeCharacter.CurrentRequest != null)
            {
                player.m_activeCharacter.CurrentRequest.CancelRequest(player, PendingRequest.CANCEL_CONDITION.CC_SELF_CANCEL);
            }
            if (player.m_activeCharacter.m_tradingWith != null)
            {
                Player otherPlayer = player.m_activeCharacter.m_tradingWith;
                player.m_activeCharacter.cancelTrade();

                if (otherPlayer != null && otherPlayer.m_activeCharacter != null)
                {
                    Program.Display(player.m_activeCharacter.m_name + " cancelling trade with " + otherPlayer.m_activeCharacter.m_name);
					string locText = Localiser.GetString(textDB, otherPlayer, (int)ZoneOfferTextDB.TextID.OTHER_CANCELLED_TRADE);
					locText = String.Format(locText, player.m_activeCharacter.Name);
					Program.processor.sendSystemMessage(locText, otherPlayer, false, SYSTEM_MESSAGE_TYPE.TRADE);
                }
            }
            player.m_activeCharacter.ForfeitPVP();

         

            //if it's not in the same zone deal with it
            if (zoneID != m_zone_id)
            {
                //add the player to the next zone
                //Program.processor.AddPlayerToZone(player, spawnPoint.ZoneID);
                //remove the player
                player.m_activeCharacter.m_zone = Program.processor.getZone(zoneID);
                player.m_activeCharacter.InLimbo = true;
                player.m_activeCharacter.saveNewZone();
                removePlayer(player, server);
                player.m_activeCharacter.UpdateSocialLists();

            }
            else
            {
                //change the current partition if required
                currentCharacter.EntityPartitionCheck();
                //tell him where all the near by mobs are now
                sendDumbMobPatrolUpdate(player);
                SendPositionUpdates(Program.Server, player, false, currentCharacter.m_lastUpdatedNearby);
                //send everyone notification this is a teleport and not a move
                List<NetConnection> connections = new List<NetConnection>();
                AddConnectionsOfPlayersToList(connections, m_players, player);
                NetOutgoingMessage teleportMessage = CreateCharacterCorrectionMessage(Program.Server, currentCharacter, position, zoneID, angle);
                Program.processor.SendMessage(teleportMessage, connections, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.CorrectPlayerPosition);
            }
        }
        public CombatEntity GetTargetFor(int targetType, int targetID, Vector3 position)
        {
            CombatEntity theTarget = null;
            if (targetType == (int)TARGET_TYPE.MOB)
            {
                theTarget = getMobFromIDWithinRangeOf(position, MAX_ACTION_INTEREST_RANGE, targetID); //getMobFromID(targetID);
            }
            else if (targetType == (int)TARGET_TYPE.SELF || targetType == (int)TARGET_TYPE.OTHER_PLAYER)
            {
                theTarget = GetCharacterForIDWithinRangeOf(position, MAX_ACTION_INTEREST_RANGE, targetID);//GetCharacterForID(targetID);
            }
           
            return theTarget;
        }

        public void ReadUseSkillMessage(NetIncomingMessage msg, NetServer server, Player player)
        {
            double serverTime = msg.ReadDouble();
            int targetType = msg.ReadVariableInt32();
            int targetID = (int)msg.ReadVariableUInt32();
            SKILL_TYPE skillID = (SKILL_TYPE)msg.ReadVariableUInt32();
            EntitySkill entitySkill = player.m_activeCharacter.GetEnitySkillForID(skillID,false);
			
            //if the skill doesn't exist send feedback
            if (entitySkill == null)
            {
				string locText = Localiser.GetString(textDB, player, (int)ZoneOfferTextDB.TextID.UNKNOWN_SKILL);
				Program.processor.sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
				Program.Display("Unknown Skill" + skillID + " used by " + player.m_activeCharacter.m_character_id + " on target " + targetID);
                player.m_activeCharacter.SendSkillUpdate((int)skillID, 0, 0.0f);
				return;	            
            }

			//check if this skill is related to companion/hunger
			if (player.m_activeCharacter.m_inventory.CheckSkillValidForCompanionHunger(entitySkill, player) == false)
	        {
				//bail
				Program.Display("Possible bug or hacked client. Stopping pet skill being used while hungry. player." 
					+ player.m_account_id + " character." + player.m_activeCharacter.m_character_id + " skillId." + skillID);
				player.m_activeCharacter.SendSkillUpdate((int)skillID, 0, 0.0f);
				//update the player			
				Program.processor.SendPlayerCompanionSkillToggled((int)skillID, false, player);
				return;
	        }

            if (player.m_activeCharacter.m_inventory.CheckSkillValidDismounted(entitySkill,player)==false)
            {
                //bail
                Program.Display("Possible bug or hacked client. Stopping mount skill being used while hungry. player."
                    + player.m_account_id + " character." + player.m_activeCharacter.m_character_id + " skillId." + skillID);
                player.m_activeCharacter.SendSkillUpdate((int)skillID, 0, 0.0f);
                //update the player			
                Program.processor.SendPlayerMountSkillToggled((int)skillID, false, player);
                return;
            }
            
            bool skillsSucceded = false;
            //find the target
            if (targetType == (int)TARGET_TYPE.MOB)
            {
                skillsSucceded = UseSkillOnMob(serverTime, targetID, entitySkill, player, false);
            }
            else if (targetType == (int)TARGET_TYPE.SELF)
            {
                skillsSucceded = UseSkillOnPlayer(serverTime, (int)player.m_activeCharacter.m_character_id, entitySkill, player, false);
            }
            else if (targetType == (int)TARGET_TYPE.OTHER_PLAYER)
            {
                skillsSucceded = UseSkillOnPlayer(serverTime, targetID, entitySkill, player, false);
            }
            if (skillsSucceded == true)
            {
                player.m_activeCharacter.CurrentPosition.m_currentSpeed = 0;
            }
            //make sure the player's character is active
            /*  if ((player.m_activeCharacter == null) || (target == null))
                  return;

              //tell the combat manager to check and use the skill
              //if the character should be able to attack them then attack
              //check the relationship
              if (target.OpinionBase < 50)                
                  m_combatManager.UseSkillOnEntity(skillID, player.m_activeCharacter.CombatData, target.CombatData);*/

        }
        internal void SendResetDeathVariable(Player player)
        {


            Character currentCharacter = player.m_activeCharacter;
            //if the active character exists
            if (currentCharacter != null)
            {
                //creat a message for this player
                NetOutgoingMessage dmgMsg = Program.Server.CreateMessage();
                dmgMsg.WriteVariableUInt32((uint)NetworkCommandType.CombatDamageMessage);

                dmgMsg.WriteVariableInt32(0);
                dmgMsg.WriteVariableInt32(0);

                //write the damage list to the message
                //write the number of damage Data -it's a fake so only 1
                dmgMsg.WriteVariableInt32(1);


                //type eg, player to player
                dmgMsg.WriteVariableInt32((int)CombatManager.DamageMessageType.PlayerToPlayer);
                //caster ID
                dmgMsg.WriteVariableInt32((int)currentCharacter.m_character_id);
                //target id
                dmgMsg.WriteVariableInt32((int)currentCharacter.m_character_id);
                //attack type
                dmgMsg.WriteVariableInt32((int)CombatManager.ATTACK_TYPE.STATUS_EFFECT);
                //attackID
                dmgMsg.WriteVariableInt32(0);
                //damage
                dmgMsg.WriteVariableInt32(0);
                //remaining health
                dmgMsg.WriteVariableInt32(0);
                //damage ID
                dmgMsg.WriteVariableInt32(-1);

                dmgMsg.WriteVariableInt32((int)CombatManager.COMBAT_REACTION_TYPES.CRT_HIT);

                //nothing is cancelled
                dmgMsg.WriteVariableInt32(0);
                //get all entities in range and send all skill data
                //we don't want to sent it in this case as this is a fake


                Program.processor.SendMessage(dmgMsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.CombatDamageMessage);
            }
        }

        internal void UpdatePlayerDamageAndHealing(double timeSinceLastFrame)
        {
            for (int i = 0; i < m_players.Count; i++)
            {
                if (m_players[i] == null || m_players[i].m_activeCharacter == null)
                {
                    continue;
                }

                Character currentCharacter = m_players[i].m_activeCharacter;

                if (currentCharacter.InCombat == false)
                {
                    currentCharacter.timeInCombat = 1.0;
                    currentCharacter.damageDone   = 0;
                    currentCharacter.healingDone  = 0;
                }
                else
                {
                    int damageDone  = 0;
                    int healingDone = 0;

                    GetPlayerDamageAndHealing(currentCharacter, ref damageDone, ref healingDone);

                    currentCharacter.timeInCombat += timeSinceLastFrame;
                    currentCharacter.damageDone   += damageDone;
                    currentCharacter.healingDone  += healingDone;
                }
            }
        }

        internal void SendCombatUpdate()
        {

            //nothing to do this frame
            if (m_combatUpdateNeedsToBeSent == false && m_deathChangedThisFrame.Count == 0 && m_cancelledDamages.Count == 0)
            { return; }

            //damaged
            List<CombatEntity> damagedEntities = m_damagedThisFrame;
            //go through the players
            m_combatUpdateNeedsToBeSent = false;
            for (int i = 0; i < m_players.Count; i++)
            {
                //creat a message for this player
                NetOutgoingMessage dmgMsg = Program.Server.CreateMessage();
                dmgMsg.WriteVariableUInt32((uint)NetworkCommandType.CombatDamageMessage);
                Player currentPlayer = m_players[i];
                Character currentCharacter = currentPlayer.m_activeCharacter;
                //if the active character exists
                if (currentCharacter != null)
                {
                    int dps = 0;
                    int hps = 0;

                    GetPlayerDpsAndHps(currentCharacter, ref dps, ref hps);

                    dmgMsg.WriteVariableInt32(dps);
                    dmgMsg.WriteVariableInt32(hps);

                    //if the damage list is not empty
                    // if (m_players[i].m_activeCharacter.CombatData.DamageListThisFrame.Count > 0)
                    //{
                    //write the damage list to the message
                    List<CombatDamageMessageData> allDamage = new List<CombatDamageMessageData>(currentCharacter.CombatListThisFrame);
                    AddNearBySkillAndAttackDamageToList(allDamage, currentCharacter, currentCharacter.CurrentPosition.m_position, playerUpdateDistance);
                    WriteDamageListToMessage(dmgMsg, allDamage);
                    WriteCancelledDamageListToMessage(dmgMsg, m_cancelledDamages);

                    //get all entities in range and send all skill data

                    WriteAllChararactersCombatDataInRange(damagedEntities, currentPlayer, dmgMsg);
                    WriteDeathInfoToMessage(currentPlayer, dmgMsg);
                    //}
                }

                Program.processor.SendMessage(dmgMsg, currentPlayer.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.CombatDamageMessage);


            }

            ClearCombatLists();
        }

        //
        void GetPlayerDpsAndHps(Character currentCharacter, ref int dps, ref int hps)
        {
            if (currentCharacter.InCombat == true)
            {
                dps = (int)(currentCharacter.damageDone / currentCharacter.timeInCombat);
                hps = (int)(currentCharacter.healingDone / currentCharacter.timeInCombat);
            }
        }

        // Prototyping displaying player dps and hps //
        void GetPlayerDamageAndHealing(Character currentCharacter, ref int damageThisFrame, ref int healingThisFrame)
        {
            if (currentCharacter == null || currentCharacter.CombatListThisFrame == null)
            {
                return;
            }

            if (currentCharacter.InCombat == false)
            {
                return;
            }

            foreach (CombatDamageMessageData combatData in currentCharacter.CombatListThisFrame)
            {
                if (combatData == null)
                {
                    continue;
                }

                // Only interested in our combat data
                if (combatData.CasterID == currentCharacter.m_character_id)
                {
                    // Damage
                    if (combatData.SentDamage > 0)
                    {
                        damageThisFrame += combatData.SentDamage;
                    }

                    // Healing (make it positive)
                    if (combatData.SentDamage < 0)
                    {
                        healingThisFrame += -combatData.SentDamage;
                    }
                }
            }
        }

        /// <summary>
        /// adds any Entities that tried to carry out an attack in this combat cycle
        /// </summary>
        /// <param name="range"></param>
        /// <param name="nearbyEntities"></param>
        void GetNearbyEntitiesInCombatThisCycle(Vector3 position, float range, List<CombatEntity> nearbyEntities, CombatEntity entityToExclude)
        {
            List<CombatEntity> allCombatEntities = m_combatListChangedThisFrame;
            float rangeSQR = range * range;

            for (int i = 0; i < allCombatEntities.Count; i++)
            {
                CombatEntity currentEntity = allCombatEntities[i];
                if ((currentEntity != null) && (currentEntity != entityToExclude))
                {
                    if (Utilities.Difference2DSquared(currentEntity.CurrentPosition.m_position, position) < rangeSQR)
                    {
                        nearbyEntities.Add(currentEntity);
                    }
                }
            }

        }
        public void AddNearBySkillAndAttackDamageToList(List<CombatDamageMessageData> theList, CombatEntity entityToExclude, Vector3 position, float range)
        {
            List<CombatEntity> nearbyEntities = new List<CombatEntity>();

            GetNearbyEntitiesInCombatThisCycle(position, range, nearbyEntities, entityToExclude);
            for (int j = 0; j < nearbyEntities.Count; j++)
            {
                CombatEntity currentEntity = nearbyEntities[j];
                if (currentEntity != null)
                {
                    for (int currentComdatDataIndex = 0; currentComdatDataIndex < currentEntity.CombatListThisFrame.Count; currentComdatDataIndex++)
                    {
                        CombatDamageMessageData currentData = currentEntity.CombatListThisFrame[currentComdatDataIndex];
                        //is it a skill
                        //was it cast on the entity to exclude (all entities hold skills cast on them, or by them)
                        //if ((currentData != null) && (currentData.AttackType == (int)CombatManager.ATTACK_TYPE.SKILL))

                        if ((currentData != null) &&
                            ((currentData.AttackType == (int)CombatManager.ATTACK_TYPE.AOE_SKILL && (currentData.CasterLink == currentEntity)) || 
                            (currentData.AttackType == (int)CombatManager.ATTACK_TYPE.SKILL)  //add skill damage
                            ||((currentData.CasterLink == currentEntity)&&//add attacks from this entity(to avoid duplicates)
                            ((currentData.AttackType == (int)CombatManager.ATTACK_TYPE.ATTACK)||(currentData.AttackType == (int)CombatManager.ATTACK_TYPE.ATTACK_TRIGGERED_SKILL)))) &&
                            (currentData.TargetLink != entityToExclude) && //the main character will have a much more detailed list
                            (currentData.CasterLink != entityToExclude) )
                        {
                            theList.Add(currentData);
                        }
                    }
                }
            }
        }
        public void SendSkillInterruptedMessage()
        {
            List<CombatEntity> skillCancelledList = m_combatManager.EntitiesWithCancelledSkills;

            for (int i = 0; i < skillCancelledList.Count; i++)
            {
                CombatEntity currentEntity = skillCancelledList[i];

                if ((currentEntity != null) && (currentEntity.Type == CombatEntity.EntityType.Player))
                {
                    Character currentCharacter = (Character)currentEntity;
                    Player thePlayer = currentCharacter.m_player;
                    if (thePlayer != null)
                    {
                        //message layout
                        //Cancel Skill message type
                        //cancelled Skill ID
                        //recharge Time
                        NetOutgoingMessage dmgMsg = Program.Server.CreateMessage();
                        dmgMsg.WriteVariableUInt32((uint)NetworkCommandType.SkillInterrupted);
                        if (currentCharacter.LastSkill != null)
                        {
                            dmgMsg.WriteVariableInt32((int)currentCharacter.LastSkill.SkillID);
                            dmgMsg.Write(0.0f);
                        }
                        else
                        {
                            dmgMsg.WriteVariableInt32(-1);
                            dmgMsg.Write(-1.0f);
                        }
                        Program.processor.SendMessage(dmgMsg, thePlayer.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.SkillInterrupted);
                    }

                }
            }
            m_combatManager.EntitiesWithCancelledSkills.Clear();


        }
        void WriteDamageListToMessage(NetOutgoingMessage dmgMsg, List<CombatDamageMessageData> damageList)
        {


            //write the number of damage Data
            dmgMsg.WriteVariableInt32(damageList.Count);
            for (int currentDamageData = 0; currentDamageData < damageList.Count; currentDamageData++)
            {
                CombatDamageMessageData currentDamage = damageList[currentDamageData];
                dmgMsg.WriteVariableInt32((int)currentDamage.MessageType);

                dmgMsg.WriteVariableInt32(currentDamage.CasterID);
                dmgMsg.WriteVariableInt32(currentDamage.TargetID);

                dmgMsg.WriteVariableInt32(currentDamage.AttackType);
                dmgMsg.WriteVariableInt32(currentDamage.SkillID);

                dmgMsg.WriteVariableInt32(currentDamage.SentDamage);
                dmgMsg.WriteVariableInt32(currentDamage.TargetHealth);

                dmgMsg.WriteVariableInt32(currentDamage.Reaction);
                dmgMsg.WriteVariableInt32(currentDamage.DamageID);

                dmgMsg.WriteVariableInt32(currentDamage.Critical);
            }

        }

        void WriteCancelledDamageListToMessage(NetOutgoingMessage dmgMsg, List<CombatDamageMessageData> damageList)
        {


            //write the number of damage Data
            dmgMsg.WriteVariableInt32(damageList.Count);
            for (int currentDamageData = 0; currentDamageData < damageList.Count; currentDamageData++)
            {
                CombatDamageMessageData currentDamage = damageList[currentDamageData];
                dmgMsg.WriteVariableInt32((int)currentDamage.MessageType);

                dmgMsg.WriteVariableInt32(currentDamage.CasterID);
                dmgMsg.WriteVariableInt32(currentDamage.TargetID);
                dmgMsg.WriteVariableInt32(currentDamage.DamageID);

            }

        }

       

        internal int WriteAllChararactersCombatDataInRange(List<CombatEntity> entitiesInCombat, Player player, NetOutgoingMessage dmgMsg)
        {
            if (entitiesInCombat.Count <= 0)
            {
                return 0;
            }
            //get the combat entities in range
            //only keep the ones with a damage List

            //the number of characters data being sent
            int dataSent = 0;
            Character playersCharacter = null;
            Vector3 playerPosition = new Vector3(0);
            if (player != null)
            {
                playersCharacter = player.m_activeCharacter;
                playerPosition = player.m_activeCharacter.m_CharacterPosition.m_position;
            }
            if (playersCharacter == null)
            {
                return 0;
            }

            for (int i = 0; i < entitiesInCombat.Count; i++)
            {

                //if the current Entity is in range
                //float distance = 0;
               /* if (player != null)
                {
                    distance = Utilities.Difference2D(entitiesInCombat[i].CurrentPosition.m_position, playerPosition);
                }*/
                Party enititiesParty = null;
                if (entitiesInCombat[i].Type == CombatEntity.EntityType.Player)
                {
                    Character theCharacter = (Character)entitiesInCombat[i];
                    enititiesParty = theCharacter.CharacterParty;
                }
                CombatEntity currentEnt = entitiesInCombat[i];
                if (playersCharacter==currentEnt||(playersCharacter.IsInterestedInEntity(currentEnt)==true) || ((player.m_activeCharacter.CharacterParty != null) && (enititiesParty == player.m_activeCharacter.CharacterParty)))
                {
                    

                    //mob type
                    dmgMsg.WriteVariableInt32((int)currentEnt.Type);
                    //mob id 
                    dmgMsg.WriteVariableInt32(currentEnt.ServerID);
                    //mob health
                    if (currentEnt.Type == CombatEntity.EntityType.Mob || currentEnt == playersCharacter)
                    {
                        dmgMsg.WriteVariableInt32(currentEnt.CurrentHealth);
                        dmgMsg.WriteVariableInt32(currentEnt.CurrentConcentrationFishing);
                    }
                    else
                    {
                        int healthPercent = (int)Math.Ceiling((float)(currentEnt.CurrentHealth * 100) / (float)currentEnt.MaxHealth);
                        if (currentEnt.CurrentHealth <= 0)
                        {
                            healthPercent = 0;
                        }
                        int concentrationPercent = (int)Math.Ceiling((float)(currentEnt.CurrentConcentrationFishing * 100) / (float)currentEnt.MaxConcentrationFishing);
                        if (currentEnt.CurrentConcentrationFishing <= 0)
                        {
                            concentrationPercent = 0;
                        }
                        dmgMsg.WriteVariableInt32(healthPercent);
                        dmgMsg.WriteVariableInt32(concentrationPercent);
                    }

                    dataSent++;

                }
               

            }

            

            return dataSent;
        }

        int WriteDeathInfoToMessage(Player player, NetOutgoingMessage dmgMsg)
        {
            int dataSent = 0;

            Character playersCharacter = player.m_activeCharacter;
            //add on any dead players/mobs
            for (int i = 0; i < m_deathChangedThisFrame.Count; i++)
            {
                CombatEntity currentEnt = m_deathChangedThisFrame[i];

                //mob type
                dmgMsg.WriteVariableInt32((int)currentEnt.Type);
                //mob id 
                dmgMsg.WriteVariableInt32(currentEnt.ServerID);
                //mob health
                if (currentEnt.Type == CombatEntity.EntityType.Mob || currentEnt == playersCharacter)
                {
                    dmgMsg.WriteVariableInt32(currentEnt.CurrentHealth);
                }
                else
                {
                    int healthPercent = (int)Math.Ceiling((float)(currentEnt.CurrentHealth * 100) / (float)currentEnt.MaxHealth);
                    if (currentEnt.CurrentHealth <= 0)
                    {
                        healthPercent = 0;
                    }
                    dmgMsg.WriteVariableInt32(healthPercent);
                }
                dataSent++;
            }
            return dataSent;
        }


		/// <summary>
		/// Update client with new status effects
		/// </summary>
		/// <param name="player"></param>
		/// <param name="currentTime"></param>
        internal void SendCharactersUpdatedStatus(Player player, double currentTime)
        {
			
            NetOutgoingMessage statusMessage = Program.Server.CreateMessage();
            statusMessage.WriteVariableUInt32((uint)NetworkCommandType.StatusEffectUpdate);



            player.m_activeCharacter.WriteStatusEffectsToMessage(statusMessage, currentTime);
			
            player.m_activeCharacter.WriteUpdatedStatsToMessage(statusMessage);
            player.m_activeCharacter.CompiledStatsSent();

            Program.processor.SendMessage(statusMessage, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.StatusEffectUpdate);

            player.m_activeCharacter.StatusListChanged = false;


        }
        internal void ProcessPartyMessage(NetIncomingMessage msg, Player player)
        {
            PartyMessageType messageType = (PartyMessageType)msg.ReadVariableInt32();

            switch (messageType)
            {
                case PartyMessageType.PartyRequestFromPlayer:
                    {
                        if (player.m_activeCharacter != null)
                        {
                            player.m_activeCharacter.m_zone.ProcessPartyInvite(msg, player);
                        }
                        break;
                    }
                case PartyMessageType.PartyReplyFromPlayer:
                    {
                        if (player.m_activeCharacter != null)
                        {
                            player.m_activeCharacter.m_zone.ProcessPartyReply(msg, player);
                        }
                        break;
                    }
                case PartyMessageType.LeaveParty:
                    {
                        player.m_activeCharacter.m_zone.processLeaveParty(msg, player);
                        break;
                    }


            }
        }
        internal void ProcessPartyInvite(NetIncomingMessage msg, Player player)
        {
            int invitedCharacterID = msg.ReadVariableInt32();
            Player playerToInvite = Program.processor.getPlayerFromActiveCharacterId(invitedCharacterID);

            Character invitedCharacter = null;//GetCharacterForID(invitedCharacterID);
            if (playerToInvite != null)
            {
                invitedCharacter = playerToInvite.m_activeCharacter;
            }
            if (invitedCharacter == null)
            {
                //invalid invite, only characters in the same zone can be invited to a party
                return;
            }
            if (Party.CanFormParty(player.m_activeCharacter, invitedCharacter, true) == true)
            {
                //invite them   
                if ((player.m_activeCharacter != null) && (invitedCharacter.HasBlockedCharacter((int)player.m_activeCharacter.m_character_id) == false))
                {
                    if (invitedCharacter.CanTakeRequest()==true)
                    {
                        SendPartyRequest(player, playerToInvite);
                    }
                    else
                    {
						Program.processor.sendSystemMessage(player.m_activeCharacter.GetPlayerBusyString(), player, false,SYSTEM_MESSAGE_TYPE.CLAN);
                    }
                }
            }

        }
        internal void SendPartyRequest(Player requestingPlayer, Player invitedPlayer)
        {
            NetOutgoingMessage partyMsg = Program.Server.CreateMessage();
            partyMsg.WriteVariableUInt32((uint)NetworkCommandType.PartyMessage);
            partyMsg.WriteVariableInt32((int)PartyMessageType.PartyRequestFromServer);
            // partyMsg.WriteVariableUInt32((uint)NetworkCommandType.PartyRequestFromServer);
            partyMsg.WriteVariableInt32((int)requestingPlayer.m_activeCharacter.m_character_id);
            Program.processor.SendMessage(partyMsg, invitedPlayer.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.PartyMessage);
        }
        internal void ProcessPartyReply(NetIncomingMessage msg, Player player)
        {
            int characterID = msg.ReadVariableInt32();
            int replyType = msg.ReadVariableInt32();
            // Character requestingCharacter = GetCharacterForID(characterID);
            Player requestingPlayer = Program.processor.getPlayerFromActiveCharacterId(characterID);

            Character requestingCharacter = null;//GetCharacterForID(invitedCharacterID);
            if (requestingPlayer != null)
            {
                requestingCharacter = requestingPlayer.m_activeCharacter;
            }
            if (replyType == (int)HW_FRIEND_REPLY.HW_FRIEND_REPLY_ACCEPT)
            {
                if (Party.CanFormParty(player.m_activeCharacter, requestingCharacter, true) == true)
                {
                    //add them to the party
                    Party resultingParty = Party.CombineParties(requestingCharacter, player.m_activeCharacter);

                    if (resultingParty != null)
                    {
                        resultingParty.SendNewPartyConfiguration();

						string locText = Localiser.GetString(textDB, player, (int)ZoneOfferTextDB.TextID.PLAYER_JOINED_GROUP);
						locText = String.Format(locText, requestingCharacter.m_name);
						Program.processor.sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.PARTY);
                        Program.processor.m_parties.Add(resultingParty);
                    }
                    //player.m_activeCharacter.PartyMembers.Add(requestingCharacter);
                    //requestingCharacter.PartyMembers.Add(player.m_activeCharacter);
                }
                else
                {
                    // the party has got to big since the request was sent
                    return;
                }
            }
            SendPartyReply(player, requestingPlayer, replyType);

        }
        internal void processLeaveParty(NetIncomingMessage msg, Player player)
        {
            if (player.m_activeCharacter != null)
            {
                if (player.m_activeCharacter.CharacterParty != null)
                {
                    player.m_activeCharacter.CharacterParty.RemovePlayer(player.m_activeCharacter,true);
                }
            }
        }
        internal void SendPartyReply(Player invitedPlayer, Player requestingPlayer, int replyType)
        {
            NetOutgoingMessage partyMsg = Program.Server.CreateMessage();
            partyMsg.WriteVariableUInt32((uint)NetworkCommandType.PartyMessage);
            partyMsg.WriteVariableInt32((int)PartyMessageType.PartyReplyFromServer);
            //partyMsg.WriteVariableUInt32((uint)NetworkCommandType.PartyReplyFromServer);
            partyMsg.WriteVariableInt32((int)invitedPlayer.m_activeCharacter.m_character_id);
            partyMsg.WriteVariableInt32(replyType);
            Program.processor.SendMessage(partyMsg, requestingPlayer.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.PartyMessage);
        }
        internal void SendUpdatedCharacerToAllPlayers(NetServer m_server, Character theCharacter)
        {
            NetOutgoingMessage othersmsg = m_server.CreateMessage();
            List<NetConnection> connections = getUpdateList(theCharacter.m_player);
            othersmsg.WriteVariableUInt32((uint)NetworkCommandType.PlayerAppearanceUpdate);
            othersmsg.WriteVariableUInt32(theCharacter.m_character_id);
            theCharacter.writeUpdateInfoToMessage(othersmsg);
            Program.processor.SendMessage(othersmsg, connections, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.PlayerAppearanceUpdate);
        }
        #endregion //networking

        #region combat handling
        internal bool UseSkillOnPlayer(double serverTime, int targetID, EntitySkill entitySkill, Player player, bool instant)
        {
            //did the skill succeed
            bool skillSucceeded = false;
            //find the target
            Character target = null;
	       

            if (player.m_activeCharacter != null)
            {
                target = GetCharacterForIDWithinRangeOf(player.m_activeCharacter.CurrentPosition.m_position, MAX_ACTION_INTEREST_RANGE, targetID);//GetCharacterForID(targetID);
            }
            if ((player.m_activeCharacter == null) || (target == null))
            {
                Program.Display("Error null player Skill Target with ID " + targetID);
                return false;
            }
            //find the skill

            if (entitySkill == null)
            {
                return false;
            }
            //can it be used on this Target
            int targetError = SkillTemplate.CheckSkillForUseAgainst(target, player.m_activeCharacter, entitySkill.Template.CastTargetGroup);

            if (entitySkill.SkillID == SKILL_TYPE.RESCUE && player.m_activeCharacter == target)
            {
                targetError = (int)SkillTemplate.SkillTargetError.InvalidTarget;
            }
            bool validAction = targetError == (int)SkillTemplate.SkillTargetError.NoError;
                               

            //bool validAction = SkillTemplate.CheckSkillForUseAgainst(target, player.m_activeCharacter, entitySkill.Template.CastTargetGroup);
            //tell the combat manager to check and use the skill
            //if the character should be able to attack them then attack
            //check the relationship
            if (validAction == false)
            {
                if (targetError == (int)SkillTemplate.SkillTargetError.NotAlly)
                {
					string locText = Localiser.GetString(textDB, player, (int)ZoneOfferTextDB.TextID.TARGET_NOT_SAME_PVP_MODE);
					Program.processor.sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.SKILLS);
				}
                else
                {
					string locText = Localiser.GetString(textDB, player, (int)ZoneOfferTextDB.TextID.INVALID_SKILL_TARGET);
					string skillName = SkillTemplateManager.GetLocaliseSkillName(player, entitySkill.Template.SkillID);
					locText = String.Format(locText, skillName);
					Program.processor.sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.SKILLS);
				} 
                player.m_activeCharacter.SendSkillUpdate((int)entitySkill.Template.SkillID, entitySkill.SkillLevel, 0.0f);
                return false;
            }
            //is the equipment right
            validAction = entitySkill.Template.EquipmentPassesRequirement(player.m_activeCharacter);
            if (validAction == false)
            {
				string locText = Localiser.GetString(textDB, player, (int)ZoneOfferTextDB.TextID.INVALID_SKILL_EQUIPMENT);
				string skillName = SkillTemplateManager.GetLocaliseSkillName(player, entitySkill.Template.SkillID);
				locText = String.Format(locText, skillName);
				Program.processor.sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.SKILLS);
                player.m_activeCharacter.SendSkillUpdate((int)entitySkill.Template.SkillID, entitySkill.SkillLevel, 0.0f);
                return false;
            }
            validAction = CombatManager.TargetInRange(player.m_activeCharacter, target, entitySkill);
            if (validAction == false)
            {
				string locText = Localiser.GetString(textDB, player, (int)ZoneOfferTextDB.TextID.SKILL_OUT_OF_RANGE);
				string skillName = SkillTemplateManager.GetLocaliseSkillName(player, entitySkill.Template.SkillID);
				locText = String.Format(locText, skillName);
				Program.processor.sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.SKILLS);
                player.m_activeCharacter.SendSkillUpdate((int)entitySkill.Template.SkillID, entitySkill.SkillLevel, 0.0f);
                return false;
            }
            if (instant == false)
            {
                //if there was a skill pending send the cancellation
                if (player.m_activeCharacter.NextSkill != null)
                {
                    player.m_activeCharacter.SendSkillUpdate((int)player.m_activeCharacter.NextSkill.Template.SkillID, player.m_activeCharacter.NextSkill.SkillLevel, 0.0f);
                    player.m_activeCharacter.NextSkill = null;
                    player.m_activeCharacter.NextSkillTarget = null;
                }


                m_combatManager.UseSkillOnEntity(entitySkill, player.m_activeCharacter, target);
                skillSucceeded = true;
            }
            else
            {
                //items shouldn't change next action time
                double oldTimeActionWillComplete = player.m_activeCharacter.TimeActionWillComplete;
                skillSucceeded = m_combatManager.CastSkill(player.m_activeCharacter, target, entitySkill, null);

                player.m_activeCharacter.TimeActionWillComplete = oldTimeActionWillComplete;
            }
            return skillSucceeded;
        }

        internal bool UseSkillOnMob(double serverTime, int targetID, EntitySkill entitySkill, Player player, bool instant)
        {
            ServerControlledEntity target = getMobFromIDWithinRangeOf(player.m_activeCharacter.CurrentPosition.m_position, MAX_ACTION_INTEREST_RANGE, targetID);//getMobFromID(targetID);

            if ((player.m_activeCharacter == null) || (target == null))
            {

                if (player.m_activeCharacter != null)
                {
                    Program.Display("Error null mob Skill Target with ID " + targetID);
                    if (entitySkill != null)
                    {
                        player.m_activeCharacter.SendSkillUpdate((int)entitySkill.Template.SkillID, entitySkill.SkillLevel, 0.0f);
						string locText = Localiser.GetString(textDB, player, (int)ZoneOfferTextDB.TextID.INVALID_SKILL_TARGET);
						string skillName = SkillTemplateManager.GetLocaliseSkillName(player, entitySkill.Template.SkillID);
						locText = String.Format(locText, skillName);
						Program.processor.sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.SKILLS);
					}
                }
                return false;
            }

            if (entitySkill == null)
            {
                Program.Display("Error failed to find Skill for character " + player.m_activeCharacter.m_character_id);
                //player.m_activeCharacter.SendSkillUpdate((int)entitySkill.Template.SkillID, entitySkill.SkillLevel, 0.0f);
                return false;
            }
            //can it be used on this Target
            int targetError = SkillTemplate.CheckSkillForUseAgainst(target, player.m_activeCharacter, entitySkill.Template.CastTargetGroup);
            bool validAction = targetError == (int)SkillTemplate.SkillTargetError.NoError;
          
            //bool validAction = SkillTemplate.CheckSkillForUseAgainst(target, player.m_activeCharacter, entitySkill.Template.CastTargetGroup);
            //tell the combat manager to check and use the skill
            //if the character should be able to attack them then attack
            //check the relationship
            if (validAction == false)
            {
				string locText = Localiser.GetString(textDB, player, (int)ZoneOfferTextDB.TextID.INVALID_SKILL_TARGET);
				string skillName = SkillTemplateManager.GetLocaliseSkillName(player, entitySkill.Template.SkillID);
				locText = String.Format(locText, skillName);
				Program.processor.sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.SKILLS);
				player.m_activeCharacter.SendSkillUpdate((int)entitySkill.Template.SkillID, entitySkill.SkillLevel, 0.0f);
                return false;
            }
            //is the equipment right
            validAction = entitySkill.Template.EquipmentPassesRequirement(player.m_activeCharacter);
            if (validAction == false)
            {
				string locText = Localiser.GetString(textDB, player, (int)ZoneOfferTextDB.TextID.INVALID_SKILL_EQUIPMENT);
				string skillName = SkillTemplateManager.GetLocaliseSkillName(player, entitySkill.Template.SkillID);
				locText = String.Format(locText, skillName);
				Program.processor.sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.SKILLS);
				player.m_activeCharacter.SendSkillUpdate((int)entitySkill.Template.SkillID, entitySkill.SkillLevel, 0.0f);
                return false;
            }
            validAction = CombatManager.TargetInRange(player.m_activeCharacter, target, entitySkill);
            if (validAction == false)
            {
				string locText = Localiser.GetString(textDB, player, (int)ZoneOfferTextDB.TextID.SKILL_OUT_OF_RANGE);
				string skillName = SkillTemplateManager.GetLocaliseSkillName(player, entitySkill.Template.SkillID);
				locText = String.Format(locText, skillName);
				Program.processor.sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.SKILLS);
				player.m_activeCharacter.SendSkillUpdate((int)entitySkill.Template.SkillID, entitySkill.SkillLevel, 0.0f);
                return false;
            }

            int energyCost = entitySkill.getSkillTemplateLevel(false).EnergyCost;
            if(!m_combatManager.CanAffordSkill(energyCost,player.m_activeCharacter, entitySkill))
            {
                if (entitySkill.SkillID == SKILL_TYPE.SACRIFICE)
                {
					string locText = Localiser.GetString(textDB, player, (int)ZoneOfferTextDB.TextID.NOT_ENOUGH_HEALTH_CAST_SKILL);
					Program.processor.sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
				}
                else
                {
					string locText = Localiser.GetString(textDB, player, (int)ZoneOfferTextDB.TextID.NOT_ENOUGHT_ENERGY_CAST_SKILL);
					Program.processor.sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
				}
            }

            if (instant == false)
            {
                //if there was a skill pending send the cancellation
                if (player.m_activeCharacter.NextSkill != null)
                {
                    player.m_activeCharacter.SendSkillUpdate((int)player.m_activeCharacter.NextSkill.Template.SkillID, player.m_activeCharacter.NextSkill.SkillLevel, 0.0f);
                    player.m_activeCharacter.NextSkill = null;
                    player.m_activeCharacter.NextSkillTarget = null;
                }
                m_combatManager.UseSkillOnEntity(entitySkill, player.m_activeCharacter, target);
            }
            else
            {
                //items shouldn't change next action time
                double oldTimeActionWillComplete = player.m_activeCharacter.TimeActionWillComplete;

                bool success = m_combatManager.CastSkill(player.m_activeCharacter, target, entitySkill, null);
                //m_combatManager.DoSkillDamage(player.m_activeCharacter, target, entitySkill);

                player.m_activeCharacter.TimeActionWillComplete = oldTimeActionWillComplete;
                return success;
            }
            return true;
        }

        /// <summary>
        /// Called if the Dead variable has changed so that the zone can update all interested parties
        /// </summary>
        /// <param name="theEntity"></param>
        internal void DeathStatusChanged(CombatEntity theEntity)
        {
            if (m_deathChangedThisFrame.Contains(theEntity) == false)
            {
                m_deathChangedThisFrame.Add(theEntity);
            }
        }
        /// <summary>
        /// Goes Through all Combat Entities damage lists and clears them
        /// </summary>
        void ClearCombatLists()
        {

            m_deathChangedThisFrame.Clear();
            
            for (int i = 0; i < m_damagedThisFrame.Count; i++)
            {
                CombatEntity currentEntity = m_damagedThisFrame[i];
                if (currentEntity != null)
                {
                    currentEntity.DamageListThisFrame.Clear();
                }

            }
            m_damagedThisFrame.Clear();
            for (int i = 0; i < m_combatListChangedThisFrame.Count; i++)
            {
                CombatEntity currentEntity = m_combatListChangedThisFrame[i];
                if (currentEntity != null)
                {
                    currentEntity.CombatListThisFrame.Clear();
                }

            }
            m_combatListChangedThisFrame.Clear();
            m_cancelledDamages.Clear();

        }

        #endregion


		#region character and mob methods

		Character GetCharacterForID(int characterID)
        {
            for (int i = 0; i < m_players.Count; i++)
            {
                Player currentPlayer = m_players[i];
                if (currentPlayer.m_activeCharacter != null)
                {
                    if (currentPlayer.m_activeCharacter.m_character_id == characterID)
                    {
                        return currentPlayer.m_activeCharacter;
                    }
                }
            }
            return null;
        }

        Character GetCharacterForIDWithinRangeOf(Vector3 position, float range, int characterID)
        {
            List<Player> playerList = new List<Player>();
            PartitionHolder.AddPlayersInRangeToList(null, position, range, playerList, ZonePartition.ENTITY_TYPE.ET_PLAYER,null);
            for (int i = 0; i < playerList.Count; i++)
            {
                Player currentPlayer = playerList[i];
                if (currentPlayer.m_activeCharacter != null)
                {
                    if (currentPlayer.m_activeCharacter.m_character_id == characterID)
                    {
                        return currentPlayer.m_activeCharacter;
                    }
                }
            }
            return null;
        }
        public void addPlayer(Player curPlayer, NetServer server, NetOutgoingMessage playermsg, bool playerZoning)
        {
            /*prepare joining players packet
             *player's full data
             *position
             *number of players in zone 
             *each players basic appearance and position*/

            SetUpCharactersUndiscoveredSpawnLocations(curPlayer.m_activeCharacter);
            /*prepare current players update
             *joining players basic appearance and position*/

            curPlayer.m_activeCharacter.writePositionInfo(playermsg);
            curPlayer.m_activeCharacter.m_QuestManager.writeAvailableQuestsToMessage(playermsg);
            curPlayer.m_activeCharacter.CharacterBountyManager.UpdateBounties();

            //if they already in the zone remove then (probably lagged out)
            removePlayer(curPlayer, server);


            int numberOfPlayers = m_players.Count;

            playermsg.WriteVariableUInt32((uint)numberOfPlayers);
            List<NetConnection> connections = new List<NetConnection>();
            for (int i = 0; i < numberOfPlayers; i++)
            {
                Player currentPlayer = m_players[i];
                if (currentPlayer.connection != null )
                {
                    connections.Add(currentPlayer.connection);
                }
                currentPlayer.m_activeCharacter.writeBasicCharacterInfoToMsg(playermsg);
                currentPlayer.m_activeCharacter.writePositionInfo(playermsg);
            }
            curPlayer.m_activeCharacter.WriteFriendsListToMessage(playermsg);

            NetOutgoingMessage zonemsg = server.CreateMessage();
            zonemsg.WriteVariableUInt32((uint)NetworkCommandType.CharacterZoningIn);
            curPlayer.m_activeCharacter.writeBasicCharacterInfoToMsg(zonemsg);
            curPlayer.m_activeCharacter.writePositionInfo(zonemsg);



            Program.processor.SendMessage(zonemsg, connections, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.CharacterZoningIn);
            if (curPlayer.m_activeCharacter.Dead == true)
            {
                SendPlayerDeadNotification(curPlayer.m_activeCharacter, connections);
            }
            CheckEnteringPlayersPVPStatus(curPlayer);
            m_players.Add(curPlayer);
            curPlayer.m_activeCharacter.TheCharactersPath.ClearList();
            curPlayer.m_activeCharacter.EntityPartitionCheck();

        }
        void SendPlayerDeadNotification(Character deadCharacter, List<NetConnection> connections)
        {
            List<CombatEntity> deadCharacterList = new List<CombatEntity>();
            deadCharacterList.Add(deadCharacter);
            NetOutgoingMessage dmgMsg = Program.Server.CreateMessage();
            dmgMsg.WriteVariableUInt32((uint)NetworkCommandType.CombatDamageMessage);

            dmgMsg.WriteVariableInt32(0);
            dmgMsg.WriteVariableInt32(0);

            //this is a blank list
            dmgMsg.WriteVariableInt32(0);
            //nothing is cancelled
            dmgMsg.WriteVariableInt32(0);
            //send all the dead characters data
            WriteAllChararactersCombatDataInRange(deadCharacterList, null, dmgMsg);

            Program.processor.SendMessage(dmgMsg, connections, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.CombatDamageMessage);


        }

        void SetUpCharactersUndiscoveredSpawnLocations(Character character)
        {
            List<int> knownTeleportLocations = character.DiscoveredSpawnPoints;
            List<PlayerSpawnPoint> undiscoveredLocations = character.UndiscoveredPoints;

            undiscoveredLocations.Clear();

            for (int currentSpawnPointIndex = 0; currentSpawnPointIndex < m_playerSpawnPoints.Count; currentSpawnPointIndex++)
            {
                PlayerSpawnPoint currentSpawnPoint = m_playerSpawnPoints[currentSpawnPointIndex];

                int currentSpawnPointID = currentSpawnPoint.SpawnPointID;

                if (knownTeleportLocations.Contains(currentSpawnPointID) == false)
                {
                    undiscoveredLocations.Add(currentSpawnPoint);
                }
            }

        }

        internal bool HasBeenToZone(Character character)
        {
            List<int> knownTeleportLocations = character.DiscoveredSpawnPoints;

            for (int currentSpawnPointIndex = 0; currentSpawnPointIndex < m_playerSpawnPoints.Count; currentSpawnPointIndex++)
            {
                PlayerSpawnPoint currentSpawnPoint = m_playerSpawnPoints[currentSpawnPointIndex];

                int currentSpawnPointID = currentSpawnPoint.SpawnPointID;

                if (knownTeleportLocations.Contains(currentSpawnPointID) == true)
                {
                    return true;
                }
            }
            return false;
        }

        public void WriteMobList(Player curPlayer)
        {
            //make a list of all the mobs and send it
            NetOutgoingMessage mobmsg = Program.Server.CreateMessage();
            mobmsg.WriteVariableUInt32((uint)NetworkCommandType.BatchZoneMonsterUpdate);
            //the total number of spawnPoints
            //figure out the number of active monsters
            int numberOfActiveMobs = 0;
            for (int currentMob = 0; currentMob < m_theMobs.Length; currentMob++)
            {
                if (m_theMobs[currentMob] != null)
                {
                    numberOfActiveMobs++;
                }
            }
            mobmsg.WriteVariableUInt32((uint)numberOfActiveMobs);
            //for each monster 
            for (int currentMob = 0; currentMob < m_theMobs.Length; currentMob++)
            {
                if (m_theMobs[currentMob] != null)
                {
                    //get the Server ID
                    mobmsg.WriteVariableUInt32((uint)m_theMobs[currentMob].ServerID);

                    //write the mob data
                    WriteMobToMsg(mobmsg, m_theMobs[currentMob], curPlayer);
                }
            }
            
            Program.processor.SendMessage(mobmsg, curPlayer.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.BatchZoneMonsterUpdate);

        }
        
        public void WriteMobToMsg(NetOutgoingMessage mobmsg, ServerControlledEntity theMob, Player curPlayer)
        {
            //get the template ID
            mobmsg.WriteVariableUInt32((uint)theMob.Template.m_templateID);
            //write the position
            mobmsg.Write((float)theMob.CurrentPosition.m_position.X);
            mobmsg.Write((float)theMob.CurrentPosition.m_position.Y);
            mobmsg.Write((float)theMob.CurrentPosition.m_position.Z);
            mobmsg.Write((float)theMob.CurrentPosition.m_direction.X);
            mobmsg.Write((float)theMob.CurrentPosition.m_direction.Z);

            mobmsg.WriteVariableInt32(theMob.OpinionBase);
            mobmsg.WriteVariableInt32(theMob.Template.m_maxHitpoints);
            mobmsg.WriteVariableInt32(theMob.Template.m_maxEnergy);
            mobmsg.WriteVariableInt32(theMob.Template.m_conversation_id);
            mobmsg.WriteVariableInt32(theMob.Template.m_level);

            //if we have an active player & character...which we should do
            if (curPlayer != null && curPlayer.m_activeCharacter != null)
            {
                //if this mob is known & friendly to the player we alter the aggro to be 0f
                if (curPlayer.m_activeCharacter.FactionManager.CheckReputationAgainstEntity(theMob.Template.FactionInfluences) == true)
                {
                    mobmsg.Write(0f);
                }
                else
                {
                    mobmsg.Write(theMob.AggroRange);
                }
            }
            else //default to just the regular aggro number
            {
                mobmsg.Write(theMob.AggroRange);
            }

            mobmsg.Write(theMob.Template.m_scale * theMob.CompiledStats.Scale);
        }

        /// <summary>
        /// Sends a mob update with all the mobs in range
        /// considered dumb as it ignores last send time and if they have moved 
        /// this is for teleporting players to get up to date info
        /// </summary>
        internal void sendDumbMobPatrolUpdate(Player thePlayer)
        {
            NetOutgoingMessage rawMessage = Program.Server.CreateMessage();

            int counter = 0;
            for (int currentMob = 0; currentMob < m_theMobs.Length; currentMob++)
            {
                if (m_theMobs[currentMob] != null)
                {
                    if (m_theMobs[currentMob].WriteDestinationToMessageNoChecks(rawMessage, thePlayer.m_activeCharacter))
                        counter++;
                }
            }
            if (counter > 0)
            {
                NetOutgoingMessage updateMobMsg = Program.Server.CreateMessage();
                updateMobMsg.WriteVariableUInt32((uint)NetworkCommandType.mobPatrolUpdate);
                updateMobMsg.WriteVariableInt32(counter);
                
                updateMobMsg.Write(rawMessage.PeekDataBuffer(),0,(int)rawMessage.LengthBytes);
                
                Program.processor.SendMessage(updateMobMsg, thePlayer.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.mobPatrolUpdate);
                //check if a mob should be removed
            }
        }
        internal void AddMobToSendList(ServerControlledEntity newMob)
        {
            m_mobsWantingToSendPositionUpdate.Add(newMob);

        }
        /// <summary>
        /// Sends the positions of mobs who have moved too far to all players in the zone
        /// </summary>
        internal void SendMobPositionsIgnoringDistance()
        {
            if (m_mobsWantingToSendPositionUpdate.Count > 0)
            {
                NetOutgoingMessage rawMessage = Program.Server.CreateMessage();
                int counter = 0;
                for (int i = 0; i < m_mobsWantingToSendPositionUpdate.Count; i++)
                {
                    ServerControlledEntity currentMob = m_mobsWantingToSendPositionUpdate[i];
                    if (currentMob != null)
                    {
                        if (currentMob.WriteDestinationToMessageNoChecks(rawMessage, null))
                            counter++;
                    }
                }
                if (counter > 0 && m_players.Count>0)
                {
                    NetOutgoingMessage updateMobMsg = Program.Server.CreateMessage();
                    updateMobMsg.WriteVariableUInt32((uint)NetworkCommandType.mobPatrolUpdate);
                    updateMobMsg.WriteVariableInt32(counter);
                    updateMobMsg.Write(rawMessage.PeekDataBuffer(), 0, (int)rawMessage.LengthBytes);

                    List<NetConnection> connections = new List<NetConnection>(m_players.Count);
                    for (int i = 0; i < m_players.Count; i++)
                    {
                        if (m_players[i].connection != null)
                        {
                            connections.Add(m_players[i].connection);
                        }
                    }
                    if (connections.Count > 0)
                    {

                        Program.processor.SendMessage(updateMobMsg, connections, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.mobPatrolUpdate);
                    }
                    //check if a mob should be removed
                }
            }

            m_mobsWantingToSendPositionUpdate.Clear();

        }
        public List<NetConnection> getUpdateList(Player exclude)
        {
            List<NetConnection> connections = new List<NetConnection>();
            for (int i = 0; i < m_players.Count; i++)
            {
                if (m_players[i] != exclude && m_players[i].connection!=null)
                {
                    connections.Add(m_players[i].connection);
                }
            }
            return connections;
        }
        #region Reading Data From Database
        void PopulateZonePointsFromDatabase(Database db)
        {
            SqlQuery query = new SqlQuery(db, "select * from zone_points where zone_id =" + m_zone_id);
            if (query.HasRows)
            {
                Vector3 position;

                int zonePointID = -1;
                int spawnPointID = -1;
                int spawnZoneID = -1;
                float radius = -1;
                int min_level = 1;
                while ((query.Read()))
                {
                    zonePointID = query.GetInt32("zone_point_id");
                    position.X = query.GetFloat("position_x");
                    position.Y = query.GetFloat("position_y");
                    position.Z = query.GetFloat("position_z");

                    spawnPointID = query.GetInt32("player_spawn_point_id");
                    spawnZoneID = query.GetInt32("spawn_point_zone_id");
                    radius = query.GetFloat("length_x") / 2;
                    min_level = query.GetInt32("min_level");
                    int quest_completed = query.GetInt32("quest_completed");
                    ZonePoint newZonePoint = new ZonePoint(position, radius, m_zone_id, zonePointID, spawnZoneID, spawnPointID,min_level,quest_completed);
                    m_zonePoints.Add(newZonePoint);
                }

            }

        }
        void PopulatePlayerSpawnPointsFromDatabase(Database db)
        {
            SqlQuery query = new SqlQuery(db, "select * from player_spawn_points where zone_id =" + m_zone_id);
            if (query.HasRows)
            {
                Vector3 position;

                int spawnPointID = -1;

                float radius = -1;
                bool teleportPoint = false;
                bool respawnPoint = false;
                float angle = 0;
                while ((query.Read()))
                {

                    position.X = query.GetFloat("position_x");
                    position.Y = query.GetFloat("position_y");
                    position.Z = query.GetFloat("position_z");
                    spawnPointID = query.GetInt32("player_spawn_point_id");
                    radius = query.GetFloat("radius");
                    teleportPoint = query.GetBoolean("teleport_point");
                    angle = query.GetFloat("angle");
                    respawnPoint = query.GetBoolean("respawn_point");
                    bool freeTravel = query.GetBoolean("free_travel");
                    PlayerSpawnPoint newSpawnPoint = new PlayerSpawnPoint(position, radius, m_zone_id, spawnPointID, teleportPoint, angle, respawnPoint,freeTravel);
                    m_playerSpawnPoints.Add(newSpawnPoint);
                }

            }
            query.Close();

        }
        void PopulateAreasFromDatabase(Database db)
        {
            SqlQuery query = new SqlQuery(db, "select * from zone_effect_areas where zone_id =" + m_zone_id);
            if (query.HasRows)
            {
                while ((query.Read()))
                {
                    int effectAreaID = query.GetInt32("zone_effect_area_id");
                    string collision_string = query.GetString("collision_string");
                    EffectArea newEffectArea = new EffectArea(this, effectAreaID,collision_string);
                    m_effectAreas.Add(newEffectArea);
                }
            }

        }
        void PopulateItemSpawnPointsFromDatabase(Database db)
        {
            SqlQuery query = new SqlQuery(db, "select * from item_spawns where zone_id =" + m_zone_id);
            if (query.HasRows)
            {



                while ((query.Read()))
                {

                    float x = query.GetFloat("position_x");
                    float y = query.GetFloat("position_y");
                    float z = query.GetFloat("position_z");
                    int itemspawnID = query.GetInt32("item_spawn_id");
                    string itemlist = query.GetString("item_list");
                    float minRespawnTime = query.GetFloat("respawn_time");
                    float maxRespawnTime = query.GetFloat("max_respawn_time");
                    ItemSpawnPoint newItemSpawnPoint = new ItemSpawnPoint(this, x, y, z, minRespawnTime, itemspawnID, itemlist,maxRespawnTime);
                    //if (itemspawnID == 304)
                    {
                        m_itemSpawnPoints.Add(newItemSpawnPoint);
                    }
                }

            }
            query.Close();

        }

        void PopulateMobsFromDatabase(Database db)
        {

            Vector3 direction;
           //   SqlQuery query = new SqlQuery(db, "select * from spawn_points where zone_id =" + m_zone_id + " and spawn_point_id=596");
            SqlQuery query = new SqlQuery(db, "select * from spawn_points where zone_id =" + m_zone_id);
            if (query.HasRows )
            {
                //int numberOfMobs = query.
                while ((query.Read()))
                {

                    //int patrolID = query.GetInt32("inventory_id");

                    float minRespawnTime = query.GetFloat("respawn_time");
                    float maxRespawnTime =  query.GetFloat("max_respawn_time");
                    float posX = query.GetFloat("position_x");
                    float posY = query.GetFloat("position_y");
                    float posZ = query.GetFloat("position_z");
                    double yangle = query.GetInt32("init_y_angle");
                    int spawnPointID = query.GetInt32("spawn_point_id");
                    float minDespawnTime = 0;
                    if (!query.isNull("min_despawn_time"))
                    {
                        minDespawnTime = query.GetFloat("min_despawn_time");
                    }
                    float maxDespawnTime = 0;
                    if (!query.isNull("max_despawn_time"))
                    {
                        maxDespawnTime = query.GetFloat("max_despawn_time");
                    }
                    bool despawn = query.GetBoolean("despawn");

                    direction.X = (float)Math.Sin(yangle * Math.PI / 180);
                    direction.Y = 0;
                    direction.Z = -(float)Math.Cos(yangle * Math.PI / 180);

                    String patrol = query.GetString("patrol");
                    //disable mob movement
                 //   patrol = "random 5 0 0 0";
                 //   patrol = "";
                    String[] patrolList = patrol.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    List<PatrolPoint> patrolPoints = new List<PatrolPoint>();

                    float patrolSpeed = query.GetFloat("patrol_speed");

                    SpawnPoint newPoint = new SpawnPoint(posX, posY, posZ, direction.X, direction.Z, minRespawnTime, maxRespawnTime,minDespawnTime,maxDespawnTime,spawnPointID, this,despawn);
                    newPoint.PatrolSpeed = patrolSpeed;
                    if (patrolList.Length > 0)
                    {
                        if (patrolList[0].Equals("random"))
                        {
                            /*
                             * random roamRadius probabilityWait(out of 100) minWaitTime maxWaitTime
                             * */
                            RandomPatrolSettings roamSettings = new RandomPatrolSettings();
                            roamSettings.Radius = float.Parse(patrolList[1]);
                            roamSettings.ProbabilityWait = int.Parse(patrolList[2]);
                            roamSettings.MinWaitTime = float.Parse(patrolList[3]);
                            roamSettings.MaxWaitTime = float.Parse(patrolList[4]);
                            newPoint.SetRoamSettings(roamSettings);
                            /*if (roamSettings.Radius < 2)
                            {
                                Program.Display("*************************spawn " + spawnPointID + " Roam Raduis = " + roamSettings.Radius);
                            }*/
                        }
                        else if (patrolList[0].Equals("patrol"))
                        {
                            /*
                             * patrol =    patrol numPoints:point;point;point
                             * patrol point =position=x,y,z
                             * wait point    =wait=time
                             * patrol 2:position=5,4.3,5;wait=3;position=2,4.3,2
                             */
                            PatrolPoint currentPatrolPoint = null;
                            // get the number of points in the patrol

                            Vector2 endDirection = new Vector2(1, 0);
                            string[] points = patrolList[1].Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                            Vector3 point = new Vector3();
                            for (int icurrentPoint = 0; icurrentPoint < points.Length; icurrentPoint++)
                            {
                                string[] currentPoint = points[icurrentPoint].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                                point.X = float.Parse(currentPoint[0]);
                                point.Y = float.Parse(currentPoint[1]);
                                point.Z = float.Parse(currentPoint[2]);

                                currentPatrolPoint = new PatrolPoint();
                                currentPatrolPoint.Position = point;
                                currentPatrolPoint.StopTime = float.Parse(currentPoint[3]);
                                patrolPoints.Add(currentPatrolPoint);



                            }
                            newPoint.SetPatrol(patrolPoints);
                        }

                    }

                    String mobString = query.GetString("monster_list");
                    String[] mobList = mobString.Split(new char[] { ' ', ';', ',' }, StringSplitOptions.RemoveEmptyEntries);

                    int numberOfMobs = mobList.Length / 2;

                    for (int currentMob = 0; currentMob < numberOfMobs; currentMob++)
                    {
                        int monsterID = Convert.ToInt32(mobList[currentMob * 2]);
                        //monsterID = 73817;// 70063;//268;//70247;//70224;//71742;
                        int probability = Convert.ToInt32(mobList[currentMob * 2 + 1]);
                        newPoint.AddMobToList(monsterID, probability);

                    }
                    //   spcheat 4333, 4334 and 4335.
                    //if (spawnPointID < 20000)// 8879)
                    {
                        m_spawnPoints.Add(newPoint);// disable spawns
                    }
                   

                }

            }

            query.Close();

            
            m_theMobs = new ServerControlledEntity[m_spawnPoints.Count];


        }

        #endregion
        /// <summary>
        /// if something has gone wrong and the player needs to be reset
        /// This is the point to reset them to
        /// </summary>
        /// <returns></returns>
        internal PlayerSpawnPoint GetDefaultSpawnPoint()
        {
            if (m_playerSpawnPoints.Count > 0)
            {
                return m_playerSpawnPoints[0];
            }
            return null;
        }
        internal PlayerSpawnPoint GetClosestRespawnPoint(Character characterToRespawn)
        {
            if (characterToRespawn == null)
            {
                return null;
            }

            PlayerSpawnPoint closestSpawnPoint = null;
            double distToClosestSpawnPoint = 0;
            double distToClosestBackUp = 0;
            PlayerSpawnPoint backupPoint = null;

            for (int currentSpawnPointIndex = 0; currentSpawnPointIndex < m_playerSpawnPoints.Count; currentSpawnPointIndex++)
            {
                PlayerSpawnPoint currentSpawnPoint = m_playerSpawnPoints[currentSpawnPointIndex];

                int currentSpawnPointID = currentSpawnPoint.SpawnPointID;
                bool pointDiscovered = characterToRespawn.DiscoveredSpawnPoints.Contains(currentSpawnPointID);
                //check that it is a valid respawn point
                Vector3 positionToCheck = characterToRespawn.m_ConfirmedPosition.m_position;
                if ((currentSpawnPoint != null) && (pointDiscovered == true))
                {
                    if ((currentSpawnPoint.RespawnPoint == true))
                    {
                        if (closestSpawnPoint == null)
                        {
                            closestSpawnPoint = currentSpawnPoint;
                            distToClosestSpawnPoint = (closestSpawnPoint.Position - positionToCheck).Length();
                            continue;
                        }

                        double distanceToCurrentSpawnPoint = (currentSpawnPoint.Position - positionToCheck).Length();
                        if (distanceToCurrentSpawnPoint < distToClosestSpawnPoint)
                        {
                            closestSpawnPoint = currentSpawnPoint;
                            distToClosestSpawnPoint = (closestSpawnPoint.Position - positionToCheck).Length();
                            continue;
                        }
                    }
                    else
                    {
                        if (closestSpawnPoint == null)
                        {
                            if (backupPoint == null)
                            {
                                backupPoint = currentSpawnPoint;
                                distToClosestBackUp = (backupPoint.Position - positionToCheck).Length();
                                continue;

                            }
                            double distanceToCurrentSpawnPoint = (currentSpawnPoint.Position - positionToCheck).Length();
                            if (distanceToCurrentSpawnPoint < distToClosestBackUp)
                            {
                                backupPoint = currentSpawnPoint;
                                distToClosestBackUp = (backupPoint.Position - positionToCheck).Length();
                                continue;
                            }
                        }
                    }
                }
            }
            if (closestSpawnPoint == null)
            {
                closestSpawnPoint = backupPoint;
            }

            return closestSpawnPoint;
        }



        PlayerSpawnPoint FindSpawnPointInRangeOfPlayer(Character character)
        {
            PlayerSpawnPoint spawnPointInRange = null;

            List<PlayerSpawnPoint> charactersUndiscoveredPoints = character.UndiscoveredPoints;
            for (int currentPoint = 0; (currentPoint < charactersUndiscoveredPoints.Count) && (spawnPointInRange == null); currentPoint++)
            {
                PlayerSpawnPoint currentSpawnPoint = charactersUndiscoveredPoints[currentPoint];
                double distanceFromPoint = (currentSpawnPoint.Position - character.CurrentPosition.m_position).Length();
                if (distanceFromPoint <= ZONE_POINT_DISCOVERY_DISTANCE)
                {
                    spawnPointInRange = currentSpawnPoint;
                    charactersUndiscoveredPoints.Remove(spawnPointInRange);

                }
            }


            return spawnPointInRange;
        }

        public ServerControlledEntity getMobFromID(int serverid)
        {
            for (int i = 0; i < m_theMobs.Length; i++)
            {
                if (m_theMobs[i] != null && m_theMobs[i].ServerID == serverid)
                {
                    return m_theMobs[i];
                }
            }
            return null;
        }
        public ServerControlledEntity getMobFromIDWithinRangeOf(Vector3 position,float range,int serverid)
        {
            List<ServerControlledEntity> mobList = new List<ServerControlledEntity>();
            PartitionHolder.AddMobsInRangeToList(null, position, range, mobList, ZonePartition.ENTITY_TYPE.ET_MOB,null);
            for (int i = 0; i < mobList.Count; i++)
            {
                if (mobList[i] != null && mobList[i].ServerID == serverid)
                {
                    return mobList[i];
                }
            }
            return null;
        }

        public PlayerSpawnPoint GetPlayerSpawnPointForID(int pointID)
        {
            PlayerSpawnPoint thePoint = null;

            for (int currentPoint = 0; (currentPoint < m_playerSpawnPoints.Count) && (thePoint == null); currentPoint++)
            {
                if (pointID == m_playerSpawnPoints[currentPoint].SpawnPointID)
                {
                    thePoint = m_playerSpawnPoints[currentPoint];
                }
            }


            return thePoint;
        }
        internal Shop getShopFromNPCId(int npc_id,int shop_id)
        {
            for (int i = 0; i < m_shops.Count(); i++)
            {
                if (m_shops[i].m_npc_id == npc_id && m_shops[i].m_shop_id==shop_id)
                {
                    return m_shops[i];
                }
            }
            Program.Display("shop not found for requested npc zone=" + m_zone_id + ", npc_id=" + npc_id+", shop_id="+shop_id);
            return null;
        }

        internal void WriteSpawnedItems(Player player)
        {
            NetOutgoingMessage msg = Program.Server.CreateMessage();
            msg.WriteVariableUInt32((uint)NetworkCommandType.ItemSpawns);
            int count = 0;
            for (int i = 0; i < m_itemSpawnPoints.Count; i++)
            {
                if (m_itemSpawnPoints[i].m_item != null)
                    count++;
            }
            msg.WriteVariableInt32(count);
            for (int i = 0; i < m_itemSpawnPoints.Count; i++)
            {
                m_itemSpawnPoints[i].WriteToMessage(msg);
            }
            Program.processor.SendMessage(msg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.ItemSpawns);

        }

        internal void WritePlayerDiscoveredSpawnPoint(Player player, int spawnPointID)
        {
            NetOutgoingMessage msg = Program.Server.CreateMessage();
            msg.WriteVariableUInt32((uint)NetworkCommandType.PlayerSpawnPointDiscovered);

            msg.WriteVariableInt32(spawnPointID);

            Program.processor.SendMessage(msg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.PlayerSpawnPointDiscovered);
        }

        internal void pickupItem(int spawnid, Character character, NetOutgoingMessage msg)
        {
            for (int i = 0; i < m_itemSpawnPoints.Count; i++)
            {
                if (m_itemSpawnPoints[i].m_itemSpawnID == spawnid && m_itemSpawnPoints[i].m_item != null && (m_itemSpawnPoints[i].m_spawnPosition - character.m_CharacterPosition.m_position).LengthSquared2D() < 100.0f)
                {
                    ItemSpawnPoint spawnPoint = m_itemSpawnPoints[i];
                    
                    Item itemAdded = character.m_inventory.AddNewItemToCharacterInventory(spawnPoint.m_item.m_item_id, 1, !spawnPoint.m_item.m_stackable);

                    if (itemAdded != null && Program.processor.CompetitionManager != null)
                    {
                        Program.processor.CompetitionManager.UpdateCompetition(character, Competitions.CompetitionType.PICKUP_ITEM, itemAdded.m_template_id);
                    }

                    Program.processor.updateShopHistory(-1, -1, itemAdded.m_inventory_id, itemAdded.m_template_id, 1, 0, (int)character.m_character_id, "Picked up");
                    if (Program.m_LogAnalytics)
                    {
                        AnalyticsMain logAnalytics = new AnalyticsMain(true);
                        logAnalytics.itemActioned(character.m_player, itemAdded.m_template_id.ToString(), itemAdded.m_template.m_item_name, itemAdded.m_template.m_subtype.ToString(), "PICKED_UP");
                    }

                    character.m_QuestManager.checkIfItemAffectsStage(spawnPoint.m_item.m_item_id);
                    msg.Write((byte)1);
                    msg.WriteVariableInt32(spawnPoint.m_item.m_item_id);
                    msg.WriteVariableInt32(1);
                    character.m_inventory.WriteInventoryWithMoneyToMessage(msg);
                    NetOutgoingMessage despawnmessage = Program.Server.CreateMessage();
                    despawnmessage.WriteVariableUInt32((uint)NetworkCommandType.ItemDespawn);
                    despawnmessage.WriteVariableInt32(spawnid);
                    List<NetConnection> connections = getUpdateList(null);
                    Program.processor.SendMessage(despawnmessage, connections, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.ItemDespawn);
                    character.increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.COLLECTOR, 1);
                    if (spawnPoint.m_item.m_item_id == 16994
                        || spawnPoint.m_item.m_item_id == 17254
                        || spawnPoint.m_item.m_item_id == 17255
                        || spawnPoint.m_item.m_item_id == 17256
                        || spawnPoint.m_item.m_item_id == 17257
                        || spawnPoint.m_item.m_item_id == 17258)
                    {
                        character.increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.MUSHROOM_PICKER, 1);
                    }
                    if (character.CharacterParty != null)
                    {
						string locText = Localiser.GetString(textDB, character.m_player, (int)ZoneOfferTextDB.TextID.CHARACTER_PICK_UP_ITEM);
						locText = String.Format(locText, character.m_name, spawnPoint.m_item.m_loc_item_name[character.m_player.m_languageIndex]);
						character.CharacterParty.SendPartySystemMessage(locText, character, true, SYSTEM_MESSAGE_TYPE.PARTY, false);
					}
                    
                    spawnPoint.despawn();
                    return;
                }
            }
            msg.Write((byte)0);
			string locTextPickUpItem = Localiser.GetString(textDB, character.m_player, (int)ZoneOfferTextDB.TextID.CAN_NOT_PICK_UP_ITEM);
			msg.Write(locTextPickUpItem);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oldPos"></param>
        /// <param name="newPos"></param>
        /// <param name="radius"></param>
        /// <param name="height"></param>
        /// <param name="ipOnly">we don't know</param>
        /// <returns></returns>
        internal Vector3 CheckCollisions(Vector3 oldPos, Vector3 newPos, float radius, float height, bool ipOnly)
        {
            if (!Program.m_CollisionsEnabled)
            {
                return newPos;
            }

            return m_collisions.checkCollisions(oldPos, newPos, radius, height, ipOnly);

        }

        internal Vector3 CheckIntersections(Vector3 oldPos, Vector3 newPos, bool ignoreY, float radius)
        {
            if (!Program.m_CollisionsEnabled)
            {
                return newPos;
            }
            return m_collisions.checkIntersections(oldPos, newPos, ignoreY, radius);
        }
        
        private void TestPlayerOutOfZone(Character currentCharacter)
        {
            //test to find characters out of zone
            if (currentCharacter.m_ConfirmedPosition.m_position.X < m_zoneRect.Left - 10.0f
                || currentCharacter.m_ConfirmedPosition.m_position.X > m_zoneRect.Right + 10.0f
                || currentCharacter.m_ConfirmedPosition.m_position.Y < -250.0f
                || currentCharacter.m_ConfirmedPosition.m_position.Y > 250.0f
                || currentCharacter.m_ConfirmedPosition.m_position.Z < m_zoneRect.Top - 10.0f
                || currentCharacter.m_ConfirmedPosition.m_position.Z > m_zoneRect.Bottom + 10.0f
                )
            {
                Program.Display("Character " + currentCharacter.Name + " found outside zone " + m_zone_name + " at "
                                + currentCharacter.m_ConfirmedPosition.m_position.X.ToString("F1") + ","
                                + currentCharacter.m_ConfirmedPosition.m_position.Y.ToString("F1") + ","
                                + currentCharacter.m_ConfirmedPosition.m_position.Z.ToString("F1"));

                ResetPlayerDueToPositionError(currentCharacter);
            }
        }

        internal void ResetPlayerDueToPositionError(Character character)
        {
            PlayerSpawnPoint respawnPoint = GetClosestRespawnPoint(character);
            if (respawnPoint == null)
            {
                respawnPoint = m_playerSpawnPoints[0];
            }
            Vector3 position = respawnPoint.RandomRespawnPosition;

            character.Respawn(position, Character.Respawn_Type.normal, -1);

            List<NetConnection> connections = new List<NetConnection>();
            AddConnectionsOfPlayersToList(connections, m_players);
            NetOutgoingMessage teleportMessage = CreateCharacterCorrectionMessage(Program.Server, character, position, respawnPoint.ZoneID, respawnPoint.Angle);
            Program.processor.SendMessage(teleportMessage, connections, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.CorrectPlayerPosition);
        }

		#endregion

		#region pvp

		void CheckEnteringPlayersPVPStatus(Player player)
        {
            player.m_activeCharacter.PopulateHateList();
            if (m_zone_id != 1||ENABLE_PVP ==false)
            {
                return;
            }

            if (player == null||player.m_activeCharacter==null)
            {
                return;
            }
            
            uint characterID = player.m_activeCharacter.m_character_id;
            //if this is a PVP area then add the ID to everyones List
            for (int i = 0; i < m_players.Count; i++)
            {
                Player currentPlayer = m_players[i];
                if (currentPlayer != player && currentPlayer != null && currentPlayer.m_activeCharacter != null)
                {
                    currentPlayer.m_activeCharacter.AddToHateList(player.m_activeCharacter);
                }
            }
        }

        /// <summary>
        /// called when a player is removed from the zone
        /// this will need to be changed to deal with keeping people hated who are there for other reasons
        /// </summary>
        /// <param name="player"></param>
        void CheckLeavingPlayersPVPStatus(Player player)
        {
            if (m_zone_id != 1)
            {
                return;
            }
            if (player == null || player.m_activeCharacter == null)
            {
                return;
            }
            uint characterID = player.m_activeCharacter.m_character_id;
            //if this is a PVP area then remove the ID from everyones List
            for (int i = 0; i < m_players.Count; i++)
            {
                Player currentPlayer = m_players[i];
                if (currentPlayer != player && currentPlayer != null && currentPlayer.m_activeCharacter != null)
                {
                    currentPlayer.m_activeCharacter.RemoveFromHateList(player.m_activeCharacter);
                }
            }
        }

        internal void ProcessPVPMessage(NetIncomingMessage msg, Player player)
        {
            //read the message type
            PVP_MESSAGE_TYPE messageType = (PVP_MESSAGE_TYPE)msg.ReadVariableInt32();

            //now pass the rest of the message to get read properly
            switch(messageType)
            {
                case PVP_MESSAGE_TYPE.CLIENT_DUEL_REQUEST:
                    {
                        ProcessClientDuelRequest(msg, player);
                        break;
                    }
                case PVP_MESSAGE_TYPE.CLIENT_DUEL_REPLY:
                    {
                        ProcessClientDuelReply(msg,player);
                        break;
                    }
                    

            }

        }

        void ProcessClientDuelRequest(NetIncomingMessage msg, Player player)
        {
            //character to duel
            uint otherplayerId = msg.ReadVariableUInt32();
            //find the character
            Player otherPlayer = null;
            Character otherCharacter = GetCharacterForID((int)otherplayerId);
            Character playersCharacter = player.m_activeCharacter;
            if (otherCharacter != null)
            {
                otherPlayer = otherCharacter.m_player;
            }
            bool duelPassesConditions = true;
            //are they busy
            bool otherPlayerBusy = false;
            bool currentPlayerBusy = false;
            bool otherPlayerInNoDuel = false;
            bool currentPlayerInNoDuel = false;
            bool playerBlockedOther = false;
            //is this deal request the same as one already active, if so don't close the players popup
            bool duplicateRequest = false;

            if(otherCharacter==null || playersCharacter==null)
            {
                duelPassesConditions = false;
            }
            else if (otherCharacter.Dead == true)
            {
                duelPassesConditions = false;
                otherPlayerBusy = true;
            }
            else if (otherCharacter.CanTakeRequest() == false || otherCharacter.HasBlockedCharacter((int)playersCharacter.m_character_id))
            {
                otherPlayerBusy = true;

                duelPassesConditions = false;
                if (otherCharacter.CurrentRequest != null && otherCharacter.CurrentRequest.IsRequestFor(playersCharacter.m_character_id, PendingRequest.REQUEST_TYPE.RT_DUEL))
                {
                    duplicateRequest = true;
                }
            }
            else if (playersCharacter.CanTakeRequest() == false)
            {
                currentPlayerBusy = true;
                duelPassesConditions = false;
            }
            else if (playersCharacter.HasBlockedCharacter((int)otherCharacter.m_character_id))
            {
                duelPassesConditions = false;
                playerBlockedOther = true;
            }
            else if (playersCharacter.IsInPVPType(Character.PVPType.CanDuel) == false)
            {
                duelPassesConditions = false;
                currentPlayerInNoDuel = true;
            }
            else if (otherCharacter.IsInPVPType(Character.PVPType.CanDuel) == false)
            {
                duelPassesConditions = false;
                otherPlayerInNoDuel = true;
            }
            // do they match the duel criteria
            if (otherPlayer == null || otherPlayer.connection == null)
            {
                duelPassesConditions = false;
            }

            //if so
            if (duelPassesConditions)
            {
                //remember the request
                playersCharacter.CurrentRequest = new PendingRequest(otherPlayer, PendingRequest.REQUEST_TYPE.RT_DUEL, PendingRequest.REQUEST_STATUS.RS_AWAITING_REPLY, 30);
                otherCharacter.CurrentRequest = new PendingRequest(player, PendingRequest.REQUEST_TYPE.RT_DUEL, PendingRequest.REQUEST_STATUS.RS_AWAITING_REPLY, 30);
                //send a request to the other player
                SendServerDuelRequest(otherPlayer, playersCharacter.m_character_id);
				//send that they have challenged the other to the duel

				string locText = Localiser.GetString(textDB, player, (int)ZoneOfferTextDB.TextID.CHALLENGED_OTHER_DUEL);
				locText = String.Format(locText, otherCharacter.Name);
				Program.processor.sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
			}
            else
            {
				//tell them the request has failed
				string locText = Localiser.GetString(textDB, player, (int)ZoneOfferTextDB.TextID.DUEL_COULD_NOT_START);
				string cancelString = locText;
				if (otherPlayerBusy == true)
                {
					locText = Localiser.GetString(textDB, player, (int)ZoneOfferTextDB.TextID.DUEL_OTHER_PLAYER_BUSY);
					cancelString = locText;
				}
                else if(currentPlayerBusy==true)
                {
					locText = Localiser.GetString(textDB, player, (int)ZoneOfferTextDB.TextID.DUEL_PLAYER_BUSY);
					cancelString = locText;
				}
                else if (currentPlayerInNoDuel == true)
                {
					locText = Localiser.GetString(textDB, player, (int)ZoneOfferTextDB.TextID.PLAYER_CAN_NOT_DUEL_AREA);
					cancelString = locText;
				}
                else if (otherPlayerInNoDuel == true)
                {
					locText = Localiser.GetString(textDB, player, (int)ZoneOfferTextDB.TextID.THEY_CAN_NOT_DUEL_AREA);
					cancelString = locText;
					//this shouldn't happen, suggests the no duel area is too small
				}
                else if (playerBlockedOther == true)
                {
					locText = Localiser.GetString(textDB, player, (int)ZoneOfferTextDB.TextID.BLOCK_CHARACTER_DUEL);
					cancelString = locText;
				}
                if (duplicateRequest == false)
                {
                    SendServerDuelReply(player, otherplayerId, false, cancelString);

                }
                else
                {
					locText = Localiser.GetString(textDB, player, (int)ZoneOfferTextDB.TextID.ALREADY_SENT_DUEL_REQUEST);
					Program.processor.sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
                }
            }
        }
        void ProcessClientDuelReply(NetIncomingMessage msg, Player player)
        {
            //accept or decline
            bool accepted = (msg.ReadByte() == 1);
            //character to duel
            uint characterID = msg.ReadVariableUInt32();
            //find the character
            Player otherPlayer = null;//getPlayerFromActiveCharacterId(otherplayerId);
            Character otherCharacter = GetCharacterForID((int)characterID);
            Character playersCharacter = player.m_activeCharacter;
            if (otherCharacter != null)
            {
                otherPlayer = otherCharacter.m_player;
            }
           

            //did they accept

            //if they declined
            if (accepted == false)
            {
                //send a declined reply
                if (playersCharacter != null && otherCharacter != null && otherPlayer != null)
                {
					string locText = Localiser.GetString(textDB, otherPlayer, (int)ZoneOfferTextDB.TextID.PLAYER_DECLINED_DEUL);
					locText = String.Format(locText, playersCharacter.Name);
					SendServerDuelReply(otherPlayer, playersCharacter.m_character_id, false, locText);
					if (playersCharacter.CurrentRequest != null && playersCharacter.CurrentRequest.IsRequestFor(otherCharacter.m_character_id, PendingRequest.REQUEST_TYPE.RT_DUEL) == true)
                    {
                        playersCharacter.CurrentRequest.CloseDown(player);
                    }
                }
                //clear any duel request data

            }
            //otherwise
            else
            {

                //can the other still be found (can use the pending request)
                bool duelPassesConditions = true;
                //was a reply expected
                if (otherCharacter == null || playersCharacter == null ||
                    (otherCharacter.CurrentRequest == null || otherCharacter.CurrentRequest.IsRequestFor(playersCharacter.m_character_id, PendingRequest.REQUEST_TYPE.RT_DUEL) == false) ||
                    (playersCharacter.CurrentRequest == null || playersCharacter.CurrentRequest.IsRequestFor(characterID, PendingRequest.REQUEST_TYPE.RT_DUEL) == false))
                {
                    duelPassesConditions = false;
                }
                else if (otherCharacter.Dead)
                {
                    duelPassesConditions = false;            
                }
                //do they still fit the criteria

                //if all the tests are passed
                if (duelPassesConditions == true)
                {
                    //set up the duel and timer
                    double timeTillStart = DuelTarget.COUNT_IN_TIME;
                    double maxTimeTillEnd = timeTillStart + DuelTarget.MAX_TIME_FOR_DUEL; 
                    double netTimeToStart = NetTime.Now+timeTillStart;
                    double netTimeToEnd = NetTime.Now + maxTimeTillEnd;
                    double currentTime = Program.MainUpdateLoopStartTime();
                    double serverTimeToStart =  currentTime+ timeTillStart;
                    double serverTimeTillEnd = currentTime + maxTimeTillEnd;
                    //send a duel start notification with a server time to begin
                    SendBeginDuel(player, otherPlayer, netTimeToStart, netTimeToEnd);
                    otherCharacter.CurrentDuelTarget = new DuelTarget(playersCharacter, serverTimeToStart, serverTimeTillEnd, otherCharacter);
                    playersCharacter.CurrentDuelTarget = new DuelTarget(otherCharacter, serverTimeToStart, serverTimeTillEnd, playersCharacter);
                    playersCharacter.CurrentRequest = null;
                    otherCharacter.CurrentRequest = null;
                }
                else
                {
					//send a failed to start duel reply
					//tell them the request has failed
					string locText = Localiser.GetString(textDB, player, (int)ZoneOfferTextDB.TextID.DUEL_COULD_NOT_START);
					string cancelString = locText;
					SendServerDuelReply(player, characterID, false, cancelString);
                    if (otherPlayer != null && playersCharacter != null)
                    {
						cancelString = Localiser.GetString(textDB, otherPlayer, (int)ZoneOfferTextDB.TextID.DUEL_COULD_NOT_START);
						SendServerDuelReply(otherPlayer, playersCharacter.m_character_id, false, cancelString);
                    }
                    //clear any duel request data 
                    if(otherCharacter!=null&&otherCharacter.CurrentRequest != null && otherCharacter.CurrentRequest.IsRequestFor(playersCharacter.m_character_id, PendingRequest.REQUEST_TYPE.RT_DUEL) == true)
                    {
                        otherCharacter.CurrentRequest = null;
                    }
                    if (playersCharacter != null && playersCharacter.CurrentRequest != null && playersCharacter.CurrentRequest.IsRequestFor(playersCharacter.m_character_id, PendingRequest.REQUEST_TYPE.RT_DUEL) == true)
                    {
                        playersCharacter.CurrentRequest = null;
                    }
                }
            }

        }
        void SendBeginDuel(Player player1, Player player2, double serverStartTime, double serverTimeAtEnd)
        {
            //they both need a message

            //player 1 message
            NetOutgoingMessage outmsg1 = Program.Server.CreateMessage();
            outmsg1.WriteVariableUInt32((uint)NetworkCommandType.PVPMessage);
            outmsg1.WriteVariableInt32((int)PVP_MESSAGE_TYPE.DUEL_BEGIN);
            //write the time it will start to the message
            outmsg1.Write(serverStartTime);
            //write the time it will end to the message
            outmsg1.Write(serverTimeAtEnd);
            //tell them who the duel is against
            outmsg1.WriteVariableUInt32(player2.m_activeCharacter.m_character_id);

            Program.processor.SendMessage(outmsg1, player1.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.PVPMessage);//was OtherCancelTrade

            //player 2 message
            NetOutgoingMessage outmsg2 = Program.Server.CreateMessage();
            outmsg2.WriteVariableUInt32((uint)NetworkCommandType.PVPMessage);
            outmsg2.WriteVariableInt32((int)PVP_MESSAGE_TYPE.DUEL_BEGIN);
            //write the time it will start to the message
            outmsg2.Write(serverStartTime);
            //write the time it will end to the message
            outmsg2.Write(serverTimeAtEnd);
            //tell them who the duel is against
            outmsg2.WriteVariableUInt32(player1.m_activeCharacter.m_character_id);

            Program.processor.SendMessage(outmsg2, player2.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.PVPMessage);//was OtherCancelTrade


        }
        void SendServerDuelRequest(Player player, uint challengerID)
        {
            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.PVPMessage);
            outmsg.WriteVariableInt32((int)PVP_MESSAGE_TYPE.SERVER_DUEL_REQUEST);
            //requesting character id
            outmsg.WriteVariableUInt32(challengerID);
            Program.processor.SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.PVPMessage);//was OtherCancelTrade

        }
        internal void SendServerDuelReply(Player player, uint challengerID, bool accepted,string infoText)
        {

            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.PVPMessage);
            outmsg.WriteVariableInt32((int)PVP_MESSAGE_TYPE.SERVER_DUEL_REPLY);
            //will the duel go ahead
            if (accepted)
            {
                outmsg.Write((byte)1);
            }
            else
            {
                outmsg.Write((byte)0);
            }
            
            //character id
            outmsg.WriteVariableUInt32(challengerID);
            //add some info text
            outmsg.Write(infoText);
            Program.processor.SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.PVPMessage);
        }
        internal void SendDuelEnd(Player player,uint opponentCharacterID, DuelTarget.DUEL_END_CONDITIONS endCondition, string infoString,Character opponentCharacter)
        {
            //create message
            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.PVPMessage);
            outmsg.WriteVariableInt32((int)PVP_MESSAGE_TYPE.DUEL_END);

            //who won the duel (if anyone) was the duel against
            outmsg.WriteVariableUInt32(opponentCharacterID);
            //how did it end
            outmsg.WriteVariableInt32((int)endCondition);
            /*
            int pvpExperience = 0;
            int pvpRating = 0;
            int pvpLevel = 0;
            int gainedXP = 0;
            if (player.m_activeCharacter!=null && opponentCharacter!=null)
            {
                int oldpvpRating=player.m_activeCharacter.getVisiblePVPRating();
                int oldpvpLevel=player.m_activeCharacter.m_pvpLevel;
                int oldOpponentpvpRating = opponentCharacter.getVisiblePVPRating();
                int oldOpponentpvpLevel = opponentCharacter.m_pvpLevel;
                if(endCondition == DuelTarget.DUEL_END_CONDITIONS.DEC_VICTORY)
                {
                    player.m_activeCharacter.increaseRanking(RankingsManager.RANKING_TYPE.PVP_KILLS, 1);
                    if(opponentCharacter!=null)
                    {
                        gainedXP=player.m_activeCharacter.updatePVPXP(opponentCharacter);
                    }
                }
                else if(endCondition==DuelTarget.DUEL_END_CONDITIONS.DEC_DEFEAT)
                {
                    player.m_activeCharacter.increaseRanking(RankingsManager.RANKING_TYPE.PVP_DEATHS, 1);
                }
                pvpExperience = player.m_activeCharacter.getVisiblePVPExperience();
                pvpRating = player.m_activeCharacter.getVisiblePVPRating();
                pvpLevel = player.m_activeCharacter.m_pvpLevel;
               // pvpLevel = 20;
                if (pvpRating != oldpvpRating)
                {
                    player.m_activeCharacter.InfoUpdated(Inventory.EQUIP_SLOT.SLOT_PVP_RATING);
                }
                if( pvpLevel != oldpvpLevel)
                {
                    player.m_activeCharacter.InfoUpdated(Inventory.EQUIP_SLOT.SLOT_PVP_LEVEL);
                }
                if (opponentCharacter.getVisiblePVPRating() != oldOpponentpvpRating )
                {
                    opponentCharacter.InfoUpdated(Inventory.EQUIP_SLOT.SLOT_PVP_RATING);
                }
                if(opponentCharacter.m_pvpLevel != oldOpponentpvpLevel)
                {
                    opponentCharacter.InfoUpdated(Inventory.EQUIP_SLOT.SLOT_PVP_LEVEL);
                }

            }
            outmsg.WriteVariableInt32(gainedXP);
            outmsg.WriteVariableInt32(pvpExperience);
            outmsg.WriteVariableInt32(pvpLevel);
            outmsg.WriteVariableInt32(pvpRating);
             */ 
            //info string
            outmsg.Write(infoString);
           
            //--reward currently not used--//
            //item/gold information
            //if inventory changes the inventory will need to be sent
            //--reward currently not used--//
            Program.processor.SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.PVPMessage);//was OtherCancelTrade


        }


        #endregion
    }
}
