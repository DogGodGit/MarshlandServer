#region Includes

using System;
using System.Text;
using System.Timers;
using System.Collections.Generic;
using Lidgren.Network;
using MainServer.AuctionHouse.Enums;
using MainServer.Localise;

#endregion

namespace MainServer.AuctionHouse
{
    #region Auction House Description

    // Class which manages incoming and outgoing data messages centered around an internal list that represents an Auction House //
    // All changes to the list are pushed to an SQL table <auction_house_listings> as the current state of the Auction House     //
    // The Auction House awaits confirmation that the SQL table update has been successful before taking the appropiate action   //

    #endregion

    class AuctionHouseManager
    {
        #region Localisation

        // #localisation
        public class AuctionHouseManagerTextDB : TextEnumDB
		{
			public AuctionHouseManagerTextDB() : base(nameof(AuctionHouseManager), typeof(TextID)) { }

			public enum TextID
			{
				AUCTION_HOUSE_OFFLINE,                  //"The Auction House is currently Offline!"
				AUCTION_HOUSE_SAFE_MODE,                //"The Auction House is currently in Safe Mode - All transactions have been disabled!"
				LISTING_ALREADY_CANCELLED,              //"The listing has already been cancelled!"
				CANCELLED_LISTING_NOT_FOUND,            //"The listing to be cancelled could not be found!"
				CANNOT_CANCEL_LISTING,                  //"You cannot cancel another characters listing!"
				LISTING_OF_ITEM_CANCELLED,              //"The auction of: {itemName0} ({itemQuantity1}) was cancelled!"
				LISTING_SUCCESSFULLY_CANCELLED,         //"Your listing has been successfully cancelled!"
				LISTING_CANCELLATION_FAILED,            //"The listing cancellation has failed!"
				LISTING_JUST_CANCELLED,                 //"The listing was just cancelled!"
				LISTING_JUST_BOUGHT_OUT,                //"The listing was just bought out"
				CANT_BUY_OWN_LISTING,                   //"You cannot buy out your own auction!"
				CANT_AFFORD_BUYOUT,                     //"You do not have enough gold to buy out this auction!"
				LISTING_NOT_FOUND,                      //"The listing could not be found!"
				LISTING_OF_ITEM_BOUGHT_OUT,             //"The auction of: {itemName0} ({itemQuantity1}) was bought out!"
				LISTING_OF_ITEM_COMPLETED,              //"Your auction of: {itemName0} ({itemQuantity1}) has been completed!"
				LISTING_SUCCESSFULLY_BOUGHT_OUT,        //"You have successfully bought out the auction!"
				LISTING_BUYOUT_FAILED,                  //"The listing buy out failed!"
				LISTING_OF_ITEM_OUTBID,                 //"You have been out bid on the auction of: {itemName0} ({itemQuantity1})!"
				LISTING_BID_SUCCESSFUL,                 //"Your bid has been placed!"
				LISTING_BID_FAILED,                     //"The bid attempt failed!"
				LISTING_JUST_BID_ON,                    //"The listing was just bid on, please try again!"
				CANT_BID_ON_OWN_LISTING,                //"You cannot bid on your own listing!"
				CANT_AFFORD_BID,                        //"You do not have enough gold to bid on this auction!"
				LISTING_OF_ITEM_WON,                    //"You have won the auction of: {itemName0} ({itemQuantity1})!"
				LISTING_OF_ITEM,                        //"Your auction of: {itemName0} ({itemQuantity1}) has {listingHasCompleted2}!"
				COMPLETED,                              //"been completed"
				EXPIRED,                                //"expired"
				LISTING_PENDING,                        //"The Auction House is still processing your last new listing, please try again!"
				NO_TRADE,                               //"You cannot list a NO TRADE item on the auction house!"
				CHARGES_USED,                           //"You cannot list an item with used charges!"
				CANT_AFFORD_LISTING,                    //"You do not have enough gold to create the auction!"
				MAX_NUMBER_OF_LISTINGS,                 //"You already have the maximum number of listings on the auction house!"
				NO_FREE_LISTINGS,                       //"You do not have any free slots to create this auction!"
				ITEM_NOT_FOUND,							//"You do not have the required item to create the auction!"
				ITEMS_NOT_FOUND,                        //"You do not have the required items to create the auction!"
				LISTING_SUCCESSFUL,                     //"Your listing has been successfully created! \n{amout0} gold listing fee taken!"
				LISTING_FAILED,                         //"Auction listing creation failed!"
			}
		}
		public static AuctionHouseManagerTextDB textDB = new AuctionHouseManagerTextDB();

        #endregion

        #region Text Strings
        /*
		private const string AUCTION_HOUSE_OFFLINE           = "The Auction House is currently Offline!";
        private const string AUCTION_HOUSE_SAFE_MODE         = "The Auction House is currently in Safe Mode - All transactions have been disabled!";
        private const string LISTING_ALREADY_CANCELLED       = "The listing has already been cancelled!";
        private const string CANCELLED_LISTING_NOT_FOUND     = "The listing to be cancelled could not be found!";
        private const string CANNOT_CANCEL_LISTING           = "You cannot cancel another characters listing!";
        private const string LISTING_OF_ITEM_CANCELLED       = "The auction of: {0} ({1}) was cancelled!";
        private const string LISTING_SUCCESSFULLY_CANCELLED  = "Your listing has been successfully cancelled!";
        private const string LISTING_CANCELLATION_FAILED     = "The listing cancellation has failed!";
        private const string LISTING_JUST_CANCELLED          = "The listing was just cancelled!";
        private const string LISTING_JUST_BOUGHT_OUT         = "The listing was just bought out";
        private const string CANT_BUY_OWN_LISTING            = "You cannot buy out your own auction!";
        private const string CANT_AFFORD_BUYOUT              = "You do not have enough gold to buy out this auction!";
        private const string LISTING_NOT_FOUND               = "The listing could not be found!";
        private const string LISTING_OF_ITEM_BOUGHT_OUT      = "The auction of: {0} ({1}) was bought out!";
        private const string LISTING_OF_ITEM_COMPLETED       = "Your auction of: {0} ({1}) has been completed!";
        private const string LISTING_SUCCESSFULLY_BOUGHT_OUT = "You have successfully bought out the auction!";
        private const string LISTING_BUYOUT_FAILED           = "The listing buy out failed!";
        private const string LISTING_OF_ITEM_OUTBID          = "You have been out bid on the auction of: {0} ({1})!";
        private const string LISTING_BID_SUCCESSFUL          = "Your bid has been placed!";
        private const string LISTING_BID_FAILED              = "The bid attempt failed!";
        private const string LISTING_JUST_BID_ON             = "The listing was just bid on, please try again!";
        //private const string HIGHEST_BIDDER                  = "You are the highest bidder!";
        private const string CANT_BID_ON_OWN_LISTING         = "You cannot bid on your own listing!";
        private const string CANT_AFFORD_BID                 = "You do not have enough gold to bid on this auction!";
        private const string LISTING_OF_ITEM_WON             = "You have won the auction of: {0} ({1})!";
        private const string LISTING_OF_ITEM                 = "Your auction of: {0} ({1}) has {2}!";
        private const string COMPLETED                       = "been completed";
        private const string EXPIRED                         = "expired";
        private const string LISTING_PENDING                 = "The Auction House is still processing your last new listing, please try again!";
        private const string NO_TRADE                        = "You cannot list a NO TRADE item on the auction house!";
        private const string CHARGES_USED                    = "You cannot list an item with used charges!";
        private const string CANT_AFFORD_LISTING             = "You do not have enough gold to create the auction!";
        private const string MAX_NUMBER_OF_LISTINGS          = "You already have the maximum number of listings on the auction house!";
        private const string NO_FREE_LISTINGS                = "You do not have any free slots to create this auction!";
        private const string ITEMS_NOT_FOUND                 = "You do not have the required item{0} to create the auction!";
        private const string LISTING_SUCCESSFUL              = "Your listing has been successfully created! \n{0} gold listing fee taken!";
        private const string LISTING_FAILED                  = "Auction listing creation failed!";
		*/
        #endregion

        #region Variables

        // Auction House Database Manager //
        private AHDatabaseManager m_databaseManager;

        // Auction House Mail Manager //
        private AHMailManager m_mailManager;

        // Auction House OutBid Manager //
        private AHOutBidManager m_outBidManager;

        // Auction House Lists //
        private List<AHCancel>         m_cancellationAttempts;    // listings which are to be cancelled
        private List<int>              m_listingHasBeenCancelled; // listings which have been cancelled this cycle
        private List<AHBuyOut>         m_buyOrderAttempts;        // listings which are to be bought out
        private List<int>              m_listingHasBeenBought;    // listings which have been bought out this cycle
        private List<AHBid>            m_bidAttempts;             // listings which are to be bid on 
        private List<int>              m_listingHasBeenBidOn;     // listings which have received a bid this cycle
        private List<int>              m_expiredListings;         // listings which have passed their expiry date time
        private List<int>              m_pendingListings;         // character ids which have a listings pending creation
        private List<AHListing>        m_newListings;             // new listings to be added 
        private List<AHListingQuery>   m_queries;                 // incoming queries
        private List<AHCharacterQuery> m_characterQueries;        // incoming character queries
        private List<AHListingQuery>   m_bidQueries;              // incoming bid queries

        #region Background Task Variables (REMOVED)

        // Background Task Times //
        //private const int TABLE_CLEAN_IN_DAYS = 30;

        // Background Task Times //
        //private DateTime m_lastTableClean;

        #endregion

        // Auction House Listings //
        private List<AHListing> m_listings;

        // Timer //
        private Timer m_timer;

        // Status //
        public AHStatus Status { get; private set; }

        // Expiry Check Interval //
        private const int TEN_SECONDS = 10000;

        // Update Timer Holder //
        private double m_maxUpdateTime = 0.0;

        #endregion

        #region Constructor & Initialization

        // Auctionhouse                                             //
        // Creates the manager objects lists                        //
        // Sets the initial Auction House status for initialization //
        public AuctionHouseManager(AHStatus status, bool resetDurations, DateTime serverShutdown)
        {
            m_databaseManager = new AHDatabaseManager();
            m_mailManager     = new AHMailManager(m_databaseManager);
            m_outBidManager   = new AHOutBidManager(m_databaseManager);

            m_cancellationAttempts    = new List<AHCancel>();
            m_listingHasBeenCancelled = new List<int>();
            m_buyOrderAttempts        = new List<AHBuyOut>();
            m_listingHasBeenBought    = new List<int>();
            m_bidAttempts             = new List<AHBid>();
            m_listingHasBeenBidOn     = new List<int>();
            m_expiredListings         = new List<int>();
            m_pendingListings         = new List<int>();
            m_newListings             = new List<AHListing>();
            m_queries                 = new List<AHListingQuery>(); 
            m_characterQueries        = new List<AHCharacterQuery>();
            m_bidQueries              = new List<AHListingQuery>();
            m_listings                = new List<AHListing>();

            SetAuctionHouseStatus(status, serverShutdown, resetDurations);
        }

        // SetAuctionHouseActive                                                       //
        // External setter for the auction houses active status                        //
        // Flag and DateTime for resetting the listings expiry times when going online //
        public void SetAuctionHouseStatus(AHStatus status, DateTime serverShutDown, bool resetDurations = false )
        {
            if (Status != status)
            {
                switch (status)
                {
                    case (AHStatus.OFFLINE):
                    {
                        SetExpiryTimer(false);
                        Program.MainForm.SetAuctionOffline(true);
                        break;
                    }
                    case (AHStatus.SAFE_MODE):
                    {
                        SetExpiryTimer(false);
                        PopulateListings(serverShutDown, resetDurations);
                        Program.MainForm.SetAuctionSafe(true);
                        break;
                    }
                    case (AHStatus.ONLINE):
                    {
                        SetExpiryTimer(true);
                        PopulateListings(serverShutDown, resetDurations);
                        CheckForExpiredListings();
                        Program.MainForm.SetAuctionOnline(true);
                        break;
                    }
                }

                Status = status;
            }
        }

        // SetExpiryTimer                                              //
        // Either subscribes or unsubscribes the expiry checking event //
        private void SetExpiryTimer(bool active)
        {
            if (active)
            {
                m_timer            = new Timer(TEN_SECONDS);
                m_timer.Elapsed   += OnTimedEvent;
                m_timer.AutoReset  = true;
                m_timer.Enabled    = true;
            }
            else if (m_timer != null)
            {
                m_timer.Elapsed -= OnTimedEvent;
                m_timer          = null;
            }
        }

        // OnTimedEvent                               //
        // Delegate to call CheckForExpiredListings() //
        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            CheckForExpiredListings();
        }

        // PopulateListings                                                                                                          //
        // Pulls down all of the active listings from the auction_house_listings table and populates the list with AHListing objects //
        // It the creation fails - the listing is not added and the failure is logged                                                //
        // If the resetDurations flag is true, each listings duration is extended by the time the server was down                    //
        private void PopulateListings(DateTime serverShutDown, bool resetDurations)
        {
            TimeSpan downTime = DateTime.Now - serverShutDown;

            m_listings.Clear();
            Timer updateTimer = new Timer();
            updateTimer.Start();

            SqlQuery query = m_databaseManager.GetActiveAHListings();

            if (resetDurations)
            {
                Program.Display(String.Format("AUCTION HOUSE - Extending listing durations by: {0}", downTime.ToString()));
            }

            if (query.HasRows == false)
            {
                updateTimer.Stop();
                Program.Display(String.Format("AUCTION HOUSE - Population Complete! // No listings found! // Populate Listings Time: {0} milliseconds", updateTimer.Interval));
                return;
            }

            while (query.Read())
            {
                AHListing listing = new AHListing(query);

                if (listing.Item == null)
                {
                    Program.Display(String.Format("AuctionHouseManager.cs - PopulateListings() unable to add listing id: {0} - the AHListings Item was null!", listing.ListingID));
                    continue;
                }

                if (resetDurations)
                {
                    listing.ExpiryDateTime += downTime;
                    m_databaseManager.UpdateListingExpiry(listing.ExpiryDateTime, listing.ListingID);
                }

                m_listings.Add(listing);
            }

            query.Close();
            updateTimer.Stop();
            Program.Display(String.Format("AUCTION HOUSE - Population Complete! // Listings Loaded: {0} // Populate Listings Time: {1} milliseconds", m_listings.Count, updateTimer.Interval));
        }

        #endregion

        #region Internal Functions

        // ProcessAuctionHouseMessage                            //
        // Main logic switch for incoming Auction House messages //
        // Sets the player as busy to prevent trades             //
        // Offline  - all messages are returned                  //
        // SafeMode - only queries are permitted                 //
        // Online   - full functionality                         //
        public void ProcessAuctionHouseMessage(NetIncomingMessage msg, Player player)
        {
            if (player != null && player.m_activeCharacter != null)
            {
                player.m_activeCharacter.PlayerIsBusy = true;
            }

            #region Offline

            if (Status == AHStatus.OFFLINE)
            {
				string locText = Localiser.GetString(textDB, player, (int)AuctionHouseManagerTextDB.TextID.AUCTION_HOUSE_OFFLINE);
				SendClientResponse(AHServerMessageType.OFFLINE, player, locText);
				return;
            }

            #endregion

            AHClientMessageType messageType = (AHClientMessageType)msg.ReadVariableInt32();

            #region Safe Mode

            if (Status == AHStatus.SAFE_MODE)
            {
                switch (messageType)
                {
                    case (AHClientMessageType.CANCEL_LISTING):
                    case (AHClientMessageType.CREATE_LISTING):
                    case (AHClientMessageType.PLACE_BID):
                    case (AHClientMessageType.BUY_OUT_LISTING):
                    {
						string locText = Localiser.GetString(textDB, player, (int)AuctionHouseManagerTextDB.TextID.AUCTION_HOUSE_SAFE_MODE);
						SendClientResponse(AHServerMessageType.SAFE_MODE, player, locText);
                        break;
                    }
                    case (AHClientMessageType.QUERY):
                    {
                        ProcessQuery(msg, player);
                        break;
                    }
                    case (AHClientMessageType.GET_MY_LISTINGS):
                    {
                        ProcessCharacterQuery(msg, player);
                        break;
                    }
                    case (AHClientMessageType.GET_MY_BIDS):
                    {
                        ProcessBidQuery(msg, player);
                        break;
                    }
                    default:
                    {
                        Program.Display(String.Format("AuctionHouse.cs - ProcessAuctionHouseMessage() received a message with an unknown AHClientMessageType - ({0})", messageType));
                        break;
                    }
                }
            }

            #endregion

            #region Online

            if (Status == AHStatus.ONLINE)
            {
                switch (messageType)
                {
                    case (AHClientMessageType.CANCEL_LISTING):
                    {
                        CancelListing(msg, player);
                        break;
                    }
                    case (AHClientMessageType.CREATE_LISTING):
                    {
                        CreateListing(msg, player);
                        break;
                    }
                    case (AHClientMessageType.PLACE_BID):
                    {
                        BidOnListing(msg, player);
                        break;
                    }
                    case (AHClientMessageType.BUY_OUT_LISTING):
                    {
                        BuyOutListing(msg, player);
                        break;
                    }
                    case (AHClientMessageType.QUERY):
                    {
                        ProcessQuery(msg, player);
                        break;
                    }
                    case (AHClientMessageType.GET_MY_LISTINGS):
                    {
                        ProcessCharacterQuery(msg, player);
                        break;
                    }
                    case (AHClientMessageType.GET_MY_BIDS):
                    {
                        ProcessBidQuery(msg, player);
                        break;
                    }
                    default:
                    {
                        Program.Display(String.Format("AuctionHouse.cs - ProcessAuctionHouseMessage() received a message with an unknown AHClientMessageType - ({0})", messageType));
                        break;
                    }
                }
            }

            #endregion
        }

        #region CancelAllListings (UNUSED)

        // CancelAllListings                                           //
        // Prototype function to clear/reset the whole Auction House   //
        // Cancels all listings - returns all bids, items and deposits //
        /*private void CancelAllListings()
        {
            foreach (AHListing listing in m_listings)
            {
                // Cancellation Success Delegate //
                MainServer.SQLSuccessDelegate successDelegate = delegate()
                {
                    // Cancelled! - Return bid to highest bidder //
                    if (listing.CurrentBid != -1 && listing.HighestBidderID != -1)
                    {
                        ReturnHighestBid(listing, AHMailMessageType.LISTING_CANCELLED_BIDDER);
                        NotifyCharacterIfOnline(AHServerMessageType.LISTING_CANCELLED_BIDDER, listing.HighestBidderID, String.Format(LISTING_OF_ITEM_CANCELLED, listing.ItemName(), listing.ItemQuantity));
                        m_databaseManager.LogAHTransaction(listing.ListingID, listing.HighestBidderID, null, listing.CurrentBid, AHServerMessageType.LISTING_CANCELLED_BIDDER);
                    }

                    // Cancelled! - Return item and deposit to seller and decrement ah slots used //
                    int listingCost = GetDeposit(listing.StartingBid, listing.Duration);
                    m_mailManager.SendMailToPlayer(listing.SellerID,
                                                   AHMailMessageType.LISTING_CANCELLED_SERVER,
                                                   listing.Item,
                                                   listing.ItemQuantity,
                                                   0,
                                                   listingCost,
                                                   false);
                    NotifyCharacterIfOnline(AHServerMessageType.LISTING_CANCELLED_SERVER, listing.SellerID, String.Format(LISTING_OF_ITEM_CANCELLED, listing.ItemName(), listing.ItemQuantity));
                    m_databaseManager.LogAHTransaction(listing.ListingID, listing.SellerID, listing.Item, 0, AHServerMessageType.LISTING_CANCELLED_SERVER);
                };

                // Cancellation Failure Delegate //
                MainServer.SQLFailureDelegate failureDelegate = delegate()
                {
                    m_databaseManager.LogAHError(listing, GetDeposit(listing.StartingBid, listing.Duration), AHServerMessageType.FAILED_CANCELLATION);
                    Program.Display("AuctionHouseManager.cs - CancelAllListings() a cancellation attempts UpdateListingAsComplete() failed!");
                };

                // Cancel //
                UpdateListingAsComplete(listing.ListingID, successDelegate, failureDelegate);
                m_listings.Remove(listing);
            }
        }*/

        #endregion

        // ReturnHighestBid //
        private void ReturnHighestBid(AHListing listing, AHMailMessageType mailMessageType)
        {
            m_mailManager.SendMailToPlayer(listing.HighestBidderID, mailMessageType, listing.Item, listing.ItemQuantity, listing.CurrentBid, 0, true);
        }

        // Update                                                                                                                   //
        // Main Logic - Cancels > BuyOuts > Bids > Expiries > NewListings > Queries                                                 //
        // BuyOuts and Bids operate on a first-come-first-serve basis for the same listings on the same cycle                       //
        // Cancelled listings first - now any buyouts/bids received will be returned as the listing was already cancelled           //
        // BuyOuts second - any further buy outs or bids received are returned as the listing has been bought out                   //
        // Bids third - duplicated are sent back as the current bid (and therefore minimum bid) have now changed                    //
        // Expired auctions fourth - checks for any listings that have expired, completes if they have bids, expires if they do not //
        // Finally new listings then queries now that the lists alteration for this cycle are complete                              //
        // Online = full functionality, Safe Mode = character queries and queries only, Offline = no functionality                  //
        internal void Update()
        {
            Timer updateTimer = new Timer();
            updateTimer.Start();

            switch (Status)
            {
                case (AHStatus.ONLINE):
                {
                    //DealWithBackgroundTasks();
                    DealWithCancelledListings();
                    DealWithBuyOuts();
                    DealWithBids();
                    DealWithExpiredListings();
                    DealWithNewListings();
                    DealWithQueries();
                    break;
                }
                case (AHStatus.SAFE_MODE):
                {
                    DealWithQueries();
                    break;
                }
            }

            updateTimer.Stop();

            if (updateTimer.Interval > m_maxUpdateTime)
                m_maxUpdateTime = updateTimer.Interval;
        }

        #endregion

        #region BackgroundTasks (REMOVED)

        // DeleteCompletedListings                                          //
        // External background task access to delete old completed listings //
        /*internal void DeleteCompletedListings(int numOfDays)
        {
            m_databaseManager.DeleteOldListings(numOfDays);
        }*/

        // DealWithBackgroundTasks                                                //
        // Manages the auction house background tasks:                            //
        // - deletes all the completed listings over 30 days old (every 30 days)  //
        /*private void DealWithBackgroundTasks()
        {
            DateTime nowTime = DateTime.Now;

            if (nowTime > (m_lastTableClean + TimeSpan.FromDays(TABLE_CLEAN_IN_DAYS)))
            {
                Program.Display("Creating new AHDeleteCompletedListings Task!");

                AHDeleteCompletedListings task = new AHDeleteCompletedListings(TABLE_CLEAN_IN_DAYS);

                lock (Program.processor.m_backgroundTasks)
                {
                    Program.processor.m_backgroundTasks.Enqueue(task);
                }

                m_lastTableClean = nowTime;
            }
        }*/

        #endregion

        #region Update Functions

        // AHRequestIsNull                                                                         //
        // Checks if the request/player or active character objects are null and prints in the log //
        private bool AHRequestIsNull(AHRequest request)
        {
            /*if (request == null)
            {
                Program.Display("AuctionHouseManager.cs - AHRequestIsNull() an AHRequest was null!");
                return true;
            }*/

            if (request.Player == null)
            {
                Program.Display("AuctionHouseManager.cs - AHRequestIsNull() a Player was null!");
                return true;
            }
            if (request.Player.m_activeCharacter == null)
            {
                Program.Display("AuctionHouseManager.cs - AHRequestIsNull() an active character was null!");
                return true;
            }

            return false;
        }

        // DealWithCancelledListings                                                                          //
        // Cancellation of a listing - requested only by the seller character                                 //
        // Returns the current highest bid by mail (if there is one) and sends a message (if they are online) //
        // Returns the listing item to the seller and flags the auction as complete                           //
        // Server-Side Checks:                                                                                //
        // - that the character attempting to cancel is the one who created the listing                       //
        private void DealWithCancelledListings()
        {
            if (m_cancellationAttempts.Count > 0)
            {
                foreach (AHCancel cancellationAttempt in m_cancellationAttempts)
                {
                    #region Null Checks

                    if (AHRequestIsNull(cancellationAttempt))
                    {
                        Program.Display("AuctionHouseManager.cs - DealWithCancelledListings() an AHCancel had a null ref!");
                        continue;
                    }

                    #endregion

                    #region Checks This Cycle

                    if (m_listingHasBeenCancelled.Contains(cancellationAttempt.ListingID))
                    {
						string locText = Localiser.GetString(textDB, cancellationAttempt.Player, (int)AuctionHouseManagerTextDB.TextID.LISTING_ALREADY_CANCELLED);
						SendClientResponse(AHServerMessageType.FAILED_CANCELLATION, cancellationAttempt.Player, locText);
						continue;
                    }

                    if (m_listingHasBeenBought.Contains(cancellationAttempt.ListingID))
                    {
						string locText = Localiser.GetString(textDB, cancellationAttempt.Player, (int)AuctionHouseManagerTextDB.TextID.LISTING_JUST_BOUGHT_OUT);
						SendClientResponse(AHServerMessageType.FAILED_BUY_OR_BID, cancellationAttempt.Player, locText);
						continue;
                    }

                    if (m_listingHasBeenBidOn.Contains(cancellationAttempt.ListingID))
                    {
						string locText = Localiser.GetString(textDB, cancellationAttempt.Player, (int)AuctionHouseManagerTextDB.TextID.LISTING_JUST_BID_ON);
						SendClientResponse(AHServerMessageType.FAILED_BUY_OR_BID, cancellationAttempt.Player, locText);
						continue;
                    }

                    #endregion

                    AHListing listingToBeCancelled = AuctionHouseFiltering.GetListing(cancellationAttempt.ListingID, m_listings);
                    if (listingToBeCancelled == null)
                    {
						string locText = Localiser.GetString(textDB, cancellationAttempt.Player, (int)AuctionHouseManagerTextDB.TextID.CANCELLED_LISTING_NOT_FOUND);
						SendClientResponse(AHServerMessageType.FAILED_CANCELLATION, cancellationAttempt.Player, locText);
						continue;
                    }

                    #region Server-Side Checks

                    if (cancellationAttempt.Player.m_activeCharacter.ServerID != listingToBeCancelled.SellerID)
                    {
						string locText = Localiser.GetString(textDB, cancellationAttempt.Player, (int)AuctionHouseManagerTextDB.TextID.CANNOT_CANCEL_LISTING);
						SendClientResponse(AHServerMessageType.FAILED_CANCELLATION, cancellationAttempt.Player, locText);
						continue;
                    }

                    #endregion

                    // Cancellation Success Delegate //
                    MainServer.SQLSuccessDelegate successDelegate = delegate()
                    {
                        #region Null Checks

                        if (AHRequestIsNull(cancellationAttempt))
                        {
                            Program.Display("AuctionHouseManager.cs - DealWithCancelledListings() successDelegate() an AHCancel had a null ref!");
                            return;
                        }

                        #endregion

                        // Cancelled! - Return bid to highest bidder //
                        m_listingHasBeenCancelled.Remove(listingToBeCancelled.ListingID);
                        if (listingToBeCancelled.CurrentBid != -1 && listingToBeCancelled.HighestBidderID != -1)
                        {
                            ReturnHighestBid(listingToBeCancelled, AHMailMessageType.LISTING_CANCELLED_BIDDER);
							m_outBidManager.RemoveListingFromOutBids(listingToBeCancelled.ListingID);
							int textID = (int)AuctionHouseManagerTextDB.TextID.LISTING_OF_ITEM_CANCELLED;
							NotifyCharacterIfOnlineLocalised(new LocaliseParams(textDB, textID, listingToBeCancelled.ItemQuantity), 
															AHServerMessageType.LISTING_CANCELLED_BIDDER, 
															listingToBeCancelled.HighestBidderID,
															listingToBeCancelled.Item);
							m_databaseManager.LogAHTransaction(listingToBeCancelled.ListingID, listingToBeCancelled.HighestBidderID, -1, null, listingToBeCancelled.CurrentBid, AHServerMessageType.LISTING_CANCELLED_BIDDER);
                        }

                        // Cancelled! - Return item to seller //
                        m_mailManager.SendMailToPlayer(listingToBeCancelled.SellerID,
                                                       AHMailMessageType.LISTING_CANCELLED_SELLER,
                                                       listingToBeCancelled.Item,
                                                       listingToBeCancelled.ItemQuantity,
                                                       0,
                                                       0,
                                                       false);
                        m_databaseManager.LogAHTransaction(listingToBeCancelled.ListingID, listingToBeCancelled.SellerID, (int)cancellationAttempt.Player.m_account_id, listingToBeCancelled.Item, 0, AHServerMessageType.LISTING_CANCELLED_SELLER);
						string locText = Localiser.GetString(textDB, cancellationAttempt.Player, (int)AuctionHouseManagerTextDB.TextID.LISTING_SUCCESSFULLY_CANCELLED);
						SendClientResponse(AHServerMessageType.LISTING_CANCELLED_SELLER, cancellationAttempt.Player, locText);
					};

                    // Cancellation Failure Delegate //
                    MainServer.SQLFailureDelegate failureDelegate = delegate()
                    {
                        #region Null Checks

                        if (AHRequestIsNull(cancellationAttempt))
                        {
                            Program.Display("AuctionHouseManager.cs - DealWithCancelledListings() failureDelegate() an AHCancel had a null ref!");
                            return;
                        }

                        #endregion

                        m_outBidManager.RemoveListingFromOutBids(listingToBeCancelled.ListingID);
                        m_listingHasBeenCancelled.Remove(listingToBeCancelled.ListingID);
                        m_databaseManager.LogAHError(listingToBeCancelled, 0, AHServerMessageType.FAILED_CANCELLATION);
						string locText = Localiser.GetString(textDB, cancellationAttempt.Player, (int)AuctionHouseManagerTextDB.TextID.LISTING_CANCELLATION_FAILED);
						SendClientResponse(AHServerMessageType.FAILED_CANCELLATION, cancellationAttempt.Player, locText);
						Program.Display("AuctionHouseManager.cs - DealWithCancelledListings() a cancellation attempts UpdateListingAsComplete() failed!");
                    };

                    // Cancel //
                    m_listingHasBeenCancelled.Add(listingToBeCancelled.ListingID);
                    UpdateListingAsComplete(listingToBeCancelled.ListingID, successDelegate, failureDelegate);
                    m_listings.Remove(listingToBeCancelled);
                }

                m_cancellationAttempts.Clear();
            }
        }

        // UpdateListingAsComplete                                         //
        // If the SQL statement to update the listing as complete succeeds //
        private void UpdateListingAsComplete(int listingID, MainServer.SQLSuccessDelegate successDelegate, MainServer.SQLFailureDelegate failureDelegate)
        {
            m_databaseManager.UpdateListingAsComplete(listingID, successDelegate, failureDelegate);
        }

        // DealWithBuyOuts                                                                                //
        // Uses first-come-first-serve - subsequent buy outs are returned as the listing no longer exists //
        // Also checks that the listing wasnt just cancelled this cycle                                   //
        // If there was a bid, returns the bid and notifies that character of the buy out                 //
        // Server-Side Checks:                                                                            //
        // - a character cannot buy out their own listing                                                 //
        // - that the character has enough gold to buy out the listing                                    //
        private void DealWithBuyOuts()
        {
            if (m_buyOrderAttempts.Count > 0)
            {
                foreach (AHBuyOut buyOrderAttempt in m_buyOrderAttempts)
                {
                    #region Null Checks

                    if (AHRequestIsNull(buyOrderAttempt))
                    {
                        Program.Display("AuctionHouseManager.cs - DealWithBuyOuts() an AHBuyOut had a null ref!");
                        continue;
                    }

                    if (buyOrderAttempt.Player.m_activeCharacter.m_inventory == null)
                    {
                        Program.Display(String.Format("AuctionHouseManager.cs - DealWithBuyOuts() character id: {0} has a null inventory!", buyOrderAttempt.Player.m_activeCharacter.ServerID));
                        continue;
                    }

                    if (buyOrderAttempt.Player.m_activeCharacter.m_inventory.m_character == null)
                    {
                        Program.Display(String.Format("AuctionHouseManager.cs - DealWithBuyOuts() character id: {0} inventory has a null character!", buyOrderAttempt.Player.m_activeCharacter.ServerID));
                        return;
                    }

                    if (buyOrderAttempt.Player.m_activeCharacter.m_inventory.m_character.m_player == null)
                    {
                        Program.Display(String.Format("AuctionHouseManager.cs - DealWithBuyOuts() character id: {0} inventory -> character has a null player!", buyOrderAttempt.Player.m_activeCharacter.ServerID));
                        return;
                    }

                    #endregion

                    #region Checks This Cycle

                    if (m_listingHasBeenCancelled.Contains(buyOrderAttempt.ListingID))
                    {
						string locText = Localiser.GetString(textDB, buyOrderAttempt.Player, (int)AuctionHouseManagerTextDB.TextID.LISTING_JUST_CANCELLED);
						SendClientResponse(AHServerMessageType.FAILED_BUY_OR_BID, buyOrderAttempt.Player, locText);
						continue;
                    }

                    if (m_listingHasBeenBought.Contains(buyOrderAttempt.ListingID))
                    {
						string locText = Localiser.GetString(textDB, buyOrderAttempt.Player, (int)AuctionHouseManagerTextDB.TextID.LISTING_JUST_BOUGHT_OUT);
						SendClientResponse(AHServerMessageType.FAILED_BUY_OR_BID, buyOrderAttempt.Player, locText);
						continue;
                    }

                    if (m_listingHasBeenBidOn.Contains(buyOrderAttempt.ListingID))
                    {
						string locText = Localiser.GetString(textDB, buyOrderAttempt.Player, (int)AuctionHouseManagerTextDB.TextID.LISTING_JUST_BID_ON);
						SendClientResponse(AHServerMessageType.FAILED_BUY_OR_BID, buyOrderAttempt.Player, locText);
						continue;
                    }

                    #endregion

                    AHListing listingToBeBoughtOut = AuctionHouseFiltering.GetListing(buyOrderAttempt.ListingID, m_listings);
                    if(listingToBeBoughtOut == null)
                    {
						string locText = Localiser.GetString(textDB, buyOrderAttempt.Player, (int)AuctionHouseManagerTextDB.TextID.LISTING_NOT_FOUND);
						SendClientResponse(AHServerMessageType.FAILED_BUY_OR_BID, buyOrderAttempt.Player, locText);
						continue;
                    }

                    #region Server-Side Checks

                    bool ownListing = listingToBeBoughtOut.SellerID == buyOrderAttempt.BuyerID; 

                    if (ownListing == true || buyOrderAttempt.Player.m_activeCharacter.m_inventory.m_coins < listingToBeBoughtOut.BuyOut)
                    {
						string locText = "";
						if (ownListing)
						{
							locText = Localiser.GetString(textDB, buyOrderAttempt.Player, (int)AuctionHouseManagerTextDB.TextID.CANT_BUY_OWN_LISTING);
						}
						else
						{
							locText = Localiser.GetString(textDB, buyOrderAttempt.Player, (int)AuctionHouseManagerTextDB.TextID.CANT_AFFORD_BUYOUT);
						}
						SendClientResponse(AHServerMessageType.FAILED_BUY_OR_BID, buyOrderAttempt.Player, locText);
						continue;
                    }

                    #endregion

                    // Take coins straight away
                    buyOrderAttempt.Player.m_activeCharacter.updateCoins(-listingToBeBoughtOut.BuyOut);
                    buyOrderAttempt.Player.m_activeCharacter.m_inventory.SendInventoryUpdate();

                    // Buy Out Success Delegate //
                    MainServer.SQLSuccessDelegate successDelegate = delegate()
                    {
                        #region Null Checks

                        bool nullRef = false;

                        if (AHRequestIsNull(buyOrderAttempt))
                        {
                            Program.Display("AuctionHouseManager.cs - DealWithBuyOuts() successDelegate() an AHBuyOut had a null ref!");
                            nullRef = true;
                        }
                        else if (buyOrderAttempt.Player.m_activeCharacter.m_inventory == null)
                        {
                            Program.Display(String.Format("AuctionHouseManager.cs - DealWithBuyOuts() successDelegate() character id: {0} has a null inventory!", buyOrderAttempt.Player.m_activeCharacter.ServerID));
                            nullRef = true;
                        }
						else if (buyOrderAttempt.Player.m_activeCharacter.m_inventory.m_character == null)
                        {
                            Program.Display(String.Format("AuctionHouseManager.cs - DealWithBuyOuts() successDelegate() character id: {0} inventory has a null character!", buyOrderAttempt.Player.m_activeCharacter.ServerID));
                            nullRef = true;
                        }
						else if (buyOrderAttempt.Player.m_activeCharacter.m_inventory.m_character.m_player == null)
                        {
                            Program.Display(String.Format("AuctionHouseManager.cs - DealWithBuyOuts() successDelegate() character id: {0} inventory -> character has a null player!", buyOrderAttempt.Player.m_activeCharacter.ServerID));
                            nullRef = true;
                        }
						else if (buyOrderAttempt.Player.m_activeCharacter.m_inventory.m_character.m_player.connection == null)
                        {
                            Program.Display(String.Format("AuctionHouseManager.cs - DealWithBuyOuts() successDelegate() character id: {0} inventory -> character -> player has a null connection!", buyOrderAttempt.Player.m_activeCharacter.ServerID));
                            nullRef = true;
                        }

                        // If we have a null ref we cannot continue with this transaction
                        // Remove the listing id from those that are being bought out
                        // Update the listing on SQL as active again
                        // Re-add the listing to the listings
                        if (nullRef == true)
                        {
                            m_listingHasBeenBought.Remove(buyOrderAttempt.ListingID);
                            m_databaseManager.UpdateListingAsNotComplete(listingToBeBoughtOut.ListingID);
                            m_listings.Add(listingToBeBoughtOut);
                            return;
                        }

                        #endregion

                        // Bought Out! - Return bid to highest bidder //
                        m_listingHasBeenBought.Remove(buyOrderAttempt.ListingID);
                        if (listingToBeBoughtOut.CurrentBid != -1 && listingToBeBoughtOut.HighestBidderID != -1)
                        {
                            ReturnHighestBid(listingToBeBoughtOut, AHMailMessageType.LISTING_BOUGHT_OUT);
							m_outBidManager.RemoveListingFromOutBids(listingToBeBoughtOut.ListingID);
							NotifyCharacterIfOnlineLocalised(new LocaliseParams(textDB, (int)AuctionHouseManagerTextDB.TextID.LISTING_OF_ITEM_BOUGHT_OUT, listingToBeBoughtOut.ItemQuantity),
												   			 AHServerMessageType.LISTING_BOUGHT_OUT,
															 listingToBeBoughtOut.HighestBidderID,
															 listingToBeBoughtOut.Item);
							m_databaseManager.LogAHTransaction(listingToBeBoughtOut.ListingID, listingToBeBoughtOut.HighestBidderID, -1, null, listingToBeBoughtOut.CurrentBid, AHServerMessageType.LISTING_BOUGHT_OUT);
                        }

                        // Bought Out! - Award gold to seller and notify them //
                        int deposit = GetDeposit(listingToBeBoughtOut.StartingBid, listingToBeBoughtOut.Duration);
                        m_mailManager.SendMailToPlayer(listingToBeBoughtOut.SellerID,
                                                       AHMailMessageType.LISTING_COMPLETED,
                                                       listingToBeBoughtOut.Item,
                                                       listingToBeBoughtOut.ItemQuantity,
                                                       listingToBeBoughtOut.BuyOut,
                                                       deposit,
                                                       true);
						NotifyCharacterIfOnlineLocalised(new LocaliseParams(textDB, (int)AuctionHouseManagerTextDB.TextID.LISTING_OF_ITEM_COMPLETED, listingToBeBoughtOut.ItemQuantity),
														 AHServerMessageType.LISTING_COMPLETED,
														 listingToBeBoughtOut.SellerID,
														 listingToBeBoughtOut.Item);
						m_databaseManager.LogAHTransaction(listingToBeBoughtOut.ListingID,
                                                           listingToBeBoughtOut.SellerID,
                                                           -1,
                                                           null,
                                                           ((int)Math.Round((listingToBeBoughtOut.BuyOut * AuctionHouseParams.SalesTax), 0) + deposit),
                                                           AHServerMessageType.LISTING_COMPLETED);

                        // Bought Out! - Award item to buyer //
                        Character buyer = buyOrderAttempt.Player.m_activeCharacter;
                        m_mailManager.SendMailToPlayer(buyOrderAttempt.BuyerID,
                                                       AHMailMessageType.LISTING_WON,
                                                       listingToBeBoughtOut.Item,
                                                       listingToBeBoughtOut.ItemQuantity,
                                                       listingToBeBoughtOut.BuyOut, // sent only to be recorded on the mail sent, not actually being awarded
                                                       0,
                                                       false);
                        buyer.m_inventory.SendInventoryUpdate();
                        m_databaseManager.LogAHTransaction(listingToBeBoughtOut.ListingID, buyOrderAttempt.BuyerID, (int)buyOrderAttempt.Player.m_account_id, listingToBeBoughtOut.Item, -listingToBeBoughtOut.BuyOut, AHServerMessageType.LISTING_WON_BUYOUT);
						string locText = Localiser.GetString(textDB, buyOrderAttempt.Player, (int)AuctionHouseManagerTextDB.TextID.LISTING_SUCCESSFULLY_BOUGHT_OUT);
						SendClientResponse(AHServerMessageType.LISTING_WON_BUYOUT, buyOrderAttempt.Player, locText);
					};

                    // Buy Out Failure Delegate //
                    MainServer.SQLFailureDelegate failureDelegate = delegate()
                    {
                        #region Null Checks

                        if (AHRequestIsNull(buyOrderAttempt))
                        {
                            Program.Display("AuctionHouseManager.cs - DealWithBuyOuts() failureDelegate() an AHBuyOut had a null ref!");
                            return;
                        }

                        #endregion

                        // Mail buyout gold back
                        m_mailManager.SendMailToPlayer(buyOrderAttempt.BuyerID,
                                                       AHMailMessageType.LISTING_BUY_OUT_FAILED,
                                                       listingToBeBoughtOut.Item,
                                                       listingToBeBoughtOut.ItemQuantity,
                                                       listingToBeBoughtOut.BuyOut,
                                                       0,
                                                       true);
                        m_outBidManager.RemoveListingFromOutBids(listingToBeBoughtOut.ListingID);
                        m_listingHasBeenBought.Remove(buyOrderAttempt.ListingID);
                        m_databaseManager.LogAHError(listingToBeBoughtOut, listingToBeBoughtOut.BuyOut, AHServerMessageType.FAILED_BUY_OR_BID);
						string locText = Localiser.GetString(textDB, buyOrderAttempt.Player, (int)AuctionHouseManagerTextDB.TextID.LISTING_BUYOUT_FAILED);
						SendClientResponse(AHServerMessageType.FAILED_BUY_OR_BID, buyOrderAttempt.Player, locText);
						Program.Display("AuctionHouseManager.cs - DealWithBuyOuts() a buyout attempts UpdateListingAsComplete() failed!");
                    };

                    // Buy Out //
                    m_listingHasBeenBought.Add(buyOrderAttempt.ListingID);
                    UpdateListingAsComplete(listingToBeBoughtOut.ListingID, successDelegate, failureDelegate);
                    m_listings.Remove(listingToBeBoughtOut);
                }

                m_buyOrderAttempts.Clear();
            }
        }

        // DealWithBids                                                                                                                //
        // Checks for cancellations and buy outs made this cycle - in which case the listing no longer exists                          //
        // First Come First Serve - only the first bid (per cycle) will be recognised per listing as the minimum bid will have changed //
        // Server-Side Checks:                                                                                                         //
        // - prevents a character from outbidding themselves (REMOVED - REQUESTED BY DESIGN)                                           //
        // - prevents a character from bidding on their own listings                                                                   //
        // - checks that the bidder has the required coins                                                                             //
        // - if the bid exceeds the listings buyout - an out-of-sequence buyout of the listing is triggered instead of a bid           //
        // - prevents a bid below a 5% increase of the current bid from being placed                                                   //
        private void DealWithBids()
        {
            if (m_bidAttempts.Count > 0)
            {
                foreach (AHBid bidAttempt in m_bidAttempts)
                {
                    #region Null Checks

                    if (AHRequestIsNull(bidAttempt))
                    {
                        Program.Display("AuctionHouseManager.cs - DealWithBids() an AHBid had a null ref!");
                        continue;
                    }

                    if (bidAttempt.Player.m_activeCharacter.m_inventory == null)
                    {
                        Program.Display(String.Format("AuctionHouseManager.cs - DealWithBids() character id: {0} has a null inventory!", bidAttempt.Player.m_activeCharacter.ServerID));
                        continue;
                    }

                    if (bidAttempt.Player.m_activeCharacter.m_inventory.m_character == null)
                    {
                        Program.Display(String.Format("AuctionHouseManager.cs - DealWithBids() character id: {0} inventory has a null character!", bidAttempt.Player.m_activeCharacter.ServerID));
                        return;
                    }

                    if (bidAttempt.Player.m_activeCharacter.m_inventory.m_character.m_player == null)
                    {
                        Program.Display(String.Format("AuctionHouseManager.cs - DealWithBids() character id: {0} inventory -> character has a null player!", bidAttempt.Player.m_activeCharacter.ServerID));
                        return;
                    }

                    #endregion

                    #region Checks This Cycle

                    if (m_listingHasBeenCancelled.Contains(bidAttempt.ListingID))
                    {
						string locText = Localiser.GetString(textDB, bidAttempt.Player, (int)AuctionHouseManagerTextDB.TextID.LISTING_JUST_CANCELLED);
						SendClientResponse(AHServerMessageType.FAILED_BUY_OR_BID, bidAttempt.Player, locText);
                        continue;
                    }

                    if (m_listingHasBeenBought.Contains(bidAttempt.ListingID))
                    {
						string locText = Localiser.GetString(textDB, bidAttempt.Player, (int)AuctionHouseManagerTextDB.TextID.LISTING_JUST_BOUGHT_OUT);
						SendClientResponse(AHServerMessageType.FAILED_BUY_OR_BID, bidAttempt.Player, locText);
						continue;
                    }

                    if (m_listingHasBeenBidOn.Contains(bidAttempt.ListingID))
                    {
						string locText = Localiser.GetString(textDB, bidAttempt.Player, (int)AuctionHouseManagerTextDB.TextID.LISTING_JUST_BID_ON);
						SendClientResponse(AHServerMessageType.FAILED_BUY_OR_BID, bidAttempt.Player, locText);
                        continue;
                    }

                    #endregion

                    AHListing listingToBeBidOn = AuctionHouseFiltering.GetListing(bidAttempt.ListingID, m_listings);
                    if (listingToBeBidOn == null)
                    {
						string locText = Localiser.GetString(textDB, bidAttempt.Player, (int)AuctionHouseManagerTextDB.TextID.LISTING_NOT_FOUND);
						SendClientResponse(AHServerMessageType.FAILED_BUY_OR_BID, bidAttempt.Player, locText);
						continue;
                    }

                    #region Server-Side Checks

                    /*if (bidAttempt.BidderID == listingToBeBidOn.HighestBidderID)
                    {
                        SendClientResponse(AHServerMessageType.FAILED_BUY_OR_BID, bidAttempt.Player, HIGHEST_BIDDER);
                        continue;
                    }*/

                    bool ownListing = listingToBeBidOn.SellerID == bidAttempt.Player.m_activeCharacter.ServerID;

                    if (ownListing == true || bidAttempt.Player.m_activeCharacter.m_inventory.m_coins < bidAttempt.BidAmount)
                    {
						string locText = "";
						if (ownListing)
						{
							locText = Localiser.GetString(textDB, bidAttempt.Player, (int)AuctionHouseManagerTextDB.TextID.CANT_BID_ON_OWN_LISTING);
						}
						else
						{
							locText = Localiser.GetString(textDB, bidAttempt.Player, (int)AuctionHouseManagerTextDB.TextID.CANT_AFFORD_BID);
						}
						SendClientResponse(AHServerMessageType.FAILED_BUY_OR_BID, bidAttempt.Player, locText);
						continue;
                    }

                    if (listingToBeBidOn.BuyOut > -1 && (bidAttempt.BidAmount >= listingToBeBidOn.BuyOut))
                    {
                        AHBuyOut newBuyOut = new AHBuyOut(bidAttempt.ListingID, bidAttempt.BidderID, bidAttempt.Player);
                        m_buyOrderAttempts.Add(newBuyOut);
                        DealWithBuyOuts();
                        continue;
                    }

                    int minimumRequiredBid = listingToBeBidOn.CurrentBid == -1 ? listingToBeBidOn.StartingBid : GetMinimumBid(listingToBeBidOn.CurrentBid);

                    if (bidAttempt.BidAmount < minimumRequiredBid)
                    {
                        SendClientResponse(AHServerMessageType.FAILED_BUY_OR_BID, bidAttempt.Player, "The bid amount is not 5% higher than the current highest bid!");
                        continue;
                    }

                    #endregion

                    // Save the current bidder details as we are about to attempt to overwrite them!
                    int    currentBid        = listingToBeBidOn.CurrentBid;
                    int    currentBidderID   = listingToBeBidOn.HighestBidderID;
                    string currentBidderName = listingToBeBidOn.HighestBidderName;

                    // Take bid straight away
                    Character bidder = bidAttempt.Player.m_activeCharacter;
                    bidder.updateCoins(-bidAttempt.BidAmount);
                    bidder.m_inventory.SendInventoryUpdate();

                    // Bid Success Delegate //
                    MainServer.SQLSuccessDelegate successDelegate = delegate()
                    {
                        #region Null Checks

                        bool nullRef = false;

                        if (AHRequestIsNull(bidAttempt))
                        {
                            Program.Display("AuctionHouseManager.cs - DealWithBids() successDelegate() an AHBid had a null ref!");
                            nullRef = true;
                        }
                        else if (bidAttempt.Player.m_activeCharacter.m_inventory == null)
                        {
                            Program.Display(String.Format("AuctionHouseManager.cs - DealWithBids() successDelegate() character id: {0} has a null inventory!", bidAttempt.Player.m_activeCharacter.ServerID));
                            nullRef = true;
                        }
						else if (bidAttempt.Player.m_activeCharacter.m_inventory.m_character == null)
                        {
                            Program.Display(String.Format("AuctionHouseManager.cs - DealWithBids() successDelegate() character id: {0} inventory has a null character!", bidAttempt.Player.m_activeCharacter.ServerID));
                            nullRef = true;
                        }
						else if (bidAttempt.Player.m_activeCharacter.m_inventory.m_character.m_player == null)
                        {
                            Program.Display(String.Format("AuctionHouseManager.cs - DealWithBids() successDelegate() character id: {0} inventory -> character has a null player!", bidAttempt.Player.m_activeCharacter.ServerID));
                            nullRef = true;
                        }
						else if (bidAttempt.Player.m_activeCharacter.m_inventory.m_character.m_player.connection == null)
                        {
                            Program.Display(String.Format("AuctionHouseManager.cs - DealWithBids() successDelegate() character id: {0} inventory -> character -> player has a null connection!", bidAttempt.Player.m_activeCharacter.ServerID));
                            nullRef = true;
                        }

                        // Revert the listing back to the old bidder details and update SQL
                        if (nullRef == true)
                        {
                            m_listingHasBeenBidOn.Remove(bidAttempt.ListingID);
                            ResetListingBidDetails(listingToBeBidOn, currentBidderID, currentBid, currentBidderName);
                            return;
                        }

                        #endregion

                        // Bid Placed! - Return the previous bid //
                        m_listingHasBeenBidOn.Remove(bidAttempt.ListingID);
                        if (currentBid != -1 && currentBidderID != -1)
                        {
                            m_mailManager.SendMailToPlayer(currentBidderID,
                                                           AHMailMessageType.OUT_BID,
                                                           listingToBeBidOn.Item,
                                                           listingToBeBidOn.ItemQuantity,
                                                           currentBid,
                                                           0,
                                                           true);
							m_outBidManager.AddOutBid(currentBidderID, listingToBeBidOn.ListingID);
							m_outBidManager.RemoveListingFromCharacterOutBids(bidAttempt.BidderID, bidAttempt.ListingID);
							NotifyCharacterIfOnlineLocalised(new LocaliseParams(textDB, (int)AuctionHouseManagerTextDB.TextID.LISTING_OF_ITEM_OUTBID, listingToBeBidOn.ItemQuantity),
														     AHServerMessageType.OUT_BID,
														     currentBidderID,
														     listingToBeBidOn.Item);
							m_databaseManager.LogAHTransaction(listingToBeBidOn.ListingID, currentBidderID, -1, null, currentBid, AHServerMessageType.OUT_BID);
                        }

                        // Bid Placed! - Complete the new bid //
                        m_databaseManager.LogAHTransaction(listingToBeBidOn.ListingID, bidAttempt.BidderID, (int)bidAttempt.Player.m_account_id, null, -bidAttempt.BidAmount, AHServerMessageType.BID_PLACED);
						string locText = Localiser.GetString(textDB, bidAttempt.Player, (int)AuctionHouseManagerTextDB.TextID.LISTING_BID_SUCCESSFUL);
						SendClientResponse(AHServerMessageType.BID_PLACED, bidAttempt.Player, locText);
					};

                    // Bid Failure Delegate //
                    MainServer.SQLFailureDelegate failureDelegate = delegate()
                    {
                        #region Null Checks

                        if (AHRequestIsNull(bidAttempt))
                        {
                            Program.Display("AuctionHouseManager.cs - DealWithBids() failureDelegate() an AHBid had a null ref!");
                            return;
                        }

                        #endregion

                        // Mail bid gold back
                        m_mailManager.SendMailToPlayer(bidAttempt.BidderID,
                                                       AHMailMessageType.LISTING_BID_FAILED,
                                                       listingToBeBidOn.Item,
                                                       listingToBeBidOn.ItemQuantity,
                                                       bidAttempt.BidAmount,
                                                       0,
                                                       true);
                        m_listingHasBeenBidOn.Remove(bidAttempt.ListingID);
						string locText = Localiser.GetString(textDB, bidAttempt.Player, (int)AuctionHouseManagerTextDB.TextID.LISTING_BID_FAILED);
						SendClientResponse(AHServerMessageType.FAILED_BUY_OR_BID, bidAttempt.Player, locText);
						Program.Display("AuctionHouseManager.cs - DealWithBids() a bid attempts UpdateListingBidDetails() failed!");
                    };

                    // Bid //
                    m_listingHasBeenBidOn.Add(bidAttempt.ListingID);
                    UpdateListingBidDetails(listingToBeBidOn, bidAttempt, successDelegate, failureDelegate);
                }

                m_bidAttempts.Clear();
            }
        }

        // GetMinimumBid                                                               //
        // Returns the value plus the set amount for bidding increase                  //
        // If the value is so small that they are the same - it returns that value + 1 //
        private int GetMinimumBid(int currentBid)
        {
            int newBid = ((int)Math.Round((currentBid * AuctionHouseParams.MinimumBidIncrease), 0));

            if (currentBid == newBid)
            {
                newBid += 1;
            }

            return newBid;
        }

        // UpdateListingBidDetails                                          //
        // If the SQL statement to update the listings bid details succeeds //
        // Updates the listings bid details on the passed AHListing object  //
        private void UpdateListingBidDetails(AHListing bidOnListing, AHBid bidDetails, MainServer.SQLSuccessDelegate successDelegate, MainServer.SQLFailureDelegate failureDelegate)
        {
            string bidderName = bidDetails.Player.m_activeCharacter.Name;

            m_databaseManager.UpdateListingBidDetails(bidDetails.BidAmount, bidDetails.BidderID, bidderName, bidDetails.ListingID, successDelegate, failureDelegate);

            bidOnListing.CurrentBid        = bidDetails.BidAmount;
            bidOnListing.HighestBidderID   = bidDetails.BidderID;
            bidOnListing.HighestBidderName = bidderName;
        }

        // ResetListingBidDetails                         //
        // Function to return a bid to its previous state //
        private void ResetListingBidDetails(AHListing bidOnListing, int oldBidAmount, int oldBidderID, string oldBiddersName)
        {
            m_databaseManager.UpdateListingBidDetails(oldBidAmount, oldBidderID, oldBiddersName, bidOnListing.ListingID);

            bidOnListing.CurrentBid        = oldBidAmount;
            bidOnListing.HighestBidderID   = oldBidderID;
            bidOnListing.HighestBidderName = oldBiddersName;
        }

        // CheckForExpiredListings                                                    //
        // Timer method to check for listings that have expired                       //
        // If the listing currently has a transaction in progress, it will be skipped //
        private void CheckForExpiredListings()
        {
            Timer updateTimer = new Timer();
            updateTimer.Start();

            DateTime nowTime = DateTime.Now;
            foreach (AHListing listing in m_listings)
            {
                #region Checks This Cycle

                if (m_listingHasBeenCancelled.Contains(listing.ListingID))
                {
                    continue;
                }

                if (m_listingHasBeenBought.Contains(listing.ListingID))
                {
                    continue;
                }

                if (m_listingHasBeenBidOn.Contains(listing.ListingID))
                {
                    continue;
                }

                #endregion

                if (listing.ExpiryDateTime < nowTime && !m_expiredListings.Contains(listing.ListingID))
                {
                    m_expiredListings.Add(listing.ListingID);
                }
            }

            updateTimer.Stop();

            // Update processor variables for server form display
            Program.m_AHActiveListings    = m_listings.Count;
            Program.m_AHExpiringListings  = m_expiredListings.Count;
            Program.m_AHListingUpdateTime = updateTimer.Interval;
            Program.m_AHUpdateTime        = m_maxUpdateTime;
            m_maxUpdateTime               = 0.0;
        }

        // DealWithExpiredListings                                                                            //
        // Gets all the listings which have expired this cycle (any earlier action this cycle is still valid) //
        // Either completes the listing if there was a bidder, or the listing expires                         //
        // If the seller is online - a completion or expiry message is sent                                   //
        // Finally removes the listing from the listing expiry times dictionary                               //
        private void DealWithExpiredListings()
        {
            if (m_expiredListings.Count > 0)
            {
                for( int i = m_expiredListings.Count - 1; i > -1; i--)
                {
                    int expiredListingID = m_expiredListings[i];

                    AHListing listingThatHasExpired = AuctionHouseFiltering.GetListing(expiredListingID, m_listings);
                    if (listingThatHasExpired == null)
                    {
                        Program.Display("AuctionHouseManager.cs - DealWithExpiredListings() an expiring listing could not be found!");
                        continue;
                    }

                    bool listingHasCompleted = (listingThatHasExpired.HighestBidderID != -1 && listingThatHasExpired.CurrentBid != -1);
                    int  deposit             = GetDeposit(listingThatHasExpired.StartingBid, listingThatHasExpired.Duration);

                    // Completion Success Delegate //
                    MainServer.SQLSuccessDelegate successDelegate = delegate()
                    {
                        // Completed Listing //
                        if (listingHasCompleted)
                        {
                            // Award gold to seller and refund deposit
                            m_mailManager.SendMailToPlayer(listingThatHasExpired.SellerID,
                                                           AHMailMessageType.LISTING_COMPLETED,
                                                           listingThatHasExpired.Item,
                                                           listingThatHasExpired.ItemQuantity,
                                                           listingThatHasExpired.CurrentBid,
                                                           deposit,
                                                           true);
                            m_databaseManager.LogAHTransaction(listingThatHasExpired.ListingID,
                                                               listingThatHasExpired.SellerID,
                                                               -1,
                                                               null,
                                                               ((int)Math.Round((listingThatHasExpired.CurrentBid * AuctionHouseParams.SalesTax), 0) + deposit),
                                                               AHServerMessageType.LISTING_COMPLETED);

                            // Award item(s) to highest bidder and notify them
                            m_mailManager.SendMailToPlayer(listingThatHasExpired.HighestBidderID,
                                                           AHMailMessageType.LISTING_WON,
                                                           listingThatHasExpired.Item,
                                                           listingThatHasExpired.ItemQuantity,
                                                           listingThatHasExpired.CurrentBid,
                                                           0,
                                                           false);
							m_outBidManager.RemoveListingFromOutBids(listingThatHasExpired.ListingID);
							int textID = (int)AuctionHouseManagerTextDB.TextID.LISTING_OF_ITEM_WON;
							NotifyCharacterIfOnlineLocalised(new LocaliseParams(textDB, textID, listingThatHasExpired.ItemQuantity),
															 AHServerMessageType.LISTING_WON_COMPLETION,
															 listingThatHasExpired.HighestBidderID,
															 listingThatHasExpired.Item);
							m_databaseManager.LogAHTransaction(listingThatHasExpired.ListingID, listingThatHasExpired.HighestBidderID, -1, listingThatHasExpired.Item, 0, AHServerMessageType.LISTING_WON_COMPLETION);
                        }

                        // Expired Listing - Return the item and deposit to the seller //
                        else
                        {
                            m_mailManager.SendMailToPlayer(listingThatHasExpired.SellerID,
                                                           AHMailMessageType.LISTING_EXPIRIED,
                                                           listingThatHasExpired.Item,
                                                           listingThatHasExpired.ItemQuantity,
                                                           0,
                                                           deposit,
                                                           false);
                            m_databaseManager.LogAHTransaction(listingThatHasExpired.ListingID, listingThatHasExpired.SellerID, -1, listingThatHasExpired.Item, deposit, AHServerMessageType.LISTING_EXPIRED);
                        }

						// Notify seller
						string completeStatusLocText = "";
						Player player = Program.processor.getPlayerFromActiveCharacterId(listingThatHasExpired.SellerID);
						if (player != null)
						{
							if (listingHasCompleted)
							{
								completeStatusLocText = Localiser.GetString(textDB, player, (int)AuctionHouseManagerTextDB.TextID.COMPLETED);
							}
							else
							{
								completeStatusLocText = Localiser.GetString(textDB, player, (int)AuctionHouseManagerTextDB.TextID.EXPIRED);
							}
						}

						NotifyCharacterIfOnlineLocalised(new LocaliseParams(textDB, (int)AuctionHouseManagerTextDB.TextID.LISTING_OF_ITEM, listingThatHasExpired.ItemQuantity, completeStatusLocText),
														 AHServerMessageType.LISTING_COMPLETED,
														 listingThatHasExpired.SellerID,
													     listingThatHasExpired.Item);
					};

                    // Completion Failure Delegate //
                    MainServer.SQLFailureDelegate failureDelegate = delegate()
                    {
                        m_databaseManager.LogAHError(listingThatHasExpired,
                                                     (listingHasCompleted ? ((int)Math.Round((listingThatHasExpired.CurrentBid * AuctionHouseParams.SalesTax), 0) + deposit) : deposit),
                                                     (listingHasCompleted ? AHServerMessageType.LISTING_COMPLETED : AHServerMessageType.LISTING_EXPIRED));
                        Program.Display("AuctionHouseManager.cs - DealWithExpiredListings() an expiring listings UpdateListingAsComplete() failed!");
                    };

                    // Complete/Expire //
                    UpdateListingAsComplete(listingThatHasExpired.ListingID, successDelegate, failureDelegate);
                    m_listings.Remove(listingThatHasExpired);
                    m_expiredListings.RemoveAt(i);
                }

                m_expiredListings.Clear();
            }
        }

		// Localise text for player
		// Item name will always be the first string format with this function
		private void NotifyCharacterIfOnlineLocalised(LocaliseParams param, AHServerMessageType messageType, int characterID, Item item)
		{
			Player player = Program.processor.getPlayerFromActiveCharacterId(characterID);
            if (player != null)
            {
                string locText = Localiser.GetString(param.textDB, player, param.textID);
                string itemName = item.m_template.m_loc_item_name[player.m_languageIndex];
                object[] newArray = new object[param.args.Length + 1];
                newArray[0] = itemName;
                Array.Copy(param.args, 0, newArray, 1, param.args.Length);
				locText = string.Format(locText, newArray);
				SendClientResponse(messageType, player, locText);
			}
		}

		// AHListingIsNull                                                                        //
		// Checks if an AHListing / Player / activeCharacter / Item is null and prints in the log //
		private bool AHListingIsNull(AHListing listing)
        {
            /*if (listing == null)
            {
                Program.Display("AuctionHouseManager.cs - AHListingIsNull() a listing was null!");
                return true;
            }*/

            if (listing.Player == null)
            {
                Program.Display("AuctionHouseManager.cs - AHListingIsNull() a Player was null!");
                return true;
            }
            if (listing.Player.m_activeCharacter == null)
            {
                Program.Display("AuctionHouseManager.cs - AHListingIsNull() an active character was null!");
                return true;
            }
            if (listing.Item == null)
            {
                Program.Display("AuctionHouseManager.cs - AHListingIsNull() an item was null!");
                return true;
            }

            return false;
        }

        // DealWithNewListings                                                                        //
        // Adds the recieved new listings to the Auction House                                        //
        // Server-Side Checks:                                                                        //
        // - that the character does not have a listing currently awaiting creation                   //
        // - item is not NO TRADE                                                                     //
        // - the item has its full number of charges                                                  //
        // - character has enough gold to cover the deposit                                           //
        // - character does not exceed limit of listings                                              //
        // - listing is over the number of free listings + the characters number of extra slots       //
        // - if the character has the listing item and quantity of it in their inventory              //
        private void DealWithNewListings()
        {
            if (m_newListings.Count > 0)
            {
                foreach (AHListing newListing in m_newListings)
                {
                    #region Null Checks

                    if (AHListingIsNull(newListing))
                    {
                        Program.Display("AuctionHouseManager.cs - DealWithNewListings() an AHListing had a null ref!");
                        continue;
                    }

                    if (newListing.Player.m_activeCharacter.m_inventory == null)
                    {
                        Program.Display(String.Format("AuctionHouseManager.cs - DealWithNewListings() character id: {0} has a null inventory!", newListing.Player.m_activeCharacter.ServerID));
                        continue;
                    }

                    if (newListing.Player.m_activeCharacter.m_inventory.m_character == null)
                    {
                        Program.Display(String.Format("AuctionHouseManager.cs - DealWithNewListings() character id: {0} inventory has a null character!", newListing.Player.m_activeCharacter.ServerID));
                        continue;
                    }

                    if (newListing.Player.m_activeCharacter.m_inventory.m_character.m_player == null)
                    {
                        Program.Display(String.Format("AuctionHouseManager.cs - DealWithNewListings() character id: {0} inventory -> character has a null player!", newListing.Player.m_activeCharacter.ServerID));
                        continue;
                    }

                    #endregion

                    #region Server-Side Checks

                    if (m_pendingListings.Contains(newListing.SellerID))
                    {
						string locText = Localiser.GetString(textDB, newListing.Player, (int)AuctionHouseManagerTextDB.TextID.LISTING_PENDING);
						SendClientResponse(AHServerMessageType.FAILED_LISTING, newListing.Player, locText);
						continue;
                    }

                    if (newListing.Item.m_template.m_noTrade == true)
                    {
						string locText = Localiser.GetString(textDB, newListing.Player, (int)AuctionHouseManagerTextDB.TextID.NO_TRADE);
						SendClientResponse(AHServerMessageType.FAILED_LISTING, newListing.Player, locText);
						continue;
                    }

                    if (newListing.Item.m_remainingCharges != newListing.Item.m_template.m_maxCharges)
                    {
						string locText = Localiser.GetString(textDB, newListing.Player, (int)AuctionHouseManagerTextDB.TextID.CHARGES_USED);
						SendClientResponse(AHServerMessageType.FAILED_LISTING, newListing.Player, locText);
						continue;
                    }

                    if (newListing.Player.m_activeCharacter.m_inventory.m_coins < GetDeposit(newListing.StartingBid, newListing.Duration))
                    {
						string locText = Localiser.GetString(textDB, newListing.Player, (int)AuctionHouseManagerTextDB.TextID.CANT_AFFORD_LISTING);
						SendClientResponse(AHServerMessageType.FAILED_LISTING, newListing.Player, locText);
						continue;
                    }

                    int listingCharactersSlotsUsed = AuctionHouseFiltering.GetNumberOfActiveListings(newListing.SellerID, m_listings);

                    if (listingCharactersSlotsUsed >= AuctionHouseParams.MaxNumOfListings)
                    {
						string locText = Localiser.GetString(textDB, newListing.Player, (int)AuctionHouseManagerTextDB.TextID.MAX_NUMBER_OF_LISTINGS);
						SendClientResponse(AHServerMessageType.FAILED_LISTING, newListing.Player, locText);
						continue;
                    }

                    if (listingCharactersSlotsUsed >= (AuctionHouseParams.NumOfFreeListings + newListing.Player.m_activeCharacter.m_numberOfExtraAHSlots))
                    {
						string locText = Localiser.GetString(textDB, newListing.Player, (int)AuctionHouseManagerTextDB.TextID.NO_FREE_LISTINGS);
						SendClientResponse(AHServerMessageType.FAILED_LISTING, newListing.Player, locText);
						continue;
                    }

                    if (!(newListing.Player.m_activeCharacter.m_inventory.checkHasItems(newListing.ItemTemplateID) >= newListing.ItemQuantity))
                    {
                        Program.Display(String.Format("DealWithNewListings() - Trying to list {0} items and we have {1}!", newListing.ItemQuantity, newListing.Player.m_activeCharacter.m_inventory.checkHasItems(newListing.ItemTemplateID)));
						string locText = "";
						if (newListing.ItemQuantity == 1)
						{
							locText = Localiser.GetString(textDB, newListing.Player, (int)AuctionHouseManagerTextDB.TextID.ITEM_NOT_FOUND);
						}
						else
						{
							locText = Localiser.GetString(textDB, newListing.Player, (int)AuctionHouseManagerTextDB.TextID.ITEMS_NOT_FOUND);
						}
						SendClientResponse(AHServerMessageType.FAILED_LISTING, newListing.Player, locText);
						continue;
                    }

                    #endregion

                    // Save the Player reference as we are about to null it
                    Player sellerPlayer = newListing.Player;

                    // Creation Success Delegate (3) //
                    MainServer.SQLSuccessDelegate successDelegate = delegate()
                    {
                        #region Null Checks

                        bool nullRef = false;

                        // Null checks for required operations
                        if (AHListingIsNull(newListing))
                        {
                            Program.Display("AuctionHouseManager.cs - DealWithNewListings() successDelegate() an AHListing had a null ref!");
                            nullRef = true;
                        }
                        else if (sellerPlayer.m_activeCharacter.m_inventory == null)
                        {
                            Program.Display(String.Format("AuctionHouseManager.cs - DealWithNewListings() successDelegate() character id: {0} has a null inventory!", newListing.Player.m_activeCharacter.ServerID));
                            nullRef = true;
                        }
						else if (sellerPlayer.m_activeCharacter.m_inventory.m_character == null)
                        {
                            Program.Display(String.Format("AuctionHouseManager.cs - DealWithNewListings() successDelegate() character id: {0} inventory has a null character!", newListing.Player.m_activeCharacter.ServerID));
                            nullRef = true;
                        }
						else if (sellerPlayer.m_activeCharacter.m_inventory.m_character.m_player == null)
                        {
                            Program.Display(String.Format("AuctionHouseManager.cs - DealWithNewListings() successDelegate() character id: {0} inventory -> character has a null player!", newListing.Player.m_activeCharacter.ServerID));
                            nullRef = true;
                        }
						else if (sellerPlayer.m_activeCharacter.m_inventory.m_character.m_player.connection == null)
                        {
                            Program.Display(String.Format("AuctionHouseManager.cs - DealWithNewListings() successDelegate() character id: {0} inventory -> character -> player has a null connection!", sellerPlayer.m_activeCharacter.ServerID));
                            nullRef = true;
                        }

                        // Cancel this listing as one of the references has become null and remove the seller from the pending listings
                        if (nullRef == true)
                        {
                            m_databaseManager.UpdateListingAsComplete(newListing.ListingID);
                            m_pendingListings.Remove(newListing.SellerID);
                            return;
                        }

                        #endregion

                        Character listingCharacter = sellerPlayer.m_activeCharacter;
                        int       deposit          = GetDeposit(newListing.StartingBid, newListing.Duration);
                        if (listingCharacter.m_inventory.DeleteItem(newListing.ItemTemplateID, newListing.InventoryID, newListing.ItemQuantity) != String.Empty)
                        {
                            m_databaseManager.UpdateListingAsComplete(newListing.ListingID);
                            m_pendingListings.Remove(newListing.SellerID);
                            m_databaseManager.LogAHError(newListing, 0, AHServerMessageType.LISTING_CREATED);
                            Program.Display("AUCTION HOUSE - COULD NOT TAKE ITEM ON CREATE LISTING!");
                        }
                        else
                        {
                            listingCharacter.updateCoins(-deposit);
                            listingCharacter.m_inventory.SendInventoryUpdate();
                            m_databaseManager.LogAHTransaction(newListing.ListingID, newListing.SellerID, (int)newListing.Player.m_account_id, newListing.Item, -deposit, AHServerMessageType.LISTING_CREATED);
							string locText = Localiser.GetString(textDB, sellerPlayer, (int)AuctionHouseManagerTextDB.TextID.LISTING_SUCCESSFUL);
							locText = string.Format(locText, GetDeposit(newListing.StartingBid, newListing.Duration));
							SendClientResponse(AHServerMessageType.LISTING_CREATED, sellerPlayer, locText);
							newListing.Item = new Item(-1, newListing.ItemTemplateID, newListing.ItemQuantity, -1);
                            newListing.Player = null;
                            m_pendingListings.Remove(newListing.SellerID);
                            m_listings.Add(newListing);
                        }
                    };
                    // Creation Failure Delegate (3) //
                    MainServer.SQLFailureDelegate failureDelegate = delegate()
                    {
                        #region Null Checks

                        if (AHListingIsNull(newListing))
                        {
                            Program.Display("AuctionHouseManager.cs - DealWithNewListings() failureDelegate() an AHListing had a null ref!");
                            return;
                        }

                        #endregion

                        m_pendingListings.Remove(newListing.SellerID);
						string locText = Localiser.GetString(textDB, sellerPlayer, (int)AuctionHouseManagerTextDB.TextID.LISTING_FAILED);
						SendClientResponse(AHServerMessageType.FAILED_LISTING, sellerPlayer, locText);
						Program.Display("AuctionHouseManager.cs - DealWithNewListings() a new listings CreateAHListing() failed!");
                    };

                    // Creation Delegate (2) //
                    AHDatabaseManager.OnListingIDSuccess listingIDSuccess = delegate(int listingID)
                    {
                        if (AHListingIsNull(newListing))
                        {
                            Program.Display("AuctionHouseManager.cs - DealWithNewListings() listingIDSuccess() an AHListing had a null ref!");
                            return;
                        }

                        newListing.ListingID = listingID;
                        m_databaseManager.CreateAHListing(newListing, successDelegate, failureDelegate);
                    };

                    // Create (1) //
                    m_pendingListings.Add(newListing.SellerID);
                    m_databaseManager.GetAvailableListingID(listingIDSuccess);
                }

                m_newListings.Clear();
            }
        }

        // DealWithQueries                                                                                         //
        // Handles three types of queries of the auction house listings:                                           //
        // - queries are general queries using a custom filter string                                              //
        // - character queries are listings placed by a specific character                                         //
        // - bid queries are listings for which a specific character is the highest bidder, or has been out bid on //
        private void DealWithQueries()
        {
            if (m_queries.Count > 0)
            {
                foreach (AHListingQuery query in m_queries)
                {
                    int numberOfResults = 0;
                    ReturnQuery(AuctionHouseFiltering.QueryListings(query.ItemFilter,
                                                                    query.QueryString,
                                                                    query.MinLevel,
                                                                    query.MaxLevel,
                                                                    query.SortType,
                                                                    query.SortDirection,
                                                                    query.PageNumber,
                                                                    query.ResultsPerPage,
                                                                    m_listings,
                                                                    ref numberOfResults,
																	query.Player),
                                query.Player,
                                AHServerMessageType.RETURN_QUERY,
                                numberOfResults);
                }

                m_queries.Clear();
            }

            if (m_characterQueries.Count > 0)
            {
                foreach (AHCharacterQuery characterQuery in m_characterQueries)
                {
                    int numberOfResults = 0;
                    ReturnQuery(AuctionHouseFiltering.GetMyListings(characterQuery.CharacterID,
                                                                    characterQuery.SortType,
                                                                    characterQuery.SortDirection,
                                                                    characterQuery.PageNumber,
                                                                    characterQuery.ResultsPerPage,
                                                                    m_listings,
                                                                    ref numberOfResults,
																	characterQuery.Player),
                                characterQuery.Player,
                                AHServerMessageType.RETURN_CHARACTER_QUERY,
                                numberOfResults);
                }

                m_characterQueries.Clear();
            }

            if (m_bidQueries.Count > 0)
            {
                foreach (AHListingQuery bidQuery in m_bidQueries)
                {
                    int numberOfResults   = 0;
                    int characterServerID = bidQuery.Player.m_activeCharacter.ServerID;
                    ReturnQuery(AuctionHouseFiltering.GetMyBids(characterServerID,
                                                                m_outBidManager.GetOutBidListingIDs(characterServerID),
                                                                bidQuery.ItemFilter,
                                                                bidQuery.QueryString,
                                                                bidQuery.MinLevel,
                                                                bidQuery.MaxLevel,
                                                                bidQuery.SortType,
                                                                bidQuery.SortDirection,
                                                                bidQuery.PageNumber,
                                                                bidQuery.ResultsPerPage,
                                                                m_listings,
                                                                ref numberOfResults,
																bidQuery.Player),
                                bidQuery.Player,
                                AHServerMessageType.RETURN_CHARACTER_BID_QUERY,
                                numberOfResults);
                }

                m_bidQueries.Clear();
            }
        }

        #endregion

        #region Incoming Client Functions

        // CancelListing                                         //
        // Converts the incoming message into an AHCancel object //
        private void CancelListing(NetIncomingMessage msg, Player player)
        {
            // Remaining message structure
            // { int32(listingID) }

            AHCancel newCancel = new AHCancel(msg.ReadVariableInt32(), player);

            if (newCancel.IsValidRequest())
            {
                m_cancellationAttempts.Add(newCancel);
            }
        }

        // BuyOutListing                                                                                  //
        // Converts the incoming data into a AHBuyOut object - to potentially buy out an existing listing //
        public void BuyOutListing(NetIncomingMessage msg, Player player) 
        {
            // Remaining message structure
            // { int32(listingID), int32(buyerID) }

            AHBuyOut newBuyOut = new AHBuyOut(msg.ReadVariableInt32(), // read listingID
                                              msg.ReadVariableInt32(), // read buyerID
                                              player);                 // set the bidders player reference

            if (newBuyOut.IsValidRequest())
            {
                m_buyOrderAttempts.Add(newBuyOut);
            }
        }

        // BidOnListing                                                                                              //
        // Converts the incoming data into an AHBid object - for a potential bid to be placed on an existing listing //
        public void BidOnListing(NetIncomingMessage msg, Player player)
        {
            // Remaing message structure
            // { int32(listingID), int32(bidderID), int32(bidAmount) }

            AHBid newBid = new AHBid(msg.ReadVariableInt32(), // read listingID
                                     msg.ReadVariableInt32(), // read bidderID
                                     msg.ReadVariableInt32(), // read bid amount
                                     player);                 // set the buyers player reference

            if (newBid.IsValidRequest())
            {
                m_bidAttempts.Add(newBid);
            }
        }

        // Listing Functions                                                                               //
        // Converts the incoming data into an AHListing object - to be added the database as a new listing //
        public void CreateListing(NetIncomingMessage msg, Player player)
        {
            // Remaining message structure
            // { int32(itemTemplateID), int32(inventoryID), int32(itemQuantity), int32(remainingCharges), int32(duration), int32(sellerID), int32(startingBid), int32(buyOut) }

            AHListing newListing = new AHListing(-1,                                  // listingID - not set yet
                                                 msg.ReadVariableInt32(),             // read item template ID
                                                 msg.ReadVariableInt32(),             // read inventory ID
                                                 msg.ReadVariableInt32(),             // read item quantity
                                                 msg.ReadVariableInt32(),             // read remaining charges
                                                 (AHDuration)msg.ReadVariableInt32(), // read and cast the listing duration
                                                 DateTime.Now,                        // set the date time as when the server receives the request
                                                 msg.ReadVariableInt32(),             // read sellers ID
                                                 player.m_activeCharacter.Name,       // get the players active chracters name
                                                 player,                              // set the sellers player reference
                                                 msg.ReadVariableInt32(),             // read starting bid
                                                 -1,                                  // highestBid - not set yet
                                                 -1,                                  // highestBidderID - not set yet
                                                 "NONE",                              // highestBidderName - not set yet
                                                 msg.ReadVariableInt32());            // read buy out amount

            newListing.ExpiryDateTime += TimeSpan.FromHours(GetDurationInHours(newListing.Duration)); // add duration to the expiry date time

            if (newListing.IsValidListing())
            {
                m_newListings.Add(newListing);
            }
        }

        // ProcessQuery                                                 //
        // Converts the incoming network message into an AHQuery object //
        private void ProcessQuery(NetIncomingMessage msg, Player player)
        {
            // Remaining message structure
            // { int32(filterType), string(queryString), int32(sortType), int32(sortDirection), int32(minLevel), int32(maxLevel), int32(pageNumber, int32(resultsPerPage) }

            AHListingQuery newQuery = new AHListingQuery((FilterType)msg.ReadVariableInt32(),      // read the filter type
                                                         msg.ReadString(),                         // read query string
                                                         (AHSortType)msg.ReadVariableInt32(),      // read sort type
                                                         (AHSortDirection)msg.ReadVariableInt32(), // read sort direction
                                                         msg.ReadVariableInt32(),                  // read min item level
                                                         msg.ReadVariableInt32(),                  // read max item level
                                                         msg.ReadVariableInt32(),                  // read page number
                                                         msg.ReadVariableInt32(),                  // read number of results per page 
                                                         player);                                  // set player reference

            if (newQuery.IsValidRequest())
            {
                m_queries.Add(newQuery);
            }
        }

        // ProcessCharacterQuery                                                 //
        // Converts the incoming network message into an AHCharacterQuery object //
        private void ProcessCharacterQuery(NetIncomingMessage msg, Player player)
        {
            // Remaining message structure
            // { int32(characterID), int32(pageNumber), int32(resultsPerPage) }

            AHCharacterQuery newCharacterQuery = new AHCharacterQuery(msg.ReadVariableInt32(), // read character id
                                                                      msg.ReadVariableInt32(), // read page number
                                                                      msg.ReadVariableInt32(), // read number of results per page
                                                                      player);                 // set player reference

            if (newCharacterQuery.IsValidRequest())
            {
                m_characterQueries.Add(newCharacterQuery);
            }
        }

        // ProcessBidQuery                                                       //
        // Converts the incoming network message into an AHCharacterQuery object //
        private void ProcessBidQuery(NetIncomingMessage msg, Player player)
        {
            // Remaining message structure
            // { int32(filterType), string(queryString), int32(sortType), int32(sortDirection), int32(minLevel), int32(maxLevel), int32(pageNumber, int32(resultsPerPage) }

            AHListingQuery newBidQuery = new AHListingQuery((FilterType)msg.ReadVariableInt32(),      // read the filter type
                                                            msg.ReadString(),                         // read query string
                                                            (AHSortType)msg.ReadVariableInt32(),      // read sort type
                                                            (AHSortDirection)msg.ReadVariableInt32(), // read sort direction
                                                            msg.ReadVariableInt32(),                  // read min item level
                                                            msg.ReadVariableInt32(),                  // read max item level
                                                            msg.ReadVariableInt32(),                  // read page number
                                                            msg.ReadVariableInt32(),                  // read number of results per page 
                                                            player);                                  // set player reference

            if (newBidQuery.IsValidRequest())
            {
                m_bidQueries.Add(newBidQuery);
            }
        }

        #endregion

        #region Client Bound Functions

        // SendClienResponse                                                                     //
        // Generic function to send messages to client                                           //
        // When sending messages that alter the number of slots used - the value is sent as well //
        private void SendClientResponse(AHServerMessageType messageType, Player player, String message)
        {
            #region Null Checks

            if (player.connection == null)
            {
                Program.Display("AuctionHouseManager.cs - SendClientResponse() a player connection was null");
                return;
            }

            #endregion

            NetOutgoingMessage auctionHouseQueryMsg = Program.Server.CreateMessage();
            auctionHouseQueryMsg.WriteVariableUInt32((uint)NetworkCommandType.AuctionHouse);
            auctionHouseQueryMsg.WriteVariableInt32((int)messageType);
            auctionHouseQueryMsg.Write(message);
            Program.processor.SendMessage(auctionHouseQueryMsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.AuctionHouse);
        }

        #endregion

        #region Private Functions

        // ReturnQuery                                                                                   //
        // Creates and sends a message that is returning queried data to the client                      //
        // If it is a character query, an addition int (the number of ah slots used) is pulled and added //
        private void ReturnQuery(IList<AHListing> list, Player player, AHServerMessageType queryType, int totalResults)
        {
            #region Null Checks

            if (player == null)
            {
                Program.Display("AuctionHouseManager.cs - ReturnQuery() a player was null");
                return;
            }

            if (player.m_activeCharacter == null)
            {
                Program.Display("AuctionHouseManager.cs - ReturnQuery() an active character was null");
                return;
            }

            if (player.connection == null)
            {
                Program.Display("AuctionHouseManager.cs - ReturnQuery() a player connection was null");
                return;
            }

            #endregion

            NetOutgoingMessage auctionHouseQueryMsg = Program.Server.CreateMessage();
            auctionHouseQueryMsg.WriteVariableUInt32((uint)NetworkCommandType.AuctionHouse);

            if (list.Count > 0)
            {
                auctionHouseQueryMsg.WriteVariableInt32((int)queryType);
                auctionHouseQueryMsg.WriteVariableInt32(totalResults);
                auctionHouseQueryMsg.WriteVariableInt32(list.Count);
                foreach (AHListing queryListing in list)
                {
                    auctionHouseQueryMsg.WriteVariableInt32(queryListing.ListingID);                             // listing id (int)
                    auctionHouseQueryMsg.WriteVariableInt32(queryListing.ItemTemplateID);                        // item template (int)
                    auctionHouseQueryMsg.WriteVariableInt32(queryListing.ItemQuantity);                          // item quantity (int)
                    TimeSpan timeLeft = queryListing.ExpiryDateTime - DateTime.Now;
                    auctionHouseQueryMsg.Write(timeLeft.ToString());                                             // timeleft (TimeSpan as string) 
                    auctionHouseQueryMsg.Write(queryListing.SellerName);                                         // seller name (string)
                    auctionHouseQueryMsg.WriteVariableInt32(queryListing.CurrentBid != -1 ?
                                                            queryListing.CurrentBid : queryListing.StartingBid); // current bid (int)
                    auctionHouseQueryMsg.Write(queryListing.HighestBidderName);                                  // highest bidder name (string)
                    auctionHouseQueryMsg.WriteVariableInt32(queryListing.BuyOut);                                // buy out (int)
                }

                if (queryType == AHServerMessageType.RETURN_CHARACTER_QUERY)
                {
                    auctionHouseQueryMsg.WriteVariableInt32(list.Count);
                    auctionHouseQueryMsg.WriteVariableInt32(player.m_activeCharacter.m_numberOfExtraAHSlots);
                }
            }
            else
            {
                auctionHouseQueryMsg.WriteVariableInt32((int)AHServerMessageType.FAILED_QUERY);
                auctionHouseQueryMsg.WriteVariableInt32(list.Count);
                auctionHouseQueryMsg.WriteVariableInt32(player.m_activeCharacter.m_numberOfExtraAHSlots);
            }

            Program.processor.SendMessage(auctionHouseQueryMsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.AuctionHouse);
        }

        // GetDeposit                                                                                    //
        // Takes in the bid amount of a listing and returns the deposit which is a percentage of the bid //
        // Checks and enforces a minimum deposit                                                         //
        private int GetDeposit(int bid, AHDuration duration)
        {
            int   deposit    = 0;
            float multiplier = 0.01f;

            switch (duration)
            {
                case (AHDuration.LONG):
                {
                    multiplier = AuctionHouseParams.LongMultiplier;
                    break;
                }
                case (AHDuration.MEDIUM):
                {
                    multiplier = AuctionHouseParams.MediumMultiplier;
                    break;
                }
                case (AHDuration.SHORT):
                {
                    multiplier = AuctionHouseParams.ShortMultiplier;
                    break;
                }
                default:
                {
                    Program.Display(String.Format("AuctionHouse.cs - GetDeposit() received an unknown AHDuration - ({0})", (int)duration));
                    break;
                }
            }

            deposit = (int)Math.Round((bid * multiplier), 0);

            if (deposit < AuctionHouseParams.MinimumDeposit)
            {
                deposit = AuctionHouseParams.MinimumDeposit;
            }

            return deposit;
        }
 
        // GetDurationInHours                                                    //
        // Returns the listings duration in hours (the values can be configured) //
        private int GetDurationInHours(AHDuration duration)
        {
            switch (duration)
            {
                case (AHDuration.LONG):
                {
                    return AuctionHouseParams.LongDuration;
                }
                case (AHDuration.MEDIUM):
                {
                    return AuctionHouseParams.MediumDuration;
                }
                case (AHDuration.SHORT):
                {
                    return AuctionHouseParams.ShortDuration;
                }
                default:
                {
                    Program.Display("AuctionHouseDatabase.cs - GetDurationInHours() received an incorrect type");
                    return 0;
                }
            }
        }

        #endregion
    }
}