using System;
using Analytics.Global;

namespace Analytics.Gameplay
{
    public class BountyTracking
    {
        public string eventName = "bountyTracking";
        public string userID = String.Empty;
        public string sessionID = String.Empty;
        public string eventTimestamp = DateTime.Now.ToString();
        public EventParams_BountyTracking eventParams = new EventParams_BountyTracking();

        public GoalCounts goalCounts = new GoalCounts();

        public BountyTracking()
        {
        }

        public BountyTracking(string i_userID, string i_sessionID, string i_eventTimestamp)
        {
            userID = i_userID;
            sessionID = i_sessionID;
            eventTimestamp = i_eventTimestamp;
        }
    }

    public class EventParams_BountyTracking
    {
        public string characterID = String.Empty;
        public string missionID = String.Empty;
        public string bountyStatus = String.Empty;

        public EventParams_BountyTracking()
        {
        }

        public EventParams_BountyTracking(string i_characterID, string i_missionID, string i_bountyStatus)
        {
            characterID = i_characterID;
            missionID = i_missionID;
            bountyStatus = i_bountyStatus;
        }
    }


    /*
    Variable	Enumeration/Format	Description
    bountyStatus: Purchased, Claimed, Completed
    {
        "eventName": "BountyTracking",
        "userID": "92349",
        "sessionID": "1690097810",
        "eventTimestamp": "2015-06-02 17:58:32.000",
        "eventParams": {
            "characterID": "1530",
            "missionID": "523",
            "bountyStatus": "Claimed"
        },
        "goalCounts": {
            "userLevel": 100,
            "experience": 0,
            "health": 63,
            "energy": 54,
            "strength": 5,
            "dexterity": 5,
            "focus": 10,
            "vitality": 10,
            "attack": 5,
            "defence": 10,
            "damage": 6,
            "armour": 8,
            "gold": 0,
            "platinum": 92490
        }
    }
    */
}