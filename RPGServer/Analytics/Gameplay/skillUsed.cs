using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Analytics.Global;

namespace Analytics.Gameplay
{
    public class SkillUsed
    {

        public string eventName = "skillUsed";
        public string userID = "";
        public string sessionID = "";
        public string eventTimestamp = DateTime.Now.ToString();
        public EventParams_SkillUsed eventParams = new EventParams_SkillUsed();

        public GoalCounts goalCounts = new GoalCounts();

        public SkillUsed()
        { }

        public SkillUsed(string i_userID, string i_sessionID, string i_eventTimestamp)
        {
            userID = i_userID;
            sessionID = i_sessionID;
            eventTimestamp = i_eventTimestamp;
        }
    }

    public class EventParams_SkillUsed
    {
        public string skillID = "";
        public string skillName = "";
        public bool success = false;
        public string reasonForFailure = "";

        public EventParams_SkillUsed()
        { }

        public EventParams_SkillUsed(string i_skillID, string i_skillName, bool i_success, string i_reasonForFailure)
        {
            skillID = i_skillID;
            skillName = i_skillName;
            success = i_success;
            reasonForFailure = i_reasonForFailure;
        }
    }
        /*   
            Variable	Enumeration/Format	Description
            success	    true / false	    Was the skill cast successfully.

         * Example:
            {
                "eventName": "skillUsed",
                "userID": " GA_098987AY127FAFS1192",
                "sessionID": "f32f4a04-e75b-42f0-a2a9-18c0805f09b1",
                "eventParams": {
                    "skillID": "12133223",
                    "skillName": "Fire Shield",
                    "success ": false,
                    "reasonForFailure ": "Spell Interrupted"
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
