using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MainServer.Items;
using Lidgren.Network;
using MainServer.TokenVendors;
using MainServer.Localise;

namespace MainServer
{

	

	enum Inventory_Types
    {
        BACKPACK=0,
        TRADE=1,
        SOLO_BANK=2
    }
    class Inventory
	{
		// #localisation
		public class InventoryTextDB : TextEnumDB
		{
			public InventoryTextDB() : base(nameof(Inventory), typeof(TextID)) { }

			public enum TextID
			{
				GAINED_CHARACTER_SLOT,                      //"You have gained {quantity0} character slot"
				GAINED_CHARACTER_SLOTS,                     //"You have gained {quantity0} character slots"
				GAINED_GOLD,                                //"You have gained {amount0} gold"
				GAINED_PLATINUM,                            //"You have gained {amount0} platinum"
				STATUS_REMOVED,                             //"{statusEffectName0} removed"
				ITEM_CANNOT_EQUIPPED_SAME_TIME,             //"{itemName0} cannot be equipped at the same time as the following items: {itemName1}"
				CANNOT_USE_ITEM_FOR_TIME,                   //"Cannot use an item for at least another {time0} seconds."
				ITEM_USED,                                  //"{itemName0} used"
				NOT_USABLE_ITEM,                            //"not a usable item"
				DO_NOT_NEED_WHISTLE,                        //"You don't need to use the whistle right now."
				NOT_USABLE_BY_CHARACTER,                    //"not usable by character"
				INVALID_ITEM,                               //"invalid item"
				CANNOT_FIND_ITEM,                           //"can't find item"
				ALREADY_KNOW_SKILL,                         //"You already know this skill"
				ALREADY_KNOW_RECIPE,                        //"You already know this recipe"
				ALREADY_HAVE_ELIXIR_ACTIVE,                 //"You already have that elixir active"
				ALREADY_HAVE_POTION_ACTIVE,                 //"You already have that potion active"
				HEALTH_FULL,                                //"Your health is full"
				ENERGY_FULL,                                //"Your energy is full"
				HEALTH_AND_ENERGY_FULL,                     //"Your health and energy are full"
				INVALID_SKILL_TARGET,                       //"invalid target for {skillName0}"
				COMBINATION_EFFECT_ACTIVE,                  //"You already have a combination effect active"
				ILLEGAL_QUANTITY,                           //"illegal quantity"s
				PURCHASED_ITEMS,                            //"You have purchased {quantity0} {itemName1}s for {goldAmount2} gold."
                PURCHASED_ITEMS_NO_S,                       //"You have purchased {quantity0} {itemName1} for {goldAmount2} gold."
                PURCHASED_ITEM,                             //"You have purchased a {itemName0} for {goldAmount1} gold."
				USE_ITEM_FAILED,                            //"failed to use item {errorString0}"
				ITEM_NOT_FOUND,                             //"item not found"
				CANNOT_EQUIP_WHILE_MOUNTED,                 //"cannot equip while mounted"
				DOES_NOT_MEET_REQUIREMENTS,                 //"doesn't meet requirements"
				OTHER_ITEM_NEED_REMOVED,                    //"Other items need to be removed"
				NOT_EQUIPABLE,                              //"not equipable"
				DELETED_MORE_THAN_EQUIPED,                  //"deleted more items than equiped"
				DELETED_MORE_THAN_INVENTORY,                //"deleted more items than in inventory"
				COULD_NOT_ADD_ITEM,                         //"couldn't add item to inventory"
				CANNOT_AFFORD_ITEM,                         //"can't afford item"
				INSUFFICIENT_STOCK,                         //"insufficient stock"
				NO_ITEM_FOR_SALE,                           //"no such item for sale"
				UNKNOWN_ERROR,                              //"unknown error"
				CANNOT_SELL_THIS_ITEM,                      //"You cannot sell this item."
				SHOP_NOT_PURCHASING,                        //"The shop isn't interested in purchasing {itemName0}s"
			}
		}
		public static InventoryTextDB textDB = new InventoryTextDB();

		#region constants & enums:  EQUIPS_SLOTS, attack speeds

		public const int NUM_EQUIP_SLOTS = 23;
        public const int DEFAULT_ATTACK_SPEED = 2250;
        public const int MIN_ATTACK_SPEED = 1000;

        public const float ATTACK_TIME_ADD = 0.1f;

        public enum EQUIP_SLOT
        {
            SLOT_NONE = -2,
            SLOT_UNEQUIPABLE = -1,
            SLOT_WEAPON = 0,
            SLOT_HEAD = 1,
            SLOT_CHEST = 2,
            SLOT_LEG = 3,
            SLOT_FEET = 4,
            SLOT_OFFHAND = 5,
            SLOT_HANDS = 6,
            SLOT_MISC = 7,

            SLOT_RING_R1 = 8,
            SLOT_RING_R2 = 9,
            SLOT_RING_L1 = 10,
            SLOT_RING_L2 = 11,
            SLOT_BANGLE_R = 12,
            SLOT_BANGLE_L = 13,
            SLOT_NECK = 14,

            SLOT_FASH_HEAD = 15,
            SLOT_FASH_TORSO = 16,
            SLOT_FASH_LEGS = 17,
            SLOT_FASH_FEET = 18,
            SLOT_FASH_HANDS = 19,
			SLOT_COMPANION = 20,

			SLOT_SADDLE = 21,
			SLOT_MOUNT = 22,

            SLOT_FACE = 50,
            SLOT_HAIR = 51,
            SLOT_HAIR_COLOUR = 52,
            SLOT_FACE_ACCESSORY = 53,
            SLOT_FACE_ACCESSORY_COLOUR = 54,
            SLOT_SKIN = 55,
            SLOT_SKIN_COLOUR = 56,
            SLOT_CREATURESKIN = 57,
            SLOT_CREATURESKIN_COLOUR = 58,
            SLOT_RIG = 59,
            SLOT_GENDER = 60,
            SLOT_RACE = 61,
            SLOT_ATTACK_RANGE = 62,
            SLOT_SHOW_HEADGEAR = 63,
            SLOT_SHOW_FASHION = 64,
            SLOT_SCALE = 65,
            SLOT_SERVER_ID = 66,
            SLOT_NAME = 67,
            SLOT_ZONE_ID = 68,
            SLOT_LEVEL = 69,
            SLOT_CLASS = 70,
            SLOT_ATTACK_SPEED = 71,
            SLOT_PVP_LEVEL = 72,
            SLOT_PVP_RATING = 73,
            SLOT_CLAN_NAME = 74

        };

		#endregion

		

		public int m_coins = 0;
       
        public Character m_character=null;
        public Item[] m_equipedItems = new Item[NUM_EQUIP_SLOTS];

        public List<Item> m_bagItems;
        bool m_rewardsValid = false;
        public List<EquipmentSetRewardContainer> m_qualifiedRewards = new List<EquipmentSetRewardContainer>();
        public string m_inventoryTableName;
        public Inventory_Types m_InventoryType;
        public Inventory(Character character,Inventory_Types inventoryType)
        {
            m_character = character;
            m_bagItems = new List<Item>();
            m_InventoryType = inventoryType;
            if (m_InventoryType == Inventory_Types.BACKPACK|| m_InventoryType == Inventory_Types.TRADE)
            {
                m_inventoryTableName = "inventory";
            }
            else if(m_InventoryType==Inventory_Types.SOLO_BANK)
            {
                m_inventoryTableName = "bank";
            }
        }
        public Item GetEquipmentForSlot(int slot)
        {
            if (m_equipedItems[slot] == null)
            {
                return null;
            }

            return m_equipedItems[slot];
        }
        
        public void WriteEquipmentToMessage(NetOutgoingMessage outmsg)
        {
            //write Equiped Items
            for (int i = 0; i < m_equipedItems.Length; i++)
            {
                Item currentItem = m_equipedItems[i];
                if (currentItem != null)
                {
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
                    
                    if (currentItem.IsFavourite)
                    {
                        outmsg.Write((byte)1);
                    }
                    else
                    {
                        outmsg.Write((byte)0);
                    }
                }
                else
                {
                    outmsg.WriteVariableInt32(0);
                    outmsg.WriteVariableInt32(0);
                    outmsg.WriteVariableInt32(0);
                    outmsg.Write((byte)0);
                    outmsg.WriteVariableInt32(0);
                    outmsg.Write((byte)0);
                }
            }
        }

        public void WriteInventoryToMessage(NetOutgoingMessage outmsg)
        {

            //write the total number of items
            outmsg.WriteVariableInt32(m_bagItems.Count);
            Item currentItem= null;
            //write the items in the bag
            for (int i = 0; i < m_bagItems.Count; i++)
            {
                currentItem = m_bagItems[i];
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
                if (currentItem.IsFavourite)
                {
                    outmsg.Write((byte)1);
                }
                else
                {
                    outmsg.Write((byte)0);
                }
            }
        }

        public void WriteInventoryWithMoneyToMessage(NetOutgoingMessage outmsg)
        {
            outmsg.WriteVariableInt32(m_coins);
            WriteInventoryToMessage(outmsg);
        }
        internal void SendReplaceItem(Item currentItem, Item newItem)
        {
            if (currentItem == null)
            {
                Program.Display("#HOTFIX Inventory.SendReplaceItem first item is null. Character name " + m_character.Name);
                return;                
            }

            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.ReplaceItem);
            outmsg.WriteVariableInt32(currentItem.m_inventory_id);
            if (newItem != null)
            {
                outmsg.WriteVariableInt32(newItem.m_inventory_id);
                outmsg.WriteVariableInt32(newItem.m_template_id);
                outmsg.WriteVariableInt32(newItem.m_quantity);
                if (newItem.m_bound == true)
                {
                    outmsg.Write((byte)1);
                }
                else
                {
                    outmsg.Write((byte)0);
                }
                outmsg.WriteVariableInt32(newItem.m_remainingCharges);

            }
            else
            {
                outmsg.WriteVariableInt32(-1);
                outmsg.WriteVariableInt32(-1);
                outmsg.WriteVariableInt32(-1);
                outmsg.Write((byte)0);
                outmsg.WriteVariableInt32(-1);
                outmsg.Write((byte)0);
            }


            Program.processor.SendMessage(outmsg, m_character.m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.ReplaceItem);

        }
        internal void SendInventoryUpdate()
        {
            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.InventoryUpdate);

            WriteInventoryWithMoneyToMessage(outmsg);

            Program.processor.SendMessage(outmsg, m_character.m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.InventoryUpdate);
        }
        internal void SendUseItemReply(string errorString, float cooldownForItem)
        {
            Player player = m_character.m_player;
            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.UseItemReply);
            if (errorString != "")
            {
                outmsg.Write((byte)0);
				string locText = Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.USE_ITEM_FAILED);
				locText = string.Format(locText, errorString);
				outmsg.Write(locText);
			}
            else
            {
                outmsg.Write((byte)1);
                outmsg.Write(cooldownForItem);
                player.m_activeCharacter.m_inventory.WriteInventoryWithMoneyToMessage(outmsg);
            }
            Program.processor.SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.UseItemReply);
        }
        internal int getNextSortOrder()
        {
            int sortOrder = -1;
            
            if (m_bagItems.Count > 0)
            {
                sortOrder= m_bagItems[m_bagItems.Count - 1].m_sortOrder;
            }
            for(int i=0;i<m_equipedItems.Length;i++)
            {
                if (m_equipedItems[i] != null && m_equipedItems[i].m_sortOrder > sortOrder)
                {
                    sortOrder = m_equipedItems[i].m_sortOrder;
                }
            }
            return sortOrder+1;
        }
        public void FillBagForCharacterID(Database db, uint characterID)
        {

            SqlQuery query = new SqlQuery(db, "select * from "+m_inventoryTableName+" where character_id =" + characterID+" order by sort_order");
            if (query.HasRows)
            {
                while ((query.Read()))
                {
                    int inventoryID = query.GetInt32("inventory_id");

                    int templateId = query.GetInt32("item_id");
                    int quantity = query.GetInt32("quantity");

                    bool bound = query.GetBoolean("bound");
                    int remainingCharges = query.GetInt32("remaining_charges");
                    double timeRecharged = query.GetDouble("time_skill_last_cast");
                    int sortOrder = query.GetInt32("sort_order");
                    bool isFavourite = false;
                    if (m_inventoryTableName == "inventory")
                    {
                        isFavourite = query.GetBoolean("is_favourite");
                    }
                    Item newItem = new Item(inventoryID, templateId, quantity, bound, remainingCharges, timeRecharged, sortOrder, isFavourite);
                    m_bagItems.Add(newItem);

                }
            }

            query.Close();


        }
        public void PopulateRewardListFromDB(Database db, uint characterID)
        {
            SqlQuery query = new SqlQuery(db, "select * from character_equipment_set_rewards where character_id =" + characterID + " order by equipment_set_id");
            if (query.HasRows)
            {
                while ((query.Read()))
                { 
                    int setID = query.GetInt32("equipment_set_id");
                    int rewardID = query.GetInt32("equipment_set_reward_id");
                    double timeSinceLastCast = query.GetDouble("time_skill_last_cast");
                    EquipmentSet setForID = EquipmentSet.GetEquipmentSetForID(setID, Program.processor.m_equipmentSets);

                    if (setForID != null)
                    {
                        EquipmentSetRewards setReward = setForID.GetRewardForID(rewardID);
                        if (setReward != null)
                        {
                            EquipmentSetRewardContainer newContainer = new EquipmentSetRewardContainer(setReward, timeSinceLastCast, characterID);
                            m_qualifiedRewards.Add(newContainer);
                        }
                    }

                
                }
            }

            query.Close();
        }

        /// <summary>
        /// Places an item in an equipment slot, removing it from the bag
        /// Does Basic EquipCheck
        /// </summary>
        /// <param name="inventoryID"></param>
        /// <param name="desiredSlot"></param>
        /// <returns></returns>
        public int EquipItemNoDB(int inventoryID, int desiredSlot)
        {
            Item theItem = null;
            //Get Item from bag
            int itemsInBag = m_bagItems.Count;
            bool itemFound = false;
            for (int currentItem = 0; (currentItem < itemsInBag) && (itemFound == false); currentItem++)
            {
                if (m_bagItems[currentItem].m_inventory_id == inventoryID)
                {
                    theItem = m_bagItems[currentItem];
                    itemFound = true;
                }            
            }

            if (theItem == null)
            {
                return -1;
            }
            //if item exists check it is equipable
            ItemTemplate template = theItem.m_template;//ItemTemplateManager.GetItemForID(theItem.m_template_id);
            
            if (template == null)
            {
                return -1;
            }
            bool classCanEquip = template.CheckClassRestriction(m_character);
            
            //int slot = template.m_slotNumber;
            if (template.CanBeEquippedInSlot(desiredSlot)==false|| classCanEquip==false)
            {
                m_character.m_db.runCommandSync("delete from equipment where inventory_id=" + theItem.m_inventory_id);
                return -1; 
            }
            //unequip the slot
            UnequipItem(desiredSlot);
            //equip the slot
            m_equipedItems[desiredSlot] = theItem;
            //remove from bag
            m_bagItems.Remove(theItem);
            theItem.Equipped(m_character);

            return 1;
        }
        /// <summary>
        /// Unequips an item from the entered slot, adds item to the players bag
        /// </summary>
        /// <param name="slot"></param>
        /// <returns>1 if an item was removed, -1 if there was no item to remove</returns>
        public int UnequipItem(int slot)
        {
            //if anything is equiped to the slot
            if (m_equipedItems[slot] != null)
            {
              
                //add it to the bag
                Item item1 = m_equipedItems[slot];
                m_bagItems.Add(item1);
                item1.m_sortOrder = getNextSortOrder();
                m_character.m_db.runCommandSync("update " + m_inventoryTableName + " set sort_order=" + item1.m_sortOrder + " where inventory_id=" + item1.m_inventory_id);

                m_character.m_db.runCommandSync("delete from equipment where inventory_id=" + item1.m_inventory_id);
                //unequip it
                m_equipedItems[slot] = null;
                if (m_character.ShowFashion == true && slot > (int)EQUIP_SLOT.SLOT_FASH_HEAD)
                {
                    VerifyFashion();
                }
                return 1;
            }
            return -1;
        }

        internal void VerifyFashion()
        {
            bool fashionFound = false;
            for (int i = (int)EQUIP_SLOT.SLOT_FASH_HEAD; i < m_equipedItems.Count(); i++)
            {
                if (m_equipedItems[i] != null)
                {
                    //any item other than the head gear
                    //or the head gear if it's showing
                    if (i != (int)EQUIP_SLOT.SLOT_FASH_HEAD || m_character.ShowHeadgear == true)
                    {
                        fashionFound = true;
                    }
                }
            }
            if (fashionFound == false && m_character.ShowFashion == true)
            {
                m_character.ShowFashion = false;
                m_character.SaveCharacterPreferences();
            }

        }
        public void UnequipAllItems()
        {
            //if anything is equiped to the slot
            for (int i = 0; i < m_equipedItems.Count(); i++)
            {
                if (m_equipedItems[i] != null)
                {
                    //add it to the bag
                    m_bagItems.Add(m_equipedItems[i]);
                    m_equipedItems[i].Unequipped(m_character);
                    //unequip it
                    m_equipedItems[i] = null;
                    m_character.InfoUpdated((Inventory.EQUIP_SLOT)i);
                }
            }
            m_character.ShowFashion = false;
            m_character.SaveCharacterPreferences();
            ResetEquipmentSetRewards();
            m_character.AddSkillsFromEquipment(true);
            
       
            m_character.m_db.runCommandSync("delete from equipment where character_id="+m_character.m_character_id);
            NetOutgoingMessage outmsg = Program.processor.m_server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.EquipItemReply);
            outmsg.Write((byte)1);
            WriteEquipmentToMessage(outmsg);
            WriteInventoryToMessage(outmsg);
            Program.processor.SendMessage(outmsg, m_character.m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.EquipItemReply); 
        }

        //add an item 
        public Item AddExistingItemToCharacterInventory(Item item)
        {
            int inventoryID = item.m_inventory_id;
            ItemTemplate template = item.m_template;
            if (template.m_stackable)
            {
                for (int i = 0; i < m_bagItems.Count; i++)
                {
                    if (m_bagItems[i].m_template == template)
                    {
                        m_bagItems[i].m_quantity += item.m_quantity;
                        m_character.m_db.runCommandSync("update " + m_inventoryTableName + " set quantity=" + m_bagItems[i].m_quantity + " where inventory_id=" + m_bagItems[i].m_inventory_id);
                        return m_bagItems[i];
                    }
                }
            }
            if (m_InventoryType==Inventory_Types.BACKPACK) 
            {
                Item newItem = createItem(item);
               // m_character.m_db.runCommandSync("update inventory set character_id=" + m_character.m_character_id + ",item_id=" + newItem.m_template_id + ",quantity=" + newItem.m_quantity + ", remaining_charges =" + newItem.m_remainingCharges + ", sort_order=" + newItem.m_sortOrder + " where inventory_id=" + newItem.m_inventory_id);
                string setString = newItem.GetUpdateString(m_character.m_character_id);
                m_character.m_db.runCommandSync("update inventory set " +setString + " where inventory_id=" + newItem.m_inventory_id);

                m_bagItems.Add(newItem);
                return newItem;
            }
            else if (inventoryID==item.m_inventory_id && m_character.CompiledStats.SoloBankSizeLimit>m_bagItems.Count )
            {
                string insertString = item.GetInsertString(m_character.m_character_id);
                m_character.m_db.runCommandSync("insert into bank " + insertString);
                m_bagItems.Add(item);
                return item;
            }
            return null;
        }

        bool UseConsumeOnAquireItem(ItemTemplate itemTemplate, int totalQuantity, bool reportToPlayer)
        {
            bool success = false;


            switch ((PremiumShop.INSTANT_USE_ITEM_ID)itemTemplate.m_item_id)
            {
                case PremiumShop.INSTANT_USE_ITEM_ID.COIN_BAG:
                case PremiumShop.INSTANT_USE_ITEM_ID.COIN_BAG_1:
                case PremiumShop.INSTANT_USE_ITEM_ID.COIN_BAG_2:
                case PremiumShop.INSTANT_USE_ITEM_ID.COIN_BAG_3:
                case PremiumShop.INSTANT_USE_ITEM_ID.COIN_BAG_4:
                case PremiumShop.INSTANT_USE_ITEM_ID.COIN_BAG_5:
                case PremiumShop.INSTANT_USE_ITEM_ID.COIN_BAG_6:
                case PremiumShop.INSTANT_USE_ITEM_ID.COIN_BAG_7:
                case PremiumShop.INSTANT_USE_ITEM_ID.COIN_BAG_8:
                case PremiumShop.INSTANT_USE_ITEM_ID.COIN_BAG_9:
                case PremiumShop.INSTANT_USE_ITEM_ID.COIN_BAG_10:
                case PremiumShop.INSTANT_USE_ITEM_ID.COIN_BAG_11:
                case PremiumShop.INSTANT_USE_ITEM_ID.COIN_BAG_12:
                case PremiumShop.INSTANT_USE_ITEM_ID.COIN_BAG_13:
                    {
                        int coinsAdded = itemTemplate.m_sellprice * totalQuantity;
                        if (reportToPlayer == true)
                        {
							string locText = Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.GAINED_GOLD);
							locText = string.Format(locText, coinsAdded);
							Program.processor.sendSystemMessage(locText, m_character.m_player, true, SYSTEM_MESSAGE_TYPE.POPUP);
						}
                        m_character.updateCoins(coinsAdded);

                        success = true;

                        break;
                    }

                case PremiumShop.INSTANT_USE_ITEM_ID.PLAT_BAG_1:
                case PremiumShop.INSTANT_USE_ITEM_ID.PLAT_BAG_2:
                case PremiumShop.INSTANT_USE_ITEM_ID.PLAT_BAG_3:
                case PremiumShop.INSTANT_USE_ITEM_ID.PLAT_BAG_4:
                case PremiumShop.INSTANT_USE_ITEM_ID.PLAT_BAG_5:
                case PremiumShop.INSTANT_USE_ITEM_ID.PLAT_BAG_6:
                case PremiumShop.INSTANT_USE_ITEM_ID.PLAT_BAG_7:
                    {
                        int platAdded = itemTemplate.m_sellprice * totalQuantity;
                        int oldPlat = m_character.m_player.m_platinum;
                        m_character.m_player.m_platinum += platAdded;
                        int newPlat = m_character.m_player.m_platinum;
                        m_character.m_player.SavePlatinum(0, 0);
                        PremiumShop.SendPlatinumConfirmation(m_character.m_player, 1, "","");
                        string logMsg = "(" + itemTemplate.m_item_id + ")" + itemTemplate.m_item_name + " consumed awarded " + platAdded + " oldPlat=" + oldPlat + " newPlat="+newPlat;
                        Program.processor.m_universalHubDB.runCommandSync("insert into account_journal (account_id,journal_date,user,details) values (" + m_character.m_player.m_account_id + ",'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',' Server : " + Program.m_ServerName + "',\"" + logMsg + "\")");
                        if (reportToPlayer == true)
                        {
							string locText = Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.GAINED_PLATINUM);
							locText = string.Format(locText, platAdded);
							Program.processor.sendSystemMessage(locText, m_character.m_player, true, SYSTEM_MESSAGE_TYPE.POPUP);
						}
                        success = true;
                        break;
                    }
                case PremiumShop.INSTANT_USE_ITEM_ID.CHARACTER_SLOT:
                    {
                        m_character.m_player.m_totalCharacterSlots += totalQuantity;

						if (reportToPlayer == true)
						{
							string locText = "";
							if (totalQuantity > 1)
							{
								locText = Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.GAINED_CHARACTER_SLOTS);
							}
							else
							{
								locText = Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.GAINED_CHARACTER_SLOT);
							}

							locText = string.Format(locText, totalQuantity);
							Program.processor.sendSystemMessage(locText, m_character.m_player, true, SYSTEM_MESSAGE_TYPE.POPUP);
						}
						m_character.m_player.SaveCharacterSlots();
                        success = true;
                        break;
                    }
                default:
                    {
                        m_character.AddPermBuffsToCharacter((PERMENENT_BUFF_ID)itemTemplate.m_item_id, totalQuantity, reportToPlayer);
                        success = true;
                        break;
                    }
            }

            return success;

        }

        //add an item 
        public Item AddNewItemToCharacterInventory(int template_id, int quantity, bool newStack)
        {
            int inventoryID = -1;
            ItemTemplate template = ItemTemplateManager.GetItemForID(template_id);
            if (template.m_autoUse == true)
            {
                bool itemAdded = UseConsumeOnAquireItem(template, quantity,false);
                if(itemAdded==true){
                    //make a pretend item so that it knows that the action was a sucess
                    Item consumedItem = new Item(-1,template_id,quantity,0);
                    return consumedItem;
                }
            }
            if (!newStack && template.m_stackable)
            {
                for (int i = 0; i < m_bagItems.Count; i++)
                {
                    if (m_bagItems[i].m_template != null && m_bagItems[i].m_template.m_item_id == template_id)
                    {
                        m_bagItems[i].m_quantity += quantity;
                        m_character.m_db.runCommandSync("update " + m_inventoryTableName + " set quantity=" + m_bagItems[i].m_quantity + " where inventory_id=" + m_bagItems[i].m_inventory_id);
                        return m_bagItems[i];
                    }
                }
            }
            if (inventoryID < 0)
            {
                Item newItem = createItem(template_id, quantity);
                m_character.m_db.runCommandSync("update " + m_inventoryTableName + " set character_id=" + m_character.m_character_id + ",item_id=" + template_id + ",quantity=" + quantity + ", remaining_charges =" + template.m_maxCharges + ", sort_order="+newItem.m_sortOrder+" where inventory_id=" + newItem.m_inventory_id);
                m_bagItems.Add(newItem);
                return newItem;
            }
            return null;
        }
        internal bool IsEmpty()
        {
            bool isEmpty = true;

            if (m_coins > 0 || m_bagItems.Count > 0)
            {
                isEmpty = false;
            }

            return isEmpty;
        }
        internal void MergeInventory(Inventory otherInvent)
        {
            m_coins += otherInvent.m_coins;
            otherInvent.m_coins = 0;
            m_character.saveCoins(m_character.m_inventory, m_character.m_character_id);
            for (int i = (otherInvent.m_bagItems.Count-1); i >=0 ; i--)
            {
                Item currentItem = otherInvent.m_bagItems[i];
                if (currentItem != null)
                {
                    GetItemFromOtherInventory(currentItem, otherInvent);
                }
            }
            otherInvent.m_bagItems.Clear();
        }
        internal void GetItemFromOtherInventory(Item theItem, Inventory otherPlayerInvent)
        {
            if (theItem == null)
            {
                return;
            }
            //remember it the item was added to a pre existing stack
            bool addedToStack = false;
            
            //check if the item is stackable
            if (theItem.m_template.m_stackable)
            {
                //check if the item is already owned
                int hasStack = checkHasItems(theItem.m_template_id);

                //if already owned then add the items 
                if (hasStack > 0)
                {
                    addedToStack = true;
                    AddNewItemToCharacterInventory(theItem.m_template_id, theItem.m_quantity, false);
                    //delete te original item
                    if (otherPlayerInvent != null)
                    {
                        otherPlayerInvent.DeleteItem(theItem.m_template_id, theItem.m_inventory_id, theItem.m_quantity);
                    }
                }
            }
            // otherwise transfer the items 
            if (addedToStack == false)
            {
                m_bagItems.Add(theItem);
                if (otherPlayerInvent.m_character != m_character)
                {
                    m_character.transferOwnership(theItem, m_character.m_character_id);
                }
            }
            //check it's not belonging to the current character
            if (otherPlayerInvent.m_character != m_character)
            {
                m_character.m_QuestManager.checkIfItemAffectsStage(theItem.m_template_id);
                if (otherPlayerInvent.m_character != null)
                {
                    otherPlayerInvent.m_character.m_QuestManager.checkIfItemAffectsStage(theItem.m_template_id);
                }
            }
        }
        public Item createItem(int templateID, int quantity)
        {
            int inventoryID = Program.processor.getAvailableInventoryID();
            Item newItem = new Item(inventoryID, templateID, quantity,getNextSortOrder());
            return newItem;

        }
        public Item createItem(Item item)
        {
            int inventoryID = Program.processor.getAvailableInventoryID();
            Item newItem = new Item(item);
            newItem.m_inventory_id = inventoryID;
            return newItem;

        }
       
        public Item findBagItemByInventoryID(int inventoryID, int templateID)
        {
            for (int i = 0; i < m_bagItems.Count; i++)
            {
                if (m_bagItems[i].m_inventory_id == inventoryID && m_bagItems[i].m_template_id == templateID)
                {
                    return m_bagItems[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Looks through equipped items and removes given item if present
        /// </summary>
        /// <param name="theItem">item to be removed from equipment</param>
        public void UnequipItem(Item theItem)
        {
            // nothing to unequip
            if (theItem == null)
                return;

            // search through our equiped items and call unequip on matching one
            for (int i = 0; i < m_equipedItems.Length; i++)
            {
                if (theItem == m_equipedItems[i])
                {
                    UnequipItem(theItem.m_template_id, theItem.m_inventory_id, theItem.m_quantity, i);
                }
            }
        }

        internal string UnequipItem(int templateID, int inventoryID, int amount,int slot)
        {
			string reply = Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.ITEM_NOT_FOUND);
			int i = slot;
            
            if (slot >= 0 && slot < m_equipedItems.Length)
            {
               
                if (m_equipedItems[i] != null && m_equipedItems[i].m_inventory_id == inventoryID && m_equipedItems[i].m_template_id == templateID && m_equipedItems[i].m_quantity == amount)
                {
                    Item equipedItem = m_equipedItems[i];
                    if (m_equipedItems[i].m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.BOW)
                    {
                        RemoveArrows();
                        StatusEffect rapidShot = m_character.GetStatusEffectForID(EFFECT_ID.RAPID_SHOT);
                        if (rapidShot != null)
                        {
                            rapidShot.Complete();
							string locText = Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.STATUS_REMOVED);
							string locStatusEffectName = StatusEffectTemplateManager.GetLocaliseStatusEffectName(m_character.m_player, (int)rapidShot.Template.StatusEffectID);
							locText = string.Format(locText, locStatusEffectName);
							Program.processor.sendSystemMessage(locText, m_character.m_player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
						}
                    }
                    bool alreadyInBag = false;
                    for (int j = 0; j < m_bagItems.Count; j++)
                    {
                        Item currentItem = m_bagItems[j];
                        if (currentItem != null && currentItem.m_inventory_id == equipedItem.m_inventory_id)
                        {
                            alreadyInBag=true;
                        }
                    }
                    if (alreadyInBag == false)
                    {
                        Item item1 = m_equipedItems[i];
                        item1.m_sortOrder = getNextSortOrder();
                        m_character.m_db.runCommandSync("update " + m_inventoryTableName + " set sort_order=" + item1.m_sortOrder + " where inventory_id=" + item1.m_inventory_id);

                        m_bagItems.Add(item1);
                    }
                    m_character.m_db.runCommandSync("delete from equipment where inventory_id=" + m_equipedItems[i].m_inventory_id);
                    m_equipedItems[i] = null;
                    m_character.InfoUpdated((Inventory.EQUIP_SLOT)slot);
                    m_character.SetStatsChangeLevel(CombatEntity.STATS_CHANGE_LEVEL.EQUIPMENT_CHECK_REQUIRED);
                    reply = "";

                    if (m_character.ShowFashion == true && slot >= (int)EQUIP_SLOT.SLOT_FASH_HEAD)
                    {
                        VerifyFashion();
                    }
                    equipedItem.Unequipped(m_character);
                }
            }
            if (reply == "")
            {
                ResetEquipmentSetRewards();
            }            

            m_character.AddSkillsFromEquipment(true);
            
            return reply;
        }

        internal string  EquipItem(int templateID, int inventoryID, int amount, int slot)
        {
            //where was the item found
            int equipmentSlot = -1;
			string reply = Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.ITEM_NOT_FOUND);
			//if this item is not nil it means that it can be equiped into the slot
			Item itemToEquip = null;
                        
            //check in the bag            
            for (int i = 0; i < m_bagItems.Count && itemToEquip==null; i++)
            {
                Item item = m_bagItems[i];
                //is the current item what you are looking for
                if (item.m_inventory_id == inventoryID && item.m_template_id == templateID && item.m_quantity == amount)
                {
                    //can it be equipped in this slot                
                    if (item.m_template.CanBeEquippedInSlot(slot))
                    {
                        //can this character equip it

                        if ((m_character.InCombat && m_equipedItems[(int)EQUIP_SLOT.SLOT_MOUNT] != null) &&
                          (item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.BROOM
                        || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.SLEDGE
                        || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.MAGIC_CARPET
                        || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.FISHING_ROD
                        || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_ANGEL_WINGS
                        || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_BAGPIPES
                        || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_BANSHEE_BLADE
                        || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_BATMOUNT
                        || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_BLUNT
                        || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_BOARMOUNT
                        || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_BONE_BIRD
                        || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_BROOM
                        || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_CROW
                        || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_DRAGONSTAFF
                        || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_DRUM
                        || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_EAGLEMOUNT
                        || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_FLUTE
                        || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_HARP
                        || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_HELL_WINGS
                        || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_HORN
                        || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_HORSEMOUNT
                        || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_LUTE
                        || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_SPARROW
                        || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_SPARROWHAWK
                        || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_SPIRITCAPE
                        || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_STAFF_MOUNT
                        || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_TWO_HANDED
                        || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_WAND))
                        {
							return Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.CANNOT_EQUIP_WHILE_MOUNTED);
						}
                        if (item.m_template.checkIfAllowed(this.m_character))
                        {
                            itemToEquip = item;
                            
                        }
                        else
                        {
							return Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.DOES_NOT_MEET_REQUIREMENTS);
						}
                        if (itemToEquip != null)
                        {
                            //if it will unequip any unique items and it's a binding type
                            //then bounce it
                            string uniqueClashStr = UniqueItemClashesString(itemToEquip, m_character.m_player.m_languageIndex);
                            if (uniqueClashStr != "")
                            {
                                if (itemToEquip.m_bound == false && itemToEquip.m_template.m_bindOnEquip == true)
                                {
									string locText = Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.ITEM_CANNOT_EQUIPPED_SAME_TIME);
									locText = string.Format(locText, itemToEquip.m_template.m_loc_item_name[m_character.m_player.m_languageIndex], uniqueClashStr);
									Program.processor.sendSystemMessage(locText, m_character.m_player, false, SYSTEM_MESSAGE_TYPE.POPUP);
									return Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.OTHER_ITEM_NEED_REMOVED);
								}
                            }
                        }
                    }
                   
                    else
                    {
						return Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.NOT_EQUIPABLE);
					}
                }
            }
            //check the equipment
            for (int i = 0; i < m_equipedItems.Count() && itemToEquip == null; i++)
            {
                if (m_equipedItems[i] != null)
                {
                    //is the current item what you are looking for
                    if (m_equipedItems[i].m_inventory_id == inventoryID && m_equipedItems[i].m_template_id == templateID && m_equipedItems[i].m_quantity == amount)
                    {
                        //can it be equipped in this slot
                        Item item = m_equipedItems[i];
                        if (item.m_template.CanBeEquippedInSlot(slot))
                        {
                            //can this character equip it
                            if (item.m_template.checkIfAllowed(this.m_character))
                            {
                                itemToEquip = item;
                                //unequip the item
                                equipmentSlot = i;
                            }
                        }
                    }
                }
            }
            if (itemToEquip != null)
            {
                Item currentEquipedItem = m_equipedItems[slot];

                //remove the item to be equipped from its current equipment slot 
                //if it is already equipped
                if (equipmentSlot > 0)
                {
                    m_bagItems.Add(itemToEquip);

                    m_character.m_db.runCommandSync("delete from equipment where inventory_id=" + itemToEquip.m_inventory_id);
                    m_equipedItems[equipmentSlot] = null;
                    m_character.InfoUpdated((Inventory.EQUIP_SLOT)equipmentSlot);
                }
                //if there is an item in the destination slot 
                if (currentEquipedItem != null)
                {
                    bool removeItem = true;
                    //if the item being equipped was in another slot
                    //check if they can be swapped
                    if (equipmentSlot > 0)
                    {
                        if (currentEquipedItem.m_template.CanBeEquippedInSlot(slot))
                        {
                            //can this character equip it
                            if (currentEquipedItem.m_template.checkIfAllowed(this.m_character))
                            {
                                removeItem = false;
                                m_character.m_db.runCommandSync("replace into equipment (character_id,slot_id,inventory_id) values (" + m_character.m_character_id + "," + equipmentSlot + "," + currentEquipedItem.m_inventory_id + ")");
               
                                m_equipedItems[equipmentSlot]=currentEquipedItem;
                            }

                        }

                    }
                    // remove the item that is currently equipped
                    if (removeItem == true)
                    {
                        if ((itemToEquip.m_template.m_slotNumber == (int)Inventory.EQUIP_SLOT.SLOT_WEAPON) && (itemToEquip.m_template.m_subtype != ItemTemplate.ITEM_SUB_TYPE.BOW))
                        {
                            RemoveArrows();
                            StatusEffect rapidShot = m_character.GetStatusEffectForID(EFFECT_ID.RAPID_SHOT);
                            if (rapidShot != null)
                            {
                                rapidShot.Complete();
								string locText = Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.STATUS_REMOVED);
								string locStatusEffectName = StatusEffectTemplateManager.GetLocaliseStatusEffectName(m_character.m_player, (int)rapidShot.Template.StatusEffectID);
								locText = string.Format(locText, locStatusEffectName);
								Program.processor.sendSystemMessage(locText, m_character.m_player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
							}
                        }

                        m_bagItems.Add(currentEquipedItem);
                        m_character.m_db.runCommandSync("delete from equipment where inventory_id=" + currentEquipedItem.m_inventory_id);
                        currentEquipedItem.Unequipped(m_character);
                        currentEquipedItem = null;
                        
                    }
                }
                //remove any items that cannot be equipped at the same time as the newly equipped item
                UnequipIncompatableItems(itemToEquip, slot);
                bool canStillBeEquipped = itemToEquip.m_template.checkIfAllowed(this.m_character);
                if (canStillBeEquipped)
                {
                    //bind the item if required
                    m_equipedItems[slot] = itemToEquip;
                    itemToEquip.Equipped(m_character);
                    bindItem(itemToEquip);
                    m_character.m_db.runCommandSync("replace into equipment (character_id,slot_id,inventory_id) values (" + m_character.m_character_id + "," + slot + "," + itemToEquip.m_inventory_id + ")");
                    //birdman achievement
                    switch (itemToEquip.m_template_id)
                    {
                        case 15276:
                        case 15277:
                            {
                                m_character.increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.BIRDMAN, 1);
                                break;
                            }
                    }
                    m_bagItems.Remove(itemToEquip);
                    reply = "";
                }
                else
                {
					reply = Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.DOES_NOT_MEET_REQUIREMENTS);
				}
                //the change will need to be sent to others
                m_character.InfoUpdated((Inventory.EQUIP_SLOT)slot);
            }
            //if nothing has gone wrong prepare the sets to be rebuilt
            if (reply == "")
            {
                ResetEquipmentSetRewards();
            }
            m_character.AddSkillsFromEquipment(true);
            return reply;
        }

        public void bindItem(Item itemToEquip)
        {
            if (itemToEquip.m_bound == false && itemToEquip.m_template.m_bindOnEquip == true)
            {
                itemToEquip.m_bound = true;
                m_character.m_db.runCommandSync("update " + m_inventoryTableName + " set bound=" + itemToEquip.m_bound + " where inventory_id=" + itemToEquip.m_inventory_id);

            }
        }
        void RemoveArrows()
        {
            Item offhand = m_equipedItems[(int)Inventory.EQUIP_SLOT.SLOT_OFFHAND];
            if ((offhand != null)&&(offhand.m_template.m_subtype== ItemTemplate.ITEM_SUB_TYPE.ARROW))
            {
                UnequipItem(offhand.m_template_id, offhand.m_inventory_id, offhand.m_quantity, (int)Inventory.EQUIP_SLOT.SLOT_OFFHAND);
            }
        }
        string UniqueItemClashesString(Item item, int languageIndex)
        {
            string clashString = "";
            ItemTemplate itemTemplate = item.m_template;
            int uniqueID = itemTemplate.m_uniqueID;
            for (int i = 0; i < m_equipedItems.Count(); i++)
            {
                Item currentItem = m_equipedItems[i];

                //if so unequip it
                if (currentItem != null)
                {           
                    if (uniqueID > 0 && uniqueID == currentItem.m_template.m_uniqueID)
                    {
                        if(clashString!= "")
                        {
                            clashString += ", ";
                        }
                        clashString += currentItem.m_template.m_loc_item_name[languageIndex];
                    }
                }
            }
            return clashString;
        }

        /// <summary>
        /// Remove blocked slot items, offhands, and mount incompatibilites
        /// </summary>
        /// <param name="item"></param>
        /// <param name="slot"></param>
        void UnequipIncompatableItems(Item item, int slot)
        {

            ItemTemplate itemTemplate = item.m_template;
            int uniqueID = itemTemplate.m_uniqueID;

            //Go through the incompatable slot list
            for (int i = 0; i < itemTemplate.BlockedSlots.Count(); i++)
            {
                //check each slot
                EQUIP_SLOT currentBlockSlot = itemTemplate.BlockedSlots[i];
                //is there anything there
                Item itemInSlot = m_equipedItems[(int)currentBlockSlot];

                //if so unequip it
                if (itemInSlot != null)
                {
                    UnequipItem(itemInSlot.m_template_id, itemInSlot.m_inventory_id, itemInSlot.m_quantity, (int)currentBlockSlot);
                }

                //rings will need to be more intelligent if they do this

            }

            for (int i = 0; i < m_equipedItems.Count(); i++)
            {
                Item currentItem = m_equipedItems[i];

                //if so unequip it
                if (currentItem != null)
                {
                    bool clashesWithUniqueID = uniqueID > 0 && uniqueID == currentItem.m_template.m_uniqueID;
                    bool hasBlockedSlot = currentItem.m_template.HasBlockedSlot((EQUIP_SLOT)slot);

                    if (hasBlockedSlot == true || clashesWithUniqueID == true)
                    {
                        UnequipItem(currentItem.m_template_id, currentItem.m_inventory_id, currentItem.m_quantity, i);
                    }
                }

            }

            //if it's a two handed weapon remove what is in the misc slot			
            if (item.m_template.m_slotNumber == (int)EQUIP_SLOT.SLOT_WEAPON)
            {
                Item offhand = m_equipedItems[(int)EQUIP_SLOT.SLOT_OFFHAND];
                ItemTemplate.WEAPON_EQUIP_TYPE equipType = item.m_template.GetWeaponEquipmentType();
                if (offhand != null)
                {
                    if (equipType == ItemTemplate.WEAPON_EQUIP_TYPE.TWO_HANDED)
                    {
                        //UnequipItem((int)EQUIP_SLOT.SLOT_OFFHAND);
                        UnequipItem(offhand.m_template_id, offhand.m_inventory_id, offhand.m_quantity, (int)EQUIP_SLOT.SLOT_OFFHAND);
                    }
                    else if (equipType == ItemTemplate.WEAPON_EQUIP_TYPE.BOW)
                    {
                        if (offhand.m_template.m_subtype != ItemTemplate.ITEM_SUB_TYPE.ARROW)
                        {
                            UnequipItem(offhand.m_template_id, offhand.m_inventory_id, offhand.m_quantity, (int)EQUIP_SLOT.SLOT_OFFHAND);
                            //UnequipItem((int)EQUIP_SLOT.SLOT_OFFHAND);
                        }
                    }
                }
            }

            //if it's an offhand unequip a 2 handed weapon
            if (item.m_template.m_slotNumber == (int)EQUIP_SLOT.SLOT_OFFHAND)
            {
                Item weapon = m_equipedItems[(int)EQUIP_SLOT.SLOT_WEAPON];
                if (weapon != null)
                {
                    ItemTemplate.WEAPON_EQUIP_TYPE equipType = weapon.m_template.GetWeaponEquipmentType();

                    if (equipType == ItemTemplate.WEAPON_EQUIP_TYPE.TWO_HANDED)
                    {
                        UnequipItem(weapon.m_template_id, weapon.m_inventory_id, weapon.m_quantity, (int)EQUIP_SLOT.SLOT_WEAPON);
                        //UnequipItem((int)EQUIP_SLOT.SLOT_WEAPON);
                    }
                    else if (equipType == ItemTemplate.WEAPON_EQUIP_TYPE.BOW)
                    {
                        if (item.m_template.m_subtype != ItemTemplate.ITEM_SUB_TYPE.ARROW)
                        {
                            UnequipItem(weapon.m_template_id, weapon.m_inventory_id, weapon.m_quantity, (int)EQUIP_SLOT.SLOT_WEAPON);
                            // UnequipItem((int)EQUIP_SLOT.SLOT_WEAPON);
                        }
                    }
                }
            }

            //if it's a mounts, unequip certain items
            if (item.m_template.m_slotNumber == (int)EQUIP_SLOT.SLOT_MOUNT)
            {
                foreach (var equipptedItem in m_equipedItems)
                {
                    //nothing here
                    if (equipptedItem == null)
                        continue;

                    //incompatibilites
                    if (equipptedItem.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.BROOM
                        || equipptedItem.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.SLEDGE
                        || equipptedItem.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.MAGIC_CARPET
                        || equipptedItem.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.FISHING_ROD
                        || equipptedItem.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_ANGEL_WINGS
                        || equipptedItem.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_BAGPIPES
                        || equipptedItem.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_BANSHEE_BLADE
                        || equipptedItem.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_BATMOUNT
                        || equipptedItem.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_BLUNT
                        || equipptedItem.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_BOARMOUNT
                        || equipptedItem.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_BONE_BIRD
                        || equipptedItem.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_BROOM
                        || equipptedItem.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_CROW
                        || equipptedItem.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_DRAGONSTAFF
                        || equipptedItem.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_DRUM
                        || equipptedItem.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_EAGLEMOUNT
                        || equipptedItem.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_FLUTE
                        || equipptedItem.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_HARP
                        || equipptedItem.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_HELL_WINGS
                        || equipptedItem.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_HORN
                        || equipptedItem.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_HORSEMOUNT
                        || equipptedItem.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_LUTE
                        || equipptedItem.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_SPARROW
                        || equipptedItem.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_SPARROWHAWK
                        || equipptedItem.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_SPIRITCAPE
                        || equipptedItem.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_STAFF_MOUNT
                        || equipptedItem.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_TWO_HANDED
                        || equipptedItem.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_WAND
                        )
                    {
                        Program.Display("mount incompatible with." + equipptedItem.m_template.m_item_name + " unequiping");
                        UnequipItem(equipptedItem);
                    }

                }
            }

            //Or unequip the mount, if you're equipping something that can't be used with one.
            if (m_equipedItems[(int)EQUIP_SLOT.SLOT_MOUNT]!=null && (
                item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.BROOM
                || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.SLEDGE
                || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.MAGIC_CARPET
                || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.FISHING_ROD
                || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_ANGEL_WINGS
                || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_BAGPIPES
                || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_BANSHEE_BLADE
                || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_BATMOUNT
                || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_BLUNT
                || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_BOARMOUNT
                || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_BONE_BIRD
                || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_BROOM
                || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_CROW
                || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_DRAGONSTAFF
                || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_DRUM
                || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_EAGLEMOUNT
                || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_FLUTE
                || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_HARP
                || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_HELL_WINGS
                || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_HORN
                || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_HORSEMOUNT
                || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_LUTE
                || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_SPARROW
                || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_SPARROWHAWK
                || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_SPIRITCAPE
                || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_STAFF_MOUNT
                || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_TWO_HANDED
                || item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.NOVELTY_WAND
                ))
            {
                //Program.Display("mount incompatible with." + equipptedItem.m_template.m_item_name + " unequiping");
                
                if(!m_character.InCombat)
                UnequipItem(m_equipedItems[(int)EQUIP_SLOT.SLOT_MOUNT]);
            }
        }

        internal bool ConsumeCharge(Item item)
        {
            ItemTemplate itemTemplate = item.m_template;
            bool chargeConsumed = true;
            int itemQuantity = item.m_quantity;
            bool useCharges = false;
            //but it could be the consumable
            if (itemTemplate.m_maxCharges > 0 && itemTemplate.m_stackable == false)
            {
                useCharges = true;
                itemQuantity = item.m_remainingCharges;
            }
            int amount = 1;
            //does it pass the conditions
                    //not deleting more than owned
            if (itemQuantity <= amount)
            {
                if (useCharges == true)
                {
                    chargeConsumed = true;
                    item.m_remainingCharges = 0;
                    //you can't have it equipped with no charge
                    
                    if (itemTemplate.m_destroyOnNoCharge == true)
                    {
                        item.Destroyed = true;

                        UnequipItem(item);
                        string comment = "Used";
                        if (item.m_bound)
                        {
                            comment += " [bound]";
                        }
                        Program.processor.updateShopHistory(-1, -1, item.m_inventory_id, item.m_template_id, -item.m_quantity, 0, (int)m_character.m_character_id, comment);
                        DeleteItem(item.m_template_id, item.m_inventory_id, item.m_quantity);
                        if (Program.m_LogAnalytics && itemTemplate.m_subtype != ItemTemplate.ITEM_SUB_TYPE.ARROW)
                        {
                            AnalyticsMain logAnalytics = new AnalyticsMain(true);
                            logAnalytics.itemActioned(m_character.m_player, item.m_template_id.ToString(), item.m_template.m_item_name, item.m_template.m_subtype.ToString(), "USED");
                        }

                    }
                    else
                    {
                        m_character.m_db.runCommandSync("update " + m_inventoryTableName + " set remaining_charges=" + item.m_remainingCharges + " where inventory_id=" + item.m_inventory_id);

                    }
                    m_character.SetStatsChangeLevel(CombatEntity.STATS_CHANGE_LEVEL.EQUIPMENT_CHECK_REQUIRED);//m_character.m_statsUpdated=true;
                    return chargeConsumed;
                }
            }
            else if (itemQuantity > amount)
            {
                //which variable does it remove from
                if (useCharges == true)
                {
                    chargeConsumed = true;
                    item.m_remainingCharges -= amount;
                    //update the item and database
                    m_character.m_db.runCommandSync("update " + m_inventoryTableName + " set remaining_charges=" + item.m_remainingCharges + " where inventory_id=" + item.m_inventory_id);

                }
               
            }
            return chargeConsumed;
        }
        internal string ConsumeItem(int templateID, int inventoryID, int amount)
        {
			string reply = Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.ITEM_NOT_FOUND);

			for (int i = 0; i < m_equipedItems.Length; i++)
            {
                if (m_equipedItems[i] != null && m_equipedItems[i].m_inventory_id == inventoryID && m_equipedItems[i].m_template_id == templateID)
                {
                    Item item = m_equipedItems[i];
                    ItemTemplate itemTemplate = item.m_template;
                    //normally it is the number of items
                    int itemQuantity = item.m_quantity;
                    bool useCharges=false;
                    //but it could be the consumable
                    if(itemTemplate.m_maxCharges>0 && itemTemplate.m_stackable==false)
                    {
                        useCharges = true;
                        itemQuantity = item.m_remainingCharges;
                    }
                    //if it's an infinate don't consume it
                    else if (itemTemplate.m_maxCharges < 0)
                    {
                        return "";
                    }

                    //does it pass the conditions
                    //not deleting more than owned
                    if (itemQuantity <= amount)
                    {
                        if (useCharges == true)
                        {
                            item.m_remainingCharges = 0;
                            //you can't have it equipped with no charge
                            UnequipItem(item);
                            if (itemTemplate.m_destroyOnNoCharge == true)
                            {
                                string comment = "Used";
                                if (item.m_bound)
                                {
                                    comment += " [bound]";
                                }
                                Program.processor.updateShopHistory(-1, -1, item.m_inventory_id, item.m_template_id, -item.m_quantity, 0, (int)m_character.m_character_id, comment);
                     
                                DeleteItem(item.m_template_id, item.m_inventory_id, item.m_quantity);

                            }
                            
                            m_character.SetStatsChangeLevel(CombatEntity.STATS_CHANGE_LEVEL.EQUIPMENT_CHECK_REQUIRED);//m_character.m_statsUpdated=true;
                            if (Program.m_LogAnalytics && itemTemplate.m_subtype!= ItemTemplate.ITEM_SUB_TYPE.ARROW)
                            {
                                AnalyticsMain logAnalytics = new AnalyticsMain(true);
                                logAnalytics.itemActioned(m_character.m_player, item.m_template_id.ToString(), item.m_template.m_item_name, item.m_template.m_subtype.ToString(), "USED");
                            }

                            return "";
                        }
                        else
                        {
                            m_character.m_db.runCommandSync("delete from equipment where inventory_id=" + item.m_inventory_id);
                            string comment = "Used";
                            if (item.m_bound)
                            {
                                comment += " [bound]";
                            }
                            Program.processor.updateShopHistory(-1, -1, item.m_inventory_id, item.m_template_id, -item.m_quantity, 0, (int)m_character.m_character_id, comment);
                     
                            m_character.m_db.runCommandSync("delete from " + m_inventoryTableName + " where inventory_id=" + item.m_inventory_id);
                            m_equipedItems[i] = null;
                            m_character.SetStatsChangeLevel(CombatEntity.STATS_CHANGE_LEVEL.EQUIPMENT_CHECK_REQUIRED);//m_character.m_statsUpdated=true;

                            if (Program.m_LogAnalytics && itemTemplate.m_subtype != ItemTemplate.ITEM_SUB_TYPE.ARROW)
                            {
                                AnalyticsMain logAnalytics = new AnalyticsMain(true);
                                logAnalytics.itemActioned(m_character.m_player, item.m_template_id.ToString(), item.m_template.m_item_name, item.m_template.m_subtype.ToString(), "USED");
                            }
                            return "";
                        }
                    }
                    else if (itemQuantity > amount)
                    {
                        //which variable does it remove from
                        if (useCharges == true)
                        {
                            item.m_remainingCharges -= amount;
                            //update the item and database
                            m_character.m_db.runCommandSync("update " + m_inventoryTableName + " set remaining_charges=" + item.m_remainingCharges + " where inventory_id=" + item.m_inventory_id);

                        }
                        else
                        {
                            item.m_quantity -= amount;
                            //update the item and database
                            string comment = "Used";
                            if (item.m_bound)
                            {
                                comment += " [bound]";
                            }
                            Program.processor.updateShopHistory(-1, -1, item.m_inventory_id, item.m_template_id, -amount, 0, (int)m_character.m_character_id, comment);
                     
                            m_character.m_db.runCommandSync("update " + m_inventoryTableName + " set quantity=" + item.m_quantity + " where inventory_id=" + item.m_inventory_id);
                            if (Program.m_LogAnalytics && itemTemplate.m_subtype != ItemTemplate.ITEM_SUB_TYPE.ARROW)
                            {
                                AnalyticsMain logAnalytics = new AnalyticsMain(true);
                                logAnalytics.itemActioned(m_character.m_player, item.m_template_id.ToString(), item.m_template.m_item_name, item.m_template.m_subtype.ToString(), "USED");
                            }

                        }
                    }
                }
            }

            for (int i = 0; i < m_bagItems.Count; i++)
            {
                if (m_bagItems[i].m_inventory_id == inventoryID && m_bagItems[i].m_template_id == templateID)
                {
                    //found item
                    Item item = m_bagItems[i];
                    ItemTemplate itemTemplate = item.m_template;
                    //normally it is the number of items
                    int itemQuantity = item.m_quantity;
                    bool useCharges = false;
                    //but it could be the consumable
                    if (itemTemplate.m_maxCharges > 0 && itemTemplate.m_stackable == false)
                    {
                        useCharges = true;
                        itemQuantity = item.m_remainingCharges;
                    }
                    //if it's an infinate don't consume it
                    else if (itemTemplate.m_maxCharges < 0)
                    {
                        return "";
                    }

                    //does it pass the conditions
                    //not deleting more than owned
                    if (itemQuantity <= amount)
                    {
                        if (useCharges == true)
                        {
                            item.m_remainingCharges = 0;
                            if (itemTemplate.m_destroyOnNoCharge == true)
                            {
                                string comment = "Used";
                                if (item.m_bound)
                                {
                                    comment += " [bound]";
                                }
                                Program.processor.updateShopHistory(-1, -1, item.m_inventory_id, item.m_template_id, -item.m_quantity, 0, (int)m_character.m_character_id, comment);
                     
                                m_character.m_db.runCommandSync("delete from " + m_inventoryTableName + " where inventory_id=" + item.m_inventory_id);

                                m_bagItems.Remove(item);
                                if (Program.m_LogAnalytics && itemTemplate.m_subtype != ItemTemplate.ITEM_SUB_TYPE.ARROW)
                                {
                                    AnalyticsMain logAnalytics = new AnalyticsMain(true);
                                    logAnalytics.itemActioned(m_character.m_player, item.m_template_id.ToString(), item.m_template.m_item_name, item.m_template.m_subtype.ToString(), "USED");
                                }

                            }
                            else
                            {
                                m_character.m_db.runCommandSync("update " + m_inventoryTableName + " set remaining_charges=" + item.m_remainingCharges + " where inventory_id=" + item.m_inventory_id);

                            }
                            return "";
                        }
                        else
                        {
                            string comment = "Used";
                            if (item.m_bound)
                            {
                                comment += " [bound]";
                            }
                            Program.processor.updateShopHistory(-1, -1, item.m_inventory_id, item.m_template_id, -item.m_quantity, 0, (int)m_character.m_character_id, comment);
                     
                            m_character.m_db.runCommandSync("delete from " + m_inventoryTableName + " where inventory_id=" + item.m_inventory_id);
                            m_bagItems.Remove(item);
                            if (Program.m_LogAnalytics && itemTemplate.m_subtype != ItemTemplate.ITEM_SUB_TYPE.ARROW)
                            {
                                AnalyticsMain logAnalytics = new AnalyticsMain(true);
                                logAnalytics.itemActioned(m_character.m_player, item.m_template_id.ToString(), item.m_template.m_item_name, item.m_template.m_subtype.ToString(), "USED");
                            }

                            return "";
                        }
                    }
                    else if (itemQuantity > amount)
                    {
                        //which variable does it remove from
                        if (useCharges == true)
                        {
                            item.m_remainingCharges -= amount;
                            //update the item and database
                            m_character.m_db.runCommandSync("update " + m_inventoryTableName + " set remaining_charges=" + item.m_remainingCharges + " where inventory_id=" + item.m_inventory_id);

                        }
                        else
                        {
                            item.m_quantity -= amount;
                            //update the item and database
                            string comment = "Used";
                            if (item.m_bound)
                            {
                                comment += " [bound]";
                            }
                            Program.processor.updateShopHistory(-1, -1, item.m_inventory_id, item.m_template_id, -amount, 0, (int)m_character.m_character_id, comment);
                     
                            m_character.m_db.runCommandSync("update " + m_inventoryTableName + " set quantity=" + item.m_quantity + " where inventory_id=" + item.m_inventory_id);
                            if (Program.m_LogAnalytics && itemTemplate.m_subtype != ItemTemplate.ITEM_SUB_TYPE.ARROW)
                            {
                                AnalyticsMain logAnalytics = new AnalyticsMain(true);
                                logAnalytics.itemActioned(m_character.m_player, item.m_template_id.ToString(), item.m_template.m_item_name, item.m_template.m_subtype.ToString(), "USED");
                            }

                        }
                    }
                    else
                    {
						return Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.DELETED_MORE_THAN_EQUIPED);
					}
                }



            }
            return reply;
        }
        internal string DeleteItem(int templateID, int inventoryID, int amount)
        {
			string reply = Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.ITEM_NOT_FOUND);

			for (int i = 0; i < m_equipedItems.Length; i++)
            {
                if (m_equipedItems[i] != null && m_equipedItems[i].m_inventory_id == inventoryID && m_equipedItems[i].m_template_id == templateID)
                {
                    Item item = m_equipedItems[i];
                    
                    //item found
                    if (item.m_quantity == amount)// deleting the whole stack
                    {
                        m_character.m_db.runCommandSync("delete from equipment where inventory_id=" + item.m_inventory_id);
                        m_character.m_db.runCommandSync("delete from " + m_inventoryTableName + " where inventory_id=" + item.m_inventory_id);
                        item.Destroyed = true;
                        m_equipedItems[i] = null;
                        
                        m_character.InfoUpdated((EQUIP_SLOT)i);
                        if (item.m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.BOW)
                        {
                            RemoveArrows();
                            StatusEffect rapidShot = m_character.GetStatusEffectForID(EFFECT_ID.RAPID_SHOT);
                            if (rapidShot != null)
                            {
                                rapidShot.Complete();
								string locText = Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.STATUS_REMOVED);
								string locStatusEffectName = StatusEffectTemplateManager.GetLocaliseStatusEffectName(m_character.m_player, (int)rapidShot.Template.StatusEffectID);
								locText = string.Format(locText, locStatusEffectName);
								Program.processor.sendSystemMessage(locText, m_character.m_player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
							}
                        }

                        m_character.SetStatsChangeLevel(CombatEntity.STATS_CHANGE_LEVEL.EQUIPMENT_CHECK_REQUIRED);//m_character.m_statsUpdated=true;

                        return "";
                    }
                    else if (item.m_quantity > amount)
                    {
                        item.m_quantity -= amount;
                        m_character.m_db.runCommandSync("update " + m_inventoryTableName + " set quantity=" + item.m_quantity + " where inventory_id=" + item.m_inventory_id);
                        return "";
                    }
                    else
                    {
						return Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.DELETED_MORE_THAN_EQUIPED);
					}
                    

                }
            }

            for (int i = 0; i < m_bagItems.Count; i++)
            {
                if (m_bagItems[i].m_inventory_id == inventoryID && m_bagItems[i].m_template_id == templateID)
                {
                    //found item
                    Item item = m_bagItems[i];
                    if (item.m_quantity == amount)//deleting the whole stack
                    {
                        m_character.m_db.runCommandSync("delete from " + m_inventoryTableName + " where inventory_id=" + item.m_inventory_id);
                        item.Destroyed = true;
                        m_bagItems.Remove(item);

                        return "";
                    }
                    else if (item.m_quantity > amount)
                    {
                        item.m_quantity -= amount;
                        m_character.m_db.runCommandSync("update " + m_inventoryTableName + " set quantity=" + item.m_quantity + " where inventory_id=" + item.m_inventory_id);
                        return "";
                    }
                    else
                    {
						return Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.DELETED_MORE_THAN_INVENTORY);
					}
                }
            }
            return reply;
        }




        internal string buyItem(Shop shop, int templateID, int quantity)
        {
            if (quantity < 0)
            {
				return Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.ILLEGAL_QUANTITY);
			}

            int cost = shop.buyItem(templateID, quantity, m_coins);
            if (cost >= 0)
            {
                Item newItem = AddNewItemToCharacterInventory(templateID, quantity, false);
                if (newItem!=null)
                {
                    int newid = newItem.m_inventory_id;
                    int innitialCoins = m_coins;
                    m_coins -= cost;
                    if (cost >= 5000)
                    {
                        m_character.increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.HIGH_ROLLER, 1);
                    }
                    m_character.m_db.runCommandSync("update character_details set coins=" + m_coins + " where character_id=" + m_character.m_character_id);
                    
                    Program.processor.updateShopHistory(m_character.m_zone.m_zone_id,shop.m_shop_id, newid ,templateID ,quantity,cost,(int)m_character.m_character_id,"Bought");
                    //Program.Display(m_character.m_name + " brought " + quantity + " of " + ItemTemplateManager.GetItemForID(templateID) + " with item id " + newid);
                    ItemTemplate template = ItemTemplateManager.GetItemForID(templateID);
                    if (template != null)
                    {
                        Program.Display(m_character.m_name + " brought " + quantity + " of " + template.m_item_name + " template ID:" + templateID + " inventory ID:" + newid + "|Innitial coins:" + innitialCoins + "|Final Coins:" + m_coins);
                        NetOutgoingMessage msg = Program.Server.CreateMessage();
                        msg.WriteVariableUInt32((uint)NetworkCommandType.SimpleMessageForThePlayer);

                        if (quantity > 1)
                        {
                            if (!template.m_item_name.EndsWith("s"))
                            {
                                string locText = Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.PURCHASED_ITEMS);
                                locText = string.Format(locText, quantity, template.m_loc_item_name[m_character.m_player.m_languageIndex], cost);
                                msg.Write(locText);
                            }
                            else
                            {
                                string locText = Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.PURCHASED_ITEMS_NO_S);
                                locText = string.Format(locText, quantity, template.m_loc_item_name[m_character.m_player.m_languageIndex], cost);
                                msg.Write(locText);
                            }
                            
						}
                        else
                        {
							string locText = Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.PURCHASED_ITEM);
							locText = string.Format(locText, template.m_loc_item_name[m_character.m_player.m_languageIndex], cost);
							msg.Write(locText);
						}
                        

                        Program.processor.SendMessage(msg, m_character.m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.SimpleMessageForThePlayer);
                    }
                    else
                    {
                        Program.Display(m_character.m_name + " brought " + quantity + " of unknown template ID:" + templateID + " inventory ID:" + newid + "|Innitial coins:" + innitialCoins + "|Final Coins:" + m_coins);
                        
                       
                    }

                    if (Program.m_LogAnalytics)
                    {
                        AnalyticsMain logAnalytics = new AnalyticsMain(true);
                        logAnalytics.inGameShopPurchase(m_character.m_player, cost, template.m_item_name, /*itemTypeReceived*/ template.m_subtype.ToString(), quantity);
                    }
                    
                    return "";
                }
                else
					return Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.COULD_NOT_ADD_ITEM);
            }
            else if (cost == -1)
            {
				return Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.CANNOT_AFFORD_ITEM);
			}
            else if (cost == -2)
            {
				return Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.INSUFFICIENT_STOCK);
			}
            else if (cost == -3)
            {
				return Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.NO_ITEM_FOR_SALE);
			}
			return Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.UNKNOWN_ERROR);
		}


        internal string sellItem(Shop shop, int templateID, int inventory_id, int quantity)
        {
            if (quantity < 0)
            {
				return Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.ILLEGAL_QUANTITY);
			}
            Item theItem = findBagItemByInventoryID(inventory_id, templateID);
            if (theItem != null && ((theItem.m_template != null && theItem.m_template.m_noTrade == true)))
            {
				return Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.CANNOT_SELL_THIS_ITEM);
			}
            int numCharges = theItem.m_remainingCharges;
            int gain = shop.sellItem(templateID, quantity);
            if (gain > 0)
            {
                string errorString = DeleteItem(templateID, inventory_id, quantity);
                if (errorString != "")
                {
                    return errorString;
                }
                else
                {
                    string comment = "Sold";
                    if (theItem.m_bound)
                    {
                        comment += " [bound]";
                    }
                    if (theItem.m_template.m_maxCharges > 0)
                    {
                        comment += " Charges Remaining : " + numCharges;
                    }

                    Program.processor.updateShopHistory(m_character.m_zone.m_zone_id, shop.m_shop_id, inventory_id, templateID, -quantity, -gain, (int)m_character.m_character_id, comment);
                    int innitialCoins = m_coins;
                    m_coins += gain;
                    m_character.m_db.runCommandSync("update character_details set coins=" + m_coins + " where character_id=" + m_character.m_character_id);
                    ItemTemplate template = ItemTemplateManager.GetItemForID(templateID);
                    if (template != null)
                    {
                        Program.Display(m_character.m_name + " sold " + quantity + " of " + template.m_item_name + " template ID:" + templateID + " inventory ID:" + inventory_id + "|Initial coins:" + innitialCoins + "|Final Coins:" + m_coins);
                    }
                    else
                    {
                        Program.Display(m_character.m_name + " sold " + quantity + " of unknown template ID:" + templateID + " inventory ID:" + inventory_id + "|Initial coins:" + innitialCoins + "|Final Coins:" + m_coins);
                    }
                    if (Program.m_LogAnalytics)
                    {
                        AnalyticsMain logAnalytics = new AnalyticsMain(true);
                        logAnalytics.inGameShopSale(m_character.m_player, gain, theItem.m_template.m_item_name, /*itemTypeReceived*/ theItem.m_template.m_subtype.ToString(), quantity);
                    }

                    return "";
                }
            }
            else if (gain == 0)
            {
				string locText = Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.SHOP_NOT_PURCHASING);
				locText = string.Format(locText, theItem.m_template.m_loc_item_name[m_character.m_player.m_languageIndex]);
				return locText;
			}
			return Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.UNKNOWN_ERROR);
		}

        internal void addLoot(List<LootDetails> details, ServerControlledEntity mob)
        {
            for (int i = 0; i < details.Count; i++)
            {
                Item item=AddNewItemToCharacterInventory(details[i].m_templateID, details[i].m_quantity, false);
                Program.processor.updateShopHistory(-1, -1, item.m_inventory_id, item.m_template_id, details[i].m_quantity, 0, (int)m_character.m_character_id, "Looted");
                Program.Display(m_character.Name + " looted " + details[i].m_quantity + " of " + item.m_template.m_item_name + " inv_id=" + item.m_inventory_id + " from mob " + mob.Name);

            }
        }

        internal void addLoot(List<LootDetails> details, EntitySkill usedSkill)
        {
            for (int i = 0; i < details.Count; i++)
            {
                
                Item item = AddNewItemToCharacterInventory(details[i].m_templateID, details[i].m_quantity, false);
                m_character.m_QuestManager.checkIfItemAffectsStage(item.m_template_id);
                Program.processor.updateShopHistory(-1, -1, item.m_inventory_id, item.m_template_id, details[i].m_quantity, 0, (int)m_character.m_character_id, "Skill_Loot");
                Program.Display(m_character.Name + " looted " + details[i].m_quantity + " of " + item.m_template.m_item_name + " inv_id=" + item.m_inventory_id + " from skill " + usedSkill.Template.SkillName+ " level:"+usedSkill.SkillLevel);

            }
        }

        internal int checkHasItems(int itemTemplateID)
        {
            int count = 0;
            for (int i = 0; i < m_bagItems.Count; i++)
            {
                if (m_bagItems[i].m_template_id == itemTemplateID)
                {
                    count += m_bagItems[i].m_quantity;
                }
            }
            for (int i = 0; i < m_equipedItems.Length; i++)
            {
                if (m_equipedItems[i] != null && m_equipedItems[i].m_template_id == itemTemplateID)
                {
                    count += m_equipedItems[i].m_quantity;
                }
            }
            return count;
        }
        /// <summary>
        /// returns the first item with the requested template id
        /// </summary>
        /// <param name="itemTemplateID">the template ID to search for</param>
        /// <param name="includeEquiped">is equipment included in the search</param>
        /// <returns></returns>
        internal Item GetItemFromTemplateID(int itemTemplateID, bool includeEquiped)
        {
            for (int i = 0; i < m_bagItems.Count; i++)
            {
                if (m_bagItems[i].m_template_id == itemTemplateID)
                {
                    return m_bagItems[i];
                }
            }
            if (includeEquiped)
            {
                for (int i = 0; i < m_equipedItems.Length; i++)
                {
                    if (m_equipedItems[i] != null && m_equipedItems[i].m_template_id == itemTemplateID)
                    {
                        return m_equipedItems[i];
                    }
                }
            }
            return null;
        }
        internal Item GetItemFromInventoryID(int inventoryID, bool includeEquiped)
        {
            for (int i = 0; i < m_bagItems.Count; i++)
            {
                if (m_bagItems[i].m_inventory_id == inventoryID)
                {
                    return m_bagItems[i];
                }
            }

            if (includeEquiped)
            {
                for (int i = 0; i < m_equipedItems.Length; i++)
                {
                    if (m_equipedItems[i] != null && m_equipedItems[i].m_inventory_id == inventoryID)
                    {
                        return m_equipedItems[i];
                    }
                }
            }
            return null;
        }
        internal int GetItemCount(int itemTemplateID)
        {
            int count = 0;
            for (int i = 0; i < m_bagItems.Count; i++)
            {
                if (m_bagItems[i].m_template_id == itemTemplateID)
                {
                    count += m_bagItems[i].m_quantity;
                }
            }

            for (int i = 0; i < m_equipedItems.Length; i++)
            {
                if (m_equipedItems[i] != null && m_equipedItems[i].m_template_id == itemTemplateID)
                {
                    count+= m_equipedItems[i].m_quantity;
                }
            }
            
            return count;
        }
        internal int GetItemCountForTravel()
        {
            int itemCount =0;
            itemCount = m_bagItems.Count;
            return itemCount;
        }
        internal bool removeQuestItems(int quest_id,int stage_id,int itemTemplateID, int itemQuantity)
        {
            int remaining = itemQuantity;
            string reportStr = m_character.m_name + " removing ";
            for (int i = m_bagItems.Count - 1; i > -1; i--)
            {
                var bagItem = m_bagItems[i];
                if (bagItem.m_template_id == itemTemplateID)
                {
                    if (remaining >= bagItem.m_quantity)
                    {
                        reportStr += bagItem.m_quantity + " of " + bagItem.m_template.m_item_name + " template ID:" + bagItem.m_template_id + " inventory ID:" + bagItem.m_inventory_id + " from inventory for quest " + quest_id + "," + stage_id + ".";
                        int removed = bagItem.m_quantity;
                        string comment = "Quest " + quest_id + "," + stage_id;
                        if (bagItem.m_bound)
                        {
                            comment += " [bound]";
                        }

                        Program.processor.updateShopHistory(-2, -2, bagItem.m_inventory_id, itemTemplateID, -removed, 0, (int)m_character.m_character_id, comment);
                        remaining -= bagItem.m_quantity;
                        m_character.m_db.runCommandSync("delete from " + m_inventoryTableName + " where inventory_id=" + bagItem.m_inventory_id);

                        m_bagItems.Remove(bagItem);
                    }
                    else
                    {
                        reportStr += remaining + " of " + bagItem.m_template.m_item_name + " template ID:" + bagItem.m_template_id + " inventory ID:" + bagItem.m_inventory_id + " from inventory for quest " + quest_id + "," + stage_id + ".";
                        int removed = bagItem.m_quantity - remaining;
                        string comment = "Quest " + quest_id + "," + stage_id;
                        if (bagItem.m_bound)
                        {
                            comment += " [bound]";
                        }
                        Program.processor.updateShopHistory(-2, -2, bagItem.m_inventory_id, itemTemplateID, -removed, 0, (int)m_character.m_character_id, comment);
                        bagItem.m_quantity -= remaining;
                        m_character.m_db.runCommandSync("update " + m_inventoryTableName + " set quantity=" + bagItem.m_quantity + " where inventory_id=" + bagItem.m_inventory_id);
                        remaining = 0;
                    }
                   
                    if (remaining == 0)
                        break;

                }
            }
            if (remaining > 0)
            {

                for (int i = 0; i < m_equipedItems.Length; i++)
                {

                    if (m_equipedItems[i] != null && m_equipedItems[i].m_template_id == itemTemplateID)
                    {
                        Item itemBefore = new Item(m_equipedItems[i]);
                        Item itemAfter = null;
                        bool removedFromEquipment = false;
                        if (remaining >= m_equipedItems[i].m_quantity)
                        {
                            reportStr += m_equipedItems[i].m_quantity + " of " + m_equipedItems[i].m_template.m_item_name + " template ID:" + m_equipedItems[i].m_template_id + " inventory ID:" + m_equipedItems[i].m_inventory_id + " from equipment for quest " + quest_id + "," + stage_id + ".";
                            int removed = m_equipedItems[i].m_quantity;
                            string comment = "Quest " + quest_id + "," + stage_id;
                            if (m_equipedItems[i].m_bound)
                            {
                                comment += " [bound]";
                            }
                            Program.processor.updateShopHistory(-2, -2, m_equipedItems[i].m_inventory_id, itemTemplateID, -removed, 0, (int)m_character.m_character_id, comment);
                       
                           
                            remaining -= m_equipedItems[i].m_quantity;
                            m_character.m_db.runCommandSync("delete from " + m_inventoryTableName + " where inventory_id=" + m_equipedItems[i].m_inventory_id);
                            m_character.m_db.runCommandSync("delete from equipment where inventory_id=" + m_equipedItems[i].m_inventory_id);
                            m_equipedItems[i] = null;
                            removedFromEquipment = true;
                        }
                        else
                        {
                            reportStr += remaining + " of " + m_equipedItems[i].m_template.m_item_name + " template ID:" + m_equipedItems[i].m_template_id + " inventory ID:" + m_equipedItems[i].m_inventory_id + " from equipment for quest " + quest_id + "," + stage_id + ".";
                            int removed = m_equipedItems[i].m_quantity-remaining;
                            string comment="Quest "+quest_id+","+stage_id;
                            if (m_equipedItems[i].m_bound)
                            {
                                comment += " [bound]";
                            }

                            Program.processor.updateShopHistory(-2, -2, m_equipedItems[i].m_inventory_id, itemTemplateID, -removed, 0, (int)m_character.m_character_id, comment);
                       
                            m_equipedItems[i].m_quantity -= remaining;
                            m_character.m_db.runCommandSync("update " + m_inventoryTableName + " set quantity=" + m_equipedItems[i].m_quantity + " where inventory_id=" + m_equipedItems[i].m_inventory_id);
                            remaining = 0;
                            removedFromEquipment = true;
                            itemAfter = m_equipedItems[i];
                        }
                        if (removedFromEquipment == true && m_character.m_player != null)
                        {
                            
                            SendReplaceItem(itemBefore, itemAfter);
                            if (itemAfter == null)
                            {
                                m_character.InfoUpdated((EQUIP_SLOT)i);
                            }
                        }

                        if (remaining == 0)
                            break;

                    }
                }

            }
            if (remaining > 0)
                reportStr += " " + remaining + " outstanding";
            Program.Display(reportStr);
            if (remaining == 0)
                return true;
            else
                return false;
        }
        internal void ResetEquipmentSetRewards()
        {
            m_rewardsValid = false;
            
        }
        internal void ValidateRewards()
        {
            if (m_rewardsValid == false)
            {
                List<EquipmentSetRewards> validRewards = BuildEquipmentSetRewardList();

                List<EquipmentSetRewardContainer> listCopy = null;
                if (m_qualifiedRewards != null)
                {
                    listCopy = new List<EquipmentSetRewardContainer>(m_qualifiedRewards);
                    m_qualifiedRewards.Clear();
                }
                else
                {
                    m_qualifiedRewards = new List<EquipmentSetRewardContainer>();
                    listCopy = new List<EquipmentSetRewardContainer>();
                }

                var addedList = new List<EquipmentSetRewardContainer>();
                var removedList = new List<EquipmentSetRewardContainer>();
                double currentTime = Program.MainUpdateLoopStartTime();
                EquipmentSetRewardContainer.AddRewardsToList(m_qualifiedRewards, validRewards, m_character.m_character_id, currentTime);
                //for the old list check if any exist in the new list
                for (int curOldIndex = 0; curOldIndex < listCopy.Count; curOldIndex++)
                {
                    EquipmentSetRewardContainer oldReward = listCopy[curOldIndex];
                    EquipmentSetRewardContainer newVersion = EquipmentSetRewardContainer.GetRewardForSetAndRewardId(m_qualifiedRewards, oldReward.Reward.SetID, oldReward.Reward.RewardID);
                    if (newVersion == null)
                    {
                        removedList.Add(oldReward);
                    }
                    else
                    {
                        newVersion.TimeRecharged = oldReward.TimeRecharged;
                    }
                }

                for (int curNewIndex = 0; curNewIndex < m_qualifiedRewards.Count; curNewIndex++)
                {
                    EquipmentSetRewardContainer newReward = m_qualifiedRewards[curNewIndex];
                    EquipmentSetRewardContainer oldVersion = EquipmentSetRewardContainer.GetRewardForSetAndRewardId(listCopy, newReward.Reward.SetID, newReward.Reward.RewardID);
                    if (oldVersion == null)
                    {
                        addedList.Add(newReward);
                    }
                }

                string removedStr = "";
                string addedStr = "";
                uint characterID = m_character.m_character_id;
                for (int i = 0; i < removedList.Count; i++)
                {
                    EquipmentSetRewardContainer currentReward = removedList[i];
                    removedStr += "(character_id = " + characterID + " and equipment_set_id = " + currentReward.Reward.SetID + " and equipment_set_reward_id = " + currentReward.Reward.RewardID + ")";
                    if (i < removedList.Count-1)
                    {
                        removedStr += " or ";
                    }
                }
                if (removedStr != "")
                {
                    m_character.m_db.runCommandSync("delete from character_equipment_set_rewards where " + removedStr);
                
                }

                for (int i = 0; i < addedList.Count; i++)
                {
                    EquipmentSetRewardContainer currentReward = addedList[i];
                    addedStr += "(" + characterID + " , " + currentReward.Reward.SetID + " , " + currentReward.Reward.RewardID + ","+currentReward.Reward.ItemReward.m_item_id + "," + currentTime + ")";
                    if (i < addedList.Count - 1)
                    {
                        addedStr += " , ";
                    }
                }
                if (addedStr != "")
                {
                    m_character.m_db.runCommandSync("insert into character_equipment_set_rewards (character_id,equipment_set_id,equipment_set_reward_id,item_id,time_skill_last_cast) values " + addedStr);

                }
                    m_rewardsValid = true;
            }
        }
        internal List<EquipmentSetRewards> BuildEquipmentSetRewardList()
        {
            List<EquipmentSetContainer> availableSets = new List<EquipmentSetContainer>();
            for (int currentSlot = 0; currentSlot < m_equipedItems.Length; currentSlot++)
            {
                //do not add stats of fashion
                if (currentSlot >= (int)EQUIP_SLOT.SLOT_FASH_HEAD && currentSlot <= (int)EQUIP_SLOT.SLOT_FASH_HANDS)
                {
                    continue;
                }
                Item currentEquippedItem = m_equipedItems[currentSlot];
                if (currentEquippedItem != null && currentEquippedItem.m_template != null)
                {
                    //hold onto any item sets that are currently available
                    for (int currentSetIndex = 0; currentSetIndex < currentEquippedItem.m_template.m_equipmentSets.Count; currentSetIndex++)
                    {
                        EquipmentSet currentSet = currentEquippedItem.m_template.m_equipmentSets[currentSetIndex];
                        //find out if an item of this set has already been checked
                        EquipmentSetContainer currentContainer = EquipmentSetContainer.GetEquipmentSetHolder(availableSets, currentSet);
                        //if nopt add this to the available sets
                        if (currentContainer == null)
                        {
                            currentContainer = new EquipmentSetContainer(currentSet);
                            availableSets.Add(currentContainer);
                        }
                        //increase the number of qualifying items
                        currentContainer.Count++;
                    }
                }
            }
            
            List<EquipmentSetRewards> qualifiedRewards = EquipmentSetContainer.GetRewardsForEquipmentSetHolder(availableSets);


            return qualifiedRewards;
            
        }
        internal void AddItemStats(Item currentItem,
            CombatEntityStats equipmentStats,
            CombatEntityStats equipmentStatsMultipliers,
            ref int armourValue,
            ref int attackbonus,
            ref int defencebonus,
            ref int hpbonus,
            ref int energybonus,
            ref int encumbrance,
            ref int concentrationbonus,
            List<CharacterEffect> oldEquipmentEffects)
        {
           
            if (currentItem != null)
            {
                armourValue += currentItem.m_template.m_armour;
                
               /* for (int j = 0; j < CombatEntity.NUM_BONUS_TYPES; j++)
                {
                    equipmentStats.AddToBonusType(j, currentItem.m_template.m_BonusTypes[j]);
                }*/
                for (int j = 0; j < currentItem.m_template.m_bonusTypes.Count; j++)
                {
                    FloatForID currentVal = currentItem.m_template.m_bonusTypes[j];
                    equipmentStats.AddToBonusType(currentVal.m_bonusType, currentVal.m_amount);
                }
                for (int j = 0; j < currentItem.m_template.m_damageTypes.Count; j++)
                {
                    FloatForID currentType = currentItem.m_template.m_damageTypes[j];
                    equipmentStats.AddToWeaponDamageType(currentType.m_bonusType, currentType.m_amount);
                }


                for (int j = 0; j < currentItem.m_template.m_avoidanceTypes.Count; j++)
                {
                    FloatForID currentType = currentItem.m_template.m_avoidanceTypes[j];
                    equipmentStats.AddToAvoidanceType((AVOIDANCE_TYPE)currentType.m_bonusType, currentType.m_amount);
                   // equipmentStats.AddToAvoidanceType((AVOIDANCE_TYPE)j, currentItem.m_template.m_AvoidanceTypes[j]);
                }
                for (int j = 0; j < currentItem.m_template.m_immunityTypes.Count; j++)
                {
                    FloatForID currentVal = currentItem.m_template.m_immunityTypes[j];
                    equipmentStats.AddToImmunityType(currentVal.m_bonusType, currentVal.m_amount);
                }
                for (int j = 0; j < currentItem.m_template.m_damageReductionTypes.Count; j++)
                {
                    FloatForID currentVal = currentItem.m_template.m_damageReductionTypes[j];
                    equipmentStats.AddToDamageReductionType(currentVal.m_bonusType, currentVal.m_amount);
                }
                attackbonus += (int)currentItem.m_template.GetItemBonus(BONUS_TYPE.ATTACK_BONUS);
                defencebonus += (int)currentItem.m_template.GetItemBonus(BONUS_TYPE.DEFENCE_BONUS);
                hpbonus += (int)currentItem.m_template.GetItemBonus(BONUS_TYPE.HEALTH_BONUS);
                energybonus += (int)currentItem.m_template.GetItemBonus(BONUS_TYPE.ENERGY_BONUS);
                encumbrance += currentItem.m_template.m_weight;
                concentrationbonus += (int)currentItem.m_template.GetItemBonus(BONUS_TYPE.CONCENTRATION_BONUS);
                for (int j = 0; j < currentItem.m_template.m_statusEffects.Count; j++)
                {
                    ItemStatusEffect statusEffect = currentItem.m_template.m_statusEffects[j];
                    if (statusEffect == null)
                        continue;

                    //look for the same effect in the old status effects
                    CharacterEffect oldEffect = null;
                    for (int currentOldIndex = 0; currentOldIndex < oldEquipmentEffects.Count && oldEffect == null; currentOldIndex++)
                    {
                        CharacterEffect currentOldEffect = oldEquipmentEffects[currentOldIndex];
                        if (currentOldEffect.StatusEffect == null)
                            continue;

                        StatusEffectTemplate currentTemplate = currentOldEffect.StatusEffect.Template;
                        //if you find one add the old version
                        if (statusEffect.m_effect_id == currentTemplate.StatusEffectID && statusEffect.m_level == currentOldEffect.StatusEffect.m_statusEffectLevel)
                        {
                            oldEffect = currentOldEffect;
                            m_character.AddExistingStatusEffect(oldEffect);

                            oldEquipmentEffects.Remove(oldEffect);
                        }

                    }
                    //otherwise create a new one
                    if (oldEffect == null)
                    {
                        CharacterEffectParams param = new CharacterEffectParams
                        {
                            charEffectId = statusEffect.m_effect_id,
                            caster = m_character,
                            level = statusEffect.m_level,
                            aggressive = false,
                            PVP = false,
                            statModifier = 0
                        };
                        CharacterEffectManager.InflictNewCharacterEffect2(param, m_character);
                        //m_character.InflictNewStatusEffect(statusEffect.m_effect_id, m_character, statusEffect.m_level, false, false, 0);
                    }
                }
                //apply any other modifiers
                for (int currentModIndex = 0; currentModIndex < currentItem.m_template.m_modifiers.Count; currentModIndex++)
                {
                    CharacterModifiers currentModifier = currentItem.m_template.m_modifiers[currentModIndex];
                    currentModifier.ApplyToStatsAddition(equipmentStats);


                }
                for (int currentModIndex = 0; currentModIndex < currentItem.m_template.m_combatModifiers.Count; currentModIndex++)
                {
                    CombatModifiers currentModifier = currentItem.m_template.m_combatModifiers[currentModIndex];
                    currentModifier.ApplyCombatParam(equipmentStats, equipmentStatsMultipliers);
                }
            }
        }

		/// <summary>
		/// Duplicate of AddItemStats method but we ONLY copy the section to do with status effects.  This is for the companion
		/// system where we need the 'hungry' status effect with none of the other item bonuses
		/// </summary>
		/// <param name="currentItem"></param>
		/// <param name="equipmentStats"></param>
		/// <param name="equipmentStatsMultipliers"></param>
		/// <param name="armourValue"></param>
		/// <param name="attackbonus"></param>
		/// <param name="defencebonus"></param>
		/// <param name="hpbonus"></param>
		/// <param name="energybonus"></param>
		/// <param name="encumbrance"></param>
		/// <param name="oldEquipmentEffects"></param>
		internal void AddItemStatsStatusEffectsOnly(Item currentItem,
			CombatEntityStats equipmentStats,
			CombatEntityStats equipmentStatsMultipliers,
			ref int armourValue,
			ref int attackbonus,
			ref int defencebonus,
			ref int hpbonus,
			ref int energybonus,
			ref int encumbrance,
			List<CharacterEffect> oldEquipmentEffects)
		{

			for (int j = 0; j < currentItem.m_template.m_statusEffects.Count; j++)
			{
				ItemStatusEffect statusEffect = currentItem.m_template.m_statusEffects[j];
				if (statusEffect == null)
					continue;

				//look for the same effect in the old status effects
				CharacterEffect oldEffect = null;
				for (int currentOldIndex = 0; currentOldIndex < oldEquipmentEffects.Count && oldEffect == null; currentOldIndex++)
				{
					CharacterEffect currentOldEffect = oldEquipmentEffects[currentOldIndex];
					if (currentOldEffect.StatusEffect == null)
						continue;

					StatusEffectTemplate currentTemplate = currentOldEffect.StatusEffect.Template;
					//if you find one add the old version
					if (statusEffect.m_effect_id == currentTemplate.StatusEffectID && statusEffect.m_level == currentOldEffect.StatusEffect.m_statusEffectLevel)
					{
						oldEffect = currentOldEffect;
						m_character.AddExistingStatusEffect(oldEffect);

						oldEquipmentEffects.Remove(oldEffect);
					}

				}
				//otherwise create a new one
				if (oldEffect == null)
				{
					CharacterEffectParams param = new CharacterEffectParams
					{
						charEffectId = statusEffect.m_effect_id,
						caster = m_character,
						level = statusEffect.m_level,
						aggressive = false,
						PVP = false,
						statModifier = 0
					};
					CharacterEffectManager.InflictNewCharacterEffect2(param, m_character);

				}
			}

		}


		/// <summary>
		/// this method will also apply status effects
		/// </summary>
        internal void calculateEquipmentModifiers()
        {
			
            //JT STATS CHANGES 12_2011
            CombatEntityStats baseStats = m_character.BaseStats;
            CombatEntityStats equipmentStats = m_character.EquipStats;
            CombatEntityStats equipmentStatsMultipliers = m_character.EquipStatsMultipliers;
            equipmentStats.ResetStats(0);
            equipmentStatsMultipliers.ResetStats(1);
            ValidateRewards();
            //how should this go into the stats system
            if (m_equipedItems[0] == null)
            {
                baseStats.AttackSpeed = DEFAULT_ATTACK_SPEED;
            }
            else
            {
                baseStats.AttackSpeed = m_equipedItems[0].m_template.m_attack_speed;
            }
            m_character.BlocksAttacks = false;
            if (m_equipedItems[(int)EQUIP_SLOT.SLOT_OFFHAND] == null)
            {
                m_character.BlocksAttacks = true;
            }
            else if (m_equipedItems[(int)EQUIP_SLOT.SLOT_WEAPON] != null)
            {
                if (m_equipedItems[(int)EQUIP_SLOT.SLOT_WEAPON].m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.BOW)
                {
                    m_character.BlocksAttacks = true;
                }
            }
            if (m_equipedItems[(int)EQUIP_SLOT.SLOT_OFFHAND] != null)
            {
                if (m_equipedItems[(int)EQUIP_SLOT.SLOT_OFFHAND].m_template.m_subtype == ItemTemplate.ITEM_SUB_TYPE.SHIELD)
                {
                    m_character.BlocksAttacks = true;
                }
            }


            int armourValue = 0;

            //keep ahold of the old item status effects
            List<CharacterEffect> oldEquipmentEffects = new List<CharacterEffect>();
            for (int i = m_character.m_currentCharacterEffects.Count - 1; i >= 0; i--)
            {
                CharacterEffect cuttentEffect = m_character.m_currentCharacterEffects[i];
                if (cuttentEffect.StatusEffect != null && cuttentEffect.StatusEffect.Template.ItemOnly)
                {
                    m_character.m_currentCharacterEffects.RemoveAt(i);
                    oldEquipmentEffects.Add(cuttentEffect);
                    m_character.StatusListChanged = true;
                }
            }
            int attackbonus = 0;
            int defencebonus = 0;
            int hpbonus = 0;
            int energybonus = 0;
            int encumbrance = 0;
		    int concentrationbonus = 0;

            //goes through all equipped items and will add base stats (I think) and also
			//add status effects
            for (int currentSlot = 0; currentSlot < m_equipedItems.Length; currentSlot++)
            {
                //do not add stats of fashion
                if (currentSlot >= (int)EQUIP_SLOT.SLOT_FASH_HEAD && currentSlot <= (int)EQUIP_SLOT.SLOT_FASH_HANDS)
                    continue;
                
                //do not add off hand first; do last.
                if (currentSlot == (int)EQUIP_SLOT.SLOT_OFFHAND)
                    continue;
                
				//also skip companions as we have more complex logic below to deal with feeding
				if (currentSlot == (int)EQUIP_SLOT.SLOT_COMPANION)
					continue;

                // skip mount and saddles - these are dependant on current effects
                if (currentSlot == (int)EQUIP_SLOT.SLOT_SADDLE || currentSlot == (int)EQUIP_SLOT.SLOT_MOUNT)
                    continue;                

				//get equipped item and add stats
				Item currentEquippedItem = m_equipedItems[currentSlot];	
                AddItemStats(currentEquippedItem, equipmentStats, equipmentStatsMultipliers, 
					ref armourValue, ref attackbonus, ref defencebonus, ref hpbonus, ref energybonus, ref encumbrance,ref concentrationbonus, oldEquipmentEffects);
            }

			//off hand done seperately - add last
            const EQUIP_SLOT offhandSlot = EQUIP_SLOT.SLOT_OFFHAND;
            Item offhandEquippedItem = m_equipedItems[(int)offhandSlot];
            if (null != offhandEquippedItem)
            {
                AddItemStats(offhandEquippedItem, equipmentStats, equipmentStatsMultipliers, 
					ref armourValue, ref attackbonus, ref defencebonus, ref hpbonus, ref energybonus, ref encumbrance, ref concentrationbonus, oldEquipmentEffects);
            }

			#region companion slot

			//companion slot - special case
			//we only want to add stat bonuses from the companion if they have been fed
			//so for that we need to check what status effect the character has and see
			//if we have matching one for our own
			Item companionEquippedItem = m_equipedItems[(int)EQUIP_SLOT.SLOT_COMPANION];
			{
				//do we have a companion equipped?
				if (companionEquippedItem != null)
				{
					//perform a check to enforce only one or zero status effects						

					//----------- treat as normal item ------------------
					//normal item case - no status effect which would be hunger so we add the stats like other equipment
					if (companionEquippedItem.m_template.m_statusEffects.Count == 0)
					{
						AddItemStats(companionEquippedItem, equipmentStats, equipmentStatsMultipliers,
							ref armourValue, ref attackbonus, ref defencebonus, ref hpbonus, ref energybonus, ref encumbrance, ref concentrationbonus, oldEquipmentEffects);
					}


					//----------- hunger check here ----------------
					//has 1 status effect which we now ASSUME this is a feeding effect to check against
					if (companionEquippedItem.m_template.m_statusEffects.Count == 1)
					{
						//flag to check if we are fed or not 
						bool beenFed = false;

						//get our item status effect for this equipment 
						ItemStatusEffect itemStatusEffectOnCompanion = companionEquippedItem.m_template.m_statusEffects[0];
						//from this get status effect template (we don't check for the old status effect system)
						StatusEffectTemplate statusEffectTemplateForCompanion = StatusEffectTemplateManager.GetStatusEffectTemplateForID((EFFECT_ID)itemStatusEffectOnCompanion.m_effect_id);

						//go through current character effects and look for a matching effect for us
						//Note.  I'm assuming we ALWAYS require pets to be fed							        						
						for (int i = 0; i < this.m_character.m_currentCharacterEffects.Count; i++)
						{
							StatusEffect statusEffectOnChar = this.m_character.m_currentCharacterEffects[i].StatusEffect;

							//do we have a matching hunger status class_id -  that's higher than us? 
							//this will always be true at least once as our hungry effect will be active							
							if (statusEffectOnChar.Template.EffectClass.m_class_id == statusEffectTemplateForCompanion.EffectClass.m_class_id)
							{
								if (statusEffectOnChar.m_effectLevel.m_class_level > statusEffectTemplateForCompanion.getEffectLevel(itemStatusEffectOnCompanion.m_level, false).m_class_level)
								{
									//yes we are fed - so add stats!
									AddItemStats(companionEquippedItem, equipmentStats, equipmentStatsMultipliers,
										ref armourValue, ref attackbonus, ref defencebonus, ref hpbonus, ref energybonus, ref encumbrance, ref concentrationbonus,
										oldEquipmentEffects);
									beenFed = true;
								}
							}
						}

						//no we weren't fed
						if (beenFed == false)
						{
							//so use a modified add item stats message to ONLY apply status effects
							//which in our case will be the hungry status effect							
							AddItemStatsStatusEffectsOnly(companionEquippedItem, equipmentStats, equipmentStatsMultipliers,
								ref armourValue, ref attackbonus, ref defencebonus, ref hpbonus, ref energybonus, ref encumbrance,
								oldEquipmentEffects);
						}

					}


					// ---------------- Check for Bad Templace Design --------------
					//more than 1 status effect - we don't know how to deal with this case
					if (companionEquippedItem.m_template.m_statusEffects.Count > 1)
					{
						Program.Display("Error. Pets must have either have 0(normal item) or 1(requires feeding) status effect in their template");
					}

					//end of hunger/companion check					
				}


			}

			#endregion

            // handle effect cause by dying whilst mounted
            HandleMountAndSaddleEffects(equipmentStats, equipmentStatsMultipliers, oldEquipmentEffects, ref armourValue, ref attackbonus, ref defencebonus, ref hpbonus, ref energybonus, ref encumbrance, ref concentrationbonus);

            // see if there are any set rewards
            if (m_qualifiedRewards!=null&&m_qualifiedRewards.Count>0)
            {
                for (int rewardIndex = 0; rewardIndex < m_qualifiedRewards.Count; rewardIndex++)
                {
                    EquipmentSetRewardContainer currentContainer = m_qualifiedRewards[rewardIndex];
                    EquipmentSetRewards currentReward = currentContainer.Reward;
                    ItemTemplate rewardTemplate = currentReward.ItemReward;
                    Item tempRewardItem = Item.CreateQuickShallowItem(rewardTemplate, 1);
                    AddItemStats(tempRewardItem, equipmentStats, equipmentStatsMultipliers, ref armourValue, ref attackbonus, ref defencebonus, ref hpbonus, ref energybonus, ref encumbrance, ref concentrationbonus, oldEquipmentEffects);
                }           
            }
            // end any status effects that were not renewed
            bool recalculateStats = false;
            for (int oldStatusEffectIndex = 0; oldStatusEffectIndex < oldEquipmentEffects.Count;oldStatusEffectIndex++) 
            {
                CharacterEffect expiringEffect = oldEquipmentEffects[oldStatusEffectIndex];
                recalculateStats = true;
                if(expiringEffect.StatusEffect != null)
                    expiringEffect.StatusEffect.EndEffect();
                m_character.StatusListChanged = true;
            }
		   
            
            
            if (recalculateStats)
            {
                CharacterEffectManager.UpdateCombatStats(m_character);
            }
			
			
            //should we have this in base?-probably not as that would require base to be ajusted by changing equipment
            if (m_equipedItems[0] == null)
            {
                equipmentStats.AddToWeaponDamageType((int)DAMAGE_TYPE.CRUSHING_DAMAGE, 2);
            }
            //clamp some values
      
            //apply these to the base stats
            equipmentStats.Armour += armourValue;
            equipmentStats.Attack += attackbonus;
            equipmentStats.Defence += defencebonus;
            equipmentStats.MaxHealth += hpbonus;
            equipmentStats.MaxEnergy += energybonus;
            equipmentStats.Encumberance += encumbrance;
		    equipmentStats.MaxConcentrationFishing += concentrationbonus;            
			
			//check for hungry status effect present - if so disable/enable skill
			updateCompanionSkillBasedOnStatus();

            //check for dismounted status effect present - if so disable/enable skill
            updateMountSkillBasedOnStatus();
			

        }

  
        /// <summary>
        /// Applies saddle and mount equipment bonuses if we don't have a dismounted effect active
        /// </summary>
        /// <param name="in_equipmentStats">Hots, Dots, immunities, modifiers etc</param>
        /// <param name="in_equipmentStatsMultipliers"></param>
        /// <param name="in_oldEquipmentEffects">Previous effects</param>
        /// <param name="in_armourValue"></param>
        /// <param name="in_attackbonus"></param>
        /// <param name="in_defencebonus"></param>
        /// <param name="in_hpbonus"></param>
        /// <param name="in_energybonus"></param>
        /// <param name="in_encumbrance"></param>
        /// <param name="in_concentrationbonus"></param>
        private void HandleMountAndSaddleEffects(
            CombatEntityStats in_equipmentStats,
            CombatEntityStats in_equipmentStatsMultipliers,
            List<CharacterEffect> in_oldEquipmentEffects,
            ref int in_armourValue,
            ref int in_attackbonus,
            ref int in_defencebonus,
            ref int in_hpbonus,
            ref int in_energybonus,
            ref int in_encumbrance,
            ref int in_concentrationbonus)
        {
            // find if we have dismount effect
            for (int i = 0; i < m_character.m_currentCharacterEffects.Count; ++i)
            {                
                // if we have a dismounted effect, this prevents us from applying saddle and moutn bonuses
                if ((int)m_character.m_currentCharacterEffects[i].StatusEffect.Template.StatusEffectID == Character.DISMOUNTED_STATUS_ID) // 20007 is Dismounted status effect
                    return;
            }

            // if not, apply saddle and mount equipment effects
            Item equippedItemSaddle = m_equipedItems[(int)EQUIP_SLOT.SLOT_SADDLE];
            Item equippedItemMount = m_equipedItems[(int)EQUIP_SLOT.SLOT_MOUNT];
            
            // apply effect if saddle equipped
			if (equippedItemSaddle != null)
			{				
				AddItemStats(equippedItemSaddle, in_equipmentStats, in_equipmentStatsMultipliers,
                    ref in_armourValue, ref in_attackbonus, ref in_defencebonus, ref in_hpbonus, ref in_energybonus, ref in_encumbrance, ref in_concentrationbonus, in_oldEquipmentEffects);				
			}

            // apply effect if mount equipped
			if (equippedItemMount != null)
			{				
				AddItemStats(equippedItemMount, in_equipmentStats, in_equipmentStatsMultipliers,
                    ref in_armourValue, ref in_attackbonus, ref in_defencebonus, ref in_hpbonus, ref in_energybonus, ref in_encumbrance, ref in_concentrationbonus, in_oldEquipmentEffects);				
			}
        }

    
        internal string WriteInventoryToString()
        {
            string inventoryString = "";
            for (int i = 0; i < m_bagItems.Count; i++)
            {
                Item currentItem = m_bagItems[i];
                //inventoryID,TemplateID,amount;
                inventoryString += currentItem.m_inventory_id + "," + currentItem.m_template_id + "," + currentItem.m_quantity + ";";
            }

            return inventoryString;
        }
        internal string useItem(int item_id,uint targetServerID, int targetType)
        {
            Program.Display("useItem." + item_id + " target." + targetServerID + " tarType." + targetType);
            string errorString = "";
            TimeSpan timeSinceLastItemUse = (m_character.m_lastItemUse - DateTime.Now);

            //if (timeSinceLastItemUse.TotalSeconds < ItemCooldown.GetItemCooldownForId(GetItemFromInventoryID(item_id, true).m_template_id))
            if ((m_character.m_lastItemUse - TimeSpan.FromSeconds(ItemTemplateManager.ITEM_LEEWAY_TIME)) > DateTime.Now && ItemCooldown.GetItemCooldownForId(GetItemFromInventoryID(item_id, true).m_template_id) != 0)
            {
				string locText = Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.CANNOT_USE_ITEM_FOR_TIME);
				locText = string.Format(locText, (timeSinceLastItemUse.TotalSeconds).ToString("F0"));
				errorString = locText;

				Program.processor.sendSystemMessage(errorString, m_character.m_player, false, SYSTEM_MESSAGE_TYPE.NONE);
                return errorString;
            }
            if (m_character.CanUseItems(ref errorString) == false)
            {
                Program.processor.sendSystemMessage(errorString, m_character.m_player, false, SYSTEM_MESSAGE_TYPE.ITEM_USE);

                return errorString;
            }
            for (int i = 0; i < m_bagItems.Count; i++)
            {
                if (m_bagItems[i].m_inventory_id == item_id)
                {
                    Item item = m_bagItems[i];
                    ItemTemplate template = item.m_template;

                    if (template.m_SklllEffect == 0)
                    {
						return Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.NOT_USABLE_ITEM);
					}

                    // the mount whistle has skill effect 8066, check for dismounted
                    // also sklllEffect is misspelled /sigh                    
                    if ((int) template.m_SklllEffect == 8066)
                    {
                        if (this.m_character.HasDismountedEffect() == false)
                        {
							return Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.DO_NOT_NEED_WHISTLE);
						}
                    }
                    

                    if (!template.checkIfAllowed(this.m_character))
                    {
						return Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.NOT_USABLE_BY_CHARACTER);
					}

                    if (item != null && item.m_template != null)
                    {
                        bool itemUsed = AttemptToUseItem(item, targetServerID, targetType);
                        if (itemUsed == true)
                        {
                            m_character.m_lastItemIdUsed = item_id;

							string locText = Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.ITEM_USED);
							locText = string.Format(locText, item.m_template.m_loc_item_name[m_character.m_player.m_languageIndex]);
							Program.processor.sendSystemMessage(locText, m_character.m_player, true, SYSTEM_MESSAGE_TYPE.ITEM_USE);
						}
                        return "";
                    }
					return Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.INVALID_ITEM);
				}
            }

            for (int i = 0; i < m_equipedItems.Length; i++)
            {
                if (m_equipedItems[i] != null && m_equipedItems[i].m_inventory_id == item_id)
                {
                    ItemTemplate template = m_equipedItems[i].m_template;
                    if (template.m_SklllEffect == 0)
                    {
						return Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.NOT_USABLE_ITEM);
					}
                    if (!template.checkIfAllowed(m_character))
                    {
						return Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.NOT_USABLE_BY_CHARACTER);
					}

                    Item theItem = m_equipedItems[i];
                    if (theItem != null && theItem.m_template != null)
                    {
                        bool itemUsed = AttemptToUseItem(theItem, targetServerID, targetType);
                        if (itemUsed == true)
                        {
							string locText = Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.ITEM_USED);
							locText = string.Format(locText, theItem.m_template.m_loc_item_name[m_character.m_player.m_languageIndex]);
							Program.processor.sendSystemMessage(locText, m_character.m_player, true, SYSTEM_MESSAGE_TYPE.ITEM_USE);
						}

                        return "";
                    }
					return Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.INVALID_ITEM);
				}
            }
			return Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.CANNOT_FIND_ITEM);
		}
        internal bool AttemptToUseItem(Item item,uint targetID, int targetType)
        {
            bool itemUsed = false;

            //don't allow use if dead
            if (m_character.Dead)
            {
                return false;
            }
            SkillTemplate skillTemplate = SkillTemplateManager.GetItemForID(item.m_template.m_SklllEffect);
            int level = item.m_template.m_SkillLevel;
            EntitySkill skill= new EntitySkill(skillTemplate);
            skill.SkillLevel = level;
            skill.FromItem = true;

			string skillKnown = Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.ALREADY_KNOW_SKILL);
			string recipeKnown = Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.ALREADY_KNOW_RECIPE);
			//do skill use malarchy
			if ((int)item.m_template.m_SklllEffect >= SkillTemplate.LEARN_SKILL_START_ID && (int)item.m_template.m_SklllEffect < SkillTemplate.LEARN_SKILL_END_ID)
            {
                SKILL_TYPE skillToLearn = (SKILL_TYPE)((int)item.m_template.m_SklllEffect - 1000);
                itemUsed = m_character.AddSkill(skillToLearn, true, true);
                if (itemUsed == false)
                {
                    //you already know this skill
                    Program.processor.sendSystemMessage(skillKnown, m_character.m_player,false, SYSTEM_MESSAGE_TYPE.ITEM_USE);
                }
            }
            else if (skillTemplate.LearnRecipeID!=0)
            {
                itemUsed = m_character.AddRecipe(skillTemplate.LearnRecipeID);
                if (itemUsed == false)
                {
                    //You already know this recipe
                    Program.processor.sendSystemMessage(recipeKnown, m_character.m_player, false, SYSTEM_MESSAGE_TYPE.ITEM_USE);
                }
            }
            else
            {
                //don't allow use if dead
                if (m_character.Dead)
                {
                    return false;
                }

                switch (item.m_template.m_SklllEffect)
                {


                    case SKILL_TYPE.RESET_SKILLS:
                    {
                        m_character.ResetSkills();
                        itemUsed = true;
                        break;
                    }
                    case SKILL_TYPE.RESET_STATS:
                    {
                        m_character.ResetAttributes();
                        itemUsed = true;

                        break;
                    }
                    case SKILL_TYPE.ENERGY_REGEN_ELIXIR:
                    case SKILL_TYPE.HEALTH_REGEN_ELIXIR:
                    case SKILL_TYPE.ARM_BOOST_ELIX:
                    case SKILL_TYPE.ATT_BOOST_ELIXIR:
                    case SKILL_TYPE.ATT_SPD_BOOST_ELIX:
                    case SKILL_TYPE.DEF_BOOST_ELIXIR:
                    case SKILL_TYPE.ENERGY_REGEN_ELIX:
                    case SKILL_TYPE.EXP_BOOST_ELIXIR:
                    case SKILL_TYPE.FISHING_EXP_BOOST_ELIXIR:
                    case SKILL_TYPE.ABILITY_BOOST_ELIXIR:
                    case SKILL_TYPE.GROWTH_ELIXIR:
                    case SKILL_TYPE.HEALTH_REGEN_ELIX:
                    case SKILL_TYPE.MAX_ENERGY_BOOST_ELIX:
                    case SKILL_TYPE.MAX_HEALTH_BOOST_ELIX:
                    case SKILL_TYPE.SHRINK_ELIXIR:
                    case SKILL_TYPE.RUN_SPD_BOOST_ELIXIR:
                        {                            
                            if (skillTemplate == null)
                            {
                                break;
                            }
                            //don't allow use if dead
                            if (m_character.Dead)
                            {
                                return false;
                            }
                            //check they don't have the effect already
                            StatusEffect statusEffect = m_character.GetStatusEffectForID(skillTemplate.StatusEffectID);
                            if (statusEffect != null && statusEffect.m_statusEffectLevel >= skill.SkillLevel)
                            {
								string locText = Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.ALREADY_HAVE_ELIXIR_ACTIVE);
								Program.processor.sendSystemMessage(locText, m_character.m_player, true, SYSTEM_MESSAGE_TYPE.ITEM_USE);
								itemUsed = false;
                                break;
                            }

                            if (skillTemplate.CastTargetGroup == SkillTemplate.CAST_TARGET.SELF)
                            {                                
                                itemUsed = m_character.m_zone.UseSkillOnPlayer(0, (int)m_character.m_character_id, skill, m_character.m_player, true);
                            }
                        break;
                    }
                    case SKILL_TYPE.ENERGY_REGEN_POT:
                    case SKILL_TYPE.HEALTH_REGEN_POT:
                    case SKILL_TYPE.ARM__BOOST_POT:
                    case SKILL_TYPE.ATT_BOOST_POT:
                    case SKILL_TYPE.ATT_SPD_BOOST_POT:
                    case SKILL_TYPE.DEF_BOOST_POT:
                    case SKILL_TYPE.EXP_BOOST_POT:
                    case SKILL_TYPE.ABILITY_BOOST_POT:
                    case SKILL_TYPE.GROWTH_POTION:
                    case SKILL_TYPE.MAX_ENERGY_BOOST_POT:
                    case SKILL_TYPE.MAX_HEALTH_BOOST_POT:
                    case SKILL_TYPE.SHRINK_POTION:
                    case SKILL_TYPE.RUN_SPD_BOOST_POT:
                    {

						if (skillTemplate == null)
						{
							break;
						}
						//don't allow use if dead
						if (m_character.Dead)
						{
							return false;
						}
						//check they don't have the effect already
						StatusEffect statusEffect = m_character.GetStatusEffectForID(skillTemplate.StatusEffectID);
						if (statusEffect != null && statusEffect.m_statusEffectLevel >= skill.SkillLevel)
						{
							string locText = Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.ALREADY_HAVE_POTION_ACTIVE);
							Program.processor.sendSystemMessage(locText, m_character.m_player, true, SYSTEM_MESSAGE_TYPE.ITEM_USE);
							itemUsed = false;
							break;
						}

                        if (skillTemplate.CastTargetGroup == SkillTemplate.CAST_TARGET.SELF)
                        {
                            itemUsed = m_character.m_zone.UseSkillOnPlayer(0, (int) m_character.m_character_id, skill,
                                m_character.m_player, true);
                        }
                        break;
                    }
                    case SKILL_TYPE.COMBI_POT_1:
                    {
                        itemUsed = UseCombinationPotion1(skillTemplate, skill, targetID, targetType);
                        break;

                    }
                    case SKILL_TYPE.COMBI_ELIX_1:
                    {
                        itemUsed = UseCombinationElixir1(skillTemplate, skill, targetID, targetType);
                        break;
                    }
                    default:
                    {
						if (skillTemplate == null)
						{
							break;
						}
						if ((item.m_template.m_SklllEffect == SKILL_TYPE.INSTANT_HEAL||
							item.m_template.m_SklllEffect == SKILL_TYPE.INSTANT_HEAL_500) && 
							m_character.CurrentHealth >= m_character.MaxHealth)
						{
							string locText = Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.HEALTH_FULL);
							Program.processor.sendSystemMessage(locText, m_character.m_player, true, SYSTEM_MESSAGE_TYPE.ITEM_USE);
							break;
						}
						if ((item.m_template.m_SklllEffect == SKILL_TYPE.INSTANT_ENERGY||
							item.m_template.m_SklllEffect == SKILL_TYPE.INSTANT_ENERGY_500)&& 
							m_character.CurrentEnergy >= m_character.MaxEnergy)
						{
							string locText = Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.ENERGY_FULL);
							Program.processor.sendSystemMessage(locText, m_character.m_player, true, SYSTEM_MESSAGE_TYPE.ITEM_USE);
								break;
 						}
						if ((item.m_template.m_SklllEffect == SKILL_TYPE.RESTORATION_POTION)
							&& m_character.CurrentEnergy >= m_character.MaxEnergy && 
							m_character.CurrentHealth >= m_character.MaxHealth)
						{
							string locText = Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.HEALTH_AND_ENERGY_FULL);
							Program.processor.sendSystemMessage(locText, m_character.m_player, true, SYSTEM_MESSAGE_TYPE.ITEM_USE);
							break;
 						}
 						//if its a self only don't use the target
						if (skillTemplate.CastTargetGroup == SkillTemplate.CAST_TARGET.SELF)
						{
							itemUsed = m_character.m_zone.UseSkillOnPlayer(0, (int)m_character.m_character_id, skill, m_character.m_player, true);
						}
						else if (skillTemplate.CastTargetGroup == SkillTemplate.CAST_TARGET.ENEMY && targetType == (int)Zone.TARGET_TYPE.MOB)
						{
							itemUsed = m_character.m_zone.UseSkillOnMob(0, (int)targetID, skill, m_character.m_player, true);
						}
						else if (skillTemplate.CastTargetGroup == SkillTemplate.CAST_TARGET.FRIENDLY && (targetType == (int)Zone.TARGET_TYPE.OTHER_PLAYER || targetType == (int)Zone.TARGET_TYPE.SELF))
						{
							itemUsed = m_character.m_zone.UseSkillOnPlayer(0, (int)targetID, skill, m_character.m_player, true);
						}
						else if (skillTemplate.CastTargetGroup == SkillTemplate.CAST_TARGET.GROUP && (targetType == (int)Zone.TARGET_TYPE.OTHER_PLAYER || targetType == (int)Zone.TARGET_TYPE.SELF))
						{
							itemUsed = m_character.m_zone.UseSkillOnPlayer(0, (int)targetID, skill, m_character.m_player, true);
						}
						else
						{
							string locText = Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.INVALID_SKILL_TARGET);
							string skillName = SkillTemplateManager.GetLocaliseSkillName(m_character.m_player, skillTemplate.SkillID);
							locText = string.Format(locText, skillName);
							Program.processor.sendSystemMessage(locText, m_character.m_player, false, SYSTEM_MESSAGE_TYPE.SKILLS);
						}
                        break;
                    }
                }
            }
            if (itemUsed)
            {
                ConsumeItem(item.m_template_id, item.m_inventory_id, 1);
            }

            // Always put items on cooldown regarless of success - CHAR-2209
            if (ItemCooldown.GetItemCooldownForId(item.m_template_id) > 0)
            {
                TimeSpan cooldownTime = TimeSpan.FromSeconds(ItemCooldown.GetItemCooldownForId(item.m_template_id));
                m_character.m_lastItemUse = DateTime.Now + cooldownTime;
            }
           
            return itemUsed;
        }
        bool UseCombinationPotion1(SkillTemplate skillTemplate, EntitySkill skill, uint targetID, int targetType)
        {
            

            if (skillTemplate == null)
            {
                return false;
            }
            if (m_character.Dead)
            {
                return false;
            }
            int skillLevel = skill.SkillLevel;
            if (skillTemplate.CastTargetGroup == SkillTemplate.CAST_TARGET.SELF)
            {
                targetID = m_character.m_character_id;
            }
            else if (targetType == (int)Zone.TARGET_TYPE.MOB && skillTemplate.CastTargetGroup == SkillTemplate.CAST_TARGET.FRIENDLY)
            {
                targetID = m_character.m_character_id;
            }

            //check if they have the elixir or potion active
            StatusEffect elixirStatusEffect = m_character.GetStatusEffectForID(EFFECT_ID.COMBI_ELIX_1);
            StatusEffect statusEffect = m_character.GetStatusEffectForID(skillTemplate.StatusEffectID);
            if (elixirStatusEffect!=null|| (statusEffect != null && statusEffect.m_statusEffectLevel >= skill.SkillLevel))
            {
				string locText = Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.COMBINATION_EFFECT_ACTIVE);
				Program.processor.sendSystemMessage(locText, m_character.m_player, false, SYSTEM_MESSAGE_TYPE.ITEM_USE);
				return false;
            }

            bool didUseSkill = m_character.m_zone.UseSkillOnPlayer(0, (int)targetID, skill, m_character.m_player, true);
            //if it failed to cast then don't cast the effects or consume the item
            if (didUseSkill == false)
            {
                return false;
            }
            /* defence, armour, health, energy, offence, haste, travelling, knowledge, energiser, regeneration
                 defence, armour, health, energy, offence, haste, travelling, knowledge, energiser, regeneration*/


            SkillTemplate defenceSkillTemplate = SkillTemplateManager.GetItemForID(SKILL_TYPE.DEF_BOOST_POT);
            if (defenceSkillTemplate != null)
            {
                EntitySkill defenceSkill = new EntitySkill(defenceSkillTemplate);
                defenceSkill.SkillLevel = skillLevel;
                if (skillTemplate.CastTargetGroup != SkillTemplate.CAST_TARGET.ENEMY)
                {
                    m_character.m_zone.UseSkillOnPlayer(0, (int)targetID, defenceSkill, m_character.m_player, true);
                }
            }
            //___________________________
            SkillTemplate armourSkillTemplate = SkillTemplateManager.GetItemForID(SKILL_TYPE.ARM__BOOST_POT);
            if (armourSkillTemplate != null)
            {
                EntitySkill armourSkill = new EntitySkill(armourSkillTemplate);
                armourSkill.SkillLevel = skillLevel;
                if (skillTemplate.CastTargetGroup != SkillTemplate.CAST_TARGET.ENEMY)
                {
                    m_character.m_zone.UseSkillOnPlayer(0, (int)targetID, armourSkill, m_character.m_player, true);
                }
            }
            //___________________________
            SkillTemplate healthSkillTemplate = SkillTemplateManager.GetItemForID(SKILL_TYPE.MAX_HEALTH_BOOST_POT);
            if (healthSkillTemplate != null)
            {
                EntitySkill healthSkill = new EntitySkill(healthSkillTemplate);
                healthSkill.SkillLevel = skillLevel;
                if (skillTemplate.CastTargetGroup != SkillTemplate.CAST_TARGET.ENEMY)
                {
                    m_character.m_zone.UseSkillOnPlayer(0, (int)targetID, healthSkill, m_character.m_player, true);
                }
            }
            //___________________________

            SkillTemplate energySkillTemplate = SkillTemplateManager.GetItemForID(SKILL_TYPE.MAX_ENERGY_BOOST_POT);
            if (energySkillTemplate != null)
            {
                EntitySkill energySkill = new EntitySkill(energySkillTemplate);
                energySkill.SkillLevel = skillLevel;
                if (skillTemplate.CastTargetGroup != SkillTemplate.CAST_TARGET.ENEMY)
                {
                    m_character.m_zone.UseSkillOnPlayer(0, (int)targetID, energySkill, m_character.m_player, true);
                }
            }
            //___________________________
            SkillTemplate attackSkillTemplate = SkillTemplateManager.GetItemForID(SKILL_TYPE.ATT_BOOST_POT);
            if (attackSkillTemplate != null)
            {
                EntitySkill attackSkill = new EntitySkill(attackSkillTemplate);
                attackSkill.SkillLevel = skillLevel;
                if (skillTemplate.CastTargetGroup != SkillTemplate.CAST_TARGET.ENEMY)
                {
                    m_character.m_zone.UseSkillOnPlayer(0, (int)targetID, attackSkill, m_character.m_player, true);
                }
            }
            //___________________________
            SkillTemplate hasteSkillTemplate = SkillTemplateManager.GetItemForID(SKILL_TYPE.ATT_SPD_BOOST_POT);
            if (hasteSkillTemplate != null)
            {
                EntitySkill hasteSkill = new EntitySkill(hasteSkillTemplate);
                hasteSkill.SkillLevel = skillLevel;
                if (skillTemplate.CastTargetGroup != SkillTemplate.CAST_TARGET.ENEMY)
                {
                    m_character.m_zone.UseSkillOnPlayer(0, (int)targetID, hasteSkill, m_character.m_player, true);
                }
            }
            //___________________________
            SkillTemplate travelSkillTemplate = SkillTemplateManager.GetItemForID(SKILL_TYPE.RUN_SPD_BOOST_POT);
            if (travelSkillTemplate != null)
            {
                EntitySkill travelSkill = new EntitySkill(travelSkillTemplate);
                travelSkill.SkillLevel = skillLevel;
                if (skillTemplate.CastTargetGroup != SkillTemplate.CAST_TARGET.ENEMY)
                {
                    m_character.m_zone.UseSkillOnPlayer(0, (int)targetID, travelSkill, m_character.m_player, true);
                }
            }
            //___________________________
            SkillTemplate knowledgeSkillTemplate = SkillTemplateManager.GetItemForID(SKILL_TYPE.EXP_BOOST_POT);
            if (knowledgeSkillTemplate != null)
            {
                EntitySkill knowledgeSkill = new EntitySkill(knowledgeSkillTemplate);
                knowledgeSkill.SkillLevel = skillLevel;
                if (skillTemplate.CastTargetGroup != SkillTemplate.CAST_TARGET.ENEMY)
                {
                    m_character.m_zone.UseSkillOnPlayer(0, (int)targetID, knowledgeSkill, m_character.m_player, true);
                }
            }
            //___________________________
            SkillTemplate wisdomSkillTemplate = SkillTemplateManager.GetItemForID(SKILL_TYPE.ABILITY_BOOST_POT);
            if (wisdomSkillTemplate != null)
            {
                EntitySkill wisdomSkill = new EntitySkill(wisdomSkillTemplate);
                wisdomSkill.SkillLevel = skillLevel;
                if (skillTemplate.CastTargetGroup != SkillTemplate.CAST_TARGET.ENEMY)
                {
                    m_character.m_zone.UseSkillOnPlayer(0, (int)targetID, wisdomSkill, m_character.m_player, true);
                }
            }
            //___________________________
            SkillTemplate energiserSkillTemplate = SkillTemplateManager.GetItemForID(SKILL_TYPE.ENERGY_REGEN_POT);
            if (energiserSkillTemplate != null)
            {
                EntitySkill energiserSkill = new EntitySkill(energiserSkillTemplate);
                energiserSkill.SkillLevel = skillLevel;
                if (skillTemplate.CastTargetGroup != SkillTemplate.CAST_TARGET.ENEMY)
                {
                    m_character.m_zone.UseSkillOnPlayer(0, (int)targetID, energiserSkill, m_character.m_player, true);
                }
            }
            //___________________________
            SkillTemplate regenSkillTemplate = SkillTemplateManager.GetItemForID(SKILL_TYPE.HEALTH_REGEN_POT);
            if (regenSkillTemplate != null)
            {
                EntitySkill regenSkill = new EntitySkill(regenSkillTemplate);
                regenSkill.SkillLevel = skillLevel;
                if (skillTemplate.CastTargetGroup != SkillTemplate.CAST_TARGET.ENEMY)
                {
                    m_character.m_zone.UseSkillOnPlayer(0, (int)targetID, regenSkill, m_character.m_player, true);
                }
            }
            //___________________________

            return true;
        }
        bool UseCombinationElixir1(SkillTemplate skillTemplate, EntitySkill skill, uint targetID, int targetType)
        {

            if (skillTemplate == null)
            {
                return false;
            }
            if (m_character.Dead)
            {
                return false;
            }
            int skillLevel = skill.SkillLevel;
            if (skillTemplate.CastTargetGroup == SkillTemplate.CAST_TARGET.SELF)
            {
                targetID = m_character.m_character_id;
            }
            else if (targetType == (int)Zone.TARGET_TYPE.MOB && skillTemplate.CastTargetGroup == SkillTemplate.CAST_TARGET.FRIENDLY)
            {
                targetID = m_character.m_character_id;
            }
            //check if they have the elixir or potion active
            StatusEffect potStatusEffect = m_character.GetStatusEffectForID(EFFECT_ID.COMBI_POT_1);
            StatusEffect statusEffect = m_character.GetStatusEffectForID(skillTemplate.StatusEffectID);
            if (potStatusEffect != null || (statusEffect != null && statusEffect.m_statusEffectLevel >= skill.SkillLevel))
            {
				string locText = Localiser.GetString(textDB, m_character.m_player, (int)InventoryTextDB.TextID.COMBINATION_EFFECT_ACTIVE);
				Program.processor.sendSystemMessage(locText, m_character.m_player, false, SYSTEM_MESSAGE_TYPE.ITEM_USE);
				return false;
            }
            /* defence, armour, health, energy, offence, haste, travelling, knowledge, energiser, regeneration
                 defence, armour, health, energy, offence, haste, travelling, knowledge, energiser, regeneration*/
            bool didUseSkill = m_character.m_zone.UseSkillOnPlayer(0, (int)targetID, skill, m_character.m_player, true);
            //if it failed to cast then don't cast the effects or consume the item
            if (didUseSkill == false)
            {
                return false;
            }

            SkillTemplate defenceSkillTemplate = SkillTemplateManager.GetItemForID(SKILL_TYPE.DEF_BOOST_ELIXIR);
            if (defenceSkillTemplate != null)
            {
                EntitySkill defenceSkill = new EntitySkill(defenceSkillTemplate);
                defenceSkill.SkillLevel = skillLevel;
                if (skillTemplate.CastTargetGroup != SkillTemplate.CAST_TARGET.ENEMY)
                {
                    m_character.m_zone.UseSkillOnPlayer(0, (int)targetID, defenceSkill, m_character.m_player, true);
                }
            }
            //___________________________
            SkillTemplate armourSkillTemplate = SkillTemplateManager.GetItemForID(SKILL_TYPE.ARM_BOOST_ELIX);
            if (armourSkillTemplate != null)
            {
                EntitySkill armourSkill = new EntitySkill(armourSkillTemplate);
                armourSkill.SkillLevel = skillLevel;
                if (skillTemplate.CastTargetGroup != SkillTemplate.CAST_TARGET.ENEMY)
                {
                    m_character.m_zone.UseSkillOnPlayer(0, (int)targetID, armourSkill, m_character.m_player, true);
                }
            }
            //___________________________
            SkillTemplate healthSkillTemplate = SkillTemplateManager.GetItemForID(SKILL_TYPE.MAX_HEALTH_BOOST_ELIX);
            if (healthSkillTemplate != null)
            {
                EntitySkill healthSkill = new EntitySkill(healthSkillTemplate);
                healthSkill.SkillLevel = skillLevel;
                if (skillTemplate.CastTargetGroup != SkillTemplate.CAST_TARGET.ENEMY)
                {
                    m_character.m_zone.UseSkillOnPlayer(0, (int)targetID, healthSkill, m_character.m_player, true);
                }
            }
            //___________________________

            SkillTemplate energySkillTemplate = SkillTemplateManager.GetItemForID(SKILL_TYPE.MAX_ENERGY_BOOST_ELIX);
            if (energySkillTemplate != null)
            {
                EntitySkill energySkill = new EntitySkill(energySkillTemplate);
                energySkill.SkillLevel = skillLevel;
                if (skillTemplate.CastTargetGroup != SkillTemplate.CAST_TARGET.ENEMY)
                {
                    m_character.m_zone.UseSkillOnPlayer(0, (int)targetID, energySkill, m_character.m_player, true);
                }
            }
            //___________________________
            SkillTemplate attackSkillTemplate = SkillTemplateManager.GetItemForID(SKILL_TYPE.ATT_BOOST_ELIXIR);
            if (attackSkillTemplate != null)
            {
                EntitySkill attackSkill = new EntitySkill(attackSkillTemplate);
                attackSkill.SkillLevel = skillLevel;
                if (skillTemplate.CastTargetGroup != SkillTemplate.CAST_TARGET.ENEMY)
                {
                    m_character.m_zone.UseSkillOnPlayer(0, (int)targetID, attackSkill, m_character.m_player, true);
                }
            }
            //___________________________
            SkillTemplate hasteSkillTemplate = SkillTemplateManager.GetItemForID(SKILL_TYPE.ATT_SPD_BOOST_ELIX);
            if (hasteSkillTemplate != null)
            {
                EntitySkill hasteSkill = new EntitySkill(hasteSkillTemplate);
                hasteSkill.SkillLevel = skillLevel;
                if (skillTemplate.CastTargetGroup != SkillTemplate.CAST_TARGET.ENEMY)
                {
                    m_character.m_zone.UseSkillOnPlayer(0, (int)targetID, hasteSkill, m_character.m_player, true);
                }
            }
            //___________________________
            SkillTemplate travelSkillTemplate = SkillTemplateManager.GetItemForID(SKILL_TYPE.RUN_SPD_BOOST_ELIXIR);
            if (travelSkillTemplate != null)
            {
                EntitySkill travelSkill = new EntitySkill(travelSkillTemplate);
                travelSkill.SkillLevel = skillLevel;
                if (skillTemplate.CastTargetGroup != SkillTemplate.CAST_TARGET.ENEMY)
                {
                    m_character.m_zone.UseSkillOnPlayer(0, (int)targetID, travelSkill, m_character.m_player, true);
                }
            }
            //___________________________
            SkillTemplate knowledgeSkillTemplate = SkillTemplateManager.GetItemForID(SKILL_TYPE.EXP_BOOST_ELIXIR);
            if (knowledgeSkillTemplate != null)
            {
                EntitySkill knowledgeSkill = new EntitySkill(knowledgeSkillTemplate);
                knowledgeSkill.SkillLevel = skillLevel;
                if (skillTemplate.CastTargetGroup != SkillTemplate.CAST_TARGET.ENEMY)
                {
                    m_character.m_zone.UseSkillOnPlayer(0, (int)targetID, knowledgeSkill, m_character.m_player, true);
                }
            }
            //___________________________
            SkillTemplate wisdomSkillTemplate = SkillTemplateManager.GetItemForID(SKILL_TYPE.ABILITY_BOOST_ELIXIR);
            if (wisdomSkillTemplate != null)
            {
                EntitySkill wisdomSkill = new EntitySkill(wisdomSkillTemplate);
                wisdomSkill.SkillLevel = skillLevel;
                if (skillTemplate.CastTargetGroup != SkillTemplate.CAST_TARGET.ENEMY)
                {
                    m_character.m_zone.UseSkillOnPlayer(0, (int)targetID, wisdomSkill, m_character.m_player, true);
                }
            }
            //___________________________
            SkillTemplate energiserSkillTemplate = SkillTemplateManager.GetItemForID(SKILL_TYPE.ENERGY_REGEN_ELIX);
            if (energiserSkillTemplate != null)
            {
                EntitySkill energiserSkill = new EntitySkill(energiserSkillTemplate);
                energiserSkill.SkillLevel = skillLevel;
                if (skillTemplate.CastTargetGroup != SkillTemplate.CAST_TARGET.ENEMY)
                {
                    m_character.m_zone.UseSkillOnPlayer(0, (int)targetID, energiserSkill, m_character.m_player, true);
                }
            }
            //___________________________
            SkillTemplate regenSkillTemplate = SkillTemplateManager.GetItemForID(SKILL_TYPE.HEALTH_REGEN_ELIX);
            if (regenSkillTemplate != null)
            {
                EntitySkill regenSkill = new EntitySkill(regenSkillTemplate);
                regenSkill.SkillLevel = skillLevel;
                if (skillTemplate.CastTargetGroup != SkillTemplate.CAST_TARGET.ENEMY)
                {
                    m_character.m_zone.UseSkillOnPlayer(0, (int)targetID, regenSkill, m_character.m_player, true);
                }
            }
            //___________________________

            return true;
        }

        public bool swapInventoryItem(int inventory_id1,int template_id1,int inventory_id2,int template_id2)
        {
            Item item1 = findBagItemByInventoryID(inventory_id1, template_id1);
            Item item2 = findBagItemByInventoryID(inventory_id2, template_id2);
            if (item1 == null || item2 == null)
                return false;
            int sortorder1 = item1.m_sortOrder;
            item1.m_sortOrder = item2.m_sortOrder;
            item2.m_sortOrder = sortorder1;

            int index1 = m_bagItems.IndexOf(item1);
            int index2 = m_bagItems.IndexOf(item2);
            m_bagItems[index2] = item1;           
            m_bagItems[index1] = item2;

            m_character.m_db.runCommandSync("update " + m_inventoryTableName + " set sort_order=" + item1.m_sortOrder + " where inventory_id=" + item1.m_inventory_id);
            m_character.m_db.runCommandSync("update " + m_inventoryTableName + " set sort_order=" + item2.m_sortOrder + " where inventory_id=" + item2.m_inventory_id);
            return true;

        }

        public float GetCooldownForItem(int itemId)
        {
            Item item = GetItemFromInventoryID(itemId, true);
			if (item == null)
			{
				return 0f;
			}

			return ItemCooldown.GetItemCooldownForId(item.m_template_id);
        }

        public void FavouriteItem(int inventoryId, bool isFavourite)
        {
            var item = GetItemFromInventoryID(inventoryId, true);
            item.IsFavourite = isFavourite;
            byte isFav = Convert.ToByte(isFavourite);
            m_character.m_db.runCommandSync("update " + m_inventoryTableName + " set is_favourite =" + isFav + " where inventory_id=" + inventoryId);
        }

        public void RemoveTokenCostForCustomisation(int tokenID, int cost)
        {
            
            
            //Iterate through player inventory


            for (int i = m_bagItems.Count - 1; i > -1; i--)
            {
                var bagItem = m_bagItems[i];
                //When we find the token
                if (bagItem.m_template_id == tokenID)
                {
                    if (bagItem.m_quantity - cost > 0)
                    {
                        bagItem.m_quantity -= cost;
                        //Remove the correct number of tokens and sync with db.
                        m_character.m_db.runCommandSync("update inventory set quantity=" + bagItem.m_quantity +
                                                        " where inventory_id=" +
                                                        bagItem.m_inventory_id);
                    }
                    else
                    {
                        bagItem.m_quantity -= cost;
                        //Remove the correct number of tokens and sync with db.
                        m_character.m_db.runCommandSync("delete from inventory where inventory_id=" +
                                                        bagItem.m_inventory_id);
                        m_bagItems.Remove(bagItem);
                   
                    }



                    return;

                }
            }
        }

        public bool RemoveTokenCostForItem(int vendorId, List<TokenVendorCost> tokenVendorStockCost, int quantity)
        {
            var tokenVendorStockCostCopy =
                tokenVendorStockCost.ConvertAll(
                    TokenVendorCost => new TokenVendorCost(TokenVendorCost.ItemTemplateId, TokenVendorCost.Quantity));


            foreach (TokenVendorCost cost in tokenVendorStockCostCopy)
            {
                cost.MultiplyQuantityBy(quantity); // multiply the cost by how many we're buying
            }

            for (int i = m_bagItems.Count - 1; i > -1; i--)
            {
                var bagItem = m_bagItems[i];
                for (int j = 0; j < tokenVendorStockCostCopy.Count; j++)
                {
                    TokenVendorCost tokenVendorCost = tokenVendorStockCostCopy[j];
                    if (bagItem.m_template_id == tokenVendorCost.ItemTemplateId)
                    {
                        int removed = tokenVendorCost.Quantity;
                        StringBuilder comment = new StringBuilder();
                        comment.Append("Purchased ").Append(bagItem.m_template_id).Append(" from ").Append(vendorId);

                        if (tokenVendorCost.Quantity == bagItem.m_quantity)
                        {
                            Program.processor.updateShopHistory(-3, -3, bagItem.m_inventory_id, bagItem.m_template_id, -removed, 0, (int)m_character.m_character_id, comment.ToString());
                            m_character.m_db.runCommandSync("delete from " + m_inventoryTableName + " where inventory_id=" + bagItem.m_inventory_id);

                            m_bagItems.Remove(bagItem);

                            tokenVendorStockCostCopy.Remove(tokenVendorCost); // we've removed the item & paid the cost
                            j--;
                        }
                        else if (bagItem.m_quantity > tokenVendorCost.Quantity)
                        {
                            Program.processor.updateShopHistory(-2, -2, bagItem.m_inventory_id, bagItem.m_template_id, -removed, 0, (int)m_character.m_character_id, comment.ToString());
                            bagItem.m_quantity -= tokenVendorStockCostCopy[j].Quantity;
                            m_character.m_db.runCommandSync("update " + m_inventoryTableName + " set quantity=" + bagItem.m_quantity + " where inventory_id=" + bagItem.m_inventory_id);
                            tokenVendorStockCostCopy.Remove(tokenVendorCost); // we've removed the item & paid the cost
                            j--;
                        }
                        else if (bagItem.m_quantity < tokenVendorCost.Quantity)
                        {
                            removed = bagItem.m_quantity;

                            Program.processor.updateShopHistory(-3, -3, bagItem.m_inventory_id, bagItem.m_template_id, -removed, 0, (int)m_character.m_character_id, comment.ToString());
                            m_character.m_db.runCommandSync("delete from " + m_inventoryTableName + " where inventory_id=" + bagItem.m_inventory_id);

                            m_bagItems.Remove(bagItem);

                            tokenVendorStockCostCopy[j].ReduceQuantity(removed);
                        }
                    }
                }
            }

            if (tokenVendorStockCostCopy.Count == 0) // no remaining cost to pay
            {
                return true;
            }

            Debug.Print("this is an error, we shouldnt be here!");
            return false; // won't this mean there's cost still to pay but we've already removed some? do we need to reset them?
        }

		#region companion hungry helpers


		/// <summary>
		/// Helper method, once all statuses have been updated check if we need to enable/disable a skill
		/// from the companion item
		/// </summary>
	    private void updateCompanionSkillBasedOnStatus()
	    {
		    //if no equipped we can bail, no check required
			if(companionEquipped() == false)
				return;

			//equally, if companion has no skill we can bail again, no further checking
			int skillID = companionHasSkill();
			if(skillID <=0)
				return;
			
			//we have a companion and a skill...enable/disable based on hunger 
			//(if hungry skill disabled and vice versa)
			bool skillOn = !hungryEffectPresent();
			//update the player			
			Program.processor.SendPlayerCompanionSkillToggled(skillID, skillOn, m_character.m_player);

	    }

        /// <summary>
        /// Helper method, once all statuses have been updated check if we need to enable/disable a skill
        /// from the mount item
        /// </summary>
        private void updateMountSkillBasedOnStatus()
        {
            //if no equipped we can bail, no check required
            if (mountEquipped() == false)
                return;

            //equally, if companion has no skill we can bail again, no further checking
            int skillID = mountHasSkill();
            if (skillID <= 0)
                return;

            //we have a companion and a skill...enable/disable based on hunger 
            //(if hungry skill disabled and vice versa)
            bool skillOn = !dismountedEffectPresent();
            //update the player			
            Program.processor.SendPlayerMountSkillToggled(skillID, skillOn, m_character.m_player);

        }


		/// <summary>
		/// Go through current character status effects and check if any are hungry (i.e. effecttype = 48)
		/// </summary>
		/// <returns>true is effect_type.hungry_pet exists</returns>
	    private bool hungryEffectPresent()
	    {
			foreach (CharacterEffect charEffect in this.m_character.m_currentCharacterEffects)
			{
				if (charEffect.StatusEffect.Template.EffectType == EFFECT_TYPE.PET_HUNGRY)
				{					
					return true;
				}
			}
			
			return false;
	    }


        /// <summary>
        /// Go through current character status effects and check if any are dismounted
        /// </summary>
        /// <returns>true is effect_type.hungry_pet exists</returns>
        private bool dismountedEffectPresent()
        {
            foreach (CharacterEffect charEffect in this.m_character.m_currentCharacterEffects)
            {
                if (charEffect.StatusEffect.Template.EffectType == EFFECT_TYPE.DISMOUNTED)
                {
                    return true;
                }
            }

            return false;
        }

		/// <summary>
		/// check if this entity has a companion pet equipped
		/// </summary>
		/// <returns>true if one is equipped</returns>
	    private bool companionEquipped()
		{
			Item equippedCompanion = GetEquipmentForSlot((int)EQUIP_SLOT.SLOT_COMPANION);
			if (equippedCompanion != null)
				return true;

			return false;
		}

        /// <summary>
        /// check if this entity has a mount equipped
        /// </summary>
        /// <returns>true if one is equipped</returns>
        private bool mountEquipped()
        {
            Item equippedMount = GetEquipmentForSlot((int)EQUIP_SLOT.SLOT_MOUNT);
            if (equippedMount != null)
                return true;

            return false;
        }
        
		/// <summary>
		/// Find the skill that this companion grants - or -1 if nothing
		/// </summary>
		/// <returns>-1 for no skill OR correct skill id</returns>
	    private int companionHasSkill()
	    {
			Item equippedCompanion = GetEquipmentForSlot((int)EQUIP_SLOT.SLOT_COMPANION);
		    if (equippedCompanion == null)
		    {
			    return -1;
		    }

			//get skill
		    ItemTemplate itemTemplate = equippedCompanion.m_template;
		    int skillID = itemTemplate.m_equipSkillID;
			if (skillID <= 0)							
				return -1;
		
			return skillID;
	    }



        /// <summary>
        /// Find the skill that this companion grants - or -1 if nothing
        /// </summary>
        /// <returns>-1 for no skill OR correct skill id</returns>
        private int mountHasSkill()
        {
            Item equippedMount = GetEquipmentForSlot((int)EQUIP_SLOT.SLOT_MOUNT);
            if (equippedMount == null)
            {
                return -1;
            }

            //get skill
            ItemTemplate itemTemplate = equippedMount.m_template;
            int skillID = itemTemplate.m_equipSkillID;
            if (skillID <= 0)
                return -1;

            return skillID;
        }

		/// <summary>
		/// For a given skill, check if this is related to our companion/hunger system.
		/// </summary>
		/// <param name="entitySkill"></param>
		/// <param name="player"></param>
		/// <returns>true if valid - false if this is a pet skill & we're hungry</returns>
		internal bool CheckSkillValidForCompanionHunger(EntitySkill entitySkill, Player player)
	    {
			//if no equipped we can bail, no check required
			if (companionEquipped() == false)
				return true;

			//equally, if companion has no skill we can bail again, no further checking
			int skillID = companionHasSkill();
			if (skillID <= 0)
				return true;

			//we have a companion and a skill...enable/disable based on hunger 
			//(if hungry skill disabled and vice versa)
			bool skillOn = !hungryEffectPresent();

			//we've found a skill on this character that is related to hunger
			//now check agains the skill we've received. 
			if ((int) entitySkill.SkillID == skillID)
				return skillOn;

			return true;
	    }


        /// <summary>
        /// For a given skill, check if this is related to our companion/hunger system.
        /// </summary>
        /// <param name="entitySkill"></param>
        /// <param name="player"></param>
        /// <returns>true if valid - false if this is a pet skill & we're hungry</returns>
        internal bool CheckSkillValidDismounted(EntitySkill entitySkill, Player player)
        {
            //if no equipped we can bail, no check required
            if (mountEquipped() == false)
                return true;

            //equally, if companion has no skill we can bail again, no further checking
            int skillID = mountHasSkill();
            if (skillID <= 0)
                return true;

            //we have a companion and a skill...enable/disable based on hunger 
            //(if hungry skill disabled and vice versa)
            bool skillOn = !dismountedEffectPresent();

            //we've found a skill on this character that is related to hunger
            //now check agains the skill we've received. 
            if ((int)entitySkill.SkillID == skillID)
                return skillOn;

            return true;
        }

		#endregion
	}

}
