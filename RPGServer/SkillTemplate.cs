using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MainServer.Localise;

namespace MainServer
{
    public enum AVOIDANCE_TYPE
    {
        NONE=-1,
        PHYSICAL=0,
        SPELL=1,
        MOVEMENT=2,
        WOUNDING=3,
        WEAKENING=4,
        MENTAL=5
    }

    public enum SKILL_TYPE
    {
        NONE=0,
        PUMMEL=1,
        PROTECTIVE_STANCE = 2,
        RECUPERATE = 3,
        BANDAGE_WOUNDS = 4,
        NATURES_TOUCH = 5,
        INSTANT_HEAL = 6,
        INSTANT_ENERGY = 7,
        STRANGLING_VINES = 8,
        GRASPING_ROOTS = 9,
        HOWLING_WIND = 10,
        FIRE_BOLT = 11,
        CLOAK_OF_FIRE = 12,
        SHARP_SHOT = 13,
        STEADY_AIM = 14,
        TANGLING_GRASS = 15,
        SNEAKY_ATTACK = 16,
        HIDE = 17,
        REND = 18,
        HEALTH_REGEN_ELIXIR =25,
        ENERGY_REGEN_ELIXIR = 26,
        INSTANT_HEAL_500=27,
        INSTANT_ENERGY_500 = 28,
        

        CALM =32,
        TAUNT =33,
        DISTRACT = 34,
        SHIELD_OF_BARK = 36,
        NATURES_EMBRACE = 37,
        LIGHTNING_STRIKE = 38,
        ABUNDANCE = 39,
        LURE_OF_FIRE = 40,
        LURE_OF_ICE = 41,
        ICE_SHARDS = 42,
        ENERGY_SHIELD = 43,
        ENERGY_WELL = 44,
        FRENZY = 45,
        SHIELD_BASH = 46,
        GIANT_SWING = 47,
        FAST_REFLEXES = 48,
        POISON_WEAPON = 49,
        CAMOUFLAGE = 50,
        RAPID_SHOT = 51,
        BARBED_SHOT = 52,
        QUICK_STRIKE = 53,
        LIGHT_HEAL = 54,
        MEDITATE = 55,
        ENERGY_BOOST = 56,

        INVIS_POT = 58,
        SHRINK_POTION = 60,
        SHRINK_ELIXIR = 61,
        GROWTH_POTION = 62,
        GROWTH_ELIXIR = 63,
        DEF_BOOST_POT = 64,
        DEF_BOOST_ELIXIR = 65,
        ARM__BOOST_POT = 66,
        ARM_BOOST_ELIX = 67,
        MAX_HEALTH_BOOST_POT = 68,
        MAX_HEALTH_BOOST_ELIX = 69,
        MAX_ENERGY_BOOST_POT = 70,
        MAX_ENERGY_BOOST_ELIX = 71,
        ATT_BOOST_POT = 72,
        ATT_BOOST_ELIXIR = 73,
        ATT_SPD_BOOST_POT = 74,
        ATT_SPD_BOOST_ELIX = 75,
        RUN_SPD_BOOST_POT = 76,
        RUN_SPD_BOOST_ELIXIR = 77,
        EXP_BOOST_POT = 78,
        EXP_BOOST_ELIXIR = 79,
        HEALTH_REGEN_POT = 80,
        HEALTH_REGEN_ELIX = 81,
        ENERGY_REGEN_POT = 82,
        ENERGY_REGEN_ELIX = 83,
        COMBI_POT_1 = 84,
        COMBI_ELIX_1 = 85,
        ROOT_SCROLL=86,
        FIREBALL_SCROLL=87,
        RESTORATION_POTION=88,
        FETTLECAP_MUSHROOM=89,
        MILKSTALK_MUSHROOM=90,
        YELLOW_GIANT_MUSHROOM=91,
        BLACK_GILL_MUSHROOM=92,
        FOOLSTALK_MUSHROOM=93,
        DEATHCAP_MUSHROOM=94,
        GORE=95,
        HOG_MAUL=96,
        FIRESTRIKE=97,
        IMPALE=98,
        DEATH_CHARGE=99,
        FINISHING_BLOW=100,
        FROST_LANCE=101,
        STORM_OF_WRATH=102,
        BOON_OF_CROM=103,
        ABILITY_BOOST_ELIXIR=104,
        ABILITY_BOOST_POT=7927,
        FIRE_STORM = 126,
        WARCRY = 128,
        FREEZE = 135,
        ASSASSINATE = 143,
        REVIVE = 144,
        SACRIFICE = 145,
        SHIELD_WALL = 147,
        
        LIFE_DRAIN = 155,
        ENERGY_DRAIN = 156,
        TELEPORT_TO_START = 192,
        TELEPORT_TO_END = 196,
        RESCUE = 197,
        TELEPORT_OTHER_END = 201,
        MAGIC_BOX = 208,
        CRITICAL_STRIKE =212,
        LONG_SHOT = 211,
        ICE_BLAST = 221,
        DOUBLE_SHOT = 252,
        FISHING_EXP_BOOST_ELIXIR = 271,
        POTION_OF_CONCENTRATION = 342,
        LEARN_PUMMEL = 1001,
        LEARN_PROTECTIVE_STANCE = 1002,
        LEARN_RECUPERATE = 1003,
        LEARN_BANDAGE_WOUNDS = 1004,
        LEARN_NATURES_TOUCH = 1005,
        LEARN_STRANGLING_VINES = 1008,
        LEARN_GRASPING_ROOTS = 1009,
        LEARN_HOWLING_WIND = 1010,
        LEARN_FIRE_BOLT = 1011,
        LEARN_CLOAK_OF_FIRE = 1012,
        LEARN_SHARP_SHOT = 1013,
        LEARN_STEADY_AIM = 1014,
        LEARN_TANGLING_GRASS = 1015,
        LEARN_SNEAKY_ATTACK = 1016,
        LEARN_HIDE = 1017,
        LEARN_REND = 1018,
        LEARN_CALM = 1032,
        LEARN_TAUNT = 1033,
        LEARN_DISTRACT = 1034,
        LEARN_SHIELD_OF_BARK= 1036,
        LEARN_NATURES_EMBRACE = 1037,
        LEARN_LIGHTNING_STRIKE= 1038,
        LEARN_ABUNDANCE = 1039,

        LEARN_LURE_OF_FIRE = 1040,
        LEARN_LURE_OF_ICE = 1041,
        LEARN_ICE_SHARDS = 1042,
        LEARN_ENERGY_SHIELD = 1043,
        LEARN_ENERGY_WELL = 1044,
        LEARN_FRENZY = 1045,
        LEARN_SHIELD_BASH = 1046,
        LEARN_GIANT_BASH = 1047,
        LEARN_FAST_REFLEXES = 1048,
        LEARN_POISON_WEAPON = 1049,
        LEARN_CAMOUFLAGE = 1050,
        LEARN_RAPID_SHOT = 1051,
        LEARN_BARBED_SHOT = 1052,
        LEARN_QUICK_STRIKE = 1053,
        LEARN_LIGHT_HEAL = 1054,
        LEARN_MEDITATE = 1055,
        LEARN_ENERGY_BOOST = 1056,

        MOB_LIFE_DRAIN = 6185,
        MOB_AREA_LIFE_DRAIN = 7006,

        RESET_SKILLS = 10000,
        RESET_STATS = 10001
        

    }
    public class SkillTemplateLevel
    {
		// #localisation
		public class SkillTemplateTextDB : TextEnumDB
		{
			public SkillTemplateTextDB() : base(nameof(SkillTemplateLevel), typeof(TextID)) { }

			public enum TextID
			{
				GAINED_ITEM,        // "Gained item: {lootList0}"
				NOT_GAINED_ITEM     // "{name0} gained item: None"
			}
		} 
		public static SkillTemplateTextDB textDB = new SkillTemplateTextDB();

		internal List<LootSetHolder> m_lootSets = new List<LootSetHolder>();

        internal SkillTemplateLevel(Database db,SqlQuery query)
        {
            int skillID = query.GetInt32("skill_template_id");
            m_skill_level = query.GetInt32("skill_level");
            bool isPVP = query.GetBoolean("pvp");
            m_initialDamage =  query.GetDouble("initial_damage");
            m_castingTime = query.GetDouble("casting_time");
            m_rechargeTime = query.GetDouble("recharge_time");
            m_energyCost = query.GetInt32("energy_cost");
            m_successChance = query.GetInt32("chance_of_success");
            m_chargingProtection = query.GetInt32("charging_protection");
            m_minLevel = query.GetInt32("min_level");
            m_baseDamage = query.GetInt32("base_amount");
            m_aggroMulti = query.GetDouble("aggro_multi");

        }
        internal double InitialDamage
        {
            get { return m_initialDamage; }
        }
        internal double CastingTime
        {
            get { return m_castingTime; }
        }
        internal int EnergyCost
        {
            get { return m_energyCost; }
        }
        internal int SuccessChance
        {
            get { return m_successChance; }
        }
        internal int ChargingProtection
        {
            get { return m_chargingProtection; }
        }
        internal double RechargeTime
        {
            get { return m_rechargeTime; }
        }
        internal int MinLevel
        {
            get { return m_minLevel; }
        }
        internal double baseDamage
        {
            get { return m_baseDamage; }
        }



        internal double getUnModifiedAmount(EntitySkill theSkill, bool inPVP)
        {
            double baseDamage = m_baseDamage;
            if (theSkill != null)
            {
                SkillAugment castingAugment = theSkill.GetAugmentForType(CombatModifiers.Modifier_Type.AddedSkillDamage);
                if (castingAugment != null)
                {
                    if (inPVP == true)
                    {
                        baseDamage = castingAugment.PVPModParam;
                    }
                    else
                    {
                        baseDamage = castingAugment.PVEModParam;
                    }
                }
            }
            return baseDamage + m_initialDamage;
        }
        internal int getModifiedAmout(float abilityLevel, float statModifier, EntitySkill theSkill, bool inPVP)
        {
            double baseDamage = m_baseDamage;
            if (theSkill != null)
            {
                SkillAugment castingAugment = theSkill.GetAugmentForType(CombatModifiers.Modifier_Type.AddedSkillDamage);
                if (castingAugment != null)
                {
                    if (inPVP == true)
                    {
                        baseDamage = castingAugment.PVPModParam;
                    }
                    else
                    {
                        baseDamage = castingAugment.PVEModParam;
                    }
                }
            }
            return (int)Math.Ceiling(baseDamage + m_initialDamage * (1 + Math.Sqrt(abilityLevel / 100.0f) + Math.Sqrt(statModifier)));
        }
        internal double AggroMultiplier
        {
            get { return m_aggroMulti; }
        }
        internal int m_skill_level;
        double m_initialDamage;
        double m_castingTime;
        double m_rechargeTime;
        int m_energyCost;
        int m_successChance;
        int m_chargingProtection;
        int m_minLevel;
        double m_baseDamage;
        double m_aggroMulti = 1;

        internal double GetRechargeTime(EntitySkill theSkill, bool inPVP)
        {
            /*get { return m_rechargeTime; 
            
            }*/
            double rechargeTime = m_rechargeTime;
            if (theSkill != null)
            {
                SkillAugment castingAugment = theSkill.GetAugmentForType(CombatModifiers.Modifier_Type.ChangesRecastTime);
                if (castingAugment != null)
                {
                    if (inPVP == true)
                    {
                        rechargeTime = castingAugment.PVPModParam;
                    }
                    else
                    {
                        rechargeTime = castingAugment.PVEModParam;
                    }
                }
            }

            return rechargeTime;
        }
        internal double GetCastingTime(EntitySkill theSkill,bool inPVP)
        {
            double castTime = m_castingTime;
            if (theSkill != null)
            {
                SkillAugment castingAugment = theSkill.GetAugmentForType(CombatModifiers.Modifier_Type.ChangesCastingTime);
                if (castingAugment != null)
                {
                    if (inPVP == true)
                    {
                        castTime = castingAugment.PVPModParam;
                    }
                    else
                    {
                        castTime = castingAugment.PVEModParam;
                    }
                }
            }

            return castTime;
        }
        internal List<LootDetails> GetLootDropped()
        {
            List<LootDetails> lootdetails = new List<LootDetails>();
            for (int i = 0; i < m_lootSets.Count; i++)
            {
                LootSetHolder currentSet = m_lootSets[i];
                currentSet.TheLootSet.getLootDropped(lootdetails, currentSet.NumDrops);
            }
            return lootdetails;
        }
        internal bool CanDropLoot()
        {
            if (m_lootSets.Count > 0)
            {
                return true;
            }
            return false;
        }
        internal bool GiveLootToPlayer(CombatEntity caster, EntitySkill theSkill)
        {

            bool success = false;
            
            var character = caster as Character;
            if (caster.Type != CombatEntity.EntityType.Player || character == null)
            {
                return success;
            }

            List<LootDetails> lootdetails = GetLootDropped();
            LootDetails.CompressAllStackable(lootdetails);
            character.m_inventory.addLoot(lootdetails, theSkill);

            if (lootdetails.Count > 0)
            {
                var lootList = new StringBuilder();
                StringBuilder fullLootList = null;// = new StringBuilder();
                bool isOverLimit = false;

                for (int lootIndex = 0; lootIndex < lootdetails.Count; lootIndex++)
                {
                    LootDetails currentLoot = lootdetails[lootIndex];
                    int currentID = currentLoot.m_templateID;
                    int currentQuantity = currentLoot.m_quantity;
                    ItemTemplate currentLootTemplate = ItemTemplateManager.GetItemForID(currentID);
                    if (currentLootTemplate != null)
                    {
                        string checkListIsNotOverCap = lootList + currentLootTemplate.m_loc_item_name[character.m_player.m_languageIndex];
                        if (checkListIsNotOverCap.Length < 75)
                        {
                            AddLoot(ref lootList, currentLootTemplate, currentQuantity,
                                lootIndex >= (lootdetails.Count - 1), character.m_player);
                        }
                        else
                        {
                            isOverLimit = true;
                            // if the loot list is going to be over the cap, create a new string in order to get the full loot list for displaying in messages (not popups)
                            if (fullLootList == null) fullLootList = new StringBuilder(lootList.ToString()); 
                            AddLoot(ref fullLootList, currentLootTemplate, currentQuantity, lootIndex >= (lootdetails.Count - 1), character.m_player);
                        }
                        
                    }
                }

                if(lootList.Length > 0)
                {
                    success = true;
                    if (!isOverLimit)
                    {
                        lootList = lootList.Replace("&nbsp;", ", ");
                    }
                    else
                    {
                        fullLootList = fullLootList.Replace("&nbsp;", ", ");
                    }
                    
                }

                if (theSkill.SkillID == SKILL_TYPE.MAGIC_BOX)
                {
                    character.SendMagicBoxPopup(lootdetails,
                        fullLootList == null ? lootList.ToString() : fullLootList.ToString()); // if the full loot list exists, use it
                }
                else
                {
                    if (fullLootList == null)
                    {
						string locText = Localiser.GetString(textDB, character.m_player, (int)SkillTemplateTextDB.TextID.GAINED_ITEM);
						locText = String.Format(locText, lootList);
						Program.processor.sendSystemMessage(locText, character.m_player, false, SYSTEM_MESSAGE_TYPE.ITEM_USE);

                        character.SendLuckyItemPopup("", locText);
                    }
                    else
                    {
						string locText = Localiser.GetString(textDB, character.m_player, (int)SkillTemplateTextDB.TextID.GAINED_ITEM);
						locText = String.Format(locText, fullLootList);
						Program.processor.sendSystemMessage(locText, character.m_player, false, SYSTEM_MESSAGE_TYPE.ITEM_USE);
                        
                        character.SendLuckyItemPopup("", locText);
                    }
				}

                
                character.m_inventory.SendInventoryUpdate();
            }
            else
            {
				string locText = Localiser.GetString(textDB, character.m_player, (int)SkillTemplateTextDB.TextID.NOT_GAINED_ITEM);
				locText = String.Format(locText, character.m_name);
				Program.processor.sendSystemMessage(locText, character.m_player, false, SYSTEM_MESSAGE_TYPE.ITEM_USE);
			}
            return success;
        }

        private void AddLoot(ref StringBuilder lootList, ItemTemplate item, int quantity, bool isLastInList, Player player)
        {
            lootList.Append(item.m_loc_item_name[player.m_languageIndex]);
            if (quantity > 1)
            {
                lootList.Append(" * ").Append(quantity);
            }

            if (!isLastInList)
            {
                lootList.Append("&nbsp;");
            }
        }
        
    };


    public class SkillTemplate
    {
        public enum CAST_TARGET {NONE=-1,ENEMY=0, SELF=1, FRIENDLY=2,GROUP = 3,FISH = 4};
        internal const int LEARN_SKILL_START_ID =1000;
        internal const int LEARN_SKILL_END_ID = 2000;
        internal static int LONG_SHOT_RANGE = 6;
        internal enum SkillTargetError
        {
            NoError = 0,
            InvalidTarget = 1,
            NotAlly = 2,
            NotDead = 3,
        }

        #region variables
        internal SKILL_TYPE m_skillID;
        string m_skillName;
        string m_targetCastingString = "";
        string m_localCastingString= "";
        string m_selfCastingString = "";
        /// <summary>
        /// the type of target the skill can be cast on
        /// </summary>
        CAST_TARGET m_castTargetGroup = CAST_TARGET.ENEMY;

        bool m_includes_weapon_attack=false;
        int m_learn_recipe_id;
        EFFECT_ID m_statusEffectID;
        float m_aoe;
        float m_range;
        DAMAGE_TYPE m_DamageType;
        ABILITY_TYPE m_abilityID;
        internal List<SkillTemplateLevel> m_templateLevels=new List<SkillTemplateLevel>();
        internal List<SkillTemplateLevel> m_PVPTemplateLevels = new List<SkillTemplateLevel>();
        List<ItemTemplate.ITEM_SUB_TYPE> m_skillWeaponRequirements = new List<ItemTemplate.ITEM_SUB_TYPE>();
        float m_reportTime=0;
        float m_blockingTime = 0;
        double m_projectileSpeed = 0;
        STAT_TYPE m_primary_stat_mod;
        float m_primary_stat_divisor;
        AVOIDANCE_TYPE m_avoidanceType;
        /// <summary>
        /// does the skill send local messages when a mob starts casting it
        /// </summary>
        bool m_reportProgress = true;
        #endregion// variables
        #region Properties
        /// <summary>
        /// the type of target the skill can be cast on
        /// </summary>
        internal CAST_TARGET CastTargetGroup
        {
            get { return m_castTargetGroup; }
        }
        internal string TargetCastingString
        {
            get { return m_targetCastingString; }
        }
        internal string LocalCastingString
        {
            get { return m_localCastingString; }
        }
        internal string SelfCastingString
        {
            get { return m_selfCastingString; }
        }
        internal bool ReportProgress
        {
            get { return m_reportProgress; }
        }

        internal SKILL_TYPE SkillID
        {
            get { return m_skillID; }
        }

        internal EFFECT_ID StatusEffectID
        {
            get { return m_statusEffectID; }
        }

        internal int LearnRecipeID
        {
            get { return m_learn_recipe_id; }
        }



        internal string SkillName
        {
            get { return m_skillName; }
        }


        internal bool IncludesWeaponAttack
        {
            get { return m_includes_weapon_attack; }
        }
        internal float AOE
        {
            get { return m_aoe; }
        }
        internal float Range
        {
            get { return m_range; }
        }
        internal DAMAGE_TYPE DamageType
        {
            get { return m_DamageType; }
        }
        internal ABILITY_TYPE AbilityID
        {
            get { return m_abilityID; }
        }
        internal float ReportTime
        {
            get { return m_reportTime; }
        }
        internal double ProjectileSpeed
        {
            get { return m_projectileSpeed; }
        }
        internal float BlockingTime
        {
            get { return m_blockingTime; }
        }
        internal STAT_TYPE PrimaryStatModifier
        {
            get { return m_primary_stat_mod; }
        }
        internal float PrimaryStatDivisor
        {
            get { return m_primary_stat_divisor; }
        }

        internal AVOIDANCE_TYPE AvoidanceType
        {
            get { return m_avoidanceType; }
        }
        #endregion
        internal SkillTemplate( Database db,SqlQuery query )
        {
            m_skillID = (SKILL_TYPE)query.GetInt32("skill_id");
            m_skillName = query.GetString("skill_name");
            m_castTargetGroup = (CAST_TARGET)query.GetInt32("cast_target");
            
            m_statusEffectID = (EFFECT_ID)query.GetInt32("status_effect");
            m_aoe = query.GetFloat("area_of_effect");
            m_range = query.GetFloat("casting_range");

            m_DamageType = (DAMAGE_TYPE)query.GetInt32("damage_type");
            m_abilityID = (ABILITY_TYPE)query.GetInt32("ability_id");
            m_reportTime = query.GetFloat("report_back_time");
            m_blockingTime = query.GetFloat("blocking_time");
            m_projectileSpeed =  query.GetFloat("missile_speed");
            m_primary_stat_mod = (STAT_TYPE)query.GetInt32("primary_stat_mod");
            m_primary_stat_divisor = query.GetFloat("primary_stat_divisor");
            m_includes_weapon_attack = query.GetBoolean("includes_weapon_attack");
            m_avoidanceType = (AVOIDANCE_TYPE) query.GetInt32("avoidance_type_id");
            m_reportProgress = query.GetBoolean("report_progress");
            m_targetCastingString = query.GetString("target_casting_string");
            m_localCastingString = query.GetString("local_casting_string");
            m_selfCastingString = query.GetString("self_casting_string");

            if (query.isNull("learn_recipe_id") == false)
            {
                m_learn_recipe_id = query.GetInt32("learn_recipe_id");
            }
            else
            {
                m_learn_recipe_id = -1;
            }


            string subtypeList = query.GetString("required_subtype");
            string[] splitList=subtypeList.Split(new char[]{','},StringSplitOptions.RemoveEmptyEntries);
            for(int i=0;i<splitList.Length;i++)
            {
                m_skillWeaponRequirements.Add((ItemTemplate.ITEM_SUB_TYPE)Int32.Parse(splitList[i]));
            }
            
        }


        
        internal static int CheckSkillForUseAgainst(CombatEntity target, CombatEntity caster, CAST_TARGET targetType)
        {
/*
            int targetOpinion = target.GetOpinionFor(caster);
            bool isInParty = target.WithinPartyWith(caster);
           
           switch (targetType)
            {
                case CAST_TARGET.ENEMY:
                    if (targetOpinion < CombatManager.HostileCutoffOpinion)
                    {
                        return true;
                    }
                    break;
                case CAST_TARGET.FRIENDLY:
                    if (targetOpinion >= CombatManager.HostileCutoffOpinion)
                    {
                        return true;
                    }
                    if (caster == target)
                    {
                        return true;
                    }
                    break;
                case CAST_TARGET.SELF:
                    if (caster == target){
                        return true;
                    }
                    break;
                default:

                    break;
            }*/

            
            bool isEnemy = caster.IsEnemyOf(target);
            bool isAllyOf = target.IsAllyOf(caster);
            switch (targetType)
            {
                case CAST_TARGET.ENEMY:
                    if (isEnemy==true)
                    {
                        return (int)SkillTargetError.NoError;
                    }
                    break;
                case CAST_TARGET.FRIENDLY:
                    if (isEnemy==false)
                    {
                        if (isAllyOf == true)
                        {
                            return (int)SkillTargetError.NoError;
                        }
                        else
                        {
                            return (int)SkillTargetError.NotAlly;
                        }
                    }
                    if (caster == target)
                    {
                        return (int)SkillTargetError.NoError;
                    }
                    break;
                case CAST_TARGET.GROUP:
                    if (caster.IsInPartyWith(target) == true)
                    {
                        if (isAllyOf == true)
                        {
                            return (int)SkillTargetError.NoError;
                        }
                        else
                        {
                            return (int)SkillTargetError.NotAlly;
                        }
                    }
                    
                    break;
                case CAST_TARGET.SELF:
                    if (caster == target)
                    {
                        return (int)SkillTargetError.NoError;
                    }
                    break;
                case CAST_TARGET.FISH:
                    return (int) SkillTargetError.NoError;
                default:

                    break;
            }
            return (int)SkillTargetError.InvalidTarget;
        }
        internal bool EquipmentPassesRequirement(Character theCaster)
        {
            if (theCaster == null)
            {
                return false;
            }
            if (m_skillWeaponRequirements.Count == 0)
            {
                return true;
            }

            //check the weapon
            Item weapon = theCaster.m_inventory.m_equipedItems[(int)Inventory.EQUIP_SLOT.SLOT_WEAPON];
            if (weapon != null)
            {
                bool weaponValid = EquipmentPassesRequirement(weapon.m_template.m_subtype);
                if (weaponValid == true)
                {
                    return true;
                }
            }

            //if that didn't pass check the offhand
            Item offhand = theCaster.m_inventory.m_equipedItems[(int)Inventory.EQUIP_SLOT.SLOT_OFFHAND];
            if (offhand != null)
            {
                bool offhandValid = EquipmentPassesRequirement(offhand.m_template.m_subtype);
                if (offhandValid == true)
                {
                    return true;
                }
            }

            return false;
        }
        internal bool EquipmentPassesRequirement(ItemTemplate.ITEM_SUB_TYPE equipmentSubtype)
        {
            if (m_skillWeaponRequirements.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < m_skillWeaponRequirements.Count; i++)
            {
                if (m_skillWeaponRequirements[i] == equipmentSubtype)
                {
                    return true;
                }
            }
            return false;
        }
        public SkillTemplateLevel getSkillTemplateLevel(int skillTemplateLevel,bool PVP)
        {
            if (PVP)
            {
                if (skillTemplateLevel < m_PVPTemplateLevels.Count)
                    return m_PVPTemplateLevels[skillTemplateLevel];
            }

             if (skillTemplateLevel < m_templateLevels.Count)
                  return m_templateLevels[skillTemplateLevel];
             else
                  return null;
            
        }
        /// <summary>
        /// returns the number of levels for this skill, 
        /// nothing should attempt to use a skill beyond this as it will not exist 
        /// </summary>
        /// <returns></returns>
        internal int GetMaxLevel()
        {
            return (m_templateLevels.Count-1);
        }
        internal int GetMaxSkillLevelForPlayerLevel(int playerLevel)
        {
            int skillLevel = 0;

            for (int i = 0; i < m_templateLevels.Count; i++)
            {
                SkillTemplateLevel levelTemplate = m_templateLevels[i];

                if (levelTemplate.MinLevel <= playerLevel)
                {
                    skillLevel = i;
                }
                else
                {
                    return skillLevel;
                }
            }

            return skillLevel;
        }
        internal static string GetCastString(SkillTemplate theTemplate, CombatEntity caster, CombatEntity target, string baseString)
        {
            string finalString = baseString;


            /*Tags
             * <SN> skill name
             * <CN> caster name
             * <TN> target name
             */

                        
            Character localisedCharacter = null;
            if (caster is Character)
                localisedCharacter = (Character)caster;
            else if (target is Character)
                localisedCharacter = (Character)target;

            // if still null, then it's two server controlled entities sending a message
            // in which case default to english for the time being
            string skillName = String.Empty;
            if (localisedCharacter != null)
                skillName = SkillTemplateManager.GetLocaliseSkillName(localisedCharacter.m_player, theTemplate.SkillID);
            else
                skillName = theTemplate.SkillName;

            //replace tags
			finalString = finalString.Replace("<SN>", skillName);
			finalString = finalString.Replace("<CN>", caster.Name);
            finalString = finalString.Replace("<TN>", target.Name);


            return finalString;
        }
    }
}
