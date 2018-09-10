using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using XnaGeometry;

namespace MainServer
{
    class SpawnPointItem{


        ItemTemplate m_itemTemplate;
        int m_probability;
        public SpawnPointItem(ItemTemplate template, int probability)
        {
            m_itemTemplate = template;
            m_probability = probability;
        }

        public ItemTemplate ItemTemplate
        {
            get { return m_itemTemplate; }
        }
        public int Probability
        {
            get { return m_probability; }
        }

    }
    class ItemSpawnPoint
    {
        #region variables
        //public enum PatrolType { Stand, Random, Patrol };
        public Vector3 m_spawnPosition;

        float m_minRespawnTime;
        /// Time until the replacement mob is created
        /// should only count down once a monster is killed
        /// </summary>
        float m_maxRespawnTime;


        float m_timeTillNextRespawn;
        /// <summary>
        /// A list of what mobs can appear at this spawn point
        /// and their probability of appearing
        /// </summary>
        List<SpawnPointItem> m_itemList;
        int m_probabilitySum;
        public int m_itemSpawnID = -1;
        public ItemTemplate m_item;
        Zone m_zone;
        #endregion //variables
        public ItemSpawnPoint(Zone zone,float x, float y, float z,float minRespawnTime,int itemSpawnID,string itemList,float maxRespawnTime)
        {
            m_zone = zone;
            m_itemList = new List<SpawnPointItem>();
            m_timeTillNextRespawn = 0;
            m_probabilitySum = 0;
            m_minRespawnTime = minRespawnTime;
            m_maxRespawnTime = maxRespawnTime;
            m_spawnPosition.X = x;
            m_spawnPosition.Y = y;
            m_spawnPosition.Z = z;

            m_item=null;
            m_itemSpawnID = itemSpawnID;
            String[] itemListSplit = itemList.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            

            for (int i = 0; i < itemListSplit.Length; i++)
            {
                String[] itemsubsplit = itemListSplit[i].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                ItemTemplate itemtemplate = ItemTemplateManager.GetItemForID(Convert.ToInt32(itemsubsplit[0]));

                int probability = Convert.ToInt32(itemsubsplit[1]);
                AddItemToList(itemtemplate, probability);
            }

           
                    
        }
        
 

        public bool Update(double timeSinceLastUpdate)
        {
            m_timeTillNextRespawn -= (float)timeSinceLastUpdate;
            if (m_item==null && m_timeTillNextRespawn < 0)
            {
                SpawnItem();

                return true;
            }
            return false;

        }

        public void AddItemToList(ItemTemplate template, int probability)
        {
            SpawnPointItem newItem = new SpawnPointItem(template, m_probabilitySum+probability);
            m_itemList.Add(newItem);
            m_probabilitySum += probability;
        }


        void SpawnItem()
        {
            
    
            int randomResult = Program.getRandomNumber(m_probabilitySum); ;

            
            for (int i = 0; i < m_itemList.Count ; i++)
            {
                if (m_itemList[i].Probability > randomResult)
                {
                    m_item = m_itemList[i].ItemTemplate;
                    if (m_item != null)
                    {
                        NetOutgoingMessage msg = Program.Server.CreateMessage();
                        msg.WriteVariableUInt32((uint)NetworkCommandType.ItemRespawn);
                        WriteToMessage(msg);
                        List<NetConnection> connections = m_zone.getUpdateList(null);
                        Program.processor.SendMessage(msg, connections, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.ItemRespawn);
                        if (Program.m_LogSpawns)
                        {
                            Program.Display("item spawn point ID=" + m_itemSpawnID + " spawned " + m_item.m_item_name + "[" + m_item.m_item_id + "]");
                        }
                    }
                    else
                    {
                        if (Program.m_LogNonSpawns)
                        {
                            Program.Display("item spawn point ID=" + m_itemSpawnID + " spawned nothing");
                        }
                        int diff = (int)((m_maxRespawnTime - m_minRespawnTime) * 10);
                        int rand = Program.getRandomNumber(diff);
                        m_timeTillNextRespawn = m_minRespawnTime + rand / 10;
                        
                    }
                    break;
                }
            }
        }
        public void WriteToMessage(NetOutgoingMessage msg)
        {
            if(m_item!=null)
            {
                msg.WriteVariableInt32(m_itemSpawnID);
                msg.WriteVariableInt32(m_item.m_item_id);
                msg.Write((float)m_spawnPosition.X);
                msg.Write((float)m_spawnPosition.Y);
                msg.Write((float)m_spawnPosition.Z);

            }
        }


        internal void despawn()
        {
            if(Program.m_LogSpawns)
            Program.Display("item despawned");
            m_item = null;
            int diff = (int)((m_maxRespawnTime - m_minRespawnTime) * 10);
            int rand = Program.getRandomNumber(diff);
            m_timeTillNextRespawn = m_minRespawnTime+rand/10;
        }
    }
}
