using System.Collections.Generic;

namespace MainServer.TokenVendors
{
    public interface ITokenVendorDatabase
    {
        IDictionary<int, ITokenVendor> SetUpTokenVendors();
        List<TokenVendorStock> SetUpTokenVendorStock();
        IDictionary<int, List<TokenVendorCost>> SetUpTokenVendorCosts();
    }

    class TokenVendorDatabase : ITokenVendorDatabase
    {
        private readonly SqlQuery query;

        public TokenVendorDatabase(SqlQuery query)
        {
            this.query = query;
        }


        public IDictionary<int, ITokenVendor> SetUpTokenVendors()
        {
            var tokenVendors = new Dictionary<int, ITokenVendor>();
            query.ExecuteCommand("select * from token_vendors order by token_vendor_id");

            while (query.Read())
            {
                int tokenVendorId = query.GetInt32("token_vendor_id");
                string tokenVendorName = query.GetString("token_vendor_name");
                int zoneId = query.GetInt32("zone_id");
                int npcId = query.GetInt32("npc_id");
                int faction_id = query.GetInt32("faction_id");
                int faction_level = query.GetInt32("faction_level");

                tokenVendors.Add(tokenVendorId, new TokenVendor(tokenVendorName, zoneId, npcId, faction_id, faction_level));
            }
            query.CleanUpAfterExecute();

            return tokenVendors;
        }

        public List<TokenVendorStock> SetUpTokenVendorStock()
        {
            var tokenVendorStock = new List<TokenVendorStock>();
            query.ExecuteCommand("select * from token_vendor_stock order by token_vendor_stock_id");

            while (query.Read())
            {
                int tokenVendorStockId = query.GetInt32("token_vendor_stock_id");
                int tokenVendorId = query.GetInt32("token_vendor_id");
                int itemTemplateId = query.GetInt32("item_template_id");
                int tokenVendorCost = query.GetInt32("token_vendor_cost_id");

                tokenVendorStock.Add(new TokenVendorStock(tokenVendorStockId, tokenVendorId, itemTemplateId, tokenVendorCost));
            }

            query.CleanUpAfterExecute();
            return tokenVendorStock;
        }

        public IDictionary<int, List<TokenVendorCost>> SetUpTokenVendorCosts()
        {
            var tokenVendorCosts = new Dictionary<int, List<TokenVendorCost>>();

            query.ExecuteCommand("select * from token_vendor_cost order by token_vendor_cost_id");

            while (query.Read())
            {
                int tokenVendorCostId = query.GetInt32("token_vendor_cost_id");
                int itemTemplateId = query.GetInt32("item_template_id");
                int quantity = query.GetInt32("quantity");

                if (!tokenVendorCosts.ContainsKey(tokenVendorCostId)) // if it's not in the dictionary, add it before adding the cost
                { 
                    tokenVendorCosts.Add(tokenVendorCostId, new List<TokenVendorCost>());
                }            

                tokenVendorCosts[tokenVendorCostId].Add(new TokenVendorCost(itemTemplateId, quantity));               
            }

            query.CleanUpAfterExecute();
            return tokenVendorCosts;
        }
    }
}
