using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MainServer.Localise;

namespace MainServer.player_offers
{
    class SpecialOfferTemplate
    {
        internal static bool SPECIAL_OFFERS_ACTIVE = false;
        /// <summary>
        /// the unique id for this offer
        /// </summary>
        int m_offerID=-1;

        /// <summary>
        /// the type of item this offer contains
        /// </summary>
        int m_itemTemplateID=-1;
        /// <summary>
        /// The number of items this offer contains
        /// </summary>
        int m_quantity=-1;
        /// <summary>
        /// The Price of the offer
        /// </summary>
        int m_price=-1;
        /// <summary>
        /// The title of the offer as seen on the offers page and item shop
        /// </summary>
        string m_offerName="";
        /// <summary>
        /// The description that will be seen when an offer is examined
        /// </summary>
        string m_offerDescription="";
         /// <summary>
        /// sort order for the offers page
        /// in the item shop all special offers go to the top of their respective page
        /// </summary>
        int m_sortOrder=-1;
        /// <summary>
        /// where is the offer shown if in the item shop
        /// </summary>
        int m_itemShopTabID=-1;
        /// <summary>
        /// the image to be used when an offer is displayed
        /// </summary>
        string m_offerImage = "";

        /// <summary>
        /// the unique id for this offer
        /// </summary>
        internal int OfferID
        {
            get { return m_offerID; }
        }
        /// <summary>
        /// the type of item this offer contains
        /// </summary>
        internal int ItemTemplateID
        {
            get { return m_itemTemplateID; }
        }
        /// <summary>
        /// The number of items this offer contains
        /// </summary>
        internal int Quantity
        {
            get { return m_quantity; }
        }
        /// <summary>
        /// The Price of the offer
        /// </summary>
        internal int Price
        {
            get { return m_price; }
        }
        /// <summary>
        /// The title of the offer as seen on the offers page and item shop
        /// </summary>
        internal string OfferName
        {
            get { return m_offerName; }
        }

		/// <summary>
		/// Check string for #text and place here
		/// </summary>
		internal string OfferNameFlash { get; set; }		
        
        /// <summary>
        /// sort order for the offers page
        /// in the item shop all special offers go to the top of their respective page
        /// </summary>
        internal int SortOrder
        {
            get { return m_sortOrder; }
        }
        /// <summary>
        /// where is the offer shown if in the item shop
        /// </summary>
        internal int ItemShopTabID
        {
            get { return m_itemShopTabID; }
        }
        /// <summary>
        /// the image to be used when an offer is displayed
        /// </summary>
        internal string OfferImage
        {
            get { return m_offerImage; }
        }

        internal SpecialOfferTemplate( SqlQuery query)
        {
            m_offerID = query.GetInt32("offer_id");
            m_itemTemplateID = query.GetInt32("item_id");
            m_quantity = query.GetInt32("item_quantity");
            m_price = query.GetInt32("price");
            m_sortOrder = query.GetInt32("sort_order");
            m_itemShopTabID = query.GetInt32("premium_shop_type_id");

            m_offerName = query.GetString("offer_name");
            m_offerDescription = query.GetString("offer_description");
            m_offerImage = query.GetString("offer_image");

			//parse offername to remove hashtag and add into own field
	        string[] flashy = OfferName.Split('#');
	        if (flashy.Length > 1)
	        {
		        m_offerName = flashy[0];
		        OfferNameFlash = flashy[1];
	        }
	        else
	        {
		        OfferNameFlash = String.Empty;
	        }

            //Method to correctly format filenames from db.
            bool hasExtension = Path.HasExtension(m_offerImage);

            if (hasExtension)
            {
                //Trim file extension from string.
                string m_offerImageTrimmed = m_offerImage.Substring(0, m_offerImage.LastIndexOf('.'));
                m_offerImage = m_offerImageTrimmed;
           
            }

            //Capitalise the first char of the string.
            m_offerImage = char.ToUpper(m_offerImage[0]) + m_offerImage.Substring(1);
        }
    }

    static class SpecialOfferTemplateManager
    {
        static List<SpecialOfferTemplate> m_specialOfferTemplates = new List<SpecialOfferTemplate>();

		// #localisation
		static int specialOfferNameTextDBIndex = 0;
		static int specialOfferDescTextDBIndex = 0;

		internal static void LoadSpecialOfferTemplates(Database db)
        {

            SqlQuery query = new SqlQuery(db, "select * from special_offer_templates order by offer_id");
            if (query.HasRows)
            {
                while (query.Read())
                {
                    m_specialOfferTemplates.Add(new SpecialOfferTemplate( query));
                }
            }
            query.Close();

			// Get textNameDB index.
			specialOfferNameTextDBIndex = Localiser.GetTextDBIndex("special_offer_templates - offer_name");
			specialOfferDescTextDBIndex = Localiser.GetTextDBIndex("special_offer_templates - offer_description");
		}

        static public SpecialOfferTemplate GetOfferForID(int ID)
        {
            if (m_specialOfferTemplates == null)
            {
                return null;
            }
            for (int currentTemplate = 0; currentTemplate < m_specialOfferTemplates.Count; currentTemplate++)
            {
                if (m_specialOfferTemplates[currentTemplate].OfferID == ID)
                {
                    return m_specialOfferTemplates[currentTemplate];
                }
            }
            return null;
        }


		internal static void ClearTemplates()
		{
			m_specialOfferTemplates.Clear();
		}

		static internal string GetLocaliseSpecialOfferName(Player player, int offerID)
		{
			return Localiser.GetString(specialOfferNameTextDBIndex, player, offerID);
		}

		static internal string GetLocaliseSpecialOfferDesc(Player player, int offerID)
		{
			return Localiser.GetString(specialOfferDescTextDBIndex, player, offerID);
		}
	}
}
