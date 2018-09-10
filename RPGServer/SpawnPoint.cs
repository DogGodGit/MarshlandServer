using System.Collections.Generic;
using MainServer.Combat;
using XnaGeometry;

namespace MainServer
{
    class SpawnPointMob
    {
        public SpawnPointMob(int mobID, int probability)
        {
            MobID = mobID;
            Probability = probability;
        }

        public int MobID { get; set; }
        public int Probability { get; set; }
    }

    class SpawnPoint
    {
        #region variables
        //public enum PatrolType { Stand, Random, Patrol };
        Vector3 m_spawnPosition;
        /// <summary>
        /// The initial direction the mob will face when created
        /// </summary>
        Vector3 m_spawnDirection;
        double m_minRespawnTime;
        double m_maxRespawnTime;
       

        //Timers for despawn time.
        double m_minDespawnTime;
        double m_maxDespawnTime;

        public bool m_despawn;

        double m_patrolSpeed = 1;
        /// <summary>
        /// A link to the zone this spawn point is connected to
        /// </summary>
        Zone m_zone = null;
        /// <summary>
        /// Time until the replacement mob is created
        /// should only count down once a monster is killed
        /// </summary>
        internal double m_timeTillNextRespawn;

        internal double m_timeTillDespawn;
        /// <summary>
        /// A list of what mobs can appear at this spawn point
        /// and their probability of appearing
        /// </summary>
        List<SpawnPointMob> m_mobList;
        /// <summary>
        /// The movement behaviour of mobs at this spawn point
        /// </summary>
        ServerControlledEntity.NPC_MOVEMENT_AI m_movementAI;
 
        /// <summary>
        /// The patrol mobs appearing at this spawn point will use
        /// </summary>
        List<PatrolPoint> m_patrolPoints;
        /// <summary>
        /// Settings which dictate how a random Mob should Roam
        /// </summary>
        RandomPatrolSettings m_roamSettings;
        int m_probabilitySum;
        int m_serverID = -1;
        #endregion //variables

        public float PatrolSpeed
        {
            set { m_patrolSpeed = value; }
        }

        public SpawnPoint(double x, double y, double z, double dir_x, double dir_z, double minRespawnTime, double maxRespawnTime,double minDespawnTime,double maxDespawnTime, int serverID, Zone zone,bool despawn)
        {
            m_mobList = new List<SpawnPointMob>();
            m_timeTillNextRespawn = 0;
            m_probabilitySum = 0;
            m_minRespawnTime = minRespawnTime;
            m_maxRespawnTime = maxRespawnTime;
            m_minDespawnTime = minDespawnTime;
            m_maxDespawnTime = maxDespawnTime;
            m_despawn = despawn;
            m_spawnPosition.X = x;
            m_spawnPosition.Y = y;
            m_spawnPosition.Z = z;
            m_spawnDirection.X = dir_x;
            m_spawnDirection.Y = 0;
            m_spawnDirection.Z = dir_z;
           
            m_movementAI = ServerControlledEntity.NPC_MOVEMENT_AI.NPC_MOVEMENT_AI_STAND;
            m_serverID = serverID;
            m_zone = zone;
        }
        
        ~SpawnPoint() { }

        #region patrol methods
        public void SetPatrol(List<PatrolPoint> patrolPoints) 
        {
            m_movementAI = ServerControlledEntity.NPC_MOVEMENT_AI.NPC_MOVEMENT_AI_PATROL;
            m_patrolPoints = patrolPoints;
        }
        public void SetRoamSettings(RandomPatrolSettings settings)
        {
            m_movementAI = ServerControlledEntity.NPC_MOVEMENT_AI.NPC_MOVEMENT_AI_ROAM;
            m_roamSettings = settings;
        }
        #endregion
        public ServerControlledEntity Update(double timeSinceLastUpdate)
        {
            m_timeTillNextRespawn -= (float)timeSinceLastUpdate;

            if (m_timeTillNextRespawn < 0)
            {
                return NewMob();
            }
            
            return null;

        }

        public void updateDespawn(double timeSinceLastUpdate)
        {
            
            m_timeTillDespawn -= (float)timeSinceLastUpdate;
          
            if (m_timeTillDespawn < 0)
            {
                Despawn();

            }
        }


        private void Despawn()
        {
            foreach (ServerControlledEntity m in m_zone.TheMobs)
            {
                if (m != null)
                {
                    if (m.m_spawnPointID==m_serverID)
                    {
                        

                        if (!m.ConductingHostileAction())
                        {
                            m.m_isDespawning = true;

                            foreach(Character p in m.m_nearbyPlayers)
                            {

                                Program.processor.sendMobDespawnMessage(m.ServerID, m.m_isDespawning,p.m_player);
                            }
                           
                            m.ForceKill();
                        }
                        
                    }

                }
            }  
        }


        public void AddMobToList(int mobID, int probability)
        {
            SpawnPointMob newMob = new SpawnPointMob(mobID, m_probabilitySum+probability);
            m_mobList.Add(newMob);
            m_probabilitySum += probability;
        }

        double getRespawnTime()
        {
            if (m_maxRespawnTime - m_minRespawnTime <= 0)
                return m_minRespawnTime;
            else
                return m_minRespawnTime + Program.getRandomNumber((int)((m_maxRespawnTime - m_minRespawnTime) * 10)) / 10;
        }

        double getDespawnTime()
        {
            if (m_maxDespawnTime - m_minDespawnTime <= 0)
                return m_minDespawnTime;
            else
                return m_minDespawnTime + Program.getRandomNumber((int)((m_maxDespawnTime - m_minDespawnTime) * 10)) / 10;
        }

        protected ServerControlledEntity NewMob()
        {
            int newMobID = -1;
            int randomResult = Program.getRandomNumber(m_probabilitySum);
           
            //If a mob is to despawn, get it's timer.
            if (m_despawn)
            {
                m_timeTillDespawn = getDespawnTime();
                
            }
            
            
            for (int i = 0; i < m_mobList.Count; i++)
            {
                if (m_mobList[i].Probability > randomResult)
                {
                    newMobID = m_mobList[i].MobID;
                    break;
                }
            }

            MobTemplate mobData = MobTemplateManager.GetItemForID(newMobID);
            if (mobData == null)
            {
                m_timeTillNextRespawn = getRespawnTime();
                
                if (Program.m_LogNonSpawns)
                {
                    Program.Display("Spawn Point " + m_serverID + " spawned no mob");
                }
                return null;
            }

            ServerControlledEntity newMob = new ServerControlledEntity(mobData, m_spawnPosition, m_serverID,m_zone,m_serverID,m_despawn);
            
            if (m_movementAI == ServerControlledEntity.NPC_MOVEMENT_AI.NPC_MOVEMENT_AI_PATROL)
            {
                newMob.SetPatrol(m_patrolPoints);
            }
            else if (m_movementAI == ServerControlledEntity.NPC_MOVEMENT_AI.NPC_MOVEMENT_AI_ROAM)
            {
                newMob.SetRoamSettings(m_roamSettings);
            }
            newMob.PatrolSpeed = m_patrolSpeed;
            newMob.CurrentPosition.m_direction = m_spawnDirection;
            newMob.CurrentPosition.CorrectAngleForDirection();
            newMob.m_spawnDirection = m_spawnDirection;
            for (int i = 0; i<mobData.m_permStatusEffects.Count; i++)
            {
                CharacterEffectParams param = new CharacterEffectParams();
                param.charEffectId = mobData.m_permStatusEffects[i].m_effectID;
                param.caster = newMob;
                
                param.level = mobData.m_permStatusEffects[i].m_level;
                param.aggressive = false;
                param.PVP = false;
                param.statModifier = 0;
                CharacterEffectManager.InflictNewCharacterEffect(param, newMob);
            }
            if (mobData.m_permStatusEffects.Count > 0)
            {
                newMob.ResetStatModifiers(); 
                // added this call after the reset stat modifiers was removed from recalculate 
                // stat modifiers as the overarching character effects now use the status combat entity stats
                newMob.RecalculateStatModifiers();
            }
            m_timeTillNextRespawn = getRespawnTime();

            return newMob;
        }

    }
}
