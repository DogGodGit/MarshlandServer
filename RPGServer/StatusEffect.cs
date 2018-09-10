using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms.VisualStyles;
using MainServer.Combat;
using MainServer.partitioning;
using MainServer.Localise;

namespace MainServer
{
    class StatusEffect
    {
		// #localisation
		public class StatusEffectTextDB : TextEnumDB
		{
			public StatusEffectTextDB() : base(nameof(StatusEffect), typeof(TextID)) { }

			public enum TextID
			{
				STATUS_REMOVED,									// "{statusEffectName0} removed"
				OTHER_LANDS_CTITICAL_ABILITY_DAMAGE,			// "{name0} lands a critical {abilityName1} for {damage2} damage!"
				OTHER_LANDS_CTITICAL_ABILITY_HEALING,			// "{name0} lands a critical {abilityName1} for {damage2} healing!"
			}
		}
		public static StatusEffectTextDB textDB = new StatusEffectTextDB();

		static int RECURING_EFFECT_TIME = 5;

        #region variables
        /// <summary>
        /// An amount held by the status effect to grant extra end conditions
        /// </summary>
        float m_currentAmount=0;
        /// <summary>
        /// The time of the application during the last update
        /// </summary>
        double m_timeAtLastUpdate = 0;
        /// <summary>
        /// Time since the Health was last modified
        /// </summary>
        float m_timeSinceLastModification=0;

        /// <summary>
        /// The Server time when the status effect started
        /// </summary>
        double m_startTime;

        private bool timerNotStarted = true;

        /// <summary>
        /// The status effect template that holds all of the data concerned 
        /// with how this status effect behaves
        /// </summary>
        StatusEffectTemplate m_template;

        public StatusEffectLevel m_effectLevel;
        public int m_statusEffectLevel = 0;
        /// <summary>
        /// TheCombatEntity which is under this effect
        /// </summary>
        CombatEntity m_owner=null;
        /// <summary>
        /// the Entity which inflicted the status effect
        /// </summary>
        CombatEntity m_theCaster=null;

        int m_casterLevel = 0;
        float m_casterAbilityLevel = 0;
        bool m_dormant = false;
        float m_statModifier = 0;
        #endregion //variables

        #region Properties

        /// <summary>
        /// An amount held by the status effect to grant extra end conditions
        /// </summary>
        internal float CurrentAmount
        {
            set
            {
                m_currentAmount = value;
            }
            get
            {
                return m_currentAmount;
            }
        }

        /// <summary>
        /// The Server time when the status effect started
        /// </summary>
        internal double StartTime
        {
            set { m_startTime = value; }
            get { return m_startTime; }
        }

        /// <summary>
        /// The status effect template that holds all of the data concerned 
        /// with how this status effect behaves
        /// </summary>
        internal StatusEffectTemplate Template
        {
            get { return m_template; }
        }

        /// <summary>
        /// the Entity which inflicted the status effect
        /// </summary>
        internal CombatEntity TheCaster
        {
            get { return m_theCaster; }
        }

        internal int CasterLevel
        {
            get { return m_casterLevel; }
            set { m_casterLevel = value; }
        }
        internal float CasterAbilityLevel
        {
            get { return m_casterAbilityLevel; }
            set { m_casterAbilityLevel = value; }
        }
        internal bool Dormant
        {
            get { return m_dormant; }
            set { m_dormant = value; }
        }
        internal float StatModifier
        {
            get { return m_statModifier; }
            set { m_statModifier = value; }
        }
        internal double GetTimeSinceStart()
        {
            return (m_timeAtLastUpdate - m_startTime);
        }

        internal bool PVP { get; set; }

        #endregion //properties

        #region initialisation

        internal StatusEffect(double startTime, StatusEffectTemplate statusEffectTemplate, int level, CombatEntity owner, CombatEntity caster, bool pvpFlag, float statModifier)
        {
            m_startTime         = startTime;
            m_template          = statusEffectTemplate;
            m_effectLevel       = m_template.getEffectLevel(level,pvpFlag);
            m_owner             = owner;
            m_timeAtLastUpdate  = Program.MainUpdateLoopStartTime();
            m_theCaster         = caster;
            m_statusEffectLevel = level;
            SetLevelsForCaster(m_theCaster);
            StatModifier        = statModifier;
	        timerNotStarted     = true;
            PVP                 = pvpFlag;
        }
        #endregion //initialisation

        /// <summary>
        /// called to set up a status effect which has been in limbo/saved to db
        /// </summary>
        internal void StartStatusEffectFromSleep(double currentTime)
        {
            if (m_template == null)
            {
                return;
            }

            double duration = m_effectLevel.m_duration;
            double timeRemaining = (m_startTime - duration) - m_timeAtLastUpdate;
            if (timeRemaining > 0)
            {
                m_startTime = currentTime - (duration - timeRemaining);
            }
            m_timeAtLastUpdate = currentTime;
            m_timeSinceLastModification=0;

            // Need to flag this correctly otherwise status effect will restart
            // And you get full duration again when you log in
            timerNotStarted = false; 
        }

        internal bool UpdateEffect(double currentTime)
        {
            // If the combat manager is null there will be no way to do the damage, so don't check yet
            if (m_owner == null || m_owner.TheCombatManager == null)
            {
                return false;
            }

            // Note - flag must be set in above method startfromsleep() for loading status effects from the db
            if (timerNotStarted == true)
            {
                timerNotStarted = false;
                m_startTime = Program.MainUpdateLoopStartTime();
            }

            float timeSinceLastUpdate= (float)(currentTime - m_timeAtLastUpdate );
            m_timeAtLastUpdate = currentTime;

            bool damageInflicted = false;
            bool statsUpdated    = false;
            bool triggerEffect   = false;

            m_timeSinceLastModification += timeSinceLastUpdate;

            // Status effects may have a custom tick rate
            float updateTime = m_template.TickRate > 0 ? m_template.TickRate : RECURING_EFFECT_TIME;
            if (m_timeSinceLastModification > updateTime)
            {
                m_timeSinceLastModification -= updateTime;
                triggerEffect = true;
            }

            bool currentInterrupted = false;
            int damageEffectID = (int)m_template.StatusEffectID;

            if (m_template.ItemOnly)
            {
                damageEffectID = -1;
            }

            if (m_template.DormantOnAggressive)
            {
                Dormant = m_owner.ConductingHostileAction();
                if (m_owner.InCombat)
                {
                    Dormant = true;
                }
            }
            
            if (m_template.BreakOnAggressive && m_owner.ConductingHostileAction())
            {
                bool removeCondition = true;

                // If it's hide and the action is sneaky attack, let it pass
                if (Template.EffectType == EFFECT_TYPE.HIDE)
                {
                    if (m_owner.AttackTarget==null && m_owner.CurrentSkill != null && m_owner.CurrentSkill.SkillID == SKILL_TYPE.SNEAKY_ATTACK)
                    {
                        removeCondition = false;
                    }
                }
                if (removeCondition)
                {
                    Complete();
                    currentInterrupted = true;
                }
                
            }
            else
            {
                if (Dormant)
                {
                    triggerEffect = false;
                }
                switch (m_template.EffectType)
                {
                    case EFFECT_TYPE.HEALTH_REGEN:
                        {
                            if (triggerEffect)
                            {
                                int healing = m_effectLevel.getModifiedAmount(m_casterAbilityLevel, StatModifier);//james bad fudge
                                if (m_owner.StatusPreventsActions.Regen == false || healing < 0)
                                {
                                    CombatManager.COMBAT_REACTION_TYPES reaction = CombatManager.COMBAT_REACTION_TYPES.CRT_STATUS_HIT_POS;
                                    int damageAmount = -healing;

                                    #region CRITICAL SKILL

                                    // CRITICAL SKILL
                                    // Must intercept damage here before it is modified
                                    // Create a flag for messaging and test if the atttacker has the ability
                                    int critical       = 0;
                                    int criticalDamage = CheckForCriticalSkill(m_theCaster, m_owner, damageAmount);

                                    // If it is critical, flag and set new damage - then send an ability message
                                    if (criticalDamage != damageAmount)
                                    {
                                        critical     = 1;
                                        damageAmount = criticalDamage;
                                        SendLocalCriticalMessage(m_theCaster, damageAmount, m_template.StatusEffectID);
                                    }

                                    #endregion

                                    CombatDamageMessageData newDamage = m_owner.TakeDamage(damageAmount, damageAmount, m_theCaster, CombatManager.ATTACK_TYPE.STATUS_EFFECT, damageEffectID, false, (int)reaction, damageAmount, critical);
                                    newDamage.ApplyTime = Program.MainUpdateLoopStartTime();
                                    m_owner.TheCombatManager.AddToPendingDamage(newDamage);
                                    damageInflicted = true;
                                }
                            }
                            break;
                        }
                    case EFFECT_TYPE.ENERGY_REGEN:
                        {
                            if (triggerEffect && m_owner.StatusPreventsActions.Regen == false)
                            {
                                int healing = m_effectLevel.getModifiedAmount(m_casterAbilityLevel, StatModifier);
                                if (m_owner.StatusPreventsActions.Regen == false || healing < 0)
                                {
                                    m_owner.TakeEnergyDamage(-healing, m_theCaster, false);
                                    m_owner.SetStatsChangeLevel(CombatEntity.STATS_CHANGE_LEVEL.BASIC_CHANGED);
                                    damageInflicted = true;
                                    statsUpdated = true;
                                }
                            }
                            break;
                        }
                    case EFFECT_TYPE.CONCENTRATION_REGEN:
                        {
                            if (triggerEffect && m_owner.StatusPreventsActions.Regen == false)
                            {
                                int healing = m_effectLevel.getModifiedAmount(m_casterAbilityLevel, StatModifier);
                                if (m_owner.StatusPreventsActions.Regen == false || healing < 0)
                                {
                                    m_owner.TakeConcentrationDamage(healing, m_theCaster, false);
                                    m_owner.SetStatsChangeLevel(CombatEntity.STATS_CHANGE_LEVEL.BASIC_CHANGED);
                                    damageInflicted = true;
                                    statsUpdated = true;
                                }
                            }
                            break;
                        }
                    case EFFECT_TYPE.HEALTH_AND_ENERGY_REGEN:
                        {
                            if (triggerEffect && m_owner.StatusPreventsActions.Regen == false)
                            {
                                int healing = m_effectLevel.getModifiedAmount(m_casterAbilityLevel, StatModifier);
                                if (m_owner.StatusPreventsActions.Regen == false || healing < 0)
                                {
                                    CombatManager.COMBAT_REACTION_TYPES reaction = CombatManager.COMBAT_REACTION_TYPES.CRT_STATUS_HIT_POS;
                                    int damageAmount = -healing;
                                    CombatDamageMessageData newDamage = m_owner.TakeDamage(damageAmount, damageAmount, m_theCaster, CombatManager.ATTACK_TYPE.STATUS_EFFECT, -damageEffectID, false, (int)reaction, damageAmount, 0);
                                    newDamage.ApplyTime = Program.MainUpdateLoopStartTime();
                                    m_owner.TheCombatManager.AddToPendingDamage(newDamage);

                                    m_owner.TakeEnergyDamage(-healing, m_theCaster, false);
                                    m_owner.SetStatsChangeLevel(CombatEntity.STATS_CHANGE_LEVEL.BASIC_CHANGED);
                                    damageInflicted = true;
                                    statsUpdated = true;
                                }
                            }
                            break;
                        }
                    case EFFECT_TYPE.DOT:
                        {
                            if (triggerEffect)
                            {
                                int maxDamage = m_effectLevel.getModifiedAmount(m_casterAbilityLevel,StatModifier);
                                damageInflicted = doAggressiveStatusEffectDamage(maxDamage);
                            }
                            break;
                        }
                    case EFFECT_TYPE.AURA:
                        {
                            if (triggerEffect)
                            {
                                // Get the type of targets for this aura
                                ZonePartition.ENTITY_TYPE typeToLookFor = (ZonePartition.ENTITY_TYPE)m_template.AuraEffect.TypeToLookFor;

                                // If the type is 'mobs' (offensive) or 'enemies' (npc offensive) then exclude the caster
                                // Otherwise 'players' or 'not enemy' will not exclude (also affect) the caster (null)
                                CombatEntity entityToExclude = null;
                                if (typeToLookFor == ZonePartition.ENTITY_TYPE.ET_MOB || typeToLookFor == ZonePartition.ENTITY_TYPE.ET_ENEMY)
                                {
                                    entityToExclude = m_theCaster;
                                }

                                // Get the combat entities which are valid target for the aura status effect
                                List<CombatEntity> auraTargets = new List<CombatEntity>();
                                m_owner.CurrentZone.PartitionHolder.AddEntitiesInRangeToList(m_owner, m_owner.CurrentPosition.m_position, m_template.AuraEffect.Radius, auraTargets, typeToLookFor, entityToExclude);

                                for (int currentAuraIndex = 0; currentAuraIndex < auraTargets.Count; currentAuraIndex++)
                                {
                                    CombatEntity currentAuraTarget = auraTargets[currentAuraIndex];

                                    // Skip over any gathering type entites
                                    if (currentAuraTarget.Gathering != CombatEntity.LevelType.none)
                                    {
                                        continue;
                                    }

                                    // When targeting mobs skip over mobs which are friendly to the caster
                                    if (typeToLookFor == ZonePartition.ENTITY_TYPE.ET_MOB && currentAuraTarget.IsAllyOf(m_theCaster))
                                    {
                                        continue;
                                    }

                                    // Skip entities not in group if flagged
                                    if (m_template.AuraEffect.GroupOnly)
                                    {
                                        if (currentAuraTarget.IsInPartyWith(m_theCaster) == false)
                                        {
                                            continue;
                                        }
                                    }

                                    if (currentAuraTarget.CurrentHealth > 0)
                                    {
                                        int auraEffectID = m_template.AuraEffect.CharacterEffectID;

                                        // Determine aggressive flag
                                        bool aggressive = false;
                                        if (m_theCaster != null)
                                        {
                                            aggressive = m_theCaster.IsEnemyOf(currentAuraTarget);
                                        }

                                        // Inflict the aura sub-effect
                                        CharacterEffectManager.InflictNewCharacterEffect(new CharacterEffectParams
                                        {
                                            charEffectId = (EFFECT_ID)m_template.AuraEffect.CharacterEffectID,
                                            caster       = m_theCaster,
                                            level        = m_statusEffectLevel,
                                            aggressive   = aggressive,
                                            PVP          = PVP,
                                            statModifier = StatModifier
                                        }, currentAuraTarget);
                                    }
                                }
                            }

                            break;
                        }
                }
            }

            if (currentInterrupted)
            {
                if (m_owner.Type == CombatEntity.EntityType.Player)
                {
                    Character theCharacter = (Character)m_owner;
					string locText = Localiser.GetString(textDB, theCharacter.m_player, (int)StatusEffectTextDB.TextID.STATUS_REMOVED);
					string locStatusEffectName = StatusEffectTemplateManager.GetLocaliseStatusEffectName(theCharacter.m_player, (int)Template.StatusEffectID);
					locText = string.Format(locText, locStatusEffectName);
					Program.processor.sendSystemMessage(locText, theCharacter.m_player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
				}
            }

            return statsUpdated;
        }

        internal void EndEffect()
        {
            if (Template != null)
            {
                if (m_owner != null)
                {
                    if (Template.RequiresAppearanceUpdate)
                    {
                        switch (Template.StatusEffectID)
                        {
                            case EFFECT_ID.RAPID_SHOT:
                            case EFFECT_ID.ATT_SPD_BOOST_ELIX:
                            case EFFECT_ID.ATT_SPD_BOOST_POT:
                            case EFFECT_ID.ATTACK_SPEED_ITEM_PERM:
                                {
                                    m_owner.InfoUpdated(Inventory.EQUIP_SLOT.SLOT_ATTACK_SPEED);
                                    break;
                                }
                            case EFFECT_ID.SHRINK_POTION:
                            case EFFECT_ID.SHRINK_ELIXIR:
                            case EFFECT_ID.SHRINK_BOOST_PERM:
                            case EFFECT_ID.GROWTH_BOOST_PERM:
                            case EFFECT_ID.GROWTH_ELIXIR:
                            case EFFECT_ID.GROWTH_POTION:
                            case EFFECT_ID.YELLOW_GIANT_MUSHROOM:
                                {
                                    m_owner.InfoUpdated(Inventory.EQUIP_SLOT.SLOT_SCALE);
                                    break;
                                }
                        }
                    }
                }
            }
        }

        internal void SetLevelsForCaster(CombatEntity caster)
        {
            if (caster == null)
            {
                return;
            }

            m_casterLevel = caster.Level;
            CombatEntity casterCharacter = m_theCaster;
            m_casterAbilityLevel = casterCharacter.getAbilityLevel(m_template.CasterAbilityType);
        }

        private bool doAggressiveStatusEffectDamage(int maxDamage)
        {
            bool damageInflicted = false;
            // Do the damage
            int maxDefence = m_owner.GetBonusType((int)m_template.DamageType);

            CalculatedDamage calcDamage;
            if (m_theCaster != null)
            {
                calcDamage = DamageCalculator.CalculateDamage(true, false, maxDamage, maxDefence, m_theCaster, m_owner);
            }
            else
            {
                calcDamage = DamageCalculator.CalculateDamage(maxDamage, maxDefence, m_casterLevel, m_owner); // in the case the user has logged out, the caster is null
            }

            m_owner.AttemptToReduceDamage((int) m_template.DamageType, calcDamage);
            int damage = calcDamage.m_calculatedDamage;

            // CRITICAL SKILL
            // Must intercept damage here before it is modified
            // Create a flag for messaging and test if the atttacker has the ability
            int critical = 0;

            int criticalDamage = CheckForCriticalSkill(m_theCaster, m_owner, damage);
            if (criticalDamage != damage)
            {
                critical = 1;
                damage = criticalDamage;
            }

            int altereddamage = m_owner.TheCombatManager.AlterDamageDueToEffects(m_owner, damage,false);
            bool sendUpdateStats = altereddamage != damage && m_owner.Type == CombatEntity.EntityType.Player;

            if (damage > 0)
            {
                bool targetAlreadyDead = (m_owner.CurrentHealth <= 0);
                int sendDamage =altereddamage;
                if(calcDamage.m_preLvlReductionDamage > 0)
                {
                    sendDamage = calcDamage.GetAmendedOriginalDamage(altereddamage); //altereddamage * (damage / calcDamage.m_preLvlReductionDamage);
                }

                CombatDamageMessageData newDamage = m_owner.TakeDamage(damage, altereddamage, m_theCaster, CombatManager.ATTACK_TYPE.STATUS_EFFECT, (int)m_template.StatusEffectID, true, (int)CombatManager.COMBAT_REACTION_TYPES.CRT_STATUS_HIT_AGG, sendDamage, critical);

                // Send messages if it was a crit (using sendDamage)
                if (critical == 1)
                {
                    SendLocalCriticalMessage(m_theCaster, sendDamage, m_template.StatusEffectID);
                }

                // Apply now
                newDamage.ApplyTime = Program.MainUpdateLoopStartTime();
                m_owner.TheCombatManager.AddToPendingDamage(newDamage);
                
                if (!targetAlreadyDead && (m_owner.CurrentHealth <= 0))
                {
                    m_owner.m_killer = m_theCaster;
                }
                damageInflicted = true;
            }

            if (sendUpdateStats && m_owner.Type == CombatEntity.EntityType.Player)
            {
                m_owner.SetStatsChangeLevel(CombatEntity.STATS_CHANGE_LEVEL.BASIC_CHANGED);//((Character)m_owner).m_statsUpdated=true;
            }
            return damageInflicted;
        }

        // CheckForCriticalSkill                                                                                                                                              //
        // Function takes in the caster, target and skill damage, determines if the attack can be critical, checks for a critical skillup then returns the skill damage value //
        private int CheckForCriticalSkill(CombatEntity theCaster, CombatEntity theTarget, int skillDamage)
        {
            // Its possible for the caster/owner to be null here - like having a status effect and logging out and in - the caster ref will be gone
            // Added condition to exclude templates flagged as 'ItemOnly' and exclude templates with no ability type
            if (theCaster != null && theTarget != null && m_template != null && !m_template.ItemOnly && m_template.CasterAbilityType != ABILITY_TYPE.NA)
            {
                CharacterAbility criticalSkill = theCaster.getAbilityById(ABILITY_TYPE.CRITICAL_SKILL);

                // Get the players character
                Character  character = null;
                if (theCaster is Character)
                {
                    character = (Character)theCaster;
                }

                // Only if target does not have a gathering type, the attacker has the ability and damage has been done (negative damage = healing)
                if ((theTarget.Gathering == CombatEntity.LevelType.none) && (criticalSkill != null) && (skillDamage != 0))
                {
                    // Create a suitable chance
                    float criticalSkillLevel = theCaster.getAbilityLevel(criticalSkill.m_ability_id);
                    float baseAbility        = Program.processor.m_abilityVariables.CriticalSkillBaseAbility;
                    float finalChance        = Program.processor.m_abilityVariables.CriticalSkillMaxChance * ((criticalSkillLevel + baseAbility) /
                                               ((criticalSkillLevel + baseAbility) + (10 * (theTarget.Level + 3))));
                    finalChance             *= 100;
                    float criticalThreshold  = (float)(Program.getRandomDouble() * 100);

                    // Critical Skill!
                    if (criticalThreshold < finalChance)
                    {
                        float multiplier  = Program.processor.m_abilityVariables.CriticalSkillMultiplier; // multipier
                        float floatValue  = skillDamage;                                                  // convert damage to a float
                        float floatDamage = floatValue * multiplier;                                      // multiply
                        skillDamage       = (int)Math.Round(floatDamage, 0);                              // round

                        // Log the new critical damage
                        if (Program.m_LogDamage)
                        {
                            Program.Display("criticalSkill = " + skillDamage);
                        }
                    }

                    // Chance of skilling up on any attack which results in damage - check nulls and casts
                    if (character != null && theTarget != null)
                    {
                        // Check if its a npc and that the no ability test flag is false
                        if (theTarget is ServerControlledEntity)
                        {
                            if (!((ServerControlledEntity)theTarget).Template.m_noAbilityTest)
                            {
                                character.testAbilityUpgrade(criticalSkill);
                            }
                        }
                        // Allow chance to skill up if target is a character
                        if (theTarget is Character)
                        {
                            character.testAbilityUpgrade(criticalSkill);
                        }
                    }
                }
            }

            return skillDamage;
        }

        // SendLocalCriticalMessage                                                                                               //
        // As damage used within the CheckForCriticalSkill() function is before modification, the altered value is passed through //
        private void SendLocalCriticalMessage(CombatEntity theCaster, int skillDamage, EFFECT_ID statusEffectID)
        {
            // Send message to local players
            if (theCaster is Character)
            {
                Character character = (Character)theCaster;
                StatusEffectTemplate effectTemplate;        // effect info for its name
                string playerName    = string.Empty;        // players anme
				int statusEffectIntID = -1;
                //string abilityName   = string.Empty;        // ability name
                //string messageString = string.Empty;        // final message string

                // Get the players name
                playerName = character.m_player.m_activeCharacter.Name;

                // Get the status effects name 
                effectTemplate = StatusEffectTemplateManager.GetStatusEffectTemplateForID(statusEffectID);
                if (effectTemplate != null)
                {
					statusEffectIntID = (int)effectTemplate.StatusEffectID;
                }

				// Create the message string - "PlayerName lands a critical SpellName for xxx damage/healing!"
				LocaliseParams locParams = null;
				if (skillDamage > 0)
				{
					locParams = new LocaliseParams(textDB, (int)StatusEffectTextDB.TextID.OTHER_LANDS_CTITICAL_ABILITY_DAMAGE, playerName, statusEffectIntID, skillDamage);
				}
				else
				{
					locParams = new LocaliseParams(textDB, (int)StatusEffectTextDB.TextID.OTHER_LANDS_CTITICAL_ABILITY_HEALING, playerName, statusEffectIntID, -skillDamage);
				}

				// Send the message to nearby players
				theCaster.CurrentZone.SendLocalStatusEffectNameLocalised(locParams, theCaster.CurrentPosition.m_position, Zone.LOCAL_MESSAGE_RANGE);

				//// Create the message string - "PlayerName lands a critical SpellName for xxx damage/healing!"
				//messageString = string.Format("{0} lands a critical {1} for {2} {3}!",
				//                                                                       playerName,                                     // players name
				//                                                                       abilityName,                                    // the skill /status effect name
				//                                                                       (skillDamage > 0 ? skillDamage : -skillDamage), // negative damage is healing - but dont show as negative!
				//                                                                       (skillDamage > 0 ? "damage" : "healing"));      // as above - add correct ending

				//// Send the message to nearby players
				//theCaster.CurrentZone.SendLocalAbilityMessage(messageString, theCaster.CurrentPosition.m_position, Zone.LOCAL_MESSAGE_RANGE);
			}
        }

        internal void Complete()
        {
            m_startTime=-10000;
        }

        internal bool IsComplete()
        {
            if (m_effectLevel == null)
                return true;

            if ((m_timeAtLastUpdate - m_startTime) >= m_effectLevel.m_duration && m_effectLevel.m_duration != -1)
            {
                return true;
            }
            return false;
        }
    }
}
