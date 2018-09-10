using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Analytics.Global;

namespace Analytics.Monetisation
{
    public class AchievementLog
    {
        public string eventName = "achievement";
        public string userID = "";
        public string sessionID = "";
        public string eventTimestamp = DateTime.Now.ToString();
        public eventParams_Achievement eventParams = new eventParams_Achievement();
        public GoalCounts goalCounts = new GoalCounts();

        public AchievementLog()
        {}

        public AchievementLog(string i_userID, string i_sessionID, string i_eventTimestamp)
        {
            userID = i_userID;
            sessionID = i_sessionID;
            eventTimestamp = i_eventTimestamp;
        }
    }

    public class eventParams_Achievement
    {
        public string achievementName;
        public string achievementID;

        public eventParams_Achievement()
        {
            achievementName = "";
            achievementID = ""; 
        }

        public eventParams_Achievement(string i_achievementName, string i_achievementID)
        {
            achievementName = i_achievementName;
            achievementID = i_achievementID;
        }
    }
}
        /*
Example :


{
    "eventName": "achievement",
    "userID": " GA_098987AY127FAFS1192",
    "sessionID": "f32f4a04-e75b-42f0-a2a9-18c0805f09b1",
    "eventParams": {
        "achievementName": "Gold Medal",
        "achievementId": "25412514"
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