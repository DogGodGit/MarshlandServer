using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MainServer.Collisions;
using XnaGeometry;
using MainServer.Localise;

namespace MainServer.partitioning
{
    class EntityAreaConditionalEffect 
    {
		// #localisation
		public class EntityAreaConditionalEffectTextDB : TextEnumDB
		{
			public EntityAreaConditionalEffectTextDB() : base(nameof(EntityAreaConditionalEffect), typeof(TextID)) { }

			public enum TextID
			{
				LEFT_PVP_AREA,                  //"You have left the {pvpString0} area."
				LEFT_NO_FAST_TRAVEL_AREA,       //"You have left the no fast-travel area."
				ENTER_PVP_AREA,                 //"You have entered a {pvpString0} area."
				ENTER_NO_FAST_TRAVEL_AREA,      //"You have entered a no fast-travel area."
			}
		}
		public static EntityAreaConditionalEffectTextDB textDB = new EntityAreaConditionalEffectTextDB();

		internal EntityAreaConditionalEffect(AreaConditionalEffect theEffect)
        {
            m_theEffect = theEffect;
        }

        AreaConditionalEffect m_theEffect = null;

        double m_timeAtLastUpdate = 0;
        internal AreaConditionalEffect TheEffect
        {
            get {return m_theEffect; }
        }
        
        internal void ChangeOwnerEffect(AreaConditionalEffect newEffect)
        {
            m_theEffect = newEffect;
        }
        internal void EndEffect(CombatEntity affectedEntity)
        {
            switch (TheEffect.EffectType)
            {
                case AreaConditionalEffect.ACE.IsPVP:
                    {
                        AreaConditionalEffectData typeData = TheEffect.GetDataOfType(AreaConditionalEffect.ACDT.PVPType);

                        if (typeData != null && affectedEntity.Type == CombatEntity.EntityType.Player)
                        {
                            int typeIndex = (int)typeData.DataValue;
                            Character affectedCharacter = (Character)affectedEntity;
							//string pvpString = "";
							//if (typeIndex >= 0 && typeIndex < Character.PVPTypeStrings.Length)
							//{
							//    pvpString = Character.PVPTypeStrings[typeIndex];
							//}
							string pvpString = affectedCharacter.GetPVPTypeString(typeIndex);
                            if (pvpString.Length > 0 && affectedCharacter != null && affectedCharacter.m_player != null)
                            {
								string locText = Localiser.GetString(textDB, affectedCharacter.m_player, (int)EntityAreaConditionalEffectTextDB.TextID.LEFT_PVP_AREA);
								locText = string.Format(locText, pvpString);
								Program.processor.sendSystemMessage(locText, affectedCharacter.m_player, false, SYSTEM_MESSAGE_TYPE.PVP);
							}
                        }
                        affectedEntity.PVPTypeChanged();
                        break;
                    }
                case AreaConditionalEffect.ACE.PreventsFastTravel:
                    {
                        if (affectedEntity.Type == CombatEntity.EntityType.Player)
                        {

                            Character affectedCharacter = (Character)affectedEntity;
							string locText = Localiser.GetString(textDB, affectedCharacter.m_player, (int)EntityAreaConditionalEffectTextDB.TextID.LEFT_NO_FAST_TRAVEL_AREA);
							Program.processor.sendSystemMessage(locText, affectedCharacter.m_player, false, SYSTEM_MESSAGE_TYPE.NONE);
						}
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }
        internal void StartUpEffect(CombatEntity affectedEntity)
        {
            m_timeAtLastUpdate = Program.MainUpdateLoopStartTime();

            if (affectedEntity.Dead)
            {
                return;
            }
            switch (TheEffect.EffectType)
            {
                case AreaConditionalEffect.ACE.InflictStatus:
                    {

                        int statusEffectID = 0;
                        int statusEffectLevel = 0;
                        int statusEffectCastAsLevel = 0;
                        AreaConditionalEffectData typeData = TheEffect.GetDataOfType(AreaConditionalEffect.ACDT.StatusEffectID);
                        AreaConditionalEffectData levelData = TheEffect.GetDataOfType(AreaConditionalEffect.ACDT.StatusEffectLevel);
                        AreaConditionalEffectData castAsLevelData = TheEffect.GetDataOfType(AreaConditionalEffect.ACDT.StatusEffectCastAsLevel);
                        if (typeData != null)
                        {
                            statusEffectID = typeData.DataValue;
                        }
                        if (levelData != null)
                        {
                            statusEffectLevel = levelData.DataValue;
                        }

                        CharacterEffectParams param = new CharacterEffectParams();
                        param.charEffectId = (EFFECT_ID)statusEffectID;
                        param.caster = null;
                        param.level = statusEffectLevel;
                        param.aggressive = false;
                        param.PVP = false;
                        param.statModifier = 0;

                        if (castAsLevelData != null)
                        {
                            statusEffectCastAsLevel = castAsLevelData.DataValue;

                            CharacterEffectManager.InflictNewCharacterEffect(param, affectedEntity);

                            // Test if the status effect went off
                            CharacterEffect newEffect = param.QueryStatusEffect(param.charEffectId);
                            if (newEffect != null && newEffect.StatusEffect != null)
                            {
                                newEffect.StatusEffect.CasterLevel = statusEffectCastAsLevel;
                            }

                            /*                            
                                                        StatusEffect newEffect = affectedEntity.InflictNewStatusEffect((EFFECT_ID)statusEffectID, null, statusEffectLevel, false, false, 0);
                                                        if (newEffect != null)
                                                        {
                                                            newEffect.CasterLevel = statusEffectCastAsLevel;
                                                        }
                             */
                        }
                        else
                        {
                            CharacterEffectManager.InflictNewCharacterEffect(param, affectedEntity);
                            //affectedEntity.InflictNewStatusEffect((EFFECT_ID)statusEffectID, null, statusEffectLevel, false, false, 0);
                        }

                        break;
                    }
                case AreaConditionalEffect.ACE.RemovesStatus:
                    {
                        break;
                    }
                case AreaConditionalEffect.ACE.IsPVP:
                    {
                        AreaConditionalEffectData typeData = TheEffect.GetDataOfType(AreaConditionalEffect.ACDT.PVPType);
                 
                        if (typeData!=null&&affectedEntity.Type == CombatEntity.EntityType.Player)
                        {
                            int typeIndex = (int)typeData.DataValue;
                            Character affectedCharacter = (Character)affectedEntity;
							//string pvpString = "";
							//if(typeIndex>=0&& typeIndex<Character.PVPTypeStrings.Length)
							//{
							//	pvpString = Character.PVPTypeStrings[typeIndex];
							//}
							string pvpString = affectedCharacter.GetPVPTypeString(typeIndex);
                            if (pvpString.Length>0 && affectedCharacter != null && affectedCharacter.m_player != null)
                            {
								string locText = Localiser.GetString(textDB, affectedCharacter.m_player, (int)EntityAreaConditionalEffectTextDB.TextID.ENTER_PVP_AREA);
								locText = string.Format(locText, pvpString);
								Program.processor.sendSystemMessage(locText, affectedCharacter.m_player, false, SYSTEM_MESSAGE_TYPE.PVP);
							}
                        }
                        affectedEntity.PVPTypeChanged();
                        break;
                    }
                case AreaConditionalEffect.ACE.PreventsFastTravel:
                    {
                        if (affectedEntity.Type == CombatEntity.EntityType.Player)
                        {
                           
                            Character affectedCharacter = (Character)affectedEntity;
							string locText = Localiser.GetString(textDB, affectedCharacter.m_player, (int)EntityAreaConditionalEffectTextDB.TextID.ENTER_NO_FAST_TRAVEL_AREA);
							Program.processor.sendSystemMessage(locText, affectedCharacter.m_player, false, SYSTEM_MESSAGE_TYPE.NONE);
						}
                    break;     
                    }
                default:
                    {
                        break;
                    }

            };
        }
        internal void UpdateEffect(double currentTime, CombatEntity affectedEntity)
        {
            if (TheEffect.UpdateTime <= 0)
            {
                return;
            }
            if (affectedEntity.Dead)
            {
                return;
            }
            if (currentTime > m_timeAtLastUpdate + TheEffect.UpdateTime)
            {
                m_timeAtLastUpdate = currentTime;
                switch (TheEffect.EffectType)
                {
                    case AreaConditionalEffect.ACE.InflictStatus:
                        {
                            int statusEffectID = 0;
                            int statusEffectLevel = 0;
                            int statusEffectCastAsLevel = 0;
                            AreaConditionalEffectData typeData = TheEffect.GetDataOfType(AreaConditionalEffect.ACDT.StatusEffectID);
                            AreaConditionalEffectData levelData = TheEffect.GetDataOfType(AreaConditionalEffect.ACDT.StatusEffectLevel);
                            AreaConditionalEffectData castAsLevelData = TheEffect.GetDataOfType(AreaConditionalEffect.ACDT.StatusEffectCastAsLevel);
                            if (typeData != null)
                            {
                                statusEffectID = typeData.DataValue;
                            }
                            if (levelData != null)
                            {
                                statusEffectLevel = levelData.DataValue;
                            }

                            CharacterEffectParams param = new CharacterEffectParams();
                            param.charEffectId = (EFFECT_ID)statusEffectID;
                            param.caster = null;
                            param.level = statusEffectLevel;
                            param.aggressive = false;
                            param.PVP = false;
                            param.statModifier = 0;

                            if (castAsLevelData != null)
                            {
                                statusEffectCastAsLevel = castAsLevelData.DataValue;

                                CharacterEffectManager.InflictNewCharacterEffect(param, affectedEntity);

                                // Test if the status effect went off
                                CharacterEffect newEffect = param.QueryStatusEffect(param.charEffectId);
                                if (newEffect != null && newEffect.StatusEffect != null)
                                {
                                    newEffect.StatusEffect.CasterLevel = statusEffectCastAsLevel;
                                }

/*                              StatusEffect newEffect=affectedEntity.InflictNewStatusEffect((EFFECT_ID)statusEffectID, null, statusEffectLevel, false, false, 0);
                                if (newEffect != null)
                                {
                                    newEffect.CasterLevel = statusEffectCastAsLevel;
                                }
*/
                            }
                            else
                            {
                                CharacterEffectManager.InflictNewCharacterEffect(param, affectedEntity);
                                //affectedEntity.InflictNewStatusEffect((EFFECT_ID)statusEffectID, null, statusEffectLevel, false, false, 0);
                            }

                            break;
                        }
                    case AreaConditionalEffect.ACE.RemovesStatus:
                        {
                            break;
                        }
                    default:
                        {
                            break;
                        }

                };
            }
        }
        
    }
    class AreaConditionalEffectData
    {
        internal AreaConditionalEffectData(AreaConditionalEffect.ACDT dataType, int dataValue)
        {
            m_dataType = dataType;
            m_dataValue = dataValue;
        }
        AreaConditionalEffect.ACDT m_dataType;
        int m_dataValue;

        internal AreaConditionalEffect.ACDT DataType
        {
            get { return m_dataType; }

        }
        internal int DataValue
        {
            get { return m_dataValue; }
        }

        
        
    }
    
    class AreaConditionalEffect 
    {


        internal EffectArea TheArea
        {
            get { return m_area; }
        }
        //public declaration of different types of effect targets
        /// <summary>
        /// AreaConditionalEffectTargets
        /// </summary>
        public enum ACET
        {
            COMBAT_ENTITIES=1,
            PLAYERS = 2,
            MOBS = 3
        };

        //public decleration of different types of effect
        /// <summary>
        /// AreaConditionalEffects
        /// </summary>
        public enum ACE
        {
            /// <summary>
            /// Applies and maintains an effect on an entity
            /// </summary>
            InflictStatus=1,
            /// <summary>
            /// Removes a particular effect on entities
            /// </summary>
            RemovesStatus=2,
            /// <summary>
            /// When a player enters this area they go into whatever pvp mode is set
            /// </summary>
            IsPVP=3,
            /// <summary>
            /// Area Prevents Entities from Applying
            /// </summary>
            IgnoresAreasWithType=4,
            /// <summary>
            /// removes effects that are already active
            /// </summary>
            ClearsAreasWithType=5,
            /// <summary>
            /// stops the player from using items
            /// </summary>
            PreventsItemUse =6,
            /// <summary>
            /// stops player teleport requests being accepted
            /// </summary>
            PreventsFastTravel = 7,
        }
        /// <summary>
        /// AreaConditionalDataTypes
        /// </summary>
        public enum ACDT
        {
            StatusEffectID =1,
            StatusEffectLevel=2,
            PVPType=3,
            StatusEffectCastAsLevel=4
        }
        

        EffectArea m_area = null;
        int m_uniqueID = 0;
        int m_setID = 0;
        /// <summary>
        /// what kind of entity does this effect target
        /// </summary>
        ACET m_targetType = 0;
        /// <summary>
        /// what does this effect do
        /// </summary>
        ACE m_effectType = 0;
        /// <summary>
        /// how often is this effect Re-applied
        /// </summary>
        float m_updateTime = 0;
        /// <summary>
        /// what are the values to allow this effect to do this
        /// eg, status effect ID, PVP Type
        /// </summary>
        List<AreaConditionalEffectData> m_effectData = new List<AreaConditionalEffectData>();
        
        /// <summary>
        /// the Id which determins what effects this can interact with
        /// </summary>
        internal int SetID
        {
            get { return m_setID; }
        }
        /// <summary>
        /// what does this effect do
        /// </summary>
        internal ACE EffectType
        {
            get { return m_effectType; }
        }
        /// <summary>
        /// what kind of entity does this effect target
        /// </summary>
        internal ACET TargetType
        {
            get { return m_targetType; }
        }
        /// <summary>
        /// how often is this effect Re-applied
        /// </summary>
        internal float UpdateTime
        {
            get { return m_updateTime; }
        }
        /// <summary>
        /// what are the values to allow this effect to do this
        /// eg, status effect ID, PVP Type
        /// </summary>
        List<AreaConditionalEffectData> EffectData
        {
            get { return m_effectData; }
        }

        internal bool PreEffect
        {
            get { return ((m_effectType == ACE.IgnoresAreasWithType)||(m_effectType == ACE.ClearsAreasWithType)); }
        }
        internal AreaConditionalEffect(SqlQuery query, EffectArea containingArea)
        {
            m_area = containingArea;
            m_uniqueID = query.GetInt32("zone_effect_area_effect_id");
            m_setID = query.GetInt32("set_id");
            m_effectType =  (ACE)query.GetInt32("effect_type_id");
            m_targetType = (ACET)query.GetInt32("target_id");
            string dataString = query.GetString("type_data");

            m_updateTime = query.GetFloat("update_time");
            //tear apart the typeData
            string[] effectDataStringArray = dataString.Split('|');
            for (int i = 0; i < effectDataStringArray.Length; i++)
            {
                string currentEffectString = effectDataStringArray[i];
                string[] currentDataArray = currentEffectString.Split(',');
                if (currentDataArray.Length > 1)
                {
                    AreaConditionalEffectData newData = new AreaConditionalEffectData((ACDT)int.Parse(currentDataArray[0]), int.Parse(currentDataArray[1]));
                    m_effectData.Add(newData);
                }
            }
        }

       internal AreaConditionalEffectData GetDataOfType(ACDT typeRequired)
        {
            for (int i = 0; i < m_effectData.Count; i++)
            {
                AreaConditionalEffectData currentData = m_effectData[i];
                if (currentData.DataType == typeRequired)
                {
                    return currentData;
                }
            }
                return null;
        }

        internal bool CanBeAppliedToEntity(CombatEntity theEntity)
        {
            bool isValid = (m_targetType == ACET.COMBAT_ENTITIES);

            if (theEntity.Type == CombatEntity.EntityType.Mob && m_targetType == ACET.MOBS)
            {
                isValid = true;
            }
            else if (theEntity.Type == CombatEntity.EntityType.Player && m_targetType == ACET.PLAYERS)
            {
                isValid = true;
            }
            return isValid;

        }
        /// <summary>
        /// Checks one effect against another to see if they would behave in the same way
        /// </summary>
        /// <param name="effect1"></param>
        /// <param name="effect2"></param>
        /// <returns></returns>
        static internal bool CompareEffects(AreaConditionalEffect effect1, AreaConditionalEffect effect2)
        {
            bool currentlyMatches = true;

            if (effect1.EffectType != effect2.EffectType)
            {
                currentlyMatches = false;
            }
            if (effect1.SetID != effect2.SetID)
            {
                currentlyMatches = false;
            }
            if (effect1.UpdateTime != effect2.UpdateTime)
            {
                currentlyMatches = false;
            }
            if (effect1.TargetType != effect2.TargetType)
            {
                currentlyMatches = false;
            }
            if (effect1.EffectData.Count != effect2.EffectData.Count)
            {
                currentlyMatches = false;
            }
            for (int i = 0; i < effect1.EffectData.Count && currentlyMatches==true; i++)
            {
                AreaConditionalEffectData currentEffect1Data = effect1.EffectData[i];
                AreaConditionalEffectData currentEffect2Data = effect2.GetDataOfType(currentEffect1Data.DataType);

                if (currentEffect2Data == null || currentEffect2Data.DataValue != currentEffect1Data.DataValue)
                {
                    currentlyMatches = false;
                }
            }

            return currentlyMatches;
        }
      

        /// <summary>
        /// Incomplete
        /// using Pre-effects clear down the List of new post effects and current post effects
        /// remember any forgotton current Effects 
        /// </summary>
        /// <param name="preEffects"></param>
        /// <param name="postEffects"></param>
        /// <param name="currentEffects"></param>
        /// <param name="forgottonEffects"></param>
        static internal void ClearDownPostEffectsFromPreEffects
            (List<AreaConditionalEffect>preEffects,List<AreaConditionalEffect>postEffects,
            List<EntityAreaConditionalEffect> currentEffects, List<EntityAreaConditionalEffect> forgottonEffects)
        {
            //for each pre effect
            for (int i = 0; i < preEffects.Count; i++)
            {
                AreaConditionalEffect currentPreEffect = preEffects[i];
                //what lists will it need to check against
                bool checkStandardList = (currentPreEffect.EffectType == ACE.ClearsAreasWithType) || (currentPreEffect.EffectType == ACE.IgnoresAreasWithType);
                bool checkCurrentLists = (currentPreEffect.EffectType == ACE.ClearsAreasWithType);
                //what ID will it remove
                int idToClear = currentPreEffect.SetID;
                //if it needs to check the standard post effects
                for (int postEffectIndex = postEffects.Count - 1; postEffectIndex >= 0 && checkStandardList==true; postEffectIndex--)
                {
                    AreaConditionalEffect currentPostEffect = postEffects[postEffectIndex];
                    //is this ID the same as the id to be removed
                    if (currentPostEffect.SetID == idToClear)
                    {
                        //if so then remove it from the list
                        postEffects.Remove(currentPostEffect);
                    }
                }

                //if it needs to check the already active effects
                for (int currentEffectIndex = currentEffects.Count - 1; currentEffectIndex >= 0 && checkCurrentLists==true; currentEffectIndex--)
                {
                    EntityAreaConditionalEffect currentEffect = currentEffects[currentEffectIndex];
                    //is this ID the same as the id to be removed
                    if (currentEffect.TheEffect.SetID == idToClear)
                    {
                        //if so then remove it from the list
                        currentEffects.Remove(currentEffect);
                        //remember it was removed
                        forgottonEffects.Add(currentEffect);
                    }
                }
            }
        }
    };

    class EffectArea
    {

        //remember the zone you are connected to
        Zone m_zone=null;
        int m_effectAreaID = -1;
        //hold on to the entities within you
        internal List<CombatEntity> m_entities=new List<CombatEntity>();
        //hold a record of what partitions you are within
        internal List<ZonePartition> m_partitions = new List<ZonePartition>();
        //how does this area effect what other areas do
        List<AreaConditionalEffect> m_preEffects = new List<AreaConditionalEffect>();
 
        //what sort of effects are done to Entities within the area
        List<AreaConditionalEffect> m_postEffects = new List<AreaConditionalEffect>();
        //what sort of area are you
        internal CCollisionObject m_collisionObject=null;
        internal Zone TheZone
        {
            get { return m_zone; }
        }
        internal EffectArea(Zone containingZone, int effectAreaID,string collision_string)
        {
            m_zone = containingZone;
            m_effectAreaID = effectAreaID;
            //collisionString = "COAACYLINDER Y 0.0 0.0 0.0 50 1";
            m_collisionObject= CCollisions.ReadCollisionObjectFromString(collision_string);
            LoadEffectsFromDatabase();
            CheckContainedData();
        }

        //if you have moved you'll need to update everyone involved, partitions and entities
        internal void CheckContainedData()
        {

            CheckEntities();
            CheckPartitions();

        }
        void CheckEntities()
        {
            //a list of those who have left the area
            List<CombatEntity> removedEntities = null;
            //a list of people who have entered the area
            List<CombatEntity> addedEntities = null;


            //has anyone left the area who was in it previously
            for (int i = m_entities.Count - 1; i >= 0; i--)
            {
                //check all combat entities
                CombatEntity currentEntity = m_entities[i];
                //check each against the collision
                bool stillInArea = m_collisionObject.CheckPositionIntersection(currentEntity.CurrentPosition.m_position);
                
                //if they are nolonger within the collision
                if (stillInArea == false)
                {
                    //add them to the list of people that have been removed
                    if (removedEntities == null)
                    {
                        removedEntities = new List<CombatEntity>();
                    }
                    removedEntities.Add(currentEntity);
                    //remove them from this list
                    m_entities.Remove(currentEntity);
                }

            }



            //get everyone within the area
            //get all within the bounding circle
            List<CombatEntity> entitiesWithinCloseRange = new List<CombatEntity>();

            m_zone.PartitionHolder.AddEntitiesInRangeToList(null, m_collisionObject.m_bounding_Sphere.m_centre, m_collisionObject.m_bounding_Sphere.m_radius, entitiesWithinCloseRange,  ZonePartition.ENTITY_TYPE.ET_PLAYER | ZonePartition.ENTITY_TYPE.ET_MOB, null);
            
            //check each one to see if it is already known
            for (int i = entitiesWithinCloseRange.Count - 1; i >= 0; i++)
            {
                CombatEntity currentEntity = entitiesWithinCloseRange[i];

                //if they are already known then they have already passed the collision
                if (m_entities.Contains(currentEntity) == false)
                {
                    //if not then check the collision
                    bool inArea = m_collisionObject.CheckPositionIntersection(currentEntity.CurrentPosition.m_position);
                   
                    
                    //if collision passes, add them to a list of new people
                    if (inArea == true)
                    {
                        if (addedEntities == null)
                        {
                            addedEntities = new List<CombatEntity>();
                        }
                        addedEntities.Add(currentEntity);
                        //add them to the known list
                        m_entities.Add(currentEntity);

                    }
                }
            }

            if (removedEntities != null)
            {
                //notify all those that have been removed
                for (int i = removedEntities.Count - 1; i >= 0; i--)
                {
                    CombatEntity currentEntity = removedEntities[i];
                    currentEntity.EffectAreaNowOutOfRange(this);
                }
            }
            //notify all those that have been added
            if (addedEntities != null)
            {
                //notify all those that have been removed
                for (int i = addedEntities.Count - 1; i >= 0; i--)
                {
                    CombatEntity currentEntity = addedEntities[i];
                    currentEntity.EffectAreaNowInRange(this);
                }
            }
        }
        void CheckPartitions()
        {
            //A list of partitions that this area is no longer part of
            List<ZonePartition> removedPartitions = null;
            //A list of partitions that the area is now a part of
            List<ZonePartition> addedPartitions = null;

            //get a list of all partitions in range
            List<ZonePartition> partitionsInRange = new List<ZonePartition>();
            
            Vector2 pos2D = new Vector2(m_collisionObject.m_bounding_Sphere.m_centre.X, m_collisionObject.m_bounding_Sphere.m_centre.Z);
            m_zone.PartitionHolder.AddPartitionsInRangeToList(pos2D, m_collisionObject.m_bounding_Sphere.m_radius, partitionsInRange);
            
            //for all the current partitions
            for (int i = m_partitions.Count - 1; i >= 0; i--)
            {
                ZonePartition currentPartition = m_partitions[i];
                //is it contained in the new list
                if (partitionsInRange.Contains(currentPartition) == false)
                {
                    //if not chuck it compleatly
                    m_partitions.Remove(currentPartition);
                    //add it to the removed list
                    if (removedPartitions == null)
                    {
                        removedPartitions = new List<ZonePartition>();
                    }
                    removedPartitions.Add(currentPartition);
                }
                else
                {
                    //if it is remove it from the possible new partitions
                    partitionsInRange.Remove(currentPartition);
                    //then recheck a more exact collision

                    //if it's not within the collision add it to the removed list
                    //remove it from the known list

                    //else if it is within the collision leave it be
                }
            }


            //for all remaining possible new partitions
            for (int i = partitionsInRange.Count - 1; i >= 0; i--)
            {
                ZonePartition currentPartition = partitionsInRange[i];
                //check against the collision 
                bool passesCollisionCheck = true; //currently not checking
                //if it passes add it to the list of added partitions, add it to the known list
                if (passesCollisionCheck == true)
                {
                    m_partitions.Add(currentPartition);
                    if (addedPartitions == null)
                    {
                        addedPartitions = new List<ZonePartition>();
                    }
                    addedPartitions.Add(currentPartition);
                }
            }
            //notify all partitions that have been removed
            if (removedPartitions != null)
            {
                for (int i = removedPartitions.Count - 1; i >= 0; i--)
                {
                    ZonePartition currentPartition = removedPartitions[i];
                    currentPartition.EffectAreaEnteringPartition(this);
                }
            }
            //notify all partitions that have been added
            if (addedPartitions != null)
            {
                for (int i = addedPartitions.Count - 1; i >= 0; i--)
                {
                    ZonePartition currentPartition = addedPartitions[i];
                    currentPartition.EffectAreaEnteringPartition(this);
                }
            }
        }
        internal bool PositionIsInArea(Vector3 position)
        {
            return m_collisionObject.CheckPositionIntersection(position);
        }
        //read yourself from the database query




        internal void LoadEffectsFromDatabase()
        {

             SqlQuery query = new SqlQuery(Program.processor.m_dataDB, "select * from zone_effect_area_effects where zone_effect_area_id =" + m_effectAreaID);
             if (query.HasRows)
             {
                 //int numberOfMobs = query.
                 while ((query.Read()))
                 {
                     AreaConditionalEffect newEffect = new AreaConditionalEffect(query,this);
                     if (newEffect.PreEffect == true)
                     {
                         m_preEffects.Add(newEffect);
                     }
                     else
                     {
                         m_postEffects.Add(newEffect);
                     }
                 }
             }
             query.Close();
        }

        internal void EntityLeavingArea(CombatEntity entity)
        {
            
            if (entity != null && m_entities.Contains(entity) == true)
            {
                m_entities.Remove(entity);

            }
            
        }
        internal void EntityEnteringArea(CombatEntity entity)
        {

            if (entity != null && m_entities.Contains(entity) == false)
                {
                    m_entities.Add(entity);
                    
                }
        }
        internal void AddPreEffectsToList(List<AreaConditionalEffect> theEffects)
        {
            theEffects.AddRange(m_preEffects);
        }
        internal void AddPostEffectsToList(List<AreaConditionalEffect> theEffects)
        {
            theEffects.AddRange(m_postEffects);
        }
        //does the entered position lie within your range
    }
}
