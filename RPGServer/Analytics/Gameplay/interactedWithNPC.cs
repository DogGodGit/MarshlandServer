using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Analytics.Global;

namespace Analytics.Gameplay

{
    public class InteractedWithNPC
    {
        public string eventName = "interactedWithNPC";
        public string userID = "";
        public string sessionID = "";
        public string eventTimestamp = DateTime.Now.ToString();
        public EventParams_InteractedWithNPC eventParams = new EventParams_InteractedWithNPC();
        public GoalCounts goalCounts = new GoalCounts();

        public InteractedWithNPC()
        { }

        public InteractedWithNPC(string i_userID, string i_sessionID, string i_eventTimestamp)
        {
            userID = i_userID;
            sessionID = i_sessionID;
            eventTimestamp = i_eventTimestamp;
        }
    }

    public class EventParams_InteractedWithNPC
    {
        public string NPCID = "";
        public string NPCName = "";
        public string NPCType = "";

        public EventParams_InteractedWithNPC()
        { }

        public EventParams_InteractedWithNPC(string i_NPCID, string i_NPCName, string i_NPCType)
        {
            NPCID = i_NPCID;
            NPCName = i_NPCName;
            NPCType = i_NPCType; 
        }
    }

        /*             
            Example:
            {
                "eventName": "interactedWithNPC",
                "userID": " GA_098987AY127FAFS1192",
                "sessionID": "f32f4a04-e75b-42f0-a2a9-18c0805f09b1",
                "eventParams": {
                    "NPCID": "12133223",
                    "NPCName": "John the Jam Maker",
                    "NPCType": "Store"
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
