using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Analytics.Global;

namespace Analytics.Social
{
    public class Social
    {
        public string eventName = "social";
        public string userID = "";
        public string sessionID = "";
        public string eventTimestamp = DateTime.Now.ToString();
        public EventParams_Social eventParams;
        public GoalCounts goalCounts = new GoalCounts();

        public Social(RewardType i_rewardType)
        {
            eventParams = new EventParams_Social(i_rewardType);
        }

        public Social(string i_userID, string i_sessionID, string i_eventTimestamp, RewardType i_rewardType)
        {
            userID = i_userID;
            sessionID = i_sessionID;
            eventTimestamp = i_eventTimestamp;
            eventParams = new EventParams_Social(i_rewardType);
        }
    }

    public class EventParams_Social
    {
        public string socialType = "";
        public Reward reward;

        public EventParams_Social(RewardType i_rewardType)
        {
            reward = new Reward(i_rewardType);
        }
    }
    enum Social_Action
    {
        LIKED,
        FOLLOWED,
        WALL_POST,
        TWEET,
    }

        /*
            Example :

            {
                "eventName": "social",
                "userID": " GA_098987AY127FAFS1192",
                "sessionID": "f32f4a04-e75b-42f0-a2a9-18c0805f09b1",
                "eventParams": {
                    "socialType": "Wall Post",
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
                        },
                        "items": [
                            {
                                "item": {
                                    "itemName": "Pick Axe",
                                    "itemType": "235652353",
                                    "itemAmount": 2
                                }
                            }
                        ]
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
