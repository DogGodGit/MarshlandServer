using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;

namespace MainServer.Factions
{
    /// <summary>
    /// Route all faction related messages through here
    /// </summary>
    class FactionNetworkManager
    {
        private enum FactionMessageType { none, currentFactions, firstContact, pointsChanged, levelChanged, }

        internal void ProcessMessage(NetIncomingMessage msg, Player player)
        {
            var messageType = (FactionMessageType)msg.ReadVariableInt32();

            switch (messageType)
            {

                case FactionMessageType.none:
                    
                        //ProcessPurchaseCustomisationRequest(msg, player);
                        break;
                case FactionMessageType.pointsChanged:

                        //ProcessPurchaseCustomisationRequest(msg, player);
                        break;
                                    
                default:
                    break;
            }
        }


        internal void SendFactionsCurrent(Player player, Character currentCharacter)
        {
            //send it out
            NetOutgoingMessage factionMsg = Program.Server.CreateMessage();
            factionMsg.WriteVariableUInt32((uint)NetworkCommandType.FactionsMessage);
            factionMsg.WriteVariableUInt32((uint)FactionNetworkManager.FactionMessageType.currentFactions);
            factionMsg.WriteVariableInt32(currentCharacter.FactionManager.Factions.Count);
            foreach (var faction in currentCharacter.FactionManager.Factions)
            {
                factionMsg.WriteVariableInt32(faction.Value.Id);
                factionMsg.WriteVariableInt32(faction.Value.Points);
            }            
            Program.processor.SendMessage(factionMsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.FactionsMessage);
        }


        internal void SendFactionsPoints(Player player, Faction faction, int pointsDelta)
        {             
		    //send it out
			NetOutgoingMessage factionMsg = Program.Server.CreateMessage();
			factionMsg.WriteVariableUInt32((uint)NetworkCommandType.FactionsMessage);
            factionMsg.WriteVariableUInt32((uint)FactionNetworkManager.FactionMessageType.pointsChanged);
		    factionMsg.WriteVariableInt32(faction.Id);
            factionMsg.WriteVariableInt32(faction.Points);
		    factionMsg.WriteVariableInt32(pointsDelta);
			Program.processor.SendMessage(factionMsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.FactionsMessage);			
	    }

       

        internal void SendFactionsLevel(Player player, Faction faction, FactionTemplate factionTemplate, FactionTemplate.Level level, bool repIncreased)
        {
            //send it out
            NetOutgoingMessage factionMsg = Program.Server.CreateMessage();
            factionMsg.WriteVariableUInt32((uint)NetworkCommandType.FactionsMessage);
            factionMsg.WriteVariableUInt32((uint)FactionNetworkManager.FactionMessageType.levelChanged);
            factionMsg.WriteVariableInt32(faction.Id);
            factionMsg.WriteVariableInt32(faction.Points);            
            // writing a bool to network message
            factionMsg.Write(repIncreased ? (byte)1 : (byte)0);
            factionMsg.Write(factionTemplate.Name);
            factionMsg.Write(level.title);
            Program.processor.SendMessage(factionMsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.FactionsMessage);
        }
    }
}
