using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Analytics.Global;

namespace Analytics.Social
{
    public class MessageSent
    {
        public string eventName = "messageSent";
        public string userID = "";
        public string sessionID = "";
        public string eventTimestamp = DateTime.Now.ToString();
        public EventParams_MessageSent eventParams = new EventParams_MessageSent();
        public GoalCounts goalCounts = new GoalCounts();

        public MessageSent()
        { }

        public MessageSent(string i_userID, string i_sessionID, string i_eventTimestamp)
        {
            userID = i_userID;
            sessionID = i_sessionID;
            eventTimestamp = i_eventTimestamp;
        }
    }

    public class EventParams_MessageSent
    {
        public string uniqueTracking = "";
        public string communicationType = "";
        public List<Receipient> recipients = new List<Receipient>();

        public EventParams_MessageSent()
        { }

        public EventParams_MessageSent(string i_uniqueTracking, string i_communicationType)
        {
            uniqueTracking = i_uniqueTracking;
            communicationType = i_communicationType;
        }

        public void addReceipient(string i_receipientID)
        {
            Receipient receipient = new Receipient(i_receipientID);
            recipients.Add(receipient);
        }
    }
        /*
            Example :
            {
                "eventName": "messageSent",
                "userID": " GA_098987AY127FAFS1192",
                "sessionID": "f32f4a04-e75b-42f0-a2a9-18c0805f09b1",
                "eventParams": {
                    "uniqueTracking": "2415123332",
                    "communicationType": "In Game",
                    "recipients ": [
                        {
                            "recipientId": " GA_098987AY127FAFS125625"
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
