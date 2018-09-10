using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MainServer
{
    static class LootSetManager
    {
        static List<LootTable> m_lootTables = new List<LootTable>();
        static List<LootSet> m_lootSets = new List<LootSet>();
        static public void FillTemplate(Database db)
        {
            FillLootTables(db);
            FillLootSets(db);
        }

        static void FillLootTables(Database db)
        {
            SqlQuery query = new SqlQuery(db, "select * from loot_table");
            while (query.Read())
            {
                m_lootTables.Add(new LootTable(db, query));
            }
            query.Close();

        }

        static void FillLootSets(Database db)
        {//loot_set_id
            SqlQuery query = new SqlQuery(db, "select distinct loot_set_id from loot_set_details");
            while (query.Read())
            {
                //get the set ID
                int setID = query.GetInt32("loot_set_id");
                //get the num drops
                //int numDrops = query.GetInt32("num_drops");
                //create the new loot set
                LootSet newSet = new LootSet(db, setID);
                //add it to the list
                m_lootSets.Add(newSet);
            }
            query.Close();
        }

        static public LootTable getLootTable(int lootTableID)
        {

            for (int i = 0; i < m_lootTables.Count; i++)
            {
                if (m_lootTables[i].m_lootTableID == lootTableID)
                {
                    return m_lootTables[i];
                }
            }
            return null;
        }
        static public LootSet getLootSet(int lootSetID)
        {

            for (int i = 0; i < m_lootSets.Count; i++)
            {
                if (m_lootSets[i].SetID == lootSetID)
                {
                    return m_lootSets[i];
                }
            }
            return null;
        }

        static internal bool GetLootSetsLoaded()
        {
            return (m_lootSets != null && m_lootSets.Count > 0 && m_lootTables != null && m_lootTables.Count > 0);
        }
    }
}
