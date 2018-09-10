#region Includes

// Includes //
using System;
using System.Text;
using MainServer.AuctionHouse.Enums;
using MainServer.Localise;

#endregion

namespace MainServer.AuctionHouse
{
    #region Auction House Mail Manager Description

    // AHMailManager Class                                              //
    // Class which manages all of the Auction Houses mail functionality //

    #endregion

    internal class AHMailManager
    {
		// #localisation
		public class AHMailManagerTextDB : TextEnumDB
		{
			public AHMailManagerTextDB() : base(nameof(AHMailManager), typeof(TextID)) { }

			public enum TextID
			{
				AUCTION_HOUSE,							// "Auction House"
				AUCTION_CANCELLED,						// "Auction Cancelled"
				AUCTION_COMPLETE,						// "Auction Complete"
				AUCTION_BOUGHT_OUT,						// "Auction Bought Out"
				AUCTION_WON,							// "Auction Won"
				OUTBID_ON_AUCTION,						// "OutBid On Auction"
				AUCTION_EXPIRED,						// "Auction Expired"
                AUCTION_BUYOUT_FAILED,                  // "Auction Buyout Failed"
                AUCTION_BID_FAILED,                     // "Auction Bid Failed"
				AUCTION_OF_ITEM_CANCELLED_BY_SELLER,	// "The auction of: {itemName0} ({itemQuantity1}) has been cancelled by the seller \n\nYour bid of {listingGold2} gold has been returned."
				AUCTION_OF_ITEM_CANCELLED_BY_SERVER,	// "The auction of: {itemName0} ({itemQuantity1}) has been cancelled by the server \n\nYour deposit of {depositReturned4} gold has been returned."
				AUCTION_OF_ITEM_CANCELLED,				// "Your auction of: {itemName0} ({itemQuantity1}) has been cancelled"
				AUCTION_OF_ITEM_COMPLETED,				// "Your auction of: {itemName0} ({itemQuantity1}) was completed {listingGold2} gold \nAH Commission (5%): -{auctionHouseCut3} gold \nDeposit Refund: {depositReturned4} gold \nTotal Awarded: {goldAwarded5} gold"
				AUCTION_OF_ITEM_BOUGHT_OUT,				// "The auction of: {itemName0} ({itemQuantity1}) has been bought out \n\nYour bid of {listingGold2} gold has been returned."
				AUCTION_OF_ITEM_WON,                    // "Congratulations! \nYou have won the auction for: {itemName0} ({itemQuantity1})! \nYou paid: {listingGold2} gold.";
                OUTBID_ON_AUCTION_OF_ITEM,				// "You have been outbid on the auction of: {itemName0} ({itemQuantity1}) \n\nYour bid of {listingGold2} gold has been returned."
				AUCTION_OF_ITEM_EXPIRED,			    // "Your auction of: {itemName0} ({itemQuantity1}) expired \n\nYour deposit of {depositReturned4} gold has been returned."
                AUCTION_BUYOUT_RETURNED,                // "The buyout of the auction for: {itemName0} ({itemQuantity1}) failed! \n\nYour buyout of {buyoutReturned5} gold has been returned."
                AUCTION_BID_RETURNED                    // "The bid on the auction for: {itemName0} ({itemQuantity1}) failed! \n\nYour bid of {bidReturned5} gold has been returned."
            }
		}
		public static AHMailManagerTextDB textDB = new AHMailManagerTextDB();

/*		#region Text Strings

		private const string AUCTION_HOUSE                       = "Auction House";
        private const string AUCTION_CANCELLED                   = "Auction Cancelled";
        private const string AUCTION_COMPLETE                    = "Auction Complete";
        private const string AUCTION_BOUGHT_OUT                  = "Auction Bought Out";
        private const string AUCTION_WON                         = "Auction Won";
        private const string OUTBID_ON_AUCTION                   = "Outbid On Auction";
        private const string AUCTION_EXPIRED                     = "Auction Expired";
        private const string AUCTION_OF_ITEM_CANCELLED_BY_SELLER = "The auction of: {0} ({1}) has been cancelled by the seller! \n\nYour bid of: {2} gold has been returned.";
        private const string AUCTION_OF_ITEM_CANCELLED_BY_SERVER = "The auction of: {0} ({1}) has been cancelled by the server! \n\nYour deposit of: {4} gold has been returned.";
        private const string AUCTION_OF_ITEM_CANCELLED           = "Your auction of: {0} ({1}) has been cancelled!";
        private const string AUCTION_OF_ITEM_COMPLETED           = "Your auction of: {0} ({1}) was completed! \n\nBid Amount: {2} gold \nAH Commission (5%): -{3} gold \nDeposit Refund: {4} gold \nTotal Awarded: {5} gold!";
        private const string AUCTION_OF_ITEM_BOUGHT_OUT          = "The auction of: {0} ({1}) has been bought out! \n\nYour bid of: {2} gold has been returned.";
        private const string AUCTION_OF_ITEM_WON                 = "Congratulations! \nYou have won the auction for: {0} ({1})! \nYou paid: {2} gold.";
        private const string OUTBID_ON_AUCTION_OF_ITEM           = "You have been outbid on the auction of: {0} ({1})! \n\nYour bid of: {2} gold has been returned.";
        private const string AUCTION_OF_ITEM_EXPIRED             = "Your auction of: {0} ({1}) expired! \n\nYour deposit of: {4} gold has been returned.";

        #endregion
*/
        #region Variables

        // Auction House Database Manager //
        private AHDatabaseManager m_databaseManager;

        #endregion

        // AHMailManager                                            //
        // Set the reference to the Auction Houses database manager //
        public AHMailManager(AHDatabaseManager databaseManager)
        {
            m_databaseManager = databaseManager;
        }

        // SendMailToPlayer                                                                                     //
        // Generic function to send an item (could be a stack) or gold (or both) through the mail to a player   //
        // The flag 'goldOnly' dictates wether the passed itemID anf quantity are sent to the recipient         //
        // The values for the item are always required as they as used to build the correct subject and message //
        // This function will reduce the passed 'listingGold' for completed listings - and mail all the values  //
        internal void SendMailToPlayer(int characterID, AHMailMessageType mailType, Item item, int itemQuantity, int listingGold, int depositReturned, bool goldOnly)
        {
            if (item == null)
            {
                Program.Display("AHMailManager.cs - SendMailToPlayer() Received a null item!");
                return;
            }

            if (characterID == -1)
            {
                return;
            }

            int numAttachedItem = goldOnly ? 0 : 1;
            int auctionHouseCut = 0;

            // Only calculate and take the auction house cut if this mail is to the seller of a completed listing
            if (mailType == AHMailMessageType.LISTING_COMPLETED)
            {
                auctionHouseCut = (int)Math.Round((listingGold * 0.05f), 0);
            }

            int goldAwarded = listingGold - auctionHouseCut + depositReturned;

			int languageID = Localiser.GetLanguageIndexOfCharacter(characterID);

			string mailSubject = GetMailSubject(mailType, languageID);           // for localise text
			string mailMessage = GetMailMessage(mailType, item, itemQuantity,    // returns the message 
                                                                listingGold,     // the highest bid / buyout 
                                                                auctionHouseCut, // the cut taken by the auction house
                                                                depositReturned, // the deposit returned
                                                                goldAwarded,     // the total gold awarded
																languageID);     // for localise text

            // For mail types where the listing has been won (bought out) the value is required but is then no gold is awarded
            if (mailType == AHMailMessageType.LISTING_WON)
            {
                goldAwarded = 0;
            }

			string AHNameText = Localiser.GetStringByLanguageIndex(textDB, languageID, (int)AHMailManagerTextDB.TextID.AUCTION_HOUSE);
			// Create a background task to handle the sending of the mail
			AHSendPlayerMail newTask = new AHSendPlayerMail(characterID,
                                                            mailSubject,
                                                            mailMessage,
                                                            item,
                                                            numAttachedItem,
                                                            goldAwarded,
															AHNameText,
                                                            m_databaseManager);

            lock (Program.processor.m_backgroundTasks)
            {
                Program.processor.m_backgroundTasks.Enqueue(newTask);
            }

            #region Old Non Task Mail Code

            /*int mailID = Program.processor.getAvailableMailID();

            Mailbox.SaveOutMail(characterID,     // to this character id
                                mailSubject,     // mail subject based on the mail type
                                mailMessage,     // mail message with required variables
                                goldAwarded,     // the amount of gold being sent (can be zero)
                                numAttachedItem, // the number of attached items (one or zero)
                                mailID,          // the mails generated UUID
                                -2,              // -2 identifies the auction house (disables the profanity filter)
                                AUCTION_HOUSE,   // senders name
                                false);          // auction house mail does not expire

            if (!goldOnly)
            {
                m_databaseManager.AddItemToMail(item, mailID, characterID);
            }

            Mailbox.NotifyOnlinePlayerWithCharacterID(-1, characterID);*/

            #endregion 
        }

        // GetMailSubject                                                                                     //
        // Returns a string formatted as "Auction Successful: itemName (itemQuantity)" for use a mail subject //
        internal string GetMailSubject(AHMailMessageType mailType, int languageID)
        {
            string message = String.Empty;

            switch (mailType)
            {
                case (AHMailMessageType.LISTING_CANCELLED_BIDDER):
                case (AHMailMessageType.LISTING_CANCELLED_SELLER):
                case (AHMailMessageType.LISTING_CANCELLED_SERVER):
                {
                    message = Localiser.GetStringByLanguageIndex(textDB, languageID, (int)AHMailManagerTextDB.TextID.AUCTION_CANCELLED);
                    break;
                }
                case (AHMailMessageType.LISTING_COMPLETED):
                {
                    message = Localiser.GetStringByLanguageIndex(textDB, languageID, (int)AHMailManagerTextDB.TextID.AUCTION_COMPLETE);
                    break;
                }
                case (AHMailMessageType.LISTING_BOUGHT_OUT):
                {
                    message = Localiser.GetStringByLanguageIndex(textDB, languageID, (int)AHMailManagerTextDB.TextID.AUCTION_BOUGHT_OUT);
                    break;
                }
                case (AHMailMessageType.LISTING_WON):
                {
                    message = Localiser.GetStringByLanguageIndex(textDB, languageID, (int)AHMailManagerTextDB.TextID.AUCTION_WON);
                    break;
                }
                case (AHMailMessageType.OUT_BID):
                {
                    message = Localiser.GetStringByLanguageIndex(textDB, languageID, (int)AHMailManagerTextDB.TextID.OUTBID_ON_AUCTION);
                    break;
                }
                case (AHMailMessageType.LISTING_EXPIRIED):
                {
                    message = Localiser.GetStringByLanguageIndex(textDB, languageID, (int)AHMailManagerTextDB.TextID.AUCTION_EXPIRED);
                    break;
                }
                case (AHMailMessageType.LISTING_BUY_OUT_FAILED):
                {
                    message = Localiser.GetStringByLanguageIndex(textDB, languageID, (int)AHMailManagerTextDB.TextID.AUCTION_BUYOUT_FAILED);
                    break;
                }
                case (AHMailMessageType.LISTING_BID_FAILED):
                {
                    message = Localiser.GetStringByLanguageIndex(textDB, languageID, (int)AHMailManagerTextDB.TextID.AUCTION_BID_FAILED);
                    break;
                }
                default:
                {
                    Program.Display("AHMailManager.cs - GetMailSubject() Unknown AHMailMessageType received");
                    break;
                }
            }

            return message;
        }

        // GetMailMessage                             //
        // Returns a message based on the passed type //
        internal string GetMailMessage(AHMailMessageType mailType, Item item, int itemQuantity, int listingGold, int auctionHouseCut, int depositReturned, int goldAwarded, int languageID)
        {
            string itemName = item.m_template.m_loc_item_name[languageID];
            string message  = String.Empty;

            switch (mailType)
            {
                case (AHMailMessageType.LISTING_CANCELLED_BIDDER):
                {
                    message = Localiser.GetStringByLanguageIndex(textDB, languageID, (int)AHMailManagerTextDB.TextID.AUCTION_OF_ITEM_CANCELLED_BY_SELLER);
                    break;
                }
                case (AHMailMessageType.LISTING_CANCELLED_SELLER):
                {
                    message = Localiser.GetStringByLanguageIndex(textDB, languageID, (int)AHMailManagerTextDB.TextID.AUCTION_OF_ITEM_CANCELLED);
                    break;
                }
                case (AHMailMessageType.LISTING_CANCELLED_SERVER):
                {
                    message = Localiser.GetStringByLanguageIndex(textDB, languageID, (int)AHMailManagerTextDB.TextID.AUCTION_OF_ITEM_CANCELLED_BY_SERVER);
                    break;
                }
                case (AHMailMessageType.LISTING_COMPLETED):
                {
                    message = Localiser.GetStringByLanguageIndex(textDB, languageID, (int)AHMailManagerTextDB.TextID.AUCTION_OF_ITEM_COMPLETED);
                    break;
                }
                case (AHMailMessageType.LISTING_BOUGHT_OUT):
                {
                    message = Localiser.GetStringByLanguageIndex(textDB, languageID, (int)AHMailManagerTextDB.TextID.AUCTION_OF_ITEM_BOUGHT_OUT);
                    break;
                }
                case (AHMailMessageType.LISTING_WON):
                {
                    message = Localiser.GetStringByLanguageIndex(textDB, languageID, (int)AHMailManagerTextDB.TextID.AUCTION_OF_ITEM_WON);
                    break;
                }
                case (AHMailMessageType.OUT_BID):
                {
                    message = Localiser.GetStringByLanguageIndex(textDB, languageID, (int)AHMailManagerTextDB.TextID.OUTBID_ON_AUCTION_OF_ITEM);
                    break;
                }
                case (AHMailMessageType.LISTING_EXPIRIED):
                {
                    message = Localiser.GetStringByLanguageIndex(textDB, languageID, (int)AHMailManagerTextDB.TextID.AUCTION_OF_ITEM_EXPIRED);
                    break;
                }
                case (AHMailMessageType.LISTING_BUY_OUT_FAILED):
                {
                    message = Localiser.GetStringByLanguageIndex(textDB, languageID, (int)AHMailManagerTextDB.TextID.AUCTION_BUYOUT_RETURNED);
                    break;
                }
                case (AHMailMessageType.LISTING_BID_FAILED):
                {
                    message = Localiser.GetStringByLanguageIndex(textDB, languageID, (int)AHMailManagerTextDB.TextID.AUCTION_BID_RETURNED);
                    break;
                }
                default:
                {
                    Program.Display("AHMailManager.cs - GetMailMessage() Unknown AHMailMessageType received");
                    break;
                }
            }

            return String.Format(message, itemName, itemQuantity, listingGold, auctionHouseCut, depositReturned, goldAwarded);
        }
    };
}