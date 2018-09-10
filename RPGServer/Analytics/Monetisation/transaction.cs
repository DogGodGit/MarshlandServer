using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Analytics.Global;


//Buying premium content
//Buying shop items
//User trades
namespace Analytics.Monetisation
{
    public enum TransactionItem
    { realCurrency = 0, virtualCurrency = 1, items = 2, vcAndItems = 3 };

    public enum TransactionServer
    { APPLE, AMAZON, GOOGLE };

    public enum VirtualCurrencyType
    { GRIND, PREMIUM, PREMIUM_GRIND, TOKEN };
    /*
    US = $x.xx - USD
    Mexico = $x.xx
    Canada = CA$x.xx - CAD
    UK = £x.xx - GBP
    European Union = x,xx € - EUR
    Norway = x.xxKr(NO) - NOK
    Sweden = x.xxKr(SE) - SEK
    Denmark = x.xxKr(DK) - DKK
    Switzerland = x.xxFr - CHF
    Australia = AU$x.xx - AUD
    New Zealand = NZ$x.xx - NZD
    Japan = ¥xxx - JPY
    Russia: Ruble - RUB
    Turkey: Lira - TRY
    India: Rupee - INR
    Indonesian: Rupiah - IDR
    Israel: New Shekel - ILS
    Saudi Arabia: Riyal - SAR
    South Africa: Rand - ZAR
    United Arab Emirates: Dirham - AED
     */
    public enum RealCurrencyType
    { USD, GBP };

    public enum PaymentCountry
    { US, GB };

    public enum TransactionType
    { PURCHASE, SALE, TRADE };

    public class Transaction
    {
        public string eventName = "transaction";
        public string userID = "";
        public string sessionID = "";
        public string eventTimestamp = DateTime.Now.ToString();
        public eventParams_transaction eventParams;
        public GoalCounts goalCounts = new GoalCounts();

        public Transaction(TransactionItem i_productSpentEnum, TransactionItem i_productReceivedEnum)
        {
            eventParams = new eventParams_transaction(i_productSpentEnum, i_productReceivedEnum);
        }

        public Transaction(string i_userID, string i_sessionID, TransactionItem i_productSpentEnum, TransactionItem i_productReceivedEnum, string i_eventTimestamp)
        {
            userID = i_userID;
            sessionID = i_sessionID;
            eventParams = new eventParams_transaction(i_productSpentEnum, i_productReceivedEnum);
            eventTimestamp = i_eventTimestamp;
        }

        /*public void setGoalCounts(int i_userLevel, int i_XP, int i_health, int i_energy, int i_strength, int i_dexterity, int i_focus,
                                    int i_vitality, int i_attack, int i_defence, int i_damage, int i_armour, int i_gold, int i_platinum)
        {
            //goalCounts
        }*/
    }

    public class eventParams_transaction
    {
        public string transactionName = "";
        public string transactionID = "";
        public string transactorID = "";
        //public string productID = "";
        public string transactionReceipt = "";
        public string transactionServer = "";
        public string paymentCountry = PaymentCountry.US.ToString();
        public bool isInitiator = false;
        public string transactionType = "";

        public object productsSpent;
        public object productsReceived;

        public eventParams_transaction(TransactionItem i_productSpentEnum, TransactionItem i_productReceivedEnum)
        {
            checkTransaction(i_productSpentEnum, i_productReceivedEnum);//allocate the correct objects to be serialised
        }

        public eventParams_transaction(TransactionItem i_productSpentEnum, TransactionItem i_productReceivedEnum, string i_transactionName, string i_transactionID, string i_transacteeId, string i_productID, string i_transactionReceipt, string i_transactionServer,
                                        string i_paymentCountry, bool i_isInitiator, string i_transactionType)
        {
            transactionName = i_transactionName;
            transactionID = i_transactionID;
            //productID = i_productID;
            transactionReceipt = i_transactionReceipt;
            transactionServer = i_transactionServer;
            paymentCountry = i_paymentCountry;
            isInitiator = i_isInitiator;
            transactionType = i_transactionType;
            checkTransaction(i_productSpentEnum, i_productReceivedEnum);//allocate the correct objects to be serialised
        }

        private void checkTransaction(TransactionItem productSpentEnum, TransactionItem productReceivedEnum)
        {
            switch (productSpentEnum)//set final object type
            {
                case TransactionItem.realCurrency://Real
                    {
                        productsSpent = new ProductsSpent_RC();
                        break;
                    }

                case TransactionItem.virtualCurrency://Virtual
                    {
                        productsSpent = new ProductsSpent_VC();
                        break;
                    }
                case TransactionItem.items://Items
                    {
                        productsSpent = new ProductsSpent_Items();
                        break;
                    }
                case TransactionItem.vcAndItems:
                    {
                        productsSpent = new ProductsSpent_VCAndItems();
                        break;
                    }
            }

            switch (productReceivedEnum)//set final object type
            {
                case TransactionItem.virtualCurrency://Virtual
                    {
                        productsReceived = new ProductsReceived_VC();
                        break;
                    }
                case TransactionItem.items://Items
                    {
                        productsReceived = new ProductsReceived_Items();
                        break;
                    }
                case TransactionItem.vcAndItems:
                    {
                        productsReceived = new ProductsReceived_VCAndItems();
                        break;
                    }
            }
        }

        //return the cast value of the object, this relies on the coder returning the correct value
        //this is to allow dynamic allocation of the object whilst allowing access to the objects members
        public ProductsSpent_RC getProductsSpentRC()
        { return (ProductsSpent_RC)productsSpent; }

        public ProductsSpent_VC getProductsSpentVC()
        { return (ProductsSpent_VC)productsSpent; }

        public ProductsSpent_Items getProductsSpentItems()
        { return (ProductsSpent_Items)productsSpent; }

        public ProductsSpent_VCAndItems getProductsSpentVCAndItems()
        { return (ProductsSpent_VCAndItems)productsSpent; }


        public ProductsReceived_VC getProductsReceivedVC()
        { return (ProductsReceived_VC)productsReceived; }

        public ProductsReceived_Items getProductsReceivedItems()
        { return (ProductsReceived_Items)productsReceived; }

        public ProductsReceived_VCAndItems getProductsReceivedVCAndItems()
        { return (ProductsReceived_VCAndItems)productsReceived; }
    }

    public class ProductsSpent_RC
    {
        public RealCurrency realCurrency;
        public ProductsSpent_RC()
        {
            realCurrency = new RealCurrency();
        }
    }

    public class ProductsSpent_VC
    {
        public List<VirtualCurrencies> virtualCurrencies = new List<VirtualCurrencies>();//will be an array

        public ProductsSpent_VC()
        { }

        public void addItem(string i_virtualCurrencyName, string i_virtualCurrencyType, int i_virtualCurrencyAmount)
        {
            VirtualCurrencies vc = new VirtualCurrencies();
            vc.virtualCurrency.virtualCurrencyName = i_virtualCurrencyName;
            vc.virtualCurrency.virtualCurrencyType = i_virtualCurrencyType;
            vc.virtualCurrency.virtualCurrencyAmount = i_virtualCurrencyAmount;
            virtualCurrencies.Add(vc);
        }
    }

    public class ProductsSpent_Items
    {
        public List<Items> items = new List<Items>();//will be an array

        public ProductsSpent_Items()
        { }

        public void addItem(string i_itemName, string i_itemType, int i_itemAmount)
        {
            Items item = new Items();
            item.item.itemName = i_itemName;
            item.item.itemType = i_itemType;
            item.item.itemAmount = i_itemAmount;
            items.Add(item);
        }
    }

    public class ProductsSpent_VCAndItems
    {
        public List<VirtualCurrencies> virtualCurrencies = new List<VirtualCurrencies>();//will be an array
        public List<Items> items = new List<Items>();//will be an array

        public ProductsSpent_VCAndItems()
        { }

        public void addVC(string i_virtualCurrencyName, string i_virtualCurrencyType, int i_virtualCurrencyAmount)
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
    }

    public class ProductsReceived_VC
    {
        public List<VirtualCurrencies> virtualCurrencies;//will be an array

        public ProductsReceived_VC()
        {
            virtualCurrencies = new List<VirtualCurrencies>();
        }

        public void addItem(string i_virtualCurrencyName, string i_virtualCurrencyType, int i_virtualCurrencyAmount)
        {
            VirtualCurrencies vc = new VirtualCurrencies();
            vc.virtualCurrency.virtualCurrencyName = i_virtualCurrencyName;
            vc.virtualCurrency.virtualCurrencyType = i_virtualCurrencyType;
            vc.virtualCurrency.virtualCurrencyAmount = i_virtualCurrencyAmount;
            virtualCurrencies.Add(vc);
        }
    }

    public class ProductsReceived_Items
    {
        public List<Items> items;//will be an array

        public ProductsReceived_Items()
        {
            items = new List<Items>();
            //items.Add(item);
        }

        public void addItem(string i_itemName, string i_itemType, int i_itemAmount)
        {
            Items item = new Items();
            item.item.itemName = i_itemName;
            item.item.itemType = i_itemType;
            item.item.itemAmount = i_itemAmount;
            items.Add(item);
        }
    }

    public class ProductsReceived_VCAndItems
    {
        public List<VirtualCurrencies> virtualCurrencies = new List<VirtualCurrencies>();//will be an array
        public List<Items> items = new List<Items>();//will be an array

        public ProductsReceived_VCAndItems()
        { }

        public void addVC(string i_virtualCurrencyName, string i_virtualCurrencyType, int i_virtualCurrencyAmount)
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
    }
}
