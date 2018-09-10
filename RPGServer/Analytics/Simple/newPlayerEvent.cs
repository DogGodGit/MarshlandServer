using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/*
 * Not using the m_"obj" convention because the serialisation of the string
   relies on the object name as well as its value to serialise the string
 */
namespace Analytics.Simple
{
    public class newPlayerEvent
    {
        //Params for newPlayer log
        public string eventName = "newPlayer";
        public string userID = "";
        public string sessionID = "";
        public string eventTimestamp = DateTime.Now.ToString();
        public eventParams_newPlayer eventParams = new eventParams_newPlayer("");
        public customParams_newPlayer customParams = new customParams_newPlayer("");

        public newPlayerEvent()
        {
            eventParams.platform = "iOS";
            customParams.accountName = "";
        }

        public newPlayerEvent(string i_userID, string i_sessionID, string i_eventTimestamp, string i_accName)
        {
            userID = i_userID;
            sessionID = i_sessionID;
            eventTimestamp = i_eventTimestamp;
            eventParams.platform = "iOS";
            customParams.accountName = i_accName;
        }

    }

    public class eventParams_newPlayer
    {
        public string platform;

        public eventParams_newPlayer()
        { platform = ""; }

        public eventParams_newPlayer(string i_platform)
        {
            platform = i_platform;
        }
    }

    public class customParams_newPlayer
    {
        public string accountName;

        public customParams_newPlayer()
        { accountName = ""; }

        public customParams_newPlayer(string i_accountName)
        {
            accountName = i_accountName;
        }
    }
    /*
    //added all accepted formats even though we'll only currently be using iOS
    public class Platform 
    {
        public string IOS = "IOS";
        public string ANDROID = "ANDROID";
        public string WINDOWS_MOBILE = "WINDOWS_MOBILE";
        public string RIM = "RIM";
        public string FACEBOOK = "FACEBOOK";
        public string WEB = "WEB";
        public string PC_CLIENT = "PC_CLIENT";
    }
    */
}
