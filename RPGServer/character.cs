using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms.VisualStyles;
using Lidgren.Network;
using MainServer.Combat;
using MainServer.Crafting;
using MainServer.Factions;
using MainServer.partitioning;
using MainServer.Signposting;
using MainServer.player_offers;
using XnaGeometry;
using MainServer.Localise;
using MySql.Data.MySqlClient;

namespace MainServer
{
	#region helpers and enums

	enum HudSlotItemType
    {
        HUD_SLOT_ITEM_EMPTY = 0,
        HUD_SLOT_ITEM_SKILL = 1,
        HUD_SLOT_ITEM_ITEM = 2,
        HUD_SLOT_EITHER = 3,
        HUD_SLOT_EQUIPMENT = 4
    };

    internal class HudSlotItem
    {
        internal HudSlotItem(HudSlotItemType slotItemType, int item_id)
        {
            m_slotItemType = slotItemType;
            m_item_id = item_id;
        }

        internal string getString()
        {
            return ((int)m_slotItemType).ToString() + "^" + m_item_id;
        }
        public HudSlotItemType m_slotItemType;
        public int m_item_id;
    };




    internal class FactionStanding
    {
        int m_factionID;
        int m_factionPoints;

        internal int FactionID
        {
            get { return m_factionID; }
            set { m_factionID = value; }
        }
        internal int FactionPoints
        {
            get { return m_factionPoints; }
            set { m_factionPoints = value; }
        }

        FactionStanding(int factionID, int factionPoints)
        {
            m_factionID = factionID;
            m_factionPoints = factionPoints;
        }

    }
    public class ActorPosition
    {
        public ActorPosition()
        {
            m_position = new Vector3(0, 0, 0);
            m_direction = new Vector3(1, 0, 0);
        }
        public Vector3 m_position;
        public Vector3 m_direction;
        public float m_yangle;
        public float m_currentSpeed;
        
        /// <summary>
        /// Sets m_yangle based on the value of m_direction
        /// </summary>
        internal void CorrectAngleForDirection()
        {
           float DTOR = 0.0174532925f;
            //double angle = 180.0 - (float)Math.Atan2(-m_direction.X, m_direction.Z) / DTOR;
           m_yangle = (float)Math.Atan2(m_direction.X, m_direction.Z) / DTOR;
            
        }
        /// <summary>
        /// Sets m_direction based on the value of m_yangle
        /// </summary>
        internal void CorrectDirectionForAngle()
        {
            /* float dirx = (float)-Math.Sin((double)(180 - m_yangle) * Math.PI / 180);
             float dirz = (float)Math.Cos((double)(180 - m_yangle) * Math.PI / 180);
             m_direction = new Vector3(dirx, 0, dirz);*/

            m_direction = Utilities.GetDirectionFromYAngle(m_yangle);
 
        }
    };
    public enum GENDER
    {
        GENDER_BOTH = 0,
        MALE = 1,
        FEMALE = 2
    }

    class PVPDamage
    {
        Character m_character = null;
        List<PVPDamageStub> m_damageList = new List<PVPDamageStub>();
        /// <summary>
        /// This Is Only Tallied After Death
        /// </summary>
        int m_totalDamage = 0;
        internal int TotalDamage
        {
            get { return m_totalDamage; }
            set
            {
                m_totalDamage = value;
            }
        }
        internal Character CharacterInvolved
        {
            get { return m_character; }
        }

        internal PVPDamage(Character theCharacter)
        {
            m_character = theCharacter;
        }
        internal void Update(double currentTime, Character owner)
        {
            double minTime = currentTime - 30;
            //check the oldest damage time
            bool allDamageRemoved = false;
            //remove a max of 100 to prevent an infinate loop
            int i = 0;
            while (allDamageRemoved == false && m_damageList.Count > 0 && i < 100)
            {
                //if it's 
                PVPDamageStub currentDamage = m_damageList[0];
                if (currentDamage.TimeStamp < minTime)
                {
                    m_damageList.RemoveAt(0);
                }
                else
                {
                    allDamageRemoved = true;
                }
                i++;
            }
        }
        internal bool IsStillValid(Character owner)
        {
            return (m_character.Destroyed == false && m_character.Dead == false && m_character.CurrentZone == owner.CurrentZone && m_character.IsEnemyOf(owner) && m_damageList.Count > 0);
        }
        internal int CalculateTotalDamage()
        {
            int totalDamage = 0;

            for (int i = 0; i < m_damageList.Count; i++)
            {
                totalDamage += m_damageList[i].Damage;
            }
            m_totalDamage = totalDamage;

            return totalDamage;
        }
        internal void AddDamage(int damage, double currentTime)
        {
            PVPDamageStub newDamage = new PVPDamageStub(damage, currentTime);
            m_damageList.Add(newDamage);
        }

    }
    class PVPDamageStub
    {
        int m_damageAmount = 0;
        double m_timeStamp = 0;

        internal PVPDamageStub(int damage, double timeStamp)
        {
            m_damageAmount = damage;
            m_timeStamp = timeStamp;
        }
        internal int Damage
        {
            get { return m_damageAmount; }
        }
        internal double TimeStamp
        {
            get { return m_timeStamp; }
        }
    }
    class PVPKillRecord
    {
        internal int m_victim_character_id;
        internal int m_num_kills;
        internal DateTime m_killDate;
    }

    class CharacterAppearance
    {
        public int m_face_id=-1;
        public int m_skin_id=-1;
        public int m_skin_colour=-1;
        public int m_hair_id=-1;
        public int m_hair_colour=-1;
        public int m_face_acc_id=-1;
        public int m_face_acc_colour=-1;
    };

#endregion

    internal class Character : CombatEntity, IEntitySocialStanding,ITargetOwner
	{
		#region constants & enums

        // #localisation
		public class CharacterTextDB : TextEnumDB
		{
			public CharacterTextDB() : base(nameof(Character), typeof(TextID)) {  }

			public enum TextID
			{
				FAST_TRAVEL_INCREASED,			// "Your fast travel limit has been increased by {quantity0}"
				ENERGY_REGEN_INCREASED,			// "Your energy regeneration has been increased by {quantity0}"
				HEALTH_REGEN_INCREASED,			// "Your health regeneration has been increased by {quantity0}"
				EXTRA_SLOT_INCREASED,			// "Your number of Extra Slots has increased by {quantity0}"
				BANK_SLOT_INCREASED,			// "The number of slots on your bank has increased by {quantity0}"
				AUCTION_NUMBER_INCREASED,		// "The number of auctions you can create has increased by {quantity0}"
				FRIEND_LOGGED_OUT,				// "friend {name0} logged out"
				FRIEND_LOGGED_IN,				// "friend {name0} logged in"
				BLOCKED,						// "{name0} Blocked"
				SKILL_REMOVED,					// "Skill Removed : {skillName0}"
				SKILLS_REMOVED,					// "Skills Removed : {skillNames0}"
				SKILL_ADDED,					// "Skill Added : {skillName0}"
				SKILLS_ADDED,					// "Skills Added : {skillNames0}"
				TOO_LOW_LV_PVP_EXP,				// "{name0} is too low level to give PVP experience."
				GAINED_PVP_EXP,					// "Gained {exp0} PVP Experience"
				CANNOT_FAST_TRAVEL,				// "You cannot fast-travel in this area"
				CANNOT_USE_ITEM_DUEL,			// "You cannot use items while in a duel."
				CANNOT_USE_ITEM_AREA,			// "You cannot use items in this area"
				DUEL,							// "Duel"
				GROUP_PVP,						// "Group PVP"
				CLAN_PVP,						// "Clan PVP"
				FREE_FOR_ALL_PVP,				// "Free For All PVP"
				DUELING,						// "Duelling"
				HAS_DIED,						// "{name0} has died."
				REACHED_LEVEL,					// "{name0} reached level {level1}"
				HAVE_BEEN_DISMOUNTED,			// "You have been [ff0000]Dismounted[-], use a mount whistle or visit Bowen the Horsemaster to regain your mount."
				CHAR_BUSY,						// "This character is currently busy."
				ILLEGAL_TRADE,					// "Illegal Trade"
				NO_TRADE,						// "No Trade"
				ITEM_BOUND,						// "Item is Bound"
				TOO_MANY_ITEMS,					// "Too Many Items"
				NOT_ALLOWED,					// "not allowed"
				ABILITY_NOT_EXIST,				// "ability doesn't exist"
				CANNOT_AFFORD_ABILITY,			// "can't afford ability"
				ALREADY_KNOWN,					// "already known"
				FISHING_CANCELLED,				// "Fishing Cancelled"
				CONT_ZERO_FISHING,				// "Concentration at zero...your fish got away"
				NEED_TO_REST_FISHING,           // "You need to rest a moment before you can fish again."
				GAINED_ITEM,					// "Gained item: {lootList0}"
			}
		}
		public static new CharacterTextDB textDB = new CharacterTextDB();

		internal string GetPlayerBusyString()
		{
			return Localiser.GetString(textDB, m_player, (int)CharacterTextDB.TextID.CHAR_BUSY);
		}

		internal enum Respawn_Type
        {
            normal = 0,
            ResIdol = 20,
            ResSpell = 21
        };
        internal enum Player_Help_Type
        {
            None=0,
            Position =1,
            Mob=2,
            MobTemplateID=3,
            PickupItemTemplateID = 4,
        }
        internal enum Tutorial_Message_type
        {
            StartTutorial = 1,
            EndTutorial = 2,
            TutorialComplete = 3,

        }

        public static float m_NewCharPosX = 0;
        public static float m_NewCharPosY = 0;
        public static float m_NewCharPosZ = 0;
        public static float m_NewCharStartAngle = 0;
        public static float m_pvpMeleeMult = 1.0f;
        public static int m_NewCharZone = 0;
        public static string m_NewCharTeleportList = "";
        
        
        internal static float MAX_EVENT_LIFE = 3600;
        public const int DEATH_TIMER_ID = 20006; 
        const int START_DEATH_STATUS_ID = 20002;
        const int DEATH_STATUS_ID = 20001;
        public const int DISMOUNTED_STATUS_ID = 20007;
        const int MIN_LVL_DEATH_PENALTY = 10;
        const int MAX_EXTRA_HUD_SLOTS = 72;
        const int MAX_EXTRA_AH_SLOTS = 25;
        const float DEFAULT_ATTACK_REPORT_TIME = 0.6f;
        const int NUMBER_OF_BONUS_TYPES_PASSED_DOWN = 7;
        internal const int FORWARD_PROJECTION_LENGTH = 5;
        internal const int SENT_FORWARD_PROJECTION_LENGTH = 0;
        const int MAX_ENCUMBRANCE = 250;
        const int MIN_ENERGY_PERCENT_AFTER_ENCUMBRANCE = 10;
        const int NEW_CHARACTER_STARTING_GOLD = 0;
        internal const int characterFactionID = -1;
        internal const int POSITION_SEND_DIST = 65;
        internal const int SQUARED_POSITION_SEND_DIST = 4225;
        internal const int SQUARED_BATTLE_UPDATE_SEND_DIST = 900;
        
		/// <summary>
        /// what fraction of the normal battle Data Send Distance the character must move before getting a new update
        /// </summary>
        const float NORMAL_SEND_DIST_BATTLE_UPDATE_MOD = 0.25f;
        const float DIST_BETWEEN_MOB_UPDATES = 900;
        const int BASE_HUD_SLOTS = 8;
        
        enum SOCIAL_LIST_TYPE
        {
            FRIENDS_LIST = 1,
            BLOCK_LIST = 2
        };

		#endregion

		#region variables
		protected CombatEntity.STATS_CHANGE_LEVEL m_statsChangedLevel = STATS_CHANGE_LEVEL.NO_CHANGE;
        protected PlayerSignpostManager m_signpostManager = new PlayerSignpostManager();
        public static double EXPERIENCE_ACCELLERATOR = 0.75f;

        List<int> m_completedTutorialList = new List<int>();
        List<int> m_completedFirstTimeList = new List<int>();

        public uint m_character_id;
        public int m_account_id;
        public string m_name;
        public RaceTemplate m_race;
        public CharacterAppearance m_basicAppearance = new CharacterAppearance();
        public int m_face_id;
        public int m_skin_id;
        public int m_skin_colour;
        public int m_hair_id;
        public int m_hair_colour;
        public int m_face_acc_id;
        public int m_face_acc_colour;
        public Player m_player { get; private set; }
        public bool m_batchPositionRequired = false;
        public List<PVPKillRecord> m_pvpKills = new List<PVPKillRecord>();

        List<PVPDamage> m_pvpDamages = new List<PVPDamage>();
        CharacterPath m_theCharactersPath = new CharacterPath();
        List<CombatEntity> m_lockedList = new List<CombatEntity>();

        public Zone m_zone;
        public Int64 m_experience;
        public Int64 m_fishingExperience;
       

        public Int64 CookingExperience { get; set; }
        //public int CookingLevel { get; set; }

        // AUCTION HOUSE
        public int m_numberOfExtraAHSlots;


        // DAILY REWARDS 
        public DateTime m_lastRewardRecieved;
        public int m_nextDailyRewardStep;
        public int m_numRecievedRewards;

        //JT STATS CHANGES 12_2011
        public int m_baseVitality;
        public int m_baseDexterity;
        public int m_baseFocus;
        public int m_baseStrength;
        /*public int m_vitality;
        public int m_dexterity;
        public int m_focus;
        public int m_strength;*/
        public ActorPosition m_CharacterPosition;
        public ActorPosition m_ConfirmedPosition;
        public ActorPosition m_ProjectedPosition;

        public bool PlayerIsBusy;
        public ActorPosition m_SentPosition;
        public Inventory m_inventory;
        public Inventory m_SoloBank;
        //a link to the main database
        public Database m_db;
        public Player m_tradingWith = null;
        PendingRequest m_pendingRequest = null;
        public Inventory m_tradingInventory;
        public bool m_tradeReady = false;
        bool m_tradeAccepted = false;
        public bool m_isTradeInitator = false;
        internal DateTime m_lastLoggedIn = DateTime.MinValue;
        public List<CharacterAbility> m_abilities;
        //PermanentBuff
        public List<PermanentBuff> m_permanentBuff = new List<PermanentBuff>();
        //public float m_scale;
        public ClassTemplate m_class;
        public GENDER m_gender;
        float m_timeTillRespawn = 10;
        int m_numberOfExtraHudSlots = 0;
        bool m_hateListChanged = false;
        public RankingsManager m_characterRankings;
        public AchievementsManager m_characterAchievements;
        Vector3 m_lastCombatSendPos = new Vector3(0);
        Vector3 m_lastMobUpdatePos = new Vector3(0);
        Vector3 m_lastDeadSendPos = new Vector3(-99999);
        bool m_showHeadgear = true;
        bool m_showFashion = false;
        public int m_pvpLevel;
        public double m_pvpRating;
        public bool m_statsUpdated = false;
        public double m_timeCharacterLoggedIn = 0;
        Mailbox m_characterMail = new Mailbox();
        CharacterBountyManager mCharacterBountyManager = new CharacterBountyManager();
        
        /// <summary>
        /// A list of Skills received from current equipment/status
        /// </summary>
        protected List<StatusSkill> m_AdditionalSkill = new List<StatusSkill>();
        public List<Character> m_nearbyPlayers = new List<Character>();
        public List<Character> m_PlayersToUpdate = new List<Character>();
        internal string m_reasonForExit = "Unknown";

        //public List<int> m_nearbyMobs = new List<int>();
        //public List<int> m_oldNearbyMobs = new List<int>();
        /// <summary>
        /// The clan this character belongs to;
        /// </summary>
        Clan m_charactersClan = null;
        Party m_party = null;
        List<FriendTemplate> m_blockList = new List<FriendTemplate>();
        /// <summary>
        /// A list of friends character ids 
        /// </summary>
        List<FriendTemplate> m_friendCharacterIDs = new List<FriendTemplate>();

        /// <summary>
        /// A List of character ID's that this character can be attacked by
        /// </summary>
        List<CombatEntity> m_hateList = new List<CombatEntity>();
        const int MAX_HUD_SLOT_ITEMS = 16;
        const int MAX_HUD_SLOT_ITEM_ONLY = 2;

        /// <summary>
        /// A list of the characters current item/skill bar on their hud
        /// </summary>
        /// 

        //HudSlotItem[] m_HudSlotItems;
        CharacterSlotSetHolder m_slotSetHolder = null;
        /// <summary>
        /// A List of all the factions this character has encountered and their standing with the faction
        /// </summary>
        List<FactionStanding> m_factionStandings;
        /// <summary>
        /// A List of all the spawn points Discovered by the character
        /// Used when respawning and teleporting
        /// </summary>
        List<int> m_discoveredSpawnPoints;

        /// <summary>
        /// A List of all the undiscovered Teleport Points in the current Zone
        /// </summary>
        List<PlayerSpawnPoint> m_undiscoveredPoints;

        public QuestManager m_QuestManager;

        List<AggroData> m_assistList = new List<AggroData>();

        DuelTarget m_currentDuelTarget = null;

        List<CombatEntity> m_newToInterestList = new List<CombatEntity>();
        List<CombatEntity> m_changedStatusEffectList = new List<CombatEntity>();
        List<CombatEntity> m_entitiesToForgetList = new List<CombatEntity>();
        List<CombatEntity> m_entitiesInCombatChanged = new List<CombatEntity>();
        //public DateTime m_lastResIdol;
        public DateTime m_lastItemUse;
        public int m_lastItemIdUsed;
        /// <summary>
        /// A list Of Events that have happened to the character within the last
        /// </summary>
        List<CharacterEvent> m_recentEvents = new List<CharacterEvent>();
        double m_timeAtLastClanEvent = 0;
        CharacterSpecialOfferManager m_offerManager = null;
        bool m_hasUsedSkill = false;

        public double timeInCombat = 1.0;
        public int    damageDone   = 0;
        public int    healingDone  = 0;

        #endregion //variables

        #region Properties -  getters & setters

        public CharacterBountyManager CharacterBountyManager
        {
            get { return mCharacterBountyManager; }
        }        

        internal List<CharacterEvent> RecentEvents
        {
            get { return m_recentEvents; }
        }
        internal List<FriendTemplate> BlockList
        {
            get { return m_blockList; }
        }
        
        public Mailbox CharacterMail
        {
            get { return m_characterMail; }
        }
        override internal Zone CurrentZone
        {
            get
            {
                return m_zone;
            }
        }

        internal int Vitality
        {
            get { return (int)m_compiledStats.Vitality; }
        }
        internal int Focus
        {
            get { return (int)m_compiledStats.Focus; }
        }
        internal int Strength
        {
            get { return (int)m_compiledStats.Strength; }
        }
        internal int Dexterity
        {
            get { return (int)m_compiledStats.Dexterity; }
        }
        internal bool ShowHeadgear
        {
            get { return m_showHeadgear; }
            set
            {
                if (m_showHeadgear != value)
                {
                    m_showHeadgear = value;
                    InfoUpdated(Inventory.EQUIP_SLOT.SLOT_SHOW_HEADGEAR);
                }

            }
        }
        internal bool ShowFashion
        {
            get { return m_showFashion; }
            set
            {
                if (m_showFashion != value)
                {
                    m_showFashion = value;
                    InfoUpdated(Inventory.EQUIP_SLOT.SLOT_SHOW_FASHION);
                }

            }
        }
        internal bool TradeAccepted
        {
            get { return m_tradeAccepted; }
            set { m_tradeAccepted = value; }
        }
        /// <summary>
        /// if the character is currently in a duel this will have information on who they are duelling with
        /// </summary>
        internal DuelTarget CurrentDuelTarget
        {
            get { return m_currentDuelTarget; }
            set { m_currentDuelTarget = value; }
        }
        /// <summary>
        /// The clan this character belongs to;
        /// </summary>
        internal Clan CharactersClan
        {
            get { return m_charactersClan; }
            set
            {
                InfoUpdated(Inventory.EQUIP_SLOT.SLOT_CLAN_NAME);
                m_charactersClan = value;
            }
        }
        internal Party CharacterParty
        {
            set { m_party = value; }
            get { return m_party; }
        }
        /// <summary>
        /// A List of all the spawn points Discovered by the character
        /// Used when respawning and teleporting
        /// </summary>
        internal List<int> DiscoveredSpawnPoints
        {
            get { return m_discoveredSpawnPoints; }
        }
        /// <summary>
        /// A List of all the undiscovered Teleport Points in the current Zone
        /// </summary>
        internal List<PlayerSpawnPoint> UndiscoveredPoints
        {
            get { return m_undiscoveredPoints; }
        }

        /// <summary>
        /// How many Items the character can carry before travel stones are needed
        /// </summary>

        internal override CombatManager TheCombatManager
        {
            get
            {
                if ((m_zone != null) && (m_InLimbo == false))
                {
                    return m_zone.m_combatManager;
                }
                return null;
            }
        }
        
        internal override string Name
        {
            get { return m_name; }
        }
        internal override float Scale
        {
            get { return m_compiledStats.Scale; }//m_scale * m_scaleEffectModifier; }
            set { m_compiledStats.Scale = value; }
        }


        /// <summary>
        /// Raw scale value from database, unmodified by compilation.
        /// </summary>
        internal float UnmodifiedScale
        {
            get; private set; 
        }

        internal PendingRequest CurrentRequest
        {
            get { return m_pendingRequest; }
            set { m_pendingRequest = value; }
        }

        internal CharacterPath TheCharactersPath
        {
            get { return m_theCharactersPath; }
        }
        internal double TimeAtLastClanEvent
        {
            set { m_timeAtLastClanEvent = value; }
            get { return m_timeAtLastClanEvent; }
        }

        internal CharacterSpecialOfferManager OfferManager
        {
            get { return m_offerManager; }
        }

        internal bool HasUsedSkill
        {
            get { return m_hasUsedSkill; }
        }

		internal STATS_CHANGE_LEVEL GetStatsLevel()
		{
			return m_statsChangedLevel;
		}

        /// <summary>
        /// Manager for recording and altering changes to a character faction standing
        /// </summary>
        internal FactionManager FactionManager { get; set; }

        internal CraftingManager CraftingManager { get; set; }

        #endregion //Properties

        #region initialisation

        public enum ProfessionType { Cooking };

		public Character(Database db, Player player)
        {
            m_player = player;
            m_db = db;
            m_account_id = (int)player.m_account_id;
            SetUp();
        }

        void SetUp()
        {
            m_timeCharacterLoggedIn = NetTime.Now;
            m_CharacterPosition = new ActorPosition();
            
            m_ConfirmedPosition = new ActorPosition();
            m_ProjectedPosition = new ActorPosition();
            m_SentPosition = new ActorPosition();
            m_inventory = new Inventory(this, Inventory_Types.BACKPACK);
            m_SoloBank = new Inventory(this, Inventory_Types.SOLO_BANK);
            m_CharacterPosition.m_currentSpeed = 0;

            m_tradingInventory = new Inventory(this, Inventory_Types.TRADE);
            m_abilities = new List<CharacterAbility>();
            /*m_HudSlotItems = new HudSlotItem[MAX_HUD_SLOT_ITEMS];
            for (int i = 0; i < MAX_HUD_SLOT_ITEMS; i++)
            {
                m_HudSlotItems[i] = new HudSlotItem(HudSlotItemType.HUD_SLOT_ITEM_EMPTY, -1);
            }*/
            setDefaultStats();
            m_factionStandings = new List<FactionStanding>();
            m_discoveredSpawnPoints = new List<int>();
            m_undiscoveredPoints = new List<PlayerSpawnPoint>();
            //add the base character Faction
            //MaxSpeed = 6.0f;
            MaxSpeed = 4.2f;
            m_compiledStats.m_Character = this;
            //m_lastResIdol = DateTime.Now - TimeSpan.FromSeconds(ItemTemplateManager.RES_IDOL_RECHARGE_TIME);
            m_lastItemUse = DateTime.Now - TimeSpan.FromSeconds(ItemTemplateManager.ITEM_RECHARGE_TIME);
            m_defaultInterestTypes = partitioning.ZonePartition.ENTITY_TYPE.ET_ENEMY | partitioning.ZonePartition.ENTITY_TYPE.ET_NOT_ENEMY|partitioning.ZonePartition.ENTITY_TYPE.ET_PLAYER|partitioning.ZonePartition.ENTITY_TYPE.ET_MOB;

            if (SpecialOfferTemplate.SPECIAL_OFFERS_ACTIVE == true)
            {
                m_offerManager = new CharacterSpecialOfferManager();
               // m_offerManager.LoadIndividualOffers(Program.processor.m_universalHubDB, this);
            }
        }

        public Character(Database db, Player player, int account_id, string name, int race_id, int face_id, int skin_id, int skincol, int hair_id, int hair_col, int face_acc, int face_acc_col, float scale, int class_id, GENDER gender)
        {
            SetUp();
            m_db = db;

            m_player = player;
            m_QuestManager = new QuestManager(db, this,m_character_id);


            // m_inventory = new Inventory(this);

            setDefaultStats();
            m_zone = Program.processor.getZone(m_NewCharZone);
            Level= 1;
            m_experience = 0;
            m_fishingExperience = 0;
            m_inventory.m_coins = NEW_CHARACTER_STARTING_GOLD;
            m_account_id = account_id;

            m_name = name;
            m_race = RaceTemplateManager.getRaceTemplate((RACE_TYPE)race_id);
            m_face_id = face_id;
            m_skin_id = skin_id;
            m_skin_colour = skincol;
            m_hair_id = hair_id;
            m_hair_colour = hair_col;
            m_face_acc_id = face_acc;
            m_face_acc_colour = face_acc_col;
            //m_scale = scale;
            m_baseStats.Scale = scale;

            CookingExperience = 0;
            LevelCooking = 0;

            m_class = ClassTemplateManager.getClassTemplate((CLASS_TYPE)class_id);
            m_gender = gender;
            InLimbo = true;
            CurrentHealth = (int)(m_race.m_starting_vitality * m_race.m_vitality_modifier * 5.0f + 0.5f);
            CurrentConcentrationFishing = (int) (6.25*(LevelFishing*3) + 7);
            
            CurrentEnergy = (int)(m_race.m_starting_focus * m_race.m_focus_modifier * 5.0f + 0.5f);
            //JT STATS CHANGES 12_2011
            /*m_baseStats.Vitality = m_race.m_starting_vitality;
            m_baseStats.Focus = m_race.m_starting_focus;
            m_baseStats.Strength = m_race.m_starting_strength;
            m_baseStats.Dexterity = m_race.m_starting_dexterity;*/
            m_baseVitality = m_race.m_starting_vitality;
            m_baseFocus = m_race.m_starting_focus;
            m_baseStrength = m_race.m_starting_strength;
            m_baseDexterity = m_race.m_starting_dexterity;
            UpdateBaseStats();
            DateTime createTime = DateTime.Now;
			//create a default character

			/*string createCharString = String.Format(@"insert into character_details 
                                                    (account_id, name, race_id, face_id, skin,
                                                     skin_colour, hair_id, hair_colour, face_accessory_id, face_accessory_colour,
                                                     experience, fishing_experience, zone, level, vitality,
                                                     focus, strength, dexterity, coins, current_health,
                                                     current_energy, model_scale, class_id, gender, xpos,
                                                     ypos, zpos, yangle, teleport_locations, xp,
                                                     created_date, cooking_experience, cooking_level, perm_buffs)
                                                    values
                                                    ({0}, '{1}', {2}, {3}, {4},
                                                     {5}, {6}, {7}, {8}, {9},
                                                     {10}, {11}, {12}, {13}, {14},
                                                     {15}, {16}, {17}, {18}, {19},
                                                     {20}, {21}, {22}, {23}, {24},
                                                     {25}, {26}, {27}, '{28}', {29},
                                                     '{30}', {31}, {32}, '{33}')",
                                                    m_account_id, m_name, race_id, face_id, skin_id,
                                                    m_skin_colour, m_hair_id, m_hair_colour, m_face_acc_id, m_face_acc_colour,
                                                    m_experience, m_fishingExperience, m_zone.m_zone_id, Level, (int)m_baseVitality, 
                                                    (int)m_baseFocus, (int)m_baseStrength, (int)m_baseDexterity, m_inventory.m_coins, CurrentHealth,
                                                    CurrentEnergy, scale, class_id, (int)gender, m_NewCharPosX, 
                                                    m_NewCharPosY, m_NewCharPosZ, m_NewCharStartAngle, m_NewCharTeleportList, 0,
                                                    createTime.ToString("yyyy-MM-dd HH:mm:ss"), CookingExperience, LevelCooking, "13,2");

            db.runCommand(createCharString, true);

            SqlQuery query = new SqlQuery(m_db, "select character_id from character_details where account_id=" + account_id + " and name='" + m_name + "' and deleted=0");*/

			// instead of adding all 34 parameters, just add one that really needed
			List<MySqlParameter> sqlParams = new List<MySqlParameter>();
			sqlParams.Add(new MySqlParameter("@name", m_name));

			string createCharString = String.Format(@"insert into character_details 
                                                    (account_id, name, race_id, face_id, skin,
                                                     skin_colour, hair_id, hair_colour, face_accessory_id, face_accessory_colour,
                                                     experience, fishing_experience, zone, level, vitality,
                                                     focus, strength, dexterity, coins, current_health,
                                                     current_energy, model_scale, class_id, gender, xpos,
                                                     ypos, zpos, yangle, teleport_locations, xp,
                                                     created_date, cooking_experience, cooking_level, perm_buffs)
                                                    values
                                                    ({0}, {1}, {2}, {3}, {4},
                                                     {5}, {6}, {7}, {8}, {9},
                                                     {10}, {11}, {12}, {13}, {14},
                                                     {15}, {16}, {17}, {18}, {19},
                                                     {20}, {21}, {22}, {23}, {24},
                                                     {25}, {26}, {27}, '{28}', {29},
                                                     '{30}', {31}, {32}, '{33}')",
													m_account_id, "@name", race_id, face_id, skin_id,
													m_skin_colour, m_hair_id, m_hair_colour, m_face_acc_id, m_face_acc_colour,
													m_experience, m_fishingExperience, m_zone.m_zone_id, Level, (int)m_baseVitality,
													(int)m_baseFocus, (int)m_baseStrength, (int)m_baseDexterity, m_inventory.m_coins, CurrentHealth,
													CurrentEnergy, scale, class_id, (int)gender, m_NewCharPosX,
													m_NewCharPosY, m_NewCharPosZ, m_NewCharStartAngle, m_NewCharTeleportList, 0,
													createTime.ToString("yyyy-MM-dd HH:mm:ss"), CookingExperience, LevelCooking, "13,2");

			db.runCommandWithParams(createCharString, sqlParams.ToArray(), true);

			// using the same parameter, but just clear and adding new in case that above statement has changed
			// try to follow the pattern that dosen't mixed up sql pamaters with instance strings
			sqlParams.Clear();
			sqlParams.Add(new MySqlParameter("@account_id", account_id));
			sqlParams.Add(new MySqlParameter("@name", m_name));

			SqlQuery query = new SqlQuery(m_db, "select character_id from character_details where account_id=@account_id and name=@name and deleted=0", sqlParams.ToArray());

			if (query.HasRows)
            {
                query.Read();
                m_character_id = query.GetUInt32("character_id");
            }

            query.Close();
            ServerID = (int)m_character_id;
            Type = EntityType.Player;
            m_CharacterPosition.m_position = new Vector3(m_NewCharPosX, m_NewCharPosY, m_NewCharPosZ);

            m_CharacterPosition.m_yangle = m_NewCharStartAngle;


            CurrentPosition = m_CharacterPosition;

            //now the character exists, give them some starting items
            string[] equipment_list = m_class.m_starting_equipment.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < equipment_list.Length; i++)
            {
                int templateID = Int32.Parse(equipment_list[i]);
                Item newItem = m_inventory.AddNewItemToCharacterInventory(templateID, 1, true);
                if (newItem != null)
                {
                    m_inventory.EquipItem(templateID, newItem.m_inventory_id, 1, newItem.m_template.m_slotNumber);
                }
            }

            string[] ability_split = m_class.m_ability_list.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < ability_split.Length; i++)
            {
                m_abilities.Add(new CharacterAbility((ABILITY_TYPE)Int32.Parse(ability_split[i]), 0));
            }
            writeAbilitiesToDB();



            string[] skill_split = m_class.m_starting_Skills.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            //create the standard empty hud
            m_slotSetHolder = new CharacterSlotSetHolder(m_character_id, m_db, BASE_HUD_SLOTS);
            //get the first set
            CharacterSlotSet slotSet = m_slotSetHolder.GetSetWithIndex(0);
            //set some slots in there
            for (int i = 0; i < skill_split.Length; i++) 
            {
                int skillID = Int32.Parse(skill_split[i]);
                AddSkill((SKILL_TYPE)skillID,false,false);
                if (slotSet != null)
                {

                    slotSet.SetSlot(i, HudSlotItemType.HUD_SLOT_ITEM_SKILL, skillID);
                }
                //m_HudSlotItems[i + MAX_HUD_SLOT_ITEM_ONLY] = new HudSlotItem(HudSlotItemType.HUD_SLOT_ITEM_SKILL, skillID);
                //m_abilities.Add(new CharacterAbility((ABILITY_TYPE)Int32.Parse(ability_split[i]), 0));
            }

            if (slotSet != null && slotSet.Changed == true)
            {
                slotSet.SaveToDatabase(m_db);
            }
            //SaveSkillHudToDatabase();
            m_characterRankings = new RankingsManager(db, RankingsManager.RANKING_MANAGER_TYPE.CHARACTER_RANKINGS, "character_rankings", "character_id", (int)m_character_id);
            m_characterAchievements = new AchievementsManager(db, AchievementsManager.ACHIEVEMENT_MANAGER_TYPE.CHARACTER_ACHIEVEMENTS, "character_achievements", "character_id", (int)m_character_id);

            // create our characters faction manager, it will
            // handle everything related to factions         
            FactionManager = new FactionManager(this.m_db, this);

            CraftingManager = new CraftingManager(this.m_db, this);


        }
        
		#endregion
        
        public void Update(double timeSinceLastUpdate)
        {

            //dont update if we're in limbo..whatever that means
            if (m_InLimbo)
                return;

            double currentZoneTime = Program.MainUpdateLoopStartTime();
            double netTime = NetTime.Now;
            //update recentEvents - if the oldest should be removed then remove it
            if (m_recentEvents.Count > 0)
            {
                CharacterEvent oldestEvent = m_recentEvents[0];
                if (oldestEvent.ActionedNetTime + MAX_EVENT_LIFE < netTime)
                {
                    m_recentEvents.RemoveAt(0);

                    if (oldestEvent.Type == CharacterEvent.EventType.CharacterDied)
                    {
                        m_signpostManager.CharacterDied(this);
                    }
                }
            }



            if (m_updatedInfo.Count > 0)
            {
                //    Program.Display("sending appearance update for " + m_name);
                Program.processor.SendPlayerAppearanceUpdate(m_player);
                SendAppearanceUpdateToSelf();
                m_updatedInfo.Clear();
            }

            if (m_newToInterestList.Count > 0)
            {
                SendNewEntitiesOfInterestMessage();
                m_newToInterestList.Clear();
            }
            if (m_entitiesInCombatChanged.Count > 0)
            {
                SendEntitiesChangedCombatStatus();
                m_entitiesInCombatChanged.Clear();
            }

            //if a duel is in effect check the details
            //do this before the hate list is sent as the duel may have just started
            if (m_currentDuelTarget != null)
            {
                m_currentDuelTarget.Update(currentZoneTime, this);
                ConductedHotileAction();
            }
            //if the pvp list has changed resend it
            if (m_pvpTypesChanged == true)
            {
                CheckPVPTypes();
                m_pvpTypesChanged = false;
            }
            if (m_pvpTargetsNeedChecked == true)
            {
                CalculateHateLists();
                m_pvpTargetsNeedChecked = false;
            }
            if (m_hateListChanged == true)
            {
                SendHateList();
            }
            //if there are any quests quest to be deleted then clear them now
            m_QuestManager.DeletePendingQuests();

            TheCharactersPath.Update(currentZoneTime);
            CharacterMail.Update(DateTime.Now);


            if (Program.m_signpostingOn == true)
            {
                m_signpostManager.Update(this);
            }
            if (SpecialOfferTemplate.SPECIAL_OFFERS_ACTIVE == true)
            {
                m_offerManager.Update(this);
            }
            //if there is a pending request, check it
            if (CurrentRequest != null)
            {
                if (CurrentRequest.shouldBeDestroyed(currentZoneTime) == true)
                {
                    CurrentRequest.CancelRequest(m_player, PendingRequest.CANCEL_CONDITION.CC_TIME_OUT);
                }
            }
            //if the character has moved enough then send a combat update
            double distSinceLastCombatUpdate = Utilities.Difference2DSquared(CurrentPosition.m_position, m_lastCombatSendPos);
            double distSinceLastMobBatchUpdate = Utilities.Difference2DSquared(CurrentPosition.m_position, m_lastMobUpdatePos);
            if (distSinceLastCombatUpdate > (SQUARED_BATTLE_UPDATE_SEND_DIST * NORMAL_SEND_DIST_BATTLE_UPDATE_MOD))
            {
                if (m_zone != null)
                {
                    m_zone.m_combatManager.SendBattleUpdateMessage(this);
                    //m_zone.sendDumbMobPatrolUpdate(m_player);
                }
            }
            if (distSinceLastMobBatchUpdate > (DIST_BETWEEN_MOB_UPDATES))
            {
                if (m_zone != null)
                {

                    m_zone.sendDumbMobPatrolUpdate(m_player);
                    //  m_zone.SendPositionUpdatesDisreguardingDistance(Program.Server, m_player);
                }
                m_lastMobUpdatePos = CurrentPosition.m_position;
            }
            SendOtherEntitiesStatusEffects(currentZoneTime);
            UpdatePVPDamage(currentZoneTime);
            if (CurrentHealth <= 0 && !Dead)
            {
                //cancel any skills
                if (CurrentSkill != null)
                {
                    SendSkillUpdate((int)CurrentSkill.SkillID, CurrentSkill.SkillLevel, 0);
                    CurrentSkill = null;
                    CurrentSkillTarget = null;
                }
                if (NextSkill != null)
                {
                    SendSkillUpdate((int)NextSkill.SkillID, NextSkill.SkillLevel, 0);
                    NextSkill = null;
                    NextSkillTarget = null;
                }
                Died();
                m_timeTillRespawn = 10;
                SetStatsChangeLevel(STATS_CHANGE_LEVEL.COMPILE_REQUIRED);//m_statsUpdated=true;
                return;
            }

            //if we've run out of concentration - need to cancel current attack
            if (CurrentConcentrationFishing <= 0 && !ConcentrationFishDepleted && InCombat)
            {
                ConcentrationZero();
                WriteMessageForConcentrationAtZero();
            }

            if (Dead)
            {
                m_timeTillRespawn -= (float)timeSinceLastUpdate;
                if (m_timeTillRespawn <= 0 && m_zone != null && m_player != null)
                {
                    m_zone.SendResetDeathVariable(m_player);
                    m_timeTillRespawn = 10;
                }
            }


            //check m_hasMoved before it is reset by base update
            if (Program.m_signpostingOn == true && m_hasMoved == true)
            {
                m_signpostManager.DidMove(this);
            }
            if (base.Update(m_CharacterPosition))
            {

                //m_statsUpdated=true;
            }
            float totalSpeed = m_CharacterPosition.m_currentSpeed;



            if (totalSpeed > 0)
            {
                //if not in combat assume you continue going this way
                Vector3 newPosition = m_CharacterPosition.m_position + m_CharacterPosition.m_direction * (float)timeSinceLastUpdate * totalSpeed;
                if (newPosition.X < CurrentZone.m_zoneRect.Left)
                {
                    newPosition.X = CurrentZone.m_zoneRect.Left + 0.1f;
                    m_CharacterPosition.m_currentSpeed = 0;
                }
                if (newPosition.X > CurrentZone.m_zoneRect.Right)
                {
                    newPosition.X = CurrentZone.m_zoneRect.Right - 0.1f;
                    m_CharacterPosition.m_currentSpeed = 0;
                }
                if (newPosition.Z < CurrentZone.m_zoneRect.Top)
                {
                    newPosition.Z = CurrentZone.m_zoneRect.Top + 0.1f;
                    m_CharacterPosition.m_currentSpeed = 0;
                }
                if (newPosition.Z > CurrentZone.m_zoneRect.Bottom)
                {
                    newPosition.Z = CurrentZone.m_zoneRect.Bottom - 0.1f;
                    m_CharacterPosition.m_currentSpeed = 0;
                }

                if (Program.processor.m_playerDisconnecter.PlayerIsBackgrounded(m_player) == false)
                {
                    m_CharacterPosition.m_position = newPosition;
                }

                if (m_CharacterPosition.m_currentSpeed > 0)
                {
                    m_SentPosition.m_position = m_CharacterPosition.m_position + m_CharacterPosition.m_direction * totalSpeed * SENT_FORWARD_PROJECTION_LENGTH;
                    m_ProjectedPosition.m_position = m_CharacterPosition.m_position + m_CharacterPosition.m_direction * totalSpeed * FORWARD_PROJECTION_LENGTH;
                }
                else
                {
                    m_SentPosition.m_position = m_CharacterPosition.m_position;
                    m_ProjectedPosition.m_position = m_CharacterPosition.m_position;
                }
                EntityPartitionCheck();
            }


            if (m_player.m_AccountRankings != null)
            {
                m_player.m_AccountRankings.update(this);
            }
            if (m_player.m_AccountAchievements != null)
            {
                m_player.m_AccountAchievements.update(this);
            }

            CharacterEffectManager.DebugCombatStats(this);

            //flag that status will have changed - so calculate new values
            if (m_statsUpdated)
            {
                updateCombatStats(false);
                NetOutgoingMessage msg = CompiledStats.BuildMessage(m_oldCompiledStats, null, this);
                if (msg != null)
                {
                    Program.processor.SendMessage(msg, m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.StatsUpdate);
                    m_oldCompiledStats = CompiledStats.Copy();
                }
                m_statsUpdated = false;
                m_statsChangedLevel = STATS_CHANGE_LEVEL.NO_CHANGE;
            }

            CraftingManager.Update();

            
            
        }
        
		internal override void StopTheEntity()
        {

            m_CharacterPosition.m_currentSpeed = 0;
            base.StopTheEntity();
        }

        internal void SendDeadPlayersList(List<CombatEntity> deadCharacterList)
        {

            double distSinceLastDeadPlayerUpdate = Utilities.Difference2DSquared(CurrentPosition.m_position, m_lastDeadSendPos);
            if (distSinceLastDeadPlayerUpdate > (SQUARED_BATTLE_UPDATE_SEND_DIST * NORMAL_SEND_DIST_BATTLE_UPDATE_MOD))
            {
                if (m_zone != null && deadCharacterList.Count > 0)
                {
                    NetOutgoingMessage dmgMsg = Program.Server.CreateMessage();
                    dmgMsg.WriteVariableUInt32((uint)NetworkCommandType.CombatDamageMessage);

                    dmgMsg.WriteVariableInt32(0);
                    dmgMsg.WriteVariableInt32(0);

                    //this is a blank list
                    dmgMsg.WriteVariableInt32(0);
                    //nothing is cancelled
                    dmgMsg.WriteVariableInt32(0);
                    //send all the dead characters data
                    m_zone.WriteAllChararactersCombatDataInRange(deadCharacterList, m_player, dmgMsg);

                    Program.processor.SendMessage(dmgMsg, m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.CombatDamageMessage);

                    m_lastDeadSendPos = CurrentPosition.m_position;
                }
            }

        }
        
		void EquipAllItems()
        {
            //the number above which an ID is considered a piece of equipment
            for (int i = 0; i < m_inventory.m_equipedItems.Length; i++)
            {
                m_inventory.m_equipedItems[i] = null;
            }
            SqlQuery query = new SqlQuery(m_db, "select * from equipment where character_id=" + m_character_id);
            if (query.HasRows)
            {
                while (query.Read())
                {
                    int inventoryID = query.GetInt32("inventory_id");
                    int slot_ID = query.GetInt32("slot_id");
                    int didEquip = m_inventory.EquipItemNoDB(inventoryID, slot_ID);
                    if (didEquip < 0)
                    {
                        m_db.runCommandSync("delete from equipment where inventory_id=" + inventoryID);
                    }
                }
            }
            query.Close();
            updateCombatStats(true);

        }
        
		void CalculateAttackRange()
        {

            Item weapon = m_inventory.m_equipedItems[(int)Inventory.EQUIP_SLOT.SLOT_WEAPON];
            float currentRange = 0;
            if (weapon != null)
            {
                currentRange = weapon.m_template.m_attackRange;
            }
            Item offhand = m_inventory.m_equipedItems[(int)Inventory.EQUIP_SLOT.SLOT_OFFHAND];
            if (offhand != null)
            {
                if (offhand.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.ARROW)
                {
                    currentRange += offhand.m_template.m_attackRange;
                }
            }
            if (currentRange <= 0)
            {
                currentRange = ItemTemplate.DEFAULT_ATTACK_RANGE;
            }
            CompiledStats.MaxAttackRange = currentRange;
        }

        public void writeUpdateInfoToMessage(NetOutgoingMessage msg)
        {
            msg.WriteVariableInt32(m_updatedInfo.Count);
            for (int i = 0; i < m_updatedInfo.Count; i++)
            {
                msg.WriteVariableInt32((int)m_updatedInfo[i]);
                switch (m_updatedInfo[i])
                {
                    case Inventory.EQUIP_SLOT.SLOT_WEAPON:
                    case Inventory.EQUIP_SLOT.SLOT_HEAD:
                    case Inventory.EQUIP_SLOT.SLOT_CHEST:
                    case Inventory.EQUIP_SLOT.SLOT_LEG:
                    case Inventory.EQUIP_SLOT.SLOT_FEET:
                    case Inventory.EQUIP_SLOT.SLOT_OFFHAND:
                    case Inventory.EQUIP_SLOT.SLOT_HANDS:
                    case Inventory.EQUIP_SLOT.SLOT_RING_R1:
                    case Inventory.EQUIP_SLOT.SLOT_RING_R2:
                    case Inventory.EQUIP_SLOT.SLOT_RING_L1:
                    case Inventory.EQUIP_SLOT.SLOT_RING_L2:
                    case Inventory.EQUIP_SLOT.SLOT_BANGLE_R:
                    case Inventory.EQUIP_SLOT.SLOT_BANGLE_L:
                    case Inventory.EQUIP_SLOT.SLOT_NECK:
                    case Inventory.EQUIP_SLOT.SLOT_FASH_HEAD:
                    case Inventory.EQUIP_SLOT.SLOT_FASH_TORSO:
                    case Inventory.EQUIP_SLOT.SLOT_FASH_LEGS:
                    case Inventory.EQUIP_SLOT.SLOT_FASH_FEET:
                    case Inventory.EQUIP_SLOT.SLOT_FASH_HANDS:
					case Inventory.EQUIP_SLOT.SLOT_COMPANION:
                    case Inventory.EQUIP_SLOT.SLOT_SADDLE:
                    case Inventory.EQUIP_SLOT.SLOT_MOUNT:
                    case Inventory.EQUIP_SLOT.SLOT_MISC:
                        {
                            Item item = m_inventory.m_equipedItems[(int)m_updatedInfo[i]];
                            if (item != null)
                            {
                                msg.WriteVariableInt32(m_inventory.m_equipedItems[(int)m_updatedInfo[i]].m_template_id);
                            }
                            else
                            {
                                msg.WriteVariableInt32(-1);
                            }
                            break;
                        }
                    case Inventory.EQUIP_SLOT.SLOT_FACE:
                        {
                            msg.WriteVariableInt32(m_face_id);
                            break;
                        }
                    case Inventory.EQUIP_SLOT.SLOT_HAIR:
                        {
                            msg.WriteVariableInt32(m_hair_id);
                            break;
                        }
                    case Inventory.EQUIP_SLOT.SLOT_HAIR_COLOUR:
                        {
                            msg.WriteVariableInt32(m_hair_colour);
                            break;
                        }
                    case Inventory.EQUIP_SLOT.SLOT_FACE_ACCESSORY:
                        {
                            msg.WriteVariableInt32(m_face_acc_id);
                            break;
                        }
                    case Inventory.EQUIP_SLOT.SLOT_FACE_ACCESSORY_COLOUR:
                        {
                            msg.WriteVariableInt32(m_face_acc_colour);
                            break;
                        }
                    case Inventory.EQUIP_SLOT.SLOT_SKIN:
                        {
                            msg.WriteVariableInt32(m_skin_id);
                            break;
                        }
                    case Inventory.EQUIP_SLOT.SLOT_SKIN_COLOUR:
                        {
                            msg.WriteVariableInt32(m_skin_colour);
                            break;
                        }
                    case Inventory.EQUIP_SLOT.SLOT_GENDER:
                        {
                            msg.WriteVariableInt32((int)m_gender);
                            break;
                        }
                    case Inventory.EQUIP_SLOT.SLOT_RACE:
                        {
                            msg.WriteVariableInt32((int)m_race.m_raceType);
                            break;
                        }
                    case Inventory.EQUIP_SLOT.SLOT_ATTACK_RANGE:
                        {
                            msg.Write(AttackSpeed);
                            break;
                        }
                    case Inventory.EQUIP_SLOT.SLOT_SHOW_HEADGEAR:
                        {
                            if (m_showHeadgear)
                            {
                                msg.Write((byte)1);
                            }
                            else
                            {
                                msg.Write((byte)0);
                            }
                            break;
                        }
                    case Inventory.EQUIP_SLOT.SLOT_SHOW_FASHION:
                        {
                            if (m_showFashion)
                            {
                                msg.Write((byte)1);
                            }
                            else
                            {
                                msg.Write((byte)0);
                            }
                            break;
                        }
                    case Inventory.EQUIP_SLOT.SLOT_SCALE:
                        {
                            msg.Write(Scale);
                            break;
                        }
                    case Inventory.EQUIP_SLOT.SLOT_NAME:
                        {
                            msg.Write(m_name);
                            break;
                        }
                    case Inventory.EQUIP_SLOT.SLOT_LEVEL:
                        {
                            msg.WriteVariableInt32(Level);
                            break;
                        }
                    case Inventory.EQUIP_SLOT.SLOT_CLASS:
                        {
                            msg.WriteVariableInt32((int)m_class.m_classType);
                            break;
                        }
                    case Inventory.EQUIP_SLOT.SLOT_ATTACK_SPEED:
                        {
                            msg.WriteVariableInt32(AttackSpeed);
                            break;
                        }
                    case Inventory.EQUIP_SLOT.SLOT_PVP_LEVEL:
                        {
                            msg.WriteVariableInt32(m_pvpLevel);
                            break;
                        }
                    case Inventory.EQUIP_SLOT.SLOT_PVP_RATING:
                        {
                            msg.WriteVariableInt32(getVisiblePVPRating());
                            break;
                        }
                    case Inventory.EQUIP_SLOT.SLOT_CLAN_NAME:
                        {
                            if (CharactersClan != null)
                            {

                                msg.Write(CharactersClan.ClanName);
                            }
                            else
                            {
                                msg.Write("");
                            }
                            break;
                        }
                }
            }
        }
        
		/// <summary>
        /// Looks at all of the characters equipment and sets their stats accordingly
        /// </summary>
        /// <param name="checkAllStats">true when the stats should all be set rather than using the stat changed level</param>
        public void updateCombatStats(bool checkAllStats)
        {
            if (m_statsChangedLevel >= STATS_CHANGE_LEVEL.EQUIPMENT_CHECK_REQUIRED || checkAllStats == true)
            {
                Item weapon = m_inventory.m_equipedItems[(int)Inventory.EQUIP_SLOT.SLOT_WEAPON];
                Item offHand = m_inventory.m_equipedItems[(int)Inventory.EQUIP_SLOT.SLOT_OFFHAND];
				

                ItemTemplate offhandTemplate = null;
                if (offHand != null)
                {
                    offhandTemplate = offHand.m_template;
                }

                m_reportTime = DEFAULT_ATTACK_REPORT_TIME;
                m_projectileSpeed = 0;
                m_reportProgress = DEFAULT_REPORT_PROGRESS;
                //if wielding a bow the damage is different
                if (weapon != null)
                {
                    ItemTemplate weaponTemplate = weapon.m_template;
                    m_reportTime = m_gender == GENDER.FEMALE ? weaponTemplate.m_reportTimeFemale : weaponTemplate.m_reportTimeMale;

                    if (weaponTemplate.m_projectileSpeed > 0)
                    {
                        m_projectileSpeed = weaponTemplate.m_projectileSpeed;
                    }
                    if (weaponTemplate.m_subtype == ItemTemplate.ITEM_SUB_TYPE.BOW)
                    {
                        m_reportProgress = BOW_REPORT_PROGRESS;
                        if (offHand == null)
                        {
                            offhandTemplate = ItemTemplateManager.GetItemForID(ItemTemplate.DEFAULT_ARROW_ID);
                        }
                    }
                }
                if (offhandTemplate != null)
                {
                    m_projectileSpeed = offhandTemplate.m_projectileSpeed;
                }

				

				//status debug note - status not applied at this time
                m_inventory.calculateEquipmentModifiers();
				//applied by here				

                // PDH - Include any complex status effect stat updates
                CharacterEffectManager.UpdateCombatStats(this);				
 
                CalculateAttackRange();
				
            }
            if (m_statsChangedLevel >= STATS_CHANGE_LEVEL.COMPILE_REQUIRED || checkAllStats == true)
            {
				
                CompileStats();
				
                if (CurrentEnergy > MaxEnergy)
                {
                    CurrentEnergy = MaxEnergy;
                }
                if (CurrentHealth > MaxHealth)
                {
                    CurrentHealth = MaxHealth;
                }
                if (CurrentConcentrationFishing > MaxConcentrationFishing)
                {
                    CurrentConcentrationFishing = MaxConcentrationFishing;
                }
            }


        }
        
		/// <summary>
        /// If a bow and (non infinite) arrow is equipped 
        /// consume an arrow.
        /// </summary>
        void AttemptToConsumeArrows()
        {

            //get the weapon equipped
            Item weapon = m_inventory.GetEquipmentForSlot((int)Inventory.EQUIP_SLOT.SLOT_WEAPON);
            //if there is a weapon equipped
            if (weapon != null)
            {
                //what type of weapon is it
                ItemTemplate.ITEM_SUB_TYPE subtype = weapon.m_template.m_subtype;
                //if it is a bow
                if (subtype == ItemTemplate.ITEM_SUB_TYPE.BOW)
                {
                    //are there any arrows equipped
                    Item arrows = m_inventory.GetEquipmentForSlot((int)Inventory.EQUIP_SLOT.SLOT_OFFHAND);
                    if (arrows != null)
                    {
                        ItemTemplate arrowTemplate = arrows.m_template;
                        if (arrowTemplate != null && arrowTemplate.m_item_id > ItemTemplate.MAX_INFINITE_ARROW_ID)
                        {
                            //int oldInventoryID =  arrows.m_inventory_id;
                            Item oldArrows = new Item(arrows);
                            m_inventory.ConsumeItem(arrows.m_template_id, arrows.m_inventory_id, 1);
                            arrows = m_inventory.GetEquipmentForSlot((int)Inventory.EQUIP_SLOT.SLOT_OFFHAND);

                            m_inventory.SendReplaceItem(oldArrows, arrows);
                        }
                    }

                }
            }
        }
        
		internal override bool CarryOutProc(SkillDamageData theProc)
        {
            bool success = base.CarryOutProc(theProc);
            Item weapon = m_inventory.GetEquipmentForSlot((int)Inventory.EQUIP_SLOT.SLOT_WEAPON);
            if (weapon != null)
            {
                int procSkillID = weapon.m_template.m_procSkillID;
                
                EntitySkill procSkill = theProc.TheSkill;
                //ensure it's the same proc before the item is consumed 
                if (procSkill != null && procSkill.SkillID == (SKILL_TYPE)procSkillID)
                {
                    //does it need to consume charges
                    if (weapon.m_template.m_maxCharges > 0)
                    {
                        //you cant have less that 0 charges
                        if (weapon.m_remainingCharges <= 0)
                        {
                            
                        }
                        else
                        {
                            Item oldItem = new Item(weapon);
                            m_inventory.ConsumeCharge(weapon);
                            //if it was destroyed remove the item client side
                            if (weapon.Destroyed == true)
                            {
                                m_inventory.SendReplaceItem(oldItem, null);
                            }
                                //if the item was not destroyed then send an update on the charges
                            else
                            {
                                m_inventory.SendReplaceItem(oldItem, weapon);

                            }
                        }
                    }
                }
            }
            return success;
        }
        		
        void AttemptToUseWeaponProc()
        {
            if (m_currentProcData != null)
            {
                CarryOutProc(m_currentProcData);
                m_currentProcData = null;
            }
        }
        
		/// <summary>
        /// Will use the weapon information to attempt to do a proc 
        /// </summary>
        /// <param name="attackDamage">The attack damage that triggers the proc, this must a hit to be eligible</param>
        void TryToBeginProc(CombatDamageMessageData attackDamage)
        {
            Item weapon = m_inventory.GetEquipmentForSlot((int)Inventory.EQUIP_SLOT.SLOT_WEAPON);
            if (weapon != null)
            {
                //only a hit can activate a proc skill
                if (attackDamage.Reaction == (int)CombatManager.COMBAT_REACTION_TYPES.CRT_HIT)
                {
                    //attempt to use a proc skill
                    //get the proc skill and chance
                    int procSkillID = weapon.m_template.m_procSkillID;
                    float procSkillChance = weapon.m_template.m_procSkillChance;
                    //if it has a skill and a chance to use the skill
                    if (procSkillID > 0 && procSkillChance > 0)
                    {
                        //see if they manage to use the skill
                        int maxChance = 1000;
                        int chance = (int)(procSkillChance * maxChance);
                        int randResult = Program.getRandomNumber(maxChance);

                        //if they succeded 
                        if (randResult < chance)
                        {

                            int procSkillLevel = weapon.m_template.m_procSkillLevel;
                            SkillTemplate procSkillTemplate = SkillTemplateManager.GetItemForID((SKILL_TYPE)procSkillID);
                            CombatEntity target = m_currentAttackDamage.TargetLink;
                            //check the skill exists
                            if (procSkillTemplate != null)
                            {
                                //should it target the enemy or the caster
                                int targetError = SkillTemplate.CheckSkillForUseAgainst(target, this, procSkillTemplate.CastTargetGroup); ;
                                bool validAction = targetError == (int)SkillTemplate.SkillTargetError.NoError;
                                if (validAction == false)
                                {
                                    target = this;
                                    targetError = SkillTemplate.CheckSkillForUseAgainst(target, this, procSkillTemplate.CastTargetGroup);
                                    validAction = targetError == (int)SkillTemplate.SkillTargetError.NoError;
                                }
                                //if a vallid target has been found
                                if (validAction != false)
                                {
                                    bool doProc = true;
                                    //are there enough charges to do this
                                    if (weapon.m_template.m_maxCharges > 0)
                                    {
                                        if (weapon.m_remainingCharges <= 0)
                                        {
                                            //if not enough charges then do not do the proc
                                            doProc = false;
                                        }
                                        else
                                        {
                                            //item consumption will occur when the proc damage is done
                                            doProc = true;
                                        }
                                    }
                                    if (doProc)
                                    {
                                        EntitySkill procSkill = new EntitySkill(procSkillTemplate);
                                        procSkill.SkillLevel = procSkillLevel;
                                        procSkill.IsProc = true;
                                        //work out how much damage the proc will do
                                        SkillDamageData newProcDamage = TheCombatManager.DummyCastSkill(this, target, procSkill, attackDamage.ApplyTime);
                                        //set this as the proc
                                        StartProc(newProcDamage);
                                    }

                                }
                            }
                        }
                    }
                }
            }
        }
       
        internal override void DamageApplied(CombatDamageMessageData endedAttack)
        {
            if (m_currentAttackDamage != null && m_currentAttackDamage == endedAttack &&
                endedAttack.DamageApplied == false && endedAttack.AttackType == (int)CombatManager.ATTACK_TYPE.ATTACK)
            {
                AttemptToConsumeArrows();
                AttemptToUseWeaponProc();
            }
            base.DamageApplied(endedAttack);
        }
        
		internal override void StartAttack(CombatDamageMessageData attackDamage)
        {			
            base.StartAttack(attackDamage);
            if (attackDamage != null)
            {
                TryToBeginProc(attackDamage);
            }
        }
        
		internal override void EndAttack()
        {			
            double currentTime = Program.MainUpdateLoopStartTime();
            //if the time for the damage to be carried out has passed then do any after effects
            if (m_currentAttackDamage != null && m_currentAttackDamage.ActionCompleteTime < currentTime && m_currentAttackDamage.DamageApplied==false)
            {
                AttemptToConsumeArrows();
                AttemptToUseWeaponProc();               
            }
            base.EndAttack();
        }       		
        
		internal void AreaBattleUpdateSent()
        {
            m_lastCombatSendPos = CurrentPosition.m_position;
        }

        protected override int getWeaponAbilityModifier()
        {
            Item weapon = m_inventory.GetEquipmentForSlot(0);
            if (weapon == null)
                return getAbilityLevel(ABILITY_TYPE.HAND_TO_HAND); ;
            ItemTemplate.ITEM_SUB_TYPE subtype = weapon.m_template.m_subtype;
            switch (subtype)
            {
                case ItemTemplate.ITEM_SUB_TYPE.SWORD://sword
                case ItemTemplate.ITEM_SUB_TYPE.SWORD_TWO_HANDED:
                    {
                        return getAbilityLevel(ABILITY_TYPE.SWORD);
                    }
                case ItemTemplate.ITEM_SUB_TYPE.BLUNT://blunt
                case ItemTemplate.ITEM_SUB_TYPE.BLUNT_TWO_HANDED:
                    {
                        return getAbilityLevel(ABILITY_TYPE.BLUNT);
                    }
                case ItemTemplate.ITEM_SUB_TYPE.AXE://blunt
                case ItemTemplate.ITEM_SUB_TYPE.AXE_TWO_HANDED:
                    {
                        return getAbilityLevel(ABILITY_TYPE.AXE);
                    }
                case ItemTemplate.ITEM_SUB_TYPE.STAFF://blunt 
                    {
                        return getAbilityLevel(ABILITY_TYPE.STAFF);
                    }
                case ItemTemplate.ITEM_SUB_TYPE.ONE_HANDED_STAFF:
                case ItemTemplate.ITEM_SUB_TYPE.TOTEM_LONG:
                    {
                        return getAbilityLevel(ABILITY_TYPE.TOTEM);
                    }
                case ItemTemplate.ITEM_SUB_TYPE.WAND:
                    {
                        return getAbilityLevel(ABILITY_TYPE.WAND);
                    }
                case ItemTemplate.ITEM_SUB_TYPE.DAGGER:
                    {
                        return getAbilityLevel(ABILITY_TYPE.DAGGER);
                    }
                case ItemTemplate.ITEM_SUB_TYPE.BOW:
                    {
                        return getAbilityLevel(ABILITY_TYPE.BOW);
                    }
                case ItemTemplate.ITEM_SUB_TYPE.SPEAR:
                case ItemTemplate.ITEM_SUB_TYPE.SPEAR_TWO_HANDED:
                    {
                        return getAbilityLevel(ABILITY_TYPE.SPEAR);

                    }
                case ItemTemplate.ITEM_SUB_TYPE.SLEDGE:
                case ItemTemplate.ITEM_SUB_TYPE.BROOM:
                case ItemTemplate.ITEM_SUB_TYPE.MAGIC_CARPET:
                case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_BROOM:
                case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_WAND:
                case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_LUTE:
                case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_DRAGONSTAFF:
                case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_FLUTE:
                case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_HARP:
                case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_TWO_HANDED:
                case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_STAFF_MOUNT:
                case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_HORN:
                case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_BLUNT:
                case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_BATMOUNT:
                case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_ANGEL_WINGS:
                case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_DRUM:
                case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_EAGLEMOUNT:
                case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_BAGPIPES:
                case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_CROW:
                case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_SPARROW:
                case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_SPARROWHAWK:
                case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_SPIRITCAPE:
                case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_HORSEMOUNT:
                case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_BANSHEE_BLADE:
                case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_BONE_BIRD:
                case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_HELL_WINGS:
                case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_BOARMOUNT:
                    {
                        return getAbilityLevel(ABILITY_TYPE.NOVELTY_ITEM);

                    }
                case ItemTemplate.ITEM_SUB_TYPE.HAND_TO_HAND:
                    {
                        return getAbilityLevel(ABILITY_TYPE.HAND_TO_HAND);
                    }
                case ItemTemplate.ITEM_SUB_TYPE.FISHING_ROD:
                    {
                        return getAbilityLevel(ABILITY_TYPE.FISHING);
                    }
                default:
                    {
                        return 0;
                    }
            }
        }

        internal override int getAbilityLevel(ABILITY_TYPE ability_id)
        {
            int level = (int)m_compiledStats.GetAbilityValForId(ability_id);

            if (level < 0)
            {
                level = 0;
            }
            return level;           
        }

        internal override CharacterAbility getAbilityById(ABILITY_TYPE ability_id)
        {
            for (int i = 0; i < m_abilities.Count; i++)
            {
                if (m_abilities[i].m_ability_id == ability_id)
                {
                    return m_abilities[i];
                }
            }
            return null;
        }
        public void setDefaultStats()
        {
            m_baseVitality = 10;
            m_baseStrength = 10;
            m_baseFocus = 10;
            m_baseDexterity = 10;
            UpdateBaseStats();            
        }
        internal void ReturnToBasicAppearance()
        {
            m_face_id = m_basicAppearance.m_face_id;
            m_skin_id = m_basicAppearance.m_skin_id;
            m_skin_colour = m_basicAppearance.m_skin_colour;
            m_hair_id = m_basicAppearance.m_hair_id;
            m_hair_colour = m_basicAppearance.m_hair_colour;
            m_face_acc_id = m_basicAppearance.m_face_acc_id;
            m_face_acc_colour = m_basicAppearance.m_face_acc_colour;
            Scale = BaseStats.Scale;

        }

        #region Database
        public void readBasicfromDb(Database db, SqlQuery query)
        {
            m_character_id = query.GetUInt32("character_id");
            m_name = query.GetString("name");
            /*if (m_name.Contains("71"))
                m_enable_cheat = true;*/
            m_race = RaceTemplateManager.getRaceTemplate((RACE_TYPE)query.GetInt32("race_id"));
            
            
            m_basicAppearance.m_face_id = query.GetInt32("face_id");
            m_basicAppearance.m_skin_id = query.GetInt32("skin");
            m_basicAppearance.m_skin_colour = query.GetInt32("skin_colour");
            m_basicAppearance.m_hair_id = query.GetInt32("hair_id");
            m_basicAppearance.m_hair_colour = query.GetInt32("hair_colour");
            m_basicAppearance.m_face_acc_id = query.GetInt32("face_accessory_id");
            m_basicAppearance.m_face_acc_colour = query.GetInt32("face_accessory_colour");
            ReturnToBasicAppearance();
            m_zone = Program.processor.getZone(query.GetInt32("zone"));
            m_showHeadgear = query.GetBoolean("show_headgear");
            m_showFashion = query.GetBoolean("show_fashion");

            Level= query.GetInt32("level");
            LevelFishing = query.GetInt32("fishing_level");
			//fishing starts at level 1
	        if (LevelFishing <= 0)
		        LevelFishing = 1;

            CookingExperience = query.GetInt64("cooking_experience");
            LevelCooking = query.GetInt32("cooking_level");
            // As above cooking level starts at 1
            if (LevelCooking <= 0)
                LevelCooking = 1;

            m_baseStats.Scale = query.GetFloat("model_scale");
            UnmodifiedScale = m_baseStats.Scale;
            //set the resistance to player to boss damage
            m_baseStats.SetDamageReductionType((int)DAMAGE_TYPE.MYTHIC_DAMAGE, 1);
            m_class = ClassTemplateManager.getClassTemplate((CLASS_TYPE)query.GetInt32("class_id"));
            m_gender = (GENDER)query.GetInt32("gender");
            ServerID = (int)m_character_id;
            Type = EntityType.Player;
            CurrentPosition = m_CharacterPosition;



            m_inventory.FillBagForCharacterID(db, m_character_id);
            m_inventory.PopulateRewardListFromDB(db, m_character_id);

            EquipAllItems();
        }

        public void setCharacterAppearance(int face_id, int skin_colour_id, int hair_id, int hair_colour_id, int face_accessory_id, int face_accessory_colour_id,float scale)
        {
            
            //Update character details with the new id's
            string updateString = "UPDATE character_details SET face_id=" + face_id + ",skin_colour=" + skin_colour_id +
                                  ",hair_id=" + hair_id + ",hair_colour=" + hair_colour_id + ",face_accessory_id=" +
                                  face_accessory_id +
                                  ",face_accessory_colour=" + face_accessory_colour_id + ",model_scale=" + scale +
                                  " where character_id = " + m_character_id;

            m_db.runCommandSync(updateString);
            m_basicAppearance.m_face_id = face_id;
            m_basicAppearance.m_skin_colour = skin_colour_id;
            m_basicAppearance.m_hair_id = hair_id;
            m_basicAppearance.m_hair_colour = hair_colour_id;
            m_basicAppearance.m_face_acc_id = face_accessory_id;
            m_basicAppearance.m_face_acc_colour = face_accessory_colour_id;
            m_baseStats.Scale = scale;
            
            ReturnToBasicAppearance();
            

            //Messaging stuff
            //Note updated info
            m_updatedInfo.Add(Inventory.EQUIP_SLOT.SLOT_FACE);
            m_updatedInfo.Add(Inventory.EQUIP_SLOT.SLOT_HAIR);
            m_updatedInfo.Add(Inventory.EQUIP_SLOT.SLOT_HAIR_COLOUR);
            m_updatedInfo.Add(Inventory.EQUIP_SLOT.SLOT_FACE_ACCESSORY);
            m_updatedInfo.Add(Inventory.EQUIP_SLOT.SLOT_FACE_ACCESSORY_COLOUR);
            m_updatedInfo.Add(Inventory.EQUIP_SLOT.SLOT_SCALE);
            m_updatedInfo.Add(Inventory.EQUIP_SLOT.SLOT_SKIN_COLOUR);
            CurrentZone.SendUpdatedCharacerToAllPlayers(Program.Server,this);
        }

        void ReadTeleportLocationsFromDatabase(SqlQuery query)
        {
            string teleportLocationsString = query.GetString("teleport_locations");

            string[] locationNumbers = teleportLocationsString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (int currentLocationIndex = 0; currentLocationIndex < locationNumbers.Length; currentLocationIndex++)
            {
                string currentLocationString = locationNumbers[currentLocationIndex];

                if (currentLocationString.Length > 0)
                {
                    int currentLocationNumber;
                    bool result = Int32.TryParse(currentLocationString, out currentLocationNumber);
                    if (result)
                    {
                        m_discoveredSpawnPoints.Add(currentLocationNumber);
                    }
                }
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newLocation"></param>
        /// <returns>true if it was already discovered, false if it's new</returns>
        internal bool AddTeleportLocation(int newLocation)
        {
            bool alreadyObtained = m_discoveredSpawnPoints.Contains(newLocation);

            if (!alreadyObtained)
            {
                m_discoveredSpawnPoints.Add(newLocation);
                string teleportLocationString = WriteTeleportLocationsToString();
                increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.EXPLORER, 1);
                m_db.runCommandSync("update character_details set teleport_locations='" + teleportLocationString + "' where character_id=" + m_character_id);
                Program.Display("character-" + m_character_id + " discovered teleport point-" + newLocation);
                
                if (Program.m_LogAnalytics)
                {
                    AnalyticsMain logAnalytics = new AnalyticsMain(true);
                    logAnalytics.fastTravelNew(m_player, newLocation.ToString());
                }
            }

            return alreadyObtained;

        }
        string WriteTeleportLocationsToString()
        {
            string currentTeleportLocations = "";
            //write all of the numbers followed by a space

            for (int currentLocationIndex = 0; currentLocationIndex < m_discoveredSpawnPoints.Count; currentLocationIndex++)
            {
                int currentPoint = m_discoveredSpawnPoints[currentLocationIndex];

                currentTeleportLocations += currentPoint;

                if (currentLocationIndex < m_discoveredSpawnPoints.Count - 1)
                {
                    currentTeleportLocations += " ";
                }

            }

            return currentTeleportLocations;
        }
        
        internal void SaveCharacterPreferences()
        {
            m_db.runCommandSync("update character_details set show_headgear=" + m_showHeadgear + ",show_fashion=" + m_showFashion + " where character_id=" + m_character_id);

        }
        #endregion
        
		#region combat
        
        public void SendRespawn()
        {
            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.RespawnPlayer);
            outmsg.WriteVariableUInt32((uint)1);

            //the character that respawned
            outmsg.WriteVariableUInt32(m_character_id);
            //the respawn position
            Vector3 respawnPosition = CurrentPosition.m_position;//respawnPoint.Position;// currentCharacter.m_CharacterPosition.m_position;
            outmsg.Write((float)respawnPosition.X);
            outmsg.Write((float)respawnPosition.Y);
            outmsg.Write((float)respawnPosition.Z);
            outmsg.WriteVariableInt32(CurrentHealth);

            List<NetConnection> connections = CurrentZone.getUpdateList(null);

            Program.processor.SendMessage(outmsg, connections, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.RespawnPlayer);

        }
        internal void SendLuckyItemPopup(string title, string body)
        {
            string spacer = "";
            List<string> stringList = new List<string>();
            stringList.Add(title);
            stringList.Add(body);
            stringList.Add(spacer);
            if (m_player != null)
            {
                Program.processor.SendXMLPopupMessage(true, m_player, (int)XML_Popup.Set_Popup_IDs.SPI_BasicPopup,
                    XML_Popup.Popup_Type.None, "luck_box_popup.txt",
                    stringList, false);
            }

        }
        internal void SendMagicBoxPopup(List<LootDetails> lootdetails, string lootList)
        {
            if (m_player == null)
            {
                return;
            }

            string spacer = "";
            List<string> stringList = new List<string>(10);
            //open box popup
            Program.processor.SendXMLPopupMessage(true, m_player, (int)XML_Popup.Set_Popup_IDs.SPI_MAG_BOX_OPEN,
				   XML_Popup.Popup_Type.None, 
				   "popup_magbox_open_prefab", //new
				   //"popup_magbox_open.txt", //old
                   stringList, false);
            //up to the 2nd last item popup
            for (int i = 0; i < lootdetails.Count - 1; i++)
            {
                stringList.Clear();
                LootDetails currentLoot = lootdetails[i];
                string itemText = "";
                int currentID = currentLoot.m_templateID;
                int currentQuantity = currentLoot.m_quantity;
                ItemTemplate currentLootTemplate = ItemTemplateManager.GetItemForID(currentID);
                if (currentLootTemplate != null)
                {
                    itemText += currentLootTemplate.m_loc_item_name[m_player.m_languageIndex];
                    if (currentQuantity > 1)
                    {
                        itemText += " * " + currentQuantity;
                    }
                }
                stringList.Add(itemText);
                Program.processor.SendXMLPopupMessage(true, m_player, (int)XML_Popup.Set_Popup_IDs.SPI_MAG_BOX_NEXT,
				   XML_Popup.Popup_Type.None, 
				   "popup_magbox_next_prefab", 
				   //"popup_magbox_next.txt",
                   stringList, false);
            }

                //last item popup

                stringList.Add(spacer);
            if (m_player != null)
            {
                stringList.Clear();
                LootDetails currentLoot = lootdetails.Last();
                string itemText = "";
                int currentID = currentLoot.m_templateID;
                int currentQuantity = currentLoot.m_quantity;
                ItemTemplate currentLootTemplate = ItemTemplateManager.GetItemForID(currentID);
				string locText = Localiser.GetString(textDB, m_player, (int)CharacterTextDB.TextID.GAINED_ITEM);
				locText = string.Format(locText, lootList);
				string dataString = locText;
				if (currentLootTemplate != null)
                {
                    itemText += currentLootTemplate.m_loc_item_name[m_player.m_languageIndex];
                    if (currentQuantity > 1)
                    {
                        itemText += " * " + currentQuantity;
                    }
                }
                stringList.Add(itemText);
                XML_Popup newPopup = Program.processor.SendXMLPopupMessage(true, m_player, (int)XML_Popup.Set_Popup_IDs.SPI_MAG_BOX_CLOSE,
				   XML_Popup.Popup_Type.None, 
				   "popup_magbox_final_prefab", 
				   //"popup_magbox_final.txt",
                   stringList, false);
                XML_PopupData popupData = new XML_PopupData();
                popupData.m_postDataID = currentID;
                popupData.m_postString = dataString;
                newPopup.PopupData = popupData;

            }

        }


        public void Respawn(Vector3 respawnPosition, Respawn_Type respawnType, int variableID, int respawnHealth)
        {
            //call our normal respawn method
            this.Respawn(respawnPosition, respawnType, variableID);
            //now set the health correclty
            CurrentHealth = Math.Abs(respawnHealth);
        }

        /// <summary>
        /// Respawn the character, adding appropriate status effects (e.g. death timer, dismounted)
        /// </summary>
        /// <param name="respawnPosition">position in world of respawn</param>
        /// <param name="respawnType">Type of respawn, like an idol or spring of life</param>
        /// <param name="variableID">If 20 - we're a old type of res idol and so give the player max health. </param>
        public override void Respawn(Vector3 respawnPosition, Respawn_Type respawnType, int variableID)
        {
            bool didRespawn = false;
            if (respawnType!= Respawn_Type.ResSpell && CurrentHealth < 5)
            {
                CurrentHealth = (int)Math.Ceiling((double)MaxHealth / 2.0);
                didRespawn = true;
            }
            if (respawnType != Respawn_Type.ResSpell && CurrentConcentrationFishing < 5)
            {
                CurrentConcentrationFishing = (int)Math.Ceiling((double)MaxConcentrationFishing / 2.0);
                didRespawn = true;
            }
            if (CurrentHealth < 1 && respawnType == Character.Respawn_Type.ResSpell)
            {
                //don't alter energy
                //CurrentEnergy = 0;
                CurrentHealth = 1;
                didRespawn = true;
            }
            if (CurrentHealth <= 0)
            {
                CurrentHealth = 1;
                didRespawn = true;
            }
            if (respawnType == Respawn_Type.ResIdol)
            {
                //if an original style idol was used
                if (variableID == ItemTemplate.OLD_RES_ITEM_ID)
                {
                    CurrentHealth =  MaxHealth;
                }
                else
                {
                    // New resurrection idol = 25%
                    CurrentHealth = MaxHealth / 2;

                    // if the current energy is below 50%, set it to 50%
                    if (CurrentEnergy < MaxEnergy/2)
                    {
                        CurrentEnergy = MaxEnergy/2;
                    }
                    // else don't touch the energy
                }

                // Death Timer
                // Adding a new status effect (Death Timer), lasts 30secs, purpose - prevents resurrection idol usage 
                CharacterEffectManager.InflictNewCharacterEffect(new CharacterEffectParams
                {
                    charEffectId = (EFFECT_ID)DEATH_TIMER_ID,
                    caster = this,
                    level = 0,
                    aggressive = false,
                    PVP = false,
                    statModifier = 0
                }, this);

                
                didRespawn = true;
            }

            // add a dismounted effect if mounted and status effect not present
            // but not if we used a resspell (e.g. spring of life)
            if (DismountedEffectShouldBeApplied() && respawnType != Respawn_Type.ResSpell)
            {
                // Adding a new status effect (Death Timer), lasts 30secs, purpose - prevents resurrection idol usage 
                CharacterEffectManager.InflictNewCharacterEffect(new CharacterEffectParams
                {
                    //get design to spec one, we'll use strangling vines for now
                    charEffectId = (EFFECT_ID)DISMOUNTED_STATUS_ID,
                    caster = this,
                    level = 0,
                    aggressive = false,
                    PVP = false,
                    statModifier = 0
                }, this);

                //message the player about this effect
				string locText = Localiser.GetString(textDB, m_player, (int)CharacterTextDB.TextID.HAVE_BEEN_DISMOUNTED);
				SendSimpleMessageToPlayer(locText);
            }

            m_CharacterPosition.m_position = respawnPosition;
            m_SentPosition.m_position = respawnPosition;
            m_ConfirmedPosition.m_position = respawnPosition;
            m_ProjectedPosition.m_position = respawnPosition;
            m_CharacterPosition.m_currentSpeed = 0;
            TheCharactersPath.ClearList();
            EntityPartitionCheck();
            WakeupGameEffects();
            Dead = false;
            if (didRespawn == true)
            {
                SendRespawn();
                m_signpostManager.CharacterDied(this);
            }
            //time to apply death penalty
            StatusEffect starterDeathPenalty = GetStatusEffectForID((EFFECT_ID)START_DEATH_STATUS_ID);
      
            StatusEffect currentDeathPenalty = GetStatusEffectForID((EFFECT_ID)DEATH_STATUS_ID);
            StatusEffectTemplate deathPenaltyTemplate = StatusEffectTemplateManager.GetStatusEffectTemplateForID((EFFECT_ID)DEATH_STATUS_ID);
            int statusEffectLvl = 0;
            if (currentDeathPenalty != null)
            {
                statusEffectLvl = currentDeathPenalty.m_statusEffectLevel;
            }

            if (didRespawn==true && 
                deathPenaltyTemplate != null &&
                Level>= MIN_LVL_DEATH_PENALTY&&
                respawnType != Respawn_Type.ResSpell)
            {
                int nextLvl = statusEffectLvl;
                if (currentDeathPenalty != null)
                {
                    nextLvl = statusEffectLvl + 1;
                }

                if (deathPenaltyTemplate.getEffectLevel(nextLvl, false) == null)
                {
                    nextLvl = statusEffectLvl;
                }

                if (starterDeathPenalty != null || currentDeathPenalty != null)
                {
                    // Curse of the Fallen 
                    CharacterEffectManager.InflictNewCharacterEffect(new CharacterEffectParams
                    {   charEffectId = (EFFECT_ID)DEATH_STATUS_ID,
                        caster = this,
                        level = nextLvl,
                        aggressive = false,
                        PVP = false,
                        statModifier = 0}, this);

                    //InflictNewStatusEffect((EFFECT_ID)DEATH_STATUS_ID, this, nextLvl, false, false, 0);
                }
                else
                {
                    // Resurrected
                    CharacterEffectManager.InflictNewCharacterEffect( new CharacterEffectParams
                    {   charEffectId = (EFFECT_ID)START_DEATH_STATUS_ID,
                        caster = this,
                        level = 0,
                        aggressive = false,
                        PVP = false,
                        statModifier = 0 }, this);

                    //InflictNewStatusEffect((EFFECT_ID)START_DEATH_STATUS_ID, this, 0, false, false, 0);
                }

                if (starterDeathPenalty != null)
                {
                    starterDeathPenalty.Complete();
                }
            }
        }

        /// <summary>
        /// Add a dismounted effect on death, only if we have a mount and we don't already have one
        /// </summary>
        /// <returns></returns>
        private bool DismountedEffectShouldBeApplied()
        {
            //are we mounted?
            if (this.m_inventory.m_equipedItems[(int) Inventory.EQUIP_SLOT.SLOT_MOUNT] == null)
                return false;

            if (HasDismountedEffect())
                return false;

            return true;
        }

        /// <summary>
        /// Check characters status effect, if Dismounted effect is present return true. Else return false.
        /// </summary>
        /// <returns>True if dismountted effect present</returns>
        public bool HasDismountedEffect()
        {
            //we're mounted...do we have a dismounted effect already?
            foreach (var charFX in CurrentCharacterEffects)
            {
                //I think this is the correct matching? lot of ints to compare with
                if (charFX.StatusEffect.Template.EffectType == EFFECT_TYPE.DISMOUNTED)                    
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Check characters status effect, if dismounted effect is present set it to expire on the next tick
        /// </summary>        
        public void RemoveDismountedEffect()
        {                        
            foreach (var charFX in CurrentCharacterEffects)
            {
                //find the dismounted effect if present and set the duration to 1
                if (charFX.StatusEffect.Template.EffectType == EFFECT_TYPE.DISMOUNTED)
                {
                    // by setting this low it will neatly expire in the next tick
                    charFX.m_Duration = 1f;
                }
            }            
        }

        public override void SkillCancelledNotification()
        {
            if (CurrentSkill != null)
            {
                if (Program.m_LogAnalytics)
                {
                    AnalyticsMain logAnalytics = new AnalyticsMain(true);
                    int skillID = (int)CurrentSkill.SkillID;
                    string skillName = CurrentSkill.Template.SkillName;
                    string reasonForFail = "interrupted";
                    logAnalytics.skillUsed(m_player, skillID.ToString(), skillName, false, reasonForFail);
                }
            }
            base.SkillCancelledNotification();
        }

        /// <summary>
        /// The Character has logged out by choice,
        /// if they were in the middle of something then penalise them for it
        /// </summary>
        internal void CharacterRequestedLogOut()
        {

            bool otherRewarded = ForfeitPVP();
            if (otherRewarded == true&& CurrentZone!=null)
            {
                PlayerSpawnPoint respawnPoint = CurrentZone.GetClosestRespawnPoint(this);
                 if(respawnPoint!=null){
                     Respawn(respawnPoint.Position, Respawn_Type.normal, -1);
                }
            }
        }
        internal bool ForfeitPVP()
        {
            bool otherRewarded = false;
            //are they in pvp
            if (IsPVP() == true&& Dead==false&&(IsInPVPType( PVPType.Duel)==false))
            {
                //if so it's time to kill them...  politely 

                //find out who has done the most damage to them recently

                Character pseudoKiller = null;
               
                pseudoKiller = GetPVPKiller();
               
                //if someone can be rewarded, then reward them
                if (pseudoKiller != null)
                {
                    Program.Display("Pseudo Killer of " + GetIDString() + " is " + pseudoKiller.GetIDString());
                    WorkOutPVPRewards(pseudoKiller);
                    otherRewarded = true;
                }

            }
            return otherRewarded;
        }
        internal bool ForfeitDuel()
        {
            bool otherRewarded = false;
            //are they in pvp
            if (Dead == false && m_currentDuelTarget != null)
            {
                //if so it's time to kill them...  politely  
                Character pseudoKiller = m_currentDuelTarget.DuelCharacter;

                //if someone can be rewarded, then reward them
                if (pseudoKiller != null)
                {
                    Program.Display("Duel Forfeited by " + GetIDString() + " against " + pseudoKiller.GetIDString());
                    WorkOutPVPRewards(pseudoKiller);
                    otherRewarded = true;
                }

            }
            return otherRewarded;
        }
        internal override void Died()
        {
            string killerID="";
            string killerName="";
            string killerType="";
            if (m_killer != null && m_killer.Type == EntityType.Mob)
            {
                m_zone.recordCombatFinishStats(this, (ServerControlledEntity)m_killer, Zone.COMBAT_WINNER.MOB);
                ServerControlledEntity theMob = (ServerControlledEntity)m_killer;
                if (theMob != null)
                {
                    killerID = theMob.Template.m_templateID.ToString();
                    killerName = m_killer.Name;
                    killerType = "Non Player Character";
                }
            }
            if (m_killer != null && m_killer.Type == EntityType.Player)
            {
                Character finalKiller = GetPVPKiller();
                if (finalKiller != null)
                {
                    Program.Display("Final Killer of " + GetIDString() + " is " + finalKiller.GetIDString());
                    WorkOutPVPRewards(finalKiller);
                    if (finalKiller.m_player != null)
                    {
                        killerID = finalKiller.m_player.m_account_id.ToString();
                        killerName = finalKiller.Name;
                        killerType = "Player Character";
                    }
                    
                }
            }
            
            Signposting.CharacterEvent newEvent = new MainServer.Signposting.CharacterEvent(Signposting.CharacterEvent.EventType.CharacterDied, null);
            RecentEvents.Add(newEvent);
            base.Died();

            m_signpostManager.CharacterDied(this);

            if (m_party != null)
            {
				string locText = Localiser.GetString(textDB, m_player, (int)CharacterTextDB.TextID.HAS_DIED);
				locText = string.Format(locText, m_name);
				m_party.SendPartySystemMessage(locText, this, false, SYSTEM_MESSAGE_TYPE.PARTY, false);
            }
            if (Program.m_LogAnalytics && killerID!="")
            {
                AnalyticsMain logAnalytics = new AnalyticsMain(false);
                logAnalytics.playerDefeated(m_player, killerID, killerName, killerType);
            }

        }

       
        internal void WasDeadOnLogin()
        {
            Dead = true;
        }
        #endregion
        
		#region networking

        internal void SendAppearanceUpdateToSelf()
        {
            NetOutgoingMessage appMessage = Program.Server.CreateMessage();
            appMessage.WriteVariableUInt32((uint)NetworkCommandType.AppearanceOnlyUpdate);

            //write the basic appearance data
            WriteCharacterAppearanceToMessage(appMessage);
            appMessage.WriteVariableInt32(getVisiblePVPRating());
            appMessage.WriteVariableInt32(m_pvpLevel);

            //send
            Program.processor.SendMessage(appMessage, m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.AppearanceOnlyUpdate);

        }
        
		public void WriteCharacterAppearanceToMessage(NetOutgoingMessage outmsg)
        {
            outmsg.WriteVariableInt32((int)m_race.m_raceType);
            outmsg.WriteVariableInt32(m_face_id);
            outmsg.WriteVariableInt32(m_skin_id);
            outmsg.WriteVariableInt32(m_skin_colour);
            outmsg.WriteVariableInt32(m_hair_id);
            outmsg.WriteVariableInt32(m_hair_colour);
            outmsg.WriteVariableInt32(m_face_acc_id);
            outmsg.WriteVariableInt32(m_face_acc_colour);
            outmsg.Write(Scale);
            outmsg.Write((byte)m_gender);
            if (ShowHeadgear == true)
            {
                outmsg.Write((byte)1);
            }
            else
            {
                outmsg.Write((byte)0);
            }
            if (ShowFashion == true)
            {
                outmsg.Write((byte)1);
            }
            else
            {
                outmsg.Write((byte)0);
            }


        }

        public void writeBasicCharacterInfoToMsg(NetOutgoingMessage outmsg)
        {
            // any change to this method, need to mirror the changes on the client
            // method is mirrored with Client.CBasicPC() constructor

            outmsg.WriteVariableUInt32(m_character_id);            
            outmsg.Write(m_name);

            outmsg.Write(CharactersClan != null ? CharactersClan.ClanName : "");

            WriteCharacterAppearanceToMessage(outmsg);
            
            //write equipment
            for (int i = 0; i < Inventory.NUM_EQUIP_SLOTS; i++)
            {
                WriteEquipmentIDToMessage(outmsg, i);
            }

            //write the zone and level/class/pvp info
            outmsg.WriteVariableInt32(m_zone.m_zone_id);
            outmsg.WriteVariableInt32(Level);
            outmsg.WriteVariableInt32(LevelFishing);
            outmsg.WriteVariableInt32(LevelCooking);            
            outmsg.WriteVariableInt32((int)m_class.m_classType);            
            outmsg.Write(CompiledStats.MaxAttackRange);
            outmsg.WriteVariableInt32(AttackSpeed);
            outmsg.WriteVariableInt32(m_pvpLevel);
            outmsg.WriteVariableInt32(getVisiblePVPRating());            
        }
        
        #region movement
        public void WriteMovementInfoToMessage(NetOutgoingMessage outmsg)
        {
            outmsg.WriteVariableUInt32(m_character_id);
            outmsg.Write((float)m_CharacterPosition.m_position.X);
            outmsg.Write((float)m_CharacterPosition.m_position.Y);
            outmsg.Write((float)m_CharacterPosition.m_position.Z);
            outmsg.Write(m_CharacterPosition.m_yangle);

            byte stopped = 1;
            if (m_CharacterPosition.m_currentSpeed != 0)
                stopped = 0;
            outmsg.Write((byte)stopped);
        }
        
        public void readMovementInfofromMessage(NetIncomingMessage msg, double timeSinceSend)
        {

            float posX = msg.ReadFloat();
            float posY = msg.ReadFloat();
            float posZ = msg.ReadFloat();
            float angle = msg.ReadFloat();
            float currentSpeed = msg.ReadFloat();

            if (m_InLimbo == true)
            {
                return;
            }

            m_CharacterPosition.m_position.X = posX;
            m_CharacterPosition.m_position.Y = posY;
            m_CharacterPosition.m_position.Z = posZ;
            /*position.X = msg.ReadFloat();
            position.Y = msg.ReadFloat();
            position.Z = msg.ReadFloat();*/
            m_CharacterPosition.m_yangle = angle;
            m_CharacterPosition.m_currentSpeed = currentSpeed;

            //Program.Display(GetIDString() + " Speed update = "+currentSpeed);
           
            /*float dirx = (float)Math.Sin((double)(m_CharacterPosition.m_yangle) * Math.PI / 180);
            float dirz = (float)Math.Cos((double)(m_CharacterPosition.m_yangle) * Math.PI / 180);
             m_CharacterPosition.m_direction = new Vector3(dirx, 0, dirz);*/

            m_CharacterPosition.m_direction = Utilities.GetDirectionFromYAngle(m_CharacterPosition.m_yangle); ;
            m_CharacterPosition.CorrectDirectionForAngle();
            m_CharacterPosition.m_direction.Normalize();
            if ((m_ConfirmedPosition.m_position - m_currentPosition.m_position).Length() > 20)
            {
                Program.Display("unexpectly large position correction for " + m_name + " from " + m_ConfirmedPosition.m_position + " to " + m_currentPosition.m_position);
            }

            m_ConfirmedPosition.m_position = m_CharacterPosition.m_position;
            m_QuestManager.checkPosition();
            if (m_CharacterPosition.m_currentSpeed > 0)
            {
                m_SentPosition.m_position = m_CharacterPosition.m_position + m_CharacterPosition.m_direction * m_CharacterPosition.m_currentSpeed * SENT_FORWARD_PROJECTION_LENGTH;
                m_ProjectedPosition.m_position = m_CharacterPosition.m_position + m_CharacterPosition.m_direction * m_CharacterPosition.m_currentSpeed * FORWARD_PROJECTION_LENGTH;
            }
            else
            {
                m_SentPosition.m_position = m_CharacterPosition.m_position;
                m_ProjectedPosition.m_position = m_CharacterPosition.m_position;
            }
            TheCharactersPath.AddPosition(m_ConfirmedPosition.m_position, Program.MainUpdateLoopStartTime());
            EntityPartitionCheck();
            // Program.Display("newpos="+m_ConfirmedPosition.m_position.ToString());
            //m_CharacterPosition.m_position = position + m_CharacterPosition.m_direction * (float)timeSinceSend * speed * m_CharacterPosition.m_percentageSpeed/100;

        }
        #endregion

        internal void ReadUpdatedSkillBarFromMessage(NetIncomingMessage msg)
        {

            //read the data
            //what set is it for
            int setPos = msg.ReadVariableInt32();
            //how many skills are in this update
            int arraySize = msg.ReadVariableInt32();
            //int[] newSkills = new int[arraySize];
            //bool changed = false;
            CharacterSlotSet currentSet = m_slotSetHolder.GetSet(setPos);
            for (int i = 0; i < arraySize; i++)
            {
                HudSlotItemType itemType = (HudSlotItemType)msg.ReadVariableInt32();
                int itemID = msg.ReadVariableInt32();
                if (currentSet != null)
                {
                    currentSet.SetSlot(i, itemType, itemID);
                    /* changed = true;
                     m_HudSlotItems[i].m_item_id = itemID;
                     m_HudSlotItems[i].m_slotItemType = itemType;*/
                }
            }
            if (currentSet != null && currentSet.Changed == true)
            {
                currentSet.SaveToDatabase(Program.processor.m_worldDB);
                //SaveSkillHudToDatabase();
            }
        }

        internal void SendHudUpdate()
        {
            NetOutgoingMessage hudMessage = Program.Server.CreateMessage();
            hudMessage.WriteVariableUInt32((uint)NetworkCommandType.ServerHUDUpdate);

            writeHudSlotsToMessage(hudMessage);


            //send
            Program.processor.SendMessage(hudMessage, m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.ServerHUDUpdate);

        }

        void WriteEquipmentIDToMessage(NetOutgoingMessage outmsg, int slotID)
        {

            Item equipment = m_inventory.GetEquipmentForSlot(slotID);

            if (equipment == null)
            {
                outmsg.WriteVariableInt32(-1);
                return;
            }

            outmsg.WriteVariableInt32(equipment.m_template_id);
            return;




        }
		
        public void writeFullCharacterInfoToMsg(NetOutgoingMessage outmsg)
        {
            // any changes to this method need to mirror on the client too 
			// method is paired in client-> NetworkManager.ReadFullCharacterData

            writeBasicCharacterInfoToMsg(outmsg);

			//our xp levels
            float expVal = getVisibleExperience(LevelType.none);
            outmsg.Write(expVal);            
            outmsg.WriteVariableInt32(getVisiblePVPExperience());
			//gathering xp
			outmsg.Write(getVisibleExperience(LevelType.fish));


            //abilities must be before stats so the modified values can be sent in stats data
            writeAbilitiesToMsg(outmsg);
            //skills must be before stats so the modified values can be sent in stats data
            WriteSkillsToMessage(outmsg);
            writeStatsToMessage(outmsg);

            outmsg.WriteVariableInt32(Level);
            outmsg.WriteVariableInt32(LevelFishing);

            //add cooking level and percentage progress
            outmsg.WriteVariableInt32(LevelCooking);
            outmsg.Write(getVisibleExperience(LevelType.cook));


            outmsg.WriteVariableInt32(m_inventory.m_coins);
            //write equipment
            m_inventory.WriteEquipmentToMessage(outmsg);
            //white inventory
            m_inventory.WriteInventoryToMessage(outmsg);
            //write the characters available skills
            //outmsg.Write(CreateStringForSkills());

            //write the current Skill Bar
            //skill bar
            writeHudSlotsToMessage(outmsg);
            WriteBaseStatsToMessage(outmsg);

            //write abilities to message



        }

        public void writeHudSlotsToMessage(NetOutgoingMessage outmsg)
        {
            outmsg.WriteVariableInt32(BASE_HUD_SLOTS + m_numberOfExtraHudSlots);
            //how many slots sets are there
            int totalSets = m_slotSetHolder.GetNumSets();
            outmsg.WriteVariableInt32(totalSets);
            //for each slot set
            for (int currentSetIndex = 0; currentSetIndex < totalSets; currentSetIndex++)
            {
                CharacterSlotSet currentSet = m_slotSetHolder.GetSetWithIndex(currentSetIndex);
                if (currentSet != null)
                {
                    //write the id
                    outmsg.WriteVariableInt32(currentSet.Position);
                    //write the capacity
                    outmsg.WriteVariableInt32(currentSet.Capacity);
                    //for each slot write the type and id
                    for (int currentSlotIndex = 0; currentSlotIndex < currentSet.Capacity; currentSlotIndex++)
                    {
                        HudSlotItem currentItem = currentSet.GetItemAtSlot(currentSlotIndex);
                        if (currentItem != null)
                        {
                            outmsg.WriteVariableInt32((int)currentItem.m_slotItemType);
                            outmsg.WriteVariableInt32(currentItem.m_item_id);

                        }
                        else
                        {
                            outmsg.WriteVariableInt32(-1);
                            outmsg.WriteVariableInt32(-1);

                        }
                    }
                }
                //write an empty set to do
                else
                {
                    outmsg.WriteVariableInt32(-1);
                    outmsg.WriteVariableInt32(0);
                }

            }
            /*outmsg.WriteVariableInt32(BASE_HUD_SLOTS+m_numberOfExtraHudSlots);
            outmsg.WriteVariableInt32(m_HudSlotItems.Length);
            for (int i = 0; i < m_HudSlotItems.Length; i++)
            {
                outmsg.WriteVariableInt32((int)m_HudSlotItems[i].m_slotItemType);
                outmsg.WriteVariableInt32(m_HudSlotItems[i].m_item_id);

            }*/
        }

        public void WriteBaseStatsToMessage(NetOutgoingMessage outmsg)
        {
            outmsg.WriteVariableInt32(m_baseVitality);
            outmsg.WriteVariableInt32(m_baseStrength);
            outmsg.WriteVariableInt32(m_baseFocus);
            outmsg.WriteVariableInt32(m_baseDexterity);

        }

        public void writeStatsToMessage(NetOutgoingMessage outmsg)
        {
			//Program.Display("Writing Stats. concentraion." + CurrentConcentration + " maxConcentraion." + MaxConcentration + " level." + LevelFishing);
            outmsg.Write((byte)1);//full stats
            outmsg.WriteVariableInt32(CurrentHealth);
            outmsg.WriteVariableInt32(MaxHealth);
            outmsg.WriteVariableInt32(CurrentEnergy);
            outmsg.WriteVariableInt32(MaxEnergy);
            outmsg.WriteVariableInt32(Attack);
            outmsg.WriteVariableInt32(Defence);
            outmsg.WriteVariableInt32(AttackSpeed);
            outmsg.WriteVariableInt32(TotalWeaponDamage);
            outmsg.WriteVariableInt32(ArmourValue);
            outmsg.WriteVariableInt32(Vitality);
            outmsg.WriteVariableInt32(Strength);
            outmsg.WriteVariableInt32(Focus);
            outmsg.WriteVariableInt32(Dexterity);
            outmsg.WriteVariableInt32(Encumbrance);
            outmsg.WriteVariableInt32(CompiledStats.SkillPoints);
            outmsg.WriteVariableInt32(CompiledStats.AttributePoints);
            
            outmsg.WriteVariableInt32(CurrentConcentrationFishing);
            outmsg.WriteVariableInt32(MaxConcentrationFishing);

            //write the resistances
            outmsg.WriteVariableInt32(CompiledStats.BonusTypes.Count);
            for (int i = 0; i < CompiledStats.BonusTypes.Count; i++)
            {
                FloatForID currentType = CompiledStats.BonusTypes[i];
                outmsg.WriteVariableInt32(currentType.m_bonusType);
                outmsg.WriteVariableInt32((int)currentType.m_amount);
                /*int totalBonus = GetBonusType(i);
                outmsg.WriteVariableInt32(totalBonus);*/
            }
            /*
            for (int i = 0; i < NUM_BONUS_TYPES; i++)
            {
                int totalBonus = GetBonusType(i);
                outmsg.WriteVariableInt32(totalBonus);
            }*/
            outmsg.Write(CompiledStats.MaxAttackRange);
            outmsg.WriteVariableInt32(CompiledStats.FastTravelItemLimit);
            outmsg.Write(MaxSpeed);
            outmsg.Write(Scale);
            writeCompiledAbilitiesToMessage(outmsg);
            writeAvoidancesToMessage(outmsg);
            WriteCompiledSkillsToTheList(outmsg);
            outmsg.WriteVariableInt32(CompiledStats.SoloBankSizeLimit);
            
        }

        public void WriteUpdatedStatsToMessage(NetOutgoingMessage outmsg)
        {
            m_compiledStats.BuildMessage(m_oldCompiledStats, outmsg, this);
        }
        
        /// <summary>
        /// remember the state that has been sent
        /// Do anything required to allow stats to be kept up to date
        /// </summary>
        public void CompiledStatsSent()
        {
            m_oldCompiledStats = m_compiledStats.Copy();
        }

        public void WriteSkillsToMessage(NetOutgoingMessage outmsg)
        {
            double currentTime = Program.MainUpdateLoopStartTime();//NetTime.Now;


            //message layout

            //number of skills

            //for each skill
            //skill number
            //skill level

            int numberOfSkills = m_EntitySkills.Count() + m_AdditionalSkill.Count();

            outmsg.WriteVariableInt32(numberOfSkills);

            for (int currentSkillIndex = 0; currentSkillIndex < m_EntitySkills.Count(); currentSkillIndex++)
            {
                EntitySkill currentSkill = m_EntitySkills[currentSkillIndex];

                SKILL_TYPE currentSkillID = currentSkill.SkillID;
                int currentSkillLvl = currentSkill.SkillLevel;
                int currentMaxLevel = currentSkill.MaxLevel;
                outmsg.WriteVariableInt32((int)currentSkillID);
                outmsg.WriteVariableInt32(currentSkillLvl);
                outmsg.WriteVariableInt32(currentMaxLevel);
                double timeSinceLastCast = currentTime - currentSkill.TimeLastCast;
                //check if it's finnished recharging
                double timeremaining = -1;
                double rechargeTime=0;
                SkillTemplateLevel skillLevel = currentSkill.getSkillTemplateLevel(false);
                if (skillLevel != null)
                {
                    rechargeTime = skillLevel.GetRechargeTime(currentSkill, false);
                }
                if (skillLevel != null && timeSinceLastCast < rechargeTime)
                    timeremaining = rechargeTime - (float)timeSinceLastCast;
                outmsg.Write((float)timeremaining);
            }

            for (int currentSkillIndex = 0; currentSkillIndex < m_AdditionalSkill.Count(); currentSkillIndex++)
            {
                EntitySkill currentSkill = m_AdditionalSkill[currentSkillIndex];

                SKILL_TYPE currentSkillID = currentSkill.SkillID;
                int currentSkillLvl = currentSkill.SkillLevel;
                int currentMaxLevel = currentSkill.MaxLevel;
                outmsg.WriteVariableInt32((int)currentSkillID);
                outmsg.WriteVariableInt32(currentSkillLvl);
                outmsg.WriteVariableInt32(currentMaxLevel);
                
                double timeSinceLastCast = currentTime - currentSkill.TimeLastCast;
                //check if it's finnished recharging
                double timeremaining = -1;
                double rechargeTime = 0;
                SkillTemplateLevel skillLevel = currentSkill.getSkillTemplateLevel(false);
                if (skillLevel != null)
                {
                    rechargeTime = skillLevel.GetRechargeTime(currentSkill, false);
                }
                if (skillLevel != null && timeSinceLastCast < rechargeTime)
                    timeremaining = rechargeTime - timeSinceLastCast;
                outmsg.Write((float)timeremaining);
            }
            //this will overide all modified levels client side,
            //if there are any modified skills in the last send, clear them and compile again
            //This will also cause the data to be sent
            if(m_oldCompiledStats!=null && m_oldCompiledStats.Skills.Count>0)
            {
                //as the new level has been sent it is as if no modified levels have been set
                m_oldCompiledStats.Skills.Clear();
                SetStatsChangeLevel(STATS_CHANGE_LEVEL.COMPILE_REQUIRED);
            }
        }

        void SendPermBuffMessage()
        {
            NetOutgoingMessage buffMessage = Program.Server.CreateMessage();
            buffMessage.WriteVariableUInt32((uint)NetworkCommandType.PermBuffUpdate);

            int numBuff = m_permanentBuff.Count();
            buffMessage.WriteVariableInt32(numBuff);

            for (int i = 0; i < numBuff; i++)
            {
                PermanentBuff currentBuff = m_permanentBuff[i];
                int currentBuffID = -1;
                int currentQuantity = -1;

                if (currentBuff != null)
                {
                    currentBuffID = (int)currentBuff.BuffID;
                    currentQuantity = currentBuff.BuffQuantity;
                }
                buffMessage.WriteVariableInt32(currentBuffID);
                buffMessage.WriteVariableInt32(currentQuantity);
            }
            Program.processor.SendMessage(buffMessage, m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.PermBuffUpdate);
        }

        void SendNewEntitiesOfInterestMessage()
        {
            NetOutgoingMessage interestMessage = Program.Server.CreateMessage();
            interestMessage.WriteVariableUInt32((uint)NetworkCommandType.NewEntitiesOfInterest);
            
            //remove nulls from interest list
            int howManNulls = 0;
            for (int i = 0; i < m_newToInterestList.Count; ++i)
            {
                if (m_newToInterestList[i] == null)
                    ++howManNulls;
            }
            
            int numentities = m_newToInterestList.Count - howManNulls;
            interestMessage.WriteVariableInt32(numentities);

            for (int i = 0; i < m_newToInterestList.Count; ++i)
            {
                //if null, something has gone wrong
                if (m_newToInterestList[i] == null)
                {
                    Program.Display("#HOTFIX Character.SendNewEntitiesOfInterestMessage entity is null. character name." + m_name);
                    continue;
                }
                CombatEntity currentEntity = m_newToInterestList[i];
                currentEntity.WriteDataRequiredByInterestedEntitiesToMessage(interestMessage);
            }
            Program.processor.SendMessage(interestMessage, m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.NewEntitiesOfInterest);

        }

        void SendEntitiesChangedCombatStatus()
        {
			

            NetOutgoingMessage interestMessage = Program.Server.CreateMessage();
            interestMessage.WriteVariableUInt32((uint)NetworkCommandType.EntityCombatStateChanged);

            int numentities = m_entitiesInCombatChanged.Count;
            interestMessage.WriteVariableInt32(numentities);
			

            for (int i = 0; i < m_entitiesInCombatChanged.Count; i++)
            {
                CombatEntity currentEntity = m_entitiesInCombatChanged[i];
                currentEntity.WriteInCombatDataToMessage(interestMessage);
            }
            Program.processor.SendMessage(interestMessage, m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.EntityCombatStateChanged);

        }
        
        #endregion

        #region Database
        public bool SetUpCharacterWithDetails(Database db, long account_id, uint character_id, string name)
        {


			//string sqlstr = "select * from character_details where character_id=" + character_id + " and account_id=" + account_id + " and name='" + name + "'";


			List<MySqlParameter> sqlParams = new List<MySqlParameter>();
			sqlParams.Add(new MySqlParameter("@character_id", character_id));
			sqlParams.Add(new MySqlParameter("@account_id", account_id));
			sqlParams.Add(new MySqlParameter("@name", name));

			string sqlstr = "select * from character_details where character_id=@character_id and account_id=@account_id and name=@name";

			SqlQuery query = new SqlQuery(db, sqlstr, sqlParams.ToArray());
            if (!query.HasRows)
            {
                query.Close();
                return false;
            }



            m_character_id = character_id;
            query.Read();
            m_db = db;
            readBasicfromDb(db, query);
            m_SoloBank.FillBagForCharacterID(db, m_character_id);
            readAbilitiesFromDB(query);
            ReadTeleportLocationsFromDatabase(query);
            readPermanentBuffsFromDB(query);
            SetUpPermBuffs();
            AddSkillsFromEquipment(false);
            
            //level
            //experience
            //strenth
            //focus
            //vitality
            //attribute_points
            //coins
            m_experience = query.GetInt64("xp");
            if (m_experience < 0)
            {
                int oldxp = query.GetInt32("experience");
                int level = query.GetInt32("level");
                int offset = (int)Math.Round(((oldxp - (level - 1) * 1000) / 1000.0f) * (level + 4) * (level * 20 + 80) * (level / 80.0f + 1));
                if (offset < 0)
                    offset = 0;
                m_experience = offset + getMinExperienceForNextLevel(level - 1);
                db.runCommandSync("update character_details set xp=" + m_experience + " where character_id=" + character_id);
            }

			// FISHING SUMMER
 			// Pull 'm_fishingExperience' from character_details
			// 'fishing_xp' does not exist yet
			m_fishingExperience = query.GetInt64("fishing_experience");
            m_db.runCommandSync("update character_details set fishing_experience=" + m_fishingExperience + " where character_id=" + m_character_id);

            /*m_baseStats.Strength = query.GetInt32("strength");
            m_baseStats.Dexterity = query.GetInt32("dexterity");
            m_baseStats.Vitality = query.GetInt32("vitality");
            m_baseStats.Focus = query.GetInt32("focus");*/
            m_baseStrength = query.GetInt32("strength");
            m_baseDexterity = query.GetInt32("dexterity");
            m_baseVitality = query.GetInt32("vitality");
            m_baseFocus = query.GetInt32("focus");
            //set the ones used to calculate stats
            UpdateBaseStats();
            m_CharacterPosition.m_position.X = query.GetFloat("xpos");
            m_CharacterPosition.m_position.Y = query.GetFloat("ypos");
            m_CharacterPosition.m_position.Z = query.GetFloat("zpos");
            m_ConfirmedPosition.m_position = m_CharacterPosition.m_position;
            m_CharacterPosition.m_yangle = query.GetFloat("yangle");
            m_inventory.m_coins = query.GetInt32("coins");

            m_lastMobUpdatePos = m_CharacterPosition.m_position;
            updateCombatStats(true);
            CompiledStats.SkillPoints = query.GetInt32("skill_points");
            CompiledStats.AttributePoints = query.GetInt32("attribute_points");
            m_InLimbo = true;
            m_pvpRating = query.GetDouble("pvp_rating");
            m_pvpLevel = query.GetInt32("pvp_level");
            CompiledStats.PVPExperience = query.GetInt32("pvp_xp");
            if (!query.isNull("last_logged_in"))
            {
                m_lastLoggedIn = query.GetDateTime("last_logged_in");
                
            }

            if (!query.isNull("last_daily_reward_recieved"))
            {
                m_lastRewardRecieved = query.GetDateTime("last_daily_reward_recieved");
            }
            m_nextDailyRewardStep = query.GetInt32("next_daily_reward_step");
            m_numRecievedRewards = query.GetInt32("daily_rewards_received");

            int currentHealth = query.GetInt32("current_health");
            int currentEnergy = query.GetInt32("current_energy");
            int currentConcentration = query.GetInt32("current_concentration_fishing");
            //read Skills
            LoadSkillEntities();
            SetAllMaxSkillLevels();


            string friendList = query.GetString("friends_list");
            string blockList = query.GetString("block_list");
            //get the clan ID
            int clanID = query.GetInt32("clan_id");
            //read skill hud
            string hudSlots = query.GetString("hud_slots");
            string outstandingEffects = query.GetString("outstanding_status_effects");

            query.Close();

            CharacterEffectManager.PopulateEffectsFromString(this, outstandingEffects);

            if (friendList.Length > 0)
            {
                ReadFriendsFromString(friendList, m_friendCharacterIDs);
                for (int i = 0; i < m_friendCharacterIDs.Count; i++)
                {
                    db.runCommandSync("insert into friend_list (character_id,other_character_id) values (" + m_character_id + "," + m_friendCharacterIDs[i].CharacterID + ")");
                }
                db.runCommandSync("update character_details set friends_list='' where character_id=" + m_character_id);
            }
            else
            {
                SqlQuery friendQuery = new SqlQuery(db, "select other_character_id from friend_list where character_id=" + m_character_id);
                while (friendQuery.Read())
                {
                    AddFriendToArray(friendQuery.GetInt32("other_character_id"), m_friendCharacterIDs);
                }
                friendQuery.Close();
            }
            if (blockList.Length > 0)
            {
                ReadFriendsFromString(blockList, m_blockList);
                for (int i = 0; i < m_blockList.Count; i++)
                {
                    db.runCommandSync("insert into block_list (character_id,other_character_id) values (" + m_character_id + "," + m_blockList[i].CharacterID + ")");
                }
                db.runCommandSync("update character_details set block_list='' where character_id=" + m_character_id);

            }
            else
            {
                SqlQuery blockedQuery = new SqlQuery(db, "select other_character_id from block_list where character_id=" + m_character_id);
                while (blockedQuery.Read())
                {
                    AddFriendToArray(blockedQuery.GetInt32("other_character_id"), m_blockList);
                }
                blockedQuery.Close();
            }
            m_characterMail.SetUp(this);

            m_charactersClan = Program.processor.GetClanWithID(clanID);
            //update the friend list with who is online
            RefreshFriendsList();
            //update the clan if there is one
            if (m_charactersClan != null)
            {
                if (m_lastLoggedIn < DateTime.Now.AddDays(-30))
                {
                    m_charactersClan.AddReturningMember(this, m_character_id);
                }
                bool isRecognised = m_charactersClan.MemberUpdate(this);
                if (isRecognised == false)
                {
                    m_charactersClan = null;
                }
            }
            //read skill hud

            //read the skill bar from the string 
            /*if (hudSlots.Length > 0)
            {
                string[] skillHudSkills = hudSlots.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < skillHudSkills.Length; i++)
                {
                    m_HudSlotItems[i] = new HudSlotItem(skillHudSkills[i]);
                }
            }*/
            int totalSlots = BASE_HUD_SLOTS + m_numberOfExtraHudSlots;
            m_slotSetHolder = new CharacterSlotSetHolder(character_id, db, totalSlots);

            m_QuestManager = new QuestManager(m_db, this, m_character_id);
            mCharacterBountyManager.SetUp(this);

            if (Program.m_SendAchievements!=UPDATE_ACHIEVEMENTS.NONE)
            {
                m_characterRankings = new RankingsManager(db, RankingsManager.RANKING_MANAGER_TYPE.CHARACTER_RANKINGS, "character_rankings", "character_id", (int)m_character_id);
            }
            if(Program.m_SendAchievements==UPDATE_ACHIEVEMENTS.ALL)
            {
                m_characterAchievements = new AchievementsManager(db, AchievementsManager.ACHIEVEMENT_MANAGER_TYPE.CHARACTER_ACHIEVEMENTS, "character_achievements", "character_id", (int)m_character_id);
            }
            CompileStats();

            CurrentHealth = currentHealth;
            CurrentEnergy = currentEnergy;
            CurrentConcentrationFishing = currentConcentration;
            
            if (CurrentHealth <= 0)
            {
                WasDeadOnLogin();
                //Died();
                //now used to send reset death value
                // m_timeTillRespawn = -1;
            }

            SqlQuery killsQuery = new SqlQuery(db, "select * from pvp_recent_kills where character_id=" + character_id + " and killDate>=CURRENT_DATE");
            while (killsQuery.Read())
            {
                PVPKillRecord newRecord = new PVPKillRecord();
                newRecord.m_victim_character_id = killsQuery.GetInt32("target_character_id");
                newRecord.m_num_kills = killsQuery.GetInt32("num_kills");
                newRecord.m_killDate = DateTime.Today;
                m_pvpKills.Add(newRecord);
            }
            killsQuery.Close();

            m_signpostManager.Setup(m_player.m_account_id,(int) m_character_id);

            if (SpecialOfferTemplate.SPECIAL_OFFERS_ACTIVE == true)
            {
                
                m_offerManager.LoadIndividualOffers(Program.processor.m_universalHubDB, this);
            }
            readCompletedTutorialsFromDatabase();
            readCompletedFirstTimeFromDatabase();

            //create the faction manager
            FactionManager = new FactionManager(this.m_db,this);

            CraftingManager = new CraftingManager(this.m_db,this);

            return true;
        }


        static public Character loadCharacter(Database db, Player player, long account_id, uint character_id, string name)
        {
            Character character = null;

            character = new Character(db, player);

            if (character.SetUpCharacterWithDetails(db, account_id, character_id, name) == false)
            {
                character = null;
            }
            
            return character;
        }
        static public FriendTemplate LoadCharacterStub(Database db,string characterName)
        {
            FriendTemplate recipient = null;

			//SqlQuery query = new SqlQuery(db, "select * from character_details where name='" + characterName + "' and deleted=false");

			List<MySqlParameter> sqlParams = new List<MySqlParameter>();
			sqlParams.Add(new MySqlParameter("@name", characterName));

			SqlQuery query = new SqlQuery(db, "select * from character_details where name=@name and deleted=false", sqlParams.ToArray());

			if (query.HasRows)
            {
                if (query.Read())
                {
                    int character_id = query.GetInt32("character_id");
                    //name
                    string name = query.GetString("name");

                    int zone = query.GetInt32("zone");
                    //level
                    int level = query.GetInt32("level");
                    //class
                    int characterClass = query.GetInt32("class_id");

                    int characterRace = query.GetInt32("race_id");
                    recipient = new FriendTemplate();
                    {
                        recipient.CharacterID = character_id;
                        recipient.CharacterName = name;
                        recipient.Level = level;
                        recipient.Online = false;
                        recipient.Zone = zone;
                        recipient.Class = characterClass;
                        recipient.Race = characterRace;
                        
                    }
                }
            }
            query.Close();

            return recipient;

        }
        #endregion //Database
        
        #region EXP

		/// <summary>
		/// What's the experience progress for our character, gathering.none is normal player xp
		/// </summary>
		/// <param name="type"></param>
		/// <returns>Returns the percentage of experience for the from range 0f-100f</returns>
        public float getVisibleExperience(LevelType type)
        {
            switch(type)
            {
                case LevelType.none:
                {
                    float experienceForLevel = getMinExperienceForNextLevel(Level - 1);
                    float experienceForNextLevel = getMinExperienceForNextLevel(Level);
                    if (experienceForLevel == experienceForNextLevel)
                    {
                        return 100;
                    }
                    else
                    {
                        return (100 * (m_experience - experienceForLevel)) / (experienceForNextLevel - experienceForLevel);
                    }
                }
                case LevelType.fish:
                {
                    float fishingXpForLevel = getMinProfessionExperienceForNextLevel(LevelFishing - 1);
                    float fishingXpForNextLevel = getMinProfessionExperienceForNextLevel(LevelFishing);
                    if (fishingXpForLevel == fishingXpForNextLevel)
                    {
                        return 100;
                    }
                    else
                    {
                        return (100 * (m_fishingExperience - fishingXpForLevel)) / (fishingXpForNextLevel - fishingXpForLevel);
                    }
                }
                case LevelType.cook:
                {
                    float cookingXpForLevel = getMinProfessionExperienceForNextLevel(LevelCooking - 1);
                    float cookingXpForNextLevel = getMinProfessionExperienceForNextLevel(LevelCooking);
                    if (cookingXpForLevel == cookingXpForNextLevel)
                    {
                        return 100;
                    }
                    else
                    {
                        return (100 * (CookingExperience - cookingXpForLevel)) / (cookingXpForNextLevel - cookingXpForLevel);
                    }
                }
                default:
                {
                    return 0.0f;
                }
            }
        }

        public float GetProfessionExperience()
        {
            float experienceForLevel = getMinProfessionExperienceForNextLevel(LevelCooking - 1);
            float experienceForNextLevel = getMinProfessionExperienceForNextLevel(LevelCooking);
            if (experienceForLevel == experienceForNextLevel)
            {
                return 100;
            }
            else
            {
                return (100 * (CookingExperience - experienceForLevel)) / (experienceForNextLevel - experienceForLevel);
            }
        }

        public int getVisiblePVPExperience()
        {
            Int64 experienceForLevel = getMinPVPExperienceForNextLevel(m_pvpLevel - 1);
            Int64 experienceForNextLevel = getMinPVPExperienceForNextLevel(m_pvpLevel);
            return (int)((100 * (CompiledStats.PVPExperience - experienceForLevel)) / (experienceForNextLevel - experienceForLevel));

        }
        static internal Int64 getMinProfessionExperienceForNextLevel(int level)
        {
            if (level < 0)
                level = 1;
            if (level < Program.MAX_PROFESSION_LEVEL)
            {
                return Program.m_professionLevelRequirements[level];
            }
            else
            {
                return Program.m_professionLevelRequirements[Program.MAX_PROFESSION_LEVEL - 1];
            }
        }
        static internal Int64 getMinExperienceForNextLevel(int level)
        {
	        if (level < 0)
		        level = 1;
            if (level < Program.MAX_PVE_LEVEL)
            {
                return Program.m_levelRequirements[level];
            }
            else
            {
                return Program.m_levelRequirements[Program.MAX_PVE_LEVEL - 1];
            }
        }
        static internal Int64 getMinPVPExperienceForNextLevel(int level)
        {

            return Program.m_pvpLevelRequirements[level];

        }
        #endregion EXP
        
		#region trade
        /// <summary>
        /// Some actions can not be innitiated while another action or request is in effect
        /// </summary>
        /// <returns></returns>
        internal bool CanTakeRequest()
        {
            if (PlayerIsBusy)
            {
                // if player is in shop then cannot take request!
                return false;
            }

            bool canTakeRequest = true;

            //if a request is currently active then it cannot accept another one
            if (m_pendingRequest != null)
            {
                canTakeRequest = false;
            }

            //can't take another request while trading
            if (m_tradingWith != null)
            {
                canTakeRequest = false;
            }


            //can't take another request while in a duel
            if (m_currentDuelTarget != null)
            {
                canTakeRequest = false;
            }

            return canTakeRequest;
        }


        public void writePositionInfo(NetOutgoingMessage outmsg)
        {
            outmsg.Write((float)m_CharacterPosition.m_position.X);
            outmsg.Write((float)m_CharacterPosition.m_position.Y);
            outmsg.Write((float)m_CharacterPosition.m_position.Z);
            outmsg.Write(m_CharacterPosition.m_yangle);


        }

        internal void initTrade(Player otherPlayer)
        {
            string playertradeString = m_tradingInventory.WriteInventoryToString();
            Program.Display(Name + " trade inventory before trade : C = " + m_tradingInventory.m_coins + " | Items (inventoryID,TemplateID,quantity;)= " + playertradeString);

            m_tradingWith = otherPlayer;
            m_tradeReady = false;
            TradeAccepted = false;
            m_inventory.MergeInventory(m_tradingInventory);
            m_pendingRequest = null;

        }

        internal void cancelTrade()
        {
            m_isTradeInitator = false;
            m_tradingWith = null;
            m_tradeReady = false;
            TradeAccepted = false;
            m_inventory.MergeInventory(m_tradingInventory);

            //if all the connections are still up then send the merged inventory
            if (m_player != null && m_player.connection != null)
            {
                m_inventory.SendInventoryUpdate();
            }
        }

        internal string MoveTradeItem(DestinationBucket destBucket, int templateID, int inventoryID, int amount)
        {

            if (destBucket == DestinationBucket.Bucket_CharacterTradingInventory)
            {
                Item item = m_inventory.findBagItemByInventoryID(inventoryID, templateID);
                if (item == null)
                {
					return Localiser.GetString(textDB, m_player, (int)CharacterTextDB.TextID.ILLEGAL_TRADE);
                }
                else if (item.m_template.m_noTrade)
                {
					return Localiser.GetString(textDB, m_player, (int)CharacterTextDB.TextID.NO_TRADE);
                }
                else if (item.m_bound == true)
                {
					return Localiser.GetString(textDB, m_player, (int)CharacterTextDB.TextID.ITEM_BOUND);
                }
                else if (m_tradingInventory.m_bagItems.Count > 15)
                {
					return Localiser.GetString(textDB, m_player, (int)CharacterTextDB.TextID.TOO_MANY_ITEMS);
                }
                else
                {
                    if (amount >= item.m_quantity)
                    {

                        //m_tradingInventory.m_bagItems.Add(item);
                        m_tradingInventory.GetItemFromOtherInventory(item, m_inventory);
                        m_inventory.m_bagItems.Remove(item);
                    }
                    else
                    {
                        m_tradingInventory.AddNewItemToCharacterInventory(templateID, amount, false);
                        m_inventory.DeleteItem(item.m_template_id, item.m_inventory_id, amount);
                    }
                    return "";
                }
            }
            else
            {
                Item item = m_tradingInventory.findBagItemByInventoryID(inventoryID, templateID);
                if (item == null)
                {
					return Localiser.GetString(textDB, m_player, (int)CharacterTextDB.TextID.ILLEGAL_TRADE);
                }
                else
                {
                    //m_tradingInventory.m_bagItems.Remove(item);
                    //m_inventory.m_bagItems.Add(item);
                    if (amount >= item.m_quantity)
                    {
                        //
                        //m_inventory.m_bagItems.Add(item);
                        m_inventory.GetItemFromOtherInventory(item, m_tradingInventory);
                        m_tradingInventory.m_bagItems.Remove(item);
                    }
                    else
                    {
                        m_inventory.AddNewItemToCharacterInventory(templateID, amount, false);
                        m_tradingInventory.DeleteItem(item.m_template_id, item.m_inventory_id, amount);
                    }
                    //m_inventory.GetItemFromOtherInventory(item, m_tradingInventory);
                    //m_tradingInventory.m_bagItems.Remove(item);
                    return "";
                }
            }
        }

        internal string setTradeMoney(int amount)
        {
            int totalmoney = m_inventory.m_coins + m_tradingInventory.m_coins;
            if (amount > totalmoney)
            {
				return Localiser.GetString(textDB, m_player, (int)CharacterTextDB.TextID.ILLEGAL_TRADE);
            }
            else if (amount < 0)
            {
                Program.Display("*GH20130801* " + GetIDString() + " tried to Trade " + amount + " coins ");
				return Localiser.GetString(textDB, m_player, (int)CharacterTextDB.TextID.ILLEGAL_TRADE);
            }
            else
            {
                m_inventory.m_coins = totalmoney - amount;
                m_tradingInventory.m_coins = amount;
                return "";
            }
        }

        internal void completeTrade()
        {           

            Inventory othersMainInventory = m_tradingWith.m_activeCharacter.m_inventory;
            Inventory othersTradeInventory = m_tradingWith.m_activeCharacter.m_tradingInventory;

            if (m_tradingInventory.m_bagItems.Count > 0 || othersTradeInventory.m_bagItems.Count > 0)
            {
                increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.TRADER, 1);
                m_tradingWith.m_activeCharacter.increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.TRADER, 1);
            }
            int new_id = Program.processor.getAvailableTradeHistoryID();
            m_db.runCommandSync("update trade_history set trade_date='" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "' ,character1_id=" + m_character_id + ",character1_gold=" + m_tradingInventory.m_coins + ",character2_id=" + m_tradingWith.m_activeCharacter.m_character_id + ",character2_gold=" + othersTradeInventory.m_coins + " where trade_history_id=" + new_id);


            string recordstr = "";
            for (int i = 0; i < m_tradingInventory.m_bagItems.Count; i++)
            {
                recordstr += ",(" + new_id + "," + m_character_id + "," + m_tradingInventory.m_bagItems[i].m_inventory_id + "," + m_tradingInventory.m_bagItems[i].m_template_id + "," + m_tradingInventory.m_bagItems[i].m_quantity + ")";
            }
            for (int i = 0; i < othersTradeInventory.m_bagItems.Count; i++)
            {
                recordstr += ",(" + new_id + "," + m_tradingWith.m_activeCharacter.m_character_id + "," + othersTradeInventory.m_bagItems[i].m_inventory_id + "," + othersTradeInventory.m_bagItems[i].m_template_id + "," + othersTradeInventory.m_bagItems[i].m_quantity + ")";
            }
            if (recordstr.Length > 0)
            {
                m_db.runCommandSync("insert into trade_history_items (trade_history_id,character_id,inventory_id,item_id,amount) values " + recordstr.Substring(1));
            }

            m_inventory.MergeInventory(othersTradeInventory);
            othersMainInventory.MergeInventory(m_tradingInventory);
            m_tradingWith.m_activeCharacter.m_tradingWith = null;
            m_tradingWith.m_activeCharacter.m_tradeReady = false;
            m_tradingWith.m_activeCharacter.TradeAccepted = false;
            m_tradingWith.m_activeCharacter.m_isTradeInitator = false;
            m_tradingWith = null;
            m_tradeReady = false;
            TradeAccepted = false;
            m_isTradeInitator = false;

        }

        #endregion
        
        internal void saveCoins(Inventory in_inventory, uint character_id)
        {
            m_db.runCommandSync("update character_details set coins=" + in_inventory.m_coins + " where character_id=" + character_id);
        }

        internal void transferOwnership(Item item, uint newplayer)
        {
            if (item.m_inventory_id > 0)
            {
                item.IsFavourite = false;
                m_db.runCommandSync("update inventory set character_id=" + newplayer + ", is_favourite=" + Convert.ToByte(0) + " where inventory_id=" + item.m_inventory_id);
            }
        }

		/// <summary>
		/// Level up the player for this given type, GatheringType.none is normal leveling
		/// </summary>
		/// <param name="gathering"></param>
        void LevelUp(LevelType gathering)
        {
            switch(gathering)
            {
                case LevelType.none:
                {
                    Level += 1;
                    m_db.runCommandSync("update character_details set level=" + Level + " where character_id=" + m_character_id);
                    CompiledStats.SkillPoints += 1;
                    int maxAttributePoints = ((Level - 1) * 5 + m_race.m_starting_strength + m_race.m_starting_vitality + m_race.m_starting_focus + m_race.m_starting_dexterity);
                    int newRemainingPoints = maxAttributePoints - m_baseStrength - m_baseFocus - m_baseDexterity - m_baseVitality;
                    CompiledStats.AttributePoints = newRemainingPoints;

                    m_db.runCommandSync("update character_details set skill_points=" + CompiledStats.SkillPoints + " where character_id=" + m_character_id);
                    m_db.runCommandSync("update character_details set attribute_points=" + CompiledStats.AttributePoints + " where character_id=" + m_character_id);
                    break;
                }
                case LevelType.fish:
                {
                    LevelFishing += 1;
                    m_db.runCommandSync("update character_details set fishing_level=" + LevelFishing + " where character_id=" + m_character_id);
                    updateRanking(RankingsManager.RANKING_TYPE.PLAYER_LEVEL_FISHING, LevelFishing, false);
                    break;
                }
                case LevelType.cook:
                {
                    LevelCooking += 1;
                    m_db.runCommandSync("update character_details set cooking_level=" + LevelCooking + " where character_id=" + m_character_id);
                    updateRanking(RankingsManager.RANKING_TYPE.PLAYER_LEVEL_COOKING, LevelCooking, false);
                    break;
                }

            }

            //send levelling stuff
            CurrentEnergy = MaxEnergy;
            CurrentHealth = MaxHealth;
			
			//we need to perform a quick calculation for the new max concentration - this is usually done
			//in the compileStats() method but we don't want to wait for that update.  Instead we want
			//the new value now so we can correctly fill the concentration bar.
			CompiledStats.MaxConcentrationFishing = this.CalculateMaxConcentration() + EquipStats.MaxConcentrationFishing + StatusStats.MaxConcentrationFishing;
			CurrentConcentrationFishing = MaxConcentrationFishing;
		

            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.LevelUp);
            outmsg.WriteVariableInt32((int)gathering);

		    if (gathering == LevelType.none)
		    {
		        outmsg.WriteVariableInt32(Level);
		    }
            else if (gathering == LevelType.fish)
            {
                outmsg.WriteVariableInt32(LevelFishing);
            }
            else if (gathering == LevelType.cook)
            {
                outmsg.WriteVariableInt32(LevelCooking);
            }

		    
            
            m_QuestManager.writeAvailableQuestsToMessage(outmsg);
            Program.processor.SendMessage(outmsg, m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.LevelUp);
            SetStatsChangeLevel(STATS_CHANGE_LEVEL.COMPILE_REQUIRED);//m_statsUpdated=true;
            InfoUpdated(Inventory.EQUIP_SLOT.SLOT_LEVEL);
            UpdateSocialLists();

			//inform party, but only for normal levelling up
            if (m_party != null && gathering == LevelType.none)
            {
                m_party.RecalculateMaxLevel();
				string locText = Localiser.GetString(textDB, m_player, (int)CharacterTextDB.TextID.REACHED_LEVEL);
				locText = string.Format(locText, m_name, Level);
				m_party.SendPartySystemMessage(locText, this, false, SYSTEM_MESSAGE_TYPE.PARTY, false);
            }

            updateRanking(RankingsManager.RANKING_TYPE.PLAYER_LEVEL, Level,false);
            SetAllMaxSkillLevels();
            SendSkillListUpdate();

            CharacterBountyManager.RerollBounties();
            
            
            if (Program.m_LogAnalytics)
            {
                var logAnalytics = new AnalyticsMain(true);

                //log a normal or fishing level up event
                switch (gathering)
                {
                    case LevelType.none:                        
                            logAnalytics.levelUp(m_player, "characterLevel", CompiledStats.AttributePoints);
                            break;                        
                    case LevelType.fish:                        
                            logAnalytics.levelUp(m_player, "fishingLevel", CompiledStats.AttributePoints);
                            break;                        
                    default:                        
                            logAnalytics.levelUp(m_player, "unkownLevelUpName", CompiledStats.AttributePoints);
                            break;                        
                }
            }

            SignpostsNeedRechecked();
        }
        

        internal void PVPLevelUp()
        {
            m_pvpLevel += 1;
            //send levelling stuff
            m_db.runCommandSync("update character_details set pvp_level=" + m_pvpLevel + " where character_id=" + m_character_id);
        }
        void UpdatePVPLevelAchievements()
        {
            switch (m_pvpLevel)
            {
                case 5:
                    {
                        increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.PVP_SOLDIER, 1);
                        break;
                    }
                case 10:
                    {
                        increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.PVP_SERGEANT, 1);
                        break;
                    }
                case 15:
                    {
                        increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.PVP_LIEUTENANT, 1);
                        break;
                    }
                case 20:
                    {
                        increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.PVP_MARSHAL, 1);
                        break;
                    }
                case 25:
                    {
                        increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.PVP_WARDEN, 1);
                        break;
                    }
                case 29:
                    {
                        increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.PVP_LORD_COMMANDER, 1);
                        break;
                    }
            }
        }
        void UpdatePVPRatingAchievements()
        {
            int visableRating  = getVisiblePVPRating();

            switch (visableRating)
            {
               /* case 4:
                    {
                        increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.PVP_AVERAGE, 1);
                        break;
                    }*/
                case 5:
                    {
                        increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.PVP_COMPETANT, 1);
                        break;
                    }
                case 6:
                    {
                        increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.PVP_SKILLFUL, 1);
                        break;
                    }
                case 7:
                    {
                        increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.PVP_POWERFUL, 1);
                        break;
                    }
                case 8:
                    {
                        increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.PVP_FORMIDABLE, 1);
                        break;
                    }
                case 9:
                    {
                        increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.PVP_MIGHTY, 1);
                        break;
                    }
                case 10:
                    {
                        increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.PVP_DEADLY, 1);
                        break;
                    }
                case 11:
                    {
                        increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.PVP_DEVASTATING, 1);
                        break;
                    }
                case 12:
                    {
                        increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.PVP_INVINCIBLE, 1);
                        break;
                    }
            }
        }
        internal void UpdateSkillPoints()
        {
            m_db.runCommandSync("update character_details set skill_points=" + CompiledStats.SkillPoints + " where character_id=" + m_character_id);

        }
        
        internal int UpgradeSkillLevel(SKILL_TYPE skillNumber, int newSkillLevel)
        {
            //Get the skill involved
            EntitySkill skillToUpgrade = GetEnitySkillForID(skillNumber, true);
            if (skillToUpgrade == null)
            {
                return 0;
            }
            //can they go up another skill level
            else if (newSkillLevel > skillToUpgrade.MaxLevel)
            {
                return 0;
            }

            //check there are enough Skill Points
            if (CompiledStats.SkillPoints >= 1)
            {
                //check the prerequisite skill is owned


                //int newskillLevel = skillToUpgrade.SkillLevel + 1;


                //try to add the skill
                SkillTemplateLevel skillLevel = skillToUpgrade.Template.getSkillTemplateLevel(newSkillLevel, false);
                if (skillLevel != null && skillLevel.MinLevel <= Level)
                {

                    if (skillToUpgrade.SkillLevel != skillToUpgrade.ModifiedLevel|| skillToUpgrade.SkillAugments.Count>0)
                    {
                        SetStatsChangeLevel(STATS_CHANGE_LEVEL.COMPILE_REQUIRED);
                    }

                    int currLevel = skillToUpgrade.SkillLevel;
                    if (Program.m_LogAnalytics)
                    {
                        int skillID = (int)skillToUpgrade.SkillID;
                        string skillName = skillToUpgrade.Template.SkillName;
                        

                        AnalyticsMain logAnalytics = new AnalyticsMain(true);
                        logAnalytics.skillUpgraded(m_player, skillID.ToString(), skillName, currLevel, newSkillLevel);
                    }

                    skillToUpgrade.SkillLevel = newSkillLevel;
                    //remove the correct number of skill points
                    CompiledStats.SkillPoints -= (newSkillLevel - currLevel);
                    UpdateSkillPoints();

                    //set the database
                    m_db.runCommandSync("update character_skills set skill_level=" + newSkillLevel + " where character_id=" + m_character_id + " and skill_id=" + (int)skillNumber);
                }
            }
            else
            {
                return 0;
            }
            SendSkillListUpdate();
            SetStatsChangeLevel(STATS_CHANGE_LEVEL.BASIC_CHANGED);//m_statsUpdated=true;
            return 1;
        }
        internal override void CancelNextSkill()
        {
            //if there was a skill pending send the cancellation
            if (NextSkill != null)
            {
                SendSkillUpdate((int)NextSkill.Template.SkillID, NextSkill.SkillLevel, 0.0f);
                NextSkill = null;
                NextSkillTarget = null;
            }
            base.CancelNextSkill();
        }
        public void SendBuySkillResponse(int newSkill)
        {
            NetOutgoingMessage buySkillReply = Program.Server.CreateMessage();
            buySkillReply.WriteVariableUInt32((uint)NetworkCommandType.BuySkillReply);
            //write remaining skill points
            buySkillReply.WriteVariableInt32(CompiledStats.SkillPoints);
            //write the skill learned
            buySkillReply.WriteVariableInt32(newSkill);
            //write skills
            WriteSkillsToMessage(buySkillReply);
            //buySkillReply.Write(player.m_activeCharacter.CreateStringForSkills());
            Program.processor.SendMessage(buySkillReply, m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.BuySkillReply);

        }

        public void SendLearnRecipeResponse(int recipeID)
        {
            NetOutgoingMessage learnRecipeReply = Program.Server.CreateMessage();
            learnRecipeReply.WriteVariableUInt32((uint) NetworkCommandType.LearnRecipeReply);
            //Write the recipe learned
            learnRecipeReply.WriteVariableInt32(recipeID);
            Program.processor.SendMessage(learnRecipeReply,m_player.connection,NetDeliveryMethod.ReliableOrdered,NetMessageChannel.NMC_Normal,NetworkCommandType.LearnRecipeReply);
        }

        private void SendSkillListUpdate()
        {
            NetOutgoingMessage buySkillReply = Program.Server.CreateMessage();
            buySkillReply.WriteVariableUInt32((uint)NetworkCommandType.SkillListUpdate);
            //write remaining skill points
            buySkillReply.WriteVariableInt32(CompiledStats.SkillPoints);
            //write skills
            WriteSkillsToMessage(buySkillReply);
            //buySkillReply.Write(player.m_activeCharacter.CreateStringForSkills());
            Program.processor.SendMessage(buySkillReply, m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.SkillListUpdate);

        }
        
        // FISHING SUMMER //
        // Fish case needs its own comparison variable - using 'MAX_PVE_LEVEL' just now
        internal bool CanLevelUp(LevelType type)
        {      
            switch(type)
            {
                case LevelType.fish:
                {
                    return (LevelFishing < Program.MAX_PROFESSION_LEVEL);
                }
                case LevelType.none:
                {
                    return (Level < Program.MAX_PVE_LEVEL);
                }
                case LevelType.cook:
                {
                    return (LevelCooking < Program.MAX_PROFESSION_LEVEL);
                }
                default:
                {
	                return false;
                }
            }
        }
        
        internal int updateCoinsAndXP(int coinAmount, int experience, LevelType gathering)
        {

            // Deal with coins
            m_inventory.m_coins += coinAmount;
            if (coinAmount > 0)
            {
                //not sure this is still in use
                increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.GOLDEN, coinAmount);
            }


            // Deal with experience
            int maxXP;
            switch (gathering)
            {
                // FISHING SUMMER //
                // Again fishing requires its own experience variables 
                case LevelType.fish:
                    {
                        maxXP = (int)(getMinProfessionExperienceForNextLevel(LevelFishing + 1) - getMinProfessionExperienceForNextLevel(LevelFishing));
                        if (experience > maxXP)
                        {
                            experience = maxXP;
                        }
                        m_fishingExperience += experience;
                        if (m_fishingExperience >= getMinProfessionExperienceForNextLevel(LevelFishing) && CanLevelUp(gathering))
                        {
                            LevelUp(gathering);
                        }
                        m_db.runCommandSync("update character_details set coins=" + m_inventory.m_coins + ",fishing_experience=" + m_fishingExperience + " where character_id=" + m_character_id);
                        break;
                    }
                case LevelType.none:
                    {
                        maxXP = (int)(getMinExperienceForNextLevel(Level + 1) - getMinExperienceForNextLevel(Level));
                        if (experience > maxXP)
                        {
                            experience = maxXP;
                        }
                        m_experience += experience;
                        if (m_experience >= getMinExperienceForNextLevel(Level) && CanLevelUp(gathering))
                        {
                            LevelUp(gathering);
                        }
                        m_db.runCommandSync("update character_details set coins=" + m_inventory.m_coins + ",xp=" + m_experience + " where character_id=" + m_character_id);
                        break;
                    }
                case LevelType.cook:
                    {
                        maxXP = (int)(getMinProfessionExperienceForNextLevel(LevelCooking + 1) - getMinProfessionExperienceForNextLevel(LevelCooking));
                        if (experience > maxXP)
                        {
                            experience = maxXP;
                        }
                        CookingExperience += experience;
                        if (CookingExperience >= getMinProfessionExperienceForNextLevel(LevelCooking) && CanLevelUp(gathering))
                        {
                            LevelUp(gathering);
                        }
                        m_db.runCommandSync("update character_details set coins=" + m_inventory.m_coins + ",cooking_experience=" + CookingExperience + " where character_id=" + m_character_id);
                        break;
                    }
            }

            return experience;
        }

        internal void updateCoins(int coinAmount)
        {
            m_inventory.m_coins += coinAmount;
            m_db.runCommandSync("update character_details set coins=" + m_inventory.m_coins + " where character_id=" + m_character_id);
            if (coinAmount > 0)
            {
                increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.GOLDEN, coinAmount);
            }
        }

        void ValidatePosition()
        {
            //is there an error in the position?
            if (double.IsNaN(m_ConfirmedPosition.m_position.X) == true || double.IsNaN(m_ConfirmedPosition.m_position.Y) == true || double.IsNaN(m_ConfirmedPosition.m_position.Z) == true)
            {
                bool dataChanged = false;
                //if so try to get the zone
                if (m_zone != null)
                {
                    //find the first player spawn point
                    //and move the character there
                    PlayerSpawnPoint resetPoint = m_zone.GetDefaultSpawnPoint();
                    if (resetPoint != null)
                    {
                        m_ConfirmedPosition.m_position = resetPoint.Position;
                        dataChanged = true;
                    }
                }
                    //if the data could not be set go to 0,0,0
                if(dataChanged==false)
                {
                    m_ConfirmedPosition.m_position = new Vector3(0.0f);
                    dataChanged = true;
                }

            }
        }
        internal void SaveKeyData()
        {
            string statusEffects = CharacterEffectManager.CreateStringForEffects(this);
            if (m_tradingInventory != null && m_tradingInventory.IsEmpty() == false)
            {
                Program.Display("trading inventory merged in SaveKeyData for character" + Name);
                m_inventory.MergeInventory(m_tradingInventory);
                m_tradeReady = false;
            }
            SaveOutstandingSkills();
            //check the position is valid
            ValidatePosition();
            //   m_CharacterPosition.m_yangle = 0;
            Program.Display(m_name + " conf pos (" + m_ConfirmedPosition.m_position.X.ToString("f2") + "," + m_ConfirmedPosition.m_position.Y.ToString("f2") + "," + m_ConfirmedPosition.m_position.Z.ToString("f2") + ") est pos (" + m_CharacterPosition.m_position.X.ToString("f2") + "," + m_CharacterPosition.m_position.Y.ToString("f2") + "," + m_CharacterPosition.m_position.Z.ToString("f2") + ")");
            m_db.runCommandSync("update character_details set current_health=" + CurrentHealth + ",current_energy=" + CurrentEnergy +",current_concentration_fishing=" + CurrentConcentrationFishing + ",xpos=" + m_ConfirmedPosition.m_position.X + ",ypos=" + m_ConfirmedPosition.m_position.Y + ",zpos=" + m_ConfirmedPosition.m_position.Z + ",yangle=" + m_CharacterPosition.m_yangle + ",outstanding_status_effects='" + statusEffects + "', last_logged_in ='" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "' where character_id=" + m_character_id);
            
            if (m_characterAchievements != null)
                m_characterAchievements.saveAchievements();
            if (m_characterRankings != null)
                m_characterRankings.saveRankings();
            m_db.runCommandSync("delete from pvp_recent_kills where character_id=" + m_character_id);

            string recordstr="";

            for (int i = 0; i < m_pvpKills.Count; i++)
            {
                if(m_pvpKills[i].m_killDate>=DateTime.Today)
                {
                    recordstr += ",(" + m_character_id + "," + m_pvpKills[i].m_victim_character_id + "," + m_pvpKills[i].m_num_kills + ",'" + m_pvpKills[i].m_killDate.ToString("yyyy-MM-dd 00:00:00") + "')";
                }
            }
            if(recordstr.Length>0)
            {
                m_db.runCommandSync("insert into pvp_recent_kills (character_id,target_character_id,num_kills,killDate) values "+recordstr.Substring(1));
            }

            if (Program.m_LogAnalytics)
            {
                AnalyticsMain logAnalytics = new AnalyticsMain(true);
                logAnalytics.stoppedPlaying(m_player,m_reasonForExit);
            }
          
        }
        internal void HealForLoggedOutTime()
        {
            //if the player is dead then no healing for you
            if (CurrentHealth <= 0)
            {
                return;
            }

            TimeSpan LoggedOutTime = DateTime.Now - m_lastLoggedIn;

            if (LoggedOutTime.TotalMinutes > DamageCalculator.FULL_HEAL_LOGOUT_TIME)
            {
                CurrentHealth = MaxHealth;
                CurrentEnergy = MaxEnergy;
                CurrentConcentrationFishing = MaxConcentrationFishing;
                Program.DisplayDelayed(GetIDString() + " fully healed to CurrentHealth=" + CurrentHealth + " CurrentEnergy=" + CurrentEnergy + " CurrentConcentration= "+CurrentConcentrationFishing);
            }
            else
            {
                float numTicks = (float)(LoggedOutTime.TotalSeconds / RegenTime);

                int healthRegen = (int)Math.Floor(HW_BASE_HEALTH_REGEN * numTicks);
                int energyRegen = (int)Math.Floor(HW_BASE_ENERGY_REGEN * numTicks);
                int concentrationRegen = (int) Math.Floor(HW_BASE_CONCENTRATION_REGEN * numTicks);
                if (CurrentHealth > 0 && CurrentHealth < MaxHealth)
                {
                    CurrentHealth += healthRegen;
                    if (CurrentHealth > MaxHealth)
                    {
                        CurrentHealth = MaxHealth;

                    }
                }
                if (CurrentHealth > 0 && CurrentEnergy < MaxEnergy)
                {
                    CurrentEnergy += energyRegen;
                    if (CurrentEnergy > MaxEnergy)
                    {
                        CurrentEnergy = MaxEnergy;
                    }
                }
                if (CurrentHealth > 0 && CurrentConcentrationFishing < MaxConcentrationFishing)
                {
                    CurrentConcentrationFishing += concentrationRegen;
                    if (CurrentConcentrationFishing > MaxConcentrationFishing)
                    {
                        CurrentConcentrationFishing = MaxConcentrationFishing;
                    }
                }
                Program.DisplayDelayed(GetIDString() + " healed to CurrentHealth=" + CurrentHealth + " CurrentEnergy=" + CurrentEnergy + ", healthRegen = " + healthRegen +", concentration = "+CurrentConcentrationFishing + ", energyRegen = "+energyRegen +" number of ticks ="+numTicks);
            }
        }

        internal void saveNewZone()
        {
            m_db.runCommandSync("update character_details set zone=" + m_zone.m_zone_id + ",xpos=" + m_ConfirmedPosition.m_position.X + ",ypos=" + m_ConfirmedPosition.m_position.Y + ",zpos=" + m_ConfirmedPosition.m_position.Z + ",yangle=" + m_CharacterPosition.m_yangle + " where character_id=" + m_character_id);
        }

        internal void sendStatsUpdate()
        {
            updateCombatStats(false);
            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.StatsUpdate);
            writeStatsToMessage(outmsg);
            Program.processor.SendMessage(outmsg, m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.StatsUpdate);
            m_oldCompiledStats = CompiledStats.Copy();
        }
        //WriteBaseStatsToMessage
        internal void sendBaseStatsUpdate()
        {
            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.BaseStats);
            WriteBaseStatsToMessage(outmsg);
            Program.processor.SendMessage(outmsg, m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.BaseStats);

        }

        #region IEntitySocialStanding Members

        public int GetOpinionFor(IEntitySocialStanding theEntity)
        {
            //if they are of the character faction then they are an Ally
            if (theEntity.GetFactionID() == characterFactionID)
                return 100;
            //if not a character ask the npc for it's opinion of you instead
            return theEntity.GetOpinionFor(this);
        }

        public int GetFactionID()
        {
            return characterFactionID;
        }

        public int GetFactionStanding(int factionID)
        {
            for (int i = 0; i < m_factionStandings.Count; i++)
            {
                if (m_factionStandings[i].FactionID == factionID)
                {
                    return m_factionStandings[i].FactionPoints;
                }
            }
            return 0;
        }

        public bool WithinPartyWith(IEntitySocialStanding theEntity)
        {
            //entities are in their parties
            if (theEntity == this)
            {
                return true;
            }
            //currently no parties
            return false;
        }



        #endregion

		#region attributes & abilities

		internal void updateAttributes(uint addStr, uint addDex, uint addSta, uint addVit, uint remainingPoints)
        {
			//we perform a few checks here to check for valid points data

			//make sure what we spent & have remaining equals points available
			//e.g. +1str +1dex 0sta 0vit 8rem = 10 points
	        if (addStr + addDex + addSta + addVit + remainingPoints != CompiledStats.AttributePoints)
	        {		        
				return;
	        }

			//next check what the total available points would be for this class.  Make sure we are commiting valid values for our level
			//this can crop up if we are adding values in sql
	        uint totalAvailable=(uint)((Level-1)*5+m_race.m_starting_strength+m_race.m_starting_vitality+m_race.m_starting_focus+m_race.m_starting_dexterity);
            if (addStr > totalAvailable || addSta > totalAvailable || addVit > totalAvailable || addDex > totalAvailable)
                return;			
            int newRemainingPoints = (int)(totalAvailable - m_baseStrength - m_baseFocus - m_baseDexterity - m_baseVitality-addStr-addSta-addDex-addVit);
            if (newRemainingPoints < 0 || newRemainingPoints > 1000000)
                return;

			//checks passed ok so upgrade
            if (Program.m_LogAnalytics)
            {
                AnalyticsMain logAnalytics = new AnalyticsMain(true);
                logAnalytics.statUpgraded(m_player, m_baseStrength, (int)addStr, m_baseFocus, (int)addSta, m_baseDexterity, (int)addDex, m_baseVitality, (int)addVit);
            }

            m_baseStrength += (int)addStr;
            m_baseFocus += (int)addSta;
            m_baseDexterity += (int)addDex;
            m_baseVitality += (int)addVit;
            
            UpdateBaseStats();
            CompiledStats.AttributePoints = newRemainingPoints;
            m_db.runCommandSync("update character_details set strength=" + m_baseStrength + ",focus=" + m_baseFocus + ",dexterity=" + m_baseDexterity + ",vitality=" + m_baseVitality + ",attribute_points=" + CompiledStats.AttributePoints + " where character_id=" + m_character_id);

        }
        
		internal void ResetAttributes()
        {
         //   int extraPoints = m_baseStrength - m_race.m_starting_strength;
         //   extraPoints += m_baseVitality - m_race.m_starting_vitality;
         //   extraPoints += m_baseFocus - m_race.m_starting_focus;
         //   extraPoints += m_baseDexterity - m_race.m_starting_dexterity;
            m_inventory.UnequipAllItems();
           // if (extraPoints > 0)
           // {

                m_baseStrength = m_race.m_starting_strength;
                m_baseVitality = m_race.m_starting_vitality;
                m_baseFocus = m_race.m_starting_focus;
                m_baseDexterity = m_race.m_starting_dexterity;
                UpdateBaseStats();
                CompiledStats.AttributePoints = (Level-1)*5;
                sendBaseStatsUpdate();
                m_db.runCommandSync("update character_details set strength=" + m_baseStrength + ",focus=" + m_baseFocus + ",dexterity=" + m_baseDexterity + ",vitality=" + m_baseVitality + ",attribute_points=" + CompiledStats.AttributePoints + " where character_id=" + m_character_id);

            //}
            updateCombatStats(true);
            SetStatsChangeLevel(STATS_CHANGE_LEVEL.EQUIPMENT_CHECK_REQUIRED);
            /* m_baseStrength += (int)addStr;
             m_baseFocus += (int)addSta;
             m_baseDexterity += (int)addDex;
             m_baseVitality += (int)addVit;
             m_attributePoints = (int)remainingPoints;
             m_db.runCommand("update character_details set strength=" + m_baseStrength + ",focus=" + m_baseFocus + ",dexterity=" + m_baseDexterity + ",vitality=" + m_baseVitality + ",attribute_points=" + m_attributePoints + " where character_id=" + m_character_id);
            */


        }

        internal void writeAbilitiesToDB()
        {
            string abilityString = "";
            for (int i = 0; i < m_abilities.Count; i++)
            {
                abilityString += ";" + (int)m_abilities[i].m_ability_id + "," + m_abilities[i].m_currentLevel;
            }
            if (abilityString.Length == 0)
            {
                abilityString = " ";
            }
            else
            {
                abilityString = abilityString.Substring(1);
            }
            m_db.runCommandSync("update character_details set ability_list='" + abilityString + "' where character_id=" + m_character_id);
        }
        internal void readAbilitiesFromDB(SqlQuery query)
        {
            string abilityString = query.GetString("ability_list").Trim();
            if (abilityString.Length == 0)
            {
                return;
            }
            else
            {
                string[] abilityStringSplit = abilityString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < abilityStringSplit.Length; i++)
                {
                    string[] abilitysubSplit = abilityStringSplit[i].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    ABILITY_TYPE ability_id = (ABILITY_TYPE)Int32.Parse(abilitysubSplit[0]);
                    int currentLevel = Int32.Parse(abilitysubSplit[1]);
                    m_abilities.Add(new CharacterAbility(ability_id, currentLevel));
                }
            }

        }
        #region Permanent Buffs
        internal void writePermanentBuffsToDB()
        {
            //m_permanentBuff 
            string buffsString = "";
            for (int i = 0; i < m_permanentBuff.Count; i++)
            {
                buffsString += ";" + (int)m_permanentBuff[i].BuffID + "," + m_permanentBuff[i].BuffQuantity;
            }
            if (buffsString.Length == 0)
            {
                buffsString = "";
            }
            else
            {
                buffsString = buffsString.Substring(1);
            }
            m_db.runCommandSync("update character_details set perm_buffs='" + buffsString + "' where character_id=" + m_character_id);
        }
        internal void readPermanentBuffsFromDB(SqlQuery query)
        {
            string buffsString = query.GetString("perm_buffs").Trim();
            if (buffsString.Length == 0)
            {
                return;
            }
            else
            {
                string[] buffStringSplit = buffsString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < buffStringSplit.Length; i++)
                {
                    string[] abilitysubSplit = buffStringSplit[i].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    PERMENENT_BUFF_ID buff_id = (PERMENENT_BUFF_ID)Int32.Parse(abilitysubSplit[0]);
                    int buffLevel = Int32.Parse(abilitysubSplit[1]);
                    m_permanentBuff.Add(new PermanentBuff(buff_id, buffLevel));
                }
            }

        }
        internal void AddPermBuffsToCharacter(PERMENENT_BUFF_ID buffID, int quantity,bool reportToPlayer)
        {

            PermanentBuff theBuff = null;
            for (int i = 0; i < m_permanentBuff.Count && theBuff == null; i++)
            {
                PermanentBuff currentBuff = m_permanentBuff[i];
                if (currentBuff != null && currentBuff.BuffID == buffID)
                {
                    theBuff = currentBuff;
                    currentBuff.BuffQuantity += quantity;
                }
            }
            if (theBuff == null)
            {
                theBuff = new PermanentBuff(buffID, quantity);
                m_permanentBuff.Add(theBuff);
            }
            SetUpPermBuffs();
            writePermanentBuffsToDB();
            SetStatsChangeLevel(STATS_CHANGE_LEVEL.COMPILE_REQUIRED);//m_statsUpdated=true;
            SendPermBuffMessage();

            switch (buffID)
            {
                case PERMENENT_BUFF_ID.BACKPACK:
                    {
                        if (reportToPlayer == true)
                        {
							string locText = Localiser.GetString(textDB, m_player, (int)CharacterTextDB.TextID.FAST_TRAVEL_INCREASED);
							locText = string.Format(locText, 5 * quantity);
							Program.processor.sendSystemMessage(locText, m_player, true, SYSTEM_MESSAGE_TYPE.POPUP);
                        }
                        break;
                    }
                case PERMENENT_BUFF_ID.ENERGY_REGEN_1:
                    {
                        if (reportToPlayer == true)
                        {
							string locText = Localiser.GetString(textDB, m_player, (int)CharacterTextDB.TextID.ENERGY_REGEN_INCREASED);
							locText = string.Format(locText, quantity);
							Program.processor.sendSystemMessage(locText, m_player, true, SYSTEM_MESSAGE_TYPE.POPUP);
                        }
                        break;
                    }
                case PERMENENT_BUFF_ID.HEALTH_REGEN_1:
                    {
                        if (reportToPlayer == true)
                        {
							string locText = Localiser.GetString(textDB, m_player, (int)CharacterTextDB.TextID.HEALTH_REGEN_INCREASED);
							locText = string.Format(locText, quantity);
							Program.processor.sendSystemMessage(locText, m_player, true, SYSTEM_MESSAGE_TYPE.POPUP);
                        }
                        break;
                    }
                case PERMENENT_BUFF_ID.EXTRA_HUD_SLOT:
                    {
                        if (reportToPlayer == true)
                        {
							string locText = Localiser.GetString(textDB, m_player, (int)CharacterTextDB.TextID.EXTRA_SLOT_INCREASED);
							locText = string.Format(locText, quantity);
							Program.processor.sendSystemMessage(locText, m_player, true, SYSTEM_MESSAGE_TYPE.POPUP);
                        }
                        //send the new Hud Configuration to the player
                        int totalSlots = BASE_HUD_SLOTS + m_numberOfExtraHudSlots;
                        m_slotSetHolder.SetTotalSlots(totalSlots);
                        SendHudUpdate();
                        break;
                    }
                case PERMENENT_BUFF_ID.SOLO_BANK_EXPANSION:
                    {
                        if (reportToPlayer == true)
                        {
							string locText = Localiser.GetString(textDB, m_player, (int)CharacterTextDB.TextID.BANK_SLOT_INCREASED);
							locText = string.Format(locText, 10 * quantity);
							Program.processor.sendSystemMessage(locText, m_player, true, SYSTEM_MESSAGE_TYPE.POPUP);
                        }
                        break;
                    }
                case PERMENENT_BUFF_ID.AUCTION_HOUSE_SLOT_EXPANSION:
                    {
                        if (reportToPlayer == true)
                        {
							string locText = Localiser.GetString(textDB, m_player, (int)CharacterTextDB.TextID.AUCTION_NUMBER_INCREASED);
							locText = string.Format(locText, quantity);
							Program.processor.sendSystemMessage(locText, m_player, true, SYSTEM_MESSAGE_TYPE.POPUP);
                        }
                        break;
                    }
            }
        }


        void SetUpPermBuffs()
        {
            CompiledStats.FastTravelItemLimit = CombatEntityStats.BASE_FAST_TRAVEL_LIMIT;
            CompiledStats.SoloBankSizeLimit = CombatEntityStats.BASE_SOLO_BANK_SIZE;
            m_additionalHealthRegenPerTick = 0;
            m_additionalEnergyRegenPerTick = 0;
            m_numberOfExtraHudSlots = 0;
            m_numberOfExtraAHSlots = 0;

            for (int i = 0; i < m_permanentBuff.Count; i++)
            {
                PermanentBuff currentBuff = m_permanentBuff[i];
                if (currentBuff != null)
                {
                    switch (currentBuff.BuffID)
                    {
                        case PERMENENT_BUFF_ID.BACKPACK:
                        {
                            CompiledStats.FastTravelItemLimit += 5 * currentBuff.BuffQuantity;
                            break;
                        }
                        case PERMENENT_BUFF_ID.ENERGY_REGEN_1:
                        {
                            m_additionalEnergyRegenPerTick += 1 * currentBuff.BuffQuantity;
                            break;
                        }
                        case PERMENENT_BUFF_ID.HEALTH_REGEN_1:
                        {
                            m_additionalHealthRegenPerTick += 1 * currentBuff.BuffQuantity;
                            break;
                        }
                        case PERMENENT_BUFF_ID.EXTRA_HUD_SLOT:
                        {
                            m_numberOfExtraHudSlots += 1 * currentBuff.BuffQuantity;
                            break;
                        }
                        case PERMENENT_BUFF_ID.SOLO_BANK_EXPANSION:
                        {
                            CompiledStats.SoloBankSizeLimit += 10 * currentBuff.BuffQuantity;
                            break;
                        }
                        case PERMENENT_BUFF_ID.AUCTION_HOUSE_SLOT_EXPANSION:
                        {
                            m_numberOfExtraAHSlots += 1 * currentBuff.BuffQuantity;
                            break;
                        }
                    }

                }

            }

        }

        /// <summary>
        /// Some purhcases have a max limit, get the max remaining for this item
        /// </summary>
        /// <param name="item">premium item to check</param>
        /// <returns>max purchases left to buy for this item</returns>
        internal int GetMaxExtraSlotsToBuy(PremiumShopItem item)
        {
            switch ((PERMENENT_BUFF_ID)item.ItemTemplateID)
            {
                // HUD slots only sold as singles
                case (PERMENENT_BUFF_ID.EXTRA_HUD_SLOT):
                    return MAX_EXTRA_HUD_SLOTS - m_numberOfExtraHudSlots;
                
                // AH slots sold as singles and as pack of five
                case (PERMENENT_BUFF_ID.AUCTION_HOUSE_SLOT_EXPANSION):
                    // Get the number of slots that can be purchased
                    float freeSlots = MAX_EXTRA_AH_SLOTS - m_numberOfExtraAHSlots;
                    
                    // We do not care about the fraction only how many sets can be purchased
                    if (item.StockID == 161)
                    {
                        return (int)(freeSlots / item.Quantity);
                    }
                    // Single slots - no modification
                    else
                    {
                        return (int)freeSlots;
                    }
        
                default:
                    return 0;
            } 
        }
        
        #endregion

        internal void WriteDiscoveredTeleportLocationsToMsg(NetOutgoingMessage msg)
        {
            //number of locations Discovered
            //number of each location
            msg.WriteVariableInt32(m_discoveredSpawnPoints.Count);
            for (int i = 0; i < m_discoveredSpawnPoints.Count; i++)
            {
                msg.WriteVariableInt32(m_discoveredSpawnPoints[i]);
            }
        }
        internal void WriteDiscoveredZonesToMsg(NetOutgoingMessage msg)
        {
            List<int> knownZones = Program.processor.GetKnownZonesForCharacter(this);
            //number of locations Discovered
            //number of each location

            msg.WriteVariableInt32(knownZones.Count);
            for (int i = 0; i < knownZones.Count; i++)
            {
                msg.WriteVariableInt32(knownZones[i]);
            }
        }
        internal void writeAbilitiesToMsg(NetOutgoingMessage msg)
        {
            msg.WriteVariableInt32(m_abilities.Count);
            for (int i = 0; i < m_abilities.Count; i++)
            {
                msg.WriteVariableInt32((int)m_abilities[i].m_ability_id);
                msg.WriteVariableInt32(m_abilities[i].m_currentLevel);
            }
        }
        internal void writeCompiledAbilitiesToMessage(NetOutgoingMessage msg)
        {

            List<CES_AbilityHolder> compiledAbilities = m_compiledStats.Abilities;
            msg.WriteVariableInt32(compiledAbilities.Count);
            for (int i = 0; i < compiledAbilities.Count; i++)
            {
                msg.WriteVariableInt32((int)compiledAbilities[i].m_ability_id);
                msg.WriteVariableInt32((int)compiledAbilities[i].m_currentValue);
            }
        }
        internal void writeAvoidancesToMessage(NetOutgoingMessage msg)
        {
            msg.WriteVariableInt32(m_compiledStats.AvoidanceTypes.Count);
            for (int i = 0; i < CompiledStats.AvoidanceTypes.Count; i++)
            {
                FloatForID currentType = CompiledStats.AvoidanceTypes[i];
                msg.WriteVariableInt32(currentType.m_bonusType);
                msg.WriteVariableInt32((int)currentType.m_amount);
            }
            /*for (int i = 0; i < NUM_AVOIDANCE_TYPES; i++)
            {
                msg.WriteVariableInt32((int)Math.Ceiling(m_compiledStats.GetAvoidanceType((AVOIDANCE_TYPE)i)));
            }*/
        }
        internal void WriteCompiledSkillsToTheList(NetOutgoingMessage msg)
        {
            List<CES_SkillHolder> compiledSkills = m_compiledStats.Skills;
            msg.WriteVariableInt32(compiledSkills.Count);
            for (int i = 0; i < compiledSkills.Count; i++)
            {
                msg.WriteVariableInt32((int)compiledSkills[i].m_skillID);
                msg.WriteVariableInt32((int)compiledSkills[i].m_currentValue);
            }
        }
        internal void updateWeaponAbility()
        {
            Item weapon = m_inventory.GetEquipmentForSlot(0);
            CharacterAbility ability = null;

            if (weapon == null)
            {
                ability = getAbilityById(ABILITY_TYPE.HAND_TO_HAND);

            }
            else
            {
                ItemTemplate.ITEM_SUB_TYPE subtype = weapon.m_template.m_subtype;

                switch (subtype)
                {
                    case ItemTemplate.ITEM_SUB_TYPE.SWORD://sword
                    case ItemTemplate.ITEM_SUB_TYPE.SWORD_TWO_HANDED:
                        {
                            ability = getAbilityById(ABILITY_TYPE.SWORD);
                            break;
                        }
                    case ItemTemplate.ITEM_SUB_TYPE.BLUNT:
                    case ItemTemplate.ITEM_SUB_TYPE.BLUNT_TWO_HANDED:
                        {
                            ability = getAbilityById(ABILITY_TYPE.BLUNT);
                            break;
                        }
                    case ItemTemplate.ITEM_SUB_TYPE.AXE:
                    case ItemTemplate.ITEM_SUB_TYPE.AXE_TWO_HANDED:
                        {
                            ability = getAbilityById(ABILITY_TYPE.AXE);
                            break;
                        }
                    case ItemTemplate.ITEM_SUB_TYPE.STAFF:
                        {
                            ability = getAbilityById(ABILITY_TYPE.STAFF);
                            break;
                        }
                    case ItemTemplate.ITEM_SUB_TYPE.ONE_HANDED_STAFF:
                    case ItemTemplate.ITEM_SUB_TYPE.TOTEM_LONG:
                        {
                            ability = getAbilityById(ABILITY_TYPE.TOTEM);
                            break;
                        }
                    case ItemTemplate.ITEM_SUB_TYPE.WAND:
                        {
                            ability = getAbilityById(ABILITY_TYPE.WAND);
                            break;
                        }
                    case ItemTemplate.ITEM_SUB_TYPE.DAGGER:
                        {
                            ability = getAbilityById(ABILITY_TYPE.DAGGER);
                            break;
                        }
                    case ItemTemplate.ITEM_SUB_TYPE.BOW:
                        {
                            ability = getAbilityById(ABILITY_TYPE.BOW);
                            break;
                        }
                    case ItemTemplate.ITEM_SUB_TYPE.SPEAR:
                    case ItemTemplate.ITEM_SUB_TYPE.SPEAR_TWO_HANDED:
                        {
                            ability = getAbilityById(ABILITY_TYPE.SPEAR);
                            break;
                        }
                    case ItemTemplate.ITEM_SUB_TYPE.SLEDGE:
                    case ItemTemplate.ITEM_SUB_TYPE.BROOM:
                    case ItemTemplate.ITEM_SUB_TYPE.MAGIC_CARPET:
                    case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_BROOM:
                    case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_WAND:
                    case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_LUTE:
                    case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_DRAGONSTAFF:
                    case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_FLUTE:
                    case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_HARP:
                    case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_TWO_HANDED:
                    case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_STAFF_MOUNT:
                    case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_HORN:
                    case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_BLUNT:
                    case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_BATMOUNT:
                    case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_ANGEL_WINGS:
                    case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_DRUM:
                    case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_BAGPIPES:                        
                    case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_EAGLEMOUNT:
                    case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_CROW:
                    case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_SPARROW:
                    case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_SPARROWHAWK:
                    case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_SPIRITCAPE:
                    case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_HORSEMOUNT:
                    case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_BANSHEE_BLADE:
                    case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_BONE_BIRD:
                    case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_HELL_WINGS:
                    case ItemTemplate.ITEM_SUB_TYPE.NOVELTY_BOARMOUNT:
                        {
                            ability = getAbilityById(ABILITY_TYPE.NOVELTY_ITEM);
                            break;
                        }
                    case ItemTemplate.ITEM_SUB_TYPE.HAND_TO_HAND:
                        {
                            ability = getAbilityById(ABILITY_TYPE.HAND_TO_HAND);
                            break;
                        }
                    case ItemTemplate.ITEM_SUB_TYPE.FISHING_ROD:
                        {
                            ability = getAbilityById(ABILITY_TYPE.FISHING);
                            break;
                        }
                    default:
                        {
                            return;
                        }
                }
            }
            if (ability != null)
            {
                testAbilityUpgrade(ability);
            }
        }
        internal void testAbilityUpgrade(ABILITY_TYPE abilityType)
        {
            CharacterAbility ability = getAbilityById(abilityType);
            if (ability == null)
                return;
            testAbilityUpgrade(ability);
        }
        internal void testAbilityUpgrade(CharacterAbility ability)
        {
            bool update = AbilityManager.testUpgrade(ability.m_ability_id, ability.m_currentLevel, GetRelevantLevel(ability), m_compiledStats.AbilityRate);
            if (update)
            {
                ability.m_currentLevel++;
                processAbilityUpdate(ability);
                SetStatsChangeLevel(STATS_CHANGE_LEVEL.COMPILE_REQUIRED);//m_statsUpdated=true;
                //Program.Display("Updating ability level to "+ability.m_currentLevel);
                //Program.Display("Ability rate is "+m_compiledStats.AbilityRate);
            }
        }
		
        internal void processAbilityUpdate(CharacterAbility ability)
        {
            writeAbilitiesToDB();
            NetOutgoingMessage msg = Program.Server.CreateMessage();
            msg.WriteVariableUInt32((uint)NetworkCommandType.AbilityUpdate);
            msg.WriteVariableInt32((int)ability.m_ability_id);
            msg.WriteVariableInt32(ability.m_currentLevel);
            writeAbilitiesToMsg(msg);
            Program.processor.SendMessage(msg, m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.AbilityUpdate);

        }

		#endregion

		#region friends
		internal bool HasFriend(int friendID)
        {
            bool hasFriend = false;
            if (GetFriendForID(friendID) != null)
            {
                hasFriend = true;
            }
            return hasFriend;
        }
        internal FriendTemplate GetFriendForID(int friendID)
        {
            for (int i = 0; (i < m_friendCharacterIDs.Count); i++)
            {
                FriendTemplate currentFriend = m_friendCharacterIDs[i];
                if (currentFriend.CharacterID == friendID)
                {
                    return currentFriend;
                }
            }
            return null;
        }
        
        internal void AddFriend(int newFriendID, Character newFriendCharacter)
        {
            if (HasFriend(newFriendID) == true)
            {
                return;
            }
            FriendTemplate newFriend = new FriendTemplate(newFriendCharacter);
            m_friendCharacterIDs.Add(newFriend);
            if (m_friendCharacterIDs.Count > 4)
            {
                increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.SOCIALISER, 1);
            }
            //save to database
            m_db.runCommandSync("replace into friend_list (character_id,other_character_id) values (" + m_character_id + "," + newFriendID + ")");
        }
        internal void RemoveFriend(int oldFriendID)
        {
            if (HasFriend(oldFriendID) == false)
            {
                return;
            }
            bool friendRemoved = false;
            for (int i = 0; (i < m_friendCharacterIDs.Count) && (friendRemoved == false); i++)
            {
                FriendTemplate currentFriend = m_friendCharacterIDs[i];
                if (currentFriend.CharacterID == oldFriendID)
                {
                    m_friendCharacterIDs.Remove(currentFriend);
                    friendRemoved = true;
                }
            }



            //save to database
            m_db.runCommandSync("delete from friend_list where character_id=" + m_character_id + " and other_character_id=" + oldFriendID);
        }
        static internal string WriteFriendsToString(List<FriendTemplate> friendList)
        {
            string friendsString = "";

            for (int currentFriendIndex = 0; currentFriendIndex < friendList.Count; currentFriendIndex++)
            {
                friendsString += "," + friendList[currentFriendIndex].CharacterID;
            }
            if (friendsString.Length > 0)
            {
                friendsString = friendsString.Substring(1);
            }

            return friendsString;
        }


        static internal void ReadFriendsFromString(string friendsString, List<FriendTemplate> friendList)
        {
            if (friendsString.Trim().Length == 0)
                return;

            //string friendsListString = WriteFriendsToString();
            String sqlstr = "select * from character_details where character_id in(" + friendsString + ")";
            //Character character;

            SqlQuery query = new SqlQuery(Program.processor.m_worldDB, sqlstr);
            if (!query.HasRows)
            {
                //send error report, no character of this name
                query.Close();
                return;
            }
            while ((query.Read()))
            {
                //character = null;
                //bool currentlyOnline;
                int character_id = query.GetInt32("character_id");
                //name
                string name = query.GetString("name");

                int zone = query.GetInt32("zone");
                //level
                int level = query.GetInt32("level");
                //class
                int characterClass = query.GetInt32("class_id");

                int characterRace = query.GetInt32("race_id");

                FriendTemplate newFriend = new FriendTemplate();
                {
                    newFriend.CharacterID = character_id;
                    /*if (character != null)
                    {
                        newFriend.Character = character;
                        character.UpdateFriendInformation(this);
                    }*/
                    newFriend.CharacterName = name;
                    newFriend.Level = level;
                    newFriend.Online = false;
                    newFriend.Zone = zone;
                    newFriend.Class = characterClass;
                    newFriend.Race = characterRace;
                    friendList.Add(newFriend);
                }
            }
            query.Close();
        }



        static internal void AddFriendToArray(int friend_id, List<FriendTemplate> friendList)
        {


            //string friendsListString = WriteFriendsToString();
            String sqlstr = "select * from character_details where character_id =" + friend_id;
            //Character character;

            SqlQuery query = new SqlQuery(Program.processor.m_worldDB, sqlstr);
            if (!query.HasRows)
            {
                //send error report, no character of this name
                query.Close();
                return;
            }
            while ((query.Read()))
            {
                //character = null;
                //bool currentlyOnline;
                int character_id = query.GetInt32("character_id");
                //name
                string name = query.GetString("name");

                int zone = query.GetInt32("zone");
                //level
                int level = query.GetInt32("level");
                //class
                int characterClass = query.GetInt32("class_id");

                int characterRace = query.GetInt32("race_id");

                FriendTemplate newFriend = new FriendTemplate();
                {
                    newFriend.CharacterID = character_id;
                    /*if (character != null)
                    {
                        newFriend.Character = character;
                        character.UpdateFriendInformation(this);
                    }*/
                    newFriend.CharacterName = name;
                    newFriend.Level = level;
                    newFriend.Online = false;
                    newFriend.Zone = zone;
                    newFriend.Class = characterClass;
                    newFriend.Race = characterRace;
                    friendList.Add(newFriend);
                }
            }
            query.Close();
        }
        /// <summary>
        /// Checks What friends are online and updates them that this character is now online
        /// </summary>
        internal void RefreshFriendsList()
        {

            for (int i = 0; i < m_friendCharacterIDs.Count; i++)
            {
                Character character = null;
                bool currentlyOnline = false;
                FriendTemplate currentFriend = m_friendCharacterIDs[i];
                Player friendsPlayer = Program.processor.getPlayerFromActiveCharacterId(currentFriend.CharacterID);
                if (friendsPlayer != null)
                {
                    currentlyOnline = true;
                    character = friendsPlayer.m_activeCharacter;
                }
                if (currentFriend != null)
                {
                    if (character != null)
                    {
                        currentFriend.Character = character;
                        character.UpdateFriendInformation(this);
                    }

                    currentFriend.Online = currentlyOnline;

                }
            }

        }
        internal void WriteFriendsListToMessage(NetOutgoingMessage msg)
        {
            int numberOfFriends = m_friendCharacterIDs.Count;
            //number of friends
            msg.WriteVariableInt32(numberOfFriends);
            for (int currentFriendIndex = 0; currentFriendIndex < numberOfFriends; currentFriendIndex++)
            {
                FriendTemplate currentFriend = m_friendCharacterIDs[currentFriendIndex];
                msg.Write(currentFriend.CharacterName);
                msg.WriteVariableInt32(currentFriend.CharacterID);
                //online
                if (currentFriend.Online == true)
                {
                    msg.Write((byte)1);
                }
                else
                {
                    msg.Write((byte)0);
                }
                //location
                msg.WriteVariableInt32(currentFriend.Zone);
                //level
                msg.WriteVariableInt32(currentFriend.Level);
                //class
                msg.WriteVariableInt32(currentFriend.Class);
            }
        }

        void FriendLoggingOut(Character friendsCharacter)
        {
            bool friendUpdated = false;
            int characterID = (int)friendsCharacter.m_character_id;
            int numberOfFriends = m_friendCharacterIDs.Count;
            for (int currentFriendIndex = 0; (currentFriendIndex < numberOfFriends) && (friendUpdated == false); currentFriendIndex++)
            {
                FriendTemplate currentFriend = m_friendCharacterIDs[currentFriendIndex];
                if (currentFriend.CharacterID == characterID)
                {

                    currentFriend.Online = false;
                    currentFriend.UpdateWithDetails(friendsCharacter);
                    currentFriend.Character = null;
                    friendUpdated = true;
                    //Program.processor.sendActiveCharactersFriendList(m_player);
                    if ((m_charactersClan == null) || (friendsCharacter.CharactersClan != m_charactersClan))
                    {
						string locText = Localiser.GetString(textDB, m_player, (int)CharacterTextDB.TextID.FRIEND_LOGGED_OUT);
						locText = string.Format(locText, currentFriend.CharacterName);
						Program.processor.sendSystemMessage(locText, m_player, false, SYSTEM_MESSAGE_TYPE.FRIENDS);
                    }
                }
            }

        }
        internal void UpdateFriendInformation(Character friendsCharacter)
        {
            bool friendUpdated = false;
            int characterID = (int)friendsCharacter.m_character_id;
            int numberOfFriends = m_friendCharacterIDs.Count;
            for (int currentFriendIndex = 0; (currentFriendIndex < numberOfFriends) && (friendUpdated == false); currentFriendIndex++)
            {
                FriendTemplate currentFriend = m_friendCharacterIDs[currentFriendIndex];
                if (currentFriend.CharacterID == characterID)
                {
                    bool alreadyOnline = currentFriend.Online;

                    currentFriend.Online = true;
                    currentFriend.UpdateWithDetails(friendsCharacter);
                    friendUpdated = true;
                    if (alreadyOnline == false)
                    {
                        //Program.processor.sendActiveCharactersFriendList(m_player);
                        if ((m_charactersClan == null) || (friendsCharacter.CharactersClan != m_charactersClan))
                        {
							string locText = Localiser.GetString(textDB, m_player, (int)CharacterTextDB.TextID.FRIEND_LOGGED_IN);
							locText = string.Format(locText, currentFriend.CharacterName);
							Program.processor.sendSystemMessage(locText, m_player, false, SYSTEM_MESSAGE_TYPE.FRIENDS);
                        }
                    }
                }
            }


        }

        internal void UpdateSocialLists()
        {
            int numberOfFriends = m_friendCharacterIDs.Count;
            for (int currentFriendIndex = 0; currentFriendIndex < numberOfFriends; currentFriendIndex++)
            {
                FriendTemplate currentFriend = m_friendCharacterIDs[currentFriendIndex];
                if (currentFriend.Character != null)
                {
                    currentFriend.Character.UpdateFriendInformation(this);
                }
            }
            if (m_charactersClan != null)
            {
                m_charactersClan.MemberUpdate(this);
            }
            //UpdateFriendInformation(this);

        }
        internal void LogoutOfSocialLists()
        {
            try
            {
                int numberOfFriends = m_friendCharacterIDs.Count;
                for (int currentFriendIndex = 0; currentFriendIndex < numberOfFriends; currentFriendIndex++)
                {
                    FriendTemplate currentFriend = m_friendCharacterIDs[currentFriendIndex];
                    if (currentFriend.Character != null)
                    {
                        currentFriend.Character.FriendLoggingOut(this);
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Display("error in remove from friends " + ex.Message + " :" + ex.StackTrace);
            }
            try
            {
                //log out of the party
                if (m_party != null)
                {
                    m_party.RemovePlayer(this,false);
                }
            }
            catch (Exception ex)
            {
                Program.Display("error in remove party " + ex.Message + " :" + ex.StackTrace);
            }
            try
            {
                //log out of the clan
                if (m_charactersClan != null)
                {
                    m_charactersClan.MemberLogout(this);
                }
            }
            catch (Exception ex)
            {
                Program.Display("error in remove from clam " + ex.Message + " :" + ex.StackTrace);
            }
        }
        #endregion//friends

        #region blocking

        internal void AddToBlockedList(int characterID, string characterName)
        {
            if (FriendTemplate.ContainsTemplateForID(m_blockList, characterID) != null)
            {
                return;
            }
            FriendTemplate newBlocked = new FriendTemplate();
            newBlocked.CharacterID = characterID;
            newBlocked.CharacterName = characterName;

            m_blockList.Add(newBlocked);

			string locText = Localiser.GetString(textDB, m_player, (int)CharacterTextDB.TextID.BLOCKED);
			locText = string.Format(locText, characterName);
			Program.processor.sendSystemMessage(locText, m_player, false, SYSTEM_MESSAGE_TYPE.BLOCK);

            Program.processor.sendActiveCharactersBlockedList(m_player,false);
            //save to database
            m_db.runCommandSync("replace into block_list (character_id,other_character_id) values (" + m_character_id + "," + characterID + ")");
        }
        internal void RemoveFromBlockedList(int characterID)
        {
            if (FriendTemplate.ContainsTemplateForID(m_blockList, characterID) == null)
            {
                return;
            }
            bool blockRemoved = false;
            for (int i = 0; (i < m_blockList.Count) && (blockRemoved == false); i++)
            {
                FriendTemplate currentBlocked = m_blockList[i];
                if (currentBlocked.CharacterID == characterID)
                {
                    m_blockList.Remove(currentBlocked);
                    blockRemoved = true;
                }
            }
     
            string newBlockedString = WriteFriendsToString(m_blockList);
            Program.processor.sendActiveCharactersBlockedList(m_player, false);

            //save to database
            m_db.runCommandSync("delete from block_list where character_id=" + m_character_id + " and other_character_id=" + characterID);
        }
        internal bool HasBlockedCharacter(int characterID)
        {
            if (FriendTemplate.ContainsTemplateForID(m_blockList, characterID) == null)
            {
                return false;
            }
            return true;
        }

        internal void WriteBlockedListToMessage(NetOutgoingMessage msg)
        {
            msg.WriteVariableInt32(m_blockList.Count);
            for (int i = 0; (i < m_blockList.Count); i++)
            {
                FriendTemplate currentBlockedCharacter = m_blockList[i];
                msg.WriteVariableInt32(currentBlockedCharacter.CharacterID);
                msg.Write(currentBlockedCharacter.CharacterName);
            }
        }
        #endregion//block
        
        #region skills & abilities

		internal string LearnAbility(int ability_id, int coins)
		{

			CharacterAbility ability = getAbilityById((ABILITY_TYPE)ability_id);
			if (ability == null)
			{
				if (m_inventory.m_coins >= coins && coins >= 0)
				{
					ability = new CharacterAbility((ABILITY_TYPE)ability_id, 0);

					if (ability != null)
					{
						if (AbilityManager.isAvailable((ABILITY_TYPE)ability_id, m_class.m_classType))
						{
							m_inventory.m_coins -= coins;
                            m_db.runCommandSync("update character_details set coins=" + m_inventory.m_coins + " where character_id=" + m_character_id);
							m_abilities.Add(ability);


							processAbilityUpdate(ability);
							SetStatsChangeLevel(STATS_CHANGE_LEVEL.COMPILE_REQUIRED);//m_statsUpdated=true;
							return "";
						}
						else
						{
							return Localiser.GetString(textDB, m_player, (int)CharacterTextDB.TextID.NOT_ALLOWED);
						}
					}
					else
					{
						return Localiser.GetString(textDB, m_player, (int)CharacterTextDB.TextID.ABILITY_NOT_EXIST);
					}
				}
				else
				{
					return Localiser.GetString(textDB, m_player, (int)CharacterTextDB.TextID.CANNOT_AFFORD_ABILITY);
				}
			}
			else
			{
				return Localiser.GetString(textDB, m_player, (int)CharacterTextDB.TextID.ALREADY_KNOWN);
			}
		}

        public void SaveOutstandingSkills()
        {

            //get the current time
            double currentTime = Program.MainUpdateLoopStartTime(); //NetTime.Now;
            string timeLastCastClearList = "";
            List<string> transactionList = new List<string>();
            for (int i = 0; i < m_EntitySkills.Count; i++)
            {

                //only record it if needed
                EntitySkill currentSkill = m_EntitySkills[i];


                SkillTemplate currectSkillTemplate = currentSkill.Template;
                SkillTemplateLevel currentSkillTemplateLevel = currentSkill.getSkillTemplateLevel(false);

                if (currectSkillTemplate != null)
                {
                    SKILL_TYPE currentSkillID = currentSkill.SkillID;
                    double currentSkillLastCast = currentSkill.TimeLastCast;
                    double timeSinceLastCast = currentTime - currentSkillLastCast;
                    double rechargeTime = 0;
                    if (currentSkillTemplateLevel != null)
                    {
                        rechargeTime = currentSkillTemplateLevel.GetRechargeTime(currentSkill, false);
                    }
                    //check if it's finnished recharging
                    if (currentSkillTemplateLevel != null && timeSinceLastCast < rechargeTime)
                    {
                        string skillSetString = "update character_skills set time_last_cast=" + currentSkillLastCast.ToString("f0") + " where character_id=" + m_character_id + " and skill_id=" + (int)currentSkillID;
                        transactionList.Add(skillSetString);
                        //m_db.runCommandSync("update character_skills set time_last_cast=" + currentSkillLastCast.ToString("f0") + " where character_id=" + m_character_id + " and skill_id=" + (int)currentSkillID);
                    }
                    else
                    {
                        timeLastCastClearList += "," + (int)currentSkillID;
                    }
                    if (currentSkill.TimesCastSinceLog > 0)
                    {
                        string skillSetString = "update character_skills set cast_count =" + (currentSkill.TimesCastBeforeLog+ currentSkill.TimesCastSinceLog) + " where character_id=" + m_character_id + " and skill_id=" + (int)currentSkillID;
                        transactionList.Add(skillSetString);
                    }


                }

            }
            if (transactionList.Count > 0)
            {
                m_db.runCommandsInTransaction(transactionList);
            }
            if (timeLastCastClearList.Length > 0)
            {
                m_db.runCommandSync("update character_skills set time_last_cast=0 where character_id=" + m_character_id + " and skill_id in (" + timeLastCastClearList.Substring(1) + ")");

            }
        }

        public void LoadSkillEntities()
        {
            double currentTime = Program.MainUpdateLoopStartTime(); //NetTime.Now;
            SqlQuery query = new SqlQuery(m_db, "select * from character_skills where character_id=" + m_character_id);
            while (query.Read())
            {
                SKILL_TYPE skill_id = (SKILL_TYPE)query.GetInt32("skill_id");
                SkillTemplate theSkill = SkillTemplateManager.GetItemForID(skill_id);
                EntitySkill newSkill = new EntitySkill(theSkill);
                newSkill.SkillLevel = query.GetInt32("skill_level");
                int castCount = query.GetInt32("cast_count"); 

                int timeLastCast = query.GetInt32("time_last_cast");

                double timeSinceSkillCast = currentTime - timeLastCast;

                if (castCount > 0)
                {
                    m_hasUsedSkill = true;
                }
                newSkill.TimesCastBeforeLog = castCount;

                double rechargeTime = 0;
                SkillTemplateLevel skillTemplateLevel = newSkill.getSkillTemplateLevel(false);
                if (skillTemplateLevel != null)
                {
                    rechargeTime = skillTemplateLevel.GetRechargeTime(newSkill, false);
                }
                if (skillTemplateLevel != null && rechargeTime > timeSinceSkillCast)
                {
                    newSkill.TimeLastCast = timeLastCast;
                }
                m_EntitySkills.Add(newSkill);

            }
            query.Close();

        }

        void AddSkillFromItem(Item currentEquipment,EquipmentSetRewardContainer rewardContainer, List<StatusSkill> listCopy, List<StatusSkill> addedList, List<StatusSkill> removedList,bool resetTimers)
        {

            ItemTemplate itemTemplate = null;
            if (currentEquipment != null)
            {
                itemTemplate = currentEquipment.m_template;
            }
            else if (rewardContainer != null && rewardContainer.Reward != null)
            {
                itemTemplate = rewardContainer.Reward.ItemReward;
            }
            if ((itemTemplate != null && itemTemplate.m_equipSkillID > 0 &&
                (((currentEquipment!=null)&&
                 (currentEquipment.m_template.m_maxCharges < 0 || currentEquipment.m_remainingCharges > 0))
                || (rewardContainer!=null)))
                )
            {
                //ItemTemplate itemTemplate = currentEquipment.m_template;
                SkillTemplate skillTemp = SkillTemplateManager.GetItemForID((SKILL_TYPE)itemTemplate.m_equipSkillID);
                StatusSkill previousEntry = null;
                //if a higher version of the skill does not exist
                //then you want to add the skill to the list
                bool addNewSkill = true;
                //check the skill hasn't already been added by another piece of equipment
                //check through the skills that have been added so far
                for (int j = 0; j < m_AdditionalSkill.Count() && previousEntry == null; j++)
                {
                    StatusSkill oldSkill = m_AdditionalSkill[j];
                    if (oldSkill.SkillID == (SKILL_TYPE)itemTemplate.m_equipSkillID)
                    {
                        previousEntry = oldSkill;

                        // if the new skill is a higher Level then remove the earlier version
                        if (oldSkill.SkillLevel < itemTemplate.m_equipSkillLevel)
                        {
                            m_AdditionalSkill.Remove(oldSkill);

                        }
                        else
                        {
                            //a higher version of the skill exists
                            //do not add the skill to the list
                            addNewSkill = false;
                        }
                    }
                }




                //if it has a skill then add it to the list
                if (addNewSkill == true)
                {
                    StatusSkill newSkill = new StatusSkill(skillTemp, currentEquipment, m_inventory, rewardContainer);
                    newSkill.SkillLevel = itemTemplate.m_equipSkillLevel;
                    newSkill.MaxLevel = -1;
                    //treat it like it's just been cast
                    if (resetTimers == true)
                    {
                        newSkill.TimeLastCast = Program.MainUpdateLoopStartTime();
                    }
                    else
                    {
                        if (currentEquipment != null)
                        {
                            newSkill.TimeLastCast = currentEquipment.m_timeRecharged;
                        }
                        else if(rewardContainer!=null)
                        {
                            newSkill.TimeLastCast = rewardContainer.TimeRecharged;
                        }
                    }
                    //if it is also in the old list
                    bool alreadyThere = false;
                    for (int j = 0; j < listCopy.Count() && alreadyThere == false; j++)
                    {
                        StatusSkill oldSkill = listCopy[j];
                        if (oldSkill.SkillID == newSkill.SkillID && oldSkill.SkillLevel == newSkill.SkillLevel)
                        {
                            //set the last cast to the others cast
                            newSkill.TimeLastCast = oldSkill.TimeLastCast;
                            previousEntry = oldSkill;
                        }

                    }
                    if (previousEntry == null)
                    {
                        addedList.Add(newSkill);
                    }
                    m_AdditionalSkill.Add(newSkill);
                }

            }
        }
        internal void AddSkillsFromEquipment (bool resetTimers)
        {
            m_inventory.ValidateRewards();

            //make a copy of the current Additional Skills
            List<StatusSkill> listCopy = new List<StatusSkill>(m_AdditionalSkill);
            List<StatusSkill> addedList = new List<StatusSkill>();
            List<StatusSkill> removedList = new List<StatusSkill>();

            m_AdditionalSkill.Clear();

            //check each piece of armor
            for (int i = 0; i < Inventory.NUM_EQUIP_SLOTS; i++)
            {
                //if it exists
                //check if it has a skill
                Item currentEquipment = m_inventory.GetEquipmentForSlot(i);	            

                AddSkillFromItem(currentEquipment,null, listCopy, addedList, removedList, resetTimers);
               
            }
            List<EquipmentSetRewardContainer> m_qualifiedRewards = m_inventory.m_qualifiedRewards;
            if (m_qualifiedRewards != null && m_qualifiedRewards.Count > 0)
            {
                //List<EquipmentSetRewards> qualifiedRewards = EquipmentSetContainer.GetRewardsForEquipmentSetHolder(availableSets);

                for (int rewardIndex = 0; rewardIndex < m_qualifiedRewards.Count; rewardIndex++)
                {
                    EquipmentSetRewardContainer currentContainer = m_qualifiedRewards[rewardIndex];
                    AddSkillFromItem(null, currentContainer, listCopy, addedList, removedList, resetTimers);
        
                }

            }
            if (resetTimers == true)
            {
                //sort through the old skills
                //it there is a skill in this list and not on the current list then add it to the removed list
                for (int i = 0; i < listCopy.Count(); i++)
                {

                    StatusSkill oldSkill = listCopy[i];
                    StatusSkill currentVersion = null;
                    for (int j = 0; j < m_AdditionalSkill.Count() && currentVersion == null; j++)
                    {
                        StatusSkill currentSkill = m_AdditionalSkill[j];
                        if (currentSkill.SkillID == oldSkill.SkillID)
                        {
                            currentVersion = currentSkill;

                        }

                    }
                    if (currentVersion == null)
                    {
                        removedList.Add(oldSkill);
                    }

                }
                //send removed messages
                if (removedList.Count() > 0)
                {
                    string removedString = "";
                    if (removedList.Count() > 1)
                    {
						removedString = Localiser.GetString(textDB, m_player, (int)CharacterTextDB.TextID.SKILLS_REMOVED);
                    }
                    else
                    {
						removedString = Localiser.GetString(textDB, m_player, (int)CharacterTextDB.TextID.SKILL_REMOVED);
                    }

                    // added string will be of format Skill/s Removed : {0}
                    // so want to append the list of skills
                    string skillNamesString = String.Empty;
                    for (int i = 0; i < removedList.Count(); i++)
                    {
                        StatusSkill currentSkill = removedList[i];
                        string skillName = SkillTemplateManager.GetLocaliseSkillName(m_player, currentSkill.Template.SkillID);
                        skillNamesString += skillName;
                        if (i < removedList.Count() - 1)
                        {
                            skillNamesString += ", ";
                        }
                    }
                    Program.processor.sendSystemMessage(String.Format(removedString, skillNamesString), m_player, false, SYSTEM_MESSAGE_TYPE.SKILLS);
                }
                //send added messages
                if (addedList.Count() > 0)
                {
                    string addedString = "";
                    if (addedList.Count() > 1)
                    {
						addedString = Localiser.GetString(textDB, m_player, (int)CharacterTextDB.TextID.SKILLS_ADDED);
                    }
                    else
                    {
						addedString = Localiser.GetString(textDB, m_player, (int)CharacterTextDB.TextID.SKILL_ADDED);
                    }
                    // added string will be of format Skill/s Added : {0}
                    // so want to append the list of skills
                    string skillNamesString = String.Empty;
                    for (int i = 0; i < addedList.Count(); i++)
                    {
                        StatusSkill currentSkill = addedList[i];
						string skillName = SkillTemplateManager.GetLocaliseSkillName(m_player, currentSkill.Template.SkillID);
						skillNamesString += skillName;
                        if (i < removedList.Count() - 1)
                        {
                            skillNamesString += ", ";
                        }
                    }
                    Program.processor.sendSystemMessage(String.Format(addedString,skillNamesString), m_player, false, SYSTEM_MESSAGE_TYPE.SKILLS);
                }
            }
            //clear any duplicates or clashes with the current Skills List - this may need some careful management
            if ((resetTimers == true) && (listCopy.Count() > 0 || m_AdditionalSkill.Count() > 0))
            {
                SendSkillListUpdate();
            }

        }
        void SetAllMaxSkillLevels()
        {
            for (int i = 0; i < m_EntitySkills.Count; i++)
            {

                //get the current skill
                EntitySkill currentSkill = m_EntitySkills[i];
                //work out what it's maximum Level is for the player
                int maxLevel = currentSkill.Template.GetMaxSkillLevelForPlayerLevel(Level);
                //set the maximum level
                currentSkill.MaxLevel = maxLevel;

            }
        }
        internal override void ResetSkillModifiers()
        {
            base.ResetSkillModifiers();

            for (int currentSkillIndex = 0; currentSkillIndex < m_AdditionalSkill.Count; currentSkillIndex++)
            {
                EntitySkill currentSkill = m_AdditionalSkill[currentSkillIndex];


                currentSkill.ModifiedLevel = currentSkill.SkillLevel;
                currentSkill.SkillAugments.Clear();
            }

        }

        public bool AddSkill(SKILL_TYPE skillID,bool openSkillPage,bool addToHud)
        {


            for (int i = 0; i < m_EntitySkills.Count; i++)
            {
                if (m_EntitySkills[i].SkillID == skillID)
                    return false;

            }
            SkillTemplate theSkill = SkillTemplateManager.GetItemForID(skillID);
            if (theSkill != null)
            {
                EntitySkill newSkill = new EntitySkill(theSkill);
                newSkill.SkillLevel = 0;
                m_EntitySkills.Add(newSkill);
                m_db.runCommandSync("insert into character_skills (character_id,skill_id,skill_level,time_last_cast) values (" + m_character_id + "," + (int)skillID + ",0,0)");
                SetAllMaxSkillLevels();
                if (openSkillPage == true)
                {
                    SendBuySkillResponse((int)skillID);
                }
                else
                {
                    SendSkillListUpdate();
                }
                SetStatsChangeLevel(STATS_CHANGE_LEVEL.BASIC_CHANGED);//m_statsUpdated=true;
                CharacterSlotSet slotSet = m_slotSetHolder.GetSetWithIndex(0);
                if (slotSet != null && addToHud==true)
                {
                    int freeSlot =slotSet.GetFirstEmptySlot();
                    if (freeSlot >= 0)
                    {
                        slotSet.SetSlot(freeSlot, HudSlotItemType.HUD_SLOT_ITEM_SKILL, (int)theSkill.SkillID);
                    }
                    if (slotSet.Changed == true)
                    {
                        slotSet.SaveToDatabase(m_db);
                        SendHudUpdate();
                    }
                }
                
                return true;
            }

            return false;
        }

        public bool AddRecipe(int recipeID)
        {
            CraftingManager.knownRecipe newRecipe = new CraftingManager.knownRecipe();

            foreach (CraftingManager.knownRecipe i in this.CraftingManager.recipeList)
            {
                if (i.recipeID == recipeID)
                {
                    return false;
                }
            }

            newRecipe.recipeID = recipeID;

            this.CraftingManager.recipeList.Add(newRecipe);
            m_db.runCommandSync("insert into character_recipes (character_id,recipe_id) values (" + m_character_id + "," +
                                newRecipe.recipeID + ")");
            SendLearnRecipeResponse(newRecipe.recipeID);
            return true;
        }

        public void ResetSkills()
        {
            int pointsToReturn = 0;
            for (int i = 0; i < m_EntitySkills.Count; i++)
            {
                EntitySkill currentSkill = m_EntitySkills[i];

                pointsToReturn += currentSkill.SkillLevel;
                currentSkill.SkillLevel = 0;
            }

            CompiledStats.SkillPoints += pointsToReturn;
            UpdateSkillPoints();

            //set the database
            m_db.runCommandSync("update character_skills set skill_level=" + 0 + " where character_id=" + m_character_id);
            SendSkillListUpdate();
            SetStatsChangeLevel(STATS_CHANGE_LEVEL.BASIC_CHANGED);//m_statsUpdated=true;

        }

        internal void SendSkillUpdate(int skillID, int skillLevel, double rechargeTime)
        {
            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.SkillUpdate);
            outmsg.WriteVariableInt32(skillID);
            outmsg.WriteVariableInt32(skillLevel);
            outmsg.Write((float)rechargeTime);
            Program.processor.SendMessage(outmsg, m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.SkillUpdate);
        }
        override public EntitySkill GetEnitySkillForID(SKILL_TYPE skillID, bool onlyCharacterOwned)
        {
            EntitySkill theSkill = base.GetEnitySkillForID(skillID, onlyCharacterOwned);

            if (theSkill != null)
            {
                return theSkill;
            }

            if (onlyCharacterOwned == false)
            {
                for (int currentSkillIndex = 0; currentSkillIndex < m_AdditionalSkill.Count; currentSkillIndex++)
                {
                    EntitySkill currentSkill = m_AdditionalSkill[currentSkillIndex];


                    if (currentSkill.SkillID == skillID)
                    {
                        return currentSkill;
                    }

                }
            }
            return theSkill;
        }
        override public float getStatModifier(STAT_TYPE stat, float divisor)
        {
            if (divisor == 0)
                return 0;
            switch (stat)
            {
                case STAT_TYPE.STRENGTH:
                    return CompiledStats.Strength / divisor;
                case STAT_TYPE.DEXTERITY:
                    return CompiledStats.Dexterity / divisor;
                case STAT_TYPE.FOCUS:
                    return CompiledStats.Focus / divisor;
                case STAT_TYPE.VITALITY:
                    return CompiledStats.Vitality / divisor;
                default:
                    return 0;
            }
        }

		/// <summary>
		/// Extends method to also include skills added from items
		/// </summary>
		/// <param name="skillID"></param>
		/// <returns></returns>
		internal override double TimeSinceSkillLastCast(SKILL_TYPE skillID)
		{
			//get the last time as usual
			double last = base.TimeSinceSkillLastCast(skillID);
			//if 0, we didn't find anything so check in additional
			if (last == 0f)
			{
				foreach (StatusSkill skill in m_AdditionalSkill)
				{
					if (skill.SkillID == skillID)
						return skill.TimeLastCast;
				}
			}

			//by default return the last value
			return last;
		}

        #endregion

        
        override internal void ActUponDamage(int damage, CombatEntity caster, CombatManager.ATTACK_TYPE attackType, int attackID, bool aggressive, double aggroModifier)
        {

            if (caster != null && aggressive)
            {
                m_lastAttacker = caster;
                				
                ConductedHotileAction();
                if (damage > 0 && caster.Type == EntityType.Player)
                {
                    AddPVPDamage(damage, Program.MainUpdateLoopStartTime(), (Character)caster);
                }
            }
            else if (caster != null && !aggressive)
            {
                float aggroValue = -damage * 0.1f * caster.AggroModifier;
                aggroValue += (float)aggroModifier;
                if (m_zone != null)
                {
                    m_zone.m_combatManager.EntityAssistedByEntity(this, caster, aggroValue);
                }                
            }
        }        

        public void increaseRanking(RankingsManager.RANKING_TYPE rankingType, double value, bool allowDecrease)
        {
            if (m_characterRankings != null)
            {
                double newValue = m_characterRankings.increaseStat(rankingType, value);
                if (m_player != null && m_player.m_AccountRankings != null)
                {
                    if (m_player.m_AccountRankings.getStat(rankingType) < newValue || allowDecrease)
                    {
                        m_player.m_AccountRankings.setStat(rankingType, newValue, allowDecrease);
                        //m_player.m_AccountRankings.sendLeaderBoardUpdate(this, rankingType);
                    }
                }
            }
        }

        public void updateRanking(RankingsManager.RANKING_TYPE rankingType, double value,bool allowDecrease)
        {
            if (m_characterRankings != null)
            {
                m_characterRankings.setStat(rankingType, value, allowDecrease);
                if (m_player.m_AccountRankings != null)
                {
                    if (m_player.m_AccountRankings.setStat(rankingType, value, allowDecrease))
                    {
                        //m_player.m_AccountRankings.sendLeaderBoardUpdate(this, rankingType);
                    }
                }
            }
        }

        public void increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE achievementType, double value)
        {
            if (m_characterAchievements != null)
            {
                double newValue = m_characterAchievements.increaseStat(achievementType, value);
                if (m_player.m_AccountAchievements != null)
                {
                    if (m_player.m_AccountAchievements.getStat(achievementType) < newValue)
                    {
                        m_player.m_AccountAchievements.setStat(achievementType, newValue);
                        if (m_player.connection != null)
                        {
                            m_player.m_AccountAchievements.sendAchievementUpdate(this, achievementType);
                        }
                    }
                }
            }
        }

        internal bool PlayerOrPartyIsInRange(Vector3 position, float range, CombatManager zoneCombatManager)
        {
            bool someoneIsInRange = false;
            //is the player in a party
            if (CharacterParty != null)
            {
                //get akll the party members
                List<Character> fullMembersList = CharacterParty.CharacterList;
                //check each mamber
                for (int i = 0; i < fullMembersList.Count && someoneIsInRange == false; i++)
                {
                    Character currentCharacter = fullMembersList[i];
                    //if they are in range
                    if ((zoneCombatManager == currentCharacter.TheCombatManager) && (Utilities.Difference2DSquared(currentCharacter.CurrentPosition.m_position, position) < range))
                    {
                        //then someone can get the reward
                        someoneIsInRange = true;
                    }
                }
            }
            else
            {
                if ((zoneCombatManager == TheCombatManager) && (Utilities.Difference2DSquared(CurrentPosition.m_position, position) < range))
                {
                    someoneIsInRange = true;
                }
            }

            return someoneIsInRange;
        }
       
        internal void PrepareForDisconnect()
        {

            CharacterRequestedLogOut();
            try
            {
                if (Program.m_serverID == m_zone.m_serverConfigID)
                {
                    // Player may be NULL.
                    if (null != m_player)
                    {
                        m_zone.removePlayer(m_player, Program.processor.m_server);
                    }
                    SaveKeyData();
                }
            }
            catch (Exception ex)
            {
                Program.Display("error in remove from zone " + ex.Message + ":" + ex.StackTrace);
            }
            try
            {
                LogoutOfSocialLists();
            }
            catch (Exception ex)
            {
                Program.Display("error in logout socialLists :" + ex.Message + ":" + ex.StackTrace);
            }

            //close down any pending requests
            if (m_pendingRequest != null)
            {
                //if there's a pending request
                //send a cancel request
                //clear both requests
                m_pendingRequest.CancelRequest(m_player, PendingRequest.CANCEL_CONDITION.CC_LOGOUT);
            }

            //check the trading

            //check any duels
            //if there's a duel target
            if (m_currentDuelTarget != null)
            {
                //end the duel
                //clear both duel targets
                m_currentDuelTarget.ForceEndDuel(this, Name + " is disconnecting");
            }

            //this could also be a place to clear down social lists
            m_nearbyPlayers.Clear();
            m_PlayersToUpdate.Clear();
            /*m_nearbyMobs.Clear();
            m_oldNearbyMobs.Clear();*/
            //tell the base this should no longer take part in battle
            m_friendCharacterIDs.Clear();
            m_blockList.Clear();
            m_player = null;
            Destroyed = true;


        }

        #region pvp

        internal override bool ConductingHostileAction()
        {
            if (m_currentDuelTarget != null)
            {
                return true;

            }
            return base.ConductingHostileAction();
        }

        internal void PopulateHateList()
        {
            m_hateListChanged = true;
            //clear down the current hate list
            m_hateList.Clear();

            if (Zone.ENABLE_PVP == false)
            {
                return;
            }

            //populate with any zone requirements
            /*if (m_zone != null&&m_zone.m_zone_id ==1)
            {
                m_zone.addAllCharactersIDToList(m_hateList);
            }*/
            //populate with any clan requirements
            //populate with any party requirements


        }
        internal enum PVPType
        {
            Duel = 1,
            Group = 2,
            Clan = 3,
            FreeForAll = 4,
            CanDuel = 5
        }

		//internal static string[] PVPTypeStrings = new string[] { "", "Duel", "Group PVP", "Clan PVP", "Free For All PVP", "Duelling" };
		internal static int[] PVPTypeStringIDs = new int[]
		{
			// 0 will be returned blank string by GetPVPTypeString()
			0,
			(int)CharacterTextDB.TextID.DUEL,
			(int)CharacterTextDB.TextID.GROUP_PVP,
			(int)CharacterTextDB.TextID.CLAN_PVP,
			(int)CharacterTextDB.TextID.FREE_FOR_ALL_PVP,
			(int)CharacterTextDB.TextID.DUELING
		};

		internal string GetPVPTypeString(int typeIndex)
		{
			if (typeIndex <= 0 || typeIndex > PVPTypeStringIDs.Length)
				return "";

			return Localiser.GetString(textDB, m_player, PVPTypeStringIDs[typeIndex]);
		}

        bool m_pvpTypesChanged = false;
        bool m_pvpTargetsNeedChecked = false;
        List<PVPType> m_pvpTypes = new List<PVPType>();

        internal override void PVPTypeChanged()
        {
            m_pvpTypesChanged = true;

        }
        internal override bool IsInPVPType(PVPType type)
        {
            return m_pvpTypes.Contains(type);
        }
        internal override void RemoveEntityFromInterestList(CombatEntity theEntity)
        {
            if (m_entitiesOfInterest.Contains(theEntity) == true)
            {
                base.RemoveEntityFromInterestList(theEntity);
                bool isInPVPAgainst = IsOnHateList(theEntity);
                if (isInPVPAgainst == true)
                {
                    RemoveFromHateList(theEntity);
                    theEntity.RemoveFromHateList(this);
                }
            }
        }

        internal override void EntityOfInterestChangedCombatType(CombatEntity theEntity)
        {
            if (m_entitiesInCombatChanged.Contains(theEntity) == false)
            {
                m_entitiesInCombatChanged.Add(theEntity);
            }
        }

        internal override void AddEntityToInterestList(CombatEntity theEntity)
        {
            if (m_entitiesOfInterest.Contains(theEntity) == false)
            {
                m_newToInterestList.Add(theEntity);
                base.AddEntityToInterestList(theEntity);
                bool isInPVPAgainst = IsInPVPWithEntity(theEntity);
                if (isInPVPAgainst == true)
                {
                    AddToHateList(theEntity);
                    theEntity.AddToHateList(this);
                }
            }
        }

        internal void CheckPVPTypes()
        {
            m_pvpTypes.Clear();

            //List<PVPType> newPVPTypes = new List<PVPType>();

            for (int i = 0; i < m_areaEffectsList.Count; i++)
            {
                EntityAreaConditionalEffect currentEffect = m_areaEffectsList[i];
                if (currentEffect.TheEffect.EffectType == AreaConditionalEffect.ACE.IsPVP)
                {
                    AreaConditionalEffectData pvpData = currentEffect.TheEffect.GetDataOfType(AreaConditionalEffect.ACDT.PVPType);

                    if (pvpData != null && m_pvpTypes.Contains((PVPType)pvpData.DataValue) == false)
                    {
                        m_pvpTypes.Add((PVPType)pvpData.DataValue);
                    }
                }
            }
            if (m_currentDuelTarget != null && m_currentDuelTarget.IsInProgress == true)
            {
                m_pvpTypes.Add(PVPType.Duel);
            }
            m_pvpTargetsNeedChecked = true;

        }

        internal void PVPListNeedsChecked()
        {
            m_pvpTargetsNeedChecked = true;
        }


        void CalculateHateLists()
        {
            List<CombatEntity> newHateList = new List<CombatEntity>();
            List<CombatEntity> needsRemoved = null;

            //for everyone you are interested in
            for (int i = 0; i < m_entitiesOfInterest.Count; i++)
            {
                CombatEntity currentEntity = m_entitiesOfInterest[i];
                //check if you should be in pvp
                bool isInPVPAgainst = currentEntity.IsInPVPWithEntity(this);
                //if they are in pvp with this entity
                if (isInPVPAgainst == true)
                {
                    //add them to the overall list
                    newHateList.Add(currentEntity);
                }
            }
            //who did you previously hate
            for (int i = m_hateList.Count - 1; i >= 0; i--)
            {
                // get each person you hated
                CombatEntity currentEntity = m_hateList[i];
                // do you still hate them
                if (newHateList.Contains(currentEntity) == true)
                {
                    // if you still hate them then they are not new 
                    // remove them from the new list
                    newHateList.Remove(currentEntity);
                }
                else
                {
                    //you need to stop hating this character 
                    //remember you need to remove them 
                    if (needsRemoved == null)
                    {
                        needsRemoved = new List<CombatEntity>();
                    }
                    needsRemoved.Add(currentEntity);
                }
            }
            if (needsRemoved != null || newHateList.Count > 0)
            {
                m_hateListChanged = true;
            }
            //for everyone you need to remove
            if (needsRemoved != null)
            {
                for (int i = 0; i < needsRemoved.Count; i++)
                {
                    CombatEntity currentToRemove = needsRemoved[i];
                    //remove them from your List
                    RemoveFromHateList(currentToRemove);
                    //tell them to remove you from their list
                    currentToRemove.RemoveFromHateList(this);
                }
                needsRemoved.Clear();
            }

            //all of the entities in newHateList are to be added
            for (int i = 0; i < newHateList.Count; i++)
            {
                CombatEntity newEntityToAdd = newHateList[i];
                //remove them from your List
                AddToHateList(newEntityToAdd);
                //tell them to remove you from their list
                newEntityToAdd.AddToHateList(this);
            }
            newHateList.Clear();
            
        }

        internal override bool IsInPVPWithEntity(CombatEntity theEntity)
        {
            //you can only be in pvp with players
            if (theEntity.Type != EntityType.Player)
            {
                return false;
            }
            Character theCharacter = (Character)theEntity;
            bool isInPVP = false;
            for (int i = 0; i < m_pvpTypes.Count && isInPVP == false; i++)
            {
                PVPType currentType = m_pvpTypes[i];
                if (theCharacter.IsInPVPType(currentType) == true)
                {
                    switch (currentType)
                    {
                        case PVPType.FreeForAll:
                            {
                                isInPVP = true;
                                break;
                            }
                        case PVPType.Clan:
                            {
                                if (CharactersClan == null || CharactersClan != theCharacter.CharactersClan)
                                {
                                    isInPVP = true;
                                }
                                break;
                            }
                        case PVPType.Group:
                            {
                                if (CharacterParty == null || theCharacter.CharacterParty != CharacterParty)
                                {
                                    isInPVP = true;
                                }
                                break;
                            }
                        case PVPType.Duel:
                            {
                                if (m_currentDuelTarget == null)
                                {
                                    throw new Exception("currentType = PVPType.Duel with no duel Target");
                                }
                                else if (m_currentDuelTarget.DuelCharacter == theCharacter)
                                {

                                    isInPVP = true;
                                }
                                break;
                            }
                    }

                }
            }


            return isInPVP;
        }
        internal override void AddToHateList(CombatEntity newEntity)
        {
            bool alreadythere = m_hateList.Contains(newEntity);

            if (alreadythere == false)
            {
                m_hateListChanged = true;
                m_hateList.Add(newEntity);
            }

        }
        internal override void RemoveFromHateList(CombatEntity removeEntity)
        {

            if (m_hateList.Remove(removeEntity) == true)
            {
                m_hateListChanged = true;
            }
        }
        internal override void ClearDownHateList()
        {
            for (int i = 0; i < m_hateList.Count; i++)
            {
                m_hateList[i].RemoveFromHateList(this);
            }
            m_hateList.Clear();
        }

        /// <summary>
        /// Send updated hate list of ServerID's to the player, removing any nulls, then flag as done
        /// </summary>
        internal void SendHateList()
        {

            // check they have a player and a connection
            if (m_player == null || m_player.connection == null)
            {
                Program.Display(m_character_id + " failed to send hate list because player or connection was null");
                return;
            }

            if (m_hateList == null)
                return;

            // remove nulls
            for (int i = m_hateList.Count-1; i >= 0; i--)
            {
                if (m_hateList[i] == null)
                    m_hateList.RemoveAt(i);
            }

            // compose message
            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.PlayerHateList);
            outmsg.WriteVariableInt32(m_hateList.Count);

            // write each character ID
            for (int i = 0; i < m_hateList.Count; i++)
            {
                outmsg.WriteVariableUInt32((uint)m_hateList[i].ServerID);
            }

            // send message and flag as done
            Program.processor.SendMessage(outmsg, m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.PlayerHateList);
            m_hateListChanged = false;
        }
        
        internal override bool IsPVP()
        {
            bool inPVP = false;
            for (int i = 0; i < m_pvpTypes.Count && inPVP == false; i++)
            {
                if (m_pvpTypes[i] != Character.PVPType.CanDuel)
                {
                    inPVP = true;
                }
            }

            return inPVP;//(m_hateList.Count>0);
        }

        internal override int GetOpinionOf(CombatEntity otherEntity)
        {

            if (otherEntity.Type == EntityType.Mob)
            {
                return otherEntity.GetOpinionOf(this);
            }
            return 100;


        }
        internal override bool IsInPartyWith(CombatEntity otherEntity)
        {
            if (otherEntity == this)
            {
                return true;
            }
            if (otherEntity.Type == EntityType.Player && m_party != null)
            {
                Character otherPlayer = (Character)otherEntity;
                if (m_party.CharacterList.Contains(otherPlayer))
                {
                    return true;
                }
            }
            return false;

        }
        internal bool CanFastTravel(ref string errorString)
        {

            bool canFastTravel = true;

            for (int i = 0; i < m_areaEffectsList.Count && canFastTravel == true; i++)
            {
                EntityAreaConditionalEffect currentEffect = m_areaEffectsList[i];
                if (currentEffect != null && currentEffect.TheEffect.EffectType == AreaConditionalEffect.ACE.PreventsFastTravel)
                {
                    canFastTravel = false;
					errorString = Localiser.GetString(textDB, m_player, (int)CharacterTextDB.TextID.CANNOT_FAST_TRAVEL);
                }
            }

            return canFastTravel;
        }
        internal bool CanUseItems(ref string errorString)
        {
            bool canUseItems = true;
            if (m_currentDuelTarget != null && m_currentDuelTarget.ItemsLocked == true)
            {
                canUseItems = false;
				errorString = Localiser.GetString(textDB, m_player, (int)CharacterTextDB.TextID.CANNOT_USE_ITEM_DUEL);
            }
            for (int i = 0; i < m_areaEffectsList.Count && canUseItems == true; i++)
            {
                EntityAreaConditionalEffect currentEffect = m_areaEffectsList[i];
                if (currentEffect != null && currentEffect.TheEffect.EffectType == AreaConditionalEffect.ACE.PreventsItemUse)
                {
                    canUseItems = false;
					errorString = Localiser.GetString(textDB, m_player, (int)CharacterTextDB.TextID.CANNOT_USE_ITEM_AREA);
                }
            }

            return canUseItems;
        }
        internal override bool IsOnHateList(CombatEntity otherEntity)
        {
            /*if (otherEntity.Type == EntityType.Player)
            {
                return IsOnHateList(otherEntity.ServerID);
            }
            return false;*/
            bool isOnHateList = false;
            for (int i = 0; i < m_hateList.Count && isOnHateList == false; i++)
            {
                if (m_hateList[i] == otherEntity)
                {
                    isOnHateList = true;
                }
            }
            return isOnHateList;
        }
        private static int ComparePVPDamage(PVPDamage first, PVPDamage second)
        {
            if (first == null)
            {
                if (second == null)
                {
                    return 0;
                }

                return -1;

            }
            if (second == null)
            {
                return 1;
            }

            if (first.TotalDamage > second.TotalDamage)
            {
                return -1;
            }
            else if (first.TotalDamage < second.TotalDamage)
            {
                return 1;
            }

            return 0;
        }
        void UpdatePVPDamage(double currentTime)
        {
            if (IsPVP() == false)
            {
                //if not in pvp stop all pvp damage
                m_pvpDamages.Clear();
                return;
            }
            for (int i = m_pvpDamages.Count - 1; i >= 0; i--)
            {

                PVPDamage currentDamage = m_pvpDamages[i];
                currentDamage.Update(currentTime, this);
                if (currentDamage.IsStillValid(this) == false)
                {
                    m_pvpDamages.RemoveAt(i);
                }
            }
        }
        int GetPVPExpGainedForCharacter(Character killer,int numKills)
        {
            int exp = 0;
            int levelDiff = killer.m_pvpLevel - m_pvpLevel;
            if (levelDiff < 0)
                levelDiff = 0;
            levelDiff += numKills - 1;
            exp = (int)Math.Round(Math.Pow(Character.EXPERIENCE_ACCELLERATOR, levelDiff) * (m_pvpLevel * 10 + 90) * killer.m_pvpRating);




            return exp;

        }
        double GetPVPRatingUpgradeForCharacter(Character killer)
        {
            double currentRating = killer.m_pvpRating;

            double ratingDiff = m_pvpRating - currentRating;
            if (ratingDiff < 0)
            {
                ratingDiff = 0;
            }
            double ratingUpgrade = (m_pvpRating + ratingDiff) / 20;

            return ratingUpgrade;
        }
        void WorkOutPVPRewards(Character killer)
        {

         
            Program.Display(GetIDString() + " old rating " + m_pvpRating + ", killed by " + killer.GetIDString() + " old rating=" + killer.m_pvpRating);

         

            Party theParty = killer.CharacterParty;
            //get members in range
            List<Character> fullMembersList = null;
            if (theParty != null && m_currentDuelTarget == null)
            {
                fullMembersList = theParty.CharacterList;
            }
            List<Character> inRangeMembers = new List<Character>();
            if (fullMembersList != null)
            {
                for (int i = 0; i < fullMembersList.Count; i++)
                {
                    Character currentCharacter = fullMembersList[i];
                    //if they are in range
                    if (IsEnemyOf(currentCharacter) == true && killer.IsEnemyOf(currentCharacter) == false && (TheCombatManager == currentCharacter.TheCombatManager) && (Utilities.Difference2DSquared(currentCharacter.CurrentPosition.m_position, CurrentPosition.m_position) < Zone.MAX_PARTY_EXP_SHARE_DISTANCE_SQR))
                    {

                        //add them to the list
                        inRangeMembers.Add(currentCharacter);

                    }
                }
            }
            else
            {
                inRangeMembers.Add(killer);
            }
            int charactersToReward = inRangeMembers.Count;
            if (charactersToReward == 0)
            {
                Program.Display("attempted to reward party but no members were in range");
                return;
            }
			if (Level < 5 || killer.Level - Level > 15)
            {
                List<NetConnection> connections=new List<NetConnection>();
                for (int i = 0; i < inRangeMembers.Count; i++)
                {
                    connections.Add(inRangeMembers[i].m_player.connection);
                    if (Program.m_LogAnalytics)
                    {
                        AnalyticsMain logAnalytics = new AnalyticsMain(false);
                        logAnalytics.opponentDefeated(inRangeMembers[i].m_player, m_player.m_account_id.ToString(), m_name, 0, null);
                    }
                }
				string locText = Localiser.GetString(textDB, m_player, (int)CharacterTextDB.TextID.TOO_LOW_LV_PVP_EXP);
				locText = string.Format(locText, m_name);
				Program.processor.sendSystemMessage(locText, connections, false, SYSTEM_MESSAGE_TYPE.PVP);
                 
                return;
            }
            increaseRanking(RankingsManager.RANKING_TYPE.PVP_DEATHS, 1,false);
            setPVPKillsToDeaths();
            
            killer.increaseRanking(RankingsManager.RANKING_TYPE.PVP_KILLS, 1,false);
            killer.setPVPKillsToDeaths();
            int killerKills = killer.increasePVPKillRecord(m_character_id, 1);

            //get the highest lvl of the party

            //work out how much exp they get
            //int totalExp = mob.getExperienceValue(killerLevel);
            //add the party boost
            float expBoost = 1; //+ (charactersToReward - 1) * Party.EXPERIANCE_BOOST_PER_PLAYER;

            float totalEXPAwarded = 0;
            double totalRatingAwarded = 0;




            float MaxExpPreSplit = GetPVPExpGainedForCharacter(killer,killerKills);
            double MaxRatingPreSplit = 0;
            if (killerKills < 4)
            {
                MaxRatingPreSplit=GetPVPRatingUpgradeForCharacter(killer);
            }

            //send out the messages
            for (int i = 0; i < inRangeMembers.Count; i++)
            {
                Character currentCharacter = inRangeMembers[i];
                int numKills = 1;
                if (currentCharacter == killer)
                {
                    numKills = killerKills;
                }
                else if (MaxExpPreSplit >= charactersToReward || MaxRatingPreSplit > 0)
                {
                    numKills = currentCharacter.increasePVPKillRecord(m_character_id,1);
                }
                else
                {
                    numKills = currentCharacter.increasePVPKillRecord(m_character_id, 0) + 1;
                }

                    float currentTotalExp = GetPVPExpGainedForCharacter(currentCharacter,numKills);
                    double ratingUpgrade = 0;
                    if (numKills < 4)
                    {
                        ratingUpgrade = GetPVPRatingUpgradeForCharacter(currentCharacter);
                    }
                    Program.Display(currentCharacter.GetIDString() + " old rating " + currentCharacter.m_pvpRating);

                    //don't let them get more xp than they should
                    if (MaxExpPreSplit < currentTotalExp)
                    {
                        currentTotalExp = MaxExpPreSplit;
                    }
                    else
                    {
                        //you get the same as the killer
                    }

                    Int64 maxXP = Character.getMinPVPExperienceForNextLevel(currentCharacter.m_pvpLevel + 1) - Character.getMinPVPExperienceForNextLevel(currentCharacter.m_pvpLevel);
                    if (currentCharacter.m_pvpLevel == 30)
                    {
                        maxXP = 0;
                    }


                    currentTotalExp = (int)(currentTotalExp * expBoost);
                    Int64 expForPlayer = (int)(currentTotalExp / charactersToReward);

                    if (expForPlayer > maxXP)
                    {
                        expForPlayer = maxXP;
                    }
                    if (expForPlayer > 0)
				{
					string locText = Localiser.GetString(textDB, m_player, (int)CharacterTextDB.TextID.GAINED_PVP_EXP);
					locText = string.Format(locText, expForPlayer);
					Program.processor.sendSystemMessage(locText, currentCharacter.m_player, false, SYSTEM_MESSAGE_TYPE.PVP);
				}

                    currentCharacter.CompiledStats.PVPExperience += expForPlayer;

                    totalEXPAwarded += expForPlayer;
                    //calculate new rating


                    if (ratingUpgrade > MaxRatingPreSplit)
                    {
                        ratingUpgrade = MaxRatingPreSplit;
                    }
                    ratingUpgrade = ratingUpgrade / charactersToReward;
                    int oldpvpRating = currentCharacter.getVisiblePVPRating();
                    currentCharacter.m_pvpRating += ratingUpgrade;

                    totalRatingAwarded += ratingUpgrade;
                    if (currentCharacter.m_pvpRating > 5)
                    {
                        currentCharacter.m_pvpRating = 5;
                    }
                    int newpvpRating = currentCharacter.getVisiblePVPRating();

                    m_db.runCommandSync("update character_details set pvp_xp=" + currentCharacter.CompiledStats.PVPExperience + ",pvp_rating=" + currentCharacter.m_pvpRating + " where character_id=" + currentCharacter.m_character_id);
                    currentCharacter.SetStatsChangeLevel(STATS_CHANGE_LEVEL.BASIC_CHANGED);
    


                    if (currentCharacter.CompiledStats.PVPExperience >= Character.getMinPVPExperienceForNextLevel(currentCharacter.m_pvpLevel))
                    {
                        currentCharacter.PVPLevelUp();
                        currentCharacter.UpdatePVPLevelAchievements();
                        currentCharacter.InfoUpdated(Inventory.EQUIP_SLOT.SLOT_PVP_LEVEL);
                    }
                    if (m_pvpLevel > 0)
                    {
                        Int64 prevLevelreq=Character.getMinPVPExperienceForNextLevel(currentCharacter.m_pvpLevel - 1);
                        Int64 levelreq=Character.getMinPVPExperienceForNextLevel(currentCharacter.m_pvpLevel);
                        double pc = ((double)(currentCharacter.CompiledStats.PVPExperience - prevLevelreq)) / (levelreq - prevLevelreq);
                        currentCharacter.updateRanking(RankingsManager.RANKING_TYPE.PVP_RANKING, currentCharacter.m_pvpLevel + pc, false);

                    }
                    
                    Program.Display(currentCharacter.GetIDString() + " xp=" + expForPlayer + ", new rating " + currentCharacter.m_pvpRating + ", for killing " + GetIDString());

                    if (oldpvpRating != newpvpRating)
                    {
                        currentCharacter.InfoUpdated(Inventory.EQUIP_SLOT.SLOT_PVP_RATING);
                        currentCharacter.UpdatePVPRatingAchievements();
                    }
                    //SendWonCombat(currentCharacter.m_player, mob, expForPlayer, individualCoins, currentLootList, player.m_activeCharacter.m_character_id);
                    if (Program.m_LogAnalytics)
                    {
                        AnalyticsMain logAnalytics = new AnalyticsMain(false);
                        logAnalytics.opponentDefeated(currentCharacter.m_player, m_player.m_account_id.ToString(), m_name, 0, null);
                    }
            }
            int oldVictimRating = getVisiblePVPRating();
            m_pvpRating -= totalRatingAwarded;
            if (m_pvpRating < 0.001f)
            {
                m_pvpRating = 0.001f;
            }
            Program.Display(GetIDString() + " new rating " + m_pvpRating);
            m_db.runCommandSync("update character_details set pvp_rating=" + m_pvpRating + " where character_id=" + m_character_id);
            int newVictimRating = getVisiblePVPRating();
            if (newVictimRating != oldVictimRating)
            {
                InfoUpdated(Inventory.EQUIP_SLOT.SLOT_PVP_RATING);
            }
        }

        internal void setPVPKillsToDeaths()
        {
            if(m_characterRankings!=null)
            {
                double kills=0;
                double deaths=0;
                Ranking killsRanking= m_characterRankings.getRanking(RankingsManager.RANKING_TYPE.PVP_KILLS);
                Ranking deathsRanking= m_characterRankings.getRanking(RankingsManager.RANKING_TYPE.PVP_DEATHS);
                if(killsRanking!=null)
                {
                    kills=killsRanking.m_value;
                }
                if(deathsRanking!=null)
                {
                    deaths=deathsRanking.m_value;
                }
                if(kills+deaths>20)
                {
                    updateRanking(RankingsManager.RANKING_TYPE.PVP_KILLS_V_DEATHS, (100 * kills) / (kills + deaths), true);
                }
            }
        }
        internal void setPVEKillsToDeaths()
        {
            if (m_characterRankings != null)
            {
                double kills = 0;
                double deaths = 0;
                Ranking killsRanking = m_characterRankings.getRanking(RankingsManager.RANKING_TYPE.ENEMIES_KILLED);
                Ranking deathsRanking = m_characterRankings.getRanking(RankingsManager.RANKING_TYPE.NUMBER_OF_DEATHS);
                if (killsRanking != null)
                {
                    kills = killsRanking.m_value;
                }
                if (deathsRanking != null)
                {
                    deaths = deathsRanking.m_value;
                }
                if (kills + deaths > 100)
                {
                    updateRanking(RankingsManager.RANKING_TYPE.PVE_KILLS_V_DEATHS, (100 * kills) / (kills + deaths), true);
                }
            }
        }

        void AddPVPDamage(int damage, double currentTime, Character characterCausingDamage)
        {
            PVPDamage damageRecord = null;
            //first see if the character is already being held
            for (int i = 0; i < m_pvpDamages.Count && damageRecord == null; i++)
            {
                if (m_pvpDamages[i].CharacterInvolved == characterCausingDamage)
                {
                    damageRecord = m_pvpDamages[i];
                }
            }
            //if not then create a record
            if (damageRecord == null)
            {
                damageRecord = new PVPDamage(characterCausingDamage);
                m_pvpDamages.Add(damageRecord);
            }

            // add this damage to the record
            damageRecord.AddDamage(damage, currentTime);
        }

        Character GetPVPKiller()
        {
            for (int i = m_pvpDamages.Count - 1; i >= 0; i--)
            {

                PVPDamage currentDamage = m_pvpDamages[i];
                currentDamage.CalculateTotalDamage();
                if (currentDamage.IsStillValid(this) == false)
                {
                    m_pvpDamages.RemoveAt(i);
                }
            }
            //get the highest individual damage to the top
            m_pvpDamages.Sort(ComparePVPDamage);
            //now add any joint efforts to the list
            for (int i = 0; i < m_pvpDamages.Count; i++)
            {
                PVPDamage currentAggroData = m_pvpDamages[i];

                if (currentAggroData != null)
                {
                    CombatEntity combatEnt = currentAggroData.CharacterInvolved;
                    //Are they a character
                    if (combatEnt != null && combatEnt.Type == CombatEntity.EntityType.Player)
                    {
                        Character currentCharacter = (Character)combatEnt;
                        //do they have a party
                        if (currentCharacter.CharacterParty != null)
                        {
                            //if so compile the entire parties damage
                            for (int j = i + 1; j < m_pvpDamages.Count; j++)
                            {
                                PVPDamage aggroData = m_pvpDamages[j];
                                if (aggroData != null)
                                {
                                    combatEnt = aggroData.CharacterInvolved;
                                    if (combatEnt != null && combatEnt.Type == CombatEntity.EntityType.Player)
                                    {
                                        Character possiblePartyCharacter = (Character)combatEnt;
                                        if (currentCharacter.CharacterParty == possiblePartyCharacter.CharacterParty && currentCharacter.IsEnemyOf(possiblePartyCharacter) == false)
                                        {
                                            currentAggroData.TotalDamage += aggroData.TotalDamage;
                                            m_pvpDamages.Remove(aggroData);
                                            j--;
                                        }
                                    }
                                }

                            }
                        }
                    }

                }
            }
            Character killer = null;
            //now Get the highest group effort
            m_pvpDamages.Sort(ComparePVPDamage);

            if (m_pvpDamages.Count > 0)
            {
                //check every one on the aggro list untill the killer is found
                for (int i = 0; i < m_pvpDamages.Count && killer == null; i++)
                {
                    PVPDamage currentAggroData = m_pvpDamages[i];

                    if (currentAggroData != null)
                    {
                        //get the entity
                        CombatEntity combatEnt = currentAggroData.CharacterInvolved;
                        //Are they a character
                        if (combatEnt != null && combatEnt.Type == CombatEntity.EntityType.Player)
                        {
                            //check they can be rewarded
                            //otherwise go to the next group
                            Character currentCharacter = (Character)combatEnt;
                            bool isInRange = currentCharacter.PlayerOrPartyIsInRange(CurrentPosition.m_position, Zone.MAX_PARTY_EXP_SHARE_DISTANCE_SQR, TheCombatManager);
                            if (isInRange == true)
                            {
                                killer = currentCharacter;
                            }
                        }
                    }
                }
            }


            return killer;
        }
        #endregion

        

        internal override void SetStatsChangeLevel(STATS_CHANGE_LEVEL newLevel)
        {
            if (m_statsChangedLevel < newLevel)
            {
                m_statsChangedLevel = newLevel;
                m_statsUpdated = true;
            }
        }
        void UpdateBaseStats()
        {
            m_baseStats.Focus = m_baseFocus;
            m_baseStats.Dexterity = m_baseDexterity;
            m_baseStats.Strength = m_baseStrength;
            m_baseStats.Vitality = m_baseVitality;

            //MaxSpeed = 6.0f;
            MaxSpeed = 4.2f;
            m_baseStats.ExpRate = 1;
            m_baseStats.FishingExpRate = 1;
            m_baseStats.AbilityRate = 1;
        }
        internal override void AddAbiltiesToStats(CombatEntityStats statsToAddTo)
        {
            CES_AbilityHolder.AddCharacterAbilitiesToList(statsToAddTo.Abilities, m_abilities);
        }

        internal int getVisiblePVPRating()
        {
            return PVP_RatingLookupManager.getRatingID(m_pvpRating);
        }
        internal bool HasMovedSince(DateTime lastTime)
        {
            bool hasMoved = false;

            if (m_currentPosition.m_currentSpeed > 0)
            {
                hasMoved = true;
            }
                //they may have just teleported in
            else if (TheCharactersPath.m_pastPath.Count == 0)
            {
                hasMoved = true;
            }
            else
            {
                CharacterPathMarker lastMarker=TheCharactersPath.m_pastPath.Last();
                double lastSendSecondsSinceRef = (lastTime-Program.m_referenceDate).TotalSeconds;
                if (lastMarker != null && lastMarker.TimeStamp > lastSendSecondsSinceRef)
                {
                    hasMoved = true;
                }
            }

            return hasMoved;
        }

        internal void bankTransfer(bool transferToBank, int inventory_id, int template_id, int quantity)
        {
            if (quantity <= 0)
            {
                return;
            }
            if (transferToBank)
            {
                Item item = m_inventory.findBagItemByInventoryID(inventory_id, template_id);
                if (item!=null && item.m_quantity >= quantity)
                {
                    Program.Display(m_name + " transfering " + quantity + " of " + item.m_template.m_item_name + " template ID:" + item.m_template_id + " inventory ID:" + item.m_inventory_id + " to bank");

                    int remainder = item.m_quantity - quantity;
                    Item newItem = new Item(item);
                    newItem.m_quantity = quantity;
                    newItem.m_sortOrder = m_SoloBank.getNextSortOrder();
                    Item transferedItem = m_SoloBank.AddExistingItemToCharacterInventory(newItem);
                    if (transferedItem != null)
                    {
                        m_inventory.DeleteItem(template_id, inventory_id, item.m_quantity);
                        if (remainder > 0)
                        {
                            m_inventory.AddNewItemToCharacterInventory(template_id, remainder, false);
                        }
                        Program.Display(m_name + " transfer to bank successful leaving  " + remainder + " of " + item.m_template.m_item_name + " in inventory and "+transferedItem.m_quantity+" in bank");

                    }
                    m_QuestManager.checkIfItemAffectsStage(template_id);
                    
                }
            }
            else
            {
                Item item = m_SoloBank.findBagItemByInventoryID(inventory_id, template_id);
                if (item != null&&item.m_quantity >= quantity)
                {
                    Program.Display(m_name + " transfering " + quantity + " of " + item.m_template.m_item_name + " template ID:" + item.m_template_id + " inventory ID:" + item.m_inventory_id + " from bank");

                    int remainder = item.m_quantity - quantity;
                    Item newItem = new Item(item);
                    newItem.m_quantity = quantity;
                    newItem.m_sortOrder = m_inventory.getNextSortOrder();
                    Item transferedItem = m_inventory.AddExistingItemToCharacterInventory(newItem);
                    if (transferedItem != null)
                    {
                        m_SoloBank.DeleteItem(template_id, inventory_id, quantity);
                        Program.Display(m_name + " out of bank transfer successful leaving  " + remainder + " of " + item.m_template.m_item_name + " in bank and " + transferedItem.m_quantity + " in inventory, new id="+transferedItem.m_inventory_id);

                    }

                    m_QuestManager.checkIfItemAffectsStage(template_id);
                }
            }
        }
        internal void SignpostsNeedRechecked()
        {
            m_signpostManager.RecheckSignposts = true;
        }
        internal void SaveTutorialCompleted(int tutorialID)
        {
            

            if (m_completedTutorialList.Contains(tutorialID)==false)
            {

                if (m_signpostManager != null)
                {
                    m_signpostManager.RecheckSignposts = true;
                }
                m_completedTutorialList.Add(tutorialID);
                Program.processor.m_worldDB.runCommand("insert into character_completed_tutorials (character_id,tutorial_id) values (" + m_character_id + "," + tutorialID + ")");
            }
            // m_completedTutorialList
        }

        internal void SaveFirstTimeCompleted(int firstTimeID)
        {

            if (m_completedFirstTimeList.Contains(firstTimeID) == false)
            {
                m_completedFirstTimeList.Add(firstTimeID);
                Program.processor.m_worldDB.runCommand("insert into character_completed_firsttimes (character_id,firsttime_id) values (" + m_character_id + "," + firstTimeID + ")");

                if (m_signpostManager != null)
                {

                    m_signpostManager.RecheckSignposts = true;
                }
            }
        }

        internal bool FirstTimeComplete(int firstTimeID)
        {

            return m_completedFirstTimeList.Contains(firstTimeID);
        }

        internal bool TutorialComplete(int tutorialID)
        {

            return m_completedTutorialList.Contains(tutorialID);
            // m_completedTutorialList
        }
        void readCompletedTutorialsFromDatabase()
        {
            SqlQuery tutorialQuery = new SqlQuery(Program.processor.m_worldDB, "select * from character_completed_tutorials where character_id =" + m_character_id);
            m_completedTutorialList.Clear();
            if (tutorialQuery.HasRows)
            {
                
                while (tutorialQuery.Read())
                {
                    int tutorialID = tutorialQuery.GetInt32("tutorial_id");
                    m_completedTutorialList.Add(tutorialID);
                }
            }

        }
        void readCompletedFirstTimeFromDatabase()
        {

            SqlQuery firstTimeQuery = new SqlQuery(Program.processor.m_worldDB, "select * from character_completed_firstTimes where character_id =" + m_character_id);
            m_completedFirstTimeList.Clear();

            if (firstTimeQuery.HasRows)
            {

                while (firstTimeQuery.Read())
                {

                    int firstTimeID = firstTimeQuery.GetInt32("firstTime_id");
                    m_completedFirstTimeList.Add(firstTimeID);
                }
            }
        }
        override internal void EntityHasChangedStatusList(CombatEntity theEntity)
        {
            //add the entity to the List of entities whos status effects have changed
            if (!m_changedStatusEffectList.Contains(theEntity))
            {
                m_changedStatusEffectList.Add(theEntity);
            }
        }

        override internal void ForgetStatusEffectsOnEntity(CombatEntity theEntity)
        {
            //add this Entity to the List of Entities to forget
            if (!m_entitiesToForgetList.Contains(theEntity))
            {
                m_entitiesToForgetList.Add(theEntity);
            }
        }

        void SendOtherEntitiesStatusEffects(double currentTime)
        {



            if (m_entitiesToForgetList.Count == 0 && m_changedStatusEffectList.Count == 0)
            {
                //no need to send anything if empty
                return;
            }

            NetOutgoingMessage statusEffectMsg = Program.Server.CreateMessage();
            statusEffectMsg.WriteVariableUInt32((uint)NetworkCommandType.OthersStatusEffect);


            //people to remember 1st
            statusEffectMsg.WriteVariableInt32(m_changedStatusEffectList.Count);
            for (int i = 0; i < m_changedStatusEffectList.Count; i++)
            {
                CombatEntity currentEntity = m_changedStatusEffectList[i];
                statusEffectMsg.Write((byte)currentEntity.Type);
                statusEffectMsg.WriteVariableUInt32((uint)currentEntity.ServerID);
                currentEntity.WriteStatusEffectsToMessage(statusEffectMsg, currentTime);
            }


            //people to forget last
            statusEffectMsg.WriteVariableInt32(m_entitiesToForgetList.Count);
            for (int i = 0; i < m_entitiesToForgetList.Count; i++)
            {
                CombatEntity currentEntity = m_entitiesToForgetList[i];
                statusEffectMsg.Write((byte)currentEntity.Type);
                statusEffectMsg.WriteVariableUInt32((uint)currentEntity.ServerID);
                statusEffectMsg.WriteVariableInt32(0);
            }
            m_changedStatusEffectList.Clear();
            m_entitiesToForgetList.Clear();

            //send the message
            Program.processor.SendMessage(statusEffectMsg, m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.OthersStatusEffect);

        }
        override internal void DealWithChangeInStatusEffects(double currentTime)
        {
            if (StatusListChanged == true)
            {
                NotifyAllInterestedOfStatusEffectChange();
                if (m_player != null)
                {
                    CurrentZone.SendCharactersUpdatedStatus(m_player, currentTime);
                }
                StatusListChanged = false;
            }
        }
        internal override Vector3 GetCombatLocation(CombatEntity aggressor)
        {
            if (aggressor != this && aggressor.Type == EntityType.Player)
            {
                return TheCharactersPath.GetRollbackPositionFromCurrentTime(Program.MainUpdateLoopStartTime(), m_currentPosition.m_position);
            }
            return m_currentPosition.m_position;
        }
        internal override bool IsAllyOf(CombatEntity otherEntity)
        {
            if (otherEntity == this)
            {
                return true;
            }

            bool isAlly = (IsEnemyOf(otherEntity) == false);

            if (isAlly == true && otherEntity.Type == EntityType.Player)
            {
                for (int i = 0; i < m_pvpTypes.Count && isAlly == true; i++)
                {
                    PVPType currentType = m_pvpTypes[i];
                    if (currentType != PVPType.CanDuel)
                    {
                        if (currentType == PVPType.Duel)
                        {
                            isAlly = false;
                        }
                        else
                        {
                            if (otherEntity.IsInPVPType(currentType) == false)
                            {
                                isAlly = false;
                            }
                        }
                    }

                }
            }


            return isAlly;
        }
        internal int increasePVPKillRecord(uint victim_character_id,int increase)
        {
            for (int i = 0; i < m_pvpKills.Count; i++)
            {
                if (m_pvpKills[i].m_victim_character_id == victim_character_id)
                {
                    if (m_pvpKills[i].m_killDate < DateTime.Today)
                    {
                        m_pvpKills[i].m_num_kills = increase;
                        m_pvpKills[i].m_killDate = DateTime.Today;
                    }
                    else
                    {
                        m_pvpKills[i].m_num_kills+=increase;

                    }
                    return m_pvpKills[i].m_num_kills;
                }
            }
            if (increase > 0)
            {
                PVPKillRecord newRecord = new PVPKillRecord();
                newRecord.m_killDate = DateTime.Today;
                newRecord.m_num_kills = increase;
                newRecord.m_victim_character_id = (int)victim_character_id;
                m_pvpKills.Add(newRecord);
            }
            return increase;
        }

        /// <summary>
        /// Using the enum NetworkCommandType.SimpleMessageForThePlayer, send the player a message popup
        /// </summary>
        /// <param name="message"></param>
        internal void SendSimpleMessageToPlayer(string message)
        {
            NetOutgoingMessage msg = Program.Server.CreateMessage();
            msg.WriteVariableUInt32((uint)NetworkCommandType.SimpleMessageForThePlayer);
            msg.Write(message);
            Program.processor.SendMessage(msg, m_player.connection, NetDeliveryMethod.ReliableOrdered,
                NetMessageChannel.NMC_Normal, NetworkCommandType.SimpleMessageForThePlayer);

        }

        #region ITargetOwner Members

        public bool ResignOwnership(CombatEntity theEntity)
        {
            bool ownershipResigned = false;
            if (m_lockedList.Contains(theEntity) == true)
            {
                m_lockedList.Remove(theEntity);
                if (theEntity.LockOwner == this)
                {
                    theEntity.LockOwner = null;
                }
            }
            return ownershipResigned;
        }

        public bool TakeOwnership(CombatEntity theEntity)
        {
            bool ownershipTaken = false;
            if (m_lockedList.Contains(theEntity) == false)
            {
                if (theEntity.LockOwner!=null)
                {
                    theEntity.LockOwner.ResignOwnership(theEntity);
                }
                m_lockedList.Add(theEntity);
                theEntity.LockOwner = this;
                ownershipTaken = true;
            }
            return ownershipTaken;
        }

        public bool HasOwnership(CombatEntity theEntity)
        {
            bool targetFound = false;
            if (CharacterParty != null)
            {
                targetFound = CharacterParty.HasOwnership(theEntity);
            }
            else
            {
                targetFound = m_lockedList.Contains(theEntity);
            }
            return targetFound;
        }

        public List<Character> GetCharacters
        {
            get { 
                List<Character> charList = new List<Character>();
                charList.Add(this);
                return charList;
                }
        }
        public List<CombatEntity> GetCurrentLocks
        {
            get { return m_lockedList; }
        }
        /// <summary>
        /// Send Any client notification required when a lock is made
        /// </summary>
        /// <param name="theEntity">the entity that this has just become the owner for</param>
        public void NotifyOwnershipTaken(CombatEntity theEntity)
        {
            if (m_player != null)
            {
              //  Program.processor.SendPlaySound2D(m_player, "player_hit_by_cat");
            }
             
        }
        #endregion

		#region gathering overloads

		internal override int GetRelevantLevel(CombatEntity entity)
		{
			switch (entity.Gathering)
			{
				case (LevelType.none):
					return this.Level;

				case (LevelType.fish):					
			        if (LevelFishing == 0)
			            return 1;
                    return this.LevelFishing;
                case (LevelType.cook):
			        if (LevelCooking == 0)
			            return 1;
			        return this.LevelCooking;

			}
			return 0;
		}

		/// <summary>
		/// THIS NEEDS TO BE IMPLEMENTER - right now returns current player level
		/// </summary>
		/// <param name="questTemplate"></param>
		/// <returns>FINISH IMPLEMENTATION</returns>
		internal int GetRelevantLevel(QuestTemplate questTemplate)
		{

            if (questTemplate.m_quest_type == 0)
            {
                return Level;
            }
            if (questTemplate.m_quest_type == 1)
            {
                return LevelFishing;
            }
            
            return LevelCooking;
     
		}

		private int GetRelevantLevel(CharacterAbility ability)
		{
			//if we're a crafting/gathering level, use a different check
			switch (ability.m_ability_id)
			{
                //gathering/crafting here
                case (ABILITY_TYPE.BEARTAMING):
                    return this.LevelFishing;
				case (ABILITY_TYPE.FISHING):
					return this.LevelFishing;
                case(ABILITY_TYPE.COOKING_PROFICIENCY):
			        return this.LevelCooking;
                case(ABILITY_TYPE.COOKING_MASTERY):
			        return this.LevelCooking;
						default:
					return Level;

			}
		}

        internal override void TakeDamage(CombatDamageMessageData theDamage)
        {
	        if (theDamage.CasterLink == null)
	        {
		        base.TakeDamage(theDamage);
		        return;
	        }

	        switch (theDamage.CasterLink.Gathering)
            {
                case (LevelType.none):
                    theDamage.Gathering = LevelType.none;
                    base.TakeDamage(theDamage);                    
                    break;
                case (LevelType.fish):
                    theDamage.Gathering = LevelType.fish;
                    base.TakeDamage(theDamage);                    
                    break;

            }
        }

	    internal int GetFishingAttack()
	    {
			//#FISH

		    int level = LevelFishing;
		    if (level == 0)
			    level = 1;
		    int ability = getAbilityLevel(ABILITY_TYPE.FISHING);

			//attack is a some magic number based on these values
			//looks like a simple addition
			//check CombatEntity.compileStats() -> uses getWeaponAbilityModifier() for swords etc...then adds the ability level to attack
		    return ability + 5;
	    }

	    internal void WriteMessageForConcentrationBroken()
	    {
			//if our current concentration is zero, we in fact did not interrupt, we lost fishing, so skip this message
			if(this.CurrentConcentrationFishing <=0 )
				return;

			string locText = Localiser.GetString(textDB, m_player, (int)CharacterTextDB.TextID.FISHING_CANCELLED);
			SendSimpleMessageToPlayer(locText);	
	    }

		internal void WriteMessageForConcentrationAtZero()
		{
			string locText = Localiser.GetString(textDB, m_player, (int)CharacterTextDB.TextID.CONT_ZERO_FISHING);
			SendSimpleMessageToPlayer(locText);
		}
        
		#endregion
        
	}
}


