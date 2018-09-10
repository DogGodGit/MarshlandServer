#region Includes

// Includes //
using System;
using MainServer.AuctionHouse.Enums;

#endregion

namespace MainServer.AuctionHouse
{
    #region Auction House Request

    // AHRequest           //
    // Abstract Base Class //
    internal abstract class AHRequest
    {
        // Properties //
        internal Player Player { get; set; }

        // IsValidRequest //
        internal virtual bool IsValidRequest()
        {
            if (Player == null)
            {
                Program.Display("AuctionHouseRequests.cs - IsValidRequest() an AHRequest had a null Player reference");
                return false;
            }

            if (Player.m_activeCharacter == null)
            {
                Program.Display("AuctionHouseRequests.cs - IsValidRequest() an AHRequest had a null activeCharacter reference");
                return false;
            }

            return true;
        }
    };

    #endregion

    #region Auction House BuyOut

    // AHBuyOut                                                    //
    // Container class to hold an incoming Auction House buy order //
    internal class AHBuyOut : AHRequest
    {
        // Properties //
        internal int ListingID { get; set; }
        internal int BuyerID   { get; set; }

        // Constructor //
        internal AHBuyOut(int listingID, int buyerID, Player player)
        {
            ListingID = listingID;
            BuyerID   = buyerID;
            Player    = player;
        }

        // IsValidRequest //
        internal override bool IsValidRequest()
        {
            return base.IsValidRequest();
        }
    };

    #endregion

    #region Auction House Bid
     
    // AHBid                                                 //
    // Container class to hold an incoming Auction House bid //
    internal class AHBid : AHRequest
    {
        // Properties //
        internal int ListingID { get; set; }
        internal int BidderID  { get; set; }
        internal int BidAmount { get; set; }

        // Constructor //
        internal AHBid(int listingID, int bidderID, int bidAmount, Player player)
        {
            ListingID = listingID;
            BidderID  = bidderID;
            BidAmount = bidAmount;
            Player    = player;
        }

        // IsValidRequest //
        internal override bool IsValidRequest()
        {
            return base.IsValidRequest();
        }
    };

    #endregion 

    #region Auction House Query

    // AHQuery                                                 //
    // Base Class for AHListing and AHCharacterListing classes //
    internal class AHQuery : AHRequest
    {
        // Variable getters & setters //
        internal AHSortType      SortType      { get; set; }
        internal AHSortDirection SortDirection { get; set; }

        // Constructor //
        internal AHQuery(AHSortType sortType, AHSortDirection sortDirection, Player player)
        {
            SortType      = sortType;
            SortDirection = sortDirection;
            Player        = player;
        }

        // IsValidRequest                                     //
        // Checks if the AHQuery objects data is valid or not //
        internal override bool IsValidRequest()
        {
            bool isValid = true;

            if (SortType == AHSortType.NONE && SortDirection != AHSortDirection.NONE)
            {
                Program.Display("AuctionHouseRequests.cs - IsValidRequest() an AHQuery had no sort type but had a sort direction");
                isValid = false;
            }

            isValid = base.IsValidRequest();

            return isValid;
        }
    };

    #endregion

    #region Auction House ListingQuery

    // AHListingQuery                                                          //
    // Container class to hold an incoming Auction House listing query request //
    internal class AHListingQuery : AHQuery
    {
        // Properties //
        internal FilterType ItemFilter     { get; set; }
        internal string     QueryString    { get; set; }
        internal int        MinLevel       { get; set; }
        internal int        MaxLevel       { get; set; }
        internal int        PageNumber     { get; set; }
        internal int        ResultsPerPage { get; set; }

        // Constructor //
        internal AHListingQuery(FilterType itemFilter, string queryString, AHSortType sortType, AHSortDirection sortDirection, int minLevel, int maxLevel, int pageNumber, int resultPerPage, Player player) :
            base(sortType, sortDirection, player)
        {
            ItemFilter     = itemFilter;
            QueryString    = queryString;
            MinLevel       = minLevel;
            MaxLevel       = maxLevel;
            PageNumber     = pageNumber;
            ResultsPerPage = resultPerPage;
        }

        // IsValidRequest                                                  //
        // Checks if the AHQuery objects data is valid or not              //
        // A query must have either a search name or a item type (or both) //
        internal override bool IsValidRequest()
        {
            bool isValid = true;

            if (ItemFilter == FilterType.All && QueryString == String.Empty)
            {
                Program.Display("AuctionHouseRequests.cs - IsValidRequest() an AHQuery had both no search name and no search type - not returning the whole auction house sorry!");
                isValid = false;
            }

            if (MinLevel > MaxLevel)
            {
                Program.Display("AuctionHouseRequests.cs - IsValidRequest() an AHQuery had a min level greater than its max level");
                isValid = false;
            }

            isValid = base.IsValidRequest();

            return isValid;
        }
    };

    #endregion

    #region Auction House CharacterQuery

    // AHCharacterQuery                                                          //
    // Container class to hold an incoming Auction House character query request //
    internal class AHCharacterQuery : AHQuery
    {
        // Properties //
        internal int CharacterID    { get; set; }
        internal int PageNumber     { get; set; }
        internal int ResultsPerPage { get; set; }

        // Constructor //
        internal AHCharacterQuery(int characterID, int pageNumber, int resultsPerPage, Player player) :
            base(AHSortType.EXPIRY_DATE_TIME, AHSortDirection.ASC, player)
        {
            CharacterID    = characterID;
            PageNumber     = pageNumber;
            ResultsPerPage = resultsPerPage;
        }

        // IsValidRequest //
        internal override bool IsValidRequest()
        {
            return base.IsValidRequest();
        }
    };

    #endregion

    #region Auction House Cancel

    // AHCancel                                                                 //
    // Container class to hold an incoming Auction House listing cancel request //
    internal class AHCancel : AHRequest
    {
        // Properties //
        internal int ListingID { get; set; }

        // Constructor //
        internal AHCancel(int listingID, Player player)
        {
            ListingID = listingID;
            Player    = player;
        }
    };

    #endregion
}