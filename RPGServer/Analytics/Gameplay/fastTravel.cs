using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Analytics.Global;

namespace Analytics.Gameplay
{
    public class FastTravel
    {

        public string eventName = "fastTravel";
        public string userID = "";
        public string sessionID = "";
        public string eventTimestamp = DateTime.Now.ToString();
        public object eventParams;// = new EventParams_FastTravel();
        public GoalCounts goalCounts = new GoalCounts();

        public FastTravel(bool isNew)
        {
            if (isNew)
            {
                eventParams = new EventParams_FastTravelNew();
            }
            else
            {
                eventParams = new EventParams_FastTravel();
            }
        }

        public FastTravel(string i_userID, string i_sessionID, string i_eventTimestamp, bool isNew)
        {
            userID = i_userID;
            sessionID = i_sessionID;
            eventTimestamp = i_eventTimestamp;

            if (isNew)
            {
                eventParams = new EventParams_FastTravelNew();
            }
            else
            {
                eventParams = new EventParams_FastTravel();
            }
        }

        public EventParams_FastTravel getEventParamsTravel()
        { return (EventParams_FastTravel)eventParams; }

        public EventParams_FastTravelNew getEventParamsNew()
        { return (EventParams_FastTravelNew)eventParams; }
    }

    public class EventParams_FastTravelNew
    {
        public string action = "FOUND";
        public string currentFastTravelID = "";
        //public string currentFastTravelName = "Unknown";

        public EventParams_FastTravelNew()
        { }

        public EventParams_FastTravelNew(string i_action, string i_currentFastTravelID /*string i_currentFastTravelName*/)
        {
            action = i_action;
            currentFastTravelID = i_currentFastTravelID;
        }
    }

    public class EventParams_FastTravel
    {
        public string action = "USED";
        //public string currentFastTravelID = "";
        //public string currentFastTravelName = "Unknown";
        public string toFastTravelID = "";
        //public string toFastTravelName = "Unknown";

        public EventParams_FastTravel()
        { }

        public EventParams_FastTravel(string i_action, /*string i_currentFastTravelID, string i_currentFastTravelName,*/ string i_toFastTravelID /*string i_toFastTravelName*/)
        {
            action = i_action;
            //currentFastTravelID = i_currentFastTravelID;
            toFastTravelID = i_toFastTravelID;
        }
    }


    /*
                Variable	Enumeration/Format	Description
                action	FOUND
                        USED	                Which action was taken with the item.
    */

    /*             
    Example Of Fast Travel Found:
    {
        "eventName": "fastTravel",
        "userID": " GA_098987AY127FAFS1192",
        "sessionID": "f32f4a04-e75b-42f0-a2a9-18c0805f09b1",
        "eventParams": {
            "action ": "FOUND",
            "currentFastTravelID": "78FA908AL",
            "currentFastTravelName": "Main Gate"
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
 *  Example of FastTravel Used:

    {
        "eventName": "fastTravel",
        "userID": " GA_098987AY127FAFS1192",
        "sessionID": "f32f4a04-e75b-42f0-a2a9-18c0805f09b1",
        "eventParams": {
            "action ": "USED",
            "currentFastTravelID": "78FA908AL",
            "currentFastTravelName": "Main Gate",
            "toFastTravelID": "90GFD88QV",
            "toFastTravelName": "Castle Entrance"

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
