using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Analytics.Global;

namespace Analytics.Social
{
    public class InviteReceived
    {
        public string eventName = "inviteReceived";
        public string userID = "";
        public string sessionID = "";
        public string eventTimestamp = DateTime.Now.ToString();
        public EventParams_InviteReceived eventParams = new EventParams_InviteReceived();
        public GoalCounts goalCounts = new GoalCounts();

        public InviteReceived()
        { }

        public InviteReceived(string i_userID, string i_sessionID, string i_eventTimestamp)
        {
            userID = i_userID;
            sessionID = i_sessionID;
            eventTimestamp = i_eventTimestamp;
        }
    }

    public class EventParams_InviteReceived
    {
        public string inviteType = "";
        public string senderID = "";
        public string uniqueTracking = "";
        public bool isInviteAccepted = false;

        public EventParams_InviteReceived()
        { }

        public EventParams_InviteReceived(string i_senderID, string i_uniqueTracking, bool i_isInviteAccepted)
        {
            senderID = i_senderID;
            uniqueTracking = i_uniqueTracking;
            isInviteAccepted = i_isInviteAccepted;
        }
    }
        /*             
            Example :
            {
                "eventName": "inviteReceived",
                "userID": " GA_098987AY127FAFS1192",
                "sessionID": "f32f4a04-e75b-42f0-a2a9-18c0805f09b1",
                "eventParams": {
                    "senderId": "tr32ygy232gy3g2y",
                    "uniqueTracking": "2415123332",
                    "isInviteAccepted": true
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
