using System;
using Analytics.Global;
using MainServer;

namespace Analytics.Gameplay
{
    public class EmoteUsed
    {
        //Members set to public so they will be serialised by JSON.net
        public string eventName = "emoteUsed";
        public string userID;        
        public string sessionID;
        public string eventTimestamp  = DateTime.Now.ToString();
        public EventParams_EmoteUsed eventParams;
        public GoalCounts goalCounts = new GoalCounts();

        public EmoteUsed(long i_userID, UInt32 i_sessionID, string i_eventTimestamp)
        {
            userID = i_userID.ToString();
            sessionID = i_sessionID.ToString();
            eventTimestamp = i_eventTimestamp;
        }
    }

    public class EventParams_EmoteUsed : BaseEventParams
    {
        public string emoteName;

        internal void SetValues(string emoteID, Player i_player, string i_worldID)
        {
            
            SetBaseValues(i_player, i_worldID);
            emoteName = emoteID;
        }
    }
}
