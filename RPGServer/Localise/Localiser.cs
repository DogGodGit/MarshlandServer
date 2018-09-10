using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;
using System.Linq;
using System.Diagnostics;
using System.Configuration;
using MainServer;
using MainServer.Combat;
using MainServer.partitioning;
using MainServer.Support;
using MainServer.player_offers;
using MainServer.TokenVendors;
using MainServer.AuctionHouse;
using MainServer.Signposting;
using System.Windows.Forms;

namespace MainServer.Localise
{
	// Use in case of sending same message to multiple recipients but need to localise
	// per client basis
	class LocaliseParams
	{
		public TextEnumDB textDB;
		public int textID;
		public object[] args;

		public LocaliseParams(TextEnumDB _textDB, int _textID, params object[] _args)
		{
			textDB = _textDB;
			textID = _textID;
			args = _args;
		}
	}

	public class TextEnumDB
	{
		// Inherit this class and declare ONLY enums
		protected string myClassName;
		protected int myClassIndex;

		public int GetIndex()
		{
			return myClassIndex;
		}

		public TextEnumDB(string className, Type enumType)
		{
			myClassName = className;
			myClassIndex = Localiser.RegisterClass(myClassName, enumType);
		}

	}

	public class LocalisedTextDB
	{
		public int index;
		public string className;
		// query by name
		public Dictionary<string, string[]> textNameDB = new Dictionary<string, string[]>();
		// query by id
		public Dictionary<int, string[]> textIDDB = new Dictionary<int, string[]>();
	}

	static class Localiser
	{
		private static Dictionary<string, LocalisedTextDB> textDBDic = new Dictionary<string, LocalisedTextDB>();
		private static List<LocalisedTextDB> textDBList = new List<LocalisedTextDB>();
		public static string[] languageIndex;
		private static Dictionary<string, int> csvLanguageIndex;
		private static LocalisedTextDB sharedTextDB = new LocalisedTextDB();
		readonly public static string TextFilter = "[^a-zA-Z0-9ก-๛]";
		readonly public static string TextWithEmptySpaceFilter = "[^a-zA-Z0-9ก-๛ ]";
		readonly public static string TextSymbolFilter = "[^a-zA-Z0-9ก-๛ ‘“'\",()/?.:;!+\n\\-#%$@©º£_|*]";
		readonly public static string TextSymbolNewLineFilter = "[^a-zA-Z0-9ก-๛ ‘“'\",()/?.:!+\n\\-#%$@©º£_|*\n]";

        public static void InitTextDB()
        {
            InitSharedTextDB();
            LoadLocalisedTextDB();
            PreRegisterTextDBEnum();

            VerifyDB();
        }

        internal static void ResetTextDB()
        {
            textDBDic.Clear();
            textDBList.Clear();

            InitTextDB();
        }

        [Conditional("DEBUG")]
		private static void VerifyDB()
		{
			string message = "";
			// check for missing key or missing translation
			foreach (var textDB in textDBList)
			{
				if (textDB.textNameDB.Count != textDB.textIDDB.Count)
				{
					// text id and name count mismatch
					// error
					message = string.Format("TextEnumDB name {0}:{1} id and name count mismatch", textDB.index, textDB.className);
					ShowErrorDialog(message);
				}

				foreach (var entry in textDB.textIDDB)
				{
					if (!textDB.textNameDB.ContainsValue(entry.Value))
					{
						// content not found
						// error
						message = string.Format("TextEnumDB name {0}:{1} key {2} not found", textDB.index, textDB.className, entry.Key);
						ShowErrorDialog(message);
					}
				}
			}

			// 
		}

		[Conditional("DEBUG")]
		private static void ShowErrorDialog(string errMessage)
		{
			MessageBox.Show(errMessage, "Error", MessageBoxButtons.OK);
		}

		private static void InitSharedTextDB()
		{
			sharedTextDB.index = -1;
			sharedTextDB.className = "SharedTextDB";
		}

		private static void PreRegisterTextDBEnum()
		{
			// pre register here to verify consistent between enums and csv
			// and avoid lazy load by static class
			RegisterClass(nameof(Character), typeof(Character.CharacterTextDB.TextID));
			RegisterClass(nameof(Mailbox), typeof(Mailbox.MailboxTextDB.TextID));
			RegisterClass(nameof(XML_Popup), typeof(XML_Popup.XML_PopupTextDB.TextID));
			RegisterClass(nameof(CharacterBountyManager), typeof(CharacterBountyManager.CharacterBountyManagerTextDB.TextID));
			RegisterClass(nameof(CharacterEffectManager), typeof(CharacterEffectManager.CharacterEffectManagerTextDB.TextID));
			RegisterClass(nameof(Clan), typeof(Clan.ClanTextDB.TextID));
			RegisterClass(nameof(CombatManager), typeof(CombatManager.CombatManagerTextDB.TextID));
			RegisterClass(nameof(CombatEntity), typeof(CombatEntity.CombatEntityTextDB.TextID));
			RegisterClass(nameof(CommandProcessor), typeof(CommandProcessor.CommandProcessorTextDB.TextID));
			RegisterClass(nameof(DuelTarget), typeof(DuelTarget.DuelTargetTextDB.TextID));
			RegisterClass(nameof(Inventory), typeof(Inventory.InventoryTextDB.TextID));
			RegisterClass(nameof(PremiumShop), typeof(PremiumShop.PremiumShopTextDB.TextID));
			RegisterClass(nameof(ItemTemplate), typeof(ItemTemplate.ItemTemplateTextDB.TextID));
			RegisterClass(nameof(EntityAreaConditionalEffect), typeof(EntityAreaConditionalEffect.EntityAreaConditionalEffectTextDB.TextID));
			RegisterClass(nameof(Party), typeof(Party.PartyTextDB.TextID));
			RegisterClass(nameof(PendingRequest), typeof(PendingRequest.PendingRequestTextDB.TextID));
			RegisterClass(nameof(FyberOfferController), typeof(FyberOfferController.FyberOfferTextDB.TextID));
			RegisterClass(nameof(SuperSonicOfferController), typeof(SuperSonicOfferController.SuperSonicOfferTextDB.TextID));
			RegisterClass(nameof(TrialpayController), typeof(TrialpayController.TrialpayTextDB.TextID));
			RegisterClass(nameof(W3iOfferController), typeof(W3iOfferController.W3iTextDB.TextID));
			RegisterClass(nameof(QuestManager), typeof(QuestManager.QuestManagerTextDB.TextID));
			RegisterClass(nameof(ServerControlledEntity), typeof(ServerControlledEntity.ServerControlledEntityTextDB.TextID));
			RegisterClass(nameof(SkillTemplateLevel), typeof(SkillTemplateLevel.SkillTemplateTextDB.TextID));
			RegisterClass(nameof(SupportActionReader), typeof(SupportActionReader.SupportActionReaderTextDB.TextID));
			RegisterClass(nameof(Zone), typeof(Zone.ZoneOfferTextDB.TextID));
			RegisterClass(nameof(CAI_ScriptContainer), typeof(CAI_ScriptContainer.CAI_ScriptContainerTextDB.TextID));
			RegisterClass(nameof(CharacterSpecialOfferManager), typeof(CharacterSpecialOfferManager.CharacterSpecialOfferManagerTextDB.TextID));
			RegisterClass(nameof(TokenVendorManager), typeof(TokenVendorManager.TokenVendorManagerTextDB.TextID));
			RegisterClass(nameof(TokenVendorNetworkManager), typeof(TokenVendorNetworkManager.TokenVendorNetworkManagerTextDB.TextID));
			RegisterClass(nameof(ShutdownMessageManager), typeof(ShutdownMessageManager.ShutdownMessageManagerTextDB.TextID));
			RegisterClass(nameof(CharacterOfferData), typeof(CharacterOfferData.CharacterOfferDataTextDB.TextID));
			RegisterClass(nameof(PlayerMail), typeof(PlayerMail.PlayerMailTextDB.TextID));
			RegisterClass(nameof(AuctionHouseManager), typeof(AuctionHouseManager.AuctionHouseManagerTextDB.TextID));
			RegisterClass(nameof(AHMailManager), typeof(AHMailManager.AHMailManagerTextDB.TextID));
			RegisterClass(nameof(BarbershopNetworkManager), typeof(BarbershopNetworkManager.BarbershopNetworkManagerTextDB.TextID));
			RegisterClass(nameof(CreateAccountTask), typeof(CreateAccountTask.CreateAccountTaskTextDB.TextID));
			RegisterClass(nameof(LoginTask), typeof(LoginTask.LoginTaskTextDB.TextID));
			RegisterClass(nameof(RequestCharListTask), typeof(RequestCharListTask.RequestCharListTaskTextDB.TextID));
			RegisterClass(nameof(StartGameTask), typeof(StartGameTask.StartGameTaskTextDB.TextID));
            RegisterClass(nameof(LinkAccountToFacebookTask), typeof(LinkAccountToFacebookTask.LinkAccountToFacebookTaskTextDB.TextID));
            RegisterClass(nameof(RegisterEmailTask), typeof(RegisterEmailTask.RegisterEmailTaskTextDB.TextID));
			RegisterClass(nameof(DeleteCharacterTask), typeof(DeleteCharacterTask.DeleteCharacterTaskTextDB.TextID));
			RegisterClass(nameof(StatusEffect), typeof(StatusEffect.StatusEffectTextDB.TextID));
			RegisterClass(nameof(SignpostAction), typeof(SignpostAction.SignpostActionTextDB.TextID));
		}

		private static void LoadLocalisedTextDB()
		{
			string filename = "RPGServer.csv";
			// use this path for now, will change to proper path later if needed
			string resDir = ConfigurationManager.AppSettings["LocalisedResourcesPath"];
			string filePath = Path.Combine(resDir, filename);

			List<LocalisedTextDB> tDBList = null;
			if (!LocalisedTextDBLoader.LoadHardCodeLocalisedTextDB(filePath, out languageIndex, out csvLanguageIndex, out tDBList))
			{
				// error...
				return;
			}

			// populate dictionary
			for (int i = 0; i < tDBList.Count; ++i)
			{
				AddTextDB(tDBList[i]);
			}

			// localize csv file for database tables.
			string[] dbFilenames = 
			{
				"unitydatadb_0.csv",
				"unitydatadb_1.csv",
				"unitydatadb_2.csv",
				"quest_templates - quest_name.csv",
				"mob_templates.csv",
				"item_templates.csv",
				"premium_shop_stock - item_shop_name.csv",
				"premium_shop_stock - item_shop_description.csv",
				"special_offer_templates - offer_name.csv",
				"special_offer_templates - offer_description.csv"
			};

			for (int i = 0; i < dbFilenames.Length; ++i)
			{
				filePath = Path.Combine(resDir, dbFilenames[i]);
				if (LocalisedTextDBLoader.LoadLocalisedTextDB(filePath, csvLanguageIndex, out tDBList))
				{
					// populate dictionary
					for (int j = 0; j < tDBList.Count; ++j)
					{
						AddTextDB(tDBList[j]);
					}
				}
			}
		}

		private static void AddTextDB(LocalisedTextDB textDB)
		{
			textDB.index = textDBList.Count;
			textDBList.Add(textDB);
			textDBDic.Add(textDB.className, textDB);
		}

		public static int RegisterClass(string className, Type enumType)
		{
			if (!enumType.IsEnum)
			{
				// error
				return -1;
			}

			if (!textDBDic.ContainsKey(className))
			{
				// failed to register, no class template
				return -1;
			}

			LocalisedTextDB locDB = textDBDic[className];
			if (locDB.textIDDB.Count != 0)
			{
				// ready registered
				return locDB.index;
			}

			int[] allValues = Enum.GetValues(enumType) as int[];
			string[] allNames = Enum.GetNames(enumType);

			for (int i = 0; i < allValues.Length; ++i)
			{
				if (!locDB.textNameDB.ContainsKey(allNames[i]))
				{
					//@TODO: if DEBUG, show error
					// no key in csv
					string message = string.Format("TextEnumDB name {0}:{1} cannot find enum name {2}", locDB.index, locDB.className, allNames[i]);
					ShowErrorDialog(message);
					continue;
				}

				locDB.textIDDB.Add(allValues[i], locDB.textNameDB[allNames[i]]);
			}

			return locDB.index;
		}

		public static LocalisedTextDB GetTextDB(int textDBIndex)
		{
			LocalisedTextDB textDB = textDBList[textDBIndex];
			return textDB;
		}

		public static int GetTextDBIndex(string tableName)
		{
			if (!textDBDic.ContainsKey(tableName))
			{
				// failed, no table template
				return -1;
			}

			LocalisedTextDB locDB = textDBDic[tableName];
			if (locDB.textIDDB.Count == 0)
			{
				// failed, this table should already been registered when load from csv file.
				return -1;
			}

			return locDB.index;
		}
        
		public static string GetString(TextEnumDB textDB, Player player, int textID)
		{
			int classIndex = textDB.GetIndex();
			return GetString(classIndex, player, textID);
		}

		public static string GetString(int textDBIndex, Player player, int textID)
		{
            // check range of list
            try
            {
                if (textDBList.Count < textDBIndex)
                {
                  throw new Exception(string.Format("textDBIndex out of Range.{0}", textDBIndex));
                }

                // range here should now be ok, get our localised text
                LocalisedTextDB locDB = textDBList[textDBIndex];

                // check in localised dic
                if (locDB.textIDDB.ContainsKey(textID))
                {
                    string[] texts = locDB.textIDDB[textID];
                    if (texts != null)
                    {
                        // check range of langId
                        int langId = player.m_languageIndex;
                        if (langId < 0 || langId >= texts.Length)
                        {
                            // default to first language
                            langId = 0;
                        }

                        // we may have just changed lang id, so check range or array lookup
                        if (locDB.textIDDB[textID].Length > langId)
                        {
                            return locDB.textIDDB[textID][langId];
                        }
                        else
                        {
                            throw new Exception("error in index lookup for TextID." + textID + " landId." + langId);
                        }
                    }
                }

                // not found...
                return string.Format("TEXT_ID_NOT_FOUND[{0}, {1}", textDBIndex, textID);
            }
            catch (Exception e)
            {
                Program.Display("Exception in Localiser.GetString textDBIndex." + textDBIndex + " textID." + textID + " ExceptionMessage." + e.Message);
                return String.Empty;

            }
		}

		public static string[] GetStringArray(LocalisedTextDB locTextDB, int itemID)
		{
			if (!locTextDB.textIDDB.ContainsKey(itemID))
			{
				int langLength = languageIndex.Length;
				string[] textNotFoundResults = new string[langLength];

				for (int i = 0; i < langLength; i++)
				{
					textNotFoundResults[i] = string.Format("ITEM_TEXT_ID_NOT_FOUND[{0}]", itemID);
				}
				return textNotFoundResults;
			}

			return locTextDB.textIDDB[itemID];
		}

		public static string GetStringByLanguageIndex(TextEnumDB textDB, int langId, int textID)
		{
			int classIndex = textDB.GetIndex();
			LocalisedTextDB locDB = textDBList[classIndex];

			// check in localised dic
			if (locDB.textIDDB.ContainsKey(textID))
			{
				string[] texts = locDB.textIDDB[textID];
				if (texts != null)
				{
					// check range of langId
					if (langId < 0 || langId >= texts.Length)
					{
						// default to first language
						langId = 0;
					}

					return locDB.textIDDB[textID][langId];
				}

				// may be newly added
			}

			// not found...
			return string.Format("TEXT_ID_NOT_FOUND[{0}, {1}", classIndex, textID);
		}

		public static string GetStringByUsername(TextEnumDB textDB, string userName, int textID)
		{
			int classIndex = textDB.GetIndex();
			LocalisedTextDB locDB = textDBList[classIndex];

			// check in localised dic
			if (locDB.textIDDB.ContainsKey(textID))
			{
				string[] texts = locDB.textIDDB[textID];
				if (texts != null)
				{
					// check range of langId
					int langId = GetLanguageIndexOfUsername(userName);
					if (langId < 0 || langId >= texts.Length)
					{
						// default to first language
						langId = 0;
					}

					return locDB.textIDDB[textID][langId];
				}

				// may be newly added
			}

			// not found...
			return string.Format("TEXT_ID_NOT_FOUND[{0}, {1}", classIndex, textID);
		}
		public static int GetLanguageIndexOfLangString(string langString)
		{
			return Array.IndexOf(languageIndex,langString);
		}

		public static int GetLanguageIndexOfCharacter(int characterID)
		{
			int langId = Program.processor.GetAccountLangaugeID(characterID);
			return langId;
		}

		public static int GetLanguageIndexOfUsername(string username)
		{
			int langId = Program.processor.GetAccountLangaugeID(username);
			return langId;
		}

		public static int CombineData(ushort input1, ushort input2)
		{
			uint output = (uint)((input1 << 16) | input2);
			return (int)output;
		}

	}
}
