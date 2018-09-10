using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using MainServer.player_offers;
using MainServer.Localise;
using MySql.Data.MySqlClient;

namespace MainServer
{
    class XML_Popup
    {

		// #localisation
		public class XML_PopupTextDB : TextEnumDB
		{
			public XML_PopupTextDB() : base(nameof(XML_Popup), typeof(TextID)) { }

			public enum TextID
			{
				NATIVEX_NOT_AVAILABLE,          // "NativeX is not currently available."
				TRIALPLAY_NOT_AVAILABLE,        // "trialPay is not currently available"
				SPECIAL_OFFER_COMING_SOON,      // "Special Offers Coming Soon!<br/>Please check back later."
				OFFER_EXPIRED,                  // "The selected offer has expired"
				ERROR_LOCATING_OFFER            // "An issue has occured locating this offer, please try again later"
			}
		}
		public static XML_PopupTextDB textDB = new XML_PopupTextDB();

        internal static double TIME_BETWEEN_OFFER_SENDS = 3;

        internal enum Popup_Type
        {
            None,
            Questionair,
            NotificationChanging,
            OfferWall,
            SpecialOffers
        };
        internal enum Set_Popup_IDs
        {
            SPI_NewNotifications=1,
            SPI_NotificationsDeviceChange=2,
            SPI_BasicPopup = 10,
            SPI_TeleportRequest = 11,
            SPI_OfferWall = 20,
            SPI_OpenTrialPay = 21,
            SPI_OpenW3i = 22,
            SPI_SpecialOffers =30,
            SPI_SpecialOffersList = 31,
            SPI_SpecialOffersView= 32,
            SPI_MAG_BOX_OPEN=40,
            SPI_MAG_BOX_NEXT = 41,
            SPI_MAG_BOX_CLOSE = 42,
            SPI_RateUsPopup = 43
        }
        int m_popupID=-1;
        int m_dataID = -1;
        Popup_Type m_type = Popup_Type.None;
        XML_PopupData m_popupData = null;
        internal int DataID
        {
            set { m_dataID = value; }
        }

        internal XML_Popup(int popupID, Popup_Type popupType)
        {
            m_popupID = popupID;
            m_type = popupType;
        }
        internal int PopupID
        {
            get { return m_popupID; }
        }
        internal Popup_Type PopupType
        {
            get { return m_type; }
        }
        internal XML_PopupData PopupData
        {
            get { return m_popupData; }
            set { m_popupData = value; }
        }

        internal void OptionChosen(Player player, int optionID)
        {
            Set_Popup_IDs popupID = (Set_Popup_IDs)m_popupID;
            switch (popupID)
            {
                case Set_Popup_IDs.SPI_NewNotifications:
                case Set_Popup_IDs.SPI_NotificationsDeviceChange:
                    {
                        //ok pressed
                        if (optionID == 1)
                        {
                            SetNewNotificationData(player);
                            player.m_savedNotificationToken = player.m_notificationToken;
                            player.m_savedNotificationDevice = player.m_notificationDevice;
                            player.m_savedNotificationType = player.m_notificationType;
                        }
                        else
                        {
                            player.m_notificationToken = player.m_savedNotificationToken;
                            player.m_notificationDevice = player.m_savedNotificationDevice;
                        }
                       
                        break;
                    }
                case Set_Popup_IDs.SPI_OfferWall:
                    {
                        player.m_openPopups.Add(this);
                        if (optionID == 1)
                        {
                            if (Program.m_w3iActive >= 2)
                            {
                                //string modString = "All offers are the responsibility of NativeX.";
                                //XML_Popup newPopup = Program.processor.SendXMLPopupMessage(true, player, (int)XML_Popup.Set_Popup_IDs.SPI_OpenW3i, XML_Popup.Popup_Type.None, "popup_confirm_temp.txt", new List<string> { modString, " active = \"connected\"", "", "" }, false);
                                double currentTimeinSecs = Program.SecondsFromReferenceDate();
                                //if the time hasn't gone back an hour +
                                //it's has been longer than the resend time
                                if ((player.m_timeOfLastOfferWall > currentTimeinSecs) ||
                                    (player.m_timeOfLastOfferWall + TIME_BETWEEN_OFFER_SENDS) < currentTimeinSecs)
                                {
                                    Program.processor.SendOpenW3iOfferWall(player);
                                    player.m_timeOfLastOfferWall = currentTimeinSecs;
                                }
                               // Program.processor.SendOpenW3iOfferWall(player);
                            }
                            else
                            {
								string locText = Localiser.GetString(textDB, player, (int)XML_PopupTextDB.TextID.NATIVEX_NOT_AVAILABLE);
								Program.processor.SendXMLPopupMessage(true, player, (int)XML_Popup.Set_Popup_IDs.SPI_BasicPopup, XML_Popup.Popup_Type.None, "popup_template.txt", new List<string> { locText, " modal = \"true\" active = \"connected\"", "", "" }, false);
                                Program.processor.SendCloseXMLPopup(player, m_popupID);
                            }
                        }
                        else if (optionID == 2)
                        {
                            //trialPay
                            
                           
                            if (Program.m_trialpayActive >= 2)
                            {
                                //string modString = "All offers are the responsibility of Trialpay.";
                                //XML_Popup newPopup = Program.processor.SendXMLPopupMessage(true, player, (int)XML_Popup.Set_Popup_IDs.SPI_OpenTrialPay, XML_Popup.Popup_Type.None, "popup_confirm_temp.txt", new List<string> { modString, " active = \"connected\"", "", "" }, false);
                       
                                Program.processor.SendOpenWebPage("https://www.trialpay.com/dispatch/f746a4d68cd9d9a456266a49e7219449?sid=" + player.m_account_id, "TrialPay", true, player);
                            }
                            else
                            {
								string locText = Localiser.GetString(textDB, player, (int)XML_PopupTextDB.TextID.TRIALPLAY_NOT_AVAILABLE);
								Program.processor.SendXMLPopupMessage(true, player, (int)XML_Popup.Set_Popup_IDs.SPI_BasicPopup, XML_Popup.Popup_Type.None, "popup_template.txt", new List<string> { locText, " modal = \"true\" active = \"connected\"", "", "" }, false);
                                Program.processor.SendCloseXMLPopup(player, m_popupID);
                            }
                        }
                        else if (optionID == 3)
                        {
                            //PersonalOffers
                            if (SpecialOfferTemplate.SPECIAL_OFFERS_ACTIVE == true)
                            {
                               player.m_activeCharacter.OfferManager.PrepareToSendSpecialOfferWall(player.m_activeCharacter);
                            }
                            else
                            {
                                List<int> dataList = new List<int>();
                                dataList.Add(-1);
                                dataList.Add(-1);
                                dataList.Add(1);
								string locText = Localiser.GetString(textDB, player, (int)XML_PopupTextDB.TextID.SPECIAL_OFFER_COMING_SOON);
								Program.processor.SendXMLPopupMessage(true, player, (int)XML_Popup.Set_Popup_IDs.SPI_SpecialOffers, XML_Popup.Popup_Type.None, "popup_template.txt", new List<string> { locText, " modal = \"true\" active = \"connected\"", "", "" }, false);
                                Program.processor.SendCloseXMLPopup(player, m_popupID);
                            }
                        }
                        break;
                    }
                case Set_Popup_IDs.SPI_OpenTrialPay:
                    {
                        //bool takeAction = (optionID == 1);
                        if (optionID == 1)
                        {
                            Program.processor.SendOpenWebPage("https://www.trialpay.com/dispatch/f746a4d68cd9d9a456266a49e7219449?sid=" + player.m_account_id, "trialpay", true, player);
                        }
                        else
                        {
                            Program.processor.ProcessOpenFreePlatinumWall(player);
                        }
                        Program.processor.SendCloseXMLPopup(player, m_popupID);
                        break;
                    }
                case Set_Popup_IDs.SPI_OpenW3i:
                    {
                        if (optionID == 1)
                        {
                            Program.processor.SendOpenW3iOfferWall(player);
                        }
                        else
                        {
                            Program.processor.ProcessOpenFreePlatinumWall(player);
                        }
                        Program.processor.SendCloseXMLPopup(player, m_popupID);
                        break;
                    }
                case Set_Popup_IDs.SPI_SpecialOffers:
                    {
                        //PersonalOffers
                        if (SpecialOfferTemplate.SPECIAL_OFFERS_ACTIVE)
                        {
                            player.m_activeCharacter.OfferManager.PrepareToSendSpecialOfferWall(player.m_activeCharacter);
                        }
                        //else
                        //{
                        //    List<int> dataList = new List<int>();
                        //    dataList.Add(-1);
                        //    dataList.Add(-1);
                        //    dataList.Add(1);
                        //    string modString = "Special Offers Coming Soon!<br/>Please check back later.";
                        //    Program.processor.SendXMLPopupMessage(true, player, (int)XML_Popup.Set_Popup_IDs.SPI_SpecialOffers, XML_Popup.Popup_Type.None, "popup_template.txt", new List<string> { modString, " modal = \"true\" active = \"connected\"", "", "" }, false);
                        //    //Program.processor.SendOpenGuiPage(player, GameScreen.ItemShop, dataList);
                        //    Program.processor.SendCloseXMLPopup(player, m_popupID);
                        //}
                        break;
                    }
                case Set_Popup_IDs.SPI_TeleportRequest:
                    {
                        Signposting.Signpost teleportSignpost = Signposting.SignpostManager.GetSignpostForID(m_dataID);
                        if (teleportSignpost != null)
                        {
                            Signposting.SignpostAction teleportAction = Signposting.SignpostAction.GetActionOfType(teleportSignpost.Actions, MainServer.Signposting.SignpostAction.ActionType.TeleportRequest);

                            bool takeAction = (optionID == 1);
                            if (player.m_activeCharacter != null)
                            {
                                teleportAction.ActionConfirmed(player.m_activeCharacter, takeAction);
                            }

                        }
                        Program.processor.SendCloseXMLPopup(player, m_popupID);
                        break;
                    }
                case Set_Popup_IDs.SPI_MAG_BOX_OPEN:
                    {
                        if (optionID == 2)
                        {
                            player.CloseMagicBoxPopups();
                        }
                        else
                        {
                            Program.processor.SendPlaySound2D(player, "celtic_chest_1");
                        }
                        break;
                    }
                case Set_Popup_IDs.SPI_MAG_BOX_NEXT:
                    {
                        if (optionID == 2)
                        {
                            player.CloseMagicBoxPopups();
                        }
                        else
                        {
                            string soundName = "celtic_chest_";
                            if (Program.getRandomNumber(100) < 50)
                            {
                                soundName += "2";
                            }
                            else
                            {
                                soundName += "3";
                            }
                            Program.processor.SendPlaySound2D(player, soundName);
                        }
                        break;
                    }
                case Set_Popup_IDs.SPI_MAG_BOX_CLOSE:
                    {
                        if (optionID == 2)
                        {
                            player.CloseMagicBoxPopups();
                        }
                        else
                        {
                            if (m_popupData != null)
                            {
                                if (m_popupData.m_postString != "")
                                {
                                    Program.processor.sendSystemMessage(m_popupData.m_postString, player, true,
                                        SYSTEM_MESSAGE_TYPE.ITEM_USE);
                                }
                            }

                            if (player.m_activeCharacter.m_inventory.GetItemFromInventoryID(player.m_activeCharacter.m_lastItemIdUsed, false) != null)
                            {
                                float coolDownForItem = player.m_activeCharacter.m_inventory.GetCooldownForItem(player.m_activeCharacter.m_lastItemIdUsed);
                                string errorString = player.m_activeCharacter.m_inventory.useItem(player.m_activeCharacter.m_lastItemIdUsed,player.m_activeCharacter.m_character_id, 0);
                                player.m_activeCharacter.m_inventory.SendUseItemReply(errorString, coolDownForItem);
                            }
                            else
                            {
                                Program.processor.m_premiumShop.SendOpenItemShop(player);
                            }

                        }
                        break;
                    }
                case Set_Popup_IDs.SPI_SpecialOffersList:
                    {
                        if (optionID == 10)
                        {
                            //Program.processor.ProcessOpenFreePlatinumWall(player);
                            Program.processor.SendCloseXMLPopup(player, m_popupID);
                        }
                        else
                        {
                            int offerID = optionID - CharacterOfferData.OFFER_ID_OFFSET;
                            CharacterOfferData clickedOffer = player.m_activeCharacter.OfferManager.GetActiveOfferForID(offerID);
                            if (clickedOffer != null)
                            {
                                clickedOffer.SendOfferPage(player);
                            }
                            else
                            {
								string locText = Localiser.GetString(textDB, player, (int)XML_PopupTextDB.TextID.OFFER_EXPIRED);
								Program.processor.SendXMLPopupMessage(true, player, (int)XML_Popup.Set_Popup_IDs.SPI_BasicPopup, XML_Popup.Popup_Type.None, "popup_template.txt", new List<string> { locText, " modal = \"true\" active = \"connected\"", "", "" }, false);
                            }
                            Program.processor.SendCloseXMLPopup(player, m_popupID);
                        }
                        break;
                    }
                case Set_Popup_IDs.SPI_SpecialOffersView:
                    {
                        if (optionID == 10)
                        {
                            player.m_openPopups.Add(this);
                            player.m_activeCharacter.OfferManager.PrepareToSendSpecialOfferWall(player.m_activeCharacter);
                        }
                        else
                        {
                            int offerID = optionID - CharacterOfferData.OFFER_ID_OFFSET;
                            CharacterOfferData clickedOffer = null;
                            if (offerID == m_popupData.m_postDataID)
                            {
                                clickedOffer = player.m_activeCharacter.OfferManager.GetActiveOfferForID(offerID);
                            }
                            if (clickedOffer != null)
                            {
                                clickedOffer.RedeemOffer(1, player);

                            }
                            else
                            {
								string locText = Localiser.GetString(textDB, player, (int)XML_PopupTextDB.TextID.ERROR_LOCATING_OFFER);
								Program.processor.SendXMLPopupMessage(true, player, (int)XML_Popup.Set_Popup_IDs.SPI_BasicPopup, XML_Popup.Popup_Type.None, "popup_template.txt", new List<string> { locText, " modal = \"true\" active = \"connected\"", "", "" }, false);
                            }
                        }
                        
                        break;
                    }
                case Set_Popup_IDs.SPI_RateUsPopup:
                    {
                        player.updateRateUsType(optionID);
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
           // player.SendAttemptPremiumPurchase(1);
            /*if (optionID == 2)
            {
                Program.processor.SendCloseXMLPopup(player, m_popupID);
                Program.processor.SendXMLPopupMessage(player, 3, Popup_Type.None, "<body background = \"mail_char_box.png\" capping = \"15\" cellpadding = \"15\">lol lol lol <a href = \"CLOSE_AND_SEND;1\"><img src = \"smiley.png\" alt =\"smiley_alt.png\"></a></body>", false);
            }
            if (optionID == 3)
            {
                Program.processor.SendOpenWebPage("http://lodestone.finalfantasyxiv.com/pl/index.html", "Final Fantasy", true, player);
            }*/
        }
        internal void SetNewNotificationData(Player player)
        {
            string savedNotificationTokenString = "";
            if (player.m_notificationToken != "")
            {
                savedNotificationTokenString = player.m_notificationToken;
            }
			//Program.processor.m_universalHubDB.runCommandSync("update account_details set notification_device_id='" + player.m_notificationDevice + "',notification_token='" + savedNotificationTokenString + "',notification_types=" + player.m_notificationType + " where account_id=" + player.m_account_id);

			List<MySqlParameter> sqlParams = new List<MySqlParameter>();
			sqlParams.Add(new MySqlParameter("@notification_device_id", player.m_notificationDevice));
			sqlParams.Add(new MySqlParameter("@notification_token", savedNotificationTokenString));
			sqlParams.Add(new MySqlParameter("@notification_types", player.m_notificationType));
			sqlParams.Add(new MySqlParameter("@account_id", player.m_account_id));

			Program.processor.m_universalHubDB.runCommandSyncWithParams("update account_details set notification_device_id=@notification_device_id,notification_token=@notification_token,notification_types=@notification_types where account_id=@account_id", sqlParams.ToArray());

		}
        static internal void OptionChosen(Player player, int popupID,int optionID){
            bool popupFound = false;
            lock (player.m_newPopups)
            {
                player.m_openPopups.AddRange(player.m_newPopups);
                player.m_newPopups.Clear();
            }

            for(int i=0;i<player.m_openPopups.Count&&popupFound==false; i++){
                XML_Popup currentPopup = player.m_openPopups[i];
                if(currentPopup!=null&& currentPopup.PopupID==popupID){
                    currentPopup.OptionChosen(player,optionID);
                    player.m_openPopups.Remove(currentPopup);
                    popupFound = true;
                }
            }
        }
        static internal XML_Popup GetPopupForID(Player player, int popupID)
        {
            XML_Popup popup = null;
            for (int i = 0; i < player.m_openPopups.Count && popup == null; i++)
            {
                 XML_Popup currentPopup = player.m_openPopups[i];
                 if (currentPopup != null && currentPopup.PopupID == popupID)
                 {
                     popup = currentPopup;
                 }
            }
            return popup;
        }
    };

    class XML_PopupData
    {
        internal string m_postString = "";
        internal int m_postDataID = -1;
    };
}
