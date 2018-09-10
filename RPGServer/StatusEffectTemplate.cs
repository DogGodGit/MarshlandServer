using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MainServer
{
    public enum EFFECT_ID
    {
        NONE=0,
        PROTECTIVE_STANCE=1,
        RECUPERATE=2,
        STRANGLING_VINES=3,
        HOWLING_WIND=4,
        GRASPING_ROOTS=5,
        CLOAK_OF_FIRE=6,
        STEADY_AIM=7,
        BOLAS=8,
        HIDE=9,
        REND=10,
        HEALTH_REGEN = 11,
        ENERGY_REGEN = 12,
        ABUNDANCE = 13,
        SHIELD_OF_BARK = 14,
        NATURES_EMBRACE = 15,
        LURE_OF_FIRE = 16,
        LURE_OF_ICE = 17,
        ENERGY_SHIELD = 18, //not done, I think we need to move cloak of fire and pass the damage as a referance variable
        ENERGY_WELL = 19,
        FRENZY = 20,
        FAST_REFLEXES  = 21,
        POISON_WEAPON = 22,
        CAMOUFLAGE = 23,
        RAPID_SHOT = 24,
        BARBED_SHOT = 25,
        STUN = 26,
        MEDITATE = 27,
        IN_COMBAT_ENERGY_REGEN = 28,
        INVISIBILITY_POTION=29,
        INVISIBILITY_ELIXIR=30,
        SHRINK_POTION =31,
        SHRINK_ELIXIR = 32,
        GROWTH_POTION = 33,
        GROWTH_ELIXIR = 34,
        DEF_BOOST_POT = 35,
        DEF_BOOST_ELIXIR = 36,
        ARM__BOOST_POT=37,
        ARM_BOOST_ELIX = 38,
        MAX_HEALTH_BOOST_POT = 39,
        MAX_HEALTH_BOOST_ELIX = 40,
        MAX_ENERGY_BOOST_POT = 41,
        MAX_ENERGY_BOOST_ELIX = 42,
        ATT_BOOST_POT=43,
        ATT_BOOST_ELIXIR=44,
        ATT_SPD_BOOST_POT = 45,
        ATT_SPD_BOOST_ELIX = 46,
        RUN_SPD_BOOST_POT = 47,
        RUN_SPD_BOOST_ELIXIR = 48,
        EXP_BOOST_POT=49,
        EXP_BOOST_ELIXIR = 50,
        FISHING_EXP_BOOST_ELIXIR = 164,
        HEALTH_REGEN_POT = 51,
        HEALTH_REGEN_ELIX = 52,
        ENERGY_REGEN_POT = 53,
        ENERGY_REGEN_ELIX = 54,
        COMBI_POT_1 = 55,
        COMBI_ELIX_1 = 56,
        MAX_ROOT = 57,
        FOOLSTALK_MUSHROOM=58,
        YELLOW_GIANT_MUSHROOM=59,
        RUN_SPEED_ITEM_PERM=60,
        ATTACK_SPEED_ITEM_PERM=61,
        HEALTH_REGEN_ITEM_PERM=62,
        ENERGY_REGEN_ITEM_PERM=63,
        ABILITY_BOOST_ELIXIR=64,
        ABILITY_BOOST_PERM =65,
        ABILITY_BOOST_POT =163,
        EXP_BOOST_PERM = 66,
        FIRE_SHIELD_PERM = 67,
        BROOM_SPEED_ITEM_PERM = 68,
        HEALTH_REGEN_ITEM_OFFHAND_PERM = 69,
        ENERGY_REGEN_ITEM_OFFHAND_PERM = 70,
        GROWTH_BOOST_PERM = 71,
        SHRINK_BOOST_PERM = 72,
        CAMOFLAGE_PERM = 73,
        SLED_MOVEMENT_BOOST = 74,
        CAMOUFLAGE_2 = 75,
        BOLAS_2=76,
        ENERGY_SHIELD_2 = 77,
        ENTANGLED = 81,
        FROZEN = 84,
        SHEILD_BASH = 85
    }
    enum EFFECT_TYPE
    {
        CHANGE_ARMOUR = 0,
        HEALTH_REGEN = 1,
        ENERGY_REGEN = 2,
        DOT = 3,
        CHANGE_ATTACK = 4,
        ROOT = 5,
        DAMAGE_SHIELD = 6,
        BUFF_RANGE_ATTACK_AND_DAMAGE = 7,
        CHANGE_RUN_SPEED = 8,
        HIDE = 9,
        CHANGE_MAX_HEALTH = 10,
        CHANGE_MAX_ENERGY = 11,
        CHANGE_RESISTANCE = 12,
        ENERGY_SHIELD = 13,
        INC_ATTACK_DEC_ARMOUR = 14,
        CHANGE_DEFENCE = 15,
        CHANGE_DAMAGE_TYPE = 16,
        CHANGE_ATTACK_SPEED = 17,
        STUN = 18,
        INVISIBILITY = 19,
        CHANGE_SIZE = 20,
        BUFF_XP_GAIN = 21,
        CAMOUFLAGE = 22,
        COMBINATION = 23,
        CHANGE_ABILITY_GAIN = 24,
        CHANGE_RUN_SPEED_LEVEL_BASED = 25,
        CAMOUFLAGE_2 = 26,
        ENERGY_SHIELD_2 = 27,
        FROZEN=28,
        PLAYING_DEAD=29,
        HALF_ATTACK_SPEED = 30,
        STUN_2 = 31,
        CHANGE_FORTITUDE=32, 
        SILENCED = 33,
        STASIS = 34 ,
        HEALTH_AND_ENERGY_REGEN = 35,
        HEALTH_AND_ENERGY_PERCENT=36,
        MAX_HEALTH_PERCENT=37,
        MAX_ENERGY_PERCENT = 38, 
        ARMOUR_PERCENT = 39,
        DEFENCE_PERCENT = 40,
        ATTACK_PERCENT = 41,
        HEALTH_REGEN_PERCENT_OF_MAX = 42,
        ENERGY_REGEN_PERCENT_OF_MAX = 43,
        CHANGE_EVASIONS = 44,
        ICE_SKILL_BOOST = 45,
        FIRE_SKILL_BOOST = 46,
        DAMAGE_MULTI_NO_PLAYERS=47,
        PET_HUNGRY = 48,
        BUFF_FISHING_XP_GAIN = 49,
        CONCENTRATION_REGEN = 50,
        DISMOUNTED = 51,
        AURA = 52
    }
    class StatusEffectLevel
    {
        public StatusEffectLevel(SqlQuery query)
        {
            m_class_level = query.GetInt32("class_level");
            m_amount = query.GetDouble("effect_amount");
            m_duration = query.GetDouble("duration");
            m_baseAmount = query.GetDouble("base_amount");
        }
        public float getFloatModifiedAmount(float abilityLevel, float statModifier)
        {
            return (float)(m_baseAmount + m_amount * (1 + Math.Sqrt(abilityLevel / 100.0f) + Math.Sqrt(statModifier)));
        }

        public int getModifiedAmount(float abilityLevel,float statModifier)
        {
            return (int)Math.Ceiling(m_baseAmount+m_amount*(1+Math.Sqrt(abilityLevel/100.0f)+Math.Sqrt(statModifier)));
        }
        public int getUnModifiedAmount()
        {
            return (int)Math.Ceiling(m_baseAmount + m_amount);
        }

        public int m_class_level;
        public double m_amount;
        public double m_duration;
        public double m_baseAmount;
    }

    #region AuraSubEffect

    class AuraSubEffect
    {
        internal int    SubEffectID       { private set; get; }
        internal int    CharacterEffectID { private set; get; }
        internal int    TypeToLookFor     { private set; get; }
        internal double Radius            { private set; get; }
        internal bool   GroupOnly         { private set; get; }

        public AuraSubEffect(SqlQuery query)
        {
            SubEffectID       = query.GetInt32("sub_effect_id");
            CharacterEffectID = query.GetInt32("char_effect_id");
            TypeToLookFor     = query.GetInt32("type_to_look_for");
            Radius            = query.GetDouble("radius");
            GroupOnly         = query.GetInt32("group_only") == 1;
        }
    };

    #endregion

    class StatusEffectTemplate
    {
        #region variables
        /// <summary>
        /// The unique identifying number for this Status Effect
        /// </summary>
        EFFECT_ID m_statusEffectID;
        /// <summary>
        /// The name of this Status effect
        /// </summary>
        string m_statusEffectName;
   
      
        EFFECT_TYPE m_effect_type;
        StatusEffectClass m_effect_class;
        DAMAGE_TYPE m_damage_type;

        List<StatusEffectLevel> m_EffectLevels = new List<StatusEffectLevel>();
        List<StatusEffectLevel> m_PVPEffectLevels = new List<StatusEffectLevel>();

        bool m_removedOnDeath=true;
        bool m_requiresAppearanceUpdate = false;
        bool m_itemOnly;
        bool m_breakOnAggressive = false;
        bool m_dormantOnAggressive = false;
        ABILITY_TYPE m_casterAbilityType = ABILITY_TYPE.NA;
        #endregion //variables

        #region properties
        /// <summary>
        /// The unique identifying number for this Status Effect
        /// </summary>
        internal EFFECT_ID StatusEffectID
        {
            get { return m_statusEffectID; }
        }

        internal StatusEffectClass EffectClass
        {
            get { return m_effect_class; }
        }
        internal EFFECT_TYPE EffectType
        {
            get { return m_effect_type; }
        }
        internal DAMAGE_TYPE DamageType
        {
            get { return m_damage_type; }
        }
        internal bool RemovedOnDeath
        {
            get { return m_removedOnDeath; }
        }
        internal bool RequiresAppearanceUpdate
        {
            get { return m_requiresAppearanceUpdate; }
        }
        internal bool ItemOnly
        {
            get { return m_itemOnly; }
        }
        internal bool BreakOnAggressive
        {
            get { return m_breakOnAggressive; }
        }
        internal bool DormantOnAggressive
        {
            get { return m_dormantOnAggressive; }
        }
        internal ABILITY_TYPE CasterAbilityType
        {
            get { return m_casterAbilityType; }
        }
        #endregion //properties

        internal float TickRate        { private set; get; }
        internal int   AuraSubEffectID { private set; get; }
        internal bool  IsAuraSubEffect { set; get; }
        internal AuraSubEffect AuraEffect { set; get; }

        #region initialisation
        public StatusEffectTemplate(Database db,SqlQuery query)
        {
            m_statusEffectID = (EFFECT_ID)query.GetInt32("status_effect_id");
            m_statusEffectName = query.GetString("status_effect_name");
            m_effect_class = StatusEffectTemplateManager.GetStatusEffectClassForID(query.GetInt32("status_effect_class_id"));
            m_effect_type = (EFFECT_TYPE)query.GetInt32("effect_type");
            m_damage_type = (DAMAGE_TYPE)query.GetInt32("damage_type");
            m_itemOnly = query.GetBoolean("item_only");
            m_breakOnAggressive = query.GetBoolean("break_on_aggressive");
            m_dormantOnAggressive = query.GetBoolean("dormant_on_aggressive");
            m_casterAbilityType = (ABILITY_TYPE)query.GetInt32("caster_ability");
            SqlQuery subquery = new SqlQuery(db, "select * from status_effect_levels where status_effect_template_id=" + (int)m_statusEffectID + " order by PVP,status_effect_level");
            while (subquery.Read())
            {
                StatusEffectLevel newLevel=new StatusEffectLevel(subquery);
                if (subquery.GetBoolean("PVP"))
                {
                    m_PVPEffectLevels.Add(newLevel);
                }
                else
                {
                    m_EffectLevels.Add(newLevel);
                }
            }
            subquery.Close();
            m_removedOnDeath = query.GetBoolean("remove_on_death");
            m_requiresAppearanceUpdate = query.GetBoolean("requires_appearance_update");

            TickRate        = query.GetFloat("tick_rate");
            AuraSubEffectID = query.GetInt32("aura_sub_effect");
            IsAuraSubEffect = false;
            AuraEffect      = null;
        }
        public StatusEffectLevel getEffectLevel(int level,bool PVP)
        {
            if (PVP)
            {
                if (level < m_PVPEffectLevels.Count)
                {
                    return m_PVPEffectLevels[level];
                }
            }

            if (level < m_EffectLevels.Count)
            {
                return m_EffectLevels[level];
            }
            else
            {
                Program.Display("ERROR: StatusEffectLevel - No effect level for " + level);
                return null;
            }
        }

        #endregion ///initialisation
    }
}
