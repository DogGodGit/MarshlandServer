using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using System.IO;
using System.Text.RegularExpressions;
using MainServer.Localise;

namespace MainServer.player_offers
{

    class CharacterOfferData
    {
		// #localisation
		public class CharacterOfferDataTextDB : TextEnumDB
		{
			public CharacterOfferDataTextDB() : base(nameof(CharacterOfferData), typeof(TextID)) { }

			public enum TextID
			{
				HOUR_LEFT,		// "{hoursLeft0} hour"
				HOURS_LEFT,		// "{hoursLeft0} hours"
				DAYS_LEFT,		// "{daysLeft0} days"
				UNLIMITED,		// "Unlimited"
				REMAINING,		// "{quantity0} Remaining"
			}
		}
		public static CharacterOfferDataTextDB textDB = new CharacterOfferDataTextDB();

		SpecialOfferDetails m_offerDetails = null;
        int m_numberRedeemed = -1;
        int m_itemShopID = -1;

        internal static int OFFER_ID_OFFSET = 1000;
        internal SpecialOfferDetails OfferDetails
        {
            get { return m_offerDetails; }
        }
        internal int NumberRedeemed
        {
            get { return m_numberRedeemed; }
        }
        internal int ItemShopID
        {
            get { return m_itemShopID; }
            set { m_itemShopID = value; }
        }
        internal CharacterOfferData(SpecialOfferDetails offerDetails, int numberRedeemed)
        {
            m_offerDetails = offerDetails;
            m_numberRedeemed = numberRedeemed;
        }
		internal void WriteLocaliseItemToMessage(NetOutgoingMessage outmsg, Player player)
		{
			outmsg.WriteVariableInt32(m_itemShopID);
			SpecialOfferTemplate template = m_offerDetails.OfferTemplate;
			outmsg.WriteVariableInt32(template.ItemTemplateID);
			outmsg.WriteVariableInt32(template.Quantity);
			outmsg.WriteVariableInt32(template.Price);
			outmsg.Write(SpecialOfferTemplateManager.GetLocaliseSpecialOfferName(player, template.OfferID));
			outmsg.Write(SpecialOfferTemplateManager.GetLocaliseSpecialOfferDesc(player, template.OfferID));
			outmsg.WriteVariableInt32(template.ItemShopTabID);
		}

		private StringBuilder timeStringBuilder = new StringBuilder();
	    private StringBuilder listStringBuilder = new StringBuilder();


		/// <summary>
		/// Time left - will be in hours if less that 2 days
		/// </summary>
		/// <returns></returns>
		/* string TimeRemainingString()
		 {

			 //clear string builder
			 timeStringBuilder.Clear();

			 DateTime currentTime = DateTime.Now;
			 int daysLeft = (m_offerDetails.EndDate - currentTime).Days;
			 if (m_offerDetails.EndDate != DateTime.MinValue)
			 {
				 if (daysLeft < 2)
				 {
					 int hoursLeft = (int)Math.Floor((m_offerDetails.EndDate - currentTime).TotalHours);
					 if (hoursLeft != 1)
					 {
						 timeStringBuilder.AppendFormat("{0} hours", hoursLeft);                        
					 }
					 else
					 {
						 timeStringBuilder.AppendFormat("{0} hours", hoursLeft);                        
					 }
				 }
				 else
				 {
					 timeStringBuilder.AppendFormat("{0} days", daysLeft);                    
				 }
			 }
			 else
			 {
				 timeStringBuilder.Append("Unlimited");                
			 }

			 return timeStringBuilder.ToString();

		 }*/

		/// <summary>
		/// Time left - will be in hours if less that 2 days
		/// </summary>
		/// <returns></returns>
		string TimeRemainingString(Player player)
		{
			string locText;
			//clear string builder
			timeStringBuilder.Clear();

			DateTime currentTime = DateTime.Now;
			int daysLeft = (m_offerDetails.EndDate - currentTime).Days;
			if (m_offerDetails.EndDate != DateTime.MinValue)
			{
				if (daysLeft < 2)
				{
					int hoursLeft = (int)Math.Floor((m_offerDetails.EndDate - currentTime).TotalHours);
					if (hoursLeft != 1)
					{
						locText = Localiser.GetString(textDB, player, (int)CharacterOfferDataTextDB.TextID.HOURS_LEFT);
						locText = String.Format(locText, hoursLeft);
					}
					else
					{
						locText = Localiser.GetString(textDB, player, (int)CharacterOfferDataTextDB.TextID.HOUR_LEFT);
						locText = String.Format(locText, hoursLeft);
					}
				}
				else
				{
					locText = Localiser.GetString(textDB, player, (int)CharacterOfferDataTextDB.TextID.DAYS_LEFT);
					locText = String.Format(locText, daysLeft);
				}
			}
			else
			{
				locText = Localiser.GetString(textDB, player, (int)CharacterOfferDataTextDB.TextID.UNLIMITED);
			}

			return locText;
		}

		/// <summary>
		/// Time left - will be in hours if less that 2 days
		/// </summary>
		/// <returns></returns>
		/*internal string GetListPopupString()
        {
            //string listString = "";
	        listStringBuilder.Clear();

			//before 
			//listString += "<tr> <td height=\"42\" valign=\"middle\" width=\"25%\"><p><a href=\"SEND;" + (OFFER_ID_OFFSET + m_offerDetails.OfferTemplate.OfferID) + "\"><img src=\"Textures/popups/offer_popup/offer_button_viewoffer\" alt=\"Textures/popups/offer_popup/offer_button_viewoffer\" height=\"30\" width=\"84\" /></a></p></td>";
			//listString+="<td valign=\"middle\" align = \"left\">"+ m_offerDetails.OfferTemplate.OfferName+"</td>";            
			//listString += "<td valign=\"middle\" width=\"25%\">" + TimeRemainingString() + " </td>";
			//listString += "</tr>"; 

			//after - we just want the info without the markup
			listStringBuilder.Append("|"); //seperate entries with |
			listStringBuilder.Append((OFFER_ID_OFFSET + m_offerDetails.OfferTemplate.OfferID));			
			listStringBuilder.Append(";"); 
			listStringBuilder.Append(m_offerDetails.OfferTemplate.OfferName);
			if (m_offerDetails.OfferTemplate.OfferNameFlash != String.Empty)
			{
				listStringBuilder.Append("#");
				listStringBuilder.Append(m_offerDetails.OfferTemplate.OfferNameFlash);
			}
			listStringBuilder.Append(";");
	        listStringBuilder.Append(TimeRemainingString());

			return listStringBuilder.ToString();
            //return listString;
        }*/

		/// <summary>
		/// Time left - will be in hours if less that 2 days
		/// </summary>
		/// <returns></returns>
		internal string GetListPopupString(Player player)
		{
			//string listString = "";
			listStringBuilder.Clear();

			//before 
			//listString += "<tr> <td height=\"42\" valign=\"middle\" width=\"25%\"><p><a href=\"SEND;" + (OFFER_ID_OFFSET + m_offerDetails.OfferTemplate.OfferID) + "\"><img src=\"Textures/popups/offer_popup/offer_button_viewoffer\" alt=\"Textures/popups/offer_popup/offer_button_viewoffer\" height=\"30\" width=\"84\" /></a></p></td>";
			//listString+="<td valign=\"middle\" align = \"left\">"+ m_offerDetails.OfferTemplate.OfferName+"</td>";            
			//listString += "<td valign=\"middle\" width=\"25%\">" + TimeRemainingString() + " </td>";
			//listString += "</tr>"; 

			//after - we just want the info without the markup
			listStringBuilder.Append("|"); //seperate entries with |
			listStringBuilder.Append((OFFER_ID_OFFSET + m_offerDetails.OfferTemplate.OfferID));
			listStringBuilder.Append(";");
			listStringBuilder.Append(SpecialOfferTemplateManager.GetLocaliseSpecialOfferName(player, m_offerDetails.OfferTemplate.OfferID));
			if (m_offerDetails.OfferTemplate.OfferNameFlash != String.Empty)
			{
				listStringBuilder.Append("#");
				listStringBuilder.Append(m_offerDetails.OfferTemplate.OfferNameFlash);
			}
			listStringBuilder.Append(";");
			listStringBuilder.Append(TimeRemainingString(player));

			return listStringBuilder.ToString();
			//return listString;
		}

		internal void SendOfferPage(Player player)
        {

            List<string> stringList =  new List<string> ();
            SpecialOfferTemplate template = m_offerDetails.OfferTemplate;
			//title
			stringList.Add(SpecialOfferTemplateManager.GetLocaliseSpecialOfferName(player, template.OfferID));
			//description
			stringList.Add(SpecialOfferTemplateManager.GetLocaliseSpecialOfferDesc(player, template.OfferID));
			//image file
			stringList.Add(template.OfferImage);
			//timeRemaining
			stringList.Add(TimeRemainingString(player));
			//ID to Send
			stringList.Add((OFFER_ID_OFFSET + m_offerDetails.OfferTemplate.OfferID).ToString());
            //price
            stringList.Add(m_offerDetails.OfferTemplate.Price.ToString());
            //quantityRemaining
            if (m_offerDetails.Quantity > 0)
            {
				string locText = Localiser.GetString(textDB, player, (int)CharacterOfferDataTextDB.TextID.REMAINING);
				locText = String.Format(locText, (m_offerDetails.Quantity - m_numberRedeemed));
				stringList.Add(locText);    
			}
            else
            {
				string locText = Localiser.GetString(textDB, player, (int)CharacterOfferDataTextDB.TextID.UNLIMITED);
				stringList.Add(locText);
			}

			//details of a particular special offer
            XML_Popup newPopup = Program.processor.SendXMLPopupMessage(true, player, (int)XML_Popup.Set_Popup_IDs.SPI_SpecialOffersView, XML_Popup.Popup_Type.None, 
				"special_offer_details_prefab", stringList, false);
            XML_PopupData popupData = new XML_PopupData();
            popupData.m_postDataID = template.OfferID;
            newPopup.PopupData = popupData;
       
        }
        internal Item RedeemOffer(int quantity, Player player)
        {
            int originalQuantity = quantity;
            if ((m_offerDetails.Quantity>0) && (m_numberRedeemed + quantity > m_offerDetails.Quantity))
            {
                quantity = m_offerDetails.Quantity - m_numberRedeemed;
                
            }
            Item boughtItem = null;
            SpecialOfferTemplate template = m_offerDetails.OfferTemplate;
            int cost = quantity * template.Price;
            int numItems = quantity * template.Quantity;
            ItemTemplate itemTemp = ItemTemplateManager.GetItemForID(template.ItemTemplateID);
            if (itemTemp != null)
            {
                if (player.m_platinum >= cost && quantity > 0)
                {
                    Character theCharacter = player.m_activeCharacter;
                    if (theCharacter != null)
                    {
                        player.m_platinum -= cost;
                    }
                    try
                    {
                        using (TextWriter writer = File.AppendText("transactions\\special_offers" + DateTime.Now.ToString("yyyyMMdd") + ".txt"))
                        {
                            writer.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "," + player.m_account_id + "," + Program.m_worldID + "," 
								+ theCharacter.m_character_id + "," + ((int)theCharacter.m_class.m_classType) + "," + theCharacter.Level + "," 
								+ template.OfferID + "," + template.OfferName + "," + quantity + "," + cost);
                            writer.Close();
                        }
                        Program.processor.m_universalHubDB.runCommandSync("insert into offer_purchases (purchase_date,account_id,world_id,character_id,class_id," +
                                                                          "level,offer_id,offer_name,quantity,total_cost) values " +
                                                                          "(\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\"," + player.m_account_id + "," + 
																		  Program.m_worldID + "," + theCharacter.m_character_id + "," + ((int)theCharacter.m_class.m_classType) + "," 
																		  + theCharacter.Level + "," + template.OfferID + ",\"" + template.OfferName + "\"," + quantity + "," + cost + ")");
                    }
                    catch (Exception ex)
                    {
                        Program.Display("Failed to Write Transaction file:" + ex.ToString());
                    }
                    m_numberRedeemed += quantity;
                    theCharacter.OfferManager.OfferRedeemed(this);
                    boughtItem = theCharacter.m_inventory.AddNewItemToCharacterInventory(template.ItemTemplateID, numItems, false);

                    if (originalQuantity != quantity)
                    {
						string locPopupText = Localiser.GetString(PremiumShop.textDB, player, (int)PremiumShop.PremiumShopTextDB.TextID.PURCHASED_OVER_MAXIMUM_CHARGED);
						locPopupText = string.Format(locPopupText, quantity);
						Program.processor.sendSystemMessage(locPopupText, player, true, SYSTEM_MESSAGE_TYPE.POPUP);
					}
					string locText = Localiser.GetString(PremiumShop.textDB, player, (int)PremiumShop.PremiumShopTextDB.TextID.PURCHASED_ITEM);
					locText = string.Format(locText, numItems, itemTemp.m_loc_item_name[player.m_languageIndex]);
					PremiumShop.SendItemBuyReply(player, locText, true);
					Program.processor.m_universalHubDB.runCommandSync("update account_details set platinum=" + player.m_platinum + " where account_id=" + player.m_account_id);

                    //analytics logging
                    if (Program.m_LogAnalytics)
                    {
                        AnalyticsMain logAnalytics = new AnalyticsMain(true);
                        logAnalytics.itemShopItemPurchase(player, cost, itemTemp.m_item_name, "SPECIAL_OFFER", quantity);
                    }
                    
                }
                else
                {//not enough money
					string locText = Localiser.GetString(PremiumShop.textDB, player, (int)PremiumShop.PremiumShopTextDB.TextID.NOT_ENOUGHT_PLATINUM);
					PremiumShop.SendItemBuyReply(player, locText, false);
				}
            }
            else
            {
				string locText = Localiser.GetString(PremiumShop.textDB, player, (int)PremiumShop.PremiumShopTextDB.TextID.INVALID_ITEM);
				PremiumShop.SendItemBuyReply(player, locText, false);
			}


            return boughtItem;

        }
    }

    class SpecialOfferDetails
    {
        int m_offerID = -1;		
        SpecialOfferTemplate m_offerTemplate = null;

        DateTime m_startDate = DateTime.MinValue;

        DateTime m_endDate = DateTime.MinValue;
        
        int m_quantity=0;
        bool m_limitedByCharacter = false;
        bool m_displayInItemShop = false;
        bool m_displayInOffers = false;

        String m_targetedSpecialOffer = null;

        SpecialOfferManager.OfferStatus m_offerStatus = SpecialOfferManager.OfferStatus.NotYetActive;

        internal int OfferID { get { return m_offerID; } }

        internal String TargetedSpecialOffer
        {
            get { return m_targetedSpecialOffer; }
        }
        
        internal DateTime EndDate
        {
            get { return m_endDate; }
        }
        internal SpecialOfferManager.OfferStatus OfferStatus
        {
            get { return m_offerStatus; }
            set { m_offerStatus = value; }
        }

        internal SpecialOfferTemplate OfferTemplate
        {
            get { return m_offerTemplate; }
        }
        internal int Quantity
        {
            get { return m_quantity; }
        }
        internal bool LimitedByCharacter
        {
            get { return m_limitedByCharacter; }
        }
        internal bool DisplayInItemShop
        {
            get { return m_displayInItemShop; }
        }
        internal bool DisplayInOffers
        {
            get { return m_displayInOffers; }
        }

        internal SpecialOfferDetails(SqlQuery query)
        {
            m_offerID = query.GetInt32("offer_id");
            m_offerTemplate = SpecialOfferTemplateManager.GetOfferForID(m_offerID);

            if (query.isNull("start_date") == false)
            {
                m_startDate = query.GetDateTime("start_date");
            }
            if (query.isNull("end_date") == false)
            {
                m_endDate = query.GetDateTime("end_date");
            }
            m_quantity = query.GetInt32("quantity");
            m_limitedByCharacter = query.GetBoolean("limited_by_character");
            m_displayInItemShop = query.GetBoolean("item_shop");
            m_displayInOffers = query.GetBoolean("offer_page");
        }

        internal SpecialOfferDetails(SqlQuery query, bool globalSpecialOffer)
         : this(query)
        {
            if (globalSpecialOffer)
            {
                // Targetted Special Offers //
                // Check if there is a targetted special offer string - otherwise the variables remain null
                if (query.isNull("targeted_special_offer") == false)
                {
                    m_targetedSpecialOffer = query.GetString("targeted_special_offer");
                }
            }
        }

        internal SpecialOfferDetails(int offerID, DateTime startDate, DateTime endDate, int quantity, bool limitByCharacter, bool showInItemShop, bool showInOffers, string targetedSpecialOffer)
        {
            m_offerID = offerID;
            m_offerTemplate = SpecialOfferTemplateManager.GetOfferForID(m_offerID);


            m_startDate = startDate;
            m_endDate = endDate;

            m_quantity = quantity;
            m_limitedByCharacter = limitByCharacter;
            m_displayInItemShop = showInItemShop;
            m_displayInOffers = showInOffers;

            m_targetedSpecialOffer = targetedSpecialOffer;
        }

        internal SpecialOfferManager.OfferStatus GetOfferStatus(DateTime currentTime)
        {
            SpecialOfferManager.OfferStatus offerStatus = SpecialOfferManager.OfferStatus.NotYetActive;

            if(m_startDate==DateTime.MinValue || m_startDate < currentTime){
                if (m_endDate == DateTime.MinValue || m_endDate > currentTime)
                {

                    offerStatus = SpecialOfferManager.OfferStatus.Active;

                }
                else
                {
                    offerStatus = SpecialOfferManager.OfferStatus.Expired;
                }
   

            }

            return offerStatus;
        }

    }

    class SpecialOfferManager
    {

        internal enum OfferStatus
        {
            NotYetActive,
            Active,
            Expired,
            Unavailable

        }
        const double min_second_between_checks = 30;
        /// <summary>
        /// The last time that the offers were checked to still be valid 
        /// </summary>
        double m_netTimeAtLastCheck = 0;
        /// <summary>
        /// net Time at which new offers were loaded from the database
        /// </summary>
        double m_netTimeAtLastLoad = 0;
        /// <summary>
        /// These offers will not be sent to the client
        /// offers redeemed within an acceptable timescale will still be accepted
        /// </summary>
        List<SpecialOfferDetails> m_recentlyExpiredOffers = new List<SpecialOfferDetails>();
        /// <summary>
        /// These offers are active and will be sent to the client when the information is requested
        /// </summary>
        List<SpecialOfferDetails> m_activeOffers = new List<SpecialOfferDetails>();
        /// <summary>
        /// These offers are not yet available but will be soon
        /// </summary>
        List<SpecialOfferDetails> m_queuedOffers = new List<SpecialOfferDetails>();


        /// <summary>
        /// These offers are active and will be sent to the client when the information is requested
        /// </summary>
        internal List<SpecialOfferDetails> ActiveOffers
        {
            get { return m_activeOffers; }
        }
        /// <summary>
        /// These offers are active and will be sent to the client when the information is requested
        /// </summary>
        internal List<SpecialOfferDetails> QueuedOffers
        {
            get { return m_queuedOffers; }
        }

		public DateTime TimeLoaded { get; protected set; } 

        bool CheckOfferTimes()
        {
            bool availableListChanged = false;

            DateTime currentTime = DateTime.Now;

            DateTime latestExpiryTime = currentTime - TimeSpan.FromMinutes(90);
            for (int i = m_recentlyExpiredOffers.Count - 1; i >= 0; i--)
            {
                SpecialOfferDetails currentOffer = m_recentlyExpiredOffers[i];

                if (currentOffer.EndDate < latestExpiryTime)
                {
                    m_recentlyExpiredOffers.Remove(currentOffer);
                    currentOffer.OfferStatus = OfferStatus.Unavailable;
                }

            }
            for (int i = m_activeOffers.Count - 1; i >= 0; i--)
            {
                SpecialOfferDetails currentOffer = m_activeOffers[i];

                if (currentOffer.GetOfferStatus(currentTime)== OfferStatus.Expired)
                {
                    m_activeOffers.Remove(currentOffer);
                    m_recentlyExpiredOffers.Add(currentOffer);
                    availableListChanged = true;
                    currentOffer.OfferStatus = OfferStatus.Expired;
                }

            }
            for (int i = m_queuedOffers.Count - 1; i >= 0; i--)
            {
                SpecialOfferDetails currentOffer = m_queuedOffers[i];

                if (currentOffer.GetOfferStatus(currentTime) == OfferStatus.Active)
                {
                    m_queuedOffers.Remove(currentOffer);
                    m_activeOffers.Add(currentOffer);
                    availableListChanged = true;
                    currentOffer.OfferStatus = OfferStatus.Active;
                }

            }

             return availableListChanged;


        }
        internal bool Update()
        {
            bool availableListChanged = false;
            double netTime = NetTime.Now;

            if (m_netTimeAtLastCheck + min_second_between_checks < netTime)
            {
                m_netTimeAtLastCheck = netTime;
                availableListChanged = CheckOfferTimes();
            }
            return availableListChanged;
        }
        internal void LoadGlobalOffers(Database db)
        {
            string queryStr = "select * from global_special_offers";

            SqlQuery query = new SqlQuery(db, queryStr);
            DateTime currentTime = DateTime.Now;
            if (query.HasRows)
            {
                while (query.Read())
                {
                    string worldList = "";// query.GetString("world_ids");
                    if (query.isNull("world_ids") == false)
                    {
                        worldList = query.GetString("world_ids");
                    }

                    string[] worldIDs = worldList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    if (worldIDs.Length == 0|| worldIDs.Contains(Program.m_worldID.ToString()))
                    {
                        SpecialOfferDetails newDetails = new SpecialOfferDetails(query, true);
                        if (newDetails.OfferTemplate != null)
                        {
                            OfferStatus currentStatus = newDetails.GetOfferStatus(currentTime);
                            newDetails.OfferStatus = currentStatus;
                            switch (currentStatus)
                            {
                                case OfferStatus.Active:
                                    {
                                        m_activeOffers.Add(newDetails);
                                        break;
                                    }
                                case OfferStatus.Expired:
                                    {
                                        m_recentlyExpiredOffers.Add(newDetails);
                                        break;
                                    }
                                case OfferStatus.NotYetActive:
                                    {
                                        m_queuedOffers.Add(newDetails);
                                        break;
                                    }
                                default:
                                    {
                                        break;
                                    }
                            }
                        }

                    }
                }
            }
            query.Close();
            m_netTimeAtLastLoad = NetTime.Now;
	        TimeLoaded = DateTime.UtcNow;

        }
        internal void LoadIndividualOffers(Database db,Character character)
        {
            string queryAccStr = "select * from individual_special_offers where account_id = " + character.m_player.m_account_id +" and character_id = -1";
            string queryCharStr = "select * from individual_special_offers where character_id = " + character.m_character_id + " and world_id = "+ Program.m_worldID;
            string queryStr = queryAccStr + " union "+ queryCharStr;
          

            SqlQuery query = new SqlQuery(db, queryStr);
            DateTime currentTime = DateTime.Now;

            if (query.HasRows)
            {
                while (query.Read())
                {
      
                    SpecialOfferDetails newDetails = new SpecialOfferDetails(query);
                    if (newDetails.OfferTemplate != null)
                    {
                        OfferStatus currentStatus = newDetails.GetOfferStatus(currentTime);
                        newDetails.OfferStatus = currentStatus;
                        switch (currentStatus)
                        {
                            case OfferStatus.Active:
                                {
                                    m_activeOffers.Add(newDetails);
                                    break;
                                }
                            case OfferStatus.Expired:
                                {
                                    m_recentlyExpiredOffers.Add(newDetails);
                                    break;
                                }
                            case OfferStatus.NotYetActive:
                                {
                                    m_queuedOffers.Add(newDetails);
                                    break;
                                }
                            default:
                                {
                                    break;
                                }
                        }
                    }

                    
                }
            }
            query.Close();
            m_netTimeAtLastLoad = NetTime.Now;

            

        }
        protected bool RemoveAllOffersWithID(int offerID)
        {
            bool offerRemoved = false;
            for (int i = m_queuedOffers.Count-1; i >=0 ; i--)
            {
                SpecialOfferDetails currentOffer = m_queuedOffers[i];
                if (currentOffer.OfferTemplate.OfferID == offerID)
                {
                    m_queuedOffers.Remove(currentOffer);
                    offerRemoved = true;
                }
            }
            for (int i = m_activeOffers.Count - 1; i >= 0; i--)
            {
                SpecialOfferDetails currentOffer = m_activeOffers[i];
                if (currentOffer.OfferTemplate.OfferID == offerID)
                {
                    m_activeOffers.Remove(currentOffer);
                    offerRemoved = true;
                }
            }
            for (int i = m_recentlyExpiredOffers.Count - 1; i >= 0; i--)
            {
                SpecialOfferDetails currentOffer = m_recentlyExpiredOffers[i];
                if (currentOffer.OfferTemplate.OfferID == offerID)
                {
                    m_recentlyExpiredOffers.Remove(currentOffer);
                    offerRemoved = true;
                }
            }
            return offerRemoved;
        }

    }

    class LoadOfferDataTask : BaseTask
    {
        public Player m_player;
        public Character m_character;
        public SpecialOfferDetails m_offerData;

        public LoadOfferDataTask(Player player, Character character, SpecialOfferDetails offerData)
        {
            m_TaskType = TaskType.LoadOfferData;
            m_player = player;
            m_character = character;
            m_offerData = offerData;
        }
        internal override void TakeAction(CommandProcessor processor)
        {
            //if the offer is account based only look up the accountID
            string searchString = "select * from offer_purchases where account_id = " + m_player.m_account_id;

            //if the offer is character based then use the accountID, character id and world id           
            if (m_offerData.LimitedByCharacter == true)
            {
                searchString += " and world_id = " + Program.m_worldID + " and character_id = " + m_character.m_character_id;
            }
            searchString += " and offer_id = " + m_offerData.OfferTemplate.OfferID;

            SqlQuery query = new SqlQuery(processor.m_universalHubDB, searchString);

            int totalQuantity = 0;
            if (query.HasRows)
            {
                while (query.Read())
                {
                    int currentQuantity = query.GetInt32("quantity");
                    if (currentQuantity > 0)
                    {
                        totalQuantity += currentQuantity;
                    }
                }
            }
            query.Close();
            CharacterOfferData newData = new CharacterOfferData(m_offerData, totalQuantity);

            lock (m_character.OfferManager.NewOffers)
            {
                m_character.OfferManager.NewOffers.Add(newData);
            }
  
        }
    }
}
