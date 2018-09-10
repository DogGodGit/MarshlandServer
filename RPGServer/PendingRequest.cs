using MainServer.Localise;

namespace MainServer
{
    /// <summary>
    /// Holds details on a request for a certain type of action towards the target
    /// Currently only on 1-1 actions, could be extended to deal with clans or parties (or could be left up to the request thype to pull out the correct data)
    /// </summary>
    class PendingRequest
    {
		// #localisation
		public class PendingRequestTextDB : TextEnumDB
		{
			public PendingRequestTextDB() : base(nameof(PendingRequest), typeof(TextID)) { }

			public enum TextID
			{
				TRADE_TIME_OUT,             //"The trade timed out"
				OTHER_CANCELLED_TRADE,      //"{name0} cancelled the trade"
				OTHER_LOGGED_OUT,           //"{name0} has logged out"
				DUEL_CANCELLED,             //"The duel was cancelled"
				DUEL_TIME_OUT,              //"The duel timed out"
			}
		}
		public static PendingRequestTextDB textDB = new PendingRequestTextDB();

		internal enum REQUEST_TYPE
        {
            RT_NONE=0,
            RT_TRADE =1,
            RT_DUEL =2
        }
        internal enum CANCEL_CONDITION
        {
            CC_NONE=0,
            CC_TIME_OUT=1,
            CC_LOGOUT=2,
            CC_SELF_CANCEL = 3,
            CC_OTHER_CANCEL =4
        }
        internal enum REQUEST_STATUS
        {
            RS_NONE=0,
            RS_AWAITING_REPLY =1
        }
        //the player that the request is aimed towards
        Player m_requestTarget=null;
        //request type
        REQUEST_TYPE m_requestType =  REQUEST_TYPE.RT_NONE;
        double m_timeToBeDestroyed = 0;
        
        internal PendingRequest()
        {

        }
        internal PendingRequest(Player targetPlayer, REQUEST_TYPE requestType, REQUEST_STATUS requestStatus, double timeAlive )
        {
            m_requestTarget = targetPlayer;
            m_requestType = requestType;
            m_timeToBeDestroyed = Program.MainUpdateLoopStartTime() + timeAlive;

        }
        ~PendingRequest()
        {

        }
        internal bool shouldBeDestroyed(double currentTime)
        {
            if (currentTime > m_timeToBeDestroyed)
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// For Whatever reason the action can no longer take place
        /// send a message to each side
        /// </summary>
        /// <param name="owningPlayer"></param>
        internal void CancelRequest(Player owningPlayer, CANCEL_CONDITION cancelCondition)
        {
            //check what type of request it is
            switch (m_requestType)
            {

                case REQUEST_TYPE.RT_TRADE:
                    {
                       /* NetOutgoingMessage outmsg = Program.Server.CreateMessage();
                        outmsg.WriteVariableUInt32((uint)NetworkCommandType.TradeMessage);
                        outmsg.WriteVariableInt32((int)TRADE_MESSAGE.TM_OtherCancelTrade);
                        outmsg.WriteVariableInt32((int)m_requestTarget.m_activeCharacter.m_character_id);
                        Program.processor.SendMessage(outmsg, owningPlayer.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.TradeMessage);//was OtherCancelTrade

                        NetOutgoingMessage ownermsg = Program.Server.CreateMessage();
                        ownermsg.WriteVariableUInt32((uint)NetworkCommandType.TradeMessage);
                        ownermsg.WriteVariableInt32((int)TRADE_MESSAGE.TM_OtherCancelTrade);
                        ownermsg.WriteVariableInt32((int)owningPlayer.m_activeCharacter.m_character_id);
                        Program.processor.SendMessage(ownermsg, m_requestTarget.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.TradeMessage);//was OtherCancelTrade
                        */
                        switch (cancelCondition)
                        {
                            case CANCEL_CONDITION.CC_TIME_OUT:
                                {
                                    /*//it didn't cancel so send thier own character down as the canceller 
                                    Program.processor.SendOtherCancelTrade(owningPlayer, (int)owningPlayer.m_activeCharacter.m_character_id);
                                    Program.processor.SendOtherCancelTrade(m_requestTarget, (int)m_requestTarget.m_activeCharacter.m_character_id);*/
                                    Program.processor.SendOtherCancelTrade(owningPlayer, (int)m_requestTarget.m_activeCharacter.m_character_id);
                                    Program.processor.SendOtherCancelTrade(m_requestTarget, (int)owningPlayer.m_activeCharacter.m_character_id);

									string playerLocText = Localiser.GetString(textDB, owningPlayer, (int)PendingRequestTextDB.TextID.TRADE_TIME_OUT);
									string otherLocText = Localiser.GetString(textDB, m_requestTarget, (int)PendingRequestTextDB.TextID.TRADE_TIME_OUT);
									Program.processor.sendSystemMessage(playerLocText, owningPlayer, true, SYSTEM_MESSAGE_TYPE.TRADE);
									Program.processor.sendSystemMessage(otherLocText, m_requestTarget, true, SYSTEM_MESSAGE_TYPE.TRADE);
									break;
                                }
                            case CANCEL_CONDITION.CC_SELF_CANCEL:
                                {
                                    Program.processor.SendOtherCancelTrade(owningPlayer, (int)m_requestTarget.m_activeCharacter.m_character_id);
                                    Program.processor.SendOtherCancelTrade(m_requestTarget, (int)owningPlayer.m_activeCharacter.m_character_id);
									string locText = Localiser.GetString(textDB, m_requestTarget, (int)PendingRequestTextDB.TextID.OTHER_CANCELLED_TRADE);
									locText = string.Format(locText, owningPlayer.m_activeCharacter.Name);
									Program.processor.sendSystemMessage(locText, m_requestTarget, true, SYSTEM_MESSAGE_TYPE.TRADE);
									break;
                                }
                            case CANCEL_CONDITION.CC_OTHER_CANCEL:
                                {
                                    Program.processor.SendOtherCancelTrade(owningPlayer, (int)m_requestTarget.m_activeCharacter.m_character_id);
                                    Program.processor.SendOtherCancelTrade(m_requestTarget, (int)owningPlayer.m_activeCharacter.m_character_id);
									string locText = Localiser.GetString(textDB, owningPlayer, (int)PendingRequestTextDB.TextID.OTHER_CANCELLED_TRADE);
									locText = string.Format(locText, m_requestTarget.m_activeCharacter.Name);
									Program.processor.sendSystemMessage(locText, owningPlayer, true, SYSTEM_MESSAGE_TYPE.TRADE);
									break;
                                 }
                            case CANCEL_CONDITION.CC_LOGOUT:
                                {
                                    Program.processor.SendOtherCancelTrade(owningPlayer, (int)m_requestTarget.m_activeCharacter.m_character_id);
                                    Program.processor.SendOtherCancelTrade(m_requestTarget, (int)owningPlayer.m_activeCharacter.m_character_id);
									string locText = Localiser.GetString(textDB, m_requestTarget, (int)PendingRequestTextDB.TextID.OTHER_LOGGED_OUT);
									locText = string.Format(locText, owningPlayer.m_activeCharacter.Name);
									Program.processor.sendSystemMessage(locText, m_requestTarget, true, SYSTEM_MESSAGE_TYPE.TRADE);
									break;
                                }
                            default:
                                //the request was cancelled normally
                                Program.processor.SendOtherCancelTrade(owningPlayer, (int)owningPlayer.m_activeCharacter.m_character_id);
                                Program.processor.SendOtherCancelTrade(m_requestTarget, (int)m_requestTarget.m_activeCharacter.m_character_id);
                                break;
                        }
                        


                        break;
                    }
                case REQUEST_TYPE.RT_DUEL:
                    {
						string playerLocText = Localiser.GetString(textDB, owningPlayer, (int)PendingRequestTextDB.TextID.DUEL_CANCELLED);
						string otherLocText = Localiser.GetString(textDB, m_requestTarget, (int)PendingRequestTextDB.TextID.DUEL_CANCELLED);
						if (cancelCondition == CANCEL_CONDITION.CC_TIME_OUT)
						{
							playerLocText = Localiser.GetString(textDB, owningPlayer, (int)PendingRequestTextDB.TextID.DUEL_TIME_OUT);
							otherLocText = Localiser.GetString(textDB, m_requestTarget, (int)PendingRequestTextDB.TextID.DUEL_TIME_OUT);
						}
						if (owningPlayer.m_activeCharacter.m_zone != null)
						{
							owningPlayer.m_activeCharacter.m_zone.SendServerDuelReply(owningPlayer, m_requestTarget.m_activeCharacter.m_character_id, false, playerLocText);
							m_requestTarget.m_activeCharacter.m_zone.SendServerDuelReply(m_requestTarget, owningPlayer.m_activeCharacter.m_character_id, false, otherLocText);
						}
						break;
                    }
                default:
                    break;
            }

            owningPlayer.m_activeCharacter.CurrentRequest = null;
            m_requestTarget.m_activeCharacter.CurrentRequest = null;
        }
        /// <summary>
        /// blanks both players requests withought taking any other action
        /// </summary>
        internal void CloseDown(Player owningPlayer)
        {
            owningPlayer.m_activeCharacter.CurrentRequest = null;
            m_requestTarget.m_activeCharacter.CurrentRequest = null;
        }

        internal bool IsRequestFor(uint characterID, REQUEST_TYPE type)
        {
            bool isSameRequest = false;
            if(type == m_requestType && m_requestTarget.m_activeCharacter!=null&&m_requestTarget.m_activeCharacter.m_character_id==characterID)
            {
                isSameRequest = true;
            }


            return isSameRequest;
        }


    }
}
