#region Includes

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#endregion

namespace MainServer.AuctionHouse
{
    #region Auction House Parameters

    // Static container class for the Auction Houses configurable parameters //
    // Pulls the servers 'last_heard_from' date time                         //
    // Params pulled from 'auction_house_params' table in the data db        //

    #endregion

    static class AuctionHouseParams
    {
        #region Properties

        public static DateTime ServerShutdown     { get; private set; }
        public static int      ShortDuration      { get; private set; }
        public static int      MediumDuration     { get; private set; }
        public static int      LongDuration       { get; private set; }
        public static float    ShortMultiplier    { get; private set; }
        public static float    MediumMultiplier   { get; private set; }
        public static float    LongMultiplier     { get; private set; }
        public static int      MinimumDeposit     { get; private set; }
        public static int      NumOfFreeListings  { get; private set; }
        public static int      MaxNumOfListings   { get; private set; }
        public static float    MinimumBidIncrease { get; private set; }
        public static float    SalesTax           { get; private set; }

        #endregion

        #region Variables

        private const string SELECT_SERVER_LAST_HEARD_FROM = "select last_heard_from from worlds where world_id = {0}";
        private const string SELECT_AUCTION_HOUSE_PARAMS   = "select * from auction_house_params";

        private const string LAST_HEARD_FROM = "last_heard_from";
        private const string PARAM_NAME      = "param_name";
        private const string PARAM_VALUE     = "param_value";

        private const string SHORT_DURATION       = "short_duration";
        private const string MEDIUM_DURATION      = "medium_duration";
        private const string LONG_DURATION        = "long_duration";
        private const string SHORT_MULTIPLIER     = "short_multiplier";
        private const string MEDIUM_MULTIPLIER    = "medium_multiplier";
        private const string LONG_MULTIPLIER      = "long_multiplier";
        private const string MINIMUM_DEPOSIT      = "minimum_deposit";
        private const string NUM_OF_FREE_LISTINGS = "number_of_free_listings";
        private const string MAX_NUM_OF_LISTINGS  = "max_number_of_listings";
        private const string MINIMUM_BID_INCREASE = "minimum_bid_increase";
        private const string SALES_TAX            = "sales_tax";

        #endregion

        public static void Setup(Database m_db)
        {
            if (Program.MainForm.AuctionHouseStatus != -1)
            {
                Program.m_auctionHouseActive = Program.MainForm.AuctionHouseStatus;
            }
            else
            {
                Program.MainForm.AuctionHouseStatus = Program.m_auctionHouseActive;
            }

            SqlQuery query = new SqlQuery(m_db, String.Format(SELECT_SERVER_LAST_HEARD_FROM, Program.m_worldID));
            if (query.HasRows)
            {
                query.Read();
                ServerShutdown = query.GetDateTime(LAST_HEARD_FROM);
                Program.MainForm.AHResetTime    = ServerShutdown;
                Program.MainForm.AHResetTimeSet = true;
                query.Close();
            }
            else
            {
                ServerShutdown = DateTime.Now;
            }

            query = new SqlQuery(m_db, SELECT_AUCTION_HOUSE_PARAMS);
            while (query.Read())
            {
                switch(query.GetString(PARAM_NAME))
                {
                    case SHORT_DURATION:
                    {
                        ShortDuration = int.Parse(query.GetString(PARAM_VALUE));
                        break;
                    }
                    case MEDIUM_DURATION:
                    {
                        MediumDuration = int.Parse(query.GetString(PARAM_VALUE));
                        break;
                    }
                    case LONG_DURATION:
                    {
                        LongDuration = int.Parse(query.GetString(PARAM_VALUE));
                        break;
                    }
                    case SHORT_MULTIPLIER:
                    {
                        ShortMultiplier = float.Parse(query.GetString(PARAM_VALUE));
                        break;
                    }
                    case MEDIUM_MULTIPLIER:
                    {
                        MediumMultiplier = float.Parse(query.GetString(PARAM_VALUE));
                        break;
                    }
                    case LONG_MULTIPLIER:
                    {
                        LongMultiplier = float.Parse(query.GetString(PARAM_VALUE));
                        break;
                    }
                    case MINIMUM_DEPOSIT:
                    {
                        MinimumDeposit = int.Parse(query.GetString(PARAM_VALUE));
                        break;
                    }
                    case NUM_OF_FREE_LISTINGS:
                    {
                        NumOfFreeListings = int.Parse(query.GetString(PARAM_VALUE));
                        break;
                    }
                    case MAX_NUM_OF_LISTINGS:
                    {
                        MaxNumOfListings = int.Parse(query.GetString(PARAM_VALUE));
                        break;
                    }
                    case MINIMUM_BID_INCREASE:
                    {
                        MinimumBidIncrease = float.Parse(query.GetString(PARAM_VALUE));
                        break;
                    }
                    case SALES_TAX:
                    {
                        SalesTax = float.Parse(query.GetString(PARAM_VALUE));
                        break;
                    }
                    default:
                    {
                        break;
                    }
                }
            }

            query.Close();
        }
    }
}
