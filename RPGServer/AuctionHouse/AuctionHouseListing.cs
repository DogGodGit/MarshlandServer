#region Includes

// Includes //
using System;
using System.Timers;
using MainServer.AuctionHouse.Enums;

#endregion

namespace MainServer.AuctionHouse
{
    #region Auction House Listing Description

    // AHListing Class                                                                                           //
    // Container class for an auction houses listing variables                                                   //
    // Design for the Auction House database table "auction_house_listings"                                      //
    // This table would have the following columns:                                                              //
    // --------------------------------------------------------------------------------------------------------- //
    // Column Name             Type           Not Null   Description                                             //
    // "listing_ID"          - int(11)      - x        - a unique identifier for the listing                     //
    // "item_template_ID"    - int(11)      - x        - items template id                                       //
    // "item_quantity"       - int(11)      - x        - quantity of the above item tempalte id                  //
    // "expiry_date_time"    - DateTime     - x        - the datetime when the item will expire                  //
    // "seller_ID"           - int(11)      - x        - sellers character server id                             //
    // "seller_name"         - varchar(15)  - x        - sellers name                                            //
    // "token_used"          - int(1)       - x        - ah token used flag                                      //
    // "starting_bid"        - int(11)      - x        - starting bid                                            //
    // "current_bid"         - int(11)      - x        - current bid                                             //
    // "highest_bidder_ID"   - int(11)      - x        - highest bidders character server id                     //
    // "highest_bidder_name" - varchar(15)  - x        - highest bidders name                                    //
    // "buy_out"             - int(11)      - x        - buyout                                                  //
    // "completion_time"     - DateTime     - o        - the datetime where the listing was flagged as completed //
    // "completed"           - int(1)       - x        - flag for listings completion                            //
    // --------------------------------------------------------------------------------------------------------- //
    // completion_time is the only field which is not initialized                                                //
    // current_bid, highest_bidder_ID and buy_out can be initialised as -1                                       //
    // --------------------------------------------------------------------------------------------------------- //
    // m_inventoryID, m_duration, m_ahTokenInvID and m_player                                                    //
    // - are not part of the SQl listing but are required during the create listing process                      //
    // --------------------------------------------------------------------------------------------------------- //
    // IsValid() checks for errors within the basic listing details (does not require database/server checks)    //

    #endregion

    internal class AHListing
    {
        #region Properties

        internal int        ListingID         { get; set; }
        internal int        ItemTemplateID    { get; set; }
        internal int        InventoryID       { get; set; }
        internal int        ItemQuantity      { get; set; }
        internal Item       Item              { get; set; }
        internal AHDuration Duration          { get; set; }
        internal DateTime   ExpiryDateTime    { get; set; }
        internal int        SellerID          { get; set; }
        internal string     SellerName        { get; set; }
        internal Player     Player            { get; set; }
        internal int        StartingBid       { get; set; }
        internal int        CurrentBid        { get; set; }
        internal int        HighestBidderID   { get; set; }
        internal string     HighestBidderName { get; set; }
        internal int        BuyOut            { get; set; }

        // Getters for the required variables within the listings Item //
        internal string ItemName(int languageID)
        {
			return Item != null ? Item.m_template.m_loc_item_name[languageID] : String.Empty;
			//return Item != null ? Item.m_template.m_item_name : String.Empty;
		}
        internal int ItemLevel() 
        {
            return Item != null ? Item.m_template.GetMinLevel() : -1;
        }
        internal int ItemEquipSlot()
        {
            return Item != null ? Item.m_template.m_slotNumber : -2;
        }

        private const int MAX_GOLD = 999999999;

        #endregion

        #region Constructors

        // AHListing                                                                                        //
        // Constructor using passed variables                                                               //
        // Used by the Auction House Manager to store incoming listing requests to be added as new listings //
        // Requires three unique variables:                                                                 //
        // - m_inventoryID  - this is used to find and remove the listing item from the players inventory   //
        // - m_ahTokenInvID - as above but specifically for the Auction House tokens                        //
        // - m_player       - reference to the player to allow for the actions above                        //
        internal AHListing(int listingID, int itemTemplateID, int inventoryID, int itemQuantity, int remainingCharges, AHDuration duration, DateTime expiryDateTime,
                           int sellerID, string sellerName, Player player, int startingBid, int currentBid, int highestBidderID, string highestBidderName, int buyOut)
        {
            ListingID         = listingID;
            ItemTemplateID    = itemTemplateID;
            InventoryID       = inventoryID;
            ItemQuantity      = itemQuantity;
            Item              = new Item(InventoryID, ItemTemplateID, ItemQuantity, -1, remainingCharges);
            Duration          = duration;
            ExpiryDateTime    = expiryDateTime;
            SellerID          = sellerID;
            SellerName        = sellerName;
            Player            = player;
            StartingBid       = startingBid;
            CurrentBid        = currentBid;
            HighestBidderID   = highestBidderID;
            HighestBidderName = highestBidderName;
            BuyOut            = buyOut;
        }

        // AHListing                                                                                    //
        // Constructor using SQL query                                                                  //
        // Used when pulling down rows from the SQL table at startup                                    //
        // m_inventoryID, m_player and m_ahTokenInvID are not set as they are not part of the SQL table //
        internal AHListing(SqlQuery query)
        {
            ListingID         = query.GetInt32("listing_ID");
            ItemTemplateID    = query.GetInt32("item_template_ID");
            InventoryID       = -1;
            ItemQuantity      = query.GetInt32("item_quantity");
            Item              = new Item(InventoryID, ItemTemplateID, ItemQuantity, -1);
            ExpiryDateTime    = query.GetDateTime("expiry_date_time");
            Duration          = (AHDuration)query.GetInt32("duration");
            SellerID          = query.GetInt32("seller_ID");
            SellerName        = query.GetString("seller_name");
            Player            = null; 
            StartingBid       = query.GetInt32("starting_bid");
            CurrentBid        = query.GetInt32("current_bid");
            HighestBidderID   = query.GetInt32("highest_bidder_ID");
            HighestBidderName = query.GetString("highest_bidder_name");
            BuyOut            = query.GetInt32("buy_out");
        }

        #endregion

        #region IsValid

        // IsValid                                                                //
        // Checks that:                                                           //
        // - the listings item is not null                                        //
        // - the quantity is not less than one                                    //
        // - if there is a buy out set, that the starting bid is not more than it //
        internal bool IsValidListing()
        {
            bool isValid = true;

            if (Item == null)
            {
                Program.Display("AuctionHouseListing.cs - IsValidlisting() an AHListings item was null!");
                isValid = false;
            }

            if (ItemQuantity < 1)
            {
                Program.Display("AuctionHouseListing.cs - IsValidlisting() an AHListings quantity is less than one!");
                isValid = false;
            }

            if (StartingBid < 1)
            {
                Program.Display("AuctionHouseListing.cs - IsValidlisting() an AHListings starting bid was zero or less!");
                isValid = false;
            }

            if (StartingBid > MAX_GOLD)
            {
                Program.Display("AuctionHouseListing.cs - IsValidlisting() an AHListings starting bid was a billion or more!");
                isValid = false;
            }

            if (BuyOut != -1 && BuyOut < 1)
            {
                // invalid buyout?
            }

            if (BuyOut != -1 && StartingBid >= BuyOut)
            {
                Program.Display("AuctionHouseListing.cs - IsValidlisting() an AHListings starting bid is greater than its buy out!");
                isValid = false;
            }

            if (BuyOut > MAX_GOLD)
            {
                Program.Display("AuctionHouseListing.cs - IsValidlisting() an AHListings buy out was a billion or more!");
                isValid = false;
            }

            return isValid;
        }

        #endregion
    };
}