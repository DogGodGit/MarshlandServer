using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Analytics.Global;

namespace Analytics.Gameplay
{
    public class Options
    {
        public string eventName = "options";
        public string userID = "";
        public string sessionID = "";
        public string eventTimestamp = DateTime.Now.ToString();
        public EventParams_Options eventParams = new EventParams_Options();
        public GoalCounts goalCounts = new GoalCounts();

        public Options()
        { }

        public Options(string i_userID, string i_sessionID, string i_eventTimestamp)
        {
            userID = i_userID;
            sessionID = i_sessionID;
            eventTimestamp = i_eventTimestamp;
        }
    }

    public class EventParams_Options
    {
        public string option = "";
        public string action = "";

        public EventParams_Options()
        { }

        public EventParams_Options(string i_option, string i_action)
        {
            option = i_option;
            action = i_action;
        }
    }
     
    /*
             
            Example:
            {
                "eventName": "options",
                "userID": " GA_098987AY127FAFS1192",
                "sessionID": "f32f4a04-e75b-42f0-a2a9-18c0805f09b1",
                "eventParams": {
                    "option": "SOUND",
                    "action": "ON"
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
