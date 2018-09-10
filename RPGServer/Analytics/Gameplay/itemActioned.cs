using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Analytics.Global;

namespace Analytics.Gameplay
{
    public enum ItemAction
    {
        PICKED_UP, EQUIPPED, USED, DROPPED
    };

    public class ItemActioned
    {
        public string eventName = "itemActioned";
        public string userID = "";
        public string sessionID = "";
        public string eventTimestamp = DateTime.Now.ToString();
        public EventParams_ItemActioned eventParams = new EventParams_ItemActioned();
        public GoalCounts goalCounts = new GoalCounts();

        public ItemActioned()
        { }

        public ItemActioned(string i_userID, string i_sessionID, string i_eventTimestamp)
        {
            userID = i_userID;
            sessionID = i_sessionID;
            eventTimestamp = i_eventTimestamp;
        }
    }

    public class EventParams_ItemActioned
    {
        public string itemID = "";
        public string itemName = "";
        public string itemType = "";
        public string action = "";

        public EventParams_ItemActioned()
        {}

        public EventParams_ItemActioned(string i_itemID, string i_itemName, string i_itemType, string i_action)
        {
            itemID = i_itemID;
            itemName = i_itemName;
            itemType = i_itemType;
            action = i_action; 
        }

    }
/*
            Variable	Enumeration/Format	Description
            action	PICKED UP
                    EQUIPPED
                    USED
                    DROPPED	                Which action was taken with the item.
*/

            /*             
            Example:
            {
                "eventName": "itemActioned",
                "userID": " GA_098987AY127FAFS1192",
                "sessionID": "f32f4a04-e75b-42f0-a2a9-18c0805f09b1",
                "eventParams": {
                    "itemID": "12SDF24",
                    "itemName": "Big Sword",
                    "itemType": "Weapon", 
                    "action": "USED"
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
