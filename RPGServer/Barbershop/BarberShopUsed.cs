using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analytics.Global;
using MainServer;

namespace Analytics.Gameplay
{
    internal class BarberShopUsed
    {
        public string eventName      = "barberShopUsed";
        public string userID         = String.Empty;
        public string sessionID      = String.Empty;
        public string eventTimestamp = DateTime.Now.ToString();

        public EventParams_BarberShopUsed eventParams;

        public BarberShopUsed()
        {

        }

        public BarberShopUsed(string i_userID, string i_sessionID, string i_eventTimestamp)
        {
            userID         = i_userID;
            sessionID      = i_sessionID;
            eventTimestamp = i_eventTimestamp;
        }
    }

    internal class EventParams_BarberShopUsed : BaseEventParams
    {
        public int virtualCurrencyAmount;
        public int faceId;
        public int skinColourId;
        public int hairId;
        public int hairColourId;
        public int faceAccessoryId;
        public int faceAccessoryColourId;
        //public string characterGender;

        public EventParams_BarberShopUsed() //initialise to default values
        {
         
        }

        public EventParams_BarberShopUsed(int i_cost, int i_faceId, int i_skinColourId, int i_hairId, int i_hairColourId, int i_faceAccessoryId, int i_faceAccessoryColourId, /*string i_characterGender,*/ Player i_player, string i_worldID)
        {
            SetBaseValues(i_player, i_worldID);

            virtualCurrencyAmount = i_cost;
            faceId                = i_faceId;
            skinColourId          = i_skinColourId;
            hairId                = i_hairId;
            hairColourId          = i_hairColourId;
            faceAccessoryId       = i_faceAccessoryId;
            faceAccessoryColourId = i_faceAccessoryColourId;
            //characterGender       = i_characterGender;
        }
    }
}