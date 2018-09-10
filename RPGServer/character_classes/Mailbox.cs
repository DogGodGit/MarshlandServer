using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Lidgren.Network;
using MainServer.Localise;
using MySql.Data.MySqlClient;

namespace MainServer
{
    class Mailbox
    {

		// #localisation
		public class MailboxTextDB : TextEnumDB
		{
			public MailboxTextDB() : base(nameof(Mailbox), typeof(TextID)) { }

			public enum TextID
			{
				MESSAGE_SENT,					// "Message Sent : {subject0}"
				SEND_FAILED,					// "Failed To send mail : {errorMessage0}"
				REMOVE_ALL_ATTACHMENTS,			// "Please remove all attachments before deleting this mail"
				CANNOT_HANDLE_ITEMS,			// "Sorry, our couriers can't handle one or more of these items."
				PRICE_DIFFERENT,				// "\nThe price was different than expected."
				ERROR_OCCURED,					// "\nAn Error Has Occurred."
				NOT_ENOUGH_COINS,				// "\nNot Enough Coins."
				NOT_ENOUGH_STAMPS,				// "\nYou do not have enough stamps."
				ERROR_PACKING_ITEMS,			// "\nThere was an issue packing your items"
			}
		}
		public static MailboxTextDB textDB = new MailboxTextDB();

        public static int MAX_NAME_LENGTH = 30;
        public static int MAX_SUBJECT_LENGTH = 30;
        public static int MAX_DATABASE_SUBJECT_LENGTH = 35;
        public static int MAX_MESSAGE_LENGTH = 250;
        public static int MAX_ITEMS_ATTACHED = 16;
        public static int MIN_SEND_LEVEL = 0;
        static int m_stampID = 21948;
        Character m_owningCharacter;
        bool m_unreadMail;
        bool m_sendMailList;
        bool m_mailLoaded;
        List<PlayerMail> m_mailList = new List<PlayerMail>();
        List<PlayerMail> m_newList = new List<PlayerMail>();
        DateTime m_mailLastChecked;
        internal const int MAIL_ID_POOL_SIZE = 50;
        internal const float MAIL_CHECK_FREQ_MIN = 5.0f;
        internal const float MAIL_CLEAR_OLD_MIN = 60.0f;
        internal static int NO_PROFANITY_FILTER = -2;
        internal bool UnreadMail
        {
            get { return m_unreadMail; }
            set { m_unreadMail = value; }
        }

        public Mailbox()
        {
            //LoadTestMail();
        }
        internal void SetUp(Character owner)
        {
            m_owningCharacter = owner;
            CheckForUnreadMail(Program.processor.m_worldDB, owner.m_character_id);
            //LoadMailFromDatabase(Program.processor.m_worldDB, owner.m_character_id);
        }
        internal void ProcessMailMessage(NetIncomingMessage msg)
        {
            MailMessageType messageType = (MailMessageType)msg.ReadVariableInt32();

            switch (messageType)
            {
                case MailMessageType.MMT_RequestMailBox:
                    {
                        ProcessRequestMailBox(msg);
                        break;
                    }
                case MailMessageType.MMT_RequestMessageInfo:
                    {
                        ProcessRequestMailInfo(msg);
                        break;
                    }
                case MailMessageType.MMT_RequestSendDetails:
                    {
                        ProcessRequestSendInfo(msg);
                        break;
                    }
                case MailMessageType.MMT_SendMessage:
                    {
                        ProcessSendMail(msg);
                        break;
                    }
                case MailMessageType.MMT_TakeAttachments:
                    {
                        ProcessTakeAttachments(msg);
                        break;
                    }
                case MailMessageType.MMT_ReturnMessage:
                    {
                        ProcessReturnMail(msg);
                        break;
                    }
                case MailMessageType.MMT_DeleteMessage:
                    {
                        ProcessDeleteMail(msg);
                        break;
                    }
                case MailMessageType.MMT_ClosedMailBox:
                    {
                        ProcessClosedMailbox(msg);
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }
        void SendMailList()
        {
            lock (m_newList)
            {
                if (m_newList.Count > 0)
                {
                    m_mailList.AddRange(m_newList);
                    m_newList.Clear();
                }
            }
            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.MailMessage);
            outmsg.WriteVariableInt32((int)MailMessageType.MMT_MailList);

            //basic Mail info
            outmsg.WriteVariableInt32(Mailbox.MAX_NAME_LENGTH);
            outmsg.WriteVariableInt32(Mailbox.MAX_SUBJECT_LENGTH);
            outmsg.WriteVariableInt32(Mailbox.MAX_MESSAGE_LENGTH);
            outmsg.WriteVariableInt32(Mailbox.MAX_ITEMS_ATTACHED);
            outmsg.WriteVariableInt32(Mailbox.MIN_SEND_LEVEL);
            //number of mails
            outmsg.WriteVariableInt32(m_mailList.Count);
            //the stub for each mail
            for (int currentMailIndex = 0; currentMailIndex < m_mailList.Count; currentMailIndex++)
            {
                PlayerMail currentMail = m_mailList[currentMailIndex];
                currentMail.WriteMailStubToMessage(outmsg);
            }
            NetConnection connection = null;
            if (m_owningCharacter != null && m_owningCharacter.m_player != null)
            {
                connection = m_owningCharacter.m_player.connection;
            }

            Program.processor.SendMessage(outmsg, connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.MailMessage);

        }
        void SendMailInfoForID(int mailID)
        {

            PlayerMail theMail = GetMailForID(mailID);
            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.MailMessage);
            outmsg.WriteVariableInt32((int)MailMessageType.MMT_MailUpdate);
            //if it failed to find the mail then tell the client
            if (theMail == null)
            {
                outmsg.Write((byte)0);
                outmsg.WriteVariableInt32(mailID);
            }
            //if it found the mail send all the data
            else
            {
                if (theMail.Read == false)
                {
                    theMail.MailRead();
                    if (Program.m_LogAnalytics)
                    {
                        AnalyticsMain logAnalytics = new AnalyticsMain(false);
                        logAnalytics.LogMessageReceived(m_owningCharacter.m_player, theMail.SenderID.ToString(), "MAIL", mailID.ToString());
                    }
                }
                outmsg.Write((byte)1);
                theMail.WriteCompleteMailToMessage(outmsg);
            }

            NetConnection connection = null;
            if (m_owningCharacter != null && m_owningCharacter.m_player != null)
            {
                connection = m_owningCharacter.m_player.connection;
            }

            Program.processor.SendMessage(outmsg, connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.MailMessage);
        }
        void ProcessRequestMailBox(NetIncomingMessage msg)
        {
            Program.Display(m_owningCharacter.GetIDString() + "refreshed their mailbox");
            //SendMailList();
            if (m_mailLoaded == false)
            {
                m_mailLastChecked = DateTime.Now;
                LoadNewMailTask task = new LoadNewMailTask(m_owningCharacter.m_player, m_owningCharacter.m_character_id, this, m_mailLastChecked, m_owningCharacter.BlockList);
                task.m_TaskType = BaseTask.TaskType.LoadNewMail;
                lock (Program.processor.m_backgroundTasks)
                {
                    Program.processor.m_backgroundTasks.Enqueue(task);
                }
                m_unreadMail = false;
            }
            m_mailLoaded = true;
        }
        void ProcessClosedMailbox(NetIncomingMessage msg)
        {
            m_mailLoaded = false;
            m_mailList.Clear();
            Program.Display(m_owningCharacter.GetIDString() + "closed their mailbox");
        }
        void ProcessRequestMailInfo(NetIncomingMessage msg)
        {
            int mailID = msg.ReadVariableInt32();
            SendMailInfoForID(mailID);
        }
        void ProcessRequestSendInfo(NetIncomingMessage msg)
        {
            string name = msg.ReadString();
            int messageLen = msg.ReadVariableInt32();
            int gold = msg.ReadVariableInt32();
            int numItems = msg.ReadVariableInt32();

			DataValidator.JustCheckCharacterName(name);

            RequestMailSendInfoTask task = new RequestMailSendInfoTask(m_owningCharacter.m_player, name, messageLen, gold, numItems);
            task.m_TaskType = BaseTask.TaskType.RequestMailSendInfo;
            Program.processor.EnqueueTask(task);
        }
        void ProcessTakeAttachments(NetIncomingMessage msg)
        {
            int        mailID  = msg.ReadVariableInt32();
            PlayerMail theMail = GetMailForID(mailID);
            if (theMail != null && m_owningCharacter != null)
            {
                SqlQuery query = new SqlQuery(Program.processor.m_worldDB, String.Format("SELECT mail_id FROM mail where mail_id = {0} and attachments_taken = 0", mailID));
                if (query.HasRows)
                {
                    // go ahead
                    SQLSuccessDelegate takeAttachmentsDelgate = delegate ()
                    {
                        if (m_owningCharacter != null && m_owningCharacter.m_inventory != null && m_owningCharacter.m_inventory.m_character != null &&
                            m_owningCharacter.m_inventory.m_character.m_player != null && m_owningCharacter.m_inventory.m_character.m_player.connection != null)
                        {
                            SendMailInfoForID(mailID);
                            m_owningCharacter.m_inventory.SendInventoryUpdate();
                        }
                    };

                    theMail.TakeAttachments(m_owningCharacter, takeAttachmentsDelgate);
                }
                else
                {
                    // error - mail doesnt exist or attachments have been taken
                }

                query.Close();
            }
        }
        void ProcessReturnMail(NetIncomingMessage msg)
        {
            int mailID = msg.ReadVariableInt32();
            PlayerMail theMail = GetMailForID(mailID);
            if (theMail != null)
            {
                ReturnMail(theMail);
            }
        }
        void ProcessDeleteMail(NetIncomingMessage msg)
        {
            int mailID = msg.ReadVariableInt32();
            PlayerMail theMail = GetMailForID(mailID);
            if (theMail != null)
            {
                if (theMail.AttachedItems != null || theMail.AttachedGold != 0)
                {
					string locText = Localiser.GetString(textDB, m_owningCharacter.m_player, (int)MailboxTextDB.TextID.REMOVE_ALL_ATTACHMENTS);
					Program.processor.SendXMLPopupMessage(true, m_owningCharacter.m_player, (int)XML_Popup.Set_Popup_IDs.SPI_BasicPopup, XML_Popup.Popup_Type.None, "popup_template.txt", new List<string> { locText, " modal = \"true\" active = \"connected\"", "", "" }, false);
                }
                else
                {
                    DeleteMail(theMail);
                }
            }
        }

        void ProcessSendMail(NetIncomingMessage msg)
        {
            //price
            int price = msg.ReadVariableInt32();
            //recipient id
            int recipientID = msg.ReadVariableInt32();
            //subject
            String subject = msg.ReadString();
            //message
            string message = msg.ReadString();
            //gold
            int gold = msg.ReadVariableInt32();
            //items
            int numItems = msg.ReadVariableInt32();
            List<Item> attachedItems = null;
            if (numItems > 0)
            {
                attachedItems = new List<Item>();
            }

			//flag for possible hacked client sending non trade item
	        bool failedMail = false;

            int numItemDuplicates = 0;
            for (int i = 0; i < numItems; i++)
            {
                int inventID = msg.ReadVariableInt32();
                int templateID = msg.ReadVariableInt32();
                int quantity = msg.ReadVariableInt32();
				
                Item attachedItem = new Item(inventID, templateID, quantity, 0);

				//server check for trade allowed
	            if (attachedItem.m_template.m_noTrade == true)
	            {
					
					Program.Display("Possible hacked client - player trying to send no-trade item. itemId." + templateID + " itemName." + attachedItem.m_template.m_item_name
						+ " playerID." + this.m_owningCharacter.m_player.GetIDString() + 
						" character." + this.m_owningCharacter.Name);
		            failedMail = true;
					break;		            
	            }

                //has this item already been added then add the last quantity
                bool itemexists = false;
                for (int currentItemIndex = 0; currentItemIndex < attachedItems.Count; currentItemIndex++)
                {
                    Item currentItem = attachedItems[currentItemIndex];
                    if (currentItem.m_inventory_id == attachedItem.m_inventory_id)
                    {
                        itemexists = true;
                        currentItem.m_quantity += attachedItem.m_quantity;
                        numItemDuplicates++;
                    }
                }
                //if the item has already been added don't count it again
                if (itemexists == false)
                {
                    attachedItems.Add(attachedItem);
                }

            }
            if (numItemDuplicates > 0)
            {
                price -= numItemDuplicates;
                numItems -= numItemDuplicates;
            }

			//everything went through ok, send mail as normal
	        if (failedMail == false)
	        {
		        AttemptToSendMail(price, recipientID, subject, message, gold, numItems, attachedItems);
	        }
	        else //we failed our hack check tell the player
	        {
				NetOutgoingMessage outmsg = Program.Server.CreateMessage();
				outmsg.WriteVariableUInt32((uint)NetworkCommandType.SimpleMessageForThePlayer);
				string locText = Localiser.GetString(textDB, m_owningCharacter.m_player, (int)MailboxTextDB.TextID.CANNOT_HANDLE_ITEMS);
				outmsg.Write(locText);
				//outmsg.Write("Sorry, our couriers can't handle one or more of these items.");
				Program.processor.SendMessage(outmsg, this.m_owningCharacter.m_player.connection, NetDeliveryMethod.ReliableOrdered, 
					NetMessageChannel.NMC_Normal, NetworkCommandType.SimpleMessageForThePlayer);				
	        }
        }

        void AttemptToSendMail(int price, int recipientID, String subject, string message, int gold, int numItems, List<Item> attachedItems)
        {
            string errorString = String.Empty;
            bool valid = IsValidMail(price, recipientID, subject, message, gold, numItems, attachedItems, ref errorString);
            if (0 != errorString.Length)
            {
                Program.Display("AttemptToSendMail error: " + errorString);
            }
            if (valid)
            {
                subject = Regex.Replace(subject, Localiser.TextSymbolFilter, "");
                message = Regex.Replace(message, Localiser.TextSymbolNewLineFilter, "");
                int mailID = Program.processor.getAvailableMailID();
                RemoveCoinsForMail(mailID, gold);
                if (subject.Length > MAX_DATABASE_SUBJECT_LENGTH)
                {
                    if (subject.Contains("RE : "))
                    {
                        subject = subject.Replace("RE : ", "");
                        subject = "RE : " + subject;
                    }
                    if (subject.Length > MAX_DATABASE_SUBJECT_LENGTH)
                    {
                        subject = subject.Remove(MAX_DATABASE_SUBJECT_LENGTH);
                    }
                }

                if (message.Length > MAX_MESSAGE_LENGTH)
                {
                    if (message.Contains("RE : "))
                    {
                        message = subject.Replace("RE : ", "");
                        message = "RE : " + message;
                    }
                    if (message.Length > MAX_MESSAGE_LENGTH)
                    {
                        message = message.Remove(MAX_MESSAGE_LENGTH);
                    }
                }

                SaveOutMail(recipientID, subject, message, gold, numItems, mailID, (int)m_owningCharacter.m_character_id, m_owningCharacter.Name, true);
                TransferItemsToMail(mailID, attachedItems, recipientID);
                PayForMail(mailID, price);
                bool playerFound = NotifyOnlinePlayerWithCharacterID((int)m_owningCharacter.m_character_id, recipientID);
                if (playerFound == false)
                {
                    // get their accountid so we're not notifying ourselves of character offline
                    int recipientAccountID = CommandProcessor.GetAccountIDWithCharacterIDFromDatabase(recipientID);

                    if (recipientAccountID != m_owningCharacter.m_account_id)
                    {
                        string profanityFilteredSubject = ProfanityFilter.GetStarredOutOffendingStrings(subject);

                        SendMailNotification task = new SendMailNotification(Program.m_ServerName, (int)m_owningCharacter.m_character_id, m_owningCharacter.Name, recipientID, profanityFilteredSubject, 0, "default");
                        lock (Program.processor.m_backgroundTasks)
                        {
                            Program.processor.m_backgroundTasks.Enqueue(task);
                        }
                    }
                }

                //send an inventory update
                m_owningCharacter.m_inventory.SendInventoryUpdate();
                if (Program.m_LogAnalytics)
                {
                    AnalyticsMain logAnalytics = new AnalyticsMain(false);
                    logAnalytics.LogMessageSent(m_owningCharacter.m_player, recipientID.ToString(), "MAIL", mailID.ToString());
                }
				string locText = Localiser.GetString(textDB, m_owningCharacter.m_player, (int)MailboxTextDB.TextID.MESSAGE_SENT);
				locText = string.Format(locText, subject);
				Program.processor.sendSystemMessage(locText, m_owningCharacter.m_player, false, SYSTEM_MESSAGE_TYPE.POPUP);
            }
            else
            {
                Program.Display("AttemptToSendMail error: " + errorString);
				string locText = Localiser.GetString(textDB, m_owningCharacter.m_player, (int)MailboxTextDB.TextID.SEND_FAILED);
				locText = string.Format(locText, errorString);
				Program.processor.sendSystemMessage(locText, m_owningCharacter.m_player, false, SYSTEM_MESSAGE_TYPE.POPUP);
            }
        }
        void ReturnMail(PlayerMail mailToReturn)
        {
            bool success = mailToReturn.ReturnSelf(m_owningCharacter.m_character_id);
            if (success == true)
            {
                m_mailList.Remove(mailToReturn);
            }
            SendMailList();
        }
        void DeleteMail(PlayerMail mailToReturn)
        {
            mailToReturn.DeleteSelf();
            m_mailList.Remove(mailToReturn);
            SendMailList();
        }
        void TransferItemsToMail(int mailID, List<Item> attachedItems, int recipientID)
        {
            if (attachedItems == null)
            {
                return;
            }
            for (int currentItemIndex = 0; currentItemIndex < attachedItems.Count; currentItemIndex++)
            {
                Item currentStubItem = attachedItems[currentItemIndex];
                Item baseItem = m_owningCharacter.m_inventory.findBagItemByInventoryID(currentStubItem.m_inventory_id, currentStubItem.m_template_id);
                if (baseItem != null && baseItem.m_quantity >= currentStubItem.m_quantity)
                {
                    Item newItem = new Item(baseItem);
                    newItem.m_quantity = currentStubItem.m_quantity;
                    string errorStr = m_owningCharacter.m_inventory.DeleteItem(newItem.m_template_id, newItem.m_inventory_id, newItem.m_quantity);

                    if (errorStr == "")
                    {
                        string insertString = "(mail_id," + newItem.GetInsertFieldsString() + ") values (" + mailID + "," + newItem.GetInsertValuesString((int)m_owningCharacter.m_character_id) + ")";
                        m_owningCharacter.m_db.runCommandSync("insert into mail_inventory " + insertString);
                        m_owningCharacter.m_QuestManager.checkIfItemAffectsStage(newItem.m_template_id);
                        Program.processor.updateShopHistory(-2, -2, newItem.m_inventory_id, newItem.m_template_id, -newItem.m_quantity, 0, (int)m_owningCharacter.m_character_id, "Mail - " + mailID + " to " + recipientID);
                    }
                    else
                    {
                        Program.Display("Error Removing Item" + newItem.GetBasicItemIDString() + " from " + m_owningCharacter.GetIDString());
                    }
                }
            }
        }
        internal static bool NotifyOnlinePlayerWithCharacterID(int senderCharacterID, int characterID)
        {
            Player player = Program.processor.getPlayerFromActiveCharacterId(characterID);
            if (player != null && player.m_activeCharacter != null)
            {
                Character character = player.m_activeCharacter;

                if (character.CharacterMail.UnreadMail == false)
                {
                    if (character.HasBlockedCharacter(senderCharacterID) == false)
                    {
                        character.CharacterMail.UnreadMail = true;
                        SendNewMailMessageToPlayer(false, player);
                        return true;
                    }
                }
            }
            return false;
        }
        void RemoveCoinsForMail(int mailID, int attachedGold)
        {
            int totalGold = attachedGold;

            if (totalGold < 0)
            {
                Program.Display("*GH20130801* " + m_owningCharacter.GetIDString() + " tried to send " + totalGold + " coins ");
                totalGold = 0;
            }

            m_owningCharacter.updateCoins(-totalGold);
            Program.Display("Removed " + totalGold + " from " + m_owningCharacter.GetIDString() + " to send mail " + mailID + " ( attached" + attachedGold + ")");
        }
        void PayForMail(int mailID, int price)
        {
            bool stampsRemoved = false;
            bool notEnoughStamps = false;
            int currentStampsRemoved = 0;
            int saftyCatch = 0;

            while (stampsRemoved == false && notEnoughStamps == false && saftyCatch < 1000)
            {
                Item stamps = m_owningCharacter.m_inventory.GetItemFromTemplateID(m_stampID, true);
                if (stamps == null)
                {
                    notEnoughStamps = true;
                }
                else
                {
                    int numStamps = stamps.m_quantity;
                    if (currentStampsRemoved + numStamps < price)
                    {
                        currentStampsRemoved += numStamps;
                        m_owningCharacter.m_inventory.DeleteItem(stamps.m_template_id, stamps.m_inventory_id, numStamps);
                    }
                    else
                    {
                        int remainingStamps = price - currentStampsRemoved;
                        currentStampsRemoved += remainingStamps;
                        m_owningCharacter.m_inventory.DeleteItem(stamps.m_template_id, stamps.m_inventory_id, remainingStamps);
                        stampsRemoved = true;
                    }
                }
            }
            if (stampsRemoved == false)
            {
                Program.Display("PayForMail for " + m_owningCharacter.GetIDString() + " failed to remove required stamps. removed " + currentStampsRemoved + " of " + price);
            }
        }
        internal static int SaveOutMail(int recipientID, String subject, string message, int gold, int numItems, int mailID, int senderID, string senderName, bool expires)
        {
            int deliveryTimeMin = 0;
            int expiryTimeDays = 28;
            DateTime deliveryTime = DateTime.Now + TimeSpan.FromMinutes(deliveryTimeMin);
            DateTime expiryTime = deliveryTime + TimeSpan.FromDays(expiryTimeDays);

            string deliveryDateSTR = deliveryTime.ToString("yyyy-MM-dd HH:mm:ss");
            string expiryDateSTR = expiryTime.ToString("yyyy-MM-dd HH:mm:ss");
            bool hasItems = (numItems > 0);

			/*string setString = " recipient_id = " + recipientID + ", sender_id = " + senderID + ", sender_name = '" + senderName
                + "', subject = \"" + subject + "\", message = \"" + message + "\", attached_gold = " + gold + ", attached_items=" + hasItems +
                ",delivery_time ='" + deliveryDateSTR + "'";
            if (expires == true)
            {
                setString += ", expiry_time = '" + expiryDateSTR + "'";
            }
            Program.processor.m_worldDB.runCommandSync("update mail set " + setString + " where mail_id=" + mailID);*/

			List<MySqlParameter> sqlParams = new List<MySqlParameter>();
			sqlParams.Add(new MySqlParameter("@recipient_id", recipientID));
			sqlParams.Add(new MySqlParameter("@sender_id", senderID));
			sqlParams.Add(new MySqlParameter("@sender_name", senderName));
			sqlParams.Add(new MySqlParameter("@subject", subject));
			sqlParams.Add(new MySqlParameter("@message", message));
			sqlParams.Add(new MySqlParameter("@attached_gold", gold));
			sqlParams.Add(new MySqlParameter("@attached_items", hasItems));
			sqlParams.Add(new MySqlParameter("@delivery_time", deliveryDateSTR));

			string setString = " recipient_id=@recipient_id,sender_id=@sender_id,sender_name=@sender_name,subject=@subject,message=@message,attached_gold=@attached_gold,attached_items=@attached_items,delivery_time=@delivery_time";
			if (expires == true)
			{
				sqlParams.Add(new MySqlParameter("@expiry_time", expiryDateSTR));

				setString += ",expiry_time=@expiry_time";
			}

			Program.processor.m_worldDB.runCommandSyncWithParams("update mail set " + setString + " where mail_id=" + mailID, sqlParams.ToArray());

			return mailID;
        }

        bool IsValidMail(int price, int recipientID, String subject, string message, int gold, int numItems, List<Item> attachedItems, ref string errorString)
        {
            bool IsValid = true;

            //is it the price the player was expecting
            int newPrice = GetCostForMessage(message.Length, gold, numItems);
            if (newPrice != price)
            {
                IsValid = false;
				errorString = Localiser.GetString(textDB, m_owningCharacter.m_player, (int)MailboxTextDB.TextID.PRICE_DIFFERENT);
            }
            //do they have enough to pay the fee and the gold
            if (gold < 0)
            {
                IsValid = false;
				errorString = Localiser.GetString(textDB, m_owningCharacter.m_player, (int)MailboxTextDB.TextID.ERROR_OCCURED);
                Program.Display("*GH20130801* " + m_owningCharacter.GetIDString() + " tried to send " + gold + " coins ");
            }
            if (gold > m_owningCharacter.m_inventory.m_coins)
            {
                IsValid = false;
				errorString = Localiser.GetString(textDB, m_owningCharacter.m_player, (int)MailboxTextDB.TextID.NOT_ENOUGH_COINS);
            }

            int availableStamps = m_owningCharacter.m_inventory.GetItemCount(m_stampID);
            if (attachedItems != null)
            {
                for (int i = 0; i < attachedItems.Count; i++)
                {
                    Item currentItem = attachedItems[i];
                    if (currentItem.m_template_id == m_stampID)
                    {
                        availableStamps -= currentItem.m_quantity;
                    }
                }
            }
            if (availableStamps < newPrice)
            {
                IsValid = false;
				errorString = Localiser.GetString(textDB, m_owningCharacter.m_player, (int)MailboxTextDB.TextID.NOT_ENOUGH_STAMPS);
            }
            /*int totalGold = price + gold;
            if (m_owningCharacter.m_inventory.m_coins < totalGold)
            {
                IsValid = false;
            }*/

            //check the correct number of items are attached
            if (numItems > 0 && attachedItems == null)
            {
                IsValid = false;
				errorString = Localiser.GetString(textDB, m_owningCharacter.m_player, (int)MailboxTextDB.TextID.ERROR_PACKING_ITEMS);
            }
            else if (attachedItems != null)
            {
                if (attachedItems.Count != numItems)
                {
                    IsValid = false;
					errorString = Localiser.GetString(textDB, m_owningCharacter.m_player, (int)MailboxTextDB.TextID.ERROR_PACKING_ITEMS);
                }
            }
            //check the player has enough of each item
            if (attachedItems != null)
            {
                for (int i = 0; i < attachedItems.Count; i++)
                {
                    Item currentItem = attachedItems[i];
                    Item baseItem = m_owningCharacter.m_inventory.findBagItemByInventoryID(currentItem.m_inventory_id, currentItem.m_template_id);
                    if (baseItem == null)
                    {
                        if (errorString == "")
                        {
							errorString = Localiser.GetString(textDB, m_owningCharacter.m_player, (int)MailboxTextDB.TextID.ERROR_PACKING_ITEMS);
                        }
                        IsValid = false;
                    }
                    else if (baseItem.m_quantity < currentItem.m_quantity)
                    {
                        if (errorString == "")
                        {
							errorString = Localiser.GetString(textDB, m_owningCharacter.m_player, (int)MailboxTextDB.TextID.ERROR_PACKING_ITEMS);
                        }
                        IsValid = false;
                    }
                    else if (currentItem.m_quantity < 0)
                    {
                        if (errorString == "")
                        {
							errorString = Localiser.GetString(textDB, m_owningCharacter.m_player, (int)MailboxTextDB.TextID.ERROR_PACKING_ITEMS);
                        }
                        IsValid = false;
                    }
                }
            }

            return IsValid;
        }
        internal void CheckForUnreadMail(Database db, uint characterID)
        {
            m_mailLastChecked = DateTime.Now;
            string nowStr = m_mailLastChecked.ToString("yyyy-MM-dd HH:mm:ss");
            SqlQuery query = new SqlQuery(db, "select * from mail where recipient_id =" + characterID + " and deleted = 0 and mail_read = 0 and delivery_time <= '" + nowStr + "'");

            if (query.HasRows)
            {
                while (query.Read())
                {
                    int senderID = query.GetInt32("sender_id");
                    if (m_owningCharacter != null && m_owningCharacter.HasBlockedCharacter(senderID) == false)
                    {
                        m_unreadMail = true;
                    }
                }
            }
            query.Close();
        }
        internal void LoadMailFromDatabase(Database db, uint characterID, List<FriendTemplate> blockedList)
        {
            m_mailLastChecked = DateTime.Now;
            string nowStr = m_mailLastChecked.ToString("yyyy-MM-dd HH:mm:ss");
            SqlQuery query = new SqlQuery(db, "select * from mail where recipient_id =" + characterID + " and deleted = 0 and delivery_time <= '" + nowStr + "'");
            m_mailList.Clear();
            lock (m_newList)
            {
                m_newList.Clear();
            }
            if (query.HasRows)
            {
                while (query.Read())
                {
                    PlayerMail newMail = new PlayerMail(query, db);
                    /*if (newMail.Read == false)
                    {
                        m_unreadMail = true;
                    }*/
                    if (FriendTemplate.ContainsTemplateForID(blockedList, newMail.SenderID) == null)
                    {
                        lock (m_newList)
                        {
                            m_newList.Add(newMail);
                        }
                    }
                    //m_mailList.Add(newMail);

                }
            }
            query.Close();
            m_sendMailList = true;
        }

        internal static void SendNewMailMessageToPlayer(bool inBackground, Player player)
        {
            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.MailMessage);
            outmsg.WriteVariableInt32((int)MailMessageType.MMT_UnreadMailNotification);

            NetConnection connection = player.connection;
            if (inBackground)
            {
                DelayedMessageDescriptor msgDesc = new DelayedMessageDescriptor(outmsg, connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.MailMessage, player);

                lock (CommandProcessor.m_delayedMessages)
                {
                    CommandProcessor.m_delayedMessages.Enqueue(msgDesc);
                }
            }
            else
            {
                Program.processor.SendMessage(outmsg, connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.MailMessage);
            }
        }
        internal void Update(DateTime currentTime)
        {
            /*if(currentTime>(m_mailLastChecked+TimeSpan.FromMinutes(MAIL_CHECK_FREQ_MIN)))
            {
                DateTime oldTime = m_mailLastChecked;
                m_mailLastChecked = currentTime;
                LoadNewMailTask task = new LoadNewMailTask(m_owningCharacter.m_player, m_owningCharacter.m_character_id, this, oldTime,m_mailLastChecked);
                task.m_TaskType = BaseTask.TaskType.LoadNewMail;
                lock (Program.processor.m_backgroundTasks)
                {
                    Program.processor.m_backgroundTasks.Enqueue(task);
                }
            }*/
            if (m_sendMailList)
            {
                m_sendMailList = false;
                SendMailList();
            }
        }

        PlayerMail GetMailForID(int mailID)
        {
            for (int i = 0; i < m_mailList.Count; i++)
            {
                PlayerMail currentMail = m_mailList[i];
                if (currentMail.MailID == mailID)
                {
                    return currentMail;
                }
            }
            return null;
        }
        internal static int GetCostForMessage(int messageLen, int goldAdded, int itemsAdded)
        {
            int cost = 1 + itemsAdded;
            /*int cost = 5 + 10 * itemsAdded;
            if (goldAdded > 0)
            {
                cost += 5;
            }*/
            return cost;
        }
    }
}