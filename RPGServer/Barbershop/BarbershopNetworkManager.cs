using System;
using System.Collections.Generic;
using Lidgren.Network;
using MainServer.Localise;

namespace MainServer.TokenVendors
{
    class BarbershopNetworkManager
    {
		// #localisation
		public class BarbershopNetworkManagerTextDB : TextEnumDB
		{
			public BarbershopNetworkManagerTextDB() : base(nameof(BarbershopNetworkManager), typeof(TextID)) { }

			public enum TextID
			{
				ENOUGH_BARBERSHOP_COUPONS,		// "enough barbershop coupons"
				ERROR_IN_BARBERSHOP,			// "Error in barbershop."
				PURCHASE_SUCCESSFULL			// "Purchase successful."

			}
		}
		public static BarbershopNetworkManagerTextDB textDB = new BarbershopNetworkManagerTextDB();

		private enum BarbershopMessageType
        {
           
            PurchaseCustomisationRequest = 1,
            PurchaseCustomisationRequestReply = 2,
            updateCharacterDetails = 3
        }

        internal void ProcessMessage(NetIncomingMessage msg, Player player)
        {
            var messageType = (BarbershopMessageType)msg.ReadVariableInt32();

            switch (messageType)
            {
                
                case BarbershopMessageType.PurchaseCustomisationRequest:
                    {
                        ProcessPurchaseCustomisationRequest(msg, player);
                        break;
                    }
                //update is now a verification check of changes
                case BarbershopMessageType.updateCharacterDetails:
                {
                    ProcessCharacterDetailsUpdate(msg, player);
                    break;
                }

                default:
                    break;
            } 
        }

       
        /// <summary>
        /// process change appearance request
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="player"></param>
        private void ProcessPurchaseCustomisationRequest(NetIncomingMessage msg, Player player)
        {
           //we read in everything from the client, not just changes
            int cost = msg.ReadVariableInt32();
            int face_id = msg.ReadVariableInt32();
            int skin_colour_id = msg.ReadVariableInt32();
            int hair_id = msg.ReadVariableInt32();
            int hair_colour_id = msg.ReadVariableInt32();
            int face_accessory_id = msg.ReadVariableInt32();
            int face_accessory_colour_id = msg.ReadVariableInt32();
            float scale = msg.ReadFloat();

            List<int> changedIDs = new List<int>();
            
            //check validity of changes
            bool costVerified = checkCostAccurate(cost, face_id, skin_colour_id, hair_id, hair_colour_id,
                face_accessory_id, face_accessory_colour_id, ref scale, player, changedIDs);

            //check if we can afford it
            bool canAfford = checkAffordability(cost, player);


            Program.Display("Receiving message purchaseSuccess." + canAfford + " costVerified." + costVerified);

            var message = CreateOutgoingMessageOfType(BarbershopMessageType.PurchaseCustomisationRequestReply);


            //check if somethng has gone wrong
            if (!canAfford)
            {
                message.Write(Convert.ToByte(false));
				string locText = Localiser.GetString(textDB, player, (int)BarbershopNetworkManagerTextDB.TextID.ENOUGH_BARBERSHOP_COUPONS);
				message.Write(locText);
				message.Write(Convert.ToByte(true));
			}
            //else we can't afford it
            else if (!costVerified)
            {
                message.Write(Convert.ToByte(false));
				string locText = Localiser.GetString(textDB, player, (int)BarbershopNetworkManagerTextDB.TextID.ERROR_IN_BARBERSHOP);
				message.Write(locText);
				message.Write(Convert.ToByte(false));
			}
            //else everything is fine, apply changes
            else
            {
                LogBarberShopAnalytics(player, changedIDs, cost);
                message.Write(Convert.ToByte(true));
				string locText = Localiser.GetString(textDB, player, (int)BarbershopNetworkManagerTextDB.TextID.PURCHASE_SUCCESSFULL);
				message.Write(locText);
				message.Write(Convert.ToByte(false));
				//deduct tokens now
				player.m_activeCharacter.m_inventory.RemoveTokenCostForCustomisation(60485, cost); 
                //and set character apperaance

                player.m_activeCharacter.setCharacterAppearance(face_id, skin_colour_id, hair_id, 
                    hair_colour_id, face_accessory_id, face_accessory_colour_id, scale);
            }

            //all done, refresh inventory and send message to client
            player.m_activeCharacter.m_inventory.WriteInventoryWithMoneyToMessage(message);
            Program.processor.SendMessage(message, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.BarbershopMessage);
        }

       
        private void ProcessCharacterDetailsUpdate(NetIncomingMessage msg, Player player)
        {
            int face_id = msg.ReadVariableInt32();
            int skin_colour_id = msg.ReadVariableInt32();
            int hair_id = msg.ReadVariableInt32();
            int hair_colour_id = msg.ReadVariableInt32();
            int face_accessory_id = msg.ReadVariableInt32();
            int face_accessory_colour_id = msg.ReadVariableInt32();
            float scale = msg.ReadFloat();

            //use this to verify our changes
            if (player.m_activeCharacter.m_face_id != face_id)
            {
                Program.Display("Barbershop error, face_id mismatch.");
            }

            //use this to verify our changes
            if (player.m_activeCharacter.m_skin_colour != skin_colour_id)
            {
                Program.Display("Barbershop error, skin_colour mismatch.");
            }
            //use this to verify our changes
            if (player.m_activeCharacter.m_hair_id != hair_id)
            {
                Program.Display("Barbershop error, hair_id mismatch.");
            }
            //use this to verify our changes
            if (player.m_activeCharacter.m_hair_colour != hair_colour_id)
            {
                Program.Display("Barbershop error, hair_colour mismatch.");
            }
            //use this to verify our changes
            if (player.m_activeCharacter.m_face_acc_id != face_accessory_id)
            {
                Program.Display("Barbershop error, accessory mismatch.");
            }
            //use this to verify our changes
            if (player.m_activeCharacter.m_face_acc_colour!= face_accessory_colour_id)
            {
                Program.Display("Barbershop error, accessory_colour mismatch.");
            }
            //use this to verify our changes
            if (Math.Abs(player.m_activeCharacter.Scale - scale) > 0.001)
            {
                Program.Display("Barbershop error, scale mismatch.");
            }
            //player.m_activeCharacter.setCharacterAppearance(face_id,skin_colour_id,hair_id,
            //    hair_colour_id,face_accessory_id,face_accessory_colour_id,scale);
        }

        #region check validity

        /// <summary>
        /// Check if the players character has enough tokens to buy this customisation
        /// </summary>
        /// <param name="cost">token cost</param>
        /// <param name="player">player - we'll use their active ahcaracter</param>
        /// <returns>true if they have the token id (60485)</returns>
        private bool checkAffordability(int cost, Player player)
        {
            bool canAfford;
            
            //We're only ever checking against one type of token (for now).
            if (player.m_activeCharacter.m_inventory.checkHasItems(60485) >= cost)
            {
               canAfford = true;               
            }
            else
            {
                canAfford = false;
            }
            Program.Display("Cost."+cost +" Player Tokens."+player.m_activeCharacter.m_inventory.checkHasItems(60485));

            return canAfford;
        }

       
        private bool checkCostAccurate(int cost, int face_id, int skin_colour_id, int hair_id, int hair_colour_id, 
            int face_accessory_id, int face_accessory_colour_id, ref float scale, Player player, IList<int> changedIDs)
        {

            //these are the players current ids
            List<int> originalIDs = new List<int>();
            originalIDs.Add(player.m_activeCharacter.m_face_id);
            originalIDs.Add(player.m_activeCharacter.m_skin_colour);
            originalIDs.Add(player.m_activeCharacter.m_hair_id);
            originalIDs.Add(player.m_activeCharacter.m_hair_colour);
            originalIDs.Add(player.m_activeCharacter.m_face_acc_id);
            originalIDs.Add(player.m_activeCharacter.m_face_acc_colour);
           
            List<int> newIDs = new List<int>();
            newIDs.Add(face_id);
            newIDs.Add(skin_colour_id);
            newIDs.Add(hair_id);
            newIDs.Add(hair_colour_id);
            newIDs.Add(face_accessory_id);
            newIDs.Add(face_accessory_colour_id);
            

            int predictedCost = 0;

            for (int i = 0; i < originalIDs.Count; i++)
            {
                if (newIDs[i] != originalIDs[i])
                {
                    predictedCost++;
                    changedIDs.Add(newIDs[i]); // set this entry with the value of the changed id
                }
                else
                {
                    changedIDs.Add(-1); // otherwise it has not changed, were not interested, -1 it
                }
            }

            //Comparing scale            
            if (Math.Abs(player.m_activeCharacter.Scale - scale) > 0.001)
            {
                predictedCost++;
            }
                
            if (predictedCost == cost)
            {
                //player.m_activeCharacter.m_face_id = face_id;
                //player.m_activeCharacter.m_skin_colour = skin_colour_id;
                //player.m_activeCharacter.m_hair_id = hair_id;
                //player.m_activeCharacter.m_hair_colour = hair_colour_id;
                //player.m_activeCharacter.m_face_acc_id = face_accessory_id ;
                //player.m_activeCharacter.m_face_acc_colour = face_accessory_id;

                return true;
            }
            else
            {
                //If we're dealing with a legacy scale, make our customisation equal to the characters original scale.
                if (checkForLegacyScale(player.m_activeCharacter.Scale))
                {
                    predictedCost--;

                   
                    scale = player.m_activeCharacter.UnmodifiedScale;

                    if (predictedCost==cost)
                    {
                        return true;
                    }
                }

                return false;

            }
        }

        #endregion

        private bool checkForLegacyScale(float playerScale)
        {
            List<float> validScales = new List<float>(){0.9f,0.925f,0.95f,0.975f,1f,1.025f,1.05f,1.075f,1.1f};

            foreach (float i in validScales)
            {
                if (Math.Abs(playerScale - i) < 0.001)
                {
                    return false;
                }
            }
            
            return true;
            
        }

        private NetOutgoingMessage CreateOutgoingMessageOfType(BarbershopMessageType messageType)
        {
            NetOutgoingMessage msg = Program.Server.CreateMessage();
            msg.WriteVariableUInt32((uint)NetworkCommandType.BarbershopMessage);
            msg.WriteVariableInt32((int)messageType);

            return msg;
        }

       
        /// <summary>
        ///  Logs any purchased barbershop transactions, logging the parameters which have been changed and which ids they have been changed to 
        /// </summary>
        /// <param name="player"></param>
        /// <param name="changedIDs"></param>
        /// <param name="cost"></param>
        private void LogBarberShopAnalytics(Player player, IList<int> changedIDs, int cost)
        {
            if (Program.m_LogAnalytics)
            {
                try
                {
                    AnalyticsMain logAnalytics = new AnalyticsMain(false);
                    logAnalytics.BarberShopUsed(player,
                                                Program.m_worldID.ToString(),
                                                cost,
                                                changedIDs[0],  // face id
                                                changedIDs[1],  // skin colour id
                                                changedIDs[2],  // hair id
                                                changedIDs[3],  // hair colour id
                                                changedIDs[4],  // face accessory id
                                                changedIDs[5]); // face accessory colour id
                }
                catch
                {
                    Program.Display("BarberShopNetworkManager.cs - LogBarberShopAnalytics() failed to create the barber shop analytic event!");
                }
            }
        }

    }
}
