using System.Collections.Generic;

namespace MainServer.TokenVendors
{
    public interface ITokenVendor
    {
        void SetTokenVendorStock(List<TokenVendorStock> stockList);
        List<TokenVendorStock> GetStock();
        List<int> GetStockCostIds();
        TokenVendorStock GetStockForId(int id);
    }

    public class TokenVendor: ITokenVendor
    {
        public string TokenVendorName { get; private set; }
        public int ZoneId { get; private set; }
        public int NpcId { get; private set; }
        public int FactionIDRestriction { get; private set; }
        public int FactionLevelRestriction { get; private set; }

        List<TokenVendorStock> m_stock = new List<TokenVendorStock>();  

        public TokenVendor(string vendorName, int zoneId, int npcId, int factionId, int factionLevel)
        {
            TokenVendorName = vendorName;
            ZoneId = zoneId;
            NpcId = npcId;
            this.FactionIDRestriction = factionId;
            this.FactionLevelRestriction = factionLevel;
        }

        public void SetTokenVendorStock(List<TokenVendorStock> stockList)
        {
            m_stock = stockList;
        }

        public List<TokenVendorStock> GetStock()
        {
            return m_stock;
        }

        public List<int> GetStockCostIds()
        {
            var StockCostIds = new List<int>();
            for (int i = 0; i < m_stock.Count; i++)
            {
                if (!StockCostIds.Contains(m_stock[i].TokenVendorCostId))
                {
                    StockCostIds.Add(m_stock[i].TokenVendorCostId);
                }
            }

            return StockCostIds;
        }

        public TokenVendorStock GetStockForId(int id)
        {
            for (int i = 0; i < m_stock.Count; i++)
            {
                if (m_stock[i].TokenVendorStockId == id)
                {
                    return m_stock[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Certain shops are restricted by class or faction, check against these now.
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        internal bool CharacterMeetsRequirment(Character character)
        {
           
            //not high enough in faction
            if (FactionIDRestriction != 0)
            {
                if (character.FactionManager.HasFactionLevel(this.FactionIDRestriction, this.FactionLevelRestriction) == false)
                    return false;
            }

            //all checks passed ok
            return true;
        }

    }
}
