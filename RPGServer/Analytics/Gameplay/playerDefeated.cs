using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Analytics.Global;

namespace Analytics.Gameplay
{
    public class PlayerDefeated
    {

        public string eventName = "playerDefeated";
        public string userID = "";
        public string sessionID = "";
        public string eventTimestamp = DateTime.Now.ToString();
        public EventParams_PlayerDefeated eventParams = new EventParams_PlayerDefeated();
        public GoalCounts goalCounts = new GoalCounts();

        public PlayerDefeated()
        { }

        public PlayerDefeated(string i_userID, string i_sessionID, string i_eventTimestamp)
        {
            userID = i_userID;
            sessionID = i_sessionID;
            eventTimestamp = i_eventTimestamp;
        }
    }

    public class EventParams_PlayerDefeated
    {
        public string defeatedByID = "";
        public string defeatedByName = "";
        public string defeatedByType = "";

        public EventParams_PlayerDefeated()
        { }

        public EventParams_PlayerDefeated(string i_defeatedByID, string i_defeatedByName, string i_defeatedByType)
        {
            defeatedByID = i_defeatedByID;
            defeatedByName = i_defeatedByName;
            defeatedByType = i_defeatedByType;
        }
    }
        /*             
            Example:
            {
                "eventName": "playerDefeated",
                "userID": " GA_098987AY127FAFS1192",
                "sessionID": "f32f4a04-e75b-42f0-a2a9-18c0805f09b1",
                "eventParams": {
                    " defeatedByID ": "GA_18263167ASDV5A",
                    " defeatedByName ": "Borthar ",
                    " defeatedByType": "Player Character"
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
