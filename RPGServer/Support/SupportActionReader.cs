using System;
using System.Collections.Generic;
using MainServer.Localise;

namespace MainServer.Support
{
    class SupportActionReader
    {
		// #localisation
		public class SupportActionReaderTextDB : TextEnumDB
		{
			public SupportActionReaderTextDB() : base(nameof(SupportActionReader), typeof(TextID)) { }

			public enum TextID
			{
				GOLD_REMOVED,				// "{amount0} Gold has been removed from your character"
				GOLD_ADDED,					// "{amount0} Gold has been added to your character"
				ITEMS_ADDED,				// "Items Added : {itemNames0}."
				ITEM_ADDED,					// "Item Added : {itemName0}."
				PLATINUM_REMOVED,			// "{amount0} Platinum has been removed from your account"
				PLATINUM_ADDED,				// "{amount0} Platinum has been added to your account"
				ITEM_REMOVED,				// "Item Removed : {quantity0} {itemName1}."
				QUEST_REMOVED,				// "Quest Removed, please check your quest page."
				GOLD_ADDED_BY_SUPPORT,		// "Gold Added By Support Team"
				ITEMS_ADDED_BY_SUPPORT,		// "Items Added By Support Team"
				SUPPORT,					// "Support Team"
			}
		}
		public static SupportActionReaderTextDB textDB = new SupportActionReaderTextDB();

		enum SupportActionCode
        {
            /// <summary>
            /// [character_id]|[amount]
            /// </summary>
            AddGold = 1,
            /// <summary>
            /// [character_id]|[item_id]|[amount]
            /// </summary>
            AddItem = 2,
            /// <summary>
            /// [account_id]|[amount]
            /// </summary>
            AddPlatinum = 3,
            /// <summary>
            /// [character_id]|[item_id]|[amount]
            /// </summary>
            RemoveItem = 4,
            /// <summary>
            /// [character_id]|[quest_id]
            /// </summary>
            RestartQuest = 5,
            /// <summary>
            /// [character_id]|[quest_id]|[stage_id]
            /// </summary>
            ResetQuestStage = 6,
            /// <summary>
            /// [character_id]|[quest_id]
            /// </summary>
            CompleteQuest =7,
            /// <summary>
            /// [character_id]|[quest_id]
            /// </summary>
            DeleteQuest =8,
            /// <summary>
            /// [character_id]|[quest_id]|[stage_id]
            /// </summary>
            CompleteQuestStage = 9,
            /// <summary>
            /// [character_id]|[message] 
            /// </summary>
            PopupMessage=10,
            /// <summary>
            /// [character_id]|[subject]|[message] 
            /// </summary>
            MailMessage = 11,
            // [character_id]
            KickPlayer = 12

        }
        int m_latestActionCompleted = -1;
        internal void Update()
        {
           
             SqlQuery query = new SqlQuery(Program.processor.m_worldDB, "select * from pending_action");
             if (query.HasRows == true)
             {
                 while (query.Read())
                 {
                     int actionID = query.GetInt32("pending_action_id");
                     int actionCode = query.GetInt32("action_code");
                     string actionDetails = query.GetString("action_details");
                     string[] actionDetailsArray = actionDetails.Split('|');

                     if (actionID > m_latestActionCompleted)
                     {
                         
                         bool actionSuccessful = TakeAction(actionID, actionCode, actionDetailsArray);
                         if (actionSuccessful==true)
                         {
                             Program.processor.m_worldDB.runCommandSync("delete from pending_action where pending_action_id=" + actionID);
                         }
                         m_latestActionCompleted = actionID;
                     }
                 }
             }
             query.Close();
        }
        bool TakeAction(int actionID,int actionCode, string[] actionDetailsArray)
        {
            bool actionSuccessful = false;
            SupportActionCode accountOptionsCode = (SupportActionCode)actionCode;

            switch (accountOptionsCode)
            {
                case SupportActionCode.AddGold:
                    {
                        //actionSuccessful = AddGold(actionID, actionDetailsArray);
                        actionSuccessful = AddGoldByMail(actionID, actionDetailsArray);
                        break;
                    }
                case SupportActionCode.CompleteQuest:
                    {
                        actionSuccessful = CompleteQuest(actionID, actionDetailsArray);
                        break;
                    }
                case SupportActionCode.AddItem:
                    {
                        //actionSuccessful = AddItem(actionID, actionDetailsArray);
                        actionSuccessful = AddItemByMail(actionID, actionDetailsArray);
                        break;
                    }
                case SupportActionCode.AddPlatinum:
                    {
                        actionSuccessful = AddPlatinum(actionID, actionDetailsArray);
                        break;
                    }
                case SupportActionCode.RemoveItem:
                    {
                        actionSuccessful = RemoveItem(actionID, actionDetailsArray);
                        break;
                    }
                case SupportActionCode.RestartQuest:
                    {
                        actionSuccessful = RestartQuest(actionID, actionDetailsArray);
                        break;
                    }
                case SupportActionCode.ResetQuestStage:
                    {
                        actionSuccessful = RestartQuestStage(actionID, actionDetailsArray);
                        break;
                    }
                case SupportActionCode.CompleteQuestStage:
                    {
                        actionSuccessful = CompleteQuestStage(actionID, actionDetailsArray);
                        break;
                    }
                case SupportActionCode.DeleteQuest:
                    {
                        actionSuccessful = DeleteQuest(actionID, actionDetailsArray);
                        break;
                    }
                case SupportActionCode.PopupMessage:
                    {
                        actionSuccessful = SendPopupMessage(actionID, actionDetailsArray);
                        break;
                    }
                case SupportActionCode.MailMessage:
                    {
                        actionSuccessful = SendMailMessage(actionID, actionDetailsArray);
                        break; 
                    }
                case SupportActionCode.KickPlayer:
                    {
                        var processor = Program.processor;

                        if (actionDetailsArray.Length != 1)
                        {
                            actionSuccessful = false;
                            break;
                        }

                        int characterID = Int32.Parse(actionDetailsArray[0]);

                        Player player = processor.getPlayerFromActiveCharacterId(characterID);

						if (player == null)
						{
							actionSuccessful = true;
							break;
						}

						Character character = player.m_activeCharacter;

                        if (character == null)
                        {
                            actionSuccessful = true;
                            break;
                        }

                        string locText = Localiser.GetString(textDB, player, (int)CommandProcessor.CommandProcessorTextDB.TextID.DISCONNECTED_BY_GM);
                        
                        // send them a disconnect message and dc them
                        processor.disconnect(player, true, locText);
                    
                        string locTextKicked = Localiser.GetString(textDB, player, (int)CommandProcessor.CommandProcessorTextDB.TextID.OTHER_KICKED);
                        locTextKicked = string.Format(locTextKicked, character.m_name);

                        // post to chat that they were kicked
                        processor.sendSystemMessage(locTextKicked, player, true, SYSTEM_MESSAGE_TYPE.NONE);
                        actionSuccessful = true;
                        break;
                    }
                default:
                    {
                        Program.Display("Unhandled Support Action Code " + accountOptionsCode + "for action " + actionID);
                        break;
                    }
            }

            return actionSuccessful;
        }
        int GetAccountIDForCharacterID(int characterID)
        {
            int accountID = -1;
             SqlQuery query = new SqlQuery(Program.processor.m_worldDB, "select account_id from character_details where character_id=" + characterID );
                if (query.Read())
                {
                     accountID = query.GetInt32("account_id");
                    
                }
                query.Close();
                return accountID;
        }
        string GetCharacterNameForCharacterID(int characterID)
        {
            string name ="";
            SqlQuery query = new SqlQuery(Program.processor.m_worldDB, "select name from character_details where character_id=" + characterID);
            if (query.Read())
            {
                name = query.GetString("name");

            }
            query.Close();
            return name;
        }
        bool CompleteQuest(int actionID, string[] actionDetailsArray)
        {
            if (actionDetailsArray.Length < 2)
            {
                return false;
            }
            bool actionComplete = false;
            int characterID = Int32.Parse(actionDetailsArray[0]);
            int questID = Int32.Parse(actionDetailsArray[1]);
            long accountID = -1;
            //is the character online 
            //if so get ahold of them
            Player activePlayer = Program.processor.getPlayerFromActiveCharacterId(characterID);
            Character activeCharacter = null;
            if (activePlayer != null)
            {
                activeCharacter = activePlayer.m_activeCharacter;
            }
            string reportString = "";
            if (activeCharacter != null)
            {
                //this should send enough data to resolve the issue
                activeCharacter.m_QuestManager.completeQuest(questID);

                accountID = activePlayer.m_account_id;
                reportString = "pending action CompleteQuest online quest_id : " + questID + ": pending action id:" + actionID + " accountID:" + accountID + " characterID:" + characterID;
               
                actionComplete = true;
            }
            else
            {
                accountID = GetAccountIDForCharacterID(characterID);
                Player thePlayer = new Player(Program.processor.m_universalHubDB, (int)accountID);
                string characterName = GetCharacterNameForCharacterID(characterID); 

                activeCharacter = Character.loadCharacter(Program.processor.m_worldDB, thePlayer, accountID, (uint)characterID, characterName);
                activeCharacter.m_QuestManager.completeQuest(questID);
                actionComplete = true;

                reportString = "pending action CompleteQuest offline quest_id : " + questID + ": pending action id:" + actionID + " accountID:" + accountID + " characterID:" + characterID;
                Program.Display(reportString);

            }
            if (actionComplete)
            {
                Program.processor.m_universalHubDB.runCommandSync("insert into account_journal (account_id,journal_date,user,details) values (" + accountID + ",'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',' Server : " + Program.m_ServerName + "',\"" + reportString + "\")");
            }

            if (actionComplete)
            {
                Program.processor.m_universalHubDB.runCommandSync("insert into account_journal (account_id,journal_date,user,details) values (" + accountID + ",'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',' Server : " + Program.m_ServerName + "',\"pending action CompleteQuest " + questID + ": pending action id:" + actionID + "\")");
            }
            return actionComplete;
        }
        bool AddGold(int actionID, string[] actionDetailsArray)
        {
            if (actionDetailsArray.Length < 2)
            {
                return false;
            }
            bool actionComplete = false;
            int characterID = Int32.Parse(actionDetailsArray[0]);
            int amount = Int32.Parse(actionDetailsArray[1]);

            long accountID = -1;
              //is the character online 
            //if so get ahold of them
            Player activePlayer = Program.processor.getPlayerFromActiveCharacterId(characterID);
            Character activeCharacter = null;
            if (activePlayer != null)
            {
                activeCharacter = activePlayer.m_activeCharacter;
            }
            if (activeCharacter != null)
            {
                activeCharacter.updateCoins(amount);
                if (amount < 0)
                {
					string locText = Localiser.GetString(textDB, activePlayer, (int)SupportActionReaderTextDB.TextID.GOLD_REMOVED);
					locText = string.Format(locText, -amount);
					Program.processor.sendSystemMessage(locText, activePlayer, true, SYSTEM_MESSAGE_TYPE.POPUP);
				}
                else
                {
					string locText = Localiser.GetString(textDB, activePlayer, (int)SupportActionReaderTextDB.TextID.GOLD_ADDED);
					locText = string.Format(locText, amount);
					Program.processor.sendSystemMessage(locText, activePlayer, true, SYSTEM_MESSAGE_TYPE.POPUP);
				}
                activeCharacter.m_inventory.SendUseItemReply("", 0.0f);
                actionComplete = true;
                accountID = activePlayer.m_account_id;
                Program.Display("pending action AddGold online amount : " + amount + ": pending action id:" + actionID + " accountID:" + accountID + " characterID:" + characterID);
       
            }
            else
            {

                Program.processor.m_worldDB.runCommandSync("update character_details set coins=coins+" + amount + " where character_id=" + characterID);
                actionComplete = true;
                accountID = GetAccountIDForCharacterID(characterID);

                Program.Display("pending action AddGold offline amount : " + amount +": pending action id:" + actionID + " accountID:" + accountID + " characterID:" + characterID);
       
            }
            if (actionComplete)
            {
                Program.processor.m_universalHubDB.runCommandSync("insert into account_journal (account_id,journal_date,user,details) values (" + accountID + ",'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',' Server : " + Program.m_ServerName + "',\"pending action addgold " + amount + ": pending action id:" + actionID + "\")");
            }

            return actionComplete;
        }
        bool AddGoldByMail(int actionID, string[] actionDetailsArray)
        {
            if (actionDetailsArray.Length < 2)
            {
                return false;
            }
            bool actionComplete = false;
            int characterID = Int32.Parse(actionDetailsArray[0]);
            int amount = Int32.Parse(actionDetailsArray[1]);
            Player activePlayer = Program.processor.getPlayerFromActiveCharacterId(characterID);
            Character activeCharacter = null;
            long accountID = -1;
            if (activePlayer != null)
            {
                accountID = activePlayer.m_account_id;
                activeCharacter = activePlayer.m_activeCharacter;
            }
            else
            {
                accountID = GetAccountIDForCharacterID(characterID);
            }
            if (amount > 0)
            {
                int mailID = Program.processor.getAvailableMailID();
				int languageIndex = Localiser.GetLanguageIndexOfCharacter(characterID);
				string locSubject = Localiser.GetStringByLanguageIndex(textDB, languageIndex, (int)SupportActionReaderTextDB.TextID.GOLD_ADDED_BY_SUPPORT);
				string locSenderName = Localiser.GetStringByLanguageIndex(textDB, languageIndex, (int)SupportActionReaderTextDB.TextID.SUPPORT);
				Mailbox.SaveOutMail(characterID, locSubject, "", amount, 0, mailID, -1, locSenderName, false);
				actionComplete = true;
                Mailbox.NotifyOnlinePlayerWithCharacterID(-1, characterID);
                Program.processor.m_universalHubDB.runCommandSync("insert into account_journal (account_id,journal_date,user,details) values (" + accountID + ",'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',' Server : " + Program.m_ServerName + "',\"pending action addgold " + amount + ": pending action id:" + actionID + "\")");
         
            }
            else
            {
                actionComplete = AddGold(actionID, actionDetailsArray);
            }


            return actionComplete;
        }

        bool AddItemByMail(int actionID, string[] actionDetailsArray)
        {
            if (actionDetailsArray.Length < 3)
            {
                return false;
            }
            bool actionComplete = false;
            int characterID = Int32.Parse(actionDetailsArray[0]);
            int totalItems = (actionDetailsArray.Length - 1) / 2;
            string displayStr = "";
           // string messageStr = "";
            long accountID = -1;
            Player activePlayer = Program.processor.getPlayerFromActiveCharacterId(characterID);
            accountID = activePlayer != null ? activePlayer.m_account_id : GetAccountIDForCharacterID(characterID);
            //is the character online 
            //if so get ahold of them
         
            
			int mailID = Program.processor.getAvailableMailID();
			int mailItemID = 0;
			int languageIndex = Localiser.GetLanguageIndexOfCharacter(characterID);
			string locSubject = Localiser.GetStringByLanguageIndex(textDB, languageIndex, (int)SupportActionReaderTextDB.TextID.ITEMS_ADDED_BY_SUPPORT);
			string locSenderName = Localiser.GetStringByLanguageIndex(textDB, languageIndex, (int)SupportActionReaderTextDB.TextID.SUPPORT);
			Mailbox.SaveOutMail(characterID, locSubject, "", 0, totalItems, mailID, -1, locSenderName, false);
			for (int itemno = 0; itemno < totalItems; itemno++)
                {
                    string itemString = actionDetailsArray[itemno * 2 + 1];
                    string[] itemSubStrings = itemString.Split(new char[] { '^' });
                    int itemID = Int32.Parse(itemSubStrings[0]);
                    bool itemBound = false;
                    if (itemSubStrings.Length > 1)
                    {
                        itemBound = itemSubStrings[1].Equals("1");
                    }
                    int quantity = Int32.Parse(actionDetailsArray[itemno * 2 + 2]);
                    Item newItem = null;//activeCharacter.m_inventory.AddNewItemToCharacterInventory(itemID, quantity, false);
                    //newItem = new Item(-1, itemID, quantity, -1);
                    //get the template
                    ItemTemplate itemTemp = ItemTemplateManager.GetItemForID(itemID);
                    
                    if (itemTemp != null)
                    {
                        actionComplete = true;

                        if (itemTemp.m_stackable == true)
                        {
                            mailItemID++;
                            newItem = new Item(mailItemID, itemID, quantity, -1);//activeCharacter.m_inventory.AddNewItemToCharacterInventory(itemID, quantity, false);

                            if (itemBound)
                            {
                                newItem.m_bound = true;
                            }
                            string insertString = "(mail_id," + newItem.GetInsertFieldsString() + ") values (" + mailID + "," + newItem.GetInsertValuesString(-1) + ")";
                            Program.processor.m_worldDB.runCommandSync("insert into mail_inventory " + insertString);
                            displayStr += ",stacking" + itemID + " Quantity " + quantity;

                        }

                       //otherwise add them one at a time
                        else
                        {
                            for (int i = 0; i < quantity; i++)
                            {
                                mailItemID++;
                                newItem = new Item(mailItemID, itemID, 1, -1);//activeCharacter.m_inventory.AddNewItemToCharacterInventory(itemID, 1, false);
                                if (itemBound)
                                {
                                    newItem.m_bound = true;
                                }
                                string insertString = "(mail_id," + newItem.GetInsertFieldsString() + ") values (" + mailID + "," + newItem.GetInsertValuesString(-1) + ")";
                                Program.processor.m_worldDB.runCommandSync("insert into mail_inventory " + insertString);
                            }
                            displayStr += ", not stacking " + itemID + " Quantity " + quantity;
                        }
                    }
                    if (displayStr != "")
                    {
                        Program.Display("pending action AddItem " + displayStr.Substring(1) + ": pending action id:" + actionID + " accountID:" + accountID + " characterID:" + characterID);
                        
                    }
                   
            }
                if (displayStr.Length > 1)
                {
                    displayStr = displayStr.Substring(1);
                }
                if (actionComplete)
                {
                    Program.processor.m_universalHubDB.runCommandSync("insert into account_journal (account_id,journal_date,user,details) values (" + accountID + ",'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',' Server : " + Program.m_ServerName + "',\"pending action AddItem " + displayStr + ": pending action id:" + actionID + "\")");
                }
                Mailbox.NotifyOnlinePlayerWithCharacterID(-1, characterID);
            return actionComplete;
        }
        bool AddPlatinum(int actionID, string[] actionDetailsArray)
        {
             if (actionDetailsArray.Length < 2)
            {
                return false;
            }
            bool actionComplete = false;
            long accountID = Int32.Parse(actionDetailsArray[0]);
            int amount = Int32.Parse(actionDetailsArray[1]);
           
           
            //is the character online 
            //if so get ahold of them
            Player activePlayer = Program.processor.getPlayerFromAccountId((int)accountID);

            if (activePlayer != null)
            {
                activePlayer.m_platinum += amount;
                activePlayer.SavePlatinum(0,0);
                PremiumShop.SendPlatinumConfirmation(activePlayer, 1, "", "");
                Program.Display("pending action AddPlatinum online amount : " + amount + ": pending action id:" + actionID + " accountID:" + accountID );
       
                if (amount < 0)
                {
					string locText = Localiser.GetString(textDB, activePlayer, (int)SupportActionReaderTextDB.TextID.PLATINUM_REMOVED);
					locText = string.Format(locText, -amount);
					Program.processor.sendSystemMessage(locText, activePlayer, true, SYSTEM_MESSAGE_TYPE.POPUP);

				}
                else
                {
					string locText = Localiser.GetString(textDB, activePlayer, (int)SupportActionReaderTextDB.TextID.PLATINUM_ADDED);
					locText = string.Format(locText, amount);
					Program.processor.sendSystemMessage(locText, activePlayer, true, SYSTEM_MESSAGE_TYPE.POPUP);

				}
                actionComplete = true;
            }
            else
            {
                Program.Display("pending action AddPlatinum offline amount : " + amount + ": pending action id:" + actionID + " accountID:" + accountID);
       
                Program.processor.m_universalHubDB.runCommandSync("update account_details set platinum=platinum+" + amount + " where account_id=" + accountID);
                actionComplete = true;
            }
            if (actionComplete)
            {
                Program.processor.m_universalHubDB.runCommandSync("insert into account_journal (account_id,journal_date,user,details) values (" + accountID + ",'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',' Server : " + Program.m_ServerName + "',\"pending action AddPlatinum " + amount + ": pending action id:" + actionID + "\")");
            }
            return actionComplete;
        }
        bool RemoveItem(int actionID, string[] actionDetailsArray)
        {
            if (actionDetailsArray.Length < 3)
            {
                return false;
            }
            bool actionComplete = false;
            int characterID = Int32.Parse(actionDetailsArray[0]);
            int templateID = Int32.Parse(actionDetailsArray[1]);
            int quantity = Int32.Parse(actionDetailsArray[2]);

            long accountID = -1;
            //is the character online 
            //if so get ahold of them
            Player activePlayer = Program.processor.getPlayerFromActiveCharacterId(characterID);
            Character activeCharacter = null;
            if (activePlayer != null)
            {
                accountID = activePlayer.m_account_id;
                activeCharacter = activePlayer.m_activeCharacter;
            }
            //how many items have been located and removed
            int removedCount = 0;
            if (activeCharacter != null)
            {
                int quantityToRemove = quantity - removedCount;
                int numRemovedFromBag = RemoveItemsOfTypeFromInventory(activeCharacter.m_inventory, templateID, quantityToRemove);
                removedCount += numRemovedFromBag;
                quantityToRemove = quantity - removedCount;

                if (quantityToRemove > 0)
                {
                    int numRemovedFromBank = RemoveItemsOfTypeFromInventory(activeCharacter.m_SoloBank, templateID, quantityToRemove);
                    removedCount += numRemovedFromBank;
                    quantityToRemove = quantity - removedCount;
                    if (numRemovedFromBank > 0)
                    {
                        Program.processor.SendBankReply(activePlayer, 0);
                    }
                }

                ItemTemplate itemTemp = ItemTemplateManager.GetItemForID(templateID);
                actionComplete = true;
                
                if (numRemovedFromBag > 0)
                {
                    Program.processor.SendDeleteItemReply("", activePlayer);
                }
                if (itemTemp != null)
                {
					string locText = Localiser.GetString(textDB, activePlayer, (int)SupportActionReaderTextDB.TextID.ITEM_REMOVED);
					locText = string.Format(locText, quantity, itemTemp.m_loc_item_name[activePlayer.m_languageIndex]);
					Program.processor.sendSystemMessage(locText, activePlayer, true, SYSTEM_MESSAGE_TYPE.POPUP);

				}
                Program.Display("pending action RemoveItem online " + templateID + " quantity " + quantity + ": pending action id:" + actionID + " removed " + removedCount + " of " + quantity + " accountID:" + accountID + " characterID:" + characterID);
       
            }
            else
            {
                int quantityToRemove = quantity - removedCount;
                int numRemovedFromBag = RemoveItemsOfTypeFromInventoryOfType("inventory",characterID, templateID, quantityToRemove);
                removedCount += numRemovedFromBag;
                quantityToRemove = quantity - removedCount;

                if (quantityToRemove > 0)
                {
                    int numRemovedFromBank = RemoveItemsOfTypeFromInventoryOfType("bank", characterID, templateID, quantityToRemove);
                    removedCount += numRemovedFromBank;
                    quantityToRemove = quantity - removedCount;
                }
                accountID = GetAccountIDForCharacterID(characterID);
                actionComplete = true;
                Program.Display("pending action RemoveItem offline " + templateID + " quantity " + quantity + ": pending action id:" + actionID + " removed " + removedCount + " of " + quantity + " accountID:" + accountID+" characterID:"+characterID);
       
            }

            if (actionComplete)
            {
                if (removedCount == quantity)
                {
                    Program.processor.m_universalHubDB.runCommandSync("insert into account_journal (account_id,journal_date,user,details) values (" + accountID + ",'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',' Server : " + Program.m_ServerName + "',\"pending action RemoveItem " + templateID + " quantity " + quantity + ": pending action id:" + actionID + " removed " + removedCount + " of " + quantity + "\")");
                }
                else
                {
                    Program.processor.m_universalHubDB.runCommandSync("insert into account_journal (account_id,journal_date,user,details) values (" + accountID + ",'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',' Server : " + Program.m_ServerName + "',\"pending action RemoveItem error " + templateID + " quantity " + quantity + ": pending action id:" + actionID + " removed " + removedCount + " of " + quantity + "\")");
                }
            }
            return actionComplete;
        }
        int RemoveItemsOfTypeFromInventoryOfType(string inventoryType, int characterID, int templateID, int quantity)
        {
            int removedCount = 0;
            //read all of the items of this type into a list
            List<Item> invItems = new List<Item>();
            //get all items belonging to this character where the template ID = templateID
            SqlQuery query = new SqlQuery(Program.processor.m_worldDB, "select * from " + inventoryType + " where character_id =" + characterID + " and item_id = " + templateID);
            if (query.HasRows)
            {


                while ((query.Read()))
                {
                    int inventoryID = query.GetInt32("inventory_id");

                    int itemID = query.GetInt32("item_id");
                    int currentQuantity = query.GetInt32("quantity");
                    bool bound = query.GetBoolean("bound");
                    int remainingCharges = query.GetInt32("remaining_charges"); ;
                    double timeRecharged = query.GetDouble("time_skill_last_cast");
                    int sortOrder = query.GetInt32("sort_order");
                    Item newItem = new Item(inventoryID, itemID, currentQuantity, bound, remainingCharges, timeRecharged, sortOrder, false);
                    invItems.Add(newItem);
                }


            }
            query.Close();
            if (invItems.Count > 0)
            {
                //items that are to be removed from the inventory completely
                List<Item> itemsToDelete = new List<Item>();
                // if an item is to be removed but the stack still exists
                Item itemToChangeQuantity = null;

                for (int i = 0; i < invItems.Count && removedCount < quantity && itemToChangeQuantity == null; i++)
                {
                    int quantityToRemove = quantity - removedCount;
                    Item currentItem = invItems[i];

                    //if the quantity is <= total  To Remove
                    //delete the whole item
                    if (quantityToRemove >= currentItem.m_quantity)
                    {
                        itemsToDelete.Add(currentItem);
                        removedCount += currentItem.m_quantity;
                    }
                    else
                    {
                        currentItem.m_quantity -= quantityToRemove;
                        removedCount += quantityToRemove;
                        itemToChangeQuantity = currentItem;
                    }

                }
                if (itemsToDelete.Count > 0)
                {
                    string idsToDelete = "";
                    for (int i = 0; i < itemsToDelete.Count; i++)
                    {
                        Item currentItem = itemsToDelete[i];
                        if (currentItem != null)
                        {
                            if (i > 0)
                            {
                                idsToDelete += ",";
                            }

                            idsToDelete += currentItem.m_inventory_id;

                        }
                    }
                    if (idsToDelete != "")
                    {
                        Program.processor.m_worldDB.runCommandSync("delete from " + inventoryType + " where inventory_id in (" + idsToDelete + ")");

                    }
                }
                if (itemToChangeQuantity != null)
                {
                    Program.processor.m_worldDB.runCommandSync("update " + inventoryType + " set quantity=" + itemToChangeQuantity.m_quantity + " where inventory_id=" + itemToChangeQuantity.m_inventory_id);

                }
            }

            return removedCount;
        }
        int RemoveItemsOfTypeFromInventory(Inventory inv,int templateID, int quantity)
        {
            int removedCount = 0;
            //first try the inventory
            //the maximum possible items that could be of the desired type 
            //number of items in the bag + 4 rings could be equipped
            int maxCheckCount = inv.m_bagItems.Count + 4;
            // have all of the items of type templateID been found & removed
            bool allOfTypeFound = false;

            int currentCheck = 0;
            //it's possible for it to be in many stacks
            //so the search msy need to be done more than once
            while ((currentCheck < maxCheckCount) && (removedCount < quantity) && (allOfTypeFound == false))
            {
                currentCheck++;
                //need to  try to find the items
                Item correctItem = inv.GetItemFromTemplateID(templateID, true);
                //do they have any of this item
                if (correctItem != null)
                {
                    //you have found the item
                    //how many are there
                    int currentQuantity = correctItem.m_quantity;
                    //how many of these do you need to remove
                    int quantityToRemove = quantity - removedCount;
                    if (currentQuantity < quantityToRemove)
                    {
                        quantityToRemove = currentQuantity;
                    }

                    //try to remove the items
                    string errorString = inv.DeleteItem(correctItem.m_template_id, correctItem.m_inventory_id, quantityToRemove);
                    //if sucessful then update the number removed
                    if (errorString == "")
                    {
                        removedCount += quantityToRemove;
                    }
                    //if unsucessfull, stop the search and report
                    else
                    {
                        allOfTypeFound = true;
                    }
                }
                //if they do not have the item stop looking in this bag
                else
                {
                    allOfTypeFound = true;
                }
            }
            return removedCount;
        }
        bool SendPopupMessage(int actionID, string[] actionDetailsArray)
        {
            bool actionComplete = false;
             if (actionDetailsArray.Length < 2)
            {
                return false;
            }
            
            int characterID = Int32.Parse(actionDetailsArray[0]);
            string message = actionDetailsArray[1];
            
            long accountID = -1;
            //is the character online 
            //if so get ahold of them
            Player activePlayer = Program.processor.getPlayerFromActiveCharacterId(characterID);
            Character activeCharacter = null;
            if (activePlayer != null)
            {

                activeCharacter = activePlayer.m_activeCharacter;
            }
            if (activeCharacter != null)
            {
                Program.processor.sendSystemMessage(message, activePlayer, true, SYSTEM_MESSAGE_TYPE.POPUP);

                Program.Display("pending action PopupMessage online \" " + message +" \": pending action id:" + actionID+ " accountID:" + accountID + " characterID:" + characterID);
       
                actionComplete = true;
            }
            else
            {

                accountID = GetAccountIDForCharacterID(characterID);
                Program.processor.m_universalHubDB.runCommandSync("insert into message_to_player (account_id,message_text) values (" + accountID + ", \"" + message + "\")");
                Program.Display("pending action PopupMessage offline \" " + message + " \": pending action id:" + actionID + " accountID:" + accountID + " characterID:" + characterID);
                actionComplete = true;
            }
            if (actionComplete == true)
            {
                Program.processor.m_universalHubDB.runCommandSync("insert into account_journal (account_id,journal_date,user,details) values (" + accountID + ",'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',' Server : " + Program.m_ServerName + "',\"pending action PopupMessage :" + message +  ": pending action id:" + actionID  + "\")");
            }   
            return actionComplete;

        }
        bool SendMailMessage(int actionID, string[] actionDetailsArray)
        {
            bool actionComplete = false;
            if (actionDetailsArray.Length < 3)
            {
                return false;
            }

            int characterID = Int32.Parse(actionDetailsArray[0]);
            string subject = actionDetailsArray[1];
            string message = actionDetailsArray[2];

            long accountID = -1;
            //is the character online 
            //if so get ahold of them
            Player activePlayer = Program.processor.getPlayerFromActiveCharacterId(characterID);
            Character activeCharacter = null;
            if (activePlayer != null)
            {
                accountID = activePlayer.m_account_id;
                activeCharacter = activePlayer.m_activeCharacter;
            }
            else
            {
                accountID = GetAccountIDForCharacterID(characterID);
            }
          
			int mailID = Program.processor.getAvailableMailID();
			int languageIndex = Localiser.GetLanguageIndexOfCharacter(characterID);
			string locSenderName = Localiser.GetStringByLanguageIndex(textDB, languageIndex, (int)SupportActionReaderTextDB.TextID.SUPPORT);
			Mailbox.SaveOutMail(characterID, subject, message, 0, 0, mailID, -1, locSenderName, false);
			actionComplete = true;
            Mailbox.NotifyOnlinePlayerWithCharacterID(-1, characterID);
            Program.processor.m_universalHubDB.runCommandSync("insert into account_journal (account_id,journal_date,user,details) values (" + accountID + ",'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',' Server : " + Program.m_ServerName + "',\"pending action MailMessage:'" + subject + "': pending action id:" + actionID + "\")");
       
            actionComplete = true;
            
            
            return actionComplete;

        }
        internal bool RestartQuest(int actionID, string[] actionDetailsArray)
        {
            if (actionDetailsArray.Length < 2)
            {
                return false;
            }
            bool actionComplete = false;
            int characterID = Int32.Parse(actionDetailsArray[0]);
            int questID = Int32.Parse(actionDetailsArray[1]);


            long accountID = -1;
            //is the character online 
            //if so get ahold of them
            Player activePlayer = Program.processor.getPlayerFromActiveCharacterId(characterID);
            Character activeCharacter = null;
            if (activePlayer != null)
            {
                accountID = activePlayer.m_account_id;
                activeCharacter = activePlayer.m_activeCharacter;
            }
            string reportString = "";
            if (activeCharacter != null)
            {
                activeCharacter.m_QuestManager.RestartQuest(questID);
                activeCharacter.m_QuestManager.SendQuestRefresh();
                actionComplete = true;
                reportString = "pending action RestartQuest online quest_id : " + questID + ": pending action id:" + actionID + " accountID:" + accountID + " characterID:" + characterID;
                Program.Display(reportString);
            }
            else
            {
                accountID = GetAccountIDForCharacterID(characterID);
                QuestManager.RestartQuestOffline(questID, characterID);
                actionComplete = true;
                reportString = "pending action RestartQuest offline quest_id : " + questID + ": pending action id:" + actionID + " accountID:" + accountID + " characterID:" + characterID;
                Program.Display(reportString);

            }
            if (actionComplete)
            {
                Program.processor.m_universalHubDB.runCommandSync("insert into account_journal (account_id,journal_date,user,details) values (" + accountID + ",'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',' Server : " + Program.m_ServerName + "',\""+reportString+"\")");
            }
            return actionComplete;
        }
        internal bool RestartQuestStage(int actionID, string[] actionDetailsArray)
        {
            if (actionDetailsArray.Length < 3)
            {
                return false;
            }
            bool actionComplete = false;
            int characterID = Int32.Parse(actionDetailsArray[0]);
            int questID = Int32.Parse(actionDetailsArray[1]);
            int stageID = Int32.Parse(actionDetailsArray[2]);

            long accountID = -1;
            //is the character online 
            //if so get ahold of them
            Player activePlayer = Program.processor.getPlayerFromActiveCharacterId(characterID);
            Character activeCharacter = null;
            if (activePlayer != null)
            {
                accountID = activePlayer.m_account_id;
                activeCharacter = activePlayer.m_activeCharacter;
            }
            string reportString = "";
            if (activeCharacter != null)
            {
                activeCharacter.m_QuestManager.RestartQuestStage(questID, stageID);
                activeCharacter.m_QuestManager.SendQuestRefresh();
                actionComplete = true;
                reportString = "pending action RestartQuestStage online quest_id : " + questID + ", stage_id : " + stageID + ": pending action id:" + actionID + " accountID:" + accountID + " characterID:" + characterID;
                Program.Display(reportString);
            }
            else
            {
                accountID = GetAccountIDForCharacterID(characterID);
                QuestManager.RestartQuestStageOffline(questID, stageID, characterID);
                actionComplete = true;
                reportString = "pending action RestartQuestStage offline quest_id : " + questID + ", stage_id : "+stageID+": pending action id:" + actionID + " accountID:" + accountID + " characterID:" + characterID;
                Program.Display(reportString);

            }
            if (actionComplete)
            {
                Program.processor.m_universalHubDB.runCommandSync("insert into account_journal (account_id,journal_date,user,details) values (" + accountID + ",'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',' Server : " + Program.m_ServerName + "',\"" + reportString + "\")");
            }
            return actionComplete;
        }
        internal bool CompleteQuestStage(int actionID, string[] actionDetailsArray)
        {
            if (actionDetailsArray.Length < 3)
            {
                return false;
            }
            bool actionComplete = false;
            int characterID = Int32.Parse(actionDetailsArray[0]);
            int questID = Int32.Parse(actionDetailsArray[1]);
            int stageID = Int32.Parse(actionDetailsArray[2]);

            long accountID = -1;
            //is the character online 
            //if so get ahold of them
            Player activePlayer = Program.processor.getPlayerFromActiveCharacterId(characterID);
            Character activeCharacter = null;
            if (activePlayer != null)
            {
                accountID = activePlayer.m_account_id;
                activeCharacter = activePlayer.m_activeCharacter;
            }
            string reportString = "";
            if (activeCharacter != null)
            {
                activeCharacter.m_QuestManager.CompleteQuestStageSupportBase(questID, stageID);
                activeCharacter.m_QuestManager.SendQuestRefresh();
                actionComplete = true;
                reportString = "pending action CompleteQuestStage online quest_id : " + questID + ", stage_id : " + stageID + ": pending action id:" + actionID + " accountID:" + accountID + " characterID:" + characterID;
                Program.Display(reportString);
            }
            else
            {
                accountID = GetAccountIDForCharacterID(characterID);
                Player thePlayer = new Player(Program.processor.m_universalHubDB, (int)accountID);
                string characterName = GetCharacterNameForCharacterID(characterID);

                activeCharacter = Character.loadCharacter(Program.processor.m_worldDB, thePlayer, accountID, (uint)characterID, characterName);
                activeCharacter.m_QuestManager.CompleteQuestStageSupportBase(questID, stageID);
                actionComplete = true;
                reportString = "pending action CompleteQuestStage offline quest_id : " + questID + ", stage_id : " + stageID + ": pending action id:" + actionID + " accountID:" + accountID + " characterID:" + characterID;
                Program.Display(reportString);

            }
            if (actionComplete)
            {
                Program.processor.m_universalHubDB.runCommandSync("insert into account_journal (account_id,journal_date,user,details) values (" + accountID + ",'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',' Server : " + Program.m_ServerName + "',\"" + reportString + "\")");
            }
            return actionComplete;
        }
        internal bool DeleteQuest(int actionID, string[] actionDetailsArray)
        {
            if (actionDetailsArray.Length < 2)
            {
                return false;
            }
            bool actionComplete = false;
            int characterID = Int32.Parse(actionDetailsArray[0]);
            int questID = Int32.Parse(actionDetailsArray[1]);


            long accountID = -1;
            //is the character online 
            //if so get ahold of them
            Player activePlayer = Program.processor.getPlayerFromActiveCharacterId(characterID);
            Character activeCharacter = null;
            if (activePlayer != null)
            {
                accountID = activePlayer.m_account_id;
                activeCharacter = activePlayer.m_activeCharacter;
            }
            string reportString = "";
            if (activeCharacter != null)
            {
                activeCharacter.m_QuestManager.DeleteQuest(questID,true);
                QuestTemplate template = Program.processor.m_QuestTemplateManager.GetQuestTemplate(questID);

				string locText = Localiser.GetString(textDB, activePlayer, (int)SupportActionReaderTextDB.TextID.QUEST_REMOVED);
				Program.processor.sendSystemMessage(locText, activePlayer, true, SYSTEM_MESSAGE_TYPE.POPUP);

				activeCharacter.m_QuestManager.SendQuestRefresh();
                actionComplete = true;
                reportString = "pending action DeleteQuest online quest_id : " + questID + ": pending action id:" + actionID + " accountID:" + accountID + " characterID:" + characterID;
                Program.Display(reportString);
            }
            else
            {
                accountID = GetAccountIDForCharacterID(characterID);
                QuestManager.DeleteQuestOffline(questID, characterID);
                actionComplete = true;
                reportString = "pending action DeleteQuest offline quest_id : " + questID +  ": pending action id:" + actionID + " accountID:" + accountID + " characterID:" + characterID;
                Program.Display(reportString);

            }
            if (actionComplete)
            {
                Program.processor.m_universalHubDB.runCommandSync("insert into account_journal (account_id,journal_date,user,details) values (" + accountID + ",'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',' Server : " + Program.m_ServerName + "',\"" + reportString + "\")");
            }
            return actionComplete;
        }
    }
}
