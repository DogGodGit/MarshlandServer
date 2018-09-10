using System.Collections.Generic;

namespace MainServer
{
    class LootDetails
    {
        public LootDetails(int templateID, int quantity)
        {
            m_templateID = templateID;
            m_quantity = quantity;
        }
        public int m_templateID;
        public int m_quantity;
        public static void AddLootToCompiledList(LootDetails newLoot, List<LootDetails> listToAddTo)
        {
            bool itemFound = false;
            for (int j = 0; j < listToAddTo.Count && itemFound == false; j++)
            {
                LootDetails currentCompiledDrop = listToAddTo[j];
                if (currentCompiledDrop.m_templateID == newLoot.m_templateID)
                {
                    currentCompiledDrop.m_quantity += newLoot.m_quantity;
                    itemFound = true;
                }
            }
            if (itemFound == false)
            {
                listToAddTo.Add(new LootDetails(newLoot.m_templateID, newLoot.m_quantity));
            }
        }
        
        internal static void CompressAllStackable(List<LootDetails> currentLootList)
        {
            //check up to the second last
            for (int lootIndex = 0; lootIndex < currentLootList.Count - 1; lootIndex++)
            {
                LootDetails currentLoot = currentLootList[lootIndex];
                int currentID = currentLoot.m_templateID;
                int currentQuantity = currentLoot.m_quantity;
                ItemTemplate currentLootTemplate = ItemTemplateManager.GetItemForID(currentID);

                //if it can be stacked then check there's not more than one of them
                if (currentLootTemplate != null && currentLootTemplate.m_stackable == true)
                {
                    for (int secondLootIndex = (lootIndex + 1); secondLootIndex < currentLootList.Count; secondLootIndex++)
                    {
                        LootDetails comparingLoot = currentLootList[secondLootIndex];
                        if (comparingLoot.m_templateID == currentID)
                        {
                            currentLoot.m_quantity += comparingLoot.m_quantity;
                            currentLootList.RemoveAt(secondLootIndex);
                            secondLootIndex--;
                        }
                    }
                }
            }
        }
    }

    class Item
    {
        
        public int m_inventory_id;
        public int m_template_id;
        //the number of this
        public int m_quantity;
        //the equipment slot the item can attach to, -1 for unequipable
        public ItemTemplate m_template=null;
        public int m_sortOrder;
        public bool m_bound = false;
        public int m_remainingCharges = 1;
        public double m_timeRecharged = 0;
        private bool m_isFavourite;

        internal bool Destroyed { get; set; }

        internal bool IsFavourite
        {
            get { return m_isFavourite; }
            set { m_isFavourite = value; }
        }

        public Item(int invID, int templateID, int quantity, int sortOrder, int remainingCharges = -1)
        {
            Destroyed = false;
            m_inventory_id = invID;
            m_template_id = templateID;
            m_quantity = quantity;
            m_timeRecharged = 0;
            m_bound = false;
            m_sortOrder = sortOrder;
            m_template = ItemTemplateManager.GetItemForID(m_template_id);
            
            if (m_template != null)
            {
                m_remainingCharges = remainingCharges == -1 ? m_template.m_maxCharges : remainingCharges;
            }
            if (m_template == null)
            {
                m_template = ItemTemplateManager.GetItemForID(ItemTemplateManager.INVALID_ITEM_TEMP_ID);
            }
        }
        internal static Item CreateQuickShallowItem(ItemTemplate template, int quantity)
        {
            Item tempItem = new Item();

            tempItem.m_inventory_id = -1;
            tempItem.m_template_id = template.m_item_id;
            tempItem.m_quantity = quantity;
            tempItem.m_timeRecharged = 0;
            tempItem.m_bound = false;
            tempItem.m_sortOrder = 0;
            tempItem.m_template = template;


            return tempItem;
        }
        
        public Item(int invID, int templateID, int quantity, bool bound, int remainingCharges, double timeRecharged,int sortOrder, bool isFavourite)
        {
            Destroyed = false;
            m_inventory_id = invID;
            m_template_id = templateID;
            m_quantity = quantity;

            m_bound = bound;
            m_remainingCharges = remainingCharges;
            m_timeRecharged = timeRecharged;
            m_sortOrder = sortOrder;
            m_isFavourite = isFavourite;
            m_template = ItemTemplateManager.GetItemForID(m_template_id);
            if (m_template == null)
            {
                m_template = ItemTemplateManager.GetItemForID(ItemTemplateManager.INVALID_ITEM_TEMP_ID);
            }
        }
        public Item(Item oldItem)
        {
            Destroyed = false;
            m_inventory_id = oldItem.m_inventory_id;
            m_template_id = oldItem.m_template_id;
            m_quantity = oldItem.m_quantity;
            m_bound = oldItem.m_bound;
            m_remainingCharges = oldItem.m_remainingCharges;
            m_timeRecharged = oldItem.m_timeRecharged;
            m_sortOrder = oldItem.m_sortOrder;
            m_isFavourite = oldItem.m_isFavourite;
            m_template = ItemTemplateManager.GetItemForID(m_template_id);
            if (m_template == null)
            {
                m_template = ItemTemplateManager.GetItemForID(ItemTemplateManager.INVALID_ITEM_TEMP_ID);
            }
        }

        public Item()
        {
            Destroyed = false;
            m_inventory_id = 0;
            m_template_id = 0;
            m_quantity = 0;
        }
        public void Equipped(Character character)
        {
            switch (m_template_id)
            {
                case 20603:
                case 20604:
                case 20605:
                case 20606:
                case 20607:
                case 20608:
                case 20609:
                case 20610:

                case 21969:
                case 21970:
                case 21971:
                case 21972:
                case 21973:
                case 21974:
                case 21975:
                case 21976:
                case 21977:
                case 21978:
                case 21979:
                case 21980:
                case 21981:
                case 21982:
                case 21983:
                case 21984:
                case 21985:
                case 21986:
                case 21987:
                case 21988:
                case 21989:
                case 21990:
                case 21991:
                case 21992:
                case 21993:
                case 21994:
                    {
                        character.m_face_id = -1;
                        character.InfoUpdated(Inventory.EQUIP_SLOT.SLOT_FACE);
                        character.m_hair_id = -1;
                        character.InfoUpdated(Inventory.EQUIP_SLOT.SLOT_HAIR);
                        character.m_face_acc_id = -1;
                        character.InfoUpdated(Inventory.EQUIP_SLOT.SLOT_FACE_ACCESSORY);
                        character.m_skin_colour = -1;
                        character.InfoUpdated(Inventory.EQUIP_SLOT.SLOT_SKIN_COLOUR);
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }
        public void Unequipped(Character character)
        {
            switch (m_template_id)
            {
                case 20603:
                case 20604:
                case 20605:
                case 20606:
                case 20607:
                case 20608:
                case 20609:
                case 20610:

                case 21969:
                case 21970:
                case 21971:
                case 21972:
                case 21973:
                case 21974:
                case 21975:
                case 21976:
                case 21977:
                case 21978:
                case 21979:
                case 21980:
                case 21981:
                case 21982:
                case 21983:
                case 21984:
                case 21985:
                case 21986:
                case 21987:
                case 21988:
                case 21989:
                case 21990:
                case 21991:
                case 21992:
                case 21993:
                case 21994:
                    {
                        character.ReturnToBasicAppearance();
                        character.InfoUpdated(Inventory.EQUIP_SLOT.SLOT_FACE);

                        character.InfoUpdated(Inventory.EQUIP_SLOT.SLOT_HAIR);

                        character.InfoUpdated(Inventory.EQUIP_SLOT.SLOT_FACE_ACCESSORY);

                        character.InfoUpdated(Inventory.EQUIP_SLOT.SLOT_SKIN_COLOUR);
                        break;
                    }
                default:
                    {
                        
                        break;
                    }
            }
        }

        /// <summary>
        /// Should ONLY be used for backpack/main inventory table as is_favourite isn't included in the bank or mail tables
        /// </summary>
        /// <param name="characterID"></param>
        /// <returns></returns>
        internal string GetUpdateString(uint characterID)
        {
            return "character_id=" + characterID + ",item_id=" + m_template_id + ",quantity=" + m_quantity + ", remaining_charges =" + m_remainingCharges + ",bound=" + m_bound + ",time_skill_last_cast = " + m_timeRecharged + ", sort_order=" + m_sortOrder + ", is_favourite =" + m_isFavourite;
        }

        internal string GetInsertString(uint characterID)
        {
            return "(inventory_id,character_id,item_id,quantity,remaining_charges,bound,time_skill_last_cast,sort_order) values (" + m_inventory_id + "," + characterID + "," + m_template_id + "," + m_quantity + "," + m_remainingCharges + "," + m_bound + "," + m_timeRecharged + "," + m_sortOrder + ")";
            
        }
        internal string GetInsertFieldsString()
        {
            return "inventory_id,character_id,item_id,quantity,remaining_charges,bound,time_skill_last_cast,sort_order";
        
        }
        internal string GetInsertValuesString(int characterID)
        {
            return m_inventory_id + "," + characterID + "," + m_template_id + "," + m_quantity + "," + m_remainingCharges + "," + m_bound + "," + m_timeRecharged + "," + m_sortOrder;
        
        }
        internal string GetBasicItemIDString()
        {
            return "(Item:" + m_inventory_id + ", TempID:" + m_template_id + ",Quantity:" + m_quantity + ")";
        }
    }
}
