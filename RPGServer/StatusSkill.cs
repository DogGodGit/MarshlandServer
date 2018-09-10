using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MainServer
{
    /// <summary>
    /// A temporary skill added by items or status effects
    /// </summary>
    class StatusSkill:EntitySkill
    {
        Item m_owningItem=null;
        Inventory m_inventory = null;
        EquipmentSetRewardContainer m_rewardContainer = null;

        public StatusSkill(SkillTemplate template, Item owningItem, Inventory owningInventory, EquipmentSetRewardContainer rewardContainer)
            : base(template)
        {
            m_owningItem = owningItem;
            m_inventory = owningInventory;
            m_rewardContainer = rewardContainer;
        }

        public override double TimeLastCast
        {
            set
            {
                //let the base react normally
                base.TimeLastCast = value;
                //save out data to the item
                if (m_owningItem != null)
                {
                    m_owningItem.m_timeRecharged = value;
                    Program.processor.m_worldDB.runCommandSync("update inventory set time_skill_last_cast=" + m_owningItem.m_timeRecharged + " where inventory_id=" + m_owningItem.m_inventory_id);
                }
                if (m_rewardContainer != null)
                {
                    m_rewardContainer.TimeRecharged = value;
                    Program.processor.m_worldDB.runCommandSync("update character_equipment_set_rewards set time_skill_last_cast=" + m_rewardContainer.TimeRecharged
                        + " where character_id =" + m_rewardContainer.CharacterID + " and equipment_set_reward_id = " + m_rewardContainer.Reward.RewardID + " and equipment_set_id = "+m_rewardContainer.Reward.SetID );
                }
            }
        
        }
        internal override void SkillCast(double currentTime)
        {
            base.SkillCast(currentTime);

            if (m_owningItem != null)
            {
                if (m_owningItem.m_template.m_maxCharges > 0)
                {
                    if (m_inventory!=null)
                    {
                        Item oldItem = new Item(m_owningItem);
                        m_inventory.ConsumeCharge(m_owningItem);
                        if (m_owningItem.Destroyed == true)
                        {
                            m_inventory.SendReplaceItem(oldItem, null);
                        }
                        else
                        {

                            m_inventory.SendReplaceItem(oldItem, m_owningItem);
                        }
                                       
                        if (m_owningItem.Destroyed||m_owningItem.m_remainingCharges<=0)
                        {

                            m_inventory.ResetEquipmentSetRewards();
                            
                            m_inventory.m_character.AddSkillsFromEquipment(true);
                        }
                    }
                }

            }
        }
    }
}
