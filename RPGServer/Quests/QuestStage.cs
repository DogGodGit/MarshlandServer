using Lidgren.Network;

namespace MainServer
{
    public class QuestStage
    {

        int m_stage_id;
        public QuestStageTemplate m_QuestStageTemplate;
        internal int m_stage_open_sum;
        internal int m_completion_sum;
        internal bool m_completed;
        internal bool m_locked;
        public QuestStage(int stage_id, QuestStageTemplate questStageTemplate, int stage_open_sum, int completion_sum, bool completed)
        {
            m_stage_id = stage_id;
            m_QuestStageTemplate = questStageTemplate;
            m_stage_open_sum = stage_open_sum;
            m_completion_sum = completion_sum;
            m_completed = completed;
            m_locked = false;
        }
        public bool IsAvailable()
        {
            if (m_stage_open_sum < m_QuestStageTemplate.m_stage_open_sum)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        internal void WriteQuestStageToMsg(NetOutgoingMessage outmsg)
        {
            if (m_completed)
                outmsg.Write((byte)1);
            else
                outmsg.Write((byte)0);
            if (IsAvailable())
                outmsg.Write((byte)1);
            else
                outmsg.Write((byte)0);


        }
        internal int requiredMob(int mobTemplateID)
        {
            if (m_completed)
                return -1;
            if (m_stage_open_sum < m_QuestStageTemplate.m_stage_open_sum)
                return -1;
            if (m_QuestStageTemplate.requiredMob(mobTemplateID))
            {
                m_completion_sum++;

                return m_QuestStageTemplate.m_completionDetails[0].m_total - m_completion_sum;
            }
            else
                return -1;

        }

        internal int requiredQuestComplete(int completedQuestID)
        {
            if (m_completed)
                return -1;
            if (m_stage_open_sum < m_QuestStageTemplate.m_stage_open_sum)
                return -1;
            if (m_QuestStageTemplate.m_completion_type==QuestStageTemplate.CompletionType.CompletedQuest &&  m_QuestStageTemplate.m_completedQuest==completedQuestID)
            {
                m_completion_sum++;

                return 0;
            }
            else
                return -1;

        }

        internal int requiredSkill(int skillID,int mobTemplateID)
        {
            if (m_completed)
                return -1;
            if (m_stage_open_sum < m_QuestStageTemplate.m_stage_open_sum)
                return -1;
            if (m_QuestStageTemplate.requiredSkill(skillID, mobTemplateID))
            {
                m_completion_sum++;

                int result =  m_QuestStageTemplate.m_completionDetails[0].m_total - m_completion_sum;
                if(result<0)
                {
                    result=0;
                }
                return result;
            }
            else
                return -1;

        }

        internal int requiredLootItem(int itemTemplateID,int itemCount)
        {
            if (m_locked)
                return -1;
            if (m_stage_open_sum < m_QuestStageTemplate.m_stage_open_sum)
                return -1;

            if (m_QuestStageTemplate.requiredLootItem(itemTemplateID))
            {
                if (m_completed && itemCount >= m_completion_sum)
                {
                    return -1;
                }
                m_completion_sum=itemCount;
                if (m_completion_sum > m_QuestStageTemplate.m_completionDetails[0].m_total)
                {
                    m_completion_sum = m_QuestStageTemplate.m_completionDetails[0].m_total;
                }

                return m_QuestStageTemplate.m_completionDetails[0].m_total - m_completion_sum;
            }
            else
                return -1;

        }

        // IncreaseAvailabilityCounter
        // Possible recursive functionality if quest stages that follow share the same 'next stage'
        // Where the next stage is actually a group of stages to be activated together
        internal void IncreaseAvailabilityCounter(Quest quest)
        {
            // Increase this stages open sum
            m_stage_open_sum++;

            // Get the next stage index of this stages next quest
            int next_stage = m_QuestStageTemplate.m_nextStage;

            // Get this stages index
            int this_stages_index = m_QuestStageTemplate.m_stage_id;

            // If this quest has now become available - increment the quests number of stages unlocked
            // Recursive calls which result in unlocking further stages also add to this
            // Is passed to client to unlock the correct number of stages
            if (IsAvailable())
            {
                quest.m_num_unlocked_stages++;
            }

            // If this stages next stage is the last (= -1) OR the last stage (we need to compare against the next stage)
            // Avoid recursive calls where this quest is the last or avoid going out of bounds in the stage list within quest
            if (next_stage < 0 || this_stages_index >= (quest.m_QuestStages.Count - 2))
            {
                return;
            }

            // Get the next QuestStage in the quest stages list
            QuestStage next_quest_stage = quest.m_QuestStages[m_stage_id + 1];

            // If this and the next stage share the same 'next stage' they are part of a group of quests
            // Call recursively on the next quest stage
            if (next_stage == next_quest_stage.m_QuestStageTemplate.m_nextStage)
            {
                quest.m_QuestStages[this_stages_index + 1].IncreaseAvailabilityCounter(quest);
            }
        }
    }
}