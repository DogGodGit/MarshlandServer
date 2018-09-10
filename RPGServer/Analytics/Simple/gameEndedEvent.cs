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
    public class gameEndedEvent
    {
 
        //Params for gameEnded log
        public string eventName = "gameEnded";
        public string userID = "";
        public string sessionID = "";
        public string eventTimestamp = DateTime.Now.ToString();
        public CustomParams_GameEnded customParams = new CustomParams_GameEnded();
        public GoalCounts goalCounts = new GoalCounts();

         public gameEndedEvent()
         {}

         public gameEndedEvent(string i_userID, string i_sessionID, string i_eventTimestamp)
         {
             userID = i_userID;
             sessionID = i_sessionID;
             eventTimestamp = i_eventTimestamp;
         }

    }
    public class CustomParams_GameEnded
    {
        public string reasonForEnd = "";
        internal CustomParams_GameEnded()
        {
           
        }
        ~CustomParams_GameEnded()
        {

        }
    }
    /*
    public class customParams_gameEnd
    {

    }
    */
}
