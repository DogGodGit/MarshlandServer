using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Analytics.Global;

namespace Analytics.Monetisation
{
    public class LevelUp
    {
        public string eventName = "levelUp";
        public string userID = "";
        public string sessionID = "";
        public string eventTimestamp = DateTime.Now.ToString();
        public eventParams_LevelUp eventParams = new eventParams_LevelUp();
        public customParams_LevelUp customParams = new customParams_LevelUp();
        public GoalCounts goalCounts = new GoalCounts();

        public LevelUp()
        {}

        public LevelUp(string i_userID, string i_sessionID, string i_eventTimestamp)
        {
            userID =  i_userID;
            sessionID = i_sessionID;
            eventTimestamp = i_eventTimestamp;
        }
    }

    public class eventParams_LevelUp
    {
        public string levelUpName;

        public eventParams_LevelUp()
        {
            levelUpName = "";
        }

        public eventParams_LevelUp(string i_levelUpName)
        {
            levelUpName = i_levelUpName;
        }
    }

    public class customParams_LevelUp
    {
        public int statPointsGained;

        public customParams_LevelUp()
        {
            statPointsGained = -1;
        }

        public customParams_LevelUp(int i_statPointsGained)
        {
            statPointsGained = i_statPointsGained;
        }
    }
}
         /*
Example :

{
    "eventName": "levelUp",
    "userID": " GA_098987AY127FAFS1192",
    "sessionID": "f32f4a04-e75b-42f0-a2a9-18c0805f09b1",
    "eventParams": {
        "levelUpName": "CharacterLevel"
    },
    "customParams": {
        "statPointsGained": 5
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