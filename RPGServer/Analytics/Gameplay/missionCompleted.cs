using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Analytics.Global;

namespace Analytics.Gameplay
{
    public class MissionCompleted
    {
        public string eventName = "missionCompleted";
        public string userID = "";
        public string sessionID = "";
        public string eventTimestamp = DateTime.Now.ToString();
        public EventParams_MissionCompleted eventParams;
        public CustomParams_MissionCompleted customParams = new CustomParams_MissionCompleted();
        public GoalCounts goalCounts = new GoalCounts();

        public MissionCompleted(RewardType i_rewardType)
        {
            eventParams = new EventParams_MissionCompleted(i_rewardType);
        }

        public MissionCompleted(string i_userID, string i_sessionID, string i_eventTimestamp, RewardType i_rewardType)
        {
            userID = i_userID;
            sessionID = i_sessionID;
            eventTimestamp = i_eventTimestamp;
            eventParams = new EventParams_MissionCompleted(i_rewardType);
        }
    }

    public class EventParams_MissionCompleted
    {
        public string missionName = "";
        public string missionID = "";
        public Reward reward;
        public bool isTutorial = false;

        public EventParams_MissionCompleted(RewardType i_rewardType)
        {
            reward = new Reward(i_rewardType);
        }

        public EventParams_MissionCompleted(string i_missionName, string i_missionID, bool i_isTutorial, RewardType i_rewardType)
        {
            missionName = i_missionName;
            missionID = i_missionID;
            isTutorial = i_isTutorial;
            reward = new Reward(i_rewardType);
        }

        /*
             List<VirtualCurrencies>	virtualCurrencies;
           
            virtualCurrency	Object	True
            virtualCurrencyName	String	True
            virtualCurrencyType	String	True
            virtualCurrencyAmount	Integer	True
 
            List <Items> items;	False
            item	Object	True
            itemName	String	True
            itemType	String	True
            itemAmount	Integer	True
        */
    }

    public class CustomParams_MissionCompleted
    {
        public int experienceGained;
        public CustomParams_MissionCompleted()
        {}

        public CustomParams_MissionCompleted(int i_xpGained)
        {
            experienceGained = i_xpGained;
        }
    }

        /*             
            Example:
            {
                "eventName": "missionCompleted",
                "userID": " GA_098987AY127FAFS1192",
                "sessionID": "f32f4a04-e75b-42f0-a2a9-18c0805f09b1",
                "eventParams": {
                    "missionName": "In to the Valley of Death",
                    "missionId": "25412514",
                    "isTutorial": false,
                    "reward": {
                        "name": "Reward",
                        "products": {
                            "virtualCurrencies": [
                                {
                                    "virtualCurrency": {
                                        "virtualCurrencyName": "Gems",
                                        "virtualCurrencyType": "PREMIUM",
                                        "virtualCurrencyAmount": 500
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
