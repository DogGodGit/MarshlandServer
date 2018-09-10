using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;
using Lidgren.Network;

namespace MainServer.DailyLoginReward
{
    static class DailyRewardManager
    {
        internal enum RewardChainType {Consecutive, Incremental}
        internal enum NextRewardAction {None, Next, Restart}

        internal static bool DAILY_REWARDS_ACTIVE = false;
        internal static string DEFAULT_DAILY_REWARD_INTERVAL = "0.23:00:00";
        
        private static string k_setDailyRewardReceivedSQL = "update character_details set last_daily_reward_recieved = '{0}', next_daily_reward_step = '{1}', daily_rewards_received = {2} where character_id = {3}";

        static public TimeSpan GetDailyRewardInterval()
        {
            if (ConfigurationManager.AppSettings["DailyRewardInterval"] != null)
                return TimeSpan.Parse(ConfigurationManager.AppSettings["DailyRewardInterval"]);
            else
                return TimeSpan.Parse(DEFAULT_DAILY_REWARD_INTERVAL);
        }

        // Process a player's login message. Check to see if the player qualifies for a new daily reward and awards it if so.
        static public DailyRewardTemplate ProcessPlayerLogin(Player i_Player, RewardChainType i_RewardChainType/*, ref bool newCharacter*/)
        {
            if (!DAILY_REWARDS_ACTIVE)
            {
                return null;
            }

            NextRewardAction shouldReward = NextRewardAction.None;

            if (i_Player.m_activeCharacter.m_lastRewardRecieved.Equals(default(DateTime))) //character has never recieved a reward before
            {
                DateTime test = (DateTime.UtcNow - new TimeSpan(1, 0, 0, 1));
                string command = String.Format("update character_details set last_daily_reward_recieved = '{0}', next_daily_reward_step = '1' where character_id = {1}", test.ToString("yyyy-MM-dd HH:mm:ss"), i_Player.m_activeCharacter.m_character_id);
                i_Player.m_activeCharacter.m_numRecievedRewards = 0;
                Program.processor.m_worldDB.runCommandSync(command);
            }

            switch (i_RewardChainType)
            {
                case RewardChainType.Consecutive:
                    shouldReward = EvaluateConsecutiveTime(i_Player.m_activeCharacter.m_lastRewardRecieved);
                    break;
                case RewardChainType.Incremental:
                    shouldReward = EvaluateIncrementalTime(i_Player.m_activeCharacter.m_lastRewardRecieved);
                    break;
            }

            DailyRewardTemplate reward = null;
            if (shouldReward != NextRewardAction.None)
            {
                reward = AddReward(i_Player, shouldReward);
                UpdateRewardRecords(i_Player, reward);
            }

            return reward;
        }

        // Get the next reward for the current player
        static public DailyRewardTemplate GetTodaysReward(Player i_Player)
        {
            int currentStep = i_Player.m_activeCharacter.m_nextDailyRewardStep == 1
                ? DailyRewardTemplateManager.MAX_STEPS
                : i_Player.m_activeCharacter.m_nextDailyRewardStep - 1;

            return DailyRewardTemplateManager.GetRewardForStep(currentStep);
        }

        // Evaluates if the current date is within a 24 hour to 48 hour period after the last login date
        static NextRewardAction EvaluateConsecutiveTime(DateTime i_LastLogin)
        {
            // Untested!
            // contains DateTime.Now NOT DateTime.UtcNow
            // ignores time component

            /*if ((DateTime.UtcNow - SingleDayTimespan).Date < i_LastLogin.Date) //less then a day has passed
            {
                return NextRewardAction.None;
            }
            else if ((DateTime.UtcNow - SingleDayTimespan).Date == i_LastLogin.Date) //last login was yesterday
            {
                return NextRewardAction.Next;
            }
            else if ((DateTime.UtcNow - SingleDayTimespan).Date > i_LastLogin.Date) //logged in over a day ago
            {
                return NextRewardAction.Restart;
            }*/

            return NextRewardAction.None;
        }

        // Evaluates if the current date is later then the last login date
        static NextRewardAction EvaluateIncrementalTime(DateTime i_LastLogin)
        {
            return (DateTime.UtcNow - GetDailyRewardInterval()) >= i_LastLogin ? NextRewardAction.Next : NextRewardAction.None;
        }

        // Add the next daily reward to the player
        static DailyRewardTemplate AddReward(Player i_Player, NextRewardAction i_ActionToTake)
        {
            i_Player.m_activeCharacter.m_nextDailyRewardStep = i_Player.m_activeCharacter.m_nextDailyRewardStep > DailyRewardTemplateManager.MAX_STEPS || i_ActionToTake == NextRewardAction.Restart ? 1 : i_Player.m_activeCharacter.m_nextDailyRewardStep; // if the user expects a larger reward then exists or needs to be reset for not logging in on time set the reward to 1;
            
            DailyRewardTemplate reward = DailyRewardTemplateManager.GetRewardForStep(i_Player.m_activeCharacter.m_nextDailyRewardStep);

            // There is a reward for this step
            if (reward != null)
            {
                Item addedItem = i_Player.m_activeCharacter.m_inventory.AddNewItemToCharacterInventory(reward.ItemTemplateID, reward.Quantity, false);
                i_Player.m_activeCharacter.m_inventory.SendInventoryUpdate();
                Program.processor.updateShopHistory(-1, -1, addedItem.m_inventory_id, addedItem.m_template_id, reward.Quantity, -1, (int)i_Player.m_activeCharacter.m_character_id, "Daily Login Reward for step " + reward.StepID);
            }

            return reward;
        }

        // Update our daily reward records on the database
        static void UpdateRewardRecords(Player i_Player, DailyRewardTemplate reward)
        {
            int newNextStep = i_Player.m_activeCharacter.m_nextDailyRewardStep;

            newNextStep = newNextStep >= DailyRewardTemplateManager.MAX_STEPS ? 1 : newNextStep + 1;
           
            if(reward != null)
                i_Player.m_activeCharacter.m_numRecievedRewards += 1;

            i_Player.m_activeCharacter.m_nextDailyRewardStep = newNextStep;
            i_Player.m_activeCharacter.m_lastRewardRecieved  = DateTime.UtcNow;
            
            string command = String.Format(k_setDailyRewardReceivedSQL, i_Player.m_activeCharacter.m_lastRewardRecieved.ToString("yyyy-MM-dd HH:mm:ss"),
                newNextStep, i_Player.m_activeCharacter.m_numRecievedRewards, i_Player.m_activeCharacter.m_character_id);

            Program.processor.m_worldDB.runCommandSync(command);
        }
    }
}
