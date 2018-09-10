using System;
using System.Collections.Generic;
using Lidgren.Network;

namespace MainServer
{

    public class CombatModifiers
    {

        public enum Modifier_Type
        {
            None = -1,
            AddedSkillDamage=50,
            ChangesCastingTime=51,
            ChangesRecastTime=52
        }

        Modifier_Type m_modType = Modifier_Type.None;
        List<float> m_modParams = new List<float>();
        
        public CombatModifiers(Modifier_Type modType, List<float> modParams)
        {
            m_modType = modType;
            m_modParams = modParams;
        }
        internal void ApplyCombatParam(CombatEntityStats theStats, CombatEntityStats theStatsMultiplier)
        {
            switch (m_modType)
            {
                case Modifier_Type.AddedSkillDamage:
                    {
                        if (m_modParams.Count > 1)
                        {
                            float skillID = m_modParams[0];
                            float paramVal = m_modParams[1];
                            SkillAugment newAugment = new SkillAugment(m_modType, paramVal);
                            theStats.AddToSkillAugment((SKILL_TYPE)skillID, newAugment);
                        }
                        else
                        {
                            Program.Display("CombatModifiers::ApplyItemParam num param failure for " + m_modType);
                        }
                        break;
                    }
                case Modifier_Type.ChangesRecastTime:
                case Modifier_Type.ChangesCastingTime:
                    {
                        if (m_modParams.Count > 1)
                        {
                            float skillID = m_modParams[0];
                            float paramVal = m_modParams[1];
                             
                            CES_SkillHolder currentSkill = CES_SkillHolder.GetSkillForID((SKILL_TYPE)skillID, theStatsMultiplier.Skills);
                            bool augmentAdded = false;

                            if (currentSkill != null)
                            {
                                SkillAugment currentAugment = currentSkill.GetAugmentForType(m_modType);
                                if (currentAugment != null)
                                {
                                    double pveDominantVal = CombatEntity.GetDominantSmallerIsBuff(currentAugment.PVEModParam, paramVal);
                                    double pvpDominantVal = CombatEntity.GetDominantSmallerIsBuff(currentAugment.PVPModParam, paramVal);
                                    currentAugment.PVEModParam = (float)pveDominantVal;
                                    currentAugment.PVPModParam = (float)pvpDominantVal;
                                    augmentAdded = true;
                                }
                            }

                            if (augmentAdded == false)
                            {
                                SkillAugment newAugment = new SkillAugment(m_modType, paramVal);
                                theStatsMultiplier.SetSkillAugmentVal((SKILL_TYPE)skillID, newAugment,true);
                                theStatsMultiplier.SetSkillAugmentVal((SKILL_TYPE)skillID, newAugment, false);
                            }
                        }
                        else
                        {
                            Program.Display("CombatModifiers::ApplyItemParam num param failure for " + m_modType);
                        }
                        break;
                    }
              
            }

        }

    }

    class SkillAugment
    {
        CombatModifiers.Modifier_Type m_modType = CombatModifiers.Modifier_Type.None;
        float m_pveModParam = 0;
        float m_pvpModParam = 0;

        internal CombatModifiers.Modifier_Type ModType
        {
            get { return m_modType; }
        }

        internal float PVEModParam
        {
            set { m_pveModParam = value; }
            get { return m_pveModParam; }
        }
        internal float PVPModParam
        {
            set { m_pvpModParam = value; }
            get { return m_pvpModParam; }
        }
        internal SkillAugment(CombatModifiers.Modifier_Type modType, float param)
        {
            m_modType = modType;
           
            m_pveModParam = param;
            m_pvpModParam = param;

        }
        internal SkillAugment(CombatModifiers.Modifier_Type modType, float pveParam, float pvpParam)
        {
            m_modType = modType;

            m_pveModParam = pveParam;
            m_pvpModParam = pvpParam;

        }
    }

    class CES_SkillHolder
    {
        public SKILL_TYPE m_skillID;
        public float m_currentValue;

        List<SkillAugment> m_skillAugments = new List<SkillAugment>();

        internal List<SkillAugment> SkillAugments 
        {
            get {return m_skillAugments;}
        }

        public CES_SkillHolder(SKILL_TYPE skillID, float currentValue)
        {
            m_skillID = skillID;
            m_currentValue = currentValue;
        }
        static internal CES_SkillHolder GetSkillForID(SKILL_TYPE skillID, List<CES_SkillHolder> theList)
        {
            for (int i = 0; i < theList.Count; i++)
            {

                CES_SkillHolder currentSkill = theList[i];
                if (currentSkill.m_skillID == skillID)
                {
                    return currentSkill;
                }
            }
            return null;
        }
        internal SkillAugment GetAugmentForType(CombatModifiers.Modifier_Type modType)
        {
            SkillAugment augmentForType = null;
            for (int i = 0; i < m_skillAugments.Count && augmentForType==null; i++)
            {
                SkillAugment currentAugment = m_skillAugments[i];
                if (currentAugment.ModType == modType)
                {
                    augmentForType = currentAugment;
                }
            }
            return augmentForType;
        }
        /// <summary>
        /// Adds the contents of one list to another ensureing no skill is entered twice
        /// </summary>
        /// <param name="theListToAddTo"></param>
        /// <param name="baseList"></param>
        static internal void AbsorbSkillsToList(List<CES_SkillHolder> theListToAddTo, List<CES_SkillHolder> baseList)
        {
            for (int i = 0; i < baseList.Count; i++)
            {
                CES_SkillHolder currentSkill = baseList[i];
                CES_SkillHolder duplicate = null;
                for (int j = 0; j < theListToAddTo.Count&&duplicate==null; j++)
                {
                    CES_SkillHolder currentMasterList = theListToAddTo[j];
                    if (currentMasterList.m_skillID == currentSkill.m_skillID)
                    {
                        duplicate = currentMasterList;
                        for (int augmentIndex = 0; augmentIndex < currentSkill.SkillAugments.Count; augmentIndex++)
                        {
                            SkillAugment currentAugment = currentSkill.SkillAugments[augmentIndex];
                            if (duplicate.GetAugmentForType(currentAugment.ModType) == null)
                            {
                                SkillAugment newAugment = new SkillAugment(currentAugment.ModType, 0);
                                duplicate.SkillAugments.Add(newAugment);
                            }
                        }
                    }
                }

                if (duplicate == null)
                {
                    //this needs to be 0 lvl
                    //they will then be populated by the entity
                    CES_SkillHolder theSkill = new CES_SkillHolder(currentSkill.m_skillID, 0);
                    for (int augmentIndex = 0; augmentIndex < currentSkill.SkillAugments.Count;augmentIndex++ )
                    {
                        SkillAugment currentAugment = currentSkill.SkillAugments[augmentIndex];
                        SkillAugment newAugment = new SkillAugment(currentAugment.ModType,0);
                        theSkill.SkillAugments.Add(newAugment);
                    }
                    theListToAddTo.Add(theSkill);
                }
            }

        }
        void AddAugmentsToCopy(CES_SkillHolder newCopy)
        {
            for (int i = 0; i < m_skillAugments.Count; i++)
            {
                SkillAugment currentAugment = m_skillAugments[i];

                SkillAugment augmentCopy = new SkillAugment(currentAugment.ModType, currentAugment.PVEModParam, currentAugment.PVPModParam);

                newCopy.SkillAugments.Add(augmentCopy);
            }
        }
        internal CES_SkillHolder Copy()
        {
            CES_SkillHolder newCopy = new CES_SkillHolder(m_skillID, m_currentValue);
            AddAugmentsToCopy(newCopy);
            return newCopy;
        }
    }

    class CES_AbilityHolder
    {
        
        public CES_AbilityHolder(ABILITY_TYPE ability_id, float currentLevel)
        {
            m_ability_id = ability_id;
            m_currentValue = currentLevel;
        }
        public ABILITY_TYPE m_ability_id;
        public float m_currentValue;

        static internal CES_AbilityHolder GetAbilityForID(ABILITY_TYPE ability_id, List<CES_AbilityHolder> theList)
        {
            for(int i=0; i<theList.Count;i++)
            {

                CES_AbilityHolder currentAbility = theList[i];
                if(currentAbility.m_ability_id==ability_id)
                {
                    return currentAbility;
                }
            }
                return null;
        }
        static internal void AddCharacterAbilitiesToList(List<CES_AbilityHolder> theListToAddTo, List<CharacterAbility> baseList)
        {
            for (int i = 0; i < baseList.Count; i++)
            {
                CharacterAbility currentAbility = baseList[i];
                CES_AbilityHolder newAbility = new CES_AbilityHolder(currentAbility.m_ability_id, currentAbility.m_currentLevel);
                theListToAddTo.Add(newAbility);
            }

        }
        internal CES_AbilityHolder Copy()
        {
            return new CES_AbilityHolder(m_ability_id, m_currentValue);
        }
    }

    public enum STATISTIC_TYPE
        {
            NONE = -1,
            STRENGTH = 0,
            DEXTERITY = 1,
            FOCUS = 2,
            VITALITY = 3,
            MAX_HEALTH = 4,
            MAX_ENERGY = 5,
            ATTACK = 6,
            DEFENCE = 7,
            ARMOUR = 8,
            ENCUMBERANCE = 9,
            DAMAGE = 10,
            HEALTH_REGEN_PER_TICK = 11,
            ENERGY_REGEN_PER_TICK = 12,
            EXP_RATE = 13,
            ABILITY_RATE = 14,
            RUN_SPEED = 15,
            ATTACK_SPEED = 16,
            SCALE = 17,
            BONUS_TYPE = 18,
            DAMAGE_TYPE = 19,
            AVOIDANCE_TYPE = 20,
            ABILITY = 21,
            SKILL = 22,
            FAST_TRAVEL_ITEM_LIMIT = 23,
            SKILL_POINTS = 24,
            ATTRIBUTE_POINTS = 25,
            MAX_ATTACK_RANGE = 26,
            CURRENT_HEALTH = 27,
            CURRENT_ENERGY = 28,
            TOTAL_DAMAGE = 29,
            SOLO_BANK_ITEM_LIMIT = 30,
            PVP_XP=31,
            //The first of our gathering stats, should this be here??
            CURRENT_CONCENTRATION_FISHING=32,
            MAX_CONCENTRATION_FISHING = 33,
            FISHING_EXP_RATE = 34
        };


    class DifferenceInfo
    {
        public DifferenceInfo(STATISTIC_TYPE major_type, int minor_type)
        {
            m_major_type = major_type;
            m_minor_type = minor_type;
        }
        public STATISTIC_TYPE m_major_type;
        public int m_minor_type;
    };

    class CombatEntityStats
    {
        public const int BASE_FAST_TRAVEL_LIMIT = 10;
        public const int BASE_SOLO_BANK_SIZE = 20;
        protected float m_vitality = 0;
        protected float m_dexterity = 0;
        protected float m_focus = 0;

        protected float m_maxHealth = 0;
        protected float m_attack = 0;
        protected float m_defence = 0;
        protected float m_armour = 0;
        protected float m_encumberance = 0;
        protected float m_damage = 0;

       
        protected List<FloatForID> m_bonusTypes = new List<FloatForID>();
        protected List<FloatForID> m_newWeaponDamageTypes = new List<FloatForID>();
        protected List<FloatForID> m_newOtherDamageTypes = new List<FloatForID>();
        protected List<FloatForID> m_newCombinedDamageTypes = new List<FloatForID>();
        protected List<FloatForID> m_immunityTypes = new List<FloatForID>();
        protected List<FloatForID> m_damageReductionTypes = new List<FloatForID>();        
        protected List<FloatForID> m_newAvoidanceTypes = new List<FloatForID>();
        float m_defaultDamageVal = 0;

        List<CES_AbilityHolder> m_abilities = new List<CES_AbilityHolder>();
        List<CES_SkillHolder> m_skills = new List<CES_SkillHolder>();
        float m_defaultAbilityVal = 0;

        protected float m_healthRegenPerTick = 0;
        protected float m_energyRegenPerTick = 0;
        protected float m_concentrationRegenPerTick = 0;

        protected float m_healthRegenPerTickCombat = 0;
        protected float m_energyRegenPerTickCombat = 0;

        protected int m_currentConcentrationFishing = 0;

        protected float m_expRate = 1.0f;
        protected float m_fishingExpRate = 1.0f;
        protected float m_fishingDamage = 1.0f;
		
        protected float m_abilityRate = 1.0f;
        protected float m_runSpeed = 1.0f;
        protected float m_attackSpeed = 1.0f;
        protected float m_scale = 1.0f;
        protected int m_fastTravelItemLimit = BASE_FAST_TRAVEL_LIMIT;
        protected int m_soloBankSizeLimit = BASE_SOLO_BANK_SIZE;
        protected int m_skillPoints = 0;
        protected int m_attributePoints = 0;
        protected float m_maxAttackRange = 1.0f;
        internal int m_currentHealth = 0;
        protected int m_currentEnergy = 0;
        protected Int64 m_pvpExperience = 0;
        public Character m_Character=null;
        public List<DifferenceInfo> m_updatedInfoList = new List<DifferenceInfo>();

        internal float Vitality
        {
            get { return m_vitality; }
            set { m_vitality = value; }
        }
        internal float Dexterity
        {
            get { return m_dexterity; }
            set { m_dexterity = value; }
        }
        internal float Focus
        {
            get { return m_focus; }
            set { m_focus = value; }
        }

        protected internal float Strength { get; set; }

        internal float MaxHealth
        {
            get { return m_maxHealth; }
            set { m_maxHealth = value; }
        }

        public  float MaxEnergy { get; set; }

        internal float Attack
        {
            get { return m_attack; }
            set { m_attack = value; }
        }
        internal float Defence
        {
            get { return m_defence; }
            set { m_defence = value; }
        }
        internal float Armour
        {
            get { return m_armour; }
            set { m_armour = value; }
        }
        internal float Encumberance
        {
            get { return m_encumberance; }
            set { m_encumberance = value; }
        }
        internal float Damage
        {
            get { return m_damage; }
            set { m_damage = value; }
        }

        public float MaxConcentrationFishing { get;  set; }

        public int TotalWeaponDamage(int level)
        {
            
                int damage = 0;// m_damageEffectModifier + m_baseDamageValue;
                for (int i = 0; i < m_newCombinedDamageTypes.Count; i++)
                {
                    int currentType = m_newCombinedDamageTypes[i].m_bonusType;
					//if (currentType == (int) DAMAGE_TYPE.FISHING_DAMAGE)
					//{
					//	if(th)
					//	continue;
					//}
	                damage += (int)GetCombinedDamageType(currentType);
                    

                }
                // james old return damage;
                return damage ;
            
        }
        internal List<CES_AbilityHolder> Abilities
        {
            get { return m_abilities; }
        }
        internal List<CES_SkillHolder> Skills
        {
            get { return m_skills; }
        }
        internal float HealthRegenPerTick
        {
            get { return m_healthRegenPerTick; }
            set { m_healthRegenPerTick = value; }
        }

        internal float FishingConcentrationRegenPerTick {
            get { return m_concentrationRegenPerTick; }
            set { m_concentrationRegenPerTick = value; }
        }

        internal float EnergyRegenPerTick
        {
            get { return m_energyRegenPerTick; }
            set { m_energyRegenPerTick = value; }
        }
        internal float HealthRegenPerTickCombat
        {
            get { return m_healthRegenPerTickCombat; }
            set { m_healthRegenPerTickCombat = value; }
        }
        internal float EnergyRegenPerTickCombat
        {
            get { return m_energyRegenPerTickCombat; }
            set { m_energyRegenPerTickCombat = value; }
        }
        internal float ExpRate
        {
            get { return m_expRate; }
            set { m_expRate = value; }
        }


        internal float FishingExpRate
        {
            get { return m_fishingExpRate; }
            set { m_fishingExpRate = value; }
        }
        
        internal float AbilityRate
        {
            get { return m_abilityRate; }
            set { m_abilityRate = value; }
        }
        internal float RunSpeed
        {
            get { return m_runSpeed; }
            set { m_runSpeed = value; }
        }
        internal float AttackSpeed
        {
            get { return m_attackSpeed; }
            set { m_attackSpeed = value; }
        }
        internal float Scale
        {
            get { return m_scale; }
            set { m_scale = value; }
        }

        internal int FastTravelItemLimit
        {
            get { return m_fastTravelItemLimit; }
            set { m_fastTravelItemLimit = value; }
        }

        internal int SoloBankSizeLimit
        {
            get { return m_soloBankSizeLimit; }
            set { m_soloBankSizeLimit = value; }
        }
        internal int SkillPoints
        {
            get { return m_skillPoints; }
            set { m_skillPoints = value; }
        }
        internal int AttributePoints
        {
            get { return m_attributePoints; }
            set { m_attributePoints = value; }
        }
        internal float MaxAttackRange
        {
            get { return m_maxAttackRange; }
            set { m_maxAttackRange = value; }
        }
        internal int CurrentHealth
        {
            get { return m_currentHealth; }
            set
            {
                if (value >= 0)
                {
                    m_currentHealth = value;
                }
                else
                    m_currentHealth = 0;
            }
        }

        internal int CurrentConcentrationFishing
        {
            get { return m_currentConcentrationFishing; }
            set
            {
                if (value >= 0)
                {
                    m_currentConcentrationFishing = value;
                }
                else
                    m_currentConcentrationFishing = 0;
            }
        }
        internal int CurrentEnergy
        {
            get { return m_currentEnergy; }
            set
            {
                if (value < 0)
                {
                    m_currentEnergy = 0;
                    return;
                }


                m_currentEnergy = value;

            }
        }
        internal Int64 PVPExperience
        {
            get { return m_pvpExperience; }
            set
            {
                m_pvpExperience = value;
            }
        }      
        internal List<FloatForID> BonusTypes
        {
            get { return m_bonusTypes; }
        }
        internal float GetBonusType(int bonusType)
        {
            FloatForID currentVal = FloatForID.GetEntryForID(m_bonusTypes, bonusType);
            if (currentVal != null)
            {
                return currentVal.m_amount;
            }
            return m_defaultDamageVal;

        }
        internal void SetBonusType(int bonusType, float newVal)
        {
            FloatForID currentVal = FloatForID.GetEntryForID(m_bonusTypes, bonusType);
            if (currentVal != null)
            {
                currentVal.m_amount = newVal;
            }
            else
            {
                m_bonusTypes.Add(new FloatForID(bonusType,newVal));
            }
        }
        internal void AddToBonusType(int bonusType, float newVal)
        {
            FloatForID currentVal = FloatForID.GetEntryForID(m_bonusTypes, bonusType);
            if (currentVal != null)
            {
                currentVal.m_amount += newVal;
            }
            else
            {
                m_bonusTypes.Add(new FloatForID(bonusType,m_defaultDamageVal+newVal));
            }
        }
        internal List<FloatForID> CombinedDamageType
        {
            get { return m_newCombinedDamageTypes; }
        }
        internal float GetCombinedDamageType(int bonusType)
        {
            //return GetWeaponDamageType(bonusType)+GetOtherDamageType(bonusType);
            FloatForID currentVal = FloatForID.GetEntryForID(m_newCombinedDamageTypes, bonusType);
            if (currentVal != null)
            {
                return currentVal.m_amount;
            }
            return m_defaultDamageVal;
        }
        internal void SetCombinedDamageType(int bonusType, float newVal)
        {
            FloatForID currentVal = FloatForID.GetEntryForID(m_newCombinedDamageTypes, bonusType);
            if (currentVal != null)
            {
                currentVal.m_amount = newVal;
            }
            else
            {
                m_newCombinedDamageTypes.Add(new FloatForID(bonusType, newVal));
            }
        }        
        internal List<FloatForID> WeaponDamageTypes
        {
            get { return m_newWeaponDamageTypes; }
        }
        internal float GetWeaponDamageType(int bonusType)
        {
            FloatForID currentVal = FloatForID.GetEntryForID(m_newWeaponDamageTypes, bonusType);
            if (currentVal != null)
            {
                return currentVal.m_amount;
            }
            return m_defaultDamageVal;
        }      
        internal List<FloatForID> OtherDamageTypes
        {
            get { return m_newOtherDamageTypes; }
        }
        internal float GetOtherDamageType(int bonusType)
        {
            FloatForID currentVal = FloatForID.GetEntryForID(m_newOtherDamageTypes, bonusType);
            if (currentVal != null)
            {
                return currentVal.m_amount;
            }
            return m_defaultDamageVal;
        }
        internal void SetWeaponDamageType(int bonusType, float newVal)
        {
            //m_weaponDamageTypes[bonusType] = newVal;
            FloatForID currentVal = FloatForID.GetEntryForID(m_newWeaponDamageTypes, bonusType);
            if (currentVal != null)
            {
                currentVal.m_amount = newVal;
            }
            else
            {
                m_newWeaponDamageTypes.Add(new FloatForID(bonusType, newVal));
            }
        }
        internal void SetOtherDamageType(int bonusType, float newVal)
        {
            //m_otherDamageTypes[bonusType] = newVal;
            FloatForID currentVal = FloatForID.GetEntryForID(m_newOtherDamageTypes, bonusType);
            if (currentVal != null)
            {
                currentVal.m_amount = newVal;
            }
            else
            {
                m_newOtherDamageTypes.Add(new FloatForID(bonusType, newVal));
            }
        }
        internal void AddToWeaponDamageType(int bonusType, float newVal)
        {
            //m_weaponDamageTypes[bonusType] += newVal;
            FloatForID currentVal = FloatForID.GetEntryForID(m_newWeaponDamageTypes, bonusType);
            if (currentVal != null)
            {
                currentVal.m_amount += newVal;
            }
            else
            {
                m_newWeaponDamageTypes.Add(new FloatForID(bonusType, m_defaultDamageVal + newVal));
            }
        }
        internal void AddToOtherDamageType(int bonusType, float newVal)
        {
            //m_otherDamageTypes[bonusType] += newVal;
            FloatForID currentVal = FloatForID.GetEntryForID(m_newOtherDamageTypes, bonusType);
            if (currentVal != null)
            {
                currentVal.m_amount += newVal;
            }
            else
            {
                m_newOtherDamageTypes.Add(new FloatForID(bonusType, m_defaultDamageVal + newVal));
            }
        }
        internal List<FloatForID> AvoidanceTypes
        {
            get { return m_newAvoidanceTypes; }
        }
        internal float GetAvoidanceType(AVOIDANCE_TYPE avoidanceType)
        {
            //return m_avoidanceTypes[(int)avoidanceType];
            FloatForID currentVal = FloatForID.GetEntryForID(m_newAvoidanceTypes, (int)avoidanceType);
            if (currentVal != null)
            {
                return currentVal.m_amount;
            }
            return m_defaultDamageVal;
        }
        internal void SetAvoidanceType(AVOIDANCE_TYPE avoidanceType, float newVal)
        {
            //m_avoidanceTypes[(int)avoidanceType] = newVal;
            FloatForID currentVal = FloatForID.GetEntryForID(m_newAvoidanceTypes, (int)avoidanceType);
            if (currentVal != null)
            {
                currentVal.m_amount = newVal;
            }
            else
            {
                m_newAvoidanceTypes.Add(new FloatForID((int)avoidanceType, newVal));
            }
        }
        internal void AddToAvoidanceType(AVOIDANCE_TYPE avoidanceType, float newVal)
        {
           // m_avoidanceTypes[(int)avoidanceType] += newVal;
            FloatForID currentVal = FloatForID.GetEntryForID(m_newAvoidanceTypes, (int)avoidanceType);
            if (currentVal != null)
            {
                currentVal.m_amount += newVal;
            }
            else
            {
                m_newAvoidanceTypes.Add(new FloatForID((int)avoidanceType, m_defaultDamageVal + newVal));
            }
        }
        internal List<FloatForID> ImmunityTypes
        {
            get { return m_immunityTypes; }
        }
        internal float GetImmunityType(int bonusType)
        {
            //return m_avoidanceTypes[(int)avoidanceType];
            FloatForID currentVal = FloatForID.GetEntryForID(m_immunityTypes, (int)bonusType);
            if (currentVal != null)
            {
                return currentVal.m_amount;
            }
            return m_defaultDamageVal;
        }
        internal void SetImmunityType(int bonusType, float newVal)
        {
            //m_avoidanceTypes[(int)avoidanceType] = newVal;
            FloatForID currentVal = FloatForID.GetEntryForID(m_immunityTypes, bonusType);
            if (currentVal != null)
            {
                currentVal.m_amount = newVal;
            }
            else
            {
                m_immunityTypes.Add(new FloatForID(bonusType, newVal));
            }
        }
        internal void AddToImmunityType(int bonusType, float newVal)
        {
            // m_avoidanceTypes[(int)avoidanceType] += newVal;
            FloatForID currentVal = FloatForID.GetEntryForID(m_immunityTypes, bonusType);
            if (currentVal != null)
            {
                currentVal.m_amount += newVal;
            }
            else
            {
                m_immunityTypes.Add(new FloatForID(bonusType, m_defaultDamageVal + newVal));
            }
        }
        internal List<FloatForID> DamageReductionTypes
        {
            get { return m_damageReductionTypes; }
        }
        internal float GetDamageReductionType(int bonusType)
        {
            //return m_avoidanceTypes[(int)avoidanceType];
            FloatForID currentVal = FloatForID.GetEntryForID(m_damageReductionTypes, (int)bonusType);
            if (currentVal != null)
            {
                return currentVal.m_amount;
            }
            return m_defaultDamageVal;
        }
        internal void SetDamageReductionType(int bonusType, float newVal)
        {
            //m_avoidanceTypes[(int)avoidanceType] = newVal;
            FloatForID currentVal = FloatForID.GetEntryForID(m_damageReductionTypes, bonusType);
            if (currentVal != null)
            {
                currentVal.m_amount = newVal;
            }
            else
            {
                m_damageReductionTypes.Add(new FloatForID(bonusType, newVal));
            }
        }
        internal void AddToDamageReductionType(int bonusType, float newVal)
        {
            // m_avoidanceTypes[(int)avoidanceType] += newVal;
            FloatForID currentVal = FloatForID.GetEntryForID(m_damageReductionTypes, bonusType);
            if (currentVal != null)
            {
                currentVal.m_amount += newVal;
            }
            else
            {
                m_damageReductionTypes.Add(new FloatForID(bonusType, m_defaultDamageVal + newVal));
            }
        }
        internal float GetAbilityValForId(ABILITY_TYPE abilityID)
        {
            float abilityLevel = m_defaultAbilityVal;

            CES_AbilityHolder ability = CES_AbilityHolder.GetAbilityForID(abilityID, m_abilities);
            if (ability != null)
            {
                abilityLevel = ability.m_currentValue;
            }

            return abilityLevel;
        }
        internal void AddToAbilityLevel(ABILITY_TYPE abilityID, float newVal)
        {
            CES_AbilityHolder ability = CES_AbilityHolder.GetAbilityForID(abilityID, m_abilities);
            if (ability == null)
            {
                ability = new CES_AbilityHolder(abilityID, newVal);
                m_abilities.Add(ability);
            }
            else
            {
                ability.m_currentValue += newVal;
            }
        }
        internal float GetSkillValForId(SKILL_TYPE skillID)
        {
            float skillLevel = m_defaultAbilityVal;

            CES_SkillHolder theSkill = CES_SkillHolder.GetSkillForID(skillID, m_skills);
            if (theSkill != null)
            {
                skillLevel = theSkill.m_currentValue;
            }

            return skillLevel;
        }
        internal float GetSkillAugmentValForId(SKILL_TYPE skillID, CombatModifiers.Modifier_Type modType, bool inpvp)
        {
            float augmentVal = m_defaultAbilityVal;

            CES_SkillHolder theSkill = CES_SkillHolder.GetSkillForID(skillID, m_skills);
            if (theSkill != null)
            {
                SkillAugment theAugment = theSkill.GetAugmentForType(modType);
                if (theAugment != null)
                {
                    if (inpvp == true)
                    {
                        augmentVal = theAugment.PVPModParam;
                    }
                    else
                    {
                        augmentVal = theAugment.PVEModParam;
                    }
                }

            }

            return augmentVal;
        }
        internal void SetSkillAugmentVal(SKILL_TYPE skillID, SkillAugment newAugment,bool inPVP)
        {
            CES_SkillHolder theSkill = CES_SkillHolder.GetSkillForID(skillID, m_skills);
            if (theSkill == null)
            {
                theSkill = new CES_SkillHolder(skillID, m_defaultAbilityVal);
                m_skills.Add(theSkill);
            }

            if (theSkill != null)
            {
                SkillAugment currentAugment = theSkill.GetAugmentForType(newAugment.ModType);
                if (currentAugment != null)
                {
                    if (inPVP == true)
                    {
                        currentAugment.PVEModParam = newAugment.PVPModParam;
                    }
                    else
                    {
                        currentAugment.PVEModParam = newAugment.PVEModParam;
                    }
                }
                else
                {
                    //float augmentVal = newAugment.PVEModParam;
                    SkillAugment addAugment = null;//new SkillAugment(newAugment.ModType, augmentVal);
                    if (inPVP == true)
                    {
                        addAugment = new SkillAugment(newAugment.ModType, m_defaultAbilityVal,newAugment.PVPModParam);
                       // augmentVal = newAugment.PVPModParam;
                    }
                    else
                    {
                        addAugment = new SkillAugment(newAugment.ModType,newAugment.PVEModParam, m_defaultAbilityVal);
                    }

                    
                    theSkill.SkillAugments.Add(addAugment);
                }
            }
        }
        internal void AddToSkillLevel(SKILL_TYPE skillID, float newVal)
        {
            CES_SkillHolder theSkill = CES_SkillHolder.GetSkillForID(skillID, m_skills);
            if (theSkill == null)
            {
                theSkill = new CES_SkillHolder(skillID, newVal);
                m_skills.Add(theSkill);
            }
            else
            {
                theSkill.m_currentValue += newVal;
            }
        }
        internal void AddToSkillAugment(SKILL_TYPE skillID, SkillAugment newAugment)
        {
            CES_SkillHolder theSkill = CES_SkillHolder.GetSkillForID(skillID, m_skills);
            if (theSkill == null)
            {
                theSkill = new CES_SkillHolder(skillID, m_defaultAbilityVal);
                m_skills.Add(theSkill);
            }

            if (theSkill != null)
            {
                SkillAugment currentAugment = theSkill.GetAugmentForType(newAugment.ModType);
                if (currentAugment != null)
                {
                    currentAugment.PVPModParam += newAugment.PVPModParam;
                    currentAugment.PVEModParam += newAugment.PVEModParam;
                }
                else
                {
                    SkillAugment addAugment = new SkillAugment(newAugment.ModType, newAugment.PVEModParam, newAugment.PVPModParam);
                    //addAugment.PVPModParam = newAugment.PVPModParam;
                    theSkill.SkillAugments.Add(addAugment);
                }
            }
        }

        internal CombatEntityStats()
        {
            MaxEnergy = 0;
            Strength = 0;
            
            ResetStats();
        }

        internal CombatEntityStats(float baseValue)
        {
            MaxEnergy = 0;
            Strength = 0;
            
            ResetStats(baseValue);
        }

        internal void ResetStats(float baseValue)
        {
            m_vitality = baseValue;
            m_dexterity = baseValue;
            m_focus = baseValue;
            Strength = baseValue;

            MaxConcentrationFishing = baseValue;
            m_maxHealth = baseValue;
            MaxEnergy = baseValue;
            m_attack = baseValue;
            m_defence = baseValue;
            m_armour = baseValue;
            m_encumberance = baseValue;
            m_damage = baseValue;

            m_bonusTypes.Clear();
            m_defaultDamageVal = baseValue;

            m_newWeaponDamageTypes.Clear();
            m_newOtherDamageTypes.Clear();
            m_newCombinedDamageTypes.Clear();

            m_newAvoidanceTypes.Clear();
            m_immunityTypes.Clear();
            m_damageReductionTypes.Clear();
            m_abilities.Clear();
            m_skills.Clear();
            m_defaultAbilityVal = baseValue;
            

            // these values are always reset to 0 as they multiply against the max health
            m_healthRegenPerTick = 0;// baseValue;
            m_energyRegenPerTick = 0;// baseValue;
            m_healthRegenPerTickCombat = 0;
            m_energyRegenPerTickCombat = 0;
            m_concentrationRegenPerTick = 0;

            m_expRate = baseValue;
            m_fishingExpRate = baseValue;
            m_fishingDamage = baseValue;
            m_abilityRate = baseValue;
            m_runSpeed = baseValue;
            m_attackSpeed = baseValue;
            m_scale = baseValue;

        }
        //do a basic entity Reset
        internal void ResetStats()
        {
            m_vitality = 0;
            m_dexterity = 0;
            m_focus = 0;
            Strength = 0;

            
            m_maxHealth = 0;
            MaxEnergy = 0;
            m_attack = 0;
            m_defence = 0;
            m_armour = 0;
            m_encumberance = 0;
            m_damage = 0;

            m_abilities.Clear();
            m_skills.Clear();

            m_bonusTypes.Clear();
            m_newWeaponDamageTypes.Clear();
            m_newOtherDamageTypes.Clear();
            m_newCombinedDamageTypes.Clear();
            m_newAvoidanceTypes.Clear();
            m_immunityTypes.Clear();
            m_damageReductionTypes.Clear();
            m_healthRegenPerTick = 0;
            m_energyRegenPerTick = 0;
            m_healthRegenPerTickCombat = 0;
            m_energyRegenPerTickCombat = 0;

            m_expRate = 1;
            m_fishingExpRate = 1;
            m_fishingDamage = 0;
            m_abilityRate = 1;
            m_runSpeed = 1;
            m_attackSpeed = 1;
            m_scale = 1;

        }
        /// <summary>
        /// This should only be used on final combined stats
        /// </summary>
        internal void ClampStats(CombatEntity ownerEntity)
        {
            if (m_expRate < 0)
            {
                m_expRate = 0;
            }
            if (m_fishingExpRate < 0)
            {
                m_fishingExpRate = 0;
            }
            if (m_abilityRate < 0)
            {
                m_abilityRate = 0;
            }
            if (m_runSpeed < 0)
            {
                m_runSpeed = 0;
            }
            if (m_maxHealth < 1)
            {
                m_maxHealth = 1;
            }
           
            if (MaxEnergy < 1)
            {
                MaxEnergy = 1;
            }
            if (m_attackSpeed < Inventory.MIN_ATTACK_SPEED)
            {
                m_attackSpeed = Inventory.MIN_ATTACK_SPEED;
            }
            
            if (ownerEntity != null && ownerEntity.Type == CombatEntity.EntityType.Mob)
            {
                float maxReduction = ServerControlledEntity.MOB_MIN_STAT_REMAINS;
                CombatEntityStats baseStats = ownerEntity.BaseStats;

                //Attack clamp
                float minTarget = baseStats.Attack * maxReduction;
                minTarget = (float)Math.Ceiling(minTarget);
                if (Attack < minTarget)
                {
                    Attack = minTarget;
                }
                //Defence clamp
                minTarget = baseStats.Defence * maxReduction;
                minTarget = (float)Math.Ceiling(minTarget);
                if (Defence < minTarget)
                {
                    Defence = minTarget;
                }
                //Max Health clamp
                minTarget = baseStats.MaxHealth * maxReduction;
                minTarget = (float)Math.Ceiling(minTarget);
                if (MaxHealth < minTarget)
                {
                    MaxHealth = minTarget;
                    if (m_maxHealth < 1)
                    {
                        m_maxHealth = 1;
                    }
                }
                

                //Resists Clamp
                for (int i = 0; i < m_bonusTypes.Count; i++)
                {
                    FloatForID currentResist = m_bonusTypes[i];
                    minTarget = baseStats.GetBonusType(currentResist.m_bonusType);
                    //the first 3 resists have armor added
                    if (currentResist.m_bonusType < 3)
                    {
                        minTarget += baseStats.Armour;
                    }
                       
                    minTarget = minTarget* maxReduction;

                    minTarget = (float)Math.Ceiling(minTarget);
                    if (currentResist.m_amount < minTarget)
                    {
                        currentResist.m_amount = minTarget;
                    }

                }

            }
            
        }
        /// <summary>
        /// returns the calues of the object in string format for debugging
        /// </summary>
        /// <returns></returns>
        internal string GetDebugString()
        {
           /*       = 0;

        protected float m_maxHealth = 0;
        protected float m_maxEnergy = 0;
        protected float m_attack = 0;
        protected float m_defence = 0;
        protected float m_armour = 0;
        protected float m_encumberance = 0;
        protected float m_damage = 0;

        protected float[] m_bonusTypes = new float[CombatEntity.NUM_BONUS_TYPES];
        protected float[] m_damageTypes = new float[CombatEntity.NUM_DAMAGE_TYPES];
        protected float[] m_immunityTypes = new float[CombatEntity.NUM_IMMUNITY_TYPES];

        protected float m_healthRegenPerTick = 0;
        protected float m_energyRegenPerTick = 0;

        protected float m_expRate = 1.0f;
        protected float m_abilityRate = 1.0f;
        protected float m_runSpeed = 1.0f;
        protected float m_attackSpeed = 1.0f;
        protected float m_scale = 1.0f;*/

            string debugString = "";

            //base stats
           
            debugString += "Stats V=" + m_vitality + ";D=" + m_dexterity + ";F=" + m_focus + ";S=" + Strength;

            //combat stats
            debugString += "\n Combat HP ="+m_maxHealth + ";EP="+MaxEnergy + ";ATT="+m_attack+";DEF="+m_defence+";ARM="+m_armour+"DMG ="+m_damage;

           
            //bonus
            debugString +="\n bonus = ";
            for (int i = 0; i < m_bonusTypes.Count; i++)
            {
                debugString += m_bonusTypes[i].m_bonusType+"^"+ m_bonusTypes[i].m_amount+ ",";
            }


            //damage
            debugString +="\n damage = ";
            List<int> damageTypes = new List<int>();
            FloatForID.AddAlITypesToList(m_newWeaponDamageTypes, damageTypes);
            FloatForID.AddAlITypesToList(m_newOtherDamageTypes, damageTypes);
            for (int i = 0; i < damageTypes.Count; i++)
            {
                debugString += damageTypes[i] + "^" + GetCombinedDamageType(damageTypes[i]) +",";
            }
            /*for(int i=0;i<m_weaponDamageTypes.Length;i++)
            {
                debugString +=GetCombinedDamageType(i)+",";
            }*/

            //immunity
            debugString +="\n immunity = ";
            for (int i = 0; i < m_newAvoidanceTypes.Count; i++)
            {
                debugString += m_newAvoidanceTypes[i].m_bonusType + "^" + m_newAvoidanceTypes[i].m_amount + ",";
            }

            debugString += "\nOther ENCM=" + m_encumberance +";Run="+m_runSpeed+";ATTSPD="+m_attackSpeed+ ";HealthRgn=" + m_healthRegenPerTick + ";EnergyRgn=" + m_energyRegenPerTick+";EXP="+m_expRate+";ABT="+m_abilityRate+";Scale="+m_scale ;



            return debugString;
        }
        internal CombatEntityStats Copy()
        {
            CombatEntityStats newStats = new CombatEntityStats();
            newStats.Vitality = Vitality;
            newStats.Dexterity = Dexterity;
            newStats.Focus = Focus;
            newStats.Strength = Strength;
            newStats.MaxHealth = MaxHealth;
            newStats.MaxEnergy = MaxEnergy;
            newStats.MaxConcentrationFishing = MaxConcentrationFishing;
            newStats.Attack = Attack;
            newStats.Defence = Defence;
            newStats.Armour = Armour;
            newStats.Encumberance = Encumberance;
            newStats.Damage = Damage;
            newStats.HealthRegenPerTick = HealthRegenPerTick;
            newStats.EnergyRegenPerTick = EnergyRegenPerTick;
            newStats.HealthRegenPerTickCombat = HealthRegenPerTickCombat;
            newStats.EnergyRegenPerTickCombat = EnergyRegenPerTickCombat;
            newStats.ExpRate = ExpRate;
            newStats.AbilityRate = AbilityRate;
            newStats.RunSpeed = RunSpeed;
            newStats.AttackSpeed = AttackSpeed;
            newStats.Scale = Scale;
            newStats.PVPExperience = PVPExperience;

            for (int i = 0; i < m_bonusTypes.Count; i++)
            {
                newStats.SetBonusType(m_bonusTypes[i].m_bonusType, m_bonusTypes[i].m_amount);
            }
            for (int i = 0; i < m_newWeaponDamageTypes.Count; i++)
            {
                newStats.SetWeaponDamageType(m_newWeaponDamageTypes[i].m_bonusType, m_newWeaponDamageTypes[i].m_amount);
            }
            for (int i = 0; i < m_newOtherDamageTypes.Count; i++)
            {
                newStats.SetOtherDamageType(m_newOtherDamageTypes[i].m_bonusType, m_newOtherDamageTypes[i].m_amount);
            }
            for (int i = 0; i < m_newCombinedDamageTypes.Count; i++)
            {
                newStats.SetOtherDamageType(m_newCombinedDamageTypes[i].m_bonusType, m_newCombinedDamageTypes[i].m_amount);
            }
         
            for (int i = 0; i < m_newAvoidanceTypes.Count; i++)
            {
                AVOIDANCE_TYPE at = (AVOIDANCE_TYPE)m_newAvoidanceTypes[i].m_bonusType;
                newStats.SetAvoidanceType(at, m_newAvoidanceTypes[i].m_amount);
            }
            for (int i = 0; i < m_immunityTypes.Count; i++)
            {
                newStats.SetImmunityType(m_immunityTypes[i].m_bonusType, m_immunityTypes[i].m_amount);
            }
            for (int i = 0; i < m_damageReductionTypes.Count; i++)
            {
                newStats.SetImmunityType(m_damageReductionTypes[i].m_bonusType, m_damageReductionTypes[i].m_amount);
            }
            for (int i = 0; i < m_abilities.Count; i++)
            {
                newStats.m_abilities.Add(m_abilities[i].Copy());
            }
            for (int i = 0; i < m_skills.Count; i++)
            {
                newStats.m_skills.Add(m_skills[i].Copy());
            }
            newStats.SkillPoints = SkillPoints;
            newStats.AttributePoints = AttributePoints;
            newStats.FastTravelItemLimit = FastTravelItemLimit;
            newStats.MaxAttackRange = MaxAttackRange;
            newStats.CurrentEnergy = CurrentEnergy;
            newStats.CurrentHealth = CurrentHealth;
            newStats.CurrentConcentrationFishing = CurrentConcentrationFishing;
            newStats.SoloBankSizeLimit = SoloBankSizeLimit;
            return newStats;
        }

        internal NetOutgoingMessage BuildMessage(CombatEntityStats baseStats, NetOutgoingMessage msg,CombatEntity entity)
        {
           
           // NetOutgoingMessage msg=null;
            m_updatedInfoList.Clear();
            if (baseStats.Strength != Strength)
            {
                m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.STRENGTH, 0));
            }
            if (baseStats.Dexterity != Dexterity)
            {
                m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.DEXTERITY, 0));
            }
            if (baseStats.Focus != Focus)
            {
                m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.FOCUS, 0));
            }
            if (baseStats.Vitality != Vitality)
            {
                m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.VITALITY,0));
            }



            if (baseStats.MaxHealth != MaxHealth)
            {
                m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.MAX_HEALTH, 0));
            }
            if (baseStats.MaxConcentrationFishing != MaxConcentrationFishing)
            {
                m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.MAX_CONCENTRATION_FISHING,0));
            }
            if (baseStats.MaxEnergy != MaxEnergy)
            {
                m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.MAX_ENERGY, 0));
            }
            if (baseStats.Attack != Attack)
            {
                m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.ATTACK, 0));
            }
            if (baseStats.Defence != Defence)
            {
                m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.DEFENCE, 0));
            }
            if (baseStats.Armour != Armour)
            {
                m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.ARMOUR, 0));
            }
            if (baseStats.Encumberance != Encumberance)
            {
                m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.ENCUMBERANCE, 0));
            }
            if (baseStats.Damage != Damage)
            {
                m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.DAMAGE, 0));
            }
            if (baseStats.HealthRegenPerTick != HealthRegenPerTick)
            {
                m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.HEALTH_REGEN_PER_TICK, 0));
            }
            if (baseStats.EnergyRegenPerTick != EnergyRegenPerTick)
            {
                m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.ENERGY_REGEN_PER_TICK, 0));
            }
            if (baseStats.ExpRate != ExpRate)
            {
                m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.EXP_RATE, 0));
            }
            if (baseStats.FishingExpRate != FishingExpRate)
            {
                m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.FISHING_EXP_RATE, 0));
            }
            if (baseStats.AbilityRate != AbilityRate)
            {
                m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.ABILITY_RATE, 0));
            }
            if (baseStats.RunSpeed != RunSpeed)
            {
                m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.RUN_SPEED, 0));
            }
            if (baseStats.AttackSpeed != AttackSpeed)
            {
                m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.ATTACK_SPEED, 0));
                entity.InfoUpdated(Inventory.EQUIP_SLOT.SLOT_ATTACK_SPEED);
            }
            if (baseStats.Scale != Scale)
            {
                m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.SCALE, 0));
            }
            //////////-------------------bonus start
            List<FloatForID> bonusListCopy = new List<FloatForID>(baseStats.BonusTypes);
            for (int i = 0; i < m_bonusTypes.Count; i++)
            {
                FloatForID currentContainer= m_bonusTypes[i];
                int currentType = currentContainer.m_bonusType;
                if (baseStats.GetBonusType(currentType) != GetBonusType(currentType))
                {
                    m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.BONUS_TYPE, currentType));
                    
                }
                FloatForID.RemoveEntryForeTypeID(bonusListCopy, currentType);
            }
            for (int i = 0; i < bonusListCopy.Count; i++)
            {
                FloatForID currentContainer = bonusListCopy[i];
                int currentType = currentContainer.m_bonusType;
                m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.BONUS_TYPE, currentType));
            }
            //////////--------------------bonus end
            //////////--------------------damage start
            bool sendTotalDamage = false;
            
            List<FloatForID> damageListCopy = new List<FloatForID>(baseStats.CombinedDamageType);
            for (int i = 0; i < m_newCombinedDamageTypes.Count; i++)
            {
                FloatForID currentContainer = m_newCombinedDamageTypes[i];
                int currentType = currentContainer.m_bonusType;
                if (baseStats.GetCombinedDamageType(currentType) != GetCombinedDamageType(currentType))
                {
                    m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.DAMAGE_TYPE, currentType));
                    sendTotalDamage = true;
                }
                FloatForID.RemoveEntryForeTypeID(damageListCopy, currentType);
            }
            for (int i = 0; i < damageListCopy.Count; i++)
            {
                FloatForID currentContainer = damageListCopy[i];
                int currentType = currentContainer.m_bonusType;
                m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.DAMAGE_TYPE, currentType));
                sendTotalDamage = true;
            }
            if (sendTotalDamage == true)
            {
                m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.TOTAL_DAMAGE, 0));
            }
            ///////-----------------------damage end
            ///////-----------------------avoidance start
            /*for (int i = 0; i < CombatEntity.NUM_AVOIDANCE_TYPES; i++)
            {
                if (baseStats.GetAvoidanceType((AVOIDANCE_TYPE)i) != GetAvoidanceType((AVOIDANCE_TYPE)i))
                {
                    m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.AVOIDANCE_TYPE, i));
                }
            }*/
            List<FloatForID> avoidanceListCopy = new List<FloatForID>(baseStats.AvoidanceTypes);
            for (int i = 0; i < m_newAvoidanceTypes.Count; i++)
            {
                FloatForID currentContainer = m_newAvoidanceTypes[i];
                int currentType = currentContainer.m_bonusType;
                if (baseStats.GetAvoidanceType((AVOIDANCE_TYPE)currentType) != GetAvoidanceType((AVOIDANCE_TYPE)currentType))
                {
                    m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.AVOIDANCE_TYPE, currentType));
                }
                FloatForID.RemoveEntryForeTypeID(avoidanceListCopy, currentType);
            }
            for (int i = 0; i < avoidanceListCopy.Count; i++)
            {
                FloatForID currentContainer = avoidanceListCopy[i];
                int currentType = currentContainer.m_bonusType;
                m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.AVOIDANCE_TYPE, currentType));
            }
            ///////-----------------------avoidance end
            bool abilitiesChanged = false;
            if (m_abilities.Count != baseStats.m_abilities.Count)
            {
                abilitiesChanged = true;
            }
            else
            {
                for (int i = 0; i < m_abilities.Count; i++)
                {
                    if (m_abilities[i].m_ability_id != baseStats.m_abilities[i].m_ability_id || m_abilities[i].m_currentValue != baseStats.m_abilities[i].m_currentValue)
                    {
                        abilitiesChanged = true;
                        break;
                    }
                }
            }
            if (abilitiesChanged)
            {
                m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.ABILITY, 0));
            }
            bool skillsChanged = false;
            if (m_skills.Count != baseStats.m_skills.Count)
            {
                skillsChanged = true;
            }
            else
            {
                for (int i = 0; i < m_skills.Count; i++)
                {
                    if (m_skills[i].m_skillID != baseStats.m_skills[i].m_skillID || m_skills[i].m_currentValue != baseStats.m_skills[i].m_currentValue)
                    {
                        skillsChanged = true;
                        break;
                    }
                }
            }
            if (skillsChanged)
            {
                m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.SKILL, 0));
            }
            if (baseStats.SkillPoints != SkillPoints)
            {
                m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.SKILL_POINTS, 0));
            }
            if (baseStats.AttributePoints != AttributePoints)
            {
                m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.ATTRIBUTE_POINTS, 0));
            }
            if (baseStats.FastTravelItemLimit != FastTravelItemLimit)
            {
                m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.FAST_TRAVEL_ITEM_LIMIT, 0));
            }
            if (baseStats.MaxAttackRange != MaxAttackRange)
            {
                m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.MAX_ATTACK_RANGE, 0));
                entity.InfoUpdated(Inventory.EQUIP_SLOT.SLOT_ATTACK_RANGE);
            }
            if (baseStats.CurrentEnergy != CurrentEnergy)
            {
                m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.CURRENT_ENERGY, 0));
            }
            if (baseStats.CurrentHealth != CurrentHealth)
            {
                m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.CURRENT_HEALTH, 0));
            }
            if (baseStats.CurrentConcentrationFishing != CurrentConcentrationFishing)
            {
                m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.CURRENT_CONCENTRATION_FISHING, 0));
            }
			if (baseStats.MaxConcentrationFishing != MaxConcentrationFishing)
			{
				m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.MAX_CONCENTRATION_FISHING, 0));
			}
            if (baseStats.SoloBankSizeLimit != SoloBankSizeLimit)
            {
                m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.SOLO_BANK_ITEM_LIMIT, 0));
            }
            if (baseStats.PVPExperience != PVPExperience)
            {
                m_updatedInfoList.Add(new DifferenceInfo(STATISTIC_TYPE.PVP_XP, 0));
            }
            if (m_updatedInfoList.Count > 0)
            {
                if (msg == null)
                {
                    msg = Program.Server.CreateMessage();
                    msg.WriteVariableUInt32((uint)NetworkCommandType.StatsUpdate);
                }
                msg.Write((byte)0);//partial list
                msg.WriteVariableInt32(m_updatedInfoList.Count);
                for(int i=0;i<m_updatedInfoList.Count;i++)
                {
                    DifferenceInfo diffInfo=m_updatedInfoList[i];
                    msg.WriteVariableInt32((int)diffInfo.m_major_type);
                    switch (diffInfo.m_major_type)
                    {
                        case STATISTIC_TYPE.STRENGTH:
                            {
                                msg.WriteVariableInt32((int)Strength);
                                break;
                            }
                        case STATISTIC_TYPE.DEXTERITY:
                            {
                                msg.WriteVariableInt32((int)Dexterity);
                                break;
                            }
                        case STATISTIC_TYPE.FOCUS:
                            {
                                msg.WriteVariableInt32((int)Focus);
                                break;
                            }
                        case STATISTIC_TYPE.VITALITY:
                            {
                                msg.WriteVariableInt32((int)Vitality);
                                break;
                            }


                        case  STATISTIC_TYPE.MAX_CONCENTRATION_FISHING:
                            {
                                msg.WriteVariableInt32((int) MaxConcentrationFishing);
                                break;
                            }
                        case STATISTIC_TYPE.MAX_HEALTH:
                            {
                                msg.WriteVariableInt32((int)MaxHealth);
                                break;
                            }
                        case STATISTIC_TYPE.MAX_ENERGY:
                            {
                                msg.WriteVariableInt32((int)MaxEnergy);
                                break;
                            }
                        case STATISTIC_TYPE.ATTACK:
                            {
                                msg.WriteVariableInt32((int)Attack);
                                break;
                            }
                        case STATISTIC_TYPE.DEFENCE:
                            {
                                msg.WriteVariableInt32((int)Defence);
                                break;
                            }
                        case STATISTIC_TYPE.ARMOUR:
                            {
                                msg.WriteVariableInt32((int)Armour);
                                break;
                            }
                        case STATISTIC_TYPE.ENCUMBERANCE:
                            {
                                msg.WriteVariableInt32((int)Encumberance);
                                break;
                            }
                        case STATISTIC_TYPE.DAMAGE:
                            {
                                msg.WriteVariableInt32((int)Damage);
                                break;
                            }
                        case STATISTIC_TYPE.HEALTH_REGEN_PER_TICK:
                            {
                                msg.WriteVariableInt32((int)HealthRegenPerTick);
                                break;
                            }
                        case STATISTIC_TYPE.ENERGY_REGEN_PER_TICK:
                            {
                                msg.WriteVariableInt32((int)EnergyRegenPerTick);
                                break;
                            }
                        case STATISTIC_TYPE.EXP_RATE:
                            {
                                msg.WriteVariableInt32((int)ExpRate);
                                break;
                            }
                        case STATISTIC_TYPE.FISHING_EXP_RATE:
                        {
                            msg.WriteVariableInt32((int) FishingExpRate);
                            break;
                        }
                        case STATISTIC_TYPE.ABILITY_RATE:
                            {
                                msg.WriteVariableInt32((int)AbilityRate);
                                break;
                            }
                        case STATISTIC_TYPE.RUN_SPEED:
                            {
                                msg.Write(RunSpeed);
                                break;
                            }
                        case STATISTIC_TYPE.ATTACK_SPEED:
                            {
                                msg.WriteVariableInt32((int)AttackSpeed);
                                break;
                            }
                        case STATISTIC_TYPE.SCALE:
                            {
                                msg.Write(Scale);
                                break;
                            }
                        case STATISTIC_TYPE.BONUS_TYPE:
                            {
                                msg.WriteVariableInt32(diffInfo.m_minor_type);
                                msg.WriteVariableInt32((int)GetBonusType(diffInfo.m_minor_type));
                                break;
                            }
                        case STATISTIC_TYPE.DAMAGE_TYPE:
                            {
                                msg.WriteVariableInt32(diffInfo.m_minor_type);
                                msg.WriteVariableInt32((int)GetCombinedDamageType(diffInfo.m_minor_type));
                                break;
                            }
                        case STATISTIC_TYPE.AVOIDANCE_TYPE:
                            {
                                msg.WriteVariableInt32(diffInfo.m_minor_type);
                                msg.WriteVariableInt32((int)Math.Ceiling(GetAvoidanceType((AVOIDANCE_TYPE)diffInfo.m_minor_type)));
                                break;
                            }
                        case STATISTIC_TYPE.ABILITY:
                            {
                                msg.WriteVariableInt32(m_abilities.Count);
                                for (int j = 0; j < m_abilities.Count; j++)
                                {
                                    msg.WriteVariableInt32((int)m_abilities[j].m_ability_id);
                                    msg.WriteVariableInt32((int)m_abilities[j].m_currentValue);
                                }
                                break;
                            }
                        case STATISTIC_TYPE.SKILL:
                            {
                                msg.WriteVariableInt32(m_skills.Count);
                                for (int j = 0; j < m_skills.Count; j++)
                                {
                                    msg.WriteVariableInt32((int)m_skills[j].m_skillID);
                                    msg.WriteVariableInt32((int)m_skills[j].m_currentValue);
                                }
                                break;
                            }
                        case STATISTIC_TYPE.SKILL_POINTS:
                            {
                                msg.WriteVariableInt32(SkillPoints);
                                break;
                            }
                        case STATISTIC_TYPE.ATTRIBUTE_POINTS:
                            {
                                msg.WriteVariableInt32(AttributePoints);
                                break;
                            }
                        case STATISTIC_TYPE.FAST_TRAVEL_ITEM_LIMIT:
                            {
                                msg.WriteVariableInt32(FastTravelItemLimit);
                                break;
                            }
                        case STATISTIC_TYPE.MAX_ATTACK_RANGE:
                            {
                                msg.Write(MaxAttackRange);
                                break;
                            }
                        case STATISTIC_TYPE.CURRENT_ENERGY:
                            {
                                msg.WriteVariableInt32(CurrentEnergy);
                                break;
                            }
                        case STATISTIC_TYPE.CURRENT_HEALTH:
                            {
                                msg.WriteVariableInt32(CurrentHealth);
                                break;
                            }
                        case STATISTIC_TYPE.CURRENT_CONCENTRATION_FISHING:
                            {
                                msg.WriteVariableInt32(CurrentConcentrationFishing);
                                break;
                            }
                        case STATISTIC_TYPE.TOTAL_DAMAGE:
                            {
                                msg.WriteVariableInt32(TotalWeaponDamage(entity.Level));
                                break;
                            }
                        case STATISTIC_TYPE.SOLO_BANK_ITEM_LIMIT:
                            {
                                msg.WriteVariableInt32(SoloBankSizeLimit);
                                break;
                            }
                        case STATISTIC_TYPE.PVP_XP:
                            {
                                int relXP=0;
                                if (m_Character != null)
                                {
                                    relXP = m_Character.getVisiblePVPExperience();
                                }
                                msg.WriteVariableInt32(relXP);
                                break;
                            }
                    }
                }
            }
            else if (msg != null)
            {
                msg.Write((byte)0);//partial list
                msg.WriteVariableInt32(0);//no items
            }
            
            m_updatedInfoList.Clear();

            return msg;
        }
    }
}
