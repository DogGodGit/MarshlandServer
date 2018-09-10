#region Includes

// Includes //
using System;
using System.Configuration;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using MainServer.AuctionHouse.Enums;

#endregion

namespace MainServer.AuctionHouse
{
    #region Auction House Database Manager Description

    // Contains and handles all the Auction House functionality that deals directly with the SQL Database                      //
    // All of the main Auction House SQL calls take optional success and failure delegates - to be called once the SQL returns //

    #endregion

    internal class AHDatabaseManager
    {
        #region Variables
        
        // String Builder //
        private StringBuilder STRING_BUILDER = new StringBuilder();

        // Auction House Sql Strings //
        private const string SELECT_ACTIVE_LISTINGS      = "select * from auction_house_listings where completed = 0";
        private const string SELECT_MAX_LISTING_ID       = "select max(listing_id) as maxListingID from auction_house_listings";
        private const string SELECT_BLANK_LISTING        = @"select listing_id from auction_house_listings where listing_id = {0} 
                                                             and item_template_ID = -1 and item_quantity = -1 and expiry_date_time = '2016-11-18 14:07:00' and duration = -1 and seller_ID = -1 and seller_name = 'Auction House'
                                                             and starting_bid = -1 and current_bid = -1 and highest_bidder_ID = -1 and highest_bidder_name = 'Dummy Listing' and buy_out = -1 and completed = -1";
        private const string SELECT_ACC_ID_FOR_CHAR_ID   = "select account_id from character_details where character_id = {0}";
        private const string UPDATE_LISTING_COMPLETE     = "update auction_house_listings set completed = 1, completion_time = '{0}' where listing_ID = {1}";
        private const string UPDATE_LISTING_NOT_COMPLETE = "update auction_house_listings set completed = 0, completion_time = NULL where listing_ID = {0}";
        private const string UPDATE_BID_DETAILS          = "update auction_house_listings set current_bid = {0}, highest_bidder_ID = {1}, highest_bidder_name = '{2}' where listing_ID = {3}";
        private const string UPDATE_LISTING_COLUMNS      = @"update auction_house_listings set item_template_ID = {1}, item_quantity = {2}, expiry_date_time = '{3}', duration = {4}, seller_ID = {5}, seller_name = '{6}',
                                                            starting_bid = {7}, current_bid = {8}, highest_bidder_ID = {9}, highest_bidder_name = '{10}', buy_out = {11}, completed = {12} where listing_ID = {0}";
        private const string UPDATE_LISTING_EXPIRY       = "update auction_house_listings set expiry_date_time = '{0}' where listing_ID = {1}";
        private const string INSERT_BLANK_LISTING        = @"insert into auction_house_listings 
                                                             (item_template_ID, item_quantity, expiry_date_time, duration, seller_ID, seller_name, starting_bid, current_bid, highest_bidder_ID, highest_bidder_name, buy_out, completed) 
                                                             values (-1, -1, '2016-11-18 14:07:00', -1, -1, 'Auction House', -1, -1, -1, 'Dummy Listing', -1, -1)";
        private const string INSERT_ITEM_TO_MAIL         = "insert into mail_inventory (mail_id, {0}) values ({1}, {2})";
        private const string INSERT_AH_TRANSACTION       = "insert into auction_house_history (listing_ID, event_date_time, character_ID, item_template_ID, item_quantity, gold, transaction_type) values ({0}, '{1}', {2}, {3}, {4}, {5}, '{6}')";
        //private const string DELETE_OLD_LISTINGS         = "delete from auction_house_listings where completion_time < NOW() - INTERVAL {0} DAY and completed = 1";
                                                        
        private const string SELECT_OUT_BIDS             = "select * from auction_house_out_bids";
        private const string INSERT_OUT_BID              = "insert into auction_house_out_bids (listing_ID, character_ID) values ({0}, {1})";
        private const string DELETE_LISTING_OUT_BID      = "delete from auction_house_out_bids where listing_ID = {0}";
        private const string DELETE_CHARACTER_OUT_BID    = "delete from auction_house_out_bids where listing_ID = {0} and character_ID = {1}";

        // Object Lock //
        private Object m_objLock = new Object();

        // Get ListingID Delegate //
        public delegate void OnListingIDSuccess(int listingID); 

        #endregion

        #region SQL Queries

        // GetAvailableListingID                                                                               //
        // To allow the database to generate the UUID 'listing_ID' that column is set as AUTO_INCREMENT        //
        // Inserts a 'dummy' row into auction_house_listings and then requests the max listing_ID of the table //
        // If returned and correctly 'blank' this is now returned as a new UUID to be filled in                //
        internal void GetAvailableListingID(OnListingIDSuccess onListingSuccess)
        {
            int  newID = -1;
            bool found = false;

            lock (m_objLock)
            {
                while (!found)
                {
                    if (newID == -1)
                    {
                        Program.processor.m_worldDB.runCommand(INSERT_BLANK_LISTING);
                        SqlQuery query = new SqlQuery(Program.processor.m_worldDB, SELECT_MAX_LISTING_ID);
                        if (query.Read())
                        {
                            newID = query.GetInt32("maxListingID");
                            if (Database.debug_database)
                            {
                                Program.DisplayDelayed("AuctionHouseDatabase.cs - GetAvailableListingID() created new listing id: " + newID);
                            }
                        }

                        query.Close();
                        found = true;
                    }

                    if (newID != -1)
                    {
                        string sql = String.Format(SELECT_BLANK_LISTING, newID);
                        SqlQuery query = new SqlQuery(Program.processor.m_worldDB, sql);
                        found = query.Read();
                        if (!found)
                        {
                            Program.Display(String.Format("AuctionHouseDatabase.cs - GetAvailableListingID() listing id: {0} is already been used!", newID));
                        }

                        query.Close();
                    }
                }
            }

            onListingSuccess(newID);
        }

        // GetActiveAHListings                                     //
        // Returns all the listings which have yet to be completed //
        internal SqlQuery GetActiveAHListings()
        {
            return Program.processor.getAuctionHouseQuery(SELECT_ACTIVE_LISTINGS);
        }

        // GetAHOutBids                                   //
        // Returns all the outbids which are still active //
        internal SqlQuery GetAHOutBids()
        {
            return Program.processor.getAuctionHouseQuery(SELECT_OUT_BIDS);
        }

        // GetAccountID                                      //
        // Returns the account id of the passed character id //
        private int GetAccountID(int characterID)
        {
            int accountID = -1;
            SqlQuery query = Program.processor.getAuctionHouseQuery(String.Format(SELECT_ACC_ID_FOR_CHAR_ID, characterID));

            if (query.HasRows)
            {
                query.Read();
                accountID = query.GetInt32("account_id");
                query.Close();
            }

            return accountID;
        }

        #endregion

        #region SQL Modifications (Bid History)

        // InsertNewOutBid                                                                                             //
        // Adds a new row to the auction house bid history table - representing a character being out bid on a listing //
        public void InsertNewOutBid(int listingID, int characterID, MainServer.SQLSuccessDelegate successDelegate, MainServer.SQLFailureDelegate failureDelegate)
        {
            Program.processor.auctionHouseSql(String.Format(INSERT_OUT_BID, listingID, characterID), successDelegate, failureDelegate);
        }

        // DeleteAllListingsOutBids                                                                                              //
        // Sets all the found out bids for the passed listing as no longer active - the listing has been bought out or completed //
        public void DeleteAllListingsOutBids(int listingID, MainServer.SQLSuccessDelegate successDelegate, MainServer.SQLFailureDelegate failureDelegate)
        {
            Program.processor.auctionHouseSql(String.Format(DELETE_LISTING_OUT_BID, listingID), successDelegate, failureDelegate);
        }

        // DeleteCharacteroutBid                                                                                                                              //
        // Sets a specific out bid as no longer active, identified by both the listing and character ID's - the character has become the highest bidder again //
        public void DeleteCharacterOutBid(int listingID, int characterID, MainServer.SQLSuccessDelegate successDelegate, MainServer.SQLFailureDelegate failureDelegate)
        {
            Program.processor.auctionHouseSql(String.Format(DELETE_CHARACTER_OUT_BID, listingID, characterID), successDelegate, failureDelegate);
        }

        #endregion

        #region SQL Modifications (Transaction History)

        // LogAHTransaction                                                                                   //
        // Inserts a new row into the auction_house_history table representing the passed transaction details //
        // An item has been received / taken if the item is NOT null                                          //
        // If the type is LISTING_CREATED the passed item was taken - therefore the quantity is negative      //
        // Also creates a suitable auction house event for analytics                                          //
        internal void LogAHTransaction(int listingID, int characterID, int accountID, Item item, int gold, AHServerMessageType messageType)
        {
            int      itemTemplateID = -1;
            int      itemQuantity   = 0;
            DateTime eventTime      = DateTime.Now;

            if (item != null)
            {
                itemTemplateID = item.m_template_id;
                itemQuantity   = item.m_quantity;

                if (messageType == AHServerMessageType.LISTING_CREATED)
                {
                    itemQuantity *= -1;
                }
            }

            Program.processor.auctionHouseSql(String.Format(INSERT_AH_TRANSACTION, listingID,
                                                                                   eventTime.ToString("yyyy-MM-dd HH:mm:ss"),
                                                                                   characterID,
                                                                                   itemTemplateID,
                                                                                   itemQuantity,
                                                                                   gold,
                                                                                   messageType.ToString()));

            if (Program.m_LogAnalytics)
            {
                try
                {
                    if (accountID == -1)
                    {
                        accountID = GetAccountID(characterID);
                    }

                    AnalyticsMain logAnalytics = new AnalyticsMain(false);
                    logAnalytics.auctionHouseEvent(listingID, accountID, item, gold, messageType, eventTime);
                }
                catch
                {
                    Program.Display("AuctionHouseDatabase.cs - LogAHTransaction() failed to create the auction house event!");
                }
            }
        }

        // LogAHError                                                                                             //
        // Logs a transaction where a listing failed to complete:                                                 //
        // - cancellation/buyout/completed/expired                                                                //
        // In these cases the item and any bids that are lost are logged                                          //
        // Listing creation/bids and buyouts are not logged as their failure means the item(s) / gold arent taken //
        public void LogAHError(AHListing listing, int sellerGoldReturned, AHServerMessageType messageType)
        {
            DateTime eventTime = DateTime.Now;

            // Log error transaction for the seller //
            Program.processor.auctionHouseSql(String.Format(INSERT_AH_TRANSACTION, listing.ListingID,
                                                                                   eventTime.ToString("yyyy-MM-dd HH:mm:ss"),
                                                                                   listing.SellerID,
                                                                                   listing.ItemTemplateID,
                                                                                   listing.ItemQuantity,
                                                                                   sellerGoldReturned,
                                                                                   String.Format("ERROR_{0}_SELLER", messageType.ToString())));

            // Log error transaction for the bidder //
            if (messageType != AHServerMessageType.LISTING_EXPIRED && listing.CurrentBid != -1 && listing.HighestBidderID != -1)
            {
                Program.processor.auctionHouseSql(String.Format(INSERT_AH_TRANSACTION, listing.ListingID,
                                                                                       eventTime.ToString("yyyy-MM-dd HH:mm:ss"),
                                                                                       listing.HighestBidderID,
                                                                                       -1,
                                                                                       0,
                                                                                       listing.CurrentBid,
                                                                                       String.Format("ERROR_{0}_BIDDER", messageType.ToString())));
            }
        }

        #endregion

        #region SQL Modifications (Listings)

        // UpdateListingExpiry                          //
        // Updates the passed listings expiry date time //
        public void UpdateListingExpiry(DateTime newExpiry, int listingID)
        {
            Program.processor.auctionHouseSql(String.Format(UPDATE_LISTING_EXPIRY, newExpiry.ToString("yyyy-MM-dd HH:mm:ss"), listingID));
        }

        // CreateAHListing                                                                 //
        // Takes in an AHListing object, builds a correctly formatted SQL insert statement //
        // Both "current_bid" and "highest_bidder_ID" are set to -1 (no current values)    //
        // The value of the buy out is either set or defaults to -1 (no buy out)           //
        // Finally "compeleted" flag is set to 0 (false)                                   //
        internal void CreateAHListing(AHListing newListing, MainServer.SQLSuccessDelegate successDelegate, MainServer.SQLFailureDelegate failureDelegate)
        {
            STRING_BUILDER.Length = 0;
            STRING_BUILDER.AppendFormat(UPDATE_LISTING_COLUMNS, newListing.ListingID,                                      // listing_ID
                                                                newListing.ItemTemplateID,                                 // item_template_ID
                                                                newListing.ItemQuantity,                                   // item_quantity
                                                                newListing.ExpiryDateTime.ToString("yyyy-MM-dd HH:mm:ss"), // expiry_date_time
                                                                (int)newListing.Duration,                                  // duration
                                                                newListing.SellerID,                                       // seller_ID
                                                                newListing.SellerName,                                     // seller_name
                                                                newListing.StartingBid,                                    // starting_bid
                                                                -1,                                                        // current_bid 
                                                                -1,                                                        // highest_bidder_ID
                                                                newListing.HighestBidderName,                              // highest_bidder_name
                                                                newListing.BuyOut != -1 ? newListing.BuyOut : -1,          // buy_out 
                                                                0);                                                        // completed

            Program.processor.auctionHouseSql(STRING_BUILDER.ToString(), successDelegate, failureDelegate);
        }

        // UpdateListingAsComplete                  //
        // Updates the passed listingID as complete //
        internal void UpdateListingAsComplete(int listingID, MainServer.SQLSuccessDelegate successDelegate = null, MainServer.SQLFailureDelegate failureDelegate = null)
        {
            Program.processor.auctionHouseSql(String.Format(UPDATE_LISTING_COMPLETE, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), listingID), successDelegate, failureDelegate);
        }

        // UpdateListingAsNotComplete                   //
        // Updates the passed listingID as not complete //
        internal void UpdateListingAsNotComplete(int listingID, MainServer.SQLSuccessDelegate successDelegate = null, MainServer.SQLFailureDelegate failureDelegate = null)
        {
            Program.processor.auctionHouseSql(String.Format(UPDATE_LISTING_NOT_COMPLETE, listingID), successDelegate, failureDelegate);
        }

        // UpdateListingBidDetails                                        //
        // Updates the passed listing with a new bid amount and bidder id //
        internal void UpdateListingBidDetails(int newBidAmount, int newBidderID, string newBiddersName, int listingID, MainServer.SQLSuccessDelegate successDelegate = null, MainServer.SQLFailureDelegate failureDelegate = null)
        {
            Program.processor.auctionHouseSql(String.Format(UPDATE_BID_DETAILS, newBidAmount, newBidderID, newBiddersName, listingID), successDelegate, failureDelegate);
        }

        // AddItemToMail                                                                      //
        // Builds a correctly formatted SQL string to add the send item to the passed mail id //
        internal void AddItemToMail(Item itemToBeAdded, int mailID, int characterID)
        {
            Program.processor.m_worldDB.runCommandSync(String.Format(INSERT_ITEM_TO_MAIL, itemToBeAdded.GetInsertFieldsString(), mailID, itemToBeAdded.GetInsertValuesString(characterID)));
        }

        // DeleteOldListings                                                    //
        // Deletes old completed listings from the auction house listings table //
        /*internal void DeleteOldListings(int numOfDays)
        {
            Program.processor.m_worldDB.runCommand(String.Format(DELETE_OLD_LISTINGS, numOfDays));
        }*/

        #endregion
    };
}