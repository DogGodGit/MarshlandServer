using System;
using Lidgren.Network;
using MainServer.Localise;

namespace MainServer.TokenVendors
{
    class TokenVendorNetworkManager
    {
		// #localisation
		public class TokenVendorNetworkManagerTextDB : TextEnumDB
		{
			public TokenVendorNetworkManagerTextDB() : base(nameof(TokenVendorNetworkManager), typeof(TextID)) { }

			public enum TextID
			{
				CANNOT_BUY_ITEM,           // "You can not buy this item at this time"
			}
		}
		public static TokenVendorNetworkManagerTextDB textDB = new TokenVendorNetworkManagerTextDB();

		private readonly ITokenVendorManager TokenVendorManger;

        public TokenVendorNetworkManager(ITokenVendorManager tokenVendorManager)
        {
            TokenVendorManger = tokenVendorManager;
        }

        private enum TokenVendorMessageType
        {
            TokenVendorRequest = 1,
            TokenVendorRequestReply = 2,
            PurchaseItemRequest = 3,
            PurchaseItemRequestReply = 4
        }

        internal void ProcessMessage(NetIncomingMessage msg, Player player)
        {
            var messageType = (TokenVendorMessageType)msg.ReadVariableInt32();

            switch (messageType)
            {
                case TokenVendorMessageType.TokenVendorRequest:
                {
                    ProcessTokenVendorRequest(msg, player);
                    break;
                }
                case TokenVendorMessageType.PurchaseItemRequest:
                {
                    ProcessPurchaseItemRequest(msg, player);
                    break;
                }
                default:
                    break;
            }
        }
        
        private void ProcessTokenVendorRequest(NetIncomingMessage msg, Player player)
        {
            int tokenVendorId = msg.ReadVariableInt32();

            //check the requirements
            ITokenVendor itokenVendor = TokenVendorManger.GetTokenVendorForId(tokenVendorId);
            TokenVendor tokenVendor = (TokenVendor) itokenVendor;
            if (tokenVendor != null)
            {
                if (tokenVendor.CharacterMeetsRequirment(player.m_activeCharacter) == false)
                {
                    NetOutgoingMessage msgreply = Program.Server.CreateMessage();
                    msgreply.WriteVariableUInt32((uint)NetworkCommandType.SimpleMessageForThePlayer);
                    msgreply.Write("You are not high enough in the faction for this");
                    Program.processor.SendMessage(msgreply, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.SimpleMessageForThePlayer);

                    return;
                }
            }

            var message = CreateOutgoingMessageOfType(TokenVendorMessageType.TokenVendorRequestReply);
			TokenVendorManger.WriteVendorStockToMessage(player, message, tokenVendorId);
			//TokenVendorManger.WriteVendorStockToMessage(message, tokenVendorId);

			Program.processor.SendMessage(message, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.TokenVendorMessage);
        }
        
        private void ProcessPurchaseItemRequest(NetIncomingMessage msg, Player player)
        {
            int tokenVendorId = msg.ReadVariableInt32();
            int tokenVendorStockId = msg.ReadVariableInt32();
            int quantityRequested = msg.ReadVariableInt32();
            if (quantityRequested == 0)
                quantityRequested = 1;

            var purchasedItem = TokenVendorManger.PurchaseItemRequest(player.m_activeCharacter, tokenVendorId, tokenVendorStockId, quantityRequested);
            
            if (tokenVendorId == 221)
            {
                if (purchasedItem != null)
                {
                    Program.processor.CompetitionManager.UpdateCompetition(player.m_activeCharacter, Competitions.CompetitionType.COOKED_ITEM, purchasedItem.m_template_id);
                }
            }

            var message = CreateOutgoingMessageOfType(TokenVendorMessageType.PurchaseItemRequestReply);
            
            if (purchasedItem == null)
            {
                message.Write(Convert.ToByte(false));
				string locText = Localiser.GetString(textDB, player, (int)TokenVendorNetworkManagerTextDB.TextID.CANNOT_BUY_ITEM);
				message.Write(locText);
			}
            else
            {
                message.Write(Convert.ToByte(true));
                message.WriteVariableInt32(purchasedItem.m_inventory_id);
            }
            player.m_activeCharacter.m_inventory.WriteInventoryWithMoneyToMessage(message);
            Program.processor.SendMessage(message, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.RequestShopReply);
        }

        private NetOutgoingMessage CreateOutgoingMessageOfType(TokenVendorMessageType messageType)
        {
            NetOutgoingMessage msg = Program.Server.CreateMessage();
            msg.WriteVariableUInt32((uint)NetworkCommandType.TokenVendorMessage);
            msg.WriteVariableInt32((int)messageType);

            return msg;
        }
    }
}
