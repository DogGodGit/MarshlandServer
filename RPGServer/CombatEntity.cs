using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Lidgren.Network;
using MainServer.Combat;
using MainServer.partitioning;
using XnaGeometry;
using MainServer.Localise;

namespace MainServer
{

    #region enums: DAMAGE_TYPE,  BONUS_TYPE,

    public enum DAMAGE_TYPE
    {
        PIERCING_DAMAGE = 0,
        SLASHING_DAMAGE = 1,
        CRUSHING_DAMAGE = 2,
        HEAT_DAMAGE = 3,
        COLD_DAMAGE = 4,
        MAGIC_DAMAGE = 5,
        POISON_DAMAGE = 6,
        MYTHIC_DAMAGE = 7,
        FISHING_DAMAGE = 15
    }
    public enum BONUS_TYPE
    {
        PIERCING_RESIST = 0,
        SLASHING_RESIST = 1,
        CRUSHING_RESIST = 2,
        HEAT_RESIST = 3,
        COLD_RESIST = 4,
        MAGIC_RESIST = 5,
        POISON_RESIST = 6,
        UNDEFINED_RESIST1 = 7,
        UNDEFINED_RESIST2 = 8,
        UNDEFINED_RESIST3 = 9,
        ATTACK_BONUS = 10,
        DEFENCE_BONUS = 11,
        HEALTH_BONUS = 12,
        ENERGY_BONUS = 13,
        FISHING_RESIST = 15,
        CONCENTRATION_BONUS = 16
    }

    #endregion

    #region helper classes - CombatRecord & StatusEffectActionConditions

    class CombatRecord
    {
        public DateTime m_combatStartTime = DateTime.Now;
        public int m_num_attacks = 0;
        public int m_num_misses = 0;
        public int m_num_skill_casts = 0;
        public int m_total_Skill_Damage = 0;
        public int m_total_AA_Damage = 0;
        public int m_total_SE_Damage = 0;

    };


    /// <summary>
    /// Used to keep tabs on how the current set of Status effects restrict the entity
    /// </summary>
    internal class StatusEffectActionConditions
    {
        bool m_move = false;
        bool m_attack = false;
        bool m_skills = false;
        bool m_takeDamage = false;
        bool m_detection = false;
        bool m_hostileAction = false;
        bool m_regen = false;

        internal StatusEffectActionConditions()
        {

        }
        internal void Reset()
        {
            m_move = false;
            m_attack = false;
            m_skills = false;
            m_takeDamage = false;
            m_detection = false;
            m_hostileAction = false;
            m_regen = false;
        }


        internal bool Move
        {
            get { return m_move; }
            set { m_move = value; }
        }

        internal bool Attack
        {
            get { return m_attack; }
            set { m_attack = value; }
        }
        internal bool Skills
        {
            get { return m_skills; }
            set { m_skills = value; }
        }
        internal bool TakeDamage
        {
            get { return m_takeDamage; }
            set { m_takeDamage = value; }
        }
        internal bool Detection
        {
            get { return m_detection; }
            set { m_detection = value; }

        }
        internal bool HostileAction
        {
            get { return m_hostileAction; }
            set { m_hostileAction = value; }
        }
        internal bool Regen
        {
            get { return m_regen; }
            set { m_regen = value; }
        }
    }

    #endregion

    internal class CombatEntity
    {
		// #localisation
		public class CombatEntityTextDB : TextEnumDB
		{
			public CombatEntityTextDB() : base(nameof(CombatEntity), typeof(TextID)) { }

			public enum TextID
			{
				STATUS_REMOVED,				// "{statusEffectName0} removed"
				TARGET_LEVEL_TOO_HIGH,		// "Target's level is too high."
				OTHER_INTERRUPTED_SKILL,	// "{name0} was interrupted during {skillName1}."
			}
		}
		public static CombatEntityTextDB textDB = new CombatEntityTextDB();

        #region enums: STATS_CHANGE_LEVEL, TargetLockType, EntityType, GatheringType

        internal enum STATS_CHANGE_LEVEL
        {
            /// <summary>
            /// the stats do not need to be rechecked or sent
            /// </summary>
            NO_CHANGE = 0,
            /// <summary>
            /// Basic information has been altered,this does not require stats to be recompiled
            /// </summary>
            BASIC_CHANGED = 1,
            /// <summary>
            ///Data has been changed that effects the compiled Stats, the stats will require recompiled to get the correct values
            /// </summary>
            COMPILE_REQUIRED = 2,
            /// <summary>
            /// The equipment stats may have changed, all combat stats need to be recompiled
            /// </summary>
            EQUIPMENT_CHECK_REQUIRED = 3
        }

        internal enum TargetLockType
        {
            /// <summary>
            /// The Entity has not been Locked by anyone
            /// </summary>
            Open = 1,
            /// <summary>
            /// The entity has been locked by someone else
            /// </summary>
            Locked = 2,
            /// <summary>
            /// The entity belongs to the character (or group)
            /// </summary>
            Owned = 3,
        };

        public enum EntityType { Player = 0, Mob = 1, Default = 2, Node = 3 };

        public enum LevelType { none, fish, cook }; //match with Client.CharacterEnums.cs

        #endregion

        #region const &  static fields

        static internal float HW_CHARACTER_DEFAULT_RADIUS = 1.0f;
        static internal int HW_BASE_HEALTH_REGEN = 4;
        static internal int HW_BASE_ENERGY_REGEN = 4;
        static internal int HW_BASE_CONCENTRATION_REGEN = 4;
        static int numberOfSkills = 20;


        static internal float DEFAULT_REPORT_PROGRESS = 0.5f;
        static internal float BOW_REPORT_PROGRESS = 0.579f;
        static internal string INTEREST_LIST_ERROR_STRING = "****Interest List Operation Failed With Error : ";
        static internal double MIN_TIME_BETWEEN_INTEREST_CHECKS = 0.5;

        internal const float MAX_SQUARED_COLLISION_ERROR = 0.01f;
        const double hostileTimeOut = 5;
        const float IN_COMBAT_REGEN_MULTI = 0.25f;
        protected const double RegenTime = 5;
        const float MIN_ATTACK_SPEED_MODIFIER = 0.3f;

        #endregion

        #region variables


        internal virtual CombatManager TheCombatManager
        {
            get { return null; }
        }
        internal CombatRecord m_AttackTargetCombatRecord = null;


        bool m_areasEffectsToBeChecked = true;
        internal partitioning.ZonePartition.ENTITY_TYPE m_defaultInterestTypes = partitioning.ZonePartition.ENTITY_TYPE.ET_NONE;

        public LevelType Gathering { get; protected set; }


        /// <summary>
        /// Stores the owner that has locked this Entity
        /// </summary>
        protected ITargetOwner m_lockOwner = null;
        bool m_lockOwnerChanged = false;
        protected CombatEntityStats m_baseStats = new CombatEntityStats();

        protected CombatEntityStats m_equipStats = new CombatEntityStats(0);
        protected CombatEntityStats m_equipStatsMultipliers = new CombatEntityStats(1);

        protected CombatEntityStats m_statusStats = new CombatEntityStats(0);
        protected CombatEntityStats m_statusStatsMultipliers = new CombatEntityStats(1);

        protected CombatEntityStats m_compiledStats = new CombatEntityStats(0);
        protected CombatEntityStats m_oldCompiledStats = null;


        StatusEffectActionConditions m_statusCancelConditions = new StatusEffectActionConditions();
        StatusEffectActionConditions m_statusPreventsActions = new StatusEffectActionConditions();
        internal StatusEffectActionConditions StatusCancelConditions
        {
            get { return m_statusCancelConditions; }
        }
        internal StatusEffectActionConditions StatusPreventsActions
        {
            get { return m_statusPreventsActions; }
        }

        internal DateTime m_lastUpdatedNearby = DateTime.Now.AddSeconds(-2);
        public CombatEntityStats EquipStats
        {
            get { return m_equipStats; }
        }
        public CombatEntityStats EquipStatsMultipliers
        {
            get { return m_equipStatsMultipliers; }
        }

        public CombatEntityStats StatusStats
        {
            get { return m_statusStats; }
        }
        public CombatEntityStats StatusStatsMultipliers
        {
            get { return m_statusStatsMultipliers; }
        }

        public CombatEntityStats BaseStats
        {
            get { return m_baseStats; }
        }
        public CombatEntityStats CompiledStats
        {
            get { return m_compiledStats; }
        }
        /// <summary>
        /// true if the entity is nolonger part of an update loop
        /// </summary>
        bool m_destroyed = false;
        protected CombatDamageMessageData m_currentAttackDamage = null;
        protected SkillDamageData m_currentProcData = null;
        /// <summary>
        /// keeps Track of what skills are known by this Entity
        /// true if skill is known, false if it is not
        /// </summary>
        protected bool[] m_skillList;
        protected List<EntitySkill> m_EntitySkills;
        protected float m_reportTime = 0.5f;
        protected double m_projectileSpeed = 0;
        protected int m_currentDamageID = 0;

        private int m_level;
        internal int Level
        {
            get { return m_level; }
            set { m_level = value; }
        }
        private int m_fishingLevel = 1; //default at 1
        internal int LevelFishing
        {
            get { return m_fishingLevel; }
            set { m_fishingLevel = value; }
        }

        private int m_cookingLevel = 1;
        internal int LevelCooking
        {
            get { return m_cookingLevel; }
            set { m_cookingLevel = value; }
        }

        /// <summary>
        /// True if the combat Entities skill has been interrupted
        /// </summary>
        protected bool m_currentSkillInterrupted = false;

        protected double m_lastDamageTime = 0;
        /// <summary>
        /// the last time health or energy regen was carried out
        /// </summary>
        protected double m_timeLastRegen = 0;
        /// <summary>
        /// Whether the Status effects have changed since the last character Update was Sent
        /// </summary>
        protected bool m_statusListChanged = false;
        //JT STATS CHANGES 12_2011
        //protected int m_baseEncumbrance = 0;
        /// <summary>
        /// The List of status Effects the entity is currently under
        /// </summary>
        internal List<CharacterEffect> m_currentCharacterEffects = null;
        /// <summary>
        /// The List of actions done to or by this Entity this combat cycle
        /// </summary>
        internal List<CombatDamageMessageData> m_combatActionListThisFrame;

        /// <summary>
        /// The List of damage done to or by this Entity this combat cycle
        /// </summary>
        internal List<CombatDamageMessageData> m_damageListThisFrame;

        /// <summary>
        /// whether an action is currently being carried out
        /// </summary>
        protected bool m_actionInProgress = false;
        /// <summary>
        /// The ID Number for the current Skill int use
        /// -1 if no skill is in use
        /// </summary>
        protected EntitySkill m_currentSkill = null;
        /// <summary>
        /// The Last Skill Attempted by the entity
        /// </summary>
        protected EntitySkill m_lastSkill = null;
        /// <summary>
        /// The ID Number for the next skill to be used
        /// -1 if there is no skill queue
        /// </summary>
        protected EntitySkill m_nextSkill = null;
        //protected int m_baseArmourValue = 0;
        //protected int m_baseDamageValue = 0;
        protected float m_radius = HW_CHARACTER_DEFAULT_RADIUS;
        /// <summary>
        /// The current position of this Entity
        /// </summary>
        protected ActorPosition m_currentPosition = null;
        /// <summary>
        /// The Target being attacked by this entity
        /// </summary>
        private CombatEntity m_attackTarget = null;
        /// <summary>
        /// the target for the current skill in use 
        /// </summary>
        protected CombatEntity m_currentSkillTarget = null;
        /// <summary>
        /// the target for the next skill to be used 
        /// </summary>
        protected CombatEntity m_nextSkillTarget = null;
        /// <summary>
        /// The identity of the battle Entity that Attacked this entity
        /// </summary>
        protected CombatEntity m_lastAttacker = null;
        /// <summary>
        /// The latest Damage done to this Entity
        /// </summary>
        protected int m_latestDamage = 0;
        /// <summary>
        /// The time until the attack is considered complate and damage can be deducted
        /// </summary>
        protected double m_timeTillAttackComplete;
        /// <summary>
        /// The time the last attack was Carried out
        /// </summary>
        protected double m_timeAtLastAttack;
        /// <summary>
        /// How long ago had the last attack been started before the attack cycle was interrupted for another action
        /// </summary>
        protected double m_attackProgressBeforeInterrupt = 0;
        /// <summary>
        /// The Time At which the Next battle action can be carried out
        /// </summary>
        protected double m_timeActionWillComplete = 0;
        /// <summary>
        /// The servers id for the owner of this Entity
        /// </summary>
        protected int m_serverID = -1;
        /// <summary>
        /// whether the entity belongs to a mob or a player
        /// so that clients can differentiate between them
        /// </summary>
        protected EntityType m_entityType = EntityType.Default;
        /// <summary>
        /// True if An Entity is under Attack or committing hostile Actions
        /// </summary>
        protected bool m_InCombat = false;
        /// <summary>
        /// A list of all combat Entities currently acting in a hostile way to this Entity
        /// </summary>
        protected List<CombatEntity> m_hostileEntities = null;
        /// <summary>
        /// Combat entity has died
        /// </summary>
        protected bool m_Dead = false;

        protected bool m_gatheringFailed = false;

        public CombatEntity m_killer = null;
        /// <summary>
        /// The percentage of health and energy recovered per regeneration cycle
        /// </summary>
        protected int m_additionalHealthRegenPerTick = 0;

        protected int m_additionalConcentrationRegenPerTick = 0;
        protected int m_additionalEnergyRegenPerTick = 0;
        /// <summary>
        /// The Partition The entity currently belongs to
        /// The Entity is responsible for notifying the partition when it enters or leaves
        /// property should be used whenever possible as it will notify partitions of changes
        /// </summary>
        protected ZonePartition m_currentPartition = null;

        public List<Inventory.EQUIP_SLOT> m_updatedInfo = new List<Inventory.EQUIP_SLOT>();

        protected List<CombatEntity> m_entitiesOfInterest = new List<CombatEntity>();
        List<EffectArea> m_areas = new List<EffectArea>();
        double m_timeAtLastInterestCheck = 0;
        protected bool m_hasMoved = false;
        public bool m_isDespawning = false;
        public bool m_InLimbo = false;
        protected float m_reportProgress = DEFAULT_REPORT_PROGRESS;
        protected bool m_blocksAttacks = true;

        protected List<EntityAreaConditionalEffect> m_areaEffectsList = new List<EntityAreaConditionalEffect>();

        internal bool AdminCloakedCharacter { get; set; }
        internal bool AdminCharacter { get; set; }

        #endregion

        #region Properties

        internal bool BlocksAttacks
        {
            get { return m_blocksAttacks; }
            set { m_blocksAttacks = value; }
        }

        /// <summary>
        /// Stores the owner that has locked this Entity
        /// </summary>
        internal ITargetOwner LockOwner
        {
            get { return m_lockOwner; }
            set
            {
                if (m_lockOwner != value)
                {
                    if (value == null)
                    {
                        Program.Display(GetIDString() + " Owner changed to null");
                    }
                    else
                    {
                        Program.Display(GetIDString() + " Owner changed");
                    }
                    m_lockOwnerChanged = true;
                    m_lockOwner = value;
                }
            }
        }

        /// <summary>
        /// The Partition The entity currently belongs to
        /// The Entity is responsible for notifying the partition when it enters or leaves
        /// Set will alert partitions of changes
        /// </summary>
        internal ZonePartition CurrentPartition
        {
            get { return m_currentPartition; }
            set
            {
                if (m_currentPartition == value)
                {
                    return;
                }
                if (m_currentPartition != null)
                {
                    m_currentPartition.EntityLeavingPartition(this);
                }
                if (value != null)
                {
                    value.EntityEnteringPartition(this);
                }
                m_currentPartition = value;
            }
        }
        /// <summary>
        /// true if the entity is nolonger part of an update loop
        /// </summary>
        internal bool Destroyed
        {
            get { return m_destroyed; }
            set
            {
                if (value == true)
                {
                    m_destroyed = value;
                    DestroyEntity();
                }
            }
        }
        /// <summary>
        /// The progress through the attack animation that the damage should come off (0-1)
        /// </summary>
        internal float ReportProgress
        {
            get { return m_reportProgress; }
        }
        internal bool InLimbo
        {
            set
            {
                if (m_InLimbo == true && value == false)
                {
                    WakeupGameEffects();
                }

                m_InLimbo = value;

            }
        }
        virtual internal Zone CurrentZone
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Whether the Status effects have changed since the last character Update was Sent
        /// </summary>
        internal bool StatusListChanged
        {
            get { return m_statusListChanged; }
            set
            {
                m_statusListChanged = value;
            }
        }
        /// <summary>
        /// The List of damages done to or by this Entity this combat cycle
        /// </summary>
        internal List<CombatDamageMessageData> CombatListThisFrame
        {
            get { return m_combatActionListThisFrame; }
        }
        internal List<CombatDamageMessageData> DamageListThisFrame
        {
            get { return m_damageListThisFrame; }
        }

        /// <summary>
        /// The List of status Effects the entity is currently under
        /// </summary>
        internal List<CharacterEffect> CurrentCharacterEffects
        {
            get { return m_currentCharacterEffects; }
        }
        /// <summary>
        /// keeps Track of what skills are known by this Entity
        /// true if skill is known, false if it is not
        /// </summary>
        public bool[] SkillList
        {
            get { return m_skillList; }
        }
        /// <summary>
        /// The distance From A mob that an attack will land
        /// </summary>
        public float Radius
        {
            get { return m_radius; }
            set { m_radius = value; }
        }
        /// <summary>
        /// The servers id for the owner of this Entity
        /// </summary>
        public int ServerID
        {
            set { m_serverID = value; }
            get { return m_serverID; }
        }
        public float PercentHealth
        {
            get
            {
                if (CurrentHealth == 0)
                {
                    return 0;
                }
                return (float)((float)CurrentHealth) / (float)MaxHealth;
            }
        }
        /// <summary>
        /// whether the entity belongs to a mob or a player
        /// so that clients can differentiate between them
        /// </summary>
        public EntityType Type
        {
            set { m_entityType = value; }
            get { return m_entityType; }
        }
        /// <summary>
        /// The current position of this Entity
        /// </summary>
        public ActorPosition CurrentPosition
        {
            set { m_currentPosition = value; }
            get { return m_currentPosition; }
        }

        /// <summary>
        /// The Target being attacked by this entity
        /// </summary>
        public float MaxSpeed
        {
            set { m_baseStats.RunSpeed = value; }
            get { return m_compiledStats.RunSpeed; }//(m_baseMaxSpeed*m_speedEffectModifier)/100; }
        }

        /// <summary>
        /// The Target being attacked by this entity
        /// </summary>
        public CombatEntity AttackTarget
        {
            set { m_attackTarget = value; }
            get { return m_attackTarget; }
        }
        /// <summary>
        /// The Target being attacked by this entity
        /// </summary>
        /*public CombatEntity NextAttackTarget
        {
            set { m_NextAttackTarget = value; }
            get { return m_NextAttackTarget; }
        }*/
        /// <summary>
        /// the target for the current skill in use 
        /// </summary>
        public CombatEntity CurrentSkillTarget
        {
            set
            {
                m_currentSkillTarget = value;
            }
            get { return m_currentSkillTarget; }
        }
        /// <summary>
        /// the target for the next skill to be used 
        /// </summary>
        public CombatEntity NextSkillTarget
        {
            set { m_nextSkillTarget = value; }
            get { return m_nextSkillTarget; }
        }

        /// <summary>
        /// The time until the attack is considered complate and damage can be deducted
        /// </summary>
        public double TimeTillActionComplete
        {
            set { m_timeTillAttackComplete = value; }
            get { return m_timeTillAttackComplete; }
        }

        /// <summary>
        /// The time the last attack was Carried out
        /// </summary>
        public double TimeAtLastAttack
        {
            set { m_timeAtLastAttack = value; }
            get { return m_timeAtLastAttack; }
        }
        /// <summary>
        /// How long ago had the last attack been started before the attack cycle was interrupted for another action
        /// </summary>
        public double AttackProgressBeforeInterrupt
        {
            set
            {
                if (value < 0)
                {
                    value = 0;
                }
                m_attackProgressBeforeInterrupt = value;

            }
            get { return m_attackProgressBeforeInterrupt; }
        }
        /// <summary>
        /// The Time At which the Next battle action can be carried out
        /// </summary>
        public double TimeActionWillComplete
        {
            set { m_timeActionWillComplete = value; }
            get { return m_timeActionWillComplete; }
        }
        public int AttackSpeed
        {
            get { return (int)m_compiledStats.AttackSpeed; }//(m_baseAttackSpeed * m_attackSpeedEffectModifier); }

        }

        public int BaseAttackSpeed
        {
            set { m_baseStats.AttackSpeed = value; }
        }

        // FISHING
        // Base attack speed is that of the weapon (without any modifiers from status effects)
        public float GetBaseAttackSpeed { get { return m_baseStats.AttackSpeed; } }

        public int Attack
        {
            get
            {
                int ret = (int)m_compiledStats.Attack;//m_baseAttack + m_attackEffectModifier;
                if (ret < 0)
                    ret = 0;
                return ret;
            }

        }
        public int BaseAttack
        {
            set { m_baseStats.Attack = value; }
        }
        public int Defence
        {
            get
            {
                int defence = (int)m_compiledStats.Defence;//m_baseDefence + m_defenceEffectModifier;
                if (defence < 0)
                    defence = 0;
                return defence;
            }
        }
        public int BaseDefence
        {
            set
            {
                m_baseStats.Defence = value;
            }
        }

        public int Encumbrance
        {
            get
            {

                if (m_compiledStats.Encumberance < 0)
                {
                    return 0;
                }
                return (int)m_compiledStats.Encumberance;

            }
        }


        public int TotalWeaponDamage
        {
            get
            {
                return m_compiledStats.TotalWeaponDamage(Level);
            }

        }

        public int ArmourValue
        {
            get { return (int)m_compiledStats.Armour; }//return m_baseArmourValue + m_armourEffectModifier; }
        }

        public int BaseArmourValue
        {
            set { m_baseStats.Armour = value; }//m_baseArmourValue = value; }
        }

        public int MaxHealth
        {
            get { return (int)m_compiledStats.MaxHealth; }
        }

        public int MaxConcentrationFishing
        {
            get { return (int)m_compiledStats.MaxConcentrationFishing; }
        }

        public int MaxEnergy
        {

            get { return (int)m_compiledStats.MaxEnergy; }
        }

        public int BaseMaxEnergy
        {
            set { m_baseStats.MaxEnergy = value; }
        }

        public int CurrentHealth
        {
            get { return m_compiledStats.CurrentHealth; }
            set
            {
                if (value >= 0)
                {
                    m_compiledStats.CurrentHealth = value;
                }
                else
                    m_compiledStats.CurrentHealth = 0;
            }
        }
        public int CurrentConcentrationFishing
        {
            get { return m_compiledStats.CurrentConcentrationFishing; }
            set
            {
                if (value >= 0)
                {
                    m_compiledStats.CurrentConcentrationFishing = value;
                }
                else
                    m_compiledStats.CurrentConcentrationFishing = 0;
            }
        }
        public int CurrentEnergy
        {
            get { return m_compiledStats.CurrentEnergy; }
            set
            {
                if (value < 0)
                {
                    m_compiledStats.CurrentEnergy = 0;
                    return;
                }


                m_compiledStats.CurrentEnergy = value;

            }
        }

        /// <summary>
        /// True if An Entity is under Attack or committing hostile Actions
        /// </summary>
        public bool InCombat
        {
            get { return m_InCombat; }
            set
            {
                if (m_InCombat != value)
                {
                    for (int i = 0; i < m_entitiesOfInterest.Count; i++)
                    {
                        CombatEntity currentEnt = m_entitiesOfInterest[i];
                        if (currentEnt != null)
                        {
                            currentEnt.EntityOfInterestChangedCombatType(this);
                        }
                    }
                    EntityOfInterestChangedCombatType(this);
                    m_InCombat = value;
                }
            }
        }

        /// <summary>
        /// entity dead
        /// </summary>
        public bool Dead
        {
            get { return m_Dead; }
            set
            {
                if (m_Dead != value)
                {
                    m_areasEffectsToBeChecked = true;
                }
                if (m_Dead != value && TheCombatManager != null)
                {
                    TheCombatManager.zone.DeathStatusChanged(this);
                }
                m_Dead = value;
            }
        }

        public bool ConcentrationFishDepleted { get; set; }

        /// <summary>
        /// whether an action is currently being carried out
        /// </summary>
        public bool ActionInProgress
        {
            get { return m_actionInProgress; }
            set { m_actionInProgress = value; }

        }
        /// <summary>
        /// The ID Number for the current Skill int use
        /// -1 if no skill is in use
        /// </summary>
        public EntitySkill CurrentSkill
        {
            get { return m_currentSkill; }
            set { m_currentSkill = value; }
        }
        /// <summary>
        /// The Last Skill Attempted by the entity
        /// </summary>
        public EntitySkill LastSkill
        {
            get { return m_lastSkill; }
            set { m_lastSkill = value; }
        }
        /// <summary>
        /// The ID Number for the next skill to be used
        /// -1 if there is no skill queue
        /// </summary>
        public EntitySkill NextSkill
        {
            get { return m_nextSkill; }
            set { m_nextSkill = value; }
        }

        /// <summary>
        /// A list of all combat Entities currently acting in a hostile way towards this Entity
        /// </summary>
        public List<CombatEntity> HostileEntities
        {
            get { return m_hostileEntities; }
        }

        internal bool CurrentSkillInterrupted
        {
            set { m_currentSkillInterrupted = value; }
        }

        internal float ExpRate
        {
            get { return m_compiledStats.ExpRate; }//m_expModifier; }
        }

        internal float ExpRateFish
        {
            get { return m_compiledStats.FishingExpRate; }//m_expModifier; }
        }
        
        public int HealthRegenPerTick
        {
            get { return m_additionalHealthRegenPerTick + (int)m_compiledStats.HealthRegenPerTick; }
        }

        public int ConcentrationRegenPerTick
        {
            get { return m_additionalConcentrationRegenPerTick + (int)m_compiledStats.FishingConcentrationRegenPerTick; }
        }

        public int EnergyRegenPerTick
        {
            get { return m_additionalEnergyRegenPerTick + (int)m_compiledStats.EnergyRegenPerTick; }
        }
        public int HealthRegenPerTickCombat
        {
            get { return (int)m_compiledStats.HealthRegenPerTickCombat; }
        }
        public int EnergyRegenPerTickCombat
        {
            get { return (int)m_compiledStats.EnergyRegenPerTickCombat; }
        }
        internal virtual string Name
        {
            get { return ""; }
        }

        internal virtual float AggroModifier
        {
            get { return 1.5f; }
        }
        internal virtual float AggroTickReduction
        {
            get { return 1.0f; }
        }
        internal virtual float Scale
        {
            get { return 1; }
            set { Scale = value; }
        }
        internal virtual float ReportTime
        {
            get { return m_reportTime; }
        }
        internal virtual double ProjectileSpeed
        {
            get { return m_projectileSpeed; }
        }
        #endregion

        #region Additional Setters/Getters

        internal string GetIDString()
        {
            string idString = "(" + Type + "" + ServerID + "," + Name + ")";

            return idString;
        }

        /// <summary>
        /// Specific Type of damage done by the entity
        /// </summary>
        /// <param name="damageType"></param>
        /// <returns></returns>
        internal float GetCombinedDamageType(int damageType)
        {
            float totalDamage = m_compiledStats.GetCombinedDamageType(damageType);//m_damageTypes[damageType] + m_damageTypeModifiers[damageType];
            return totalDamage;
        }

        /// <summary>
        /// Special Resistances or bonuses to the entity
        /// </summary>
        /// <param name="bonusType"></param>
        /// <returns></returns>
        internal int GetBonusType(int bonusType)
        {
            int totalBonus = (int)m_compiledStats.GetBonusType(bonusType);//m_bonusTypes[bonusType] + m_bonusTypeModifiers[bonusType];
            if (totalBonus < 0)
            {
                totalBonus = 0;
            }
            return totalBonus;
        }

        /// <summary>
        /// Types of damage the entity has immunity to
        /// </summary>
        /// <param name="immunityType"></param>
        /// <returns></returns>
        internal int GetAvoidanceType(AVOIDANCE_TYPE avoidanceType)
        {
            int totalAvoidance = (int)Math.Ceiling(m_compiledStats.GetAvoidanceType(avoidanceType));//m_immunityTypes[immunityType] + m_immunityTypeModifiers[immunityType];
            return totalAvoidance;
        }

        internal bool IsImmuneToType(int bonusType)
        {
            bool isImmune = false;

            float immunity = CompiledStats.GetImmunityType(bonusType);
            if (immunity > 0)
            {
                int immunityMulty = 10000;

                float immunityChance = immunity * immunityMulty;
                float result = Program.getRandomNumber(immunityMulty);
                if (immunityChance > result)
                {
                    isImmune = true;
                }
            }

            return isImmune;
        }

        internal float GetRemainingDamage(int bonusType)
        {
            float remainingDamage = 1.0f;
            float reduction = CompiledStats.GetDamageReductionType(bonusType);
            if (reduction != 0)
            {
                remainingDamage -= reduction;
                if (remainingDamage < 0)
                {
                    remainingDamage = 0;
                }
            }
            return remainingDamage;
        }

        virtual public float getStatModifier(STAT_TYPE stat, float divisor)
        {
            return 0;
        }

        internal virtual int GetRelevantLevel(CombatEntity entity)
        {
            return this.Level;
        }

        internal virtual int getAbilityLevel(ABILITY_TYPE ability_id)
        {
            return 0;
        }

        internal virtual CharacterAbility getAbilityById(ABILITY_TYPE ability_id)
        {
            return null;
        }

        /// <summary>
        /// returns the amount that the base speed will be multiplied be to get the current max speed
        /// </summary>
        /// <returns></returns>
        internal float GetSpeedModMultiplier()
        {
            float amount = 0; //0;

            if (m_compiledStats.RunSpeed > 0)
            {
                //amount = m_speedEffectModifier/100 ;
                amount = m_baseStats.RunSpeed / m_compiledStats.RunSpeed;
            }

            return amount;
        }
        
        #endregion

        #region Constructor & destroy

        public CombatEntity()
        {
            m_skillList = new bool[numberOfSkills];
            for (int i = 0; i < m_skillList.Length; i++)
            {
                m_skillList[i] = false;
            }
            m_hostileEntities = new List<CombatEntity>();
            m_currentCharacterEffects = new List<CharacterEffect>();
            m_EntitySkills = new List<EntitySkill>();
            m_combatActionListThisFrame = new List<CombatDamageMessageData>();
            m_damageListThisFrame = new List<CombatDamageMessageData>();
            m_timeAtLastAttack = 0;
        }

        internal virtual void DestroyEntity()
        {
            DestroyInterestLists();
            CurrentPartition = null;
            if (m_lockOwner != null)
            {
                m_lockOwner.ResignOwnership(this);
            }
        }
        #endregion

        #region main update loop

        public bool Update(ActorPosition position)
        {
            bool statsUpdate = false;
            double currentTime = Program.MainUpdateLoopStartTime();//NetTime.Now;
            m_currentPosition = position;
            double regtime = RegenTime;


            CheckInterestData(currentTime);
            if (InCombat)
            {
                //regtime *= 2;
                /*if (m_NextAttackTarget != null && m_NextAttackTarget.Dead)
                {
                    m_NextAttackTarget = null;
                    SkillFailedConditions();
                }
                */
                if (ConductingHostileAction() == false /*&& m_NextAttackTarget == null*/)
                {
                    //is anything attacking the entity
                    if ((m_hostileEntities.Count == 0) && ((currentTime - m_lastDamageTime) > hostileTimeOut))
                    {
                        InCombat = false;
                    }


                }
            }

            if (!Dead && currentTime - m_timeLastRegen > regtime)
            {

                if (InCombat == false && m_statusPreventsActions.Regen == false)
                {
                    if (CurrentHealth < MaxHealth || CurrentEnergy < MaxEnergy || CurrentConcentrationFishing < MaxConcentrationFishing)
                    {
                        SetStatsChangeLevel(STATS_CHANGE_LEVEL.BASIC_CHANGED);
                        statsUpdate = true;
                    }
                    if (CurrentHealth > 0 && CurrentHealth < MaxHealth)
                    {
                        CurrentHealth += HealthRegenPerTick + HW_BASE_HEALTH_REGEN;//Math.Max(1, MaxHealth * PercentageRegenPerTick);
                        if (CurrentHealth > MaxHealth)
                        {
                            CurrentHealth = MaxHealth;
                        }

                        CombatDamageMessageData messageData = new CombatDamageMessageData();
                        TakeDamage(messageData);
                    }
                    if (CurrentHealth > 0 && CurrentEnergy < MaxEnergy)
                    {
                        CurrentEnergy += EnergyRegenPerTick + HW_BASE_ENERGY_REGEN;//Math.Max(1, MaxEnergy / 50);
                        //m_currentEnergy += Math.Max(1, MaxEnergy / 2);
                        if (CurrentEnergy > MaxEnergy)
                        {
                            CurrentEnergy = MaxEnergy;
                        }
                    }

                    //#FISH always flag off regardless
                    if (CurrentHealth > 0 && CurrentConcentrationFishing <= MaxConcentrationFishing)
                    {
                        //flag off concentration death
                        ConcentrationFishDepleted = false;
                    }
                    //#FISH if we're not Dead and our concentration is below the max, we want to rengereate here
                    if (CurrentHealth > 0 && CurrentConcentrationFishing < MaxConcentrationFishing)
                    {
                        CurrentConcentrationFishing += ConcentrationRegenPerTick + HW_BASE_CONCENTRATION_REGEN;
                        CurrentConcentrationFishing = (int)MathHelper.Clamp(CurrentConcentrationFishing, 0, MaxConcentrationFishing);


                        //need to use a combat message to add concentration
                        CombatDamageMessageData messageData = new CombatDamageMessageData();
                        TakeDamage(messageData);
                    }
                }
                else
                {
                    if (m_statusPreventsActions.Regen == false)
                    {
                        if ((CurrentHealth < MaxHealth && HealthRegenPerTickCombat != 0) || (CurrentEnergy < MaxEnergy && EnergyRegenPerTickCombat != 0))
                        {
                            SetStatsChangeLevel(STATS_CHANGE_LEVEL.BASIC_CHANGED);
                            statsUpdate = true;
                        }
                        if (HealthRegenPerTickCombat != 0 && CurrentHealth > 0 && CurrentHealth < MaxHealth)
                        {
                            CurrentHealth += HealthRegenPerTickCombat;//Math.Max(1, MaxHealth * PercentageRegenPerTick);
                            if (CurrentHealth > MaxHealth)
                            {
                                CurrentHealth = MaxHealth;

                            }
                            CombatDamageMessageData messageData = new CombatDamageMessageData();
                            TakeDamage(messageData);
                        }
                        if (EnergyRegenPerTickCombat != 0 && CurrentHealth > 0 && CurrentEnergy < MaxEnergy)
                        {
                            CurrentEnergy += EnergyRegenPerTickCombat;//Math.Max(1, MaxEnergy / 50);
                            //m_currentEnergy += Math.Max(1, MaxEnergy / 2);
                            if (CurrentEnergy > MaxEnergy)
                            {
                                CurrentEnergy = MaxEnergy;
                            }
                        }


                    }

                }
                m_timeLastRegen = currentTime;
            }
            //this should be done before status effects to ensure destriyed effects are sent
            for (int i = 0; i < m_areaEffectsList.Count; i++)
            {
                m_areaEffectsList[i].UpdateEffect(currentTime, this);
            }
            if (m_areasEffectsToBeChecked == true)
            {
                CheckAreaEffects();
                m_areasEffectsToBeChecked = false;
            }
            if (m_lockOwnerChanged == true)
            {
                SendLockDataChanged();
            }


            if (!Dead)
            {
                bool statusChangedStats = CharacterEffectManager.UpdateCharacterEffects(this);
                if (statusChangedStats == true)
                {
                    SetStatsChangeLevel(STATS_CHANGE_LEVEL.COMPILE_REQUIRED);
                    statsUpdate = true;
                }
            }

            //if the status effects have changes then resent stats
            if (StatusListChanged)
            {
                //statsUpdate=true;
                CharacterEffectManager.UpdateCombatStats(this);
                //the StatusListChanged will be set to false once the changed status message is sent
            }
            if (CurrentEnergy > MaxEnergy)
            {
                CurrentEnergy = MaxEnergy;
                SetStatsChangeLevel(STATS_CHANGE_LEVEL.BASIC_CHANGED);
                statsUpdate = true;
            }
            if (CurrentHealth > MaxHealth)
            {
                CurrentHealth = MaxHealth;
                SetStatsChangeLevel(STATS_CHANGE_LEVEL.BASIC_CHANGED);
                statsUpdate = true;
            }
            if (CurrentConcentrationFishing > MaxConcentrationFishing)
            {
                CurrentConcentrationFishing = MaxConcentrationFishing;
                SetStatsChangeLevel(STATS_CHANGE_LEVEL.BASIC_CHANGED);
                statsUpdate = true;
            }
            //check combat stats are in a valid condition
            //skills must have targets and targets must have skills
            if (NextSkill != null || NextSkillTarget != null)
            {
                if (NextSkill == null || NextSkillTarget == null)
                {
                    NextSkill = null;
                    NextSkillTarget = null;
                }

            }
            if (CurrentSkill != null || CurrentSkillTarget != null)
            {
                if (CurrentSkill == null || CurrentSkillTarget == null)
                {
                    CurrentSkill = null;
                    CurrentSkillTarget = null;
                }

            }

            DealWithChangeInStatusEffects(currentTime);
            return statsUpdate;
        }

        #endregion

        #region aggro/hostile/damage type of methods

        internal void ConductedHotileAction()
        {

            InCombat = true;
            double currentTime = Program.MainUpdateLoopStartTime();
            m_lastDamageTime = currentTime;
        }

        internal void AffectedByHotileAction()
        {
            InCombat = true;
            double currentTime = Program.MainUpdateLoopStartTime();
            m_lastDamageTime = currentTime;

        }

        void CancelStatusEffectsDueToDamage(CombatDamageMessageData theDamage)
        {
            for (int currentEffectIndex = 0; currentEffectIndex < m_currentCharacterEffects.Count; currentEffectIndex++)
            {
                StatusEffect currectEffect = m_currentCharacterEffects[currentEffectIndex].StatusEffect;
                if (currectEffect != null)
                {
                    bool effectRemoved = false;
                    switch (currectEffect.Template.EffectType)
                    {
                        case EFFECT_TYPE.FROZEN:
                            {
                                //only aggressive damage should cancel it
                                if (theDamage.Aggressive && theDamage.DamageTaken > 0)
                                {
                                    //check it's not from a freeze skill
                                    if ((theDamage.AttackType == (int)CombatManager.ATTACK_TYPE.SKILL && theDamage.SkillID == (int)SKILL_TYPE.FREEZE) == false)
                                    {
                                        currectEffect.Complete();
                                        effectRemoved = true;
                                    }

                                }
                                break;
                            }
                        default:
                            {
                                break;
                            }

                    }
                    if (currectEffect.Template.BreakOnAggressive)
                    {
                        if (theDamage.Aggressive == true)
                        {
                            currectEffect.Complete();
                            effectRemoved = true;
                        }
                    }
                    if (effectRemoved == true)
                    {
                        if (Type == CombatEntity.EntityType.Player)
                        {
                            Character theCharacter = (Character)this;
                            if (theCharacter != null)
                            {
								string locText = Localiser.GetString(textDB, theCharacter.m_player, (int)CombatEntityTextDB.TextID.STATUS_REMOVED);
								string locStatusEffectName = StatusEffectTemplateManager.GetLocaliseStatusEffectName(theCharacter.m_player, (int)currectEffect.Template.StatusEffectID);
								locText = string.Format(locText, locStatusEffectName);
								Program.processor.sendSystemMessage(locText, theCharacter.m_player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This will remove the damage from the health and signal that the entities health has changed so needs to be resent
        /// </summary>
        /// <param name="theDamage"></param>
        internal virtual void TakeDamage(CombatDamageMessageData theDamage)
        {
            //if the combat manager is null then the damage will be dropped
            if (TheCombatManager == null)
            {
                return;
            }
            //boss survival hack

            /*if(Type == EntityType.Player && theDamage.Aggressive==true)
             {
                 theDamage.DamageTaken =1;
                 theDamage.SentDamage=1;
             }*/
            //InterruptStatusEffectsDueToHostileAction(theDamage.DamageTaken, theDamage.CasterLink, (CombatManager.ATTACK_TYPE) theDamage.AttackType, theDamage.SkillID, theDamage.Aggressive);
            int altereddamage = theDamage.DamageTaken;
            if (m_statusCancelConditions.TakeDamage == true)
            {
                CancelStatusEffectsDueToDamage(theDamage);
            }
            if (theDamage.DamageTaken > 0)
            {
                if (theDamage.Gathering == LevelType.none)
                {
                    altereddamage = TheCombatManager.AlterDamageDueToEffects(this, theDamage.DamageTaken, true);
                    //bool sendUpdateStats = false;
                    if (altereddamage != theDamage.DamageTaken && Type == CombatEntity.EntityType.Player)
                    {
                        //sendUpdateStats = true;

                        SetStatsChangeLevel(CombatEntity.STATS_CHANGE_LEVEL.BASIC_CHANGED);//((Character)m_owner).m_statsUpdated=true;

                    }
                }

            }
            //if the damage is different than the origional guess
            if (altereddamage > theDamage.EstimatedDamage)
            {
                CalculatedDamage tempDamage = new CalculatedDamage(theDamage.EstimatedDamage, theDamage.SentDamage);
                int alteredAmount = theDamage.EstimatedDamage - altereddamage;

                theDamage.SentDamage = tempDamage.GetAmendedOriginalDamage(alteredAmount); //theDamage.SentDamage - altereddamage;
                AddToCombatList(theDamage);
                //and add it to the caster's list
                //so it can be resent to the client
                if (theDamage.CasterLink != null && theDamage.CasterLink != this)
                {
                    //caster.CombatListThisFrame.Add(messageData);
                    theDamage.CasterLink.AddToCombatList(theDamage);
                }
            }


            m_latestDamage = altereddamage;//theDamage.DamageTaken;
            //check if it is going to kill the mob then only apply as much damage as health remaining
            int damageToApply = altereddamage;// theDamage.DamageTaken;
            if (damageToApply > CurrentHealth)
            {
                damageToApply = CurrentHealth;
            }

            //If the damage to be applied is for a fish

            if (theDamage.TargetLink != null)
                if (theDamage.TargetLink.Gathering == LevelType.fish)
                {
                    //and if the damage is more than 60% of the fish max health
                    if (damageToApply > MaxHealth * 0.6)
                    {
                        //only apply 60%
                        damageToApply = (int)(MaxHealth * 0.6);


                    }

                }

            //if dead then don't actually take any damage
            if (Dead == true)
            {
                damageToApply = 0;
            }
            if (theDamage.AttackType == (int)CombatManager.ATTACK_TYPE.ATTACK && theDamage.CasterLink != null)
            {
                TheCombatManager.DoReflectDamage(theDamage.CasterLink, this);
            }
            //check if the damage has interrupted the skill 
            if (damageToApply > 0 && (theDamage.AttackType == (int)CombatManager.ATTACK_TYPE.ATTACK || theDamage.AttackType == (int)CombatManager.ATTACK_TYPE.SKILL))
            {

                bool skillInterupted = AttemptToProtectSkillCharging();
                if (skillInterupted == true)
                {
                    TheCombatManager.EntitiesWithCancelledSkills.Add(this);
                }
            }
            double aggroMod = theDamage.AggroModifier;
            //reduce the aggro of a miss
            if (theDamage.Reaction == (int)CombatManager.COMBAT_REACTION_TYPES.CRT_MISS)
            {
                aggroMod = aggroMod * 0.5;
            }
            //take any action required due to taking damage
            ActUponDamage(damageToApply, theDamage.CasterLink, (CombatManager.ATTACK_TYPE)theDamage.AttackType, theDamage.SkillID, theDamage.Aggressive, theDamage.AggroModifier);
            //update the health


            #region alter health/concentration based on damage gathering


            switch (theDamage.Gathering)
            {

                case (LevelType.none):

                    CurrentHealth = CurrentHealth - damageToApply;
                    CurrentHealth = (int)MathHelper.Clamp(CurrentHealth, 0, MaxHealth);
                    break;
                case (LevelType.fish):
                    CurrentConcentrationFishing = CurrentConcentrationFishing - damageToApply;
                    CurrentConcentrationFishing = (int)MathHelper.Clamp(CurrentConcentrationFishing, 0, MaxConcentrationFishing);
                    break;
            }


            #endregion



            //if they  have just died then remember who killed them
            if (theDamage.Aggressive == true && CurrentHealth <= 0 && Dead == false)
            {
                m_killer = theDamage.CasterLink;
            }
            //remember that hostile activity has happened
            if (theDamage.Aggressive == true)
            {
                ConductedHotileAction();
            }


            AddToDamageList(theDamage);
            //notify the caster that the damage has been carried out
            if (theDamage.CasterLink != null && theDamage.CasterLink.Destroyed == false)
            {
                theDamage.CasterLink.DamageApplied(theDamage);
            }

        }

        /// <summary>
        /// This creates the base damage message which must be saved by the combat manager and applied at the correct time
        /// </summary>
        /// <param name="damage"></param>
        /// <param name="caster"></param>
        /// <param name="attackType"></param>
        /// <param name="attackID"></param>
        /// <param name="aggressive"></param>
        /// <param name="reaction"></param>
        /// <returns></returns>
        internal CombatDamageMessageData TakeDamage(int damage, int estimatedDamage, CombatEntity caster, CombatManager.ATTACK_TYPE attackType, int attackID, bool aggressive, int reaction, int sentDamage, int critical)
        {
            //create the message
            int casterID = -1;
            EntityType casterType = EntityType.Mob;
            //if it hit, but the damage is 0 and its a skill. tell the client to not play the damage
            //if it has not been changed to a specific reaction
            if (damage == 0 &&
                (attackType == CombatManager.ATTACK_TYPE.SKILL || attackType == CombatManager.ATTACK_TYPE.AOE_SKILL) &&
                (reaction == (int)CombatManager.COMBAT_REACTION_TYPES.CRT_SKILL_HIT_AGG || reaction == (int)CombatManager.COMBAT_REACTION_TYPES.CRT_SKILL_HIT_POS))
            {
                reaction = (int)CombatManager.COMBAT_REACTION_TYPES.CRT_NO_REACTION;
            }
            if (caster != null)
            {
                casterID = caster.ServerID;
                casterType = caster.Type;
            }
            int targetID = ServerID;
            //what type of entity did the damage to what
            CombatManager.DamageMessageType dmgMsgType = CombatManager.DamageMessageType.PlayerToMob;


            if ((casterType == EntityType.Mob) && (Type == EntityType.Mob))
            {
                dmgMsgType = CombatManager.DamageMessageType.MobToMob;
            }
            else if ((casterType == EntityType.Mob) && (Type == EntityType.Player))
            {
                dmgMsgType = CombatManager.DamageMessageType.MobToPlayer;
            }
            else if ((casterType == CombatEntity.EntityType.Player) && (Type == CombatEntity.EntityType.Player))
            {
                dmgMsgType = CombatManager.DamageMessageType.PlayerToPlayer;
            }
            // Are these actually not used? 
            else if ((casterType == CombatEntity.EntityType.Node) && (Type == CombatEntity.EntityType.Player))
            {
                dmgMsgType = CombatManager.DamageMessageType.PlayerToNode;
            }
            else if ((casterType == CombatEntity.EntityType.Player) && (Type == CombatEntity.EntityType.Node))
            {
                dmgMsgType = CombatManager.DamageMessageType.NodeToPlayer;
            }


            /*InterruptStatusEffectsDueToHostileAction(damage, caster, attackType, attackID, aggressive);
            m_latestDamage = damage;
            ActUponDamage(damage, caster, attackType, attackID, aggressive);
            */
            //whats the estimated new health
            int targetHealth = CurrentHealth - damage;


            CombatDamageMessageData messageData = new CombatDamageMessageData();
            messageData.CasterID = casterID;
            m_currentDamageID++;
            if (m_currentDamageID > 5000)
            {
                m_currentDamageID = 0;
            }
            messageData.DamageID = m_currentDamageID;
            messageData.DamageTaken = damage;
            messageData.SentDamage = sentDamage;
            messageData.EstimatedDamage = estimatedDamage;
            messageData.MessageType = dmgMsgType;
            messageData.TargetID = targetID;
            messageData.TargetHealth = targetHealth;
            messageData.SkillID = attackID;
            messageData.AttackType = (int)attackType;
            messageData.Reaction = reaction;
            messageData.Aggressive = aggressive;
            messageData.CasterLink = caster;
            messageData.TargetLink = this;
            messageData.Gathering = LevelType.none; //default to none

            messageData.Critical = critical;

            //add this to this entity's list
            //m_combatActionListThisFrame.Add(messageData);
            AddToCombatList(messageData);
            //and add it to the caster's list,
            if (caster != null && caster != this)
            {
                //caster.CombatListThisFrame.Add(messageData);
                caster.AddToCombatList(messageData);
            }
            return messageData;
        }

        void AddToCombatList(CombatDamageMessageData theData)
        {
            if (CurrentZone != null)
            {
                CurrentZone.EntityAffectedByAction(theData.CasterLink, theData.TargetLink);
            }
            CombatListThisFrame.Add(theData);
        }

        void AddToDamageList(CombatDamageMessageData theData)
        {
            if (CurrentZone != null)
            {
                CurrentZone.EntityDamaged(this);
            }
            DamageListThisFrame.Add(theData);
        }

        virtual internal void ActUponDamage(int damage, CombatEntity caster, CombatManager.ATTACK_TYPE attackType, int attackID, bool aggressive, double AggroModifier)
        {

            if (caster != null && aggressive)
            {
                m_lastAttacker = caster;
                //m_NextAttackTarget = caster;
                //InCombat = true;
                ConductedHotileAction();
            }
        }

        virtual internal void EntityAidedByEntity(CombatEntity targetedEnt, CombatEntity assistingEnt, float AggroOfAssist)
        {

        }

        internal virtual void AddToAggroValueToExistingData(CombatEntity entityToAddAgro, float aggroToAdd)
        {

        }

        internal virtual void AddToAggroValue(CombatEntity entityToAddAgro, float aggroToAdd)
        {

        }

        internal virtual void ClearAggroForEntity(CombatEntity entityToChangeAggro)
        {

        }

        public void TakeEnergyDamage(int damage, CombatEntity caster, bool aggressive)
        {
            InterruptStatusEffectsDueToHostileAction(damage, caster, 0, 0, aggressive);
            if (caster != null && aggressive)
            {
                m_lastAttacker = caster;
                //m_NextAttackTarget = caster;
            }
            CurrentEnergy = CurrentEnergy - damage;
            if (aggressive == true)
            {
                AffectedByHotileAction();//InCombat = true;
            }
            if (damage != 0)
            {
                SetStatsChangeLevel(STATS_CHANGE_LEVEL.BASIC_CHANGED);
            }
            //m_lastDamageTime = NetTime.Now;
        }

        public void TakeConcentrationDamage(int damage, CombatEntity caster, bool aggressive)
        {
            InterruptStatusEffectsDueToHostileAction(damage, caster, 0, 0, aggressive);
            if (caster != null && aggressive)
            {
                m_lastAttacker = caster;
            }
            CurrentConcentrationFishing -= damage;
            if (aggressive == true)
            {
                AffectedByHotileAction();
            }
            if (damage != 0)
            {
                SetStatsChangeLevel(STATS_CHANGE_LEVEL.BASIC_CHANGED);
            }
        }

        #endregion

        #region entity checks

        internal bool EntityIsHostileTowards(CombatEntity targetEntity)
        {
            if ((AttackTarget == targetEntity) || (NextSkillTarget == targetEntity) || (CurrentSkillTarget == targetEntity))
            {
                return true;
            }

            return false;
        }

        internal void RemoveFromHostileEntities(CombatEntity nolongerHostileEntity)
        {
            m_hostileEntities.Remove(nolongerHostileEntity);
        }

        internal virtual bool ConductingHostileAction()
        {
            //attacking is hostile
            if ((m_attackTarget != null))
            {
                return true;
            }
           
            //skills against mobs are hostile
            if ((m_currentSkillTarget != null) && (m_currentSkillTarget.IsEnemyOf(this) == true))
            {
                return true;
            }           

            return false;
        }

        #endregion

        #region effects & stats methods
        
        internal virtual void SetStatsChangeLevel(STATS_CHANGE_LEVEL newLevel)
        {

        }

        internal void ResetStatModifiers()
        {
            m_statusStats.ResetStats(0);
            m_statusStatsMultipliers.ResetStats(1);
            m_statusPreventsActions.Reset();
            m_statusCancelConditions.Reset();
        }

        public void UpdateCancelConditions()
        {
            if (StatusCancelConditions.Attack && AttackTarget != null)
            {
                CancelEffectsDueToAttack();
            }
            if (StatusCancelConditions.Skills && CurrentSkill != null)
            {
                CancelEffectsDueToSkillUse(CurrentSkill, false);
            }
            if (StatusCancelConditions.Move && CurrentPosition.m_currentSpeed != 0)
            {
                CancelEffectsDueToMove();
            }
        }

        internal void CancelEffectsDueToMove()
        {
            for (int currentEffect = 0; currentEffect < m_currentCharacterEffects.Count; currentEffect++)
            {
                CharacterEffect currentStatusEffect = m_currentCharacterEffects[currentEffect];
                if (currentStatusEffect.StatusEffect == null)
                    continue;

                switch (currentStatusEffect.StatusEffect.Template.EffectType)
                {
                    case EFFECT_TYPE.PLAYING_DEAD:
                        {
                            currentStatusEffect.StatusEffect.Complete();
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
        }

        internal void CancelEffectsDueToAttack()
        {
            for (int currentEffect = 0; currentEffect < m_currentCharacterEffects.Count; currentEffect++)
            {
                CharacterEffect currentStatusEffect = m_currentCharacterEffects[currentEffect];
                if (currentStatusEffect.StatusEffect == null)
                    continue;

                switch (currentStatusEffect.StatusEffect.Template.EffectType)
                {
                    case EFFECT_TYPE.PLAYING_DEAD:
                        {
                            currentStatusEffect.StatusEffect.Complete();
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
                if (currentStatusEffect.StatusEffect.Template.BreakOnAggressive)
                {
                    currentStatusEffect.StatusEffect.Complete();
                }
            }
        }

        internal void CancelEffectsDueToSkillUse(EntitySkill theSkill, bool complete)
        {
            for (int currentEffect = 0; currentEffect < m_currentCharacterEffects.Count; currentEffect++)
            {
                CharacterEffect currentStatusEffect = m_currentCharacterEffects[currentEffect];
                if (currentStatusEffect.StatusEffect == null)
                    continue;

                switch (currentStatusEffect.StatusEffect.Template.EffectType)
                {
                    case EFFECT_TYPE.PLAYING_DEAD:
                        {
                            currentStatusEffect.StatusEffect.Complete();
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
                if (currentStatusEffect.StatusEffect.Template.BreakOnAggressive && theSkill.Template.CastTargetGroup == SkillTemplate.CAST_TARGET.ENEMY)
                {
                    bool hiddenSneakAttack = (currentStatusEffect.StatusEffect.Template.EffectType == EFFECT_TYPE.HIDE && theSkill.SkillID == SKILL_TYPE.SNEAKY_ATTACK);
                    //completed skills always stop the status 
                    if (hiddenSneakAttack == false || complete == true)
                    {
                        currentStatusEffect.StatusEffect.Complete();
                    }
                }
            }
        }
        
        /// <summary>
        /// Looks through all current status effect on character and returns one that mathes ID, else returns null
        /// </summary>
        /// <param name="effectID"></param>
        /// <returns>null if status effect that mathces id found</returns>
        internal StatusEffect GetStatusEffectForID(EFFECT_ID effectID)
        {
            for (int currentEffect = 0; currentEffect < m_currentCharacterEffects.Count; currentEffect++)
            {
                StatusEffect currentStatusEffect = m_currentCharacterEffects[currentEffect].StatusEffect;
                if (currentStatusEffect == null)
                    continue;

                if ((currentStatusEffect != null) && (currentStatusEffect.Template != null) &&
                    (currentStatusEffect.Template.StatusEffectID == effectID))
                {
                    return currentStatusEffect;
                }
            }
            return null;

        }

        internal bool InterruptStatusEffectsDueToHostileAction(int damage, CombatEntity caster, CombatManager.ATTACK_TYPE attackType, int attackID, bool aggressive)
        {
            bool statusEffectsInterrupted = false;
            for (int currentEffect = 0; currentEffect < m_currentCharacterEffects.Count; currentEffect++)
            {
                bool currentInterrupted = false;
                StatusEffect currentStatusEffect = m_currentCharacterEffects[currentEffect].StatusEffect;
                if (currentStatusEffect == null)
                    continue;

                switch (currentStatusEffect.Template.StatusEffectID)
                {
                    case EFFECT_ID.MEDITATE:
                    case EFFECT_ID.HIDE:
                    case EFFECT_ID.CAMOUFLAGE:
                    case EFFECT_ID.INVISIBILITY_POTION:
                    case EFFECT_ID.RECUPERATE:
                        {
                            if (aggressive == true)
                            {
                                currentStatusEffect.Complete();
                                currentInterrupted = true;
                            }
                            break;
                        }

                }
                if (currentInterrupted)
                {
                    if (Type == EntityType.Player)
                    {
                        Character theCharacter = (Character)this;
						string locText = Localiser.GetString(textDB, theCharacter.m_player, (int)CombatEntityTextDB.TextID.STATUS_REMOVED);
						string locStatusEffectName = StatusEffectTemplateManager.GetLocaliseStatusEffectName(theCharacter.m_player, (int)currentStatusEffect.Template.StatusEffectID);
						locText = string.Format(locText, locStatusEffectName);
						Program.processor.sendSystemMessage(locText, theCharacter.m_player, false, SYSTEM_MESSAGE_TYPE.BATTLE);

                    }
                }
            }

            return statusEffectsInterrupted;
        }

        // New function to allow status effects of lower 'class level' to potentially overwrite higher ones if their effect is stronger (due to stats)
        // Opposite is also true - lower level but higher effect SE's should not be overwritten by higher level but weaker value ones
        private bool IsExistingStatusEffectStronger(StatusEffect existingStatusEffect, StatusEffect newStatusEffect)
        {
            EFFECT_TYPE newStatusEffectType = existingStatusEffect.Template.EffectType;

            switch (newStatusEffectType)
            {
                case EFFECT_TYPE.CHANGE_ARMOUR:
                case EFFECT_TYPE.HEALTH_REGEN:
                case EFFECT_TYPE.ENERGY_REGEN:
                case EFFECT_TYPE.DOT:
                case EFFECT_TYPE.CHANGE_ATTACK:
                case EFFECT_TYPE.DAMAGE_SHIELD:
                case EFFECT_TYPE.BUFF_RANGE_ATTACK_AND_DAMAGE:
                case EFFECT_TYPE.CHANGE_MAX_HEALTH:
                case EFFECT_TYPE.CHANGE_MAX_ENERGY:
                case EFFECT_TYPE.CHANGE_RESISTANCE:
                case EFFECT_TYPE.CHANGE_DEFENCE:
                case EFFECT_TYPE.CHANGE_DAMAGE_TYPE:
                case EFFECT_TYPE.HEALTH_AND_ENERGY_REGEN:
                case EFFECT_TYPE.ICE_SKILL_BOOST:
                case EFFECT_TYPE.FIRE_SKILL_BOOST:
                case EFFECT_TYPE.DAMAGE_MULTI_NO_PLAYERS:
                case EFFECT_TYPE.CHANGE_RUN_SPEED:
                case EFFECT_TYPE.CHANGE_RUN_SPEED_LEVEL_BASED:
                case EFFECT_TYPE.CHANGE_ATTACK_SPEED:
                case EFFECT_TYPE.CHANGE_SIZE:
                case EFFECT_TYPE.CHANGE_FORTITUDE:
                case EFFECT_TYPE.CHANGE_EVASIONS:
                case EFFECT_TYPE.CHANGE_ABILITY_GAIN:
                case EFFECT_TYPE.CONCENTRATION_REGEN:
                {
                    Program.Display("STATUS EFFECT - getModifiedAmount");
                    return existingStatusEffect.m_effectLevel.getModifiedAmount(existingStatusEffect.CasterAbilityLevel, existingStatusEffect.StatModifier) >
                           newStatusEffect.m_effectLevel.getModifiedAmount(newStatusEffect.CasterAbilityLevel, newStatusEffect.StatModifier);
                }
                case EFFECT_TYPE.ENERGY_SHIELD:
                case EFFECT_TYPE.ENERGY_SHIELD_2:
                {
                    Program.Display("STATUS EFFECT - CurrentAmount");
                    return existingStatusEffect.CurrentAmount < newStatusEffect.CurrentAmount;
                }
                default:
                {
                    Program.Display("STATUS EFFECT - level");
                    return existingStatusEffect.m_effectLevel.m_class_level > newStatusEffect.m_effectLevel.m_class_level;
                }
            }
        }

        internal virtual StatusEffect InflictNewStatusEffect(EFFECT_ID newEffect, CombatEntity caster, int level, bool aggressive, bool PVP, float statModifier)
        {
            StatusEffectTemplate newTemplate = StatusEffectTemplateManager.GetStatusEffectTemplateForID(newEffect);
            
            // check for aura immunities
            if (Type == EntityType.Mob && newTemplate.IsAuraSubEffect)
            {
                ServerControlledEntity serverControlledCombatEntity = (ServerControlledEntity)this;
                
                if (serverControlledCombatEntity != null && serverControlledCombatEntity.Template != null)
                {
                    int mobTemplateID = serverControlledCombatEntity.Template.m_templateID;
                    
                    // immunities - check blacklist
                    if (StatusEffectTemplateManager.MobIsImmuneToAura(mobTemplateID, (int)newTemplate.StatusEffectID))
                        return null;

                    // whitelist 
                    if (StatusEffectTemplateManager.AuraHasWhitelist((int)newTemplate.StatusEffectID) == true)
                    {
                        if (StatusEffectTemplateManager.AuraWhitelistContainsMob((int)newTemplate.StatusEffectID, mobTemplateID) == false)
                            return null;
                    }
                }
            }
            
            if (newTemplate == null)
            {
                Program.Display("ERROR: InflictNewStatusEffect failed to find status effect template for status effect " + (int)newEffect);
                return null;
            }

            double startTime = Program.MainUpdateLoopStartTime(); //NetTime.Now;
            bool effectBounced = false;
            //check the status effect isn't currently in effect

            StatusEffect newStatusEffect = new StatusEffect(startTime, newTemplate, level, this, caster, PVP, statModifier);

            List<CharacterEffect> statusEffectsForRemoval = new List<CharacterEffect>();

            for (int i = 0; i < m_currentCharacterEffects.Count; i++)
            {
                if (m_currentCharacterEffects[i].StatusEffect == null)
                    continue;

                // If the same class of effect is active...
                if (m_currentCharacterEffects[i].StatusEffect.Template.EffectClass == newTemplate.EffectClass)
                {
                    // ... check whether the new effect is higher or lower in strength. If current effect is higher...
                    //if (IsExistingStatusEffectStronger(m_currentCharacterEffects[i].StatusEffect, newStatusEffect))
                    if (m_currentCharacterEffects[i].StatusEffect.m_effectLevel.m_class_level > newStatusEffect.m_effectLevel.m_class_level)
                    {
                        // ... then the new effect 'bounces'. 
                        effectBounced = true;
                        break;
                    }
                    // This 'refresh' up status effects should be limited to AuraSubEffects only
                    else if (m_currentCharacterEffects[i].StatusEffect.Template.IsAuraSubEffect && m_currentCharacterEffects[i].StatusEffect.m_effectLevel.m_class_level == newStatusEffect.m_effectLevel.m_class_level)
                    {
                        m_currentCharacterEffects[i].StatusEffect.StartTime = Program.MainUpdateLoopStartTime();
                        effectBounced = true;
                        break;
                    }
                    else
                    {
                        statusEffectsForRemoval.Add(m_currentCharacterEffects[i]);
                        break;
                    }
                }
            }

            if (!effectBounced)
            {
                if (newStatusEffect.Template.EffectType == EFFECT_TYPE.ROOT
                    || newStatusEffect.Template.EffectType == EFFECT_TYPE.CHANGE_RUN_SPEED_LEVEL_BASED
                    || newStatusEffect.Template.EffectType == EFFECT_TYPE.HALF_ATTACK_SPEED
                    || newStatusEffect.Template.EffectType == EFFECT_TYPE.FROZEN
                    || newStatusEffect.Template.EffectType == EFFECT_TYPE.STUN_2)
                {
                    if (Level > newStatusEffect.m_effectLevel.getUnModifiedAmount())
                    {
                        effectBounced = true;
                        //m_currentPosition.m_currentSpeed = 0;
                        //send a message to the caster
                        if (newStatusEffect.TheCaster != null && newStatusEffect.TheCaster.Type == CombatEntity.EntityType.Player)
                        {
                            Character theCharacter = (Character)newStatusEffect.TheCaster;
							string locText = Localiser.GetString(textDB, theCharacter.m_player, (int)CombatEntityTextDB.TextID.TARGET_LEVEL_TOO_HIGH);
							Program.processor.sendSystemMessage(locText, theCharacter.m_player, false, SYSTEM_MESSAGE_TYPE.BATTLE);

                        }
                    }
                }
                else if (newStatusEffect.Template.StatusEffectID == EFFECT_ID.STUN)
                {
                    if (Program.getRandomNumberFromZero(100) < newStatusEffect.m_effectLevel.getUnModifiedAmount())
                    {
                        Program.Display("Stun succeeded");
                        effectBounced = true;
                    }
                }
            }
            if (!effectBounced)
            {
                for (int i = 0; i < statusEffectsForRemoval.Count; i++)
                {
                    if (statusEffectsForRemoval[i].StatusEffect != null)
                    {
                        statusEffectsForRemoval[i].StatusEffect.Complete();
                        m_currentCharacterEffects.Remove(statusEffectsForRemoval[i]);
                    }
                }


                if (newStatusEffect.m_effectLevel != null)
                {
                    DealWithNewEffect(newStatusEffect);
                    //m_currentStatusEffects.Add(newStatusEffect);
                }
                else
                {
                    Program.Display("Removed status effect due to null level statusEffectID:" + newStatusEffect.Template.StatusEffectID + " level:" + newStatusEffect.m_statusEffectLevel + " from " + GetIDString());
                }
                //the status List has changed so send update at earliest opportunity
                m_statusListChanged = true;

                return newStatusEffect;
            }
            return null;
        }

        internal virtual StatusEffect InflictNewStatusEffect2(EFFECT_ID newEffect, CombatEntity caster, int level, bool aggressive, bool PVP, float statModifier)
        {
            StatusEffectTemplate newTemplate = StatusEffectTemplateManager.GetStatusEffectTemplateForID(newEffect);

            if (newTemplate == null)
            {
                Program.Display("ERROR: InflictNewStatusEffect failed to find status effect template for status effect " + (int)newEffect);
                return null;
            }

            double startTime = Program.MainUpdateLoopStartTime(); //NetTime.Now;
            bool effectBounced = false;
            //check the status effect isn't currently in effect

            StatusEffect newStatusEffect = new StatusEffect(startTime, newTemplate, level, this, caster, PVP, statModifier);

            List<CharacterEffect> statusEffectsForRemoval = new List<CharacterEffect>();

            for (int i = 0; i < m_currentCharacterEffects.Count; i++)
            {
                if (m_currentCharacterEffects[i].StatusEffect == null)
                    continue;

                // If the same class of effect is active...
                if (m_currentCharacterEffects[i].StatusEffect.Template.EffectClass == newTemplate.EffectClass)
                {
                    // ... check whether the new effect is higher or lower in class 'level'. If current effect is higher...
                    if (m_currentCharacterEffects[i].StatusEffect.m_effectLevel.m_class_level > newStatusEffect.m_effectLevel.m_class_level)
                    {
                        // ... then the new effect 'bounces'. 
                        effectBounced = true;
                        break;
                    }
                    else
                    {
                        // If the new effect is higher, then flag the existing effect to be removed.
                        statusEffectsForRemoval.Add(m_currentCharacterEffects[i]);
                        //Program.Display("ERROR: InflictNewCharacterEffect, added for removal sub-effect id " + m_currentCharacterEffects[i].m_SubEffectId);
                        break;
                    }
                }
            }


            if (!effectBounced)
            {
                if (newStatusEffect.Template.EffectType == EFFECT_TYPE.ROOT
                    || newStatusEffect.Template.EffectType == EFFECT_TYPE.CHANGE_RUN_SPEED_LEVEL_BASED
                    || newStatusEffect.Template.EffectType == EFFECT_TYPE.HALF_ATTACK_SPEED
                    || newStatusEffect.Template.EffectType == EFFECT_TYPE.FROZEN
                    || newStatusEffect.Template.EffectType == EFFECT_TYPE.STUN_2)
                {
                    if (Level > newStatusEffect.m_effectLevel.getUnModifiedAmount())
                    {
                        effectBounced = true;
                        //m_currentPosition.m_currentSpeed = 0;
                        //send a message to the caster
                        if (newStatusEffect.TheCaster != null && newStatusEffect.TheCaster.Type == CombatEntity.EntityType.Player)
                        {
                            Character theCharacter = (Character)newStatusEffect.TheCaster;
							string locText = Localiser.GetString(textDB, theCharacter.m_player, (int)CombatEntityTextDB.TextID.TARGET_LEVEL_TOO_HIGH);
							Program.processor.sendSystemMessage(locText, theCharacter.m_player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
                        }
                    }
                }
                else if (newStatusEffect.Template.StatusEffectID == EFFECT_ID.STUN)
                {
                    if (Program.getRandomNumberFromZero(100) < newStatusEffect.m_effectLevel.getUnModifiedAmount())
                    {
                        Program.Display("Stun succeeded");
                        effectBounced = true;
                    }
                }
            }
            if (!effectBounced)
            {
                for (int i = 0; i < statusEffectsForRemoval.Count; i++)
                {
                    if (statusEffectsForRemoval[i].StatusEffect != null)
                    {
                        statusEffectsForRemoval[i].StatusEffect.Complete();
                        m_currentCharacterEffects.Remove(statusEffectsForRemoval[i]);
                    }
                }


                if (newStatusEffect.m_effectLevel != null)
                {
                    DealWithNewEffect(newStatusEffect);
                    //m_currentStatusEffects.Add(newStatusEffect);
                }
                else
                {
                    Program.Display("Removed status effect due to null level statusEffectID:" + newStatusEffect.Template.StatusEffectID + " level:" + newStatusEffect.m_statusEffectLevel + " from " + GetIDString());
                }
                //the status List has changed so send update at earliest opportunity
                m_statusListChanged = true;

                return newStatusEffect;
            }
            return null;
        }

        internal void DealWithNewEffect(StatusEffect newStatusEffect)
        {
            switch (newStatusEffect.Template.EffectType)
            {
                case EFFECT_TYPE.PLAYING_DEAD:
                    {
                        //this needs to
                        //interrupt skills
                        InterruptSkills();
                        //interrupt attacks
                        if (AttackTarget != null)
                        {
                            TheCombatManager.StopAttacking(this);
                        }
                        //stop the player
                        StopTheEntity();
                        break;
                    }
                case EFFECT_TYPE.FROZEN:
                    {
                        //this needs to
                        //interrupt skills
                        InterruptSkills();
                        //interrupt attacks
                        CancelCurrentAttack();
                        //stop the player
                        StopTheEntity();
                        break;
                    }
                case EFFECT_TYPE.STUN_2:
                    {
                        //this needs to
                        //interrupt skills
                        InterruptSkills();
                        //interrupt attacks
                        CancelCurrentAttack();
                        //stop the player
                        StopTheEntity();
                        break;
                    }
                case EFFECT_TYPE.SILENCED:
                    {
                        //this needs to
                        //interrupt skills
                        InterruptSkills();
                        break;
                    }
                default:
                    {
                        break;
                    }

            }

        }

        internal bool AddExistingStatusEffect(CharacterEffect existingEffect)
        {
            if (existingEffect.StatusEffect == null)
                return false;

            StatusEffectTemplate newTemplate = existingEffect.StatusEffect.Template;


            if (newTemplate != null)
            {
                double startTime = Program.MainUpdateLoopStartTime(); //NetTime.Now;
                bool effectBounced = false;
                //check the status effect isn't currently in effect

                CharacterEffect newStatusEffect = existingEffect;
                CharacterEffect characterEffectForRemoval = null;

                for (int i = 0; i < m_currentCharacterEffects.Count; i++)
                {
                    if (m_currentCharacterEffects[i].StatusEffect == null)
                        continue;

                    if (m_currentCharacterEffects[i].StatusEffect.Template.EffectClass == newTemplate.EffectClass)
                    {
                        if (m_currentCharacterEffects[i].StatusEffect.m_effectLevel.m_class_level > newStatusEffect.StatusEffect.m_effectLevel.m_class_level)
                        {
                            effectBounced = true;
                            break;
                        }
                        else
                        {
                            characterEffectForRemoval = m_currentCharacterEffects[i];
                            break;
                        }

                    }
                }


                if (!effectBounced)
                {

                    if (characterEffectForRemoval != null)
                    {
                        characterEffectForRemoval.StatusEffect.Complete();
                        m_currentCharacterEffects.Remove(characterEffectForRemoval);
                    }
                    m_currentCharacterEffects.Add(newStatusEffect);
                    //the status List has changed so send update at earliest opportunity
                    m_statusListChanged = true;


                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns a list of status effects, removing any that were caused by a combination effect
        /// </summary>
        List<CharacterEffect> GetStrippedStatusEffects()
        {

            List<CharacterEffect> strippedList = new List<CharacterEffect>(m_currentCharacterEffects);

            StatusEffect potStatusEffect = GetStatusEffectForID(EFFECT_ID.COMBI_POT_1);
            StatusEffect elixirStatusEffect = GetStatusEffectForID(EFFECT_ID.COMBI_ELIX_1);

            if (potStatusEffect != null)
            {
                for (int i = 0; i < strippedList.Count; i++)
                {
                    StatusEffect currentEffect = strippedList[i].StatusEffect;
                    if (currentEffect != null &&
                        ((currentEffect.Template.StatusEffectID == EFFECT_ID.DEF_BOOST_POT) ||
                        (currentEffect.Template.StatusEffectID == EFFECT_ID.ARM__BOOST_POT) ||
                        (currentEffect.Template.StatusEffectID == EFFECT_ID.MAX_HEALTH_BOOST_POT) ||
                        (currentEffect.Template.StatusEffectID == EFFECT_ID.MAX_ENERGY_BOOST_POT) ||
                        (currentEffect.Template.StatusEffectID == EFFECT_ID.ATT_BOOST_POT) ||
                        (currentEffect.Template.StatusEffectID == EFFECT_ID.ATT_SPD_BOOST_POT) ||
                        (currentEffect.Template.StatusEffectID == EFFECT_ID.RUN_SPD_BOOST_POT) ||
                        (currentEffect.Template.StatusEffectID == EFFECT_ID.EXP_BOOST_POT) ||
                        (currentEffect.Template.StatusEffectID == EFFECT_ID.ABILITY_BOOST_POT) ||
                        (currentEffect.Template.StatusEffectID == EFFECT_ID.ENERGY_REGEN_POT) ||
                        (currentEffect.Template.StatusEffectID == EFFECT_ID.HEALTH_REGEN_POT)
                        ) && currentEffect.m_statusEffectLevel <= potStatusEffect.m_statusEffectLevel)
                    {
                        strippedList.RemoveAt(i);
                        i--;
                        continue;
                    }
                }
            }
            if (elixirStatusEffect != null)
            {
                for (int i = 0; i < strippedList.Count; i++)
                {
                    StatusEffect currentEffect = strippedList[i].StatusEffect;
                    if (currentEffect != null &&
                        ((currentEffect.Template.StatusEffectID == EFFECT_ID.DEF_BOOST_ELIXIR) ||
                        (currentEffect.Template.StatusEffectID == EFFECT_ID.ARM_BOOST_ELIX) ||
                        (currentEffect.Template.StatusEffectID == EFFECT_ID.MAX_HEALTH_BOOST_ELIX) ||
                        (currentEffect.Template.StatusEffectID == EFFECT_ID.MAX_ENERGY_BOOST_ELIX) ||
                        (currentEffect.Template.StatusEffectID == EFFECT_ID.ATT_BOOST_ELIXIR) ||
                        (currentEffect.Template.StatusEffectID == EFFECT_ID.ATT_SPD_BOOST_ELIX) ||
                        (currentEffect.Template.StatusEffectID == EFFECT_ID.RUN_SPD_BOOST_ELIXIR) ||
                        (currentEffect.Template.StatusEffectID == EFFECT_ID.EXP_BOOST_ELIXIR) ||
                        (currentEffect.Template.StatusEffectID == EFFECT_ID.ABILITY_BOOST_ELIXIR) ||
                        (currentEffect.Template.StatusEffectID == EFFECT_ID.ENERGY_REGEN_ELIX) ||
                        (currentEffect.Template.StatusEffectID == EFFECT_ID.HEALTH_REGEN_ELIX)
                        ) && currentEffect.m_statusEffectLevel <= elixirStatusEffect.m_statusEffectLevel)
                    {
                        strippedList.RemoveAt(i);
                        i--;
                        continue;
                    }
                }
            }

            return strippedList;

        }

        void adjustSpeedEffectModifier(int value, CombatEntityStats statsToChange)
        {

            float newVal = (100 + value) / 100.0f;
            float currentSpeedVal = statsToChange.RunSpeed;
            if (value > 0)
            {//(100 + value) / 100.0f
                if (currentSpeedVal >= 1 && currentSpeedVal < newVal)
                {
                    statsToChange.RunSpeed = newVal;
                }
            }
            else if (currentSpeedVal > newVal)
            {
                statsToChange.RunSpeed = newVal;
                if (statsToChange.RunSpeed < 0)
                {
                    statsToChange.RunSpeed = 0;
                }
            }
            /*if (value > 0)
            {
                if (m_speedEffectModifier >= 100 && m_speedEffectModifier < value + 100)
                {
                    m_speedEffectModifier = value + 100;
                }
            }
            else if(m_speedEffectModifier>100+value)
            {
                m_speedEffectModifier = 100 + value;
                if (m_speedEffectModifier < 0)
                    m_speedEffectModifier = 0;
            }*/
        }

        void adjustAttackSpeedModifier(float value, CombatEntityStats statsToChange)
        {
            float currentValue = statsToChange.AttackSpeed;
            if (value > 0)
            {
                if (currentValue <= 1 && currentValue > (100 - value) / 100.0f)
                {
                    statsToChange.AttackSpeed = (100 - value) / 100.0f;
                    if (statsToChange.AttackSpeed < MIN_ATTACK_SPEED_MODIFIER)
                    {
                        statsToChange.AttackSpeed = MIN_ATTACK_SPEED_MODIFIER;

                    }

                }
            }
            else if (statsToChange.AttackSpeed < (100 - value) / 100.0f)
            {
                statsToChange.AttackSpeed = (100 - value) / 100.0f;
            }
        }

        float addToMulti(float currentVal, float newAmount, float baseAmount)
        {
            float result = currentVal;

            float currentChange = currentVal - baseAmount;
            float newChange = newAmount - baseAmount;
            float finalChange = newChange + currentChange;
            result = baseAmount + finalChange;

            return result;

        }

        void adjustExpModifier(double value, CombatEntityStats statsToChange)
        {

            if (value > 0)
            {
                if (statsToChange.ExpRate >= 1 && statsToChange.ExpRate < value)
                {
                    statsToChange.ExpRate = (float)value;
                }
            }//don't quite understand this bit
            else if (statsToChange.ExpRate < value)
            {
                statsToChange.ExpRate = (float)value;
                if (statsToChange.ExpRate < 1)
                {
                    statsToChange.ExpRate = 1;
                }
            }
            /* if (value > 0)
             {
                 if (m_expModifier >= 1 && m_expModifier < value)
                 {
                     m_expModifier = (float)value;
                 }
             }
             else if (m_expModifier < value)
             {
                 m_expModifier = (float)value;
                 if (m_expModifier < 1)
                     m_expModifier = 1;
             }*/
        }

        void adjustFishingExpModifier(double value, CombatEntityStats statsToChange)
        {

            if (value > 0)
            {
                if (statsToChange.FishingExpRate >= 1 && statsToChange.FishingExpRate < value)
                {
                    statsToChange.FishingExpRate = (float)value;
                }
            }//don't quite understand this bit
            else if (statsToChange.FishingExpRate < value)
            {
                statsToChange.FishingExpRate = (float)value;
                if (statsToChange.FishingExpRate < 1)
                {
                    statsToChange.FishingExpRate = 1;
                }
            }

        }

        void adjustAbilityRateModifier(double value, CombatEntityStats statsToChange)
        {

            //get the currentValues
            float changingVal = statsToChange.AbilityRate;

            if (value > 0)
            {
                if (changingVal >= 1 && changingVal < value)
                {
                    statsToChange.AbilityRate = (float)value;
                }
            }
            else if (changingVal < value)
            {
                statsToChange.AbilityRate = (float)value;
                if (statsToChange.AbilityRate < 1)
                {

                    statsToChange.AbilityRate = 1;
                }
            }
        }

        internal void RecalculateStatModifiers()
        {
            //ResetStatModifiers(); 
            // removed this call as it's called further up in the character effect update (which calls this)
            // and added it to places where it's not called by character effect


            for (int i = m_currentCharacterEffects.Count - 1; i > -1; i--)
            {
                StatusEffect currentEffect = m_currentCharacterEffects[i].StatusEffect;
                if (currentEffect == null)
                    continue;

                switch (currentEffect.Template.EffectType)
                {
                    case EFFECT_TYPE.CHANGE_ARMOUR:
                        {
                            m_statusStats.Armour += currentEffect.m_effectLevel.getModifiedAmount(currentEffect.CasterAbilityLevel, currentEffect.StatModifier);
                            break;
                        }
                    case EFFECT_TYPE.CHANGE_ATTACK:
                        {
                            m_statusStats.Attack += currentEffect.m_effectLevel.getModifiedAmount(currentEffect.CasterAbilityLevel, currentEffect.StatModifier);
                            break;
                        }
                    case EFFECT_TYPE.BUFF_RANGE_ATTACK_AND_DAMAGE:
                        {
                            if (CompiledStats.MaxAttackRange > 1)
                            {
                                m_statusStats.Attack += currentEffect.m_effectLevel.getModifiedAmount(currentEffect.CasterAbilityLevel, currentEffect.StatModifier);
                                m_statusStats.Damage += ((float)(currentEffect.m_effectLevel.getModifiedAmount(currentEffect.CasterAbilityLevel, currentEffect.StatModifier) / 5)) / 100.0f;
                            }
                            break;
                        }
                    case EFFECT_TYPE.CHANGE_RUN_SPEED:
                        {
                            adjustSpeedEffectModifier(currentEffect.m_effectLevel.getUnModifiedAmount(), m_statusStatsMultipliers);
                            break;
                        }
                    case EFFECT_TYPE.CHANGE_RUN_SPEED_LEVEL_BASED:
                        {
                            int amount = 90 + ((currentEffect.CasterLevel - Level) / 2);

                            if (amount < 50)
                            {
                                amount = 50;
                            }
                            else if (amount > 95)
                            {
                                amount = 95;
                            }
                            adjustSpeedEffectModifier(-amount, m_statusStatsMultipliers);

                            break;
                        }

                    case EFFECT_TYPE.ROOT:
                        {
                            m_statusPreventsActions.Move = true;
                            m_statusStatsMultipliers.RunSpeed = 0;
                            break;
                        }

                    case EFFECT_TYPE.CHANGE_MAX_HEALTH:
                        {
                            m_statusStats.MaxHealth += currentEffect.m_effectLevel.getModifiedAmount(currentEffect.CasterAbilityLevel, currentEffect.StatModifier);
                            break;
                        }
                    case EFFECT_TYPE.CHANGE_RESISTANCE:
                        {
                            m_statusStats.AddToBonusType((int)currentEffect.Template.DamageType, currentEffect.m_effectLevel.getModifiedAmount(currentEffect.CasterAbilityLevel, currentEffect.StatModifier));
                            break;
                        }
                    case EFFECT_TYPE.CHANGE_MAX_ENERGY:
                        {
                            m_statusStats.MaxEnergy += (float)currentEffect.m_effectLevel.getModifiedAmount(currentEffect.CasterAbilityLevel, currentEffect.StatModifier);
                            break;
                        }
                    case EFFECT_TYPE.INC_ATTACK_DEC_ARMOUR:
                        {   //#7503 Remove negative effects from Frenzy. 
                            m_statusStats.Attack += currentEffect.m_effectLevel.getModifiedAmount(currentEffect.CasterAbilityLevel, currentEffect.StatModifier);
                            m_statusStats.Damage += ((float)(currentEffect.m_effectLevel.getModifiedAmount(currentEffect.CasterAbilityLevel, currentEffect.StatModifier) / 10)) / 100.0f;
                            break;
                        }
                    case EFFECT_TYPE.CHANGE_DEFENCE:
                        {
                            m_statusStats.Defence += currentEffect.m_effectLevel.getModifiedAmount(currentEffect.CasterAbilityLevel, currentEffect.StatModifier);
                            break;
                        }
                    case EFFECT_TYPE.CHANGE_DAMAGE_TYPE:
                        {
                            m_statusStats.AddToOtherDamageType((int)currentEffect.Template.DamageType, currentEffect.m_effectLevel.getModifiedAmount(currentEffect.CasterAbilityLevel, currentEffect.StatModifier));
                            break;
                        }
                    case EFFECT_TYPE.CHANGE_ATTACK_SPEED:
                        {

                            adjustAttackSpeedModifier((int)currentEffect.m_effectLevel.getUnModifiedAmount(), m_statusStatsMultipliers);
                            InfoUpdated(Inventory.EQUIP_SLOT.SLOT_ATTACK_SPEED);
                            break;
                        }
                    case EFFECT_TYPE.STUN:
                        {
                            m_statusStatsMultipliers.Damage *= 0.5f;
                            break;
                        }
                    case EFFECT_TYPE.CHANGE_SIZE:
                        {
                            float diff = (float)currentEffect.m_effectLevel.getUnModifiedAmount() / 100.0f;

                            m_statusStatsMultipliers.Scale *= diff;

                            // CHAR-2207 - prevent scaling up beyond 200%
                            if (m_statusStatsMultipliers.Scale > 2.0f)
                            {
                                m_statusStatsMultipliers.Scale = 2.0f;
                            }
                            InfoUpdated(Inventory.EQUIP_SLOT.SLOT_SCALE);
                            break;
                        }
                    case EFFECT_TYPE.BUFF_XP_GAIN:
                        {
                            adjustExpModifier(currentEffect.m_effectLevel.getUnModifiedAmount(), m_statusStatsMultipliers);
                            break;
                        }
                    case EFFECT_TYPE.BUFF_FISHING_XP_GAIN:
                        {
                            adjustFishingExpModifier(currentEffect.m_effectLevel.getUnModifiedAmount(), m_statusStatsMultipliers);
                            break;
                        }
                    case EFFECT_TYPE.CHANGE_ABILITY_GAIN:
                        {
                            adjustAbilityRateModifier(currentEffect.m_effectLevel.getUnModifiedAmount(), m_statusStatsMultipliers);
                            break;
                        }
                    case EFFECT_TYPE.PLAYING_DEAD:
                        {
                            m_statusCancelConditions.Move = true;
                            m_statusCancelConditions.Skills = true;
                            m_statusCancelConditions.Attack = true;
                            m_statusPreventsActions.Detection = true;
                            m_statusPreventsActions.HostileAction = true;
                            break;
                        }
                    case EFFECT_TYPE.FROZEN:
                        {
                            m_statusPreventsActions.Attack = true;
                            m_statusPreventsActions.Skills = true;
                            m_statusPreventsActions.Move = true;
                            m_statusCancelConditions.TakeDamage = true;
                            m_statusPreventsActions.Regen = true;
                            m_statusStatsMultipliers.RunSpeed = 0;
                            break;
                        }
                    case EFFECT_TYPE.SILENCED:
                        {
                            m_statusPreventsActions.Skills = true;
                            break;
                        }
                    case EFFECT_TYPE.STASIS:
                        {
                            m_statusPreventsActions.Regen = true;
                            break;
                        }
                    case EFFECT_TYPE.STUN_2:
                        {
                            m_statusPreventsActions.Attack = true;
                            m_statusPreventsActions.Skills = true;
                            m_statusPreventsActions.Move = true;
                            m_statusPreventsActions.Regen = true;
                            m_statusStatsMultipliers.RunSpeed = 0;
                            break;
                        }
                    case EFFECT_TYPE.HIDE:
                        {
                            m_statusPreventsActions.Detection = true;
                            break;
                        }
                    case EFFECT_TYPE.CAMOUFLAGE:
                        {
                            m_statusPreventsActions.Detection = true;
                            break;
                        }
                    case EFFECT_TYPE.CAMOUFLAGE_2:
                        {
                            m_statusPreventsActions.Detection = true;
                            break;
                        }
                    case EFFECT_TYPE.INVISIBILITY:
                        {
                            m_statusPreventsActions.Detection = true;
                            break;
                        }
                    case EFFECT_TYPE.HALF_ATTACK_SPEED:
                        {
                            adjustAttackSpeedModifier(-50, m_statusStatsMultipliers);
                            break;
                        }
                    case EFFECT_TYPE.CHANGE_FORTITUDE:
                        {
                            m_statusStats.AddToAvoidanceType((int)AVOIDANCE_TYPE.PHYSICAL, currentEffect.m_effectLevel.getModifiedAmount(currentEffect.CasterAbilityLevel, currentEffect.StatModifier));
                            break;
                        }
                    case EFFECT_TYPE.HEALTH_AND_ENERGY_PERCENT:
                        {
                            m_statusStatsMultipliers.MaxHealth *= currentEffect.m_effectLevel.getFloatModifiedAmount(currentEffect.CasterAbilityLevel, currentEffect.StatModifier);
                            m_statusStatsMultipliers.MaxEnergy *= currentEffect.m_effectLevel.getFloatModifiedAmount(currentEffect.CasterAbilityLevel, currentEffect.StatModifier);
                            break;
                        }
                    case EFFECT_TYPE.MAX_HEALTH_PERCENT:
                        {
                            m_statusStats.MaxHealth += (float)currentEffect.m_effectLevel.m_amount;
                            m_statusStatsMultipliers.MaxHealth = addToMulti(m_statusStatsMultipliers.MaxHealth, (float)currentEffect.m_effectLevel.m_baseAmount, 1.0f);
                            break;
                        }
                    case EFFECT_TYPE.MAX_ENERGY_PERCENT:
                        {
                            m_statusStats.MaxEnergy += (float)currentEffect.m_effectLevel.m_amount;
                            m_statusStatsMultipliers.MaxEnergy = addToMulti(m_statusStatsMultipliers.MaxEnergy, (float)currentEffect.m_effectLevel.m_baseAmount, 1.0f);
                            break;
                        }
                    case EFFECT_TYPE.ARMOUR_PERCENT:
                        {
                            m_statusStats.Armour += (float)currentEffect.m_effectLevel.m_amount;
                            m_statusStatsMultipliers.Armour = addToMulti(m_statusStatsMultipliers.Armour, (float)currentEffect.m_effectLevel.m_baseAmount, 1.0f);
                            break;
                        }
                    case EFFECT_TYPE.DEFENCE_PERCENT:
                        {
                            m_statusStats.Defence += (float)currentEffect.m_effectLevel.m_amount;
                            m_statusStatsMultipliers.Defence = addToMulti(m_statusStatsMultipliers.Defence, (float)currentEffect.m_effectLevel.m_baseAmount, 1.0f);
                            break;
                        }
                    case EFFECT_TYPE.ATTACK_PERCENT:
                        {
                            m_statusStats.Attack += (float)currentEffect.m_effectLevel.m_amount;
                            m_statusStatsMultipliers.Attack = addToMulti(m_statusStatsMultipliers.Attack, (float)currentEffect.m_effectLevel.m_baseAmount, 1.0f);
                            break;
                        }
                    case EFFECT_TYPE.HEALTH_REGEN_PERCENT_OF_MAX:
                        {
                            m_statusStats.HealthRegenPerTick += (float)currentEffect.m_effectLevel.m_amount;
                            m_statusStatsMultipliers.HealthRegenPerTick = addToMulti(m_statusStatsMultipliers.HealthRegenPerTick, (float)currentEffect.m_effectLevel.m_baseAmount, 0);
                            if (currentEffect.Template.DormantOnAggressive == false)
                            {
                                m_statusStats.HealthRegenPerTickCombat += (float)currentEffect.m_effectLevel.m_amount;
                                m_statusStatsMultipliers.HealthRegenPerTickCombat = addToMulti(m_statusStatsMultipliers.HealthRegenPerTickCombat, (float)currentEffect.m_effectLevel.m_baseAmount, 0);
                            }
                            break;
                        }
                    case EFFECT_TYPE.ENERGY_REGEN_PERCENT_OF_MAX:
                        {
                            m_statusStats.EnergyRegenPerTick += (float)currentEffect.m_effectLevel.m_amount;
                            m_statusStatsMultipliers.EnergyRegenPerTick = addToMulti(m_statusStatsMultipliers.EnergyRegenPerTick, (float)currentEffect.m_effectLevel.m_baseAmount, 0);
                            if (currentEffect.Template.DormantOnAggressive == false)
                            {
                                m_statusStats.EnergyRegenPerTickCombat += (float)currentEffect.m_effectLevel.m_amount;
                                m_statusStatsMultipliers.EnergyRegenPerTickCombat = addToMulti(m_statusStatsMultipliers.EnergyRegenPerTickCombat, (float)currentEffect.m_effectLevel.m_baseAmount, 0);
                            }
                            break;
                        }
                    case EFFECT_TYPE.CHANGE_EVASIONS:
                        {
                            m_statusStats.AddToAvoidanceType(AVOIDANCE_TYPE.PHYSICAL, currentEffect.m_effectLevel.getModifiedAmount(currentEffect.CasterAbilityLevel, currentEffect.StatModifier));
                            m_statusStats.AddToAvoidanceType(AVOIDANCE_TYPE.MOVEMENT, currentEffect.m_effectLevel.getModifiedAmount(currentEffect.CasterAbilityLevel, currentEffect.StatModifier));
                            m_statusStats.AddToAvoidanceType(AVOIDANCE_TYPE.SPELL, currentEffect.m_effectLevel.getModifiedAmount(currentEffect.CasterAbilityLevel, currentEffect.StatModifier));
                            m_statusStats.AddToAvoidanceType(AVOIDANCE_TYPE.WEAKENING, currentEffect.m_effectLevel.getModifiedAmount(currentEffect.CasterAbilityLevel, currentEffect.StatModifier));
                            m_statusStats.AddToAvoidanceType(AVOIDANCE_TYPE.WOUNDING, currentEffect.m_effectLevel.getModifiedAmount(currentEffect.CasterAbilityLevel, currentEffect.StatModifier));
                            break;
                        }
                    case EFFECT_TYPE.FIRE_SKILL_BOOST:
                        {
                            SkillAugment newAugment = new SkillAugment(CombatModifiers.Modifier_Type.AddedSkillDamage, currentEffect.m_effectLevel.getModifiedAmount(currentEffect.CasterAbilityLevel, currentEffect.StatModifier));
                            m_statusStats.AddToSkillAugment(SKILL_TYPE.FIRE_BOLT, newAugment);
                            newAugment = new SkillAugment(CombatModifiers.Modifier_Type.AddedSkillDamage, currentEffect.m_effectLevel.getModifiedAmount(currentEffect.CasterAbilityLevel, currentEffect.StatModifier));
                            m_statusStats.AddToSkillAugment(SKILL_TYPE.FIRE_STORM, newAugment);
                            break;
                        }
                    case EFFECT_TYPE.ICE_SKILL_BOOST:
                        {

                            SkillAugment newAugment = new SkillAugment(CombatModifiers.Modifier_Type.AddedSkillDamage, currentEffect.m_effectLevel.getModifiedAmount(currentEffect.CasterAbilityLevel, currentEffect.StatModifier));
                            m_statusStats.AddToSkillAugment(SKILL_TYPE.ICE_SHARDS, newAugment);
                            newAugment = new SkillAugment(CombatModifiers.Modifier_Type.AddedSkillDamage, currentEffect.m_effectLevel.getModifiedAmount(currentEffect.CasterAbilityLevel, currentEffect.StatModifier));
                            m_statusStats.AddToSkillAugment(SKILL_TYPE.ICE_BLAST, newAugment);

                            break;
                        }
                    case EFFECT_TYPE.DAMAGE_MULTI_NO_PLAYERS:
                        {
                            float amount = currentEffect.m_effectLevel.getModifiedAmount(currentEffect.CasterAbilityLevel, currentEffect.StatModifier);
                            List<int> otherDamageTypes = new List<int>();
                            FloatForID.AddAlITypesToList(m_baseStats.OtherDamageTypes, otherDamageTypes);
                            FloatForID.AddAlITypesToList(m_equipStats.OtherDamageTypes, otherDamageTypes);
                            for (int currentBonus = 0; currentBonus < otherDamageTypes.Count; currentBonus++)
                            {
                                int currentType = otherDamageTypes[currentBonus];
                                float currentAmount = m_statusStatsMultipliers.GetOtherDamageType(currentType);
                                if (amount > 1 && currentAmount >= 1)
                                {
                                    if (amount > m_statusStatsMultipliers.Damage)
                                    {
                                        m_statusStatsMultipliers.SetOtherDamageType(currentType, amount);
                                    }
                                }
                                else
                                {
                                    if (amount < currentAmount)
                                    {
                                        m_statusStatsMultipliers.SetOtherDamageType(currentType, amount);
                                    }
                                }
                            }
                            otherDamageTypes = new List<int>();
                            FloatForID.AddAlITypesToList(m_baseStats.WeaponDamageTypes, otherDamageTypes);
                            FloatForID.AddAlITypesToList(m_equipStats.WeaponDamageTypes, otherDamageTypes);
                            for (int currentBonus = 0; currentBonus < otherDamageTypes.Count; currentBonus++)
                            {
                                int currentType = otherDamageTypes[i];
                                float currentAmount = m_statusStatsMultipliers.GetWeaponDamageType(currentType);
                                if (amount > 1 && currentAmount >= 1)
                                {
                                    if (amount > m_statusStatsMultipliers.Damage)
                                    {
                                        m_statusStatsMultipliers.SetWeaponDamageType(currentType, amount);
                                    }
                                }
                                else
                                {
                                    if (amount < currentAmount)
                                    {
                                        m_statusStatsMultipliers.SetWeaponDamageType(currentType, amount);
                                    }
                                }
                            }
                            break;
                        }
                }
                if (currentEffect.Template.BreakOnAggressive)
                {
                    m_statusCancelConditions.TakeDamage = true;
                    m_statusCancelConditions.Skills = true;
                    m_statusCancelConditions.Attack = true;
                }
            }
            ClampStatModifiers();
            CompileStats();
        }

        void ClampStatModifiers()
        {
            //don't take away all the health

        }

        //JT STATS CHANGES 12_2011
        virtual protected int getWeaponAbilityModifier()
        {
            return 0;
        }

        //this should be replaced with default or base stat
        virtual protected float getWeapoReportTime()
        {
            return 100000;
        }

        float StatConcentrationCompileBasic(float baseVal, float equip, float equipMulti, float status)
        {
            return (((baseVal * equipMulti) + equip)) + status;
            //return (baseVal * equipMulti * statusMulti) + equip + status;
        }

        float StatCompileBasic(float baseVal, float equip, float equipMulti, float status, float statusMulti)
        {
            return (((baseVal * equipMulti) + equip) * statusMulti) + status;
            //return (baseVal * equipMulti * statusMulti) + equip + status;
        }

        /// <summary>
        /// Compile used for regen stats
        /// The multi components are used with a ref stat rather than the base stat
        /// </summary>
        /// <param name="baseVal"></param>
        /// <param name="equip"></param>
        /// <param name="equipMulti"></param>
        /// <param name="status"></param>
        /// <param name="statusMulti"></param>
        /// <param name="refStat">the stat that the multi use </param>
        /// <returns></returns>
        float StatCompileRegen(float baseVal, float equip, float equipMulti, float status, float statusMulti, float refStat)
        {
            return baseVal + equip + status + (refStat * equipMulti) + (refStat * statusMulti);
            //return (baseVal * equipMulti * statusMulti) + equip + status;
        }

        /// <summary>
        /// Compiles the stats only using one multiplier
        /// numbers more than 1 considered buffs
        /// </summary>
        /// <returns></returns>
        float StatCompileOneMultiLargerIsBuff(float baseVal, float equip, float equipMulti, float status, float statusMulti)
        {
            //work out wich value to use
            float multiToUse = 1;
            float max = Math.Max(equipMulti, statusMulti);
            float min = Math.Min(equipMulti, statusMulti);
            if (min < 1)
            {
                multiToUse = min;
            }
            else
            {
                multiToUse = max;
            }

            return (baseVal * multiToUse) + status + equip;
        }

        /// <summary>
        /// Compiles the stats only using one multiplier
        /// numbers less than 1 considered buffs
        /// </summary>
        /// <returns></returns>
        float StatCompileOneMultiSmallerIsBuff(float baseVal, float equip, float equipMulti, float status, float statusMulti)
        {
            //work out wich value to use
            float multiToUse = 1;
            float max = Math.Max(equipMulti, statusMulti);
            float min = Math.Min(equipMulti, statusMulti);
            if (max > 1)
            {
                multiToUse = max;
            }
            else
            {
                multiToUse = min;
            }

            return (baseVal * multiToUse) + status + equip;
        }
        
        internal static double GetDominantSmallerIsBuff(double oldValue, double newVal)
        {
            //if it's a buff
            if (newVal < 1)
            {
                //if the old val is a buff or base val
                //and the new buff is better than the old buff
                if (oldValue <= 1 && oldValue > newVal)
                    return newVal;
            }
            //it's a debuff
            //the use the most harmful debuff
            else if (oldValue > newVal)
                return newVal;
            
            return oldValue;
        }

        internal virtual void AddAbiltiesToStats(CombatEntityStats statsToAddTo)
        {

        }

        internal void CompileStats()
        {

            m_compiledStats.ResetStats(0);

            ResetSkillModifiers();
            //1st do things that have no dependancies

            //deal with abilities
            AddAbiltiesToStats(m_compiledStats);
            List<CES_AbilityHolder> abilities = m_compiledStats.Abilities;
            for (int i = 0; i < abilities.Count(); i++)
            {
                CES_AbilityHolder currentAbility = abilities[i];
                ABILITY_TYPE currentType = currentAbility.m_ability_id;

                currentAbility.m_currentValue = StatCompileBasic(m_baseStats.GetAbilityValForId(currentType) + currentAbility.m_currentValue,
                    m_equipStats.GetAbilityValForId(currentType), m_equipStatsMultipliers.GetAbilityValForId(currentType),
                    m_statusStats.GetAbilityValForId(currentType), m_statusStatsMultipliers.GetAbilityValForId(currentType));

                calculateBaseAvoidances(currentAbility);
            }
            //compile base stats 1st
            m_compiledStats.Vitality = StatCompileBasic(m_baseStats.Vitality, m_equipStats.Vitality, m_equipStatsMultipliers.Vitality, m_statusStats.Vitality, m_statusStatsMultipliers.Vitality);//(m_baseStats.Vitality * m_equipStatsMultipliers.Vitality * m_statusStatsMultipliers.Vitality) + m_equipStatsMultipliers.Vitality + m_statusStatsMultipliers.Vitality;
            m_compiledStats.Dexterity = StatCompileBasic(m_baseStats.Dexterity, m_equipStats.Dexterity, m_equipStatsMultipliers.Dexterity, m_statusStats.Dexterity, m_statusStatsMultipliers.Dexterity);//(m_baseStats.Dexterity * m_equipStatsMultipliers.Dexterity * m_statusStatsMultipliers.Dexterity) + m_equipStatsMultipliers.Dexterity + m_statusStatsMultipliers.Dexterity;
            m_compiledStats.Strength = StatCompileBasic(m_baseStats.Strength, m_equipStats.Strength, m_equipStatsMultipliers.Strength, m_statusStats.Strength, m_statusStatsMultipliers.Strength);//(m_baseStats.Strength * m_equipStatsMultipliers.Strength * m_statusStatsMultipliers.Strength) + m_equipStatsMultipliers.Strength + m_statusStatsMultipliers.Strength;
            m_compiledStats.Focus = StatCompileBasic(m_baseStats.Focus, m_equipStats.Focus, m_equipStatsMultipliers.Focus, m_statusStats.Focus, m_statusStatsMultipliers.Focus);//(m_baseStats.Focus * m_equipStatsMultipliers.Vitality * m_statusStatsMultipliers.Vitality) + m_equipStatsMultipliers.Vitality + m_statusStatsMultipliers.Vitality;

            m_compiledStats.Encumberance = StatCompileBasic(m_baseStats.Encumberance, m_equipStats.Encumberance, m_equipStatsMultipliers.Encumberance, m_statusStats.Encumberance, m_statusStatsMultipliers.Encumberance);//(m_baseStats.Focus * m_equipStatsMultipliers.Vitality * m_statusStatsMultipliers.Vitality) + m_equipStatsMultipliers.Vitality + m_statusStatsMultipliers.Vitality;

            RaceTemplate race = RaceTemplateManager.getRaceTemplate(RACE_TYPE.HIGHLANDER);
            float strMod = race.m_strength_modifier;
            float dexMod = race.m_dexterity_modifier;
            float focMod = race.m_focus_modifier;
            float vitMod = race.m_vitality_modifier;


            //now the compiled base stats can be used to compile a base amount of the other stats
            float weaponAbilityModifier = getWeaponAbilityModifier();
            m_compiledStats.Attack = (int)m_compiledStats.Dexterity + (int)weaponAbilityModifier;
            m_compiledStats.Defence = (int)(m_compiledStats.Dexterity * 2 * dexMod + 0.5f);
            m_compiledStats.MaxEnergy = (int)(m_compiledStats.Focus * 5 * focMod + 0.5f);
            m_compiledStats.MaxHealth = (int)(m_compiledStats.Vitality * 5 * vitMod + 0.5f);
            m_compiledStats.MaxConcentrationFishing = CalculateMaxConcentration();

            //     m_compiledStats.Damage = (int)(m_compiledStats.Strength*strMod + weaponAbilityModifier * 0.25f);
            m_compiledStats.Damage = (float)(Math.Sqrt(m_compiledStats.Strength / 40) + Math.Sqrt(weaponAbilityModifier / 280));


            //temp create these
            int MAX_ENCUMBRANCE = 250;
            int MIN_ENERGY_PERCENT_AFTER_ENCUMBRANCE = 10;
            //encumbrance will only affect this base
            int weightModifier = MAX_ENCUMBRANCE - Encumbrance;
            int minWeightMod = MAX_ENCUMBRANCE / MIN_ENERGY_PERCENT_AFTER_ENCUMBRANCE;
            if (weightModifier < minWeightMod)
            {
                weightModifier = minWeightMod;
            }
            m_compiledStats.MaxEnergy = (m_compiledStats.MaxEnergy * weightModifier) / MAX_ENCUMBRANCE;


            //Do the Stat Derived types
            m_compiledStats.Attack = StatCompileBasic(m_baseStats.Attack + m_compiledStats.Attack, m_equipStats.Attack, m_equipStatsMultipliers.Attack, m_statusStats.Attack, m_statusStatsMultipliers.Attack);
            m_compiledStats.Defence = StatCompileBasic(m_baseStats.Defence + m_compiledStats.Defence, m_equipStats.Defence, m_equipStatsMultipliers.Defence, m_statusStats.Defence, m_statusStatsMultipliers.Defence);
            m_compiledStats.MaxEnergy = StatCompileBasic(m_baseStats.MaxEnergy + m_compiledStats.MaxEnergy, m_equipStats.MaxEnergy, m_equipStatsMultipliers.MaxEnergy, m_statusStats.MaxEnergy, m_statusStatsMultipliers.MaxEnergy);
            m_compiledStats.MaxHealth = StatCompileBasic(m_baseStats.MaxHealth + m_compiledStats.MaxHealth, m_equipStats.MaxHealth, m_equipStatsMultipliers.MaxHealth, m_statusStats.MaxHealth, m_statusStatsMultipliers.MaxHealth);
            m_compiledStats.MaxConcentrationFishing =
                StatConcentrationCompileBasic(m_baseStats.MaxConcentrationFishing + m_compiledStats.MaxConcentrationFishing,
                    m_equipStats.MaxConcentrationFishing, m_equipStatsMultipliers.MaxConcentrationFishing, m_statusStats.MaxConcentrationFishing
                    );
            m_compiledStats.Damage = StatCompileBasic(m_baseStats.Damage + m_compiledStats.Damage, m_equipStats.Damage, m_equipStatsMultipliers.Damage, m_statusStats.Damage, m_statusStatsMultipliers.Damage);

            //do the non stat derived stats
            //bonus types
            List<int> bonusTypes = new List<int>();
            FloatForID.AddAlITypesToList(m_baseStats.BonusTypes, bonusTypes);
            FloatForID.AddAlITypesToList(m_equipStats.BonusTypes, bonusTypes);
            FloatForID.AddAlITypesToList(m_equipStatsMultipliers.BonusTypes, bonusTypes);
            FloatForID.AddAlITypesToList(m_statusStats.BonusTypes, bonusTypes);
            FloatForID.AddAlITypesToList(m_statusStatsMultipliers.BonusTypes, bonusTypes);
            for (int i = 0; i < bonusTypes.Count; i++)
            {
                int currentType = bonusTypes[i];
                float currentBonusType = StatCompileBasic(m_baseStats.GetBonusType(currentType), m_equipStats.GetBonusType(currentType), m_equipStatsMultipliers.GetBonusType(currentType), m_statusStats.GetBonusType(currentType), m_statusStatsMultipliers.GetBonusType(currentType));
                m_compiledStats.AddToBonusType(currentType, currentBonusType);
            }
            List<int> damageTypes = new List<int>();
            FloatForID.AddAlITypesToList(m_baseStats.WeaponDamageTypes, damageTypes);
            FloatForID.AddAlITypesToList(m_equipStats.WeaponDamageTypes, damageTypes);
            FloatForID.AddAlITypesToList(m_equipStatsMultipliers.WeaponDamageTypes, damageTypes);
            FloatForID.AddAlITypesToList(m_statusStats.WeaponDamageTypes, damageTypes);
            FloatForID.AddAlITypesToList(m_statusStatsMultipliers.WeaponDamageTypes, damageTypes);
            for (int i = 0; i < damageTypes.Count; i++)
            {
                int currentType = damageTypes[i];
                float currentWeaponDamageType = StatCompileBasic(m_baseStats.GetWeaponDamageType(currentType), m_equipStats.GetWeaponDamageType(currentType), m_equipStatsMultipliers.GetWeaponDamageType(currentType), m_statusStats.GetWeaponDamageType(currentType), m_statusStatsMultipliers.GetWeaponDamageType(currentType));
                m_compiledStats.AddToWeaponDamageType(currentType, currentWeaponDamageType);
            }
            List<int> otherDamageTypes = new List<int>();
            FloatForID.AddAlITypesToList(m_baseStats.OtherDamageTypes, otherDamageTypes);
            FloatForID.AddAlITypesToList(m_equipStats.OtherDamageTypes, otherDamageTypes);
            FloatForID.AddAlITypesToList(m_equipStatsMultipliers.OtherDamageTypes, otherDamageTypes);
            FloatForID.AddAlITypesToList(m_statusStats.OtherDamageTypes, otherDamageTypes);
            FloatForID.AddAlITypesToList(m_statusStatsMultipliers.OtherDamageTypes, otherDamageTypes);
            for (int i = 0; i < otherDamageTypes.Count; i++)
            {
                int currentType = otherDamageTypes[i];
                float currentOtherDamageType = StatCompileBasic(m_baseStats.GetOtherDamageType(currentType), m_equipStats.GetOtherDamageType(currentType), m_equipStatsMultipliers.GetOtherDamageType(currentType), m_statusStats.GetOtherDamageType(currentType), m_statusStatsMultipliers.GetOtherDamageType(currentType));
                m_compiledStats.AddToOtherDamageType(currentType, currentOtherDamageType);
            }
            /*for (int i = 0; i < NUM_DAMAGE_TYPES; i++)
            {
                float currentWeaponDamageType = StatCompileBasic(m_baseStats.GetWeaponDamageType(i), m_equipStats.GetWeaponDamageType(i), m_equipStatsMultipliers.GetWeaponDamageType(i), m_statusStats.GetWeaponDamageType(i), m_statusStatsMultipliers.GetWeaponDamageType(i));
                m_compiledStats.AddToWeaponDamageType(i, currentWeaponDamageType);
                float currentOtherDamageType = StatCompileBasic(m_baseStats.GetOtherDamageType(i), m_equipStats.GetOtherDamageType(i), m_equipStatsMultipliers.GetOtherDamageType(i), m_statusStats.GetOtherDamageType(i), m_statusStatsMultipliers.GetOtherDamageType(i));
                m_compiledStats.AddToOtherDamageType(i, currentOtherDamageType);
            }*/

            List<int> avoidanceTypes = new List<int>();
            FloatForID.AddAlITypesToList(m_baseStats.AvoidanceTypes, avoidanceTypes);
            FloatForID.AddAlITypesToList(m_equipStats.AvoidanceTypes, avoidanceTypes);
            FloatForID.AddAlITypesToList(m_equipStatsMultipliers.AvoidanceTypes, avoidanceTypes);
            FloatForID.AddAlITypesToList(m_statusStats.AvoidanceTypes, avoidanceTypes);
            FloatForID.AddAlITypesToList(m_statusStatsMultipliers.AvoidanceTypes, avoidanceTypes);
            for (int i = 0; i < avoidanceTypes.Count; i++)
            {
                AVOIDANCE_TYPE at = (AVOIDANCE_TYPE)avoidanceTypes[i];
                float currentAvoidanceType = StatCompileBasic(m_baseStats.GetAvoidanceType(at), m_equipStats.GetAvoidanceType(at), m_equipStatsMultipliers.GetAvoidanceType(at), m_statusStats.GetAvoidanceType(at), m_statusStatsMultipliers.GetAvoidanceType(at));
                m_compiledStats.AddToAvoidanceType(at, currentAvoidanceType);
            }
            //bonus types
            List<int> immuneTypes = new List<int>();
            FloatForID.AddAlITypesToList(m_baseStats.ImmunityTypes, immuneTypes);
            FloatForID.AddAlITypesToList(m_equipStats.ImmunityTypes, immuneTypes);
            FloatForID.AddAlITypesToList(m_equipStatsMultipliers.ImmunityTypes, immuneTypes);
            FloatForID.AddAlITypesToList(m_statusStats.ImmunityTypes, immuneTypes);
            FloatForID.AddAlITypesToList(m_statusStatsMultipliers.ImmunityTypes, immuneTypes);
            for (int i = 0; i < immuneTypes.Count; i++)
            {
                int currentType = immuneTypes[i];
                float currentBonusType = StatCompileBasic(m_baseStats.GetImmunityType(currentType), m_equipStats.GetImmunityType(currentType), m_equipStatsMultipliers.GetImmunityType(currentType), m_statusStats.GetImmunityType(currentType), m_statusStatsMultipliers.GetImmunityType(currentType));
                m_compiledStats.SetImmunityType(currentType, currentBonusType);
            }
            List<int> damageResistTypes = new List<int>();
            FloatForID.AddAlITypesToList(m_baseStats.DamageReductionTypes, damageResistTypes);
            FloatForID.AddAlITypesToList(m_equipStats.DamageReductionTypes, damageResistTypes);
            FloatForID.AddAlITypesToList(m_equipStatsMultipliers.DamageReductionTypes, damageResistTypes);
            FloatForID.AddAlITypesToList(m_statusStats.DamageReductionTypes, damageResistTypes);
            FloatForID.AddAlITypesToList(m_statusStatsMultipliers.DamageReductionTypes, damageResistTypes);
            for (int i = 0; i < damageResistTypes.Count; i++)
            {
                int currentType = damageResistTypes[i];
                float currentBonusType = StatCompileBasic(m_baseStats.GetDamageReductionType(currentType), m_equipStats.GetDamageReductionType(currentType), m_equipStatsMultipliers.GetDamageReductionType(currentType), m_statusStats.GetDamageReductionType(currentType), m_statusStatsMultipliers.GetDamageReductionType(currentType));
                m_compiledStats.AddToDamageReductionType(currentType, currentBonusType);
            }
            /*for (int i = 0; i < NUM_AVOIDANCE_TYPES; i++)
            {
                AVOIDANCE_TYPE at=(AVOIDANCE_TYPE)i;
                float currentAvoidanceType = StatCompileBasic(m_baseStats.GetAvoidanceType(at), m_equipStats.GetAvoidanceType(at), m_equipStatsMultipliers.GetAvoidanceType(at), m_statusStats.GetAvoidanceType(at), m_statusStatsMultipliers.GetAvoidanceType(at));
                m_compiledStats.AddToAvoidanceType(at, currentAvoidanceType);
            }*/

            //regen must be done after mas health and energy as they ise these values as reference
            m_compiledStats.HealthRegenPerTick = StatCompileRegen(m_baseStats.HealthRegenPerTick, m_equipStats.HealthRegenPerTick, m_equipStatsMultipliers.HealthRegenPerTick, m_statusStats.HealthRegenPerTick, m_statusStatsMultipliers.HealthRegenPerTick, m_compiledStats.MaxHealth);
            m_compiledStats.EnergyRegenPerTick = StatCompileRegen(m_baseStats.EnergyRegenPerTick, m_equipStats.EnergyRegenPerTick, m_equipStatsMultipliers.EnergyRegenPerTick, m_statusStats.EnergyRegenPerTick, m_statusStatsMultipliers.EnergyRegenPerTick, m_compiledStats.MaxEnergy);


            m_compiledStats.HealthRegenPerTickCombat = StatCompileRegen(m_baseStats.HealthRegenPerTickCombat, m_equipStats.HealthRegenPerTickCombat, m_equipStatsMultipliers.HealthRegenPerTickCombat, m_statusStats.HealthRegenPerTickCombat, m_statusStatsMultipliers.HealthRegenPerTickCombat, m_compiledStats.MaxHealth);
            m_compiledStats.EnergyRegenPerTickCombat = StatCompileRegen(m_baseStats.EnergyRegenPerTickCombat, m_equipStats.EnergyRegenPerTickCombat, m_equipStatsMultipliers.EnergyRegenPerTickCombat, m_statusStats.EnergyRegenPerTickCombat, m_statusStatsMultipliers.EnergyRegenPerTickCombat, m_compiledStats.MaxEnergy);

            m_compiledStats.FishingConcentrationRegenPerTick = StatCompileRegen(m_baseStats.FishingConcentrationRegenPerTick,
                m_equipStats.FishingConcentrationRegenPerTick, m_equipStatsMultipliers.FishingConcentrationRegenPerTick,
                m_statusStats.FishingConcentrationRegenPerTick, m_statusStatsMultipliers.FishingConcentrationRegenPerTick,
                m_compiledStats.MaxConcentrationFishing);

            m_compiledStats.Armour = StatCompileBasic(m_baseStats.Armour, m_equipStats.Armour, m_equipStatsMultipliers.Armour, m_statusStats.Armour, m_statusStatsMultipliers.Armour);


            m_compiledStats.ExpRate = StatCompileOneMultiLargerIsBuff(m_baseStats.ExpRate, m_equipStats.ExpRate, m_equipStatsMultipliers.ExpRate, m_statusStats.ExpRate, m_statusStatsMultipliers.ExpRate);
            m_compiledStats.FishingExpRate = StatCompileOneMultiLargerIsBuff(m_baseStats.FishingExpRate, m_equipStats.FishingExpRate, m_equipStatsMultipliers.FishingExpRate, m_statusStats.FishingExpRate, m_statusStatsMultipliers.FishingExpRate);
            m_compiledStats.AbilityRate = StatCompileOneMultiLargerIsBuff(m_baseStats.AbilityRate, m_equipStats.AbilityRate, m_equipStatsMultipliers.AbilityRate, m_statusStats.AbilityRate, m_statusStatsMultipliers.AbilityRate);
            m_compiledStats.RunSpeed = StatCompileOneMultiLargerIsBuff(m_baseStats.RunSpeed, m_equipStats.RunSpeed, m_equipStatsMultipliers.RunSpeed, m_statusStats.RunSpeed, m_statusStatsMultipliers.RunSpeed);
            m_compiledStats.AttackSpeed = StatCompileOneMultiSmallerIsBuff(m_baseStats.AttackSpeed, m_equipStats.AttackSpeed, m_equipStatsMultipliers.AttackSpeed, m_statusStats.AttackSpeed, m_statusStatsMultipliers.AttackSpeed);
            m_compiledStats.Scale = StatCompileBasic(m_baseStats.Scale, m_equipStats.Scale, m_equipStatsMultipliers.Scale, m_statusStats.Scale, m_statusStatsMultipliers.Scale);


            for (int i = 0; i < 3; i++)
            {
                m_compiledStats.AddToBonusType(i, m_compiledStats.Armour);
            }

            //take strength into account 
            for (int i = 0; i < 3; i++)
            {
                float currentVal = m_compiledStats.GetWeaponDamageType(i);
                if (currentVal > 0)
                {
                    m_compiledStats.SetWeaponDamageType(i, currentVal * (1.0f + m_compiledStats.Damage));
                }
            }
            // maxdamage += (int)Math.Ceiling((attackingEntity.GetDamageType(i) * attackingEntity.ModifiedDamage) / 100.0f);


            //all of the skills that have modifiers need an entry, 
            //these will need to be reduced down to only have one entry for the same skill
            List<CES_SkillHolder> skills = m_compiledStats.Skills;

            CES_SkillHolder.AbsorbSkillsToList(skills, m_baseStats.Skills);
            CES_SkillHolder.AbsorbSkillsToList(skills, m_equipStats.Skills);
            CES_SkillHolder.AbsorbSkillsToList(skills, m_equipStatsMultipliers.Skills);
            CES_SkillHolder.AbsorbSkillsToList(skills, m_statusStats.Skills);
            CES_SkillHolder.AbsorbSkillsToList(skills, m_statusStatsMultipliers.Skills);


            for (int i = 0; i < skills.Count(); i++)
            {

                CES_SkillHolder currentSkill = skills[i];
                SKILL_TYPE currentType = currentSkill.m_skillID;

                EntitySkill originalSkill = GetEnitySkillForID(currentType, false);

                //if the original skill exists then set it's modified level
                if (originalSkill != null)
                {
                    originalSkill.SkillAugments.Clear();
                    int currentLevel = originalSkill.SkillLevel;
                    currentSkill.m_currentValue = StatCompileBasic(m_baseStats.GetSkillValForId(currentType) + currentLevel,
                        m_equipStats.GetSkillValForId(currentType), m_equipStatsMultipliers.GetSkillValForId(currentType),
                        m_statusStats.GetSkillValForId(currentType), m_statusStatsMultipliers.GetSkillValForId(currentType));
                    //this accessor will also clamp the value
                    originalSkill.SetModifiedLevel((int)currentSkill.m_currentValue, Level);
                    //get the clamped value back
                    currentSkill.m_currentValue = originalSkill.ModifiedLevel;

                    for (int augmentIndex = 0; augmentIndex < currentSkill.SkillAugments.Count; augmentIndex++)
                    {
                        SkillAugment currentAugment = currentSkill.SkillAugments[augmentIndex];
                        CombatModifiers.Modifier_Type augmentType = currentAugment.ModType;
                        float pveBaseVal = (float)originalSkill.GetBaseValForAugment(augmentType, false);
                        float pvpBaseVal = (float)originalSkill.GetBaseValForAugment(augmentType, true);
                        switch (augmentType)
                        {
                            case CombatModifiers.Modifier_Type.AddedSkillDamage:
                                {

                                    float pvpVal = StatCompileBasic(pvpBaseVal,
                                        m_equipStats.GetSkillAugmentValForId(currentType, augmentType, true), m_equipStatsMultipliers.GetSkillAugmentValForId(currentType, augmentType, true),
                                        m_statusStats.GetSkillAugmentValForId(currentType, augmentType, true), m_statusStatsMultipliers.GetSkillAugmentValForId(currentType, augmentType, true));
                                    float pveVal = StatCompileBasic(pveBaseVal,
                                       m_equipStats.GetSkillAugmentValForId(currentType, augmentType, false), m_equipStatsMultipliers.GetSkillAugmentValForId(currentType, augmentType, false),
                                       m_statusStats.GetSkillAugmentValForId(currentType, augmentType, false), m_statusStatsMultipliers.GetSkillAugmentValForId(currentType, augmentType, false));
                                    currentAugment.PVPModParam = pvpVal;
                                    currentAugment.PVEModParam = pveVal;
                                    break;
                                }
                            case CombatModifiers.Modifier_Type.ChangesCastingTime:
                            case CombatModifiers.Modifier_Type.ChangesRecastTime:
                                {
                                    float pvpVal = StatCompileOneMultiSmallerIsBuff(pvpBaseVal,
                                        m_equipStats.GetSkillAugmentValForId(currentType, augmentType, true), m_equipStatsMultipliers.GetSkillAugmentValForId(currentType, augmentType, true),
                                        m_statusStats.GetSkillAugmentValForId(currentType, augmentType, true), m_statusStatsMultipliers.GetSkillAugmentValForId(currentType, augmentType, true));
                                    float pveVal = StatCompileOneMultiSmallerIsBuff(pveBaseVal,
                                        m_equipStats.GetSkillAugmentValForId(currentType, augmentType, false), m_equipStatsMultipliers.GetSkillAugmentValForId(currentType, augmentType, false),
                                        m_statusStats.GetSkillAugmentValForId(currentType, augmentType, false), m_statusStatsMultipliers.GetSkillAugmentValForId(currentType, augmentType, false));

                                    currentAugment.PVPModParam = pvpVal;
                                    currentAugment.PVEModParam = pveVal;
                                    break;
                                }
                            default:
                                {
                                    break;
                                }
                        }
                        SkillAugment newAugment = new SkillAugment(currentAugment.ModType, currentAugment.PVEModParam, currentAugment.PVPModParam);
                        originalSkill.SkillAugments.Add(newAugment);


                    }

                }
                //if it does not exist then remove it from the compiled list so it will not get sent down in character stats
                else
                {
                    skills.RemoveAt(i);
                    i--;

                }

            }

            List<int> allDamageTypes = new List<int>();
            FloatForID.AddAlITypesToList(m_compiledStats.WeaponDamageTypes, allDamageTypes);
            FloatForID.AddAlITypesToList(m_compiledStats.OtherDamageTypes, allDamageTypes);
            for (int i = 0; i < allDamageTypes.Count; i++)
            {
                int currentType = allDamageTypes[i];
                float combinedDamage = m_compiledStats.GetWeaponDamageType(currentType) + m_compiledStats.GetOtherDamageType(currentType);
                m_compiledStats.SetCombinedDamageType(currentType, combinedDamage);
            }


            m_compiledStats.ClampStats(this);

            /* Program.Display("Compiled Stats For " + Name);
             Program.Display("base stats = " + m_baseStats.GetDebugString());
             Program.Display("equip stats = " + m_equipStats.GetDebugString());
             Program.Display("equip stats multi = " + m_equipStatsMultipliers.GetDebugString());
             Program.Display("status stats = " + m_statusStats.GetDebugString());
             Program.Display("status stats multi= " + m_baseStats.GetDebugString());
             Program.Display("compiled stats = " + m_compiledStats.GetDebugString());*/


            /*if (this.m_entityType == EntityType.Player)
            {
                Program.Display("Base Attack Speed: " + m_baseStats.AttackSpeed);
            }*/
        }

        /// <summary>
        /// Use our fishing level to calculate our desired max concentration
        /// </summary>
        /// <returns></returns>
        internal float CalculateMaxConcentration()
        {
            //note - this is used above with CompileStats()
            //and also in the character.LevelUp() method
            return (float)(6.25 * (LevelFishing * 3 + 7));
        }

        private void calculateBaseAvoidances(CES_AbilityHolder currentAbility)
        {

            switch (currentAbility.m_ability_id)
            {
                case ABILITY_TYPE.FORTITUDE:
                    {
                        m_compiledStats.SetAvoidanceType(AVOIDANCE_TYPE.PHYSICAL, currentAbility.m_currentValue / 2.0f);
                        break;
                    }
                case ABILITY_TYPE.WARDING:
                    {
                        m_compiledStats.SetAvoidanceType(AVOIDANCE_TYPE.SPELL, currentAbility.m_currentValue / 2.0f);
                        break;
                    }
                case ABILITY_TYPE.EVASION:
                    {
                        m_compiledStats.SetAvoidanceType(AVOIDANCE_TYPE.MOVEMENT, currentAbility.m_currentValue / 2.0f);
                        break;
                    }
                case ABILITY_TYPE.VIGOUR:
                    {
                        m_compiledStats.SetAvoidanceType(AVOIDANCE_TYPE.WOUNDING, currentAbility.m_currentValue / 2.0f);
                        break;
                    }
                case ABILITY_TYPE.WILLPOWER:
                    {
                        m_compiledStats.SetAvoidanceType(AVOIDANCE_TYPE.WEAKENING, currentAbility.m_currentValue / 2.0f);
                        break;
                    }

            }
        }


        /// <summary>
        /// Notify this entity that the status effects on someone it is interested in have changed
        /// </summary>
        /// <param name="theEntity">the entity that has had it's status effects changed</param>
        virtual internal void EntityHasChangedStatusList(CombatEntity theEntity)
        {
            //the base has nothing to do here
        }

        /// <summary>
        /// Notify this entity that it should forget the status effects of theEntity as it is no longer of interest
        /// </summary>
        /// <param name="theEntity">The entity to be forgotton</param>        
        virtual internal void ForgetStatusEffectsOnEntity(CombatEntity theEntity)
        {
            //the base has nothing to do here
        }

        virtual internal void NotifyAllInterestedOfStatusEffectChange()
        {

            for (int i = 0; i < m_entitiesOfInterest.Count; i++)
            {
                CombatEntity currentEntity = m_entitiesOfInterest[i];

                currentEntity.EntityHasChangedStatusList(this);
            }
        }

        virtual internal void DealWithChangeInStatusEffects(double currentTime)
        {
            if (StatusListChanged == true)
            {
                NotifyAllInterestedOfStatusEffectChange();
                StatusListChanged = false;
            }
        }


        #endregion

        #region skills
        
        virtual public EntitySkill GetEnitySkillForID(SKILL_TYPE skillID, bool onlyCharacterOwned)
        {
            for (int currentSkillIndex = 0; currentSkillIndex < m_EntitySkills.Count; currentSkillIndex++)
            {
                EntitySkill currentSkill = m_EntitySkills[currentSkillIndex];


                if (currentSkill.SkillID == skillID)
                {
                    return currentSkill;
                }

            }

            return null;
        }

        /// <summary>
        /// If a skill if being cast this will test if that skill is interrupted
        /// returns true if a skill was interrupted
        /// </summary>
        /// <returns></returns>
        internal bool AttemptToProtectSkillCharging()
        {
            bool skillInterrupted = false;

            if (CurrentSkillTarget != null)
            {
                bool PVP = (CurrentSkillTarget.Type == EntityType.Player && CurrentSkillTarget.IsPVP());
                SkillTemplateLevel currentSkilllevel = CurrentSkill.getSkillTemplateLevel(PVP);

                if (currentSkilllevel != null)
                {
                    int chargingProtection = currentSkilllevel.ChargingProtection;
                    int randomChance = Program.getRandomNumber(100);
                    if (randomChance > chargingProtection)
                    {

                        //notify the entity that ir's skill is being cancelled
                        SkillCancelledNotification();
                        //skill Interrupted
                        Program.Display("Skill " + CurrentSkill.Template.SkillName + " interupted");
                        if (CombatManager.REPORT_MOB_SKILLS == true && Type == CombatEntity.EntityType.Mob && CurrentSkill.Template.ReportProgress == true)
                        {
							int textID = (int)CombatEntityTextDB.TextID.OTHER_INTERRUPTED_SKILL;
							TheCombatManager.zone.SendLocalSystemSkillMessageLocalised(new LocaliseParams(textDB, textID, Name, CurrentSkill.Template.SkillID), CurrentPosition.m_position, Zone.LOCAL_MESSAGE_RANGE, false, SYSTEM_MESSAGE_TYPE.BATTLE);
                        }
                        LastSkill = CurrentSkill;
                        CurrentSkillTarget = null;
                        CurrentSkill = null;
                        ActionInProgress = false;
                        skillInterrupted = true;
                        CurrentSkillInterrupted = true;

                    }

                }
            }


            return skillInterrupted;
        }

        internal void InterruptSkills()
        {
            if (CurrentSkillTarget != null && CurrentSkill != null)
            {
                bool PVP = (CurrentSkillTarget.Type == EntityType.Player && CurrentSkillTarget.IsPVP());
                SkillTemplateLevel currentSkilllevel = CurrentSkill.getSkillTemplateLevel(PVP);

                if (currentSkilllevel != null)
                {
                    //notify the entity that ir's skill is being cancelled
                    SkillCancelledNotification();
                    //skill Interrupted
                    Program.Display("Skill " + CurrentSkill.Template.SkillName + " interupted");
                    if (CombatManager.REPORT_MOB_SKILLS == true && Type == CombatEntity.EntityType.Mob && CurrentSkill.Template.ReportProgress == true)
                    {
                        if (TheCombatManager != null)
                        {
							int textID = (int)CombatEntityTextDB.TextID.OTHER_INTERRUPTED_SKILL;
							TheCombatManager.zone.SendLocalSystemSkillMessageLocalised(new LocaliseParams(textDB, textID, Name, CurrentSkill.Template.SkillID), CurrentPosition.m_position, Zone.LOCAL_MESSAGE_RANGE, false, SYSTEM_MESSAGE_TYPE.BATTLE);
                        }
                    }
                    LastSkill = CurrentSkill;
                    CurrentSkillTarget = null;
                    CurrentSkill = null;
                    ActionInProgress = false;
                    CurrentSkillInterrupted = true;

                    if (TheCombatManager != null)
                    {
                        TheCombatManager.EntitiesWithCancelledSkills.Add(this);
                    }
                }
            }
        }

        internal void CancelCurrentSkill()
        {
            if (CurrentSkillTarget != null)
            {
                Program.Display("Skill " + CurrentSkill + " Canceled");
                LastSkill = CurrentSkill;
                CurrentSkillTarget = null;
                CurrentSkill = null;
                ActionInProgress = false;
                CurrentSkillInterrupted = true;
                if (TheCombatManager != null)
                {
                    TheCombatManager.EntitiesWithCancelledSkills.Add(this);
                }

            }
        }

        internal virtual void CancelNextSkill()
        {
            if (NextSkillTarget != null)
            {
                Program.Display("Skill " + NextSkill + " Canceled");
                LastSkill = NextSkill;
                NextSkillTarget = null;
                NextSkill = null;

                if (TheCombatManager != null)
                {
                    TheCombatManager.EntitiesWithCancelledSkills.Add(this);
                }
            }
        }

        //called when an attack is started
        internal virtual void StartAttack(CombatDamageMessageData attackDamage)
        {
            //keep hold of the attack data
            m_currentAttackDamage = attackDamage;
            ////Program.Display(Name + " starting attack");
        }

        internal virtual void StartProc(SkillDamageData newProc)
        {
            m_currentProcData = newProc;
        }

        /// <summary>
        /// If success the base can consume the item if required 
        /// </summary>
        /// <param name="theProc"></param>
        /// <returns></returns>
        internal virtual bool CarryOutProc(SkillDamageData theProc)
        {
            bool success = false;
            if (TheCombatManager != null && theProc != null && theProc.TargetDamage != null && theProc.TargetDamage.TargetLink != null)
            {
                EntitySkill procSkill = theProc.TheSkill;
                procSkill.SkillLevel = procSkill.SkillLevel;
                procSkill.IsProc = true;
                CombatEntity target = theProc.TargetDamage.TargetLink;
                success = TheCombatManager.CastSkill(this, target, procSkill, theProc);
                //if it was not a success then cancel all the damage
                if (success == false)
                {
                    theProc.CancelSkillDamage(TheCombatManager);
                }
            }
            return success;
        }

        //called when a skill casting fails to complete
        internal virtual void EndCasting()
        {
            m_currentAttackDamage = null;
        }

        public virtual void CarryOutSkill()
        {
            m_currentAttackDamage = null;
        }

        public virtual void SkillCancelledNotification()
        {

        }

        /// <summary>
        /// called if a next skill is removed (or not set) due to an entity failing various conditions
        /// mainly used for combat ai
        /// </summary>
        public virtual void SkillFailedConditions()
        {

        }

        internal virtual void ResetSkillModifiers()
        {
            for (int i = 0; i < m_EntitySkills.Count; i++)
            {
                EntitySkill currentSkill = m_EntitySkills[i];
                currentSkill.SkillAugments.Clear();
                currentSkill.ModifiedLevel = currentSkill.SkillLevel;
            }
        }

        virtual internal double TimeSinceSkillLastCast(SKILL_TYPE skillID)
        {
            for (int i = 0; i < m_EntitySkills.Count; i++)
            {

                if (m_EntitySkills[i].SkillID == skillID)
                {
                    return m_EntitySkills[i].TimeLastCast;
                }
            }

            return 0;
        }

        #endregion

        #region casting, damage

        /// <summary>
        /// Called when damage done by this entity has been applied to the targed
        /// </summary>
        /// <param name="endedAttack">The damage that has been applied to the target</param>
        internal virtual void DamageApplied(CombatDamageMessageData endedAttack)
        {
            endedAttack.DamageApplied = true;
        }

        internal void AttemptToReduceDamage(int currentType, CalculatedDamage calcDamage)
        {
            if (IsImmuneToType(currentType) == true)
            {
                calcDamage.m_calculatedDamage = 0;
                calcDamage.m_preLvlReductionDamage = 0;
            }
            else
            {
                float damageRemaining = GetRemainingDamage(currentType);
                calcDamage.m_calculatedDamage = (int)Math.Ceiling(calcDamage.m_calculatedDamage * damageRemaining);
                calcDamage.m_preLvlReductionDamage = (int)Math.Ceiling(calcDamage.m_preLvlReductionDamage * damageRemaining);
            }
        }

        //called when an attack is considered complete by the combat manager
        internal virtual void EndAttack()
        {
            if (Program.m_LogSysBattle)
            {
                Program.Display("EndAttack called for " + GetIDString());
            }
            //forget about the attack data, this attack is complete
            m_currentAttackDamage = null;
            // Program.Display(Name + " ending attack");
        }

        internal virtual void CancelCurrentAttack()
        {
            if (Program.m_LogSysBattle)
            {
                Program.Display("CancelCurrentAttack called for " + GetIDString());
            }
            //is an attack in progress
            if (m_currentAttackDamage != null && AttackTarget != null)
            {
                double currentTime = Program.MainUpdateLoopStartTime();
                //has the damage been applied
                if (m_currentAttackDamage.ActionCompleteTime > currentTime && TheCombatManager != null)
                {
                    //if not, remove it from the combat managers list
                    TheCombatManager.RemoveDamageFromList(m_currentAttackDamage, false);
                    //allow the player to start an attack as soon as the interuption is complete

                    // FISHING
                    // Fishing uses base attack speed - this is currently hacked on the client (CombatManager.cs / StartAttack() / line: 548 / to: 2500)
                    AttackProgressBeforeInterrupt = (TheCombatManager.CheckFishingAttack(this, this.AttackTarget) ? this.GetBaseAttackSpeed : CompiledStats.AttackSpeed) / 1000.0f;
                }
                else
                {
                    AttackProgressBeforeInterrupt = (currentTime - TimeAtLastAttack);
                }

                //reset variables so a new skill can be set in the next loop
                TimeActionWillComplete = 0;
                ActionInProgress = false;
            }
            if (m_currentProcData != null && TheCombatManager != null)
            {
                m_currentProcData.CancelSkillDamage(TheCombatManager);
            }
            EndAttack();
        }
        /// <summary>
        /// called when a skill starts it's casting time
        /// </summary>
        internal virtual void StartCasting()
        {
            m_currentAttackDamage = null;
        }

        #region wake up, died, concentration at zero, respawn

        internal void WakeupGameEffects()
        {
            double currentTime = Program.MainUpdateLoopStartTime(); //NetTime.Now;

            for (int currentEffect = 0; currentEffect < m_currentCharacterEffects.Count; currentEffect++)
            {
                CharacterEffect currentStatusEffect = m_currentCharacterEffects[currentEffect];

                if (currentStatusEffect != null && currentStatusEffect.StatusEffect != null)
                {
                    currentStatusEffect.StatusEffect.StartStatusEffectFromSleep(currentTime);
                }
            }
        }

        internal virtual void Died()
        {
            Dead = true;
            if (m_currentCharacterEffects.Count > 0 && Dead)
            {
                for (int effectIndex = m_currentCharacterEffects.Count - 1; effectIndex >= 0; effectIndex--)
                {
                    StatusEffect currentEffect = m_currentCharacterEffects[effectIndex].StatusEffect;

                    if (currentEffect != null && currentEffect.Template != null)
                    {
                        if (!currentEffect.Template.RemovedOnDeath) continue;
                    }

                    if (currentEffect != null && currentEffect.Template != null && currentEffect.Template.RequiresAppearanceUpdate)
                    {
                        switch (currentEffect.Template.EffectType)
                        {
                            case EFFECT_TYPE.CHANGE_ATTACK_SPEED:
                                {
                                    InfoUpdated(Inventory.EQUIP_SLOT.SLOT_ATTACK_SPEED);
                                    break;
                                }
                            case EFFECT_TYPE.CHANGE_SIZE:
                                {
                                    InfoUpdated(Inventory.EQUIP_SLOT.SLOT_SCALE);
                                    break;
                                }

                        }
                    }
                    m_currentCharacterEffects.Remove(m_currentCharacterEffects[effectIndex]);
                    m_statusListChanged = true;
                }

                CharacterEffectManager.UpdateCombatStats(this);
            }

            m_currentSkillTarget = null;
            m_hostileEntities.Clear();
            InCombat = false;
            m_nextSkillTarget = null;

            if (AttackTarget != null)
            {
                CombatManager combatManager = TheCombatManager;
                if (combatManager != null)
                {
                    combatManager.StopAttacking(this);
                    combatManager.RemoveDamageForEntity(this);
                }
            }
            AttackTarget = null;
            m_actionInProgress = false;

            if (TheCombatManager != null)
            {
                TheCombatManager.UpdateEntitiesDueToEntityDeath(this);
            }

        }

        internal virtual void ConcentrationZero()
        {
            this.ConcentrationZero(true);
        }

        internal virtual void ConcentrationZero(bool withStop)
        {
            ConcentrationFishDepleted = true;
            CancelCurrentAttack();

            //tell the thing we're attack to stop it
            if (this.AttackTarget != null && this.AttackTarget is ServerControlledEntity)
            {
                ((ServerControlledEntity)AttackTarget).Return();
                //Program.Display("Yo fish, stop it");
            }

            //clear out our own
            m_currentSkillTarget = null;
            m_hostileEntities.Clear();
            InCombat = false;
            m_nextSkillTarget = null;

            if (AttackTarget != null)
            {
                CombatManager combatManager = TheCombatManager;
                if (combatManager != null)
                {
                    if (withStop == true)
                        combatManager.StopAttacking(this);
                    else
                        AttackTarget = null;
                    combatManager.RemoveDamageForEntity(this);
                    this.TheCombatManager.RemoveAllReferenceToEntity(this);
                }
            }
            AttackTarget = null;
            m_actionInProgress = false;


            if (TheCombatManager != null)
            {
                TheCombatManager.UpdateEntitiesDueToEntityDeath(this);
            }

        }

        public virtual void Respawn(Vector3 respawnPosition, Character.Respawn_Type respawnType, int variableID)
        {
            if (respawnType != Character.Respawn_Type.ResSpell && CurrentHealth < 5)
            {
                CurrentHealth = (int)Math.Ceiling((double)MaxHealth / 2.0);
            }
            if (CurrentHealth < 1 && respawnType == Character.Respawn_Type.ResSpell)
            {
                CurrentEnergy = 0;
                CurrentHealth = 1;
            }
            if (CurrentHealth <= 0)
            {
                CurrentHealth = 1;
            }
            if (respawnType == Character.Respawn_Type.ResIdol)
            {
                CurrentHealth = MaxHealth;
            }
            CurrentPosition.m_position = respawnPosition;

            EntityPartitionCheck();
            WakeupGameEffects();
            Dead = false;


        }

        #endregion

        internal virtual List<AggroData> GetAssistList()
        {
            return null;
        }

        internal void InfoUpdated(Inventory.EQUIP_SLOT info)
        {
            for (int i = 0; i < m_updatedInfo.Count; i++)
            {
                if (m_updatedInfo[i] == info)
                {
                    return;
                }
            }
            m_updatedInfo.Add(info);
        }

        #endregion

        #region is entity bool checks e.g. targetting, enemy, ally

        internal virtual bool IsTargetting(CombatEntity otherEntity)
        {
            if ((AttackTarget == otherEntity) || (CurrentSkillTarget == otherEntity) || (NextSkillTarget == otherEntity))
            {
                return true;
            }
            return false;
        }
        internal virtual bool IsEnemyOf(CombatEntity otherEntity)
        {
            bool isEnemy = false;

            isEnemy = (Type != otherEntity.Type);

            int opinion = otherEntity.GetOpinionOf(this);
            bool onHateList = IsOnHateList(otherEntity);

            isEnemy = (onHateList == true || opinion < 50);

            return isEnemy;

        }
        internal virtual bool IsAllyOf(CombatEntity otherEntity)
        {
            bool isAlly = (IsEnemyOf(otherEntity) == false);

            return isAlly;
        }
        internal virtual bool IsInPartyWith(CombatEntity otherEntity)
        {
            if (otherEntity == this)
            {
                return true;
            }

            return false;

        }
        internal virtual int GetOpinionOf(CombatEntity otherEntity)
        {
            return 100;
        }
        internal virtual bool IsOnHateList(CombatEntity otherEntity)
        {
            return false;
        }
        internal virtual bool IsPVP()
        {
            return false;
        }

        #endregion

        #region interest data
        internal void CheckInterestData(double currentTime)
        {
            //if nothing has happened
            if (m_hasMoved == false || currentTime < (m_timeAtLastInterestCheck + MIN_TIME_BETWEEN_INTEREST_CHECKS))
            {
                //don't bother to check the lists
                return;
            }
            m_timeAtLastInterestCheck = currentTime;
            m_hasMoved = false;
            CheckInterestEntities();
            CheckInterestAreas();
        }
        /// <summary>
        /// updates the list of people that this entity is interested in
        /// </summary>
        void CheckInterestEntities()
        {
            float maxInterestRangeSQR = 3600;
            float minInterestRange = 40;

            //remember any entities that have been removed
            //only create this list if you need to
            List<CombatEntity> removedEntities = null;
            //remember all entities that were not there before
            //only create this list if you need to
            List<CombatEntity> addedEntities = null;

            CombatEntity currentEntity = null;
            double currentDistanceSQR = 0;
            //check the current list to see if anyone should be removed
            for (int i = 0; i < m_entitiesOfInterest.Count; i++)
            {
                currentEntity = m_entitiesOfInterest[i];
                currentDistanceSQR = Utilities.Difference2DSquared(currentEntity.CurrentPosition.m_position, CurrentPosition.m_position);
                //if they are out of max range, in a different zone or destroyed
                if (currentDistanceSQR > maxInterestRangeSQR || CurrentZone != currentEntity.CurrentZone || currentEntity.Destroyed == true)
                {
                    //remove them from the list
                    RemoveEntityFromInterestList(currentEntity);
                    //m_entitiesOfInterest.Remove(currentEntity);
                    i--;
                    //ForgetStatusEffectsOnEntity(currentEntity);
                    //add them to the list of people who have been removed
                    if (removedEntities == null)
                    {
                        removedEntities = new List<CombatEntity>();
                    }
                    if (Type == EntityType.Player)
                    {
                        if (Program.m_LogInterestLists)
                        {
                            Program.Display("CheckInterestData operation:" + GetIDString() + " removed " + currentEntity.GetIDString() + "from interestList");
                        }
                    }
                    removedEntities.Add(currentEntity);
                }
            }
            currentEntity = null;


            //find entities within the add to interest range
            List<CombatEntity> entitiesWithinCloseRange = new List<CombatEntity>();
            CurrentZone.PartitionHolder.AddEntitiesInRangeToList(null, CurrentPosition.m_position, minInterestRange, entitiesWithinCloseRange, m_defaultInterestTypes, this);
            //check each one to see if it is already known
            for (int i = 0; i < entitiesWithinCloseRange.Count; i++)
            {
                currentEntity = entitiesWithinCloseRange[i];
                if (m_entitiesOfInterest.Contains(currentEntity) == false)
                {
                    //if it is not known add it to the list of new entities 
                    if (addedEntities == null)
                    {
                        addedEntities = new List<CombatEntity>();
                    }
                    addedEntities.Add(currentEntity);
                    AddEntityToInterestList(currentEntity);
                    //m_entitiesOfInterest.Add(currentEntity);
                    //EntityHasChangedStatusList(currentEntity);
                    if (Type == EntityType.Player)
                    {
                        //Program.Display("CheckInterestData operation:" + GetIDString() + " added " + currentEntity.GetIDString() + "to interestList");
                    }
                }
            }
            currentEntity = null;


            //for all removed entities
            if (removedEntities != null)
            {
                for (int i = 0; i < removedEntities.Count; i++)
                {
                    //tell the entity they are nolonger interested in this 
                    currentEntity = removedEntities[i];
                    currentEntity.RemoveEntityFromInterestList(this);
                }
                currentEntity = null;
            }

            //for all new entities
            if (addedEntities != null)
            {
                //add them to the list
                for (int i = 0; i < addedEntities.Count; i++)
                {
                    //notify the entity that they are now interested in this entity
                    currentEntity = addedEntities[i];
                    currentEntity.AddEntityToInterestList(this);
                }
            }
        }
        void CheckInterestAreas()
        {

            //get the list of areas in your partition
            List<EffectArea> partitionsEffectAreas = null;// CurrentPartition.Areas;
            if (CurrentPartition != null)
            {
                //use the correct list
                partitionsEffectAreas = CurrentPartition.Areas;
            }
            else
            {
                //empty list

                Program.Display(GetIDString() + " does not appear to be in the partitioned area zone=" + CurrentZone.m_zone_name + " pos = " + CurrentPosition.m_position.X + "," + CurrentPosition.m_position.Y + "," + CurrentPosition.m_position.Z);
                partitionsEffectAreas = new List<EffectArea>(); ;
            }
            //areas that have been removed from your current list
            List<EffectArea> removedAreas = null;
            //areas that have been added to your current list
            List<EffectArea> addedAreas = null;


            //check your current list for areas that you have left
            for (int i = m_areas.Count - 1; i >= 0; i--)
            {
                EffectArea currentArea = m_areas[i];
                //check the collision
                bool isInArea = currentArea.PositionIsInArea(CurrentPosition.m_position);
                //if you are not inside the area
                if (isInArea == false || currentArea.TheZone != CurrentZone)
                {
                    //remove it from your list
                    m_areas.Remove(currentArea);
                    //add it to the list of removed areas
                    if (removedAreas == null)
                    {
                        removedAreas = new List<EffectArea>();
                    }
                    removedAreas.Add(currentArea);
                }
            }

            //for all areas within the partition
            for (int i = partitionsEffectAreas.Count - 1; i >= 0; i--)
            {
                EffectArea currentArea = partitionsEffectAreas[i];
                //check if it's already in the list (if it's in the list it has been checked)
                if (m_areas.Contains(currentArea) == false)
                {
                    //if not check if it collides with the entity
                    bool isInArea = currentArea.PositionIsInArea(CurrentPosition.m_position);
                    //if so then add it to the current list
                    if (isInArea == true)
                    {
                        m_areas.Add(currentArea);
                        //add it to the list of new areas
                        if (addedAreas == null)
                        {
                            addedAreas = new List<EffectArea>();
                        }
                        addedAreas.Add(currentArea);
                    }
                }
            }


            //notify areas that you have left
            if (removedAreas != null)
            {
                for (int i = removedAreas.Count - 1; i >= 0; i--)
                {
                    EffectArea currentArea = removedAreas[i];
                    currentArea.EntityLeavingArea(this);
                }
                m_areasEffectsToBeChecked = true;
            }
            //notify areas that you have arrived
            if (addedAreas != null)
            {
                for (int i = addedAreas.Count - 1; i >= 0; i--)
                {
                    EffectArea currentArea = addedAreas[i];
                    currentArea.EntityEnteringArea(this);
                }
                m_areasEffectsToBeChecked = true;
            }

        }
        /// <summary>
        /// To Be Called if an entity is to be destroyed
        /// </summary>
        internal void DestroyInterestLists()
        {


            for (int i = m_areas.Count - 1; i >= 0; i--)
            {
                m_areas[i].EntityLeavingArea(this);
            }
            m_areas.Clear();
            for (int i = 0; i < m_entitiesOfInterest.Count; i++)
            {
                m_entitiesOfInterest[i].RemoveEntityFromInterestList(this);
            }
            m_entitiesOfInterest.Clear();
            ClearDownHateList();
        }
        internal virtual void EntityOfInterestChangedCombatType(CombatEntity theEntity)
        {


        }
        /// <summary>
        /// Adds theEntity to the receivingEntitys Interest List
        /// This should only be called when the receivingEntity is also added to theEntities InterestList
        /// </summary>
        /// <param name="theEntity"></param>
        internal virtual void AddEntityToInterestList(CombatEntity theEntity)
        {
            //check they are not already on the list
            if (m_entitiesOfInterest.Contains(theEntity) == true)
            {
                //if they are on the list then throw an error
                Program.Display(INTEREST_LIST_ERROR_STRING + "AddEntityToInterestList tried to add existing entity " + theEntity.GetIDString());
            }
            else
            {
                //otherwise add them to the list
                EntityHasChangedStatusList(theEntity);
                m_entitiesOfInterest.Add(theEntity);
                if (Type == EntityType.Mob && theEntity.Type == EntityType.Player)
                {
                    SendLockRelationshipTo((Character)theEntity);
                }
            }
        }
        /// <summary>
        /// removes theEntity from the receivingEntitys Interest List
        /// This should only be called when the receivingEntity is also removed from theEntities InterestList
        /// </summary>
        /// <param name="theEntity"></param>
        internal virtual void RemoveEntityFromInterestList(CombatEntity theEntity)
        {
            //remove the entity checking it was removed
            bool entityRemoved = m_entitiesOfInterest.Remove(theEntity);

            //if nothing removed then throw an error
            if (entityRemoved == false)
            {
                Program.Display(INTEREST_LIST_ERROR_STRING + "RemoveEntityFromInterestList failed to remove entity " + theEntity.GetIDString());
            }
            else
            {
                ForgetStatusEffectsOnEntity(theEntity);
                if (Type == EntityType.Player)
                {
                    //  Program.Display(GetIDString() + " removed " + theEntity.GetIDString() + "from interestList");
                }
            }
        }
        internal bool IsInterestedInEntity(CombatEntity theEntity)
        {
            return m_entitiesOfInterest.Contains(theEntity);
        }
        internal void EffectAreaNowInRange(EffectArea area)
        {
            if (area != null && m_areas.Contains(area) == false)
            {
                m_areasEffectsToBeChecked = true;
                m_areas.Add(area);
            }
            //cry baby cry
            else
            {
                Program.Display(INTEREST_LIST_ERROR_STRING + "EffectAreaNowInRange failed to remove area");

            }
        }
        internal void EffectAreaNowOutOfRange(EffectArea area)
        {
            if (area != null && m_areas.Contains(area) == true)
            {
                m_areasEffectsToBeChecked = true;
                m_areas.Remove(area);
            }
            //cry baby cry
            else
            {
                Program.Display(INTEREST_LIST_ERROR_STRING + "EffectAreaNowOutOfRange failed to remove area");

            }
        }
        #endregion //interest data

        #region position/area/pvp/entity range type methods

        internal virtual void StopTheEntity()
        {
            CurrentPosition.m_currentSpeed = 0;
        }

        internal bool EntityCanBeAffectedByAttack(CombatEntity castingEntity)
        {
            bool canContinue = true;
            if (m_statusPreventsActions.HostileAction)
            {
                for (int i = m_currentCharacterEffects.Count - 1; i > -1 && canContinue == true; i--)
                {
                    StatusEffect currentEffect = m_currentCharacterEffects[i].StatusEffect;
                    if (currentEffect == null)
                        continue;

                    switch (currentEffect.Template.EffectType)
                    {
                        case EFFECT_TYPE.PLAYING_DEAD:
                            {
                                if (castingEntity.Level <= currentEffect.m_effectLevel.getUnModifiedAmount())
                                {
                                    bool remainsHidden = castingEntity.AttemptToSpotFromHidden(this);
                                    if (remainsHidden)
                                    {
                                        canContinue = false;
                                    }
                                }
                                break;
                            }
                        default:
                            {
                                break;
                            }
                    }

                }
            }
            return canContinue;
        }

        internal bool EntityCanBeAffectedBySkill(CombatEntity castingEntity)
        {
            bool canContinue = true;
            if (m_statusPreventsActions.HostileAction && IsEnemyOf(castingEntity) == true)
            {
                for (int i = m_currentCharacterEffects.Count - 1; i > -1 && canContinue == true; i--)
                {
                    StatusEffect currentEffect = m_currentCharacterEffects[i].StatusEffect;
                    if (currentEffect == null)
                        continue;

                    switch (currentEffect.Template.EffectType)
                    {
                        case EFFECT_TYPE.PLAYING_DEAD:
                            {
                                if (castingEntity.Level <= currentEffect.m_effectLevel.getUnModifiedAmount())
                                {
                                    bool remainsHidden = castingEntity.AttemptToSpotFromHidden(this);
                                    if (remainsHidden)
                                    {
                                        canContinue = false;
                                    }
                                }
                                break;
                            }
                        default:
                            {
                                break;
                            }
                    }

                }
            }
            return canContinue;
        }

        static internal float GetDistanceBetweenEntities(CombatEntity ent1, CombatEntity ent2)
        {
            float distance = 0;
            if (ent1 != null && ent2 != null)
            {
                distance = Utilities.Difference2D(ent1.CurrentPosition.m_position, ent2.CurrentPosition.m_position);

                distance = distance - (ent1.Radius + ent2.Radius);
            }
            else
            {
                Program.Display("CombatEntity::GetDistanceBetweenEntities(CombatEntity ent1, CombatEntity ent2) - error:null entity encountered");
            }

            return distance;
        }

        internal Vector3 GetValidLocationFromTarget(Vector3 targetLocation, float distFromTargetRequired, ref bool foundPoint)
        {
            foundPoint = false;
            Vector3 currentPos = CurrentPosition.m_position;
            Vector3 closestPos = currentPos;

            Zone currentZone = CurrentZone;
            if (currentZone != null)
            {
                if (distFromTargetRequired == 0)
                {

                }
                else
                {
                    Vector3 vecToTarget = targetLocation - CurrentPosition.m_position;
                    vecToTarget.Y = 0;
                    if (vecToTarget.LengthSquared() == 0)
                    {
                        vecToTarget = new Vector3(1, 0, 0);
                    }
                    else
                    {
                        vecToTarget.Normalize();
                    }

                    Vector3 destinationPosition = targetLocation - distFromTargetRequired * vecToTarget;
                    Vector3 destinationVector = targetLocation - CurrentPosition.m_position;

                    Vector3 collisionPosition = CurrentZone.CheckCollisions(CurrentPosition.m_position, destinationPosition, 0, 1, false);

                    // If we have LOS
                    if ((collisionPosition - destinationPosition).LengthSquared() < MAX_SQUARED_COLLISION_ERROR)
                    {
                        // And theres enough room
                        if (!CurrentZone.Collison.checkCollisions(destinationPosition, Radius))
                        {
                            foundPoint = true;
                            closestPos = destinationPosition;
                        }
                        else
                        {
                            Program.Display("RADIUS CHECK FAILED");
                        }
                    }
                    else
                    {
                        Program.Display("NO LINE OF SIGHT");
                    }

                    #region Old Code
                    //double radianChangePerTest = Math.PI / 4;
                    //double currentRadianTest = radianChangePerTest;
                    //Vector3 vectorFromTarget = destinationPosition - targetLocation;

                    //bool test = false;

                    //if (positionPassed == true)
                    //{
                    //    test = CurrentZone.Collison.CheckForCollisions(closestPos, Radius);
                    //    Program.Display("Player has collided! : " + test);
                    //}

                    //if it fails collision try moving in a different direction
                    /*while (currentRadianTest <= Math.PI && positionPassed == false)
                    {
                        Matrix rotateMatrix = Matrix.CreateRotationY((float)currentRadianTest);

                        //test positive
                        Vector3 rotatedScaledDestinationVector = vectorFromTarget;
                        rotatedScaledDestinationVector.Normalize();
                        rotatedScaledDestinationVector = Vector3.Transform(rotatedScaledDestinationVector, rotateMatrix);

                        Vector3 rotatedPosition = targetLocation + rotatedScaledDestinationVector;
                        Vector3 rotationCollisionPosition = currentZone.CheckCollisions(CurrentPosition.m_position, rotatedPosition, Radius, 1, false);
                        if ((rotationCollisionPosition - rotatedPosition).LengthSquared() < MAX_SQUARED_COLLISION_ERROR)
                        {
                            positionPassed = true;

                            foundPoint = true;
                            destinationPosition = rotatedPosition;
                            //closestPos = destinationPosition;
                            closestPos = rotationCollisionPosition;     // PDH
                            destinationVector = -rotatedScaledDestinationVector;

                            destinationVector.Normalize();
                        }
                        //test -ve
                        if (positionPassed == false)
                        {
                            rotateMatrix = Matrix.CreateRotationY((float)-currentRadianTest);

                            //test positive
                            rotatedScaledDestinationVector = vectorFromTarget;
                            rotatedScaledDestinationVector = Vector3.Transform(rotatedScaledDestinationVector, rotateMatrix);

                            rotatedPosition = targetLocation + rotatedScaledDestinationVector;
                            rotationCollisionPosition = currentZone.CheckCollisions(CurrentPosition.m_position, rotatedPosition, Radius, 1, false);
                            if ((rotationCollisionPosition - rotatedPosition).LengthSquared() < MAX_SQUARED_COLLISION_ERROR)
                            {
                                positionPassed = true;

                                foundPoint = true;
                                destinationPosition = rotatedPosition;
                                //closestPos = destinationPosition;
                                closestPos = rotationCollisionPosition;     // PDH
                                destinationVector = -rotatedScaledDestinationVector;
                                destinationVector.Normalize();
                            }
                        }
                        currentRadianTest += radianChangePerTest;
                    }*/
                    #endregion
                }
            }

            return closestPos;
        }

        internal void EntityPartitionCheck()
        {
            //check what partition it should be in
            ZonePartition newPartition = CurrentZone.PartitionHolder.GetPartitionForPosition(CurrentPosition.m_position);
            //is that the same partition it is currently in
            if (CurrentPartition != newPartition)
            {
                //if not, set the new partition (this will call entering and leaving code)
                CurrentPartition = newPartition;
            }
            //if it's checking it's partition then it must have moved
            m_hasMoved = true;
        }

        /// <summary>
        /// try to spot a hidden entity using the m_template.m_spot_hidden value
        /// </summary>
        /// <param name="theEntity"></param>
        /// <returns>true if the entity remains hidden</returns>
        internal virtual bool AttemptToSpotFromHidden(CombatEntity theEntity)
        {
            //normally you can't undo hidden
            return true;
        }

        /// <summary>
        /// Checks that all the effects for areas the entity has left are removed
        /// checks that effects for area's the entity have entered are applied
        /// </summary>
        virtual internal void CheckAreaEffects()
        {

            List<EntityAreaConditionalEffect> addedEffects = null;
            List<EntityAreaConditionalEffect> removedEffects = null;
            //check the old effects to see if you are still in the area
            for (int i = m_areaEffectsList.Count - 1; i >= 0; i--)
            {
                EntityAreaConditionalEffect currentEffect = m_areaEffectsList[i];
                //remember those that you are nolonger under
                if (m_areas.Contains(currentEffect.TheEffect.TheArea) == false || Dead == true)
                {
                    if (removedEffects == null)
                    {
                        removedEffects = new List<EntityAreaConditionalEffect>();
                    }
                    removedEffects.Add(currentEffect);
                    m_areaEffectsList.Remove(currentEffect);
                }
            }
            //don't try to get effects from areas if you have no areas to check
            if (m_areas.Count == 0 || Dead == true)
            {
                if (removedEffects != null)
                {
                    //end any effects that are being removed
                    for (int i = 0; i < removedEffects.Count; i++)
                    {
                        EntityAreaConditionalEffect currentEffect = removedEffects[i];
                        currentEffect.EndEffect(this);

                    }
                }
                return;
            }
            if (removedEffects == null)
            {
                removedEffects = new List<EntityAreaConditionalEffect>();
            }
            List<AreaConditionalEffect> preEffects = new List<AreaConditionalEffect>();
            List<AreaConditionalEffect> postEffects = new List<AreaConditionalEffect>();
            //get information from all the areas that you are in
            for (int i = m_areas.Count - 1; i >= 0; i--)
            {
                EffectArea currentArea = m_areas[i];

                //get all pre effects from the areas that you are in for this type
                //pre affects change what post effects are applied to the entity(EG destroys poison bog)
                currentArea.AddPreEffectsToList(preEffects);
                //get all the post effects from the area you are in for this type
                //these will actually do something to the entity
                currentArea.AddPostEffectsToList(postEffects);
            }

            //apply the pre effects to the new post effects and the current post effects
            AreaConditionalEffect.ClearDownPostEffectsFromPreEffects(preEffects, postEffects, m_areaEffectsList, removedEffects);
            //now all post effects are valid for this entity
            //make sure they are all active
            for (int i = postEffects.Count - 1; i >= 0; i--)
            {
                AreaConditionalEffect theEffect = postEffects[i];
                //has it already been applied
                bool isApplied = false;
                for (int appliedListIndex = 0; appliedListIndex < m_areaEffectsList.Count && isApplied == false; appliedListIndex++)
                {
                    if (m_areaEffectsList[appliedListIndex].TheEffect == theEffect)
                    {
                        isApplied = true;
                    }
                    //check this is not the same type of effect
                    else if (AreaConditionalEffect.CompareEffects(m_areaEffectsList[appliedListIndex].TheEffect, theEffect) == true)
                    {
                        isApplied = true;
                    }
                }
                //check it's not the same as one you have just removed
                if (isApplied == false)
                {
                    for (int removedListIndex = removedEffects.Count - 1; removedListIndex >= 0 && isApplied == false; removedListIndex--)
                    {
                        EntityAreaConditionalEffect removedEffect = removedEffects[removedListIndex];
                        //first check there was not the same effect on another zone that has now been removed
                        if (AreaConditionalEffect.CompareEffects(removedEffect.TheEffect, theEffect) == true)
                        {
                            //if it was the same, use the same effect wrapper but attach it to the new area
                            removedEffect.ChangeOwnerEffect(theEffect);
                            //add that to the overall list
                            m_areaEffectsList.Add(removedEffect);
                            removedEffects.Remove(removedEffect);
                            isApplied = true;
                        }
                    }
                }
                //if it has not been applied yet then set it up then add it to the list
                if (isApplied == false)
                {
                    //check it is the right sort of target
                    if (theEffect.CanBeAppliedToEntity(this) == true)
                    {
                        //create the holder
                        EntityAreaConditionalEffect newEffect = new EntityAreaConditionalEffect(theEffect);

                        m_areaEffectsList.Add(newEffect);
                        if (addedEffects == null)
                        {
                            addedEffects = new List<EntityAreaConditionalEffect>();
                        }
                        addedEffects.Add(newEffect);
                    }
                }
            }

            //end any effects that are being removed
            for (int i = 0; i < removedEffects.Count; i++)
            {
                EntityAreaConditionalEffect currentEffect = removedEffects[i];
                currentEffect.EndEffect(this);

            }
            if (addedEffects != null)
            {
                for (int i = 0; i < addedEffects.Count; i++)
                {
                    EntityAreaConditionalEffect newEffect = addedEffects[i];
                    //call any set up code required
                    newEffect.StartUpEffect(this);
                }
            }
        }

        internal virtual void RemoveFromHateList(CombatEntity removeEntity)
        {

        }

        internal virtual void AddToHateList(CombatEntity newEntity)
        {

        }

        internal virtual void ClearDownHateList()
        {

        }

        internal virtual bool IsInPVPWithEntity(CombatEntity theEntity)
        {
            return false;
        }

        internal virtual bool IsInPVPType(Character.PVPType type)
        {
            return false;
        }

        internal virtual void PVPTypeChanged()
        {

        }

        internal virtual Vector3 GetCombatLocation(CombatEntity aggressor)
        {
            return m_currentPosition.m_position;
        }

        public bool avoidanceTest(CombatEntity attackingEntity, CombatEntity targetEntity, EntitySkill entitySkill)
        {
            if (entitySkill.Template.AvoidanceType == AVOIDANCE_TYPE.NONE)
                return false;
            if (targetEntity.Type == EntityType.Player)
            {
                switch (entitySkill.Template.AvoidanceType)
                {
                    case AVOIDANCE_TYPE.PHYSICAL:
                        {
                            ((Character)targetEntity).testAbilityUpgrade(ABILITY_TYPE.FORTITUDE);
                            break;
                        }
                    case AVOIDANCE_TYPE.SPELL:
                        {
                            ((Character)targetEntity).testAbilityUpgrade(ABILITY_TYPE.WARDING);
                            break;
                        }
                    case AVOIDANCE_TYPE.MOVEMENT:
                        {
                            ((Character)targetEntity).testAbilityUpgrade(ABILITY_TYPE.EVASION);
                            break;
                        }
                    case AVOIDANCE_TYPE.WOUNDING:
                        {
                            ((Character)targetEntity).testAbilityUpgrade(ABILITY_TYPE.VIGOUR);
                            break;
                        }
                    case AVOIDANCE_TYPE.WEAKENING:
                        {
                            ((Character)targetEntity).testAbilityUpgrade(ABILITY_TYPE.WILLPOWER);
                            break;
                        }

                }
            }
            int avoidanceRating = targetEntity.GetAvoidanceType(entitySkill.Template.AvoidanceType);
            if (avoidanceRating <= 0)
            {
                return false;
            }
            int total = avoidanceRating + 50 * attackingEntity.Level;
            if (attackingEntity as Character != null) // means it has to be a player as combat entitys that are controlled by the server are server controlled entities
            {
                avoidanceRating = (int)(avoidanceRating * Program.processor.EvasionFactorManager.GetEvasionFactorForClass(((Character)attackingEntity).m_class.m_classType));
            }
            if (targetEntity.Type == EntityType.Player && avoidanceRating * 100 > 30 * total)
            {
                int rndNum = Program.getRandomNumber(100);
                if (Program.m_LogDamage)
                {
                    Program.Display("avoidance test level=" + attackingEntity.Level + ",rawrating=" + avoidanceRating + ", maxed, rndnum=" + rndNum);
                }
                if (rndNum < 40)//30% cap for players
                    return true;
                return false;
            }
            int rndNum2 = Program.getRandomNumber(total);
            if (Program.m_LogDamage)
            {
                Program.Display("avoidance test level=" + attackingEntity.Level + ",rating=" + avoidanceRating + ", total=" + total + ", rndnum=" + rndNum2);
            }
            if (rndNum2 < avoidanceRating)
                return true;
            return false;
        }

        internal void PointTowardsEntity(CombatEntity otherEntity)
        {
            Vector3 newDirection = otherEntity.CurrentPosition.m_position - CurrentPosition.m_position;
            newDirection.Y = 0;
            if (newDirection.Length() > 0)
            {
                newDirection.Normalize();

                CurrentPosition.m_direction = newDirection;
                CurrentPosition.CorrectAngleForDirection();
            }


        }

        #endregion

        #region send/write messages

        internal void SendEntityChangedDirectionMessage()
        {
            //a list of who needs the data sent to them
            List<NetConnection> connections = new List<NetConnection>();
            //search through the interest list
            for (int i = 0; i < m_entitiesOfInterest.Count; i++)
            {
                CombatEntity currentEntity = m_entitiesOfInterest[i];
                //only players need the information sent
                if (currentEntity != null && currentEntity.Type == EntityType.Player)
                {
                    Character currentCharacter = (Character)currentEntity;
                    if (currentCharacter != null && currentCharacter.m_player != null && currentCharacter.m_player.connection != null)
                    {
                        connections.Add(currentCharacter.m_player.connection);
                    }
                }
            }
            //if someone needs to know
            if (connections.Count > 0)
            {
                //create the message
                NetOutgoingMessage msg = Program.Server.CreateMessage();
                msg.WriteVariableUInt32((uint)NetworkCommandType.EntityChangedDirection);
                WriteDirectionUpdateToMessage(msg);
                Program.processor.SendMessage(msg, connections, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.EntityChangedDirection);

            }

        }
        internal void SendLockDataChanged()
        {
            //a list of who needs the data sent to them
            List<NetConnection> connections = new List<NetConnection>();
            List<NetConnection> ownerConnections = new List<NetConnection>();
            //search through the interest list
            for (int i = 0; i < m_entitiesOfInterest.Count; i++)
            {
                CombatEntity currentEntity = m_entitiesOfInterest[i];
                //only players need the information sent
                if (currentEntity != null && currentEntity.Type == EntityType.Player)
                {
                    Character currentCharacter = (Character)currentEntity;
                    if (currentCharacter != null && currentCharacter.m_player != null && currentCharacter.m_player.connection != null)
                    {
                        List<Character> ownerList = null;
                        if (m_lockOwner != null)
                        {
                            ownerList = m_lockOwner.GetCharacters;
                        }

                        if (m_lockOwner == null || (ownerList == null || ownerList.Contains(currentCharacter) == false))
                        {
                            connections.Add(currentCharacter.m_player.connection);
                        }
                        else
                        {
                            ownerConnections.Add(currentCharacter.m_player.connection);
                        }
                    }
                }
            }
            //if someone needs to know
            if (connections.Count > 0)
            {

                if (m_lockOwner == null)
                {
                    SendLockMessage(connections, TargetLockType.Open);
                }
                else
                {
                    SendLockMessage(connections, TargetLockType.Locked);
                }
            }
            if (ownerConnections.Count > 0)
            {
                SendLockMessage(ownerConnections, TargetLockType.Owned);
            }
            m_lockOwnerChanged = false;
        }
        internal void SendLockMessage(List<NetConnection> connections, TargetLockType lockType)
        {
            NetOutgoingMessage msg = Program.Server.CreateMessage();
            msg.WriteVariableUInt32((uint)NetworkCommandType.CombatEntityLocked);
            //mob type
            msg.WriteVariableInt32((int)Type);
            //mob id 
            msg.WriteVariableInt32(ServerID);

            msg.WriteVariableInt32((int)lockType);



            Program.processor.SendMessage(msg, connections, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.CombatEntityLocked);

        }
        internal void SendLockRelationshipTo(Character theCharacter)
        {
            if (theCharacter == null)
            {
                return;
            }
            List<NetConnection> connections = new List<NetConnection>();
            if (theCharacter != null && theCharacter.m_player != null && theCharacter.m_player.connection != null)
            {
                connections.Add(theCharacter.m_player.connection);
            }

            if (connections.Count > 0)
            {

                if (m_lockOwner == null)
                {
                    SendLockMessage(connections, TargetLockType.Open);
                }
                else
                {
                    if (theCharacter.HasOwnership(this) == true)
                    {
                        SendLockMessage(connections, TargetLockType.Owned);
                    }
                    else
                    {
                        SendLockMessage(connections, TargetLockType.Locked);
                    }
                }
            }
        }
        
        internal void WriteDataRequiredByInterestedEntitiesToMessage(NetOutgoingMessage msg)
        {
            //mob type
            msg.WriteVariableInt32((int)Type);
            //mob id 
            msg.WriteVariableInt32(ServerID);
            //mob health
            if (Type == CombatEntity.EntityType.Mob)
            {
                msg.WriteVariableInt32(CurrentHealth);
                msg.WriteVariableInt32(CurrentConcentrationFishing);
                
            }
            else
            {
                int healthPercent = (int)Math.Ceiling((float)(CurrentHealth * 100) / (float)MaxHealth);
                if (CurrentHealth <= 0)
                {
                    healthPercent = 0;
                }
                int concentrationPercent = (int)Math.Ceiling((float)(CurrentConcentrationFishing * 100) / (float)MaxConcentrationFishing);
                if (CurrentConcentrationFishing <= 0)
                {
                    concentrationPercent = 0;
                }
                msg.WriteVariableInt32(healthPercent);
                msg.WriteVariableInt32(concentrationPercent);
            }
            if (InCombat == true)
            {
                msg.Write((byte)1);
            }
            else
            {
                msg.Write((byte)0);
            }
            msg.Write((float)m_currentPosition.m_position.X);
            msg.Write((float)m_currentPosition.m_position.Y);
            msg.Write((float)m_currentPosition.m_position.Z);

        }
        internal void WriteDirectionUpdateToMessage(NetOutgoingMessage msg)
        {
            //mob type
            msg.WriteVariableInt32((int)Type);
            //mob id 
            msg.WriteVariableInt32(ServerID);
            msg.Write(CurrentPosition.m_yangle);
        }
        internal void WriteInCombatDataToMessage(NetOutgoingMessage msg)
        {
            //mob type
            msg.WriteVariableInt32((int)Type);
            //mob id 
            msg.WriteVariableInt32(ServerID);

            //is it in combat
            if (InCombat == true)
            {
                msg.Write((byte)1);
            }
            else
            {
                msg.Write((byte)0);
            }

        }
        internal void WriteStatusEffectsToMessage(NetOutgoingMessage msg, double currentTime)
        {
            //strip out combination effects
            List<CharacterEffect> writeList = GetStrippedStatusEffects();

            //the number of current Status Effects
            msg.WriteVariableInt32(writeList.Count);

            //write the ID of each Status Effect
            for (int i = 0; i < writeList.Count; i++)
            {
                if (writeList[i].StatusEffect == null)
                    continue;

                StatusEffect currentEffect = writeList[i].StatusEffect;
                msg.WriteVariableInt32((int)currentEffect.Template.StatusEffectID);
                msg.WriteVariableInt32((int)currentEffect.m_statusEffectLevel);

                //we're losing a second on the client here by casting to an (int), instead Round
                //int secondsRemaining =(int)(currentEffect.m_effectLevel.m_duration -(currentTime - currentEffect.StartTime));
                int secondsRemaining = (int)Math.Round((currentEffect.m_effectLevel.m_duration - (currentTime - currentEffect.StartTime)));


                msg.WriteVariableInt32(secondsRemaining);
                msg.Write(currentEffect.CasterAbilityLevel);
                msg.Write(currentEffect.StatModifier);
            }
        }

        #endregion

    }

}
