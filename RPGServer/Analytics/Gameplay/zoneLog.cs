using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Analytics.Global;

namespace Analytics.Gameplay
{
    public class ZoneLog
    {

        public string eventName = "zone";
        public string userID = "";
        public string sessionID = "";
        public string eventTimestamp = DateTime.Now.ToString();
        public object eventParams;// = new EventParams_Zone();
        public GoalCounts goalCounts = new GoalCounts();

        public ZoneLog(bool isNew)
        {
            if (isNew)
            {
                eventParams = new EventParams_ZoneNew();
            }
            else
            {
                eventParams = new EventParams_ZoneTravel();
            }

        }

        public ZoneLog(string i_userID, string i_sessionID, string i_eventTimestamp, bool isNew)
        {
            userID = i_userID;
            sessionID = i_sessionID;
            eventTimestamp = i_eventTimestamp;
            if (isNew)
            {
                eventParams = new EventParams_ZoneNew();
            }
            else
            {
                eventParams = new EventParams_ZoneTravel();
            }
        }

        public EventParams_ZoneTravel getEventParamsTravel()
        { return (EventParams_ZoneTravel)eventParams; }

        public EventParams_ZoneNew getEventParamsNew()
        { return (EventParams_ZoneNew)eventParams; }

    }

    public class EventParams_ZoneNew
    {
        public string action = "";
        public string currentZoneID = "";
        public string currentZoneName = "";

        public EventParams_ZoneNew()
        { }

        public EventParams_ZoneNew(string i_action, string i_currentZoneID, string i_currentZoneName)
        {
            action = i_action;
            currentZoneID = i_currentZoneID;
            currentZoneName = i_currentZoneName;
        }

    }

    public class EventParams_ZoneTravel
    {
        public string action = "";
        public string currentZoneID = "";
        public string currentZoneName = "";
        public string newZoneID = "";
        public string newZoneName = "";

        public EventParams_ZoneTravel()
        { }

        public EventParams_ZoneTravel(string i_action, string i_currentZoneID, string i_currentZoneName, string i_newZoneID, string i_newZoneName)
        {
            action = i_action;
            currentZoneID = i_currentZoneID;
            currentZoneName = i_currentZoneName;
            newZoneID = i_newZoneID;
            newZoneName = i_newZoneName; 
        }
    }
         /*            
            Example JSON for when a player discovers a new zone:
            {
                "eventName": "zone",
                "userID": " GA_098987AY127FAFS1192",
                "sessionID": "f32f4a04-e75b-42f0-a2a9-18c0805f09b1",
                "eventParams": {
                    "action": "Discovered",
                    "currentZoneID": "12133223",
                    "currentZoneName": "Desert"
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
             
            Example JSON for when a player changes zone:
            {
                "eventName": "zone",
                "userID": " GA_098987AY127FAFS1192",
                "sessionID": "f32f4a04-e75b-42f0-a2a9-18c0805f09b1",
                "eventParams": {
                    "action": "Changed",
                    "currentZoneID": "12133223",
                    "currentZoneName": "Desert",
                    "newZoneID": "2321323",
                    "newZoneName": "Winterfra"
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
