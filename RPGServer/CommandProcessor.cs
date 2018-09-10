using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using Lidgren.Network;
using log4net.Util;
using MainServer.Combat;
using MainServer.CombatAlgorithms;
using MainServer.Crafting;
using MainServer.DailyLoginReward;
using MainServer.Items;
using MainServer.NamedPipeController;
using MainServer.player_offers;
using MainServer.Support;
using MainServer.TokenVendors;
using MainServer.AuctionHouse;
using MainServer.Signposting;
using MainServer.Competitions;

using XnaGeometry;
using MainServer.Factions;

using MainServer.Localise;
using MySql.Data.MySqlClient;

namespace MainServer
{
    #region enums
    enum NetworkCommandType
    {
        Login = 0,
        LoginReply = 1,
        GeneralChat = 2,
        Logout = 3,
        LogoutReply = 4,
        CreateAccount = 5,
        CreateAccountReply = 6,
        RequestCharacterList = 7,
        RequestCharacterListReply = 8,
        SelectCharacter = 9,
        SelectCharacterReply = 10,
        CreateCharacter = 11,
        CreateCharacterReply = 12,
        StartGame = 13,
        StartGameReply = 14,
        PlayerMove = 15,
        FinishedZoning = 16,
        CharacterZoningIn = 17,
        CharacterZoningOut = 18,
        DeleteCharacter = 19,
        DeleteCharacterReply = 20,
        CharacterPlayingEmote = 21,
        TradeMessage = 22,
        InactivityMessage = 23,
        AccountOptionsChange = 24,
        AccountOptionsChangeConnected = 25,
        AppearanceOnlyUpdate = 26,
        UpdateCharacterOptions = 27,
        BaseStats = 28,
        InventoryUpdate = 29,
        //SelfInitiateTrade = 22,
        //OtherInitiateTrade = 23,
        //SelfCancelTrade = 24,
        //OtherCancelTrade = 25,
        //SelfUpdateTradeSlot = 26,
        //TradeScreenUpdate = 27,
        //SelfToggleTradeReadyButton = 28,
        //OtherToggleTradeReadyButton = 29,
        BatchZonePositionUpdate = 30,
        EquipItem = 31,
        EquipItemReply = 32,
        PlayerAppearanceUpdate = 33,
        InventoryItemSwap = 34,
        RespawnPlayer = 35,
        BatchZoneMonsterUpdate = 36,
        ZoneMonsterDisappeared = 37,
        ZoneMonsterAppeared = 38,
        DeleteItem = 39,
        DeleteItemReply = 40,
        RequestShop = 41,
        RequestShopReply = 42,
        PurchaseItem = 43,
        SellItem = 44,
        CorrectPlayerPosition = 45,
        EnteringNewZoneUpdate = 46,
        StartAttackingNPC = 47,
        StopAttackingNPC = 48,
        UseSkillOnNPC = 49,
        UseSkillOnOtherPlayer = 50,
        CancelSkill = 51,
        WonCombat = 52,
        LevelUp = 53,
        mobPatrolUpdate = 54,
        InventoryItemSwapReply = 55,
        InventoryFavouriteItem = 56,

        CombatDamageMessage = 58,
        QuestStart = 59,
        QuestStartReply = 60,
        QuestStageComplete = 61,
        QuestStageProgressReply = 62,
        QuestCompleteReply = 63,
        TeleportRequest = 64,
        TeleportReply = 65, //not a standard positioning message so that teleporting animations can be used if desired
        BuySkill = 66,
        BuySkillReply = 67,
        SkillBarUpdate = 68,
        StatsUpdate = 69,
        StatusEffectUpdate = 70,
        ClientErrorMessage = 71,
        SkillListUpdate = 72,
        AttackState = 73,
        AttributesUpdate = 74,
        ItemSpawns = 75,
        ItemRespawn = 76,
        ItemDespawn = 77,
        PickupItem = 78,
        PickupItemReply = 79,
        AbilityUpdate = 80,
        PlayerSpawnPointDiscovered = 81,
        UseItem = 82,
        UseItemReply = 83,
        CharacterZoningRequest = 84,
        SkillInterrupted = 85,
        LearnAbility = 86,
        LearnAbilityReply = 87,
        ClientFriendRequest = 88,
        ServerFriendRequest = 89,
        ClientFriendReply = 90,
        ServerFreindReply = 91,
        RequestSocialLists = 92,
        FriendList = 93,
        RemoveFriend = 94,
        PartyMessage = 95,
        ClanMessage = 96,
        ReplaceItem = 97,
        BlockCharacter = 98,
        UnblockCharacter = 99,
        BlockedList = 100,
        ZoneLagList = 101,
        SkillUpdate = 102,
        ItemShop = 103,
        PlatinumReceipt = 104,
        PlatinumConfirmation = 105,
        AreaBattleUpdate = 106, // a list of everyone attacking around a character
        PermBuffUpdate = 107,
        RessurectionRequest = 108,
        LeaderBoardUpdate = 109,
        AchievementUpdate = 110,
        LeaderBoardUpdateSuccess = 111,
        AchievementUpdateSuccess = 112,
        LikedOnFaceBook = 113,
        PostedOnSocialNetwork = 114,
        PlayerHateList = 115,
        PVPMessage = 116,
        BankingMessage = 117,
        BankingMessageReply = 118,
        OthersStatusEffect = 119,
        ServerHUDUpdate = 120,
        NewEntitiesOfInterest = 121,
        EntityCombatStateChanged = 122,
        SupportRequest = 123,
        SupportRequestReply = 124,
        EntityChangedDirection = 125,
        QuestRefreshMessage = 126,
        MailMessage = 127,
        ClientChecksumFailure = 128,
        CombatEntityLocked = 129,
        GameDataMessage = 130,
        InGamePopup = 131,
        OpenWebPage = 132,
        SettingsMessage = 133,
        SettingsChangedMessage = 134,
        PlaySound = 135,
        ShowPlayerHelp = 136,
        AttemptPremiumPurchase = 137,
        PlatinumBundelInfo = 138,
        OpenGuiPage = 139,
        OpenOfferWall = 140,
        Tutorial = 141,
        LoginAdvanced = 142,
        ClearGuestAccount = 143,
        AllZoningMessagesSent = 144,
        PlatinumReceiptGooglePlay = 145,
        RateUsPopup = 146,
        OfferwallPopup = 148,
        PerformanceLog = 149,
        CharacterStuck = 150,
        PlayerIsBusy = 151,
        ClientBackgrounded = 152,
        ClientResumed = 153,
        ServerDebugMessage = 154,       // PDH
        ReqServerDynamicData = 155,   // PDH
        RecServerDynamicData = 156,     // PDH
        TokenVendorMessage = 157,
        BountyBoard = 158,
        SigilsCount = 159,
        RequestAccurateQuestData = 160,
        PreviewQuestRewards = 161,
        DeleteQuest = 162,
        RequestSpecialOffers = 163,
        TrackQuest = 164,
        TrackAllQuests = 165,
		CompanionSkillToggle = 166,
        AuctionHouse = 167, 
		SimpleMessageForThePlayer = 168,
        CastSkillOnPlayer = 169,
        MountSkillToggle = 170,
        BarbershopMessage = 171,
        CustomiseCharacter = 172,
        FactionsMessage = 173,
        CraftingMessage = 174,
        LearnRecipe = 175,
        LearnRecipeReply = 176,	
        DailyRewardMessage = 177,
		FirstTime = 180,
        RequestTweak = 181,
        TweakReply = 182,
        MobDespawnMsg = 183,
        QuestsInZoneRefresh = 184,
        MobsFactionReputationUpdate = 185,
        whte_rbt = 1234,    };

    public enum SERVER_DEBUG_TYPES
    {
        SERVER_DEBUG_ENTITY_EFFECTS = 0,
        SERVER_DEBUG_ENTITY_STATS = 1,
        SERVER_DEBUG_KEY_VAR = 2,

        SERVER_DEBUG_DEBUG_MAX
    }

    public enum GameScreen
    {
        Game = 0,
        Inventory = 1,
        Character = 2,
        Quest = 3,
        SkillTree = 4,
        Map = 5,
        Options = 6,
        Abilities = 7,
        ItemShop = 8,
        Help = 9,
        Social = 10,
        Emotes = 11,
        Shop = 12,
        Trade = 13,
        InventoryOptions = 14,
        Settings = 15,
        SocialNetworking = 16,
        Bank = 17,
        Support = 18,
        ComposeMail = 19,
        MailInbox = 20,
        OpenMail = 21,
    }
    enum PopupMessageType
    {
        OpenPopup = 1,
        OptionSelected = 2,
        ClosePopup = 3,
    };
    enum PREMIUM_SHOP_MESSAGE
    {
        PSM_REQUEST_SHOP = 0,
        PSM_SHOP_REPLY = 1,
        PSM_BUY_ITEM_REQUEST = 2,
        PSM_BUY_ITEM_REPLY = 3,
        PSM_OPEN_ITEM_SHOP_MYSTERY_CHEST = 4
    };
    enum ChecksumFailureTypes
    {
        CFT_MissingFile = 1,
        CFT_EditedFile = 2,
    }
    enum MailMessageType
    {
        MMT_MailList = 1,
        MMT_UnreadMailNotification = 2,
        MMT_MailUpdate = 3,
        MMT_SendInfo = 4, //price and recipient info
        MMT_RequestMailBox = 5,
        MMT_RequestMessageInfo = 6,
        MMT_DeleteMessage = 7,
        MMT_ReturnMessage = 8,
        MMT_TakeAttachments = 9,
        MMT_RequestSendDetails = 10,
        MMT_SendMessage = 11,
        MMT_ClosedMailBox = 12,
    }
    enum NetMessageChannel
    {
        NMC_Normal = 0,
        NMC_Login = 1,
        NMC_Movement = 2,
        NMC_Chat = 3
    };
    enum OfferMessagetype
    {
        OMT_OpenFreePlatOffers = 1,
        OMT_OpenSpecialOffers = 2,
        OMT_OpenW3iOffers = 3,
        OMT_RedeemW3iOffers = 4,
        OMT_OpenSuperSonicOffers = 5,
        OMT_OpenFyberOffers_Offerwall = 6,
        OMT_OpenFyberOffers_Video = 7
    };

    enum HW_FRIEND_REPLY
    {
        HW_FRIEND_REPLY_ERROR = 0,
        HW_FRIEND_REPLY_ACCEPT = 1,
        HW_FRIEND_REPLY_REJECT = 2,
        HW_FRIEND_REPLY_TIMEOUT = 3
    };
    enum HW_CLAN_MESSAGE_TYPE
    {
        HW_CLAN_MESSAGE_TYPE_CREATE_CLAN = 0,
        HW_CLAN_MESSAGE_TYPE_CLAN_LIST = 1,
        HW_CLAN_MESSAGE_TYPE_INVITE_MEMBER_REQUEST = 2,
        HW_CLAN_MESSAGE_TYPE_INVITE_MEMBER_REPLY = 3,
        HW_CLAN_MESSAGE_TYPE_PROMOTE_MEMBER = 4,
        HW_CLAN_MESSAGE_TYPE_DEMOTE_MEMBER = 5,
        /*
        HW_CLAN_MESSAGE_TYPE_REMOVE_MEMBER = 4,
        HW_CLAN_MESSAGE_TYPE_PROMOTE_TO_NOBLE = 5,
        HW_CLAN_MESSAGE_TYPE_PROMOTE_TO_LEADER = 6,
        HW_CLAN_MESSAGE_TYPE_DEMOTE_TO_MEMBER = 7,*/
        HW_CLAN_MESSAGE_TYPE_INVITE_MEMBER_SERVER_REQUEST = 8,
        HW_CLAN_MESSAGE_TYPE_INVITE_MEMBER_SERVER_REPLY = 9,
        HW_CLAN_MESSAGE_TYPE_DISBAND_CLAN = 10,
        HW_CLAN_MESSAGE_TYPE_CHANGE_MESSAGE = 11
    };
    enum TRADE_MESSAGE
    {
        TM_SelfInitiateTrade = 0,
        TM_OtherInitiateTrade = 1,
        TM_SelfCancelTrade = 2,
        TM_OtherCancelTrade = 3,
        TM_SelfUpdateTradeSlot = 4,
        TM_TradeScreenUpdate = 5,
        TM_SelfToggleTradeReadyButton = 6,
        TM_OtherToggleTradeReadyButton = 7,
        TM_TradeComplete = 8,
        TM_ClientAcceptTrade = 9,
        TM_OpenTradeWindow = 10

    };
    enum PVP_MESSAGE_TYPE
    {
        CLIENT_DUEL_REQUEST = 0,
        SERVER_DUEL_REQUEST = 1,
        CLIENT_DUEL_REPLY = 2,
        SERVER_DUEL_REPLY = 3,
        DUEL_BEGIN = 4,
        DUEL_END = 5
    };
    enum HW_CHAT_BOX_CHANNEL
    {
        HW_CHAT_BOX_CHANNEL_NONE = -1,
        HW_CHAT_BOX_CHANNEL_GLOBAL = 0,
        HW_CHAT_BOX_CHANNEL_TEAM = 1,
        HW_CHAT_BOX_CHANNEL_GUILD = 2,
        HW_CHAT_BOX_CHANNEL_LOCAL = 3,
        HW_CHAT_BOX_CHANNEL_SYSTEM = 4,
        HW_CHAT_BOX_CHANNEL_BATTLE = 5,
        HW_CHAT_BOX_CHANNEL_WHISPER_INCOMING = 6,
        HW_CHAT_BOX_CHANNEL_WHISPER_OUTGOING = 7,
        HW_CHAT_BOX_CHANNEL_ZONE = 8,
        HW_CHAT_BOX_CHANNEL_TRADE = 9,
        HW_CHAT_BOX_CHANNEL_COMMAND = 10,
        HW_CHAT_BOX_CHANNEL_ABILITIES = 11
    };
    enum PartyMessageType
    {
        PartyRequestFromPlayer = 0,
        PartyRequestFromServer = 1,
        PartyReplyFromPlayer = 2,
        PartyReplyFromServer = 3,
        NewPartyConfiguration = 4,
        LeaveParty = 5
    };
    public enum HW_HUD_UPDATE_TYPE
    {
        HW_HUD_UPDATE_TYPE_SKILLS = 0,
        HW_HUD_UPDATE_TYPE_ITEMS = 1,
        HW_HUD_UPDATE_TYPE_EXTRA = 2
    };
    enum SOCIAL_NETWORKING_TYPE
    {
        SNT_NONE = 0,
        SNT_FACEBOOK = 1,
        SNT_TWITTER = 2

    };
    enum SOCIAL_NETWORKING_POST_TYPE
    {
        SNPT_NONE = 0,
        SNPT_USER = 1,
        SNPT_LEVEL = 2

    };
    public enum DestinationBucket
    {
        Bucket_MainInventory = 0,
        Bucket_CharacterTradingInventory = 1
    };

    public enum SYSTEM_MESSAGE_TYPE
    {
        NONE = 0,//generic message
        SKILLS = 1,
        ITEM_USE = 2,
        TRADE = 3,
        FRIENDS = 4,
        CLAN = 5,
        BATTLE = 6,
        PARTY = 7,
        BLOCK = 8,
        SHOP = 9,
        POPUP = 10,
        PVP = 11,
    };
    enum AccountOptionsAction
    {
        AOA_None = 0,
        AOA_LinkEmail = 1,
        AOA_ChangePassword = 2,
        AOA_ResetPassword = 3,
        AOA_Reply = 4,
        AOA_LinkAccountToFacebook = 5
    };

    #endregion

    class ServerConfig
    {
        public ServerConfig(int serverConfigID, string ipaddress, int portNo)
        {
        }
    }

    class DelayedMessageDescriptor
    {
        public bool m_isMassSend;
        public NetOutgoingMessage m_msg;
        public NetConnection m_recipient;
        public NetDeliveryMethod m_deliveryMethod;
        public NetMessageChannel m_sequenceChannel;
        public NetworkCommandType m_messageType;
        public Object m_object;

        public DelayedMessageDescriptor(NetOutgoingMessage msg, NetConnection recipient, NetDeliveryMethod deliveryMethod, NetMessageChannel sequenceChannel, NetworkCommandType messageType, Object passedObject)
        {
            m_isMassSend = false;
            m_msg = msg;
            m_recipient = recipient;
            m_deliveryMethod = deliveryMethod;
            m_sequenceChannel = sequenceChannel;
            m_messageType = messageType;
            m_object = passedObject;
        }
    }


    class CommandProcessor
	{
		// #localisation
		public class CommandProcessorTextDB : TextEnumDB
		{
			public CommandProcessorTextDB() : base(nameof(CommandProcessor), typeof(TextID)) { }

			public enum TextID
			{
				PLAYER_NOT_IN_GROUP,					// "You are not in a group"
				PLAYER_NOT_IN_CLAN,						// "You are not in a clan"
				BAN_REQUIRES_NAME,						// "ban requires character name"
				BAN_REQUIRE_REASON,						// "ban requires reason"
				OTHER_NOT_FOUND,                        // "player not found : {name0}"
				OTHER_BANNED,                           // "player {name0} banned"
				GLOBAL_REQUIRES_MESSAGE,                // "global requires message"
				SILENCING_REQUIRES_NAME,                // "silencing requires character name"
				SILENCING_REQUIRES_TIME,                // "silencing requires time period"
				SILENCING_REQUIRES_REASON,              // "silencing requires reason"
				OTHER_SILENCED_PEROID,                  // "player {name0} silenced for {peroid1} seconds"
				KICKING_REQUIRES_NAME,                  // "kicking requires character name"
				KICKING_REQUIRES_REASON,                // "kicking requires reason"
				OTHER_KICKED,                           // "player {name0} kicked"
				ADDGOLD_REQUIRES_NAME,                  // "addgold requires character name"
				ADDGOLD_REQUIRES_AMOUNT,                // "addgold requires amount"
				ADDGOLD_REQUIRES_REASON,                // "addgold requires reason"
				PLAYER_GAINED_GOLD,                     // "You have gained {amount0} gold"
				OTHER_GIVEN_GOLD,                       // "player {name0} given {amount1} gold"
				ADDPLATINUM_REQUIRES_NAME,				// "addplatinum requires name"
				ADDPLATINUM_REQUIRES_AMOUNT,            // "addplatinum requires amount"
				ADDPLATINUM_REQUIRES_REASON,            // "addplatinum requires reason"
				PLAYER_GAINED_PLATINUM,                 // "You have gained {amount0} platinum"
				OTHER_GIVEN_PLATINUM,                   // "player {name0} given {amount1} platinum"
				REPORTING_REQUIRES_NAME,                // "reporting requires character name"
				REPORTING_REQUIRES_REASON,              // "reporting requires reason"
				OTHER_REPORTED,                         // "player {name0} reported"
				OTHER_OFFLINE,                          // "{name0} appears to be offline"
				PLAYER_INACTIVE,                        // "Your character appears to be inactive, you will be logged out in 1 minute"
				DATA_ERROR,                             // "Data Integrity Error, Please ensure all Checksums are correct before Patch"
				INVALID_CLAN_NAME,                      // "Please enter a clan name with valid characters"
				ALREADY_IN_CLAN,                        // "You are already in a clan"
				CLAN_NAME_ALREADY_TAKEN,                // "The clan name {clanName0} is already taken"
				CREATE_CLAN_REQUIRE_ITEM,               // "You require a {itemName0} to create a clan"
				CREATE_CLAN_NO_ITEM,                    // "You  do not have the required item to create a clan"
				CLAN_CREATED,                           // "Clan {clanName0} created"
				TOO_MANY_CLAN_ACTIONS,                  // "Too many clan actions, please wait a few seconds."
				OTHER_DECLINED_CLAN_INVITATION,         // "{name0} declined your clan invitation"
				CLAN_NOT_EXIST,                         // "This clan does not exist"
				PLAYER_ALREADY_CLAN_MEMBER,             // "You are already a member of a clan"
				PLAYER_JOINED_CLAN,                     // "You have joined {clanName0}"
				CLAN_DISBANDED,                         // "Clan Disbanded"
				PLAYER_NOT_CLAN_MEMBER,                 // "you are not a member of a clan"
				PLAYER_NO_INVITATION_RIGHTS,            // "you do not have invitation rights"
				THEY_ALREADY_CLAN_MEMBER,               // "they are already a member of a clan"
				PLAYER_INVITE_OTHER_JOIN_CLAN,          // "you invite {name0} to join your clan"
				CONFIRMATION_CHANGE_TRADE_CANCELLED,    // "Due to a change made during the confirmation stage the trade has been cancelled."
				OTHER_SET_GOLD,                         // "{name0} set gold: {amount1} gold"
				OTHER_ADDED_ITEM,                       // "{name0} added: {amount1} {itemName2}"
				OTHER_REMOVED_ITEM,                     // "{name0} removed: {amount1} {itemName2}"
				OTHER_CANCELLED_TRADE,                  // "{name0} has cancelled the trade"
				THEY_ALREADY_ASK_PLAYER_TRADE,          // "They have already asked you to trade"
				UNABLE_TRADE_PLAYER_NOT_FOUND,          // "Unable to start trade. The player could not be found."
				UNABLE_TRADE_OTHER_BUSY,                // "Unable to start trade. {name0} is currently busy."
				UNABLE_TRADE_PLAYER_BUSY,               // "Unable to start trade. You are currently busy."
				UNABLE_TRADE_PLAYER_BLOCKED,            // "You have blocked {name0}. You must unblock this character before you can trade with them."
				ALREADY_FRIEND,                         // "{name0} is already your friend"
				OTHER_NOT_ONLINE,                       // "This character is not online"
				THANK_LIKING_FACEBOOK,                  // "Thank you for Liking us on Facebook! You have been given {amount0} Platinum as a reward!"
				THANK_FOLLOWING_TWITTER,                // "Thank you for following us on twitter! You have been given {amount0} Platinum as a reward!"
				THANK_SPREADING_FACEBOOK,               // "Thank you for spreading the word on Facebook! You have been given {amount0} Platinum as a reward!"
				THANK_SPEADING_TWITTER,                 // "Thank you for spreading the word on Twitter! You have been given {amount0} Platinum as a reward!"
				OTHER_LOGGED_OUT,                       // "{name0} has logged out."
				PLAYER_ALREADY_KNOW_SKILL,              // "You already know {skillName0}"
				PATCH_OUTDATE_REENTER,                  // "Patch files not up to date.\nPlease re-enter the game from the login screen"
				PATCH_OUTDATE_RESTART,                  // "patch files not up to date.\nPlease start again from the login screen"
				CORRUPTED_DATA_ENCOUNTERED,             // "Failed To Log In\nCorrupt Data Encountered"
				WORLD_FULL,                             // "World is currently full, please try again later"
				USERNAME_IN_USE,                        // "This username is already in use"
				INVALID_EMIAL,                          // "Invalid Email"
				PLEASE_REGISTER_YOUR_EMAIL,				// "Please register your email address in the character select menu."
				REQUEST_RECEIEVED,                      // "Request Received"
				THANK_YOU_FOR_CONTACTING_SUPPORT,		// "Thank you for contacting support, your issue will be dealt with in the next few days."
				PASSWORD_INCORRECT,                     // "Password incorrect"
				PASSWORD_CONFLICTED,					// "Password conflicted, please enter another"
				SUCCESSFULLY_CHANGE_DETAILS,            // "Successfully changed details"
				REGISTERED_EMAIL_CHANGED,               // "Registered Email Changed Notification"
				NO_LONGER_REGISTERED_EMAIL,             // "<h2>Celtic Heroes</h2><p>This is no longer your registered email address. If you did not change your registered email address please contact support@onethumbmobile.com</p>"
				DETAILS_NOT_CHANGED,                    // "The details were not changed, please try again"
				SUCCESSFULLY_REGISTERED_EMAIL,          // "Successfully registered new email address"
				EMAIL_NOT_CHANGED,                      // "The email address was not changed, please try again"
				SUCCESSFULLY_CHANGE_NAME,               // "Successfully changed your name"
				NO_DETAILED_CHANGED,                    // "No details were changed, please try again"
				SUCCESSFULLY_REGISTERED_PASSWORD,       // "Successfully registered new password"
				PASSWORD_CHANGED,                       // "Password Change Notification"
				SUCCESSFULLY_CHANGE_PASSWORD,           // "<h2>Celtic Heroes</h2><p>You have successfully changed your password.<br>If you did not request this password change please reset your password from the app. Doing so will send a new password to your registered email address.</p>"
				USERNAME_OR_EMAIL_INCORRECT,            // "Username or Email incorrect"
				ACCOUNT_CURRENTLY_DISABLED,             // "This account is currently disabled"
				ACCOUNT_SUPPORT,                        // "Celtic Heroes Account Support"
				PASSWORD_HAS_BEEN_RESET,                // "Your password has been reset. \n\nnew password = {password0}\n\n"
				NEW_PASSWORD_SENT_EMAIL,                // "Your new password has been sent to your email address"
				PASSWORD_NOT_CHANGED,                   // "The password was not changed, please try again"
				ACCOUNT_HAS_BEEN_SILENCED,              // "Your account has been silenced until {dateTime0}"
				SPECIAL_OFFERS_NOT_AVAILABLE,			// "Special Offers Coming Soon!<br/>Please check back later."
				NATIVEX_NOT_AVAILABLE,					// "NativeX is not currently available."
				SUPERSONIC_NOT_AVAILABLE,               // "SuperSonic Ads are not currently available."
				FYBER_NOT_AVAILABLE,                    // "Fyber Ads are not currently available."
				ACCOUNT_HAS_BEEN_BANNED,                // "This account has been banned.\nPlease contact support to appeal this decision if you believe it is in error"
				DISCONNECTED_BY_GM,                     // "Disconnected by GM"
				PLAYER_DISCONNECTED_INACTIVITY,         // "You have been disconnected due to inactivity"
				ISSUE_WITH_GAME_FILES,                  // "There is an issue with your game files, if this problem persists please delete then reinstall the app."
				ACCOUNT_ALREADY_LOGGED_IN,              // "Login Failure\nAccount is already logged in"
				FAILED_TO_LEARN_ABILITY,                // "failed to learn ability: {errorString0}"
				SHOP_NOT_FOUND,                         // "shop not found"
				MUST_BUY_AT_LEAST_ONE,                  // "Must buy at least one item"
				NO_ITEMS_TRADED,                        // "No Items traded"
				CHARACTER_NAME_UNAVAILABLE,             // "This character name is unavailable.\nPlease try another name"
				MAX_CREATEED_CHARACTER_REACH,           // "You have already created your maximum number of character slot on this world.\nPlease purchase more slots from the item shop."
				CHARACTER_NAME_NOT_CONTAIN_USERNAME,    // "Character names must not contain the username"
                FACEBOOK_ACCOUNT_ALREADY_LINKED,        // "This facebook account is already linked to another Celtic Heroes account!"
                FACEBOOK_ACCOUNT_ALREADY_REGISTERED,    // "This facebook account is already registered to Celtic Heroes!"
            }
		}
		public static CommandProcessorTextDB textDB = new CommandProcessorTextDB();
		/**/

		private static int BACKGROUND_THREAD_SLEEP_MILLIS_DEFAULT = 1; // set one to mirror original behaviour

		#region fields

		public Object objLock = new Object();

        bool m_inShutDown;
        internal bool InShutDown
        {
            get { return m_inShutDown; }
        }

        const int CLAN_CREATION_ITEM_ID = 6;
        public float m_globalEXPMod = 1.0f;
        public float m_globalGoldMod = 1.0f;
        public NetServer m_server;
        public QuestTemplateManager m_QuestTemplateManager;

        public List<Player> m_players;
        public List<LogInFailure> m_logInFailures = new List<LogInFailure>();

        public List<Zone> m_zones;
        public List<EquipmentSet> m_equipmentSets = new List<EquipmentSet>();
        public List<Clan> m_clanList = new List<Clan>();
        public List<AchievementTemplate> m_achievement_templates = new List<AchievementTemplate>();
        public List<Party> m_parties = new List<Party>();
        public Database m_worldDB;
        public Database m_universalHubDB;
        public Database m_dataDB;
        public SqlQuery m_dataDatabaseQuery;
        public Thread m_backgroundThread;
		public Thread m_backgroundTasksThread;
        public Queue<int> m_inventoryPool;
        public int m_maxInventoryID;
        public Queue<int> m_tradeHistoryPool;
        public int m_maxTradeHistoryID;
        public Queue<int> m_mailPool;
        public Queue<BaseTask> m_backgroundTasks;
		public bool m_backgroundThreadFinished;
		public bool m_backgroundTasksThreadFinished;
        public bool m_backgroundThreadExit;
        public int m_patchVersion;
        public string m_verificationHash = String.Empty;
        internal PremiumShop m_premiumShop;
        public DateTime m_lastOldMailClearDown = DateTime.Now - TimeSpan.FromMinutes(Mailbox.MAIL_CLEAR_OLD_MIN);
        public DateTime m_lastQuestClearDown = DateTime.Today - TimeSpan.FromDays(30);
        public DateTime m_lastFailedLoginClearDown = DateTime.Now;

        public Queue<Player> m_PlayersForDeletion = new Queue<Player>();
        public Queue<Player> m_PlayerActiveCharForDeletion = new Queue<Player>();

        public static Queue<DelayedMessageDescriptor> m_delayedMessages = new Queue<DelayedMessageDescriptor>();
        public List<string> m_randomStrings = new List<string>();
        double m_lastSupportProcessTime;
        double m_lastDuplicateUserProcessTime;
        SupportActionReader m_supportReader = new SupportActionReader();
        internal TrialpayController m_trialpayController = new TrialpayController();
        internal W3iOfferController m_w3iController = new W3iOfferController();
        internal SuperSonicOfferController m_supersonicController = new SuperSonicOfferController();
        internal FyberOfferController m_fyberController = new FyberOfferController();
        internal SpecialOfferManager m_globalOfferManager;
        
        private Object m_initialLoadLock = new Object();

        internal TokenVendorNetworkManager TokenVendorNetworkManager { get; set; }
        internal BarbershopNetworkManager BarbershopNetworkManager { get; set; }
        internal CraftingNetworkHandler CraftingNetworkHandler { get; set; }
        internal CraftingTemplateManager CraftingTemplateManager { get; set; }
        public ITokenVendorManager m_tokenVendorManager;
        internal IEvasionFactorManager EvasionFactorManager { get;  set; }
        internal IMeleeDamageFluctuationManager MeleeDamageFluctuationManager { get;  set; }
        internal ISkillDamageFluctuationManager SkillDamageFluctuationManager { get;  set; }
        internal IServerControlledClientManager ServerControlledClientManager { get;  set; }

        internal TargetedSpecialOfferManager TargetedSpecialOfferManager { get; set; }
        internal AbilityVariables m_abilityVariables = new AbilityVariables();

        internal AuctionHouseManager TheAuctionHouse { get; set; }
        internal FactionTemplateManager FactionTemplateManager { get; set; }
        internal FactionNetworkManager FactionNetworkManager { get; set; }
        internal DailyRewardNetworkManager DailyRewardManager { get; set; }
        internal CompetitionManager CompetitionManager { get; set; }

        private Queue<string> m_diagnosticTaskNamesQueue = new Queue<string>(16);

        public List<string> tweakerFilenames { get; set; }

        internal string m_diagnosticCurrentTaskName = "pending";

        public float GlobalEXPMod
        {
            get { return m_globalEXPMod; }
        }
        public float GlobalGoldMod
        {
            get { return m_globalGoldMod; }
        }
        internal SpecialOfferManager GlobalOfferManager
        {
            get { return m_globalOfferManager; }
        }
        public PlayerDisconnecter m_playerDisconnecter;

        public LoadingState CurrentLoadingState { get { return commandProcessorLoading.CurrentLoadingState; } }   
        public CommandProcessorLoading commandProcessorLoading;

        private Thread m_commandProcessorLoadingThread;

        private bool m_performanceLoggingEnabled = false;
        private int m_cheatDetectionMinPerformanceMessageTimespan = 120 - 2; // give a little room

        private List<int> m_adminsListCloaked = new List<int>(2);
        private List<int> m_adminsListUncloaked = new List<int>(2);

        #endregion

        #region constructor and loading

        internal string GetCurrentTaskName()
        {
            if (m_diagnosticTaskNamesQueue.Count == 0)
                return string.Empty;
            
            return m_diagnosticTaskNamesQueue.Dequeue();
        }

        internal int GetNumRemainingBGTaskNames()
        {
            return m_diagnosticTaskNamesQueue.Count;
        }

        public CommandProcessor(NetServer server, string hubConStr, string worldConStr, string dataConStr)
        {
            m_server = server;
            m_players = new List<Player>();
            m_zones = new List<Zone>();
            m_universalHubDB = new Database(hubConStr);
            m_universalHubDB.SpawnThread();			
            m_worldDB = new Database(worldConStr);
            m_worldDB.SpawnThread();
            m_dataDB = new Database(dataConStr);
            m_dataDB.SpawnThread();
            m_inventoryPool = new Queue<int>();
            m_tradeHistoryPool = new Queue<int>();
            m_mailPool = new Queue<int>();
            m_backgroundTasks = new Queue<BaseTask>();
            m_playerDisconnecter = new PlayerDisconnecter(this);
            tweakerFilenames = new List<string>();

            var dataDB = new Database(dataConStr);
            m_dataDatabaseQuery = new SqlQuery(dataDB);
           
            //start up our main background Thread for mail/tradehistory/inventory/buffer handling
            m_backgroundThread = new Thread(new ThreadStart(updateBackgroundThread));
			m_backgroundThread.Name = "MiscBackgroundThread";
            m_backgroundThread.Start();

			// start thread that handles the BackgroundTasks classes
			m_backgroundTasksThread = new Thread(new ThreadStart(updateBackgroundTasksThread));
			m_backgroundTasksThread.Name = "BackgroundTasksThread";
			m_backgroundTasksThread.Start();

            //our loading helper 
            commandProcessorLoading = new CommandProcessorLoading(this);

            var performanceMinTimespan = ConfigurationManager.AppSettings["CheatDetect_PerformanceMsgMinTimespan"];
            if(performanceMinTimespan != null)
                m_cheatDetectionMinPerformanceMessageTimespan = int.Parse(performanceMinTimespan);

            ReloadAdminsList();
        }
        
        public void InitializeLoadingThread()
        {
            lock (m_initialLoadLock)
            {
                // catch threading issue where we can get back here again
                if (m_commandProcessorLoadingThread != null)
                {
                    Program.Display("Caught attempt to create another command processor thread, ignoring.");
                    return;
                }

                m_commandProcessorLoadingThread = new Thread(new ThreadStart(commandProcessorLoading.Initialize));
                m_commandProcessorLoadingThread.Start();
            }
        }

        		
		#endregion

		#region update

		public void Update()
        {
            
            bool finishedDequeuing = false;
            while (!finishedDequeuing)
            {
                DelayedMessageDescriptor desc = null;
                lock (m_delayedMessages)
                {
                    if (m_delayedMessages.Count > 0)
                    {
                        desc = m_delayedMessages.Dequeue();
                    }
                    else
                    {
                        finishedDequeuing = true;
                    }

                }
                if (desc != null)
                {
                    SendDelayedMessage(desc);
                }
            }
            bool finishedDeletingPlayer = false;
            while (!finishedDeletingPlayer)
            {
                Player player = null;

                lock (m_PlayersForDeletion)
                {
                    if (m_PlayersForDeletion.Count > 0)
                    {
                        player = m_PlayersForDeletion.Dequeue();
                    }
                    else
                    {
                        finishedDeletingPlayer = true;
                    }
                }
                if (player != null)
                {

                    try
                    {

                        disconnect(player, true, String.Empty);
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            DateTime nowTime = DateTime.Now;
            m_playerDisconnecter.Update(nowTime);
            if ((nowTime - Program.m_lastWorldUpdateTime).TotalMilliseconds > Program.m_worldUpdatePeriod)
            {
                Program.m_updateLoops++;
                for (int currentZone = 0; currentZone < m_zones.Count; currentZone++)
                {
                    if (Program.m_usingThreads)
                    {
                        m_zones[currentZone].m_threadWorking = true;
                    }
                    else
                    {
                        m_zones[currentZone].Update();
                    }
                }

                if (Program.m_usingThreads)
                {
                    bool allfinished = false;
                    int loops = 0;
                    while (allfinished == false)
                    {
                        loops++;
                        Thread.Sleep(1);
                        bool foundone = false;
                        for (int currentZone = 0; currentZone < m_zones.Count; currentZone++)
                        {
                            if (m_zones[currentZone].m_threadWorking == true)
                            {
                                foundone = true;
                                break;
                            }

                        }
                        if (!foundone)
                            allfinished = true;
                    }
                }
               
                if (!Program.m_StopOnError)
                {
                    for (int i = 0; i < m_players.Count; i++)
                    {

                        if (m_players[i].connection == null || Program.Server.Connections.IndexOf(m_players[i].connection) == -1)
                        {
                            Program.Display("Removing abnormally disconnected player " + m_players[i].m_UserName);
							if(m_players[i].connection == null)
								Program.Display("m_player connection was null");
							else
								Program.Display("Program.Server.Connections reference was null");
                            removePlayer(m_players[i]);
                        }
                    }
                }
                for (int i = m_parties.Count - 1; i > -1; i--)
                {
                    if (!m_parties[i].update())
                    {
                        m_parties.RemoveAt(i);
                    }
                }
                Program.MainForm.UpdateCurrentUsers(m_players.Count().ToString());
                Program.m_lastWorldUpdateTime = nowTime;
            }

            if (m_lastSupportProcessTime + 10 < Program.MainUpdateLoopStartTime())
            {
                m_lastSupportProcessTime = Program.MainUpdateLoopStartTime();
                m_supportReader.Update();
            }
            if (m_lastDuplicateUserProcessTime + 30 < Program.MainUpdateLoopStartTime())
            {
                m_lastDuplicateUserProcessTime = Program.MainUpdateLoopStartTime();
                CheckDuplicatePlayers();
            }
            if (nowTime > (m_lastOldMailClearDown + TimeSpan.FromMinutes(Mailbox.MAIL_CLEAR_OLD_MIN)))
            {
                DeleteOldMailTask task = new DeleteOldMailTask();
                lock (m_backgroundTasks)
                {
                    m_backgroundTasks.Enqueue(task);
                }
                m_lastOldMailClearDown = nowTime;
            }

            if (nowTime.Date != m_lastQuestClearDown.Date)
            {
                if (m_lastQuestClearDown.DayOfWeek > nowTime.DayOfWeek)
                {
                    DeleteCompleteRepeatableQuestsTask weekTask = new DeleteCompleteRepeatableQuestsTask(QuestTemplate.Repeatability.week_repeat);
                    lock (m_backgroundTasks)
                    {
                        m_backgroundTasks.Enqueue(weekTask);
                    }
                }
                DeleteCompleteRepeatableQuestsTask dayTask = new DeleteCompleteRepeatableQuestsTask(QuestTemplate.Repeatability.day_repeat);
                lock (m_backgroundTasks)
                {
                    m_backgroundTasks.Enqueue(dayTask);
                }
                m_lastQuestClearDown = nowTime;
                UpdateLastClearedQuestsTime();
            }
            if ((nowTime - m_lastFailedLoginClearDown).TotalMinutes > 5)
            {
                ClearExpiredLoginFailures loginFailsTask = new ClearExpiredLoginFailures();
                m_lastFailedLoginClearDown = nowTime;
                lock (m_backgroundTasks)
                {
                    m_backgroundTasks.Enqueue(loginFailsTask);
                }
            }

            // PDH - Update the server bounty manager (principally for the midnight task)
            ServerBountyManager.Update(nowTime);

            if (Program.m_trialpayActive >= 1)
            {
                m_trialpayController.Update();
            }
            if (Program.m_w3iActive >= 1)
            {
                m_w3iController.Update();
            }
            if (Program.m_superSonicActive >= 1)
            {
                m_supersonicController.Update();
            }
            if (Program.m_fyberActive >= 1)
            {
                m_fyberController.Update();
            }
            // Update the auction house
            if (Program.m_auctionHouseActive >= 1)
            {
                TheAuctionHouse.Update();
            }
        }

        public void updateLastHeardFrom()
        {
            m_dataDB.runCommandSync("update worlds set last_heard_from='" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "', active_players=" + m_players.Count() + " where world_id=" + Program.m_worldID);
        }

        public void UpdateLastClearedQuestsTime()
        {
            //param_name "last quest clear time"
            m_worldDB.runCommandSync("replace world_params (param_name,param_value) values ('last quest clear time','" + m_lastQuestClearDown.ToString("yyyy-MM-dd HH:mm:ss") + "')");
            // param_name='" + m_lastQuestClearDown.ToString("yyyy-MM-dd HH:mm:ss") + "' where param_name = 'last quest clear time'");
        }

		#endregion

		void ProcessAdvancedLogin(NetIncomingMessage msg, ref string out_messageType)
        {
            double startTime = NetTime.Now;
            NetOutgoingMessage outmsg = m_server.CreateMessage();

            Player curplayer = getPlayerfromConnection(msg);
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.LoginReply);
            if (curplayer != null)
            {
                disconnect(curplayer, false, String.Empty);
            }
           
            string userName = msg.ReadString().ToLower();

			Program.Display("received advanced login message " + userName);

            out_messageType = "advlogin " + userName;

            string password = msg.ReadString();
            byte allowedPrivate = msg.ReadByte();
            string analyticsStr = msg.ReadString();
            int patchVersion = msg.ReadVariableInt32();
			int serverAvailabilityErrorID = getServerAvailabilityError(allowedPrivate);
			string serverAvailabilityError = Localiser.GetStringByUsername(textDB, userName, (int)serverAvailabilityErrorID);
			string verificationHash = msg.ReadString();
            verificationHash = verificationHash.ToUpper();
            string deviceID = msg.ReadString();
            int registrationType = msg.ReadVariableInt32();
            string deviceToken = msg.ReadString();
			string languageString = msg.ReadString();

			if (serverAvailabilityErrorID != -1)
            {
                //report to the client that the login failed
                outmsg.Write((byte)0);
                outmsg.Write(serverAvailabilityError);
                outmsg.Write((byte)1); //goto world select
                Program.Display("login failure: " + serverAvailabilityError);
                SendMessage(outmsg, msg.SenderConnection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Login, NetworkCommandType.LoginReply);
            }
            else if (patchVersion < m_patchVersion) //permit login from any patch_version higher than the server
            {
                outmsg.Write((byte)0);
				string locText = Localiser.GetString(textDB, curplayer, (int)CommandProcessorTextDB.TextID.PATCH_OUTDATE_REENTER);
				outmsg.Write(locText);
                outmsg.Write((byte)0); //goto world select
                Program.Display("login failure: patchversion mismatch server=" + m_patchVersion + ",client=" + patchVersion);
                SendMessage(outmsg, msg.SenderConnection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Login, NetworkCommandType.LoginReply);
            }
            else if (verificationHash != m_verificationHash && Program.m_kickChecksumFailures == 2)
            {
                outmsg.Write((byte)0);
				string locText = Localiser.GetString(textDB, curplayer, (int)CommandProcessorTextDB.TextID.CORRUPTED_DATA_ENCOUNTERED); 
				outmsg.Write(locText);
				outmsg.Write((byte)0); //goto world select
                Program.Display("login failure: Checksum mismatch server=" + m_verificationHash + ",client=" + verificationHash + "|(" + userName + ")");
                SendMessage(outmsg, msg.SenderConnection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Login, NetworkCommandType.LoginReply);
            }
            else
            {
                LoginTask task = new LoginTask(curplayer, msg.SenderConnection, userName, password, allowedPrivate, analyticsStr, deviceID, deviceToken, languageString, (Player.Registration_Type)registrationType);
                
				// tag this incase they force a disconnect and we have to pull the login task out
				task.Tag = userName;
                
                lock (m_backgroundTasks)
                {
					Program.Display("Enqueuing login task for " + userName + ": " + msg.SenderConnection.RemoteEndPoint.ToString() + ", already pending: " + m_backgroundTasks.Count);
                    m_backgroundTasks.Enqueue(task);
                }
            }

            double timeTaken = (NetTime.Now - startTime);
            if (timeTaken > Program.m_longMessageThreshold)
            {
                if (curplayer != null)
                {
                    Program.Display("Login message from " + curplayer.m_UserName + " took " + NetTime.ToReadable(timeTaken) + " to process");
                }
                else
                {
                    Program.Display("Login message from unknown player took " + NetTime.ToReadable(timeTaken) + " to process");
                }
            }
        }

		private void processAuthorisedCommand(NetIncomingMessage msg, NetOutgoingMessage outmsg, NetworkCommandType commandType, Player player, ref string out_messageType)
		{
			double startTime = NetTime.Now;
			string printStr = String.Empty;
			if (Program.m_showAllMsgs)
			{
				if (player.m_activeCharacter != null)
				{
					printStr = player.connection.RemoteUniqueIdentifier.ToString("X") + ":in :chr " + player.m_activeCharacter.m_name + ": " + commandType + " " + msg.LengthBytes;
				}
				else
					printStr = player.connection.RemoteUniqueIdentifier.ToString("X") + ":in :acc " + player.m_UserName + ": " + commandType + " " + msg.LengthBytes;
			}

			// Log active character name (if available)
			// Rather that username (which can be GUID)
			string playerName = "Unknown player";
			if (null != player.m_activeCharacter)
			{
				playerName = String.Format("Player: \"{0}\" {1} [{2}]", player.m_activeCharacter.Name, player.m_UserName, player.m_account_id);
			}

            out_messageType = commandType.ToString() + " " + player.m_UserName;

            switch (commandType)
			{
				case NetworkCommandType.CreateCharacter:
					{
						processCreateCharacter(msg, outmsg, player);
						break;
					}
				case NetworkCommandType.GeneralChat:
					{
						processChatMessage(msg, player);

						break;
					}
				case NetworkCommandType.RequestCharacterList:
					{
						processRequestCharacterList(msg, outmsg, player);
						break;
					}
				case NetworkCommandType.StartGame:
					{
						processStartGame(msg, outmsg, player);
						break;
					}

				case NetworkCommandType.PlayerMove:
					{
						//player.m_activeCharacter.m_zone
						//processStartGame(msg, outmsg, player);
						processPlayerMove(msg, outmsg, player);
						break;
					}
				case NetworkCommandType.PerformanceLog:
					{
						ProcessPerformanceLogMessage(msg, player);
						break;

					}
				case NetworkCommandType.CharacterPlayingEmote:
					{
						if (player.m_activeCharacter != null && player.m_activeCharacter.Dead == false)
						{
							/*int zone_id = (int)player.m_activeCharacter.m_zone.m_zone_id;
							Zone zone = getZone(zone_id);*/
							Zone zone = player.m_activeCharacter.m_zone;
							if (zone != null)
							{
							    zone.ReadEmoteMessage(msg, m_server, player);
							}
						}
						break;
					}
				case NetworkCommandType.StartAttackingNPC:
					{
						if (player.m_activeCharacter != null)
						{
							//int zone = (int)player.m_activeCharacter.m_zone.m_zone_id;
							player.m_activeCharacter.m_zone.ReadStartAttackingMessage(msg, m_server, player); ;
						}
						break;
					}
				case NetworkCommandType.StopAttackingNPC:
					{
						if (player.m_activeCharacter != null)
						{
							player.m_activeCharacter.m_zone.ReadStopAttackingMessage(msg, m_server, player);
						}
						break;
					}
				case NetworkCommandType.UseSkillOnNPC:
					{
						if (player.m_activeCharacter != null)
						{
							player.m_activeCharacter.m_zone.ReadUseSkillMessage(msg, m_server, player);
						}
						break;
					}
				case NetworkCommandType.TeleportRequest:
					{
						if (player.m_activeCharacter != null)
						{
							player.m_activeCharacter.m_zone.ReadTeleportRequestMessage(msg, m_server, player);
						}
						break;
					}
				case NetworkCommandType.CharacterZoningRequest:
					{
						if (player.m_activeCharacter != null)
						{
							player.m_activeCharacter.m_zone.ReadZoneRequest(msg, m_server, player);
						}
						break;
					}
				case NetworkCommandType.CancelSkill:
					{
						if (player.m_activeCharacter != null)
						{
							SKILL_TYPE skillNumber = (SKILL_TYPE)msg.ReadVariableUInt32();
							if ((player.m_activeCharacter.CurrentSkill != null) && (player.m_activeCharacter.CurrentSkill.SkillID == skillNumber))
							{
								player.m_activeCharacter.CancelCurrentSkill();
							}
							if ((player.m_activeCharacter.NextSkill != null) && (player.m_activeCharacter.NextSkill.SkillID == skillNumber))
							{
								player.m_activeCharacter.CancelNextSkill();
							}
						}
						break;
					}
				case NetworkCommandType.DeleteCharacter:
					{
						processDeleteCharacter(msg, outmsg, player);
						break;
					}
				case NetworkCommandType.Logout:
					{
						processLogout(player);
						break;
					}
				case NetworkCommandType.TradeMessage:
					{
						processTradeMessage(msg, player);
						break;
					}
				case NetworkCommandType.InactivityMessage:
					{
						processInactivityMessage(msg, player);
						break;
					}
				case NetworkCommandType.AccountOptionsChangeConnected:
					{
						processAccountOptionsChangeConnectedMessage(msg, player);
						break;
					}
				case NetworkCommandType.UpdateCharacterOptions:
					{
						ProcessUpdateCharacterOptionsUpdate(msg, player);
						break;
					}
				/*case NetworkCommandType.SelfInitiateTrade:
					{
						processSelfInitiateTrade(msg, player);
						break;
					}
				case NetworkCommandType.SelfCancelTrade:
					{
						processSelfCancelTrade(msg, player);
						break;
					}
				case NetworkCommandType.SelfUpdateTradeSlot:
					{
						processUpdateTradeSlot(msg, player);
						break;
					}*/
				case NetworkCommandType.EquipItem:
					{
						processEquipItem(msg, player);
						break;
					}
				/*case NetworkCommandType.SelfToggleTradeReadyButton:
					{
						processSelfToggleTradeReady(msg, player);
						break;
					}*/
				case NetworkCommandType.DeleteItem:
					{
						processDeleteItem(msg, player);
						break;
					}
				case NetworkCommandType.RequestShop:
					{
						processRequestShop(msg, player);
						break;
					}
				case NetworkCommandType.PurchaseItem:
					{
						processPurchaseItem(msg, player);
						break;
					}
				case NetworkCommandType.SellItem:
					{
						processSellItem(msg, player);
						break;
					}
				case NetworkCommandType.ItemShop:
					{
						if (m_premiumShop != null)
						{
							m_premiumShop.ProcessPremiumShopMessage(player, msg);
						}
						break;
					}
				case NetworkCommandType.PlatinumReceipt:
					{
						if (m_premiumShop != null)
						{
							string productIdentifier = msg.ReadString();
							string transactionIdentifier = msg.ReadString();
							string base64EncodedTransactionReceipt = msg.ReadString();
							string dateStr = msg.ReadString();
							byte[] receipt = System.Convert.FromBase64String(base64EncodedTransactionReceipt);

							Program.Display("Received iOS platinum purchase request from " + playerName + " product=" + productIdentifier + " transactionID=" + transactionIdentifier + " local time=" + dateStr + " receipt=" + base64EncodedTransactionReceipt);
							Thread thread = new Thread(() => PremiumShop.ReadPlatinumPurchaseReceiptApple(player, playerName, productIdentifier, transactionIdentifier, dateStr, receipt));
							thread.Name = "PlatinumReceiptThread";
							thread.Start();
						}
						break;
					}

				case NetworkCommandType.PlatinumReceiptGooglePlay:
					{
						if (m_premiumShop != null)
						{
							string productIdentifier = msg.ReadString();
							string transactionIdentifier = msg.ReadString();
							string dateStr = msg.ReadString();

							Program.Display("Received Android platinum purchase request from " + playerName + " product=" + productIdentifier + " transactionID=" + transactionIdentifier + " local time=" + dateStr);
							Thread thread = new Thread(() => m_premiumShop.ReadPlatinumPurchaseReceiptGooglePlay(player, playerName, productIdentifier, transactionIdentifier, dateStr));
							thread.Name = "PlatinumReceiptGPlayThread";
							thread.Start();
						}
						break;
					}
				case NetworkCommandType.BuySkill:
					{
						ProcessPlayerBuySkill(msg, player);
						break;
					}
				case NetworkCommandType.InventoryFavouriteItem:
					{
						ProcessInventoryFavouriteItem(msg, player);
						break;
					}
				case NetworkCommandType.QuestStart:
					{
						processQuestStart(msg, player);
						break;
					}

				case NetworkCommandType.QuestStageComplete:
					{
						processQuestStageComplete(msg, player);
						break;
					}

				case NetworkCommandType.SkillBarUpdate:
					{
						if (player.m_activeCharacter != null)
						{
							player.m_activeCharacter.ReadUpdatedSkillBarFromMessage(msg);
						}
						break;
					}
				case NetworkCommandType.AttributesUpdate:
					{
						processAttributesUpdate(msg, player);
						break;
					}
				case NetworkCommandType.PickupItem:
					{
						processPickupItem(msg, player);
						break;
					}
				case NetworkCommandType.UseItem:
					{
						processUseItem(msg, player);
						break;
					}

				case NetworkCommandType.ClientFriendRequest:
					{
						processClientFriendRequest(msg, player);
						break;
					}
				case NetworkCommandType.ClientFriendReply:
					{
						processClientFriendReply(msg, player);
						break;
					}

				case NetworkCommandType.LearnAbility:
					{
						processLearnAbility(msg, player);
						break;
					}
				case NetworkCommandType.RequestSocialLists:
					{
						sendSocialLists(player);
						break;
					}
				case NetworkCommandType.RemoveFriend:
					{
						processRemoveFriend(msg, player);
						break;
					}
				case NetworkCommandType.PartyMessage:
					{
						if (player.m_activeCharacter != null)
						{
							player.m_activeCharacter.m_zone.ProcessPartyMessage(msg, player);
						}
						break;
					}
				case NetworkCommandType.ClanMessage:
					{
						if (player.m_activeCharacter != null)
						{
							ProcessClanMessage(msg, player);
						}
						break;
					}
				case NetworkCommandType.BlockCharacter:
					{
						processBlockCharacter(msg, player);
						break;
					}
				case NetworkCommandType.UnblockCharacter:
					{
						processUnblockCharacter(msg, player);
						break;
					}
				case NetworkCommandType.FinishedZoning:
					{
						processFinishedZoning(msg, player);
						break;

					}
				case NetworkCommandType.RessurectionRequest:
					{
						processRessurectRequest(msg, player);
						break;
					}
				case NetworkCommandType.LeaderBoardUpdateSuccess:
					{
						processUpdateLeaderBoardSuccess(msg, player);
						break;
					}
				case NetworkCommandType.AchievementUpdateSuccess:
					{
						processUpdateAchievementSuccess(msg, player);
						break;
					}
				case NetworkCommandType.LikedOnFaceBook:
					{
						processLikedOnSocialNetwork(msg, player);
						break;
					}
				case NetworkCommandType.PostedOnSocialNetwork:
					{
						processSentPostViaSocialNetwork(msg, player);
						break;
					}
				case NetworkCommandType.ClientErrorMessage:
					{
						processClientErrorMessage(msg, player);
						break;
					}
				case NetworkCommandType.PVPMessage:
					{
						if (player.m_activeCharacter != null)
						{
							player.m_activeCharacter.m_zone.ProcessPVPMessage(msg, player);
						}
						break;
					}
				case NetworkCommandType.BankingMessage:
					{
						processBankingMessage(msg, player);
						break;
					}
				case NetworkCommandType.InventoryItemSwap:
					{
						processInventoryItemSwap(msg, player);
						break;
					}
				case NetworkCommandType.SupportRequest:
					{
						procesSupportRequest(msg, player);
						break;
					}
				case NetworkCommandType.MailMessage:
					{
						if (player != null && player.m_activeCharacter != null)
						{
							player.m_activeCharacter.CharacterMail.ProcessMailMessage(msg);
						}
						break;
					}
				case NetworkCommandType.ClientChecksumFailure:
					{
						ProcessClientChecksumError(msg, player);
						break;
					}
				case NetworkCommandType.InGamePopup:
					{
						ProcessPopupMessage(msg, player);
						break;
					}
				case NetworkCommandType.SettingsChangedMessage:
					{
						ProcessSettingsChanged(msg, player);
						break;
					}
				case NetworkCommandType.OpenOfferWall:
					{
						ProcessOpenOfferWall(msg, player);
						break;
					}
				case NetworkCommandType.Tutorial:
					{
						ProcessTutorialMassage(msg, player);
						break;
					}
                case NetworkCommandType.FirstTime:
                    {
                        ProcessFirstTimeMassage(msg, player);
                        break;
                    }
                case NetworkCommandType.ClearGuestAccount:
					{
						ProcessClearGuestAccount(msg, player);
						break;
					}
				case NetworkCommandType.CharacterStuck:
					{
						ProcessCharacterStuck(player);
						break;
					}
				case NetworkCommandType.PlayerIsBusy:
					{
						ProcessPlayerBusy(msg, player);
						break;
					}
				case NetworkCommandType.ClientBackgrounded:
					{
						m_playerDisconnecter.PlayerBackground(player);
						break;
					}
				case NetworkCommandType.ClientResumed:
					{
                        m_playerDisconnecter.PlayerResume(player);
						break;
					}
				case NetworkCommandType.ReqServerDynamicData:
					{
						ProcessRequestServerDynamicData(msg, outmsg, player);
						break;
					}
				case NetworkCommandType.BountyBoard:
					{
						ProcessBountyBoardMessage(msg, player);
						break;
					}  
				case NetworkCommandType.SigilsCount:
					ProcessSigilCountMessage(msg, player);
					break;
                case NetworkCommandType.PreviewQuestRewards:
                    ProcessQuestRewardPreviewMessage(msg, player);
                    break;
                case NetworkCommandType.RequestAccurateQuestData:
                    ProcessQuestDataCheckMessage(msg, player);
                    break;
                case NetworkCommandType.DeleteQuest:
                    DeleteQuest(msg,player);
			        break;
                case NetworkCommandType.RequestSpecialOffers:
			        ProcessSendSpecialOffersNumber(player);
			        break;
                case NetworkCommandType.TrackQuest:
                    TrackQuest(msg,player);
                    break;
                case NetworkCommandType.TrackAllQuests:
                    TrackAllQuests(msg, player);
                    break;				
                case NetworkCommandType.TokenVendorMessage:
					{
						TokenVendorNetworkManager.ProcessMessage(msg, player);
						break;
					}
                case NetworkCommandType.BarbershopMessage:
			        {
                        BarbershopNetworkManager.ProcessMessage(msg,player);
			            break;
			        }
                case NetworkCommandType.CraftingMessage:
			        {
			            CraftingNetworkHandler.ProcessMessage(msg,player);
                        break;
			        }
			    case NetworkCommandType.CastSkillOnPlayer:
                    ProcessCastSkillOnPlayer(msg, player);
                    break;
                case NetworkCommandType.AuctionHouse:
                    {
                        TheAuctionHouse.ProcessAuctionHouseMessage(msg, player);
                        break;
                    }
                case NetworkCommandType.DailyRewardMessage:
			        {
                        DailyRewardManager.ProcessMessage(msg, player);
			            break;
			        }
                case NetworkCommandType.RequestTweak:
                    {
                        NetOutgoingMessage tweakMsg = Program.Server.CreateMessage();
                        tweakMsg.WriteVariableUInt32((uint)NetworkCommandType.TweakReply);
                        tweakMsg.WriteVariableInt32(tweakerFilenames.Count);
                        foreach (string s in tweakerFilenames)
                        {
                            tweakMsg.Write(s);
                        }
                        Program.processor.SendMessage(tweakMsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.TweakReply);
                        break;
                    }
                case NetworkCommandType.MobDespawnMsg:
                    {
                        processMobDespawnMsg(msg,player);
                        break;
                    }
                case NetworkCommandType.whte_rbt:
					{
                        //Program.Display("whte_rbt");    
                        //player.m_activeCharacter.Whte_Rbt();
                        
                        break;
					}
				default:
					{
						Program.Display("unknown message type received " + commandType);
						break;
					}
			}
			if (Program.m_showAllMsgs)
			{
				Program.Display(printStr + " t:" + ((NetTime.Now - startTime) * 1000).ToString("F2") + " ms");
			}
		}

        public NetOutgoingMessage processMessage(NetIncomingMessage msg, ref string out_messageType)
        {
            double startTime = NetTime.Now;
            NetOutgoingMessage outmsg = m_server.CreateMessage();
            NetworkCommandType commandType = (NetworkCommandType)msg.ReadVariableUInt32();

            //  Program.Display("received " + commandType);
            if (commandType == NetworkCommandType.Login)
            {
                Player curplayer = getPlayerfromConnection(msg);
                outmsg.WriteVariableUInt32((uint)NetworkCommandType.LoginReply);
                if (curplayer != null)
                {
                    disconnect(curplayer, false, String.Empty);
                }

                string userName = msg.ReadString().ToLower();

				Program.Display("received login message " + userName);

                out_messageType = "login " + userName;

                string password = msg.ReadString();
                byte allowedPrivate = msg.ReadByte();
                string analyticsStr = msg.ReadString();
                int patchVersion = msg.ReadVariableInt32();
				int serverAvailabilityErrorID = getServerAvailabilityError(allowedPrivate);
				string serverAvailabilityError = Localiser.GetStringByUsername(textDB, userName, (int)serverAvailabilityErrorID);
				string verificationHash = msg.ReadString();
                verificationHash = verificationHash.ToUpper();
                string deviceID = msg.ReadString();
                string deviceToken = msg.ReadString();
				string languageString = msg.ReadString();

				DataValidator.JustCheckUserName(userName);

				if (serverAvailabilityErrorID != -1)
                {
                    //report to the client that the login failed
                    outmsg.Write((byte)0);
                    outmsg.Write(serverAvailabilityError);
                    outmsg.Write((byte)1); //goto world select
                    Program.Display("login failure: " + serverAvailabilityError);
                    SendMessage(outmsg, msg.SenderConnection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Login, NetworkCommandType.LoginReply);
                }
                else if (patchVersion < m_patchVersion) //permit login from any patch_version higher than the server
                {
                    outmsg.Write((byte)0);
                    outmsg.Write("Patch files not up to date.\nPlease re-enter the game from the login screen");
                    outmsg.Write((byte)0); //goto world select
                    Program.Display("login failure: patchversion mismatch server=" + m_patchVersion + ",client=" + patchVersion);
                    SendMessage(outmsg, msg.SenderConnection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Login, NetworkCommandType.LoginReply);
                }
                else if (verificationHash != m_verificationHash && Program.m_kickChecksumFailures == 2)
                {
                    outmsg.Write((byte)0);
                    outmsg.Write("Failed To Log In\nCorrupt Data Encountered");
                    outmsg.Write((byte)0); //goto world select
                    Program.Display("login failure: Checksum mismatch server=" + m_verificationHash + ",client=" + verificationHash + "|(" + userName + ")");
                    SendMessage(outmsg, msg.SenderConnection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Login, NetworkCommandType.LoginReply);
                }
                else
                {
                    LoginTask task = new LoginTask(curplayer, msg.SenderConnection, userName, password, allowedPrivate, analyticsStr, deviceID, deviceToken, languageString, Player.Registration_Type.Normal);
					
					// tag this incase they force a disconnect and we have to pull the login task out
					task.Tag = userName;

					Program.Display("enqueuing normal login task for: " + userName + ": " + msg.SenderConnection.RemoteEndPoint.ToString() + ", already pending: " + m_backgroundTasks.Count);
                    lock (m_backgroundTasks)
                    {
                        m_backgroundTasks.Enqueue(task);
                    }
                }

                double timeTaken = (NetTime.Now - startTime);
                if (timeTaken > Program.m_longMessageThreshold)
                {
                    if (curplayer != null)
                    {
                        Program.Display("Login message from " + curplayer.m_UserName + " took " + NetTime.ToReadable(timeTaken) + " to process");
                    }
                    else
                    {
                        Program.Display("Login message from unknown player took " + NetTime.ToReadable(timeTaken) + " to process");
                    }
                }

            }
            else if (commandType == NetworkCommandType.LoginAdvanced)
            {
                ProcessAdvancedLogin(msg, ref out_messageType);
            }
            else if (commandType == NetworkCommandType.CreateAccount)
            {
                Player curplayer = getPlayerfromConnection(msg);
                outmsg.WriteVariableUInt32((uint)NetworkCommandType.CreateAccountReply);
                if (curplayer != null)
                {
                    disconnect(curplayer, false, String.Empty);
                }
                bool failed = true;
                string firstName = msg.ReadString();
                string lastName = msg.ReadString();
				string userName = msg.ReadString().ToLower();

                out_messageType = "createaccount " + userName;


				Program.Display("received login message " + userName);

				string password = msg.ReadString();
                byte allowedPrivate = msg.ReadByte();
                string analyticsStr = msg.ReadString();
                int patchVersion = msg.ReadVariableInt32();
                string uuid = msg.ReadString();
                string deviceToken = msg.ReadString();

				DataValidator.JustCheckUserName(userName);
				DataValidator.CheckNonSymbolString(ref uuid);

				int langId = Localiser.GetLanguageIndexOfUsername(userName);

				int serverAvailabilityErrorID = getServerAvailabilityError(allowedPrivate);
				string serverAvailabilityError = Localiser.GetStringByLanguageIndex(textDB, langId, (int)serverAvailabilityErrorID);
				if (serverAvailabilityErrorID != -1)
                {
                    outmsg.Write((byte)0);
                    outmsg.Write(serverAvailabilityError);
                    outmsg.Write((byte)1); //goto world select
                    Program.Display("create account failure: " + serverAvailabilityError);
                }
                else if (patchVersion < m_patchVersion) //permit login from any patch_version higher than the server
                {
                    outmsg.Write((byte)0);
					string locText = Localiser.GetStringByLanguageIndex(textDB, langId, (int)CommandProcessorTextDB.TextID.PATCH_OUTDATE_RESTART); ;
					outmsg.Write(locText);
					outmsg.Write((byte)0); //goto world select
                    Program.Display("create account failure: patchversion mismatch");
                }
                /*  else if (!Utilities.IsEmail(emailAddress))
                  {
                      outmsg.Write((byte)0);
                      outmsg.Write("The entered email address format has not been recognised");
                      outmsg.Write((byte)1); //goto world select
                      Program.Display("create account failure: " + serverAvailabilityError);
                  }
                 */
                else
                {
                    bool validName = ProfanityFilter.isAllowed(userName);
                    int id = getAccountIDFromUserName(userName);
                    bool isUUIDBanned = checkUUIDBanned(uuid);
                    if (id > -1) //player already exists
                    {
                        outmsg.Write((byte)0);
						string locText = Localiser.GetStringByLanguageIndex(textDB, langId, (int)CommandProcessorTextDB.TextID.USERNAME_IN_USE);
						outmsg.Write(locText);
						Program.Display("incorrect account create: username already taken: " + userName);
                        outmsg.Write((byte)0); //stay on main menu
                    }
                    else if (validName == false)
                    {
                        outmsg.Write((byte)0);
						string locText = Localiser.GetStringByLanguageIndex(textDB, langId, (int)CommandProcessorTextDB.TextID.USERNAME_IN_USE);
						outmsg.Write(locText);
						Program.Display("incorrect account create: invalid username: " + userName);
                        outmsg.Write((byte)0); //stay on main menu
                    }
                    else if (isUUIDBanned)
                    {
                        outmsg.Write((byte)0);
						string locText = Localiser.GetStringByLanguageIndex(textDB, langId, (int)CommandProcessorTextDB.TextID.USERNAME_IN_USE);
						outmsg.Write(locText);
						Program.Display("incorrect account create: banned uuid: " + uuid);
                        outmsg.Write((byte)0); //stay on main menu
                    }
                    else
                    {
                        failed = false;
                        CreateAccountTask newTask = new CreateAccountTask(msg.SenderConnection, firstName, lastName, userName, password, analyticsStr, uuid, deviceToken);
                        lock (m_backgroundTasks)
                        {
                            m_backgroundTasks.Enqueue(newTask);
                        }

                        //  Thread thread = new Thread(() => createAccountOffline(msg.SenderConnection, firstName, lastName, userName, password, analyticsStr));
                        // createAccountOffline(msg.SenderConnection, curplayer, firstName, lastName, userName, password, analyticsStr);
                        // thread.Start();
                    }
                }
                if (failed)
                {
                    SendMessage(outmsg, msg.SenderConnection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Login, NetworkCommandType.CreateAccountReply);
                }
                double timeTaken = NetTime.Now - startTime;
                if (timeTaken > Program.m_longMessageThreshold)
                {
                    if (!String.IsNullOrEmpty(userName))
                    {
                        Program.Display("Create Account message from " + userName + " took " + NetTime.ToReadable(timeTaken) + " to process");
                    }
                    else
                    {
                        Program.Display("Create Account message from unknown player took " + NetTime.ToReadable(timeTaken) + " to process");
                    }
                }
            }
            else if (commandType == NetworkCommandType.AccountOptionsChange)
            {
                ProcessAccountOptionChange(msg, msg.SenderConnection);
                out_messageType = "accountoptions " + msg.SenderConnection.RemoteEndPoint.ToString();
            }
            else
            {
                UInt32 sessionID = msg.ReadUInt32();
                Player player = getPlayerfromConnection(msg);
                if (player == null)
                {
                    Program.Display("received message from player that wasn't logged in: " + commandType);
                    out_messageType = "nologinDC ";
                    disconnect(msg.SenderConnection, true, String.Empty);
                }
                else if (player.m_sessionID != sessionID)
                {
                    Program.Display("SessionID mismatch server=" + player.m_sessionID + ", client=" + sessionID);
                    out_messageType = "sessionMismatch";
                }
                else
                {
                    processAuthorisedCommand(msg, outmsg, commandType, player, ref out_messageType);
                }
                double timeTaken = NetTime.Now - startTime;
                if (timeTaken > Program.m_longMessageThreshold)
                {
                    if (player != null)
                    {
                        Program.Display(commandType + " message from " + player.m_UserName + " took " + NetTime.ToReadable(timeTaken) + " to process");
                    }
                    else
                    {
                        Program.Display(commandType + " message from unknown player took " + NetTime.ToReadable(timeTaken) + " to process");
                    }
                }
            }

            return outmsg;
        }

        private void ProcessPerformanceLogMessage(NetIncomingMessage msg, Player player)
        {
            if (m_performanceLoggingEnabled == false)
                return;

            DateTime reportTime = DateTime.Now;
            
            // cheat handling
            if (player != null)
            {
                // just set m_lastPerformanceUpdate on the first entry for this player so initial value isn't huge
                if (player.m_lastPerformanceUpdate != DateTime.MinValue)
                {
                    int secondsSinceLastUpdate = (reportTime - player.m_lastPerformanceUpdate).Seconds;

                    if (secondsSinceLastUpdate < m_cheatDetectionMinPerformanceMessageTimespan)
                    {
                        // write to ban table
                        m_universalHubDB.runCommandSync(string.Format("INSERT INTO potential_cheaters(account_id, username, profiling_timespan, date_time) VALUES({0},'{1}',{2}, '{3}') ON DUPLICATE KEY UPDATE profiling_timespan = IF({2} > profiling_timespan, {2}, profiling_timespan)", player.m_account_id, player.m_UserName, secondsSinceLastUpdate, reportTime.ToString("yyyy-MM-dd HH:mm:ss")));
                        Program.Display(string.Format("Potential cheater {0}({1}), updateTime {2}, expected {3}", player.m_UserName, player.m_account_id, secondsSinceLastUpdate, m_cheatDetectionMinPerformanceMessageTimespan));
                    }
                }

                player.m_lastPerformanceUpdate = reportTime;
            }
            
            try
            {
                int fps = (int) msg.ReadVariableInt32();
                int drawcalls = (int) msg.ReadVariableInt32();
                int batchedDrawCAlls = msg.ReadVariableInt32();
                string platform = msg.ReadString();
                string device = msg.ReadString();
                bool lowEnd = msg.ReadBoolean();

                int totalAllocated = (int)msg.ReadVariableInt32();
                int totalReserved = (int)msg.ReadVariableInt32();
                int unusedReserved = (int)msg.ReadVariableInt32();
                int usedHeapSize = (int)msg.ReadVariableInt32();
                int monoHeapSize = (int)msg.ReadVariableInt32();
                int monoHeapUsed = (int)msg.ReadVariableInt32();
                int revision = (int)msg.ReadVariableInt32();
                bool fastGraphics = msg.ReadBoolean();
                bool defaultPlayers = msg.ReadBoolean();

                if (player.m_activeCharacter != null)
                {
                    string pos = player.m_activeCharacter.CurrentPosition.m_position.ToString();
                    string reportTimeString = reportTime.ToString("yyyy-MM-dd HH:mm:ss");

					/*m_universalHubDB.runCommandSync("insert into performance_metrics(account_id, character_id, zone_id, fps, platform, device, timestamp, low_end, draw_calls, batched_draw_calls, position, total_allocated, total_reserved, unused_reserved, used_heap, used_monoheap, monoheap_size, revision, fast_graphics, default_players) values ("
                                                    + player.m_account_id + ","
                                                    + player.m_activeCharacter.m_character_id + ","
                                                    + player.m_activeCharacter.m_zone.m_zone_id + ","
                                                    + fps + ",'"
                                                    + platform + "','"
                                                    + device + "','"
                                                    + reportTimeString + "',"
                                                    + lowEnd + ","
                                                    + drawcalls + ","
                                                    + batchedDrawCAlls + ","
                                                    + "'" + pos + "',"
                                                    + totalAllocated + ","
                                                    + totalReserved + ","
                                                    + unusedReserved + ","
                                                    + usedHeapSize + ","
                                                    + monoHeapUsed + ","
                                                    + monoHeapSize + ","
                                                    + revision + ","
                                                    + fastGraphics + ","
                                                    + defaultPlayers
                                                    + ");");*/

					List<MySqlParameter> sqlParams = new List<MySqlParameter>();
					sqlParams.Add(new MySqlParameter("@platform", platform));
					sqlParams.Add(new MySqlParameter("@device", device));

					m_universalHubDB.runCommandSyncWithParams("insert into performance_metrics(account_id, character_id, zone_id, fps, platform, device, timestamp, low_end, draw_calls, batched_draw_calls, position, total_allocated, total_reserved, unused_reserved, used_heap, used_monoheap, monoheap_size, revision, fast_graphics, default_players) values ("
													+ player.m_account_id + ","
													+ player.m_activeCharacter.m_character_id + ","
													+ player.m_activeCharacter.m_zone.m_zone_id + ","
													+ fps
													+ ",@platform,@device,'"
													+ reportTimeString + "',"
													+ lowEnd + ","
													+ drawcalls + ","
													+ batchedDrawCAlls + ","
													+ "'" + pos + "',"
													+ totalAllocated + ","
													+ totalReserved + ","
													+ unusedReserved + ","
													+ usedHeapSize + ","
													+ monoHeapUsed + ","
													+ monoHeapSize + ","
													+ revision + ","
													+ fastGraphics + ","
													+ defaultPlayers
													+ ");",
													sqlParams.ToArray());

					string text = String.Format("{0} : {1} : {2} : {3} : {4} : {5}", player.m_account_id,
                        player.m_activeCharacter.m_character_id, player.m_activeCharacter.m_zone.m_zone_id, fps,
                        platform, device);

                    Program.Display(text);
                }
            }
            catch (Exception ex)
            {
                Program.Display(ex.ToString());
            }
        }

	    private void ProcessSigilCountMessage(NetIncomingMessage msg, Player player)
	    {
			
			
			//cribbed from the support tool
			SqlQuery sigilsQuery = new SqlQuery(this.m_worldDB, "select perm_buffs from character_details where character_id =" + player.m_activeCharacter.m_character_id);
		    int sigilofenergy = 0;
		    int sigilofhealth = 0;
			if (sigilsQuery.Read())
			{
				string permBuffString = sigilsQuery.GetString("perm_buffs");
				string[] permBuffArray = permBuffString.Split(';');
				
				foreach (string buff in permBuffArray)
				{
					string[] itemIdAndQuantity = buff.Split(',');

					if (itemIdAndQuantity.Length > 0 && itemIdAndQuantity[0] != "")
					{
						//look for the item in our dictionary.
						ItemTemplate result = ItemTemplateManager.GetItemForID(Int32.Parse(itemIdAndQuantity[0]));
						//did we find a sigil?
						if (result != null && result.m_item_name.Contains("Sigil"))
						{							
							if (result.m_item_name.Contains("Energy"))
							{
								sigilofenergy += Int32.Parse(itemIdAndQuantity[1]);
							}
							if (result.m_item_name.Contains("Health"))
							{
								sigilofhealth+= Int32.Parse(itemIdAndQuantity[1]);
							}														
						}
					}
				}
			}
			sigilsQuery.Close();

			
			//we don't care about the message, we just need to send back a reply with the correct values
			SendSigilCount(player, sigilofenergy, sigilofhealth);
	    }

        public void processMobDespawnMsg(NetIncomingMessage msg, Player player)
        {
            int mobID = msg.ReadVariableInt32();
            bool isDespawning = player.m_activeCharacter.m_zone.getMobFromID(mobID).m_isDespawning;
            sendMobDespawnMessage(mobID,isDespawning,player);
            
        }
        public void sendMobDespawnMessage(int mobID, bool despawn,Player player)
        {
            if (player == null)
                return;

            NetOutgoingMessage despawnMsg = Program.Server.CreateMessage();
            despawnMsg.WriteVariableUInt32((uint)NetworkCommandType.MobDespawnMsg);
            despawnMsg.WriteVariableInt32(mobID);
            despawnMsg.Write(despawn);
            Program.processor.SendMessage(despawnMsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.MobDespawnMsg);
        }

	    private void SendSigilCount(Player player, int sigilOfEnergy, int sigilOfHealth)
	    {
		    //send it out
			NetOutgoingMessage sigilsMsg = Program.Server.CreateMessage();
			sigilsMsg.WriteVariableUInt32((uint)NetworkCommandType.SigilsCount);
		    sigilsMsg.WriteVariableInt32(sigilOfEnergy);
		    sigilsMsg.WriteVariableInt32(sigilOfHealth);
			Program.processor.SendMessage(sigilsMsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.SigilsCount);
			
	    }

        private void ProcessQuestRewardPreviewMessage(NetIncomingMessage msg, Player player)
        {
            bool from_log = msg.ReadBoolean();
            int quest_id = msg.ReadInt32();
            player.m_activeCharacter.m_QuestManager.previewQuestRewards(quest_id, from_log);
        }

        public void sendBackSpecialOffers(Player player)
        {
          
            //player.m_activeCharacter.OfferManager.UpdateOfferList(player.m_activeCharacter);
            if (player.m_activeCharacter.OfferManager.LoadingData == true)
            {
                player.m_activeCharacter.OfferManager.WaitingToSendSpecialOfferNumber = true;
            }
            else
            {


                List<CharacterOfferData> offerWallList = player.m_activeCharacter.OfferManager.GetOfferWalOffers();
                NetOutgoingMessage specialOfferDataMsg = Program.Server.CreateMessage();

                specialOfferDataMsg.WriteVariableUInt32((uint)NetworkCommandType.RequestSpecialOffers);
                specialOfferDataMsg.WriteVariableInt32(offerWallList.Count());


                Program.Display("Offers active: " + offerWallList.Count());
              
                Program.processor.SendMessage(specialOfferDataMsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.RequestSpecialOffers);
                
            }
        }        
        
        private void ProcessQuestDataCheckMessage(NetIncomingMessage msg, Player player)
        {
			// Create our outgoing message
            NetOutgoingMessage questDataMsg = Program.Server.CreateMessage();
			questDataMsg.WriteVariableUInt32((uint)NetworkCommandType.RequestAccurateQuestData);

            // Write the number of current quests for this character         
            questDataMsg.WriteVariableInt32(player.m_activeCharacter.m_QuestManager.m_currentQuests.Count);
            // For each quest in the player's current quests    
            foreach (Quest i in player.m_activeCharacter.m_QuestManager.m_currentQuests)
            {
                // Write the Quest ID to msg
                questDataMsg.WriteVariableInt32(i.m_quest_id);

                // Write the quest tracked status to msg
                questDataMsg.Write(i.m_tracked);

                // Write the number of quest stages for this quest
                questDataMsg.WriteVariableInt32(i.m_QuestStages.Count);

                // For each quest stage in each quest
                foreach (QuestStage j in i.m_QuestStages)
                {
                    // Write the stage ID and completion sum to msg
                    questDataMsg.WriteVariableInt32(j.m_QuestStageTemplate.m_stage_id);
                    questDataMsg.WriteVariableInt32(j.m_completion_sum); // this the progress
                }
            }

            Program.processor.SendMessage(questDataMsg,player.connection,NetDeliveryMethod.ReliableOrdered,NetMessageChannel.NMC_Normal,NetworkCommandType.RequestAccurateQuestData);            
        }

        private void DeleteQuest(NetIncomingMessage msg, Player player)
        {
            int quest_id = msg.ReadInt32();
            Program.Display("Deleting Quest ID: " + quest_id);
            player.m_activeCharacter.m_QuestManager.DeleteQuest(quest_id, true);
            player.m_activeCharacter.m_QuestManager.SendQuestRefresh();    
        }


        // TrackQuest
        // Client -> Server request to update a 'current quest' tracked status both in database and server side QuestManager 
        private void TrackQuest(NetIncomingMessage msg, Player player)
        {
            // Get variables passed (Int32, Boolean)
            int quest_id = msg.ReadInt32();
            bool tracked = msg.ReadBoolean();

            // Update database and QuestHolder
            player.m_activeCharacter.m_QuestManager.TrackQuest(quest_id, tracked);
        }

        // TrackAllQuests
        // Client -> Server request to update all 'current quests' tracked status both in database and server side QuestManager
        private void TrackAllQuests(NetIncomingMessage msg, Player player)
        {
            // Get variables passed (Boolean)
            bool track_all = msg.ReadBoolean();

            // Update database and QuestHolder
            player.m_activeCharacter.m_QuestManager.TrackAllQuests(track_all);
        }

        internal static bool checkUUIDBanned(string uuid)
        {
            SqlQuery query = new SqlQuery(Program.processor.m_universalHubDB, "select uuid from banned_uuid where uuid='" + uuid + "'");
            if (query.HasRows)
            {
                query.Close();
                return true;
            }

            query.Close();
            return false;
        }

        private int getServerAvailabilityError(byte allowedPrivate)
        {
			int stringID = -1;
            if (allowedPrivate == 0)
            {
                if (m_players.Count >= Program.m_max_users)
                {
					stringID = (int)CommandProcessorTextDB.TextID.WORLD_FULL;
				}
			}
            else
            {
                if (m_players.Count >= Program.m_max_users)
                {
					stringID = (int)CommandProcessorTextDB.TextID.WORLD_FULL;
				}
            }
			return (int)stringID;
        }

        private static int getAccountIDFromUserName(string userName)
        {
			//SqlQuery query = new SqlQuery(Program.processor.m_universalHubDB, "select account_id from account_details where user_name='" + userName + "' union select account_id from archive_account_details where user_name='" + userName + "'");
			//SqlQuery query = new SqlQuery(Program.processor.m_universalHubDB, "select account_id from account_details where user_name='" + userName + "'");

			List<MySqlParameter> sqlParams = new List<MySqlParameter>();
			sqlParams.Add(new MySqlParameter("@userName", userName));

			SqlQuery query = new SqlQuery(Program.processor.m_universalHubDB, "select account_id from account_details where user_name=@userName", sqlParams.ToArray());

			int account_id = -1;
            if (query.HasRows)
            {
                query.Read();
                account_id = query.GetInt32("account_id");
            }

            query.Close();
            return account_id;
        }

        internal static int GetAccountIDWithCharacterIDFromDatabase(int in_characterID)
        {
            SqlQuery query = new SqlQuery(Program.processor.m_worldDB, String.Format("select account_id from character_details where character_id='{0}'", in_characterID));
            int account_id = -1;
            if (query.HasRows)
            {
                query.Read();
                account_id = query.GetInt32("account_id");
            }

            query.Close();
            return account_id;
        }

        private void ProcessBountyBoardMessage(NetIncomingMessage msg, Player player)
        {
            player.m_activeCharacter.CharacterBountyManager.ProcessBountyBoardMessage(msg);
        }

        private void procesSupportRequest(NetIncomingMessage msg, Player player)
        {
            string subjectText = msg.ReadString();
            string bodyText = msg.ReadString();

            // if they have not registered an email address then bounce the request
            if (String.IsNullOrEmpty(player.m_email))
            {
				string locTextTitle = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.INVALID_EMIAL);
				string locTextBody = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.PLEASE_REGISTER_YOUR_EMAIL);
				SendSupportReply(locTextTitle, locTextBody, false, player);
			}
			//otherwise record the issue
			else
            {
                DateTime reportTime = DateTime.Now;
                string reportTimeString = reportTime.ToString("yyyy-MM-dd HH:mm:ss");
                int characterID = (int)player.m_activeCharacter.m_character_id;

                subjectText = Regex.Replace(subjectText, Localiser.TextSymbolNewLineFilter, String.Empty).Replace("\"", "\'");
                if (subjectText.Length > 200)
                {
                    subjectText = subjectText.Substring(0, 200);
                }
                bodyText = Regex.Replace(bodyText, Localiser.TextSymbolNewLineFilter, String.Empty).Replace("\"", "\'");
                if (bodyText.Length > 2000)
                {
                    bodyText = bodyText.Substring(0, 2000);
                }
				/*m_worldDB.runCommandSync("insert into  in_game_support_requests (request_date,subject,issue,character_id,name,account_id,user_name,pound_value,plat_value) values ('"
                    + reportTimeString + "',\"" + subjectText + "\",\"" + bodyText + "\"," + characterID + ",'" + player.m_activeCharacter.Name + "'," + player.m_account_id + ",'" + player.m_UserName + "'," + player.m_pounds_spent + "," + player.m_plat_purchased + ")");
                m_universalHubDB.runCommandSync("insert into account_journal (account_id,journal_date,user,details) values (" + player.m_account_id + ",'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','player',\"player has raised support request for character " + player.m_activeCharacter.Name + " \r\n\r\n" + subjectText + "\r\n\r\n " + bodyText + "\")");*/

				List<MySqlParameter> sqlParams = new List<MySqlParameter>();
				sqlParams.Add(new MySqlParameter("@request_date", reportTimeString));
				sqlParams.Add(new MySqlParameter("@subject", subjectText));
				sqlParams.Add(new MySqlParameter("@issue", bodyText));
				sqlParams.Add(new MySqlParameter("@character_id", characterID));
				sqlParams.Add(new MySqlParameter("@name", player.m_activeCharacter.Name));
				sqlParams.Add(new MySqlParameter("@account_id", player.m_account_id));
				sqlParams.Add(new MySqlParameter("@user_name", player.m_UserName));
				sqlParams.Add(new MySqlParameter("@pound_value", player.m_pounds_spent));
				sqlParams.Add(new MySqlParameter("@plat_value", player.m_plat_purchased));

				m_worldDB.runCommandSyncWithParams("insert into in_game_support_requests (request_date,subject,issue,character_id,name,account_id,user_name,pound_value,plat_value)"
					+ " values (@request_date, @subject, @issue, @character_id, @name, @account_id, @user_name, @pound_value, @plat_value)", sqlParams.ToArray());

				sqlParams.Clear();
				sqlParams.Add(new MySqlParameter("@account_id", player.m_account_id));
				sqlParams.Add(new MySqlParameter("@journal_date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
				sqlParams.Add(new MySqlParameter("@user", player.m_activeCharacter.Name));
				sqlParams.Add(new MySqlParameter("@details", "player has raised support request for character " + player.m_activeCharacter.Name + " \r\n\r\n" + subjectText + "\r\n\r\n " + bodyText));

				m_universalHubDB.runCommandSyncWithParams("insert into account_journal (account_id,journal_date,user,details) values (@account_id, @journal_date, @user, @details)", sqlParams.ToArray());

				string locTextTitle = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.REQUEST_RECEIEVED);
				string locTextBody = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.THANK_YOU_FOR_CONTACTING_SUPPORT);
				SendSupportReply(locTextTitle, locTextBody, true, player);

				if (Program.m_LogAnalytics)
                {
                    AnalyticsMain logAnalytics = new AnalyticsMain(true);

                    logAnalytics.support(player, "-1");
                }
            }
        }

        internal void SendSupportReply(string title, string body, bool clearCurrent, Player player)
        {
            NetOutgoingMessage outmsg = m_server.CreateMessage();
            //outmsg.WriteVariableUInt32((uint)NetworkCommandType.OtherCancelTrade);
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.SupportRequestReply);
            outmsg.Write(title);
            outmsg.Write(body);

            if (clearCurrent)
            {
                outmsg.Write((byte)1);
            }
            else
            {
                outmsg.Write((byte)0);
            }

            SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.SupportRequestReply);
        }

        private void processInventoryItemSwap(NetIncomingMessage msg, Player player)
        {
            //  NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            //   outmsg.WriteVariableUInt32((uint)NetworkCommandType.InventoryItemSwapReply);
            Inventory_Types inventoryType = (Inventory_Types)msg.ReadByte();
            int inventory_id1 = msg.ReadVariableInt32();
            int template_id1 = msg.ReadVariableInt32();
            int inventory_id2 = msg.ReadVariableInt32();
            int template_id2 = msg.ReadVariableInt32();

            if (player != null && player.m_activeCharacter != null)
            {
                //  outmsg.Write((byte)(byte)inventoryType);
                if (inventoryType == Inventory_Types.BACKPACK)
                {
                    player.m_activeCharacter.m_inventory.swapInventoryItem(inventory_id1, template_id1, inventory_id2, template_id2);
                    //    player.m_activeCharacter.m_inventory.WriteInventoryToMessage(outmsg);
                }
                else
                {
                    player.m_activeCharacter.m_SoloBank.swapInventoryItem(inventory_id1, template_id1, inventory_id2, template_id2);
                    //    player.m_activeCharacter.m_SoloBank.WriteInventoryToMessage(outmsg);
                }


                // SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.AccountOptionsChangeConnected);
            }
        }

        private void processBankingMessage(NetIncomingMessage msg, Player player)
        {
            // NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            //outmsg.WriteVariableUInt32((uint)NetworkCommandType.BankingMessageReply);
            byte bankTransfer = msg.ReadByte();

            if (bankTransfer == 1)
            {
                bool transferToBank = (msg.ReadByte() == 1);
                int inventory_id = msg.ReadVariableInt32();
                int template_id = msg.ReadVariableInt32();
                int quantity = msg.ReadVariableInt32();
                if (player != null && player.m_activeCharacter != null)
                {
                    player.m_activeCharacter.bankTransfer(transferToBank, inventory_id, template_id, quantity);
                }
            }
            SendBankReply(player, bankTransfer);
            /*outmsg.Write((byte)bankTransfer);
            if (player != null && player.m_activeCharacter != null)
            {
                if (bankTransfer==1)
                {
                    player.m_activeCharacter.m_inventory.WriteInventoryToMessage(outmsg);
                }
                player.m_activeCharacter.m_SoloBank.WriteInventoryToMessage(outmsg);
                SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.AccountOptionsChangeConnected);
            }*/
        }

        internal void SendBankReply(Player player, byte bankTransfer)
        {

            if (player != null && player.m_activeCharacter != null)
            {
                NetOutgoingMessage outmsg = Program.Server.CreateMessage();
                outmsg.WriteVariableUInt32((uint)NetworkCommandType.BankingMessageReply);
                outmsg.Write((byte)bankTransfer);
                //if your transferring you need to know your new inventory
                if (bankTransfer == 1)
                {
                    player.m_activeCharacter.m_inventory.WriteInventoryToMessage(outmsg);
                }
                //bank inventory
                player.m_activeCharacter.m_SoloBank.WriteInventoryToMessage(outmsg);
                //send
                SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.AccountOptionsChangeConnected);
            }
        }

        private void ProcessAccountOptionChange(NetIncomingMessage msg, NetConnection connection)
        {
            //get the type
            AccountOptionsAction selectedOption = (AccountOptionsAction)msg.ReadVariableInt32();
            switch (selectedOption)
            {
                /*case AccountOptionsAction.AOA_LinkEmail:
                    {
                        ProcessRegisterEmail(msg, connection);
                        break;
                    }
                case AccountOptionsAction.AOA_ChangePassword:
                    {
                        ProcessChangePassword(msg, connection); 
                        break;
                    }*/
                case AccountOptionsAction.AOA_ResetPassword:
                    {
                        ProcessResetPassword(msg, connection);
                        break;
                    }
                default:
                    break;
            }
        }

        private void processAccountOptionsChangeConnectedMessage(NetIncomingMessage msg, Player player)
        {
            //get the type
            AccountOptionsAction selectedOption = (AccountOptionsAction)msg.ReadVariableInt32();
            switch (selectedOption)
            {
                case AccountOptionsAction.AOA_LinkEmail:
                    {
                        //ProcessRegisterEmail(msg, player);
                        ProcessRegisterEmailNew(msg, player);
                        break;
                    }
                case AccountOptionsAction.AOA_ChangePassword:
                    {
                        ProcessChangePassword(msg, player);
                        break;
                    }
                case AccountOptionsAction.AOA_LinkAccountToFacebook:
                    {
                        ProcessLinkAccountToFacebook(msg, player);
                        break;
                    }
                default:
                    break;
            }
        }

        void SendAccountOptionChangeReply(bool success, string infoString, NetConnection connection)
        {
            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.AccountOptionsChange);
            outmsg.WriteVariableInt32((int)AccountOptionsAction.AOA_Reply);
            if (success)
            {
                outmsg.Write((byte)1);
            }
            else
            {
                outmsg.Write((byte)0);
            }
            outmsg.Write(infoString);
            SendMessage(outmsg, connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.AccountOptionsChange);
        }

        void SendAccountOptionChangeConnectedReply(bool success, string infoString, Player player)
        {
            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.AccountOptionsChangeConnected);
            outmsg.WriteVariableInt32((int)AccountOptionsAction.AOA_Reply);
            if (success)
            {
                outmsg.Write((byte)1);
            }
            else
            {
                outmsg.Write((byte)0);
            }
            outmsg.Write(infoString);
            SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.AccountOptionsChangeConnected);
        }

        internal void SendAccountOptionChangeConnectedReplyDelayed(bool success, string infoString, Player player, AccountOptionsAction accountOptionsAction = AccountOptionsAction.AOA_Reply)
        {
            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.AccountOptionsChangeConnected);
            outmsg.WriteVariableInt32((int)accountOptionsAction);
            if (success)
            {
                outmsg.Write((byte)1);
            }
            else
            {
                outmsg.Write((byte)0);
            }
            outmsg.Write(infoString);
            DelayedMessageDescriptor msgDesc = new DelayedMessageDescriptor(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.AccountOptionsChangeConnected, player);
            lock (CommandProcessor.m_delayedMessages)
            {
                CommandProcessor.m_delayedMessages.Enqueue(msgDesc);
            }
            //SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.AccountOptionsChangeConnected);
        }

        void ProcessLinkAccountToFacebook(NetIncomingMessage msg, Player player)
        {
            string currentUserName          = player.m_UserName;
            string currentHashedPassword    = player.m_hashedPass;
            string newUserName              = msg.ReadString();
            string encryptedCurrentPassword = msg.ReadString();
            string encryptedNewPassword     = msg.ReadString();

			DataValidator.JustCheckUserName(newUserName);
			DataValidator.CheckHashPassword(ref encryptedNewPassword);

            string failString = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.PASSWORD_INCORRECT);

            string unconflictedUsername  = string.Empty;
            bool   unconflictHashMatches = false;

            // conflicted users will be tagged with ^platform, strip that out incase their password was generated from their original name
            int caretIndex = currentUserName.IndexOf('^');

            if (caretIndex != -1)
            {
                unconflictedUsername = currentUserName.Substring(0, caretIndex);
                unconflictHashMatches = Utilities.hashString(unconflictedUsername + currentHashedPassword).Equals(encryptedCurrentPassword);
            }

            // is the password Correct
            if (!Utilities.hashString(currentUserName + currentHashedPassword).Equals(encryptedCurrentPassword) && !unconflictHashMatches)
            {
                // If the password was not correct tell them they failed report to the client that the change failed
                SendAccountOptionChangeConnectedReply(false, failString, player);
                return;
            }

            bool alreadyLinked = false;

			// is this facebook id already being used as a user_name 
			//string sqlString = String.Format("select user_name from account_details where user_name = '{0}'", newUserName);
			//SqlQuery query = new SqlQuery(m_universalHubDB, sqlString, true);

			List<MySqlParameter> sqlParams = new List<MySqlParameter>();
			sqlParams.Add(new MySqlParameter("@newUserName", newUserName));

			string sqlString = "select user_name from account_details where user_name = @newUserName";
			SqlQuery query = new SqlQuery(m_universalHubDB, sqlString, sqlParams.ToArray(), true);

			if (query.HasRows)
            {
                failString = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.FACEBOOK_ACCOUNT_ALREADY_REGISTERED);
                alreadyLinked = true;
            }

            // is this account id already in the linked accounts table
            sqlString = String.Format("select account_id from linked_account_details where account_id = {0}", player.m_account_id);
            query = new SqlQuery(m_universalHubDB, sqlString, true);
            if (query.HasRows)
            {
                failString = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.FACEBOOK_ACCOUNT_ALREADY_LINKED);
                alreadyLinked = true;
            }

            if (alreadyLinked == true)
            {
                SendAccountOptionChangeConnectedReply(false, failString, player);
                return;
            }

            // new fb account background task
            LinkAccountToFacebookTask linkAcccountToFbTask = new LinkAccountToFacebookTask(newUserName, encryptedNewPassword, player);
            lock (m_backgroundTasks)
            {
                m_backgroundTasks.Enqueue(linkAcccountToFbTask);
            }
        }

        void ProcessRegisterEmailNew(NetIncomingMessage msg, Player player)
        {
            double startTime = NetTime.Now;
            string username = msg.ReadString();
            string encryptedPass = msg.ReadString();
            string emailEncrypted = msg.ReadString();
            string encryptedName = msg.ReadString();
            string baseEmail = Program.DecodeFrom64(emailEncrypted);
            string normalName = Program.DecodeFrom64(encryptedName);

            string failString = String.Empty;

			long account_id = player.m_account_id;
            string hashedPassword = player.m_hashedPass;
            string userName = player.m_UserName;

			string unconflictedUsername = string.Empty;
			bool unconflictHashMatches = false;

            // catch requests with no email (client should check this already, but still...)
            if (baseEmail.Length < 1)
            {
                failString = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.INVALID_EMIAL);
                SendAccountOptionChangeConnectedReply(false, failString, player);
                return;
            }

			// conflicted users will be tagged with ^platform, strip that out incase their password was generated from their original name
			int caretIndex = userName.IndexOf('^');

			if (caretIndex != -1)
			{
				unconflictedUsername = userName.Substring(0, caretIndex);
				unconflictHashMatches = Utilities.hashString(unconflictedUsername + hashedPassword).Equals(encryptedPass);
			}

            // check password for non Facebook accounts
            if (player.m_registrationType != Player.Registration_Type.Facebook)
            {
                // is the password Correct
                if (!Utilities.hashString(userName + hashedPassword).Equals(encryptedPass) && !unconflictHashMatches) // password incorrect
                {
                    // if the password was not correct tell them they failed
                    // report to the client that the change failed
                    failString = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.PASSWORD_INCORRECT);
                    SendAccountOptionChangeConnectedReply(false, failString, player);
                    return;
                }
            }

            // go ahead with email registration
            if (baseEmail.Length > 0)
            {
                RegisterEmailTask dayTask = new RegisterEmailTask(baseEmail, player);

                lock (m_backgroundTasks)
                {
                    m_backgroundTasks.Enqueue(dayTask);
                }
            }

            // notify client - no text here, the registermail task will respond once done
            SendAccountOptionChangeConnectedReply(true, String.Empty, player);
        }

        void ProcessChangePassword(NetIncomingMessage msg, Player player)
        {
            string username = msg.ReadString();
            string encryptedPass = msg.ReadString();
            string encryptedNewPass = msg.ReadString();

			string failString = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.PASSWORD_INCORRECT);
            string failConflictString = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.PASSWORD_CONFLICTED); // "Password conflicted, please enter another."


			DataValidator.JustCheckUserName(username);
			DataValidator.CheckHashPassword(ref encryptedNewPass);

			//they exist

			long account_id = player.m_account_id;
            string hashedPassword = player.m_hashedPass;
            string userName = player.m_UserName;

			string preconflictedUsername = string.Empty;
			bool preconflictHashMatches = false;

			// conflicted users will be tagged with ^platform, strip that out incase their password was generated from their original name
			int caretIndex = userName.IndexOf('^');
			
			if (caretIndex != -1)
			{
				preconflictedUsername = userName.Substring(0, caretIndex);
				preconflictHashMatches = Utilities.hashString(preconflictedUsername + hashedPassword).Equals(encryptedPass);
			}
			
			if(Utilities.hashString(userName + hashedPassword).Equals(encryptedPass) || preconflictHashMatches) 
			{
                player.m_hashedPass = encryptedNewPass;
                string logMsg = "password changed for " + player.m_UserName;
                m_universalHubDB.runCommandSync("insert into account_journal (account_id,journal_date,user,details) values (" + player.m_account_id + ",'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',' Server : " + Program.m_ServerName + "',\"" + logMsg + "\")");

                Program.Display(logMsg);

				// we need to wait until the pw change has gone through before notifying patch to update conflict table
				MainServer.SQLSuccessDelegate onPWUpdate = delegate()
				{
					Program.DisplayDelayed("Sending deconflict request to patch for " + player.m_UserName);

					// let patch know to check if the password conflict has been resolved
					System.Net.HttpWebRequest request = Utilities.CreatePatchHttpRequest("PasswordDeconflict.aspx?params=" + userName);
					request.GetResponse();
				};

				MainServer.SQLFailureDelegate onPQFailure = delegate()
				{
					Program.DisplayDelayed("Failed to update password for " + player.m_UserName);
				};

                //Program.processor.m_universalHubDB.runCommandSync("update account_details set hashed_pwd = '" + encryptedNewPass + "' where account_id=" + account_id, onPWUpdate, onPQFailure);

				List<MySqlParameter> sqlParams = new List<MySqlParameter>();
				sqlParams.Add(new MySqlParameter("@hashed_pwd", encryptedNewPass));
				sqlParams.Add(new MySqlParameter("@account_id", account_id));

				Program.processor.m_universalHubDB.runCommandSyncWithParams("update account_details set hashed_pwd=@hashed_pwd where account_id=@account_id", sqlParams.ToArray(), onPWUpdate, onPQFailure);

				string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.SUCCESSFULLY_REGISTERED_PASSWORD);
				SendAccountOptionChangeConnectedReply(true, locText, player);
				

                //send an email to tell them
                if (player.m_email != null)
                {
					string subjectStr = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.PASSWORD_CHANGED);
					string emailStr = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.SUCCESSFULLY_CHANGE_PASSWORD);
                    SendEmailTask task = new SendEmailTask(player.m_email, Program.m_ServerEmail, String.Empty, String.Empty, subjectStr, emailStr, String.Empty);
                    lock (m_backgroundTasks)
                    {
                        m_backgroundTasks.Enqueue(task);
                    }
                }
            }
			else
            {
                //if the password was not correct tell them they failed
                //report to the client that the change failed

                SendAccountOptionChangeConnectedReply(false, failString, player);
            }
        }

        void ProcessResetPassword(NetIncomingMessage msg, NetConnection connection)
        {
            string username = msg.ReadString();
            string emailEncrypted = msg.ReadString();

            string baseEmail = Program.DecodeFrom64(emailEncrypted);

			int langId = Localiser.GetLanguageIndexOfUsername(username);
			string failString = Localiser.GetStringByLanguageIndex(textDB, langId, (int)CommandProcessorTextDB.TextID.USERNAME_OR_EMAIL_INCORRECT);

			DataValidator.JustCheckUserName(username);

			//SqlQuery query = new SqlQuery(m_universalHubDB, "select * from account_details where user_name='" + username + "'", true);

			List<MySqlParameter> sqlParams = new List<MySqlParameter>();
			sqlParams.Add(new MySqlParameter("@username", username));

			SqlQuery query = new SqlQuery(m_universalHubDB, "select * from account_details where user_name=@username", sqlParams.ToArray(), true);

			if (!query.HasRows) //no such player
            {
                //username does not exist
                SendAccountOptionChangeReply(false, failString, connection);
            }
            else
            {
                //they exist
                query.Read();
                int account_id = query.GetInt32("account_id");
                string hashedPassword = query.GetString("hashed_pwd");
                string userName = query.GetString("user_name");
                string email = String.Empty;
                email = query.GetString("email");

                //has the account been disabled
                if (query.GetBoolean("disabled")) // account has been disabled
                {
                    //report to the client that the change failed
					string locText = Localiser.GetStringByLanguageIndex(textDB, langId, (int)CommandProcessorTextDB.TextID.ACCOUNT_CURRENTLY_DISABLED);
					SendAccountOptionChangeReply(false, locText, connection);
					return;
                }
                //is the password Correct
                else if (email.Length == 0 || email != baseEmail) // email incorrect
                {
                    //if the password was not correct tell them they failed
                    //report to the client that the change failed

                    SendAccountOptionChangeReply(false, failString, connection);
                }
                else
                {
                    //generate the new password
                    string newPassword = GenerateRandomPassword();
                    string newHashedPassword = Utilities.hashString(newPassword);

					//Program.processor.m_universalHubDB.runCommandSync("update account_details set email = '" + baseEmail + "' where account_id=" + account_id);

					//if the password is correct then change the email 
					//try to send an email to it to comfirm
					string locSubject = Localiser.GetStringByLanguageIndex(textDB, langId, (int)CommandProcessorTextDB.TextID.ACCOUNT_SUPPORT);
					string locBody = Localiser.GetStringByLanguageIndex(textDB, langId, (int)CommandProcessorTextDB.TextID.PASSWORD_HAS_BEEN_RESET);
					locBody = String.Format(locBody, newPassword);

					bool sendSucessfull = Program.MailHandler.sendMail(baseEmail, Program.m_ServerEmail, String.Empty, String.Empty, locSubject, locBody, String.Empty);
					if (sendSucessfull)
                    {
                        Program.Display("password reset for " + username);
                        Program.processor.m_universalHubDB.runCommandSync("update account_details set hashed_pwd = '" + newHashedPassword + "' where account_id=" + account_id);
						string locText = Localiser.GetStringByLanguageIndex(textDB, langId, (int)CommandProcessorTextDB.TextID.NEW_PASSWORD_SENT_EMAIL);
						SendAccountOptionChangeReply(true, locText, connection);
					}
                    else
                    {
						string locText = Localiser.GetStringByLanguageIndex(textDB, langId, (int)CommandProcessorTextDB.TextID.PASSWORD_NOT_CHANGED);
						SendAccountOptionChangeReply(false, locText, connection);
					}
                }
            }

            // SendAccountOptionChangeReply(false, "Reset Password Set To Fail, email entered " + baseEmail, connection);
        }
        string GenerateRandomPassword()
        {
            string randString = String.Empty;
            //pick a starting string
            randString = m_randomStrings[Program.getRandomNumber(m_randomStrings.Count)];

            //randomly capitalize a letter
            int indexToCaps = Program.getRandomNumber(randString.Length);

            //randString = randString.Replace(randString[indexToCaps],char.ToUpper(randString[indexToCaps]));
            //add a number to the end

            randString += 100 + Program.getRandomNumber(899);
            return randString;
        }
        void ProcessSettingsChanged(NetIncomingMessage msg, Player player)
        {
            int numSettings = msg.ReadVariableInt32();
            for (int i = 0; i < numSettings; i++)
            {
                ReadSettingFromMessage(msg, player);
            }
        }

        internal void SendSettings(Player player, bool inBackground)
        {
            NetOutgoingMessage outmsg = m_server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.SettingsMessage);
            outmsg.WriteVariableInt32(2);
            WriteSettingToMessage(outmsg, player, Player.SettingTypes.deviceNotifications);
            WriteSettingToMessage(outmsg, player, Player.SettingTypes.emailNotifications);
            if (inBackground)
            {
                DelayedMessageDescriptor msgDesc = new DelayedMessageDescriptor(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.SettingsMessage, player);
                lock (CommandProcessor.m_delayedMessages)
                {
                    CommandProcessor.m_delayedMessages.Enqueue(msgDesc);
                }
            }
            else
            {
                SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.SettingsMessage);
            }
        }

        internal void SendRegisterAccountRequired(Player player)
        {
            NetOutgoingMessage outmsg = m_server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.SettingsMessage);
            outmsg.WriteVariableInt32(1);//one item to send
            WriteSettingToMessage(outmsg, player, Player.SettingTypes.registerRequired);
            SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.SettingsMessage);
        }

        internal void SendRateUs(Player player)
        {
            Program.processor.SendXMLPopupMessage(false, player, (int)XML_Popup.Set_Popup_IDs.SPI_RateUsPopup, XML_Popup.Popup_Type.None, "popup_rate_us", null, false);
            NetOutgoingMessage outmsg = m_server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.RateUsPopup);
            outmsg.WriteVariableInt32((int) XML_Popup.Set_Popup_IDs.SPI_RateUsPopup);
            // XML popup will fail to load the filename (since it's got specific android/ios versions
            // This will mean the server thinks it's open but client opens a popup after this xml popup fails to open
            SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.RateUsPopup);
        }

        internal void SendOfferwallPopup(Player player)
        {
            NetOutgoingMessage outmsg = m_server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.OfferwallPopup);
            outmsg.WriteVariableInt32(1);//one item to send
            SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.OfferwallPopup);
        }

        void WriteSettingToMessage(NetOutgoingMessage outmsg, Player player, Player.SettingTypes settingType)
        {
            outmsg.WriteVariableInt32((int)settingType);
            switch (settingType)
            {
                case Player.SettingTypes.deviceNotifications:
                    {
                        if (player.m_deviceNotificationsOn)
                        {
                            outmsg.Write((byte)1);
                        }
                        else
                        {
                            outmsg.Write((byte)0);
                        }
                        break;
                    }
                case Player.SettingTypes.emailNotifications:
                    {
                        if (player.m_emailNotificationsOn)
                        {
                            outmsg.Write((byte)1);
                        }
                        else
                        {
                            outmsg.Write((byte)0);
                        }
                        break;
                    }
                case Player.SettingTypes.registerRequired:
                    {//currently this setting is only ever set to true
                        outmsg.Write((byte)1);
                    }
                    break;
                default:
                    {
                        break;
                    }
            }
        }

        void ReadSettingFromMessage(NetIncomingMessage msg, Player player)
        {
            Player.SettingTypes settingType = (Player.SettingTypes)msg.ReadVariableInt32();
            switch (settingType)
            {
                case Player.SettingTypes.deviceNotifications:
                    {
                        byte onByte = msg.ReadByte();
                        bool settingOn = (onByte == 1);
                        int tokenLen = msg.ReadVariableInt32();
                        byte[] notificationToken = null;
                        if (tokenLen > 0)
                        {
                            notificationToken = msg.ReadBytes(tokenLen);
                        }
                        int notificationTypes = msg.ReadVariableInt32();
                        string uuid = msg.ReadString();
                        player.m_notificationToken = String.Empty;
                        if (notificationToken != null)
                        {
                            player.m_notificationToken = Utilities.baToHex(notificationToken);
                        }
                        player.m_notificationType = notificationTypes;
                        player.m_notificationDevice = uuid;
                        player.SetDeviceNotificationsOn(settingOn);
                        player.CheckForNotificationsChange();
                        break;
                    }
                case Player.SettingTypes.emailNotifications:
                    {
                        byte onByte = msg.ReadByte();
                        bool settingOn = (onByte == 1);
                        player.SetEmailNotificationsOn(settingOn);
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }
        private void processUpdateLeaderBoardSuccess(NetIncomingMessage msg, Player player)
        {
            Character character = player.m_activeCharacter;
            RankingsManager.RANKING_TYPE rankingType = (RankingsManager.RANKING_TYPE)msg.ReadVariableInt32();
            if (character != null)
            {
                player.m_AccountRankings.confirmed(rankingType);
                if (Program.m_LogRanking)
                    Program.Display("ranking update confirmed " + rankingType);
            }
        }

        private void processUpdateAchievementSuccess(NetIncomingMessage msg, Player player)
        {
            Character character = player.m_activeCharacter;
            AchievementsManager.ACHIEVEMENT_TYPE achievementType = (AchievementsManager.ACHIEVEMENT_TYPE)msg.ReadVariableInt32();
            int tempAchievementID = Convert.ToInt32(achievementType);
            if (character != null)
            {
                player.m_AccountAchievements.confirmed(achievementType);
                Achievement achievement = player.m_AccountAchievements.getAchievement(achievementType);
                if (achievement != null && achievement.m_template != null && achievement.m_value >= achievement.m_template.m_target)//100)
                {
                    if (Program.m_LogAnalytics)
                    {
                        AnalyticsMain logAnalytics = new AnalyticsMain(true);
                        logAnalytics.achievement(player, tempAchievementID.ToString(), achievementType.ToString());
                    }
                }
            }
        }

        private void processFinishedZoning(NetIncomingMessage msg, Player player)
        {
            Character character = player.m_activeCharacter;
            if (character != null)
            {
                character.InLimbo = false;
                character.SignpostsNeedRechecked();
                Program.processor.AddPlayerToZone(player, character.m_zone.m_zone_id);
                for (int i = 0; i < m_parties.Count; i++)
                {
                    if (m_parties[i].checkPreviousMembers(character))
                    {
                        m_parties[i].SendNewPartyConfiguration();
                        break;
                    }
                }
                player.m_activeCharacter.sendStatsUpdate();
                player.CheckForNotificationsChange();
            }
        }

        #region chat
        private void processChatMessage(NetIncomingMessage msg, Player player)
        {
            //SendXMLPopupMessage(player, 1, XML_Popup.Popup_Type.None, "testhtml.txt", null, true);

            if (player.m_silencedUntil > DateTime.Now)
            {
				string modstring = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.ACCOUNT_HAS_BEEN_SILENCED);
				modstring = String.Format(modstring, Utilities.GetFormatedDateTimeString(player.m_silencedUntil));
				Program.processor.SendXMLPopupMessage(true, player, (int)XML_Popup.Set_Popup_IDs.SPI_BasicPopup, XML_Popup.Popup_Type.None, "popup_template.txt", new List<string> { modstring, " modal = \"true\" active = \"connected\"", String.Empty, String.Empty }, false);

                return;
            }

            HW_CHAT_BOX_CHANNEL chatChannel = (HW_CHAT_BOX_CHANNEL)msg.ReadVariableInt32();
            string receivedmsg = msg.ReadString();
            /*if (player.m_account_id == 58)
            {//\ue524
                SendNotificationTask task = new SendNotificationTask(95, "Fire Lady Says:" + receivedmsg + " \U0001f42f" + " Æ", 1, "default");
                lock (m_backgroundTasks)
                {
                    m_backgroundTasks.Enqueue(task);
                }
            }*/
            /*if (chatChannel == HW_CHAT_BOX_CHANNEL.HW_CHAT_BOX_CHANNEL_GLOBAL)
            {
                sendGlobalChatMessage(chatmsg, player);
            }*/
            int targetID = 0;
            string whisperTargetStr = String.Empty;
            Player whisperTarget = null;
            //now filter the message
            if (chatChannel == HW_CHAT_BOX_CHANNEL.HW_CHAT_BOX_CHANNEL_COMMAND)
            {

                int indexOfFirstSpace = receivedmsg.IndexOf(' ');
                if (indexOfFirstSpace < 1)
                {
                    chatChannel = HW_CHAT_BOX_CHANNEL.HW_CHAT_BOX_CHANNEL_LOCAL;
                }
                else if (receivedmsg.IndexOf('/') == 0 && receivedmsg.Length > 1 && player.m_moderatorLevel > MODERATOR_LEVEL.STANDARD_PLAYER)
                {
                    processCommand(player, receivedmsg.Substring(1));
                }
                else
                {
                    string command = receivedmsg.Substring(0, indexOfFirstSpace).ToLower();
                    string originalMessage = receivedmsg;
                    receivedmsg = receivedmsg.Substring(indexOfFirstSpace + 1);
                    switch (command)
                    {
                        case "c":
                            chatChannel = HW_CHAT_BOX_CHANNEL.HW_CHAT_BOX_CHANNEL_GUILD;
                            break;
                        case "s":

                            chatChannel = HW_CHAT_BOX_CHANNEL.HW_CHAT_BOX_CHANNEL_ZONE;
                            break;
                        case "w":
                            int indexOfspace = receivedmsg.IndexOf(", ");

                            if (indexOfspace > 0 && (indexOfspace + 2) < receivedmsg.Length)
                            {
                                chatChannel = HW_CHAT_BOX_CHANNEL.HW_CHAT_BOX_CHANNEL_WHISPER_OUTGOING;
                                string charName = receivedmsg.Substring(0, indexOfspace);
                                whisperTargetStr = charName;
                                whisperTarget = findPlayerByCharName(charName);
                                if (whisperTarget != null)
                                {
                                    receivedmsg = receivedmsg.Substring(indexOfspace + 2);
                                    targetID = (int)whisperTarget.m_activeCharacter.m_character_id;
                                }
                            }
                            else
                            {
                                chatChannel = HW_CHAT_BOX_CHANNEL.HW_CHAT_BOX_CHANNEL_LOCAL;
                            }
                            break;
                        case "g":
                            chatChannel = HW_CHAT_BOX_CHANNEL.HW_CHAT_BOX_CHANNEL_TEAM;
                            break;
                        default:
                            receivedmsg = originalMessage;
                            chatChannel = HW_CHAT_BOX_CHANNEL.HW_CHAT_BOX_CHANNEL_LOCAL;
                            break;
                    }
                }
            }
            receivedmsg = Regex.Replace(receivedmsg, Localiser.TextSymbolFilter, String.Empty);
            int maxMessageLength = 2000;
            if (receivedmsg.Length > maxMessageLength)
            {
                receivedmsg = receivedmsg.Remove(maxMessageLength - 5);
                receivedmsg += "...";
            }
            string chatmsg = ProfanityFilter.replaceOffendingStrings(receivedmsg);
            if (chatmsg.Trim().Length == 0)
                return;

            switch (chatChannel)
            {
                case HW_CHAT_BOX_CHANNEL.HW_CHAT_BOX_CHANNEL_LOCAL:
                    {
                        if (player.m_activeCharacter != null)
                        {
                            if (player.m_activeCharacter.m_zone != null)
                            {
                                player.m_activeCharacter.m_zone.SendLocalChatMessage(chatmsg, player);
                            }
                        }
                        break;
                    }
                case HW_CHAT_BOX_CHANNEL.HW_CHAT_BOX_CHANNEL_ZONE:
                    {
                        if (player.m_activeCharacter != null)
                        {
                            if (player.m_activeCharacter.m_zone != null)
                            {
                                player.m_activeCharacter.m_zone.SendZoneChatMessage(chatmsg, player);
                            }
                        }
                        break;
                    }
                case HW_CHAT_BOX_CHANNEL.HW_CHAT_BOX_CHANNEL_GLOBAL:
                    {
                        sendGlobalChatMessage(chatmsg, player);
                        break;
                    }
                case HW_CHAT_BOX_CHANNEL.HW_CHAT_BOX_CHANNEL_TEAM:
                    {
                        if (player.m_activeCharacter != null)
                        {
                            if (player.m_activeCharacter.CharacterParty != null)
                            {
                                player.m_activeCharacter.CharacterParty.SendPartyChatMessage(chatmsg, player);
                            }
                            else
                            {
								string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.PLAYER_NOT_IN_GROUP);
								sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.NONE);
                            }
                        }
                        break;
                    }
                case HW_CHAT_BOX_CHANNEL.HW_CHAT_BOX_CHANNEL_GUILD:
                    {
                        if (player.m_activeCharacter != null)
                        {
                            if (player.m_activeCharacter.CharactersClan != null)
                            {
                                player.m_activeCharacter.CharactersClan.SendClanChatMessege(chatmsg, player);
                            }
                            else
                            {
								string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.PLAYER_NOT_IN_CLAN);
								sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.NONE);
                            }
                        }
                        break;
                    }
                case HW_CHAT_BOX_CHANNEL.HW_CHAT_BOX_CHANNEL_WHISPER_OUTGOING:
                    {
                        if (player.m_activeCharacter != null)
                        {
                            sendWhisperTo(whisperTarget, chatmsg, player, whisperTargetStr);
                        }
                        break;
                    }
                case HW_CHAT_BOX_CHANNEL.HW_CHAT_BOX_CHANNEL_TRADE:
                    {
                        if (player.m_activeCharacter != null)
                        {
                            sendTradeChatMessage(chatmsg, player);
                        }
                        break;
                    }
                case HW_CHAT_BOX_CHANNEL.HW_CHAT_BOX_CHANNEL_COMMAND:
                    {
                        break;
                    }
                default:
                    {
                        Program.Display("received Chat message of unknown type:" + chatChannel + " from " + player.m_account_id);
                        break;
                    }
            }
            if (chatmsg.Length > maxMessageLength)
            {
                chatmsg = chatmsg.Remove(maxMessageLength - 5);
                chatmsg += "...";
            }

			// have to delimit the doublequote delimiter so sql doesn't get confused
            chatmsg = chatmsg.Replace("\"", "\\\"");

			//m_worldDB.runCommandSync("insert into chat_log (chat_date,chat_channel,character_id,target_character_id,message) values ('" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'," + (int)chatChannel + "," + player.m_activeCharacter.m_character_id + "," + targetID + ",\"" + chatmsg + "\")");

			List<MySqlParameter> sqlParams = new List<MySqlParameter>();
			sqlParams.Add(new MySqlParameter("@chat_date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
			sqlParams.Add(new MySqlParameter("@chat_channel", (int)chatChannel));
			sqlParams.Add(new MySqlParameter("@character_id", player.m_activeCharacter.m_character_id));
			sqlParams.Add(new MySqlParameter("@target_character_id", targetID));
			sqlParams.Add(new MySqlParameter("@message", chatmsg));

			m_worldDB.runCommandSyncWithParams("insert into chat_log (chat_date,chat_channel,character_id,target_character_id,message) values (@chat_date, @chat_channel, @character_id, @target_character_id, @message)", sqlParams.ToArray());
		}

        private Player findPlayerByCharName(string charName)
        {
            for (int i = 0; i < m_players.Count; i++)
            {
                if (m_players[i] != null && m_players[i].m_activeCharacter != null && m_players[i].m_activeCharacter.m_name.ToLower().Equals(charName.ToLower()))
                {
                    return m_players[i];
                }
            }
            return null;
        }

        private void processCommand(Player player, string receivedmsg)
        {
            int indexofFirstSpace = receivedmsg.IndexOf(' ');
            if (indexofFirstSpace < 2 || receivedmsg.Length < 2)
                return;
            string command = receivedmsg.Substring(0, indexofFirstSpace);
            receivedmsg = receivedmsg.Substring(indexofFirstSpace + 1).Trim();
            switch (command)
            {
                case "ban":
                    if (player.m_moderatorLevel >= MODERATOR_LEVEL.ADMINISTRATOR)
                    {
                        if (receivedmsg.Length == 0)
                        {
							string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.BAN_REQUIRES_NAME);
							sendSystemMessage(locText, player, true, SYSTEM_MESSAGE_TYPE.POPUP);
                            return;
                        }
                        int indexOfNextSpace = receivedmsg.IndexOf(' ');
                        if (indexOfNextSpace < 0)
                        {
							string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.BAN_REQUIRE_REASON);
							sendSystemMessage(locText, player, true, SYSTEM_MESSAGE_TYPE.POPUP);
                            return;
                        }

						// no need to check sql injection bacause already separated by spaces
                        string charactername = receivedmsg.Substring(0, indexOfNextSpace).Trim();
                        Player banPlayer = findPlayerByCharName(charactername);
                        long account_id = -1;
                        if (banPlayer != null)
                        {
                            account_id = banPlayer.m_account_id;
                        }
                        if (account_id < 0)
                        {
                            SqlQuery query = new SqlQuery(m_worldDB, "select account_id from character_details where name='" + charactername + "'");
                            if (query.Read())
                            {
                                account_id = query.GetInt32("account_id");
                            }
                            query.Close();
                        }
                        if (account_id == -1)
                        {
							string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.OTHER_NOT_FOUND);
							locText = string.Format(locText, charactername);
							sendSystemMessage(locText, player, true, SYSTEM_MESSAGE_TYPE.POPUP);
                            return;
                        }
                        receivedmsg = receivedmsg.Substring(indexOfNextSpace + 1);

                        m_universalHubDB.runCommandSync("update account_details set disabled=1 where account_id=" + account_id);
                        m_universalHubDB.runCommandSync("insert into account_journal (account_id,journal_date,user,details) values (" + account_id + ",'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + player.m_UserName + "',\"banned:" + receivedmsg + "\")");
                        if (banPlayer != null)
                        {
							string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.ACCOUNT_HAS_BEEN_BANNED);
							disconnect(banPlayer, true, locText);
						}
						string locTextBanned = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.OTHER_BANNED);
						locTextBanned = string.Format(locTextBanned, charactername);
						sendSystemMessage(locTextBanned, player, true, SYSTEM_MESSAGE_TYPE.NONE);
                    }
                    break;
                case "global":
                    if (player.m_moderatorLevel >= MODERATOR_LEVEL.ADMINISTRATOR)
                    {
                        int indexOfSecondSpace = receivedmsg.IndexOf(' ');
                        if (receivedmsg.Trim().Length == 0)
                        {
							string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.GLOBAL_REQUIRES_MESSAGE);
							sendSystemMessage(locText, player, true, SYSTEM_MESSAGE_TYPE.POPUP);

                            return;
                        }
                        receivedmsg = receivedmsg.Substring(indexOfSecondSpace + 1);
                        Program.sendSystemMessage(receivedmsg, false, false);
                    }
                    break;
                case "silence":
                    if (player.m_moderatorLevel >= MODERATOR_LEVEL.ADMINISTRATOR)
                    {
                        if (receivedmsg.Length == 0)
                        {
							string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.SILENCING_REQUIRES_NAME);
							sendSystemMessage(locText, player, true, SYSTEM_MESSAGE_TYPE.POPUP);
                            return;
                        }
                        int indexOfSecondSpace = receivedmsg.IndexOf(' ');
                        if (indexOfSecondSpace < 0)
                        {
							string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.SILENCING_REQUIRES_TIME);
							sendSystemMessage(locText, player, true, SYSTEM_MESSAGE_TYPE.POPUP);
                            return;
                        }
                        string charactername = receivedmsg.Substring(0, indexOfSecondSpace).Trim();
                        Player silencePlayer = findPlayerByCharName(charactername);
                        long account_id = -1;
                        if (silencePlayer != null)
                        {
                            account_id = silencePlayer.m_account_id;
                        }
                        if (account_id < 0)
                        {
                            SqlQuery query = new SqlQuery(m_worldDB, "select account_id from character_details where name='" + charactername + "'");
                            if (query.Read())
                            {
                                account_id = query.GetInt32("account_id");
                            }
                            query.Close();
                        }
                        if (account_id == -1)
                        {
							string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.OTHER_NOT_FOUND);
							locText = string.Format(locText, charactername);
							sendSystemMessage(locText, player, true, SYSTEM_MESSAGE_TYPE.POPUP);
                            return;
                        }
                        receivedmsg = receivedmsg.Substring(indexOfSecondSpace + 1).Trim();
                        int indexOfThirdSpace = receivedmsg.IndexOf(' ');
                        if (indexOfThirdSpace < 0)
                        {
							string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.SILENCING_REQUIRES_REASON);
							sendSystemMessage(locText, player, true, SYSTEM_MESSAGE_TYPE.POPUP);
                            return;
                        }
                        int period = 0;
                        Int32.TryParse(receivedmsg.Substring(0, indexOfThirdSpace), out period);
                        if (period == 0)
                        {
							string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.SILENCING_REQUIRES_TIME);
							sendSystemMessage(locText, player, true, SYSTEM_MESSAGE_TYPE.POPUP);
                            return;
                        }
                        receivedmsg = receivedmsg.Substring(indexOfThirdSpace + 1).Trim();

                        if (silencePlayer != null)
                        {
                            silencePlayer.m_silencedUntil = DateTime.Now.AddSeconds(period);
                        }
                        m_universalHubDB.runCommandSync("update account_details set silenced_until='" + DateTime.Now.AddSeconds(period).ToString("yyyy-MM-dd HH:mm:ss") + "' where account_id=" + account_id);
                        m_universalHubDB.runCommandSync("insert into account_journal (account_id,journal_date,user,details) values (" + account_id + ",'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + player.m_UserName + "',\"silenced:" + receivedmsg + "\")");
						string locTextSilienced = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.OTHER_SILENCED_PEROID);
						locTextSilienced = string.Format(locTextSilienced, charactername, period);
						sendSystemMessage(locTextSilienced, player, true, SYSTEM_MESSAGE_TYPE.NONE);
                    }
                    break;
                case "kick":
                    if (player.m_moderatorLevel >= MODERATOR_LEVEL.ADMINISTRATOR)
                    {
                        int indexOfNextSpace = receivedmsg.IndexOf(' ');
                        if (receivedmsg.Length == 0)
                        {
							string locText= Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.KICKING_REQUIRES_NAME);
							sendSystemMessage(locText, player, true, SYSTEM_MESSAGE_TYPE.POPUP);
                            return;
                        }
                        if (indexOfNextSpace < 0)
                        {
							string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.REPORTING_REQUIRES_REASON);
							sendSystemMessage(locText, player, true, SYSTEM_MESSAGE_TYPE.POPUP);
                            return;
                        }
                        string charactername = receivedmsg.Substring(0, indexOfNextSpace);
                        Player kickPlayer = findPlayerByCharName(charactername);
                        long account_id = -1;
                        if (kickPlayer != null)
                        {
                            account_id = kickPlayer.m_account_id;
                        }
                        if (account_id == -1)
                        {
							string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.OTHER_NOT_FOUND);
							locText = string.Format(locText, charactername);
							sendSystemMessage(locText, player, true, SYSTEM_MESSAGE_TYPE.POPUP);
                            return;
                        }
                        receivedmsg = receivedmsg.Substring(indexOfNextSpace + 1);

                        m_universalHubDB.runCommandSync("insert into account_journal (account_id,journal_date,user,details) values (" + account_id + ",'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + player.m_UserName + "',\"kicked:" + receivedmsg + "\")");
                        if (kickPlayer != null)
                        {

							string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.DISCONNECTED_BY_GM);
							disconnect(kickPlayer, true, locText);
						}
						string locTextKicked = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.OTHER_KICKED);
						locTextKicked = string.Format(locTextKicked, charactername);
						sendSystemMessage(locTextKicked, player, true, SYSTEM_MESSAGE_TYPE.NONE);
                    }
                    break;
                case "addgold":
                    if (player.m_moderatorLevel >= MODERATOR_LEVEL.ADMINISTRATOR)
                    {
                        int indexOfSecondSpace = receivedmsg.IndexOf(' ');
                        if (receivedmsg.Length == 0)
                        {
							string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.ADDGOLD_REQUIRES_NAME);
							sendSystemMessage(locText, player, true, SYSTEM_MESSAGE_TYPE.POPUP);
                            return;
                        }
                        if (indexOfSecondSpace < 0)
                        {
							string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.ADDGOLD_REQUIRES_AMOUNT);
							sendSystemMessage(locText, player, true, SYSTEM_MESSAGE_TYPE.POPUP);
                            return;
                        }
                        string charactername = receivedmsg.Substring(0, indexOfSecondSpace);
                        Player addGoldPlayer = findPlayerByCharName(charactername);
                        long account_id = -1;
                        long character_id = -1;
                        if (addGoldPlayer != null)
                        {
                            account_id = addGoldPlayer.m_account_id;
                        }
                        if (account_id < 0)
                        {
                            SqlQuery query = new SqlQuery(m_worldDB, "select character_id,account_id from character_details where name='" + charactername + "'");
                            if (query.Read())
                            {
                                account_id = query.GetInt32("account_id");
                                character_id = query.GetInt32("character_id");
                            }
                            query.Close();
                        }
                        if (account_id == -1)
                        {
							string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.OTHER_NOT_FOUND);
							locText = string.Format(locText, charactername);
							sendSystemMessage(locText, player, true, SYSTEM_MESSAGE_TYPE.POPUP);
                            return;
                        }
                        receivedmsg = receivedmsg.Substring(indexOfSecondSpace + 1).Trim();
                        int indexOfThirdSpace = receivedmsg.IndexOf(' ');
                        if (indexOfThirdSpace < 0)
                        {
							string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.ADDGOLD_REQUIRES_AMOUNT);
							sendSystemMessage(locText, player, true, SYSTEM_MESSAGE_TYPE.POPUP);
                            return;
                        }
                        if (receivedmsg.Trim().Length == 0)
                        {
							string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.ADDGOLD_REQUIRES_REASON);
							sendSystemMessage(locText, player, true, SYSTEM_MESSAGE_TYPE.POPUP);
                            return;
                        }
                        int amount = 0;
                        Int32.TryParse(receivedmsg.Substring(0, indexOfThirdSpace), out amount);
                        if (amount == 0)
                        {
							string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.ADDGOLD_REQUIRES_AMOUNT);
							sendSystemMessage(locText, player, true, SYSTEM_MESSAGE_TYPE.POPUP);
							return;
                        }
                        receivedmsg = receivedmsg.Substring(indexOfThirdSpace + 1);

                        if (addGoldPlayer != null)
                        {
                            addGoldPlayer.m_activeCharacter.updateCoins(amount);
							string locText = Localiser.GetString(textDB, addGoldPlayer, (int)CommandProcessorTextDB.TextID.PLAYER_GAINED_GOLD);
							locText= string.Format(locText, amount);
							Program.processor.sendSystemMessage(locText, addGoldPlayer, true, SYSTEM_MESSAGE_TYPE.POPUP);
                            addGoldPlayer.m_activeCharacter.m_inventory.SendUseItemReply(String.Empty, 0.0f);
                        }
                        else
                        {
                            m_worldDB.runCommandSync("update character_details set coins=coins+" + amount + " where character_id=" + character_id);
                        }
                        m_universalHubDB.runCommandSync("insert into account_journal (account_id,journal_date,user,details) values (" + account_id + ",'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + player.m_UserName + "',\"addgold " + amount + ":" + receivedmsg + "\")");

						string locTextGold = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.OTHER_GIVEN_GOLD);
						locTextGold = string.Format(locTextGold, charactername, amount);
						sendSystemMessage(locTextGold, player, true, SYSTEM_MESSAGE_TYPE.NONE);
					}
                    break;
                case "addplatinum":
                    if (player.m_moderatorLevel >= MODERATOR_LEVEL.ADMINISTRATOR)
                    {
                        int indexOfSecondSpace = receivedmsg.IndexOf(' ');
                        if (receivedmsg.Length == 0)
                        {
							string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.ADDPLATINUM_REQUIRES_NAME);
							sendSystemMessage(locText, player, true, SYSTEM_MESSAGE_TYPE.POPUP);
							return;
                        }
                        
                        if (receivedmsg.Trim().Length == 0)
                        {
							string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.ADDPLATINUM_REQUIRES_AMOUNT);
							sendSystemMessage(locText, player, true, SYSTEM_MESSAGE_TYPE.POPUP);
                            return;
                        }
                        string charactername = receivedmsg.Substring(0, indexOfSecondSpace);
                        Player addPlatPlayer = findPlayerByCharName(charactername);
                        long account_id = -1;
                        if (addPlatPlayer != null)
                        {
                            account_id = addPlatPlayer.m_account_id;
                        }
                        if (account_id < 0)
                        {
                            SqlQuery query = new SqlQuery(m_worldDB, "select account_id from character_details where name='" + charactername + "'");
                            if (query.Read())
                            {
                                account_id = query.GetInt32("account_id");
                            }
                            query.Close();
                        }
                        if (account_id == -1)
                        {
							string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.OTHER_NOT_FOUND);
							locText = string.Format(locText, charactername);
							sendSystemMessage(locText, player, true, SYSTEM_MESSAGE_TYPE.POPUP);
                            return;
                        }
                        receivedmsg = receivedmsg.Substring(indexOfSecondSpace + 1).Trim();
                        int indexOfThirdSpace = receivedmsg.IndexOf(' ');
                        if (indexOfThirdSpace < 0)
                        {
							string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.ADDPLATINUM_REQUIRES_REASON);
							sendSystemMessage(locText, player, true, SYSTEM_MESSAGE_TYPE.POPUP);
                            return;
                        }
                        int amount = 0;
                        Int32.TryParse(receivedmsg.Substring(0, indexOfThirdSpace), out amount);
                        if (amount == 0)
						{
							string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.ADDPLATINUM_REQUIRES_AMOUNT);
							sendSystemMessage(locText, player, true, SYSTEM_MESSAGE_TYPE.POPUP);
							return;
                        }
                        receivedmsg = receivedmsg.Substring(indexOfThirdSpace + 1);
                        if (addPlatPlayer != null)
                        {
                            addPlatPlayer.m_platinum += amount;
                            addPlatPlayer.SavePlatinum(0, 0);
                            PremiumShop.SendPlatinumConfirmation(addPlatPlayer, 1, String.Empty, String.Empty);
							string locText = Localiser.GetString(textDB, addPlatPlayer, (int)CommandProcessorTextDB.TextID.PLAYER_GAINED_PLATINUM);
							locText = string.Format(locText, amount);
							Program.processor.sendSystemMessage(locText, addPlatPlayer, true, SYSTEM_MESSAGE_TYPE.POPUP);
						}
                        else
                        {
                            m_universalHubDB.runCommandSync("update account_details set platinum=platinum+" + amount + " where account_id=" + account_id);
                        }
                        m_universalHubDB.runCommandSync("insert into account_journal (account_id,journal_date,user,details) values (" + account_id + ",'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + player.m_UserName + "',\"addplat " + amount + ":" + receivedmsg + "\")");

						string locTextPlat = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.OTHER_GIVEN_PLATINUM);
						locTextPlat = string.Format(locTextPlat, charactername, amount);
						sendSystemMessage(locTextPlat, player, true, SYSTEM_MESSAGE_TYPE.NONE);
                    }
                    break;
                case "report":
                    if (player.m_moderatorLevel >= MODERATOR_LEVEL.GM_PLAYER)
                    {
                        int indexOfNextSpace = receivedmsg.IndexOf(' ');
                        if (receivedmsg.Length == 0)
                        {
							string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.REPORTING_REQUIRES_NAME);
							sendSystemMessage(locText, player, true, SYSTEM_MESSAGE_TYPE.POPUP);
                            return;
                        }
                        if (indexOfNextSpace < 0)
                        {
							string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.REPORTING_REQUIRES_REASON);
							sendSystemMessage(locText, player, true, SYSTEM_MESSAGE_TYPE.POPUP);
							return;
                        }
                        string charactername = receivedmsg.Substring(0, indexOfNextSpace);
                        Player reportPlayer = findPlayerByCharName(charactername);
                        long account_id = -1;
                        if (reportPlayer != null)
                        {
                            account_id = reportPlayer.m_account_id;
                        }
                        if (account_id < 0)
                        {
                            SqlQuery query = new SqlQuery(m_worldDB, "select account_id from character_details where name='" + charactername + "'");
                            if (query.Read())
                            {
                                account_id = query.GetInt32("account_id");
                            }
                            query.Close();
                        }
                        if (account_id == -1)
                        {
							string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.OTHER_NOT_FOUND);
							locText = string.Format(locText, charactername);
							sendSystemMessage(locText, player, true, SYSTEM_MESSAGE_TYPE.POPUP);
                            return;
                        }
                        receivedmsg = receivedmsg.Substring(indexOfNextSpace + 1);
                        m_universalHubDB.runCommandSync("insert into account_journal (account_id,journal_date,user,details) values (" + account_id + ",'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + player.m_UserName + "',\"reported:" + receivedmsg + "\"");

						string locTextReport = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.OTHER_REPORTED);
						locTextReport = string.Format(locTextReport, charactername);
						sendSystemMessage(locTextReport, player, true, SYSTEM_MESSAGE_TYPE.NONE);
                    }
                    break;
            }
        }

        void sendTradeChatMessage(string chatmsg, Player player)
        {
            Character playerCharacter = player.m_activeCharacter;
            if (playerCharacter != null)
            {
                Player otherPlayer = playerCharacter.m_tradingWith;
                if (otherPlayer != null)
                {
                    NetOutgoingMessage outmsg = m_server.CreateMessage();
                    outmsg.WriteVariableUInt32((uint)NetworkCommandType.GeneralChat);
                    outmsg.WriteVariableInt32((int)HW_CHAT_BOX_CHANNEL.HW_CHAT_BOX_CHANNEL_TRADE);
                    outmsg.Write(player.m_activeCharacter.m_name);
                    outmsg.Write(chatmsg);
                    outmsg.WriteVariableInt32((int)player.m_activeCharacter.m_character_id);
                    Program.Display("got trade chat message from " + player.m_activeCharacter.m_name + " : " + chatmsg);

                    List<NetConnection> connections = new List<NetConnection>();
                    connections.Add(player.connection);
                    connections.Add(otherPlayer.connection);
                    SendMessage(outmsg, connections, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Chat, NetworkCommandType.GeneralChat);
                }
            }
        }

        internal void SendCloseXMLPopup(Player player, int popupID)
        {
            NetOutgoingMessage outmsg = m_server.CreateMessage();

            outmsg.WriteVariableUInt32((uint)NetworkCommandType.InGamePopup);
            outmsg.WriteVariableInt32((int)PopupMessageType.ClosePopup);
            outmsg.WriteVariableInt32((int)popupID);
            SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.InGamePopup);
        }

        internal XML_Popup SendXMLPopupMessage(bool inBackground, Player player, int popupID, XML_Popup.Popup_Type popupType, string filename, List<string> strVariables, bool forceOpen)
        {
            XML_Popup newPopup = new XML_Popup(popupID, popupType);
            if (inBackground == true)
            {
                lock (player.m_newPopups)
                {
                    player.m_newPopups.Add(newPopup);
                }
            }
            else
            {
                player.m_openPopups.Add(newPopup);
            }

            NetOutgoingMessage outmsg = m_server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.InGamePopup);
            outmsg.WriteVariableInt32((int)PopupMessageType.OpenPopup);

            outmsg.WriteVariableInt32(popupID);
            if (forceOpen == true)
            {
                outmsg.Write((byte)1);
            }
            else
            {
                outmsg.Write((byte)0);
            }
            //it's a template file popup
            outmsg.WriteVariableInt32(1);

            outmsg.Write(filename);
            if (strVariables != null)
            {
                outmsg.WriteVariableInt32(strVariables.Count);
                for (int i = 0; i < strVariables.Count; i++)
                {
                    outmsg.Write(strVariables[i]);
                }
            }
            else
            {
                outmsg.WriteVariableInt32(0);
            }
            if (inBackground == true)
            {
                DelayedMessageDescriptor desc = new DelayedMessageDescriptor(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.InGamePopup, null);
                lock (CommandProcessor.m_delayedMessages)
                {
                    CommandProcessor.m_delayedMessages.Enqueue(desc);
                }
            }
            else
            {
                SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.InGamePopup);
            }
            return newPopup;
        }
        
        void ProcessPopupMessage(NetIncomingMessage msg, Player player)
        {
            PopupMessageType messageType = (PopupMessageType)msg.ReadVariableInt32();
            switch (messageType)
            {
                case PopupMessageType.OptionSelected:
                    {
                        ProcessPopupOptionSelectedMessage(player, msg);
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }

        void ProcessPopupOptionSelectedMessage(Player player, NetIncomingMessage msg)
        {
            int popupID = msg.ReadVariableInt32();
            int optionID = msg.ReadVariableInt32();
            XML_Popup.OptionChosen(player, popupID, optionID);
        }

        void ProcessOpenOfferWall(NetIncomingMessage msg, Player player)
        {
            //SendXMLPopupMessage(false, player, (int)XML_Popup.Set_Popup_IDs.SPI_OfferWall, XML_Popup.Popup_Type.OfferWall, "offer_popup.txt", true);
            //SendXMLPopupMessage(false, player, (int)XML_Popup.Set_Popup_IDs.SPI_OfferWall, XML_Popup.Popup_Type.OfferWall, "offer_popup.txt", null, false);
            OfferMessagetype currentType = (OfferMessagetype)msg.ReadVariableInt32();

            Program.Display("ProcessOpenOfferWall " + currentType);

            switch (currentType)
            {
                case OfferMessagetype.OMT_OpenFreePlatOffers:
                    {
                        ProcessOpenFreePlatinumWall(player);
                        break;
                    }
                case OfferMessagetype.OMT_OpenSpecialOffers:
                    {
                        ProcessOpenSpecialOffers(player);
                        break;
                    }
                case OfferMessagetype.OMT_OpenW3iOffers:
                    {
                        ProcessOpenW3iOffers(player);
                        break;
                    }
                case OfferMessagetype.OMT_RedeemW3iOffers:
                    {
                        break;
                    }
                case OfferMessagetype.OMT_OpenSuperSonicOffers:
                    {
                        ProcessOpenSuperSonicOffers(player);
                        break;
                    }
                case OfferMessagetype.OMT_OpenFyberOffers_Offerwall:                
                case OfferMessagetype.OMT_OpenFyberOffers_Video:
                    {
                        ProcessOpenFyberOffers(player, currentType);
                        break;
                    }
            }
        }

        internal void ProcessOpenFreePlatinumWall(Player player)
        {
            if (Program.m_offerPopupActive == true)
            {
                SendXMLPopupMessage(false, player, (int)XML_Popup.Set_Popup_IDs.SPI_OfferWall, XML_Popup.Popup_Type.OfferWall, "offer_popup.txt", null, false);
            }
            else
            {
				string modString = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.SPECIAL_OFFERS_NOT_AVAILABLE);
				Program.processor.SendXMLPopupMessage(true, player, (int)XML_Popup.Set_Popup_IDs.SPI_BasicPopup, XML_Popup.Popup_Type.None, "popup_template.txt", new List<string> { modString, " modal = \"true\" active = \"connected\"", String.Empty, String.Empty }, false);
            }
        }

        void ProcessOpenSpecialOffers(Player player)
        {
            // SendXMLPopupMessage(false, player, (int)XML_Popup.Set_Popup_IDs.SPI_SpecialOffers, XML_Popup.Popup_Type.SpecialOffers, "offer_popup.txt", null, false);
            if (player.m_activeCharacter.OfferManager != null)
            player.m_activeCharacter.OfferManager.PrepareToSendSpecialOfferWall(player.m_activeCharacter);
        }

        private void ProcessSendSpecialOffersNumber(Player player)
        {
            if (player.m_activeCharacter.OfferManager != null)
            player.m_activeCharacter.OfferManager.PrepareToSendSpecialOfferWallNumber(player.m_activeCharacter);
            
        }

        void ProcessOpenW3iOffers(Player player)
        {
            if (Program.m_w3iActive >= 2)
            {
                double currentTimeinSecs = Program.SecondsFromReferenceDate();
                //if the time hasn't gone back an hour +
                //it's has been longer than the resend time
                if ((player.m_timeOfLastOfferWall > currentTimeinSecs) ||
                    (player.m_timeOfLastOfferWall + XML_Popup.TIME_BETWEEN_OFFER_SENDS) < currentTimeinSecs)
                {
                    Program.processor.SendOpenW3iOfferWall(player);
                    player.m_timeOfLastOfferWall = currentTimeinSecs;
                }
            }
            else
            {
				string modString = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.NATIVEX_NOT_AVAILABLE);
				Program.processor.SendXMLPopupMessage(true, player, (int)XML_Popup.Set_Popup_IDs.SPI_BasicPopup,
                    XML_Popup.Popup_Type.None, "popup_template.txt",
                    new List<string> { modString, " modal = \"true\" active = \"connected\"", String.Empty, String.Empty }, false);
            }
        }

        internal void SendOpenW3iOfferWall(Player player)
        {
            NetOutgoingMessage outmsg = m_server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.OpenOfferWall);
            outmsg.WriteVariableInt32((int)OfferMessagetype.OMT_OpenW3iOffers);
            SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.OpenOfferWall);
        }

        void ProcessOpenSuperSonicOffers(Player player)
        {
            if (Program.m_superSonicActive >= 2)
            {
                double currentTimeinSecs = Program.SecondsFromReferenceDate();
                //if the time hasn't gone back an hour +
                //it's has been longer than the resend time
                if ((player.m_timeOfLastOfferWall > currentTimeinSecs) ||
                    (player.m_timeOfLastOfferWall + XML_Popup.TIME_BETWEEN_OFFER_SENDS) < currentTimeinSecs)
                {
                    Program.processor.SendOpenSuperSonicOfferWall(player);
                    player.m_timeOfLastOfferWall = currentTimeinSecs;
                }
            }
            else
            {
				string modString = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.SUPERSONIC_NOT_AVAILABLE);
				Program.processor.SendXMLPopupMessage(true, player, (int)XML_Popup.Set_Popup_IDs.SPI_BasicPopup,
                    XML_Popup.Popup_Type.None, "popup_template.txt",
                    new List<string> { modString, " modal = \"true\" active = \"connected\"", String.Empty, String.Empty }, false);
            }
        }

        void ProcessOpenFyberOffers(Player player, OfferMessagetype in_messageType)
        {
            Program.Display("ProcessOpenFyberOffers " + in_messageType.ToString() + ", fyber active " + Program.m_fyberActive);

            if (Program.m_fyberActive >= 2)
            {
                double currentTimeinSecs = Program.SecondsFromReferenceDate();
                //if the time hasn't gone back an hour +
                //it's has been longer than the resend time
                if ((player.m_timeOfLastOfferWall > currentTimeinSecs) ||
                    (player.m_timeOfLastOfferWall + XML_Popup.TIME_BETWEEN_OFFER_SENDS) < currentTimeinSecs)
                {
                    Program.processor.SendOpenFyberOffer(player, in_messageType);

                    player.m_timeOfLastOfferWall = currentTimeinSecs;
                }
                else
                {
                    Program.Display("ProcessOpenFyberOffers attempted to make fyber offer too soon");
                }
            }
            else
            {
				string modString = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.FYBER_NOT_AVAILABLE);
				Program.processor.SendXMLPopupMessage(true, player, (int)XML_Popup.Set_Popup_IDs.SPI_BasicPopup,
                    XML_Popup.Popup_Type.None, "popup_template.txt",
                    new List<string> { modString, " modal = \"true\" active = \"connected\"", String.Empty, String.Empty }, false);

                Program.Display("Fyber Ads are not currently available");
            }
        }

        internal void SendOpenSuperSonicOfferWall(Player player)
        {
            NetOutgoingMessage outmsg = m_server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.OpenOfferWall);
            outmsg.WriteVariableInt32((int)OfferMessagetype.OMT_OpenSuperSonicOffers);
            outmsg.WriteVariableInt64((int)player.m_account_id);
            SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.OpenOfferWall);
        }

        internal void SendOpenFyberOffer(Player player, OfferMessagetype in_messageType)
        {
            NetOutgoingMessage outmsg = m_server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.OpenOfferWall);
            outmsg.WriteVariableInt32((int)in_messageType);
            outmsg.WriteVariableInt64((int)player.m_account_id);
            SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.OpenOfferWall);
        }
        

        /// <summary>
        /// Sends players a system message for error reporting
        /// </summary>
        /// <param name="chatmsg"></param>
        /// <param name="connections"></param>
        internal void sendSystemMessage(string chatmsg, IEnumerable<NetConnection> connections, bool important, SYSTEM_MESSAGE_TYPE type)
        {
            NetOutgoingMessage outmsg = m_server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.GeneralChat);
            outmsg.WriteVariableInt32((int)HW_CHAT_BOX_CHANNEL.HW_CHAT_BOX_CHANNEL_SYSTEM); 
            
            string typeString = Convert.ToString((int)type);
            outmsg.Write(typeString);
            outmsg.Write(chatmsg);
            outmsg.WriteVariableInt32(-1);
            if (shouldDisplay(type, important))
            {
                Program.Display("sending System message: " + chatmsg);
            }
            SendMessage(outmsg, connections.ToList<NetConnection>(), NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Chat, NetworkCommandType.GeneralChat);
        }

        /// <summary>
        /// Sends players a battle message
        /// </summary>
        /// <param name="chatmsg"></param>
        /// <param name="connections"></param>
        internal void SendBattleMessage(string chatmsg, IEnumerable<NetConnection> connections, bool important, SYSTEM_MESSAGE_TYPE type)
        {
            NetOutgoingMessage outmsg = m_server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.GeneralChat);
            outmsg.WriteVariableInt32((int)HW_CHAT_BOX_CHANNEL.HW_CHAT_BOX_CHANNEL_BATTLE);

            string typeString = Convert.ToString((int)type);
            outmsg.Write(typeString);
            outmsg.Write(chatmsg);
            outmsg.WriteVariableInt32(-1);
            if (shouldDisplay(type, important))
            {
                Program.Display("sending battle message: " + chatmsg);
            }
            SendMessage(outmsg, connections.ToList<NetConnection>(), NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Chat, NetworkCommandType.GeneralChat);
        }
        // SendAbilityMessage                          //
        // Sends a message using the Abilities channel //
		internal void SendAbilityMessage(string msg, NetConnection connection)
		{
			NetOutgoingMessage outmsg = m_server.CreateMessage();
			outmsg.WriteVariableUInt32((uint)NetworkCommandType.GeneralChat);
			outmsg.WriteVariableInt32((int)HW_CHAT_BOX_CHANNEL.HW_CHAT_BOX_CHANNEL_ABILITIES);
			outmsg.Write("-1");
			outmsg.Write(msg);
			outmsg.WriteVariableInt32(-1);
			SendMessage(outmsg, connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Chat, NetworkCommandType.GeneralChat);
		}
        internal void SendAbilityMessage(string msg, IEnumerable<NetConnection> connections)
        {
            NetOutgoingMessage outmsg = m_server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.GeneralChat);
            outmsg.WriteVariableInt32((int)HW_CHAT_BOX_CHANNEL.HW_CHAT_BOX_CHANNEL_ABILITIES);
            outmsg.Write("-1");
            outmsg.Write(msg);
            outmsg.WriteVariableInt32(-1);
            SendMessage(outmsg, connections.ToList<NetConnection>(), NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Chat, NetworkCommandType.GeneralChat);
        }

        internal bool shouldDisplay(SYSTEM_MESSAGE_TYPE type, bool important)
        {
            if (important)
                return true;
            switch (type)
            {
                case SYSTEM_MESSAGE_TYPE.BATTLE:
                    return Program.m_LogSysBattle;
                case SYSTEM_MESSAGE_TYPE.BLOCK:
                    return Program.m_LogSysBlock;
                case SYSTEM_MESSAGE_TYPE.CLAN:
                    return Program.m_LogSysClan;
                case SYSTEM_MESSAGE_TYPE.FRIENDS:
                    return Program.m_LogSysFriends;
                case SYSTEM_MESSAGE_TYPE.PARTY:
                    return Program.m_LogSysParty;
                case SYSTEM_MESSAGE_TYPE.SKILLS:
                    return Program.m_LogSysSkills;
                default:
                    return true;
            }
        }

        /// <summary>
        /// Sends players a system message for error reporting to a single connection
        /// </summary>
        /// <param name="chatmsg"></param>
        /// <param name="connections"></param>
        internal void sendSystemMessage(string chatmsg, Player player, bool important, SYSTEM_MESSAGE_TYPE type)
        {
            if (player != null && player.connection != null)
            {
                sendSystemMessage(chatmsg, player, important, type, false);
            }
        }

        internal void sendSystemMessage(string chatmsg, Player player, bool important, SYSTEM_MESSAGE_TYPE type, bool delayed)
        {
            if (player != null && player.connection != null)
            {
                NetOutgoingMessage outmsg = m_server.CreateMessage();
                outmsg.WriteVariableUInt32((uint)NetworkCommandType.GeneralChat);
                outmsg.WriteVariableInt32((int)HW_CHAT_BOX_CHANNEL.HW_CHAT_BOX_CHANNEL_SYSTEM);
                string typeString = Convert.ToString((int)type);
                outmsg.Write(typeString);
                outmsg.Write(chatmsg);
                outmsg.WriteVariableInt32(-1);
                string target;

                if (player.m_activeCharacter != null)
                {
                    target = " to character " + player.m_activeCharacter.Name;
                }
                else
                {
                    target = " to player " + player.m_UserName;
                }
                if (shouldDisplay(type, important))
                {
                    Program.Display("sending System message" + target + ": " + chatmsg);
                }
                if (delayed)
                {
                    DelayedMessageDescriptor desc = new DelayedMessageDescriptor(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Chat, NetworkCommandType.GeneralChat, null);
                    lock (m_delayedMessages)
                    {
                        m_delayedMessages.Enqueue(desc);
                    }
                }
                else
                {
                    SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Chat, NetworkCommandType.GeneralChat);
                }
            }
        }

        internal void SendGameDataMessage(Player player)
        {
            int maxChatLenth = 150;
            NetOutgoingMessage outmsg = m_server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.GameDataMessage);
            outmsg.WriteVariableInt32(maxChatLenth);
            outmsg.Write(Inventory.ATTACK_TIME_ADD);
            //SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.GameDataMessage);
            DelayedMessageDescriptor desc = new DelayedMessageDescriptor(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.GameDataMessage, null);
            lock (CommandProcessor.m_delayedMessages)
            {
                CommandProcessor.m_delayedMessages.Enqueue(desc);
            }
        }

        internal void SendPlaySound2D(Player player, string filename)
        {
            NetOutgoingMessage outmsg = m_server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.PlaySound);
            outmsg.Write(filename);
            //not 3d
            outmsg.Write((byte)0);
            SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.PlaySound);
        }
        
        internal void SendShowPlayerHelp(Player player, int zoneID, Vector3 position)
        {
            NetOutgoingMessage outmsg = m_server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.ShowPlayerHelp);
            outmsg.Write((byte)1);
            outmsg.WriteVariableInt32((int)Character.Player_Help_Type.Position);
            outmsg.WriteVariableInt32(zoneID);
            outmsg.Write((float)position.X);
            outmsg.Write((float)position.Y);
            outmsg.Write((float)position.Z);
            SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.ShowPlayerHelp);
        }

        internal void SendShowPlayerHelpUsingID(Player player, int zoneID, int mobID, Character.Player_Help_Type helpType)
        {
            NetOutgoingMessage outmsg = m_server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.ShowPlayerHelp);
            outmsg.Write((byte)1);
            outmsg.WriteVariableInt32((int)helpType);
            outmsg.WriteVariableInt32(zoneID);
            outmsg.WriteVariableInt32(mobID);
            SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.ShowPlayerHelp);

        }

        internal void SendHidePlayerHelp(Player player)
        {
            NetOutgoingMessage outmsg = m_server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.ShowPlayerHelp);
            outmsg.Write((byte)0);
            SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.ShowPlayerHelp);
        }

        internal void StartTutorialMessage(Player player, int tutorialID, bool inBackground)
        {
            NetOutgoingMessage outmsg = m_server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.Tutorial);
            outmsg.WriteVariableInt32((int)Character.Tutorial_Message_type.StartTutorial);
            outmsg.WriteVariableInt32(tutorialID);
            if (inBackground)
            {
                DelayedMessageDescriptor msgDesc = new DelayedMessageDescriptor(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.Tutorial, player);

                lock (CommandProcessor.m_delayedMessages)
                {
                    CommandProcessor.m_delayedMessages.Enqueue(msgDesc);
                }
            }
            else
            {
                SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.Tutorial);
            }
        }

        internal void SendFirstTimeIDMessage(Player player, int firstTimeID, bool inBackground)
        {
            NetOutgoingMessage outmsg = m_server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.FirstTime);
            outmsg.WriteVariableInt32(firstTimeID);
            if (inBackground)
            {
                DelayedMessageDescriptor msgDesc = new DelayedMessageDescriptor(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.FirstTime, player);

                lock (CommandProcessor.m_delayedMessages)
                {
                    CommandProcessor.m_delayedMessages.Enqueue(msgDesc);
                }
            }
            else
            {
                SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.FirstTime);
            }
        }
        
        internal void ProcessTutorialMassage(NetIncomingMessage msg, Player player)
        {
            Character.Tutorial_Message_type messageType = (Character.Tutorial_Message_type)msg.ReadVariableInt32();

            switch (messageType)
            {
                case Character.Tutorial_Message_type.TutorialComplete:
                    {
                        int tutorialID = msg.ReadVariableInt32();
                        if (player.m_activeCharacter != null)
                        {
                            player.m_activeCharacter.SaveTutorialCompleted(tutorialID);
                        }
                        break;
                    }
            }
        }

        internal void ProcessFirstTimeMassage(NetIncomingMessage msg, Player player)
        {

               int firstTimeID = msg.ReadVariableInt32();
               if (player.m_activeCharacter != null)
               {
                            player.m_activeCharacter.SaveFirstTimeCompleted(firstTimeID);
               }
        }
        
        void sendGlobalChatMessage(string chatmsg, Player player)
        {
            NetOutgoingMessage outmsg = m_server.CreateMessage();
            List<Player> currentPlayers = new List<Player>(m_players);
            int sendersID = (int)player.m_activeCharacter.m_character_id;
            for (int i = currentPlayers.Count - 1; i >= 0; i--)
            {
                if ((currentPlayers[i].m_activeCharacter == null) && (currentPlayers[i].m_activeCharacter.HasBlockedCharacter(sendersID) == true))
                {
                    currentPlayers.RemoveAt(i);
                }
            }

            outmsg.WriteVariableUInt32((uint)NetworkCommandType.GeneralChat);
            outmsg.WriteVariableInt32((int)HW_CHAT_BOX_CHANNEL.HW_CHAT_BOX_CHANNEL_GLOBAL);
            outmsg.Write(player.m_activeCharacter.m_name);
            outmsg.Write(chatmsg);
            outmsg.WriteVariableInt32((int)player.m_activeCharacter.m_character_id);
            Program.Display("got global chat message from " + player.m_activeCharacter.m_name + " : " + chatmsg);
            SendMessage(outmsg, m_server.Connections, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Chat, NetworkCommandType.GeneralChat);
        }

        void sendWhisperTo(Player recievingPlayer, string chatMessage, Player player, string whisperTargetStr)
        {
            if (recievingPlayer == null)
            {
				string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.OTHER_OFFLINE);
				locText = string.Format(locText, whisperTargetStr);
				sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.NONE);
                return;
            }
            if (recievingPlayer.m_activeCharacter.HasBlockedCharacter((int)player.m_activeCharacter.m_character_id) == false)
            {
                NetOutgoingMessage outmsg = m_server.CreateMessage();
                outmsg.WriteVariableUInt32((uint)NetworkCommandType.GeneralChat);
                outmsg.WriteVariableInt32((int)HW_CHAT_BOX_CHANNEL.HW_CHAT_BOX_CHANNEL_WHISPER_INCOMING);
                outmsg.Write(player.m_activeCharacter.m_name);
                outmsg.Write(chatMessage);
                outmsg.WriteVariableInt32((int)player.m_activeCharacter.m_character_id);
                Program.Display("got whisper message from " + player.m_activeCharacter.m_name + " to " + recievingPlayer.m_activeCharacter.Name + " : " + chatMessage);
                SendMessage(outmsg, recievingPlayer.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Chat, NetworkCommandType.GeneralChat);
                if (Program.m_LogAnalytics)
                {
                    AnalyticsMain logAnalytics = new AnalyticsMain(false);
                    logAnalytics.LogMessageReceived(recievingPlayer, player.m_account_id.ToString(), "WHISPER", "-1");
                }
            }

            NetOutgoingMessage confirmMsg = m_server.CreateMessage();
            confirmMsg.WriteVariableUInt32((uint)NetworkCommandType.GeneralChat);
            confirmMsg.WriteVariableInt32((int)HW_CHAT_BOX_CHANNEL.HW_CHAT_BOX_CHANNEL_WHISPER_OUTGOING);
            confirmMsg.Write(recievingPlayer.m_activeCharacter.m_name);
            confirmMsg.Write(chatMessage);
            confirmMsg.WriteVariableInt32((int)player.m_activeCharacter.m_character_id);

            SendMessage(confirmMsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Chat, NetworkCommandType.GeneralChat);
            if (Program.m_LogAnalytics)
            {
                AnalyticsMain logAnalytics = new AnalyticsMain(false);
                logAnalytics.LogMessageSent(player, recievingPlayer.m_account_id.ToString(), "WHISPER", "-1");
            }
        }

        #endregion

        private void processClientErrorMessage(NetIncomingMessage msg, Player player)
        {
            string errorString = msg.ReadString();

            long playerID = -1;
            long characterID = -1;

            playerID = player.m_account_id;
            if (player.m_activeCharacter != null)
            {
                characterID = player.m_activeCharacter.m_character_id;
            }

            Program.Display("*** Received error from user " + playerID + " with character " + characterID + "error = " + errorString + "***");
        }

        private void processInactivityMessage(NetIncomingMessage msg, Player player)
        {
            //allow for 5min inactivity
            double maxInactivityTimeAllowed = Program.m_inactivity_timeout;
            int numberOfSeconds = msg.ReadVariableInt32();

            if (maxInactivityTimeAllowed > -1)
            {
                if (numberOfSeconds >= maxInactivityTimeAllowed)
                {
                    if (Program.m_LogInactivityUpdates)
                    {
                        Program.Display("received Inactivity Message time in seconds = " + numberOfSeconds + " exceded maxInactivityTimeAllowed, disconnecting player " + player.m_account_id);
                    }
					string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.PLAYER_DISCONNECTED_INACTIVITY);
					disconnect(player, true, locText);
					return;
                }
                else if (numberOfSeconds >= (maxInactivityTimeAllowed - 60))
                {
                    if (player.m_activeCharacter != null)
                    {
						string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.PLAYER_INACTIVE);
						sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.NONE);
                    }
                }
                if (Program.m_LogInactivityUpdates)
                {
                    Program.Display("received Inactivity Message time in seconds = " + numberOfSeconds + " from player" + player.m_account_id);
                }
            }
        }

        private void ProcessClientChecksumError(NetIncomingMessage msg, Player player)
        {
            ChecksumFailureTypes checkFailed = (ChecksumFailureTypes)msg.ReadVariableInt32();
            if (Program.m_kickChecksumFailures == 2)
            {
				string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.ISSUE_WITH_GAME_FILES);
				disconnect(player, true, locText);
			}
            else if (Program.m_kickChecksumFailures == 1)
            {
				string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.DATA_ERROR);
				sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.POPUP);
            }
            else
            {
                Program.Display("Received ClientChecksumError from : " + player.GetIDString());
            }
        }

        private void processLearnAbility(NetIncomingMessage msg, Player player)
        {
            int ability_id = msg.ReadVariableInt32();
            int coins = msg.ReadVariableInt32();
            NetOutgoingMessage outmsg = m_server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.LearnAbilityReply);
            string errorString = player.m_activeCharacter.LearnAbility(ability_id, coins);
            if (!String.IsNullOrEmpty(errorString))
            {
                outmsg.Write((byte)0);
				string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.FAILED_TO_LEARN_ABILITY);
				locText = String.Format(locText, errorString);
				outmsg.Write(locText);
			}
            else
            {
                outmsg.Write((byte)1);
                outmsg.WriteVariableInt32(ability_id);
                outmsg.WriteVariableInt32(player.m_activeCharacter.m_inventory.m_coins);
                player.m_activeCharacter.m_QuestManager.writeAvailableQuestsToMessage(outmsg);
            }
            Program.processor.SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.LearnAbilityReply);
        }

        private void processPickupItem(NetIncomingMessage msg, Player player)
        {
            int spawnid = msg.ReadVariableInt32();
            Zone zone = player.m_activeCharacter.m_zone;
            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.PickupItemReply);
            zone.pickupItem(spawnid, player.m_activeCharacter, outmsg);
            Program.processor.SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.PickupItemReply);
        }

        private void processAttributesUpdate(NetIncomingMessage msg, Player player)
        {
            uint addStr = msg.ReadVariableUInt32();
            uint addDex = msg.ReadVariableUInt32();
            uint addSta = msg.ReadVariableUInt32();
            uint addVit = msg.ReadVariableUInt32();
            uint remainingPoints = msg.ReadVariableUInt32();
            player.m_activeCharacter.updateAttributes(addStr, addDex, addSta, addVit, remainingPoints);
            player.m_activeCharacter.SetStatsChangeLevel(CombatEntity.STATS_CHANGE_LEVEL.COMPILE_REQUIRED);//player.m_activeCharacter.m_statsUpdated=true;
            player.m_activeCharacter.sendBaseStatsUpdate();
        }

        private void processUseItem(NetIncomingMessage msg, Player player)
        {
            int item_id = msg.ReadVariableInt32();
            int targetType = msg.ReadVariableInt32();
            uint targetID = msg.ReadVariableUInt32();
            float coolDownForItem = player.m_activeCharacter.m_inventory.GetCooldownForItem(item_id);
            Item theItem = player.m_activeCharacter.m_inventory.GetItemFromInventoryID(item_id, true);
            int templateId = (theItem != null ? theItem.m_template_id : -1);            
            string errorString = player.m_activeCharacter.m_inventory.useItem(item_id, targetID, targetType);          
            player.m_activeCharacter.m_inventory.SendUseItemReply(errorString, coolDownForItem);
            player.m_activeCharacter.m_QuestManager.checkIfItemAffectsStage(templateId);
        }

        #region quests
        private void processQuestStageComplete(NetIncomingMessage msg, Player player)
        {
            NetOutgoingMessage outmsg = m_server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.QuestStartReply);
            int quest_id = msg.ReadVariableInt32();
            int stage_id = msg.ReadVariableInt32();
            player.m_activeCharacter.m_QuestManager.tryCompleteStage(quest_id, stage_id);
        }

        private void processQuestStart(NetIncomingMessage msg, Player player)
        {
            NetOutgoingMessage outmsg = m_server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.QuestStartReply);
            int quest_id = msg.ReadVariableInt32();
            string errorString = player.m_activeCharacter.m_QuestManager.StartQuest(quest_id);
            if (!String.IsNullOrEmpty(errorString))
            {
                outmsg.Write((byte)0);
                outmsg.Write(errorString);
            }
            else
            {
                outmsg.Write((byte)1);
                player.m_activeCharacter.m_QuestManager.GetCurrentQuest(quest_id).WriteQuestToMsg(outmsg);
                player.m_activeCharacter.m_QuestManager.writeAvailableQuestsToMessage(outmsg);

                if (Program.m_LogAnalytics)
                {
                    Quest tempQuest = player.m_activeCharacter.m_QuestManager.GetCurrentQuest(quest_id);
                    if (tempQuest != null)
                    {
                        AnalyticsMain logAnalytics = new AnalyticsMain(true);
                        logAnalytics.missionStarted(player, tempQuest.m_QuestTemplate.m_questName, quest_id.ToString());
                    }
                }
            }
            SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.QuestStartReply);
            if (String.IsNullOrEmpty(errorString))
            {
                player.m_activeCharacter.m_QuestManager.checkIfAlreadyHaveRequirements(quest_id);
            }
        }
        #endregion //quests
        #region shop

        private void processSellItem(NetIncomingMessage msg, Player player)
        {
            NetOutgoingMessage outmsg = m_server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.RequestShopReply);

            int npc_id = msg.ReadVariableInt32();
            int shop_id = msg.ReadVariableInt32();
            int templateID = msg.ReadVariableInt32();
            int inventoryid = msg.ReadVariableInt32();
            int number = msg.ReadVariableInt32();
            // find the shop the player has clicked on
            Shop shop = player.m_activeCharacter.m_zone.getShopFromNPCId(npc_id, shop_id);
            if (shop == null)
            {
                outmsg.Write((byte)0);
				string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.SHOP_NOT_FOUND);
				outmsg.Write(locText);
				SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.RequestShopReply);
            }
            else
            {
                String errorString = player.m_activeCharacter.m_inventory.sellItem(shop, templateID, inventoryid, number);
                if (!String.IsNullOrEmpty(errorString))
                {
                    outmsg.Write((byte)0);
                    outmsg.Write(errorString);
                }
                else
                {
                    outmsg.Write((byte)1);
                }
                player.m_activeCharacter.m_inventory.WriteInventoryWithMoneyToMessage(outmsg);
                shop.writeShopStockToMessage(outmsg);
                outmsg.WriteVariableInt32(0);//no item prices update
                SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.RequestShopReply);
                player.m_activeCharacter.m_QuestManager.checkIfItemAffectsStage(templateID);
            }
        }

        private void processPurchaseItem(NetIncomingMessage msg, Player player)
        {
            NetOutgoingMessage outmsg = m_server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.RequestShopReply);

            int npc_id = msg.ReadVariableInt32();
            int shop_id = msg.ReadVariableInt32();
            int templateID = msg.ReadVariableInt32();
            int number = msg.ReadVariableInt32();
            // find the shop the player has clicked on
            Shop shop = player.m_activeCharacter.m_zone.getShopFromNPCId(npc_id, shop_id);

            if (shop == null)
            {
                outmsg.Write((byte)0);
				string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.SHOP_NOT_FOUND);
				outmsg.Write(locText);
				SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.RequestShopReply);
            }
            else
            {
                String errorString = String.Empty;
                if (number < 1)
                {
					errorString = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.MUST_BUY_AT_LEAST_ONE);
                }
                else
                {
                    errorString = player.m_activeCharacter.m_inventory.buyItem(shop, templateID, number);
                }
                if (!String.IsNullOrEmpty(errorString))
                {
                    outmsg.Write((byte)0);
                    outmsg.Write(errorString);
                }
                else
                {
                    outmsg.Write((byte)1);
                }

                player.m_activeCharacter.m_inventory.WriteInventoryWithMoneyToMessage(outmsg);
                shop.writeShopStockToMessage(outmsg);
                outmsg.WriteVariableInt32(0);//no shop prices update
                SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.RequestShopReply);
                player.m_activeCharacter.m_QuestManager.checkIfItemAffectsStage(templateID);
                
                
            }
        }

        private void processRequestShop(NetIncomingMessage msg, Player player)
        {
            NetOutgoingMessage outmsg = m_server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.RequestShopReply);

            int npc_id = msg.ReadVariableInt32();
            int shop_id = msg.ReadVariableInt32();
            // find the shop the player has clicked on
            Shop shop = player.m_activeCharacter.m_zone.getShopFromNPCId(npc_id, shop_id);
            if (shop == null)
            {
                outmsg.Write((byte)0);
				string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.SHOP_NOT_FOUND);
				outmsg.Write(locText);
			}
            else if (shop.CharacterMeetsRequirment(player.m_activeCharacter) == false)
            {
                outmsg.Write((byte)0);
                outmsg.Write(Shop.ShopRestricted);
            }
            else
            {
                player.m_activeCharacter.PlayerIsBusy = true;
                outmsg.Write((byte)1);
                player.m_activeCharacter.m_inventory.WriteInventoryWithMoneyToMessage(outmsg);
                shop.fillShop();
                shop.writeShopStockToMessage(outmsg);
                List<int> pricesList = new List<int>();
                int numPriceRequests = msg.ReadVariableInt32();
                for (int i = 0; i < numPriceRequests; i++)
                {
                    pricesList.Add(msg.ReadVariableInt32());
                }
                shop.appendShopPrices(pricesList);
                outmsg.WriteVariableInt32(pricesList.Count);
                for (int i = 0; i < pricesList.Count; i++)
                {

                    ItemTemplate itemTemplate = ItemTemplateManager.GetItemForID(pricesList[i]);
                    if (itemTemplate != null)
                    {
                        outmsg.WriteVariableInt32(pricesList[i]);
                        outmsg.WriteVariableInt32((int)Math.Ceiling(itemTemplate.m_buyprice * shop.getBuyMultiplier(itemTemplate.m_subtype)));
                        outmsg.WriteVariableInt32((int)Math.Ceiling(itemTemplate.m_sellprice * shop.getSellMultiplier(itemTemplate.m_subtype)));
                    }
                    else
                    {
                        Program.Display("null item " + pricesList[i] + " found in shop for npc " + npc_id);
                        outmsg.WriteVariableInt32(-1);
                        outmsg.WriteVariableInt32(-1);
                        outmsg.WriteVariableInt32(-1);
                    }
                }
                if (Program.m_LogAnalytics)
                {
                    AnalyticsMain logAnalytics = new AnalyticsMain(true);
                    logAnalytics.shopEntered(player, shop.m_shop_id.ToString(), shop.m_shop_name);
                }
            }

            SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.RequestShopReply);
        }
        #endregion
        #region clans

        internal void ProcessClanMessage(NetIncomingMessage msg, Player player)
        {
            HW_CLAN_MESSAGE_TYPE messageType = (HW_CLAN_MESSAGE_TYPE)msg.ReadVariableInt32();
            switch (messageType)
            {
                case HW_CLAN_MESSAGE_TYPE.HW_CLAN_MESSAGE_TYPE_CREATE_CLAN:
                    {
                        if (player.m_activeCharacter != null)
                        {
                            Program.Display("Creating clan");

                            processCreateClanMessage(msg, player);
                            //player.m_activeCharacter.m_zone.ProcessPartyInvite(msg, player);
                        }
                        break;
                    }
                case HW_CLAN_MESSAGE_TYPE.HW_CLAN_MESSAGE_TYPE_INVITE_MEMBER_REQUEST:
                    {
                        if (player.m_activeCharacter != null)
                        {
                            Program.Display("Inviting as Member");
                            processInviteToClanRequest(msg, player);
                        }
                        break;
                    }
                case HW_CLAN_MESSAGE_TYPE.HW_CLAN_MESSAGE_TYPE_INVITE_MEMBER_REPLY:
                    {
                        if (player.m_activeCharacter != null)
                        {
                            processInviteToClanReply(msg, player);
                        }
                        break;
                    }
                case HW_CLAN_MESSAGE_TYPE.HW_CLAN_MESSAGE_TYPE_PROMOTE_MEMBER:
                    {
                        processPromoteClanMember(msg, player);
                        break;
                    }
                case HW_CLAN_MESSAGE_TYPE.HW_CLAN_MESSAGE_TYPE_DEMOTE_MEMBER:
                    {
                        processDemoteClanMember(msg, player);
                        break;
                    }
                /*case HW_CLAN_MESSAGE_TYPE.HW_CLAN_MESSAGE_TYPE_PROMOTE_TO_NOBLE:
                    {
                        Program.Display("Promoting To Noble");
                        processPromoteToNoble(msg, player);
                        //player.m_activeCharacter.m_zone.processLeaveParty(msg, player);
                        break;
                    }
                case HW_CLAN_MESSAGE_TYPE.HW_CLAN_MESSAGE_TYPE_DEMOTE_TO_MEMBER:
                    {
                        processDemoteToMember(msg, player);
                        break;
                    }
                case HW_CLAN_MESSAGE_TYPE.HW_CLAN_MESSAGE_TYPE_REMOVE_MEMBER:
                    {
                        processRemoveFromClan(msg, player);
                        break;
                    }
                case HW_CLAN_MESSAGE_TYPE.HW_CLAN_MESSAGE_TYPE_PROMOTE_TO_LEADER:
                    {
                        processPromoteToLeader(msg, player);
                        break;
                    }*/
                case HW_CLAN_MESSAGE_TYPE.HW_CLAN_MESSAGE_TYPE_DISBAND_CLAN:
                    {
                        processDisbandClan(msg, player);
                        break;
                    }
                case HW_CLAN_MESSAGE_TYPE.HW_CLAN_MESSAGE_TYPE_CHANGE_MESSAGE:
                    {
                        processChangeClanMessage(msg, player);
                        break;
                    }
            }
        }

        void processCreateClanMessage(NetIncomingMessage msg, Player player)
        {
            string clanName = msg.ReadString();
            clanName = Regex.Replace(clanName, Localiser.TextWithEmptySpaceFilter, String.Empty);
            //check if the name is now empty
            if (clanName.Length == 0)
            {
				string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.INVALID_CLAN_NAME);
				sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.CLAN);
                return;
            }

            //if they are not already in a clan
            if (player.m_activeCharacter.CharactersClan != null)
            {
				string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.ALREADY_IN_CLAN);
				sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.CLAN);
				return;
            }
            //check availability
            Clan clanWithName = GetClanWithNameAnyCase(clanName);//
            // Clan clanWithName = GetClanWithName(clanName);
            bool nameIsValid = ProfanityFilter.isAllowed(clanName);
            if (clanWithName != null)
            {
				string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.CLAN_NAME_ALREADY_TAKEN);
				locText = string.Format(locText, clanName);
				sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.CLAN);
				return;
            }
            else if (nameIsValid == false)
            {
				string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.CLAN_NAME_ALREADY_TAKEN);
				locText = string.Format(locText, clanName);
				sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.CLAN);
				Program.Display("user " + player.m_account_id + " character " + player.m_activeCharacter.ServerID + " attempted to make a clan with invalid name " + clanName);
                return;
            }
            //deduct money
            Character currentCharacter = player.m_activeCharacter;
            Item clanToken = currentCharacter.m_inventory.GetItemFromTemplateID(CLAN_CREATION_ITEM_ID, false);
            if (clanToken != null)
            {
                Item oldClanToken = new Item(clanToken);
                //currentCharacter.currentCharacter.m_inventory.consumeItem(teleportToken);
                int numWilRemain = clanToken.m_quantity - 1;
                currentCharacter.m_inventory.ConsumeItem(clanToken.m_template_id, clanToken.m_inventory_id, 1);//
                if (numWilRemain > 0)
                {
                    currentCharacter.m_inventory.SendReplaceItem(oldClanToken, clanToken);
                }
                else
                {
                    currentCharacter.m_inventory.SendReplaceItem(oldClanToken, null);
                }
            }
            else
            {
                ItemTemplate clanTokenTemplate = ItemTemplateManager.GetItemForID(CLAN_CREATION_ITEM_ID);
                if (clanTokenTemplate != null)
                {
					string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.CREATE_CLAN_REQUIRE_ITEM);
					locText = string.Format(locText, clanTokenTemplate.m_loc_item_name[player.m_languageIndex]);
					sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.CLAN);
				}
                else
                {
					string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.CREATE_CLAN_NO_ITEM);
					sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.CLAN);
                }
                return;
            }
            //create clan
            Clan newClan = new Clan();
            newClan.CreateClan(clanName, player.m_activeCharacter);
			string locTextClan = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.CLAN_CREATED);
			locTextClan = string.Format(locTextClan, clanName);
			sendSystemMessage(locTextClan, player, false, SYSTEM_MESSAGE_TYPE.CLAN);
            //add to active clan list

            if (newClan.Leader != null)
            {
                m_clanList.Add(newClan);

                newClan.SendClanListToAllMembers();
            }

            if (Program.m_LogAnalytics)
            {

                AnalyticsMain logAnalytics = new AnalyticsMain(false);
                logAnalytics.LogGuildEvent(player, String.Empty, String.Empty, clanName, Analytics.Social.GuildAction.FOUNDED.ToString());
            }
        }

        void processInviteToClanRequest(NetIncomingMessage msg, Player player)
        {
            int inviteeID = msg.ReadVariableInt32();
			 
            Player invitee = getPlayerFromActiveCharacterId(inviteeID);
            if (invitee != null)
            {
                Character inviter = player.m_activeCharacter;
                double netTime = NetTime.Now;
                if (inviter.TimeAtLastClanEvent + Clan.TIME_BETWEEN_CLAN_ACTIONS > netTime)
                {
					string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.TOO_MANY_CLAN_ACTIONS);
					Program.processor.sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.CLAN, false);
                }
                else
                {           
                    //send server clan invite
                    if (inviter != null)
                    {
                        inviter.TimeAtLastClanEvent = netTime;
                        sendServerClanInvite(player, invitee);
                    }
                }
            }
        }

        void processInviteToClanReply(NetIncomingMessage msg, Player player)
        {
            int clanID = msg.ReadVariableInt32();
            HW_FRIEND_REPLY replyType = (HW_FRIEND_REPLY)msg.ReadVariableInt32();
            int invitingCharacterID = msg.ReadVariableInt32();
            Clan theClan = GetClanWithID(clanID);

            if (replyType != HW_FRIEND_REPLY.HW_FRIEND_REPLY_ACCEPT)
            {
                Player initialSender = getPlayerFromActiveCharacterId(invitingCharacterID);
                int senderID = -1;
                if (initialSender != null)
                {
                    senderID = (int)initialSender.m_account_id;
					string locText= Localiser.GetString(textDB, initialSender, (int)CommandProcessorTextDB.TextID.OTHER_DECLINED_CLAN_INVITATION);
					locText = string.Format(locText, player.m_activeCharacter.m_name);
					sendSystemMessage(locText, initialSender, false, SYSTEM_MESSAGE_TYPE.CLAN);
				}
                if (Program.m_LogAnalytics)
                {
                    bool accepted = false;
                    AnalyticsMain logAnalytics = new AnalyticsMain(false);
                    logAnalytics.LogInviteRecieved(player, senderID.ToString(), "GUILD", "-1", accepted);
                }
                return;
            }

            if (theClan == null)
            {
				string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.CLAN_NOT_EXIST);
				sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.CLAN);
				return;
            }
            if (player.m_activeCharacter.CharactersClan != null)
            {
				string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.PLAYER_ALREADY_CLAN_MEMBER);
				sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.CLAN);
				return;
            }
            theClan.AddMember(player.m_activeCharacter);

            Program.Display("Added " + player.m_activeCharacter.m_name + " to clan " + theClan.ClanName);
            theClan.SendClanListToAllMembers();
			string locTextJoinClan = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.PLAYER_JOINED_CLAN);
			locTextJoinClan = string.Format(locTextJoinClan, theClan.ClanName);
			sendSystemMessage(locTextJoinClan, player, false, SYSTEM_MESSAGE_TYPE.CLAN);

			if (Program.m_LogAnalytics)
            {
                bool accepted = true;
                Player initialSender = getPlayerFromActiveCharacterId(invitingCharacterID);
                int senderID = -1;
                if (initialSender != null)
                {
                    senderID = (int)initialSender.m_account_id;
                }
                AnalyticsMain logAnalytics = new AnalyticsMain(false);
                logAnalytics.LogInviteRecieved(player, senderID.ToString(), "GUILD", "-1", accepted);
                logAnalytics.LogGuildEvent(player, String.Empty, String.Empty, theClan.ClanName, Analytics.Social.GuildAction.JOINED.ToString());
            }
        }

        void processPromoteClanMember(NetIncomingMessage msg, Player player)
        {
            int characterID = msg.ReadVariableInt32();
            Clan.CLAN_RANKS currentRank = (Clan.CLAN_RANKS)msg.ReadVariableInt32();

            Clan theClan = player.m_activeCharacter.CharactersClan;
            if (theClan == null)
            {
                return;
            }
            Character promoter = player.m_activeCharacter;
            double netTime = NetTime.Now;
            if (promoter.TimeAtLastClanEvent + Clan.TIME_BETWEEN_CLAN_ACTIONS > netTime)
            {
				string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.TOO_MANY_CLAN_ACTIONS);
				Program.processor.sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.CLAN, false);
                return;
            }
            else
            {
                promoter.TimeAtLastClanEvent = netTime;
            }
            theClan.PromoteMember(characterID, currentRank, (int)player.m_activeCharacter.m_character_id);
            theClan.SendClanListToAllMembers();
        }

        void processDemoteClanMember(NetIncomingMessage msg, Player player)
        {
            int characterID = msg.ReadVariableInt32();
            Clan.CLAN_RANKS currentRank = (Clan.CLAN_RANKS)msg.ReadVariableInt32();
            Clan theClan = player.m_activeCharacter.CharactersClan;
            if (theClan == null)
            {
                return;
            }
            Character promoter = player.m_activeCharacter;
            double netTime = NetTime.Now;
            if (promoter.TimeAtLastClanEvent + Clan.TIME_BETWEEN_CLAN_ACTIONS > netTime)
            {
				string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.TOO_MANY_CLAN_ACTIONS);
				Program.processor.sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.CLAN, false);
				return;
            }
            else
            {
                promoter.TimeAtLastClanEvent = netTime;
            }
            theClan.DemoteMember(characterID, currentRank, (int)player.m_activeCharacter.m_character_id);
            theClan.SendClanListToAllMembers();
        }
        
        void processDisbandClan(NetIncomingMessage msg, Player player)
        {
            Clan theClan = player.m_activeCharacter.CharactersClan;
            if (theClan == null)
            {
                return;
            }
            bool hasLeaderPrivileges = theClan.HasLeaderRights((int)player.m_activeCharacter.m_character_id);
            if ((theClan.ClanMembers.Count <= 1) && (hasLeaderPrivileges))
            {
                theClan.DisbandClan();
                m_clanList.Remove(theClan);
				string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.CLAN_DISBANDED);
				Program.processor.sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.CLAN);
				Clan.sendNoClanMessage(player.m_activeCharacter);
            }
        }

        void processChangeClanMessage(NetIncomingMessage msg, Player player)
        {
            string newMessage = msg.ReadString();
            Character character = player.m_activeCharacter;
            if (character == null)
            {
                return;
            }
            Clan theClan = character.CharactersClan;
            if (theClan == null)
            {
                return;
            }
            bool canEditMessage = theClan.HasEditClanMessageRights((int)character.m_character_id);
            if (canEditMessage)
            {
                theClan.SetClanMessage(newMessage, (int)character.m_character_id);
                theClan.SendClanListToAllMembers();
            }
        }

        void sendServerClanInvite(Player player, Player invitee)
        {
            Clan theClan = player.m_activeCharacter.CharactersClan;
            //is there a clan to join
            if (theClan == null)
            {
				string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.PLAYER_NOT_CLAN_MEMBER);
				sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.CLAN);
				return;
            }

            if (theClan.HasInviteRights((int)player.m_activeCharacter.m_character_id) == false)
            {
				string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.PLAYER_NO_INVITATION_RIGHTS);
				sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.CLAN);
				return;
            }
            if (invitee.m_activeCharacter.CharactersClan != null)
            {
				string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.THEY_ALREADY_CLAN_MEMBER);
				sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.CLAN);
				return;
            }
            bool tellPlayerInviteSent = true;
            if (invitee.m_activeCharacter.HasBlockedCharacter((int)player.m_activeCharacter.m_character_id) == false)
            {
                if (invitee.m_activeCharacter.CanTakeRequest() == true)
                {
                    NetOutgoingMessage outmsg = m_server.CreateMessage();
                    outmsg.WriteVariableUInt32((uint)NetworkCommandType.ClanMessage);
                    outmsg.WriteVariableInt32((int)HW_CLAN_MESSAGE_TYPE.HW_CLAN_MESSAGE_TYPE_INVITE_MEMBER_SERVER_REQUEST);
                    outmsg.WriteVariableInt32(theClan.ClanID);
                    outmsg.Write(theClan.ClanName);
                    outmsg.WriteVariableInt32((int)player.m_activeCharacter.m_character_id);
                    outmsg.Write(player.m_activeCharacter.m_name);
                    SendMessage(outmsg, invitee.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.ClanMessage);
                    if (Program.m_LogAnalytics)
                    {
                        AnalyticsMain logAnalytics = new AnalyticsMain(false);
                        logAnalytics.LogInviteSent(player, invitee, "GUILD", "-1");
                    }
                }
                else
                {
                    tellPlayerInviteSent = false;
					sendSystemMessage(player.m_activeCharacter.GetPlayerBusyString(), player, false, SYSTEM_MESSAGE_TYPE.CLAN);
                }
            }
            if (tellPlayerInviteSent)
            {
				string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.PLAYER_INVITE_OTHER_JOIN_CLAN);
				locText = String.Format(locText, invitee.m_activeCharacter.m_name);
				sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.CLAN);
			}
        }
        
        internal Clan GetClanWithNameAnyCase(string clanName)
        {
            Clan clanForName = null;
            string uppercaseClanName = clanName.ToUpper();
            for (int i = 0; i < m_clanList.Count; i++)
            {
                Clan currentClan = m_clanList[i];
                if (currentClan.ClanName.ToUpper() == uppercaseClanName)
                {
                    clanForName = currentClan;
                    return clanForName;
                }
            }
            return clanForName;
        }

        internal Clan GetClanWithID(int clanID)
        {
            Clan clanForName = null;

            for (int i = 0; i < m_clanList.Count; i++)
            {
                Clan currentClan = m_clanList[i];
                if (currentClan.ClanID == clanID)
                {
                    clanForName = currentClan;
                    return clanForName;
                }
            }
            return clanForName;
        }

       
        #endregion

        public void updateShopHistory(int zone_id, int shop_id, int inventory_id, int template_id, int amount, int total_cost, int character_id, string detail)
        {
            m_worldDB.runCommandSync("insert into shop_history (zone_id,shop_id,inventory_id,item_id,amount,transaction_date,total_cost,character_id,detail) values (" + zone_id + "," + shop_id + "," + inventory_id + "," + template_id + "," + amount + ",'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'," + total_cost + "," + character_id + ",\"" + detail + "\")");
        }

        private void processDeleteItem(NetIncomingMessage msg, Player player)
        {
            NetOutgoingMessage outmsg = m_server.CreateMessage();
            int templateID = msg.ReadVariableInt32();
            int inventoryID = msg.ReadVariableInt32();
            int amount = msg.ReadVariableInt32();
            string errorMessage = String.Empty;
            Item theItem = player.m_activeCharacter.m_inventory.GetItemFromInventoryID(inventoryID, true);
            errorMessage = player.m_activeCharacter.m_inventory.DeleteItem(templateID, inventoryID, amount);
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.DeleteItemReply);

            if (!String.IsNullOrEmpty(errorMessage))
            {
                string templateStr = "template id" + templateID;
                Program.Display("not deleting item " + amount + " of template ID " + templateID + " for " + player.m_activeCharacter.m_name + " " + errorMessage);
                outmsg.Write((byte)0);
                outmsg.Write(errorMessage);
                player.m_activeCharacter.m_inventory.WriteEquipmentToMessage(outmsg);
                player.m_activeCharacter.m_inventory.WriteInventoryToMessage(outmsg);
                SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.DeleteItemReply);
            }
            else
            {
                string comment = "Dropped";
                if (theItem != null && theItem.m_template != null)
                {
                    if (theItem.m_bound)
                    {
                        comment += " [bound]";
                    }
                    if (theItem.m_template.m_maxCharges > 0)
                    {
                        comment += " Charges Remaining : " + theItem.m_remainingCharges;
                    }
                }
                updateShopHistory(-1, -1, inventoryID, templateID, -amount, 0, (int)player.m_activeCharacter.m_character_id, comment);

                Program.Display("deleting item " + amount + " of " + ItemTemplateManager.GetItemForID(templateID).m_item_name + " [" + templateID + "] inv_id=" + inventoryID + " for " + player.m_activeCharacter.m_name);
                outmsg.Write((byte)1);

                player.m_activeCharacter.m_inventory.WriteEquipmentToMessage(outmsg);
                player.m_activeCharacter.m_inventory.WriteInventoryToMessage(outmsg);
                SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.DeleteItemReply);

                NetOutgoingMessage othersmsg = m_server.CreateMessage();
                List<NetConnection> connections = player.m_activeCharacter.m_zone.getUpdateList(player);
                othersmsg.WriteVariableUInt32((uint)NetworkCommandType.PlayerAppearanceUpdate);
                othersmsg.WriteVariableUInt32(player.m_activeCharacter.m_character_id);
                player.m_activeCharacter.writeUpdateInfoToMessage(othersmsg);
                SendMessage(othersmsg, connections, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.PlayerAppearanceUpdate);
                player.m_activeCharacter.m_QuestManager.checkIfItemAffectsStage(templateID);
                if (Program.m_LogAnalytics)
                {
                    ItemTemplate tempItem = ItemTemplateManager.GetItemForID(templateID);

                    if (tempItem != null)
                    {
                        AnalyticsMain logAnalytics = new AnalyticsMain(true);
                        logAnalytics.itemActioned(player, tempItem.m_item_id.ToString(), tempItem.m_item_name, tempItem.m_subtype.ToString(), "DROPPED");
                    }
                }
            }
        }

        internal void SendDeleteItemReply(string errorMessage, Player player)
        {
            NetOutgoingMessage outmsg = m_server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.DeleteItemReply);

            if (!String.IsNullOrEmpty(errorMessage))
            {
                outmsg.Write((byte)0);
                outmsg.Write(errorMessage);
            }
            else
            {
                outmsg.Write((byte)1);
            }
            player.m_activeCharacter.m_inventory.WriteEquipmentToMessage(outmsg);
            player.m_activeCharacter.m_inventory.WriteInventoryToMessage(outmsg);
            SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.DeleteItemReply);
        }

        private void processEquipItem(NetIncomingMessage msg, Player player)
        {
                        
            byte destination = msg.ReadByte();
            int templateID = msg.ReadVariableInt32();
            int inventoryID = msg.ReadVariableInt32();
            int amount = msg.ReadVariableInt32();
            int slot = msg.ReadVariableInt32();
            string errorMessage = String.Empty;

            // unequip
            if (destination == 0) 
            {
                errorMessage = player.m_activeCharacter.m_inventory.UnequipItem(templateID, inventoryID, amount, slot);                
            }
            // equip
            else if (destination == 1) 
            {
                errorMessage = player.m_activeCharacter.m_inventory.EquipItem(templateID, inventoryID, amount, slot);               
            }

            // create reply message
            NetOutgoingMessage outmsg = m_server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.EquipItemReply);

            if (!String.IsNullOrEmpty(errorMessage))
            {
                outmsg.Write((byte)0);
                outmsg.Write(errorMessage);
                player.m_activeCharacter.m_inventory.WriteEquipmentToMessage(outmsg);
                player.m_activeCharacter.m_inventory.WriteInventoryToMessage(outmsg);
                SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.EquipItemReply);
            }
            else
            {
                player.m_activeCharacter.SetStatsChangeLevel(CombatEntity.STATS_CHANGE_LEVEL.EQUIPMENT_CHECK_REQUIRED);//player.m_activeCharacter.m_statsUpdated=true;
                outmsg.Write((byte)1);
                player.m_activeCharacter.m_inventory.WriteEquipmentToMessage(outmsg);
                player.m_activeCharacter.m_inventory.WriteInventoryToMessage(outmsg);
                SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.EquipItemReply);
                if (Program.m_LogAnalytics)
                {
                    ItemTemplate tempItem = ItemTemplateManager.GetItemForID(templateID);

                    if (tempItem != null && destination == 1)
                    {
                        AnalyticsMain logAnalytics = new AnalyticsMain(true);
                        logAnalytics.itemActioned(player, tempItem.m_item_id.ToString(), tempItem.m_item_name, tempItem.m_subtype.ToString(), "EQUIPPED");
                    }
                }
            }

		
        }

        private void ProcessInventoryFavouriteItem(NetIncomingMessage msg, Player player)
        {
            byte isFavouritedByte = msg.ReadByte();
            int inventoryID = msg.ReadVariableInt32();
            player.m_activeCharacter.m_inventory.FavouriteItem(inventoryID, isFavouritedByte == 1);
        }

        internal void SendPlayerAppearanceUpdate(Player player)
        {
            NetOutgoingMessage othersmsg = m_server.CreateMessage();
            List<NetConnection> connections = player.m_activeCharacter.m_zone.getUpdateList(player);
            othersmsg.WriteVariableUInt32((uint)NetworkCommandType.PlayerAppearanceUpdate);
            othersmsg.WriteVariableUInt32(player.m_activeCharacter.m_character_id);
            player.m_activeCharacter.writeUpdateInfoToMessage(othersmsg);
            SendMessage(othersmsg, connections, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.PlayerAppearanceUpdate);
        }
        
        #region Trade

        void processTradeMessage(NetIncomingMessage msg, Player player)
        {
            TRADE_MESSAGE messageType = (TRADE_MESSAGE)msg.ReadVariableInt32();
            switch (messageType)
            {
                case TRADE_MESSAGE.TM_SelfInitiateTrade:
                    {
                        processSelfInitiateTrade(msg, player);
                        break;
                    }
                case TRADE_MESSAGE.TM_SelfCancelTrade:
                    {
                        processSelfCancelTrade(msg, player);
                        break;
                    }
                case TRADE_MESSAGE.TM_SelfToggleTradeReadyButton:
                    {
                        processSelfToggleTradeReady(msg, player);
                        break;
                    }
                case TRADE_MESSAGE.TM_SelfUpdateTradeSlot:
                    {
                        processUpdateTradeSlot(msg, player);
                        break;
                    }
                case TRADE_MESSAGE.TM_ClientAcceptTrade:
                    {
                        processClientAcceptTrade(msg, player);
                        break;
                    }
                /*case TRADE_MESSAGE.TM_OtherInitiateTrade:
                {
                    //sent by server
                    break;
                }
                case TRADE_MESSAGE.TM_OtherCancelTrade:
                {
                    //sent by server
                    break;
                }
                case TRADE_MESSAGE.TM_OtherToggleTradeReadyButton:
                {
                    //sent by server
                    break;
                }
                case TRADE_MESSAGE.TM_TradeScreenUpdate:
                {
                    //sent by server
                    break;
                }*/
                default:
                    {
                        Program.Display("Encountered unhandled trade message " + messageType);
                        break;
                    }
            }

            /* TM_SelfInitiateTrade = 0,
         TM_OtherInitiateTrade = 1,
         TM_SelfCancelTrade = 2,
         TM_OtherCancelTrade = 3,
         TM_SelfUpdateTradeSlot = 4,
         TM_TradeScreenUpdate = 5,
         TM_SelfToggleTradeReadyButton = 6,
         TM_OtherToggleTradeReadyButton = 7*/
        }

        private void processSelfToggleTradeReady(NetIncomingMessage msg, Player player)
        {
            // validate that trading is taking place
            bool process = false;
            Player otherPlayer = null;
            if (player.m_activeCharacter.m_tradingWith != null)
            {
                otherPlayer = player.m_activeCharacter.m_tradingWith;
                if (otherPlayer.m_activeCharacter.m_tradingWith == player)
                {
                    process = true;
                }
            }
            if (process)
            {
                byte val = msg.ReadByte();
                Boolean buttonOn = val == 1 ? true : false;
                player.m_activeCharacter.m_tradeReady = buttonOn;
                //trade Goes Through
                /*if (otherPlayer.m_activeCharacter.m_tradeReady && buttonOn)
                {
                    player.m_activeCharacter.completeTrade();
                    //tell each side the trade is complete
                    sendTradeComplete(player, otherPlayer);
                    sendTradeComplete(otherPlayer, player);

                    /*NetOutgoingMessage outmsg = m_server.CreateMessage();
                    //outmsg.WriteVariableUInt32((uint)NetworkCommandType.TradeComplete);
                    outmsg.WriteVariableUInt32((uint)NetworkCommandType.TradeMessage);
                    outmsg.WriteVariableInt32((int)TRADE_MESSAGE.TM_TradeComplete);
                    writeTradeWindow(outmsg, player, otherPlayer);
                    SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.TradeMessage);//was TradeComplete
                    NetOutgoingMessage othersmsg = m_server.CreateMessage();
                    //othersmsg.WriteVariableUInt32((uint)NetworkCommandType.TradeComplete);
                    othersmsg.WriteVariableUInt32((uint)NetworkCommandType.TradeMessage);
                    othersmsg.WriteVariableInt32((int)TRADE_MESSAGE.TM_TradeComplete);
                    writeTradeWindow(othersmsg, otherPlayer, player);
                    SendMessage(othersmsg, otherPlayer.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.TradeMessage);//was TradeComplete
                    */
                /*
          }
          else*/
                {
                    sendOtherToggleTradeReady(otherPlayer, buttonOn);

                    /*NetOutgoingMessage outmsg = m_server.CreateMessage();
                    //outmsg.WriteVariableUInt32((uint)NetworkCommandType.OtherToggleTradeReadyButton);
                    outmsg.WriteVariableUInt32((uint)NetworkCommandType.TradeMessage);
                    outmsg.WriteVariableInt32((int)TRADE_MESSAGE.TM_OtherToggleTradeReadyButton);
                    outmsg.Write((byte)val);
                    SendMessage(outmsg, otherPlayer.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.TradeMessage);//was OtherToggleTradeReadyButton
                    */
                }
            }
        }

        void sendTradeComplete(Player player, Player otherPlayer)
        {
            NetOutgoingMessage outmsg = m_server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.TradeMessage);
            outmsg.WriteVariableInt32((int)TRADE_MESSAGE.TM_TradeComplete);
            writeTradeWindow(outmsg, player, otherPlayer);
            SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.TradeMessage);//was TradeComplete
        }
        void sendOtherToggleTradeReady(Player player, bool tradeReady)
        {
            NetOutgoingMessage outmsg = m_server.CreateMessage();
            //outmsg.WriteVariableUInt32((uint)NetworkCommandType.OtherToggleTradeReadyButton);
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.TradeMessage);
            outmsg.WriteVariableInt32((int)TRADE_MESSAGE.TM_OtherToggleTradeReadyButton);
            if (tradeReady)
            {
                outmsg.Write((byte)1);
            }
            else
            {
                outmsg.Write((byte)0);
            }
            SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.TradeMessage);//was OtherToggleTradeReadyButton
        }

        private void processUpdateTradeSlot(NetIncomingMessage msg, Player player)
        {
            // validate that trading is taking place
            bool process = false;
            Player otherPlayer = null;
            string playerName = player.m_activeCharacter.Name;
            string otherName = "Unknown player";
            if (player.m_activeCharacter.m_tradingWith != null)
            {
                otherPlayer = player.m_activeCharacter.m_tradingWith;
                otherName = otherPlayer.m_activeCharacter.Name;

                if (otherPlayer.m_activeCharacter.m_tradingWith == player)
                {
                    process = true;
                }
            }
            if (!process)
            {
                //outmsg.WriteVariableUInt32((uint)NetworkCommandType.OtherCancelTrade);
                /* outmsg.WriteVariableUInt32((uint)NetworkCommandType.TradeMessage);
                 outmsg.WriteVariableInt32((int)TRADE_MESSAGE.TM_OtherCancelTrade);*/
                Program.Display("Trade between " + playerName + " and " + otherName + " failed as they did not both know they were trading" + playerName + " gold at end " + player.m_activeCharacter.m_inventory.m_coins);

				string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.CONFIRMATION_CHANGE_TRADE_CANCELLED);
				sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.TRADE);
                if (otherPlayer != null && otherPlayer.m_activeCharacter != null)
                {
                    //outmsg.WriteVariableInt32((int)otherPlayer.m_activeCharacter.m_character_id);
                    SendOtherCancelTrade(player, (int)otherPlayer.m_activeCharacter.m_character_id);
                }
                else
                {
                    SendOtherCancelTrade(player, -1);
                    //outmsg.WriteVariableInt32(-1);
                }
                //SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.TradeMessage);//was OtherCancelTrade
            }
            else
            {
                string errorMessage = String.Empty;
                byte tradingMoney = msg.ReadByte();
                if (tradingMoney == 1)
                {
                    int amount = msg.ReadVariableInt32();
                    errorMessage = player.m_activeCharacter.setTradeMoney(amount);
                    //if the change succeded explain what was done

                    if (String.IsNullOrEmpty(errorMessage))
                    {
						// only send a message if it's not returned an error

						string locText = Localiser.GetString(textDB, otherPlayer, (int)CommandProcessorTextDB.TextID.OTHER_SET_GOLD);
						locText = String.Format(locText, playerName, amount);
						sendSystemMessage(locText, otherPlayer, false, SYSTEM_MESSAGE_TYPE.TRADE);

                        Program.Display(playerName + " (trading With " + otherName + ") set trade money to " + amount);
                    }
                }
                else
                {
                    DestinationBucket destBucket = (DestinationBucket)msg.ReadByte();
                    int templateID = msg.ReadVariableInt32();
                    int inventoryID = msg.ReadVariableInt32();
                    int amount = msg.ReadVariableInt32();
                    // attempt the move between buckets
                    ItemTemplate tradedItemTemp = ItemTemplateManager.GetItemForID(templateID);

                    if (amount > 0)
                    {
                        errorMessage = player.m_activeCharacter.MoveTradeItem(destBucket, templateID, inventoryID, amount);
                    }
                    else
                    {
						errorMessage = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.NO_ITEMS_TRADED);
					}
                    if (String.IsNullOrEmpty(errorMessage))
                    {
                        if (tradedItemTemp != null)
                        {
							string locText = String.Empty;
							if (destBucket == DestinationBucket.Bucket_CharacterTradingInventory)
                            {
								locText = Localiser.GetString(textDB, otherPlayer, (int)CommandProcessorTextDB.TextID.OTHER_ADDED_ITEM);
								locText = String.Format(locText, playerName, amount, tradedItemTemp.m_loc_item_name[otherPlayer.m_languageIndex]);
							}
                            else
							{
								locText = Localiser.GetString(textDB, otherPlayer, (int)CommandProcessorTextDB.TextID.OTHER_REMOVED_ITEM);
								locText = String.Format(locText, playerName, amount, tradedItemTemp.m_loc_item_name[otherPlayer.m_languageIndex]);
							}
							sendSystemMessage(locText, otherPlayer, false, SYSTEM_MESSAGE_TYPE.TRADE);
						}
                        Program.Display(playerName + " (trading With " + otherName + ") moved item TempID= " + templateID + " invID = " + inventoryID + " amount =" + amount + " to " + destBucket);
                    }
                }
                if (String.IsNullOrEmpty(errorMessage))
                {
                    player.m_activeCharacter.m_tradeReady = false;
                    otherPlayer.m_activeCharacter.m_tradeReady = false;
                    SendTradeScreenUpdate(player, otherPlayer, errorMessage);
                    SendTradeScreenUpdate(otherPlayer, player, errorMessage);
                }
                else
                {
                    Program.Display(playerName + " (trading With " + otherName + ") processUpdateTradeSlot failed with error : " + errorMessage);
                    player.m_activeCharacter.m_tradeReady = false;
                    SendTradeScreenUpdate(player, otherPlayer, errorMessage);
                }
            }
        }

        void SendTradeScreenUpdate(Player player, Player otherPlayer, string errorMessage)
        {
            NetOutgoingMessage outmsg = m_server.CreateMessage();
            //outmsg.WriteVariableUInt32((uint)NetworkCommandType.TradeScreenUpdate);
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.TradeMessage);
            outmsg.WriteVariableInt32((int)TRADE_MESSAGE.TM_TradeScreenUpdate);
            if (!String.IsNullOrEmpty(errorMessage))
            {
                outmsg.Write((byte)0);
                outmsg.Write(errorMessage);
                player.m_activeCharacter.m_tradeReady = false;
                writeTradeWindow(outmsg, player, otherPlayer);
                SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.TradeMessage);//was TradeScreenUpdate
            }
            else
            {
                // player.m_activeCharacter.m_tradeReady = false;
                //otherPlayer.m_activeCharacter.m_tradeReady = false;
                outmsg.Write((byte)1);
                writeTradeWindow(outmsg, player, otherPlayer);
                SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.TradeMessage);//was TradeScreenUpdate
                /* NetOutgoingMessage othersmsg = m_server.CreateMessage();
                 //othersmsg.WriteVariableUInt32((uint)NetworkCommandType.TradeScreenUpdate);
                 othersmsg.WriteVariableUInt32((uint)NetworkCommandType.TradeMessage);
                 othersmsg.WriteVariableInt32((int)TRADE_MESSAGE.TM_TradeScreenUpdate);
                 othersmsg.Write((byte)1);
                 writeTradeWindow(othersmsg, otherPlayer, player);
                 SendMessage(othersmsg, otherPlayer.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.TradeMessage);//was TradeScreenUpdate
 */
            }
        }

        private void writeTradeWindow(NetOutgoingMessage outmsg, Player player, Player otherPlayer)
        {
            player.m_activeCharacter.m_inventory.WriteInventoryWithMoneyToMessage(outmsg);
            player.m_activeCharacter.m_tradingInventory.WriteInventoryWithMoneyToMessage(outmsg);
            otherPlayer.m_activeCharacter.m_tradingInventory.WriteInventoryWithMoneyToMessage(outmsg);
        }

        private void processSelfCancelTrade(NetIncomingMessage msg, Player player)
        {
            int otherplayerId = msg.ReadVariableInt32();
            Player otherPlayer = getPlayerFromActiveCharacterId(otherplayerId);
            /*NetOutgoingMessage outmsg = m_server.CreateMessage();

            //outmsg.WriteVariableUInt32((uint)NetworkCommandType.OtherCancelTrade);
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.TradeMessage);
            outmsg.WriteVariableInt32((int)TRADE_MESSAGE.TM_OtherCancelTrade);*/
            if (otherPlayer == null || otherPlayer.m_activeCharacter.m_tradingWith != player)
            {
                string playerTradingDataString = " " + player.m_activeCharacter.Name + " gold = " + player.m_activeCharacter.m_inventory.m_coins;

                if (player.m_activeCharacter.CurrentRequest != null)
                {
                    player.m_activeCharacter.CurrentRequest.CancelRequest(player, PendingRequest.CANCEL_CONDITION.CC_SELF_CANCEL);
                }

                player.m_activeCharacter.cancelTrade();
                if (otherPlayer == null)
                {
                    Program.Display(player.m_activeCharacter.m_name + " failed to cancel trade, unknown player" + playerTradingDataString);
                }
                else if (otherPlayer.m_activeCharacter.m_tradingWith == null)
                {
                    Program.Display(player.m_activeCharacter.m_name + " failed to cancel trade, " + otherPlayer.m_activeCharacter.m_name + " has no trading with" + playerTradingDataString);
                }
                else
                {
                    Program.Display(player.m_activeCharacter.m_name + " failed to cancel trade, " + otherPlayer.m_activeCharacter.m_tradingWith.m_activeCharacter.m_name + " not " + player.m_activeCharacter.m_name + playerTradingDataString);

                }
                SendOtherCancelTrade(player, otherplayerId);
                /*
                outmsg.WriteVariableInt32(otherplayerId);
                SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.TradeMessage);//was OtherCancelTrade
                */
            }
            else
            {
                player.m_activeCharacter.cancelTrade();
                otherPlayer.m_activeCharacter.cancelTrade();

                string playerTradingDataString = player.m_activeCharacter.Name + "gold = " + player.m_activeCharacter.m_inventory.m_coins;
                string otherTradingDataString = otherPlayer.m_activeCharacter.Name + "gold = " + otherPlayer.m_activeCharacter.m_inventory.m_coins;

                if (player.m_activeCharacter.CurrentRequest != null)
                {
                    player.m_activeCharacter.CurrentRequest.CancelRequest(player, PendingRequest.CANCEL_CONDITION.CC_SELF_CANCEL);
                    Program.Display(player.m_activeCharacter.m_name + " refusing trade with " + otherPlayer.m_activeCharacter.m_name);
                    Program.Display(playerTradingDataString);
                    Program.Display(otherTradingDataString);
                }//trading has started
                else
                {
                    Program.Display(player.m_activeCharacter.m_name + " cancelling trade with " + otherPlayer.m_activeCharacter.m_name);
                    Program.Display(playerTradingDataString);
                    Program.Display(otherTradingDataString);

					string locText = Localiser.GetString(textDB, otherPlayer, (int)CommandProcessorTextDB.TextID.OTHER_CANCELLED_TRADE);
					locText = String.Format(locText, player.m_activeCharacter.Name);
					sendSystemMessage(locText, otherPlayer, false, SYSTEM_MESSAGE_TYPE.TRADE);
                }

                SendOtherCancelTrade(otherPlayer, (int)player.m_activeCharacter.m_character_id);
                //close the senders own window if it's open
                SendOtherCancelTrade(player, otherplayerId);
                /*

                outmsg.WriteVariableInt32((int)player.m_activeCharacter.m_character_id);
               
                Program.Display(player.m_activeCharacter.m_name + " cancelling trade with " + otherPlayer.m_activeCharacter.m_name);
                SendMessage(outmsg, otherPlayer.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.TradeMessage);//was OtherCancelTrade
            */
            }
        }

        private void SendOpenTradeWindow(Player player, int otherCharacterID)
        {
            NetOutgoingMessage outmsg = m_server.CreateMessage();
            //outmsg.WriteVariableUInt32((uint)NetworkCommandType.OtherCancelTrade);
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.TradeMessage);
            outmsg.WriteVariableInt32((int)TRADE_MESSAGE.TM_OpenTradeWindow);
            outmsg.WriteVariableInt32(otherCharacterID);
            SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.TradeMessage);//was OtherCancelTrade
        }

        internal void SendOtherCancelTrade(Player player, int otherCharacterID)
        {
            NetOutgoingMessage outmsg = m_server.CreateMessage();
            //outmsg.WriteVariableUInt32((uint)NetworkCommandType.OtherCancelTrade);
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.TradeMessage);
            outmsg.WriteVariableInt32((int)TRADE_MESSAGE.TM_OtherCancelTrade);
            outmsg.WriteVariableInt32(otherCharacterID);
            SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.TradeMessage);//was OtherCancelTrade
        }

        void processClientAcceptTrade(NetIncomingMessage msg, Player player)
        {
            //who do you think you are trading with
            int otherplayerId = msg.ReadVariableInt32();
            Player otherPlayer = player.m_activeCharacter.m_tradingWith;
            //check they are the right person
            if (otherPlayer == null || otherPlayer.m_activeCharacter == null || otherPlayer.m_activeCharacter.m_character_id != (uint)otherplayerId || otherPlayer.m_activeCharacter.m_tradingWith != player)
            {
                Program.Display("error in processClientAcceptTrade. Character " + player.m_activeCharacter.m_character_id + " " + player.m_activeCharacter.Name + " trade partner could not be found");
				//something's gone wrong, cancel the trade

				string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.CONFIRMATION_CHANGE_TRADE_CANCELLED);
				sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.TRADE);
				player.m_activeCharacter.cancelTrade();
                SendOtherCancelTrade(player, otherplayerId);
            }
            //check the conditions for the confirm are met
            if (player.m_activeCharacter.m_tradeReady && otherPlayer.m_activeCharacter.m_tradeReady)
            {
                // set that they have accepted
                player.m_activeCharacter.TradeAccepted = true;

                //if they have both accepted then complete the trade
                if (otherPlayer.m_activeCharacter.TradeAccepted)
                {
                    if (Program.m_LogAnalytics)
                    {
                        AnalyticsMain logAnalytics = new AnalyticsMain(true);
                        logAnalytics.playerTrade(player, otherPlayer);
                        logAnalytics.playerTrade(otherPlayer, player);
                    }

                    player.m_activeCharacter.completeTrade();
                    //tell each side the trade is complete
                    sendTradeComplete(player, otherPlayer);
                    sendTradeComplete(otherPlayer, player);
                }
            }
            else
            {
				string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.CONFIRMATION_CHANGE_TRADE_CANCELLED);
				sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.TRADE);
				sendSystemMessage(locText, otherPlayer, false, SYSTEM_MESSAGE_TYPE.TRADE);
				//if not cancel the trade as something has gone wrong
				player.m_activeCharacter.cancelTrade();
                SendOtherCancelTrade(player, otherplayerId);
                otherPlayer.m_activeCharacter.cancelTrade();
                SendOtherCancelTrade(otherPlayer, (int)player.m_activeCharacter.m_character_id);
                Program.Display("error in processClientAcceptTrade. Character " + player.m_activeCharacter.m_character_id + " " + player.m_activeCharacter.Name + " sent an accept when both characters were not ready");
            }
        }

        private void processSelfInitiateTrade(NetIncomingMessage msg, Player player)
        {
            int otherplayerId = msg.ReadVariableInt32();
            bool isReply = (msg.ReadByte() == 1);
            Player otherPlayer = getPlayerFromActiveCharacterId(otherplayerId);
            Character otherCharacter = null;
            Character playersCharacter = player.m_activeCharacter;
            if (otherPlayer != null)
            {
                otherCharacter = otherPlayer.m_activeCharacter;
            }
            //check they are not already trading with each other
            if (otherPlayer != null && (otherPlayer.m_activeCharacter.m_tradingWith != null) && (otherPlayer.m_activeCharacter.m_tradingWith == player))
            {
                Program.Display("Received an InitiateTrade message from players already trading sent by " + player.m_activeCharacter.Name + " to " + otherPlayer.m_activeCharacter.Name);
                return;
            }

            //you can't trade with someone who is not there, 
            //or trading with someone else,
            //or either is awaiting a request, unless this is a conformation
            bool otherExpectingTrade = false;
            bool playerExpectingTrade = false;
            bool targetCanAcceptTrade = false;
            //can they accept this trade request
            /* if (otherCharacter != null&&//they must exist
                 (otherCharacter.m_tradingWith == null)&&//they must not yet be trading with anyone
                     ((otherCharacter.CanTakeRequest() == true && isReply == false)||//they must not be expecting anything to get sent a request
                     (otherCharacter.CanTakeRequest() == false && isReply == true && otherCharacter.CurrentRequest != null && 
                     otherCharacter.CurrentRequest.IsRequestFor(player.m_activeCharacter.m_character_id, PendingRequest.REQUEST_TYPE.RT_TRADE) == true))//they must be expecting this request if it is a reply
                 )
             {
                 targetCanAcceptTrade = true;   
             }*/

            if (otherCharacter != null &&//they must exist
                (otherCharacter.m_tradingWith == null))//they must not yet be trading with anyone
            {
                //they must not be expecting anything to get sent a request
                if ((otherCharacter.CanTakeRequest() == true || (otherCharacter.CurrentRequest != null && otherCharacter.CurrentRequest.IsRequestFor(player.m_activeCharacter.m_character_id, PendingRequest.REQUEST_TYPE.RT_TRADE) == true)) && isReply == false)
                {
                    if (otherCharacter.CurrentRequest != null && otherCharacter.CurrentRequest.IsRequestFor(player.m_activeCharacter.m_character_id, PendingRequest.REQUEST_TYPE.RT_TRADE) == true)
                    {
                        otherExpectingTrade = true;
                    }
                    else
                    {
                        targetCanAcceptTrade = true;
                    }
                }
                //they must be expecting this request if it is a reply
                else if (otherCharacter.CanTakeRequest() == false && isReply == true && otherCharacter.CurrentRequest != null &&
                    otherCharacter.CurrentRequest.IsRequestFor(player.m_activeCharacter.m_character_id, PendingRequest.REQUEST_TYPE.RT_TRADE) == true)
                {
                    targetCanAcceptTrade = true;
                }
            }
            //can the player accept this trade request
            bool playerCanAcceptTrade = false;
            if (otherCharacter != null &&//they must exist
                (playersCharacter.m_tradingWith == null) /*&&//they must not yet be trading with anyone
                    ((playersCharacter.CanTakeRequest() == true && isReply == false) ||//they must not be expecting anything to get sent a request
                    (playersCharacter.CanTakeRequest() == false && isReply == true && playersCharacter.CurrentRequest != null &&
                    playersCharacter.CurrentRequest.IsRequestFor(otherCharacter.m_character_id, PendingRequest.REQUEST_TYPE.RT_TRADE) == true)*/)//they must be expecting this request if it is a reply
            {
                if ((playersCharacter.CanTakeRequest() == true || (playersCharacter.CurrentRequest != null && playersCharacter.CurrentRequest.IsRequestFor(otherCharacter.m_character_id, PendingRequest.REQUEST_TYPE.RT_TRADE) == true)) && isReply == false)//they must not be expecting anything to get sent a request
                {
                    if (playersCharacter.CurrentRequest != null && playersCharacter.CurrentRequest.IsRequestFor(otherCharacter.m_character_id, PendingRequest.REQUEST_TYPE.RT_TRADE) == true)
                    {
                        playerExpectingTrade = true;
                    }
                    else
                    {
                        playerCanAcceptTrade = true;
                    }
                }
                else if ((playersCharacter.CanTakeRequest() == false && isReply == true && playersCharacter.CurrentRequest != null &&
                    playersCharacter.CurrentRequest.IsRequestFor(otherCharacter.m_character_id, PendingRequest.REQUEST_TYPE.RT_TRADE) == true))
                {
                    playerCanAcceptTrade = true;
                }
            }

            //did they fulfill the conditions
            if (targetCanAcceptTrade == false || playerCanAcceptTrade == false)
            //if (otherPlayer == null || ((otherPlayer.m_activeCharacter.m_tradingWith != null) && (otherPlayer.m_activeCharacter.m_tradingWith != player)))
            {
                //if not send a cancel out
                /* NetOutgoingMessage outmsg = m_server.CreateMessage();
                // outmsg.WriteVariableUInt32((uint)NetworkCommandType.OtherCancelTrade);
                 outmsg.WriteVariableUInt32((uint)NetworkCommandType.TradeMessage);
                 outmsg.WriteVariableInt32((int)TRADE_MESSAGE.TM_OtherCancelTrade);
                 outmsg.WriteVariableInt32(otherplayerId);
                 SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.TradeMessage);//was OtherCancelTrade
                 */
                //if this is actually just a duplicate, tell the sender
                if (playerExpectingTrade == true && otherExpectingTrade == true)
                {
					string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.THEY_ALREADY_ASK_PLAYER_TRADE);
					sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.TRADE);
				}
                //otherwise stop this trade
                else
                {
                    string playerTradingDataString = " " + player.m_activeCharacter.Name + "gold = " + player.m_activeCharacter.m_inventory.m_coins;
                    SendOtherCancelTrade(player, otherplayerId);
                    if (otherPlayer == null)
                    {
						string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.UNABLE_TRADE_PLAYER_NOT_FOUND);
						sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.TRADE);
						Program.Display(player.m_activeCharacter.m_name + " failed to start trade, can't find other player" + playerTradingDataString);
                    }
                    else if (otherPlayer.m_activeCharacter.m_tradingWith != null)
                    {
						string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.UNABLE_TRADE_OTHER_BUSY);
						locText = String.Format(locText, otherCharacter.Name);
						sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.TRADE);
						Program.Display(player.m_activeCharacter.m_name + " failed to start trade " + otherPlayer.m_activeCharacter.m_name + " already trading with " + otherPlayer.m_activeCharacter.m_tradingWith.m_activeCharacter.m_name + playerTradingDataString);
                    }
                    else
                    {
                        if (otherCharacter.CurrentRequest != null)
                        {
							string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.UNABLE_TRADE_OTHER_BUSY);
							locText = String.Format(locText, otherCharacter.Name);
							sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.TRADE);					
							Program.Display(player.m_activeCharacter.m_name + " failed to start trade, " + otherCharacter.Name + " already has a pending request");
                        }
                        else if (playersCharacter.CurrentRequest != null)
                        {
							string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.UNABLE_TRADE_PLAYER_BUSY);
							sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.TRADE);
							Program.Display(player.m_activeCharacter.m_name + " failed to start trade, " + playersCharacter.Name + " already has a pending request");

                        }
                        else if (otherCharacter.CanTakeRequest() == false)
                        {
							string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.UNABLE_TRADE_OTHER_BUSY);
							locText = String.Format(locText, otherCharacter.Name);
							sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.TRADE);
							Program.Display(player.m_activeCharacter.m_name + " failed to start trade, " + otherCharacter.Name + " is busy");
                        }
                        else if (playersCharacter.CanTakeRequest() == false)
                        {
							string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.UNABLE_TRADE_PLAYER_BUSY);
							sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.TRADE);
							Program.Display(player.m_activeCharacter.m_name + " failed to start trade, " + playersCharacter.Name + " is busy");
                        }
                    }
                }
                return;
            }
            else
            {
                //if it's a reply they must have the correct pending request type
                if (player.m_activeCharacter.HasBlockedCharacter((int)otherPlayer.m_activeCharacter.m_character_id) == true)
                {
					string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.UNABLE_TRADE_PLAYER_BLOCKED);
					locText = String.Format(locText, otherPlayer.m_activeCharacter.m_name);
					sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.TRADE);
				}
                else if (otherPlayer.m_activeCharacter.HasBlockedCharacter((int)player.m_activeCharacter.m_character_id) == false)
                {
                    string tradeStatus = " is requesting a trade with ";
                    if (isReply)
                    {
                        tradeStatus = " is accepting a trade with ";
                        player.m_activeCharacter.initTrade(otherPlayer);
                        otherPlayer.m_activeCharacter.initTrade(player);
                        player.m_activeCharacter.m_isTradeInitator = false;
                        otherPlayer.m_activeCharacter.m_isTradeInitator = true;
                    }
                    else
                    {
                        //get them to remember that they are waiting for trade conformation
                        playersCharacter.CurrentRequest = new PendingRequest(otherPlayer, PendingRequest.REQUEST_TYPE.RT_TRADE, PendingRequest.REQUEST_STATUS.RS_AWAITING_REPLY, 30);
                        otherCharacter.CurrentRequest = new PendingRequest(player, PendingRequest.REQUEST_TYPE.RT_TRADE, PendingRequest.REQUEST_STATUS.RS_AWAITING_REPLY, 30);
                    }
                    NetOutgoingMessage outmsg = m_server.CreateMessage();
                    //outmsg.WriteVariableUInt32((uint)NetworkCommandType.OtherInitiateTrade);
                    outmsg.WriteVariableUInt32((uint)NetworkCommandType.TradeMessage);
                    outmsg.WriteVariableInt32((int)TRADE_MESSAGE.TM_OtherInitiateTrade);
                    outmsg.WriteVariableInt32((int)player.m_activeCharacter.m_character_id);
                    if (isReply)
                    {
                        outmsg.Write((byte)1);
                    }
                    else
                    {
                        outmsg.Write((byte)0);
                    }
                    SendMessage(outmsg, otherPlayer.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.TradeMessage);//was OtherCancelTrade

                    //tell them to open their trade windows
                    if (isReply == true)
                    {
                        SendOpenTradeWindow(otherPlayer, (int)player.m_activeCharacter.m_character_id);
                        SendOpenTradeWindow(player, (int)otherPlayer.m_activeCharacter.m_character_id);
                    }
                    //Program.Display(player.m_activeCharacter.m_name + " starting trade with " + otherPlayer.m_activeCharacter.m_name);
                    Program.Display(player.m_activeCharacter.m_name + tradeStatus + otherPlayer.m_activeCharacter.m_name + "(" + player.m_activeCharacter.m_name + " gold = " + player.m_activeCharacter.m_inventory.m_coins + ")" + "(" + otherPlayer.m_activeCharacter.m_name + " gold = " + otherPlayer.m_activeCharacter.m_inventory.m_coins + ")");
                }
                else if (otherPlayer.m_activeCharacter.HasBlockedCharacter((int)player.m_activeCharacter.m_character_id))
                {
					string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.UNABLE_TRADE_OTHER_BUSY);
					locText = String.Format(locText, otherCharacter.Name);
					sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.TRADE);
				}

            }
        }

        #endregion

        private void processLogout(Player player)
        {
            if (player != null && player.m_activeCharacter != null)
            {
                player.m_activeCharacter.m_reasonForExit = "Logout";
                player.m_activeCharacter.CharacterRequestedLogOut();
            }
            disconnect(player, true, String.Empty);
        }
        void ProcessClearGuestAccount(NetIncomingMessage msg, Player player)
        {
                string userName = msg.ReadString().ToLower();

				Program.Display("received login message " + userName);

                string password = msg.ReadString();

            //ClearGuestAccountTask newTask = new ClearGuestAccountTask(userName, password);
            // lock(m_)
        }

        private void processPlayerMove(NetIncomingMessage msg, NetOutgoingMessage outmsg, Player player)
        {
            if (player.m_activeCharacter != null)
                player.m_activeCharacter.m_zone.ReadPositionUpdate(msg, m_server, player);
        }

        private void ProcessCharacterStuck(Player player)
        {
            int characterID = player.m_lastSelectedCharacter;
            PlayerSpawnPoint spawnPoint = getZone(93).GetPlayerSpawnPointForID(9935);
            Vector3 position = spawnPoint.RandomRespawnPosition;

            Program.processor.m_worldDB.runCommandSync("update character_details set zone=93 ,xpos=" + position.X + ",ypos=" + position.Y + ",zpos=" + position.Z + ",yangle=" + spawnPoint.Angle + " where character_id=" + characterID);
        }

        private void ProcessPlayerBusy(NetIncomingMessage msg, Player player)
        {
            bool status = (msg.ReadByte() == 1);
            if(player != null && player.m_activeCharacter != null)
                player.m_activeCharacter.PlayerIsBusy = status;
        }

        private void ProcessUpdateCharacterOptionsUpdate(NetIncomingMessage msg, Player player)
        {
            byte headgear = msg.ReadByte();
            byte fashion = msg.ReadByte();
            if (player.m_activeCharacter != null)
            {
                player.m_activeCharacter.ShowHeadgear = (headgear == 1);
                player.m_activeCharacter.ShowFashion = (fashion == 1);
                if (fashion == 1)
                {
                    player.m_activeCharacter.m_inventory.VerifyFashion();
                }
                player.m_activeCharacter.SaveCharacterPreferences();

            }
        }

        internal void SendOpenWebPage(string theUrl, string title, bool stretches, Player player)
        {
            NetOutgoingMessage outmsg = m_server.CreateMessage();

            //outmsg.WriteVariableUInt32((uint)NetworkCommandType.OtherCancelTrade);
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.OpenWebPage);

            outmsg.Write(theUrl);
            outmsg.Write(title);
            if (stretches == true)
            {
                outmsg.Write((byte)1);
            }
            else
            {
                outmsg.Write((byte)0);
            }

            SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.OpenWebPage);
        }

        #region skills

        private void ProcessPlayerBuySkill(NetIncomingMessage msg, Player player)
        {
            SKILL_TYPE skillID = (SKILL_TYPE)msg.ReadVariableInt32();
            //what lvl should it be going up to,
            //to prevent lagged players continually spending points accidentally
            int skillLevel = msg.ReadVariableInt32(); ;
            //try to buy the skill
            if (player.m_activeCharacter != null)
            {
                player.m_activeCharacter.UpgradeSkillLevel(skillID, skillLevel);
            }


        }

		/// <summary>
		/// Update the players skills to enable/disable this skillID
		/// </summary>
		/// <param name="skillId"></param>
		/// <param name="on"></param>
		/// <param name="player"></param>
		public void SendPlayerCompanionSkillToggled(int skillId, bool on, Player player)
		{
			
			NetOutgoingMessage outmsg = m_server.CreateMessage();
			outmsg.WriteVariableUInt32((uint)NetworkCommandType.CompanionSkillToggle);
			outmsg.Write(skillId);
			outmsg.Write(on);
						
			SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.CompanionSkillToggle);
		}

        public void SendPlayerMountSkillToggled(int skillId, bool on, Player player)
        {

            NetOutgoingMessage outmsg = m_server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.MountSkillToggle);
            outmsg.Write(skillId);
            outmsg.Write(on);

            SendMessage(outmsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.MountSkillToggle);
        }
        #endregion 

        #region friends
        internal void sendSocialLists(Player player)
        {
            if (player.m_activeCharacter != null)
            {
                if (player.m_activeCharacter.CharacterParty != null)
                {
                    player.m_activeCharacter.CharacterParty.SendNewPartyConfiguration();
                }
                if (player.m_activeCharacter.CharactersClan != null)
                {
                    player.m_activeCharacter.CharactersClan.SendClanListToPlayer(player);
                }
            }

            sendActiveCharactersFriendList(player);

            sendActiveCharactersBlockedList(player, false);
        }
        private void processClientFriendRequest(NetIncomingMessage msg, Player player)
        {
            string sendersCharacterName = null;
            int characterID = msg.ReadVariableInt32();
            //string requestRecipient = msg.ReadString();
            if (player.m_activeCharacter == null)
            {
                return; //error, requesting player not signed in
            }
            else
            {
                sendersCharacterName = player.m_activeCharacter.m_name;
            }


            FriendTemplate friend = player.m_activeCharacter.GetFriendForID(characterID);
            if (friend != null)
            {
				string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.ALREADY_FRIEND);
				locText = String.Format(locText, friend.CharacterName);
				sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.FRIENDS);
				//send error report,this character is already a friend
				return;
            }
            Player recievingPlayer = getPlayerFromActiveCharacterId(characterID);
            if (recievingPlayer == null)
            {
				string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.OTHER_NOT_ONLINE);
				//send error report,player is not online
				sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.FRIENDS);
				return;
            }
            if ((player.m_activeCharacter != null) && (recievingPlayer.m_activeCharacter.HasBlockedCharacter((int)player.m_activeCharacter.m_character_id) == false))
            {
                //are they busy
                if (recievingPlayer.m_activeCharacter.CanTakeRequest() == true)
                {
                    writeServerFriendRequest(sendersCharacterName, (int)player.m_activeCharacter.m_character_id, recievingPlayer);
                    if (Program.m_LogAnalytics)
                    {
                        AnalyticsMain logAnalytics = new AnalyticsMain(false);
                        logAnalytics.LogInviteSent(player, recievingPlayer, "FRIEND", "-1");
                    }

                }
                else
                {
					sendSystemMessage(player.m_activeCharacter.GetPlayerBusyString(), player, false, SYSTEM_MESSAGE_TYPE.FRIENDS);
                }
            }

        }
        private void processClientFriendReply(NetIncomingMessage msg, Player player)
        {
            int characterID = msg.ReadVariableInt32();
            //string replyRecipient = msg.ReadString();

            int replyType = msg.ReadVariableInt32();

            string sendersCharacterName = null;
            if (player.m_activeCharacter == null)
            {
                return; //error, requesting player not signed in
            }
            else
            {
                sendersCharacterName = player.m_activeCharacter.m_name;
            }

            int character_id = characterID;

            Player recievingPlayer = getPlayerFromActiveCharacterId(character_id);
            if (recievingPlayer == null)
            {
                //send error report,player is not online
                //query.Close();
                return;
            }
            if (replyType == (int)HW_FRIEND_REPLY.HW_FRIEND_REPLY_ACCEPT)
            {

                player.m_activeCharacter.AddFriend((int)recievingPlayer.m_activeCharacter.m_character_id, recievingPlayer.m_activeCharacter);
                recievingPlayer.m_activeCharacter.AddFriend((int)player.m_activeCharacter.m_character_id, player.m_activeCharacter);
                sendActiveCharactersFriendList(player);
                sendActiveCharactersFriendList(recievingPlayer);
            }

            if (Program.m_LogAnalytics)
            {
                bool accepted = (replyType == (int)HW_FRIEND_REPLY.HW_FRIEND_REPLY_ACCEPT);
                AnalyticsMain logAnalytics = new AnalyticsMain(false);
                logAnalytics.LogInviteRecieved(player, recievingPlayer.m_account_id.ToString(), "FRIEND", "-1", accepted);
            }
            writeServerFriendReply(sendersCharacterName, replyType, recievingPlayer);
            // query.Close();
        }
        private void writeServerFriendRequest(string friendRequestSender, int senderID, Player player)
        {
            NetOutgoingMessage freindRequest = Program.Server.CreateMessage();
            freindRequest.WriteVariableUInt32((uint)NetworkCommandType.ServerFriendRequest);
            freindRequest.WriteVariableInt32(senderID);
            freindRequest.Write(friendRequestSender);
            SendMessage(freindRequest, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.ServerFriendRequest);

        }
        private void writeServerFriendReply(string friendReplySender, int reply, Player player)
        {
            NetOutgoingMessage friendRequest = Program.Server.CreateMessage();
            friendRequest.WriteVariableUInt32((uint)NetworkCommandType.ServerFreindReply);
            friendRequest.Write(friendReplySender);
            friendRequest.WriteVariableInt32(reply);
            SendMessage(friendRequest, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.ServerFreindReply);
        }
        internal void sendActiveCharactersFriendList(Player player)
        {
            if (player.m_activeCharacter == null)
            {
                return;
            }
            NetOutgoingMessage friendList = Program.Server.CreateMessage();
            friendList.WriteVariableUInt32((uint)NetworkCommandType.FriendList);
            player.m_activeCharacter.WriteFriendsListToMessage(friendList);
            SendMessage(friendList, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.FriendList);
        }
        internal void sendActiveCharactersBlockedList(Player player, bool delayed)
        {
            if (player.m_activeCharacter == null)
            {
                return;
            }
            NetOutgoingMessage blockedList = Program.Server.CreateMessage();
            blockedList.WriteVariableUInt32((uint)NetworkCommandType.BlockedList);

            player.m_activeCharacter.WriteBlockedListToMessage(blockedList);

            if (delayed == false)
            {
                SendMessage(blockedList, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.BlockedList);
            }
            else
            {
                DelayedMessageDescriptor desc = new DelayedMessageDescriptor(blockedList, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Chat, NetworkCommandType.GeneralChat, null);
                lock (m_delayedMessages)
                {
                    m_delayedMessages.Enqueue(desc);
                }
            }
        }

        void processRemoveFriend(NetIncomingMessage msg, Player player)
        {
            //friend name
            int friendID = msg.ReadVariableInt32();
            //string friendName = msg.ReadString();
            int character_id = friendID;

            if (player.m_activeCharacter.HasFriend(friendID) == false)
            {
                //send error report,this character is not a friend
                //query.Close();
                return;
            }
            Player recievingPlayer = getPlayerFromActiveCharacterId(character_id);
            if (recievingPlayer == null)
            {
                m_worldDB.runCommandSync("delete from friend_list where character_id=" + friendID + " and other_character_id=" + player.m_activeCharacter.m_character_id);
                player.m_activeCharacter.RemoveFriend(character_id);
                sendActiveCharactersFriendList(player);
                return;
            }

            player.m_activeCharacter.RemoveFriend(character_id);
            recievingPlayer.m_activeCharacter.RemoveFriend((int)player.m_activeCharacter.m_character_id);
            sendActiveCharactersFriendList(player);
            sendActiveCharactersFriendList(recievingPlayer);
            //query.Close();
            return;
        }
        #endregion
        #region blocking

        private void processBlockCharacter(NetIncomingMessage msg, Player player)
        {
            int characterID = msg.ReadVariableInt32();

            if (player.m_activeCharacter == null)
            {
                return;
            }
            Player playerToBlock = getPlayerFromActiveCharacterId(characterID);
            if (playerToBlock == null)
            {
                return;
            }

            player.m_activeCharacter.AddToBlockedList(characterID, playerToBlock.m_activeCharacter.m_name);

        }
        private void processUnblockCharacter(NetIncomingMessage msg, Player player)
        {
            int characterID = msg.ReadVariableInt32();

            if (player.m_activeCharacter == null)
            {
                return;
            }
            player.m_activeCharacter.RemoveFromBlockedList(characterID);
        }
        #endregion
        private void processLikedOnSocialNetwork(NetIncomingMessage msg, Player player)
        {
            int iNetworkType = msg.ReadVariableInt32();
            string userID = msg.ReadString();

            int likereward = 5;
            SOCIAL_NETWORKING_TYPE networkType = (SOCIAL_NETWORKING_TYPE)iNetworkType;
            if (networkType == SOCIAL_NETWORKING_TYPE.SNT_FACEBOOK)
            {
                bool platAlreadyAwarded = false;
                //has the player got a reward

                if (player.m_likedOnFacebook == true)
                {
                    platAlreadyAwarded = true;
                }
                else
                {
					//if not has this facebook ID already been used
					//SqlQuery query = new SqlQuery(m_universalHubDB, "select * from facebook_likes where facebook_id='" + userID + "'");

					List<MySqlParameter> sqlParams = new List<MySqlParameter>();
					sqlParams.Add(new MySqlParameter("@facebook_id", userID));

					SqlQuery query = new SqlQuery(m_universalHubDB, "select * from facebook_likes where facebook_id=@facebook_id", sqlParams.ToArray());

					if (query.HasRows)
                    {
                        platAlreadyAwarded = true;
                        //this facebook account has already been used
                    }

                    query.Close();
                }
                //award the platinum
                if (platAlreadyAwarded == false)
                {
                    int rewardAmount = likereward;
                    //record the event
                    player.m_platinum += rewardAmount;
                    player.SavePlatinum(0, 0);
                    DateTime serverDate = DateTime.Now;
					//Program.processor.m_universalHubDB.runCommandSync("insert into facebook_likes (facebook_id,account_id,platinum_reward,server_time) values ('" + userID + "'," + player.m_account_id + "," + rewardAmount + ",'" + serverDate.ToString("yyyy-MM-dd HH:mm:ss") + "')");

					List<MySqlParameter> sqlParams = new List<MySqlParameter>();
					sqlParams.Add(new MySqlParameter("@facebook_id", userID));
					sqlParams.Add(new MySqlParameter("@account_id", player.m_account_id));
					sqlParams.Add(new MySqlParameter("@platinum_reward", rewardAmount));
					sqlParams.Add(new MySqlParameter("@server_time", serverDate.ToString("yyyy-MM-dd HH:mm:ss")));

					Program.processor.m_universalHubDB.runCommandSyncWithParams("insert into facebook_likes (facebook_id,account_id,platinum_reward,server_time) values (@facebook_id, @account_id, @platinum_reward, @server_time)", sqlParams.ToArray());

					player.m_likedOnFacebook = true;
                    Program.processor.m_universalHubDB.runCommandSync("update account_details set liked_on_facebook = 1 where account_id=" + player.m_account_id);
                    Program.Display("Awarded " + rewardAmount + " platinum to user " + player.m_account_id + " for liking with facebook ID " + userID);

					string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.THANK_LIKING_FACEBOOK);
					locText = String.Format(locText, rewardAmount);
					sendSystemMessage(locText, player, true, SYSTEM_MESSAGE_TYPE.POPUP);

					PremiumShop.SendPlatinumConfirmation(player, 1, String.Empty, String.Empty);
                    if (Program.m_LogAnalytics)
                    {
                        AnalyticsMain logAnalytics = new AnalyticsMain(false);
                        logAnalytics.LogSocialString(player, Analytics.Social.Social_Action.LIKED.ToString(), rewardAmount);
                    }
                }
            }
            else if (networkType == SOCIAL_NETWORKING_TYPE.SNT_TWITTER)
            {
                //register twitter Data
                bool platAlreadyAwarded = false;
                //has the player got a reward

                if (player.m_followedOnTwitter == true)
                {
                    platAlreadyAwarded = true;
                }
                else
                {
					//if not has this facebook ID already been used
					//SqlQuery query = new SqlQuery(m_universalHubDB, "select * from twitter_follows where twitter_id='" + userID + "'");

					List<MySqlParameter> sqlParams = new List<MySqlParameter>();
					sqlParams.Add(new MySqlParameter("@twitter_id", userID));

					SqlQuery query = new SqlQuery(m_universalHubDB, "select * from twitter_follows where twitter_id=@twitter_id", sqlParams.ToArray());

					if (query.HasRows)
                    {
                        platAlreadyAwarded = true;
                        //this facebook account has already been used
                    }

                    query.Close();
                }
                //award the platinum
                if (platAlreadyAwarded == false)
                {
                    int rewardAmount = likereward;
                    //record the event
                    player.m_platinum += rewardAmount;
                    player.SavePlatinum(0, 0);
                    DateTime serverDate = DateTime.Now;
					//Program.processor.m_universalHubDB.runCommandSync("insert into twitter_follows (twitter_id,account_id,platinum_reward,server_time) values ('" + userID + "'," + player.m_account_id + "," + rewardAmount + ",'" + serverDate.ToString("yyyy-MM-dd HH:mm:ss") + "')");

					List<MySqlParameter> sqlParams = new List<MySqlParameter>();
					sqlParams.Add(new MySqlParameter("@twitter_id", userID));
					sqlParams.Add(new MySqlParameter("@account_id", player.m_account_id));
					sqlParams.Add(new MySqlParameter("@platinum_reward", rewardAmount));
					sqlParams.Add(new MySqlParameter("@server_time", serverDate.ToString("yyyy-MM-dd HH:mm:ss")));

					Program.processor.m_universalHubDB.runCommandSyncWithParams("insert into twitter_follows (twitter_id,account_id,platinum_reward,server_time) values (@twitter_id, @account_id, @platinum_reward, @server_time)", sqlParams.ToArray());

					player.m_followedOnTwitter = true;
                    Program.processor.m_universalHubDB.runCommandSync("update account_details set followed_on_twitter = 1 where account_id=" + player.m_account_id);
                    Program.Display("Awarded " + rewardAmount + " platinum to user " + player.m_account_id + " for following with twitter ID " + userID);

					string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.THANK_FOLLOWING_TWITTER);
					locText = String.Format(locText, rewardAmount);
					sendSystemMessage(locText, player, true, SYSTEM_MESSAGE_TYPE.POPUP);

					PremiumShop.SendPlatinumConfirmation(player, 1, String.Empty, String.Empty);
                    if (Program.m_LogAnalytics)
                    {
                        AnalyticsMain logAnalytics = new AnalyticsMain(false);
                        logAnalytics.LogSocialString(player, Analytics.Social.Social_Action.FOLLOWED.ToString(), rewardAmount);
                    }
                }
            }
        }

        void processSentPostViaSocialNetwork(NetIncomingMessage msg, Player player)
        {
            int networkType = msg.ReadVariableInt32();
            string userID = msg.ReadString();
            int postype = msg.ReadVariableInt32();
            int postValue = msg.ReadVariableInt32();
            int numFriends = msg.ReadVariableInt32();

            int accountID = (int)player.m_account_id;
            int characterID = -1;
            if (player.m_activeCharacter != null)
            {
                characterID = (int)player.m_activeCharacter.m_character_id;
            }
            DateTime serverDate = DateTime.Now;
			//post_id, account_id, character_id, social_networking_type,social_networking_id *
			//post_type, post_value,num_friends, server_time
			/*Program.processor.m_universalHubDB.runCommandSync("insert into social_networking_posts (social_networking_id,social_networking_type,account_id,character_id,post_type,post_value,num_friends,server_time) values ('" +
                userID + "'," + networkType + "," + player.m_account_id + "," + characterID + "," + postype + "," + postValue + "," + numFriends + ",'" + serverDate.ToString("yyyy-MM-dd HH:mm:ss") + "')");*/

			List<MySqlParameter> sqlParams = new List<MySqlParameter>();
			sqlParams.Add(new MySqlParameter("@social_networking_id", userID));
			sqlParams.Add(new MySqlParameter("@social_networking_type", networkType));
			sqlParams.Add(new MySqlParameter("@account_id", player.m_account_id));
			sqlParams.Add(new MySqlParameter("@character_id", characterID));
			sqlParams.Add(new MySqlParameter("@post_type", postype));
			sqlParams.Add(new MySqlParameter("@post_value", postValue));
			sqlParams.Add(new MySqlParameter("@num_friends", numFriends));
			sqlParams.Add(new MySqlParameter("@server_time", serverDate.ToString("yyyy-MM-dd HH:mm:ss")));

			Program.processor.m_universalHubDB.runCommandSyncWithParams("insert into social_networking_posts (social_networking_id,social_networking_type,account_id,character_id,post_type,post_value,num_friends,server_time) " +
				"values (@social_networking_id, @social_networking_type, @account_id, @character_id, @post_type, @post_value, @num_friends, @server_time)", sqlParams.ToArray());

			//only reward for a level post
			if (postype == (int)SOCIAL_NETWORKING_POST_TYPE.SNPT_LEVEL)
            {
                //check if they should get a reward
                int oldfriends = 0;
                PlatinumRewards.REWARD_TYPES currentReward = PlatinumRewards.REWARD_TYPES.NONE;
                bool shouldBeRewarded = true;
                int friendsPerPlat = 100000000;
                int previousReward = 0;
                string socialEventType = String.Empty;
                if (networkType == (int)SOCIAL_NETWORKING_TYPE.SNT_FACEBOOK)
                {

                    oldfriends = player.m_highestFacebookFriendsRewarded;
                    currentReward = PlatinumRewards.REWARD_TYPES.FACEBOOK_POSTS;
                    previousReward = player.m_currentRewardsForFacebookPosts;
                    friendsPerPlat = 50;
                    socialEventType = Analytics.Social.Social_Action.WALL_POST.ToString();
                    /*if (player.m_likedOnFacebook == true)
                    {
                        shouldBeRewarded = true;
                    }*/
                }
                else if (networkType == (int)SOCIAL_NETWORKING_TYPE.SNT_TWITTER)
                {
                    oldfriends = player.m_highestTwitterFollowersRewarded;
                    currentReward = PlatinumRewards.REWARD_TYPES.TWITTER_TWEET;
                    previousReward = player.m_currentRewardsForTwitterTweets;
                    friendsPerPlat = 100;
                    socialEventType = Analytics.Social.Social_Action.TWEET.ToString();
                    /*if (player.m_followedOnTwitter == true)
                    {
                        shouldBeRewarded = true;
                    }*/
                }
                int platinumDifference = 0;
                //are they due for a new reward
                if (numFriends >= oldfriends + friendsPerPlat && shouldBeRewarded == true)
                {
                    //how much should they have,
                    int currentAwardLevel = numFriends / friendsPerPlat;
                    int minFriendsNeeded = currentAwardLevel * friendsPerPlat;

                    //how much have they received
                    if (previousReward < currentAwardLevel)
                    {
						//check the social network account has not got a reward greater than this
						//SqlQuery query = new SqlQuery(Program.processor.m_universalHubDB, "select * from platinum_rewards where reward_type =" + (int)currentReward + " and reward_string ='" + userID + "'");

						sqlParams.Clear();
						sqlParams.Add(new MySqlParameter("@reward_type", (int)currentReward));
						sqlParams.Add(new MySqlParameter("@reward_string", userID));

						SqlQuery query = new SqlQuery(Program.processor.m_universalHubDB, "select * from platinum_rewards where reward_type = @reward_type and reward_string = @reward_string", sqlParams.ToArray());

						if (query.HasRows)
                        {
                            while ((query.Read()) && shouldBeRewarded == true)
                            {

                                int rewardValue = query.GetInt32("reward_value");

                                if (rewardValue >= minFriendsNeeded)
                                {
                                    shouldBeRewarded = false;
                                }
                            }
                        }
                        query.Close();

                        if (shouldBeRewarded == true)
                        {
                            platinumDifference = currentAwardLevel - previousReward;
                            player.m_platinum += platinumDifference;
                            player.SavePlatinum(0, 0);
                            PlatinumRewards newReward = PlatinumRewards.SaveReward(currentReward, minFriendsNeeded, platinumDifference, player.m_account_id, (uint)characterID, "For posting with ID " + userID, userID);

                            player.AddToRewardsList(newReward);
                            PremiumShop.SendPlatinumConfirmation(player, 1, String.Empty, String.Empty);

                            if (networkType == (int)SOCIAL_NETWORKING_TYPE.SNT_FACEBOOK)
                            {
								string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.THANK_SPREADING_FACEBOOK);
								locText = String.Format(locText, platinumDifference);
								sendSystemMessage(locText, player, true, SYSTEM_MESSAGE_TYPE.POPUP);
								Program.Display("Awarded " + platinumDifference + " platinum to user " + player.m_account_id + " for posting with facebook ID " + userID + " with " + numFriends + " friends");
                                player.m_highestFacebookFriendsRewarded = minFriendsNeeded;
                                player.m_currentRewardsForFacebookPosts = currentAwardLevel;

                            }
                            else if (networkType == (int)SOCIAL_NETWORKING_TYPE.SNT_TWITTER)
                            {
								string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.THANK_SPEADING_TWITTER);
								locText = String.Format(locText, platinumDifference);
								sendSystemMessage(locText, player, true, SYSTEM_MESSAGE_TYPE.POPUP);
								Program.Display("Awarded " + platinumDifference + " platinum to user " + player.m_account_id + " for posting with twitter ID " + userID + " with " + numFriends + " friends");
                                player.m_highestTwitterFollowersRewarded = minFriendsNeeded;
                                player.m_currentRewardsForTwitterTweets = currentAwardLevel;
                            }
                        }
                    }
                }

                if (Program.m_LogAnalytics)
                {
                    AnalyticsMain logAnalytics = new AnalyticsMain(false);
                    logAnalytics.LogSocialString(player, socialEventType, platinumDifference);
                }
            }
        }

        void processRessurectRequest(NetIncomingMessage msg, Player player)
        {
            bool useItem = (msg.ReadByte() == 1);
            int itemID = msg.ReadVariableInt32();

            if (player.m_activeCharacter != null && player.m_activeCharacter.m_zone != null)
            {
                player.m_activeCharacter.m_zone.ResWithOptions(player, useItem, itemID);
            }
        }

        private void processStartGame(NetIncomingMessage msg, NetOutgoingMessage outmsg, Player player)
        {
            // outmsg.WriteVariableUInt32((uint)NetworkCommandType.StartGameReply);
            uint characterid = msg.ReadVariableUInt32();
            string charactername = msg.ReadString();
            int tokenLen = msg.ReadVariableInt32();

			DataValidator.JustCheckCharacterName(charactername);

            byte[] notificationToken = null;

            if (tokenLen > 0)
            {
                notificationToken = msg.ReadBytes(tokenLen);
            }
            int notificationTypes = msg.ReadVariableInt32();
            string uuid = msg.ReadString();
            player.m_notificationToken = String.Empty;
            if (notificationToken != null)
            {
                player.m_notificationToken = Utilities.baToHex(notificationToken);
            }
            player.m_notificationType = notificationTypes;
            player.m_notificationDevice = uuid;
            StartGameTask task = new StartGameTask(player, characterid, charactername);
            lock (m_backgroundTasks)
            {
                m_backgroundTasks.Enqueue(task);
            }
        }

        internal List<int> GetKnownZonesForCharacter(Character character)
        {
            List<int> knownZones = new List<int>();

            for (int i = 0; i < m_zones.Count; i++)
            {
                bool zoneKnown = m_zones[i].HasBeenToZone(character);
                if (zoneKnown)
                {
                    knownZones.Add(m_zones[i].m_zone_id);
                }
            }
            return knownZones;
        }

        internal void ReloadAdminsList()
        {
            m_adminsListCloaked.Clear();
            m_adminsListUncloaked.Clear();

            try
            {
                SqlQuery adminListQuery = new SqlQuery(m_universalHubDB, "select * from admin_list where cloaked=1", true, true);
                if (adminListQuery.HasRows == true)
                {
                    while (adminListQuery.Read())
                    {
                        if( adminListQuery.GetInt32("cloaked") == 1)
                            m_adminsListCloaked.Add(adminListQuery.GetInt32("account_id"));
                        else
                            m_adminsListUncloaked.Add(adminListQuery.GetInt32("account_id"));
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Expected an admin_list table in hub database, but it is missing.\n\n" + e.ToString(), "Database Schema Error",
                    MessageBoxButtons.OK);
            }
        }

        internal bool IsAdminAccount(int in_accountID)
        {
            if (m_adminsListUncloaked != null)
            {
                if (m_adminsListUncloaked.Contains(in_accountID))
                    return true;
            }

            return IsCloakedAdminAccount(in_accountID);
        }

        internal bool IsCloakedAdminAccount(int in_accountID)
        {
            if (m_adminsListCloaked == null)
                return false;

            return (m_adminsListCloaked.Contains(in_accountID));
        }

        private void processCreateCharacter(NetIncomingMessage msg, NetOutgoingMessage outmsg, Player player)
        {

            string name = msg.ReadString();
            int race_id = msg.ReadVariableInt32();
            int face_id = msg.ReadVariableInt32();
            int skin = msg.ReadVariableInt32();
            int skincol = msg.ReadVariableInt32();
            int hair_id = msg.ReadVariableInt32();
            int hair_col = msg.ReadVariableInt32();
            int face_acc = msg.ReadVariableInt32();
            int face_acc_col = msg.ReadVariableInt32();
            float scale = msg.ReadFloat();
            GENDER gender = (GENDER)msg.ReadByte();
            int class_id = msg.ReadVariableInt32();


			DataValidator.JustCheckCharacterName(name);
			DataValidator.CheckRace_Id(ref race_id);
			DataValidator.CheckModel_Scale(ref scale);
			DataValidator.CheckGender(ref gender);
			DataValidator.CheckClass_Id(ref class_id);


			//SqlQuery query = new SqlQuery(m_worldDB, "select null from character_details where name='" + name + "' and deleted=false");

			List<MySqlParameter> sqlParams = new List<MySqlParameter>();
			sqlParams.Add(new MySqlParameter("@name", name));

			SqlQuery query = new SqlQuery(m_worldDB, "select null from character_details where name=@name and deleted=false", sqlParams.ToArray());


			bool found = false;
            bool validName = false;
            if (query.HasRows)
            {
                found = true;
            }
            else
            {
                validName = ProfanityFilter.isAllowed(name);
                if (validName)
                {
                    if (name.ToLower().Contains("support") || name.ToLower().Contains("otm"))
                    {
                        validName = false;
                    }
                }
            }

            query.Close();
            
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.CreateCharacterReply);
            uint maxCharSlots = getNumberActiveSlots(player.m_account_id);
            uint usedCharSlots = getNumberActiveCharacters(player.m_account_id);
            bool failed = false;
            bool containsUsername = name.IndexOf(player.m_UserName, StringComparison.OrdinalIgnoreCase) >= 0;

			string nametaken = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.CHARACTER_NAME_UNAVAILABLE);
			if (found) //player already exists
            {
                outmsg.Write((byte)0);
                outmsg.Write(nametaken);//"This character name has already been taken");
                Program.Display("incorrect character create: name already taken: " + name);
                failed = true;
            }
            else if (validName == false)
            {
                outmsg.Write((byte)0);
                outmsg.Write(nametaken);//"This character name has already been taken");
                Program.Display("user " + player.m_account_id + " incorrect character create: invalid name: " + name);
                failed = true;
            }
            else if (usedCharSlots >= maxCharSlots)
            {
                outmsg.Write((byte)0);
				string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.MAX_CREATEED_CHARACTER_REACH);
				outmsg.Write(locText);
				Program.Display("Character Tree full: " + name);
                failed = true;
            }
            else if (containsUsername)
            {
                outmsg.Write((byte)0);
				string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.CHARACTER_NAME_NOT_CONTAIN_USERNAME);
				outmsg.Write(locText);
				Program.Display("user " + player.m_account_id + " incorrect character create: Character name is username: " + name);
                failed = true;
            }
            else
            {
                outmsg.Write((byte)1);
                CreateCharacterTask task = new CreateCharacterTask(player, (int)player.m_account_id, name, race_id, face_id, skin, skincol, hair_id, hair_col, face_acc, face_acc_col, scale, class_id, gender);
                lock (m_backgroundTasks)
                {
                    m_backgroundTasks.Enqueue(task);
                }

            }
            if (failed)
            {
                SendMessage(outmsg, msg.SenderConnection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.CreateCharacterReply);
            }
        }

        private void processRequestCharacterList(NetIncomingMessage msg, NetOutgoingMessage outmsg, Player player)
        {
            Program.Display("showing character list for: " + player.m_UserName);
            RequestCharListTask task = new RequestCharListTask(player);
            double a = NetTime.Now;
            if (player.m_activeCharacter != null && player.m_activeCharacter.m_zone != null)
            {

                player.m_activeCharacter.m_reasonForExit = "CharacterSelect";
                player.m_activeCharacter.PrepareForDisconnect();
                player.m_activeCharacter = null;
            }

            lock (m_backgroundTasks)
            {
                m_backgroundTasks.Enqueue(task);
            }
            // Program.Display("time taken=" + (NetTime.Now - a));
        }

        internal uint getNumberActiveSlots(long account_id)
        {
            int ret = 0;
            SqlQuery query = new SqlQuery(m_universalHubDB, "select used_slots,max_character_slots from account_details where account_id=" + account_id);
            if (query.HasRows)
            {
                query.Read();

                // ret = query.GetInt32("max_character_slots")-query.GetInt32("used_slots");
                ret = query.GetInt32("max_character_slots");
            }
            query.Close();
            /* query = new SqlQuery(m_worldDB, "select count(*) as charactercount from character_details where account_id=" + account_id +" and deleted=false");
             if (query.HasRows)
             {
                 query.Read();

                 ret += query.GetInt32("charactercount");

             }
             if (ret < 0)
                 ret = 0;
             query.Close();
            */
            return (uint)ret;
        }

        internal uint getNumberActiveCharacters(long account_id)
        {
            uint ret = 0;
            SqlQuery query = new SqlQuery(m_worldDB, "select count(*) as charcount from character_details where account_id=" + account_id + " and deleted=false");
            if (query.HasRows)
            {
                query.Read();
                ret = query.GetUInt32("charcount");
            }
            query.Close();

            return ret;
        }
        /*
        private int getLastSelectedCharacter(long account_id)
        {
            int ret = 0;
            SqlQuery query = new SqlQuery(m_universalHubDB, "select last_selected_character from account_details where account_id=" + account_id);
            if (query.HasRows)
            {
                query.Read();
                ret = query.GetInt32("last_selected_character");
            }
            query.Close();

            return ret;
        }
        */
        private Player getPlayerfromConnection(NetIncomingMessage msg)
        {
            Player curplayer = null;
            for (int i = 0; i < m_players.Count; i++)
            {
                Player p1 = m_players[i];
                if (p1.connection == msg.SenderConnection)
                {

                    curplayer = p1;

                    break;
                }
            }
            return curplayer;
        }

        internal Player getPlayerFromActiveCharacterId(int character_id)
        {
            for (int i = 0; i < m_players.Count; i++)
            {
                if (m_players[i].m_activeCharacter != null)
                {
                    if (character_id == m_players[i].m_activeCharacter.m_character_id)
                    {
                        return m_players[i];
                    }
                }
            }
            return null;
        }

        internal Player getPlayerFromAccountId(int account_id)
        {
            for (int i = 0; i < m_players.Count; i++)
            {
                if (m_players[i].m_account_id == account_id)
                {
                    return m_players[i];
                }
            }
            return null;
        }

        void RemoveCharacterFromAllClans(int characterID)
        {
            for (int i = 0; i < m_clanList.Count; i++)
            {
                Clan currentClan = m_clanList[i];
                if (currentClan != null)
                {
                    currentClan.RemoveCharacter(characterID);

                    if (currentClan.Leader == null)
                    {
                        m_clanList.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

        private void processDeleteCharacter(NetIncomingMessage msg, NetOutgoingMessage outmsg, Player player)
        {
            uint characterid = msg.ReadVariableUInt32();
            string charactername = msg.ReadString();

            //remove from the clan
            RemoveCharacterFromAllClans((int)characterid);
            Program.Display("deleting character " + charactername + " for: " + player.m_UserName);
            DeleteCharacterTask task = new DeleteCharacterTask(player, characterid, charactername);
            lock (m_backgroundTasks)
            {
                m_backgroundTasks.Enqueue(task);
            }
        }

        internal void removePlayer(Player player)
        {
            try
            {
                if (player.m_activeCharacter != null)
                {

                    try
                    {
                        if (player.m_activeCharacter.m_tradingWith != null)
                        {
                            Player otherplayer = player.m_activeCharacter.m_tradingWith;

                            string otherPlayerName = "Unknown Player";
                            if (otherplayer != null && otherplayer.m_activeCharacter != null)
                            {
                                otherPlayerName = otherplayer.m_activeCharacter.Name;

								string locText = Localiser.GetString(textDB, otherplayer, (int)CommandProcessorTextDB.TextID.OTHER_LOGGED_OUT);
								locText = String.Format(locText, player.m_activeCharacter.Name);
								sendSystemMessage(locText, otherplayer, false, SYSTEM_MESSAGE_TYPE.TRADE);
								SendOtherCancelTrade(otherplayer, (int)player.m_activeCharacter.m_character_id);
                                otherplayer.m_activeCharacter.cancelTrade();
                            }
                            player.m_activeCharacter.cancelTrade();
                            Program.Display("Closing Down trade on logout between " + player.m_activeCharacter.Name + " and " + otherPlayerName + " " + player.m_activeCharacter.Name + " gold = " + player.m_activeCharacter.m_inventory.m_coins);


                        }
                    }
                    catch (Exception ex)
                    {
                        Program.Display("error in remove trading with " + ex.Message + ":" + ex.StackTrace);
                    }
                    Program.Display("Disconnecting " + player.m_activeCharacter.m_name);
                    try
                    {
                        player.m_activeCharacter.PrepareForDisconnect();
                    }
                    catch (Exception ex)
                    {
                        Program.Display("error in preparing character for disconnect with " + ex.Message + ":" + ex.StackTrace);
					}			
				}
                else
                {
                    Program.Display("Disconnecting " + player.m_UserName);
                }

                if (Program.m_serverRole == Program.SERVER_ROLE.WORLD_SERVER)
                {
                    double totalPlayTime = (DateTime.Now - player.m_loggedInTime).TotalSeconds;
                    m_universalHubDB.runCommandSync("update account_details set logged_in_world=0,session_id=0,total_play_time=total_play_time+" + totalPlayTime + " where account_id=" + player.m_account_id);
                }

                player.saveAchievementsAndLeaderBoards();
                player.connection = null;
				Program.Display("removing player, nulling connection object " + player.m_UserName);
            }
            catch (Exception ex)
            {
                Program.Display("error in remove player outer loop " + ex.Message + " : " + ex.StackTrace);
            }
            m_players.Remove(player);

			//remove player if player is in the PlayerBackground
			m_playerDisconnecter.PlayerRemove(player);
        }

        /// <summary>
        /// called on lag out,
        /// on message received from logged out player
        /// and shut down
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="removeConnection"></param>
        internal void disconnect(NetConnection connection, bool removeConnection, string reason)
        {
            for (int i = m_players.Count - 1; i >= 0; i--)
            {
                if (connection == m_players[i].connection)
                {
                    Player removedPlayer = m_players[i];
                    if (reason.Length > 0 && removedPlayer != null && removedPlayer.m_activeCharacter != null)
                    {
                        removedPlayer.m_activeCharacter.m_reasonForExit = reason;
                    }
                    removePlayer(removedPlayer);
					
					if (removeConnection == false)
						continue;
					
					connection.Disconnect(String.Empty);
                }
            }
        }

        /// <summary>
        /// disconnected by server 
        /// after account create or errors occurring
        /// also on character log out - from pressing home(unreliable message)
        /// </summary>
        /// <param name="player"></param>
        /// <param name="removeConnection"></param>
        /// <param name="dcString"></param>
        internal void disconnect(Player player, bool removeConnection, string dcString)
        {
		Program.DisplayDelayed("Program disconnect " + player.m_UserName);

            NetConnection connection = player.connection;
            removePlayer(player);
            if (removeConnection && connection != null)
            {
                connection.Disconnect(dcString);
            }
			else
			{
				Program.DisplayDelayed("[ERROR] Program disconnect - ignored " + (removeConnection?"(removal not requested) ":"(connection was null) ") + player.m_UserName);
			}
        }

        public PlayerSpawnPoint GetSpawnPointForID(int pointID, int zoneID)
        {
            Zone zone = getZone(zoneID);
            if (zone == null)
            {
                Program.Display("out of bounds Zone Requested = " + zoneID);
                return null;
            }

            return zone.GetPlayerSpawnPointForID(pointID);
        }

        public bool AddPlayerToZone(Player player, int zoneID)
        {
            Zone zone = getZone(zoneID);
            if (zone == null)
            {
                Program.Display("out of bounds Zone Requested = " + zoneID);
                return false;
            }
            NetOutgoingMessage playermsg = m_server.CreateMessage();

            playermsg.WriteVariableUInt32((uint)NetworkCommandType.EnteringNewZoneUpdate);
            zone.addPlayer(player, m_server, playermsg, true);
            SendMessage(playermsg, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.EnteringNewZoneUpdate);
            zone.WriteMobList(player);
            zone.WriteSpawnedItems(player);
            zone.m_combatManager.SendBattleUpdateMessage(player.m_activeCharacter);
            zone.sendDumbMobPatrolUpdate(player);

            NetOutgoingMessage zoningMessagesComplete = m_server.CreateMessage();
            zoningMessagesComplete.WriteVariableUInt32((uint)NetworkCommandType.AllZoningMessagesSent);
            SendMessage(zoningMessagesComplete, player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.AllZoningMessagesSent);

            //this causes the client to crash as game may not have been Set
            //so play damage will fail
            //zone.SendResetDeathVariable(player);
            return true;
        }

        public Zone getZone(int zone_id)
        {
            for (int i = 0; i < m_zones.Count; i++)
            {
                if (m_zones[i].m_zone_id == zone_id)
                {
                    return m_zones[i];
                }
            }
            return null;
        }

        public bool SendMessage(NetOutgoingMessage msg, NetConnection recipient, NetDeliveryMethod deliveryMethod, NetMessageChannel sequenceChannel, NetworkCommandType messageType)
        {
            if (Program.m_showAllMsgs && recipient != null)
            {
                Program.Display(recipient.RemoteUniqueIdentifier.ToString("X") + ":out: " + messageType + " " + msg.LengthBytes);
            }

            if (recipient == null)
            {
                if (!Program.m_StopOnError)
                {
                    Program.Display("Warning SendMessage attempted to send a message to a null recipient message type: " + messageType + " " + msg.LengthBytes);
                }
                return false;
            }
            if (m_server != null)
            {
                try
                {
                    NetSendResult res = m_server.SendMessage(msg, recipient, deliveryMethod, (int)sequenceChannel);
                    return (res == NetSendResult.Queued || res == NetSendResult.Sent);
                }
                catch (Exception e)
                {
                    Program.DisplayDelayed("Error in send message: " + e.Message + ": " + e.StackTrace);
                }
            }
            return false;
        }

        public bool SendMessage(NetOutgoingMessage msg, List<NetConnection> recipients, NetDeliveryMethod deliveryMethod, NetMessageChannel sequenceChannel, NetworkCommandType messageType)
        {
            if (Program.m_showAllMsgs)
            {
                Program.Display("out: " + messageType + " " + msg.LengthBytes);
            }

            if (m_server != null)
            {
                try
                {
                    if (recipients.Count() > 0)
                    {
                        m_server.SendMessage(msg, recipients, deliveryMethod, (int)sequenceChannel);
                        return true;
                    }
                }
                catch (Exception e)
                {
                    Program.DisplayDelayed("Error in send message: " + e.Message + ": " + e.StackTrace);
                }
            }
            return false;
        }

        public bool SendDelayedMessage(DelayedMessageDescriptor msgDesc)
        {
            if (msgDesc.m_isMassSend)
            {
                // DelayedMessageDescriptor taking a recipient list was never called, so list was always blank
                return SendMessage(msgDesc.m_msg, new List<NetConnection>() { }, msgDesc.m_deliveryMethod, msgDesc.m_sequenceChannel, msgDesc.m_messageType);
            }
            else
            {
                if ((msgDesc.m_messageType == NetworkCommandType.CreateAccountReply || msgDesc.m_messageType == NetworkCommandType.LoginReply) && msgDesc.m_object != null)
                {

                    Player thePlayer = (Player)msgDesc.m_object;

                    if (thePlayer != null)
                    {
                        bool playerFound = false;
                        for (int i = 0; i < m_players.Count && playerFound == false; i++)
                        {
							if (m_players[i].m_account_id == thePlayer.m_account_id && m_players[i].m_markedForDeletion == true)
								Program.Display("[SendDelayedMessage] found existing player but marked for deletion " + m_players[i].m_account_id);

                            if (m_players[i].m_account_id == thePlayer.m_account_id && m_players[i].m_markedForDeletion == false)
                            {
                                playerFound = true;
                            }
                        }

                        if (playerFound == false)
                        {
							Player player = (Player)msgDesc.m_object;

                            m_players.Add(player);

							if (Program.Server.Connections.IndexOf(player.connection) == -1)
							{
								Program.Display("[ERROR] Tried to add a player but its connection isn't in the lidgren list " + player.m_UserName);

								Program.Display("[ERROR] player connection was " + (player.connection == null ? "null" : "not null"));
							}
                        }
                        else
                        {
                            //they are trying to log in on 2 devices, tisk tisk
                            // Program.Display("*****************************" + thePlayer.m_account_id + ":" + thePlayer.m_UserName + "Is trying to log in with 2 devices! Naughty Naughty****************************************");
                            string errorString = "*GH20131125* " + thePlayer.GetIDString() + " tried to log in with 2 devices";
                            Program.Display(errorString);
                            m_universalHubDB.runCommandSync("insert into account_journal (account_id,journal_date,user,details) values (" + thePlayer.m_account_id + ",'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + thePlayer.m_UserName + "',\"" + errorString + "\")");

							string locText = Localiser.GetString(textDB, thePlayer, (int)CommandProcessorTextDB.TextID.ACCOUNT_ALREADY_LOGGED_IN);
							disconnect(thePlayer.connection, (thePlayer.connection != null), locText);
							return false;
                        }
                    }
                    //   msgDesc.m_object = null;
                }
                //  Program.Display("sending delayed message");
                try
                {
                    return SendMessage(msgDesc.m_msg, msgDesc.m_recipient, msgDesc.m_deliveryMethod, msgDesc.m_sequenceChannel, msgDesc.m_messageType);
                }
                catch (Exception e)
                {
                    Program.DisplayDelayed("Error in send message: " + e.Message + ": " + e.StackTrace);
                }
            }
            return false;
        }

        internal void CheckDuplicatePlayers()
        {
            for (int i = (m_players.Count - 1); i >= 0; i--)
            {
                Player currentPlayerI = m_players[i];
                long accountID = currentPlayerI.m_account_id;
                bool duplicateFound = false;
                int duplicateCount = 0;
                for (int j = (i - 1); j >= 0; j--)
                {
                    Player currentPlayerJ = m_players[j];

                    if (accountID == currentPlayerJ.m_account_id)
                    {
                        currentPlayerJ.m_markedForDeletion = true;
                        lock (m_PlayersForDeletion)
                        {
                            m_PlayersForDeletion.Enqueue(currentPlayerJ);
                        }

                        duplicateFound = true;
                        duplicateCount++;
                    }
                }
                if (duplicateFound)
                {
                    currentPlayerI.m_markedForDeletion = true;
                    lock (m_PlayersForDeletion)
                    {
                        m_PlayersForDeletion.Enqueue(currentPlayerI);
                    }
                    string errorString = "*GH20131125* " + currentPlayerI.GetIDString() + " was logged in with " + (duplicateCount + 1) + " devices";
                    Program.Display(errorString);
                }
            }
        }

        internal void shutDown()
        {
            m_inShutDown = true;
            if (Program.m_usingThreads)
            {
                for (int i = 0; i < m_zones.Count; i++)
                {
                    m_zones[i].m_threadExit = true;
                }
                bool finished = false;
                while (finished == false)
                {
                    Thread.Sleep(1);
                    bool foundone = false;
                    for (int i = 0; i < m_zones.Count; i++)
                    {
                        if (m_zones[i].m_updateThread.IsAlive)
                        {
                            foundone = true;
                            break;
                        }
                        if (!foundone)
                            finished = true;
                    }
                }
            }

            m_backgroundThreadExit = true;
			while (m_backgroundThreadFinished == false && m_backgroundTasksThreadFinished == false)
            {
                Thread.Sleep(10);
            }
            bool finishedDequeuing = false;
            while (!finishedDequeuing)
            {
                DelayedMessageDescriptor desc = null;
                lock (m_delayedMessages)
                {
                    if (m_delayedMessages.Count > 0)
                    {
                        desc = m_delayedMessages.Dequeue();
                    }
                    else
                    {
                        finishedDequeuing = true;
                    }
                }
                if (desc != null)
                {
                    SendDelayedMessage(desc);
                }
            }
            for (int i = Program.Server.Connections.Count() - 1; i >= 0; i--)
            {
                try
                {
                    disconnect(Program.Server.Connections[i], true, "ServerShutdown");
                }
                catch (Exception)
                {
                }
            }
            Program.Display("finished kicking");
            m_worldDB.m_exitThread = true;
            m_universalHubDB.m_exitThread = true;
            m_dataDB.m_exitThread = true;
            m_dataDatabaseQuery.Close();

            if (ServerControlledClientManager != null)
                ServerControlledClientManager.DespawnServerControlledClient();
			
			while (m_worldDB.m_finishedThread == false || m_universalHubDB.m_finishedThread == false || m_dataDB.m_finishedThread == false)
            {
                Thread.Sleep(10);
            }
            Program.Display("finished threads");

            if (Program.m_abortGracefully)
            {
                //   m_worldDB.runCommand("delete from inventory where character_id=-1 and item_id=-1");
                //   m_worldDB.runCommand("delete from trade_history where character1_id=-1 and character2_id=-1");
                m_dataDB.runCommand("update worlds set running=0 where world_id=" + Program.m_worldID);
            }
        }

        internal void updateWorldCharacterTotal(long account_id)
        {
            int characterCount = 0;
            SqlQuery countQuery = new SqlQuery(m_worldDB, "select count(character_id) as character_count from character_details where account_id=" + account_id + " and deleted=false", true);
            if (countQuery.Read())
            {
                characterCount = countQuery.GetInt32("character_count");
            }
            countQuery.Close();
            m_universalHubDB.runCommand("replace into world_characters (account_id,world_id,character_count) values (" + account_id + "," + Program.m_worldID + "," + characterCount + ")", true);
            int slotsUsed = 0;
            SqlQuery query = new SqlQuery(m_universalHubDB, "select sum(character_count) as global_count from world_characters where account_id=" + account_id, true);
            if (query.Read())
            {
                slotsUsed = query.GetInt32("global_count");
            }
            query.Close();
            m_universalHubDB.runCommand("update account_details set used_slots=" + slotsUsed + " where account_id=" + account_id, true);
        }

        internal void BackgroundMailIDUpdate()
        {
            int sizeMailPool = m_mailPool.Count;

            string sql = String.Empty;
            SqlQuery query = null;
            lock (objLock)
            {
                sql = "select mail_id from mail where recipient_id=-1 and sender_id=-1 order by mail_id";
                query = new SqlQuery(m_worldDB, sql, true);
                while (query.Read())
                {
                    int newid = query.GetInt32("mail_id");
                    if (!m_mailPool.Contains(newid))
                    {
                        m_mailPool.Enqueue(newid);
                    }
                }
                query.Close();

                sql = "select count(*) as counter from mail where recipient_id=-1 and sender_id=-1";
                query = new SqlQuery(m_worldDB, sql, true);
                if (query.Read())
                {
                    if (!query.isNull("counter"))
                    {
                        sizeMailPool = query.GetInt32("counter");
                    }
                }
                query.Close();

                if (sizeMailPool < 1)
                {
                    string insertString = String.Empty;
                    for (int i = 0; i < Mailbox.MAIL_ID_POOL_SIZE; i++)
                    {
                        insertString += ",(-1,-1)";
                    }
                    m_worldDB.runCommand("insert into mail (recipient_id,sender_id) values " + insertString.Substring(1), true);
                    SqlQuery newIDQuery = new SqlQuery(m_worldDB, "select mail_id from mail where recipient_id=-1 and sender_id=-1 order by mail_id", true);
                    while (newIDQuery.Read())
                    {
                        int newid = newIDQuery.GetInt32("mail_id");
                        if (Database.debug_database)
                        {
                            Program.DisplayDelayed("enqueued new mail id " + newid);
                        }
                        if (!m_mailPool.Contains(newid))
                        {
                            m_mailPool.Enqueue(newid);
                        }
                    }
                    newIDQuery.Close();
                }
            }
        }

        internal void runBackgroundProcess()
        {
            if (Program.m_initialised == false)
            {
                return;
            }

            int sizeInventoryPool = m_inventoryPool.Count;
            if (m_maxInventoryID == 0)
            {
                SqlQuery query = new SqlQuery(m_worldDB, "select max(inventory_id) as maxid from inventory", true);
                if (query.Read())
                {
                    if (!query.isNull("maxid"))
                    {
                        m_maxInventoryID = query.GetInt32("maxid");
                    }
                }
                query.Close();
            }

            if (sizeInventoryPool == 99)
            {
                m_worldDB.runCommand("insert into inventory (character_id,item_id,quantity) values (-1,-1,0)", true);
                int newID = -1;
                SqlQuery query = new SqlQuery(m_worldDB, "select max(inventory_id) as maxid from inventory", true);
                if (query.Read())
                {
                    if (!query.isNull("maxid"))
                    {
                        newID = query.GetInt32("maxid");
                    }
                }
                query.Close();
                if (newID > -1)
                {
                    if (Database.debug_database)
                    {
                        Program.DisplayDelayed("enqueued new inv id " + newID);
                    }
                    lock (m_inventoryPool)
                    {
                        m_inventoryPool.Enqueue(newID);
                    }
                    m_maxInventoryID = newID;

                }
            }
            else if (sizeInventoryPool < 100)
            {
                string insertString = String.Empty;
                for (int i = 0; i < 100 - sizeInventoryPool; i++)
                {
                    insertString += ",(-1,-1,0)";
                }
                m_worldDB.runCommand("insert into inventory (character_id,item_id,quantity) values " + insertString.Substring(1), true);
                SqlQuery newIDQuery = new SqlQuery(m_worldDB, "select inventory_id from inventory where inventory_id>" + m_maxInventoryID + " and character_id=-1 and item_id=-1", true);
                while (newIDQuery.Read())
                {
                    int newid = newIDQuery.GetInt32("inventory_id");
                    m_maxInventoryID = newid;
                    if (Database.debug_database)
                    {
                        Program.DisplayDelayed("enqueued new inv id " + newid);
                    }
                    lock (m_inventoryPool)
                    {
                        m_inventoryPool.Enqueue(newid);
                    }
                }
                newIDQuery.Close();
            }

            int sizeTradeHistoryPool = m_tradeHistoryPool.Count;
            if (m_maxTradeHistoryID == 0)
            {
                SqlQuery query = new SqlQuery(m_worldDB, "select max(trade_history_id) as maxid from trade_history", true);
                if (query.Read())
                {
                    if (!query.isNull("maxid"))
                    {
                        m_maxTradeHistoryID = query.GetInt32("maxid");
                    }
                }
                query.Close();
            }
            if (sizeTradeHistoryPool == 49)
            {
                m_worldDB.runCommand("insert into trade_history (character1_id,character1_gold,character2_id,character2_gold) values (-1,0,-1,0)", true);

                int newID = -1;
                SqlQuery query = new SqlQuery(m_worldDB, "select max(trade_history_id) as maxid from trade_history", true);
                if (query.Read())
                {
                    newID = query.GetInt32("maxid");
                }
                query.Close();
                if (newID > -1)
                {
                    m_maxTradeHistoryID = newID;
                    lock (m_tradeHistoryPool)
                    {
                        m_tradeHistoryPool.Enqueue(newID);
                    }
                    if (Database.debug_database)
                    {
                        Program.DisplayDelayed("enqueued new trade history id " + newID);
                    }
                }
            }
            else if (sizeTradeHistoryPool < 50)
            {
                string insertString = String.Empty;
                for (int i = 0; i < 50 - sizeTradeHistoryPool; i++)
                {
                    insertString += ",(-1,0,-1,0)";
                }
                m_worldDB.runCommand("insert into trade_history (character1_id,character1_gold,character2_id,character2_gold) values " + insertString.Substring(1), true);
                SqlQuery newIDQuery = new SqlQuery(m_worldDB, "select trade_history_id from trade_history where trade_history_id>" + m_maxTradeHistoryID + " and character1_id=-1 and character2_id=-1", true);
                while (newIDQuery.Read())
                {
                    int newID = newIDQuery.GetInt32("trade_history_id");
                    m_maxTradeHistoryID = newID;
                    if (Database.debug_database)
                    {
                        Program.DisplayDelayed("enqueued new tradeHistory id " + newID);
                    }
                    lock (m_tradeHistoryPool)
                    {
                        m_tradeHistoryPool.Enqueue(newID);
                    }
                }
                newIDQuery.Close();
            }

            BackgroundMailIDUpdate();            
        }

		internal int RunBackgroundTasksProcess()
		{
			if(Program.m_initialised == false)
				return 0;

			BaseTask task = null;
			int backgroundTasksCount = 0;

			lock (m_backgroundTasks)
			{
				backgroundTasksCount = m_backgroundTasks.Count;
				if (backgroundTasksCount > 0)
				{
					task = m_backgroundTasks.Dequeue();
                    m_diagnosticTaskNamesQueue.Enqueue(task.ToString());
                }
			}

			if (task != null)
			{
				task.TakeAction(this);
				task = null;
			}
            return backgroundTasksCount;
		}

        internal void updateBackgroundThread()
        {
            while (m_backgroundThreadExit == false)
            {
                if (Program.m_StopOnError)
                {
                    runBackgroundProcess();
                }
                else
                {
                    try
                    {
                        runBackgroundProcess();
                    }
                    catch (Exception e)
                    {
                        Program.DisplayDelayed("error in background thread " + e.Message + " " + e.StackTrace);
                    }
                }

				if (ConfigurationManager.AppSettings["BackgroundThreadSleepMillis"] != null)
					Thread.Sleep( int.Parse(ConfigurationManager.AppSettings["BackgroundThreadSleepMillis"]));
				else
					Thread.Sleep(BACKGROUND_THREAD_SLEEP_MILLIS_DEFAULT);				
				
                if (m_backgroundThreadExit == true)
					break;                
            }
            m_backgroundThreadFinished = true;
        }

		/**
		 * Updates any pending background task classes like logintask, accountcreatetask
		 */
		internal void updateBackgroundTasksThread()
		{
			while (m_backgroundThreadExit == false || m_backgroundTasks.Count > 0)
			{
				int backgroundTasksCount = 0;
				if (Program.m_StopOnError)
				{
					backgroundTasksCount = RunBackgroundTasksProcess();
				}
				else
				{
					try
					{
						backgroundTasksCount = RunBackgroundTasksProcess();
					}
					catch (Exception e)
					{
						Program.DisplayDelayed("error in background tasks thread " + e.Message + " " + e.StackTrace);
					}
				}

				Thread.Sleep(1);

				if (backgroundTasksCount == 0 && m_backgroundThreadExit == true)
					break;
			}

			m_backgroundTasksThreadFinished = true;
		}

        internal int getAvailableInventoryID()
        {
            int newID = -1;
            lock (m_inventoryPool)
            {
                if (m_inventoryPool.Count > 0)
                {
                    newID = m_inventoryPool.Dequeue();
                }
            }

            if (newID == -1)
            {
                m_worldDB.runCommand("insert into inventory (character_id,item_id,quantity) values (-1,-1,0)");
                SqlQuery query = new SqlQuery(m_worldDB, "select max(inventory_id) as maxid from inventory");
                if (query.Read())
                {
                    newID = query.GetInt32("maxid");
                    if (Database.debug_database)
                    {
                        Program.DisplayDelayed("created new inv id " + newID);
                    }
                }
                query.Close();
            }
            else if (Database.debug_database)
            {
                Program.DisplayDelayed("dequeued new inv id " + newID);
            }
            return newID;
        }

        #region Auction House Functions

        // getAuctionHouseQuery                //
        // Auction House database query access //
        internal SqlQuery getAuctionHouseQuery(string sqlString)
        {
            return new SqlQuery(m_worldDB, sqlString);
        }

        // auctionHouseSql                                         //
        // Allows the use of SQL commands and returns is succesful //
        internal void auctionHouseSql(string sqlString, MainServer.SQLSuccessDelegate successDelegate = null, MainServer.SQLFailureDelegate failureDelegate = null)
        {
            m_worldDB.runCommandSync(sqlString, successDelegate, failureDelegate);
        }

        #endregion

        internal int getAvailableMailID()
        {
            int newID = -1;
            bool found = false;

            lock (objLock)
            {
                while (!found)
                {
                    if (m_mailPool.Count > 0)
                    {
                        newID = m_mailPool.Dequeue();
                    }

                    if (newID == -1)
                    {
                        m_worldDB.runCommand("insert into mail (recipient_id,sender_id) values (-1,-1)");
                        SqlQuery query = new SqlQuery(m_worldDB, "select max(mail_id) as maxid from mail");
                        if (query.Read())
                        {
                            newID = query.GetInt32("maxid");
                            if (Database.debug_database)
                            {
                                Program.DisplayDelayed("created new mail id " + newID);
                            }
                        }
                        query.Close();
                        found = true;
                    }
                    else if (Database.debug_database)
                    {
                        Program.DisplayDelayed("dequeued new mail id " + newID);
                    }

                    if (newID != -1)
                    {
                        string sql = String.Format("select mail_id from mail where mail_id = {0} and recipient_id = -1 and sender_id -1", newID);
                        SqlQuery query = new SqlQuery(m_worldDB, sql);
                        found = query.Read();
                        if (!found)
                        {
                            Program.Display(newID + "already been used!");
                        }

                        query.Close();
                    }
                }

                string updsql = "update mail set recipient_id=0, sender_id=0 where mail_id=" + newID;
                m_worldDB.runCommand(updsql);
            }

            return newID;
        }

        internal int getAvailableTradeHistoryID()
        {
            int newID = -1;
            lock (m_tradeHistoryPool)
            {
                if (m_tradeHistoryPool.Count > 0)
                {
                    newID = m_tradeHistoryPool.Dequeue();
                }
            }
            if (newID == -1)
            {
                m_worldDB.runCommand("insert into trade_history (character1_id,character1_gold,character2_id,character2_gold) values (-1,0,-1,0)");
                SqlQuery query = new SqlQuery(m_worldDB, "select max(trade_history_id) as maxid from trade_history");
                if (query.Read())
                {
                    newID = query.GetInt32("maxid");
                    if (Database.debug_database)
                    {
                        Program.DisplayDelayed("created new trade history id " + newID);
                    }
                }
                query.Close();
            }
            else if (Database.debug_database)
            {
                Program.DisplayDelayed("dequeued new trade history id " + newID);
            }
            return newID;
        }

        internal void CreatePlayers()
        {
            SqlQuery characterQuery = new SqlQuery(m_worldDB, "SELECT * FROM character_details  where deleted=0 and level>5 ORDER BY RAND() LIMIT 50");
            while (characterQuery.Read())
            {
                Player curplayer = new Player(Program.processor.m_universalHubDB, characterQuery.GetInt32("account_id"));

                curplayer.connection = null;
                curplayer.m_sessionID = (uint)Program.m_rand.Next();
                SqlQuery accountQuery = new SqlQuery(m_universalHubDB, "SELECT * FROM account_details ac where account_id=" + characterQuery.GetInt32("account_id"));
                if (accountQuery.Read())
                {
                    curplayer.m_hashedPass = accountQuery.GetString("hashed_pwd");
                    curplayer.m_UserName = accountQuery.GetString("user_name");
                }
                accountQuery.Close();
                curplayer.m_loggedInTime = DateTime.Now;
                m_players.Add(curplayer);
                StartGameTask task = new StartGameTask(curplayer, (uint)characterQuery.GetInt32("character_id"), characterQuery.GetString("name"));

                task.TakeAction(this);
                processFinishedZoning(null, curplayer);
            }
            characterQuery.Close();
        }



        internal void EnqueueTask(BaseTask task)
        {
            lock (m_backgroundTasks)
            {
                m_backgroundTasks.Enqueue(task);
            }
        }

        // PDH - Handle a request for server data (msg indicates the information requested)
        void ProcessRequestServerDynamicData(NetIncomingMessage msg, NetOutgoingMessage outmsg, Player player)
        {
            string incoming = msg.ReadString();
            string[] options = incoming.Split(new[] { ' ' });
            if (!options.Any())
                return;

            // A request for 
            switch (options[0])
            {
                // Bounty board, Quest Name
                case ServerBountyManager.BB_BOUNTY_DATA:
                {
                    if (player.m_activeCharacter != null)
                        player.m_activeCharacter.CharacterBountyManager.SendBounties(player);
                    break;
                }

                case ServerBountyManager.BB_PURCHASE_BOUNTY:
                {
                    if (player.m_activeCharacter != null)
                        player.m_activeCharacter.CharacterBountyManager.PurchaseBounty(player);
                    break;
                }
            }
        }


        /// <summary>
        /// Cast skill requires three ints, mobId, skillID, cost
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="player"></param>
        private void ProcessCastSkillOnPlayer(NetIncomingMessage msg, Player player)
        {
            int mobID = msg.ReadVariableInt32();
            int skillID = msg.ReadVariableInt32();
            int costGold = msg.ReadVariableInt32();

            //perform a couple of checks
            Character character = player.m_activeCharacter;
            ServerControlledEntity mobEntity = player.m_activeCharacter.m_zone.getMobFromID(mobID);
            if (mobEntity == null)
            {
                Program.Display("ProcessCastSkillOnPlayer.can't find mob to cast skill");
                return;
            }
            MobSkill mobsSkill = mobEntity.SkillTable.GetSkillForID((SKILL_TYPE)skillID);

            //Mob does not have the skill
            if (mobsSkill == null)
            {
                Program.Display("ProcessCastSkillOnPlayer.mob." + mobEntity.Name + " does NOT have skill." + skillID);
                return;
            }


            //can the player afford it
            if (player.m_activeCharacter.m_inventory.m_coins < costGold)
            {
                Program.Display("ProcessCastSkillOnPlayer.character." + character.Name + " can't afford skill." + skillID);
                return;                
            }

            //check cost
            if (costGold < mobEntity.Template.m_minCoins)
            {
                Program.Display("ProcessCastSkillOnPlayer.goldCost invalid less than." + mobEntity.Template.m_minCoins);
                return; 
            }

            //we passed our checks, mob has skill and player can afford it            
            //try casting skill
            SkillTemplate skillTemplate = SkillTemplateManager.GetItemForID((SKILL_TYPE)skillID);
            if (skillTemplate == null)
            {
                Program.Display("ProcessCastSkillOnPlayer.skill template null." + skillID);
                return;
            }

            //lifted from Questmanager.GiveReward().line 1244
            bool castSkillOk = false;

            EntitySkill entitySkill = new EntitySkill(skillTemplate);
            entitySkill.SkillLevel = mobsSkill.TheSkill.SkillLevel;
            CombatEntity skillCaster = mobEntity;

            if ((int)entitySkill.SkillID >= SkillTemplate.LEARN_SKILL_START_ID &&
                (int)entitySkill.SkillID < SkillTemplate.LEARN_SKILL_END_ID)
            {
                SKILL_TYPE skillToLearn = (SKILL_TYPE)((int)entitySkill.SkillID - 1000);
                bool skillGained = character.AddSkill(skillToLearn, false, true);                
                castSkillOk = skillGained;
                if (skillGained == true)
                {
                    character.SendBuySkillResponse((int)entitySkill.SkillID);                    
                }
                else
                {
					string locText = Localiser.GetString(textDB, player, (int)CommandProcessorTextDB.TextID.PLAYER_ALREADY_KNOW_SKILL);
					string skillName = SkillTemplateManager.GetLocaliseSkillName(player, entitySkill.Template.SkillID);
					locText = String.Format(locText, skillName);
					sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.ITEM_USE);
				}
            }
            if (skillTemplate.LearnRecipeID!=0)
            {
                bool recipeLearned = character.AddRecipe(skillTemplate.LearnRecipeID);

                if (recipeLearned)
                {
                    character.SendLearnRecipeResponse(skillTemplate.LearnRecipeID);
                }
                else
                {
                    string recipeKnown = "You already know " + skillTemplate.SkillName;
                    Program.processor.sendSystemMessage(recipeKnown, player, false, SYSTEM_MESSAGE_TYPE.ITEM_USE);
                }
            }
            else if (character.TheCombatManager != null)
            {
                double oldTimeActionWillComplete = skillCaster.TimeActionWillComplete;
                bool skillSucceeded = character.TheCombatManager.CastSkill(skillCaster, character, entitySkill, null);
                castSkillOk = skillSucceeded;
                skillCaster.TimeActionWillComplete = oldTimeActionWillComplete;
                if (skillSucceeded == false)
                {
                    Program.Display("error in skill cast for conversation");
                }

            }
            else
            {
                Program.Display("error in skill cast for conversation");
            }

            //if everything has gone ok, take payment
            if (castSkillOk)
            {
                player.m_activeCharacter.updateCoins(-costGold);
                character.m_inventory.SendInventoryUpdate();
            }

        }

        internal void SendMailWithItemToPlayer(Player player,String mailSubject,String mailMessage,int itemID, int quantity,String senderName) {

            Item item = new Item( -1, itemID, quantity, -1);

            SignpostMailTask newTask = new SignpostMailTask((int)player.m_activeCharacter.m_character_id, mailSubject, mailMessage, item, 1, 0, senderName);

            lock (Program.processor.m_backgroundTasks)
            {
                Program.processor.m_backgroundTasks.Enqueue(newTask);
            }
        }

		internal int GetAccountLangaugeID(int characterID)
		{
			int langID = 0;

			SqlQuery characterQuery = new SqlQuery(m_worldDB, "SELECT * FROM character_details where character_id=" + characterID);

			if (characterQuery.HasRows)
			{
				characterQuery.Read();
				SqlQuery accountQuery = new SqlQuery(m_universalHubDB, "SELECT * FROM account_details where account_id=" + characterQuery.GetInt32("account_id"));
				if (accountQuery.HasRows)
				{
					accountQuery.Read();
					langID = accountQuery.GetInt32("lang_id");
				}
				accountQuery.Close();
			}
			characterQuery.Close();

			return langID;
		}

		internal int GetAccountLangaugeID(string userName)
		{
			int langID = 0;

			//SqlQuery accountQuery = new SqlQuery(m_universalHubDB, "SELECT * FROM account_details WHERE user_name = '" + userName +"'");

			List<MySqlParameter> sqlParams = new List<MySqlParameter>();
			sqlParams.Add(new MySqlParameter("@userName", userName));

			SqlQuery accountQuery = new SqlQuery(m_universalHubDB, "SELECT * FROM account_details WHERE user_name = @userName", sqlParams.ToArray());

			if (accountQuery.HasRows)
			{
				accountQuery.Read();
				langID = accountQuery.GetInt32("lang_id");
			}
			accountQuery.Close();

			return langID;
		}

        internal void EnablePerformanceLogging(bool in_enableLogging)
        {
            m_performanceLoggingEnabled = in_enableLogging;
        }

        internal void SetSeasonTweak(List<string> seasonTweakList)
        {
            tweakerFilenames = seasonTweakList;
        }
    }

}