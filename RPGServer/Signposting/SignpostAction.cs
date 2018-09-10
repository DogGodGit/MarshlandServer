using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MainServer.player_offers;
using XnaGeometry;
using MainServer.Localise;

namespace MainServer.Signposting
{
	/// <summary>
	/// Describes an action to be taken once a stage has become active
	/// </summary>
	class SignpostAction
    {
		public class SignpostActionTextDB : TextEnumDB
		{
			public SignpostActionTextDB() : base(nameof(SignpostAction), typeof(TextID)) { }

			public enum TextID
			{
				TUTORIAL_MAIL_HEAD,                  //"Greetings adventure"
				TUTORIAL_MAIL_SUBJECT,               //"Greetings adventure,\n\nThank you for answering our summons in this time of peril. In order to aid your journey I've given order for a small package to be left to help you on your way.\n\nLord McLir"
				TUTORIAL_MAIL_SENDER,                //"Lord McLir"
			}
		}
		public static SignpostActionTextDB textDB = new SignpostActionTextDB();

		internal enum ActionType
        {
            None=0,
            /// <summary>
            /// no param
            /// </summary>
            Message=1,
            /// <summary>
            /// x;y;z;zoneID
            /// </summary>
            HelpPoint=2,
            /// <summary>
            /// spawnIDTempID;zoneID
            /// </summary>
            HelpSpawnPoint=3,
            /// <summary>
            /// mobTempID;zoneID
            /// </summary>
            HelpMobTemplate=4,
            /// <summary>
            /// itemTempID;zoneID
            /// </summary>
            HelpItemTemplate=5,
            /// <summary>
            /// x;y;z;angle;zoneid
            /// </summary>
            Teleport=6,
            /// <summary>
            /// x;y;z;angle;zoneid
            /// </summary>
            TeleportRequest=7,
            /// <summary>
            /// no param
            /// </summary>
            PopupFile=8,
            /// <summary>
            /// itemTemplateID;quantity
            /// </summary>
            GiveItem = 9,
            /// <summary>
            /// offerID;NumberOfHours;Quantity;Character_Limited;Item_Store
            /// </summary>
            ActivateOffer = 10,
            /// <summary>
            /// offerID
            /// </summary>
            RemoveOffer = 11,
            /// <summary>
            /// tutorialID
            /// </summary>
            StartTutorial = 12,
            /// <summary>
            /// no Val
            /// </summary>
            RegisterRequired = 13,
            /// <summary>
            /// no param
            /// </summary>
            RateUs = 14,

            OfferwallPopup = 15,
            SendFirstTimeID = 40,
            SendMailAttachedItemToPlayer = 41
        }
        int m_signpostID = -1;
        int m_actionID = -1;
        ActionType m_actionType = ActionType.None;
        List<float> m_actionParams = null;
        string m_message = "";
        internal ActionType Type
        {
            get { return m_actionType; }
        }
        internal SignpostAction(int signpostID,int actionID, ActionType actionType, List<float> actionParams, string message)
        {

            m_signpostID = signpostID;
            m_actionID = actionID;
            m_actionType = actionType;
            m_actionParams = actionParams;
            m_message = message;

        }
        static internal SignpostAction GetActionOfType(List<SignpostAction> actionsList, ActionType actionType)
        {
            SignpostAction action = null;

            for (int i = 0; i < actionsList.Count && action==null; i++)
            {
                SignpostAction currentAction=actionsList[i];
                if (currentAction.m_actionType == actionType)
                {
                    action = currentAction;
                }
            }
            return action;
        }
        /// <summary>
        /// if an action requires a player to confirm an action this will be called to carry out the action
        /// </summary>
        /// <param name="playerCharacter"></param>
        /// <param name="takeAction"></param>
        internal void ActionConfirmed(Character playerCharacter,bool takeAction)
        {
            if (takeAction == true)
            {
                 Player player = playerCharacter.m_player;
                 switch (m_actionType)
                 {
                     case ActionType.TeleportRequest:
                         {
                             if (m_actionParams.Count >= 5)
                             {
                                 float x = m_actionParams[0];
                                 float y = m_actionParams[1];
                                 float z = m_actionParams[2];
                                 float angle = m_actionParams[3];
                                 int zoneID = (int)m_actionParams[4];
                                 if (playerCharacter.m_zone != null)
                                 {
                                     playerCharacter.m_zone.ForceMoveCharacterToLocation(new Vector3(x, y, z), angle, zoneID, player);
                                 }
                             }
                             break;
                         }
                 }
            }
        }
        internal void TakeAction(Character playerCharacter)
        {
            Player player = playerCharacter.m_player;
            switch (m_actionType)
            {
                case ActionType.HelpItemTemplate:
                    {
                        if (m_actionParams.Count >= 2)
                        {
                            int itemID = (int)m_actionParams[0];
                            int zoneID = (int)m_actionParams[1];
                            Program.processor.SendShowPlayerHelpUsingID(player, zoneID, itemID, Character.Player_Help_Type.PickupItemTemplateID);
                        }
                        break;
                    }
                case ActionType.HelpMobTemplate:
                    {
                        if (m_actionParams.Count >= 2)
                        {
                            int mobTempID = (int)m_actionParams[0];
                            int zoneID = (int)m_actionParams[1];
                            Program.processor.SendShowPlayerHelpUsingID(player, zoneID, mobTempID, Character.Player_Help_Type.MobTemplateID);
                        }
                        break;
                    }
                case ActionType.HelpSpawnPoint:
                    {
                        if (m_actionParams.Count >= 2)
                        {
                            int spawnID = (int)m_actionParams[0];
                            int zoneID = (int)m_actionParams[1];
                            Program.processor.SendShowPlayerHelpUsingID(player, zoneID, spawnID, Character.Player_Help_Type.Mob);
                        }
                        break;
                    }
                case ActionType.HelpPoint:
                    {
                        if (m_actionParams.Count >= 4)
                        {
                            float x = m_actionParams[0];
                            float y = m_actionParams[1];
                            float z = m_actionParams[2];
                            int zoneID = (int)m_actionParams[3];
                            Program.processor.SendShowPlayerHelp(player, zoneID, new Vector3(x, y, z));
                        }
                        break;
                    }
                case ActionType.Message:
                    {
                        if (m_message.Length > 0)
                        {
							string localMessage = SignpostManager.GetLocaliseSignPostActionMessage(player, m_signpostID, m_actionID);
							Program.processor.SendXMLPopupMessage(true, player, (int)XML_Popup.Set_Popup_IDs.SPI_BasicPopup, XML_Popup.Popup_Type.None, "popup_template.txt", new List<string> { localMessage, " modal = \"true\" active = \"connected\"", "", "" }, false);
							//Program.processor.SendXMLPopupMessage(true, player, (int)XML_Popup.Set_Popup_IDs.SPI_BasicPopup, XML_Popup.Popup_Type.None, "popup_template.txt", new List<string> { m_message, " modal = \"true\" active = \"connected\"", "", "" }, false);

						}
                        break;
                    }
                case ActionType.Teleport:
                    {
                        if (m_actionParams.Count >= 5)
                        {
                            float x = m_actionParams[0];
                            float y = m_actionParams[1];
                            float z = m_actionParams[2];
                            float angle = m_actionParams[3];
                            int zoneID = (int)m_actionParams[4];
                            if (playerCharacter.m_zone != null)
                            {
                                playerCharacter.m_zone.ForceMoveCharacterToLocation(new Vector3(x, y, z), angle, zoneID, player);
                            }
                        }
                        break;
                    }
                case ActionType.TeleportRequest:
                    {
                        if (m_message.Length > 0)
                        {
							string localMessage = SignpostManager.GetLocaliseSignPostActionMessage(player, m_signpostID, m_actionID);
							XML_Popup newPopup = Program.processor.SendXMLPopupMessage(true, player, (int)XML_Popup.Set_Popup_IDs.SPI_TeleportRequest, XML_Popup.Popup_Type.None, "popup_confirm_temp.txt", new List<string> { localMessage, " active = \"connected\"", "", "" }, false);
							//XML_Popup newPopup = Program.processor.SendXMLPopupMessage(true, player, (int)XML_Popup.Set_Popup_IDs.SPI_TeleportRequest, XML_Popup.Popup_Type.None, "popup_confirm_temp.txt", new List<string> { m_message, " active = \"connected\"", "", "" }, false);
							newPopup.DataID = m_signpostID;
                        }
                        break;
                    }
                case ActionType.PopupFile:
                    {
                        if (m_message.Length > 0)
                        {
							string localMessage = SignpostManager.GetLocaliseSignPostActionMessage(player, m_signpostID, m_actionID);
							Program.processor.SendXMLPopupMessage(true, player, (int)XML_Popup.Set_Popup_IDs.SPI_BasicPopup, XML_Popup.Popup_Type.None, localMessage, new List<string> { }, false);
							//Program.processor.SendXMLPopupMessage(true, player, (int)XML_Popup.Set_Popup_IDs.SPI_BasicPopup, XML_Popup.Popup_Type.None, m_message, new List<string> { }, false);

						}
                        break;
                    }
                case ActionType.GiveItem:
                    {
                        if (m_actionParams.Count >= 2)
                        {
                            int templateID = (int)m_actionParams[0];
                            int quantity = (int)m_actionParams[1];

                            ItemTemplate itemTemp = ItemTemplateManager.GetItemForID(templateID);
                           
                                Item item = playerCharacter.m_inventory.AddNewItemToCharacterInventory(templateID, quantity, false);
                                if (item != null)
                                {
                                    Program.processor.updateShopHistory(-1, -1, item.m_inventory_id, item.m_template_id, quantity, 0, (int)playerCharacter.m_character_id, "Signpost");
                                    Program.Display(playerCharacter.Name + " gained " + quantity+ " of " + item.m_template.m_item_name + " inv_id=" + item.m_inventory_id + " from signpost " + m_signpostID);
                                    playerCharacter.m_inventory.SendInventoryUpdate();

                                }

                        }
                       
                        break;
                    }
                case ActionType.ActivateOffer:
                    {
                        if (m_actionParams.Count >= 5)
                        {
                            int offerID = (int)m_actionParams[0];
                            float hours = m_actionParams[1];
                            int quantity = (int)m_actionParams[2];
                            bool limitByCharacter = ((int)m_actionParams[3] !=0);
                            bool inItemShop = ((int)m_actionParams[4] !=0);

                            playerCharacter.OfferManager.AddOfferToPlayer(playerCharacter, offerID, hours, quantity, limitByCharacter, inItemShop, true, true);

                        }
                        break;
                    }
                case ActionType.RemoveOffer:
                    {
                        if (m_actionParams.Count >= 1)
                        {
                            int offerID = (int)m_actionParams[0];
                             playerCharacter.OfferManager.RemoveOfferFromPlayer(playerCharacter,offerID);

                        }

                        break;
                    }
                case ActionType.StartTutorial:
                    {
                        if (m_actionParams.Count >= 1)
                        {
                            int tutorialID = (int)m_actionParams[0];
                            Program.processor.StartTutorialMessage(player, tutorialID,false);

                        }

                        break;
                    }
                case ActionType.RegisterRequired:
                    {
                        if(player.m_registrationType == Player.Registration_Type.Guest)
                        {
                            Program.processor.SendRegisterAccountRequired(player);
                        }
                        break;
                    }
                case ActionType.RateUs:
                {
                    if (player.m_rateUsType == Player.RateUs_Type.NotAskedYet || player.m_rateUsType == Player.RateUs_Type.AskLater)
                    {
                        Program.processor.SendRateUs(player);
                    }
                    break;
                }
                case ActionType.OfferwallPopup:
                {
                    Program.processor.SendOfferwallPopup(player);
                    
                    break;
                }
                case ActionType.SendFirstTimeID:
                {
                    if (m_actionParams.Count >= 1)
                    {

                        int firstTimeID = (int)m_actionParams[0];
                        Program.processor.SendFirstTimeIDMessage(player, firstTimeID, false);
                    }

                    break;
                }
                case ActionType.SendMailAttachedItemToPlayer:
                {
                    string[] messageStrings = m_message.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                    if (m_actionParams.Count >= 2 && messageStrings.Length==3)
                    {

                        int itemID = (int)m_actionParams[0];
                        int Quantity = (int)m_actionParams[1];

						if (Enum.IsDefined(typeof(SignpostActionTextDB.TextID), messageStrings[0]))
						{
							messageStrings[0] = Localiser.GetString(textDB, player, (int)(SignpostActionTextDB.TextID)Enum.Parse(typeof(SignpostActionTextDB.TextID), messageStrings[0]));
						}
						if (Enum.IsDefined(typeof(SignpostActionTextDB.TextID), messageStrings[1]))
						{
							messageStrings[1] = Localiser.GetString(textDB, player, (int)(SignpostActionTextDB.TextID)Enum.Parse(typeof(SignpostActionTextDB.TextID), messageStrings[1]));
						}
						if (Enum.IsDefined(typeof(SignpostActionTextDB.TextID), messageStrings[2]))
						{
							messageStrings[2] = Localiser.GetString(textDB, player, (int)(SignpostActionTextDB.TextID)Enum.Parse(typeof(SignpostActionTextDB.TextID), messageStrings[2]));
						}

						Program.processor.SendMailWithItemToPlayer(player, messageStrings[0], messageStrings[1], itemID, Quantity, messageStrings[2]);
                    }

                    break;
                }
            }
        }
    }
}
