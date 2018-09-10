using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MainServer
{
    class LootSetHolder
    {
        LootSet m_lootSet=null;
        int m_numDrops = 0;

        internal LootSet TheLootSet
        {
            get { return m_lootSet; }
        }
        internal int NumDrops
        {
            get { return m_numDrops; }
        }
        internal LootSetHolder(LootSet lootSet, int numDrops)
        {
            m_lootSet = lootSet;
            m_numDrops = numDrops;
        }
        
    }
    class LootSet
    {
        List<LootTableWeight> m_lootTableWeights = new List<LootTableWeight>();
        int m_totalLootWeights = 0;
       
        int m_setID;
        internal int SetID
        {
            get { return m_setID; }
        }
        public LootSet(Database db, int setID)
        {
            m_setID = setID;
            string strQuery = "select * from loot_set_details where loot_set_id=";
           /* if (MobTemplateManager.LOOT_SETS_LOADED_CORRECTLY == false)
            {
                strQuery = "select * from mob_loot where mob_template_id=";
            }*/
            SqlQuery lootQuery = new SqlQuery(db, strQuery + setID);
            while (lootQuery.Read())
            {
                int lootTableID = lootQuery.GetInt32("loot_table_id");
                int weightVal = lootQuery.GetInt32("weight");
                m_totalLootWeights += weightVal;
                LootTableWeight weight = new LootTableWeight(lootTableID, m_totalLootWeights);
                m_lootTableWeights.Add(weight);
            }
            lootQuery.Close();
        }
        internal void getLootDropped(List<LootDetails> lootdetails,int numDrops)
        {

            for (int i = 0; i < numDrops; i++)
            {
                LootDetails detail = getLootItem();

                if (detail != null)
                {
                    if (lootdetails.Count > 0)
                    {
                        ItemTemplate itemTemplate = ItemTemplateManager.GetItemForID(detail.m_templateID);
                        if (itemTemplate.m_stackable)
                        {
                            bool found = false;
                            for (int j = 0; j < lootdetails.Count; j++)
                            {
                                if (lootdetails[j].m_templateID == detail.m_templateID)
                                {
                                    lootdetails[j].m_quantity += detail.m_quantity;
                                    found = true;
                                    break;
                                }
                            }
                            if (!found)
                            {
                                lootdetails.Add(detail);
                            }
                        }
                        else
                        {
                            lootdetails.Add(detail);
                        }
                    }
                    else
                    {
                        lootdetails.Add(detail);
                    }

                }
            }
           
        }
        internal LootDetails getLootItem()
        {
            int randNum = Program.getRandomNumber(m_totalLootWeights);
            for (int i = 0; i < m_lootTableWeights.Count; i++)
            {
                if (m_lootTableWeights[i].checkChance(randNum))
                {
                    //nothing to loot
                    if (m_lootTableWeights[i].m_lootTable == null)
                        return null;
                    else
                        return m_lootTableWeights[i].m_lootTable.getLootItem();

                }
            }
            return null;
        }
    }
}
