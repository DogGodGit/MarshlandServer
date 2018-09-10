using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analytics.Global;
using MainServer;


namespace Analytics.Gameplay
{
    public enum CraftingType // add new crafting types here
    {
        NULL,
        Cooking
    }

    public enum CraftingOutcome
    {
        NULL,
        Failure,
        Success,
        Critical,
        Master
    }

    public class CraftingEvent
    {
        //Members set to public so they will be serialised by JSON.net
        public string eventName = "emoteUsed";
        public string userID;
        public string sessionID;
        public string eventTimestamp = DateTime.Now.ToString();
        public EventParams_CraftingEvent eventParams;
        public GoalCounts goalCounts = new GoalCounts();

        public CraftingEvent(long i_userID, UInt32 i_sessionID, string i_eventTimestamp)
        {
            userID = i_userID.ToString();
            sessionID = i_sessionID.ToString();
            eventTimestamp = i_eventTimestamp;
        }
    }

    public class EventParams_CraftingEvent : BaseEventParams
    {
        public CraftingType craftingType;
        public string productID;
        public int productNumber;
        public CraftingOutcome outcome;

        internal void SetValues(CraftingType i_craftingType, string i_productName, int i_productNumber, CraftingOutcome i_outcome, Player i_player, string i_worldID)
        {

            SetBaseValues(i_player, i_worldID);
            craftingType = i_craftingType;
            productID = i_productName;
            productNumber = i_productNumber;
            outcome = i_outcome;
        }
    }
}
