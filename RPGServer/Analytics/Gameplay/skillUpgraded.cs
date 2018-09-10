using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Analytics.Global;

namespace Analytics.Gameplay
{
    public class SkillUpgraded
    {
        public string eventName = "skillUpgraded";
        public string userID = "";
        public string sessionID = "";
        public string eventTimestamp = DateTime.Now.ToString();
        public EventParams_SkillUpgraded eventParams = new EventParams_SkillUpgraded();
        public GoalCounts goalCounts = new GoalCounts();

        public SkillUpgraded()
        { }

        public SkillUpgraded(string i_userID, string i_sessionID, string i_eventTimestamp)
        {
            userID = i_userID;
            sessionID = i_sessionID;
            eventTimestamp = i_eventTimestamp; 
        }
    }

    public class EventParams_SkillUpgraded
    {
        public string skillID = "";
        public string skillName = "";
        public int currentSkillLevel = -1;
        public int newSkillLevel = -1;

        public EventParams_SkillUpgraded()
        { }

        public EventParams_SkillUpgraded(string i_skillID, string i_skillName, int i_currentSkillevel, int i_newSkillLevel)
        {
            skillID = i_skillID;
            skillName = i_skillName;
            currentSkillLevel = i_currentSkillevel;
            newSkillLevel = i_newSkillLevel;
        }
    }
            /*             
            Example:
            {
                "eventName": "skillUpgraded",
                "userID": " GA_098987AY127FAFS1192",
                "sessionID": "f32f4a04-e75b-42f0-a2a9-18c0805f09b1",
                "eventParams": {
                    "skillID": "12133223",
                    "skillName": "Fire Shield",
                    "currentSkillLevel": 2,
                    "newSkillLevel": 4
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
