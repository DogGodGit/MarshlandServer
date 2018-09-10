using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MainServer.Factions;

using MainServer.Localise;

namespace MainServer
{

    internal class LootTableItem
    {
        public LootTableItem(int templateID, int quantity, int chance)
        {
            m_itemTemplate = ItemTemplateManager.GetItemForID(templateID);
            m_quantity = quantity;
            m_chance = chance;
        }
        public ItemTemplate m_itemTemplate = null;
        int m_quantity = 0;
        public int m_chance = 0;
        public bool checkChance(int chance)
        {
            if (chance < m_chance)
                return true;
            else
                return false;
        }
        public int getQuantity()
        {
            return Program.getRandomNumber(m_quantity) + 1;
        }


    }

    internal class LootTable
    {
        public int m_lootTableID;
        public string m_lootTableName;
        public int m_lootWeightSum = 0;
        public List<LootTableItem> m_lootTableItems = new List<LootTableItem>();
        public LootTable(Database db, SqlQuery query)
        {
            m_lootTableID = query.GetInt32("loot_table_id");
            m_lootTableName = query.GetString("loot_table_name");
            SqlQuery itemsQuery = new SqlQuery(db, "select * from loot_table_items where loot_table_id=" + m_lootTableID);
            while (itemsQuery.Read())
            {
                int itemTemplateid = itemsQuery.GetInt32("item_template_id");
                int amount = itemsQuery.GetInt32("amount");
                int weight = itemsQuery.GetInt32("weight");
                m_lootWeightSum += weight;
                LootTableItem item = new LootTableItem(itemTemplateid, amount, m_lootWeightSum);

                m_lootTableItems.Add(item);
            }
            itemsQuery.Close();

        }

        internal LootDetails getLootItem()
        {
            int randNum = Program.getRandomNumber(m_lootWeightSum);
            for (int i = 0; i < m_lootTableItems.Count; i++)
            {
                if (m_lootTableItems[i].checkChance(randNum))
                {
                    //nothing to loot
                    if (m_lootTableItems[i].m_itemTemplate == null)
                        return null;
                    else
                        return new LootDetails(m_lootTableItems[i].m_itemTemplate.m_item_id, m_lootTableItems[i].getQuantity());

                }
            }
            return null;
        }

        internal int getLootItemID()
        {
            int randNum = Program.getRandomNumber(m_lootWeightSum);
            for (int i = 0; i < m_lootTableItems.Count; i++)
            {
                if (m_lootTableItems[i].checkChance(randNum))
                {
                    //nothing to loot
                    if (m_lootTableItems[i].m_itemTemplate == null)
                        return -1;
                    else
                        return m_lootTableItems[i].m_itemTemplate.m_item_id;

                }
            }
            return -1;
        }
    }
    internal class LootTableWeight
    {
        public LootTable m_lootTable = null;
        public int m_chance;
        public LootTableWeight(int lootTableid, int chance)
        {
            m_lootTable = LootSetManager.getLootTable(lootTableid);
            m_chance = chance;
        }
        public bool checkChance(int chance)
        {
            if (chance < m_chance)
                return true;
            else
                return false;
        }
    }
    internal class MobPermStatusEffect
    {
        public MobPermStatusEffect(EFFECT_ID effectID, int level)
        {
            m_effectID = effectID;
            m_level = level;
        }
        public EFFECT_ID m_effectID;
        public int m_level;
    }

    internal class MobSkillTemplate
    {
        SkillTemplate m_theSkillTemplate = null;
        int m_skillLevel = 0;
        //    int m_probabilityFactor = 0;

        internal SkillTemplate TheTemplate
        {
            get { return m_theSkillTemplate; }
        }
        internal int SkillLevel
        {
            get { return m_skillLevel; }
        }

        internal MobSkillTemplate(SkillTemplate theSkillTemplate, int skillLevel)
        {
            m_theSkillTemplate = theSkillTemplate;
            m_skillLevel = skillLevel;
        }

    }

    public class MobTemplate
    {
        public enum MOB_RACE
        {
            NOT_DEFINED = 0,
            WOLF = 1,
            GOBLIN = 2,
            SKELETON = 3,
            PUPPY = 4
        };

        public int m_templateID = 0;
        public string m_name = "";
        public float m_aggroRange = 0;
        public float m_followRange = 0;
        public float m_maxAttackRange = 1;
        public int m_factionID = 0;
        public int m_opinionBase = 0;
        public int m_level = 0;
        public int m_maxHitpoints = 0;
        public int m_minCoins = 0;
        public int m_maxCoins = 0;
        public int m_xp = -1;
        public int m_conversation_id = 0;
        public int m_maxEnergy = 0;
        public int m_attack = 0;
        public int m_defence = 0;
        public int m_attack_speed = 0;
        public int m_armour_value = 0;
        public float m_radius = CombatEntity.HW_CHARACTER_DEFAULT_RADIUS;
        public float m_scale = 1.0f;
        public int m_totalLootWeights = 0;
        public int m_power_level = 0;
        internal List<LootSetHolder> m_lootSets = new List<LootSetHolder>();        
        internal List<FloatForID> m_damageTypes = new List<FloatForID>();        
        internal List<FloatForID> m_bonusTypes = new List<FloatForID>();        
        internal List<FloatForID> m_avoidanceTypes = new List<FloatForID>();
        internal List<FloatForID> m_immunityTypes = new List<FloatForID>();
        internal List<FloatForID> m_damageReductionTypes = new List<FloatForID>();
        public float m_reportTime = 0;
        public float m_projectileSpeed = 0;
        public int m_combatAIID = -1;
        public int m_num_drops;
        public bool m_blocksAttacks = false;
        public bool m_noAbilityTest = false;
        public float m_spot_hidden = 0;
        public MOB_RACE m_mobRace;
        public int m_mobType;
        internal CombatAITemplate m_combatAITemplate = null;
        internal List<MobSkillTemplate> m_availableSkills = new List<MobSkillTemplate>();
        internal List<CharacterAbility> m_abilities = new List<CharacterAbility>();
        internal List<MobPermStatusEffect> m_permStatusEffects = new List<MobPermStatusEffect>();
        internal List<CAI_Script> m_scripts = new List<CAI_Script>();
        internal List<MobSkillSetTemplate> m_skillSets = new List<MobSkillSetTemplate>();

        internal bool m_immobile = false;

        internal int XP
        {
            get
            {
                if (m_xp < 0)
                {
                    float multiplier = 1.0f;
                    switch (m_power_level)
                    {
                        case 0:
                            multiplier = 1.0f;
                            break;
                        case 1:
                            multiplier = 4.0f / 3.0f;
                            break;
                        case 2:
                            multiplier = 2.0f;
                            break;
                        case 3:
                            multiplier = 8.0f / 3.0f;
                            break;
                        case 4:
                            multiplier = 4.0f;
                            break;
                        case 5:
                            multiplier = 16.0f / 3.0f;
                            break;
                    }
                    return (int)Math.Ceiling((80.0f + 20.0f * m_level) * multiplier);
                }
                else
                    return m_xp;
            }
        }

        /// <summary>
        /// When killed, alter faction points based on these values
        /// </summary>
        internal List<Factions.Faction> FactionInfluences { get; set; }

        #region Constructors

        internal MobTemplate(Database db, SqlQuery query, List<int> in_immobileMobIDs)
        {
            m_templateID = query.GetInt32("mob_template_id");
            m_name = query.GetString("mob_name");
            m_aggroRange = query.GetFloat("aggro_range")*Program.m_aggroRangeMultiplier; //james
            m_followRange = query.GetFloat("follow_range");
            if (m_aggroRange > m_followRange)
            {
                m_followRange = m_aggroRange;
            }
            m_factionID = query.GetInt32("faction_id");
            m_opinionBase = query.GetInt32("opinion_base");
            m_level = query.GetInt32("level");
            m_maxHitpoints = query.GetInt32("hitpoints");
            m_minCoins = query.GetInt32("min_coins");
            m_maxCoins = query.GetInt32("max_coins");
            m_xp = query.GetInt32("xp");
            m_conversation_id = query.GetInt32("conversation_id");
            m_maxEnergy = query.GetInt32("energy");
            m_attack = query.GetInt32("attack");
            m_defence = query.GetInt32("defence");
            m_attack_speed = query.GetInt32("attack_speed");
            m_armour_value = query.GetInt32("armour_value");
            m_radius = query.GetFloat("radius");
            m_scale = query.GetFloat("model_scale");
            m_power_level = query.GetInt32("mob_power");
            m_reportTime = query.GetFloat("report_back_time");
            m_projectileSpeed = query.GetFloat("missile_speed");
            m_mobRace = (MOB_RACE)query.GetInt32("mob_race");
            m_mobType = query.GetInt32("mob_type");
            //
            m_blocksAttacks = query.GetBoolean("blocks_attacks");
            m_spot_hidden = query.GetFloat("spot_hidden");

            m_noAbilityTest = query.GetBoolean("no_ability_test");

            // check if they're immobile so we can disable chase mechanics
            for (int i = 0; i < in_immobileMobIDs.Count; ++i)
            {
                if (in_immobileMobIDs[i] == m_templateID)
                {
                    m_immobile = true;
                    break;
                }
            }
            
            // damage list
            string damagelist = query.GetString("damage_list");
            string[] damagelistsplit = damagelist.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < damagelistsplit.Length; i++)
            {
                string[] subsplit = damagelistsplit[i].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                int dt = Int32.Parse(subsplit[0]);
                int amount = Int32.Parse(subsplit[1]);
                m_damageTypes.Add(new FloatForID(dt, amount));                
            }

            // resistance list
            string resistancelist = query.GetString("resistance_list");
            string[] resistancelistsplit = resistancelist.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < resistancelistsplit.Length; i++)
            {
                string[] subsplit = resistancelistsplit[i].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                int rt = Int32.Parse(subsplit[0]);
                int amount = Int32.Parse(subsplit[1]);
                m_bonusTypes.Add(new FloatForID(rt, amount));                
            }

            // avoidance lists
            string avoidancelist = query.GetString("avoidance_ratings");
            string[] avoidancelistsplit = avoidancelist.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < avoidancelistsplit.Length; i++)
            {
                string[] subsplit = avoidancelistsplit[i].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                int it = Int32.Parse(subsplit[0]);
                int amount = Int32.Parse(subsplit[1]);
                m_avoidanceTypes.Add(new FloatForID(it, amount));                
            }

            // immunity list
            string immunityList = query.GetString("immunity_list");
            string[] immunityListsplit = immunityList.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < immunityListsplit.Length; i++)
            {
                string[] subsplit = immunityListsplit[i].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                int rt = Int32.Parse(subsplit[0]);
                float amount = float.Parse(subsplit[1]);
                m_immunityTypes.Add(new FloatForID(rt, amount));

            }

            // damage reduction list
            string damageReductionlist = query.GetString("damage_reductions_list");
            string[] damageReductionlistsplit = damageReductionlist.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < damageReductionlistsplit.Length; i++)
            {
                string[] subsplit = damageReductionlistsplit[i].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                int rt = Int32.Parse(subsplit[0]);
                float amount = float.Parse(subsplit[1]);
                m_damageReductionTypes.Add(new FloatForID(rt, amount));

            }
            m_maxAttackRange = query.GetFloat("max_attack_range");
            m_combatAIID = query.GetInt32("ai_template_id");

            // permanent status effects
            string permStatusEffects = query.GetString("perm_status_effects");
            string[] permStatusEffectsplit = permStatusEffects.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < permStatusEffectsplit.Length; i++)
            {
                string[] subsplit = permStatusEffectsplit[i].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                EFFECT_ID effectID = (EFFECT_ID)Int32.Parse(subsplit[0]);
                int level = Int32.Parse(subsplit[1]);
                MobPermStatusEffect effect = new MobPermStatusEffect(effectID, level);
                m_permStatusEffects.Add(effect);
            }
            

            m_combatAITemplate = CombatAITemplateManager.GetItemForID(m_combatAIID);
        }
        
        #endregion

        internal List<LootDetails> getLootDropped()
        {
            List<LootDetails> lootdetails = new List<LootDetails>();
            for (int i = 0; i < m_lootSets.Count; i++)
            {
                LootSetHolder currentSet = m_lootSets[i];
                currentSet.TheLootSet.getLootDropped(lootdetails, currentSet.NumDrops);
            }
            return lootdetails;
        }
        
        internal MobSkillTemplate GetSkillTemplateForID(SKILL_TYPE skillID)
        {
            for (int skillIndex = 0; skillIndex < m_availableSkills.Count; skillIndex++)
            {
                MobSkillTemplate currentTemplate = m_availableSkills[skillIndex];
                if (currentTemplate.TheTemplate != null && currentTemplate.TheTemplate.SkillID == skillID)
                {
                    return currentTemplate;
                }
            }

            return null;
        }
    }

    static class MobTemplateManager
    {
		// #localisation
		static int textDBIndex = 0;
    
        static List<MobTemplate> m_mobTemplates = new List<MobTemplate>();
        
        static MobTemplateManager()
        {

        }

        static public void FillTemplate(Database db, List<int> in_immovableMobIDs)
        {

            SqlQuery query = new SqlQuery(db, "select * from mob_templates order by mob_template_id");

            while (query.Read())
            {
                m_mobTemplates.Add(new MobTemplate(db, query, in_immovableMobIDs));
            }

            query.Close();
           
            LoadLootSets(db);
            LoadSkills(db);            
            LoadSkillSets(db);
            LoadMobAbilities(db);
            LoadScripts(db);
			LoadMobFactions(db);

			// Get textNameDB index.
			textDBIndex = Localiser.GetTextDBIndex("mob_templates");        
        }
        
        static private void LoadLootSets(Database db)
        {
            int currentMob = 0;
            SqlQuery lootQuery = new SqlQuery(db, "select * from mob_loot_sets order by mob_template_id,loot_set_id");
            while (lootQuery.Read())
            {
                int mob_template_id = lootQuery.GetInt32("mob_template_id");
                int lootSetID = lootQuery.GetInt32("loot_set_id");
                int lootSetDrops = lootQuery.GetInt32("num_drops");
                while (currentMob < m_mobTemplates.Count && m_mobTemplates[currentMob].m_templateID < mob_template_id)
                {
                    currentMob++;
                }
                if (currentMob < m_mobTemplates.Count && m_mobTemplates[currentMob].m_templateID == mob_template_id)
                {
                    LootSet newLootSet = LootSetManager.getLootSet(lootSetID);

                    if (newLootSet != null)
                    {
                        LootSetHolder newSetHolder = new LootSetHolder(newLootSet, lootSetDrops);
                        m_mobTemplates[currentMob].m_lootSets.Add(newSetHolder);
                    }
                    else
                    {
                        Program.Display("Mob Template " + mob_template_id + " failed to load loot set " + lootSetID);
                    }
                }
            }
            lootQuery.Close();
        }

        static private void LoadSkills(Database db)
        {
            int currentMob = 0;
            SqlQuery skillQuery = new SqlQuery(db, "select * from mob_skills order by mob_template_id,skill_id,skill_set_id");
            while (skillQuery.Read())
            {
                int mob_template_id = skillQuery.GetInt32("mob_template_id");
                int skillID = skillQuery.GetInt32("skill_id");
                int skillLevel = skillQuery.GetInt32("skill_level");

                int skillSetID = skillQuery.GetInt32("skill_set_id");
                while (currentMob < m_mobTemplates.Count && m_mobTemplates[currentMob].m_templateID < mob_template_id)
                {
                    currentMob++;
                }
                if (currentMob < m_mobTemplates.Count && m_mobTemplates[currentMob].m_templateID == mob_template_id)
                {
                    MobSkillTemplate mobSkillTemplate = m_mobTemplates[currentMob].GetSkillTemplateForID((SKILL_TYPE)skillID);
                    if (mobSkillTemplate == null)
                    {
                        SkillTemplate currentSkillTemplate = SkillTemplateManager.GetItemForID((SKILL_TYPE)skillID);
                        if (currentSkillTemplate != null)
                        {

                            m_mobTemplates[currentMob].m_availableSkills.Add(new MobSkillTemplate(currentSkillTemplate, skillLevel));

                        }
                    }
                }

            }
            skillQuery.Close();
        }
        
        static private void LoadSkillSets(Database db)
        {
            int currentMob = 0;
            SqlQuery skillSetQuery = new SqlQuery(db, "select * from mob_skill_sets order by mob_template_id,skill_set_id,ai_skill_set_id");
            while (skillSetQuery.Read())
            {
                int mob_template_id = skillSetQuery.GetInt32("mob_template_id");
                //what skill set
                int skillSetID = skillSetQuery.GetInt32("skill_set_id");
                //what will the ai skill set id be
                int aiSkillSet = skillSetQuery.GetInt32("ai_skill_set_id");
                //how likely is this set to be picked
                int weight = skillSetQuery.GetInt32("weight");
                while (currentMob < m_mobTemplates.Count && m_mobTemplates[currentMob].m_templateID < mob_template_id)
                {
                    currentMob++;
                }
                if (currentMob < m_mobTemplates.Count && m_mobTemplates[currentMob].m_templateID == mob_template_id)
                {
                    //Get the template Set
                    SkillSetTemplate skillSetTemplate = SkillSetTemplateManager.GetSkillSetForID(skillSetID);
                    if (skillSetTemplate != null)
                    {
                        //create a MobSkillSetTemplate for this set of weights
                        MobSkillSetTemplate newSkillSet = new MobSkillSetTemplate(skillSetTemplate, aiSkillSet, weight);
                        //add it to the list
                        m_mobTemplates[currentMob].m_skillSets.Add(newSkillSet);
                    }
                }
            }

            skillSetQuery.Close();
        }

        static private void LoadMobAbilities(Database db)
        {
            int currentMob = 0;
            SqlQuery abilityQuery = new SqlQuery(db, "select * from mob_abilities order by mob_template_id,ability_id");
            while (abilityQuery.Read())
            {
                int mob_template_id = abilityQuery.GetInt32("mob_template_id");
                int abilityID = abilityQuery.GetInt32("ability_id");
                int abilityLevel = abilityQuery.GetInt32("ability_level");
                while (currentMob < m_mobTemplates.Count && m_mobTemplates[currentMob].m_templateID < mob_template_id)
                {
                    currentMob++;
                }
                if (currentMob < m_mobTemplates.Count && m_mobTemplates[currentMob].m_templateID == mob_template_id)
                {
                    m_mobTemplates[currentMob].m_abilities.Add(new CharacterAbility((ABILITY_TYPE)abilityID, abilityLevel));
                }
            }
            abilityQuery.Close();
        }

        static private void LoadScripts(Database db)
        {
            int currentMob = 0;
            SqlQuery scriptListQuery = new SqlQuery(db, "select * from mob_scripts ms,ai_scripts sc where ms.ai_script_id=sc.ai_script_id order by mob_template_id,ai_template_id,ms.ai_script_id");
            while (scriptListQuery.Read())
            {
                int mob_template_id = scriptListQuery.GetInt32("mob_template_id");
                int scriptID = scriptListQuery.GetInt32("ai_script_id");
                int aiTemplateID = scriptListQuery.GetInt32("ai_template_id");
                string scriptString = scriptListQuery.GetString("ai_script_string");
                string activationString = scriptListQuery.GetString("activation_string");
                int priority = scriptListQuery.GetInt32("priority");
                while (currentMob < m_mobTemplates.Count && m_mobTemplates[currentMob].m_templateID < mob_template_id)
                {
                    currentMob++;
                }
                if (currentMob < m_mobTemplates.Count && m_mobTemplates[currentMob].m_templateID == mob_template_id)
                {
                    CAI_Script newScript = new CAI_Script(scriptID, aiTemplateID, scriptString, activationString, priority);
                    m_mobTemplates[currentMob].m_scripts.Add(newScript);
                }
            }
            scriptListQuery.Close();
        }

        /// <summary>
        /// Load all mob faction data.  Each mob MIGHT have one or more influences as defined by a faction id.
        /// </summary>
        /// <param name="db"></param>
        static private void LoadMobFactions(Database db)
        {
            
            SqlQuery factionsQuery = new SqlQuery(db, "select * from mob_factions_list order by mob_template_id,faction_id");
            while (factionsQuery.Read())
            {
                int mob_template_id = factionsQuery.GetInt32("mob_template_id");
                int faction_id = factionsQuery.GetInt32("faction_id");
                int factions_points = factionsQuery.GetInt32("faction_points");
                int faction_level = Faction.nullLevel;
                if(factionsQuery.isNull("faction_level") == false)
                    faction_level = factionsQuery.GetInt32("faction_level");

                //find matching template
                MobTemplate mobTemplate = m_mobTemplates.Find(x => x.m_templateID == mob_template_id);

                //print out an error
                if (mobTemplate == null)
                {
                    Program.Display("Error.LoadMobFactions.No matching mob_template_id." + mob_template_id);
                    continue;                    
                }

                //create list if null
                if(mobTemplate.FactionInfluences == null)
                    mobTemplate.FactionInfluences = new List<Faction>();

                //add this influence to the list
                mobTemplate.FactionInfluences.Add(new Faction(faction_id, factions_points, faction_level));
                
            }
            factionsQuery.Close();
        }

        static public MobTemplate GetItemForID(int ID)
        {
            if (m_mobTemplates == null)
            {
                return null;
            }
            for (int currentTemplate = 0; currentTemplate < m_mobTemplates.Count; currentTemplate++)
            {
                if (m_mobTemplates[currentTemplate].m_templateID == ID)
                {
                    return m_mobTemplates[currentTemplate];
                }
            }
            return null;
        }

		static internal string GetLocaliseMobName(Player player, int mob_template_id)
		{
			return Localiser.GetString(textDBIndex, player, mob_template_id);
		}

	}
}
