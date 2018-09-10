﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Analytics.Global;

namespace Analytics.Gameplay
{
    public class SupportLog
    {
        public string eventName = "support";
        public string userID = "";
        public string sessionID = "";
        public string eventTimestamp = DateTime.Now.ToString();
        public EventParams_Support eventParams = new EventParams_Support();
        public GoalCounts goalCounts = new GoalCounts();

        public SupportLog()
        { }

        public SupportLog(string i_userID, string i_sessionID, string i_eventTimestamp)
        {
            userID = i_userID;
            sessionID = i_sessionID;
            eventTimestamp = i_eventTimestamp;
        }
    }

    public class EventParams_Support
    {
        public string ticketID;
        public EventParams_Support()
        { }

        public EventParams_Support(string i_ticketID)
        {
            ticketID = i_ticketID;
        }
    }
         /*            
            Example:
            {
                "eventName": "fastTravelUsed",
                "userID": " GA_098987AY127FAFS1192",
                "sessionID": "f32f4a04-e75b-42f0-a2a9-18c0805f09b1",
                "eventParams": {
                    "ticketID ": "1267F8720J1029BD731235423"
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
