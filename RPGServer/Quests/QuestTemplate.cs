using System;
using System.Collections.Generic;
using MainServer.Factions;

namespace MainServer
{
    public class QuestTemplate
    {
        public enum Repeatability
        {
            no_repeat = 0,
            instant_repeat = 1,
            day_repeat = 2,
            week_repeat = 3,
            bounty_board = 4
        }

        public int m_quest_id;
        int m_level_required;
        public int m_xp_reward;
        public int m_coins_reward;
        public List<QuestReward> m_item_reward_list = new List<QuestReward>();
        public List<NewQuestReward> m_quest_reward_list = new List<NewQuestReward>();
        public string m_questName = "";
        public int m_item_count;
        public int m_reward_type;
        List<int> m_preRequesits = new List<int>();
        public int m_zone;
        public int m_quest_level;
        public int m_quest_type;
        public Repeatability m_repeatable;
        public bool m_uses_loot_table;
        public CLASS_TYPE m_requires_class;
        public ABILITY_TYPE m_has_ability;
        public ABILITY_TYPE m_lacks_ability;
        public List<int> m_blocked_by = new List<int>();
        public List<int> m_blocked_by_if_current = new List<int>();
        public List<QuestStageTemplate> m_QuestStageTemplates = new List<QuestStageTemplate>();

        
        private int FactionId { get; set; }
        private int FactionLevel { get; set; }

        public Faction FactionPointReward { get; private set; }

        internal QuestTemplate(SqlQuery query)
        {
            //read in the basic information - id, level etc.
            m_quest_id=query.GetInt32("quest_id");
            m_level_required = query.GetInt32("level_required");
            m_xp_reward = query.GetInt32("xp_reward");
            m_coins_reward = query.GetInt32("coins_reward");
            string item_reward_list = query.GetString("item_reward");
            m_quest_type = query.GetInt32("quest_type");
           
            m_item_count = query.GetInt32("item_count");

            m_zone = query.GetInt32("zone");
            m_quest_level = query.GetInt32("quest_level");
            int repeats = query.GetInt32("repeatable");
            m_repeatable = (Repeatability)repeats;
            m_questName = query.GetString("quest_name");
            
            m_requires_class = (CLASS_TYPE)query.GetInt32("requires_class");
            m_has_ability = (ABILITY_TYPE)query.GetInt32("has_ability");
            m_lacks_ability = (ABILITY_TYPE)query.GetInt32("lacks_ability");
            m_uses_loot_table = query.GetBoolean("uses_loot_table");

            //parse out the item rewards
            string[] itemrewardlistsplit = item_reward_list.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i=0;i<itemrewardlistsplit.Length;i++)
            {
                string[] itemsplit=itemrewardlistsplit[i].Split(new char[] {','},StringSplitOptions.RemoveEmptyEntries);
                int templateid=Int32.Parse(itemsplit[0]);
                CLASS_TYPE classid=(CLASS_TYPE)Int32.Parse(itemsplit[1]);
                QuestReward reward=new QuestReward(classid,templateid);
                m_item_reward_list.Add(reward);
            }
            
            //is quest blocked by others
            string[] blocked_by_split = query.GetString("blocked_by").Split(new char[] {';'},StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < blocked_by_split.Length; i++)
            {
                string[] subsplit = blocked_by_split[i].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (subsplit[1] == "0")
                {
                    m_blocked_by.Add(Int32.Parse(subsplit[0]));
                }
                else
                {
                    m_blocked_by_if_current.Add(Int32.Parse(subsplit[0]));
                }
            }

            //other quest rewards
            SqlQuery rewardQuery = new SqlQuery(Program.processor.m_dataDB, "select * from quest_rewards where quest_id=" + m_quest_id);
            while (rewardQuery.Read())
            {
                NewQuestReward.Reward_Type rewardType = (NewQuestReward.Reward_Type)rewardQuery.GetInt32("reward_type");
                m_reward_type = (int)rewardType;
                CLASS_TYPE class_id = (CLASS_TYPE)rewardQuery.GetInt32("class_id");
                int rewardID = rewardQuery.GetInt32("reward_id");
                string rewardParam = rewardQuery.GetString("reward_param");
                NewQuestReward newReward = new NewQuestReward(class_id, rewardType, rewardID, rewardParam);
                m_quest_reward_list.Add(newReward);

            }
            rewardQuery.Close();

            //read in quest prerequisites
            SqlQuery prereqQuery = new SqlQuery(Program.processor.m_dataDB, "select prerequesit_quest_id from quest_prerequesits where quest_id=" + m_quest_id);
            while (prereqQuery.Read())
            {
                m_preRequesits.Add(prereqQuery.GetInt32("prerequesit_quest_id"));
            }
            prereqQuery.Close();

            //read in faction requirements
            FactionId = query.GetInt32("faction_id");
            FactionLevel = query.GetInt32("faction_level");

            // do we have faction points to reward?
            int id_reward = query.GetInt32("faction_id_reward");
            int point_reward = query.GetInt32("faction_point_reward");
            if(id_reward != 0 && point_reward != 0)
                FactionPointReward = new Faction(id_reward,point_reward);
        }
        
        public QuestStageTemplate GetStageTemplate(int stage_id)
        {
            for (int i = 0; i < m_QuestStageTemplates.Count; i++)
            {
                if (stage_id == m_QuestStageTemplates[i].m_stage_id)
                {
                    return m_QuestStageTemplates[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Is the character eligible to take this quest.
        /// </summary>
        /// <param name="character">character to check against</param>
        /// <param name="levelCheck">if true perform a level check</param>
        /// <returns></returns>
        internal bool IsQuestAllowed(Character character,bool levelCheck)
        {
            if (m_level_required > character.GetRelevantLevel(this)  && levelCheck)
                return false;
            if (m_zone != character.m_zone.m_zone_id)
                return false;
            if (m_requires_class != CLASS_TYPE.UNDEFINED && m_requires_class != character.m_class.m_classType)
                return false;
            if (m_has_ability != ABILITY_TYPE.NA && character.getAbilityById(m_has_ability) == null)
            {
                return false;
            }
            if (m_lacks_ability != ABILITY_TYPE.NA && character.getAbilityById(m_lacks_ability) != null)
            {
                return false;
            }
            for (int i = 0; i < m_blocked_by.Count; i++)
            {
                if (character.m_QuestManager.IsQuestStarted(m_blocked_by[i]) || character.m_QuestManager.IsQuestComplete(m_blocked_by[i]))
                {
                    return false;
                }
            }
            for (int i = 0; i < m_blocked_by_if_current.Count; i++)
            {
                if (character.m_QuestManager.IsQuestStarted(m_blocked_by_if_current[i]))
                {
                    return false;
                }
            }

            // clifford - hack for croms release quest handling, to be replaced with something more flexible
            if (m_quest_id == 3039)
            {
                if (character.m_QuestManager.IsQuestComplete(3006) || character.m_QuestManager.IsQuestComplete(3011))
                    return true;
            }

            for (int i = 0; i < m_preRequesits.Count; i++)
            {
                if (!character.m_QuestManager.IsQuestComplete(m_preRequesits[i]))
                    return false;
            }
            
            // if we have a faction requirement check against it now
            if (this.HasFactionRequirment() && levelCheck)
            {
                //check the characters faction manager if we equal or are greater than the faction level requirement
                if (character.FactionManager.HasFactionLevel(FactionId, FactionLevel) == false)
                {
                    // do a further check...if the faction level required is not 0 (i.e. neutral)
                    // as we consider even factions we've not met who have a requirement of neutral to be passed                    
                    if (FactionLevel != 0)
                    {
                        return false;
                    }
                    if(FactionLevel == 0 && character.FactionManager.HasMetFaction(FactionId) == true)
                    {
                        return false;
                    }
                }
            }

            //we've passed all the above tests, so return true
            if (m_quest_id == 3039)            
                return false;


            return true;

        }


        /// <summary>
        /// Does this quest have a faction requirement before we can take it
        /// </summary>
        /// <returns>True if has a faction id and level attached. else False.</returns>
        internal bool HasFactionRequirment()
        {
            if (FactionId != 0)
                return true;

            return false;
        }

        /// <summary>
        /// Does this quest have a faction point reward
        /// </summary>
        /// <returns>True if we reward some faction points</returns>
        internal bool HasFactionReward()
        {
            if (FactionPointReward == null)
                return false;

            if (FactionPointReward.Id != 0)
                return true;

            return false;
        }
    }
}