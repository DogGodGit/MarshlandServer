using System;

namespace MainServer
{
    public class CompletionDetails
    {
        public CompletionDetails(int id, int total)
        {
            m_id = id;
            m_total = total;
        }
        public CompletionDetails(string completionstring)
        {
            string[] completion_dets = completionstring.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            m_id=Int32.Parse(completion_dets[0]);
            m_total=Int32.Parse(completion_dets[1]);
        }
        public int m_id;
        public int m_total;
    }

    public class QuestReward
    {
        public QuestReward(CLASS_TYPE rewardClass, int itemTemplateID)
        {
            m_RewardClass = rewardClass;
            m_itemTemplateID = itemTemplateID;
        }
        public CLASS_TYPE m_RewardClass;
        public int m_itemTemplateID;
    }

    public class NewQuestReward
    {
        internal enum Reward_Type
        {
            LootTable=1,
            Item=2,
            Skill = 3,
        };
        internal NewQuestReward(CLASS_TYPE rewardClass,Reward_Type rewardType, int rewardID, string paramVal)
        {
            m_rewardClass = rewardClass;
            m_rewardID = rewardID;
            m_paramVal = paramVal;
            m_rewardType = rewardType;
        }
        internal Reward_Type m_rewardType;
        internal CLASS_TYPE m_rewardClass;
        internal int m_rewardID;
        internal string m_paramVal;
    }

    public class QuestStub
    {
        int m_questID = -1;
        int m_characterID = -1;
        internal int QuestID
        {
            get { return m_questID; }
        }
        internal QuestStub(int quest_id,int characterID)
        {
            m_questID = quest_id;
            m_characterID = characterID;
        }
        public override string ToString()
        {
            return "(quest_id =" + m_questID + " and character_id = " + m_characterID + ")";
        } 
        
    }
}