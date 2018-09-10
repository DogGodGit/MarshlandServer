using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MainServer;

namespace Analytics.Global
{
    /*public class SharedObjects
    {}*/
    
    //Two Letter Codes for each Country
    public enum UserCountry
    { };

    //Two Letter Codes for each Region
    public enum UserRegion
    { };

    public enum RewardType
    { itemsOnly, currencyOnly, both, none, };

    public class Reward
    {
        public string rewardName = "Reward";
        public object rewardProducts;

        public Reward(RewardType i_rewardType)
        {
            switch (i_rewardType)
            {
                case RewardType.itemsOnly:
                    rewardProducts = new Products_Items();
                    break;
                case RewardType.currencyOnly:
                    rewardProducts = new Products_VC();
                    break;
                case RewardType.both:
                    rewardProducts = new Products_Both();
                    break;
                case RewardType.none:
                    rewardProducts = new object();
                    break;
            }
        }

        public Reward(string i_rewardName)
        {
            rewardName = i_rewardName; 
        }

        public Products_Items getProductsItem()
        {
            return (Products_Items)rewardProducts;
        }

        public Products_VC getProductsVC()
        {
            return (Products_VC)rewardProducts;
        }

        public Products_Both getProductsBoth()
        {
            return (Products_Both)rewardProducts;
        }
    }

    public class Products_VC //dynamic object
    {
        public List<VirtualCurrencies> virtualCurrencies = new List<VirtualCurrencies>();
        public Products_VC()
        {}

        public void addVirtualCurrency(string i_virtualCurrencyName, string i_virtualCurrencyType, int i_virtualCurrencyAmount)
        {
            VirtualCurrencies vc = new VirtualCurrencies();
            vc.virtualCurrency.virtualCurrencyName = i_virtualCurrencyName;
            vc.virtualCurrency.virtualCurrencyType = i_virtualCurrencyType;
            vc.virtualCurrency.virtualCurrencyAmount = i_virtualCurrencyAmount;
            virtualCurrencies.Add(vc);
        }
    }

    public class Products_Items//dynamic object
    {
        public List<Items> items;//will be an array

        public Products_Items()
        {
            items = new List<Items>();
        }

        public void addItem(string i_itemName, string i_itemType, int i_itemAmount)
        {
            Items item = new Items();
            item.item.itemName = i_itemName;
            item.item.itemType = i_itemType;
            item.item.itemAmount = i_itemAmount;
            items.Add(item);
        }
        internal void AddLoot(List<LootDetails> loot)
        {

            for (int i = 0; i < loot.Count; i++)
            {
                LootDetails tempItem = loot[i];
                ItemTemplate itemTemplate = ItemTemplateManager.GetItemForID(tempItem.m_templateID);
                if (itemTemplate != null)
                {
                    int quantity = tempItem.m_quantity;
                    string name = itemTemplate.m_item_name;
                    string type = itemTemplate.m_subtype.ToString();

                    addItem(name, type, quantity);
                }
            }
        }
    }

    public class Products_Both//dynamic object
    {
        public List<VirtualCurrencies> virtualCurrencies = new List<VirtualCurrencies>();
        public List<Items> items = new List<Items>();

        public Products_Both()
        {}

        public void addVirtualCurrency(string i_virtualCurrencyName, string i_virtualCurrencyType, int i_virtualCurrencyAmount)
        {
            VirtualCurrencies vc = new VirtualCurrencies();
            vc.virtualCurrency.virtualCurrencyName = i_virtualCurrencyName;
            vc.virtualCurrency.virtualCurrencyType = i_virtualCurrencyType;
            vc.virtualCurrency.virtualCurrencyAmount = i_virtualCurrencyAmount;
            virtualCurrencies.Add(vc);
        }

        public void addItem(string i_itemName, string i_itemType, int i_itemAmount)
        {
            Items item = new Items();
            item.item.itemName = i_itemName;
            item.item.itemType = i_itemType;
            item.item.itemAmount = i_itemAmount;
            items.Add(item);
        }
        internal void AddLoot(List<LootDetails> loot)
        {

            for (int i = 0; i < loot.Count; i++)
            {
                LootDetails tempItem = loot[i];
                ItemTemplate itemTemplate = ItemTemplateManager.GetItemForID(tempItem.m_templateID);
                if (itemTemplate != null)
                {
                    int quantity = tempItem.m_quantity;
                    string name = itemTemplate.m_item_name;
                    string type = itemTemplate.m_subtype.ToString();

                    addItem(name, type, quantity);
                }
            }
        }
    }

    public class RealCurrency
    {
        public string realCurrencyType = "";
        public int realCurrencyAmount = -1;
        
        public RealCurrency()
        {}

        public RealCurrency(string i_realCurrencyType, int i_realCurrencyAmount)
        {
            realCurrencyType   = i_realCurrencyType;
            realCurrencyAmount = i_realCurrencyAmount;
        }
    }

    public class VirtualCurrencies
    {
        public VirtualCurrency virtualCurrency = new VirtualCurrency();
        public VirtualCurrencies()
        { }
    }

    public class VirtualCurrency
    {
        public string virtualCurrencyName = "";
        public string virtualCurrencyType = "";
        public int virtualCurrencyAmount = -1;

        public VirtualCurrency()
        { }

        public VirtualCurrency(string i_virtualCurrencyName, string i_virtualCurrencyType, int i_virtualCurrencyAmount)
        {
            virtualCurrencyName = i_virtualCurrencyName;
            virtualCurrencyType = i_virtualCurrencyType;
            virtualCurrencyAmount = i_virtualCurrencyAmount;
        }
    }

    public class BaseEventParams
    {
        public int fishingLevel;
        public int cookingLevel;
        public string serverName;
        public string characterClass;
        public string characterID;

        public BaseEventParams()
        {
        }

        internal void SetBaseValues(Player player, string serverID)
        {
            fishingLevel = player.m_activeCharacter.LevelFishing;
            cookingLevel = player.m_activeCharacter.LevelCooking;
            serverName = serverID; //program.ServerID;
            characterClass = player.m_activeCharacter.m_class.m_classType.ToString(); // player.m_activeCharacter.m_class.m_classType;
            characterID = player.m_activeCharacter.m_character_id.ToString();
        }
    }
    public class Items
    {
        public Item item;

        public Items()
        {
            item = new Item();
        }
    }

    public class Item
    {
        public string itemName = "";
        public string itemType = "";
        public int itemAmount = -1;

        public Item()
        { }

        public Item(string i_itemName, string i_itemType, int i_itemAmount)
        {
            itemName = i_itemName;
            itemType = i_itemType;
            itemAmount = i_itemAmount;
        }
    }

    public class Receipient
    {
        public string recipientID = "";

        public Receipient()
        { }

        public Receipient(string i_recipientID)
        {
            recipientID = i_recipientID;
        }
    }

    public class Participants
    {
        public Participant participant = new Participant();

        public Participants()
        { }
    }

    public class Participant
    {
        public string characterID = "";
        public string characterName = "";
        public int characterLevel = -1;

        public Participant()
        { }

        public Participant(string i_playerID, string i_playerName, int i_playerLevel)
        {
            characterID = i_playerID;
            characterName = i_playerName;
            characterLevel = i_playerLevel;
        }
    }

}
