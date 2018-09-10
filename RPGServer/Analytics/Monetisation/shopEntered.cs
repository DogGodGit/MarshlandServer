using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Analytics.Global;

namespace Analytics.Monetisation
{
    public class ShopEntered
    {
        public string eventName = "shopEntered";
        public string userID = "";
        public string sessionID = "";
        public string eventTimestamp = DateTime.Now.ToString();

        public eventParams_ShopEntered eventParams = new eventParams_ShopEntered();
        public GoalCounts goalCounts = new GoalCounts();

        public ShopEntered()
        {}

        public ShopEntered(string i_userID, string i_sessionID, string i_eventTimestamp)
        {
            userID = i_userID;
            sessionID  = i_sessionID;
            eventTimestamp = i_eventTimestamp;
        }
    }

    public class eventParams_ShopEntered
    {
        public string shopID = "";
        public string shopName = "";
        //public string shopType = "";

        public eventParams_ShopEntered()
        {}

        public eventParams_ShopEntered(string i_shopID, string i_shopName/*, string i_shopType*/)
        {
            shopID = i_shopID;
            shopName = i_shopName;
            //shopType = i_shopType;
        }
    }
}
        /* 
Example :
{
    "eventName": "shopEntered",
    "userID": " GA_098987AY127FAFS1192",
    "sessionID": "f32f4a04-e75b-42f0-a2a9-18c0805f09b1",
    "eventParams": {
        " shopID ": "1678FD",
        " shopName ": "SwordsAndJams",
        " shopType ": "Weapon Store"
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