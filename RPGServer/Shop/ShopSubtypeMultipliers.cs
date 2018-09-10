namespace MainServer
{
    class ShopSubtypeMultipliers
    {
        public ShopSubtypeMultipliers(SqlQuery query)
        {
            m_subType = (ItemTemplate.ITEM_SUB_TYPE)query.GetInt32("sub_type_id");
            m_buyMultiplier = query.GetFloat("buy_price_multiplier");
            m_sellMultiplier = query.GetFloat("sell_price_multiplier");
        }
        public ItemTemplate.ITEM_SUB_TYPE m_subType;
        public float m_buyMultiplier;
        public float m_sellMultiplier;
    }
}