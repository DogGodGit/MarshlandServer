using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MainServer
{
    class EquipmentSetContainer
    {
        EquipmentSet m_equipmentSet = null;
        int m_count = 0;

        internal EquipmentSet EquipmentSet
        {
            get { return m_equipmentSet; }
        }
        internal int Count
        {
            get { return m_count; }
            set { m_count = value; }
        }

        internal EquipmentSetContainer(EquipmentSet currentSet)
        {
            m_equipmentSet = currentSet;
        }
        internal void AddQualifyingRewards(List<EquipmentSetRewards> qualifiedRewards){

            for (int i = 0; i < m_equipmentSet.Rewards.Count; i++)
            {
                EquipmentSetRewards currentReward = m_equipmentSet.Rewards[i];

                if (m_count >= currentReward.Amount)
                {
                    qualifiedRewards.Add(currentReward);
                }
            }
        }
        internal static EquipmentSetContainer GetEquipmentSetHolder(List<EquipmentSetContainer> currentList,EquipmentSet setToFind)
        {
            EquipmentSetContainer theSetHolder = null;

            for (int i = 0; i < currentList.Count && theSetHolder == null; i++)
            {
                EquipmentSetContainer currentHolder = currentList[i];
                if (currentHolder.EquipmentSet == setToFind)
                {
                    theSetHolder = currentHolder;
                }
            }

            return theSetHolder;
        }

        internal static List<EquipmentSetRewards> GetRewardsForEquipmentSetHolder(List<EquipmentSetContainer> currentList)
        {
            List<EquipmentSetRewards> qualifiedRewards = new List<EquipmentSetRewards>();

            for (int i = 0; i < currentList.Count ; i++)
            {
                EquipmentSetContainer currentHolder = currentList[i];
                currentHolder.AddQualifyingRewards(qualifiedRewards);
            }

            return qualifiedRewards;
        }
    }

    public class EquipmentSet
    {
        int m_equipmentSetID=-1;
        string m_equipmentSetName = "";

        List<ItemTemplate> m_contentsList = new List<ItemTemplate>();
        List<EquipmentSetRewards> m_rewards = new List<EquipmentSetRewards>();

        internal List<EquipmentSetRewards> Rewards
        {
            get { return m_rewards; }
        }
        public EquipmentSet(Database db, int equipmentSetID, string equipmentSetName)
        {
            m_equipmentSetID = equipmentSetID;
            m_equipmentSetName = equipmentSetName;

            SqlQuery contentsQuery = new SqlQuery(db, "select * from equipment_set_items where equipment_set_id = "+m_equipmentSetID);
            if(contentsQuery.HasRows==true){
                while(contentsQuery.Read()){
                    int templateID = contentsQuery.GetInt32("item_id");
                    ItemTemplate currentTemplate = ItemTemplateManager.GetItemForID(templateID);
                    if (currentTemplate != null)
                    {
                        m_contentsList.Add(currentTemplate);
                        if (currentTemplate.m_equipmentSets.Contains(this) == false)
                        {
                            currentTemplate.m_equipmentSets.Add(this);
                        }
                        else
                        {
                            Program.Display("EquipmentSet item (" + currentTemplate.m_item_id + ") " + currentTemplate.m_item_name + "duplicate entry for equipment Set " + equipmentSetID);
                        }
                    }
                    else
                    {
                        Program.Display("EquipmentSet item(" + templateID + ") missing template for equipment Set " + equipmentSetID);
                     
                    }

                }
            }

            contentsQuery.Close();
             SqlQuery rewardsQuery = new SqlQuery(db, "select * from equipment_set_rewards where equipment_set_id = "+m_equipmentSetID);
             if (rewardsQuery.HasRows == true)
             {
                 while (rewardsQuery.Read())
                 {
                     int rewardID = rewardsQuery.GetInt32("equipment_set_reward_id");
                     int amount = rewardsQuery.GetInt32("amount");
                     int templateID = rewardsQuery.GetInt32("item_id");
                     ItemTemplate currentTemplate = ItemTemplateManager.GetItemForID(templateID);
                     if (currentTemplate != null)
                     {
                         EquipmentSetRewards newReward = new EquipmentSetRewards(m_equipmentSetID,rewardID, currentTemplate, amount);
                         m_rewards.Add(newReward);
                     }
                     else
                     {
                         Program.Display("EquipmentSet reward(" + templateID + ") missing template for equipment Set " + equipmentSetID);

                     }
                 }
             }
             rewardsQuery.Close();
        }
        internal EquipmentSetRewards GetRewardForID(int rewardID)
        {
            EquipmentSetRewards setReward = null;

            for (int i = 0; i < m_rewards.Count && setReward == null; i++)
            {
                EquipmentSetRewards currentReward = m_rewards[i];
                if (currentReward.RewardID == rewardID)
                {
                    setReward = currentReward;
                }
            }
                return setReward;
        }
        static internal EquipmentSet GetEquipmentSetForID(int setID,List<EquipmentSet> setsList)
        {
            EquipmentSet equipmentSet = null;
            for (int i = 0; i < setsList.Count && equipmentSet==null; i++)
            {
                EquipmentSet currentSet = setsList[i];
                if (currentSet.m_equipmentSetID == setID)
                {
                    equipmentSet = currentSet;
                }
            }
            return equipmentSet;
        }
    }
    class EquipmentSetRewards
    {

        int m_amount = 0;
        int m_setID = -1;
        int m_rewardID = -1;
        ItemTemplate m_itemReward = null;

        internal int Amount
        {
            get { return m_amount; }
        }
        internal ItemTemplate ItemReward
        {
            get { return m_itemReward; }
        }
        internal int SetID
        {
            get { return m_setID; }
        }
        internal int RewardID
        {
            get { return m_rewardID; }
        }

        public EquipmentSetRewards(int setID, int rewardID,ItemTemplate itemReward, int amount)
        {
            m_amount = amount;
            m_itemReward = itemReward;
            m_setID = setID;
            m_rewardID = rewardID;
        }
    }
    class EquipmentSetRewardContainer
    {
        EquipmentSetRewards m_reward=null;
        double m_timeRecharged = 0;
        uint m_characterID = 0;

        internal double TimeRecharged
        {
            get { return m_timeRecharged; }
            set { m_timeRecharged = value; }
        }
        internal uint CharacterID
        {
            get { return m_characterID; }
        }
        internal EquipmentSetRewards Reward
        {
            get { return m_reward; }
        }

        internal EquipmentSetRewardContainer(EquipmentSetRewards reward, double timeRecharged,uint characterID)
        {
            m_reward = reward;
            m_timeRecharged = timeRecharged;
            m_characterID = characterID;

        }
        static internal void AddRewardsToList(List<EquipmentSetRewardContainer> listToAddTo, List<EquipmentSetRewards> newRewards, uint characterID,double currentTime)
        {
            for (int i = 0; i < newRewards.Count; i++)
            {
                EquipmentSetRewards currentReward = newRewards[i];
                EquipmentSetRewardContainer newContainer = new EquipmentSetRewardContainer(currentReward, currentTime, characterID);
                listToAddTo.Add(newContainer);
            }

        }
        static internal EquipmentSetRewardContainer GetRewardForSetAndRewardId(List<EquipmentSetRewardContainer> theList, int setID, int rewardID)
        {
            EquipmentSetRewardContainer theContainer = null;
            for (int i = 0; i < theList.Count && theContainer==null; i++)
            {
                EquipmentSetRewardContainer currentContainer = theList[i];
                if (currentContainer.m_reward.RewardID == rewardID && currentContainer.m_reward.SetID == setID)
                {
                    theContainer = currentContainer;
                }
            }

            return theContainer;
        }
    }
    
}
