#region Includes

// Includes //
using System;

#endregion

namespace MainServer.AuctionHouse.Enums
{
    #region Auction House Enums

    public enum AHStatus
    {
        OFFLINE,
        SAFE_MODE,
        ONLINE
    };

    public enum AHDuration 
    {
        SHORT,
        MEDIUM, 
        LONG
    };

    public enum AHClientMessageType
    {
        CANCEL_LISTING,
        CREATE_LISTING,
        PLACE_BID,
        BUY_OUT_LISTING,
        QUERY,
        GET_MY_LISTINGS,
        GET_MY_BIDS
    };

    public enum AHServerMessageType
    {
        LISTING_CREATED,
        LISTING_CANCELLED_BIDDER,
        LISTING_CANCELLED_SELLER,
        LISTING_CANCELLED_SERVER,
        LISTING_EXPIRED,
        BID_PLACED,
        LISTING_WON_BUYOUT,
        LISTING_WON_COMPLETION,
        LISTING_COMPLETED,
        LISTING_BOUGHT_OUT,
        RETURN_QUERY,
        RETURN_CHARACTER_QUERY,
        RETURN_CHARACTER_BID_QUERY,
        FAILED_CANCELLATION,
        FAILED_BUY_OR_BID,
        FAILED_LISTING,
        FAILED_QUERY,
        OUT_BID, 
        OFFLINE,
        SAFE_MODE
    };

    public enum AHMailMessageType
    {
        LISTING_CANCELLED_BIDDER,
        LISTING_CANCELLED_SELLER,
        LISTING_CANCELLED_SERVER,
        LISTING_COMPLETED,
        LISTING_BOUGHT_OUT,
        LISTING_WON,
        OUT_BID,
        LISTING_EXPIRIED,
        LISTING_BUY_OUT_FAILED,
        LISTING_BID_FAILED
    };

    public enum AHSortType
    {
        NONE,
        ITEM_NAME,
        ITEM_QUANTITY,
        ITEM_LEVEL,
        ITEM_TYPE,
        EXPIRY_DATE_TIME,
        SELLER_ID,
        CURRENT_BID,
        BUY_OUT
    };

    public enum AHSortDirection
    {
        NONE,
        ASC,
        DESC
    };

    public enum FilterType
    {
        All,
        Armour,
        Weapons,
        Jewellery,
        Fashion,
        Stables,
        Tokens,
        Fishing,
        Cooking,
        Consumables,
        Favourite,
        Other,
        Custom
    };

    #endregion
}