using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Analytics.Global;

namespace Analytics.Social
{
    public class InviteSent
    {
        public string eventName = "inviteSent";
        public string userID = "";
        public string sessionID = "";
        public string eventTimestamp = DateTime.Now.ToString();
        public EventParams_InviteSent eventParams = new EventParams_InviteSent();
        public GoalCounts goalCounts = new GoalCounts();

        public InviteSent()
        {}

        public InviteSent(string i_userID, string i_sessionID, string i_eventTimestamp)
        {
            userID = i_userID;
            sessionID = i_sessionID;
            eventTimestamp = i_eventTimestamp;
        }
    }

    public class EventParams_InviteSent
    {
        public string inviteType = "";
        public string uniqueTracking = "";
        public List<Receipient> recipients = new List<Receipient>();

        public EventParams_InviteSent()
        {}

        public EventParams_InviteSent(string i_uniqueTracking)
        {
            uniqueTracking = i_uniqueTracking;
        }

        public void addReceipients(string i_receipientID)
        {
            Receipient receipient = new Receipient(i_receipientID);
            recipients.Add(receipient);
        }
    }


            /*             
            Example :
            {
                "eventName": "inviteSent",
                "userID": " GA_098987AY127FAFS1192",
                "sessionID": "f32f4a04-e75b-42f0-a2a9-18c0805f09b1",
                "eventParams": {
                    "uniqueTracking": "2415123332",
                    "recipients": [
                        {
                            "recipientId": "52365213712"
                        }
                    ]
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
