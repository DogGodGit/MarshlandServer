using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Analytics.Global;

namespace Analytics.Gameplay
{
    public class OpponentDefeated
    {
        public string eventName = "opponentDefeated";
        public string userID = "";
        public string sessionID = "";
        public string eventTimestamp = DateTime.Now.ToString();
        public EventParams_OpponentDefeated eventParams;
        public GoalCounts goalCounts = new GoalCounts();

        public OpponentDefeated(RewardType i_rewardType)
        {
            eventParams = new EventParams_OpponentDefeated(i_rewardType);
        }

        public OpponentDefeated(string i_userID, string i_sessionID, string i_eventTimestamp, RewardType i_rewardType)
        {
            userID = i_userID;
            sessionID = i_sessionID;
            eventTimestamp = i_eventTimestamp;
            eventParams = new EventParams_OpponentDefeated(i_rewardType);
        }
    }

    public class EventParams_OpponentDefeated
    {
        public string opponentID = "";
        public string opponentName = "";
        public Reward reward;

        public EventParams_OpponentDefeated(RewardType i_rewardType)
        {
            reward = new Reward(i_rewardType);
        }

        public EventParams_OpponentDefeated(string i_opponentID, string i_opponentName, RewardType i_rewardType)
        {
            opponentID = i_opponentID;
            opponentName = i_opponentName;
            reward = new Reward(i_rewardType);
        }
    }
       /*             
            Example:
            {
             "eventName": "opponentDefeated",
                "userID": " GA_098987AY127FAFS1192",
                "sessionID": "f32f4a04-e75b-42f0-a2a9-18c0805f09b1",
                "eventParams": {
                    "opponentID": "GA_129821982987GGSAFT",
                    "opponentName": "George123",
                    "reward": {
                        "name": "Reward",
                        "products": {
                            "virtualCurrencies": [
                                {
                                    "virtualCurrency": {
                                        "virtualCurrencyType": "PREMIUM",
                                        "virtualCurrencyAmount": 500,
                                        "virtualCurrencyName": "Gold"
                                    }
                                }
                            ]
                        }
                     }
                 },
                "goalCounts":{
                    "userLevel": 10, 
                    "experience": 10065,
                    "health": 1213, 
                    "energy": 1213, 
                    "strength": 6, 
                    "dexterity": 7, 
                    "focus": 12, 
                    "vitality": 13, 
                    "attack": 53, 
                    "defence": 12, 
                    "damage": 70, 
                    "armour": 15, 
                    "gold": 500, 
                    "platinum": 25
                }
            }

         */

}
