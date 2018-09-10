using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Analytics.Global;
/*
 * Not using the m_"obj" convention because the serialisation of the string
   relies on the object name as well as its value to serialise the string
 */
namespace Analytics.Simple
{
    public class characterCreatedEvent
    {

        //Params for characterCreated log
        public string eventName = "characterCreated";
        public string userID = "";
        public string sessionID = "";
        public string eventTimestamp = DateTime.Now.ToString();
        public eventParams_charCreated eventParams = new eventParams_charCreated();
        public GoalCounts goalCounts = new GoalCounts();

        public characterCreatedEvent()
        {}

        public characterCreatedEvent(string i_userID, string i_sessionID, string i_eventTimestamp)
        {
             userID = i_userID;
             sessionID = i_sessionID;
             eventTimestamp = i_eventTimestamp;
        }
    }

    public class eventParams_charCreated
    {
        public string serverName;
        public string characterName;
        public string characterID;
        public string characterClass;
        public string characterGender;

        public eventParams_charCreated()
        {
            serverName = "";
            characterName = "";
            characterID = "";
            characterClass = "";
            characterGender = "";
        }

        public eventParams_charCreated(string i_serverName, string i_charName, string i_charID, string i_charClass, string i_charGender)
        {
            serverName = i_serverName;
            characterName = i_charName;
            characterID = i_charID;
            characterClass = i_charClass;
            characterGender = i_charGender;
        }
    }
/*
    public class CharacterGender
    {
        public string MALE = "MALE";
        public string FEMALE = "FEMALE";
    }
*/
}
