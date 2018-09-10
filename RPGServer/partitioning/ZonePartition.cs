using System;
using System.Collections.Generic;
using XnaGeometry;

namespace MainServer.partitioning
{
    class ZonePartitionHolder
    {

        const int MIN_ENTITIES_TO_USE_PARTITION = 20;

        Zone m_owningZone = null;

        Vector2 m_startLocation;
        Vector2 m_endLocation;
        ZonePartition[][] m_partitions = null;

        double m_partitionLength = 0;
        int m_numHorizontalPartitions = 0;
        int m_numVerticalPartitions = 0;

        internal Vector2 StartLocation
        {
            get { return m_startLocation; }
        }

        internal double PartitionSize
        {
            get { return m_partitionLength; }
        }
        internal int NumHorizontalPartitions
        {
            get { return m_numHorizontalPartitions; }
        }
        internal int NumVerticalPartitions
        {
            get { return m_numVerticalPartitions; }
        }
        /// <summary>
        /// set up a ZonePartitionHolder with the partitions already created
        /// </summary>
        /// <param name="owningZone">the zone that owns the partitions</param>
        /// <param name="startPosition">The min position the partitions must account for </param>
        /// <param name="endPosition">the max position the partitions must account for </param>
        /// <param name="desiredSize">the preferred size of each partition</param>
        /// <param name="maxPartitions">the max number of partitions in one direction before the size of the partitions is increased</param>
        /// <param name="maxPartitionSize">The Maximum Size allowed for partitions to reach before they must Go above the desired max partitions</param>
        internal ZonePartitionHolder(Zone owningZone, Vector2 startPosition, Vector2 endPosition, float desiredSize, int maxPartitions, float maxPartitionSize)
        {
            SetUp(owningZone, startPosition, endPosition, desiredSize, maxPartitions,maxPartitionSize);
        }

        /// <summary>
        /// Sets up a grid of empty Zone Partitions
        /// </summary>
        /// <param name="owningZone">the zone that owns the partitions</param>
        /// <param name="startPosition">The min position the partitions must account for </param>
        /// <param name="endPosition">the max position the partitions must account for </param>
        /// <param name="desiredSize">the preferred size of each partition</param>
        /// <param name="maxPartitions">the max number of partitions in one direction before the size of the partitions is increased</param>
        /// <param name="maxPartitionSize">The Maximum Size allowed for partitions to reach before they must Go above the desired max partitions</param>
        void SetUp(Zone owningZone,Vector2 startPosition, Vector2 endPosition,float desiredSize, int maxPartitions, float maxPartitionSize)
        {
            m_owningZone = owningZone;

            double partitionLength = desiredSize;

            double areaWidth = endPosition.X - startPosition.X;
            double areaHeight = endPosition.Y - startPosition.Y;

            //what is the longest section?
            double maxLength = Math.Max(areaWidth, areaHeight);

            //how many partitions do we need at the standared size 
            int horizontalPartitions = (int)Math.Ceiling((areaWidth / desiredSize));
            int verticalPartitions = (int)Math.Ceiling((areaHeight / desiredSize));

            //are we allowed this many partitions at this scale
            if (horizontalPartitions > maxPartitions || verticalPartitions > maxPartitions)
            {
                //if not how big will they need to be to fit into the max partitions
                //make the partitions slightly bigger than the exact fit
                partitionLength = ((maxLength+1) / maxPartitions);

                //have these become too big, will we need to go over the max partitions?
                if (partitionLength > maxPartitionSize)
                {
                    //if their too big clamp them down
                    partitionLength = maxPartitionSize;
                    
                }

                //work out how many we need in each direction
                horizontalPartitions = (int)Math.Ceiling((areaWidth / partitionLength));
                verticalPartitions = (int)Math.Ceiling((areaHeight / partitionLength));

            }

            m_partitionLength = partitionLength;
            m_numHorizontalPartitions = horizontalPartitions;
            m_numVerticalPartitions = verticalPartitions;
            m_startLocation = startPosition;

            //what is the final end pos going to be
            m_endLocation.X = startPosition.X + horizontalPartitions * partitionLength;
            m_endLocation.Y = startPosition.Y + verticalPartitions * partitionLength;

            //We now know how many we need and of what size
            //time to create the array
            m_partitions = new ZonePartition[horizontalPartitions][];
            ZonePartition[] currentColumn = null;
            Vector2 currentStartPos = new Vector2(-1,-1);
            Vector2 currentEndPos = new Vector2(-1,-1);
            for (int i = 0; i < m_partitions.Length; i++)
            {
                currentStartPos.X = startPosition.X + i * m_partitionLength;
                currentEndPos.X = startPosition.X + i * m_partitionLength + m_partitionLength;
                
                m_partitions[i] = new ZonePartition[verticalPartitions];
                currentColumn = m_partitions[i];
                for (int j = 0; j < currentColumn.Length; j++)
                {
                    currentStartPos.Y = startPosition.Y + j * m_partitionLength;
                    currentEndPos.Y = startPosition.Y + j * m_partitionLength + m_partitionLength;
                    currentColumn[j] = new ZonePartition(currentStartPos, currentEndPos, this, owningZone,i,j);
                }
            }

        }

        internal void AddPartitionsInRangeToList(Vector2 position, double range, List<ZonePartition> thePartitions)
        {
            //get the max and min position 
            Vector2 RelativePos = position-m_startLocation;

            //get the grid location

            int minGridx = (int)Math.Floor((RelativePos.X-range) / m_partitionLength);
            int minGridy = (int)Math.Floor((RelativePos.Y-range) / m_partitionLength);
            if (minGridx < 0)
            {
                minGridx = 0;
            }
            else if (minGridx >= m_numHorizontalPartitions)
            {
                minGridx = m_numHorizontalPartitions - 1;
            }
            if (minGridy < 0)
            {
                minGridy = 0;
            }
            else if (minGridy >= m_numVerticalPartitions)
            {
                minGridy = m_numVerticalPartitions - 1;
            }
            //the floor must be taken because we want to know what grid square it is definatly in then add 1 to it, 
            //ceiling would be risky
            int maxGridx = 1 + (int)Math.Floor((RelativePos.X+range) / m_partitionLength);
            int maxGridy = 1 + (int)Math.Floor((RelativePos.Y+range) / m_partitionLength);
            if (maxGridx < 0)
            {
                maxGridx = 0;
            }
            else if (maxGridx > m_numHorizontalPartitions)
            {
                maxGridx = m_numHorizontalPartitions;
            }
            if (maxGridy < 0)
            {
                maxGridy = 0;
            }
            else if (maxGridy > m_numVerticalPartitions)
            {
                maxGridy = m_numVerticalPartitions ;
            }
            thePartitions.Capacity = (maxGridx - minGridx) * (maxGridy - minGridy);

            for (int i = minGridx; i < maxGridx; i++)
            {
                for (int j = minGridy; j < maxGridy; j++)
                {
                    thePartitions.Add(m_partitions[i][j]);
                }
            }
        }


        internal ZonePartition GetPartitionForPosition( Vector3 position)
        {
            ZonePartition thePartition=null;
            int gridX = 0;
            int gridY = 0;
            //find out it's position on the grid
            GetGridPosForPosition(ref gridX, ref gridY, position);
            //get the partition if it is within the grid
            thePartition = GetPartitionForGridPosition(gridX,gridY);;
            

            return thePartition;
        }
        internal ZonePartition GetPartitionForGridPosition(int gridX, int gridY)
        {
            ZonePartition thePartition = null;
            if (gridY >= 0 && gridX >= 0 && gridY < m_numVerticalPartitions && gridX < m_numHorizontalPartitions)
            {
                thePartition = m_partitions[gridX][gridY];
            }

            return thePartition;
        }
        internal void GetGridPosForPosition(ref int gridX, ref int gridY, Vector3 position)
        {
            gridX = (int)Math.Floor((position.X-m_startLocation.X) / m_partitionLength);
            gridY = (int)Math.Floor((position.Z - m_startLocation.Y) / m_partitionLength);
        }
        internal void Update()
        {
            for (int i = 0; i < m_partitions.Length; i++)
            {
                ZonePartition[] currentCol = m_partitions[i];
                for (int j=0;j<currentCol.Length; j++)
                {
                    ZonePartition currentPartition = currentCol[j];
                    currentPartition.Update();
                }
            }
        }
        internal void AddEntitiesInRangeToList(CombatEntity entity, Vector3 position, double range, List<CombatEntity> theEntities, ZonePartition.ENTITY_TYPE type, CombatEntity entityToExclude, bool ignoreGatheringMobs = false)
        {
            List<ZonePartition> partitions = new List<ZonePartition>();
            Vector2 position2D = new Vector2(position.X, position.Z);

            AddPartitionsInRangeToList(position2D, range, partitions);

            //now loop trough the partitions and 
            for (int i = 0; i < partitions.Count; i++)
            {
                ZonePartition currentPartition = partitions[i];
                currentPartition.AddLocalEntitiesInRangeToList(entity, position, range, theEntities, type, entityToExclude, ignoreGatheringMobs);
            }

        }
        internal void AddPlayersInRangeToList(CombatEntity entity, Vector3 position, float range, List<Player> theList, ZonePartition.ENTITY_TYPE type, CombatEntity entityToExclude)
        {

            if (m_owningZone.m_players.Count > MIN_ENTITIES_TO_USE_PARTITION)
            {
                List<ZonePartition> partitions = new List<ZonePartition>();
                Vector2 position2D = new Vector2(position.X, position.Z);

                AddPartitionsInRangeToList(position2D, range, partitions);

                //now loop trough the partitions and 
                for (int i = 0; i < partitions.Count; i++)
                {
                    ZonePartition currentPartition = partitions[i];
                    currentPartition.AddLocalPlayersInRangeToList(entity, position, range, theList, type,entityToExclude);
                }
            }
            else
            {
                float rangeSqr = range * range;
                List<Player> thePlayers = m_owningZone.m_players;

                for (int i = 0; i < thePlayers.Count; i++)
                {
                    Player currentPlayer = thePlayers[i];
                    Character currentCharcter = currentPlayer.m_activeCharacter;
                    if (currentCharcter != null&&currentCharcter!=entityToExclude)
                    {
                        double distance = Utilities.Difference2DSquared(currentCharcter.CurrentPosition.m_position, position);

                        if (distance < rangeSqr)
                        {
                            theList.Add(currentPlayer);
                        }
                    }
                }
            }

        }
        internal void AddMobsInRangeToList(CombatEntity entity, Vector3 position, float range, List<ServerControlledEntity> theList, ZonePartition.ENTITY_TYPE type, CombatEntity entityToExclude)
        {
            List<ZonePartition> partitions = new List<ZonePartition>();
            Vector2 position2D = new Vector2(position.X, position.Z);

            AddPartitionsInRangeToList(position2D, range, partitions);

            //now loop trough the partitions and 
            for (int i = 0; i < partitions.Count; i++)
            {
                ZonePartition currentPartition = partitions[i];
                currentPartition.AddLocalMobsInRangeToList(entity, position, range, theList, type, entityToExclude);
            }

        }

    }

    class ZonePartition
    {
        internal enum ENTITY_TYPE
        {
            ET_NONE = 0,
            ET_MOB = 1,
            ET_PLAYER = 2,
            ET_ENEMY = 4,
            ET_NOT_ENEMY = 8
        };

        /// <summary>
        /// the partition holder that owns this partition, required for getting information about other partitions in the zone
        /// </summary>
        ZonePartitionHolder m_partitionHolder=null;
        /// <summary>
        /// The zone that this partition is in
        /// </summary>
        Zone m_owningZone = null;
        internal Vector2 m_startLocation;
        internal Vector2 m_endLocation;
        int m_gridX = 0;
        int m_gridY = 0;

        internal ZonePartition(Vector2 startPos, Vector2 endPos,ZonePartitionHolder holder, Zone zone, int gridX, int gridY)
        {
            m_partitionHolder = holder;
            m_owningZone = zone;
            m_startLocation = startPos;
            m_endLocation = endPos;
            m_gridX = gridX;
            m_gridY = gridY;
        }

        List<Player> m_players = new List<Player>();
        List<ServerControlledEntity> m_mobs = new List<ServerControlledEntity>();
        List<EffectArea> m_areas = new List<EffectArea>();
        internal List<EffectArea> Areas
        {
            get { return m_areas; }
        }
        //functions to get info on

        internal void AddEntitiesInRangeToList(CombatEntity entity, Vector3 position, float range, List<CombatEntity> theEntities, ENTITY_TYPE type, CombatEntity entityToExclude)
        {
            m_partitionHolder.AddEntitiesInRangeToList(entity, position, range, theEntities, type,entityToExclude);
        }
        internal void AddPlayersInRangeToList(CombatEntity entity, Vector3 position, float range, List<Player> theList, ENTITY_TYPE type, CombatEntity entityToExclude)
        {
            m_partitionHolder.AddPlayersInRangeToList(entity, position, range, theList, type,entityToExclude);
        }
        internal void AddMobsInRangeToList(CombatEntity entity, Vector3 position, float range, List<ServerControlledEntity> theList, ENTITY_TYPE type, CombatEntity entityToExclude)
        {
            m_partitionHolder.AddMobsInRangeToList(entity, position, range, theList, type,entityToExclude);
        }
        //close by entities
        internal void AddLocalEntitiesInRangeToList(CombatEntity entity,Vector3 position, double range, List<CombatEntity> theEntities, ENTITY_TYPE type,CombatEntity entityToExclude, bool ignoreGatheringMobs = false)
        {
            //should enemy status be checked?

            //if so are we looking for enemy or ally
            bool isEnemy = (type & ENTITY_TYPE.ET_ENEMY)>0;
            bool isNotEnemy=(type & ENTITY_TYPE.ET_NOT_ENEMY)>0;
             bool checkEnemy = isEnemy ^ isNotEnemy ;
           
            //if looking for players only check the players list
            // otherwise look through both
            bool checkMobs = (type & ENTITY_TYPE.ET_MOB)>0;
            //if looking for mobs only check through mobs 
            // otherwise look through both
            bool checkPlayer = (type & ENTITY_TYPE.ET_PLAYER)>0;
            if (!checkMobs && !checkPlayer)
            {
                checkMobs = true;
                checkPlayer = true;
            }
            double distSqr = range * range; 
            for (int i = 0; i < m_mobs.Count && (checkMobs == true); i++)
            {
                ServerControlledEntity theMob = m_mobs[i];
                if (theMob == null || theMob == entityToExclude)
                {
                    continue;
                }
                if (theMob.Gathering != CombatEntity.LevelType.none && ignoreGatheringMobs == true)
                {
                    continue;
                }

                double mobDist = Utilities.Difference2DSquared(theMob.CurrentPosition.m_position, position);
                //is it in range
                if (mobDist <= distSqr)
                {
                    if (checkEnemy == true)
                    {
                        if ((entity != null) && (entity.IsEnemyOf(theMob) == isEnemy)&&(theMob.IsAllyOf(entity) != isEnemy))
                        {
                            theEntities.Add(theMob);
                        }
                    }
                    else
                    {
                        theEntities.Add(theMob);
                    }
                }
            }

            for (int i = 0; i < m_players.Count && (checkPlayer == true); i++)
            {

                Player thePlayer = m_players[i];
                Character theCharacter = thePlayer.m_activeCharacter;
                if (theCharacter == null || theCharacter == entityToExclude)
                {
                    continue;
                }
                double mobDist = Utilities.Difference2DSquared(theCharacter.CurrentPosition.m_position, position);
                //is it in range
                if (mobDist <= distSqr)
                {
                    if (checkEnemy == true)
                    {
                        if ((entity != null) && (entity.IsEnemyOf(theCharacter) == isEnemy) && (theCharacter.IsAllyOf(entity) != isEnemy))
                        {
                            theEntities.Add(theCharacter);
                        }
                    }
                    else
                    {
                        theEntities.Add(theCharacter);
                    }
                }
            }

            
        }
        //close by players
        internal void AddLocalPlayersInRangeToList(CombatEntity entity, Vector3 position, float range, List<Player> theList, ENTITY_TYPE type, CombatEntity entityToExclude)
        {
            float distSqr = range * range; 
            //if so are we looking for enemy or ally
            bool isEnemy = (type & ENTITY_TYPE.ET_ENEMY)>0;
            bool isNotEnemy = (type & ENTITY_TYPE.ET_NOT_ENEMY) > 0;

            //should enemy status be checked?
            bool checkEnemy = isEnemy ^ isNotEnemy;
            //if looking for mobs only check through mobs 
            // otherwise look through both
            if ((type & ENTITY_TYPE.ET_MOB)>0)
            {
                //Program.Display("");
                throw new Exception("AddLocalPlayersInRangeToList attempted to add mobs to player only list");
            }



            for (int i = 0; i < m_players.Count; i++)
            {

                Player thePlayer = m_players[i];
                Character theCharacter = thePlayer.m_activeCharacter;
                if (theCharacter == null || theCharacter == entityToExclude)
                {
                    continue;
                }
                double mobDist = Utilities.Difference2DSquared(theCharacter.CurrentPosition.m_position, position);
                //is it in range
                if (mobDist <= distSqr)
                {
                    if (checkEnemy == true)
                    {
                        if ((entity != null) && (entity.IsEnemyOf(theCharacter) == isEnemy))
                        {
                            theList.Add(thePlayer);
                        }
                    }
                    else
                    {
                        theList.Add(thePlayer);
                    }
                }
            }


        }

        //close by mobs
        internal void AddLocalMobsInRangeToList(CombatEntity entity, Vector3 position, float range, List<ServerControlledEntity> theList, ENTITY_TYPE type, CombatEntity entityToExclude)
        {
            float distSqr = range * range; 
            
            //if so are we looking for enemy or ally
            bool isEnemy = (type & ENTITY_TYPE.ET_ENEMY)>0;
            bool isNotEnemy = (type & ENTITY_TYPE.ET_NOT_ENEMY) > 0;
            //should enemy status be checked?
            bool checkEnemy = isEnemy ^ isNotEnemy;
            //if looking for mobs only check through mobs 
            // otherwise look through both
            if ( (type & ENTITY_TYPE.ET_PLAYER)>0)
            {
                //Program.Display("");
                throw new Exception("AddLocalMobsInRangeToList attempted to add mobs to player only list");
            }

            for (int i = 0; i < m_mobs.Count ; i++)
            {

                ServerControlledEntity theMob = m_mobs[i];
                if (theMob == null || theMob == entityToExclude)
                {
                    continue;
                }
                double mobDist = Utilities.Difference2DSquared(theMob.CurrentPosition.m_position, position);
                //is it in range
                if ( mobDist <= distSqr)
                {
                    if (checkEnemy == true)
                    {
                        if ((entity != null) && (entity.IsEnemyOf(theMob) == isEnemy))
                        {
                            theList.Add(theMob);
                        }
                    }
                    else
                    {
                        theList.Add(theMob);
                    }
                }
            }



        }


        internal void EntityLeavingPartition(CombatEntity entity)
        {
            bool removed = false;
            if (entity.Type == CombatEntity.EntityType.Mob)
            {
                for (int i = 0; i < m_mobs.Count; i++)
                {
                    ServerControlledEntity theMob = m_mobs[i];
                    if (theMob.ServerID == entity.ServerID)
                    {
                        m_mobs.Remove(theMob);
                        removed = true;
                    }
                }

            }
            else if (entity.Type == CombatEntity.EntityType.Player)
            {
                for (int i = 0; i < m_players.Count; i++)
                {
                    Player currentPlayer =m_players[i];

                    Character currentCharacter = currentPlayer.m_activeCharacter;
                    if (currentCharacter != null && currentCharacter.ServerID == entity.ServerID)
                    {
                        m_players.Remove(currentPlayer);
                        removed = true;
                    }
                }
           }
            if (m_owningZone.m_zone_id == 2 && (/*entity.ServerID == 277 ||*/ entity.Type == CombatEntity.EntityType.Player))
            {
                if (removed == true)
                {
                    if (Program.m_LogPartitionUpdates)
                    {
                        Program.Display(entity.GetIDString() + " left " + m_owningZone.m_zone_name + " partition " + m_gridX + "," + m_gridY);
                    }
                }
                else
                {
                    Program.Display("Partitioning Error:" + entity.GetIDString() + " failed to leave " + m_owningZone.m_zone_name + " partition " + m_gridX + "," + m_gridY);
                }
            }
        }
        internal void EntityEnteringPartition(CombatEntity entity)
        {
            bool added = false;
            if (entity.Type == CombatEntity.EntityType.Mob)
            {
                ServerControlledEntity theMob = (ServerControlledEntity)entity;

                if(theMob!=null&&m_mobs.Contains(theMob)==false)
                {
                    m_mobs.Add(theMob);
                    added = true;
                }

            }
            else if (entity.Type == CombatEntity.EntityType.Player)
            {
                Character currentCharacter = (Character)entity;
                Player currentPlayer = currentCharacter.m_player;
                if (currentPlayer!=null&&m_players.Contains(currentPlayer) == false)
                {
                    m_players.Add(currentPlayer);
                    added = true;
                }
            }
            if (m_owningZone.m_zone_id == 2&&(/*entity.ServerID==277||*/entity.Type== CombatEntity.EntityType.Player))
            {
                if (added == true)
                {
                    if (Program.m_LogInactivityUpdates)
                    {
                        Program.Display(entity.GetIDString() + " entered" + m_owningZone.m_zone_name + " partition " + m_gridX + "," + m_gridY);
                    }
                }
                else
                {
                    Program.Display("Partitioning Error:" + entity.GetIDString() + " failed to enter " + m_owningZone.m_zone_name + " partition " + m_gridX + "," + m_gridY);
                }
            }
        }

        internal void EffectAreaEnteringPartition(EffectArea area)
        {
            if (area != null && m_areas.Contains(area) == false)
            {
                m_areas.Add(area);
            }
        }

        internal void Update()
        {
            //check through all members and check they are in the right place
            //if not then scream and shout as something has gone wronge!!

            //check the mobs
            for (int i = 0; i < m_mobs.Count;i++ )
            {
                ServerControlledEntity theMob = m_mobs[i];
                if (m_partitionHolder.GetPartitionForPosition(theMob.CurrentPosition.m_position) != this)
                {
                    //tell them their wrong, check where they think they are
                    Program.Display("ZonePartition Update found "+theMob.GetIDString()+" out of position");
                }
            }
            //check the players
            for (int i = 0; i < m_players.Count; i++)
            {
                Player currentPlayer = m_players[i];

                Character currentCharacter = currentPlayer.m_activeCharacter;
                if (currentCharacter != null && m_partitionHolder.GetPartitionForPosition(currentCharacter.CurrentPosition.m_position) != this)
                {
                    //tell them their wrong, check where they think they are
                    Program.Display("ZonePartition Update found " + currentCharacter.GetIDString() + " out of position");
                }
            }
        }
    }
}
