using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Analytics.Global;
using MainServer;

namespace Analytics.Social
{
    public enum GuildAction
    { JOINED, LEFT, FOUNDED,KICKED };

    public class Guild
    {
        public string eventName = "guild";
        public string userID = "";
        public string sessionID = "";
        public string eventTimestamp = DateTime.Now.ToString();
        public EventParams_Guild eventParams = new EventParams_Guild();

        public CustomParams_Guild customParams = null;

        public GoalCounts goalCounts = new GoalCounts();

        public Guild()
        { }

        public Guild(string i_userID, string i_sessionID, string i_eventTimestamp)
        {
            userID = i_userID;
            sessionID = i_sessionID;
            eventTimestamp = i_eventTimestamp;
        }
    }

    public class EventParams_Guild
    {
        public string action = "";
        public string guildName = "";

        public EventParams_Guild()
        { }

        public EventParams_Guild(string i_action, string i_guildName)
        {
            action = i_action;
            guildName = i_guildName;
        }
    }
    public class CustomParams_Guild
    {
        public string characterID = "";
        public string characterName = "";
        public string kickedCharacterID = "";
        public string kickedCharacterName = "";

        internal CustomParams_Guild(Player actingPlayer, string kickedID, string kickedName)
        {
            if (actingPlayer != null && actingPlayer.m_activeCharacter != null)
            {
                characterID = actingPlayer.m_activeCharacter.m_character_id.ToString();
                characterName = actingPlayer.m_activeCharacter.Name;
            }
            kickedCharacterID = kickedID;
            kickedCharacterName = kickedName;
        }
    }

    public enum action
    {
        JOINED, LEFT, FOUNDED
    };

    /*
            Variable	Enumeration/Format	Description
            action	    JOINED
                        LEFT
                        FOUNDED	            This should describe which action involving the guild was taken.


             
            Example :
            {
                "eventName": "guild",
                "userID": " GA_098987AY127FAFS1192",
                "sessionID": "f32f4a04-e75b-42f0-a2a9-18c0805f09b1",
                "eventParams": {
                    "action": " JOINED",
                    "guildName": "Mercia"
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
