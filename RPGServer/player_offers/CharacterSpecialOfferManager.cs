using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MainServer.Localise;

namespace MainServer.player_offers
{
    class CharacterSpecialOfferManager : SpecialOfferManager
	{
		// #localisation
		public class CharacterSpecialOfferManagerTextDB : TextEnumDB
		{
			public CharacterSpecialOfferManagerTextDB() : base(nameof(CharacterSpecialOfferManager), typeof(TextID)) { }

			public enum TextID
			{
				OFFERS_AVAILABLE,       //"offers available = {currentOffer0}"
			}
		}
		public static CharacterSpecialOfferManagerTextDB textDB = new CharacterSpecialOfferManagerTextDB();

		#region fields

		List<CharacterOfferData> m_newOffers = new List<CharacterOfferData>();
        List<CharacterOfferData> m_currentOffers = new List<CharacterOfferData>();

        List<SpecialOfferDetails> m_loadingOffers = new List<SpecialOfferDetails>();
        List<SpecialOfferDetails> m_activeGlobalOffers = new List<SpecialOfferDetails>();
        List<SpecialOfferDetails> m_activePersonalOffers = new List<SpecialOfferDetails>();

        

        int m_currentItemShopID = -1;

        internal List<CharacterOfferData> NewOffers
        {
            get { return m_newOffers; }
        }
        internal bool LoadingData
        {
            get { return (m_loadingOffers.Count > 0); }
        }
        internal bool WaitingToSendOfferPage { get; set; }
        internal bool WaitingToSendItemShopPage { get; set; }
        internal bool WaitingToSendSpecialOfferNumber { get; set; }

		#endregion

		#region constructor

		internal CharacterSpecialOfferManager()
        {
            m_currentItemShopID = Program.processor.m_premiumShop.OfferStartID;
			TimeLoaded =  Program.processor.m_globalOfferManager.TimeLoaded;
        }

		#endregion
		
		#region main methods

		internal void Update(Character owner)
		{
			
			//the offers where reloaded in the server console, so clear and reload
			if (TimeLoaded != Program.processor.m_globalOfferManager.TimeLoaded)
			{
				m_activeGlobalOffers.Clear();
				m_activePersonalOffers.Clear();
				m_currentOffers.Clear();
				TimeLoaded = Program.processor.m_globalOfferManager.TimeLoaded;
				UpdateOfferList(owner);
			}

			CheckForLoadedOffers(owner);

			//if the loaded list is empty and it's time  to send the message
			if (m_loadingOffers.Count == 0)
			{
				if (WaitingToSendItemShopPage == true)
				{
                    WaitingToSendItemShopPage = false;
					ClearInvalidOffers();
					Program.processor.m_premiumShop.SendPremiumShopReplyWithOffers(owner.m_player);                    					
				}
                if (WaitingToSendOfferPage == true)
				{
                    WaitingToSendOfferPage = false;
					ClearInvalidOffers();				    
					SendOpenSpecialOffersPopup(owner);				
				}
			}

			if (WaitingToSendSpecialOfferNumber && !LoadingData)
			{				
                WaitingToSendSpecialOfferNumber = false;
				Program.processor.sendBackSpecialOffers(owner.m_player);				
			}

		}

		void ClearInvalidOffers()
        {
            bool offersRemoved = false;

            //check the global offers to test if any have expired and need to be removed from the list 
            for (int i = m_activeGlobalOffers.Count - 1; i >= 0; i--)
            {
                SpecialOfferDetails currentOffer = m_activeGlobalOffers[i];

                if (currentOffer.OfferStatus != SpecialOfferManager.OfferStatus.Active)
                {
                    m_activeGlobalOffers.Remove(currentOffer);
                    offersRemoved = true;
                }
				
            }
            //check the personal offers to test if any have expired and need to be removed from the list 
            for (int i = m_activePersonalOffers.Count - 1; i >= 0; i--)
            {
                SpecialOfferDetails currentOffer = m_activePersonalOffers[i];

                if (currentOffer.OfferStatus != SpecialOfferManager.OfferStatus.Active)
                {
                    m_activePersonalOffers.Remove(currentOffer);
                    offersRemoved = true;
                }
				
            }
            //if offers were removed then look for them in the active list
            if (offersRemoved == true)
            {
                for (int i = m_currentOffers.Count - 1; i >= 0; i--)
                {
                    CharacterOfferData offerData = m_currentOffers[i];
                    SpecialOfferDetails currentOffer = offerData.OfferDetails;

                    if (currentOffer.OfferStatus != SpecialOfferManager.OfferStatus.Active)
                    {
                        m_currentOffers.Remove(offerData);
                        offersRemoved = true;
                    }
					
                }
            }
        }


        internal void OfferRedeemed(CharacterOfferData offerData)
        {
            if (offerData.OfferDetails.Quantity>0 && offerData.NumberRedeemed >= offerData.OfferDetails.Quantity)
            {
                m_currentOffers.Remove(offerData);
            }
        }
        
		bool UpdateOffers(List<SpecialOfferDetails> sourceList, List<SpecialOfferDetails> currentList,Character owner)
        {
            bool offersChanged= false;
            List<SpecialOfferDetails> newOffers = GetNewOffers(sourceList, currentList, owner);

            if (newOffers != null && newOffers.Count > 0)
            {
                offersChanged = true;
                for (int i = 0; i < newOffers.Count; i++)
                {
                    SpecialOfferDetails currentOffer = newOffers[i];

                    currentList.Add(currentOffer);
                    //if the offer can only be redeemed a finite number of times
                    //create a task to load how many times it has beed purchased
                    if (currentOffer.Quantity > 0)
                    {
                        m_loadingOffers.Add(currentOffer);
                        LoadOfferDataTask newTask = new LoadOfferDataTask(owner.m_player, owner, currentOffer);
                        lock (Program.processor.m_backgroundTasks)
                        {
                            Program.processor.m_backgroundTasks.Enqueue(newTask);
                        }
                    }
                    // if the offer has no limit create the container now
                    else
                    {
                        CharacterOfferData newData = new CharacterOfferData(currentOffer, -1);
                        newData.ItemShopID = m_currentItemShopID;
                        m_currentItemShopID++;
                        m_currentOffers.Add(newData);
                    }

                }
            }
            return offersChanged;
        }       

        // Targetted Special Offers - added a new condition to
        List<SpecialOfferDetails> GetNewOffers(List<SpecialOfferDetails> sourceList, List<SpecialOfferDetails> currentList, Character owner)
        {
            List<SpecialOfferDetails> changesList = null;
            //once the expired offers have been removed
            //the list of available offers should be the same size as the active list
            //if not then there must be new offers
            if (sourceList.Count > currentList.Count)
            {
                for (int i = 0; i < sourceList.Count; i++)
                {
                    SpecialOfferDetails currentItem = sourceList[i];
                    //if the active list does not contain this offer
                    //add it to the new active list
                    if (currentList.Contains(currentItem) == false)
                    {
                        if (changesList == null)
                        {
                            changesList = new List<SpecialOfferDetails>(10);
                        }

                        // Targeted Special Offers //

                        // If there is a targeted special offer string then send the owner Character object and test string to the new TargetedSpecialOfferManagerClass
                        if (currentItem.TargetedSpecialOffer != null)
                        {
                            if (Program.processor.TargetedSpecialOfferManager.AddTargetedSpecialOffer(owner, currentItem.TargetedSpecialOffer, currentItem.OfferID))
                            {
                                changesList.Add(currentItem);
                            }
                        }
                        // Otherwise add as normal
                        else
                        {
                            changesList.Add(currentItem);
                        }
                    }
                }
            }

            return changesList;
        }
        
		internal List<CharacterOfferData> GetOfferWalOffers()
        {
            List<CharacterOfferData> offerWallOffers = new List<CharacterOfferData>();
            for (int i = 0; i < m_currentOffers.Count; i++)
            {
                CharacterOfferData currentOffers = m_currentOffers[i];
                if (currentOffers.OfferDetails.DisplayInOffers == true)
                {
                    offerWallOffers.Add(currentOffers);
                }
            }
            return offerWallOffers;
        }
        
		internal List<CharacterOfferData> GetItemShopOffers()
        {
            List<CharacterOfferData> shopOffers = new List<CharacterOfferData>();
            for (int i = 0; i < m_currentOffers.Count; i++)
            {
                CharacterOfferData currentOffers = m_currentOffers[i];
                if (currentOffers.OfferDetails.DisplayInItemShop == true)
                {
                    shopOffers.Add(currentOffers);
                }
            }
            return shopOffers;
        }
        
		internal void SendOpenSpecialOffersPopup(Character owner)
        {
            string listString = "";


            List<CharacterOfferData> offerWallList = GetOfferWalOffers();
            for (int i = 0; i < offerWallList.Count; i++)
            {
                CharacterOfferData currentOffers = offerWallList[i];
				//listString += currentOffers.GetListPopupString();
				listString += currentOffers.GetListPopupString(owner.m_player);
			}
            
			//special offer list
            Program.processor.SendXMLPopupMessage(true, owner.m_player, (int)XML_Popup.Set_Popup_IDs.SPI_SpecialOffersList, XML_Popup.Popup_Type.None, 
				"special_offer_prefab", new List<string> { listString }, false);

            XML_Popup offerwallPopup = XML_Popup.GetPopupForID(owner.m_player, (int)XML_Popup.Set_Popup_IDs.SPI_OfferWall);
            if (offerwallPopup != null)
            {
                Program.processor.SendCloseXMLPopup(owner.m_player, (int)XML_Popup.Set_Popup_IDs.SPI_OfferWall);
                owner.m_player.m_openPopups.Remove(offerwallPopup);
            }
            XML_Popup openItemPopup = XML_Popup.GetPopupForID(owner.m_player, (int)XML_Popup.Set_Popup_IDs.SPI_SpecialOffersView);
            if (openItemPopup != null)
            {
                Program.processor.SendCloseXMLPopup(owner.m_player, (int)XML_Popup.Set_Popup_IDs.SPI_SpecialOffersView);
                owner.m_player.m_openPopups.Remove(openItemPopup);
            }
           
        }
        
		void CheckForLoadedOffers(Character owner)
        {
            //if any offers have finished then add them to the list
            if (m_loadingOffers.Count > 0)
            {
                List<CharacterOfferData> loadedOffers = new List<CharacterOfferData>(m_loadingOffers.Count);

                lock (NewOffers)
                {
                    loadedOffers.AddRange(m_newOffers);
                    m_newOffers.Clear();
                }
                //add them to the active offers list
                //m_currentOffers.AddRange(loadedOffers);
               
                for (int i = 0; i < loadedOffers.Count; i++)
                {
                    CharacterOfferData currentOffer = loadedOffers[i];
                    if (m_loadingOffers.Remove(currentOffer.OfferDetails) == false)
                    {
                        string errorString = "(" + owner.m_player.m_UserName + ") " + owner.Name + " loaded offer " + currentOffer.OfferDetails.OfferTemplate.OfferID + " and was not waiting for it";
                    }
                    if(currentOffer.NumberRedeemed >= currentOffer.OfferDetails.Quantity){
                        //the player has already redeemed the maximum Quantity
                        continue;
                    }
                    currentOffer.ItemShopID = m_currentItemShopID;
                    m_currentItemShopID++;
                    CharacterOfferData existingOffer = GetActiveOfferForID(currentOffer.OfferDetails.OfferTemplate.OfferID);
                    if (existingOffer != null)
                    {
                        //use the old offer
                        //if the old offer does not end or 
                        //the old offer lasts longer than the new one
                        if (existingOffer.OfferDetails.EndDate == DateTime.MinValue ||
                            (existingOffer.OfferDetails.EndDate > currentOffer.OfferDetails.EndDate && currentOffer.OfferDetails.EndDate != DateTime.MinValue))
                        {

                        }
                            //otherwise use the new offer and remove the old one
                        else
                        {
                            m_currentOffers.Remove(existingOffer);
                            m_currentOffers.Add(currentOffer);
                        }
                    }
                    else
                    {
                        m_currentOffers.Add(currentOffer);
                    }
                }
                m_currentOffers.Sort(SortOffersBySortID);
            }
        }        

		internal CharacterOfferData GetActiveOfferForID(int offerID)
        {
            CharacterOfferData activeOffer = null;

            for (int i = 0; i < m_currentOffers.Count && activeOffer == null; i++)
            {
                CharacterOfferData currentOffer = m_currentOffers[i];
                if (currentOffer.OfferDetails.OfferTemplate.OfferID == offerID)
                {
                    activeOffer = currentOffer;
                }
            }
            return activeOffer;
        }

        internal void UpdateOfferList(Character owner)
        {
            //update personal Offers
            base.Update();


            //if any offers have finished then add them to the list
            CheckForLoadedOffers(owner);


            //clear the old data
            ClearInvalidOffers();

            SpecialOfferManager globalOffersManager = Program.processor.GlobalOfferManager;
            globalOffersManager.Update();
            //update global offers
            UpdateOffers(globalOffersManager.ActiveOffers, m_activeGlobalOffers, owner);
            //update Private Offers
            UpdateOffers(ActiveOffers, m_activePersonalOffers, owner);
            
        }


		internal void OfferPurchasedFromItemShop(int itemShopOfferID, int quantity, Character owner)
		{
			CharacterOfferData requestedItem = null;
			for (int i = 0; i < m_currentOffers.Count && requestedItem == null; i++)
			{
				CharacterOfferData currentOffer = m_currentOffers[i];
				if (currentOffer.ItemShopID == itemShopOfferID)
				{
					requestedItem = currentOffer;
				}
			}
			if (requestedItem != null)
			{
				requestedItem.RedeemOffer(quantity, owner.m_player);
				if (m_currentOffers.Contains(requestedItem) == false)
				{
					Program.processor.m_premiumShop.SendPremiumShopReplyWithOffers(owner.m_player);
				}
			}
			else
			{
				//the item doesn't exist
				string locText = Localiser.GetString(PremiumShop.textDB, owner.m_player, (int)PremiumShop.PremiumShopTextDB.TextID.INVALID_ITEM);
				PremiumShop.SendItemBuyReply(owner.m_player, locText, false);
			}
		}


		internal void AddOfferToPlayer(Character owner, int offerID, float numHours, int quantity, bool limitByCharacter, bool showInItemShop, bool showInOfferWall, bool addToDatabase)
		{
			DateTime startTime = DateTime.Now;
			DateTime endTime = DateTime.MinValue;

			if (numHours > 0)
			{
				endTime = startTime + TimeSpan.FromHours(numHours);
			}

			SpecialOfferTemplate newOfferTemplate = SpecialOfferTemplateManager.GetOfferForID(offerID);
			if (addToDatabase == true)
			{
				int character_id = -1;
				int world_id = -1;
				string strCharacterLimited = "0";

				if (limitByCharacter == true)
				{
					character_id = (int)owner.m_character_id;
					world_id = Program.m_worldID;
					strCharacterLimited = "1";
				}
				string strStartDate = "\"" + startTime.ToString("yyyy-MM-dd HH:mm:ss.000") + "\"";
				string strEndDate = "NULL";
				if (numHours > 0)
				{
					strEndDate = "\"" + endTime.ToString("yyyy-MM-dd HH:mm:ss.000") + "\"";
				}

				string strOfferWall = "0";
				string strItemShop = "0";
				if (showInOfferWall == true)
				{
					strOfferWall = "1";
				}
				if (showInItemShop == true)
				{
					strItemShop = "1";
				}

				string insertString = "insert into individual_special_offers (account_id,world_id,character_id,offer_id,start_date,end_date,quantity,limited_by_character,item_shop,offer_page) values ";
				insertString += "(" + owner.m_player.m_account_id + "," + world_id + "," + character_id + "," + offerID + "," + strStartDate + "," + strEndDate + "," + quantity + "," + strCharacterLimited + "," + strItemShop + "," + strOfferWall + ")";

				Program.processor.m_universalHubDB.runCommandSync(insertString);
				// db.runCommandSync("insert into friend_list (character_id,other_character_id) values (" + m_character_id + "," + m_friendCharacterIDs[i].CharacterID + ")");

			}
			if (newOfferTemplate != null)
			{

				SpecialOfferDetails newDetails = new SpecialOfferDetails(offerID, startTime, endTime, quantity, limitByCharacter, showInItemShop, true, null);
				newDetails.OfferStatus = OfferStatus.NotYetActive;
				//AddOfferToList(owner,newDetails); ;
				QueuedOffers.Add(newDetails);
				// ActiveOffers

			}

		}

		internal void RemoveOfferFromPlayer(Character owner, int offerID)
		{

			bool offersRemoved = base.RemoveAllOffersWithID(offerID);

			if (offersRemoved == true)
			{
				string deleteString = "delete from individual_special_offers where ";
				deleteString += "((character_id = " + owner.m_character_id + " and world_id = " + Program.m_worldID + ") or";
				deleteString += "(account_id = " + owner.m_player.m_account_id + " and limited_by_character = false)) and ";
				deleteString += "offer_id =" + offerID;
				Program.processor.m_universalHubDB.runCommandSync(deleteString);

				for (int i = m_currentOffers.Count - 1; i >= 0; i--)
				{
					CharacterOfferData currentOffer = m_currentOffers[i];
					if (currentOffer.OfferDetails.OfferTemplate.OfferID == offerID)
					{
						m_currentOffers.Remove(currentOffer);
					}

				}
			}
		}

		#endregion

		#region sending messages

        /// <summary>
        /// Updates the list of offers that are available
        /// loads data is required 
        /// </summary>
        /// <param name="owner"></param>
        internal void PrepareToSendSpecialOfferWall(Character owner)
        {
            UpdateOfferList(owner);
            if (LoadingData == true)
            {
                WaitingToSendOfferPage = true;
            }
            else
            {
                SendOpenSpecialOffersPopup(owner);
            }
        }

        internal void PrepareToSendSpecialOfferWallNumber(Character owner)
        {
            UpdateOfferList(owner);
            if (LoadingData == true)
            {
                WaitingToSendSpecialOfferNumber = true;
            }
            else
            {
                Program.processor.sendBackSpecialOffers(owner.m_player);
            }
        }

        internal void PrepareToSendItemShopList(Character owner)
        {
            UpdateOfferList(owner);
            if (LoadingData == true)
            {
                WaitingToSendItemShopPage = true;
            }
            else
            {
                Program.processor.m_premiumShop.SendPremiumShopReplyWithOffers(owner.m_player);
            }
        }
        
		#endregion

		#region sort helper

		static int SortOffersBySortID(CharacterOfferData offer1, CharacterOfferData offer2)
		{

			if (offer1 == null)
			{
				if (offer2 == null)
				{
					// If x is null and y is null, they're 
					// equal.  
					return 0;
				}
				else
				{
					// If x is null and y is not null, y 
					// is greater.  
					return -1;
				}
			}
			else
			{
				// If x is not null... 
				// 
				if (offer2 == null)
				// ...and y is null, x is greater.
				{
					return 1;
				}

				if (offer1.OfferDetails.OfferTemplate.SortOrder > offer2.OfferDetails.OfferTemplate.SortOrder)
				{
					return 1;
				}
				else if (offer1.OfferDetails.OfferTemplate.SortOrder < offer2.OfferDetails.OfferTemplate.SortOrder)
				{
					return -1;
				}
			}
			return 0;

		}

		#endregion
	}
   
}
