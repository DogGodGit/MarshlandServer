using System.Collections;
using System.Collections.Generic;

namespace MainServer.Items
{
    static class ItemCooldown
    {
        private static readonly IDictionary<int, float> m_itemCooldowns = new Dictionary<int, float>();

        public static void SetUp(Database db)
        {
            SqlQuery query = new SqlQuery(db, "select * from item_cooldowns order by item_id");
            while (query.Read())
            {
                int item_id = query.GetInt32("item_id");
                float cooldown = query.GetFloat("cooldown");

                if (!m_itemCooldowns.ContainsKey(item_id))
                {
                    m_itemCooldowns.Add(item_id, cooldown);
                }
            }

            query.Close();
        }

        public static float GetItemCooldownForId(int id)
        {
            if (m_itemCooldowns.ContainsKey(id))
            {
                return m_itemCooldowns[id];
            }
            return ItemTemplateManager.ITEM_RECHARGE_TIME; //-ItemTemplateManager.ITEM_LEEWAY_TIME; // standard item global cooldown
        }
    }
}
