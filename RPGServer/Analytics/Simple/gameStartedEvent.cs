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
    public class gameStartedEvent
    {
 
        //Params for gameStarted log
        public string eventName = "gameStarted";
        public string userID = "";
        public string sessionID = "";
        public string eventTimestamp = DateTime.Now.ToString();
        public eventParams_gameStart eventParams = new eventParams_gameStart();
        public customParams_gameStart customParams = new customParams_gameStart();
        public GoalCounts goalCounts = new GoalCounts();

        public gameStartedEvent()
        {}

        public gameStartedEvent(string i_userID, string i_sessionID, string i_eventTimestamp)
        {
            userID = i_userID;
            sessionID = i_sessionID;
            eventTimestamp = i_eventTimestamp;
        }

    }
    public class eventParams_gameStart
    {
        public string clientVersion;
        public string dataVersion;
        public string serverVersion;

        public eventParams_gameStart()
        {
            clientVersion = "";
            dataVersion = "";
            serverVersion = "";
        }

        public eventParams_gameStart(string i_clientVer, string i_dataVer, string i_serverVer)
        {
            clientVersion = i_clientVer;
            dataVersion = i_dataVer;
            serverVersion = i_serverVer;
        }
    }

    public class customParams_gameStart
    {
        public string serverName;
        public string characterName;
        public string characterID;

        public customParams_gameStart()
        {
            serverName = ""; 
            characterName = "";
            characterID = "";
        }

        public customParams_gameStart(string i_serverName, string i_charName, string i_charID)
        {
            serverName = i_serverName; 
            characterName = i_charName;
            characterID = i_charID;
        }
    }
}
