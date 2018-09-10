using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Analytics.Global;

namespace Analytics.Gameplay
{
    public class StatUpgraded
    {
        public string eventName = "statUpgraded";
        public string userID = "";
        public string sessionID = "";
        public string eventTimestamp = DateTime.Now.ToString();
        public EventParams_StatUpgraded eventParams = new EventParams_StatUpgraded();
        public GoalCounts goalCounts = new GoalCounts();

        public StatUpgraded()
        { }

        public StatUpgraded(string i_userID, string i_sessionID, string i_eventTimestamp)
        {
            userID = i_userID;
            sessionID = i_sessionID;
            eventTimestamp = i_eventTimestamp; 
        }
    }

    public class EventParams_StatUpgraded
    {
        public string statName = "";
        public int currentValue = -1;
        public int newValue = -1;

        public EventParams_StatUpgraded()
        { }

        public EventParams_StatUpgraded(string i_statName, int i_currentValue, int i_newValue)
        {
            statName = i_statName;
            currentValue = i_currentValue;
            newValue = i_newValue; 
        }
    }
        /*
            Example:
            {
                "eventName": "statUpgraded",
                "userID": " GA_098987AY127FAFS1192",
                "sessionID": "f32f4a04-e75b-42f0-a2a9-18c0805f09b1",
                "eventParams": {
                    "statName": "Dexterity",
                    "CurrentValue": 2,
                    "newValue": 10
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
