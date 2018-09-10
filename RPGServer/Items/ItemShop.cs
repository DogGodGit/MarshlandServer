using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using MainServer.player_offers;
using Lidgren.Network;
using Newtonsoft.Json;
using MainServer.Localise;
using MySql.Data.MySqlClient;

namespace MainServer
{
    class AppleReceipt
    {
        public AppleReceipt()
        {
            app_item_id = String.Empty;
            original_transaction_id = String.Empty;
            bvrs = String.Empty;
            product_id = String.Empty;
            purchase_date = String.Empty;
            purchase_date_pst = String.Empty;
            purchase_date_ms = String.Empty;
            quantity = String.Empty;
            bid = String.Empty;
            original_purchase_date_pst = String.Empty;
            original_purchase_date_ms = String.Empty;
            original_purchase_date = String.Empty;
            transaction_id = String.Empty;
            version_external_identifier = String.Empty;
            item_id = String.Empty;
        }
        public string original_purchase_date_pst;
        public string purchase_date_ms;
        public string original_transaction_id;
        public string original_purchase_date_ms;
        public string app_item_id;

        public string transaction_id;

        public string quantity;
        public string bvrs;

        public string version_external_identifier;

        public string bid;
        public string product_id;
        public string purchase_date;
        public string purchase_date_pst;

        public string original_purchase_date;
        public string item_id;
    }
    class AppleReceiptContainer
    {
        public AppleReceiptContainer()
        {
            receipt = null;
            status = 0;
        }
        public AppleReceipt receipt;
        public int status;
    }

    class PremiumShopItem
    {
        #region variables
        int m_stockID = -1;
        int m_itemTemplateID = -1;
        ItemTemplate m_itemTemplate = null;
        int m_quantity = -1;
        int m_price = -1;
        string m_shopName = String.Empty;
        string m_shopDescription = String.Empty;
        int m_stock_type = 0;
        #endregion //variables
        #region Properties
        internal int StockID
        {
            get { return m_stockID; }
        }
        internal int ItemTemplateID
        {
            get { return m_itemTemplateID; }
        }
        internal ItemTemplate StockItemTemplate
        {
            get { return m_itemTemplate; }
        }
        internal int Quantity
        {
            get { return m_quantity; }
        }
        internal int Price
        {
            get { return m_price; }
        }
        internal string ShopName
        {
            get { return m_shopName; }
        }
        internal int StockType
        {
            get { return m_stock_type; }
        }
        #endregion //Properties

        internal PremiumShopItem(int stockID, int itemTemplateID, int quantity, int price, string shopName, string description,int stock_type)
        {
            m_stockID = stockID;
            m_itemTemplateID = itemTemplateID;
            m_itemTemplate = ItemTemplateManager.GetItemForID(itemTemplateID);
            m_quantity = quantity;
            m_price = price;
            m_shopName = shopName;
            m_stock_type = stock_type;
        }

		internal void WriteLocaliseItemToMessage(NetOutgoingMessage outmsg, Player player)
		{
			outmsg.WriteVariableInt32(m_stockID);
			outmsg.WriteVariableInt32(m_itemTemplateID);
			outmsg.WriteVariableInt32(m_quantity);
			outmsg.WriteVariableInt32(m_price);
			outmsg.Write(PremiumShop.GetLocaliseItemShopName(player, m_stockID));
			outmsg.Write(PremiumShop.GetLocaliseItemShopDecs(player, m_stockID));
			outmsg.WriteVariableInt32(m_stock_type);
		}

        static internal void WriteEmptyItemToMessage(NetOutgoingMessage outmsg)
        {
            outmsg.WriteVariableInt32(-1);
            outmsg.WriteVariableInt32(-1);
            outmsg.WriteVariableInt32(-1);
            outmsg.WriteVariableInt32(-1);
            outmsg.Write(String.Empty);
            outmsg.Write(String.Empty);
            outmsg.WriteVariableInt32(-1);
        }
    };

    class PlatinumBundleInfo
    {
        int m_bundleID = 0;
        string m_bundleIdentifier = String.Empty;
        string m_bundleText = String.Empty;

        internal PlatinumBundleInfo(int bundleID, string bundleIdentifier, string bundleText, int bundlePlatinum, float poundValue)
        {
            m_bundleID = bundleID;
            m_bundleIdentifier = bundleIdentifier;
            m_bundleText = bundleText;
        }

        internal int BundleID { get { return m_bundleID; } }
        internal string BundleIdentifier { get { return m_bundleIdentifier; } }
        internal string BundleText { get { return m_bundleText; } }
    }

    class PremiumShop
    {
		// #localisation
		public class PremiumShopTextDB : TextEnumDB
		{
			public PremiumShopTextDB() : base(nameof(PremiumShop), typeof(TextID)) { }

			public enum TextID
			{
				PURCHASED_OVER_MAXIMUM_CHARGED,             //"You have purchased over the maximum amount for this item, you have only been charged for {quantity0}."
				PURCHASED_OVER_MAXIMUM_NO_CHARGED,          //"You have purchased over the maximum amount for this item, you have not been charged."
				GAINED_GOLD,                                //"You have gained {amount0} gold"
				GAINED_CHARACTER_SLOT,                      //"You have gained {quantity0} character slot"
				GAINED_CHARACTER_SLOTS,                     //"You have gained {quantity0} character slots"
				SERVER_FAILED_VERIFY_PURCHASE,              //"Server failed to verify purchase."
				PURCHASED_PLATINUM,                         //"You Purchased {amount0} Platinum"
				PURCHASED_ITEM,                             //"You Purchased {quantity0} {itemName1}"
				NOT_ENOUGHT_PLATINUM,                       //"not Enough Platinum"
				INVALID_ITEM,                               //"Invalid item"
			}
		}
		public static PremiumShopTextDB textDB = new PremiumShopTextDB();

		// #localisation
		static int itemShopNameTextDBIndex = 0;
		static int itemShopDescTextDBIndex = 0;

		internal enum INSTANT_USE_ITEM_ID
        {
            NONE = 0,
            COIN_BAG = 14,
            BACKPACK = 13,
            HEALTH_REGEN_1 = 8,
            ENERGY_REGEN_1 = 9,
            CHARACTER_SLOT = 16,
            COIN_BAG_1 = 23635,
            COIN_BAG_2 = 23636,
            COIN_BAG_3 = 23637,
            COIN_BAG_4 = 23638,
            COIN_BAG_5 = 23639,
            COIN_BAG_6 = 34877,
            COIN_BAG_7 = 34973, 
            COIN_BAG_8 = 34974,
            COIN_BAG_9 = 35015,
            COIN_BAG_10 = 55774,
            COIN_BAG_11 = 55775,
            COIN_BAG_12 = 58551,
            COIN_BAG_13 = 58552,
            PLAT_BAG_1 = 34770,
            PLAT_BAG_2 = 34771,
            PLAT_BAG_3 = 34772,
            PLAT_BAG_4 = 34773,
            PLAT_BAG_5 = 34774,
            PLAT_BAG_6 = 34775,
            PLAT_BAG_7 = 34785
        }
        static int OFFER_ID_GAP = 100;
        List<PremiumShopItem> m_shopStock = new List<PremiumShopItem>();
        List<PlatinumBundleInfo> m_platinumBundles = new List<PlatinumBundleInfo>();

        int m_maxItemStockID = -1;
        int m_offerStartID = -1;

        internal int OfferStartID
        {
            get { return m_offerStartID; }
        }
        public PremiumShop()
        {
            ReadStockFromDatabase();
            ReadPlatinumPurchasesFromDatabase();

			// Get textNameDB index.
			itemShopNameTextDBIndex = Localiser.GetTextDBIndex("premium_shop_stock - item_shop_name");
			itemShopDescTextDBIndex = Localiser.GetTextDBIndex("premium_shop_stock - item_shop_description");
		}

        void ReadStockFromDatabase()
        {
            SqlQuery itemQuery = new SqlQuery(Program.processor.m_dataDB, "select * from premium_shop_stock order by sort_order");
            if (itemQuery.HasRows)
            {
                while (itemQuery.Read())
                {
                    int stockID = itemQuery.GetInt32("item_shop_stock_id");
                    int itemTemplateID = itemQuery.GetInt32("item_id");
                    int quantity = itemQuery.GetInt32("item_quantity");
                    int price = itemQuery.GetInt32("price");
                    string shopName = itemQuery.GetString("item_shop_name");
                    string shopDescription = itemQuery.GetString("item_shop_description");// itemQuery.GetString("item_shop_description");
                    int stock_type = itemQuery.GetInt32("premium_shop_type_id");
                    PremiumShopItem shopitem = null;// new ShopItem(itemQuery.GetInt32("item_id"), itemQuery.GetInt32("stock_level"));
                    if (quantity > 0)
                    {
                        shopitem = new PremiumShopItem(stockID, itemTemplateID, quantity, price, shopName, shopDescription,stock_type);
                        m_shopStock.Add(shopitem);
                    }
                    if (stockID > m_maxItemStockID)
                    {
                        m_maxItemStockID = stockID;
                    }
                }
            }
            itemQuery.Close();
            m_offerStartID = m_maxItemStockID + PremiumShop.OFFER_ID_GAP;
            Program.Display("Offer Item Shop ID starts at " + m_offerStartID) ;
        }
        void ReadPlatinumPurchasesFromDatabase() 
        {
            m_platinumBundles.Clear();
            SqlQuery platQuery = new SqlQuery(Program.processor.m_universalHubDB, "select * from inapp_purchases order by inapp_purchase_id");
            if (platQuery.HasRows)
            {
                while (platQuery.Read())
                {

                    int bundleID =  platQuery.GetInt32("inapp_purchase_id");
                    string bundleIdentifier = platQuery.GetString("inapp_purchase_name");
                    int bundlePlatinum = platQuery.GetInt32("plat_value");
                    string bundleText = Convert.ToString(bundlePlatinum);
                    float poundValue = platQuery.GetFloat("pound_value");
                    PlatinumBundleInfo newPlatInfo = new PlatinumBundleInfo(bundleID, bundleIdentifier, bundleText, bundlePlatinum, poundValue);
                    m_platinumBundles.Add(newPlatInfo);
                }
            }
            platQuery.Close();
        }

		void WriteLocaliseShopStockToMessage(NetOutgoingMessage outmsg, Player player)
		{
			outmsg.WriteVariableInt32(m_shopStock.Count);
			for (int i = 0; i < m_shopStock.Count; i++)
			{
				PremiumShopItem currentItem = m_shopStock[i];
				if (currentItem != null)
				{
					currentItem.WriteLocaliseItemToMessage(outmsg, player);
				}
				else
				{
					PremiumShopItem.WriteEmptyItemToMessage(outmsg);
				}
			}
		}
		//void WriteShopStockToMessage(NetOutgoingMessage outmsg)
  //      {
  //          outmsg.WriteVariableInt32(m_shopStock.Count);
  //          for (int i = 0; i < m_shopStock.Count; i++)
  //          {
  //              PremiumShopItem currentItem = m_shopStock[i];
  //              if (currentItem != null)
  //              {
  //                  currentItem.WriteItemToMessage(outmsg);
  //              }
  //              else
  //              {
  //                  PremiumShopItem.WriteEmptyItemToMessage(outmsg);
  //              }
  //          }
  //      }
        void WriteShopStockToMessageIncludingOffers(NetOutgoingMessage outmsg, Player player)
        {
            List<CharacterOfferData> specialOffers = player.m_activeCharacter.OfferManager.GetItemShopOffers();

            outmsg.WriteVariableInt32(m_shopStock.Count + specialOffers.Count);

            for (int i = 0; i < specialOffers.Count; i++)
            {
                CharacterOfferData currentItem = specialOffers[i];
                if (currentItem != null)
                {
					currentItem.WriteLocaliseItemToMessage(outmsg, player);
					//currentItem.WriteItemToMessage(outmsg);
				}
                else
                {
                    PremiumShopItem.WriteEmptyItemToMessage(outmsg);
                }
            }
            for (int i = 0; i < m_shopStock.Count; i++)
            {
                PremiumShopItem currentItem = m_shopStock[i];
                if (currentItem != null)
                {
					currentItem.WriteLocaliseItemToMessage(outmsg, player);
                    //currentItem.WriteItemToMessage(outmsg);
                }
                else
                {
                    PremiumShopItem.WriteEmptyItemToMessage(outmsg);
                }
            }
        }
        void SendPremiumShopReply(Player thePlayer)
        {
            NetOutgoingMessage shopMessage = Program.Server.CreateMessage();
            shopMessage.WriteVariableUInt32((uint)NetworkCommandType.ItemShop);
            shopMessage.WriteVariableInt32((int)PREMIUM_SHOP_MESSAGE.PSM_SHOP_REPLY);
            shopMessage.Write((byte)1);
			WriteLocaliseShopStockToMessage(shopMessage, thePlayer);
            //WriteShopStockToMessage(shopMessage);

            Program.processor.SendMessage(shopMessage, thePlayer.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.ItemShop);
        }
        internal void SendPremiumShopReplyWithOffers(Player thePlayer)
        {
            NetOutgoingMessage shopMessage = Program.Server.CreateMessage();
            shopMessage.WriteVariableUInt32((uint)NetworkCommandType.ItemShop);
            shopMessage.WriteVariableInt32((int)PREMIUM_SHOP_MESSAGE.PSM_SHOP_REPLY);
            shopMessage.Write((byte)1);
            WriteShopStockToMessageIncludingOffers(shopMessage, thePlayer);

            Program.processor.SendMessage(shopMessage, thePlayer.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.ItemShop);
        }
        void AttemptToSendPremiumShopReply(Player thePlayer)
        {
            Character activeCharacter = thePlayer.m_activeCharacter;
            if (SpecialOfferTemplate.SPECIAL_OFFERS_ACTIVE == true && activeCharacter!=null)
            {
                activeCharacter.OfferManager.PrepareToSendItemShopList(activeCharacter);
            }
            else
            {
                SendPremiumShopReply(thePlayer);
            }
        }

        public void ProcessPremiumShopMessage(Player thePlayer, NetIncomingMessage msg)
        {
            PREMIUM_SHOP_MESSAGE messageType = (PREMIUM_SHOP_MESSAGE)msg.ReadVariableInt32();
            switch (messageType)
            {
                case PREMIUM_SHOP_MESSAGE.PSM_REQUEST_SHOP:
                    {
                        SendPlatinumBundleInfo(thePlayer);
                        //SendPremiumShopReply(thePlayer);
                        AttemptToSendPremiumShopReply(thePlayer);
                        break;
                    }
                case PREMIUM_SHOP_MESSAGE.PSM_BUY_ITEM_REQUEST:
                    {
                        ReadItemPurchaseRequest(thePlayer, msg);
                        break;
                    }
            }
        }

        public void SendOpenItemShop(Player thePlayer)
        {
            NetOutgoingMessage shopMessage = Program.Server.CreateMessage();
            shopMessage.WriteVariableUInt32((uint)NetworkCommandType.ItemShop);
            shopMessage.WriteVariableInt32((int)PREMIUM_SHOP_MESSAGE.PSM_OPEN_ITEM_SHOP_MYSTERY_CHEST);
            Program.processor.SendMessage(shopMessage, thePlayer.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.ItemShop);
        }

        void ReadItemPurchaseRequest(Player thePlayer, NetIncomingMessage msg)
        {
            //ANA-ITEMSHOP
            int stockID = msg.ReadVariableInt32();
            int quantity = msg.ReadVariableInt32();

            //certain players have discovered a way to login on multiple characters on the same account.
            //A quick check on the last known logged in character will show this up. Prevent the purchase in this case.
            if (!ExploitVerifier.IsLastCharacterLoggedIn(thePlayer.m_account_id, thePlayer.m_lastSelectedCharacter,
                    Program.m_worldID))
            {
                Program.Display("Player attempting to make purchase while multi-logging: " + thePlayer.m_account_id + " on character " + thePlayer.m_lastSelectedCharacter);
                return;
            }


            if (SpecialOfferTemplate.SPECIAL_OFFERS_ACTIVE==true &&stockID >= m_offerStartID)
            {
                Character theCharacter = thePlayer.m_activeCharacter;
                if (theCharacter != null)
                {
                    theCharacter.OfferManager.OfferPurchasedFromItemShop(stockID,quantity,theCharacter);
                }
                return;
            }
            PremiumShopItem requestedItem = null;

            for (int i = 0; (i < m_shopStock.Count) && (requestedItem == null); i++)
            {
                PremiumShopItem currentItem = m_shopStock[i];
                if ((currentItem != null) && (currentItem.StockID == stockID))
                {
                    requestedItem = currentItem;
                }
            }

            if ((requestedItem != null) && (requestedItem.StockItemTemplate != null))
            {
                if (requestedItem.StockItemTemplate.m_autoUse == true &&
                    (requestedItem.ItemTemplateID == (int)PERMENENT_BUFF_ID.EXTRA_HUD_SLOT || requestedItem.ItemTemplateID == (int)PERMENENT_BUFF_ID.AUCTION_HOUSE_SLOT_EXPANSION))
                {
                    Character theCharacter = thePlayer.m_activeCharacter;
                    if (theCharacter != null)
                    {
                        // find out the max extra slots we can buy for this item
                        int canBuy = theCharacter.GetMaxExtraSlotsToBuy(requestedItem);
                        if (quantity > canBuy)
                        {
                            quantity = canBuy;
                            if (quantity > 0)
                            {
								string locText = Localiser.GetString(textDB, thePlayer, (int)PremiumShopTextDB.TextID.PURCHASED_OVER_MAXIMUM_CHARGED);
								locText = string.Format(locText, quantity);
								Program.processor.sendSystemMessage(locText, thePlayer, true, SYSTEM_MESSAGE_TYPE.POPUP);
							}
                            else
                            {
								string locText = Localiser.GetString(textDB, thePlayer, (int)PremiumShopTextDB.TextID.PURCHASED_OVER_MAXIMUM_NO_CHARGED);
								Program.processor.sendSystemMessage(locText, thePlayer, true, SYSTEM_MESSAGE_TYPE.POPUP);
								quantity = 0;
                            }
                        }
                     }
                }

                int cost = quantity * requestedItem.Price;

                //check the players Platinum
                if (thePlayer.m_platinum >= cost && quantity>0)
                {
                    Character theCharacter = thePlayer.m_activeCharacter;
                    if (theCharacter != null)
                    {
                        int totalQuantity = quantity * requestedItem.Quantity;
                        thePlayer.m_platinum -= cost;
                        try
                        {
                            using (TextWriter writer = File.AppendText("transactions\\itemShop" + DateTime.Now.ToString("yyyyMMdd") + ".txt"))
                            {
                                writer.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "," + thePlayer.m_account_id + "," + Program.m_worldID + "," 
                                    + theCharacter.m_character_id + "," + ((int)theCharacter.m_class.m_classType) + "," + theCharacter.Level + "," + requestedItem.StockID 
                                    + "," + requestedItem.ShopName + "," + quantity + "," + cost);
                                writer.Close();
                            }
                            Program.processor.m_universalHubDB.runCommandSync("insert into premium_purchases (purchase_date,account_id,world_id,character_id,class_id," +
                                                                              "level,item_shop_stock_id,item_shop_name,quantity,total_cost) values " +
                                                                              "(\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\"," + thePlayer.m_account_id + "," 
                                                                              + Program.m_worldID + "," + theCharacter.m_character_id + "," + ((int)theCharacter.m_class.m_classType) + "," 
                                                                              + theCharacter.Level + "," + requestedItem.StockID + ",\"" + requestedItem.ShopName + "\"," + quantity + "," + cost + ")");
                         
                        }
                        catch (Exception ex)
                        {
                            Program.Display("Failed to Write Transaction file:" + ex.ToString());
                        }

                        string tempStockType = String.Empty;
                        if (requestedItem.StockItemTemplate.m_autoUse == true)
                        {
                            switch(requestedItem.StockType)
                            {
                                case 0:
                                    tempStockType = "GOLD";
                                    break;
                                case 1:
                                    tempStockType = "MISC.";
                                    break;
                                case 2:
                                    tempStockType = "POTIONS";
                                    break;
                                case 3:
                                    tempStockType = "ELIXIRS";
                                    break;
                                default:
                                    tempStockType = "UNKNOWN";
                                    break;
                            }

                            switch ((INSTANT_USE_ITEM_ID)requestedItem.ItemTemplateID)
                            {
                                case INSTANT_USE_ITEM_ID.COIN_BAG:
                                case INSTANT_USE_ITEM_ID.COIN_BAG_1:
                                case INSTANT_USE_ITEM_ID.COIN_BAG_2:
                                case INSTANT_USE_ITEM_ID.COIN_BAG_3:
                                case INSTANT_USE_ITEM_ID.COIN_BAG_4:
                                case INSTANT_USE_ITEM_ID.COIN_BAG_5:
                                case INSTANT_USE_ITEM_ID.COIN_BAG_6:
                                case INSTANT_USE_ITEM_ID.COIN_BAG_7:
                                case INSTANT_USE_ITEM_ID.COIN_BAG_8:
                                case INSTANT_USE_ITEM_ID.COIN_BAG_9:
                                case INSTANT_USE_ITEM_ID.COIN_BAG_10:
                                case INSTANT_USE_ITEM_ID.COIN_BAG_11:
                                case INSTANT_USE_ITEM_ID.COIN_BAG_12:
                                case INSTANT_USE_ITEM_ID.COIN_BAG_13:
                                    {
                                        int coinsAdded = requestedItem.StockItemTemplate.m_sellprice * totalQuantity;
										string locMessage = Localiser.GetString(textDB, thePlayer, (int)PremiumShopTextDB.TextID.GAINED_GOLD);
										locMessage = string.Format(locMessage, coinsAdded);
										Program.processor.sendSystemMessage(locMessage, thePlayer, true, SYSTEM_MESSAGE_TYPE.POPUP);
										theCharacter.updateCoins(coinsAdded);
                                        
                                        if (Program.m_LogAnalytics)
                                        {
                                            AnalyticsMain logAnalytics = new AnalyticsMain(true);
                                            logAnalytics.itemShopGoldPurchase(thePlayer, cost, coinsAdded);
                                        }

                                        break;
                                    }
                                case INSTANT_USE_ITEM_ID.CHARACTER_SLOT:
                                    {
                                        thePlayer.m_totalCharacterSlots += totalQuantity;

										string locMessage = "";
										if (totalQuantity > 1)
										{
											locMessage = Localiser.GetString(textDB, thePlayer, (int)PremiumShopTextDB.TextID.GAINED_CHARACTER_SLOTS);
										}
										else
										{
											locMessage = Localiser.GetString(textDB, thePlayer, (int)PremiumShopTextDB.TextID.GAINED_CHARACTER_SLOT);
										}
										locMessage = string.Format(locMessage, totalQuantity);
										Program.processor.sendSystemMessage(locMessage, thePlayer, true, SYSTEM_MESSAGE_TYPE.POPUP);
										thePlayer.SaveCharacterSlots();

                                        if (Program.m_LogAnalytics)
                                        {
                                            AnalyticsMain logAnalytics = new AnalyticsMain(true);
                                            logAnalytics.itemShopItemPurchase(thePlayer, cost, requestedItem.ShopName, tempStockType, quantity);
                                        }
                                        break;
                                    }
                                default:
                                    {
                                        theCharacter.AddPermBuffsToCharacter((PERMENENT_BUFF_ID)requestedItem.ItemTemplateID, totalQuantity,true);
                                        
                                        if(Program.m_LogAnalytics)
                                        {
                                            AnalyticsMain logAnalytics = new AnalyticsMain(true);
                                            logAnalytics.itemShopItemPurchase(thePlayer, cost, requestedItem.ShopName, tempStockType, quantity);
                                        }
                                        break;
                                    }
                            }
                        }
                        else
                        {
                            theCharacter.m_inventory.AddNewItemToCharacterInventory(requestedItem.ItemTemplateID, totalQuantity, false);
                            if(Program.m_LogAnalytics)
                            {
                                AnalyticsMain logAnalytics = new AnalyticsMain(true);
                                logAnalytics.itemShopItemPurchase(thePlayer, cost, requestedItem.ShopName, tempStockType, quantity);
                            }
                        }

						string locText = Localiser.GetString(textDB, thePlayer, (int)PremiumShopTextDB.TextID.PURCHASED_ITEM);
						locText = string.Format(locText, totalQuantity, requestedItem.StockItemTemplate.m_loc_item_name[thePlayer.m_languageIndex]);
						SendItemBuyReply(thePlayer, locText, true);
						Program.processor.m_universalHubDB.runCommandSync("update account_details set platinum=" + thePlayer.m_platinum + " where account_id=" + thePlayer.m_account_id);
                    }
                }
                else
                {//not enough money
					string locText = Localiser.GetString(textDB, thePlayer, (int)PremiumShopTextDB.TextID.NOT_ENOUGHT_PLATINUM);
					SendItemBuyReply(thePlayer, locText, false);
				}
            }
            else
            {
				//the item doesn't exist
				string locText = Localiser.GetString(textDB, thePlayer, (int)PremiumShopTextDB.TextID.INVALID_ITEM);
				SendItemBuyReply(thePlayer, locText, false);
			}
        }
        internal void SendPlatinumBundleInfo(Player player)
        {
            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.PlatinumBundelInfo);
            outmsg.WriteVariableInt32(m_platinumBundles.Count);
            for (int i = 0; i < m_platinumBundles.Count; i++)
            {
                PlatinumBundleInfo currentInfo = m_platinumBundles[i];
                outmsg.WriteVariableInt32(currentInfo.BundleID);
                outmsg.Write(currentInfo.BundleIdentifier);
                outmsg.Write(currentInfo.BundleText);
            }

            Program.processor.SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.PlatinumBundelInfo);
        }
        static internal void SendItemBuyReply(Player thePlayer, string infoString, bool success)
        {
            NetOutgoingMessage shopMessage = Program.Server.CreateMessage();
            shopMessage.WriteVariableUInt32((uint)NetworkCommandType.ItemShop);
            shopMessage.WriteVariableInt32((int)PREMIUM_SHOP_MESSAGE.PSM_BUY_ITEM_REPLY);
            if (success == true)
            {
                shopMessage.Write((byte)1);
            }
            else
            {
                shopMessage.Write((byte)0);
            }

            shopMessage.Write(infoString);

            shopMessage.WriteVariableInt32(thePlayer. m_platinum);
            if (success == true)
            {
                thePlayer.m_activeCharacter.m_inventory.WriteInventoryWithMoneyToMessage(shopMessage);
            }
            Program.processor.SendMessage(shopMessage, thePlayer.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.ItemShop);
        }

        internal static void ReadPlatinumPurchaseReceiptApple(Player thePlayer, string playerName, string productIdentifier, string transactionIdentifier, string dateStr, byte[] receipt)
        {
            try
            {
                DateTime localDate = DateTime.MinValue;
                try
                {
                    localDate=DateTime.Parse(dateStr);
                }
                catch(Exception e)
                {
                    Program.DisplayDelayed("error in parse date" + dateStr + " " + e.Message + " " + e.StackTrace);
                }

                DateTime serverDate = DateTime.Now;
                if (thePlayer != null)
                {
                    string needsVerificationStr = "0";
                    string verificationError = VerifyPurchaseApple(playerName, receipt, productIdentifier, transactionIdentifier);

                    bool needsVerification = false;
                    Regex reNum = new Regex(@"^\d+$");
                    if (verificationError.Length>0)
                    {
                        needsVerificationStr = "1";
                    }
                    if (!reNum.Match(transactionIdentifier).Success)
                    {
                        verificationError = "not numeric";
                        needsVerificationStr = "1";
                        needsVerification = true;
                    }
                    try
                    {
						//SqlQuery query = new SqlQuery(Program.processor.m_universalHubDB, "select * from inapp_purchases where inapp_purchase_name=\"" + productIdentifier + "\"");

						List<MySqlParameter> sqlParams = new List<MySqlParameter>();
						sqlParams.Add(new MySqlParameter("@inapp_purchase_name", productIdentifier));

						SqlQuery query = new SqlQuery(Program.processor.m_universalHubDB, "select * from inapp_purchases where inapp_purchase_name=@inapp_purchase_name", sqlParams.ToArray());

						if (query.Read())
                        {
							//SqlQuery subQuery = new SqlQuery(Program.processor.m_universalHubDB, "select * from inapp_transactions where transaction_id='" + transactionIdentifier + "'");
							sqlParams.Clear();

							sqlParams.Add(new MySqlParameter("@transaction_id", transactionIdentifier));

							SqlQuery subQuery = new SqlQuery(Program.processor.m_universalHubDB, "select * from inapp_transactions where transaction_id=@transaction_id", sqlParams.ToArray());

							if (subQuery.Read())
                            {
                                Program.DisplayDelayed(playerName + " duplicate purchase " + productIdentifier + " with transactionID='" + transactionIdentifier + "'");
                                SendPlatinumConfirmation(thePlayer, 0, transactionIdentifier, productIdentifier);
                            }
                            else
                            {
                                if (needsVerification)
                                {
                                    Program.DisplayDelayed(playerName + " Error: unrecognised transactionid format.  Tried to purchase " + productIdentifier + " with transactionID=" + transactionIdentifier);
                                    SendPlatinumConfirmation(thePlayer, 0, transactionIdentifier, productIdentifier);
                                }
                                else if (verificationError.Length>0)
                                {
                                    Program.DisplayDelayed(playerName + " Error: verification error :"+verificationError + " tried to purchase " + productIdentifier + " with transactionID=" + transactionIdentifier  );
                                    SendPlatinumConfirmation(thePlayer, 0, transactionIdentifier, productIdentifier);

									string locText = Localiser.GetString(textDB, thePlayer, (int)PremiumShopTextDB.TextID.SERVER_FAILED_VERIFY_PURCHASE);
									Program.processor.sendSystemMessage(locText, thePlayer, true, SYSTEM_MESSAGE_TYPE.POPUP);
								}
                                else
                                {
                                    Program.DisplayDelayed(playerName + " purchased " + productIdentifier + " with transactionID='" + transactionIdentifier + "'");
                                }
								//Program.processor.m_universalHubDB.runCommandSync("insert into inapp_transactions (transaction_id,account_id,inapp_purchase_id,local_time,server_time,needs_verification,world_id,verification_error) values ('" + transactionIdentifier + "'," + thePlayer.m_account_id + "," + query.GetInt32("inapp_purchase_id") + ",'" + localDate.ToString("yyyy-MM-dd HH:mm:ss") + "','" + serverDate.ToString("yyyy-MM-dd HH:mm:ss") + "'," + needsVerificationStr + "," + Program.m_worldID + ",'"+verificationError+"')");

								sqlParams.Clear();

								sqlParams.Add(new MySqlParameter("@transaction_id", transactionIdentifier));
								sqlParams.Add(new MySqlParameter("@account_id", thePlayer.m_account_id));
								sqlParams.Add(new MySqlParameter("@inapp_purchase_id", query.GetInt32("inapp_purchase_id")));
								sqlParams.Add(new MySqlParameter("@local_time", localDate.ToString("yyyy-MM-dd HH:mm:ss")));
								sqlParams.Add(new MySqlParameter("@server_time", serverDate.ToString("yyyy-MM-dd HH:mm:ss")));
								sqlParams.Add(new MySqlParameter("@world_id", Program.m_worldID));
								sqlParams.Add(new MySqlParameter("@verification_error", verificationError));

								Program.processor.m_universalHubDB.runCommandSyncWithParams("insert into inapp_transactions " + 
									"(transaction_id,account_id,inapp_purchase_id,local_time,server_time,needs_verification,world_id,verification_error) values " + 
									"(@transaction_id,@account_id,@inapp_purchase_id,@local_time,@server_time," + needsVerificationStr + ",@world_id,@verification_error)", sqlParams.ToArray());

								if (!needsVerification && verificationError.Length==0)//plat purchase confirmation?
                                {
                                    double costPounds=query.GetDouble("pound_value");
                                    int costPlat=query.GetInt32("plat_value");
                                    thePlayer.m_platinum += costPlat;    
                                    thePlayer.SavePlatinum(costPlat,costPounds);
                                    SendPlatinumConfirmation(thePlayer, 1, transactionIdentifier, productIdentifier);
                                    if (Program.m_LogAnalytics)
                                    {
                                        AnalyticsMain logAnalytics = new AnalyticsMain(true);
                                        int noPenniesCash = (int)(100 * costPounds);
                                        logAnalytics.itemShopPlatPurchase(thePlayer, noPenniesCash, costPlat, transactionIdentifier);
                                    }
                                    if (thePlayer.m_activeCharacter != null)
                                    {
										string locText = Localiser.GetString(textDB, thePlayer, (int)PremiumShopTextDB.TextID.PURCHASED_PLATINUM);
										locText = string.Format(locText, costPlat);
										Program.processor.sendSystemMessage(locText, thePlayer, false, SYSTEM_MESSAGE_TYPE.SHOP);
									}
                                }
                            }
                            subQuery.Close();
                        }
                        else
                        {
                            Program.DisplayDelayed(playerName + " Error: purchased unknown product " + productIdentifier + " with transactionID=" + transactionIdentifier);
                            SendPlatinumConfirmation(thePlayer, 0, transactionIdentifier, productIdentifier);
                        }
                        if (thePlayer.m_activeCharacter != null)
                        {
                            Program.DisplayDelayed(playerName + " transaction made by character " + thePlayer.m_activeCharacter.m_character_id + " " + thePlayer.m_activeCharacter.Name);
                        }
                    }
                    catch (Exception)
                    {
                        Program.DisplayDelayed(playerName + " Failed to process transaction id=" + transactionIdentifier + ", prod=" + productIdentifier + ", id=" + thePlayer.m_account_id);
                    }

                    try
                    {
                        FileStream writeStream = new FileStream("transactions\\" + transactionIdentifier + "_" + thePlayer.m_account_id + ".bin", FileMode.Create);
                        BinaryWriter writeBinary = new BinaryWriter(writeStream);
                        writeBinary.Write(receipt);
                        writeBinary.Close();
                    }
                    catch (Exception ex)
                    {
                        Program.DisplayDelayed(playerName + " Failed to Write Transaction file:" + ex.ToString());
                    }
                }
                else
                {
                    Program.DisplayDelayed("unknown player purchased (Apple) " + transactionIdentifier);
                }
            }
            catch (Exception e)
            {
                Program.DisplayDelayed("error in process platinum (Apple) " + e.Message + " " + e.StackTrace);
            }
        }

        internal void ReadPlatinumPurchaseReceiptGooglePlay(Player thePlayer, string playerName, string productIdentifier, string transactionIdentifier, string dateStr)
        {
            try
            {
                DateTime localDate = DateTime.MinValue;
                try
                {
                    // DateStr is actually total milliseconds after epoch.
                    long unixTime = Convert.ToInt64(dateStr);

                    DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();
                    localDate = epoch.AddMilliseconds(unixTime);
                }
                catch (Exception e)
                {
                    Program.DisplayDelayed(playerName + " error in parse date" + dateStr + " " + e.Message + " " + e.StackTrace);
                }

                DateTime serverDate = DateTime.Now;
                if (thePlayer != null)
                {
                    string needsVerificationStr = "0";
                    string verificationError = String.Empty;

                    verificationError = VerifyPurchaseGoogle(playerName, productIdentifier, transactionIdentifier);

                    bool needsVerification = false;
                    Regex reNum = new Regex(@"^\d+$");
                    if (verificationError.Length > 0)
                    {
                        needsVerificationStr = "1";
                    }

                    try
                    {
						//SqlQuery query = new SqlQuery(Program.processor.m_universalHubDB, "select * from inapp_purchases where inapp_purchase_name=\"" + productIdentifier + "\"");

						List<MySqlParameter> sqlParams = new List<MySqlParameter>();
						sqlParams.Add(new MySqlParameter("@inapp_purchase_name", productIdentifier));

						SqlQuery query = new SqlQuery(Program.processor.m_universalHubDB, "select * from inapp_purchases where inapp_purchase_name=@inapp_purchase_name", sqlParams.ToArray());

						if (query.Read())
                        {
							//SqlQuery subQuery = new SqlQuery(Program.processor.m_universalHubDB, "select * from inapp_transactions where transaction_id='" + transactionIdentifier + "'");

							sqlParams.Clear();

							sqlParams.Add(new MySqlParameter("@transaction_id", transactionIdentifier));

							SqlQuery subQuery = new SqlQuery(Program.processor.m_universalHubDB, "select * from inapp_transactions where transaction_id=@transaction_id", sqlParams.ToArray());

							if (subQuery.Read())
                            {
                                Program.DisplayDelayed(playerName + " duplicate purchase " + productIdentifier + " with transactionID='" + transactionIdentifier + "'");
                                SendPlatinumConfirmation(thePlayer, 0, transactionIdentifier, productIdentifier);
                            }
                            else
                            {
                                if (needsVerification)
                                {
                                    Program.DisplayDelayed("Error: unrecognised transactionid format. Player :" + playerName + " tried to purchase " + productIdentifier + " with transactionID=" + transactionIdentifier);
                                    SendPlatinumConfirmation(thePlayer, 0, transactionIdentifier, productIdentifier);
                                }
                                else if (verificationError.Length > 0)
                                {
                                    Program.DisplayDelayed(playerName + " Error: verification error :" + verificationError + " tried to purchase " + productIdentifier + " with transactionID=" + transactionIdentifier);
                                    SendPlatinumConfirmation(thePlayer, 0, transactionIdentifier, productIdentifier);

									string locText = Localiser.GetString(textDB, thePlayer, (int)PremiumShopTextDB.TextID.SERVER_FAILED_VERIFY_PURCHASE);
									Program.processor.sendSystemMessage(locText, thePlayer, true, SYSTEM_MESSAGE_TYPE.POPUP);
								}
                                else
                                {
                                    Program.DisplayDelayed(playerName + " purchased " + productIdentifier + " with transactionID='" + transactionIdentifier + "'");
                                }

								//Program.processor.m_universalHubDB.runCommandSync("insert into inapp_transactions (transaction_id,account_id,inapp_purchase_id,local_time,server_time,needs_verification,world_id,verification_error) values ('" + transactionIdentifier + "'," + thePlayer.m_account_id + "," + query.GetInt32("inapp_purchase_id") + ",'" + localDate.ToString("yyyy-MM-dd HH:mm:ss") + "','" + serverDate.ToString("yyyy-MM-dd HH:mm:ss") + "'," + needsVerificationStr + "," + Program.m_worldID + ",'" + verificationError + "')");

								sqlParams.Clear();

								sqlParams.Add(new MySqlParameter("@transaction_id", transactionIdentifier));
								sqlParams.Add(new MySqlParameter("@account_id", thePlayer.m_account_id));
								sqlParams.Add(new MySqlParameter("@inapp_purchase_id", query.GetInt32("inapp_purchase_id")));
								sqlParams.Add(new MySqlParameter("@local_time", localDate.ToString("yyyy-MM-dd HH:mm:ss")));
								sqlParams.Add(new MySqlParameter("@server_time", serverDate.ToString("yyyy-MM-dd HH:mm:ss")));
								sqlParams.Add(new MySqlParameter("@world_id", Program.m_worldID));
								sqlParams.Add(new MySqlParameter("@verification_error", verificationError));

								Program.processor.m_universalHubDB.runCommandSyncWithParams("insert into inapp_transactions " + 
									"(transaction_id,account_id,inapp_purchase_id,local_time,server_time,needs_verification,world_id,verification_error) values " + 
									"(@transaction_id,@account_id,@inapp_purchase_id,@local_time,@server_time," + needsVerificationStr + ",@world_id,@verification_error)", sqlParams.ToArray());

								if (!needsVerification && verificationError.Length == 0)//plat purchase confirmation?
                                {
                                    double costPounds = query.GetDouble("pound_value");
                                    int costPlat = query.GetInt32("plat_value");
                                    thePlayer.m_platinum += costPlat;
                                    thePlayer.SavePlatinum(costPlat, costPounds);
                                    Program.Display("Player: " + thePlayer.m_activeCharacter + " purchased " + costPlat.ToString());
                                    SendPlatinumConfirmation(thePlayer, 1, transactionIdentifier, productIdentifier);

                                    if (Program.m_LogAnalytics)
                                    {
                                        AnalyticsMain logAnalytics = new AnalyticsMain(true);
                                        int noPenniesCash = (int)(100 * costPounds);
                                        logAnalytics.itemShopPlatPurchase(thePlayer, noPenniesCash, costPlat, transactionIdentifier);
                                    }
                                    if (thePlayer.m_activeCharacter != null)
                                    {
										string locText = Localiser.GetString(textDB, thePlayer, (int)PremiumShopTextDB.TextID.PURCHASED_PLATINUM);
										locText = string.Format(locText, costPlat);
										Program.processor.sendSystemMessage(locText, thePlayer, false, SYSTEM_MESSAGE_TYPE.SHOP);
									}
                                }
                            }
                            subQuery.Close();
                        }
                        else
                        {
                            Program.DisplayDelayed(playerName + " Error Player :" + playerName + " purchased unknown product " + productIdentifier + " with transactionID=" + transactionIdentifier);
                            SendPlatinumConfirmation(thePlayer, 0, transactionIdentifier, productIdentifier);
                        }
                        if (thePlayer.m_activeCharacter != null)
                        {
                            Program.DisplayDelayed(playerName + " transaction made by character " + thePlayer.m_activeCharacter.m_character_id + " " + thePlayer.m_activeCharacter.Name);
                        }
                    }
                    catch (Exception)
                    {
                        Program.DisplayDelayed(playerName + " Failed to process transaction id=" + transactionIdentifier + ", prod=" + productIdentifier + ", id=" + thePlayer.m_account_id);
                    }
                }
                else
                {
                    Program.DisplayDelayed("unknown player purchased (Google) " + transactionIdentifier);
                }
            }
            catch (Exception e)
            {
                Program.DisplayDelayed("error in process platinum (Google) " + e.Message + " " + e.StackTrace);
            }
        }

        static internal void SendPlatinumConfirmation(Player thePlayer, byte success, String transactionIdentifier, string productIdentifier)
        {
            NetOutgoingMessage platMessage = Program.Server.CreateMessage();
            platMessage.WriteVariableUInt32((uint)NetworkCommandType.PlatinumConfirmation);
            platMessage.WriteVariableInt32(thePlayer.m_platinum);
            platMessage.Write(success);
            platMessage.Write(transactionIdentifier);
            platMessage.Write(productIdentifier);
            DelayedMessageDescriptor desc=new DelayedMessageDescriptor(platMessage, thePlayer.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.PlatinumConfirmation,false);
            lock (CommandProcessor.m_delayedMessages)
            {
                CommandProcessor.m_delayedMessages.Enqueue(desc);
            }
        }

        private static string VerifyPurchaseApple(string playerName, byte[] receipt, string productIdentifier, string transactionIdentifer)
        {
            int loops = 0;
            string error = String.Empty;

            while (loops < 5)
            {
                try
                {
                    // Default Production iTunes but override config to test.
                    string url = "https://buy.itunes.apple.com/verifyReceipt";
                    if (null != ConfigurationManager.AppSettings["AppleITunesUrl"])
                    {
                        url = ConfigurationManager.AppSettings["AppleITunesUrl"];
                    }
                    string text = String.Format("{0} Attempt to verify receipt for Transaction ID: {1} at [{2}]", playerName, transactionIdentifer, url);
                    Program.DisplayDelayed(text);

                    WebRequest request = WebRequest.Create(url);
                    request.Method = "POST";
                    request.ContentType = "application/json";
                    string encodedString = "{\"receipt-data\" : \"" + System.Convert.ToBase64String(receipt) + "\"}";
                    byte[] encodedbytes = System.Text.Encoding.UTF8.GetBytes(encodedString);

                    Stream dataStream = request.GetRequestStream();
                    dataStream.Write((byte[])encodedbytes, 0, encodedbytes.Length);
                    dataStream.Close();
                    WebResponse response = request.GetResponse();
                    // Program.Display(((HttpWebResponse)response).StatusDescription);
                    Stream data = response.GetResponseStream();
                    StreamReader reader = new StreamReader(data);
                    string responseFromServer = reader.ReadToEnd();
                    Program.DisplayDelayed(playerName + " Response from Apple " + responseFromServer);
                    reader.Close();
                    if (data != null)
                    {
                        data.Close();
                    }
                    response.Close();

                    AppleReceiptContainer container = JsonConvert.DeserializeObject<AppleReceiptContainer>(responseFromServer);
                    if (container.status != 0)
                    {
                        Program.DisplayDelayed(playerName + " failed receipt validation status code:" + container.status);
                        return "status code:"+container.status;
                    }
                    if (!container.receipt.product_id.Equals(productIdentifier))
                    {
                        Program.DisplayDelayed(playerName + " failed receipt validation mismatched productid" + container.receipt.product_id + " != " + productIdentifier);
                        return "mismatch productid"+container.receipt.product_id;
                    }
                    if (!container.receipt.transaction_id.Equals(transactionIdentifer))
                    {
                        Program.DisplayDelayed(playerName + " failed receipt validation mismatched transactionid" + container.receipt.transaction_id + " != " + transactionIdentifer);
                        return "mismatch transactionid" + container.receipt.transaction_id;
                    }
                    return String.Empty;
                }
                catch (Exception e)
                {
                    Program.DisplayDelayed(playerName + " error in validate receipt " + transactionIdentifer + " : " + e.GetType() + " : " + e.Message);
                    error = "exception:" + e.GetType();
                }
                loops++;
            }
            return error;
        }

        private string VerifyPurchaseGoogle(string playerName, string productIdentifier, string transactionIdentifer)
        {
            string error = String.Empty;
            string refreshToken = "1/QTk3A7juafegakHz0UW0Bvun5k9O7vGIyD6LoBgmD-g";

            var tokenResponse = getAccessToken(refreshToken);
            string requeststr = "https://www.googleapis.com/androidpublisher/v1.1/applications/com.onethumbmobile.celticheroes/inapp/"
                + productIdentifier + "/purchases/" + transactionIdentifer + "?access_token=" + tokenResponse.access_token;

            try
            {
                WebRequest validateRequest = WebRequest.Create(requeststr);
                validateRequest.Method = "GET";
                WebResponse validateResponse = validateRequest.GetResponse();
                Stream validateData = validateResponse.GetResponseStream();
                StreamReader validateReader = new StreamReader(validateData);
                string validateResponseFromServer = validateReader.ReadToEnd();
                validateReader.Close();
                validateData.Close();
                validateResponse.Close();
            }
            catch (Exception ex)
            {
                Program.DisplayDelayed(playerName + " error in validate receipt " + transactionIdentifer + " : " + ex.GetType() + " : " + ex.Message);
                error = "exception:" + ex.GetType();
            }

            return error;
        }

        private TokenResponse getAccessToken(string refreshToken)
        {
            WebRequest authRequest = WebRequest.Create("https://accounts.google.com/o/oauth2/token");
            authRequest.Method = "POST";
            authRequest.ContentType = "application/x-www-form-urlencoded";
            string encodedString = "grant_type=refresh_token" +
                                    "&client_id=802733667.apps.googleusercontent.com" +
                                    "&client_secret=1Ei5i-MQY-V8H7LnkxdNc1Wg" +
                                    "&refresh_token=" + refreshToken;

            byte[] encodedbytes = System.Text.Encoding.UTF8.GetBytes(encodedString);

            Stream dataStream = authRequest.GetRequestStream();
            dataStream.Write((byte[])encodedbytes, 0, encodedbytes.Length);
            dataStream.Close();
            WebResponse authResponse = authRequest.GetResponse();
        
            Stream data = authResponse.GetResponseStream();
            StreamReader reader = new StreamReader(data);
            string responseFromServer = reader.ReadToEnd();
            reader.Close();
            data.Close();
            authResponse.Close();
         
            TokenResponse container = JsonConvert.DeserializeObject<TokenResponse>(responseFromServer);
            if (container != null)
                Program.Display("Access Token: " + container.access_token);

            return container;
        }
        
		static internal string GetLocaliseItemShopName(Player player, int stockID)
		{
			return Localiser.GetString(itemShopNameTextDBIndex, player, stockID);
		}

		static internal string GetLocaliseItemShopDecs(Player player, int stockID)
		{
			return Localiser.GetString(itemShopDescTextDBIndex, player, stockID);
		}
	};

}