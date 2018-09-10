using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Analytics.Global;

namespace Analytics.Social
{
    public class PlayerInfo
    {
        public string eventName = "playerInfo";
        public string userID = "";
        public string sessionID = "";
        public string eventTimestamp = DateTime.Now.ToString();
        public EventParams_PlayerInfo eventParams = new EventParams_PlayerInfo();
        public GoalCounts goalCounts = new GoalCounts();

        public PlayerInfo()
        { }

        public PlayerInfo(string i_userID, string i_sessionID, string i_eventTimestamp)
        {
            userID = i_userID;
            sessionID = i_sessionID;
            eventTimestamp = i_eventTimestamp;
        }
    }

    public class EventParams_PlayerInfo
    {
        public int birthYear = -1;
        public int friendsCount = -1;
        public string gender = "";
        public string userRegion = "";
        public string userCountry = "";

        public EventParams_PlayerInfo()
        { }

        public EventParams_PlayerInfo(int i_birthYear, int i_friendsCount, string i_gender, string i_userRegion, string i_userCountry)
        {
            birthYear = i_birthYear;
            friendsCount = i_friendsCount;
            gender = i_gender;
            userRegion = i_userRegion;
            userCountry = i_userCountry;
        }
    }
    //finalise enum selections
    //public 
        /*
            Variable	    Enumeration/Format	Description
            birthYear		
            friendsCount		
            gender	        MALE
                            FEMALE	
            userCountry	    ISO3666-1	Two letter codes for each country (ie US)
            userRegion	    ISO3666-1	Two letter codes for each country (ie US)

             
            Example :
            {
                "eventName": "playerInfo",
                "userID": " GA_098987AY127FAFS1192",
                "sessionID": "f32f4a04-e75b-42f0-a2a9-18c0805f09b1",
                "eventParams": {
                    "birthYear": 1987,
                    "friendsCount": 2,
                    "gender": "MALE",
                    "userRegion": "CA",
                    "userCountry": "US"
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
