using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Analytics.Global;

namespace Analytics.Social
{
    public class MessageReceived
    {
        public string eventName = "messageReceived";
        public string userID = "";
        public string sessionID = "";
        public string eventTimestamp = DateTime.Now.ToString();
        public EventParams_MessageReceived eventParams = new EventParams_MessageReceived();
        public GoalCounts goalCounts = new GoalCounts();

        public MessageReceived()
        { }

        public MessageReceived(string i_userID, string i_sessionID, string i_eventTimestamp)
        {
            userID = i_userID;
            sessionID = i_sessionID;
            eventTimestamp = i_eventTimestamp;
        }
    }
    
    public class EventParams_MessageReceived
    {
        public string senderID = "";
        public string uniqueTracking = "";
        public string communicationType = "";

        public EventParams_MessageReceived()
        { }

        public EventParams_MessageReceived(string i_senderID, string i_uniqueTracking, string i_communicationType)
        {
            senderID = i_senderID;
            uniqueTracking = i_uniqueTracking;
            communicationType = i_communicationType;
        }
    }
        /*
            Example :

            {
                "eventName": "messageReceived",
                "userID": " GA_098987AY127FAFS1192",
                "sessionID": "f32f4a04-e75b-42f0-a2a9-18c0805f09b1",
                "eventParams": {
                    "senderId": " GA_098987AY127FAFS3192",
                    "uniqueTracking": "2415123332",
                    "communicationType": "In Game"
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
