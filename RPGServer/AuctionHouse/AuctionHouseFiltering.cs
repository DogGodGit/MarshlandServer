#region Includes

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Collections;
using MainServer.AuctionHouse.Enums;
using MainServer.Localise;

#endregion

namespace MainServer.AuctionHouse
{
    internal static class AuctionHouseFiltering
    {
        // GetListing                                               //
        // Returns a specific listing or null if it cannot be found //
        internal static AHListing GetListing(int listingID, IList<AHListing> list)
        {
            AHListing[] returnListings = list.Where(listing => listing.ListingID == listingID).ToArray();
            return returnListings.Length == 1 ? returnListings[0] : null;
        }

        // GetMyBids                                                                                                                       //
        // Returns listings for which the passed character is the highest bidder OR whose listing ids are contained within the passed list //
        // Has the same functionality for filtering, sorting and paging as QueryListings()                                                 //
        internal static IList<AHListing> GetMyBids(int characterServerID, IList<int> outBidListingIDs, 
                                                   FilterType filterBy, string queryString, int minLevel, int maxLevel, 
                                                   AHSortType sortType, AHSortDirection sortDirection, 
                                                   int pageNumber, int resultsPerPage,
                                                   IList<AHListing> auctionHouseListings, ref int numberOfResults, Player player)
        {
            IList<AHListing> myBidListings;

            if (outBidListingIDs != null)
            {
                myBidListings = auctionHouseListings.Where(listing => (listing.HighestBidderID == characterServerID || outBidListingIDs.Contains(listing.ListingID))).ToList();
            }
            else
            {
                myBidListings = auctionHouseListings.Where(listing => listing.HighestBidderID == characterServerID).ToList();
            }
			
            myBidListings   = FilterListings(filterBy, queryString, minLevel, maxLevel, myBidListings, player.m_languageIndex); // filter
            numberOfResults = myBidListings.Count;																				// results
            myBidListings   = SortListings(sortType, sortDirection, myBidListings, player.m_languageIndex);						// sort
            myBidListings   = PageListings(pageNumber, resultsPerPage, myBidListings);											// page

            return myBidListings;
        }

        // GetNumberOfActiveListings                                         //
        // Returns the number of listings the passed character id has active //
        internal static int GetNumberOfActiveListings(int characterID, IList<AHListing> list)
        {
            AHListing[] returnListings = list.Where(listing => listing.SellerID == characterID).ToArray();
            return returnListings.Length;
        }

        // GetMyListings                                                 //
        // Returns a (sorted) list of a specific character id's listings //
        internal static IList<AHListing> GetMyListings(int characterID, AHSortType sortType, AHSortDirection sortDirection, int pageNumber, int resultsPerPage, IList<AHListing> list, ref int numberOfResults, Player player)
        {
            IList<AHListing> myListings = list.Where(listing => listing.SellerID == characterID).ToList();
            numberOfResults             = myListings.Count;
            myListings                  = SortListings(sortType, sortDirection, myListings, player.m_languageIndex);    // sort
            myListings                  = PageListings(pageNumber, resultsPerPage, myListings); // page

            return myListings;
        }

        // QueryListings                                                                                                                                       //
        // New main function which returns a query list of AHListings by first filtering, then sorting and finally paging the full auction house listings list //
        internal static IList<AHListing> QueryListings(FilterType filterBy, string queryString, int minLevel, int maxLevel, 
                                                       AHSortType sortType, AHSortDirection sortDirection, 
                                                       int pageNumber, int resultsPerPage,
                                                       IList<AHListing> auctionHouseListings, ref int numberOfResults, Player player)
        {
            IList<AHListing> queryReturnList = new List<AHListing>();
            
            queryReturnList = FilterListings(filterBy, queryString, minLevel, maxLevel, auctionHouseListings, player.m_languageIndex); // filter
            numberOfResults = queryReturnList.Count;                                                           // results
            queryReturnList = SortListings(sortType, sortDirection, queryReturnList, player.m_languageIndex);  // sort
            queryReturnList = PageListings(pageNumber, resultsPerPage, queryReturnList);                       // page

            return queryReturnList;
        }

        // FilterListings                                                                          //
        // Old main filter call                                                                    //
        // Added an initial filter by item level                                                   //
        // Added missing keywords for ring/bangle/neck and misc slots                              //
        // Differs slightly from its implementation in client InventoryFiltering.cs:               //
        // - uses AHListing objects, so the item is accessed through AHListing.Item                //
        // - client                                  / server                                      //
        // - m_itemTemplate                          / m_template                                  //
        // - (int) m_itemTemplate.m_classTypes       / (CLASS_TYPE) m_template.m_classRestrictions //
        // - CInventory                              / Inventory                                   //
        // - (EQUIP_SLOT) m_itemTemplate.m_EquipSlot / (int) m_template.m_slotNumber               //
        // - List<>                                  / IList<>                                     //
        // - .FindAll()                              / .Where().ToList()                           //
        private static IList<AHListing> FilterListings(FilterType filterBy, string queryString, int minLevel, int maxLevel, IList<AHListing> auctionHouseListings, int langaugeID)
        {
            IList<AHListing> filteredListings = new List<AHListing>();

            foreach (AHListing listing in auctionHouseListings)
            {
                if (minLevel > -1 && maxLevel > -1)
                {
                    int itemLevel = listing.ItemLevel();
                    if (itemLevel >= minLevel && itemLevel <= maxLevel)
                    {
                        if (Filter(filterBy, listing) == true)
                        {
                            filteredListings.Add(listing);
                        }
                    }
                }
                else
                {
                    if (Filter(filterBy, listing) == true)
                    {
                        filteredListings.Add(listing);
                    }
                }
            }
 
            if (String.IsNullOrEmpty(queryString) == false)
            {
                queryString = queryString.ToLower(); 

                #region +/- trade

                if (queryString.Contains("+trade"))
                {
                    filteredListings = filteredListings.Where(x => !(x.Item.m_template.m_noTrade == true || x.Item.m_bound == true)).ToList();
                    queryString = queryString.Replace("+trade", String.Empty);
                }
                if (queryString.Contains("-trade"))
                {
                    filteredListings = filteredListings.Where(x => x.Item.m_template.m_noTrade == true || x.Item.m_bound == true).ToList();
                    queryString = queryString.Replace("-trade", String.Empty);
                }
                #endregion

                #region +/- class ... common values.class_type = UNDEFINED = 0, WARRIOR = 1,DRUID = 2, MAGE = 3, RANGER = 4, ROGUE = 5

                // +/- warrior
                if (queryString.Contains("+warrior"))
                {
                    filteredListings = filteredListings.Where(x => x.Item.m_template.m_classRestrictions.Contains((CLASS_TYPE)1)).ToList();
                    queryString = queryString.Replace("+warrior", String.Empty);
                }
                if (queryString.Contains("-warrior"))
                {
                    filteredListings = filteredListings.Where(x => !x.Item.m_template.m_classRestrictions.Contains((CLASS_TYPE)1)).ToList();
                    queryString = queryString.Replace("-warrior", String.Empty);
                }

                // +/- druid
                if (queryString.Contains("+druid"))
                {
                    filteredListings = filteredListings.Where(x => x.Item.m_template.m_classRestrictions.Contains((CLASS_TYPE)2)).ToList();
                    queryString = queryString.Replace("+druid", String.Empty);
                }
                if (queryString.Contains("-druid"))
                {
                    filteredListings = filteredListings.Where(x => !x.Item.m_template.m_classRestrictions.Contains((CLASS_TYPE)2)).ToList();
                    queryString = queryString.Replace("-druid", String.Empty);
                }

                // +/- mage
                if (queryString.Contains("+mage"))
                {
                    filteredListings = filteredListings.Where(x => x.Item.m_template.m_classRestrictions.Contains((CLASS_TYPE)3)).ToList();
                    queryString = queryString.Replace("+mage", String.Empty);
                }
                if (queryString.Contains("-mage"))
                {
                    filteredListings = filteredListings.Where(x => !x.Item.m_template.m_classRestrictions.Contains((CLASS_TYPE)3)).ToList();
                    queryString = queryString.Replace("-mage", String.Empty);
                }

                // +/- ranger
                if (queryString.Contains("+ranger"))
                {
                    filteredListings = filteredListings.Where(x => x.Item.m_template.m_classRestrictions.Contains((CLASS_TYPE)4)).ToList();
                    queryString = queryString.Replace("+ranger", String.Empty);
                }
                if (queryString.Contains("-ranger"))
                {
                    filteredListings = filteredListings.Where(x => !x.Item.m_template.m_classRestrictions.Contains((CLASS_TYPE)4)).ToList();
                    queryString = queryString.Replace("-ranger", String.Empty);
                }

                // +/- rogue
                if (queryString.Contains("+rogue"))
                {
                    filteredListings = filteredListings.Where(x => x.Item.m_template.m_classRestrictions.Contains((CLASS_TYPE)5)).ToList();
                    queryString = queryString.Replace("+rogue", String.Empty);
                }
                if (queryString.Contains("-rogue"))
                {
                    filteredListings = filteredListings.Where(x => !x.Item.m_template.m_classRestrictions.Contains((CLASS_TYPE)5)).ToList();
                    queryString = queryString.Replace("-rogue", String.Empty);
                }

                #endregion

                #region +/- slots & weapon types e.g. main, offhand

                // +/- ring
                if (queryString.Contains("+ring"))
                {
                    filteredListings = filteredListings.Where(x => (x.Item.m_template.m_slotNumber == (int)Inventory.EQUIP_SLOT.SLOT_RING_R1)).ToList();
                    queryString = queryString.Replace("+ring", String.Empty);
                }
                if (queryString.Contains("-ring"))
                {
                    filteredListings = filteredListings.Where(x => (x.Item.m_template.m_slotNumber != (int)Inventory.EQUIP_SLOT.SLOT_RING_R1)).ToList();
                    queryString = queryString.Replace("-ring", String.Empty);
                }

                // +/- bangle
                if (queryString.Contains("+bangle"))
                {
                    filteredListings = filteredListings.Where(x => (x.Item.m_template.m_slotNumber == (int)Inventory.EQUIP_SLOT.SLOT_BANGLE_R)).ToList();
                    queryString = queryString.Replace("+bangle", String.Empty);
                }
                if (queryString.Contains("-bangle"))
                {
                    filteredListings = filteredListings.Where(x => (x.Item.m_template.m_slotNumber != (int)Inventory.EQUIP_SLOT.SLOT_BANGLE_R)).ToList();
                    queryString = queryString.Replace("-bangle", String.Empty);
                }

                // +/- neck
                if (queryString.Contains("+neck"))
                {
                    filteredListings = filteredListings.Where(x => (x.Item.m_template.m_slotNumber == (int)Inventory.EQUIP_SLOT.SLOT_NECK)).ToList();
                    queryString = queryString.Replace("+neck", String.Empty);
                }
                if (queryString.Contains("-neck"))
                {
                    filteredListings = filteredListings.Where(x => (x.Item.m_template.m_slotNumber != (int)Inventory.EQUIP_SLOT.SLOT_NECK)).ToList();
                    queryString = queryString.Replace("-neck", String.Empty);
                }

                // +/- misc
                if (queryString.Contains("+misc"))
                {
                    filteredListings = filteredListings.Where(x => (x.Item.m_template.m_slotNumber == (int)Inventory.EQUIP_SLOT.SLOT_MISC)).ToList();
                    queryString = queryString.Replace("+misc", String.Empty);
                }
                if (queryString.Contains("-misc"))
                {
                    filteredListings = filteredListings.Where(x => (x.Item.m_template.m_slotNumber != (int)Inventory.EQUIP_SLOT.SLOT_MISC)).ToList();
                    queryString = queryString.Replace("-misc", String.Empty);
                }

                // +/- hands
                if (queryString.Contains("+hands"))
                {
                    filteredListings = filteredListings.Where(x => (x.Item.m_template.m_slotNumber == (int)Inventory.EQUIP_SLOT.SLOT_HANDS
                                                                   || x.Item.m_template.m_slotNumber == (int)Inventory.EQUIP_SLOT.SLOT_FASH_HANDS)).ToList();
                    queryString = queryString.Replace("+hands", String.Empty);
                }
                if (queryString.Contains("-hands"))
                {
                    filteredListings = filteredListings.Where(x => !(x.Item.m_template.m_slotNumber == (int)Inventory.EQUIP_SLOT.SLOT_HANDS
                                                                    || x.Item.m_template.m_slotNumber == (int)Inventory.EQUIP_SLOT.SLOT_FASH_HANDS)).ToList();
                    queryString = queryString.Replace("-hands", String.Empty);
                }

                // +/- feet
                if (queryString.Contains("+feet"))
                {
                    filteredListings = filteredListings.Where(x => (x.Item.m_template.m_slotNumber == (int)Inventory.EQUIP_SLOT.SLOT_FEET
                                                                   || x.Item.m_template.m_slotNumber == (int)Inventory.EQUIP_SLOT.SLOT_FASH_FEET)).ToList();
                    queryString = queryString.Replace("+feet", String.Empty);
                }
                if (queryString.Contains("-hands"))
                {
                    filteredListings = filteredListings.Where(x => !(x.Item.m_template.m_slotNumber == (int)Inventory.EQUIP_SLOT.SLOT_FEET
                                                                    || x.Item.m_template.m_slotNumber == (int)Inventory.EQUIP_SLOT.SLOT_FASH_FEET)).ToList();
                    queryString = queryString.Replace("-hands", String.Empty);
                }

                // +/- head
                if (queryString.Contains("+head"))
                {
                    filteredListings = filteredListings.Where(x => (x.Item.m_template.m_slotNumber == (int)Inventory.EQUIP_SLOT.SLOT_HEAD
                                                                   || x.Item.m_template.m_slotNumber == (int)Inventory.EQUIP_SLOT.SLOT_FASH_HEAD)).ToList();
                    queryString = queryString.Replace("+head", String.Empty);
                }
                if (queryString.Contains("-head"))
                {
                    filteredListings = filteredListings.Where(x => !(x.Item.m_template.m_slotNumber == (int)Inventory.EQUIP_SLOT.SLOT_HEAD
                                                                    || x.Item.m_template.m_slotNumber == (int)Inventory.EQUIP_SLOT.SLOT_FASH_HEAD)).ToList();
                    queryString = queryString.Replace("-head", String.Empty);
                }

                // +/- torso
                if (queryString.Contains("+torso"))
                {
                    filteredListings = filteredListings.Where(x => (x.Item.m_template.m_slotNumber == (int)Inventory.EQUIP_SLOT.SLOT_CHEST
                                                                   || x.Item.m_template.m_slotNumber == (int)Inventory.EQUIP_SLOT.SLOT_FASH_TORSO)).ToList();
                    queryString = queryString.Replace("+torso", String.Empty);
                }
                if (queryString.Contains("-torso"))
                {
                    filteredListings = filteredListings.Where(x => !(x.Item.m_template.m_slotNumber == (int)Inventory.EQUIP_SLOT.SLOT_CHEST
                                                                    || x.Item.m_template.m_slotNumber == (int)Inventory.EQUIP_SLOT.SLOT_FASH_TORSO)).ToList();
                    queryString = queryString.Replace("-torso", String.Empty);
                }

                // +/- legs
                if (queryString.Contains("+legs"))
                {
                    filteredListings = filteredListings.Where(x => (x.Item.m_template.m_slotNumber == (int)Inventory.EQUIP_SLOT.SLOT_LEG
                                                                   || x.Item.m_template.m_slotNumber == (int)Inventory.EQUIP_SLOT.SLOT_FASH_LEGS)).ToList();
                    queryString = queryString.Replace("+legs", String.Empty);
                }
                if (queryString.Contains("-legs"))
                {
                    filteredListings = filteredListings.Where(x => !(x.Item.m_template.m_slotNumber == (int)Inventory.EQUIP_SLOT.SLOT_LEG
                                                                    || x.Item.m_template.m_slotNumber == (int)Inventory.EQUIP_SLOT.SLOT_FASH_LEGS)).ToList();
                    queryString = queryString.Replace("-legs", String.Empty);
                }

                // +/- mainhand
                if (queryString.Contains("+mainhand"))
                {
                    filteredListings = filteredListings.Where(x => x.Item.m_template.m_slotNumber == (int)Inventory.EQUIP_SLOT.SLOT_WEAPON).ToList();
                    queryString = queryString.Replace("+mainhand", String.Empty);
                }
                if (queryString.Contains("-mainhand"))
                {
                    filteredListings = filteredListings.Where(x => x.Item.m_template.m_slotNumber != (int)Inventory.EQUIP_SLOT.SLOT_WEAPON).ToList();
                    queryString = queryString.Replace("-mainhand", String.Empty);
                }

                // +/- offhand
                if (queryString.Contains("+offhand"))
                {
                    filteredListings = filteredListings.Where(x => x.Item.m_template.m_slotNumber == (int)Inventory.EQUIP_SLOT.SLOT_OFFHAND).ToList();
                    queryString = queryString.Replace("+offhand", String.Empty);
                }
                if (queryString.Contains("-offhand"))
                {
                    filteredListings = filteredListings.Where(x => x.Item.m_template.m_slotNumber != (int)Inventory.EQUIP_SLOT.SLOT_OFFHAND).ToList();
                    queryString = queryString.Replace("-offhand", String.Empty);
                }

                #endregion

                // For the last part of the search, want to split the remaing strings up and strip out whitespace
                // Found via google, the new char[0] causes the method to assume that  whitespac is the seperating character
                string[] individualStrings = queryString.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);

                // Repeat similar to above, assume if player types in +warrior red black they want all warrior items that are red OR black, not red AND black 
                foreach (string match in individualStrings)
                {
                    filteredListings = filteredListings.Where(x => x.Item.m_template.m_loc_item_name[langaugeID].ToLower().Contains(match)).ToList();
                }
            }

            return filteredListings;
        }

        // Filter                                                            //
        // Use the template of the item to check against our general filters //
        private static bool Filter(FilterType filterBy, AHListing listing)
        {
            if (listing.Item.m_template == null)
            {
                return false;
            }

            // We pass in params using the first one as our equip slot
            // Remaining params are slots that will pass for our filter
            switch (filterBy)
            {
                case (FilterType.Armour):
                {
                    return Filter((Inventory.EQUIP_SLOT)listing.Item.m_template.m_slotNumber,
                                   Inventory.EQUIP_SLOT.SLOT_HEAD,
                                   Inventory.EQUIP_SLOT.SLOT_CHEST,
                                   Inventory.EQUIP_SLOT.SLOT_LEG,
                                   Inventory.EQUIP_SLOT.SLOT_FEET,
                                   Inventory.EQUIP_SLOT.SLOT_HANDS);
                }
                case (FilterType.Weapons):
                {
                    return Filter((Inventory.EQUIP_SLOT)listing.Item.m_template.m_slotNumber,
                                   Inventory.EQUIP_SLOT.SLOT_OFFHAND,
                                   Inventory.EQUIP_SLOT.SLOT_WEAPON);
                }
                case (FilterType.Jewellery):
                {
                    return Filter((Inventory.EQUIP_SLOT)listing.Item.m_template.m_slotNumber,
                                   Inventory.EQUIP_SLOT.SLOT_RING_R1,
                                   Inventory.EQUIP_SLOT.SLOT_RING_R2,
                                   Inventory.EQUIP_SLOT.SLOT_RING_L1,
                                   Inventory.EQUIP_SLOT.SLOT_RING_L2,
                                   Inventory.EQUIP_SLOT.SLOT_BANGLE_L,
                                   Inventory.EQUIP_SLOT.SLOT_BANGLE_R,
                                   Inventory.EQUIP_SLOT.SLOT_MISC,
                                   Inventory.EQUIP_SLOT.SLOT_NECK);
                }
                case (FilterType.Fashion):
                {
                    return Filter((Inventory.EQUIP_SLOT)listing.Item.m_template.m_slotNumber,
                                   Inventory.EQUIP_SLOT.SLOT_FASH_HEAD,
                                   Inventory.EQUIP_SLOT.SLOT_FASH_TORSO,
                                   Inventory.EQUIP_SLOT.SLOT_FASH_LEGS,
                                   Inventory.EQUIP_SLOT.SLOT_FASH_FEET,
                                   Inventory.EQUIP_SLOT.SLOT_FASH_HANDS);
                }
                case (FilterType.Stables):
                {
                    return (Filter((Inventory.EQUIP_SLOT)listing.Item.m_template.m_slotNumber,
                                    Inventory.EQUIP_SLOT.SLOT_COMPANION,
                                    Inventory.EQUIP_SLOT.SLOT_MOUNT,
                                    Inventory.EQUIP_SLOT.SLOT_SADDLE) ||
                                    listing.Item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.PET_FOOD);
                }
                case (FilterType.Tokens):
                {
                    return listing.Item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.TOKEN;
                }
                case (FilterType.Fishing):
                {
                    return listing.Item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.FISHING_ROD ||
                           listing.Item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.FISHING_ITEM;
                }
                case (FilterType.Consumables):
                {
                    return listing.Item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.CONSUMABLE;
                }
                case (FilterType.Cooking):
                {
                    return listing.Item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.COOKING_ITEM;
                }
                case (FilterType.Other):
                {
                    return (Filter((Inventory.EQUIP_SLOT)listing.Item.m_template.m_slotNumber, Inventory.EQUIP_SLOT.SLOT_UNEQUIPABLE) // everything unequipable
                                    && listing.Item.m_template.m_subtype != ItemTemplate.ITEM_SUB_TYPE.PET_FOOD                       // but not pets
                                    && listing.Item.m_template.m_subtype != ItemTemplate.ITEM_SUB_TYPE.TOKEN                          // and not tokens
                                    && listing.Item.m_template.m_subtype != ItemTemplate.ITEM_SUB_TYPE.FISHING_ITEM                   // and not fishing bait
                                    && listing.Item.m_template.m_subtype != ItemTemplate.ITEM_SUB_TYPE.FISHING_ROD                    // and not fishing rod
                                    && listing.Item.m_template.m_subtype != ItemTemplate.ITEM_SUB_TYPE.CONSUMABLE                     // and not consumables
                                    && listing.Item.m_template.m_subtype != ItemTemplate.ITEM_SUB_TYPE.COOKING_ITEM);                 // and not cooking items as well
                }
                case (FilterType.All):
                {
                    return true;
                }
            }

            return false;
        }

        // Filter                                                                        //
        // NOTE: First param slot must be OUR slot                                       //
        // True if we are part of the remaining slots                                    //
        // "okSlots" FIRST slot must be our item slot, remaining slot/s to check against //
        private static bool Filter(params Inventory.EQUIP_SLOT[] okSlots)
        {
            if (okSlots.Length < 2)
                return false;

            // My slot to compare other with
            Inventory.EQUIP_SLOT mySlot = okSlots[0];

            // Do we match any of the desired slots
            for (int i = 1; i < okSlots.Length; i++)
            {
                if (mySlot == okSlots[i])
                    return true;
            }

            return false;
        }

        // Sort                                                               //
        // Sorts the passed list by the passed AHSortType and AHSortDirection //
        private static IList<AHListing> SortListings(AHSortType sortType, AHSortDirection sortDirection, IList<AHListing> list, int languageID)
        {
            switch (sortType)
            {
                case AHSortType.ITEM_NAME:
                {
                    return sortDirection == AHSortDirection.DESC
                           ? list.OrderByDescending(listing => listing.ItemName(languageID)).ToList()
                           : list.OrderBy(listing => listing.ItemName(languageID)).ToList();
                }
                case AHSortType.ITEM_QUANTITY:
                {
                    return sortDirection == AHSortDirection.DESC
                           ? list.OrderByDescending(listing => listing.ItemQuantity).ToList()
                           : list.OrderBy(listing => listing.ItemQuantity).ToList();
                }
                case AHSortType.ITEM_LEVEL:
                {
                    return sortDirection == AHSortDirection.DESC
                           ? list.OrderByDescending(listing => listing.ItemLevel()).ToList()
                           : list.OrderBy(listing => listing.ItemLevel()).ToList();
                }
                case AHSortType.ITEM_TYPE:
                {
                    return sortDirection == AHSortDirection.DESC
                           ? list.OrderByDescending(listing => listing.ItemEquipSlot()).ToList()
                           : list.OrderBy(listing => listing.ItemEquipSlot()).ToList();
                }
                case AHSortType.EXPIRY_DATE_TIME:
                {
                    return sortDirection == AHSortDirection.DESC
                           ? list.OrderByDescending(listing => listing.ExpiryDateTime).ToList()
                           : list.OrderBy(listing => listing.ExpiryDateTime).ToList();
                }
                case AHSortType.SELLER_ID:
                {
                    return sortDirection == AHSortDirection.DESC
                           ? list.OrderByDescending(listing => listing.SellerID).ToList()
                           : list.OrderBy(listing => listing.SellerID).ToList();
                }
                case AHSortType.CURRENT_BID:
                {
                    return sortDirection == AHSortDirection.DESC
                           ? list.OrderByDescending(listing => (listing.CurrentBid >= listing.StartingBid ? listing.CurrentBid : listing.StartingBid)).ToList()
                           : list.OrderBy(listing => (listing.CurrentBid >= listing.StartingBid ? listing.CurrentBid : listing.StartingBid)).ToList();
                }
                case AHSortType.BUY_OUT:
                {
                    return sortDirection == AHSortDirection.DESC
                           ? list.OrderByDescending(listing => listing.BuyOut).ToList()
                           : list.OrderBy(listing => listing.BuyOut).ToList();
                }
                default:
                {
                    return list;
                }
            }
        }

        // PageListings                   //
        // Sexy one-line LINQ paging ftw! //
        private static IList<AHListing> PageListings(int pageNumber, int resultsPerPage, IList<AHListing> list)
        {
            return list.Skip((pageNumber - 1) * resultsPerPage).Take(resultsPerPage).ToList();
        }
    }
}