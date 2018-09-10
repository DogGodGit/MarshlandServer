namespace MainServer.TokenVendors
{
    public class TokenVendorStock
    {
        public int TokenVendorStockId { get; private set; }
        public int TokenVendorId { get; private set; }
        public int ItemTemplateId { get; private set; }
        public int TokenVendorCostId { get; private set; }

        public TokenVendorStock(int tokenVendorStockId, int tokenVendorId, int itemTemplateId, int tokenVendorCostId)
        {
            TokenVendorStockId = tokenVendorStockId;
            TokenVendorId = tokenVendorId;
            ItemTemplateId = itemTemplateId;
            TokenVendorCostId = tokenVendorCostId;
        }
    }
}
