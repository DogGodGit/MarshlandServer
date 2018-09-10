using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using Lidgren.Network;

namespace MainServer
{
    class Shop
    {
        public  const string ShopNotFound = "shop not found";
        public const string ShopRestricted = "you don't meet the requirements for this shop";

        public int m_shop_id;
        int m_zone_id;
        public string m_shop_name;
        public int m_npc_id;
        List<ShopItem> shopItems;
        Database m_db;

        LootTable m_lootTable = null;
        int m_loot_table_quantity;
        List<ShopItem> m_lootTableItems;
        List<ShopSubtypeMultipliers> m_subtypeMultipliers;

        /// <summary>
        /// Restrict usage of this shop to a particular player class
        /// </summary>
        public CLASS_TYPE ClassRestriction { get; private set; }

        /// <summary>
        /// Restrict usage of this shop to certain factions
        /// </summary>
        public int FactionIDRestriction { get; private set; }

        /// <summary>
        /// Faction level required for this shop stock
        /// </summary>
        public int FactionLevelRestriction { get; private set; }

        public Shop(Database db, int shop_id, int zone_id, string shop_name, int npc_id, int loot_table_id, int loot_table_quantity, int class_id, int faction_id, int faction_level)
        {
            m_db = db;
            m_shop_id = shop_id;
            m_zone_id = zone_id;
            m_shop_name = shop_name;
            m_npc_id = npc_id;
            
            this.ClassRestriction = (CLASS_TYPE) class_id;

            this.FactionIDRestriction = faction_id;
            this.FactionLevelRestriction = faction_level;

            shopItems = new List<ShopItem>();
            m_lootTableItems = new List<ShopItem>();
            m_subtypeMultipliers = new List<ShopSubtypeMultipliers>();
            SqlQuery itemQuery = new SqlQuery(db, "select * from shop_stock where shop_id=" + shop_id + " and zone_id=" + zone_id + " order by sort_order,item_id");
            if (itemQuery.HasRows)
            {
                while (itemQuery.Read())
                {

                    ShopItem shopitem = new ShopItem(itemQuery.GetInt32("item_id"), itemQuery.GetInt32("stock_level"));
                    shopItems.Add(shopitem);
                }
            }
            itemQuery.Close();
            m_loot_table_quantity = loot_table_quantity;
            if (loot_table_id != 10 && loot_table_quantity > 0)
            {
                m_lootTable = LootSetManager.getLootTable(loot_table_id);
                refreshLootTable();
            }
            SqlQuery subtypeQuery = new SqlQuery(db, "select * from shop_subtype_prices where shop_id=" + shop_id + " and zone_id=" + zone_id);

            while (subtypeQuery.Read())
            {

                ShopSubtypeMultipliers newSubtype = new ShopSubtypeMultipliers(subtypeQuery);
                m_subtypeMultipliers.Add(newSubtype);
            }

            subtypeQuery.Close();
        }


        /// <summary>
        /// Certain shops are restricted by class or faction, check against these now.
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        public bool CharacterMeetsRequirment(Character character)
        {
            //incorrect class
            if (ClassRestriction != CLASS_TYPE.UNDEFINED)
            {
                if(character.m_class.m_classType != ClassRestriction)
                    return false;
            }

            //not high enough in faction
            if (FactionIDRestriction != 0)
            {
                if (character.FactionManager.HasFactionLevel(this.FactionIDRestriction, this.FactionLevelRestriction) == false)
                    return false;
            }

            //all checks passed ok
            return true;
        }

        public float getBuyMultiplier(ItemTemplate.ITEM_SUB_TYPE subType)
        {
            float multiplier = 1.0f;
            for (int i = 0; i < m_subtypeMultipliers.Count; i++)
            {
                if (m_subtypeMultipliers[i].m_subType == subType)
                {
                    multiplier = m_subtypeMultipliers[i].m_buyMultiplier;
                    break;
                }
            }
            return multiplier;
        }

        public float getSellMultiplier(ItemTemplate.ITEM_SUB_TYPE subType)
        {
            float multiplier = 1.0f;
            for (int i = 0; i < m_subtypeMultipliers.Count; i++)
            {
                if (m_subtypeMultipliers[i].m_subType == subType)
                {
                    multiplier = m_subtypeMultipliers[i].m_sellMultiplier;
                    break;
                }
            }
            return multiplier;
        }
        
        public int buyItem(int templateID, int quantity, int playercoins)
        {
            for (int i = 0; i < shopItems.Count; i++)
            {
                if (templateID == shopItems[i].getTemplateID())
                    return shopItems[i].buyItem(quantity, playercoins,getSellMultiplier(shopItems[i].getTemplate().m_subtype));
            }
            for (int i = 0; i < m_lootTableItems.Count; i++)
            {
                if (templateID == m_lootTableItems[i].getTemplateID())
                {
                    int cost = m_lootTableItems[i].buyItem(quantity, playercoins, getSellMultiplier(m_lootTableItems[i].getTemplate().m_subtype));
                    if (cost >= 0)
                    {
                        m_lootTableItems.RemoveAt(i);

                    }
                    return cost;
                }
            }
            return -3;// no such item in shop
        }
        
        public void writeShopStockToMessage(NetOutgoingMessage msg)
        {
            msg.WriteVariableInt32(shopItems.Count() + m_lootTableItems.Count());
            for (int i = 0; i < shopItems.Count(); i++)
            {
                shopItems[i].writeItemToMessage(msg);
            }
            for (int i = 0; i < m_lootTableItems.Count(); i++)
            {
                m_lootTableItems[i].writeItemToMessage(msg);
            }
        }
        
        public void appendShopPrices(List<int> itemList)
        {
            for (int i = 0; i < shopItems.Count; i++)
            {
                bool found = false;
                for (int j = 0; j < itemList.Count; j++)
                {
                    if (itemList[j] == shopItems[i].getTemplateID())
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    itemList.Add(shopItems[i].getTemplateID());
                }
            }
            for (int i = 0; i < m_lootTableItems.Count; i++)
            {
                bool found = false;
                for (int j = 0; j < itemList.Count; j++)
                {
                    if (itemList[j] == m_lootTableItems[i].getTemplateID())
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    itemList.Add(m_lootTableItems[i].getTemplateID());
                }
            }
        }

        internal int sellItem(int templateID, int quantity)
        {
            ItemTemplate itemTemplate = ItemTemplateManager.GetItemForID(templateID);
            return (int)Math.Ceiling(itemTemplate.m_buyprice*getBuyMultiplier(itemTemplate.m_subtype)) * quantity;
        }

        internal void fillShop()
        {
            if (m_lootTableItems.Count < m_loot_table_quantity)
            {
                for (int i = m_lootTableItems.Count; i < m_loot_table_quantity; i++)
                {
                    addLootTableItem();
                }
            }
        }

        internal void refreshLootTable()
        {
            m_lootTableItems.Clear();
            for (int i = 0; i < m_loot_table_quantity; i++)
            {
                addLootTableItem();
            }
        }
        
        internal void addLootTableItem()
        {
            int item_template_id = -1;
            while (item_template_id < 0 && m_lootTableItems.Count() < m_lootTable.m_lootTableItems.Count())
            {
                item_template_id = m_lootTable.getLootItemID();
                for (int i = 0; i < m_lootTableItems.Count; i++)
                {
                    if (m_lootTableItems[i].getTemplateID() == item_template_id)
                    {
                        item_template_id = -1;
                        break;
                    }
                }
            }
            m_lootTableItems.Add(new ShopItem(item_template_id, -1));
        }
    }
}
