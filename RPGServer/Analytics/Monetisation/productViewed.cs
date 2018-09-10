using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Analytics.Global;

namespace Analytics.Monetisation
{
    public class ProductViewed
    {
        public string eventName = "productViewed";
        public string userID = "";
        public string sessionID = "";
        public string eventTimestamp = DateTime.Now.ToString();
        public eventParams_ProductViewed eventParams = new eventParams_ProductViewed();
        public GoalCounts goalCounts = new GoalCounts();

        public ProductViewed()
        {}

        public ProductViewed(string i_userID, string i_sessionID, string i_eventTimestamp)
        {
            userID = i_userID;
            sessionID = i_sessionID;
            eventTimestamp = i_eventTimestamp;
        }
    }

    public class eventParams_ProductViewed
    {
        public string viewedProductID = "";
        public string viewedProductName = "";

        public eventParams_ProductViewed()
        {}

        public eventParams_ProductViewed(string i_viewedProductID, string i_viewedProductName)
        {
            viewedProductID = i_viewedProductID;
            viewedProductName = i_viewedProductName;
        }
    }
}
/*
Example :
{
    "eventName": "productViewed",
    "userID": " GA_098987AY127FAFS1192",
    "sessionID": "f32f4a04-e75b-42f0-a2a9-18c0805f09b1",
    "eventParams": {
        " viewedProductID ": "15489",
        " viewedProductName ": "Fine Jam"
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