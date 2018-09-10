using System;
using Lidgren.Network;

namespace MainServer
{
    class ShopItem
    {
        public ShopItem(int templateID, int default_stock)
        {
            m_default_stock_levels = default_stock;
            m_stock = new Item(-1, templateID, default_stock, -1);
        }
        Item m_stock;
        int m_default_stock_levels;

        public int buyItem(int quantity, int playercoins, float multiplier)
        {
            int buycost = quantity * (int)Math.Ceiling(m_stock.m_template.m_sellprice * multiplier);
            if (m_stock.m_quantity < 0)
            {
                if (buycost <= playercoins)
                {
                    return buycost;
                }
                else
                {
                    return -1;//can't afford
                }
            }
            else if (m_stock.m_quantity < quantity)
            {
                return -2;//not enough stock
            }
            else if (buycost <= playercoins)
            {
                m_stock.m_quantity -= quantity;
                return quantity * m_stock.m_template.m_sellprice;
            }
            else
            {
                return -1;//can't afford
            }

        }
        public int getTemplateID()
        {
            return m_stock.m_template_id;
        }
        public ItemTemplate getTemplate()
        {
            return m_stock.m_template;
        }
        public void writeItemToMessage(NetOutgoingMessage msg)
        {
            msg.WriteVariableInt32(m_stock.m_inventory_id);
            msg.WriteVariableInt32(m_stock.m_template_id);
            msg.WriteVariableInt32(m_stock.m_quantity);

        }

    }
}