#region Includes

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading.Tasks;

#endregion

namespace MainServer.AuctionHouse
{
    #region Description

    // Class which manages the out bidding history of the auction house                                               //
    // Whenever a character is out bidded on a listing - that characters server id and the listing id are stored here //
    // If that character once again becomes the highest bidder - that out bid is deleted                              //
    // Upon the completion/expiry of a listing, any out bid references to that listing are deleted                    //
    // The idea is to hold a list of listings IDs that are active, and that a chraracter has been out bid on          //
    // Built around using a new SQL table <auction_house_out_bids>, <listing_ID, character_ID>                        //

    #endregion

    public class AHOutBidManager
    {
        #region Variables

        // OutBid History Dictionary //
        private Dictionary<int, List<int>> m_outBidHistory;

        // AH Database Manager Reference //
        private AHDatabaseManager m_dataBaseManager;

        // Sql Strings //
        private const string LISTING_ID   = "listing_ID";
        private const string CHARACTER_ID = "character_ID";

        #endregion

        #region Start & Initialization

        // AuctionHouseBids                                                                    //
        // Creates the Out Bid History dictionary and populates it from the SQL database table //
        internal AHOutBidManager(AHDatabaseManager dataBaseManager)
        {
            m_outBidHistory   = new Dictionary<int, List<int>>();
            m_dataBaseManager = dataBaseManager;

            Timer updateTimer = new Timer();
            updateTimer.Start();

            SqlQuery query = m_dataBaseManager.GetAHOutBids();

            while (query.Read())
            {
                int listingID   = query.GetInt32(LISTING_ID);
                int characterID = query.GetInt32(CHARACTER_ID);

                if (m_outBidHistory.ContainsKey(characterID))
                {
                    if (m_outBidHistory[characterID].Contains(listingID) == false)
                    {
                        m_outBidHistory[characterID].Add(listingID);
                    }
                }
                else
                {
                    List<int> newOutBidList = new List<int>() { listingID };
                    m_outBidHistory.Add(characterID, newOutBidList);
                }
            }

            query.Close();
            updateTimer.Stop();
            Program.Display(String.Format("AUCTION HOUSE - Population Complete! // OutBids Loaded: {0} // Populate OutBids Time: {1} milliseconds", m_outBidHistory.Count, updateTimer.Interval));
        }

        #endregion

        #region Public Functions

        // AddOutBid                                                               //
        // Adds the passed listingID to the characters list of outbids             //
        // If the character isnt in the dictionary - a new key is created for them //
        internal void AddOutBid(int characterServerID, int listingID)
        {
            if (m_outBidHistory.ContainsKey(characterServerID))
            {
                if (m_outBidHistory[characterServerID].Contains(listingID) == false)
                {
                    MainServer.SQLSuccessDelegate successDelegate = delegate()
                    {
                        m_outBidHistory[characterServerID].Add(listingID);
                    };

                    MainServer.SQLFailureDelegate failureDelegate = delegate()
                    {
                        Program.Display(String.Format("AuctionHouseBids.cs - AddOutBid() SQLFailureDelegate() Failed to add out bid on listing id:{0} to existing key for character id: {1}!", listingID, characterServerID));
                    };

                    m_dataBaseManager.InsertNewOutBid(listingID, characterServerID, successDelegate, failureDelegate);
                }
            }
            else
            {
                List<int> newOutBidList = new List<int>() { listingID };

                MainServer.SQLSuccessDelegate successDelegate = delegate()
                {
                    m_outBidHistory.Add(characterServerID, newOutBidList);
                };

                MainServer.SQLFailureDelegate failureDelegate = delegate()
                {
                    Program.Display(String.Format("AuctionHouseBids.cs - AddOutBid() SQLFailureDelegate() Failed to add out bid on listing id: {0} as a new key for character id: {1}!", listingID, characterServerID));
                };

                m_dataBaseManager.InsertNewOutBid(listingID, characterServerID, successDelegate, failureDelegate);
            }
        }

        // GetOutBidListingIDs                                   //
        // Returns a list of listingIDs for the passed character //
        internal List<int> GetOutBidListingIDs(int characterServerID)
        {
            List<int> outBidListingIDs = null;

            m_outBidHistory.TryGetValue(characterServerID, out outBidListingIDs);
            
            return outBidListingIDs;
        }

        // RemoveListingFromCharacterOutBids                                   //
        // Removes the passed listingID from a specific characters bid history //
        internal void RemoveListingFromCharacterOutBids(int characterServerID, int listingID)
        {
            if (m_outBidHistory.ContainsKey(characterServerID))
            {
                if (m_outBidHistory[characterServerID].Contains(listingID))
                {
                    MainServer.SQLSuccessDelegate successDelegate = delegate()
                    {
                        m_outBidHistory[characterServerID].Remove(listingID);
                    };

                    MainServer.SQLFailureDelegate failureDelegate = delegate()
                    {
                        Program.Display(String.Format("AuctionHouseBids.cs - RemoveListingFromCharacterOutBids() SQLFailureDelegate() Failed to remove out bid on listing id: {0} from character id: {1}!", listingID, characterServerID));
                    };

                    m_dataBaseManager.DeleteCharacterOutBid(listingID, characterServerID, successDelegate, failureDelegate);
                }
            }
        }

        // RemoveListingFromOutBids                                                    //
        // Removes the passed listingID from all of the listings within the dictionary //
        internal void RemoveListingFromOutBids(int listingID)
        {
            bool outBidsRemoved = false;
            foreach(KeyValuePair<int, List<int>> outBid in m_outBidHistory)
            {
                if (outBid.Value.Contains(listingID))
                {
                    outBidsRemoved = true;
                }
            }

            if (outBidsRemoved)
            {
                MainServer.SQLSuccessDelegate successDelegate = delegate()
                {
                    foreach (KeyValuePair<int, List<int>> outBid in m_outBidHistory)
                    {
                        outBid.Value.Remove(listingID);
                    }
                };

                MainServer.SQLFailureDelegate failureDelegate = delegate()
                {
                    Program.Display(String.Format("AuctionHouseBids.cs - RemoveListingFromOutBids() SQLFailureDelegate() Failed to remove out bids for listing id: {0}!", listingID));
                };

                m_dataBaseManager.DeleteAllListingsOutBids(listingID, successDelegate, failureDelegate);
            }
        }

        #endregion
    }
}