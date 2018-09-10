using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using MainServer.TokenVendors;

namespace MainServer.Crafting
{
    class CraftingNetworkHandler
    {
        private enum CraftingMessageType
        {
           requestKnownRecipes = 1,
           processKnownRecipes = 2,
           requestCraft = 3,
           processCraft = 4,
           interruptCraft = 5,
           updateFavourite = 6
        }

        internal void ProcessMessage(NetIncomingMessage msg, Player player)
        {
            var messageType = (CraftingMessageType)msg.ReadVariableInt32();

            if (player != null && player.m_activeCharacter != null)
                player.m_activeCharacter.PlayerIsBusy = true;

            switch (messageType)
            {

                case CraftingMessageType.requestKnownRecipes:
                    {
                        SendCharacterRecipeList(player, player.m_activeCharacter);
                        break;
                    }
                case CraftingMessageType.requestCraft:
                    {
                        ProcessCraftingRequest(msg, player, player.m_activeCharacter);
                        break;
                    }
                case CraftingMessageType.interruptCraft:
                    {
                        ProcessCraftingInterrupt(msg, player);
                        break;
                    }
                case CraftingMessageType.updateFavourite:
                    {
                        processUpdateFavourite(msg,player);
                        break;
                    }
                default:
                    break;
            }
        }

        private void SendCharacterRecipeList(Player player, Character currentCharacter)
        {
            List<CraftingManager.knownRecipe> recipeList = currentCharacter.CraftingManager.recipeList;

            NetOutgoingMessage msg = Program.Server.CreateMessage();
            msg.WriteVariableUInt32((uint)NetworkCommandType.CraftingMessage);
            msg.WriteVariableUInt32((uint)CraftingMessageType.processKnownRecipes);
            msg.WriteVariableInt32(recipeList.Count);

            foreach (CraftingManager.knownRecipe i in recipeList)
            {
                msg.WriteVariableInt32(i.recipeID);
                msg.Write(i.isFavourited);
                
            }

            msg.WriteVariableInt32(currentCharacter.LevelCooking);
            msg.Write(currentCharacter.GetProfessionExperience());
            Program.processor.SendMessage(msg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.CraftingMessage);
        }

        public  void SendCraftingResponse(Player player,int craftingResultID, int craftingXpGained)
        {
            NetOutgoingMessage msg = Program.Server.CreateMessage();
            msg.WriteVariableUInt32((uint)NetworkCommandType.CraftingMessage);
            msg.WriteVariableUInt32((uint)CraftingMessageType.processCraft);
            msg.WriteVariableInt32(craftingResultID);
            msg.WriteVariableInt32(player.m_activeCharacter.LevelCooking);
            msg.Write(player.m_activeCharacter.GetProfessionExperience());
            msg.WriteVariableInt32(craftingXpGained);            
            Program.processor.SendMessage(msg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.CraftingMessage);
        }

        private void ProcessCraftingInterrupt(NetIncomingMessage msg,Player player)
        {
            bool interruptState = msg.ReadBoolean();
            player.m_activeCharacter.CraftingManager.interruptCraft(interruptState);
        }

        public void processUpdateFavourite(NetIncomingMessage msg,Player player)
        {
            int recipeID = msg.ReadVariableInt32();
            bool isFavourite = msg.ReadBoolean();

            //Verify we have this recipe and if so, update the database
            if (player.m_activeCharacter.CraftingManager.recipeList.Find(x=>x.recipeID==recipeID)!=null)
            {

                player.m_activeCharacter.CraftingManager.recipeList.Find(x => x.recipeID == recipeID).isFavourited = isFavourite;
                String test = String.Format("UPDATE `character_recipes` SET `is_favourited`= {0} WHERE `character_id`= {1} and`recipe_id`= {2}", isFavourite, player.m_activeCharacter.m_character_id, recipeID);
                Program.Display(test);
                player.m_activeCharacter.m_db.runCommand(test);
   
            }
        }

        private void ProcessCraftingRequest(NetIncomingMessage msg,Player player,Character currentCharacter)
        {

            int recipeID   = msg.ReadVariableInt32();
            int optionalID = msg.ReadVariableInt32();

            //if timer hasnet elapsed, reject new crafting for now

            //otherwise start a new timer and note inthe manager wha we
            //are tring to build
            if (currentCharacter.CraftingManager.isCrafting == false)
            {
                currentCharacter.CraftingManager.QueueCraftin(recipeID, optionalID);
                return;
                
            }
            //double craftTime = currentCharacter.CraftingManager.getCraftingTemplateForID(recipeID).craftingTime;
            
            //int craftingResult = currentCharacter.CraftingManager.formulateCraftingResponse(recipeID);
   
        }

       
    }
}
