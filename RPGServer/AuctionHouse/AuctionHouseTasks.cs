#region Includes

// Includes //
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MainServer.AuctionHouse.Enums;

#endregion

namespace MainServer.AuctionHouse
{
    #region Auction House Tanks Description

    // Server Tasks                                                            //
    // - deleting completed listings from the auction_house_listings table     //
    // - creating player mails as the process can block the main update thread //

    #endregion 

    /*class AHDeleteCompletedListings : BaseTask
    {
        int m_deletionInterval = 0;

        internal AHDeleteCompletedListings(int deletionInterval)
        {
            m_TaskType         = TaskType.DeleteCompletedListings;
            m_deletionInterval = deletionInterval;
        }

        internal override void TakeAction(CommandProcessor processor)
        {
            processor.TheAuctionHouse.DeleteCompletedListings(m_deletionInterval);
        }
    }*/

    // AHSendPlayerMail                                              //
    // Inherits from BaseTask                                        //
    // Background task to create player mails from the Auction House //
    internal class AHSendPlayerMail : BaseTask
    {
        private int    m_characterID;
        private string m_mailSubject;
        private string m_mailMessage;
        private Item   m_item;
        private int    m_numAttachedItem;
        private int    m_goldAwarded;
        private string m_sendersName;

        AHDatabaseManager m_databaseManager;

        internal AHSendPlayerMail(int characterID, string mailSubject, string mailMessage, Item item, int numAttachedItem, int goldAwarded, string sendersName, AHDatabaseManager databaseManager)
        {
            m_characterID     = characterID;
            m_mailSubject     = mailSubject;
            m_mailMessage     = mailMessage;
            m_item            = item;
            m_numAttachedItem = numAttachedItem;
            m_goldAwarded     = goldAwarded;
            m_sendersName     = sendersName;
            m_databaseManager = databaseManager;

            m_TaskType = TaskType.AH_SendMail;
        }

        internal override void TakeAction(CommandProcessor processor)
        {
            int mailID = processor.getAvailableMailID();

            Mailbox.SaveOutMail(m_characterID,               // to this character id
                                m_mailSubject,               // mail subject based on the mail type
                                m_mailMessage,               // mail message with required variables
                                m_goldAwarded,               // the amount of gold being sent (can be zero)
                                m_numAttachedItem,           // the number of attached items (one or zero)
                                mailID,                      // the mails generated UUID
                                Mailbox.NO_PROFANITY_FILTER, // -2 identifies the auction house (disables the profanity filter)
                                m_sendersName,               // senders name
                                false);                      // auction house mail does not expire

            if (m_numAttachedItem > 0)
            {
                m_databaseManager.AddItemToMail(m_item, mailID, m_characterID);
            }

            Mailbox.NotifyOnlinePlayerWithCharacterID(-1, m_characterID);
        }
    }
}