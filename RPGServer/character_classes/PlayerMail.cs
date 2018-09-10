using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using System.Text.RegularExpressions;
using MainServer.Localise;

namespace MainServer
{
    class PlayerMail
    {
		// #localisation
		public class PlayerMailTextDB : TextEnumDB
		{
			public PlayerMailTextDB() : base(nameof(PlayerMail), typeof(TextID)) { }

			public enum TextID
			{
				ATTACHMENTS_RETURNED,				// "Your attachments have been returned"
				ATTACHMENTS_FOR_MAIL_RETURNED		// "The attachments for the mail '{subject0}' have been returned"
			}
		}
		public static PlayerMailTextDB textDB = new PlayerMailTextDB();

		int m_mailID=-1;
        string m_senderName = "";
        int m_senderID=-1;
        int m_recipientID = -1;
        string m_subject="";
        string m_message = "";
        List<Item> m_attachedItems=null;
        int m_attachedGold=0;
        DateTime m_expiryTime;
        bool m_expires = false;
        bool m_read=false;

        internal int MailID
        {
            get { return m_mailID; }
        }
        internal string SenderName
        {
            get { return m_senderName; }
        }
        internal int SenderID
        {
            get { return m_senderID; }
        }
        internal List<Item> AttachedItems
        {
            get { return m_attachedItems; }
        }
        internal int AttachedGold
        {
            get { return m_attachedGold; }
        }
        internal bool Read
        {
            get { return m_read; }
        }
        internal int RecipientID
        {
            get { return m_recipientID; }
        }
        internal void MailRead()
        {
            if (m_read == false)
            {
                Program.processor.m_worldDB.runCommandSync("update mail set mail_read = 1 where mail_id =" + m_mailID);
                m_read = true;
            }
        }
        internal void WriteMailStubToMessage(NetOutgoingMessage outmsg)
        {
            //mail ID
            outmsg.WriteVariableInt32(m_mailID);
            //sender Info
            outmsg.WriteVariableInt32(m_senderID);
            outmsg.Write(m_senderName);
            //subject
            outmsg.Write(m_subject);
            //gold amount
            outmsg.WriteVariableInt32(m_attachedGold);
            //are there items attached
            if (m_attachedItems != null&&m_attachedItems.Count>0)
            {
                outmsg.Write((byte)1);
            }
            else
            {
                outmsg.Write((byte)0);
            }
             //has it been read
            if (m_read==true)
            {
                outmsg.Write((byte)1);
            }
            else
            {
                outmsg.Write((byte)0);
            }
            //how many days until it expires
            //assime it does not expire
            int daysRemaining = -1;
            if(m_expires==true){
                TimeSpan remainingLife = m_expiryTime-DateTime.Now;
                double doubleDays = remainingLife.TotalDays;
                daysRemaining = (int)Math.Floor(doubleDays);
                if (daysRemaining < 0)
                {
                    daysRemaining = 0;
                }
            }
            outmsg.WriteVariableInt32(daysRemaining);
        }
        internal void WriteCompleteMailToMessage(NetOutgoingMessage outmsg)
        {

            WriteMailStubToMessage(outmsg);

            outmsg.Write(m_message);

            WriteItemsToMessage(outmsg);

        }
        internal void 
            WriteItemsToMessage(NetOutgoingMessage outmsg)
        {
            if (m_attachedItems == null)
            {
                outmsg.WriteVariableInt32(0);
                return;
            }

            outmsg.WriteVariableInt32(m_attachedItems.Count);
            for (int i = 0; i < m_attachedItems.Count; i++)
            {
                Item currentItem = m_attachedItems[i];
                outmsg.WriteVariableInt32(currentItem.m_inventory_id);
                outmsg.WriteVariableInt32(currentItem.m_template_id);
                outmsg.WriteVariableInt32(currentItem.m_quantity);
                if (currentItem.m_bound == true)
                {
                    outmsg.Write((byte)1);
                }
                else
                {
                    outmsg.Write((byte)0);
                }
                outmsg.WriteVariableInt32(currentItem.m_remainingCharges);
            }
        }
        
        internal PlayerMail()
        {

        }
        internal PlayerMail(SqlQuery query, Database db)
        {
            m_mailID = query.GetInt32("mail_id");
            m_senderID = query.GetInt32("sender_id");
            m_senderName = query.GetString("sender_name");
            m_subject = query.GetString("subject");
            m_message = query.GetString("message");
            // Dont alter subject or message for mails from auction house
            if (m_senderID != -2)
            {
                m_subject = ProfanityFilter.replaceOffendingStrings(m_subject);
                m_message = ProfanityFilter.replaceOffendingStrings(m_message);
            }
            m_attachedGold = query.GetInt32("attached_gold");
            m_read = query.GetBoolean("mail_read");
            bool itemsAttached = query.GetBoolean("attached_items");
            bool attachmentsTaken = query.GetBoolean("attachments_taken");
            m_recipientID = query.GetInt32("recipient_id");
            if (query.isNull("expiry_time")==false)
            {

                m_expiryTime = query.GetDateTime("expiry_time");
                m_expires = true;
            }
            if (attachmentsTaken == true)
            {
                m_attachedGold = 0;
            }
            if (itemsAttached == true && attachmentsTaken == false)
            {
                if (m_attachedItems == null)
                {
                    m_attachedItems = new List<Item>();
                }
                SqlQuery itemQuery = new SqlQuery(db, "select * from mail_inventory where mail_id =" + m_mailID);
                if (itemQuery.HasRows)
                {
                    while (itemQuery.Read())
                    {
                        int inventoryID = itemQuery.GetInt32("inventory_id");

                        int itemID = itemQuery.GetInt32("item_id");
                        int quantity = itemQuery.GetInt32("quantity");

                        bool bound = itemQuery.GetBoolean("bound");
                        int remainingCharges = itemQuery.GetInt32("remaining_charges"); ;
                        double timeRecharged = itemQuery.GetDouble("time_skill_last_cast");
                        int sortOrder = itemQuery.GetInt32("sort_order");
                        Item newItem = new Item(inventoryID, itemID, quantity, bound, remainingCharges, timeRecharged, sortOrder, false);
                        m_attachedItems.Add(newItem);
                    }
                }
                itemQuery.Close();

            }
        }

        internal void TakeAttachments(Character owner, SQLSuccessDelegate takeAttachmentsDelgate)
        {
            SQLSuccessDelegate successDelegate = delegate ()
            {
                if (owner != null && owner.m_player != null && owner.m_player.connection != null && owner.m_QuestManager != null && owner.m_inventory != null && owner.m_inventory.m_character != null &&
                    owner.m_inventory.m_character.m_player != null && owner.m_inventory.m_character.m_player.connection != null )
                {
                    if(m_attachedGold != 0 && owner.m_player.m_activeCharacter != null)
                        Program.Display(string.Format("Character {0} took mail-attached gold {1}", owner.m_player.m_activeCharacter.m_name, m_attachedGold));

                    owner.updateCoins(m_attachedGold);
                    TakeItems(owner);
                    m_attachedItems = null;
                    m_attachedGold = 0;

                    if (takeAttachmentsDelgate != null)
                    {
                        takeAttachmentsDelgate();
                    }
                }
                // If we have a null ref here we cannot award the coins and or items from the mail - therefore we need to undo the sql thats sets the attachments as taken
                else
                {
                    Program.processor.m_worldDB.runCommandSync("update mail set attachments_taken = 0 where mail_id = " + m_mailID);
                }
            };

            owner.m_db.runCommandSync("update mail set attachments_taken = 1 where mail_id ="+m_mailID, successDelegate);
        }

        internal bool ReturnSelf(uint ownerID)
        {
            SqlQuery query = new SqlQuery(Program.processor.m_worldDB, String.Format("SELECT mail_id FROM mail where attachments_taken = 0 AND mail_id = {0}", m_mailID));
            if (query.HasRows == false)
            {
                Program.Display("Mail attempted to return a mail which could not be found!");
                query.Close();
                return false;
            }
            query.Close();

            Program.processor.m_worldDB.runCommandSync("update mail set attachments_taken = 1,deleted = 1  where mail_id =" + m_mailID);
			int languageIndex = Localiser.GetLanguageIndexOfCharacter(m_senderID);
			string subject = Localiser.GetStringByLanguageIndex(textDB, languageIndex, (int)PlayerMailTextDB.TextID.ATTACHMENTS_RETURNED);
			string message = Localiser.GetStringByLanguageIndex(textDB, languageIndex, (int)PlayerMailTextDB.TextID.ATTACHMENTS_FOR_MAIL_RETURNED);
			message = String.Format(message, m_subject);
			int numItems =0;
            if(m_attachedItems!=null)
            {
                numItems = m_attachedItems.Count;
            }
            int mailID = Program.processor.getAvailableMailID();
            Mailbox.SaveOutMail(m_senderID, subject, message, m_attachedGold, numItems, mailID, SenderID, SenderName,true);
            TransferItemsToNewMail(mailID, ownerID);
            m_attachedGold = 0;
            m_attachedItems = null;

            bool playerFound = Mailbox.NotifyOnlinePlayerWithCharacterID((int)m_senderID, (int)m_senderID);
            if (playerFound == false)
            {
                string profanityFilteredSubject = ProfanityFilter.GetStarredOutOffendingStrings(subject);
                SendMailNotification task = new SendMailNotification(Program.m_ServerName, (int)m_senderID, m_senderName, (int)m_senderID, profanityFilteredSubject, 0, "default");
                lock (Program.processor.m_backgroundTasks)
                {
                    Program.processor.m_backgroundTasks.Enqueue(task);
                }
            }

            return true;
        }
        internal void DeleteSelf()
        {
            Program.processor.m_worldDB.runCommandSync("update mail set deleted = 1  where mail_id =" + m_mailID);
        }
        bool TakeItems(Character owner)
        {
            bool success = true;
            List<Item> attachedItems = m_attachedItems;
            //no items to take
            if (attachedItems == null)
            {
                Program.Display("Error detaching mail items as they were null");
                return success;
            }

            if (attachedItems.Count > 1)
                Program.Display(string.Format("Detaching {0} items from mailID {1} for character {2}", attachedItems.Count, m_mailID, m_recipientID));

            for (int currentItemIndex = 0; currentItemIndex < attachedItems.Count; currentItemIndex++)
            {
                Item baseItem = attachedItems[currentItemIndex];
                
                if (baseItem != null )
                {
                    Item newItem = new Item(baseItem);
                    newItem.m_sortOrder = owner.m_inventory.getNextSortOrder();
                    Item transferedItem = owner.m_inventory.AddExistingItemToCharacterInventory(newItem);
                    owner.m_QuestManager.checkIfItemAffectsStage(newItem.m_template_id);

                    if (transferedItem == null)
                    {
                        success = false;
                        Program.Display("Error Removing Item" + newItem.GetBasicItemIDString() + " from mail " + m_mailID);
                    }
                    else
                    {
                        Program.processor.updateShopHistory(-2, -2, transferedItem.m_inventory_id, transferedItem.m_template_id, newItem.m_quantity, 0, (int)owner.m_character_id, "Mail - " + m_mailID+" From "+ m_senderID);

                        Program.Display(string.Format("Detached item {0} from mail {1} for character {2}", transferedItem.m_inventory_id, m_mailID, m_recipientID));
                    }
                }
            }
            return success;
        }
        void TransferItemsToNewMail(int mailID,uint characterID )
        {
            if (m_attachedItems != null)
            {
                for (int i = 0; i < m_attachedItems.Count; i++)
                {
                    Item currentItem = m_attachedItems[i];
                    string insertString = "(mail_id," + currentItem.GetInsertFieldsString() + ") values (" + mailID + "," + currentItem.GetInsertValuesString((int)characterID) + ")";
                    Program.processor.m_worldDB.runCommandSync("insert into mail_inventory " + insertString);

                    Program.processor.updateShopHistory(-2, -2, currentItem.m_inventory_id, currentItem.m_template_id, currentItem.m_quantity, 0, m_recipientID, "Mail - " + m_mailID + " From " + m_senderID);
                    Program.processor.updateShopHistory(-2, -2, currentItem.m_inventory_id, currentItem.m_template_id, -currentItem.m_quantity, 0, m_recipientID, "Mail - " + mailID + " to " + m_senderID);

                }
            }
        }
        
    }
}
