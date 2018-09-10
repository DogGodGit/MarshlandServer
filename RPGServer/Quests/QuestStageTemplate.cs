using System;
using System.Collections.Generic;
using XnaGeometry;

namespace MainServer
{
    public class QuestStageTemplate
    {
        public enum CompletionType
        {
            MobKill = 0,
            LootItem = 1,
            Position = 2,
            ConversationOption = 3,
            EndQuest = 4,
            CompletedQuest = 5,
            HandInItem = 6,
            HandInItemList=7,
            CastSkill=8,
            HandInGold = 9,
            CastSkillOn = 10,
            MobKillList = 11,
            MobSetKill = 12
        }
      
        public int m_stage_id;
        public int m_nextStage;
        public CompletionType m_completion_type;
        public int m_stage_open_sum;
        public List< CompletionDetails> m_completionDetails=new List<CompletionDetails>();
        public int m_completedQuest;
        public Vector3 m_position;
        public int m_zone_id;
        public float m_radius;

        public QuestStageTemplate(QuestTemplate questTemplate, int stage_id, CompletionType completion_type, string completion_details, int next_stage, int stage_open_sum)
        {
            m_stage_id = stage_id;
            m_nextStage = next_stage;
            m_stage_open_sum = stage_open_sum;
            m_completion_type = completion_type;
            switch (m_completion_type)
            {
                case CompletionType.MobKill:
                case CompletionType.LootItem:
                case CompletionType.HandInItem:
                case CompletionType.CastSkill:
                case CompletionType.MobSetKill:
                {
                    m_completionDetails.Add(new CompletionDetails(completion_details));
                    break;
                }
                case CompletionType.CastSkillOn:
                {
                    string[] completion_dets = completion_details.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    if (completion_dets.Length>1)
                    {
                        m_completionDetails.Add(new CompletionDetails(completion_dets[0]));
                        m_completionDetails.Add(new CompletionDetails(Int32.Parse(completion_dets[1]),-1));
                    }

                    break;
                }
                case CompletionType.HandInGold:
                {
                    m_completionDetails.Add(new CompletionDetails(-1,Int32.Parse(completion_details)));
                    break;
                }
                case CompletionType.HandInItemList:
                {
                    string[] completion_dets=completion_details.Split(new char[] {';'},StringSplitOptions.RemoveEmptyEntries);
                    for(int i=0;i<completion_dets.Length;i++)
                    {
                        m_completionDetails.Add(new CompletionDetails(completion_dets[i]));
                    }
                    break;
                }
                case CompletionType.Position:
                {
                    string[] completion_dets = completion_details.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    m_zone_id = Int32.Parse(completion_dets[0]);
                    m_position = new Vector3(float.Parse(completion_dets[1]), float.Parse(completion_dets[2]), float.Parse(completion_dets[3]));
                    m_radius = float.Parse(completion_dets[4]);
                    break;
                }
                case CompletionType.CompletedQuest:
                {
                    m_completedQuest = Int32.Parse(completion_details);
                    break;
                }
            }

        }
        public bool requiredMob(int mobTemplateID)
        {
            if (m_completionDetails.Count > 0)
            {
                switch (m_completion_type)
                {
                    case CompletionType.MobKill:
                    {
                        return (m_completionDetails[0].m_id == mobTemplateID) ? true : false;
                    }
                    case CompletionType.MobSetKill:
                    {
                        return MobSets.QueryMobSet(m_completionDetails[0].m_id, mobTemplateID);
                    }
                }
            }
            return false;
        }
        public bool requiredSkill(int skillID, int mobTemplateID)
        {

            if (m_completion_type == CompletionType.CastSkill && m_completionDetails[0].m_id == skillID)
            {
                return true;
            }
            if (m_completion_type == CompletionType.CastSkillOn && m_completionDetails[0].m_id == skillID&& m_completionDetails[1].m_id== mobTemplateID)
            {
                return true;
            }

            return false;
        }
        public bool requiredLootItem(int itemTemplateID)
        {
            if (m_completion_type != CompletionType.LootItem || m_completionDetails[0].m_id != itemTemplateID)
            {
                return false;
            }
            return true;
        }

    }
}