using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Lidgren.Network;
using MainServer.Localise;

namespace MainServer.TokenVendors
{
    internal interface ITokenVendorManager
    {
        ITokenVendor GetTokenVendorForId(int tokenVendorId);
        List<TokenVendorCost> GetTokenVendorCostForId(int tokenVendorCostId);

		void WriteVendorStockToMessage(Player player, NetOutgoingMessage message, int vendorId);
		//void WriteVendorStockToMessage(NetOutgoingMessage message, int vendorId);
        Item PurchaseItemRequest(Character character, int tokenVendorId, int tokenVendorStockId, int quantity);
    }

    internal class TokenVendorManager : ITokenVendorManager
    {
		// #localisation
		public class TokenVendorManagerTextDB : TextEnumDB
		{
			public TokenVendorManagerTextDB() : base(nameof(TokenVendorManager), typeof(TextID)) { }

			public enum TextID
			{
				FAILED_FIND_STOCK,      //"Failed to find stock for this vendor."
				PURCHASED_ITEMS,        //"You have purchased {quantity0} {itemName1}s."
                PURCHASED_ITEMS_NO_S,   //"You have purchased {quantity0} {itemName1}."
				PURCHASED_ITEM,         //"You have purchased a {itemName0}."
                COOKED_ITEMS,           //"You made {quantity0} {itemName1}s"
                COOKED_ITEM,            //"You made a {itemName0}"
                COOKING_INTERRUPTED,    //"Cooking was interrupted"
            }
		}
		public static TokenVendorManagerTextDB textDB = new TokenVendorManagerTextDB();

		private IDictionary<int, ITokenVendor> m_tokenVendors = new Dictionary<int, ITokenVendor>();
        private IDictionary<int, List<TokenVendorCost>> m_tokenVendorCosts = new Dictionary<int, List<TokenVendorCost>>();
        private readonly ITokenVendorDatabase m_tokenVendorDatabase;

        public TokenVendorManager(ITokenVendorDatabase tokenVendorDatabase)
        {
            m_tokenVendorDatabase = tokenVendorDatabase;
            SetUpTokenVendors();
        }

        private void SetUpTokenVendors()
        {
            var tokenVendors = m_tokenVendorDatabase.SetUpTokenVendors();
            var tokenVendorStock = m_tokenVendorDatabase.SetUpTokenVendorStock();

            MarryStockToVendors(tokenVendors, tokenVendorStock);

            m_tokenVendors = tokenVendors;
            m_tokenVendorCosts = m_tokenVendorDatabase.SetUpTokenVendorCosts();
        }

        private void MarryStockToVendors(IDictionary<int, ITokenVendor> tokenVendors, List<TokenVendorStock> tokenVendorStock)
        {
            foreach (KeyValuePair<int, ITokenVendor> vendor in tokenVendors)
            {
                List<TokenVendorStock> stock = tokenVendorStock.Where(x => x.TokenVendorId == vendor.Key).ToList();
                vendor.Value.SetTokenVendorStock(stock);
            }
        }

        public ITokenVendor GetTokenVendorForId(int tokenVendorId)
        {
            if (m_tokenVendors.ContainsKey(tokenVendorId))
            {
                return m_tokenVendors[tokenVendorId];
            }
            return null;
        }

        public List<TokenVendorCost> GetTokenVendorCostForId(int tokenVendorCostId)
        {
            if (m_tokenVendorCosts.ContainsKey(tokenVendorCostId))
            {
                return m_tokenVendorCosts[tokenVendorCostId];
            }
            return new List<TokenVendorCost>(); // send an empty list back, not null
        }

		public void WriteVendorStockToMessage(Player player, NetOutgoingMessage message, int vendorId)
		{
			var vendor = GetTokenVendorForId(vendorId);
			if (vendor == null)
			{
				message.WriteVariableInt32(0);
				string locText = Localiser.GetString(textDB, player, (int)TokenVendorManagerTextDB.TextID.FAILED_FIND_STOCK);
				message.Write(locText);
				return;
			}
			var stock = vendor.GetStock();
			message.WriteVariableInt32(stock.Count);
			for (int i = 0; i < stock.Count; i++)
			{
				message.WriteVariableInt32(stock[i].TokenVendorStockId);
				message.WriteVariableInt32(stock[i].ItemTemplateId);
				message.WriteVariableInt32(stock[i].TokenVendorCostId);
			}

			var stockCostIdsInvolved = vendor.GetStockCostIds();
			message.WriteVariableInt32(stockCostIdsInvolved.Count);
			for (int i = 0; i < stockCostIdsInvolved.Count; i++)
			{
				int stockCostId = stockCostIdsInvolved[i];
				var tokenVendorCost = GetTokenVendorCostForId(stockCostId);
				message.WriteVariableInt32(stockCostId);
				message.WriteVariableInt32(tokenVendorCost.Count);

				for (int j = 0; j < tokenVendorCost.Count; j++)
				{
					message.WriteVariableInt32(tokenVendorCost[j].ItemTemplateId);
					message.WriteVariableInt32(tokenVendorCost[j].Quantity);
				}
			}
		}

		//public void WriteVendorStockToMessage(NetOutgoingMessage message, int vendorId)
  //      {
  //          var vendor = GetTokenVendorForId(vendorId);
  //          if (vendor == null)
  //          {
  //              message.WriteVariableInt32(0);
  //              message.Write("Failed to find stock for this vendor.");
  //              return;
  //          }
  //          var stock = vendor.GetStock();
  //          message.WriteVariableInt32(stock.Count);
  //          for (int i = 0; i < stock.Count; i++)
  //          {
  //              message.WriteVariableInt32(stock[i].TokenVendorStockId);
  //              message.WriteVariableInt32(stock[i].ItemTemplateId);

  //              message.WriteVariableInt32(stock[i].TokenVendorCostId);
  //          }

  //          var stockCostIdsInvolved = vendor.GetStockCostIds();
  //          message.WriteVariableInt32(stockCostIdsInvolved.Count);
  //          for (int i = 0; i < stockCostIdsInvolved.Count; i++)
  //          {
  //              int stockCostId = stockCostIdsInvolved[i];
  //              var tokenVendorCost = GetTokenVendorCostForId(stockCostId);
  //              message.WriteVariableInt32(stockCostId);
  //              message.WriteVariableInt32(tokenVendorCost.Count);
                
  //              for (int j = 0; j < tokenVendorCost.Count; j++)
  //              {
  //                  message.WriteVariableInt32(tokenVendorCost[j].ItemTemplateId);
  //                  message.WriteVariableInt32(tokenVendorCost[j].Quantity);
  //              }
  //          }
  //      }

        public Item PurchaseItemRequest(Character character, int tokenVendorId, int tokenVendorStockId, int quantity)
        {
            
            var tokenVendor = GetTokenVendorForId(tokenVendorId);
            if (tokenVendor == null) return null;

            var tokenVendorStockItem = tokenVendor.GetStockForId(tokenVendorStockId);

            //Program.processor.CraftingTemplateManager.IsTokenItemRecipe(tokenVendorStockItem.TokenVendorCostId);
            if (tokenVendorStockItem == null) return null;

            var tokenVendorStockCost = GetTokenVendorCostForId(tokenVendorStockItem.TokenVendorCostId);

            bool canPurchase = CheckCharacterHasCostForPurchase(character, tokenVendorStockCost, quantity);

            if (canPurchase)
            {
                
                Item newItem = character.m_inventory.AddNewItemToCharacterInventory(tokenVendorStockItem.ItemTemplateId, quantity, false);
                if (newItem != null)
                {
                    int newid = newItem.m_inventory_id;
                    character.m_inventory.RemoveTokenCostForItem(tokenVendorId, tokenVendorStockCost, quantity);
                    character.m_QuestManager.checkIfItemAffectsStage(newItem.m_template_id);

					Program.processor.updateShopHistory(character.m_zone.m_zone_id, tokenVendorId, newid, tokenVendorStockItem.ItemTemplateId, 
						1, -1, (int)character.m_character_id, "Bought TK Stock Id: " + tokenVendorStockItem.TokenVendorStockId);
					ItemTemplate template = ItemTemplateManager.GetItemForID(tokenVendorStockItem.ItemTemplateId);

                    //send a confirmation message to the player
                    if (template != null)
                    {
                        Program.Display(character.m_name + " spend tokens." + template.m_item_name + " template ID:" + tokenVendorStockItem.ItemTemplateId + " inventory ID:" + newid);
                        NetOutgoingMessage msg = Program.Server.CreateMessage();
                        msg.WriteVariableUInt32((uint)NetworkCommandType.SimpleMessageForThePlayer);

                        // this id matches the cooking vendor - so use 'made' instead of purchased
                        // we handle this messaging elsewhere now.  But keep this id around for later
                        // for every other token vendor use normal purchasing language
                        if (tokenVendorId != 221)
                        {
                            
                            if (quantity > 1)
                            {
                                if (!template.m_item_name.EndsWith("s"))
                                {
                                    string locText = Localiser.GetString(textDB, character.m_player, (int)TokenVendorManagerTextDB.TextID.PURCHASED_ITEMS);
                                    locText = string.Format(locText, quantity, template.m_loc_item_name[character.m_player.m_languageIndex]);
                                    msg.Write(locText);
                                }
                                else
                                {
                                    string locText = Localiser.GetString(textDB, character.m_player, (int)TokenVendorManagerTextDB.TextID.PURCHASED_ITEMS_NO_S);
                                    locText = string.Format(locText, quantity, template.m_loc_item_name[character.m_player.m_languageIndex]);
                                    msg.Write(locText);
                                }




                            }
                            else
                            {
                                string locText = Localiser.GetString(textDB, character.m_player, (int)TokenVendorManagerTextDB.TextID.PURCHASED_ITEM);
                                locText = string.Format(locText, template.m_loc_item_name[character.m_player.m_languageIndex]);
                                msg.Write(locText);
                            }

                            Program.processor.SendMessage(msg, character.m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.SimpleMessageForThePlayer);
                        }
                        
                        if (Program.m_LogAnalytics)
                        {
                            AnalyticsMain logAnalytics = new AnalyticsMain(true);

                            System.Text.StringBuilder sb = new System.Text.StringBuilder();
                            int totalTokenCost = 0;

                            sb.Append("Token List:");
                            for (int cnt = 0; cnt < tokenVendorStockCost.Count; cnt++)
                            {
                                sb.Append(",");
                                sb.Append(tokenVendorStockCost[cnt].ItemTemplateId.ToString());

                                totalTokenCost += tokenVendorStockCost[cnt].Quantity;
                            }

                            logAnalytics.inGameShopTokenPurchase(character.m_player, totalTokenCost, sb.ToString(), template.m_item_name, /*itemTypeReceived*/ template.m_subtype.ToString(), quantity);
                        }
                    }
                    else
                    {
                        Program.Display(character.m_name + " brought unknown template ID:" + tokenVendorStockItem.ItemTemplateId + " inventory ID:" + newid);
                    }
                    return newItem;
                } 
            }
            return null;
        }

      

        private bool CheckCharacterHasCostForPurchase(Character character, List<TokenVendorCost> tokenVendorStockCost, int quantity)
        {
            var canAfford = new List<bool>();
            foreach (var cost in tokenVendorStockCost)
            {
                if (character.m_inventory.checkHasItems(cost.ItemTemplateId) >= cost.Quantity*quantity)
                {
                    canAfford.Add(true);
                }             
            }

            if (canAfford.Count == tokenVendorStockCost.Count)
            {
                return true;
            }
            return false;
        }
    }
}
