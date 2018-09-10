using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Windows.Forms;
using Lidgren.Network;

namespace MainServer.DailyLoginReward
{
    class DailyRewardNetworkManager
    {
        private enum DailyRewardMessageType
        {
            RewardRequest = 1,
            RewardItineraryRequest = 2,
            RewardRecievedReply = 3,
            ItineraryRequestReply = 4,
            RewardsDisabledReply = 5
        }

        internal void ProcessMessage(NetIncomingMessage msg, Player player)
        {
            var messageType = (DailyRewardMessageType) msg.ReadInt32();

            if (DailyRewardManager.DAILY_REWARDS_ACTIVE) // Parse rewards as normal
            {
                switch (messageType)
                {
                    case DailyRewardMessageType.RewardRequest:
                    {
                        ProcessPlayerLogin(player, DailyRewardManager.RewardChainType.Incremental);
                        //ProcessPlayerLogin(player, DailyRewardManager.RewardChainType.Consecutive);
                        break;
                    }
                    case DailyRewardMessageType.RewardItineraryRequest:
                    {
                        ProcessRewardItineraryRequest(player);
                        break;
                    }
                    default:
                    {
                        Program.Display("DailyRewardNetworkManager.cs- ProcessMessage() recieved an unexpected message type");
                        break;
                    }
                }
            }
            else // TO DO: Send back a "daily offers are currently unavailabe message depending on request
            {
                switch (messageType)
                {
                    case DailyRewardMessageType.RewardItineraryRequest: //someone is trying to open the window.
                    {
                        SendRejectionMessage(player);
                        break;
                    }
                    default:
                    {
                        break;
                    }
                }
                return;
            }
        }

        internal void ProcessPlayerLogin(Player i_Player, DailyRewardManager.RewardChainType i_RewardChainType)
        {
            //bool newCharacter = false;
            var reward = DailyRewardManager.ProcessPlayerLogin(i_Player, i_RewardChainType/*, ref newCharacter*/);

            if (reward != null)
            {
                SendRewardNotification(i_Player, reward, DailyRewardMessageType.RewardRecievedReply);
            }
        }

        internal void ProcessRewardItineraryRequest(Player i_Player)
        {
            var reward = DailyRewardManager.GetTodaysReward(i_Player);

            if (reward != null)
            {
                SendRewardNotification(i_Player, reward, DailyRewardMessageType.ItineraryRequestReply);
            }
        }

        private void SendRewardNotification(Player i_Player, DailyRewardTemplate i_reward, DailyRewardMessageType messageType)
        {
            var message = CreateOutgoingMessageOfType(messageType);
            message.Write(i_reward.ItemTemplateID);
            message.Write(i_reward.Quantity);
            message.Write(i_reward.StepID);
            message.Write(i_reward.IsPriorityReward);
            message.Write((i_Player.m_activeCharacter.m_lastRewardRecieved + DailyRewardManager.GetDailyRewardInterval()).ToString("dd/MM/yyyy HH:mm:ss"));
            message.Write(i_Player.m_activeCharacter.m_numRecievedRewards);

            AddItineraryToMessage(message);
            Program.processor.SendMessage(message, i_Player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.DailyRewardMessage);
        }

        private NetOutgoingMessage CreateOutgoingMessageOfType(DailyRewardMessageType messageType)
        {
            NetOutgoingMessage msg = Program.Server.CreateMessage();
            msg.WriteVariableUInt32((uint)NetworkCommandType.DailyRewardMessage);
            msg.WriteVariableInt32((int)messageType);

            return msg;
        }

        private void AddItineraryToMessage(NetOutgoingMessage msg)
        {
            if (DailyRewardTemplateManager.m_RewardsLoaded)
            {
                List<DailyRewardTemplate> itinerary = DailyRewardTemplateManager.GetDailyRewardList();
                msg.Write(itinerary.Count); //write amount of rewards to message
                for (int cnt = 0; cnt < itinerary.Count; cnt++)
                {
                    msg.Write(itinerary[cnt].ItemTemplateID);
                    msg.Write(itinerary[cnt].Quantity);
                    msg.Write(itinerary[cnt].StepID);
                    msg.Write(itinerary[cnt].IsPriorityReward);
                }
            }
        }

        private void SendRejectionMessage(Player i_Player)
        {
            var message = CreateOutgoingMessageOfType(DailyRewardMessageType.RewardsDisabledReply);
            Program.processor.SendMessage(message, i_Player.connection, NetDeliveryMethod.ReliableOrdered,
                                          NetMessageChannel.NMC_Normal, NetworkCommandType.DailyRewardMessage);
        }

    }
}
