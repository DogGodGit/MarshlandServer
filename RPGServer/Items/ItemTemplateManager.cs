using System;
using System.Collections.Generic;
using System.Linq;
using MainServer.Items;
using MainServer.Localise;

namespace MainServer
{

    public enum REQUIREMENT_TYPE
    {
        STRENGTH_REQUIREMENT = 0,
        DEXTERITY_REQUIREMENT = 1,
        FOCUS_REQUIREMENT = 2,
        VITALITY_REQUIREMENT = 3,
        GENDER_REQUIREMENT = 4,
        LEVEL_REQUIREMENT = 5
    }
    public enum STAT_TYPE
    {
        NA=-1,
        STRENGTH = 0,
        DEXTERITY = 1,
        FOCUS = 2,
        VITALITY= 3
    }

    public class CharacterModifiers
    {

        public enum MODIFIER_TYPES
        {
            None = 0,
            Stats = 1,
            Abilities = 2,
            Skills = 3,
        };

        int m_attributeID = 0;
        int m_amount = 0;
        MODIFIER_TYPES m_modType = MODIFIER_TYPES.None;

        public CharacterModifiers(MODIFIER_TYPES type, int attributeID, int attributeAmount)
        {
            m_modType = type;
            m_attributeID = attributeID;
            m_amount = attributeAmount;

        }
        internal void ApplyToStatsAddition(CombatEntityStats theStats)
        {
            switch(m_modType)
            {
                case MODIFIER_TYPES.Abilities:
                    theStats.AddToAbilityLevel((ABILITY_TYPE)m_attributeID, m_amount);
                    break;
                case MODIFIER_TYPES.Skills:
                    theStats.AddToSkillLevel((SKILL_TYPE)m_attributeID, m_amount);
                    break;
                case MODIFIER_TYPES.Stats:
                    AddToDefaultAttributes(theStats);
                    break;
                default:
                    Program.Display("ApplyToStatsAddition encountered unknown type");
                    break;

            }

        }
        /// <summary>
        /// Used to add to vit/str ect.
        /// </summary>
        void AddToDefaultAttributes(CombatEntityStats theStats)
        {
            STAT_TYPE attribute = (STAT_TYPE)m_attributeID;

            switch (attribute)
            {
                case STAT_TYPE.STRENGTH:
                    {
                        theStats.Strength += m_amount;
                        break;
                    }
                case STAT_TYPE.DEXTERITY:
                    {
                        theStats.Dexterity += m_amount;
                        break;
                    }
                case STAT_TYPE.FOCUS:
                    {
                        theStats.Focus += m_amount;
                        break;
                    }
                case STAT_TYPE.VITALITY:
                    {
                        theStats.Vitality += m_amount;
                        break;
                    }
                default:
                    break;
            }
        }
    }


    public class ItemStatusEffect
    {
        public ItemStatusEffect(EFFECT_ID effect_id, int level)
        {
            m_effect_id = effect_id;
            m_level=level;
        }
        public EFFECT_ID m_effect_id;
        public int m_level;
    }
    public class FloatForID
    {
        public FloatForID(int bonusType, float amount)
        {
            m_bonusType = bonusType;
            m_amount = amount;
        }
        public int m_bonusType;
        public float m_amount;
        internal static FloatForID GetEntryForID(List<FloatForID> entryList, int entryID)
        {
            FloatForID entryForID = null;
            if (entryList != null)
            {
                for (int i = 0; i < entryList.Count && entryForID==null; i++)
                {
                    if (entryList[i].m_bonusType == entryID)
                    {
                        entryForID = entryList[i];
                    }
                }
            }

            return entryForID;
        }
        internal static void AddAlITypesToList(List<FloatForID> entryList, List<int> types)
        {
            for (int i = 0; i < entryList.Count; i++)
            {
                if (types.Contains(entryList[i].m_bonusType)==false)
                {
                    types.Add(entryList[i].m_bonusType);
                }
            }
        }
        internal static void RemoveEntryForeTypeID(List<FloatForID> entryList, int entryID)
        {
            for (int i = 0; i < entryList.Count; i++)
            {
                if (entryList[i].m_bonusType == entryID)
                {
                    entryList.Remove(entryList[i]);
                }
            }
        }
    }

    public class ItemTemplate
    {
		// #localisation
		public class ItemTemplateTextDB : TextEnumDB
		{
			public ItemTemplateTextDB() : base(nameof(ItemTemplate), typeof(TextID)) { }

			public enum TextID
			{
				FISHING_LEVEL_REQUITED,         //"Fishing Level {level0} required to equip this item."
				NOT_MEET_REQUIREMENT_EQUIP,     //"You do not meet the requirements to equip this item."
				ITEM_FOR_ANOTHER_CLASS,         //"This item is for another class."
			}
		}
		public static ItemTemplateTextDB textDB = new ItemTemplateTextDB();

		static public int DEFAULT_ARROW_ID = 15038;
        static public int OLD_RES_ITEM_ID = 20;
        static public int RES_ITEM_ID = 36759;//20;
        static public int RES_ITEM_ID_NOTRADE = 58553;
        static public float DEFAULT_ATTACK_RANGE = 1.0f;
        static public int MAX_INFINITE_ARROW_ID = 1000;
        
		public enum ITEM_SUB_TYPE
        {
            NONE = 0,
            SWORD = 1,
            AXE = 2,
            BLUNT = 3,
            CLOTH = 4,
            LEATHER = 5,
            CHAIN = 6,
            PLATE = 7,
            STAFF = 8,
            DAGGER = 9,
            WAND = 10,
            BOW = 11,
            SHIELD = 12,
            SWORD_TWO_HANDED = 13,
            AXE_TWO_HANDED = 14,
            BLUNT_TWO_HANDED = 15,
            ARROW=16,
            SPEAR = 19,
            ONE_HANDED_STAFF =20,
            BROOM = 21,
            SLEDGE = 22,
            HAND_TO_HAND = 23,
            FASHION = 24,
            JEWELRY = 25,
            MAGIC_CARPET = 26,
            NOVELTY_BROOM = 27,
            NOVELTY_WAND = 28,
            NOVELTY_LUTE = 29,
            NOVELTY_DRAGONSTAFF = 30,
            NOVELTY_FLUTE = 31,
            NOVELTY_HARP = 32,
            NOVELTY_TWO_HANDED = 33,
            NOVELTY_STAFF_MOUNT = 34,
            NOVELTY_HORN = 35,
            NOVELTY_BLUNT = 36,
            NOVELTY_BATMOUNT = 37,
            NOVELTY_ANGEL_WINGS = 38,
            NOVELTY_DRUM = 39,
            NOVELTY_BAGPIPES = 40,
            NOVELTY_EAGLEMOUNT = 41,
            TEST=42,
            NOVELTY_CROW = 43,
            NOVELTY_SPARROW = 44,
            NOVELTY_SPARROWHAWK = 45,
            NOVELTY_SPIRITCAPE = 46,
            NOVELTY_HORSEMOUNT = 47,
            NOVELTY_BANSHEE_BLADE = 48,
            NOVELTY_BONE_BIRD = 49,
            NOVELTY_HELL_WINGS = 50,
            PLAY_DEAD = 51,
            BANNER = 52,
            NOVELTY_BOARMOUNT = 53,
            FISHING_ROD = 54,
            TOTEM_LONG = 55,
            OFFHAND_BOOK = 56,
            SPEAR_TWO_HANDED = 57,
            PET_FOOD = 58,
            FISHING_ITEM = 59,
			TOKEN = 60,
			CONSUMABLE = 61,
            COOKING_ITEM = 66
        }
        public enum WEAPON_EQUIP_TYPE
        {
            NONE=0,
            ONE_HANDED=1,
            TWO_HANDED=2,
            BOW=3
        }

        #region variables
        public int m_item_id;
        public string m_item_name;
		public string[] m_loc_item_name;
		public bool m_stackable;
        public int m_armour;
        public int m_attack_speed;
        public int m_slotNumber;
        public int m_buyprice;
        public int m_sellprice;
        public int m_weight;
        public bool m_noTrade;
        public bool m_autoUse = false;
        public float m_attackRange = 1;
     
        public ITEM_SUB_TYPE m_subtype;
        //public float[] m_DamageTypes=new float[CombatEntity.NUM_DAMAGE_TYPES];
        public List<FloatForID> m_damageTypes = new List<FloatForID>();
        //public int[] m_BonusTypes=new int[CombatEntity.NUM_BONUS_TYPES];
        public List<FloatForID> m_bonusTypes = new List<FloatForID>();
        //public int[] m_AvoidanceTypes = new int[CombatEntity.NUM_AVOIDANCE_TYPES];
        public List<FloatForID> m_avoidanceTypes = new List<FloatForID>();
        public List<FloatForID> m_additionalBonus = new List<FloatForID>();

        internal List<FloatForID> m_immunityTypes = new List<FloatForID>();
        internal List<FloatForID> m_damageReductionTypes = new List<FloatForID>();

        public int[] m_RequirementTypes=new int[6];
        public List<CLASS_TYPE> m_classRestrictions=new List<CLASS_TYPE>();
        public SKILL_TYPE m_SklllEffect;
        public int m_SkillLevel;
        public float m_reportTimeMale=0;
        public float m_reportTimeFemale=0; 
        public float m_projectileSpeed=0;
        public List<ItemStatusEffect> m_statusEffects=new List<ItemStatusEffect>();
        List<Inventory.EQUIP_SLOT> m_blockedSlots=new List<Inventory.EQUIP_SLOT>();

        public int m_procSkillID = 0;
        public int m_procSkillLevel = 0;
        public float m_procSkillChance = 0;

        public int m_equipSkillID = 0;
        public int m_equipSkillLevel = 0;

        public int m_uniqueID = 0;
        public bool m_bindOnEquip = false;
        public int m_maxCharges = 0;
        public bool m_destroyOnNoCharge = true;

        public List<CharacterModifiers> m_modifiers = new List<CharacterModifiers>();
        public List<EquipmentSet> m_equipmentSets = new List<EquipmentSet>();
       // List<LootSetHolder> m_lootSets = new List<LootSetHolder>();

        public List<CombatModifiers> m_combatModifiers = new List<CombatModifiers>();

        internal List<Inventory.EQUIP_SLOT> BlockedSlots
        {
            get { return m_blockedSlots; }
        }

        #endregion

        #region Constructors

        public ItemTemplate(Database db, SqlQuery query, LocalisedTextDB textDB)
        {

            for (int i = 0; i < m_RequirementTypes.Length; i++)
            {
                m_RequirementTypes[i] = 0;
            }
            m_item_id = query.GetInt32("item_id");
            m_item_name = query.GetString("item_name");
			m_loc_item_name = Localiser.GetStringArray(textDB, m_item_id);
			m_stackable = query.GetBoolean("stackable");
            m_armour = query.GetInt32("armour");
            m_slotNumber = query.GetInt32("equipment_slot");
            m_buyprice = query.GetInt32("buy_price");
            m_sellprice = query.GetInt32("sell_price");
            m_attack_speed = query.GetInt32("attack_speed");
            m_weight = query.GetInt32("weight");
            m_subtype = (ITEM_SUB_TYPE)query.GetInt32("item_sub_type");
            m_noTrade = query.GetBoolean("no_trade");
            m_attackRange = query.GetFloat("attack_range");
            if (m_attackRange == 0)
            {
                m_attackRange = DEFAULT_ATTACK_RANGE;
            }

            string damagelist = query.GetString("damage_list");
            string[] damagelistsplit = damagelist.Split(new char[] { ';' },StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < damagelistsplit.Length; i++)
            {
                string[] subsplit=damagelistsplit[i].Split(new char[]{'^'},StringSplitOptions.RemoveEmptyEntries);
                int dt=Int32.Parse(subsplit[0]);
                float amount=(float)Double.Parse(subsplit[1]);
                m_damageTypes.Add(new FloatForID(dt, amount));
                /*if (dt < m_DamageTypes.Length)
                {
                    m_DamageTypes[dt] = amount;
                }*/
            }

            string bonuslist = query.GetString("bonus_list");
            string[] bonuslistsplit = bonuslist.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < bonuslistsplit.Length; i++)
            {
                string[] subsplit = bonuslistsplit[i].Split(new char[] { '^' }, StringSplitOptions.RemoveEmptyEntries);
                int bt = Int32.Parse(subsplit[0]);
                int amount = Int32.Parse(subsplit[1]);
                m_bonusTypes.Add(new FloatForID(bt,amount));
                
            }

            string damageReductionlist = query.GetString("damage_reductions_list");
            string[] damageReductionlistsplit = damageReductionlist.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < damageReductionlistsplit.Length; i++)
            {
                string[] subsplit = damageReductionlistsplit[i].Split(new char[] { '^' }, StringSplitOptions.RemoveEmptyEntries);
                int bt = Int32.Parse(subsplit[0]);
                float amount = float.Parse(subsplit[1]);
                m_damageReductionTypes.Add(new FloatForID(bt, amount));

            }
            string immunitylist = query.GetString("immunity_list");
            string[] immunitylistsplit = immunitylist.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < immunitylistsplit.Length; i++)
            {
                string[] subsplit = immunitylistsplit[i].Split(new char[] { '^' }, StringSplitOptions.RemoveEmptyEntries);
                int bt = Int32.Parse(subsplit[0]);
                float amount = float.Parse(subsplit[1]);
                m_immunityTypes.Add(new FloatForID(bt, amount));
                
            }
            

            string avoidanceBonuses = query.GetString("avoidance_bonuses");
            string[] avoidancelistsplit = avoidanceBonuses.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < avoidancelistsplit.Length; i++)
            {
                string[] subsplit = avoidancelistsplit[i].Split(new char[] { '^' }, StringSplitOptions.RemoveEmptyEntries);
                int at = Int32.Parse(subsplit[0]);
                int amount = Int32.Parse(subsplit[1]);

                m_avoidanceTypes.Add(new FloatForID(at, amount));
               /* if (at < m_AvoidanceTypes.Length)
                {
                    m_AvoidanceTypes[at] = amount;
                }*/
            }

            string requirementlist = query.GetString("requirement_list");
            string[] requirementlistsplit = requirementlist.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < requirementlistsplit.Length; i++)
            {
                string[] subsplit = requirementlistsplit[i].Split(new char[] { '^' }, StringSplitOptions.RemoveEmptyEntries);
                int rt = Int32.Parse(subsplit[0]);
                int amount = Int32.Parse(subsplit[1]);
                m_RequirementTypes[rt] = amount;
            }
            string[] classRestrictions = query.GetString("class_requirement_list").Split(new char[]{','},StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < classRestrictions.Length; i++)
            {
                //m_blockedSlots.Add((Inventory.EQUIP_SLOT)Int32.Parse(classRestrictions[i]));
                m_classRestrictions.Add((CLASS_TYPE)Int32.Parse(classRestrictions[i]));
            }
            string[] blocksSlots = query.GetString("blocks_slots").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < blocksSlots.Length; i++)
            {
                m_blockedSlots.Add((Inventory.EQUIP_SLOT)Int32.Parse(blocksSlots[i]));
                //m_classRestrictions.Add((CLASS_TYPE)Int32.Parse(classRestrictions[i]));
            }
            m_SklllEffect = (SKILL_TYPE)query.GetInt32("skill_id");
            m_SkillLevel = query.GetInt32("skill_level");

            m_autoUse = (m_item_id == (int)PERMENENT_BUFF_ID.BACKPACK ||
                            m_item_id == (int)PERMENENT_BUFF_ID.ENERGY_REGEN_1 ||
                            m_item_id == (int)PERMENENT_BUFF_ID.HEALTH_REGEN_1 ||
                            m_item_id == (int)PERMENENT_BUFF_ID.EXTRA_HUD_SLOT ||
                            m_item_id == (int)PremiumShop.INSTANT_USE_ITEM_ID.COIN_BAG ||
                            m_item_id == (int)PremiumShop.INSTANT_USE_ITEM_ID.COIN_BAG_1 ||
                            m_item_id == (int)PremiumShop.INSTANT_USE_ITEM_ID.COIN_BAG_2 ||
                            m_item_id == (int)PremiumShop.INSTANT_USE_ITEM_ID.COIN_BAG_3 ||
                            m_item_id == (int)PremiumShop.INSTANT_USE_ITEM_ID.COIN_BAG_4 ||
                            m_item_id == (int)PremiumShop.INSTANT_USE_ITEM_ID.COIN_BAG_5 ||
                            m_item_id == (int)PremiumShop.INSTANT_USE_ITEM_ID.COIN_BAG_6 ||
                            m_item_id == (int)PremiumShop.INSTANT_USE_ITEM_ID.COIN_BAG_7 ||
                            m_item_id == (int)PremiumShop.INSTANT_USE_ITEM_ID.COIN_BAG_8 ||
                            m_item_id == (int)PremiumShop.INSTANT_USE_ITEM_ID.COIN_BAG_9 ||
                            m_item_id == (int)PremiumShop.INSTANT_USE_ITEM_ID.COIN_BAG_10 ||
                            m_item_id == (int)PremiumShop.INSTANT_USE_ITEM_ID.COIN_BAG_11 ||
                            m_item_id == (int)PremiumShop.INSTANT_USE_ITEM_ID.COIN_BAG_12 ||
                            m_item_id == (int)PremiumShop.INSTANT_USE_ITEM_ID.COIN_BAG_13 ||
                            m_item_id == (int)PremiumShop.INSTANT_USE_ITEM_ID.PLAT_BAG_1 ||
                            m_item_id == (int)PremiumShop.INSTANT_USE_ITEM_ID.PLAT_BAG_2 ||
                            m_item_id == (int)PremiumShop.INSTANT_USE_ITEM_ID.PLAT_BAG_3 ||
                            m_item_id == (int)PremiumShop.INSTANT_USE_ITEM_ID.PLAT_BAG_4 ||
                            m_item_id == (int)PremiumShop.INSTANT_USE_ITEM_ID.PLAT_BAG_5 ||
                            m_item_id == (int)PremiumShop.INSTANT_USE_ITEM_ID.PLAT_BAG_6 ||
                            m_item_id == (int)PremiumShop.INSTANT_USE_ITEM_ID.PLAT_BAG_7 ||
                            m_item_id == (int)PremiumShop.INSTANT_USE_ITEM_ID.CHARACTER_SLOT ||
                            m_item_id == (int)PERMENENT_BUFF_ID.SOLO_BANK_EXPANSION ||
                            m_item_id == (int)PERMENENT_BUFF_ID.AUCTION_HOUSE_SLOT_EXPANSION);

            m_reportTimeMale = query.GetFloat("report_back_time_male");
            m_reportTimeFemale = query.GetFloat("report_back_time_female");
            m_projectileSpeed = query.GetFloat("missile_speed");
            

            m_procSkillID = query.GetInt32("proc_skill_id");
            m_procSkillLevel = query.GetInt32("proc_skill_level");
            m_procSkillChance = query.GetFloat("proc_skill_chance");

            m_equipSkillID = query.GetInt32("equip_skill_id");
            m_equipSkillLevel = query.GetInt32("equip_skill_level");

            m_uniqueID = query.GetInt32("unique_id");

            m_bindOnEquip = query.GetBoolean("bind_on_equip");
            m_maxCharges = query.GetInt32("max_charges");
            m_destroyOnNoCharge = query.GetBoolean("destroy_on_no_charges");

            string statsString = query.GetString("stat_bonus");
            string abilityString = query.GetString("ability_bonus");
            string skillsString = query.GetString("skill_bonus");
            
            for (int i = 1; i < 4; i++)
            {
                string stringToSplit = "";
                CharacterModifiers.MODIFIER_TYPES currentType = (CharacterModifiers.MODIFIER_TYPES)i;
                switch (currentType)
                {
                    case CharacterModifiers.MODIFIER_TYPES.Stats:
                        stringToSplit=statsString;
                        break;
                    case CharacterModifiers.MODIFIER_TYPES.Skills:
                        stringToSplit=skillsString;
                        break;
                    case CharacterModifiers.MODIFIER_TYPES.Abilities:
                        stringToSplit=abilityString;
                        break;

                    default:
                        break;
                }
                string[] characterModSplit = stringToSplit.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                for (int j = 0; j < characterModSplit.Length; j++)
                {
                    string[] subsplit = characterModSplit[j].Split(new char[] { '^' }, StringSplitOptions.RemoveEmptyEntries);
                    int attribute = Int32.Parse(subsplit[0]);
                    int amount = Int32.Parse(subsplit[1]);

                    //add to the stats modifiers
                    CharacterModifiers newMod = new CharacterModifiers(currentType, attribute, amount);
                    m_modifiers.Add(newMod);
                }

            }

            
            
            //ReadLootSets(db);
        }

       

    /*    void ReadLootSets(Database db)
        {
            SqlQuery lootQuery = new SqlQuery(db, "select * from item_loot_sets where item_id=" + m_item_id);
            while (lootQuery.Read())
            {
                int lootSetID = lootQuery.GetInt32("loot_set_id");
                int lootSetDrops = lootQuery.GetInt32("num_drops");

                LootSet newLootSet = LootSetManager.getLootSet(lootSetID);
                if (newLootSet != null)
                {
                    LootSetHolder newSetHolder = new LootSetHolder(newLootSet, lootSetDrops);
                    m_lootSets.Add(newSetHolder);
                }
            }
            lootQuery.Close();
        }*/
     

        #endregion

        /// <summary>
        /// Returns true if the character is the correct class to use an item
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        internal bool CheckClassRestriction(Character character)
        {
            bool passesClassRestriction = false;

            if (m_classRestrictions.Count == 0)
            {
                passesClassRestriction = true;
                //return true;
            }

            for (int i = 0; i < m_classRestrictions.Count; i++)
            {
                if (m_classRestrictions[i] == character.m_class.m_classType)
                {
                    passesClassRestriction = true;
                    //return true;
                }
            }

            return passesClassRestriction;
        }
        internal bool CanBeEquippedInSlot(int slot)
        {
            bool canBeEquipped = false;
            //cannot be equipped
            if (slot < 0||m_slotNumber<0)
            {
                return false;
            }

            if (m_slotNumber == slot)
            {
                canBeEquipped = true;
            }
            //check rings
            if (m_slotNumber == (int)Inventory.EQUIP_SLOT.SLOT_RING_R1)
            {
                //rings can go on any ring slot
                if ((slot == (int)Inventory.EQUIP_SLOT.SLOT_RING_R2) || 
                    (slot == (int)Inventory.EQUIP_SLOT.SLOT_RING_L1)||
                    (slot == (int)Inventory.EQUIP_SLOT.SLOT_RING_L2))
                {
                    canBeEquipped = true;
                }
            }
            //check bracelet
            if (m_slotNumber == (int)Inventory.EQUIP_SLOT.SLOT_BANGLE_R)
            {
                //rings can go on any ring slot
                if (slot == (int)Inventory.EQUIP_SLOT.SLOT_BANGLE_L) 
                {
                    canBeEquipped = true;
                }
            }

            return canBeEquipped;

        }

        internal int GetMinLevel()
        {
            return m_RequirementTypes[(int)REQUIREMENT_TYPE.LEVEL_REQUIREMENT];
        }

        internal bool CheckStatRequirements(Character character)
        {
            bool passesStatCheck = true;
            //JT STATS CHANGES 12_2011
            CombatEntityStats characterStats = character.BaseStats;


			//if we're a fishing type (either a rod, bait, and even pets can be set as fishing rod subtype
	        if (m_subtype == ITEM_SUB_TYPE.FISHING_ROD || m_subtype == ITEM_SUB_TYPE.FISHING_ITEM)
	        {
		        //do we meet the fishing level requirements
		        if (m_RequirementTypes[(int) REQUIREMENT_TYPE.LEVEL_REQUIREMENT] > character.LevelFishing)
		        {
			        passesStatCheck = false;
					string locText = Localiser.GetString(textDB, character.m_player, (int)ItemTemplateTextDB.TextID.FISHING_LEVEL_REQUITED);
					locText = string.Format(locText, m_RequirementTypes[(int)REQUIREMENT_TYPE.LEVEL_REQUIREMENT]);
					Program.processor.sendSystemMessage(locText, character.m_player, false, SYSTEM_MESSAGE_TYPE.NONE);
				}
	        }
            else if (m_subtype == ITEM_SUB_TYPE.COOKING_ITEM)
            {
                //do we meet the fishing level requirements
                if (m_RequirementTypes[(int) REQUIREMENT_TYPE.LEVEL_REQUIREMENT] > character.LevelCooking)
                {
                    passesStatCheck = false;
                    Program.processor.sendSystemMessage(
                        "Cooking Level " + m_RequirementTypes[(int) REQUIREMENT_TYPE.LEVEL_REQUIREMENT] +
                        " required to equip this item.", character.m_player, false, SYSTEM_MESSAGE_TYPE.NONE);
                }
            }
            else //we're not a fishing type, so check requirments normally
            {
                if ((m_RequirementTypes[(int) REQUIREMENT_TYPE.STRENGTH_REQUIREMENT] > characterStats.Strength)
                    || (m_RequirementTypes[(int) REQUIREMENT_TYPE.DEXTERITY_REQUIREMENT] > characterStats.Dexterity)
                    || (m_RequirementTypes[(int) REQUIREMENT_TYPE.FOCUS_REQUIREMENT] > characterStats.Focus)
                    || (m_RequirementTypes[(int) REQUIREMENT_TYPE.VITALITY_REQUIREMENT] > characterStats.Vitality)
                    || (m_RequirementTypes[(int) REQUIREMENT_TYPE.LEVEL_REQUIREMENT] > character.Level)
                    ||
                    ((m_RequirementTypes[(int) REQUIREMENT_TYPE.GENDER_REQUIREMENT] != (int) character.m_gender) &&
                     (m_RequirementTypes[(int) REQUIREMENT_TYPE.GENDER_REQUIREMENT] != (int) GENDER.GENDER_BOTH))
                    )
                {
                    passesStatCheck = false;
					string locText = Localiser.GetString(textDB, character.m_player, (int)ItemTemplateTextDB.TextID.NOT_MEET_REQUIREMENT_EQUIP);
					Program.processor.sendSystemMessage(locText, character.m_player, false, SYSTEM_MESSAGE_TYPE.NONE);

                }
                
            }

            return passesStatCheck;
        }

        internal bool checkIfAllowed(Character character)
        {
            // in debug mode anyone can equipe anything
            if (Program.MainForm.DebugItems)
            {
                Program.Display("Debug ALLOW ITEM");
                return true;
            }

            bool passesStatCheck = CheckStatRequirements(character);
            if (passesStatCheck == false)
            {
                return false;
            }
            bool passesClassRestriction = CheckClassRestriction(character);//false;

            if (passesClassRestriction == false)
            {
				string locText = Localiser.GetString(textDB, character.m_player, (int)ItemTemplateTextDB.TextID.ITEM_FOR_ANOTHER_CLASS);
				Program.processor.sendSystemMessage(locText, character.m_player, false, SYSTEM_MESSAGE_TYPE.NONE);
				return false;
            }

            if ((m_slotNumber == (int)Inventory.EQUIP_SLOT.SLOT_OFFHAND)&&(m_subtype == ITEM_SUB_TYPE.ARROW))
            {
                Item weapon = character.m_inventory.GetEquipmentForSlot((int)Inventory.EQUIP_SLOT.SLOT_WEAPON);
                //must have a bow
                if (weapon == null)
                {
                    return false;
                }
                else if (weapon.m_template.m_subtype != ITEM_SUB_TYPE.BOW)
                {
                    return false;
                }
            }

            //can't equip saddle without a mount
            if ((int)Inventory.EQUIP_SLOT.SLOT_SADDLE == m_slotNumber && character.m_inventory.GetEquipmentForSlot((int)Inventory.EQUIP_SLOT.SLOT_MOUNT) == null)
            {
                return false;               
            }

            return passesClassRestriction;

        }
        public WEAPON_EQUIP_TYPE GetWeaponEquipmentType()
        {
            WEAPON_EQUIP_TYPE itemsEquipType = WEAPON_EQUIP_TYPE.NONE;

            //m_slotNumber
            if (m_slotNumber == (int)Inventory.EQUIP_SLOT.SLOT_WEAPON)
            {
                itemsEquipType = WEAPON_EQUIP_TYPE.ONE_HANDED;
            }

            switch (m_subtype)
            {
                case ITEM_SUB_TYPE.SWORD_TWO_HANDED:
                    itemsEquipType = WEAPON_EQUIP_TYPE.TWO_HANDED;
                    break;
                case ITEM_SUB_TYPE.AXE_TWO_HANDED:
                    itemsEquipType = WEAPON_EQUIP_TYPE.TWO_HANDED;
                    break;
                case ITEM_SUB_TYPE.BLUNT_TWO_HANDED:
                    itemsEquipType = WEAPON_EQUIP_TYPE.TWO_HANDED;
                    break;
                case ITEM_SUB_TYPE.BROOM:
                case ITEM_SUB_TYPE.SLEDGE:
                case ITEM_SUB_TYPE.STAFF:
                    itemsEquipType = WEAPON_EQUIP_TYPE.TWO_HANDED;
                    break;
                case ITEM_SUB_TYPE.BOW:
                    itemsEquipType = WEAPON_EQUIP_TYPE.BOW;
                    break;
            }

            return itemsEquipType;
        }
        internal bool HasBlockedSlot(Inventory.EQUIP_SLOT slot)
        {
            bool slotIsBlocked = false;

            for (int i = 0; i < m_blockedSlots.Count()&&slotIsBlocked==false; i++)
            {
                if (m_blockedSlots[i] == slot)
                {
                    slotIsBlocked = true;
                }
            }

            return slotIsBlocked;
        }

        internal float GetItemBonus(BONUS_TYPE bonusType)
        {
            int bonusInt = (int)bonusType;
            FloatForID currentVal = FloatForID.GetEntryForID(m_bonusTypes, bonusInt);
            if (currentVal != null)
            {
                return currentVal.m_amount;
            }
            return 0;
        }
    };

    static class ItemTemplateManager
    {
        internal static int INVALID_ITEM_TEMP_ID = 1;
       	static Dictionary<int, ItemTemplate> m_items = new Dictionary<int, ItemTemplate>();

        /// <summary>
        /// The time items are locked off on the client after use
        /// </summary>
        internal static float ITEM_RECHARGE_TIME = 10;
        /// <summary>
        /// How much leeway should there be on the item use item
        /// This is to prevent the 0 seconds warning when the client is slightly out of sync
        /// </summary>
        internal static float ITEM_LEEWAY_TIME = 1;

        static ItemTemplateManager()
        {

        }
        static public void FillTemplate(Database db)
        {

			int itemTextDBIndex = Localiser.GetTextDBIndex("item_templates");
			LocalisedTextDB textDB = Localiser.GetTextDB(itemTextDBIndex);

			SqlQuery query = new SqlQuery(db, "select * from item_templates order by item_id");
            while (query.Read())
            {
	            ItemTemplate it = new ItemTemplate(db, query, textDB);
                m_items[it.m_item_id] = it;	            
            }

            query.Close();

            ReadItemStatusEffects(db);
            ReadItemParams(db);
        }

        static private void ReadItemStatusEffects(Database db)
        {
            SqlQuery itemSEQuery = new SqlQuery(db, "select * from item_status_effects order by item_id,status_effect_id");
            while (itemSEQuery.Read())
            {
                int item_id = itemSEQuery.GetInt32("item_id");
                EFFECT_ID effect_id = (EFFECT_ID)itemSEQuery.GetInt32("status_effect_id");
                int level = itemSEQuery.GetInt32("effect_level");

				ItemTemplate item = GetItemForID(item_id);

				if (item != null)
                {
					item.m_statusEffects.Add(new ItemStatusEffect(effect_id, level));
                }
            }
            itemSEQuery.Close();
        }

        static private void ReadItemParams(Database db)
        {
            //the current number of possible parameters
            int maxItemParams = 2;
            SqlQuery paramsQuery = new SqlQuery(db, "select * from item_params order by item_id,mod_id");
            if (paramsQuery.HasRows)
            {
                while (paramsQuery.Read())
                {
                    CombatModifiers.Modifier_Type modType = (CombatModifiers.Modifier_Type)paramsQuery.GetInt32("mod_type");
                    List<float> paramList = new List<float>();
                    bool dataEnded = false;
                    for (int i = 0; i < maxItemParams && dataEnded == false; i++)
                    {
                        string fieldName = "param_" + i;

                        if (paramsQuery.isNull(fieldName) == false)
                        {
                            float paramVal = paramsQuery.GetFloat(fieldName);
                            paramList.Add(paramVal);
                        }
                        else
                        {
                            dataEnded = true;
                        }
                    }
                    int item_id = paramsQuery.GetInt32("item_id");

                    CombatModifiers newMod = new CombatModifiers(modType, paramList);
                    ItemTemplate item = GetItemForID(item_id);

                    if (item != null)
                    {
                        item.m_combatModifiers.Add(newMod);
                    }
                }
            }

            paramsQuery.Close();
        }


        static public ItemTemplate GetItemForID(int in_id)
        {
            if (m_items == null)
                return null;

            ItemTemplate value;

            if (m_items.TryGetValue(in_id, out value) == false)
                return null;

            return value;
        }
    }
}
