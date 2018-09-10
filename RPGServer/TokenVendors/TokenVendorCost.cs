using System.Collections.Generic;

namespace MainServer.TokenVendors
{
    public class TokenVendorCost
    {
        public int ItemTemplateId { get; private set; }
        public int Quantity { get; private set; }

        public TokenVendorCost(int itemTemplateId, int quantity)
        {
            ItemTemplateId = itemTemplateId;
            Quantity = quantity;
        }

        public void MultiplyQuantityBy(int multipler)
        {
            Quantity = Quantity * multipler;
        }

        public void ReduceQuantity(int amount)
        {
            Quantity -= amount;
        }
    }
}
