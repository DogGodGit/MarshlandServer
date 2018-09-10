using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MainServer
{

    class CharacterSlotSetHolder
    {
        uint m_ownerID = 0;
        int m_totalSlots = 8;
        int m_highestSet=0;
        List<CharacterSlotSet> m_slotSets = new List<CharacterSlotSet>();

        internal CharacterSlotSetHolder(uint characterID, Database db, int totalSlots)
        {
            m_ownerID = characterID;
            SqlQuery query = new SqlQuery(db, "select * from character_hud_slot_sets where character_id=" + characterID + " order by slot_pos");
            while (query.Read())
            {
                LoadInSet(query);
            }
            m_totalSlots = CalculateCurrentNumSlots();
            if (m_totalSlots != totalSlots)
            {
                SetTotalSlots(totalSlots);
                for (int i = 0; i < m_slotSets.Count; i++)
                {
                    CharacterSlotSet currentSet = m_slotSets[i];
                    if (currentSet.Changed == true)
                    {
                        currentSet.SaveToDatabase(db);
                    }
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="query">query of all sets belonging to this character</param>
        void LoadInSet(SqlQuery query)
        {
            string skillString = query.GetString("slots_string");
            int capacity = query.GetInt32("capacity");
            int positionNumber = query.GetInt32("slot_pos");
            CharacterSlotSet newSet = new CharacterSlotSet(m_ownerID, positionNumber, capacity);

            AddSet(newSet);
            
            newSet.ReadSkillsFromString(skillString);
            //this data does not need to be saved as it has just been read in
            newSet.Changed = false;
            //now read the info for the string
        }
        void AddSet(CharacterSlotSet newSet)
        {
            m_slotSets.Add(newSet);
            if (newSet.Position > m_highestSet)
            {
                m_highestSet = newSet.Position;
            }
        }
        internal CharacterSlotSet GetSet(int setPosition)
        {
            for (int i = 0; i < m_slotSets.Count; i++)
            {
                CharacterSlotSet currentSet = m_slotSets[i];
                if (currentSet.Position == setPosition)
                {
                    return currentSet;
                }
            }
                return null;
        }
        internal void SetTotalSlots(int newTotalSlots)
        {
            int currentHeldSlots = 0;

            for (int i = 0; i < m_slotSets.Count; i++)
            {
                CharacterSlotSet currentSlotSet = m_slotSets[i];
                if (currentSlotSet!=null)
                {
                    currentHeldSlots += currentSlotSet.Capacity;
                }
            }
            if (currentHeldSlots < newTotalSlots)
            {
                while (currentHeldSlots < newTotalSlots)
                {
                    CharacterSlotSet currentSlotSet = null;
                    if (m_slotSets.Count > 0)
                    {
                        currentSlotSet = m_slotSets.Last();
                    }
                    if (currentSlotSet==null|| currentSlotSet.Capacity >= currentSlotSet.MAX_CAPACITY)
                    {
                        int index = m_slotSets.Count;
                        currentSlotSet = new CharacterSlotSet(m_ownerID, index,0);
                        m_slotSets.Add(currentSlotSet);
                        currentSlotSet.CreateInDatabase(Program.processor.m_worldDB);
                    }
                    if (currentSlotSet != null)
                    {
                        int currentCapacity = currentSlotSet.Capacity;
                        //how much needs to be added
                        int remainingCapacity = newTotalSlots-currentHeldSlots;
                        //can you add them all to this one
                        int numToAdd = remainingCapacity;
                        if(currentCapacity+numToAdd >= currentSlotSet.MAX_CAPACITY)
                        {
                            numToAdd = currentSlotSet.MAX_CAPACITY-currentCapacity;
                        }
                        currentSlotSet.SetCapacity(currentCapacity + numToAdd);
                        currentSlotSet.SaveToDatabase(Program.processor.m_worldDB);
                        currentHeldSlots += numToAdd;
                        if (numToAdd <= 0)
                        {
                            Program.Display("Error Adding Hud Slots numToAdd = " + numToAdd);
                            break;
                        }
                    }
                    else{
                        //something has gone very wrong
                        //don't get stuck in an infinate loop
                        Program.Display("Error Adding Hud Slots currentSlotSet = null");
                        break;
                    //currentHeldSlots++;
                    }
                }
            }
            m_totalSlots = newTotalSlots;
        }
        internal int CalculateCurrentNumSlots()
        {
            int currentHeldSlots = 0;

            for (int i = 0; i < m_slotSets.Count; i++)
            {
                CharacterSlotSet currentSlotSet = m_slotSets[i];
                if (currentSlotSet != null)
                {
                    currentHeldSlots += currentSlotSet.Capacity;
                }
            }
            return currentHeldSlots;
        }
        internal CharacterSlotSet GetSetWithIndex(int setIndex)
        {
            if (setIndex >= 0 && setIndex < m_slotSets.Count)
            {
                return m_slotSets[setIndex];
            }
            return null;
        }
        internal int GetNumSets()
        {
            return m_slotSets.Count;
        }
    }
    class CharacterSlotSet
    {
        internal int MAX_CAPACITY = 8;
        uint m_ownerID = 0;

        int m_positionNumber = 0;
        int m_capacity = 0;

        bool m_changed = false;
        List<HudSlotItem> m_hudSlotItems = new List<HudSlotItem>();

        internal int Capacity
        {
            get { return m_capacity; }
        }
        internal bool Changed
        {
            get { return m_changed; }
            set { m_changed = value; }
        }
        internal int Position{
            get { return m_positionNumber; }
        }
        
        internal CharacterSlotSet(uint ownerID, int positionNumber, int capacity)
        {

            SetUp(ownerID, positionNumber, capacity);
        
            //crea
        }

        void SetUp(uint ownerID, int positionNumber, int capacity)
        {
            m_ownerID = ownerID;
            m_positionNumber = positionNumber;
            m_capacity = capacity;

            for (int i = 0; i < m_capacity; i++)
            {
                HudSlotItem newItem = new HudSlotItem( HudSlotItemType.HUD_SLOT_ITEM_EMPTY,-1);
                m_hudSlotItems.Add(newItem);
            }
        }
        /// <summary>
        /// uses the information stored to create a new database entry
        /// used when the set is first purchased
        /// </summary>
        /// <param name="db"></param>
        internal void CreateInDatabase(Database db)
        {

            string skillList = "";
            for (int i = 0; i < m_hudSlotItems.Count; i++)
            {
                skillList += "," + m_hudSlotItems[i].getString();
            }
            if (skillList.Length > 0)
            {
                skillList = skillList.Substring(1);
            }
            m_changed = false;
            db.runCommand("insert into character_hud_slot_sets (character_id,slot_pos,capacity,slots_string) values " +
                "(" + m_ownerID + "," + m_positionNumber + "," + m_capacity + ",'" + skillList + "')");
        }
        //saves the current set to the database
        internal void SaveToDatabase(Database db)
        {
            string skillList = "";
            for (int i = 0; i < m_hudSlotItems.Count; i++)
            {
                skillList += "," + m_hudSlotItems[i].getString();
            }
            if (skillList.Length > 0)
            {
                skillList = skillList.Substring(1);
            }
            db.runCommandSync("update character_hud_slot_sets set slots_string = '" + skillList + "',capacity = "+m_capacity+"  where character_id=" + m_ownerID + " and slot_pos = " + m_positionNumber);
            m_changed = false;
            
        }
        internal void SetCapacity(int newCapacity)
        {


            for (int i = m_hudSlotItems.Count; i < newCapacity; i++)
            {
                m_changed = true;
                HudSlotItem newItem = new HudSlotItem(HudSlotItemType.HUD_SLOT_ITEM_EMPTY, -1);
                m_hudSlotItems.Add(newItem);
            }

            if (newCapacity < m_hudSlotItems.Count)
            {
                m_changed = true;
                int numToRemove = m_hudSlotItems.Count- newCapacity;

                m_hudSlotItems.RemoveRange(m_hudSlotItems.Count - numToRemove - 1, numToRemove);
            }
            m_capacity = newCapacity;
        }
        internal bool SetSlot(int slotID, HudSlotItemType type, int identifyingID)
        {
            bool changed = false;
            if (slotID >= 0 && slotID < m_hudSlotItems.Count)
            {
                HudSlotItem itemToChange = m_hudSlotItems[slotID];
                if (itemToChange.m_slotItemType != type || itemToChange.m_item_id != identifyingID)
                {
                    itemToChange.m_item_id = identifyingID;
                    itemToChange.m_slotItemType = type;
                    changed = true;
                    m_changed = true;
                }
            }

            return changed;

        }
        internal HudSlotItem GetItemAtSlot(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < m_hudSlotItems.Count)
            {
                return m_hudSlotItems[slotIndex];
            }
            return null;
        }
        internal void ReadSkillsFromString(string skillList)
        {
            string[] skillHudSkills = skillList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < skillHudSkills.Length; i++)
            {
                string currentHudSkill = skillHudSkills[i];
                string[] splitStr = currentHudSkill.Split(new char[] { '^' }, StringSplitOptions.RemoveEmptyEntries);

                HudSlotItemType slotType = (HudSlotItemType)Int32.Parse(splitStr[0]);
                int itemID = Int32.Parse(splitStr[1]);

                SetSlot(i, slotType, itemID);
                
            }
        }

        internal int GetFirstEmptySlot()
        {
            for (int i = 0; i < m_hudSlotItems.Count; i++)
            {

                HudSlotItem currentItem = m_hudSlotItems[i];
                if (currentItem != null && currentItem.m_item_id < 0)
                {
                    return i;
                }
            
            }
            return -1;
        }
    }
}
