using System;
using System.Configuration;
using System.Collections.Generic;
using System.Text;
using Lidgren.Network;
using MainServer.partitioning;
using XnaGeometry;
using MainServer.Localise;

namespace MainServer.Combat
{
    class CombatManager
    {
        #region localisation
        public class CombatManagerTextDB : TextEnumDB
		{
			public CombatManagerTextDB() : base(nameof(CombatManager), typeof(TextID)) { }

			public enum TextID
			{
				OTHER_RESISTED_ATTACK,					// "{name0} resisted the attack."
				PLAYER_BEING_TARGETED,					// "You are currently being targeted."
				TARGET_HEALTH_TOO_HIGH,					// "The target's health is too high."
				TARGET_NOT_DEAD,						// "The target is not dead."
				TARGET_ENERGY_FULL,						// "The target's energy is full."
				TARGET_DEAD,							// "The target is dead."
				TARGET_LEVEL_TOO_HIGH,					// "The target's level is too high."
				PLAYER_CANNOT_CAST_TARGET,				// "You cannot cast on that target."
				PLAYER_UNABLE_TO_CAST,					// "You are currently unable to cast skills."
				SKILL_NOT_READY,						// "{skillName0} is not ready."
				OTHER_START_CASTING,					// "{name0} started casting {skillName1}."
				PLAYER_CANNOT_REACH_TARGET,				// "You cannot reach the target!"
				PLAYER_FUMBLED_NEED_TRAINING,			// "You fumbled {skillName0}. You need to train in '{abilityName1}' ability."
				PLAYER_FUMBLED_KEEP_PRACTICING,			// "You fumbled {skillName0}. Keep practicing your '{abilityName1}' ability."
				SKILL_SUCCEEDED,						// "{skillName0} Succeeded"
				OTHER_CAST_SKILL_ON_PLAYER,				// "{name0} cast {skillName1} on you"
				OTHER_RESISTED_SKILL,					// "{name0} resisted {skillName0}."
				PLAYER_WITHTAND_SKILL,					// "You withstand {skillName0}"
				PLAYER_DEFLECT_SKILL,					// "You deflect {skillName0}"
				PLAYER_AVOID_SKILL,						// "You avoid {skillName0}"
				PLAYER_SHRUG_OFF_SKILL,					// "You shrug off {skillName0}"
				PLAYER_ENDURE_SKILL,					// "You endure {skillName0}"
				PLAYER_IGNORE_SKILL,					// "You ignore {skillName0}"
				PLAYER_RESIST_SKILL,					// "You resist {skillName0}"
				OTHER_WITHSTANDS_SKILL,					// "{name0} withstands {skillName1}"
				OTHER_DEFLECTS_SKILL,					// "{name0} deflects {skillName1}"
				OTHER_AVOIDS_SKILL,						// "{name0} avoids {skillName1}"
				OTHER_SHRUGS_OFF_SKILL,					// "{name0} shrugs off {skillName1}"
				OTHER_ENDURES_SKILL,					// "{name0} endures {skillName1}"
				OTHER_IGNORES_SKILL,					// "{name0} ignores {skillName1}"
				OTHER_RESISTS_SKILL,					// "{name0} resists {skillName1}"
				INFLICTED_SKILL_ON_OTHER,				// "Inflicted {skillName0} on {name1}"
				APPLIED_SKILL_ON_OTHER,					// "Applied {skillName0} on {name1}"
				OTHER_INFLICT_SKILL_PLAYER_RESISTED,	// "{name0} tried to inflict {skillName1} on you, but you resisted"
				OTHER_LANDS_CRITICAL_DAMAGE,			// "{name0} lands a critical hit for {damage1} damage!"
				OTHER_LANDS_CTITICAL_ABILITY_DAMAGE,	// "{name0} lands a critical {abilityName1} for {damage2} damage!"
				OTHER_LANDS_CTITICAL_ABILITY_HEALING	// "{name0} lands a critical {abilityName1} for {damage2} healing!"
			}
		}
		public static CombatManagerTextDB textDB = new CombatManagerTextDB();

        #endregion

        #region constants
        internal static int HostileCutoffOpinion = 50;
        const int BASE_ATTACK_DAMAGE = 0;
        const int ABILITY_FUDGE_FACTOR = 5;
        static float m_rangeLeeway = 1.5f;
        static bool COMBAT_TIMING_DEBUGGING = false;
        internal const bool REPORT_MOB_SKILLS = true;
        #endregion //constants

        #region enums
        internal enum ATTACK_TYPE
        {
            NONE = -1,
            ATTACK = 0,
            SKILL = 1,
            STATUS_EFFECT = 2,
            ATTACK_TRIGGERED_SKILL = 3,
            AOE_SKILL = 4
        };
        public enum DamageMessageType
        {
            PlayerToMob = 0,
            MobToPlayer = 1,
            MobToMob = 2,
            PlayerToPlayer = 3,
            PlayerToNode = 4,
            NodeToPlayer = 5
        };
        internal enum COMBAT_REACTION_TYPES
        {
            CRT_HIT = 0,
            CRT_MISS = 1,
            CRT_DODGE = 2,
            CRT_BLOCK = 3,
            CRT_DODGE2 = 4,
            CRT_BLOCK2 = 5,
            CRT_PARRY = 6,
            /// <summary>
            /// An aggresive skill hit
            /// </summary>
            CRT_SKILL_HIT_AGG = 7,
            /// <summary>
            /// a positive skill hit
            /// </summary>
            CRT_SKILL_HIT_POS = 8,
            /// <summary>
            /// An aggresive status hit
            /// </summary>
            CRT_STATUS_HIT_AGG = 9,
            /// <summary>
            /// a positive status hit
            /// </summary>
            CRT_STATUS_HIT_POS = 10,
            /// <summary>
            /// a special reaction that will prevent any damage being shown
            /// </summary>
            CRT_NO_REACTION = 11,
            /// <summary>
            /// The entity avoided the skill
            /// </summary>
            CRT_SKILL_AVOIDED = 12,
            CRT_ZERO_DAMAGE = 13,
        };
        #endregion//enums

        #region variables

        List<CombatEntity> m_entitiesWithCancelledSkills = new List<CombatEntity>();

        List<CombatEntity> m_entitiesInCombat = null;
        List<CombatDamageMessageData> m_pendingDamage = new List<CombatDamageMessageData>();
        double m_timeAtLastFrame;
        /// <summary>
        /// the zone that the combat managet belongs to
        /// </summary>
        Zone m_containingZone;

        bool m_updateDpsAndHps = false;

        #endregion //variables
        
		#region Properties
        internal List<CombatEntity> EntitiesWithCancelledSkills
        {
            get { return m_entitiesWithCancelledSkills; }
        }
        internal Zone zone
        {
            get { return m_containingZone; }
        }
        #endregion //Properties
        
		#region Initialisation
        public CombatManager(Zone zone)
        {
            m_timeAtLastFrame = Program.MainUpdateLoopStartTime(); //NetTime.Now;
            m_entitiesInCombat = new List<CombatEntity>();
            m_containingZone = zone;

            if (ConfigurationManager.AppSettings["ShowDpsAndHps"] != null)
                m_updateDpsAndHps = bool.Parse(ConfigurationManager.AppSettings["ShowDpsAndHps"]);
        }
        ~CombatManager()
        {

        }
        #endregion
        
		public void Update()
        {
            double currentTime = Program.MainUpdateLoopStartTime();
            double timeSinceLastFrame = currentTime - m_timeAtLastFrame; //NetTime.Now - m_timeAtLastFrame;
            m_timeAtLastFrame = Program.MainUpdateLoopStartTime();// NetTime.Now;

            //for all enemies attacking
            int numEntities = m_entitiesInCombat.Count;
            for (int currentAttacker = numEntities - 1; currentAttacker >= 0; currentAttacker--)
            {
                CombatEntity currentCombatEntity = m_entitiesInCombat[currentAttacker];

                // if the attacker is dead they shouldn't be here
                if (currentCombatEntity.Dead)
                {
                    Program.Display("dead removed " + currentCombatEntity.Name + " [" + currentCombatEntity.ServerID + "]");
                    m_entitiesInCombat.Remove(currentCombatEntity);
                    continue;
                }
                //if an action is currently in progress
                if (currentCombatEntity.ActionInProgress)
                {
                    //decrease the timer
                    currentCombatEntity.TimeTillActionComplete -= timeSinceLastFrame;


                    CombatEntity currentTarget = null;
                    CombatEntity attackingEntity = null;

                    //if it's time to carry out the action
                    if (currentCombatEntity.TimeTillActionComplete <= 0)
                    {
                        if (COMBAT_TIMING_DEBUGGING == true)
                        {
                            Program.Display(currentCombatEntity.Name + " current Action Complete" + currentTime);
                        }
                        attackingEntity = currentCombatEntity;

                        //if it's a skill cast the skill
                        if ((currentCombatEntity.CurrentSkillTarget != null) && (currentCombatEntity.CurrentSkill != null))
                        {


                            currentTarget = currentCombatEntity.CurrentSkillTarget;
                            bool targetAlreadyDead = (currentTarget.CurrentHealth <= 0);

                            CastSkill(attackingEntity, currentTarget, attackingEntity.CurrentSkill, null);
                            if (!targetAlreadyDead && (currentTarget.CurrentHealth <= 0))
                            {
                                currentTarget.m_killer = attackingEntity;
                            }

                            attackingEntity.EndCasting();
                        }
                        // other wise attack
                        else if (currentCombatEntity.AttackTarget != null)
                        {
                            //finnish the action
                            attackingEntity.EndAttack();
                            attackingEntity.ActionInProgress = false;

                        }
                        //if neither of these then this should not be in this section
                        else
                        {
                            attackingEntity.ActionInProgress = false;
                        }
                        if (currentTarget != null)
                        {
                            bool stillHostile = attackingEntity.EntityIsHostileTowards(currentTarget);
                            if (!stillHostile)
                            {
                                //remove this mob from the hostile List
                                currentTarget.RemoveFromHostileEntities(attackingEntity);
                            }
                        }
                    }

                    // if an attack Target is Dead then stop attacking them
                    if (currentCombatEntity.AttackTarget != null)
                    {
                        if (currentCombatEntity.AttackTarget.Dead)
                        {                            
                            StopAttacking(currentCombatEntity);
                        }
                    }
                }
                //if no action in progress
                else
                {
					//this is auto attacking happening
                    if (currentCombatEntity.TimeActionWillComplete < currentTime)
                    {

                        if (COMBAT_TIMING_DEBUGGING == true)
                        {
                            Program.Display(currentCombatEntity.Name + " ready For next action " + currentTime);
                        }
                        //if there is a skill in the queue
                        if ((currentCombatEntity.NextSkill != null) && (currentCombatEntity.NextSkillTarget != null) && (currentCombatEntity.StatusPreventsActions.Skills == false))
                        {
                            //try to carry out the next skill

                            StartNextSkill(currentCombatEntity, currentCombatEntity.NextSkillTarget);
                        }
                        //if there is no next skill
                        else if (currentCombatEntity.AttackTarget != null)
                        {
                            // FISHING
                            // Switch to use compiled attack speed in normal combat and base attack speed (weapon) for fishing combat
                            // This is currently hacked on the client (CombatManager.cs / StartAttack() / line: 548 / to: 2500)
                            bool isFishAttack = CheckFishingAttack(currentCombatEntity, currentCombatEntity.AttackTarget);

                            double attackTime = (isFishAttack ? (int)currentCombatEntity.GetBaseAttackSpeed : currentCombatEntity.AttackSpeed) / 1000.0f;
                            if (((currentCombatEntity.TimeAtLastAttack + attackTime) <= currentTime) && (currentCombatEntity.CurrentPosition.m_currentSpeed <= 0))
                            {
                                bool attackCancelled = false;

                                // handle error/hacking cases where player is attacking fish with a non-rod weapon
                                if (isFishAttack && currentCombatEntity.Type == CombatEntity.EntityType.Player)
                                {
                                    Item weapon = ((Character)currentCombatEntity).m_inventory.GetEquipmentForSlot((int)Inventory.EQUIP_SLOT.SLOT_WEAPON);
                                    bool rodEquipped = (weapon != null && weapon.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.FISHING_ROD);

                                    if (rodEquipped == false)
                                    {
                                        attackCancelled = true;

                                        // we have something equipped but it isn't a fishingrod
                                        if (weapon != null) 
                                            Program.Display("Error - fishing without rod, potential client modification. " + currentCombatEntity.GetIDString());
                                    }
                                }
                                
                                // if we didn't cancel for equip reasons, and the target is still an enemy then start the next attack
                                if (attackCancelled == false && currentCombatEntity.IsEnemyOf(currentCombatEntity.AttackTarget) == true && currentCombatEntity.AttackTarget.EntityCanBeAffectedByAttack(currentCombatEntity))
                                {
                                    if (currentCombatEntity.StatusPreventsActions.Attack == false)
                                    {
                                        StartAttack(currentCombatEntity, currentCombatEntity.AttackTarget);
                                    }
                                }
                                else //otherwise stop attacking
                                {
                                    StopAttacking(currentCombatEntity);
                                }
                            }
                        }
                        //if they have nothing to do then remove them
                        else
                        {
                            if (currentCombatEntity.NextSkillTarget == null && currentCombatEntity.AttackTarget == null)
                            {
                                if (Program.m_LogSysBattle)
                                {
                                    Program.Display("idle removed " + currentCombatEntity.ServerID);
                                }

                                m_entitiesInCombat.Remove(currentCombatEntity);
                            }
                        }
                    }
                    
                }

            }

            PlayDamages();

            if (m_updateDpsAndHps == true)
            {
                m_containingZone.UpdatePlayerDamageAndHealing(timeSinceLastFrame);
            }
            m_containingZone.SendCombatUpdate();
            m_containingZone.SendSkillInterruptedMessage();
        }
        
		/// <summary>
        /// removes the data from the pending damages
        /// </summary>
        /// <param name="oldData"></param>
        /// <param name="sendWithoutFind">will send even if the damage was not in the queue(for premptive damages)</param>
        internal void RemoveDamageFromList(CombatDamageMessageData oldData, bool sendWithoutFind)
        {			
            bool removed = m_pendingDamage.Remove(oldData);
            if (removed|| sendWithoutFind==true)
            {
                m_containingZone.QueuedDamageCancelled(oldData);
            }
        }
        
		internal void AddToPendingDamage(CombatDamageMessageData newData)
		{
			
            //if the data does not exist then don't bother
            if (newData == null)
            {
                return;
            }
						
            double timeOfReport = newData.ApplyTime;
            bool damageAdded = false;
            //find the correct position for the data
            for (int i = 0; i < m_pendingDamage.Count && damageAdded == false; i++)
            {
                //and then add it
                CombatDamageMessageData currentDamage = m_pendingDamage[i];
                //if the current damage is to be played after the new damage
                //insert the new damage before the current damage
                if (currentDamage.ApplyTime > timeOfReport)
                {
                    damageAdded = true;
                    m_pendingDamage.Insert(i, newData);
                }
            }
            //if it was not added it must be the latest in the list, add it to the end 
            if (damageAdded == false)
            {
                m_pendingDamage.Add(newData);
            }

        }

        /// <summary>
        /// if there is damage due to be processed
        /// process it
        /// </summary>
        void PlayDamages()
        {
			
            double currentTime = Program.MainUpdateLoopStartTime();
            bool damageUpToDate = false;
            try
            {				
				for (int i = 0; i < m_pendingDamage.Count && damageUpToDate == false; i++)
                {
                    //should the current damage be applied
                    CombatDamageMessageData currentDamage = m_pendingDamage[i];
					
                    if (currentDamage == null)
                    {
                        m_pendingDamage.RemoveAt(i);
                        i--;
                        continue;
                    }
                    else if (currentDamage.ApplyTime < currentTime)
                    {	                    
                        m_pendingDamage.RemoveAt(i);
                        i--;
                        if (currentDamage.TargetLink != null)
                        {
							//apply the damage to the target		
                            currentDamage.TargetLink.TakeDamage(currentDamage);
                        }
                        
                        continue;
                    }
                    else
                    {
                        damageUpToDate = true;
                    }
                }				
            }
            catch (Exception e)
            {
                Program.Display("exception in play damages loop : " + e.Message + ": " + e.StackTrace);
            }

        }
        
		/// <summary>
        /// removes any damage on or by the entity in question
        /// </summary>
        /// <param name="entityToRemove"></param>
        internal void RemoveDamageForEntity(CombatEntity entityToRemove)
		{			
            for (int i = m_pendingDamage.Count - 1; i >= 0; i--)
            {
                CombatDamageMessageData currentDamage = m_pendingDamage[i];

                if (currentDamage.TargetLink == entityToRemove || currentDamage.CasterLink == entityToRemove)
                {
                    m_pendingDamage.RemoveAt(i);
                    m_containingZone.QueuedDamageCancelled(currentDamage);
                    continue;
                }
            }

        }

        internal void UpdateEntitiesDueToEntityDeath(CombatEntity deadEntity)
        {
			
            float maxAssistRange = 30;

            List<CombatEntity> entitiesInRange = new List<CombatEntity>();

            if (deadEntity.CurrentPartition != null)
            {
                deadEntity.CurrentPartition.AddEntitiesInRangeToList(deadEntity, deadEntity.CurrentPosition.m_position, maxAssistRange, entitiesInRange, ZonePartition.ENTITY_TYPE.ET_ENEMY, null);
            }

            //for all enemies attacking
            int numEntities = entitiesInRange.Count;
            for (int currentAttacker = numEntities - 1; currentAttacker >= 0; currentAttacker--)
            {
                CombatEntity currentCombatEntity = entitiesInRange[currentAttacker];
                if (currentCombatEntity != null)
                {
                    currentCombatEntity.ClearAggroForEntity(deadEntity);
                }
            }
        }

        internal void EntityAssistedByEntity(CombatEntity targetedEnt, CombatEntity assistingEnt, float AggroOfAssist)
        {
            float maxAssistRange = 30;

            List<CombatEntity> entitiesInRange = new List<CombatEntity>();

            if (targetedEnt.CurrentPartition != null)
            {
                targetedEnt.CurrentPartition.AddEntitiesInRangeToList(targetedEnt, targetedEnt.CurrentPosition.m_position, maxAssistRange, entitiesInRange, ZonePartition.ENTITY_TYPE.ET_ENEMY, null);
            }
           
            //for all enemies attacking
            int numEntities = entitiesInRange.Count;
            for (int currentAttacker = numEntities - 1; currentAttacker >= 0; currentAttacker--)
            {
                CombatEntity currentCombatEntity = entitiesInRange[currentAttacker];
                if (currentCombatEntity != null)
                {
                    currentCombatEntity.EntityAidedByEntity(targetedEnt, assistingEnt, AggroOfAssist);
                }
            }
        }
        
        internal void AddAggroToEntity(CombatEntity alterEnt, int aggroAlter)
        {
			
            //for all enemies attacking
            int numEntities = m_entitiesInCombat.Count;
            for (int currentAttacker = numEntities - 1; currentAttacker >= 0; currentAttacker--)
            {
                CombatEntity currentCombatEntity = m_entitiesInCombat[currentAttacker];
                if (currentCombatEntity != null)
                {
                    //currentCombatEntity.EntityAidedByEntity(targetedEnt, assistingEnt, AggroOfAssist);
                    currentCombatEntity.AddToAggroValueToExistingData(alterEnt, aggroAlter);
                }
            }
        }
        
		#region Attacking


        /// <summary>
        /// Determines if the passed entity is already under attack by another entity in combat.
        /// Intended for sparse usage.
        /// </summary>
        /// <param name="in_targetEntity">Fish gathering type combat entity</param>
        /// <returns>If entity is already under attack</returns>
        public bool IsFishingTargetAlreadyUnderAttack(CombatEntity in_targetEntity)
        {
            // fail states, act as if their target is claimed
            if (in_targetEntity == null || in_targetEntity.Gathering != CombatEntity.LevelType.fish)
                return true;

            for (int i = 0; i < m_entitiesInCombat.Count; ++i)
            {
                CombatEntity nextEnt = m_entitiesInCombat[i];

                if (nextEnt == null)
                    continue;

                if (nextEnt.AttackTarget == in_targetEntity)
                    return true;
            }

            return false;
        }


        public void StartAttackingEntity(CombatEntity attackingEntity, CombatEntity targetEntity)
        {
			
            if (attackingEntity.Dead || targetEntity.Dead)
            {
                return;
            }

            CombatEntity previousTarget = attackingEntity.AttackTarget;
            bool alreadyInCombat = m_entitiesInCombat.Contains(attackingEntity);

            // FISHING - check if we have attempted to attack a different gathering type - if so take the previous target out of combat
            if ((alreadyInCombat == true) && (previousTarget != targetEntity) && (targetEntity.Gathering != CombatEntity.LevelType.none))
            {
                if (previousTarget is ServerControlledEntity)
                {
                    ((ServerControlledEntity)previousTarget).Return();
                }
            }

            //was the entity already attacking this mob
            //and is the time since last attack great enough
            double currentTime = Program.MainUpdateLoopStartTime();

            // FISHING
            // Base attack speed for fishing
            //double attackTime = (CheckFishingAttack(attackingEntity, targetEntity) ? (int)attackingEntity.GetBaseAttackSpeed : attackingEntity.AttackSpeed) / 1000.0f;

            attackingEntity.AttackTarget = targetEntity;
            if (attackingEntity.CurrentSkill == null)
            {
                attackingEntity.TimeTillActionComplete = 0;
                attackingEntity.ActionInProgress = false;
            }
            //attackingEntity.TimeTillActionComplete = 1;
            //add this to the entity in combat list
            
            if (alreadyInCombat == false)
            {				
                m_entitiesInCombat.Add(attackingEntity);
            }
           
            //only send out if they were not originally attacking the same target
            if (previousTarget == targetEntity && alreadyInCombat == true)
            {
                return;
            }
            //send info to everyone near that you started attacking
            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.AttackState);
            outmsg.Write((byte)1);//start attacking
            outmsg.Write((byte)attackingEntity.Type);
            outmsg.WriteVariableInt32(attackingEntity.ServerID);
            outmsg.Write((byte)targetEntity.Type);
            outmsg.WriteVariableInt32(targetEntity.ServerID);

            List<NetConnection> connections = m_containingZone.getUpdateList(null);
            Program.processor.SendMessage(outmsg, connections, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.AttackState);
            
            attackingEntity.m_AttackTargetCombatRecord = new CombatRecord();
        }
        
		public void StopAttacking(CombatEntity attackingEntity)
        {
            
            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.AttackState);
            outmsg.Write((byte)0);//stopping attacking
            outmsg.Write((byte)attackingEntity.Type);
            outmsg.WriteVariableInt32(attackingEntity.ServerID);
            if (attackingEntity.AttackTarget != null)
            {
                outmsg.Write((byte)attackingEntity.AttackTarget.Type);
                outmsg.WriteVariableInt32(attackingEntity.AttackTarget.ServerID);
            }
            else
            {
                outmsg.Write((byte)0);
                outmsg.WriteVariableInt32(-1);

            }

            attackingEntity.m_AttackTargetCombatRecord = null;
            //end a current attack
            attackingEntity.CancelCurrentAttack();
            Player playerToExclude = null;

           

            List<NetConnection> connections = m_containingZone.getUpdateList(playerToExclude);
            Program.processor.SendMessage(outmsg, connections, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.AttackState);

			//#FISH fishing check here
			//if the player moves or is otherwise intterrupted, stopattacking is called.
			//here we check if we have an attack target that's a fish (or other gatherer) and that we are a player
			//and from that we use the ConcentrationZero() method which resets us and the target to exit combat
			//within concentration zero, it will additionally call StopAttacking, so we need to provide an extra bool
			//to eliminate an infinite recursion.
			if (attackingEntity.AttackTarget != null && attackingEntity.AttackTarget.Gathering == CombatEntity.LevelType.fish && attackingEntity.AttackTarget.CurrentHealth > 0)
			{
				if (attackingEntity is Character)
				{					
					attackingEntity.ConcentrationZero(false);					
					((Character)attackingEntity).WriteMessageForConcentrationBroken();
				}
			}
            attackingEntity.AttackTarget = null;
        }       		
        
		bool StartAttack(CombatEntity attackingEntity, CombatEntity targetEntity)
        {
			
            //check the target is in range
            float attackRange = attackingEntity.Radius + attackingEntity.CompiledStats.MaxAttackRange + targetEntity.Radius;
            if (attackingEntity.Type == CombatEntity.EntityType.Player)
            {
                attackRange += m_rangeLeeway;
            }
            Vector3 targetPos = targetEntity.GetCombatLocation(attackingEntity);
            float distanceToTarget = Utilities.Difference2D(attackingEntity.CurrentPosition.m_position, targetPos);

            //if in range start attacking
            if ((distanceToTarget > attackRange) && (distanceToTarget > 0))
            {
                if (attackingEntity.Type == CombatEntity.EntityType.Mob)
                {
                    //Program.Display("mob attack - max damage = " + maxdamage + " max defence = " + maxdefence + " attack speed =" + attackingEntity.AttackSpeed);
                    //Program.Display("mob attack succeded, attack = " + attackingEntity.Attack + " target defence =" + targetEntity.Defence + " random chance = " + result + "** damage = " + damage);
                }
                else
                {
                    //Program.Display("player out of range distanceToTarget= " + distanceToTarget + " attackRange = " + attackRange);

                    // Program.Display("player attack failed, attack = " + attackingEntity.Attack + "| target defence =" + targetEntity.Defence + "| random chance = " + result);
                }
                return false;
            }
            if (COMBAT_TIMING_DEBUGGING == true)
            {
                Program.Display(attackingEntity.Name + " attack started " + Program.MainUpdateLoopStartTime());
            }


            attackingEntity.CurrentPosition.m_currentSpeed = 0;
            //set the time until the attack completes

            // FISHING
            // Fishing uses base attack speed - this is currently hacked on the client (CombatManager.cs / StartAttack() / line: 548 / to: 2500)
            attackingEntity.TimeTillActionComplete = (CheckFishingAttack(attackingEntity, targetEntity) ? (int)attackingEntity.GetBaseAttackSpeed : (float)attackingEntity.AttackSpeed) / 1000.0f;

            attackingEntity.TimeTillActionComplete += Inventory.ATTACK_TIME_ADD;
            //start the action
            attackingEntity.ActionInProgress = true;

            //do the attack damage now
            if (targetEntity != null)
            {
                bool targetAlreadyDead = (targetEntity.CurrentHealth <= 0);

                CarryOutAttack(attackingEntity, targetEntity);
                //attackDamageMessageData.Add(damageData);
                if (!targetAlreadyDead && (targetEntity.CurrentHealth <= 0))
                {
                    targetEntity.m_killer = attackingEntity;
                }
                if (attackingEntity.StatusCancelConditions.Attack==true)
                {
                    attackingEntity.CancelEffectsDueToAttack();
                }
            }

            return true;
        }
        
		CombatDamageMessageData CarryOutAttack(CombatEntity attackingEntity, CombatEntity targetEntity)
        {
			
            //set the time of the last completed attack
            attackingEntity.TimeAtLastAttack = Program.MainUpdateLoopStartTime();
            //do the damage
            return DoAttackDamage(attackingEntity, targetEntity);
        }
        
		CalculatedDamage CalculateAttackDamage(CombatEntity attackingEntity, CombatEntity targetEntity)
        {
            int damage = 0;
            int originalDamage = 0;                      
           
            List<FloatForID> combinedDamageTypes = attackingEntity.CompiledStats.CombinedDamageType;
            for (int i = 0; i < combinedDamageTypes.Count; i++)
            {
                int currentType = combinedDamageTypes[i].m_bonusType;
                if (attackingEntity.GetCombinedDamageType(currentType) > 0)
                {

                    //work out the damage
                    int maxdamage = (int)attackingEntity.GetCombinedDamageType(currentType);
                    int maxdefence = targetEntity.GetBonusType(currentType);//targetEntity.m_bonusTypes[i];

                    bool pvp = (targetEntity.Type == CombatEntity.EntityType.Player && targetEntity.IsPVP());
                    if (pvp)
                        maxdamage = (int)Math.Ceiling(maxdamage * Character.m_pvpMeleeMult);

                    CalculatedDamage calcDamage = DamageCalculator.CalculateDamage(false, true, maxdamage, maxdefence, attackingEntity, targetEntity);
                    targetEntity.AttemptToReduceDamage(currentType, calcDamage);
                    int newdamage = calcDamage.m_calculatedDamage;

                    if (newdamage > 0)
                    {
                        damage += newdamage;
                    }
                    if (calcDamage.m_preLvlReductionDamage > 0)
                    {
                        originalDamage += calcDamage.m_preLvlReductionDamage;
                    }
                }
            }
            CalculatedDamage damageDone = new CalculatedDamage(damage, originalDamage);
            return damageDone;
        }

        CombatDamageMessageData DoAttackDamage(CombatEntity attackingEntity, CombatEntity targetEntity)
        {
			
            //set the attacking entity to hostile if it is a hostile skill
            if (attackingEntity.IsEnemyOf(targetEntity) == true)//attackingEntity.Type != targetEntity.Type) pvp change 27/10/11
            {
                //attackingEntity.InCombat = true;
                attackingEntity.ConductedHotileAction();
            }
            
            int damage = 0;
            int originalDamage = 0;
          
            bool didHit = false;
            int reaction = (int)COMBAT_REACTION_TYPES.CRT_MISS;
            bool resistedAllAttacks = true;
            bool attackHit = false;
            
			//did it hit
            int hitProbabilitySum = attackingEntity.Attack + targetEntity.Defence;
	        
			//gathering hacks here
			//for #FISH gathering, want a defence level of zero if the player is being attackign by a fish
	        if (CheckFishingDefence(attackingEntity, targetEntity) == true)
	        {
		        hitProbabilitySum = attackingEntity.Attack; //disregard defence				
	        }	        
			
			//use in the dice roll in a second
	        int tempAttack = attackingEntity.Attack;
			
			//same for attack but in reverse
	        if (CheckFishingAttack(attackingEntity, targetEntity) == true)
	        {
		        int fishAttack = ((Character) attackingEntity).GetFishingAttack();
				//Program.Display("using a simpler attack calculation, fishAttack." + fishAttack + " insteadOf." + attackingEntity.Attack);
		        hitProbabilitySum = fishAttack + targetEntity.Defence;
		        tempAttack = fishAttack;
	        }

			
	        int result = Program.getRandomNumber(hitProbabilitySum);
            
			//which attack value to use? 			
			//if (result < attackingEntity.Attack || result == 0)
			if (result < tempAttack || result == 0)
			{
				attackHit = true;
			}

            bool damageReduced = false;
            List<FloatForID> combinedDamageTypes = attackingEntity.CompiledStats.CombinedDamageType;
            for (int i = 0; i < combinedDamageTypes.Count; i++)
            {
                int currentType = combinedDamageTypes[i].m_bonusType;
                if (attackingEntity.GetCombinedDamageType(currentType) > 0)
                {

                    resistedAllAttacks = false;
                    //did it hit
                    //int hitProbabilitySum = attackingEntity.Attack + targetEntity.Defence;
                    //int result = Program.getRandomNumber(hitProbabilitySum);
                    if (Program.m_LogDamage)
                    {
                        Program.Display(" ");
                        Program.Display(attackingEntity.Name + " prob sum=" + hitProbabilitySum + " result=" + result + " attack=" + attackingEntity.Attack + " hit=" + attackHit);
                    }
                    if (attackHit == true)
                    {
                        didHit = true;
                        reaction = (int)COMBAT_REACTION_TYPES.CRT_HIT;
                 
                        //work out the damage
                        int maxdamage = (int)attackingEntity.GetCombinedDamageType(currentType);
                        int maxdefence = targetEntity.GetBonusType(currentType);

                        bool pvp = (targetEntity.Type == CombatEntity.EntityType.Player && targetEntity.IsPVP());
                        if (pvp)
                            maxdamage = (int)Math.Ceiling(maxdamage * Character.m_pvpMeleeMult);

                        CalculatedDamage calcDamage = DamageCalculator.CalculateDamage(false, true, maxdamage, maxdefence, attackingEntity, targetEntity);
                        int initialDamage = calcDamage.m_preLvlReductionDamage;
                        targetEntity.AttemptToReduceDamage(currentType, calcDamage);
                        if (calcDamage.m_preLvlReductionDamage < initialDamage)
                        {
                            damageReduced = true;
                        }

                        int newdamage = calcDamage.m_calculatedDamage;
          
                        if (newdamage > 0)
                        {
                            damage += newdamage;
                        }
                        if (calcDamage.m_preLvlReductionDamage > 0)
                        {
                            originalDamage += calcDamage.m_preLvlReductionDamage;
                        }
                       

                    }
                    
                }
            }
            
            if (damage <= 0)
            {
                if (damageReduced == false)
                {
                    reaction = Program.getRandomNumber(3) + 1;
                    switch (reaction)
                    {
                        case (int)COMBAT_REACTION_TYPES.CRT_BLOCK:
                            {
                                if (targetEntity.BlocksAttacks)
                                {
                                    if (Program.getRandomNumber(100) > 50)
                                    {
                                        reaction = (int)COMBAT_REACTION_TYPES.CRT_BLOCK;
                                    }
                                    else
                                    {
                                        reaction = (int)COMBAT_REACTION_TYPES.CRT_BLOCK2;
                                    }
                                }
                                else
                                {
                                    reaction = (int)COMBAT_REACTION_TYPES.CRT_PARRY;
                                }
                                break;
                            }
                        case (int)COMBAT_REACTION_TYPES.CRT_DODGE:
                            {

                                if (Program.getRandomNumberFromZero(100) > 50)
                                {
                                    reaction = (int)COMBAT_REACTION_TYPES.CRT_DODGE2;
                                }
                                break;
                            }
                        default:
                            {
                                break;
                            }
                    }
                }
                else
                {
                    reaction = (int)COMBAT_REACTION_TYPES.CRT_NO_REACTION;
                }


            }

            #region Critical Strikes

            // Critical Strike 
            // Must intercept 'originalDamage' before being modified
            // Check that the CombatEntities are valid
            int       critical  = 0;
            Character character = null;
            if (attackingEntity != null && targetEntity != null)
            {
                // Attempt to get the CRITICAL_STRIKE skill and create a new integer flag for messaging
                CharacterAbility criticalStrike = attackingEntity.getAbilityById(ABILITY_TYPE.CRITICAL_STRIKE);

                // Get the players character
                if (attackingEntity is Character)
                {
                    character = (Character)attackingEntity;
                }

                // Only proceed if: target does not have a gathering type (regular combat only), the attacker has the ability and damage has been done
                if ((targetEntity.Gathering == CombatEntity.LevelType.none) && (criticalStrike != null) && (damage != 0))
                {
                    // Create a suitable chance variable
                    float criticalStrikeLevel = attackingEntity.getAbilityLevel(criticalStrike.m_ability_id);
                    float baseAbility         = Program.processor.m_abilityVariables.CriticalStrikeBaseAbility;
                    float finalChance         = Program.processor.m_abilityVariables.CriticalStrikeMaxChance * 
                                                ((criticalStrikeLevel + baseAbility) / ((criticalStrikeLevel + baseAbility) + (10 * (targetEntity.Level + 3))));
                    finalChance              *= 100;
                    float criticalThreshold   = (float)(Program.getRandomDouble() * 100);

                    // Critical Strike!
                    if (criticalThreshold < finalChance)
                    {
                        float multiplier        = Program.processor.m_abilityVariables.CriticalStrikeMultiplier; // modifier
                        float damagefloatValue  = damage;                                                        // convert damage to a float
                        float damagefloatDamage = damagefloatValue * multiplier;                                 // multiply
                        damage                  = (int)Math.Round(damagefloatDamage, 0);                         // round 

                        float originalDamagefloatValue  = originalDamage;                                // convert damage to a float
                        float originalDamagefloatDamage = originalDamagefloatValue * multiplier;         // multiply
                        originalDamage                  = (int)Math.Round(originalDamagefloatDamage, 0); // round

                        critical = 1; // flag as crit for messaging

                        // Log the new critical damage
                        if (Program.m_LogDamage)
                        {
                            Program.Display("criticalStrike = " + damage);
                        }
                    }

                    // Chance of skilling up on any attack which results in damage - check nulls and casts
                    if (character != null && targetEntity != null)
                    {
                        // Check if its a npc that the no ability test flag is false
                        if(targetEntity is ServerControlledEntity )
                        {
                            if (!((ServerControlledEntity)targetEntity).Template.m_noAbilityTest)
                            {
                                character.testAbilityUpgrade(criticalStrike);
                            }
                        }
                        // Allow chance to skill up if target is a character
                        if(targetEntity is Character)
                        {
                            character.testAbilityUpgrade(criticalStrike);
                        }
                    }
                }
            }

            // Alter damage
            CalculatedDamage finalDamage   = new CalculatedDamage(damage, originalDamage);
            int              altereddamage = AlterDamageDueToEffects(targetEntity, damage, false);
            int              sentDamage    = finalDamage.GetAmendedOriginalDamage(altereddamage);

            // Send message to local players (using sentDamage)
            if (critical == 1 && character != null && sentDamage != 0)
            {
                string playerName    = string.Empty; // players anme
                string abilityName   = string.Empty; // ability name
                string messageString = string.Empty; // final message string

                // Get the players name
                playerName = character.m_player.m_activeCharacter.Name;

				int textID = (int)CombatManagerTextDB.TextID.OTHER_LANDS_CRITICAL_DAMAGE;
				attackingEntity.CurrentZone.SendLocalAbilityMessageLocalised(new LocaliseParams(textDB, textID, playerName, sentDamage), attackingEntity.CurrentPosition.m_position, Zone.LOCAL_MESSAGE_RANGE);
            }

            #endregion

            bool sendUpdateStats = false;
            if (altereddamage != damage && targetEntity.Type == CombatEntity.EntityType.Player)
            {
                sendUpdateStats = true;
            }
            //damage = altereddamage;
            if (attackingEntity.Type == CombatEntity.EntityType.Player)
            {
                if (targetEntity.Type != CombatEntity.EntityType.Mob || !((ServerControlledEntity)targetEntity).Template.m_noAbilityTest)
                {
                    ((Character)attackingEntity).updateWeaponAbility();
                }
                ((Character)attackingEntity).updateRanking(RankingsManager.RANKING_TYPE.LARGEST_MELEE_HIT, damage,false);
                if (resistedAllAttacks == true)
                {
                    //send a message to the player
                    if (attackingEntity.Type == CombatEntity.EntityType.Player)
                    {
                        Character attackingCharacter = (Character)attackingEntity;
                        if (attackingCharacter != null && attackingCharacter.m_player != null)
                        {
                            if (targetEntity != attackingEntity)
                            {
								string locText = Localiser.GetString(textDB, attackingCharacter.m_player, (int)CombatManagerTextDB.TextID.OTHER_RESISTED_ATTACK);
								locText = string.Format(locText, targetEntity.Name);
								Program.processor.sendSystemMessage(locText, attackingCharacter.m_player, false, SYSTEM_MESSAGE_TYPE.SKILLS);
                            }
                        }
                    }
                }
            }
            //workout message data
            int casterID = attackingEntity.ServerID;
            int targetID = targetEntity.ServerID;
            //what type of entity did the damage to what

            CombatDamageMessageData newDamage = targetEntity.TakeDamage(damage, altereddamage, attackingEntity, ATTACK_TYPE.ATTACK, 0, true, reaction, sentDamage, critical);
            double currentTime = Program.MainUpdateLoopStartTime();
            double timeTillApplication = 0;
            timeTillApplication = attackingEntity.ReportTime;

            // FISHING
            // Fishing uses base attack speed - this is currently hacked on the client (CombatManager.cs / StartAttack() / line: 548 / to: 2500)
            float attackSpeed = (CheckFishingAttack(attackingEntity, targetEntity) ? (int)attackingEntity.GetBaseAttackSpeed : attackingEntity.AttackSpeed) / 1000.0f;

            if (timeTillApplication > attackSpeed)
            {
                timeTillApplication = attackSpeed;
            }

            //we want the time at which the animation has finnished
            attackingEntity.TimeActionWillComplete = currentTime + timeTillApplication;

            timeTillApplication = timeTillApplication * attackingEntity.ReportProgress;
            newDamage.ActionCompleteTime = currentTime + timeTillApplication;
            if (attackingEntity.ProjectileSpeed > 0)
            {
                double distance = (targetEntity.CurrentPosition.m_position - attackingEntity.CurrentPosition.m_position).Length();
                if (distance > 0)
                {
                    timeTillApplication += distance / attackingEntity.ProjectileSpeed;
                }
            }

            newDamage.ApplyTime = currentTime + timeTillApplication;

            AddToPendingDamage(newDamage);
            attackingEntity.StartAttack(newDamage);



            if (attackingEntity.m_AttackTargetCombatRecord != null)
            {
                attackingEntity.m_AttackTargetCombatRecord.m_num_attacks++;
                attackingEntity.m_AttackTargetCombatRecord.m_total_AA_Damage += damage;
                if (!didHit)
                {
                    attackingEntity.m_AttackTargetCombatRecord.m_num_misses++;
                }
            }

            if (sendUpdateStats && targetEntity.Type == CombatEntity.EntityType.Player)
            {
                targetEntity.SetStatsChangeLevel(CombatEntity.STATS_CHANGE_LEVEL.BASIC_CHANGED);// ((Character)targetEntity).m_statsUpdated = true;
            }
            return newDamage;
        }
		
		#region gathering helpers

		/// <summary>
		/// If a fishing/gathering mob is attackign the player, want to set the defence as zero
		/// </summary>
		/// <param name="attackingEntity"></param>
		/// <param name="targetEntity"></param>
		/// <returns></returns>
	    private bool CheckFishingDefence(CombatEntity attackingEntity, CombatEntity targetEntity)
	    {
			//we need both of these, else it will be false
		    if (attackingEntity == null)
			    return false;
		    if (targetEntity == null)
			    return false;

			//target is character AND attacker is a fish/gathering type
			if (targetEntity is Character && attackingEntity.Gathering == CombatEntity.LevelType.fish)
		    {
			    return true;				
			}

			return false;
	    }

		/// <summary>
		/// If player is attackign a fishing/gathering mob, want to use a simpler attack value and disregard normal calculation
		/// </summary>
		/// <param name="attackingEntity"></param>
		/// <param name="targetEntity"></param>
		/// <returns></returns>
		internal bool CheckFishingAttack(CombatEntity attackingEntity, CombatEntity targetEntity)
		{
			//we need both of these, else it will be false
			if (attackingEntity == null)
				return false;
			if (targetEntity == null)
				return false;

			//target is character AND attacker is a fish/gathering type
			if (attackingEntity is Character && targetEntity.Gathering == CombatEntity.LevelType.fish)
			{
				return true;
			}

			return false;
		}

		#endregion

		#endregion

		#region Skills

		public void UseSkillOnEntity(EntitySkill entitySkill, CombatEntity castingEntity, CombatEntity targetEntity)
        {
			
            //do they have the skill
            if (entitySkill == null)
            {
                castingEntity.SkillFailedConditions();
                return;
            }

            if (castingEntity.Dead)
            {
                castingEntity.SkillFailedConditions();
                return;
            }

            castingEntity.CancelCurrentAttack();
            //set the skill target
            castingEntity.NextSkillTarget = targetEntity;

            //set the next skill
            castingEntity.NextSkill = entitySkill;



            if (m_entitiesInCombat.Contains(castingEntity) == false)
            {				
                m_entitiesInCombat.Add(castingEntity);
            }
        }

        internal static bool TargetInRange(CombatEntity attackingEntity, CombatEntity targetEntity, EntitySkill theSkill)
        {
            bool canBeDone = false;
            if (attackingEntity == null || targetEntity == null || theSkill == null)
            {
                return canBeDone;
            }
            float attackRange = targetEntity.Radius+ attackingEntity.Radius + theSkill.Template.Range + m_rangeLeeway;
            Vector3 targetPos = targetEntity.GetCombatLocation(attackingEntity);
            float distanceToTarget = Utilities.Difference2D(attackingEntity.CurrentPosition.m_position, targetPos);
            if (attackRange > distanceToTarget)
            {
                canBeDone = true;
            }
            if (theSkill.SkillID == SKILL_TYPE.LONG_SHOT)
            {
                float longShotRange = SkillTemplate.LONG_SHOT_RANGE;
                if (distanceToTarget < longShotRange)
                {
                    canBeDone = false;
                }
            }
            return canBeDone;
        }
        
        /// <summary>
        /// Starts the Entities Next Skill  
        /// </summary>
        /// <param name="attackingEntity"></param>
        /// <param name="targetEntity"></param>
        /// <returns></returns>
        bool StartNextSkill(CombatEntity attackingEntity, CombatEntity targetEntity)
        {
            //check the next skill is valid
            EntitySkill entitySkill = attackingEntity.NextSkill;
            SkillTemplate theSkill = attackingEntity.NextSkill.Template;
            bool pvp = (targetEntity.Type == CombatEntity.EntityType.Player && targetEntity.IsPVP());
            SkillTemplateLevel theSkillLevel = attackingEntity.NextSkill.getSkillTemplateLevel(pvp);

            if (attackingEntity.NextSkillTarget == null || theSkill == null)
            {
                return false;
            }
            //get the time since this was last cast
            double timeSinceLastCast = Program.MainUpdateLoopStartTime() - attackingEntity.TimeSinceSkillLastCast(theSkill.SkillID);//NetTime.Now - attackingEntity.TimeSinceSkillLastCast(theSkill.SkillID);

            bool canUseSkillOnTarget = targetEntity.EntityCanBeAffectedBySkill(attackingEntity);
            //can't do a sneaky attack on someone attacking you
            if (((theSkill.SkillID == SKILL_TYPE.SNEAKY_ATTACK) && (targetEntity.AttackTarget == attackingEntity)) ||
                ((theSkill.SkillID == SKILL_TYPE.ASSASSINATE) && (targetEntity.PercentHealth > 0.3)) ||
                ((theSkill.SkillID == SKILL_TYPE.REVIVE) && (targetEntity.Dead == false)) ||
                ((theSkill.SkillID == SKILL_TYPE.SACRIFICE) && (targetEntity.CurrentEnergy>=targetEntity.MaxEnergy)) ||
                ((targetEntity.Dead == true) && (theSkill.SkillID != SKILL_TYPE.REVIVE && targetEntity.Level > theSkillLevel.getUnModifiedAmount(entitySkill,pvp))) ||
                ((targetEntity.Dead == true) && (theSkill.SkillID == SKILL_TYPE.REVIVE && targetEntity.Level > entitySkill.ModifiedLevel * 5 + 15)) ||
                (theSkill.SkillID == SKILL_TYPE.RESCUE && targetEntity.Level > theSkillLevel.getUnModifiedAmount(entitySkill,pvp))||
                canUseSkillOnTarget == false ||
                attackingEntity.StatusPreventsActions.Skills == true)
            {
                if (attackingEntity.Type == CombatEntity.EntityType.Player)
                {
                    Character theCharacter = (Character)attackingEntity;
                    theCharacter.SendSkillUpdate((int)attackingEntity.NextSkill.Template.SkillID, attackingEntity.NextSkill.SkillLevel, 0);
                    if ((theSkill.SkillID == SKILL_TYPE.SNEAKY_ATTACK) && (targetEntity.AttackTarget == attackingEntity))
                    {
						string locText = Localiser.GetString(textDB, theCharacter.m_player, (int)CombatManagerTextDB.TextID.PLAYER_BEING_TARGETED);
						Program.processor.sendSystemMessage(locText, theCharacter.m_player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
                    }
                    else if ((theSkill.SkillID == SKILL_TYPE.ASSASSINATE) && (targetEntity.PercentHealth > 0.3))
                    {
						string locText = Localiser.GetString(textDB, theCharacter.m_player, (int)CombatManagerTextDB.TextID.TARGET_HEALTH_TOO_HIGH);
						Program.processor.sendSystemMessage(locText, theCharacter.m_player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
                    }
                    else if ((theSkill.SkillID == SKILL_TYPE.REVIVE) && (targetEntity.Dead == false))
                    {
						string locText = Localiser.GetString(textDB, theCharacter.m_player, (int)CombatManagerTextDB.TextID.TARGET_NOT_DEAD);
						Program.processor.sendSystemMessage(locText, theCharacter.m_player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
                    }
                    else if ((theSkill.SkillID == SKILL_TYPE.SACRIFICE) && (targetEntity.CurrentEnergy >= targetEntity.MaxEnergy))
                    {
						string locText = Localiser.GetString(textDB, theCharacter.m_player, (int)CombatManagerTextDB.TextID.TARGET_ENERGY_FULL);
						Program.processor.sendSystemMessage(locText, theCharacter.m_player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
                    
                    }
                    else if (targetEntity.Dead == true)
                    {
                        if (theSkill.SkillID != SKILL_TYPE.REVIVE)
                        {
							string locText = Localiser.GetString(textDB, theCharacter.m_player, (int)CombatManagerTextDB.TextID.TARGET_DEAD);
							Program.processor.sendSystemMessage(locText, theCharacter.m_player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
                        }
                        else if (targetEntity.Level > entitySkill.SkillLevel * 5 + 15) //oh yeah
                        {
							string locText = Localiser.GetString(textDB, theCharacter.m_player, (int)CombatManagerTextDB.TextID.TARGET_LEVEL_TOO_HIGH);
							Program.processor.sendSystemMessage(locText, theCharacter.m_player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
                        }
                    }
                    else if (theSkill.SkillID == SKILL_TYPE.RESCUE && targetEntity.Level > theSkillLevel.getUnModifiedAmount(entitySkill, pvp))
                    {
						string locText = Localiser.GetString(textDB, theCharacter.m_player, (int)CombatManagerTextDB.TextID.TARGET_LEVEL_TOO_HIGH);
						Program.processor.sendSystemMessage(locText, theCharacter.m_player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
                    }
                    else if (canUseSkillOnTarget == false)
                    {
						string locText = Localiser.GetString(textDB, theCharacter.m_player, (int)CombatManagerTextDB.TextID.PLAYER_CANNOT_CAST_TARGET);
						Program.processor.sendSystemMessage(locText, theCharacter.m_player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
                    }
                    else if (attackingEntity.StatusPreventsActions.Skills == true)
                    {
						string locText = Localiser.GetString(textDB, theCharacter.m_player, (int)CombatManagerTextDB.TextID.PLAYER_UNABLE_TO_CAST);
						Program.processor.sendSystemMessage(locText, theCharacter.m_player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
                    }
                    //Program.processor.sendSystemMessage("Invalid Target", theCharacter.m_player.connection, SYSTEM_MESSAGE_TYPE.BATTLE);
                }
                attackingEntity.SkillFailedConditions();
                //clear the next skill
                attackingEntity.NextSkillTarget = null;
                attackingEntity.NextSkill = null;
                return false;
            }
            float attackRange = attackingEntity.Radius + theSkill.Range + targetEntity.Radius + m_rangeLeeway;
            if (attackingEntity.Type == CombatEntity.EntityType.Player)
            {
                attackRange += m_rangeLeeway;
            }

            Vector3 targetPos = targetEntity.GetCombatLocation(attackingEntity);
            float distanceToTarget = Utilities.Difference2D(attackingEntity.CurrentPosition.m_position, targetPos);

            if (theSkillLevel == null)
            {
                attackingEntity.SkillFailedConditions();
                return false;
            }
            //if in range 
            //and if the skill has recharged
            //start the skill
            double rechargeTime = 0;
            if (theSkillLevel != null)
            {
                rechargeTime = theSkillLevel.GetRechargeTime(attackingEntity.NextSkill, false);
            }
            if ((distanceToTarget > attackRange) || (rechargeTime > timeSinceLastCast))
            {//otherwise cancel the skill
                
                if (attackingEntity.Type == CombatEntity.EntityType.Player)
                {
                    /* float timeRemaining = (float)timeSinceLastCast - theSkillLevel.RechargeTime;
                     if(timeRemaining<0)
                     { timeRemaining = 0; }*/
                    Character theCharacter = (Character)attackingEntity;
                    float timeRemaining = 0;
                    if (rechargeTime > timeSinceLastCast)
                    {
						string locText = Localiser.GetString(textDB, theCharacter.m_player, (int)CombatManagerTextDB.TextID.SKILL_NOT_READY);
						string skillName = SkillTemplateManager.GetLocaliseSkillName(theCharacter.m_player, theSkill.SkillID);
						locText = string.Format(locText, skillName);
						Program.processor.sendSystemMessage(locText, theCharacter.m_player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
                        timeRemaining = (float)(timeSinceLastCast - rechargeTime);
                    }
                    theCharacter.SendSkillUpdate((int)attackingEntity.NextSkill.Template.SkillID, attackingEntity.NextSkill.SkillLevel, timeRemaining);
                }
                //clear the next skill
                attackingEntity.SkillFailedConditions();
                attackingEntity.NextSkillTarget = null;
                attackingEntity.NextSkill = null;
                return false;

            }

            //set the attacking entity to hostile if it is a hostile skill
            if (attackingEntity.IsEnemyOf(targetEntity) == true||targetEntity.InCombat==true)//attackingEntity.Type != targetEntity.Type) pvp change 27/10/11
            {                
                attackingEntity.ConductedHotileAction();
            }
            if (COMBAT_TIMING_DEBUGGING == true)
            {
                Program.Display(attackingEntity.Name + " skill started " + Program.MainUpdateLoopStartTime());
            }
            //set the next skill as the current skill
            attackingEntity.CurrentSkillTarget = attackingEntity.NextSkillTarget;
            attackingEntity.CurrentSkill = attackingEntity.NextSkill;
            if (attackingEntity != attackingEntity.NextSkillTarget)
            {
                attackingEntity.PointTowardsEntity(attackingEntity.NextSkillTarget);
                attackingEntity.SendEntityChangedDirectionMessage();
            }
            //set the time untill the skill will complete
            attackingEntity.TimeTillActionComplete = theSkillLevel.GetCastingTime(attackingEntity.NextSkill,pvp) ;
            if (theSkill.IncludesWeaponAttack)
            {//was 0.70f
                // FISHING
                // Fishing uses base attack speed - this is currently hacked on the client (CombatManager.cs / StartAttack() / line: 548 / to: 2500)
                attackingEntity.TimeTillActionComplete += (CheckFishingAttack(attackingEntity, targetEntity) ? attackingEntity.GetBaseAttackSpeed : attackingEntity.CompiledStats.AttackSpeed) * 0.35f / 1000.0f;
            }
            attackingEntity.StartCasting();
            //start the action
            attackingEntity.ActionInProgress = true;
            //clear the next skill
            attackingEntity.NextSkillTarget = null;
            attackingEntity.NextSkill = null;
            //tell players he's started casting
            if (REPORT_MOB_SKILLS == true && attackingEntity.Type == CombatEntity.EntityType.Mob && attackingEntity.CurrentSkill.Template.ReportProgress == true)
            {
				int textID = (int)CombatManagerTextDB.TextID.OTHER_START_CASTING;
				m_containingZone.SendLocalSystemSkillMessageLocalised(new LocaliseParams(textDB, textID, attackingEntity.Name, attackingEntity.CurrentSkill.Template.SkillID), attackingEntity.CurrentPosition.m_position, Zone.LOCAL_MESSAGE_RANGE, false, SYSTEM_MESSAGE_TYPE.BATTLE);
				//string castingMessage = attackingEntity.Name + " started casting " + attackingEntity.CurrentSkill.Template.SkillName + ".";
				//m_containingZone.SendLocalSystemMessage(castingMessage, attackingEntity.CurrentPosition.m_position, Zone.LOCAL_MESSAGE_RANGE, false, SYSTEM_MESSAGE_TYPE.BATTLE);
            }
            return true;
        }

        internal bool CastSkill(CombatEntity attackingEntity, CombatEntity targetEntity, EntitySkill entitySkill,SkillDamageData preCalculatedDamage)
        {
            // if the entity is already dead
            bool targetAlreadyDead = (targetEntity.CurrentHealth <= 0) && entitySkill.SkillID != SKILL_TYPE.REVIVE;
            // reverse of above...trying to revive when we are alive
            bool targetRessurectionNowAlive = !targetEntity.Dead && entitySkill.SkillID == SKILL_TYPE.REVIVE;            

            //if it's the current skill being used (not an instant skill)
            bool success = false;
            bool inflictedEffect = false;
            int damage = 0;
            bool pvp = (targetEntity.Type == CombatEntity.EntityType.Player && targetEntity.IsPVP());
            // if it is the entities current skill then that skill is now complete, clear it down 
            if (entitySkill == attackingEntity.CurrentSkill)
            {
                attackingEntity.ActionInProgress = false;
                attackingEntity.CarryOutSkill();
                attackingEntity.LastSkill = attackingEntity.CurrentSkill;
                attackingEntity.CurrentSkillTarget = null;
                attackingEntity.CurrentSkill = null;
                attackingEntity.TimeAtLastAttack = Program.MainUpdateLoopStartTime()- attackingEntity.AttackProgressBeforeInterrupt;
            }
            bool aggressive = (attackingEntity.IsEnemyOf(targetEntity) == true);
            if (attackingEntity.StatusCancelConditions.Skills && aggressive== true)
            {
                attackingEntity.CancelEffectsDueToSkillUse(entitySkill,true);
            }
            int energyCost = entitySkill.getSkillTemplateLevel(pvp).EnergyCost;
            //can the entity afford the skill (this may not just be energy cost)
            bool canAfford = CanAffordSkill(energyCost, attackingEntity, entitySkill);
            //don't cast if the target is dead
            if (targetAlreadyDead == false && canAfford == true && targetRessurectionNowAlive == false)
            {
                Character character = null;
                if (attackingEntity.Type == CombatEntity.EntityType.Player)
                {
                    character = (Character)attackingEntity;
                }
                //false if the entity fails to cast the skill
                bool passedAbilityTest = true;

                if ((int)entitySkill.SkillID >= (int)SKILL_TYPE.TELEPORT_TO_START && (int)entitySkill.SkillID <= (int)SKILL_TYPE.TELEPORT_TO_END)
                {
                    bool positionPassed = TeleportEntityToTarget(attackingEntity, targetEntity, 0); 


                    if (positionPassed == false)
                    {
                        if (character != null)
                        {
							string locText = Localiser.GetString(textDB, character.m_player, (int)CombatManagerTextDB.TextID.PLAYER_CANNOT_REACH_TARGET);
							Program.processor.sendSystemMessage(locText, character.m_player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
                        }
                        passedAbilityTest = false;
                    }
                }
                else if ((int)entitySkill.SkillID >= (int)SKILL_TYPE.RESCUE && (int)entitySkill.SkillID <= (int)SKILL_TYPE.TELEPORT_OTHER_END)
                {
                    bool positionPassed = TeleportEntityToTarget(targetEntity, attackingEntity, 0); 


                    if (positionPassed == false)
                    {
                        if (character != null)
                        {
							string locText = Localiser.GetString(textDB, character.m_player, (int)CombatManagerTextDB.TextID.PLAYER_CANNOT_REACH_TARGET);
							Program.processor.sendSystemMessage(locText, character.m_player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
                        }
                        passedAbilityTest = false;
                    }
                }

                if (attackingEntity.Type == CombatEntity.EntityType.Player && entitySkill.Template.AbilityID != ABILITY_TYPE.NA && passedAbilityTest==true)
                {
                    //random chance to fail, 
                    //chance to fail reduced with higher ability
                    CharacterAbility ability = character.getAbilityById(entitySkill.Template.AbilityID);
                    int abilityLevel = character.getAbilityLevel(entitySkill.Template.AbilityID);
                    int tweakedAbilityLevel = ABILITY_FUDGE_FACTOR + abilityLevel;
                    int randomResult = Program.getRandomNumber(tweakedAbilityLevel + entitySkill.getSkillTemplateLevel(pvp).SuccessChance);
                    //failed the skill 
                    if (randomResult > tweakedAbilityLevel)
                    {

                        //if it's a player then reset the skill and send a message
                        character.SendSkillUpdate((int)entitySkill.SkillID, entitySkill.SkillLevel, 0.0f);
                        if (ability == null)
                        {
							string locText = Localiser.GetString(textDB, character.m_player, (int)CombatManagerTextDB.TextID.PLAYER_FUMBLED_NEED_TRAINING);
							string locAbilityName = AbilityManager.GetLocaliseAbilityName(character.m_player, entitySkill.Template.AbilityID);
							string skillName = SkillTemplateManager.GetLocaliseSkillName(character.m_player, entitySkill.Template.SkillID);
							locText = string.Format(locText, skillName, locAbilityName);
							Program.processor.sendSystemMessage(locText, character.m_player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
                        }
                        else
                        {
							string locText = Localiser.GetString(textDB, character.m_player, (int)CombatManagerTextDB.TextID.PLAYER_FUMBLED_KEEP_PRACTICING);
							string locAbilityName = AbilityManager.GetLocaliseAbilityName(character.m_player, entitySkill.Template.AbilityID);
							string skillName = SkillTemplateManager.GetLocaliseSkillName(character.m_player, entitySkill.Template.SkillID);
							locText = string.Format(locText, skillName, locAbilityName);
							Program.processor.sendSystemMessage(locText, character.m_player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
                        }
                        passedAbilityTest = false;
                        //   Program.Display(attackingEntity.ServerID + " failed to cast " + entitySkill.SkillID);
                    }
                        //passed the skill (random fail passed)
                    else
                    {
						string locText = Localiser.GetString(textDB, character.m_player, (int)CombatManagerTextDB.TextID.SKILL_SUCCEEDED);
						string skillName = SkillTemplateManager.GetLocaliseSkillName(character.m_player, entitySkill.Template.SkillID);
						locText = string.Format(locText, skillName);
						Program.processor.sendSystemMessage(locText, character.m_player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
                    }
                    //if there is an associated ability 
                    //then there is a chance to increase it
                    if (ability != null)
                    {
                        if (targetEntity.Type != CombatEntity.EntityType.Mob || !((ServerControlledEntity)targetEntity).Template.m_noAbilityTest)
                        {

                            character.testAbilityUpgrade(ability);
                        }
                    }

                }
                
                if (Program.m_LogSysBattle)
                {
                    if (passedAbilityTest == true)
                    {
                        Program.Display(attackingEntity.Name + " cast " + entitySkill.Template.SkillName + " on " + targetEntity.Name);
                    }
                    else
                    {
                        Program.Display(attackingEntity.Name + " failed to cast " + entitySkill.Template.SkillName + " on " + targetEntity.Name);
                    }
                }
                
                double aggroMultiplier = 1.0;
                SkillTemplateLevel skillLevel = entitySkill.getSkillTemplateLevel(pvp);
                if (skillLevel != null)
                {
                    aggroMultiplier = skillLevel.AggroMultiplier;
                }
                if (passedAbilityTest)
                {
                    //take the cost of the skill from the entity (may not just be energy)
                    PayForSkill(energyCost, attackingEntity, entitySkill);

                    //the skill has been successfully cast, now it needs to start recharging
                    entitySkill.SkillCast(Program.MainUpdateLoopStartTime());
                    //check if it's a proc or a standard skill, this will effect how the damage is played client side
                    ATTACK_TYPE attackType = ATTACK_TYPE.SKILL;
                    if (entitySkill.IsProc == true)
                    {
                        attackType = ATTACK_TYPE.ATTACK_TRIGGERED_SKILL;
                    }
                    //do the damage to the main target
                    CombatDamageMessageData mainDamage = null;
                    if (preCalculatedDamage != null)
                    {
                        mainDamage = preCalculatedDamage.TargetDamage;
                    }

                    damage = DoSkillDamage(attackingEntity, targetEntity, entitySkill, ref inflictedEffect, attackType, mainDamage);
                    //should there be an aoe?
                    bool aoeDone = DoAOEDamage(attackingEntity, targetEntity, entitySkill, preCalculatedDamage);

                    if (skillLevel.CanDropLoot())
                    {
                        bool itemsGained =skillLevel.GiveLootToPlayer(attackingEntity, entitySkill);
                        if (itemsGained == true)
                        {
                            success = true;
                        }
                    }
                    if ((energyCost != 0) && (character != null))
                    {
                        character.SetStatsChangeLevel(CombatEntity.STATS_CHANGE_LEVEL.BASIC_CHANGED);//character.m_statsUpdated=true;
                    }
                }
                if (attackingEntity.Type == CombatEntity.EntityType.Player && character != null)
                {

                    //if the skill was a sucess
                    if (passedAbilityTest)
                    {
                      
                        //send the skill Update
                        SkillTemplateLevel currentLevel = entitySkill.Template.getSkillTemplateLevel(entitySkill.ModifiedLevel, pvp);

                        double rechargeTime = 0;
                        if (currentLevel != null)
                        {
                            rechargeTime = currentLevel.GetRechargeTime(entitySkill, pvp);
                        }
                        character.SendSkillUpdate((int)entitySkill.SkillID, entitySkill.SkillLevel, rechargeTime);
                        //update any quests
                        int mobTemplateID = -1;
                        if (targetEntity.Type == CombatEntity.EntityType.Mob)
                        {
                            ServerControlledEntity theMob = (ServerControlledEntity)targetEntity;
                            if (theMob != null&&theMob.Template!=null)
                            {
                                mobTemplateID = theMob.Template.m_templateID;
                            }
                        }
                        character.m_QuestManager.checkSkillRequired((int)entitySkill.SkillID, mobTemplateID);
                        entitySkill.TimesCastSinceLog++;
                        if (Program.m_LogAnalytics)
                        {
                            int skillID = (int)entitySkill.SkillID;
                            string skillName = entitySkill.Template.SkillName;
                            string reasonForFail = "";

                            if (!entitySkill.FromItem && !entitySkill.IsProc)
                            {
                                AnalyticsMain logAnalytics = new AnalyticsMain(true);
                                logAnalytics.skillUsed(character.m_player, skillID.ToString(), skillName, true, reasonForFail);
                            }
                        }
                    }
                        //if the skill failed
                    else
                    {
                        if (Program.m_LogAnalytics)
                        {
                            int skillID = (int)entitySkill.SkillID;
                            string skillName = entitySkill.Template.SkillName;
                            string reasonForFail = "Failed";
                            if (!entitySkill.FromItem && !entitySkill.IsProc)
                            {
                                AnalyticsMain logAnalytics = new AnalyticsMain(true);
                                logAnalytics.skillUsed(character.m_player, skillID.ToString(), skillName, false, reasonForFail);
                            }
                        }

                        //send the skill Update, it does not need to recharge
                        SkillTemplateLevel currentLevel = entitySkill.Template.getSkillTemplateLevel(entitySkill.ModifiedLevel, pvp);
                        character.SendSkillUpdate((int)entitySkill.SkillID, entitySkill.SkillLevel, 0);
                    }



                }
                else if (attackingEntity.IsEnemyOf(targetEntity) == true)
                {
                    //if a player has hit something, 
                    //send a notification to the target so they can respond (mob or player)
                    if (attackingEntity.Type != CombatEntity.EntityType.Mob)
                    {
                        CombatDamageMessageData theData = targetEntity.TakeDamage(0, 0, attackingEntity, ATTACK_TYPE.SKILL, (int)entitySkill.SkillID, true, (int)COMBAT_REACTION_TYPES.CRT_NO_REACTION, 0, 0);

                        theData.AggroModifier = aggroMultiplier;
                    }
                }
            }
            else
            {
                //it failed to no cost
                energyCost = 0;
                Character character = null;
                //explain why the skill could not be done
                if (attackingEntity.Type == CombatEntity.EntityType.Player)
                {
                    character = (Character)attackingEntity;
                    //send a skill update so the skill is recharged
                    character.SendSkillUpdate((int)entitySkill.SkillID, entitySkill.SkillLevel, 0.0f);
                }
                //notify the entity the skill has failed 
                attackingEntity.SkillFailedConditions();
            }
            //update records
            if (attackingEntity.m_AttackTargetCombatRecord != null && attackingEntity.AttackTarget == targetEntity && damage > 0)
            {
                attackingEntity.m_AttackTargetCombatRecord.m_total_Skill_Damage += damage;
                attackingEntity.m_AttackTargetCombatRecord.m_num_skill_casts++;
            }
            //if was it a successful cast
            if (damage != 0 || inflictedEffect == true || energyCost != 0)
            {
                success = true;
                //play the casting string
                SendCastString(entitySkill.Template, attackingEntity, targetEntity);
            }
            return success;
        }

        bool TeleportEntityToTarget(CombatEntity movingEntity, CombatEntity targetEntity, float distAwayFromTarget)
        {
            bool success = false;
            float currentDistToTarget = Utilities.Difference2D(movingEntity.CurrentPosition.m_position, targetEntity.CurrentPosition.m_position);
            float distFromTarget = targetEntity.Radius + (movingEntity.Radius * 3) + distAwayFromTarget;
            bool positionPassed = false;
            Vector3 newPosition = movingEntity.CurrentPosition.m_position;
            Vector3 newDirection = movingEntity.CurrentPosition.m_direction;
            float newAngle = movingEntity.CurrentPosition.m_yangle;
            if (movingEntity == targetEntity || distFromTarget > currentDistToTarget)
            {
                positionPassed = true;
            }
            else
            {
                newPosition = movingEntity.GetValidLocationFromTarget(targetEntity.CurrentPosition.m_position, distFromTarget, ref positionPassed);
                newDirection = targetEntity.CurrentPosition.m_position - newPosition;

                newDirection = newPosition - targetEntity.CurrentPosition.m_position;

                newAngle = Utilities.GetYAngleFromDirection(newDirection);
            }
            if (positionPassed)
            {
#if false
                ASTriangle targetTri = m_containingZone.PathFinder.TheMap.GetClosestTriangleForPosition(newPosition);
                if (targetTri == null)
                    return false;
                Vector3 closestPos = targetTri.Plane.GetClosestPointTo(newPosition);
                if ((closestPos.X - newPosition.X) > 0.1f )
                    return false;
                if ((closestPos.Z - newPosition.Z) > 0.1f)
                    return false;
#endif

                Program.Display("Teleport attempt currentPos =" + movingEntity.CurrentPosition.m_position.ToString() + " new Position = " + newPosition.ToString() + " yangle = " + newAngle);
                movingEntity.CurrentPosition.m_position = newPosition;
                movingEntity.CurrentPosition.m_direction = newDirection;
                movingEntity.CurrentPosition.m_yangle = newAngle;
                if (movingEntity.Type == CombatEntity.EntityType.Player )
                {
                   // Character character = null;
                    Character character = (Character)movingEntity;
                
                    if (character != null)
                    {
                        character.m_ConfirmedPosition.m_position = newPosition;
                        character.m_ConfirmedPosition.m_direction = newDirection;
                        character.m_ConfirmedPosition.m_yangle = newAngle;
                        List<NetConnection> connections = new List<NetConnection>();
                        m_containingZone.AddConnectionsOfPlayersToList(connections, m_containingZone.m_players, null);
                        NetOutgoingMessage teleportMessage = m_containingZone.CreateCharacterCorrectionMessage(Program.Server, character, newPosition, m_containingZone.m_zone_id, newAngle);
                        Program.processor.SendMessage(teleportMessage, connections, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.CorrectPlayerPosition);
                    }
                }
                else if (movingEntity.Type == CombatEntity.EntityType.Mob)
                {
                    ServerControlledEntity theMob = (ServerControlledEntity)movingEntity;
                    if (theMob != null)
                    {
                        theMob.StopAtCurrentPosition();
                    }
                }
                success = true;
                //success = false;
            }
            else
            {
                success = false;
               /* if (character != null)
                {
                    Program.processor.sendSystemMessage("Invalid location for " + entitySkill.Template.SkillName, character.m_player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
                }*/
            }
            return success;

        }

        void SendCastString(SkillTemplate theTemplate, CombatEntity caster, CombatEntity target)
        {

            //get the target string
            string targetString = theTemplate.TargetCastingString;
            if (caster == target&& theTemplate.SelfCastingString.Length>0)
            {
                targetString = theTemplate.SelfCastingString;
            }
            //if it exists then replace the tags
            if (targetString.Length > 0)
            {
                targetString = SkillTemplate.GetCastString(theTemplate, caster, target, targetString);
            }

            //get the local string
            string localString = theTemplate.LocalCastingString;
            //if it exists then replace the tags
            if (localString.Length > 0)
            {
                localString = SkillTemplate.GetCastString(theTemplate, caster, target, localString);
            }
            Character targetCharacter = null;
            //if the target is a player
            if (target.Type == CombatEntity.EntityType.Player)
            {
                targetCharacter = (Character)target;
                //check the character cast correctly
                if (targetCharacter!=null)
                {
                    //if the target string exists send the target string
                    if (targetString.Length > 0)
                    {
                        Program.processor.sendSystemMessage(targetString, targetCharacter.m_player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
                    }
                    //otherwise if the local string exists send the local string
                    else if (localString.Length > 0)
                    {
                        Program.processor.sendSystemMessage(localString, targetCharacter.m_player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
                    }
                    //else if the target != the caster then send the standard string
                    else if (target != caster&&theTemplate.ReportProgress==true)
                    {
						string locText = Localiser.GetString(textDB, targetCharacter.m_player, (int)CombatManagerTextDB.TextID.OTHER_CAST_SKILL_ON_PLAYER);
						string skillName = SkillTemplateManager.GetLocaliseSkillName(targetCharacter.m_player, theTemplate.SkillID);
						locText = string.Format(locText, caster.Name, skillName);
						Program.processor.sendSystemMessage(locText, targetCharacter.m_player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
                    }
                }
            }

            //if the local string exists
            if (localString.Length > 0)
            {
                //get a list of the local players in range
                //not including the target
                Player playerToExclude = null;
                if (targetCharacter != null)
                {
                    playerToExclude = targetCharacter.m_player;
                }
                //send them the local message
                m_containingZone.SendLocalSystemMessage(localString, target.CurrentPosition.m_position, Zone.LOCAL_MESSAGE_RANGE, false, SYSTEM_MESSAGE_TYPE.BATTLE,playerToExclude);
            
            }
            


        }
        bool DoAOEDamage(CombatEntity attackingEntity, CombatEntity targetEntity, EntitySkill entitySkill, SkillDamageData skilldamage)
        {
            bool damageDone = false;
            if (entitySkill.Template.AOE > 0)
            {
                List<AOETarget> aoeTargets = new List<AOETarget>();
                //if the skill damage exists use the data from there
                if (skilldamage != null)
                {
                    //get the list from the damage
                    List<CombatDamageMessageData> aoeDamageList = skilldamage.AOEDamages;
                    //for each damage in the list
                    for (int damageIndex = 0; damageIndex < aoeDamageList.Count; damageIndex++)
                    {
                        //get the current damage
                        CombatDamageMessageData currentDamage = aoeDamageList[damageIndex];
                        if (currentDamage != null && currentDamage.TargetLink != null)
                        {

                            //get the target link and check it's valid
                            CombatEntity currentEntity = currentDamage.TargetLink;
                            if (currentEntity.Destroyed == false)
                            {
                                //add it to the list of aoe targets
                                AOETarget newTarget = new AOETarget(currentEntity, currentDamage);
                                aoeTargets.Add(newTarget);
                            }
                        }
                    }

                }
                //otherwise look for the targets now
                else
                {
                    List<CombatEntity> aoeEntities = new List<CombatEntity>();
                    ZonePartition.ENTITY_TYPE typeToLookFor = ZonePartition.ENTITY_TYPE.ET_NOT_ENEMY;
                    if (entitySkill.Template.CastTargetGroup == SkillTemplate.CAST_TARGET.ENEMY)
                    {
                        typeToLookFor = ZonePartition.ENTITY_TYPE.ET_ENEMY;
                    }
                    //get all targets in range
                    m_containingZone.PartitionHolder.AddEntitiesInRangeToList(attackingEntity, targetEntity.CurrentPosition.m_position, entitySkill.Template.AOE, aoeEntities, typeToLookFor, targetEntity, true);
               
                    //add all of these targets to the aoe target list
                    for (int entityIndex = 0; entityIndex < aoeEntities.Count; entityIndex++)
                    {
                        CombatEntity currentAOEEntity = aoeEntities[entityIndex];
                        AOETarget newTarget = new AOETarget(currentAOEEntity, null);
                        aoeTargets.Add(newTarget);
                    }
                }
                for (int currentAOEIndex = 0; currentAOEIndex < aoeTargets.Count; currentAOEIndex++)
                {
                    bool inflictedAOEStatusEffect = false;

                    AOETarget currentAOETarget = aoeTargets[currentAOEIndex];//aoeEntities[currentAOEIndex];
                    CombatEntity currentAOEEntity = currentAOETarget.Target;
                    CombatDamageMessageData currentAOEDamage = currentAOETarget.Damage;
                    if (entitySkill.Template.CastTargetGroup != SkillTemplate.CAST_TARGET.GROUP || attackingEntity.IsInPartyWith(currentAOEEntity) == true)
                    {
                        if (currentAOEEntity.CurrentHealth > 0)
                        {
                            int currentAOEDamageResult = DoSkillDamage(attackingEntity, currentAOEEntity, entitySkill, ref inflictedAOEStatusEffect, ATTACK_TYPE.AOE_SKILL, currentAOEDamage);
                            damageDone = true;
                        }
                    }
                }
            }
            return damageDone;
        }
        internal SkillDamageData DummyCastSkill(CombatEntity attackingEntity, CombatEntity targetEntity, EntitySkill entitySkill,double startTime)
        {
            SkillDamageData skilldamage = null;
            //if the entity is already dead
            bool targetAlreadyDead = (targetEntity.CurrentHealth <= 0) && entitySkill.SkillID != SKILL_TYPE.REVIVE;
            // is it player v player
            bool pvp = (targetEntity.Type == CombatEntity.EntityType.Player && targetEntity.IsPVP());
            //how much does the skill cost
             int energyCost = entitySkill.getSkillTemplateLevel(pvp).EnergyCost;
            //can the entity afford the skill (this may not just be energy cost)
            bool canAfford = CanAffordSkill(energyCost, attackingEntity, entitySkill);
            //don't cast if the target is dead
            if (targetAlreadyDead == false && canAfford == true)
            {
                //it will succeed, time to find out how much damage it will do
                ATTACK_TYPE attackType = ATTACK_TYPE.SKILL;
                if (entitySkill.IsProc == true)
                {
                    attackType = ATTACK_TYPE.ATTACK_TRIGGERED_SKILL;
                }
                //do the damage to the main target
                CombatDamageMessageData targetDamage = DoSkillDamageInAdvance(attackingEntity, targetEntity, entitySkill, attackType, startTime);
                List<CombatDamageMessageData> aoeDamages = new List<CombatDamageMessageData>();
                //should there be an aoe?
                if (entitySkill.Template.AOE > 0)
                {
                    List<CombatEntity> aoeTargets = new List<CombatEntity>();
                    ZonePartition.ENTITY_TYPE typeToLookFor = ZonePartition.ENTITY_TYPE.ET_NOT_ENEMY;
                    if (entitySkill.Template.CastTargetGroup == SkillTemplate.CAST_TARGET.ENEMY)
                    {
                        typeToLookFor = ZonePartition.ENTITY_TYPE.ET_ENEMY;
                    }
                    //get all targets in range
                    m_containingZone.PartitionHolder.AddEntitiesInRangeToList(attackingEntity, targetEntity.CurrentPosition.m_position, entitySkill.Template.AOE, aoeTargets, typeToLookFor, targetEntity);
                    for (int currentAOEIndex = 0; currentAOEIndex < aoeTargets.Count; currentAOEIndex++)
                    {
                        CombatEntity currentAOETarget = aoeTargets[currentAOEIndex];
                        if (entitySkill.Template.CastTargetGroup != SkillTemplate.CAST_TARGET.GROUP || attackingEntity.IsInPartyWith(currentAOETarget) == true)
                        {
                            if (currentAOETarget.CurrentHealth > 0)
                            {
                                //int currentAOEDamage = DoSkillDamage(attackingEntity, currentAOETarget, entitySkill, ref inflictedAOEStatusEffect, ATTACK_TYPE.AOE_SKILL, null);
                                CombatDamageMessageData aoeDamage = DoSkillDamageInAdvance(attackingEntity, currentAOETarget, entitySkill, ATTACK_TYPE.AOE_SKILL, startTime);
                                aoeDamages.Add(aoeDamage);
                            }
                        }
                    }
                   
                }
                skilldamage = new SkillDamageData(entitySkill, targetDamage, aoeDamages);
            }

            return skilldamage;
        }
		string getAvoidanceString(Player audiencePlayer, CombatEntity targetEntity, EntitySkill entitySkill, bool personal)
        {
            if (personal)
            {
				int textID = 0;
                switch (entitySkill.Template.AvoidanceType)
                {
                    case AVOIDANCE_TYPE.PHYSICAL:
						textID = (int)CombatManagerTextDB.TextID.PLAYER_WITHTAND_SKILL;
						break;
                    case AVOIDANCE_TYPE.SPELL:
						textID = (int)CombatManagerTextDB.TextID.PLAYER_DEFLECT_SKILL;
						break;
                    case AVOIDANCE_TYPE.MOVEMENT:
						textID = (int)CombatManagerTextDB.TextID.PLAYER_AVOID_SKILL;
						break;
                    case AVOIDANCE_TYPE.WOUNDING:
						textID = (int)CombatManagerTextDB.TextID.PLAYER_SHRUG_OFF_SKILL;
						break;
                    case AVOIDANCE_TYPE.WEAKENING:
						textID = (int)CombatManagerTextDB.TextID.PLAYER_ENDURE_SKILL;
						break;
                    case AVOIDANCE_TYPE.MENTAL:
						textID = (int)CombatManagerTextDB.TextID.PLAYER_IGNORE_SKILL;
						break;
                    default:
						textID = (int)CombatManagerTextDB.TextID.PLAYER_RESIST_SKILL;
						break;
                }
				// which target language is depends on player
				string locText = Localiser.GetString(textDB, audiencePlayer, textID);
				string skillName = SkillTemplateManager.GetLocaliseSkillName(audiencePlayer, entitySkill.Template.SkillID);
				locText = string.Format(locText, skillName);
				return locText;
            }
            else
            {
				int textID = 0;
				switch (entitySkill.Template.AvoidanceType)
                {
                    case AVOIDANCE_TYPE.PHYSICAL:
						textID = (int)CombatManagerTextDB.TextID.OTHER_WITHSTANDS_SKILL;
						break;
                    case AVOIDANCE_TYPE.SPELL:
						textID = (int)CombatManagerTextDB.TextID.OTHER_DEFLECTS_SKILL;
						break;
                    case AVOIDANCE_TYPE.MOVEMENT:
						textID = (int)CombatManagerTextDB.TextID.OTHER_AVOIDS_SKILL;
						break;
                    case AVOIDANCE_TYPE.WOUNDING:
						textID = (int)CombatManagerTextDB.TextID.OTHER_SHRUGS_OFF_SKILL;
						break;
                    case AVOIDANCE_TYPE.WEAKENING:
						textID = (int)CombatManagerTextDB.TextID.OTHER_ENDURES_SKILL;
						break;
                    case AVOIDANCE_TYPE.MENTAL:
						textID = (int)CombatManagerTextDB.TextID.OTHER_IGNORES_SKILL;
						break;
                    default:
						textID = (int)CombatManagerTextDB.TextID.OTHER_RESISTS_SKILL;
						break;
                }
				// which target language is depends on player
				string locText = Localiser.GetString(textDB, audiencePlayer, textID);
				string skillName = SkillTemplateManager.GetLocaliseSkillName(audiencePlayer, entitySkill.Template.SkillID);
				locText = string.Format(locText, targetEntity.Name, skillName);
				return locText;
            }

			//if (personal)
			//{
			//	switch (entitySkill.Template.AvoidanceType)
			//	{
			//		case AVOIDANCE_TYPE.PHYSICAL:
			//			return "You withstand " + entitySkill.Template.SkillName;
			//		case AVOIDANCE_TYPE.SPELL:
			//			return "You deflect " + entitySkill.Template.SkillName;
			//		case AVOIDANCE_TYPE.MOVEMENT:
			//			return "You avoid " + entitySkill.Template.SkillName;
			//		case AVOIDANCE_TYPE.WOUNDING:
			//			return "You shrug off " + entitySkill.Template.SkillName;
			//		case AVOIDANCE_TYPE.WEAKENING:
			//			return "You endure " + entitySkill.Template.SkillName;
			//		case AVOIDANCE_TYPE.MENTAL:
			//			return "You ignore " + entitySkill.Template.SkillName;
			//		default:
			//			return "You resist " + entitySkill.Template.SkillName;
			//	}
			//}
			//else
			//{
			//	switch (entitySkill.Template.AvoidanceType)
			//	{
			//		case AVOIDANCE_TYPE.PHYSICAL:
			//			return targetEntity.Name + " withstands " + entitySkill.Template.SkillName;
			//		case AVOIDANCE_TYPE.SPELL:
			//			return targetEntity.Name + " deflects " + entitySkill.Template.SkillName;
			//		case AVOIDANCE_TYPE.MOVEMENT:
			//			return targetEntity.Name + " avoids " + entitySkill.Template.SkillName;
			//		case AVOIDANCE_TYPE.WOUNDING:
			//			return targetEntity.Name + " shrugs off " + entitySkill.Template.SkillName;
			//		case AVOIDANCE_TYPE.WEAKENING:
			//			return targetEntity.Name + " endures " + entitySkill.Template.SkillName;
			//		case AVOIDANCE_TYPE.MENTAL:
			//			return targetEntity.Name + " ignores " + entitySkill.Template.SkillName;
			//		default:
			//			return targetEntity.Name + " resists " + entitySkill.Template.SkillName;
			//	}
			//}
        }


        CalculatedDamage GetAttackSkillDamage(CombatEntity attackingEntity, CombatEntity targetEntity, EntitySkill entitySkill, float abilityLevel, float statModifier)
        {
            int skillType = -1;
            bool pvp = (targetEntity.Type == CombatEntity.EntityType.Player && targetEntity.IsPVP());
            SkillTemplate theSkill = null;
            SkillTemplateLevel theSkillLevel = null;
            if (entitySkill != null)
            {
                theSkill = entitySkill.Template;
                theSkillLevel = entitySkill.getSkillTemplateLevel(pvp);
                skillType = (int)theSkill.DamageType;
            }
            int damage = 0;
            int originalDamage = 0;

            bool extraBonusAdded = false;
            bool resistedAllAttacks = true;
            bool attackHit = false;
            int hitProbabilitySum = attackingEntity.Attack + targetEntity.Defence;
            int result = Program.getRandomNumber(hitProbabilitySum);
            //if (result < attackingEntity.Attack || result == 0)
            {
                attackHit = true;
            }
            List<FloatForID> combinedDamageTypes = attackingEntity.CompiledStats.CombinedDamageType;
            for (int i = 0; i < combinedDamageTypes.Count; i++)
            {
                int currentType = combinedDamageTypes[i].m_bonusType;
            
                if (attackingEntity.GetCombinedDamageType(currentType) > 0)
                {

                    resistedAllAttacks = false;
                    if (extraBonusAdded == false && attackingEntity.Type == CombatEntity.EntityType.Player)
                    {
                        damage += BASE_ATTACK_DAMAGE;
                        extraBonusAdded = true;
                    }

                    //work out the damage
                    int maxdamage = (int)attackingEntity.GetCombinedDamageType(currentType);
                    int maxdefence = targetEntity.GetBonusType(currentType);
                    /* if (i < 3)//physical damage
                     {
                         maxdamage += (int)Math.Ceiling((attackingEntity.GetDamageType(i) * attackingEntity.ModifiedDamage) / 100.0f);
                     }*/

                    if (pvp)
                    {
                        maxdamage = (int)Math.Ceiling(maxdamage * Character.m_pvpMeleeMult);
                    }
              
                    CalculatedDamage calcDamage = DamageCalculator.CalculateDamage(true, false, maxdamage, maxdefence, attackingEntity, targetEntity);
                    
                    targetEntity.AttemptToReduceDamage(currentType, calcDamage);
                    int newdamage = calcDamage.m_calculatedDamage;
                    if (!attackHit)
                    {
                        newdamage = (int)Math.Ceiling(newdamage / 10.0f);
                    }
                    damage += newdamage;
                    int preLVLdamage = calcDamage.m_preLvlReductionDamage;
                    if (!attackHit)
                    {
                        preLVLdamage = (int)Math.Ceiling(preLVLdamage / 10.0f);
                    }
                    originalDamage += preLVLdamage;
                   

                }
            }

            int extradamage = 0;
            if (theSkillLevel != null)
            {
                extradamage = theSkillLevel.getModifiedAmout(abilityLevel, statModifier,entitySkill,pvp);
            }
            int extradefence = 0;
            if (skillType >= 0 && theSkill != null)
            {
                extradefence = targetEntity.GetBonusType(skillType);
            }
            else if (extradamage != 0)
            {
                resistedAllAttacks = false;
            }
            if (resistedAllAttacks == true && theSkill != null)
            {
                //send a message to the player
                if (attackingEntity.Type == CombatEntity.EntityType.Player)
                {
                    Character attackingCharacter = (Character)attackingEntity;
                    if (attackingCharacter != null && attackingCharacter.m_player != null)
                    {
                        if (targetEntity != attackingEntity)
                        {
							string locText = Localiser.GetString(textDB, attackingCharacter.m_player, (int)CombatManagerTextDB.TextID.OTHER_RESISTED_SKILL);
							string skillName = SkillTemplateManager.GetLocaliseSkillName(attackingCharacter.m_player, theSkill.SkillID);
							locText = string.Format(locText, targetEntity.Name, skillName);
							Program.processor.sendSystemMessage(locText, attackingCharacter.m_player, false, SYSTEM_MESSAGE_TYPE.SKILLS);
                        }
                    }
                }
            }
            
            CalculatedDamage calcExtraDamage = DamageCalculator.CalculateDamage(true, false, extradamage, extradefence, attackingEntity, targetEntity);
            targetEntity.AttemptToReduceDamage(skillType, calcExtraDamage);
            //extradamage = Program.calcSavedDamage(extradamage, extradefence, attackingEntity.m_level, targetEntity);
            //damage += extradamage;
            damage += calcExtraDamage.m_calculatedDamage;
            originalDamage += calcExtraDamage.m_preLvlReductionDamage;
            CalculatedDamage finalDamage = new CalculatedDamage(damage, originalDamage);

            return finalDamage;
        }

        CalculatedDamage GetMagicSkillDamage(CombatEntity attackingEntity, CombatEntity targetEntity, EntitySkill entitySkill, float abilityLevel, float statModifier)
        {
            SkillTemplate theSkill = entitySkill.Template;
            bool pvp = (targetEntity.Type == CombatEntity.EntityType.Player && targetEntity.IsPVP());
            SkillTemplateLevel theSkillLevel = entitySkill.getSkillTemplateLevel(pvp);
           
            bool aggressive =( (attackingEntity.IsEnemyOf(targetEntity) == true)||theSkill.CastTargetGroup == SkillTemplate.CAST_TARGET.ENEMY);
           
             int skillType = (int)theSkill.DamageType;
            double skillDamage = theSkillLevel.InitialDamage;

            int damage = 0;

            int maxDamage = theSkillLevel.getModifiedAmout(abilityLevel, statModifier, entitySkill, pvp);
            damage = maxDamage;
            int maxDefence = 0;
            //attackingEntity.Type != targetEntity.Type); pvp change 27/10/11
            CalculatedDamage calcDamage = new CalculatedDamage(damage, damage);
            if (aggressive)
            {
                if (theSkill.DamageType >= 0)
                {
                    maxDefence = targetEntity.GetBonusType((int)theSkill.DamageType);
                }
                //damage
                calcDamage = DamageCalculator.CalculateDamage(true, false, maxDamage, maxDefence, attackingEntity, targetEntity);
                
                targetEntity.AttemptToReduceDamage((int)theSkill.DamageType, calcDamage);

            }
            else
            {
                if (entitySkill.FromItem == false)
                {
                    damage = maxDamage;// Program.calcHealingValue(maxDamage);//Program.calcSavedDamage(maxDamage, 0, targetEntity.m_level, targetEntity);
                    calcDamage = new CalculatedDamage(maxDamage, maxDamage);
                }
                if(Program.m_LogDamage)
                Program.Display(@"magic damage effect " + maxDamage);
            }
            return calcDamage;
        }

        public bool CanAffordSkill(int cost, CombatEntity theEntity, EntitySkill theSkill)
        {
            bool canAfford = false;
            switch (theSkill.SkillID)
            {
                case SKILL_TYPE.SACRIFICE:
                    {
                        if (-cost < theEntity.CurrentHealth)
                        {
                            canAfford = true;
                        }
                        break;
                    }
                default:
                    {
                        if (cost <= theEntity.CurrentEnergy)
                        {
                            canAfford = true;
                        }
                        break;
                    }
            }
            return canAfford;
        }

        int PayForSkill(int cost, CombatEntity theEntity, EntitySkill theSkill)
        {
            switch (theSkill.SkillID)
            {
                case SKILL_TYPE.SACRIFICE:
                    {

                        CombatDamageMessageData newDamage = theEntity.TakeDamage(-cost, -cost, theEntity, ATTACK_TYPE.SKILL, -(int)theSkill.SkillID, false, (int)COMBAT_REACTION_TYPES.CRT_NO_REACTION, -cost, 0);
                        double currentTime = Program.MainUpdateLoopStartTime();
                        newDamage.ApplyTime = currentTime;
                        AddToPendingDamage(newDamage);
                        break;
                    }
                default:
                    {
                        theEntity.CurrentEnergy -= cost;
                        break;
                    }
            }
            return cost;
        }

        /// <summary>
        /// Returns true if the damage isn't the standard way
        /// </summary>
        /// <param name="skillTemplate"></param>
        /// <returns></returns>
        internal Boolean IsSpecialCaseSkillCast(SkillTemplate skillTemplate)
        {
            switch (skillTemplate.SkillID)
            {
                case SKILL_TYPE.DISTRACT:
                case SKILL_TYPE.TAUNT:
                case SKILL_TYPE.WARCRY:
                case SKILL_TYPE.CALM:
                case SKILL_TYPE.SACRIFICE:
                case SKILL_TYPE.ENERGY_DRAIN:                
                case SKILL_TYPE.RESCUE:
                    {
                        return true;
                    
                    }
                default:
                    {
                        break;
                    }
            }
            return false;
        }

        internal int DoSkillDamage(CombatEntity attackingEntity, CombatEntity targetEntity, EntitySkill entitySkill, ref bool inflictedEffect, ATTACK_TYPE attackType, CombatDamageMessageData preCalculatedDamage)
        {

            //if the damage does not exist calculate it now
            if (preCalculatedDamage == null)
            {
                double currentTime = Program.MainUpdateLoopStartTime();
                preCalculatedDamage = DoSkillDamageInAdvance(attackingEntity, targetEntity, entitySkill, attackType, currentTime);
                //if the damage still does not exist drop out
                if (preCalculatedDamage == null)
                {
                    //print out the error
                    Program.Display("Error in DoSkillDamage, failed to calculate damage for " + attackingEntity.Name + " skill " + entitySkill.Template.SkillName + " (Skill Level " + entitySkill.SkillLevel + ")");
                    return 0;
                }
            }

            //set the attacking entity to hostile if it is a hostile skill
            bool aggressive = false;
            aggressive = attackingEntity.IsEnemyOf(targetEntity);
            if (aggressive == true || targetEntity.InCombat == true)
            {
                attackingEntity.ConductedHotileAction();
            }

            //what type of entity did the damage to what
            bool PVP = (targetEntity.Type == CombatEntity.EntityType.Player && targetEntity.IsPVP());
            SkillTemplate theSkill = entitySkill.Template;
            SkillTemplateLevel theSkillLevel = entitySkill.getSkillTemplateLevel(PVP);




            if (theSkill != null)
            {

                if (preCalculatedDamage.Reaction == (int)COMBAT_REACTION_TYPES.CRT_SKILL_AVOIDED)
                {

                    if (attackingEntity.Type == CombatEntity.EntityType.Player && targetEntity.Type == CombatEntity.EntityType.Player)
                    {
                        Program.processor.sendSystemMessage(getAvoidanceString(((Character)attackingEntity).m_player, targetEntity, entitySkill, false), ((Character)attackingEntity).m_player, false, SYSTEM_MESSAGE_TYPE.SKILLS);
                        Program.processor.sendSystemMessage(getAvoidanceString(((Character)targetEntity).m_player, targetEntity, entitySkill, true), ((Character)targetEntity).m_player, false, SYSTEM_MESSAGE_TYPE.SKILLS);
                    }
                    else if (targetEntity.Type == CombatEntity.EntityType.Player)
                    {
                        Program.processor.sendSystemMessage(getAvoidanceString(((Character)targetEntity).m_player, targetEntity, entitySkill, true), ((Character)targetEntity).m_player, false, SYSTEM_MESSAGE_TYPE.SKILLS);
                    }
                    else if (attackingEntity.Type == CombatEntity.EntityType.Player)
                    {
                        Program.processor.sendSystemMessage(getAvoidanceString(((Character)attackingEntity).m_player, targetEntity, entitySkill, false), ((Character)attackingEntity).m_player, false, SYSTEM_MESSAGE_TYPE.SKILLS);
                    }
                    return 0;
                }
                //if the damage was not avoided
                else
                {
                    //time to add the damage to the official queue
                    if (theSkill.SkillID != SKILL_TYPE.POTION_OF_CONCENTRATION)
                        AddToPendingDamage(preCalculatedDamage);

                    //workout message data
                    int casterID = attackingEntity.ServerID;
                    int targetID = targetEntity.ServerID;



                    float abilityLevel = attackingEntity.getAbilityLevel(entitySkill.Template.AbilityID);
                    float statModifier = attackingEntity.getStatModifier(entitySkill.Template.PrimaryStatModifier, entitySkill.Template.PrimaryStatDivisor);

                    //int maxDamage = 0;
                    switch (entitySkill.SkillID)
                    {
                        case SKILL_TYPE.DISTRACT:
                            {

                                int aggroChange = theSkillLevel.getModifiedAmout(abilityLevel, statModifier, entitySkill, PVP);
                                targetEntity.AddToAggroValueToExistingData(attackingEntity, aggroChange);
                                break;
                            }
                        case SKILL_TYPE.TAUNT:
                        case SKILL_TYPE.WARCRY:
                            {
                                int aggroChange = theSkillLevel.getModifiedAmout(abilityLevel, statModifier, entitySkill, PVP);
                                targetEntity.AddToAggroValue(attackingEntity, aggroChange);
                                break;
                            }

                        case SKILL_TYPE.CALM:
                            {

                                int aggroChange = theSkillLevel.getModifiedAmout(abilityLevel, statModifier, entitySkill, PVP);
                                AddAggroToEntity(targetEntity, aggroChange);
                                break;
                            }
                        case SKILL_TYPE.SACRIFICE:
                            {
                                int addedEnergy = theSkillLevel.getModifiedAmout(abilityLevel, statModifier, entitySkill, PVP);
                                targetEntity.TakeEnergyDamage(addedEnergy, attackingEntity, false);
                                break;
                            }
                        case SKILL_TYPE.ENERGY_DRAIN:
                            {
                                int energyDamage = theSkillLevel.getModifiedAmout(abilityLevel, statModifier, entitySkill, PVP);
                                if (targetEntity.CurrentEnergy < energyDamage)
                                {
                                    energyDamage = targetEntity.CurrentEnergy;
                                }
                                targetEntity.TakeEnergyDamage(energyDamage, attackingEntity, true);
                                targetEntity.SetStatsChangeLevel(CombatEntity.STATS_CHANGE_LEVEL.BASIC_CHANGED);
                                attackingEntity.TakeEnergyDamage(-energyDamage, attackingEntity, false);
                                attackingEntity.SetStatsChangeLevel(CombatEntity.STATS_CHANGE_LEVEL.BASIC_CHANGED);
                                break;
                            }
                        case SKILL_TYPE.REVIVE:
                            {
                                //respawn the player
                                if (targetEntity is Character)
                                {                                    
                                    ((Character)targetEntity).Respawn(targetEntity.CurrentPosition.m_position, Character.Respawn_Type.ResSpell, -1, preCalculatedDamage.DamageTaken);
                                    //remove dismounted effect if present
                                    ((Character)targetEntity).RemoveDismountedEffect();
                                }
                                else
                                {
                                    Program.Display("POSSIBLE ERROR.CombatManager.SKILL_TYPE.REVIVE on non Character???");
                                    targetEntity.Respawn(targetEntity.CurrentPosition.m_position, Character.Respawn_Type.ResSpell, -1);
                                    //apply the healing effect
                                    AddToPendingDamage(preCalculatedDamage);
                                }                                    
                                break;
                            }
                        case SKILL_TYPE.POTION_OF_CONCENTRATION:
                            {

                                int concDamage = theSkillLevel.getModifiedAmout(abilityLevel, statModifier, entitySkill, PVP);
                                targetEntity.TakeConcentrationDamage(concDamage, attackingEntity, false);
                                break;
                            }
                        default:
                            {
                                //standard damage has already been calculated and queued in preCalculatedDamage
                                break;
                            }
                    }

                    //inflictedEffect = (targetEntity.InflictNewStatusEffect(theSkill.StatusEffectID, attackingEntity, entitySkill.ModifiedLevel, aggressive, PVP, attackingEntity.getStatModifier(theSkill.PrimaryStatModifier, theSkill.PrimaryStatDivisor)) != null);
                    CharacterEffectParams param = new CharacterEffectParams();
                    param.charEffectId = theSkill.StatusEffectID;
                    param.caster = attackingEntity;
                    param.level = entitySkill.ModifiedLevel;
                    param.aggressive = aggressive;
                    param.PVP = PVP;
                    param.statModifier = attackingEntity.getStatModifier(theSkill.PrimaryStatModifier, theSkill.PrimaryStatDivisor);
                    CharacterEffectManager.InflictNewCharacterEffect(param, targetEntity);

                    //report the status effect to the player
                    //the status effect was applied 
                    inflictedEffect = param.QueryStatusEffect(param.charEffectId) != null;
                    if ((attackingEntity.Type == CombatEntity.EntityType.Player) && (inflictedEffect == true))
                    {
                        Character character = (Character)attackingEntity;
                        if (aggressive == true)
                        {
							string locText = Localiser.GetString(textDB, character.m_player, (int)CombatManagerTextDB.TextID.INFLICTED_SKILL_ON_OTHER);
							string skillName = SkillTemplateManager.GetLocaliseSkillName(character.m_player, theSkill.SkillID);
							locText = string.Format(locText, skillName, targetEntity.Name);
							Program.processor.sendSystemMessage(locText, character.m_player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
                        }
                        else
                        {
							string locText = Localiser.GetString(textDB, character.m_player, (int)CombatManagerTextDB.TextID.APPLIED_SKILL_ON_OTHER);
							string skillName = SkillTemplateManager.GetLocaliseSkillName(character.m_player, theSkill.SkillID);
							locText = string.Format(locText, skillName, targetEntity.Name);
							Program.processor.sendSystemMessage(locText, character.m_player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
                        }

                    }
                        //the status effect succeded
                    else if (inflictedEffect == false && theSkill.StatusEffectID != EFFECT_ID.NONE)
                    {

                        if (attackingEntity.Type == CombatEntity.EntityType.Player)
                        {
                            Character character = (Character)attackingEntity;
							string locText = Localiser.GetString(textDB, character.m_player, (int)CombatManagerTextDB.TextID.OTHER_RESISTED_SKILL);
							string skillName = SkillTemplateManager.GetLocaliseSkillName(character.m_player, theSkill.SkillID);
							locText = string.Format(locText, targetEntity.Name, skillName);
							Program.processor.sendSystemMessage(locText, character.m_player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
                        }
                        else if (targetEntity.Type == CombatEntity.EntityType.Player)
                        {
                            Character character = (Character)targetEntity;
							string locText = Localiser.GetString(textDB, character.m_player, (int)CombatManagerTextDB.TextID.OTHER_INFLICT_SKILL_PLAYER_RESISTED);
							string skillName = SkillTemplateManager.GetLocaliseSkillName(character.m_player, theSkill.SkillID);
							locText = string.Format(locText, attackingEntity.Name, skillName);
							Program.processor.sendSystemMessage(locText, character.m_player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
                        }
                    }

                    double timeTillApplication = 0;
                    timeTillApplication += theSkill.ReportTime;

                    if (theSkill.ProjectileSpeed > 0)
                    {
                        double distance = (targetEntity.CurrentPosition.m_position - attackingEntity.CurrentPosition.m_position).Length();
                        if (distance > 0)
                        {
                            timeTillApplication += distance / theSkill.ProjectileSpeed;
                        }
                    }
                    double currentTime = Program.MainUpdateLoopStartTime();

                    attackingEntity.TimeActionWillComplete = currentTime + theSkill.BlockingTime;
                    if (COMBAT_TIMING_DEBUGGING == true)
                    {
                        Program.Display(attackingEntity.Name + "skill complete " + currentTime + "ready for action at " + attackingEntity.TimeActionWillComplete);
                    }

                    if ((entitySkill.SkillID == SKILL_TYPE.LIFE_DRAIN || entitySkill.SkillID == SKILL_TYPE.MOB_LIFE_DRAIN || entitySkill.SkillID == SKILL_TYPE.MOB_AREA_LIFE_DRAIN) && preCalculatedDamage.EstimatedDamage > 0)
                    {
                        int healingDone = -preCalculatedDamage.EstimatedDamage;
                        CombatDamageMessageData selfHealing = attackingEntity.TakeDamage(healingDone, healingDone, attackingEntity, ATTACK_TYPE.AOE_SKILL, (int)theSkill.SkillID, false, (int)COMBAT_REACTION_TYPES.CRT_SKILL_HIT_POS, healingDone, 0);

                        selfHealing.ApplyTime = preCalculatedDamage.ApplyTime;
                        AddToPendingDamage(selfHealing);
                    }

                    if ((attackingEntity.Type == CombatEntity.EntityType.Player))
                    {
                        int damage = preCalculatedDamage.SentDamage;
                        Character character = (Character)attackingEntity;
                        if (damage > 0)
                        {
                            if (entitySkill.IsProc == false
                                && entitySkill.FromItem == false
                                && entitySkill.SkillID != SKILL_TYPE.DEATHCAP_MUSHROOM
                                && entitySkill.SkillID != SKILL_TYPE.FIREBALL_SCROLL
                                && entitySkill.SkillID != SKILL_TYPE.BLACK_GILL_MUSHROOM
                            )
                            {
                                character.updateRanking(RankingsManager.RANKING_TYPE.LARGEST_SKILLS_HIT, damage,false);
                            }
                        }
                        else if (damage < 0)
                        {
                            if (entitySkill.SkillID == SKILL_TYPE.RECUPERATE
                                || entitySkill.SkillID == SKILL_TYPE.BANDAGE_WOUNDS
                                || entitySkill.SkillID == SKILL_TYPE.NATURES_TOUCH
                                || entitySkill.SkillID == SKILL_TYPE.LIGHT_HEAL
                                )
                            {
                                character.updateRanking(RankingsManager.RANKING_TYPE.LARGEST_HEAL, -damage,false);
                                if (targetEntity != attackingEntity)
                                {
                                    character.increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.HEALER, 1);
                                }
                            }
                        }
                    }
                }
            }


            return preCalculatedDamage.SentDamage;
        }

        /// <summary>
        /// Does the damage section of the skill
        /// This CombatDamageMessageData should then be passed into the final DoSkillDamage 
        /// Skill damage will then use this damage rather than recalculating it
        /// </summary>
        /// <param name="attackingEntity"></param>
        /// <param name="targetEntity"></param>
        /// <param name="entitySkill"></param>
        /// <param name="attackType"></param>
        /// <param name="skillStartTime"></param>
        /// <returns></returns>
        internal CombatDamageMessageData DoSkillDamageInAdvance(CombatEntity attackingEntity, CombatEntity targetEntity, EntitySkill entitySkill, ATTACK_TYPE attackType, double skillStartTime)
        {

            CombatDamageMessageData theData = null;
            //what type of entity did the damage to what
            int damage = 0;
            bool aggressive = false;
            aggressive = attackingEntity.IsEnemyOf(targetEntity);
            bool PVP = (targetEntity.Type == CombatEntity.EntityType.Player && targetEntity.IsPVP());
            SkillTemplate theSkill = entitySkill.Template;
            SkillTemplateLevel theSkillLevel = entitySkill.getSkillTemplateLevel(PVP);

            int casterID = attackingEntity.ServerID;
            int targetID = targetEntity.ServerID;

            PVP = (targetEntity.Type == CombatEntity.EntityType.Player && targetEntity.IsPVP());

            float abilityLevel = attackingEntity.getAbilityLevel(theSkill.AbilityID);
            float statModifier = attackingEntity.getStatModifier(theSkill.PrimaryStatModifier, theSkill.PrimaryStatDivisor);
            if (aggressive && targetEntity.avoidanceTest(attackingEntity, targetEntity, entitySkill))
            {
                double aggroMultiplier = theSkillLevel.AggroMultiplier;
                theData = targetEntity.TakeDamage(0, 0, attackingEntity, ATTACK_TYPE.SKILL, (int)entitySkill.SkillID, true, (int)COMBAT_REACTION_TYPES.CRT_SKILL_AVOIDED, 0, 0);
                theData.AggroModifier = aggroMultiplier;
                theData.ApplyTime = skillStartTime;
                return theData;
            }
            else
            {
                bool isASpecialCaseSkill = IsSpecialCaseSkillCast(entitySkill.Template);
                //special cases must be delt with at cast time 
                CalculatedDamage calcDamage = new CalculatedDamage(0, 0);
                if (!isASpecialCaseSkill)
                {
                    switch (entitySkill.SkillID)
                    {
                        case SKILL_TYPE.CRITICAL_STRIKE:
                        case SKILL_TYPE.DOUBLE_SHOT:
                            {
                                calcDamage = CalculateAttackDamage(attackingEntity, targetEntity);
                                CalculatedDamage calcDamage2 = new CalculatedDamage(0, 0);
                                if (targetEntity.Level <= theSkillLevel.getUnModifiedAmount(entitySkill, PVP))
                                {
                                    calcDamage2 = CalculateAttackDamage(attackingEntity, targetEntity);
                                }
                                calcDamage.m_calculatedDamage += calcDamage2.m_calculatedDamage;
                                calcDamage.m_preLvlReductionDamage += calcDamage2.m_preLvlReductionDamage;
                                break;
                            }
                        default:
                            {
                                if (theSkill.IncludesWeaponAttack)
                                {
                                    calcDamage = GetAttackSkillDamage(attackingEntity, targetEntity, entitySkill, abilityLevel, statModifier);
                                }
                                else
                                {
                                    calcDamage = GetMagicSkillDamage(attackingEntity, targetEntity, entitySkill, abilityLevel, statModifier);
                                }
                                break;
                            }
                    }
                }
                damage = calcDamage.m_calculatedDamage;

                #region CRITICAL SKILL

                // Must intercept damage here before it is modified
                // Check the the passed CombatEntities are valid
                int       critical  = 0;
                Character character = null;
                if (attackingEntity != null && targetEntity != null)
                {
                    // Create a flag for messaging and test if the atttacker has the ability
                    CharacterAbility criticalSkill = attackingEntity.getAbilityById(ABILITY_TYPE.CRITICAL_SKILL);

                    // Get the players character
                    if (attackingEntity is Character)
                    {
                        character = (Character)attackingEntity;
                    }

                    // Only if target does not have a gathering type, the attacker has the ability and damage has been done
                    // Added new condition to check for the abilityID - to remove those without one
                    // Added exception for DOUBLE_SHOT and CRITICAL_STRIKE skills (ID's: 252 & 212) - these skills use no ability types (ABILITY_TYPE.NA) 
                    // And were being prevented from critting - forgive me for making godawful condition plx...
                    if (targetEntity.Gathering == CombatEntity.LevelType.none && criticalSkill != null && damage != 0 && theSkill != null 
                       && (theSkill.SkillID == SKILL_TYPE.DOUBLE_SHOT || theSkill.SkillID == SKILL_TYPE.CRITICAL_STRIKE || theSkill.AbilityID != ABILITY_TYPE.NA))
                    {
                        // Create a suitable chance
                        float criticalSkillLevel = attackingEntity.getAbilityLevel(criticalSkill.m_ability_id);
                        float baseAbility        = Program.processor.m_abilityVariables.CriticalSkillBaseAbility;
                        float finalChance        = Program.processor.m_abilityVariables.CriticalSkillMaxChance * 
                                                   ((criticalSkillLevel + baseAbility) / ((criticalSkillLevel + baseAbility) + (10 * (targetEntity.Level + 3))));
                        finalChance             *= 100;
                        float criticalThreshold  = (float)(Program.getRandomDouble() * 100);

                        // Critical Skill!
                        if (criticalThreshold < finalChance)
                        {
                            float multiplier  = Program.processor.m_abilityVariables.CriticalSkillMultiplier; // multipier
                            float floatValue  = damage;                                                       // convert damage to a float
                            float floatDamage = floatValue * multiplier;                                      // multiply
                            damage            = (int)Math.Round(floatDamage, 0);                              // round
                            critical          = 1;                                                            // flag for messaging

                            // Log the new critical damage
                            if (Program.m_LogDamage)
                            {
                                Program.Display("criticalSkill = " + damage);
                            }
                        }

                        // Chance of skilling up on any attack which results in damage - check nulls and casts
                        if (character != null && targetEntity != null)
                        {
                            // Check if its a npc and that the no ability test flag is false
                            if (targetEntity is ServerControlledEntity)
                            {
                                if (!((ServerControlledEntity)targetEntity).Template.m_noAbilityTest)
                                {
                                    character.testAbilityUpgrade(criticalSkill);
                                }
                            }
                            // Allow chance to skill up if target is a character
                            if (targetEntity is Character)
                            {
                                character.testAbilityUpgrade(criticalSkill);
                            }
                        }
                    }
                }

                // Alter the damage
                int altereddamage = AlterDamageDueToEffects(targetEntity, damage, false);
                int sentDamage    = calcDamage.GetAmendedOriginalDamage(altereddamage);

                // Send message to local players (using sentDamage)
                if (critical == 1 && character != null && sentDamage != 0)
                {
                    // SS-16 - add say messages when abilities proc //
                    string playerName    = string.Empty; // players anme
                    int abilityID = -1; // ability id
					//string abilityName = string.Empty;
					string messageString = string.Empty; // final message string

                    // Get the players name
                    playerName = character.m_player.m_activeCharacter.Name;

					// Get the skills name 
					if (theSkill != null)
					{
						abilityID = (int)theSkill.SkillID;
						//abilityName = theSkill.SkillName
					}

                    // Create the message string - "PlayerName lands a critical SpellName for xxx damage/healing!"
					LocaliseParams locParams = null;
					if (sentDamage > 0)
					{
						locParams = new LocaliseParams(textDB, (int)CombatManagerTextDB.TextID.OTHER_LANDS_CTITICAL_ABILITY_DAMAGE, playerName, abilityID, sentDamage);
					}
					else
					{
						locParams = new LocaliseParams(textDB, (int)CombatManagerTextDB.TextID.OTHER_LANDS_CTITICAL_ABILITY_HEALING, playerName, abilityID, -sentDamage);
					}

                    // Send the message to nearby players
					attackingEntity.CurrentZone.SendLocalSkillDamageMessageLocalised(locParams, attackingEntity.CurrentPosition.m_position, Zone.LOCAL_MESSAGE_RANGE);


					//// Create the message string - "PlayerName lands a critical SpellName for xxx damage/healing!"
					//messageString = string.Format("{0} lands a critical {1} for {2} {3}!",
					//																	   playerName,                                  // players name
					//																	   abilityName,                                 // the skill /status effect name
					//																	   (sentDamage > 0 ? sentDamage : -sentDamage), // negative damage is healing - but dont show as negative!
					//																	   (sentDamage > 0 ? "damage" : "healing"));    // as above - add correct ending

					//// Send the message to nearby players
					//attackingEntity.CurrentZone.SendLocalAbilityMessage(messageString, attackingEntity.CurrentPosition.m_position, Zone.LOCAL_MESSAGE_RANGE);
                }

                #endregion

                COMBAT_REACTION_TYPES reaction = COMBAT_REACTION_TYPES.CRT_SKILL_HIT_AGG;
                if (aggressive == false)
                {
                    reaction = COMBAT_REACTION_TYPES.CRT_SKILL_HIT_POS;
                }
                double aggroMultiplier = theSkillLevel.AggroMultiplier;
                theData = targetEntity.TakeDamage(damage, altereddamage, attackingEntity, attackType, (int)theSkill.SkillID, aggressive, (int)reaction, sentDamage, critical);
                theData.AggroModifier = aggroMultiplier;
                //work out the Time To apply the damage
                double timeTillApplication = 0;
                timeTillApplication += theSkill.ReportTime;

                if (theSkill.ProjectileSpeed > 0)
                {
                    double distance = (targetEntity.CurrentPosition.m_position - attackingEntity.CurrentPosition.m_position).Length();
                    if (distance > 0)
                    {
                        timeTillApplication += distance / theSkill.ProjectileSpeed;
                    }
                }
                double currentTime = skillStartTime;
                theData.ApplyTime = currentTime + timeTillApplication;
                attackingEntity.TimeActionWillComplete = currentTime + theSkill.BlockingTime;

                //the damage should be added to the queue when the actual skill is done
                //AddToPendingDamage(theData);


            }

            return theData;

        }

        internal int DoReflectDamage(CombatEntity attackingEntity, CombatEntity targetEntity)
        {
            int damage = 0;
            for (int i = 0; i < targetEntity.m_currentCharacterEffects.Count; i++)
            {

                StatusEffect currentEffect = targetEntity.m_currentCharacterEffects[i].StatusEffect;
                switch (currentEffect.Template.EffectType)
                {
                    case EFFECT_TYPE.DAMAGE_SHIELD:
                        {
                            int maxDamage = currentEffect.m_effectLevel.getModifiedAmount(currentEffect.CasterAbilityLevel, currentEffect.StatModifier);

                            int maxDefence = 0;
                            int fireDefence = attackingEntity.GetBonusType((int)currentEffect.Template.DamageType);
                            maxDefence += fireDefence;
                            
                            CalculatedDamage calcDamage = DamageCalculator.CalculateDamage(true, false, maxDamage, maxDefence, targetEntity, attackingEntity); // this is the opposite way around because the attacker is being huyrt by this spell
                            
                            attackingEntity.AttemptToReduceDamage((int)currentEffect.Template.DamageType, calcDamage);
                            damage = calcDamage.m_calculatedDamage;// Program.calcSavedDamage(maxDamage, maxDefence, targetEntity.m_level, targetEntity);
                            int altereddamage = AlterDamageDueToEffects(attackingEntity, damage, false);
                            int sentDamage = calcDamage.GetAmendedOriginalDamage(altereddamage);
                            COMBAT_REACTION_TYPES reaction = COMBAT_REACTION_TYPES.CRT_STATUS_HIT_AGG;

                            CombatDamageMessageData newDamage = attackingEntity.TakeDamage(damage, altereddamage, targetEntity, ATTACK_TYPE.STATUS_EFFECT, (int)currentEffect.Template.StatusEffectID, true, (int)reaction, sentDamage, 0);

                            //apply immediatly
                            newDamage.ApplyTime = Program.MainUpdateLoopStartTime();
                            AddToPendingDamage(newDamage);
                            break;
                        }
                }
            }
            return damage;
        }

        internal int AlterDamageDueToEffects(CombatEntity targetEntity, int initialDamage, bool degradeStatusEffect)
        {
            int damage = initialDamage;
            if (damage <= 0)
            {
                return damage;
            }
            for (int i = 0; i < targetEntity.m_currentCharacterEffects.Count; i++)
            {

                StatusEffect currentEffect = targetEntity.m_currentCharacterEffects[i].StatusEffect;
                if (currentEffect == null || currentEffect.Template == null)
                    continue;

                switch (currentEffect.Template.EffectType)
                {
                    case EFFECT_TYPE.ENERGY_SHIELD:
                        {

                            float abilityLevel = currentEffect.CasterAbilityLevel;

                            int maxEnergyDamage = currentEffect.m_effectLevel.getModifiedAmount(abilityLevel, currentEffect.StatModifier) * damage;

                            int currentEnergy = targetEntity.CurrentEnergy;

                            float amountMissed = 0.0f;
                            if (maxEnergyDamage > currentEnergy)
                            {
                                int energyDamageMissed = maxEnergyDamage - currentEnergy;
                                amountMissed = energyDamageMissed / maxEnergyDamage;
                                if (degradeStatusEffect == true)
                                {
                                    currentEffect.Complete();
                                }
                            }

                            damage = (int)(damage * amountMissed);

                            if (degradeStatusEffect == true)
                            {
                                targetEntity.TakeEnergyDamage(maxEnergyDamage, targetEntity, false);

                            }
                            //targetEntity.TakeDamage(damage, targetEntity, ATTACK_TYPE.STATUS_EFFECT, (int)currentEffect.Template.StatusEffectID, true);

                            break;
                        }
                    case EFFECT_TYPE.ENERGY_SHIELD_2:
                        {

                            float abilityLevel = currentEffect.CasterAbilityLevel;

                            int maxEnergyDamage = currentEffect.m_effectLevel.getModifiedAmount(abilityLevel, currentEffect.StatModifier);

                            int currentEnergy = targetEntity.CurrentEnergy;

                            float amountMissed = 0.0f;
                            if (maxEnergyDamage < currentEffect.CurrentAmount + damage)
                            {
                                int energyDamageMissed = ((int)currentEffect.CurrentAmount + damage) - maxEnergyDamage;
                                amountMissed = energyDamageMissed;//energyDamageMissed / maxEnergyDamage;
                                if (degradeStatusEffect == true)
                                {
                                    currentEffect.Complete();
                                }
                            }
                            if (degradeStatusEffect == true)
                            {
                                currentEffect.CurrentAmount += damage;
                            }

                            damage = (int)amountMissed;// (damage * amountMissed);


                            //targetEntity.TakeEnergyDamage(maxEnergyDamage, targetEntity, false);
                            //targetEntity.TakeDamage(damage, targetEntity, ATTACK_TYPE.STATUS_EFFECT, (int)currentEffect.Template.StatusEffectID, true);

                            break;
                        }
                }
            }
            return damage;
        }


        #endregion

        #region Networking

        internal void SendBattleUpdateMessage(Character centralCharacter)
        {
            if (centralCharacter == null)
            {
                return;
            }
            List<CombatEntity> entitiesInRange = new List<CombatEntity>();

            for (int currentEntityIndex = 0; currentEntityIndex < m_entitiesInCombat.Count; currentEntityIndex++)
            {
                CombatEntity currentEntity = m_entitiesInCombat[currentEntityIndex];
                if ((currentEntity != null) &&
                    (currentEntity.AttackTarget != null) &&
                    (!currentEntity.Dead) &&
                    (currentEntity != centralCharacter))
                {
                    double distToTargetSqr = Utilities.Difference2DSquared(currentEntity.CurrentPosition.m_position, centralCharacter.CurrentPosition.m_position);
                    if (distToTargetSqr < Character.SQUARED_POSITION_SEND_DIST)
                    {
                        entitiesInRange.Add(currentEntity);
                    }
                }
            }
            if (entitiesInRange.Count == 0)
            {
                centralCharacter.AreaBattleUpdateSent();
                return;
            }
            NetOutgoingMessage battleMsg = Program.Server.CreateMessage();
            battleMsg.WriteVariableUInt32((uint)NetworkCommandType.AreaBattleUpdate);
            battleMsg.WriteVariableInt32(entitiesInRange.Count);
            for (int currentAttackingIndex = 0; currentAttackingIndex < entitiesInRange.Count; currentAttackingIndex++)
            {
                CombatEntity currentAttackerEntity = entitiesInRange[currentAttackingIndex];
                CombatEntity currentTargetEntity = currentAttackerEntity.AttackTarget;

                //attacker info
                battleMsg.Write((byte)currentAttackerEntity.Type);
                battleMsg.WriteVariableInt32(currentAttackerEntity.ServerID);
                //target info
                if (currentTargetEntity != null)
                {
                    battleMsg.Write((byte)currentTargetEntity.Type);
                    battleMsg.WriteVariableInt32(currentTargetEntity.ServerID);
                }
                else
                {//if something went wronge send a decoy
                    battleMsg.Write((byte)currentAttackerEntity.Type);
                    battleMsg.WriteVariableInt32(-1);
                }
            }
            centralCharacter.AreaBattleUpdateSent();
            Program.processor.SendMessage(battleMsg, centralCharacter.m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.AreaBattleUpdate);
        }

        #endregion

        public void RemoveAllReferenceToEntity(CombatEntity entityToRemove)
        {
			
            for (int currentAttacker = m_entitiesInCombat.Count - 1; currentAttacker >= 0; currentAttacker--)
            {
                CombatEntity currentEntity = m_entitiesInCombat[currentAttacker];
                //if someones casting a skill on them stop it
                //if someone is attacking them or they are attacking something then remove them
                if ((currentEntity == entityToRemove)
                    || (currentEntity.AttackTarget == entityToRemove))
                {
                    
                    if (currentEntity.AttackTarget != null)
                    {
                        StopAttacking(currentEntity);
                        currentEntity.AttackTarget = null;                        
                    }

                }
                if (currentEntity.NextSkillTarget == entityToRemove)
                {
                    EntitySkill entitySkill = currentEntity.NextSkill;
                    //if the mob has a next skill it must be told that it has failed
                    if (entitySkill != null && currentEntity.Type == CombatEntity.EntityType.Mob)
                    {
                        currentEntity.SkillFailedConditions();
                        Program.Display(currentEntity.GetIDString() + " reset NextSkill");
                    }
                    //cancel This Skill and inform the player
                    if ((currentEntity.Type == CombatEntity.EntityType.Player) && (entitySkill != null))
                    {
                        Character character = (Character)currentEntity;
                        character.SendSkillUpdate((int)entitySkill.SkillID, entitySkill.SkillLevel, 0.0f);
                    }
                    currentEntity.NextSkill = null;
                    currentEntity.NextSkillTarget = null;
                }
                if (currentEntity.CurrentSkillTarget == entityToRemove)
                {
                    EntitySkill entitySkill = currentEntity.CurrentSkill;
                    if (entitySkill != null)
                    {
                        currentEntity.SkillFailedConditions();
                        Program.Display(currentEntity.GetIDString() + " reset CurrentSkill");
                    }
                    //cancel This Skill and inform the player
                    if ((currentEntity.Type == CombatEntity.EntityType.Player) && (entitySkill != null))
                    {
                        Character character = (Character)currentEntity;
                        character.SendSkillUpdate((int)entitySkill.SkillID, entitySkill.SkillLevel, 0.0f);
                    }
                    currentEntity.CurrentSkillTarget = null;
                    currentEntity.CurrentSkill = null;
                }
                if (currentEntity == entityToRemove)
                {

                    m_entitiesInCombat.RemoveAt(currentAttacker);
                }
            }
            RemoveDamageForEntity(entityToRemove);
        }
	    
    }
}
