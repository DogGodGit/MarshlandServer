using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Analytics.Simple;
using Analytics.Global;
using Analytics.Monetisation;
using Analytics.Gameplay;
using Analytics.Social;
using Analytics.Engagment;
using MainServer.AuctionHouse.Enums;

namespace MainServer
{
    public class AnalyticsMain
    {
        //each object will set this manually when created
        public bool m_runCmdSync;

        public AnalyticsMain(bool i_RunCmdSync)
        {
            m_runCmdSync = i_RunCmdSync;
        }

        //SIMPLE
        internal void newAccount(Player player)
        {
            if (player != null)
            {
                //Analytics Code and Query
                newPlayerEvent newPlayer = new newPlayerEvent(player.m_account_id.ToString(), player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"), player.m_UserName.ToString());
                getJSONAddToDB(newPlayer, m_runCmdSync, player.m_testAccount);
            }
        }

        /*
         * deviceModel
         * deviceGen
         * deviceIOS
         * deviceStr
         */
        internal void clientDevice(Player player, string deviceGen, string deviceIOS, string deviceStr)
        {
            ClientDevice clientDevice = new ClientDevice(player.m_account_id.ToString(), player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));
            string originalStr = deviceIOS;
            string[] osSplit = originalStr.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            clientDevice.eventParams.deviceType = deviceStr;
            clientDevice.eventParams.manufacturer = "APPLE";
            clientDevice.eventParams.operatingSystem = "iOS " + osSplit[0];

            for (int i = 1; i < osSplit.Length; i++)
            {
                if (i < osSplit.Length - 1)
                {
                    clientDevice.eventParams.operatingSystemVersion += osSplit[i] + ".";
                }
                else
                {
                    clientDevice.eventParams.operatingSystemVersion += osSplit[i];
                }
            }

            getJSONAddToDB(clientDevice, m_runCmdSync, player.m_testAccount);
        }

        internal void newCharacter(Character character, Player player)
        {
            if (character != null && player != null)
            {
                //Analytics Code and Query
                characterCreatedEvent charCreate = new characterCreatedEvent(player.m_account_id.ToString(), player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));

                charCreate.eventParams.serverName = Program.m_ServerName;
                charCreate.eventParams.characterName = character.m_name;
                charCreate.eventParams.characterID = character.m_character_id.ToString();
                charCreate.eventParams.characterClass = character.m_class.m_classType.ToString();
                charCreate.eventParams.characterGender = character.m_gender.ToString();

                charCreate.goalCounts.setValues(player);

                getJSONAddToDB(charCreate, m_runCmdSync, player.m_testAccount);
            }
        }

        internal void startedPlaying(Player player)
        {
            if (player != null)
            {
                gameStartedEvent gameStart = new gameStartedEvent(player.m_account_id.ToString(), player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));

                //not stored anywhere that this can be changed other than hard coding
                gameStart.eventParams.clientVersion = "3";
                gameStart.eventParams.dataVersion = Program.processor.m_patchVersion.ToString();//patch version
                gameStart.eventParams.serverVersion = "7";

                gameStart.customParams.serverName = Program.m_ServerName;
                gameStart.customParams.characterName = player.m_activeCharacter.Name;
                gameStart.customParams.characterID = player.m_activeCharacter.m_character_id.ToString();

                gameStart.goalCounts.setValues(player);

                getJSONAddToDB(gameStart, m_runCmdSync, player.m_testAccount);
            }
        }

        internal void stoppedPlaying(Player player,string reason)
        {
            if (player != null)
            {
                gameEndedEvent gameEnd = new gameEndedEvent(player.m_account_id.ToString(), player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));

                gameEnd.goalCounts.setValues(player);
                CustomParams_GameEnded customParams = gameEnd.customParams;
                customParams.reasonForEnd = reason;

                getJSONAddToDB(gameEnd, m_runCmdSync, player.m_testAccount);
            }
        }
        
        //MONETISATION
        //Item Shop
        /*
         * SPENT - 
            * Real Money 
            * Platinum
         * RECEIVED - 
            * Platinum
            * Items
            * Gold
         */
        
        //Item Shop
        internal void itemShopPlatPurchase(Player player, int realCurrencySpent, int platinumReceived, string i_transactionReceipt)
        {
            if (player != null)
            {
                TransactionItem productsSpent = TransactionItem.realCurrency;
                TransactionItem productsReceived = TransactionItem.virtualCurrency;

                Transaction transaction = new Transaction(player.m_account_id.ToString(), player.m_sessionID.ToString(), productsSpent, productsReceived, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));

                transaction.eventParams.transactionName = "Platinum Purchase";
                transaction.eventParams.transactionID = player.m_account_id.ToString();
                transaction.eventParams.transactorID = player.m_account_id.ToString();

                transaction.eventParams.transactionReceipt = i_transactionReceipt;
                transaction.eventParams.transactionServer = TransactionServer.APPLE.ToString();
                transaction.eventParams.paymentCountry = PaymentCountry.GB.ToString();
                transaction.eventParams.transactionType = TransactionType.PURCHASE.ToString();

                //WILL BE SUBTRACTED FROM THE SERIALISED STRING
                transaction.eventParams.isInitiator = false;
                //**********************************

                transaction.eventParams.getProductsSpentRC().realCurrency.realCurrencyType = RealCurrencyType.GBP.ToString();
                transaction.eventParams.getProductsSpentRC().realCurrency.realCurrencyAmount = realCurrencySpent;
                transaction.eventParams.getProductsReceivedVC().addItem("Platinum", VirtualCurrencyType.PREMIUM.ToString(), platinumReceived);

                transaction.goalCounts.setValues(player);
                //subtract of non-related members
                string tempJSON = getJSONString(transaction);
                tempJSON = removeIsInitiator(tempJSON);
                addToDB(m_runCmdSync, tempJSON, player.m_testAccount);
            }
        }

        internal void itemShopGoldPurchase(Player player, int platSpent, int goldReceived)
        {
            if (player != null)
            {
                TransactionItem productsSpent = TransactionItem.virtualCurrency;
                TransactionItem productsReceived = TransactionItem.virtualCurrency;

                Transaction transaction = new Transaction(player.m_account_id.ToString(), player.m_sessionID.ToString(), productsSpent, productsReceived, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));

                transaction.eventParams.transactionName = "Gold Purchase";
                transaction.eventParams.transactionID = player.m_account_id.ToString();
                transaction.eventParams.transactorID = player.m_account_id.ToString();

                transaction.eventParams.transactionReceipt = "";
                transaction.eventParams.transactionServer = "";
                transaction.eventParams.paymentCountry = "";
                transaction.eventParams.transactionType = TransactionType.PURCHASE.ToString();

                //WILL BE SUBTRACTED FROM THE SERIALISED STRING
                transaction.eventParams.isInitiator = false;
                //**********************************

                transaction.eventParams.getProductsSpentVC().addItem("Platinum", VirtualCurrencyType.PREMIUM.ToString(), platSpent);
                transaction.eventParams.getProductsReceivedVC().addItem("Gold", VirtualCurrencyType.PREMIUM_GRIND.ToString(), goldReceived);

                transaction.goalCounts.setValues(player); 

                //subtract of non-related members
                string tempJSON = getJSONString(transaction);
                tempJSON = removeIsInitiator(tempJSON);
                tempJSON = removeTransactionServerStrings(tempJSON);
                addToDB(m_runCmdSync, tempJSON, player.m_testAccount);
            }
        }

        internal void itemShopItemPurchase(Player player, int platSpent, string itemNameReceived, string itemTypeReceived, int quantityBought)
        {
            if (player != null)
            {
                TransactionItem productsSpent = TransactionItem.virtualCurrency;
                TransactionItem productsReceived = TransactionItem.items;

                Transaction transaction = new Transaction(player.m_account_id.ToString(), player.m_sessionID.ToString(), productsSpent, productsReceived, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));

                transaction.eventParams.transactionName = "Premium Item Purchase";
                transaction.eventParams.transactionID = player.m_account_id.ToString();
                transaction.eventParams.transactorID = player.m_account_id.ToString();

                transaction.eventParams.transactionReceipt = "";
                transaction.eventParams.transactionServer = "";
                transaction.eventParams.paymentCountry = "";
                transaction.eventParams.transactionType = TransactionType.PURCHASE.ToString();

                //WILL BE SUBTRACTED FROM THE SERIALISED STRING
                transaction.eventParams.isInitiator = false;
                //**********************************

                transaction.eventParams.getProductsSpentVC().addItem("Platinum", VirtualCurrencyType.PREMIUM.ToString(), platSpent);
                transaction.eventParams.getProductsReceivedItems().addItem(itemNameReceived, itemTypeReceived, quantityBought);

                transaction.goalCounts.setValues(player); 

                //subtract of non-related members
                string tempJSON = getJSONString(transaction);
                tempJSON = removeIsInitiator(tempJSON);
                tempJSON = removeTransactionServerStrings(tempJSON);
                addToDB(m_runCmdSync, tempJSON, player.m_testAccount);
            }
        }

        //In game
        internal void inGameShopPurchase(Player player, int goldSpent, string itemNameReceived, string itemTypeReceived, int quantitySold)
        {
            if (player != null)
            {
                TransactionItem productsSpent = TransactionItem.virtualCurrency;
                TransactionItem productsReceived = TransactionItem.items;

                Transaction transaction = new Transaction(player.m_account_id.ToString(), player.m_sessionID.ToString(), productsSpent, productsReceived, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));

                transaction.eventParams.transactionName = "Shop Item Purchase";
                transaction.eventParams.transactionID = player.m_account_id.ToString();
                transaction.eventParams.transactorID = player.m_account_id.ToString();

                transaction.eventParams.getProductsSpentVC().addItem("Gold", VirtualCurrencyType.PREMIUM_GRIND.ToString(), goldSpent);
                transaction.eventParams.getProductsReceivedItems().addItem(itemNameReceived, itemTypeReceived, quantitySold);
                transaction.eventParams.transactionType = TransactionType.PURCHASE.ToString();

                //WILL BE SUBTRACTED FROM THE SERIALISED STRING
                transaction.eventParams.transactionReceipt = "";
                transaction.eventParams.transactionServer = "";
                transaction.eventParams.paymentCountry = "";
                transaction.eventParams.isInitiator = false;
                //**********************************

                transaction.goalCounts.setValues(player);
                //full subtract of non-related members
                string tempJSON = getJSONString(transaction);
                tempJSON = removeIsInitiator(tempJSON);
                tempJSON = removeTransactionServerStrings(tempJSON);
                addToDB(m_runCmdSync, tempJSON, player.m_testAccount);
            }
        }

        internal void inGameShopTokenPurchase(Player player, int tokensSpent, string tokenName, string itemNameReceived, string itemTypeReceived, int quantitySold)
        {
            if (player != null)
            {
                TransactionItem productsSpent = TransactionItem.virtualCurrency;
                TransactionItem productsReceived = TransactionItem.items;

                Transaction transaction = new Transaction(player.m_account_id.ToString(), player.m_sessionID.ToString(), productsSpent, productsReceived, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));

                transaction.eventParams.transactionName = "Token Shop Item Purchase";
                transaction.eventParams.transactionID = player.m_account_id.ToString();
                transaction.eventParams.transactorID = player.m_account_id.ToString();

                transaction.eventParams.getProductsSpentVC().addItem("tokens", VirtualCurrencyType.PREMIUM_GRIND.ToString(), tokensSpent);
                transaction.eventParams.getProductsReceivedItems().addItem(itemNameReceived, itemTypeReceived, quantitySold);
                transaction.eventParams.transactionType = TransactionType.PURCHASE.ToString();

                //WILL BE SUBTRACTED FROM THE SERIALISED STRING
                transaction.eventParams.transactionReceipt = "";
                transaction.eventParams.transactionServer = "";
                transaction.eventParams.paymentCountry = "";
                transaction.eventParams.isInitiator = false;
                //**********************************

                transaction.goalCounts.setValues(player);
                //full subtract of non-related members
                string tempJSON = getJSONString(transaction);
                tempJSON = removeIsInitiator(tempJSON);
                tempJSON = removeTransactionServerStrings(tempJSON);
                addToDB(m_runCmdSync, tempJSON, player.m_testAccount);
            }
        }

        internal void inGameShopSale(Player player, int goldReceived, string itemNameSold, string itemTypeSold, int quantitySold)
        {
            if (player != null)
            {
                TransactionItem productsSpent = TransactionItem.items;
                TransactionItem productsReceived = TransactionItem.virtualCurrency;

                Transaction transaction = new Transaction(player.m_account_id.ToString(), player.m_sessionID.ToString(), productsSpent, productsReceived, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));

                transaction.eventParams.transactionName = "Shop Item Sale";
                transaction.eventParams.transactionID = player.m_account_id.ToString();
                transaction.eventParams.transactorID = player.m_account_id.ToString();
                transaction.eventParams.transactionType = TransactionType.SALE.ToString();

                //WILL BE SUBTRACTED FROM THE SERIALISED STRING
                transaction.eventParams.transactionReceipt = "";
                transaction.eventParams.transactionServer = "";
                transaction.eventParams.paymentCountry = "";
                transaction.eventParams.isInitiator = false;//will have to be allocated
                //**********************************

                transaction.eventParams.getProductsSpentItems().addItem(itemNameSold, itemTypeSold, quantitySold);
                transaction.eventParams.getProductsReceivedVC().addItem("Gold", VirtualCurrencyType.PREMIUM_GRIND.ToString(), goldReceived);

                transaction.goalCounts.setValues(player);

                //full subtraction of non-related members
                string tempJSON = getJSONString(transaction);
                tempJSON = removeIsInitiator(tempJSON);
                tempJSON = removeTransactionServerStrings(tempJSON);
                addToDB(m_runCmdSync, tempJSON, player.m_testAccount);
            }
        }

        //Player - will likely pass in the Inventory items to this and dissect in this code
        internal void playerTrade(Player player, Player otherPlayer)
        {
            TransactionItem productsSpent = TransactionItem.virtualCurrency;
            TransactionItem productsReceived = TransactionItem.virtualCurrency;
            Transaction transaction = new Transaction(productsSpent, productsReceived);

            Inventory thisInventory = player.m_activeCharacter.m_tradingInventory;
            Inventory otherInventory = otherPlayer.m_activeCharacter.m_tradingInventory;

            if (player != null)
            {
                if (thisInventory.m_bagItems.Count > 0 && otherInventory.m_bagItems.Count > 0)//both trading items
                {
                    productsSpent = TransactionItem.vcAndItems;
                    productsReceived = TransactionItem.vcAndItems;

                    transaction = new Transaction(player.m_account_id.ToString(), player.m_sessionID.ToString(), productsSpent, productsReceived, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));

                    for (int i = 0; i < thisInventory.m_bagItems.Count; i++)
                    {
                        transaction.eventParams.getProductsSpentVCAndItems().addItem(thisInventory.m_bagItems[i].m_template.m_item_name, thisInventory.m_bagItems[i].m_template.m_subtype.ToString(), thisInventory.m_bagItems[i].m_quantity);
                    }
                    transaction.eventParams.getProductsSpentVCAndItems().addVC("Gold", VirtualCurrencyType.PREMIUM_GRIND.ToString(), thisInventory.m_coins);

                    for (int i = 0; i < otherInventory.m_bagItems.Count; i++)
                    {
                        transaction.eventParams.getProductsReceivedVCAndItems().addItem(otherInventory.m_bagItems[i].m_template.m_item_name, otherInventory.m_bagItems[i].m_template.m_subtype.ToString(), otherInventory.m_bagItems[i].m_quantity);
                    }
                    transaction.eventParams.getProductsReceivedVCAndItems().addVC("Gold", VirtualCurrencyType.PREMIUM_GRIND.ToString(), otherInventory.m_coins);
                }
                //this player trading items but other only gold
                else if (thisInventory.m_bagItems.Count == 0 && otherInventory.m_bagItems.Count > 0)
                {
                    productsSpent = TransactionItem.virtualCurrency;
                    productsReceived = TransactionItem.vcAndItems;

                    transaction = new Transaction(player.m_account_id.ToString(), player.m_sessionID.ToString(), productsSpent, productsReceived, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));

                    transaction.eventParams.getProductsSpentVC().addItem("Gold", VirtualCurrencyType.PREMIUM_GRIND.ToString(), thisInventory.m_coins);

                    for (int i = 0; i < otherInventory.m_bagItems.Count; i++)
                    {
                        transaction.eventParams.getProductsReceivedVCAndItems().addItem(otherInventory.m_bagItems[i].m_template.m_item_name, otherInventory.m_bagItems[i].m_template.m_subtype.ToString(), otherInventory.m_bagItems[i].m_quantity);
                    }
                    transaction.eventParams.getProductsReceivedVCAndItems().addVC("Gold", VirtualCurrencyType.PREMIUM_GRIND.ToString(), otherInventory.m_coins);
                }
                //other player trading items but this player only gold
                else if (thisInventory.m_bagItems.Count > 0 && otherInventory.m_bagItems.Count == 0)
                {
                    productsSpent = TransactionItem.vcAndItems;
                    productsReceived = TransactionItem.virtualCurrency;

                    transaction = new Transaction(player.m_account_id.ToString(), player.m_sessionID.ToString(), productsSpent, productsReceived, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));

                    //This Player
                    for (int i = 0; i < thisInventory.m_bagItems.Count; i++)
                    {
                        transaction.eventParams.getProductsSpentVCAndItems().addItem(thisInventory.m_bagItems[i].m_template.m_item_name, thisInventory.m_bagItems[i].m_template.m_subtype.ToString(), thisInventory.m_bagItems[i].m_quantity);
                    }
                    transaction.eventParams.getProductsSpentVCAndItems().addVC("Gold", VirtualCurrencyType.PREMIUM_GRIND.ToString(), thisInventory.m_coins);
                    //other Player
                    transaction.eventParams.getProductsReceivedVC().addItem("Gold", VirtualCurrencyType.PREMIUM_GRIND.ToString(), otherInventory.m_coins);
                }
                else if (thisInventory.m_bagItems.Count == 0 && otherInventory.m_bagItems.Count == 0)//only gold for both
                {
                    productsSpent = TransactionItem.virtualCurrency;
                    productsReceived = TransactionItem.virtualCurrency;

                    transaction = new Transaction(player.m_account_id.ToString(), player.m_sessionID.ToString(), productsSpent, productsReceived, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));

                    transaction.eventParams.getProductsSpentVC().addItem("Gold", VirtualCurrencyType.PREMIUM_GRIND.ToString(), thisInventory.m_coins);
                    transaction.eventParams.getProductsReceivedVC().addItem("Gold", VirtualCurrencyType.PREMIUM_GRIND.ToString(), otherInventory.m_coins);
                }
                if (transaction != null)
                {
                    //WILL BE SUBTRACTED FROM THE STRING
                    transaction.eventParams.transactionReceipt = "";
                    transaction.eventParams.transactionServer = "";
                    transaction.eventParams.paymentCountry = "";
                    //**********************************
                    transaction.eventParams.isInitiator = player.m_activeCharacter.m_isTradeInitator;//will have to be allocated

                    transaction.eventParams.transactionName = "Player Trade";
                    transaction.eventParams.transactionID = player.m_account_id.ToString();
                    transaction.eventParams.transactorID = otherPlayer.m_account_id.ToString();
                    transaction.eventParams.transactionType = TransactionType.TRADE.ToString();

                    transaction.goalCounts.setValues(player);

                    //subtract of non-related members
                    string tempJSON = getJSONString(transaction);
                    tempJSON = removeTransactionServerStrings(tempJSON);
                    addToDB(m_runCmdSync, tempJSON, player.m_testAccount);
                }
            }
        }

        private string removeIsInitiator(string originalStr)
        {
            string premString = "";
            string searchStr = "\"isInitiator\":false,";//20 character string
            premString = removeSubString(originalStr, searchStr);//set as JSON to be sent to DB

            return premString;
        }

        private string removeTransactionServerStrings(string originalStr)
        {
            string inGameString = "";
            string searchStr = "\"transactionReceipt\":\"\",\"transactionServer\":\"\",\"paymentCountry\":\"\",";
            inGameString = removeSubString(originalStr, searchStr);

            return inGameString;
        }

        internal void achievement(Player player, string achievementID, string achievementName)
        {
            if (player != null)
            {

                AchievementLog achievement = new AchievementLog(player.m_account_id.ToString(), player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));

                achievement.eventParams.achievementID = achievementID;
                achievement.eventParams.achievementName = achievementName;
                achievement.goalCounts.setValues(player);

                getJSONAddToDB(achievement, m_runCmdSync, player.m_testAccount);
            }
        }

        internal void levelUp(Player player, string levelUpName, int statPointsGained)
        {
            if (player != null)
            {

                LevelUp levelUp = new LevelUp(player.m_account_id.ToString(), player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));

                levelUp.eventParams.levelUpName = levelUpName;
                levelUp.customParams.statPointsGained = statPointsGained;

                levelUp.goalCounts.setValues(player);

                getJSONAddToDB(levelUp, m_runCmdSync, player.m_testAccount);
            }
        }
/*
        internal void productViewed(Player player, string productID, string productName)
        {
            ProductViewed productsViewed = new ProductViewed(player.m_account_id.ToString(), player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));
            productsViewed.eventParams.viewedProductID = productID;
            productsViewed.eventParams.viewedProductName = productName;

            productsViewed.goalCounts.userLevel = player.m_activeCharacter.m_level;
            productsViewed.goalCounts.experience = player.m_activeCharacter.m_experience;
            productsViewed.goalCounts.health = player.m_activeCharacter.CurrentHealth;
            productsViewed.goalCounts.energy = player.m_activeCharacter.CurrentEnergy;
            productsViewed.goalCounts.strength = player.m_activeCharacter.Strength;
            productsViewed.goalCounts.dexterity = player.m_activeCharacter.Dexterity;
            productsViewed.goalCounts.focus = player.m_activeCharacter.Focus;
            productsViewed.goalCounts.vitality = player.m_activeCharacter.Vitality;
            productsViewed.goalCounts.attack = player.m_activeCharacter.Attack;
            productsViewed.goalCounts.defence = player.m_activeCharacter.Defence;
            productsViewed.goalCounts.damage = player.m_activeCharacter.TotalWeaponDamage;
            productsViewed.goalCounts.armour = player.m_activeCharacter.ArmourValue;
            productsViewed.goalCounts.gold = player.m_activeCharacter.m_inventory.m_coins;

            getJSONAddToDB(productsViewed, m_runCmdSync, player.m_testAccount);
        }
*/
        internal void shopEntered(Player player, string shopID, string shopName/*, string shopType*/)
        {
            ShopEntered shopEntered = new ShopEntered(player.m_account_id.ToString(), player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));

            shopEntered.eventParams.shopID = shopID;
            shopEntered.eventParams.shopName = shopName;
            //shopEntered.eventParams.shopType = shopType;

            shopEntered.goalCounts.setValues(player);
            getJSONAddToDB(shopEntered, m_runCmdSync, player.m_testAccount);
        }

        //GAMEPLAY
        internal void missionStarted(Player player, string missionName, string missionID)//DONE
        {
            MissionStarted missionStarted = new MissionStarted(player.m_account_id.ToString(), player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));

            missionStarted.eventParams.missionName = missionName;
            missionStarted.eventParams.missionID = missionID;
            //missionStarted.eventParams.isTutorial = false;

            missionStarted.goalCounts.setValues(player);
            getJSONAddToDB(missionStarted, m_runCmdSync, player.m_testAccount);
        }

        internal void missionCompleted(Player player, string missionName, string missionID, List<LootDetails> items, int currencyGained, int xpGained)//REWARD STUFF - DONE
        {

            RewardType missionCompleteReward = RewardType.none;
            MissionCompleted missionCompleted = new MissionCompleted(missionCompleteReward);
            bool hasItems = (items.Count > 0);

            if (hasItems == true && currencyGained != 0)
            {
                missionCompleteReward = RewardType.both;
                missionCompleted = new MissionCompleted(player.m_account_id.ToString(), player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"), missionCompleteReward);
                missionCompleted.eventParams.reward.rewardName = "Reward";
                missionCompleted.eventParams.reward.getProductsBoth().addVirtualCurrency("gold", VirtualCurrencyType.PREMIUM_GRIND.ToString(), currencyGained);
                
                for (int i = 0; i < items.Count; i++)
                {
                    LootDetails tempItem = items[i];
                    ItemTemplate itemTemplate = ItemTemplateManager.GetItemForID(tempItem.m_templateID);
                    if (itemTemplate != null)
                    {
                        int quantity = tempItem.m_quantity;
                        string name = itemTemplate.m_item_name;
                        string type = itemTemplate.m_subtype.ToString();
                        
                        missionCompleted.eventParams.reward.getProductsBoth().addItem(name, type, quantity);
                    }
                }
            }
            else if (hasItems == true && currencyGained == 0)
            {
                missionCompleteReward = RewardType.itemsOnly;
                missionCompleted = new MissionCompleted(player.m_account_id.ToString(), player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"), missionCompleteReward);
                missionCompleted.eventParams.reward.rewardName = "Reward";
                
                for (int i = 0; i < items.Count; i++)
                {
                    LootDetails tempItem = items[i];
                    ItemTemplate itemTemplate = ItemTemplateManager.GetItemForID(tempItem.m_templateID);
                    if (itemTemplate != null)
                    {
                        int quantity = tempItem.m_quantity;
                        string name = itemTemplate.m_item_name;
                        string type = itemTemplate.m_subtype.ToString();

                        missionCompleted.eventParams.reward.getProductsItem().addItem(name, type, quantity);
                    }
                }
            }
            else if (hasItems == false && currencyGained != 0)
            {
                missionCompleteReward = RewardType.currencyOnly;
                missionCompleted = new MissionCompleted(player.m_account_id.ToString(), player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"), missionCompleteReward);
                missionCompleted.eventParams.reward.rewardName = "Reward";
                missionCompleted.eventParams.reward.getProductsVC().addVirtualCurrency("gold", VirtualCurrencyType.PREMIUM_GRIND.ToString(), currencyGained);
            }
            else if (hasItems == false && currencyGained == 0)
            {
                missionCompleteReward = RewardType.none;
                missionCompleted = new MissionCompleted(player.m_account_id.ToString(), player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"), missionCompleteReward);
                missionCompleted.eventParams.reward = null;
            }


            missionCompleted.eventParams.missionName = missionName;
            missionCompleted.eventParams.missionID = missionID;
            //missionCompleted.eventParams.isTutorial = false;
            
            missionCompleted.customParams.experienceGained = xpGained;
            missionCompleted.goalCounts.setValues(player);

            getJSONAddToDB(missionCompleted, m_runCmdSync, player.m_testAccount);
        }

        internal void statUpgraded(Player player, int strCurrVal, int strInc, int focusCurrVal, int focusInc, int dexCurrVal, int dexInc, int vitCurrVal, int vitInc)//DONE
        {
            if (player != null)
            {
                if (strInc > 0)
                {
                    StatUpgraded statUpgraded = new StatUpgraded(player.m_account_id.ToString(), player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));
                    statUpgraded.eventParams.statName = "Strength";
                    statUpgraded.eventParams.currentValue = strCurrVal;
                    statUpgraded.eventParams.newValue = strCurrVal += strInc;
                    statUpgraded.goalCounts.setValues(player);
                    getJSONAddToDB(statUpgraded, m_runCmdSync, player.m_testAccount);
                }
                if (focusInc > 0)
                {
                    StatUpgraded statUpgraded = new StatUpgraded(player.m_account_id.ToString(), player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));
                    statUpgraded.eventParams.statName = "Focus";
                    statUpgraded.eventParams.currentValue = focusCurrVal;
                    statUpgraded.eventParams.newValue = focusCurrVal += focusInc;
                    statUpgraded.goalCounts.setValues(player);
                    getJSONAddToDB(statUpgraded, m_runCmdSync, player.m_testAccount);
                }
                if (dexInc > 0)
                {
                    StatUpgraded statUpgraded = new StatUpgraded(player.m_account_id.ToString(), player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));
                    statUpgraded.eventParams.statName = "Dexterity";
                    statUpgraded.eventParams.currentValue = dexCurrVal;
                    statUpgraded.eventParams.newValue = dexCurrVal += dexInc;
                    statUpgraded.goalCounts.setValues(player);
                    getJSONAddToDB(statUpgraded, m_runCmdSync, player.m_testAccount);
                }
                if (vitInc > 0)
                {
                    StatUpgraded statUpgraded = new StatUpgraded(player.m_account_id.ToString(), player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));
                    statUpgraded.eventParams.statName = "Vitality";
                    statUpgraded.eventParams.currentValue = vitCurrVal;
                    statUpgraded.eventParams.newValue = vitCurrVal += vitInc;
                    statUpgraded.goalCounts.setValues(player);
                    getJSONAddToDB(statUpgraded, m_runCmdSync, player.m_testAccount);
                }
            }
        }

        internal void opponentDefeated(Player player, string opponentID ,string opponentName, int rewardGold, List<LootDetails> loot)//REWARD STUFF - DONE
        {
            if (player != null)
            {
                RewardType opponentDefeatedReward = RewardType.none;
                bool hasItems = (loot != null && loot.Count > 0);
                bool hasGold = rewardGold > 0;

                if (hasGold && hasItems)
                {
                    opponentDefeatedReward = RewardType.both;
                }
                else if (hasGold)
                {
                    opponentDefeatedReward = RewardType.currencyOnly;
                }
                else if (hasItems)
                {
                    opponentDefeatedReward = RewardType.itemsOnly;
                }
                
                OpponentDefeated opponentDefeated = new OpponentDefeated(player.m_account_id.ToString(), player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"), opponentDefeatedReward);

                opponentDefeated.eventParams.opponentID = opponentID;
                opponentDefeated.eventParams.opponentName = opponentName;
               

                if (hasGold && hasItems)
                {
                    Products_Both reward = opponentDefeated.eventParams.reward.getProductsBoth();
                    reward.addVirtualCurrency("Gold", VirtualCurrencyType.PREMIUM_GRIND.ToString(), rewardGold);
                    reward.AddLoot(loot);
                }
                else if (hasGold)
                {
                    Products_VC reward = opponentDefeated.eventParams.reward.getProductsVC();
                    reward.addVirtualCurrency("Gold", VirtualCurrencyType.PREMIUM_GRIND.ToString(), rewardGold);
           
                }
                else if (hasItems)
                {
                    Products_Items reward = opponentDefeated.eventParams.reward.getProductsItem();
                    reward.AddLoot(loot);
                }
                

                opponentDefeated.goalCounts.setValues(player);

                getJSONAddToDB(opponentDefeated, m_runCmdSync, player.m_testAccount);
            }
        }

        internal void playerDefeated(Player player, string defeatedByID, string defeatedByName, string defeatedByType)//DONE
        {
            if (player != null)
            {
                PlayerDefeated playerDefeated = new PlayerDefeated(player.m_account_id.ToString(), player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));

                playerDefeated.eventParams.defeatedByID = defeatedByID;
                playerDefeated.eventParams.defeatedByName = defeatedByName;
                playerDefeated.eventParams.defeatedByType = defeatedByType;

                playerDefeated.goalCounts.setValues(player);
                getJSONAddToDB(playerDefeated, m_runCmdSync, player.m_testAccount);
            }
        }

        internal void skillUpgraded(Player player, string skillID, string skillName, int currSkillLevel, int newSkillLevel)//DONE
        {
            if (player != null)
            {
                SkillUpgraded skillUpgraded = new SkillUpgraded(player.m_account_id.ToString(), player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));

                skillUpgraded.eventParams.skillID = skillID;
                skillUpgraded.eventParams.skillName = skillName;
                skillUpgraded.eventParams.currentSkillLevel = currSkillLevel;
                skillUpgraded.eventParams.newSkillLevel = newSkillLevel;

                skillUpgraded.goalCounts.setValues(player);
                getJSONAddToDB(skillUpgraded, m_runCmdSync, player.m_testAccount);
            }
        }

        internal void skillUsed(Player player, string skillID, string skillName, bool isSuccessful, string reasonForFail)//DONE
        {
            if (player != null)
            {
                SkillUsed skillUsed = new SkillUsed(player.m_account_id.ToString(), player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));

                skillUsed.eventParams.skillID = skillID;
                skillUsed.eventParams.skillName = skillName;
                skillUsed.eventParams.success = isSuccessful;
                skillUsed.eventParams.reasonForFailure = reasonForFail;

                skillUsed.goalCounts.setValues(player);

                getJSONAddToDB(skillUsed, m_runCmdSync, player.m_testAccount);
            }
        }

        internal void BountyTracking(Player player, uint character_id, int questID, BountyTrackingStatus status)
        {
            if (null == player)
            {
                return;
            }

            BountyTracking bountyTracking = new BountyTracking(player.m_account_id.ToString(), player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));

            bountyTracking.eventParams.characterID = character_id.ToString();
            bountyTracking.eventParams.missionID = questID.ToString();
            bountyTracking.eventParams.bountyStatus = status.ToString();

            bountyTracking.goalCounts.setValues(player);

            getJSONAddToDB(bountyTracking, m_runCmdSync, player.m_testAccount);
        }

        internal void itemActioned(Player player, string itemID, string itemName, string itemType, string action /*ENUM*/)//DONE
        {
            //enums - PICKED_UP, EQUIPPED, USED, DROPPED
            if (player != null)
            {
                ItemActioned itemActioned = new ItemActioned(player.m_account_id.ToString(), player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));

                itemActioned.eventParams.itemID = itemID;
                itemActioned.eventParams.itemName = itemName;
                itemActioned.eventParams.itemType = itemType;
                itemActioned.eventParams.action = action;

                itemActioned.goalCounts.setValues(player);
                getJSONAddToDB(itemActioned, m_runCmdSync, player.m_testAccount);
            }
        }

        internal void fastTravelNew(Player player, string currentFastTravelID)
        {
            if (player != null)
            {
                FastTravel fastTravel = new FastTravel(player.m_account_id.ToString(), player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"), true);

                fastTravel.getEventParamsNew().currentFastTravelID = currentFastTravelID;

                fastTravel.goalCounts.setValues(player);
                getJSONAddToDB(fastTravel, m_runCmdSync, player.m_testAccount);
            }
        }

        internal void fastTravelUsed(Player player, string fastTravelToID)//DONE
        {
            if (player != null)
            {
                FastTravel fastTravel = new FastTravel(player.m_account_id.ToString(), player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"), false);

                //fastTravel.getEventParamsTravel().currentFastTravelID = currentFastTravelID;
                fastTravel.getEventParamsTravel().toFastTravelID = fastTravelToID;

                fastTravel.goalCounts.setValues(player);
                getJSONAddToDB(fastTravel, m_runCmdSync, player.m_testAccount);
            }
        }

        internal void support(Player player, string ticketID)//DONE
        {
            SupportLog support = new SupportLog(player.m_account_id.ToString(), player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));

            support.eventParams.ticketID = ticketID;

            support.goalCounts.setValues(player);
            getJSONAddToDB(support, m_runCmdSync, player.m_testAccount);
        }

        internal void zoneNew(Player player, string currentZoneID, string currentZoneName)
        {
            if (player != null)
            {
                ZoneLog zone = new ZoneLog(player.m_account_id.ToString(), player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"), true);
                zone.getEventParamsNew().action = "DISCOVERED";
                zone.getEventParamsNew().currentZoneID = currentZoneID;
                zone.getEventParamsNew().currentZoneName = currentZoneName;
                zone.goalCounts.setValues(player);

                getJSONAddToDB(zone, m_runCmdSync, player.m_testAccount);
            }
        }

        internal void zoneTravel(Player player, string currentZoneID, string currentZoneName, string newZoneID, string newZoneName)//DONE
        {
            if (player != null)
            {
                ZoneLog zone = new ZoneLog(player.m_account_id.ToString(), player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"), false);

                zone.getEventParamsTravel().action = "CHANGED";
                zone.getEventParamsTravel().currentZoneID = currentZoneID;
                zone.getEventParamsTravel().currentZoneName = currentZoneName;
                zone.getEventParamsTravel().newZoneID = newZoneID;
                zone.getEventParamsTravel().newZoneName = newZoneName;

                zone.goalCounts.setValues(player);

                getJSONAddToDB(zone, m_runCmdSync, player.m_testAccount);
            }
        }

        internal void LogInviteSent(Player player,Player invitedPlayer,string inviteType, string inviteID)
        {
            if (player != null && invitedPlayer!=null)
            {
                InviteSent invite = new InviteSent(player.m_account_id.ToString(), player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));
                EventParams_InviteSent eventParams = invite.eventParams;
                eventParams.inviteType = inviteType;
                eventParams.uniqueTracking = inviteID;
                eventParams.addReceipients(invitedPlayer.m_account_id.ToString());
                GoalCounts goalCounts = invite.goalCounts;
                goalCounts.setValues(player);
                getJSONAddToDB(invite, m_runCmdSync, player.m_testAccount);
            }


        }
        internal void LogInviteRecieved(Player player, string invitingPlayerID, string inviteType, string inviteID,bool accepted)
        {
            if (player != null)
            {
                InviteReceived invite = new InviteReceived(player.m_account_id.ToString(), player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));
                EventParams_InviteReceived eventParams = invite.eventParams;
                eventParams.inviteType = inviteType;
                eventParams.uniqueTracking = inviteID;
                eventParams.senderID = invitingPlayerID;
                eventParams.isInviteAccepted = accepted;

                GoalCounts goalCounts = invite.goalCounts;
                goalCounts.setValues(player);
                getJSONAddToDB(invite, m_runCmdSync, player.m_testAccount);
            }


        }
        internal void LogMessageSent(Player player, string recipientCharacterID, string communicationType, string messageID)
        {
            if (player != null)
            {
                MessageSent sent = new MessageSent(player.m_account_id.ToString(), player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));
                EventParams_MessageSent eventParams = sent.eventParams;
                eventParams.addReceipient(recipientCharacterID);
                eventParams.uniqueTracking = messageID;
                eventParams.communicationType = communicationType;

                sent.goalCounts.setValues(player);
                getJSONAddToDB(sent, m_runCmdSync, player.m_testAccount);
            }
        }
        internal void LogMessageReceived(Player player, string senderID, string communicationType, string messageID)
        {
            if (player != null)
            {
                
                MessageReceived received = new MessageReceived(player.m_account_id.ToString(), player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));
                EventParams_MessageReceived eventParams = received.eventParams;
                eventParams.senderID =senderID;
                eventParams.uniqueTracking = messageID;
                eventParams.communicationType = communicationType;

                received.goalCounts.setValues(player);
                getJSONAddToDB(received, m_runCmdSync, player.m_testAccount);
            }
        }

        internal void LogGuildEvent(Player player, string kickedID, string kickedName,string guildName,string action)
        {
            if (player != null)
            {

                Guild guildEvent = new Guild(player.m_account_id.ToString(), player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));
                EventParams_Guild eventParams = guildEvent.eventParams;
                eventParams.action = action;
                eventParams.guildName = guildName;
                CustomParams_Guild customParams = new CustomParams_Guild(player, kickedID, kickedName);
                guildEvent.customParams = customParams;
                guildEvent.goalCounts.setValues(player);
                getJSONAddToDB(guildEvent, m_runCmdSync, player.m_testAccount);
            }
        }
        internal void LogPVPStarted(Player player,Character opponent)
        {
            if (player != null && player.m_activeCharacter!=null && opponent != null && opponent.m_player != null)
            {
                Character playerCharacter = player.m_activeCharacter;
                PvpStarted pvpEvent = new PvpStarted(player.m_account_id.ToString(), player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));
                EventParams_PvpStarted eventParams = pvpEvent.eventParams;
                Participants selfParticipantList = new Participants();
                selfParticipantList.participant.characterID = playerCharacter.m_character_id.ToString();
                selfParticipantList.participant.characterLevel = playerCharacter.Level;
                selfParticipantList.participant.characterName = playerCharacter.m_name;
                eventParams.participants.Add(selfParticipantList);
                Participants participantList = new Participants();

                //Participant participant = new Participant(opponent.m_player.m_account_id.ToString(),opponent.m_name,opponent.m_level);
                participantList.participant.characterID = opponent.m_character_id.ToString();
                participantList.participant.characterLevel = opponent.Level;
                participantList.participant.characterName = opponent.m_name;
                eventParams.participants.Add(participantList);
                
                pvpEvent.goalCounts.setValues(player);
                getJSONAddToDB(pvpEvent, m_runCmdSync, player.m_testAccount);
            }
        }

        internal void LogPVPEnded(Player player)
        {
            if (player != null)
            {
                PvpEnded pvpEvent = new PvpEnded(player.m_account_id.ToString(), player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"), RewardType.none);
                EventParams_PvpEnded eventParams = pvpEvent.eventParams;
                
                pvpEvent.goalCounts.setValues(player);
                getJSONAddToDB(pvpEvent, m_runCmdSync, player.m_testAccount);
            }
        }

        internal void LogSocialString(Player player, string socialType ,int platReward)
        {
            if (player != null)
            {
                RewardType currentReward = RewardType.none;
                if (platReward > 0)
                {
                    currentReward = RewardType.currencyOnly;
                }
                Social pvpEvent = new Social(player.m_account_id.ToString(), player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"), currentReward);
                EventParams_Social eventParams = pvpEvent.eventParams;
                eventParams.socialType = socialType;
                pvpEvent.goalCounts.setValues(player);
                if (currentReward == RewardType.currencyOnly)
                {
                    Products_VC platRewardHolder = eventParams.reward.getProductsVC();
                    if (platRewardHolder != null)
                    {
                        platRewardHolder.addVirtualCurrency("Platinum", VirtualCurrencyType.PREMIUM.ToString(), platReward);
                    }
                }
                getJSONAddToDB(pvpEvent, m_runCmdSync, player.m_testAccount);
            }

        }

        // auctionHouseEvent                                                             //
        // Creates a suitable object for conversion to JSON to be uploaded for analytics //
        internal void auctionHouseEvent(int listingID, int accountID, Item item, int gold, AHServerMessageType messageType, DateTime eventTime)
        {
            AuctionHouseEvent ahEvent;
            string eventTimeString = eventTime.ToString("yyyy-MM-dd HH:mm:ss.000");

            switch (messageType)
            {
                // Transactions where the player receives gold //
                case (AHServerMessageType.LISTING_CANCELLED_BIDDER):
                case (AHServerMessageType.LISTING_BOUGHT_OUT):
                case (AHServerMessageType.LISTING_COMPLETED):
                case (AHServerMessageType.OUT_BID):
                {
                    ahEvent = new AuctionHouseEvent(accountID.ToString(),
                                                    AuctionedProduct.NULL,
                                                    AuctionedProduct.virtualCurrency,
                                                    eventTimeString);
                    ahEvent.eventParams.productID = String.Empty;
                    break;
                }
                // Transcations where the player receives item(s) - Listing Won when referencing a completion is only where an item is awarded (the bid that won it was a separate transaction) //
                case (AHServerMessageType.LISTING_CANCELLED_SELLER):
                case (AHServerMessageType.LISTING_WON_COMPLETION):
                {
                    ahEvent = new AuctionHouseEvent(accountID.ToString(),
                                                    AuctionedProduct.NULL,
                                                    AuctionedProduct.items,
                                                    eventTimeString);
                    ahEvent.eventParams.productID = item.m_template_id.ToString();
                    break;
                }
                // Transaction Listing Won BuyOut - Referencing a buyout is a transaction that takes gold and awards an item //
                case (AHServerMessageType.LISTING_WON_BUYOUT):
                {
                    ahEvent = new AuctionHouseEvent(accountID.ToString(),
                                                    AuctionedProduct.virtualCurrency,
                                                    AuctionedProduct.items,
                                                    eventTimeString);
                    ahEvent.eventParams.productID = item.m_template_id.ToString();
                    break;
                }
                // Transactions where the player 'spends' gold //
                case (AHServerMessageType.BID_PLACED):
                {
                    ahEvent = new AuctionHouseEvent(accountID.ToString(),
                                                    AuctionedProduct.virtualCurrency,
                                                    AuctionedProduct.NULL,
                                                    eventTimeString);
                    ahEvent.eventParams.productID = String.Empty;
                    break;
                }
                // Transactions where the player 'spends' gold and items //
                case (AHServerMessageType.LISTING_CREATED):
                {
                    ahEvent = new AuctionHouseEvent(accountID.ToString(),
                                                    AuctionedProduct.vcAndItems,
                                                    AuctionedProduct.NULL,
                                                    eventTimeString);
                    ahEvent.eventParams.productID = item.m_template_id.ToString();
                    break;
                }
                // Transaction where the player receives item(s) and gold //
                case (AHServerMessageType.LISTING_EXPIRED):
                case (AHServerMessageType.LISTING_CANCELLED_SERVER):
                {
                    ahEvent = new AuctionHouseEvent(accountID.ToString(),
                                                    AuctionedProduct.NULL,
                                                    AuctionedProduct.vcAndItems,
                                                    eventTimeString);
                    ahEvent.eventParams.productID = item.m_template_id.ToString();
                    break;
                }
                default:
                {
                    Program.Display("AnalyticsMain.cs - auctionHouseEvent() received an incorrect AHServerMessageType!");
                    return;
                }
            }

            // Populate products that are recieved depending on transaction type
            if (ahEvent.eventParams.productsReceived.GetType() != typeof(ProductsReceived_NULL))
            {
                if (ahEvent.eventParams.productsReceived.GetType() == typeof(ProductsReceived_Items))
                {
                    if (item == null)
                    {
                        Program.Display("AnalyticsMain.cs - auctionHouseEvent() Item is null where an item was being received!");
                        return;
                    }

                    ahEvent.eventParams.getProductsReceivedItems().addItem(item.m_template.m_item_name, item.m_template.m_subtype.ToString(), item.m_quantity);
                }
                else if (ahEvent.eventParams.productsReceived.GetType() == typeof(ProductsReceived_VC))
                {
                    ahEvent.eventParams.getProductsReceivedVC().addItem("Gold", VirtualCurrencyType.PREMIUM_GRIND.ToString(), gold);
                }
                else if (ahEvent.eventParams.productsReceived.GetType() == typeof(ProductsReceived_VCAndItems))
                {
                    if (item == null)
                    {
                        Program.Display("AnalyticsMain.cs - auctionHouseEvent() Item is null where an item was being received!");
                        return;
                    }

                    ahEvent.eventParams.getProductsRecievedVCAndItems().addItem(item.m_template.m_item_name, item.m_template.m_subtype.ToString(), item.m_quantity);
                    ahEvent.eventParams.getProductsRecievedVCAndItems().addItem("Gold", VirtualCurrencyType.PREMIUM_GRIND.ToString(), gold);
                }
            }

            // Populate products that are spent depending on transaction type
            if (ahEvent.eventParams.productsSpent.GetType() != typeof(ProductsSpent_NULL))
            {
                if (ahEvent.eventParams.productsSpent.GetType() == typeof(ProductsSpent_Items))
                {
                    if (item == null)
                    {
                        Program.Display("AnalyticsMain.cs - auctionHouseEvent() Item is null where an item was being spent!");
                        return;
                    }

                    ahEvent.eventParams.getProductsSpentItems().addItem(item.m_template.m_item_name, item.m_template.m_subtype.ToString(), item.m_quantity);
                }
                else if (ahEvent.eventParams.productsSpent.GetType() == typeof(ProductsSpent_VC))
                {
                    ahEvent.eventParams.getProductsSpentVC().addItem("Gold", VirtualCurrencyType.PREMIUM_GRIND.ToString(), gold);
                }
                else if (ahEvent.eventParams.productsSpent.GetType() == typeof(ProductsSpent_VCAndItems))
                {
                    if (item == null)
                    {
                        Program.Display("AnalyticsMain.cs - auctionHouseEvent() Item is null where an item was being spent!");
                        return;
                    }

                    ahEvent.eventParams.getProductsSpentVCAndItems().addItem(item.m_template.m_item_name, item.m_template.m_subtype.ToString(), item.m_quantity);
                    ahEvent.eventParams.getProductsSpentVCAndItems().addItem("Gold", VirtualCurrencyType.PREMIUM_GRIND.ToString(), gold);
                }
            }

            // Fill in some final details
            ahEvent.eventParams.serverID        = Program.m_worldID.ToString();
            ahEvent.eventParams.transactionName = messageType.ToString();
            ahEvent.eventParams.auctionLotID    = listingID.ToString();

            // Send to HUB database
            getJSONAddToDB(ahEvent, m_runCmdSync, 0);
        }

        internal void BarberShopUsed(Player i_player, string i_worldID, int i_cost, int i_faceId, int i_skinColourId, int i_hairId,
                                     int i_hairColourId, int i_faceAccessoryId, int i_faceAccessoryColourId/*, string i_characterGender*/)
        {
            BarberShopUsed barberShopEvent = new BarberShopUsed(i_player.m_account_id.ToString(), i_player.m_sessionID.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));

            barberShopEvent.eventParams = new EventParams_BarberShopUsed(i_cost, i_faceId, i_skinColourId, i_hairId, i_hairColourId,i_faceAccessoryId, i_faceAccessoryColourId, i_player, i_worldID);

            string tempJSON = getJSONString(barberShopEvent);

            getJSONAddToDB(barberShopEvent, m_runCmdSync, i_player.m_testAccount);
        }

        internal void EmoteUsed(Player i_player, string i_emoteUsed, string i_worldID)
        {
            EmoteUsed emoteUsed = new EmoteUsed(i_player.m_account_id, i_player.m_sessionID, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));
           
            emoteUsed.eventParams = new EventParams_EmoteUsed();
            emoteUsed.eventParams.SetValues(i_emoteUsed, i_player, i_worldID);

            getJSONAddToDB(emoteUsed, m_runCmdSync, i_player.m_testAccount);
        }
        
        internal void NotificationServices(long i_userID, uint sessionID, string i_notificationToken, string deviceType)
        {
            NotificationServices notificationServices = new NotificationServices(i_userID, sessionID, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));


            if (deviceType.Contains("Android")) //see which type of device this is
            {
                notificationServices.eventParams = new EventParams_NotificationServicesAndroid(i_notificationToken);
                notificationServices.eventParams.platform = "ANDROID";
                //(EventParams_NotificationServicesAndroid)notificationServices.eventParams.androidRegistrationID = i_notificationToken;
            }
            else
            {
                notificationServices.eventParams = new EventParams_NotificationServicesIOS(i_notificationToken);
                notificationServices.eventParams.platform = "IOS";
            }

            getJSONAddToDB(notificationServices, m_runCmdSync, 0);
        }

        internal void CraftingEvent(Player i_player, string i_worldID, CraftingType i_craftingType, string i_productName, int i_productNumber,CraftingOutcome i_outcome)
        {
            CraftingEvent craftingEvent = new CraftingEvent(i_player.m_account_id, i_player.m_sessionID, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000"));
            craftingEvent.eventParams = new EventParams_CraftingEvent();
            craftingEvent.eventParams.SetValues(i_craftingType,i_productName,i_productNumber,i_outcome,i_player,i_worldID);

            getJSONAddToDB(craftingEvent, m_runCmdSync, 0);
        }

        private void getJSONAddToDB(object obj, bool sync, byte isTestAccount)
        {
            JsonString JSON_obj = new JsonString();
            string eventStr = JSON_obj.getJSONString(obj).Replace("'", "");
            string cmdStr = "INSERT INTO new_analytics_log (event_time, event_details, test_account,world_id) VALUES('" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + eventStr + "'," + isTestAccount + "," + Program.m_worldID + ")";
            if (sync)
            {
                Program.processor.m_universalHubDB.runCommandSync(cmdStr);
            }
            else
            {
                Program.processor.m_universalHubDB.runCommand(cmdStr);
            }
        }

        //Seperate Methods to allow the string to be augmented before being pushed to the database
        private string getJSONString(object obj)
        {
            JsonString JSON_obj = new JsonString();
            string eventStr = JSON_obj.getJSONString(obj).Replace("'", "");
            return eventStr;
        }

        private void addToDB(bool sync, string eventStr, byte isTestAccount)
        {
            string cmdStr = "INSERT INTO new_analytics_log (event_time, event_details, test_account,world_id) VALUES('" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + eventStr + "'," + isTestAccount + "," + Program.m_worldID + ")";
            if (sync)
            {
                Program.processor.m_universalHubDB.runCommandSync(cmdStr);
            }
            else
            {
                Program.processor.m_universalHubDB.runCommand(cmdStr);
            }
        }

        private string removeSubString(string fullString, string subString)
        {
            string finalStr = "";
            if (fullString.Contains(subString))
            {
                int start = fullString.IndexOf(subString);
                int endCount = subString.Length;
                finalStr = fullString.Remove(start, endCount);
            }
            return finalStr;
        }

    }

}
