using System;
using System.Collections.Generic;
using Lidgren.Network;
using MainServer.Localise;

namespace MainServer
{
    public class CharacterEffect
    {
        public CharacterEffect(int id, int subid, CHARACTER_EFFECT_TYPE type)
        {
            m_id = id;
            m_SubEffectId = subid;
            m_SubEffectType = type;
        }


        public CharacterEffect(CharacterEffect src)
        {
            m_id = src.m_id;
            m_SubEffectId = src.m_SubEffectId;
            m_SubEffectType = src.m_SubEffectType;

            m_Duration = src.m_Duration;
            m_Level = src.m_Level;

            m_damageTypes = src.m_damageTypes;
            m_bonusTypes = src.m_bonusTypes;
            m_avoidanceTypes = src.m_avoidanceTypes;
            m_additionalBonus = src.m_additionalBonus;
            m_immunityTypes = src.m_immunityTypes;
            m_damageReductionTypes = src.m_damageReductionTypes;
            m_modifiers = src.m_modifiers;
            m_CombatModifiers = src.m_CombatModifiers;

            m_armour = src.m_armour;
            m_attack_speed = src.m_attack_speed;
            m_statModifier = src.m_statModifier;

            StatusEffect = null;
        }

        internal float GetItemBonus(BONUS_TYPE bonusType)
        {
            int bonusInt = (int) bonusType;
            FloatForID currentVal = FloatForID.GetEntryForID(m_bonusTypes, bonusInt);
            if (currentVal != null)
            {
                return currentVal.m_amount;
            }
            return 0;
        }

        public enum CHARACTER_EFFECT_TYPE
        {
            NONE = 0,
            SIMPLE_STATUS_EFFECT = 1,
            COMPLEX_STATUS_EFFECT = 2
        }

        public int m_id, m_SubEffectId;
        public CHARACTER_EFFECT_TYPE m_SubEffectType;

        internal double m_Duration;
        internal int    m_TimeStarted;

        internal List<FloatForID> m_damageTypes = new List<FloatForID>();
        internal List<FloatForID> m_bonusTypes = new List<FloatForID>();
        internal List<FloatForID> m_avoidanceTypes = new List<FloatForID>();
        internal List<FloatForID> m_additionalBonus = new List<FloatForID>();
        internal List<FloatForID> m_immunityTypes = new List<FloatForID>();
        internal List<FloatForID> m_damageReductionTypes = new List<FloatForID>();
        
        internal List<CharacterModifiers> m_modifiers = new List<CharacterModifiers>();
        internal List<CombatModifiers>  m_CombatModifiers = new List<CombatModifiers>();

        internal int m_armour, m_attack_speed;
        internal int m_Level;
        internal float m_statModifier;

        internal StatusEffect StatusEffect { get; set; }

    }

    internal class CharacterEffectParams
    {
        public EFFECT_ID    charEffectId;
        public int          subEffectId;
        public CombatEntity caster;
        public int          level;
        public bool         aggressive;
        public bool         PVP;
        public float        statModifier;
        public double       overrideTimeStarted;

        public List<CharacterEffect> triggeredEffects;

        public CharacterEffectParams()
        {
            triggeredEffects = new List<CharacterEffect>();
            
            overrideTimeStarted = 0;
            subEffectId = 0;
        }

        // Queries whether a particular status effect is active
        public CharacterEffect QueryStatusEffect(EFFECT_ID effect)
        {
            return triggeredEffects.Find(x => x.m_SubEffectId == (int) effect);
        }
    }



    static class CharacterEffectManager
    {

		// #localisation
		public class CharacterEffectManagerTextDB : TextEnumDB
		{
			public CharacterEffectManagerTextDB() : base(nameof(CharacterEffectManager), typeof(TextID)) { }

			public enum TextID
			{
				EFFECT_EXPIRED,			// "{statusEffectName0} expired"
				EFFECT_EXPIRED_ON,      // "{statusEffectName0} expired on {name1}"
				GAINED_EFFECT,			// "You gained {statusEffectName0}"
			}
		}
		public static CharacterEffectManagerTextDB textDB = new CharacterEffectManagerTextDB();

        static List<CharacterEffect> m_CharacterEffectClasses;
        public static bool DebugStatus { get; set; }
        static List<CharacterEffect> effectsToRemove = new List<CharacterEffect>();

        static CharacterEffectManager()
        {
            DebugStatus = false;
        }

        static internal void AddCharacterEffectStats(CharacterEffect currentEffect,
              CombatEntityStats statusStats,
              CombatEntityStats statusStatsMultipliers,
              ref int armourValue,
              ref int attackbonus,
              ref int defencebonus,
              ref int hpbonus,
              ref int energybonus,
            ref int concentrationbonus)
        {

            if (currentEffect != null)
            {
                armourValue += currentEffect.m_armour;
                if (currentEffect.m_attack_speed > 0)
                    statusStats.AttackSpeed *= currentEffect.m_attack_speed;

                for (int j = 0; j < currentEffect.m_bonusTypes.Count; j++)
                {
                    FloatForID currentVal = currentEffect.m_bonusTypes[j];
                    statusStats.AddToBonusType(currentVal.m_bonusType, currentVal.m_amount);
                }
                for (int j = 0; j < currentEffect.m_damageTypes.Count; j++)
                {
                    FloatForID currentType = currentEffect.m_damageTypes[j];
                    statusStats.AddToWeaponDamageType(currentType.m_bonusType, currentType.m_amount);
                }
                for (int j = 0; j < currentEffect.m_avoidanceTypes.Count; j++)
                {
                    FloatForID currentType = currentEffect.m_avoidanceTypes[j];
                    statusStats.AddToAvoidanceType((AVOIDANCE_TYPE)currentType.m_bonusType, currentType.m_amount);
                }
                for (int j = 0; j < currentEffect.m_immunityTypes.Count; j++)
                {
                    FloatForID currentVal = currentEffect.m_immunityTypes[j];
                    statusStats.AddToImmunityType(currentVal.m_bonusType, currentVal.m_amount);
                }
                for (int j = 0; j < currentEffect.m_damageReductionTypes.Count; j++)
                {
                    FloatForID currentVal = currentEffect.m_damageReductionTypes[j];
                    statusStats.AddToDamageReductionType(currentVal.m_bonusType, currentVal.m_amount);
                }
                attackbonus += (int)currentEffect.GetItemBonus(BONUS_TYPE.ATTACK_BONUS);
                defencebonus += (int)currentEffect.GetItemBonus(BONUS_TYPE.DEFENCE_BONUS);
                hpbonus += (int)currentEffect.GetItemBonus(BONUS_TYPE.HEALTH_BONUS);
                energybonus += (int)currentEffect.GetItemBonus(BONUS_TYPE.ENERGY_BONUS);
                concentrationbonus += (int) currentEffect.GetItemBonus(BONUS_TYPE.CONCENTRATION_BONUS);

                //apply any other modifiers
                for (int currentModIndex = 0; currentModIndex < currentEffect.m_modifiers.Count; currentModIndex++)
                {
                    CharacterModifiers currentModifier = currentEffect.m_modifiers[currentModIndex];
                    currentModifier.ApplyToStatsAddition(statusStats);
                }
                for (int currentModIndex = 0; currentModIndex < currentEffect.m_CombatModifiers.Count; currentModIndex++)
                {
                    CombatModifiers currentModifier = currentEffect.m_CombatModifiers[currentModIndex];
                    currentModifier.ApplyCombatParam(statusStats, statusStatsMultipliers);
                }
            }
        }
        
        static public void DebugFloatId(List<FloatForID> entries, string messHeading, NetOutgoingMessage mess)
        {
            for (int j = 0; j < entries.Count; j++)
            {
                FloatForID currentVal = entries[j];
                currentVal = FloatForID.GetEntryForID(entries, currentVal.m_bonusType);
                if (currentVal != null)
                {
                    mess.Write(messHeading);
                    mess.Write(currentVal.m_amount.ToString());
                }
            }            
        }

        static public void DebugCombatEntityStats(CombatEntityStats stats, NetOutgoingMessage mess)
        {
            mess.Write((int)SERVER_DEBUG_TYPES.SERVER_DEBUG_ENTITY_STATS);           
            mess.Write(stats.GetDebugString());           
            mess.Write((int)-1);        // EOM
        }

        static public void DebugCombatStats(Character character)
        {
            if (!DebugStatus)
                return;

            NetOutgoingMessage mess = null;
            if (character.Type == CombatEntity.EntityType.Player)
            {
                mess = Program.Server.CreateMessage();
                mess.WriteVariableUInt32((uint) NetworkCommandType.ServerDebugMessage);
                mess.Write((int) -1); // EOM

                // Base stats message
                DebugCombatEntityStats(character.CompiledStats, mess);

                // 
                mess.Write((int)SERVER_DEBUG_TYPES.SERVER_DEBUG_KEY_VAR);
                mess.Write("UPD");
                mess.Write(character.GetStatsLevel().ToString());

                foreach (CharacterEffect currEffect in character.m_currentCharacterEffects)
                {
                    mess.Write((int) SERVER_DEBUG_TYPES.SERVER_DEBUG_ENTITY_EFFECTS);

                    if (mess != null)
                    {
                        double duration = 0.0f;
                        mess.Write("DURATION");
                        if (currEffect.m_Duration > 0.0f)
                            duration = currEffect.m_Duration - currEffect.StatusEffect.GetTimeSinceStart();
                        else
                            duration = (currEffect.StatusEffect.m_effectLevel.m_duration -
                                        currEffect.StatusEffect.GetTimeSinceStart());
                        mess.Write(duration.ToString("F"));

                        // id, subeffectid, subeffecttype
                        mess.Write("ID");
                        mess.Write(currEffect.m_id.ToString());

                        mess.Write("SEID");
                        mess.Write(currEffect.m_SubEffectId.ToString());

                        mess.Write("TYPE");
                        mess.Write(currEffect.m_SubEffectType.ToString());

                        if (currEffect.m_SubEffectType == CharacterEffect.CHARACTER_EFFECT_TYPE.COMPLEX_STATUS_EFFECT)
                        {
                            DebugFloatId(currEffect.m_bonusTypes, "BON", mess);
                            DebugFloatId(currEffect.m_damageTypes, "DAM", mess);
                            DebugFloatId(currEffect.m_immunityTypes, "IMM", mess);
                            DebugFloatId(currEffect.m_damageReductionTypes, "RED", mess);
                            DebugFloatId(currEffect.m_avoidanceTypes, "AVOID", mess);
                        }
                        mess.Write((int) -1); // EOM
                    }
                }
            }
            if (mess != null)
            {
                mess.Write((int)-1);        // EOM

                Program.processor.SendMessage(mess, character.m_player.connection, NetDeliveryMethod.ReliableOrdered,
                    NetMessageChannel.NMC_Normal, NetworkCommandType.ServerDebugMessage);
            }            
        }

        static private void ResetStatModifiers(CombatEntity combatEntity)
        {
            combatEntity.StatusStats.ResetStats(0);
            combatEntity.StatusStatsMultipliers.ResetStats(1);

            combatEntity.StatusPreventsActions.Reset();
            combatEntity.StatusCancelConditions.Reset();
        }

        // Apply any stats changes to the combat engine that the CharacterEffects handle.
        static public void UpdateCombatStats(CombatEntity combatEntity)
        {
            // Stat changes are pushed into the equipment stats object - 
            // this already has some values, so ensure to update correctly.
            CombatEntityStats statusStats = combatEntity.StatusStats;
            CombatEntityStats statusStatsMultipliers = combatEntity.StatusStatsMultipliers;

            ResetStatModifiers(combatEntity);

            int armourValue = 0;            
            int attackbonus = 0;
            int defencebonus = 0;
            int hpbonus = 0;
            int energybonus = 0;
            int concentrationbonus = 0;
            //bool recalculateStatModifiers = false;

            foreach (CharacterEffect currEffect in combatEntity.m_currentCharacterEffects)
            {
                switch (currEffect.m_SubEffectType)
                {
                    case CharacterEffect.CHARACTER_EFFECT_TYPE.COMPLEX_STATUS_EFFECT:
                    {
                        AddCharacterEffectStats(currEffect, statusStats, statusStatsMultipliers,
                            ref armourValue, 
                            ref attackbonus, 
                            ref defencebonus, 
                            ref hpbonus, 
                            ref energybonus,
                            ref concentrationbonus);
                        //recalculateStatModifiers = true;
                        break;
                    }
                }
            }
            //apply these to the base stats
            statusStats.Armour += armourValue;
            statusStats.Attack += attackbonus;
            statusStats.Defence += defencebonus;
            statusStats.MaxHealth += hpbonus;
            statusStats.MaxEnergy += energybonus;
            statusStats.MaxConcentrationFishing += concentrationbonus;

            //if (recalculateStatModifiers)
            //{
            combatEntity.RecalculateStatModifiers();
            //}
        }

        // Message to the player when a StatusEffect finishes.
        static void NotifyPlayerStatusEffectDone(StatusEffect effect, CombatEntity entity)
        {
            //it expired naturally
            //inform players
            if (effect.StartTime > 0)
            {
                if (entity.Type == CombatEntity.EntityType.Player)
                {
                    Character theCharacter = (Character)entity;
					string locText = Localiser.GetString(textDB, theCharacter.m_player, (int)CharacterEffectManagerTextDB.TextID.EFFECT_EXPIRED);
					string locStatusEffectName = StatusEffectTemplateManager.GetLocaliseStatusEffectName(theCharacter.m_player, (int)effect.Template.StatusEffectID);
					locText = string.Format(locText, locStatusEffectName);
					Program.processor.sendSystemMessage(locText, theCharacter.m_player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
                }
            }
            // Auras - dont report auras fading
            if (effect.TheCaster != null &&
                effect.TheCaster.Type == CombatEntity.EntityType.Player &&
                effect.Template.IsAuraSubEffect == false)
            {
                if (effect.TheCaster != entity && effect.StartTime > 0)
                {
                    Character theCasterCharacter = (Character)effect.TheCaster;

					if (theCasterCharacter.m_player != null)
					{
						string locText = Localiser.GetString(textDB, theCasterCharacter.m_player, (int)CharacterEffectManagerTextDB.TextID.EFFECT_EXPIRED_ON);
						string locStatusEffectName = StatusEffectTemplateManager.GetLocaliseStatusEffectName(theCasterCharacter.m_player, (int)effect.Template.StatusEffectID);
						locText = string.Format(locText, locStatusEffectName, entity.Name);
						Program.processor.sendSystemMessage(locText, theCasterCharacter.m_player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
					}
                }
            }            
        }

        // Update an individual CharacterEffect
        static void UpdateStatusEffect(CombatEntity entity, CharacterEffect effect, double currentTime, ref bool statsChanged, ref List<CharacterEffect> effectsToRemove)
        {
            switch (effect.m_SubEffectType)
            {
                case CharacterEffect.CHARACTER_EFFECT_TYPE.COMPLEX_STATUS_EFFECT:
                case CharacterEffect.CHARACTER_EFFECT_TYPE.SIMPLE_STATUS_EFFECT:
                {
                    // These two CharacterEffects operate through a StatusEffect
                    StatusEffect currentStatusEffect = effect.StatusEffect;
                    if (currentStatusEffect == null)
                        return;

                    // Call the function to update Status effect
                    bool currentChangesStats = currentStatusEffect.UpdateEffect(currentTime);
                    if (currentChangesStats)
                    {
                        statsChanged = true;
                    }
                    // Check wether the StatusEffect has finished - if so, remove
                    bool readyToRemove = currentStatusEffect.IsComplete();
                    if (readyToRemove)
                    {
                        // Message for the player about the effect disappearing
                        NotifyPlayerStatusEffectDone(currentStatusEffect, entity);

                        // End the Status Effect
                        currentStatusEffect.EndEffect();
                         
                        // Remove the CharacterEffect 
                        effectsToRemove.Add(effect);
                        
                        //the status List has changed so send update at earliest opportunity
                        entity.StatusListChanged = true;

                        // Trigger a re-calculation of the stats
                        entity.SetStatsChangeLevel(CombatEntity.STATS_CHANGE_LEVEL.COMPILE_REQUIRED);

                        statsChanged = true;

                        // Addition - for pets hugry status effect when food runs out we want to recheck equipment to reapply the hungry effect
                        // Only do this for players
                        if(entity.Type == CombatEntity.EntityType.Player)
                            entity.SetStatsChangeLevel(CombatEntity.STATS_CHANGE_LEVEL.EQUIPMENT_CHECK_REQUIRED);
                    }

                    break;
                }
            }
        }

        private static void FlushDudEffects(CombatEntity entity)
        {
            if (entity.m_currentCharacterEffects.Count == 0)
                return;

            for (int i = entity.m_currentCharacterEffects.Count - 1; i >= 0; --i)
            {
                if (entity.m_currentCharacterEffects[i].StatusEffect == null)
                {
                    entity.m_currentCharacterEffects.RemoveAt(i);
                }
            }
        }
       
        public static bool UpdateCharacterEffects(CombatEntity entity)
        {
            FlushDudEffects(entity);

            effectsToRemove.Clear();

            bool statsChanged = false;
            
            //let's give them one common time
            double currentTime = Program.MainUpdateLoopStartTime(); //NetTime.Now;

            // PDH - Seperate out CombatEntity logic from Character Effects
            entity.UpdateCancelConditions();

            // Update each Character effect
            for (int currentEffect = 0; currentEffect < entity.m_currentCharacterEffects.Count; currentEffect++)
            {
                CharacterEffect currCharEffect = entity.m_currentCharacterEffects[currentEffect];

                switch (currCharEffect.m_SubEffectType)
                {
                    case CharacterEffect.CHARACTER_EFFECT_TYPE.SIMPLE_STATUS_EFFECT:
                        UpdateStatusEffect(entity, currCharEffect, currentTime, ref statsChanged, ref effectsToRemove);
                        break;

                    case CharacterEffect.CHARACTER_EFFECT_TYPE.COMPLEX_STATUS_EFFECT:
                        UpdateStatusEffect(entity, currCharEffect, currentTime, ref statsChanged, ref effectsToRemove);
                        break;
                }

                // Handle CharacterEffects with a duration
                if (currCharEffect.m_Duration > 0.0f)
                {
                    if ((currCharEffect.m_TimeStarted + currCharEffect.m_Duration) < currentTime)
                    {
                        // CharacterEffect timed out
                        effectsToRemove.Add(currCharEffect);
                    }
                }

            }

            //just iterate through once for all effects we want to remove
            for (int i = effectsToRemove.Count - 1; i >= 0; i--)
            {
                // Flag to recalculate stats, as we're removing effects
                entity.StatusListChanged = true;

                switch (effectsToRemove[i].m_SubEffectType)
                {
                    case CharacterEffect.CHARACTER_EFFECT_TYPE.COMPLEX_STATUS_EFFECT:
                    case CharacterEffect.CHARACTER_EFFECT_TYPE.SIMPLE_STATUS_EFFECT:
                        {
                            // Notify player
                            //NotifyPlayerStatusEffectDone(characterEffect.StatusEffect, entity);
                            // Finish the status effect.
                            effectsToRemove[i].StatusEffect.EndEffect();
                            break;
                        }
                }
                // remove the entries from the current Character effects list
                entity.m_currentCharacterEffects.Remove(effectsToRemove[i]);
            }


            if (entity.StatusListChanged)
            {
                statsChanged = true;
                FlushDudEffects(entity);
            }

            return statsChanged;
        }

        static internal void AddValueToString(Object obj, ref string dest, char seperator)
        {
            dest += obj.ToString() + seperator;
        }
        

        static public string CreateStringForEffects(CombatEntity entity)
        {
            if (entity == null || entity.m_currentCharacterEffects == null)
                return "";

            //string layout:statusEffect1ID^statusEffect1TimeRemaining,statusEffect2ID^statusEffect2TimeRemaining,...
            //string layout:skill1ID^skill1Level,skill2ID^skill2Level,...

            string statusString = "NEW:";
            //get the current time
            double currentTime = Program.MainUpdateLoopStartTime(); //NetTime.Now;

            foreach (var effect in entity.m_currentCharacterEffects)
            {
                if (effect.StatusEffect == null)
                    continue;

                bool store = false;
                int currentStatusEffectTimeRemaining = (int)(effect.StatusEffect.m_effectLevel.m_duration - (currentTime - effect.StatusEffect.StartTime));

                if (effect.m_Duration > 0.0f)
                {
                    currentStatusEffectTimeRemaining = (int)(effect.m_Duration - (currentTime - effect.m_TimeStarted));
                    if (currentStatusEffectTimeRemaining > 0)
                        store = true;
                }
                else
                {
                    // Get the duration of this effect from its template, accounting for the level cast.
                    StatusEffectTemplate currStatusTemplate = StatusEffectTemplateManager.GetStatusEffectTemplateForID((EFFECT_ID) effect.m_SubEffectId);
                    if (currStatusTemplate == null)
                        continue;

                    StatusEffectLevel effLevel =
                        (currStatusTemplate.getEffectLevel(effect.StatusEffect.m_statusEffectLevel, false));
                    // Calculate how much longer
                    currentStatusEffectTimeRemaining = (int)(effect.StatusEffect.m_effectLevel.m_duration - (currentTime - effect.StatusEffect.StartTime));

                    if (currentStatusEffectTimeRemaining > 0)
                        store = true;
                }

                if(store)
                {
                    // Save out the base information for the Character effect.
                    AddValueToString(effect.m_id, ref statusString, '^');
                    AddValueToString(effect.m_SubEffectId, ref statusString, '^');
                    AddValueToString(effect.m_Level, ref statusString, '^');
                    AddValueToString(currentStatusEffectTimeRemaining, ref statusString, '^');
                    AddValueToString(effect.m_statModifier, ref statusString, ',');
                }
            }          
            return statusString;            
        }

        static public void PopulateEffectsFromString(CombatEntity entity, string effectString)
        {
            if (entity == null || effectString == null)
                return;

            const string newHeader = "NEW:";
            //get the current time
            double currentTime = Program.MainUpdateLoopStartTime(); //NetTime.Now;

            if (effectString.StartsWith(newHeader))
            {
                //
                // NEW STRING FORMAT : Load an new format string. 
                //                
                effectString = effectString.Remove(0, newHeader.Length);

                //split up all of the character effects 
                string[] indEffect = effectString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < indEffect.Length && i < entity.SkillList.Length; i++)
                {
                    //separate ID and Time remaining
                    string[] effectParts = indEffect[i].Split(new char[] {'^'}, StringSplitOptions.RemoveEmptyEntries);

                    //check it has split correctly
                    if (effectParts.Length == 5)
                    {
                        int characterEffectID = Int32.Parse(effectParts[0]);
                        int subEffectID = Int32.Parse(effectParts[1]);
                        int level = Int32.Parse(effectParts[2]);
                        int timeRemaining = Int32.Parse(effectParts[3]);
                        float statModifier = (float) Double.Parse(effectParts[4]);

                        StatusEffectTemplate currentEffectTemplate = StatusEffectTemplateManager.GetStatusEffectTemplateForID((EFFECT_ID)subEffectID);
                        if (currentEffectTemplate == null)
                            continue;

                        double fullDuration = (int)currentEffectTemplate.getEffectLevel(level, false).m_duration;
                        //work out when the status effect should have started
                        double timeStarted = (int)currentTime - (fullDuration - (float)timeRemaining);

                        CharacterEffectParams param = new CharacterEffectParams();
                        param.charEffectId = (MainServer.EFFECT_ID) characterEffectID;
                        param.subEffectId = subEffectID;
                        param.caster = null;
                        param.level = level;
                        param.aggressive = false;
                        param.PVP = false;
                        param.statModifier = statModifier;
                        param.overrideTimeStarted = timeStarted;

                        InflictNewCharacterEffect(param, entity);
                    }
                }
            }
            else
            {
                //
                // OLD STRING FORMAT : Load an old format string. Will end up saved as a new format.
                //

                //string layout:statusEffect1ID^statusEffect1TimeRemaining,statusEffect2ID^statusEffect2TimeRemaining,...
                //split up all of the status effects 
                string[] indEffect = effectString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < indEffect.Length && i < entity.SkillList.Length; i++)
                {
                    //separate ID and Time remaining
                    string[] effectParts = indEffect[i].Split(new char[] {'^'}, StringSplitOptions.RemoveEmptyEntries);

                    //check it has split correctly
                    if (effectParts.Length > 4)
                    {
                        int statusEffectID = Int32.Parse(effectParts[0]);
                        int statusEffectLevel = Int32.Parse(effectParts[1]);
                        int casterLevel = Int32.Parse(effectParts[2]);
                        int casterAbilityLevel = Int32.Parse(effectParts[3]);
                        int timeRemaining = Int32.Parse(effectParts[4]);
                        float statModifier = 0;
                        if (effectParts.Length > 5)
                        {
                            statModifier = float.Parse(effectParts[5]);
                        }
                        StatusEffectTemplate currStatusTemplate =
                            StatusEffectTemplateManager.GetStatusEffectTemplateForID((EFFECT_ID) statusEffectID);

                        if (currStatusTemplate != null)
                        {

                            //work out when the status effect should have started
                            double timeStarted = currentTime -
                                                 (currStatusTemplate.getEffectLevel(statusEffectLevel, false)
                                                     .m_duration - timeRemaining);
                            

                            // Create the CharacterEffect    
                            CharacterEffect newCharTemplate = FindCharacterEffect(statusEffectID, statusEffectID);
                            if (newCharTemplate != null)
                            {
                                CharacterEffect newCharacterEffect = new CharacterEffect(newCharTemplate);
                                newCharacterEffect.m_TimeStarted = (int) timeStarted;

                                // Create a Status Effect to with it.
                                StatusEffect newStatusEffect = new StatusEffect(timeStarted, currStatusTemplate,
                                    statusEffectLevel, entity, null, false, statModifier);
                                newCharacterEffect.StatusEffect = newStatusEffect;

                                newCharacterEffect.StatusEffect.CasterAbilityLevel = casterAbilityLevel;
                                newCharacterEffect.StatusEffect.CasterLevel = casterLevel;

                                //add it to the effects list
                                if (newStatusEffect.m_effectLevel != null)
                                {
                                    //energy sheild needs to run out
                                    if (newStatusEffect.Template.EffectType == EFFECT_TYPE.ENERGY_SHIELD_2)
                                    {
                                        newStatusEffect.CurrentAmount =
                                            newStatusEffect.m_effectLevel.getModifiedAmount(
                                                newStatusEffect.CasterAbilityLevel, newStatusEffect.StatModifier);
                                    }

                                    entity.m_currentCharacterEffects.Add(newCharacterEffect);
                                    //the status List has changed so send update at earliest opportunity
                                    entity.StatusListChanged = true;

                                }
                                else
                                {
                                    Program.Display("Removed status effect due to null level statusEffectID:" +
                                                    statusEffectID + " level:" + statusEffectLevel + " from " +
                                                    entity.GetIDString());
                                }
                            }
                        }
                    }
                }
            }            
        }

        // This function represents the 'front-facing' function for triggering Games Effects.
        // A Game Effect can compromise one or more different types of sub-effects - triggering
        // a Game Effect means handling the triggering of these multiple effects, which we do here.
        static public void InflictNewCharacterEffect(CharacterEffectParams param, CombatEntity entity)
        {
            if (param == null || entity == null)
                return;

            double currentTime = Program.MainUpdateLoopStartTime(); //NetTime.Now;

            // Apply a character effect, returning a list of the effects applied.
            List<CharacterEffect> subEffects = new List<CharacterEffect>();  
  
            // subEffectId is NOT used...
            GetCharacterEffectClassForID((int)param.charEffectId, (int)param.subEffectId, ref subEffects);

            param.triggeredEffects.Clear();
         
            foreach (CharacterEffect effect in subEffects)
            {
                double currTime = currentTime;
                if (param.overrideTimeStarted > 0.0f )
                    currTime = param.overrideTimeStarted;

                effect.m_TimeStarted = (int)currTime;
                effect.m_Level = param.level;
                effect.m_statModifier = param.statModifier;

                // We could, at some point, extend this system to include other kinds of sub-effect types.
                // This would be the point where those sub-types are triggered.
                switch (effect.m_SubEffectType)
                {
                    // Complex/Simple doesnt matter...
                    case CharacterEffect.CHARACTER_EFFECT_TYPE.SIMPLE_STATUS_EFFECT:
                    case CharacterEffect.CHARACTER_EFFECT_TYPE.COMPLEX_STATUS_EFFECT:
                    {
                        // Store the Status Effect return object.
                        effect.StatusEffect = entity.InflictNewStatusEffect((EFFECT_ID)effect.m_SubEffectId,
                                                                            param.caster,
                                                                            param.level,
                                                                            param.aggressive,
                                                                            param.PVP,
                                                                            param.statModifier);

                        // Auras - this is being triggered when a status effect 'bounces' - which is a valid response, InflictNewStatusEffect() has its own Displays for null refs
                        // Debug info - if the status effect failed to trigger, log it.
                        /*if (effect.StatusEffect == null)
                        {
                            
                            //Program.Display("Failed CE:" + param.charEffectId + ", SE:" + param.subEffectId);
                        }
                        else*/
                        // Auras - this appears to work as intended and doesnt spam 'Failed CE...' on the server log
                        if (effect.StatusEffect != null)
                        {
                            // Make sure the status effect time matches the CharacterEffect start time
                            effect.StatusEffect.StartTime = effect.m_TimeStarted;
                            entity.m_currentCharacterEffects.Add(effect);

                            // Trigger a re-calculation of the stats
                            entity.SetStatsChangeLevel(CombatEntity.STATS_CHANGE_LEVEL.EQUIPMENT_CHECK_REQUIRED);

                            // For every effect that successfully goes off, add it into the list requested by the caller
                            if (param.triggeredEffects != null)
                            {
                                param.triggeredEffects.Add(effect);
                            } 

                            // Auras - notify players who gain an aura
                            if (effect.StatusEffect.Template.IsAuraSubEffect && entity.Type == CombatEntity.EntityType.Player)
                            {
                                Character thisCharacter = (Character)entity;
								string locText = Localiser.GetString(textDB, thisCharacter.m_player, (int)CharacterEffectManagerTextDB.TextID.GAINED_EFFECT);
								string locStatusEffectName = StatusEffectTemplateManager.GetLocaliseStatusEffectName(thisCharacter.m_player, (int)effect.StatusEffect.Template.StatusEffectID);
								locText = string.Format(locText, locStatusEffectName);
								Program.processor.sendSystemMessage(locText, thisCharacter.m_player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
							}
                        }
                    }
                    break;
                }
            }
            subEffects.Clear();
        }

        static public void InflictNewCharacterEffect2(CharacterEffectParams param, CombatEntity entity)
        {
            if (param == null || entity == null)
                return;

            double currentTime = Program.MainUpdateLoopStartTime(); //NetTime.Now;
            // Apply a character effect, returning a list of the effects applied.
            List<CharacterEffect> subEffects = new List<CharacterEffect>();
            GetCharacterEffectClassForID((int)param.charEffectId, (int)param.subEffectId, ref subEffects);

            param.triggeredEffects.Clear();

            foreach (CharacterEffect effect in subEffects)
            {
                double currTime = currentTime;
                if (param.overrideTimeStarted > 0.0f)
                    currTime = param.overrideTimeStarted;

                effect.m_TimeStarted = (int)currTime;
                effect.m_Level = param.level;
                effect.m_statModifier = param.statModifier;

                // We could, at some point, extend this system to include other kinds of sub-effect types.
                // This would be the point where those sub-types are triggered.
                switch (effect.m_SubEffectType)
                {
                    case CharacterEffect.CHARACTER_EFFECT_TYPE.SIMPLE_STATUS_EFFECT:
                    case CharacterEffect.CHARACTER_EFFECT_TYPE.COMPLEX_STATUS_EFFECT:
                        {
                            // Store the Status Effect return object.
                            effect.StatusEffect = entity.InflictNewStatusEffect2((EFFECT_ID)effect.m_SubEffectId,
                                                                            param.caster,
                                                                            param.level,
                                                                            param.aggressive,
                                                                            param.PVP,
                                                                            param.statModifier);

                            // Debug info - if the status effect failed to trigger, log it.
                            /*if (effect.StatusEffect == null)
                            {
                                Program.Display("Failed CE:" + param.charEffectId + ", SE:" + param.subEffectId);
                            }
                            else*/
                            if (effect.StatusEffect != null)
                            {
                                // Make sure the status effect time matches the CharacterEffect start time
                                effect.StatusEffect.StartTime = effect.m_TimeStarted;

                                entity.m_currentCharacterEffects.Add(effect);
                                // Trigger a re-calculation of the stats
                                entity.SetStatsChangeLevel(CombatEntity.STATS_CHANGE_LEVEL.EQUIPMENT_CHECK_REQUIRED);

                                // For every effect that successfully goes off, add it into the list requested by the caller.
                                if (param.triggeredEffects != null)
                                {
                                    param.triggeredEffects.Add(effect);
                                }
                            }
                        }
                        break;
                }
            }
            subEffects.Clear();
        }

        static internal void FillModifiersList(CharacterModifiers.MODIFIER_TYPES type, string entries, ref CharacterEffect newEffect)
        {
            string[] listsplit = entries.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < listsplit.Length; i++)
            {
                string[] subsplit = listsplit[i].Split(new char[] { '^' }, StringSplitOptions.RemoveEmptyEntries);
                int attribute = Int32.Parse(subsplit[0]);
                int amount = Int32.Parse(subsplit[1]);
                CharacterModifiers newMod = new CharacterModifiers(type, attribute, amount);
                newEffect.m_modifiers.Add(newMod);
            }
        }

        static internal void FillListInt(ref List<FloatForID> list, SqlQuery query, string col)
        {
            if (query.isNull(col))
                return;

            string entries = query.GetString(col);
            string[] listsplit = entries.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < listsplit.Length; i++)
            {
                string[] subsplit = listsplit[i].Split(new char[] { '^' }, StringSplitOptions.RemoveEmptyEntries);
                int bt = Int32.Parse(subsplit[0]);
                int amount = Int32.Parse(subsplit[1]);
                list.Add(new FloatForID(bt, amount));
            }            
        }

        static internal void FillListFloat(ref List<FloatForID> list, SqlQuery query, string col)
        {
            if (query.isNull(col))
                return;

            string entries = query.GetString(col);
            string[] listsplit = entries.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < listsplit.Length; i++)
            {
                string[] subsplit = listsplit[i].Split(new char[] { '^' }, StringSplitOptions.RemoveEmptyEntries);
                int bt = Int32.Parse(subsplit[0]);
                float amount = float.Parse(subsplit[1]);
                list.Add(new FloatForID(bt, amount));
            }
        }

        static public void Fill(Database db)
        {

            //create the array of status effects
            m_CharacterEffectClasses = new List<CharacterEffect>();
            
            // Read data from db
            SqlQuery query = new SqlQuery(db, "select * from character_effects");
            
            // Construct class object for every db entry.
            while (query.Read())
            {
                CharacterEffect newClass = new CharacterEffect(
                    query.GetInt32("character_effect_id"),
                    query.GetInt32("sub_effect_id"),
                    (CharacterEffect.CHARACTER_EFFECT_TYPE)query.GetInt32("character_effect_type_id"));

                switch (newClass.m_SubEffectType)
                {
                    case CharacterEffect.CHARACTER_EFFECT_TYPE.COMPLEX_STATUS_EFFECT:
                        newClass.m_Duration = Double.Parse(query.GetString("sub_effect_duration"));
                        newClass.m_armour = Int32.Parse(query.GetString("armour"));
                        newClass.m_attack_speed = Int32.Parse(query.GetString("attack_speed"));

                        FillListFloat(ref newClass.m_damageTypes, query, "damage_list");
                        FillListInt(ref newClass.m_bonusTypes, query, "bonus_list");
                        FillListFloat(ref newClass.m_damageReductionTypes, query, "damage_reductions_list");
                        FillListFloat(ref newClass.m_immunityTypes, query, "immunity_list");
                        FillListInt(ref newClass.m_avoidanceTypes, query, "avoidance_bonuses");

                        if (query.isNull("stat_bonus") == false)
                        {
                            string statsString = query.GetString("stat_bonus");
                            FillModifiersList(CharacterModifiers.MODIFIER_TYPES.Stats, statsString, ref newClass);
                        }
                        if (query.isNull("ability_bonus") == false)
                        {
                            string abilityString = query.GetString("ability_bonus");
                            FillModifiersList(CharacterModifiers.MODIFIER_TYPES.Abilities, abilityString, ref newClass);
                        }

                        if (query.isNull("skill_bonus") == false)
                        {
                            string skillsString = query.GetString("skill_bonus");
                            FillModifiersList(CharacterModifiers.MODIFIER_TYPES.Skills, skillsString, ref newClass);
                        }
                        //the current number of possible parameters
                        int maxItemParams = 2;

                        if (query.isNull("param_mod_type") == false)
                        {
                            CombatModifiers.Modifier_Type modType =
                                (CombatModifiers.Modifier_Type) query.GetInt32("param_mod_type");

                            List<float> paramList = new List<float>();
                            bool dataEnded = false;
                            for (int i = 0; i < maxItemParams && dataEnded == false; i++)
                            {
                                string fieldName = "param_" + i;

                                if (query.isNull(fieldName) == false)
                                {
                                    float paramVal = query.GetFloat(fieldName);
                                    paramList.Add(paramVal);
                                }
                                else
                                {
                                    dataEnded = true;
                                }
                            }
                            CombatModifiers newMod = new CombatModifiers(modType, paramList);
                        }


                        break;                
                }

                m_CharacterEffectClasses.Add(newClass);
            }
            query.Close();
        }

        static public CharacterEffect FindCharacterEffect(int ID, int subID)
        {
            if (m_CharacterEffectClasses == null)
                return null;

            for (int i = 0; i < m_CharacterEffectClasses.Count; i++)
            {
                if (m_CharacterEffectClasses[i].m_id == ID &&
                    (subID == 0 || subID == m_CharacterEffectClasses[i].m_SubEffectId))
                {
                    return new CharacterEffect(m_CharacterEffectClasses[i]);
                }
            }
            return null;
        }

        static public List<CharacterEffect> FindAllCharacterEffects(int ID) // should return all sub-effects for the top level character effect id
        {
            if (m_CharacterEffectClasses == null)
                return null;

            List<CharacterEffect> allCharacterEffects = new List<CharacterEffect>();

            for (int i = 0; i < m_CharacterEffectClasses.Count; i++)
            {
                if (m_CharacterEffectClasses[i].m_id == ID)
                {
                    allCharacterEffects.Add(new CharacterEffect(m_CharacterEffectClasses[i]));
                }
            }
            return allCharacterEffects;
        }

        static public void GetCharacterEffectClassForID(int ID, int subID, ref List<CharacterEffect>result )
        {
            result.Clear();
            result.AddRange(FindAllCharacterEffects(ID));
        }

    }
}
