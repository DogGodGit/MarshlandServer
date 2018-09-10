using System.Collections.Generic;
using Lidgren.Network;

namespace MainServer
{
    public class Quest
    {
        public int m_quest_id;
        public int m_num_unlocked_stages; // Open Sum Fix - number of stages to be unlocked upon a stages completion
        public bool m_tracked; // QuestTracker - tracking state bool
        public QuestTemplate m_QuestTemplate;
        public List<QuestStage> m_QuestStages = new List<QuestStage>();
        internal Character m_character;
        ~Quest()
        {
            m_character = null;
        }
        // QuestTracker - when a quest is created its tracking status can be set
        internal Quest(Character character,int quest_id, QuestTemplate questTemplate, bool tracked)
        {
            m_character = character;
            m_quest_id = quest_id;
            m_QuestTemplate = questTemplate;
            m_tracked = tracked;
            m_num_unlocked_stages = 0;
        }
        public void WriteQuestToMsg(NetOutgoingMessage outmsg)
        {
            outmsg.WriteVariableInt32(m_quest_id);
            if (m_QuestTemplate.m_QuestStageTemplates.Count != m_QuestStages.Count)
            {
                Program.DisplayDelayed("error :character " + m_character.m_name + "[" + m_character.m_character_id + "] has quest " + m_quest_id + " template stage count=" + m_QuestTemplate.m_QuestStageTemplates.Count + ",char stage count=" + m_QuestStages.Count);

            }
            outmsg.WriteVariableInt32(m_QuestStages.Count - 1);
            for (int i = 0; i < m_QuestStages.Count; i++)
            {
                if (m_QuestStages[i].m_QuestStageTemplate.m_completion_type != QuestStageTemplate.CompletionType.EndQuest)

                    m_QuestStages[i].WriteQuestStageToMsg(outmsg);
            }
        }
        internal void addNewStage(Database db, uint character_id, int quest_id, int stage_id)
        {
            QuestStageTemplate questStageTemplate = m_QuestTemplate.GetStageTemplate(stage_id);
            QuestStage newStage = new QuestStage(stage_id, questStageTemplate, 0, 0, false);
           
            m_QuestStages.Add(newStage);
        }

        public void addStage(int stage_id, int stage_open_sum, int completion_sum, bool completed)
        {
            QuestStageTemplate questStageTemplate = m_QuestTemplate.GetStageTemplate(stage_id);

            QuestStage newStage = new QuestStage(stage_id, questStageTemplate, stage_open_sum, completion_sum, completed);
            m_QuestStages.Add(newStage);

        }

        public int XP_Reward
        {
            get
            {
                return m_QuestTemplate.m_xp_reward;
            }
        }
        public int Coins_Reward
        {
            get
            {
                return m_QuestTemplate.m_coins_reward;
            }
        }

        public int getItemReward(CLASS_TYPE player_class)
        {
            for (int i = 0; i < m_QuestTemplate.m_item_reward_list.Count; i++)
            {
                if (m_QuestTemplate.m_item_reward_list[i].m_RewardClass == player_class || m_QuestTemplate.m_item_reward_list[i].m_RewardClass == CLASS_TYPE.UNDEFINED)
                {
                    if (m_QuestTemplate.m_uses_loot_table)
                    {
                        return LootSetManager.getLootTable(m_QuestTemplate.m_item_reward_list[i].m_itemTemplateID).getLootItemID();
                    }
                    else
                    {
                        return m_QuestTemplate.m_item_reward_list[i].m_itemTemplateID;
                    }
                }
            }
            return 0;
           
        }
        public List<NewQuestReward> getQuestReward(CLASS_TYPE player_class)
        {
            List<NewQuestReward> questRewards = new List<NewQuestReward>();
            for (int i = 0; i < m_QuestTemplate.m_quest_reward_list.Count; i++)
            {
                NewQuestReward currentReward = m_QuestTemplate.m_quest_reward_list[i];
                if (currentReward.m_rewardClass == player_class || currentReward.m_rewardClass == CLASS_TYPE.UNDEFINED)
                {
                    questRewards.Add(currentReward);
                }
            }
            return questRewards;

        }
        public int Item_Count
        {
            get
            {
                return m_QuestTemplate.m_item_count;
            }
        }
        public int Quest_Level
        {
            get
            {
                return m_QuestTemplate.m_quest_level;
            }
        }


    }
}