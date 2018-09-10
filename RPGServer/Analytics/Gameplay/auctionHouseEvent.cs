using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Analytics.Global;
using Analytics.Monetisation;

namespace Analytics.Gameplay
{
    public enum AuctionedProduct
    {
        NULL = 0,
        virtualCurrency = 1,
        items = 2,
        vcAndItems = 3
    };

    class AuctionHouseEvent
    {
        public string userID         = String.Empty;
        public string eventTimestamp = DateTime.Now.ToString();
        public eventParams_auctionHouseEvent eventParams;

        public AuctionHouseEvent(AuctionedProduct i_productSpentEnum, AuctionedProduct i_productReceivedEnum)
        {
            eventParams = new eventParams_auctionHouseEvent(i_productSpentEnum, i_productReceivedEnum);
        }

        public AuctionHouseEvent(string i_userID, AuctionedProduct i_productSpentEnum, AuctionedProduct i_productReceivedEnum, string i_eventTimestamp)
        {
            userID         = i_userID;
            eventParams    = new eventParams_auctionHouseEvent(i_productSpentEnum, i_productReceivedEnum);
            eventTimestamp = i_eventTimestamp;
        }
    }

    public class eventParams_auctionHouseEvent
    {
        public string transactionName = String.Empty;
        public string auctionLotID    = String.Empty;
        public string serverID;

        public string productID;
        public object productsSpent;
        public object productsReceived;
      
        public eventParams_auctionHouseEvent(AuctionedProduct i_productSpentEnum, AuctionedProduct i_productReceivedEnum)
        {
            checkTransaction(i_productSpentEnum, i_productReceivedEnum); //allocate the correct objects to be serialised
        }

        private void checkTransaction(AuctionedProduct productSpentEnum, AuctionedProduct productReceivedEnum)
        {
            switch (productSpentEnum)
            {
                case AuctionedProduct.NULL:
                {
                    productsSpent = new ProductsSpent_NULL();
                    break;
                }
                case AuctionedProduct.virtualCurrency:
                {
                    productsSpent = new ProductsSpent_VC();
                    break;
                }
                case AuctionedProduct.items:
                {
                    productsSpent = new ProductsSpent_Items();
                    break;
                }
                case AuctionedProduct.vcAndItems:
                {
                    productsSpent = new ProductsSpent_VCAndItems();
                    break;
                }
            }

            switch (productReceivedEnum)
            {
                case AuctionedProduct.NULL:
                {
                    productsReceived = new ProductsReceived_NULL();
                    break;
                }
                case AuctionedProduct.virtualCurrency:
                {
                    productsReceived = new ProductsReceived_VC();
                    break;
                }
                case AuctionedProduct.items:
                {
                    productsReceived = new ProductsReceived_Items();
                    break;
                }
                case AuctionedProduct.vcAndItems:
                {
                    productsReceived = new ProductsReceived_VCAndItems();
                    break;
                }
            }
        }

        //return the cast value of the object, this relies on the coder returning the correct value
        //this is to allow dynamic allocation of the object whilst allowing access to the objects members
        public ProductsSpent_VC getProductsSpentVC()
        {
            return (ProductsSpent_VC)productsSpent; 
        }

        public ProductsSpent_Items getProductsSpentItems()
        {
            return (ProductsSpent_Items)productsSpent;
        }

        public ProductsSpent_VCAndItems getProductsSpentVCAndItems()
        {
            return (ProductsSpent_VCAndItems)productsSpent;
        }

        public ProductsReceived_VC getProductsReceivedVC()
        {
            return (ProductsReceived_VC)productsReceived;
        }

        public ProductsReceived_Items getProductsReceivedItems()
        {
            return (ProductsReceived_Items)productsReceived;
        }

        public ProductsReceived_VCAndItems getProductsRecievedVCAndItems()
        {
            return (ProductsReceived_VCAndItems)productsReceived;
        }
    }

    #region Product objects

        /*public class ProductsSpent_VC
        {
            public List<VirtualCurrencies> virtualCurrencies = new List<VirtualCurrencies>(); //will be an array

            public ProductsSpent_VC()
            {
            }

            public void addItem(string i_virtualCurrencyName, string i_virtualCurrencyType, int i_virtualCurrencyAmount)
            {
                VirtualCurrencies vc                     = new VirtualCurrencies();
                vc.virtualCurrency.virtualCurrencyName   = i_virtualCurrencyName;
                vc.virtualCurrency.virtualCurrencyType   = i_virtualCurrencyType;
                vc.virtualCurrency.virtualCurrencyAmount = i_virtualCurrencyAmount;
                virtualCurrencies.Add(vc);
            }
        }*/

        /*public class ProductsSpent_Items
        {
            public List<Items> items = new List<Items>(); //will be an array

            public ProductsSpent_Items()
            {
            }

            public void addItem(string i_itemName, string i_itemType, int i_itemAmount)
            {
                Items item           = new Items();
                item.item.itemName   = i_itemName;
                item.item.itemType   = i_itemType;
                item.item.itemAmount = i_itemAmount;
                items.Add(item);
            }
        }*/

        /*public class ProductsReceived_VC
        {
            public List<VirtualCurrencies> virtualCurrencies; //will be an array

            public ProductsReceived_VC()
            {
                virtualCurrencies = new List<VirtualCurrencies>();
            }

            public void addItem(string i_virtualCurrencyName, string i_virtualCurrencyType, int i_virtualCurrencyAmount)
            {
                VirtualCurrencies vc                     = new VirtualCurrencies();
                vc.virtualCurrency.virtualCurrencyName   = i_virtualCurrencyName;
                vc.virtualCurrency.virtualCurrencyType   = i_virtualCurrencyType;
                vc.virtualCurrency.virtualCurrencyAmount = i_virtualCurrencyAmount;
                virtualCurrencies.Add(vc);
            }
        }*/

        /*public class ProductsReceived_Items
        {
            public List<Items> items; //will be an array

            public ProductsReceived_Items()
            {
                items = new List<Items>();
                //items.Add(item);
            }

            public void addItem(string i_itemName, string i_itemType, int i_itemAmount)
            {
                Items item           = new Items();
                item.item.itemName   = i_itemName;
                item.item.itemType   = i_itemType;
                item.item.itemAmount = i_itemAmount;
                items.Add(item);
            }
        }*/

    public class ProductsReceived_NULL
    {
    }

    public class ProductsSpent_NULL
    {
    }

    #endregion
}