using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Analytics.Global;

namespace Analytics.Gameplay
{
    public class GameHelp
    {
        public string eventName = "Help";
        public string userID = "";
        public string sessionID = "";
        public string eventTimestamp = DateTime.Now.ToString();
        
        public EventParams_Help eventParams = new EventParams_Help();
        public GoalCounts goalCounts = new GoalCounts();

        public GameHelp()
        { }

        public GameHelp(string i_userID, string i_sessionID, string i_eventTimestamp)
        {
            userID = i_userID;
            sessionID = i_sessionID;
            eventTimestamp = i_eventTimestamp;
        }

    }
    public class EventParams_Help
    {
        public string helpSection = "";
        
        public EventParams_Help()
        { }

        public EventParams_Help(string i_helpSection)
        {
            helpSection = i_helpSection;
        }
    }

             /*            
            Example:
            {
                "eventName": "help",
                "userID": " GA_098987AY127FAFS1192",
                "sessionID": "f32f4a04-e75b-42f0-a2a9-18c0805f09b1",
                "eventParams": {
                    "helpSection": "Tutorial 1"
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
