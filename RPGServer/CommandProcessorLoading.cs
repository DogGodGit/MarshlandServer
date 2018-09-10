using System;
using System.Threading;
using System.Windows.Forms;
using Lidgren.Network;
using MainServer.Combat;
using MainServer.CombatAlgorithms;
using MainServer.Crafting;
using MainServer.DailyLoginReward;
using MainServer.Items;
using MainServer.NamedPipeController;
using MainServer.player_offers;
using MainServer.TokenVendors;
using MainServer.AuctionHouse;
using MainServer.Factions;

using System.IO;
using System.Collections.Generic;

namespace MainServer
{
    public enum LoadingState
    {
        NotInitialized,        
        InitialLoad,
        ItemTemplates,
        LootSets,
        Skills,
        CombatAI,
        SkillSet,
        Signposts,
        MobTemplates,
        Abilities,
        Classes,
        Races,
        Zones,
        ClearLogins,
        Clans,
        PatchVersion,
        PremiumShop,
        RandomStrings,
        EquipmentSets,
        SpecialOffers,
        Bounties,
        MobSets,
        CharacterEffects,
        TokenVendors,
        CombatFactors,
        ServerControlledClient,
        QuestTracking,
        TargetedSpecialOffers,
        AuctionHouse,
        BarberShop,
        Factions,
		Crafting,
        DailyRewards,
        Competitions,
        LoadComplete
    }

    class CommandProcessorLoading
    {
        private CommandProcessor commandProcessor;
        private double startTime;
        public LoadingState CurrentLoadingState { get; private set; }

        public CommandProcessorLoading(CommandProcessor commandProcessor)
        {
            this.commandProcessor = commandProcessor;

            //we begin at not initialized
            CurrentLoadingState = LoadingState.NotInitialized;
        }

        public void Initialize()
        {
            //first stage of the load is initialload - note the time
            CurrentLoadingState = LoadingState.InitialLoad;
            startTime = NetTime.Now;

            //start the load
            Load();
        }

        public void Load()
        {
            //if we're shutting down...don't do anything
            if (commandProcessor.InShutDown)
                return;

            switch (CurrentLoadingState)
            {
                case LoadingState.InitialLoad:

                    InitialLoad();

                    LoadParams();

                    LoadWorldParams(commandProcessor.m_worldDB);

                    Program.Display("Initial Tidyup " + (NetTime.Now - startTime).ToString("F1") + "s");

                    startTime = NetTime.Now;

                    //create quest template manager
                    commandProcessor.m_QuestTemplateManager = new QuestTemplateManager(commandProcessor.m_dataDB);
                    Program.Display("Loaded quest templates " + (NetTime.Now - startTime).ToString("F1") + "s");
                    break;

                case LoadingState.ItemTemplates:
                    ItemTemplateManager.FillTemplate(commandProcessor.m_dataDB);
                    ItemCooldown.SetUp(commandProcessor.m_dataDB);
                    Program.Display("Loaded item templates " + (NetTime.Now - startTime).ToString("F1") + "s");
                    break;

                case LoadingState.LootSets:
                    LootSetManager.FillTemplate(commandProcessor.m_dataDB);
                    Program.Display("Loaded loot sets " + (NetTime.Now - startTime).ToString("F1") + "s");
                    break;

                case LoadingState.Skills:
                    SkillTemplateManager.FillTemplate(commandProcessor.m_dataDB);
                    Program.Display("Loaded Skill Templates " + (NetTime.Now - startTime).ToString("F1") + "s");
                    break;

                case LoadingState.CombatAI:
                    //mobs will need to link to the combat ai templates
                    CombatAITemplateManager.FillTemplate(commandProcessor.m_dataDB);
                    PVP_RatingLookupManager.loadLookups(commandProcessor.m_dataDB);
                    Program.Display("Loaded CombatAI Templates " + (NetTime.Now - startTime).ToString("F1") + "s");
                    break;

                case LoadingState.SkillSet:
                    //mobs will need their skill sets before the mob template is created
                    SkillSetTemplateManager.FillTemplate(commandProcessor.m_dataDB);
                    Program.Display("Loaded Skill Sets Templates " + (NetTime.Now - startTime).ToString("F1") + "s");
                    break;

                case LoadingState.Signposts:
                    if (Program.m_signpostingOn == true)
                    {
                        Signposting.SignpostManager.ReadSignpostsFromDatabase(commandProcessor.m_dataDB);
                        Program.Display("Loaded Signposts " + (NetTime.Now - startTime).ToString("F1") + "s");
                    }
                    else
                    {
                        Program.Display("Signposts not Loaded, SIGNPOSTS_ACTIVE off");
                    }
                    break;

                case LoadingState.MobTemplates:

                    if (LootSetManager.GetLootSetsLoaded() == false)
                    {
                        Program.Display("Caught a dodgy access to lootsets from off-thread, they aren't loaded yet");
                        return;
                    }

                    // load temporary file that holds mob ids that can't move (plant boss and anyone else immobile)
                    List<int> immovableMobIDs = LoadImmobileMobIDsFile();

                    //mobs require skills to be read in 1st
                    //mobs will need their skill sets
                    //mobs will need to link to the combat ai templates
                    MobTemplateManager.FillTemplate(commandProcessor.m_dataDB, immovableMobIDs);
                    Program.Display("Loaded Mob Templates " + (NetTime.Now - startTime).ToString("F1") + "s");
                    break;

                case LoadingState.Abilities:
                    AbilityManager.Setup(commandProcessor.m_dataDB);
                    Program.Display("Loaded Abilities " + (NetTime.Now - startTime).ToString("F1") + "s");
                    break;

                case LoadingState.Classes:
                    ClassTemplateManager.Setup(commandProcessor.m_dataDB);
                    Program.Display("Loaded Classes " + (NetTime.Now - startTime).ToString("F1") + "s");
                    break;

                case LoadingState.Races:
                    RaceTemplateManager.Setup(commandProcessor.m_dataDB);
                    Program.Display("Loaded Races " + (NetTime.Now - startTime).ToString("F1") + "s");
                    break;

                case LoadingState.Zones:
                    LoadAndSetupZones();
                    Program.Display("Loaded Zones " + (NetTime.Now - startTime).ToString("F1") + "s");
                    break;

                case LoadingState.ClearLogins:
                    if (Program.m_serverRole == Program.SERVER_ROLE.WORLD_SERVER)
                    {
                        commandProcessor.m_universalHubDB.runCommand("update account_details set session_id=0,logged_in_world=0 where logged_in_world=" + Program.m_worldID);
                    }
                    Program.Display("Cleared Logged in State " + (NetTime.Now - startTime).ToString("F1") + "s");
                    break;

                case LoadingState.Clans:
                    {
                        LoadClansFromDatabase();
                        Program.Display("Loaded Clans " + (NetTime.Now - startTime).ToString("F1") + "s");

                        break;
                    }
                case LoadingState.PatchVersion:
                    {
                        SqlQuery query = new SqlQuery(commandProcessor.m_dataDB, "select patch_version from worlds where world_id=" + Program.m_worldID);
                        if (query.Read())
                        {
                            commandProcessor.m_patchVersion = query.GetInt32("patch_version");
                        }
                        query.Close();

                        break;
                    }
                case LoadingState.PremiumShop:
                    {
                        commandProcessor.m_premiumShop = new PremiumShop();
                        Program.Display("Loaded Premium Shop");
                        LoadAchievementTargets();
                        Program.Display("Loaded Achievement Targets " + (NetTime.Now - startTime).ToString("F1") + "s");

                        break;
                    }
                case LoadingState.RandomStrings:
                    {
                        LoadInRandomStrings();
                        Program.Display("Loaded Random strings " + (NetTime.Now - startTime).ToString("F1") + "s");

                        break;
                    }
                case LoadingState.EquipmentSets:
                    {
                        LoadEquipmentSets(commandProcessor.m_dataDB);
                        Program.Display("Loaded Equipment Sets " + (NetTime.Now - startTime).ToString("F1") + "s");

                        break;
                    }
                case LoadingState.SpecialOffers:
                    {
                        if (SpecialOfferTemplate.SPECIAL_OFFERS_ACTIVE == true)
                        {
                            SpecialOfferTemplateManager.LoadSpecialOfferTemplates(commandProcessor.m_dataDB);
                            commandProcessor.m_globalOfferManager = new SpecialOfferManager();
                            commandProcessor.m_globalOfferManager.LoadGlobalOffers(commandProcessor.m_dataDB);
                            Program.Display("Loaded Special Offers " + (NetTime.Now - startTime).ToString("F1") + "s");
                        }
                        else
                        {
                            Program.Display("Special Offers not Loaded, SPECIAL_OFFERS_ACTIVE off");
                        }
                        break;
                    }
                // PDH
                case LoadingState.Bounties:
                    {
                        ServerBountyManager.Load(commandProcessor.m_dataDB);
                        Program.Display("Loaded Bounty quests");
                        break;
                    }

                // PDH
                case LoadingState.MobSets:
                    {
                        MobSets.Load(commandProcessor.m_dataDB);
                        Program.Display("Loaded Mob Sets");
                        break;
                    }
                case LoadingState.CharacterEffects:
                    {
                        StatusEffectTemplateManager.FillTemplate(commandProcessor.m_dataDB);
                        CharacterEffectManager.Fill(commandProcessor.m_dataDB);

                        Program.Display("Loaded Character Effects");
                        break;
                    }
                case LoadingState.TokenVendors:
                    {
                        ITokenVendorDatabase tokenVendorDatabase = new TokenVendorDatabase(commandProcessor.m_dataDatabaseQuery);
                        commandProcessor.m_tokenVendorManager = new TokenVendorManager(tokenVendorDatabase);
                        commandProcessor.TokenVendorNetworkManager = new TokenVendorNetworkManager(commandProcessor.m_tokenVendorManager);
                        Program.Display("Loaded Token Vendors");
                        break;
                    }
                case LoadingState.CombatFactors:
                    {
                        IEvasionFactorDatabase evasionTokenDatabase = new EvasionFactorDatabase(commandProcessor.m_dataDatabaseQuery);
                        commandProcessor.EvasionFactorManager = new EvasionFactorManager(evasionTokenDatabase);

                        IMeleeDamageFluctuationDatabase meleeDamageFluctuationDatabase = new MeleeDamageFluctuationDatabase(commandProcessor.m_dataDatabaseQuery);
                        commandProcessor.MeleeDamageFluctuationManager = new MeleeDamageFluctuationManager(meleeDamageFluctuationDatabase);

                        ISkillDamageFluctuationDatabase skillDamageFluctuationDatabase = new SkillDamageFluctuationDatabase(commandProcessor.m_dataDatabaseQuery);
                        commandProcessor.SkillDamageFluctuationManager = new SkillDamageFluctuationManager(skillDamageFluctuationDatabase);

                        Program.Display("Loaded Combat Factors");
                        break;
                    }
                case LoadingState.ServerControlledClient:
                    {
                        IServerControlledClientDatabase serverControlledClientDatabase = new ServerControlledClientDatabase(commandProcessor.m_dataDatabaseQuery);
                        commandProcessor.ServerControlledClientManager = new ServerControlledClientManager(serverControlledClientDatabase);

                        Program.Display("Loaded Server Control Preferences");
                        break;
                    }
                case LoadingState.QuestTracking:
                    {
                        QuestManager.InitializeQuestTrackingList();
                        Program.Display("Loaded Quest Tracking");

                        break;
                    }
                case LoadingState.TargetedSpecialOffers:
                    {
                        commandProcessor.TargetedSpecialOfferManager = new TargetedSpecialOfferManager(commandProcessor.m_dataDB);
                        Program.Display("Loaded Targeted Special Offer Manager");
                        break;
                    }
                case LoadingState.AuctionHouse:
                    {
                        AuctionHouseParams.Setup(commandProcessor.m_dataDB);
                        Program.m_resetAHDurations = Program.MainForm.resetDurationsCheckBox.Checked;
                        commandProcessor.TheAuctionHouse = new AuctionHouseManager((AuctionHouse.Enums.AHStatus)Program.m_auctionHouseActive, Program.m_resetAHDurations, AuctionHouseParams.ServerShutdown);
                        Program.Display("Loaded Auction House Manager");
                        break;
                    }
                case LoadingState.BarberShop:
                    {
                        commandProcessor.BarbershopNetworkManager = new BarbershopNetworkManager();
                        Program.Display("Loaded barbershop");
                        break;
                    }
                case LoadingState.Crafting:
                    {
                        commandProcessor.CraftingNetworkHandler = new CraftingNetworkHandler();
                        commandProcessor.CraftingTemplateManager = new CraftingTemplateManager();
                        Program.Display("Loaded Crafting");
                        break;
                    }
                case LoadingState.Factions:
                    {
                        commandProcessor.FactionNetworkManager = new FactionNetworkManager();
                        commandProcessor.FactionTemplateManager = new FactionTemplateManager();
                        Program.Display("Loaded Factions");
                        break;
                    }
 				case LoadingState.DailyRewards:
                    {
                        DailyRewardTemplateManager.LoadDailyRewards(Program.processor.m_dataDB);
                        commandProcessor.DailyRewardManager = new DailyRewardNetworkManager();
                        Program.Display("Loaded Daily Rewards");
                        break;
                    }
                case LoadingState.Competitions:
                    {
                        commandProcessor.CompetitionManager = new Competitions.CompetitionManager(Program.processor.m_dataDB);
                        Program.Display("Loaded Competitions");
                        break;
                    }
                case LoadingState.LoadComplete:
                    break;
                default:
                    Program.Display("unhandled initialization stage " + CurrentLoadingState);
                    break;
            }

            NextLoadingStage();
        }

        /// <summary>
        /// Change the current loading state up one, and call Load() again. Stop once
        /// we reach LoadComplete stage
        /// </summary>
        private void NextLoadingStage()
        {
            if (CurrentLoadingState != LoadingState.LoadComplete)
            {
                CurrentLoadingState++;
                Load();
            }
        }

        /// <summary>
        /// I think this does a bunch of prechecks.  Makes sure we haven't accidently
        /// lauanched the same world server twice, or have the wrong platform specified. 
        /// Loads in mail and some other bits too.
        /// </summary>
        private void InitialLoad()
        {
            if (!ConfigurationVerifier.Verify(commandProcessor.m_dataDB))
            {
                DialogResult res =
                    MessageBox.Show(
                        "The app.config target platform is different from the DB worlds table platform. Server will shut down. Please check app.config",
                        "Configuration Error", MessageBoxButtons.OK);
                {
                    if (res == DialogResult.OK)
                    {
                        this.HaltServerImmediately();
                    }
                }
            }


            // test to see if the world is running on another process
            SqlQuery stillRunningQuery = new SqlQuery(commandProcessor.m_dataDB, "select running from worlds where world_id=" + Program.m_worldID);
            bool running = true;
            if (stillRunningQuery.Read())
            {
                running = stillRunningQuery.GetBoolean("running");
            }
            stillRunningQuery.Close();
#if RELEASE
                        if (running == true)
                        {
                            DialogResult res = MessageBox.Show("Server doesn't appear to have shut down yet, please check process list", "Instance of server running", MessageBoxButtons.AbortRetryIgnore);
                            {
                                if (res == DialogResult.Abort)
                                {
                                    HaltServerImmediately();
                                }
                                else if (res == DialogResult.Retry)
                                {
                                    return false;
                                }
                            }
                        }
#endif
            //set world to be running
            commandProcessor.m_dataDB.runCommand("update worlds set running=1 where world_id=" + Program.m_worldID);

            //pick up unused inventory_ids and add them to the inventory pool
            SqlQuery newIDQuery = new SqlQuery(commandProcessor.m_worldDB, "select inventory_id from inventory where character_id=-1 and item_id=-1", true);
            while (newIDQuery.Read())
            {
                int newid = newIDQuery.GetInt32("inventory_id");
                commandProcessor.m_maxInventoryID = newid;
                if (Database.debug_database)
                {
                    Program.DisplayDelayed("enqueued new inv id " + newid);
                }
                lock (commandProcessor.m_inventoryPool)
                {
                    commandProcessor.m_inventoryPool.Enqueue(newid);
                }
            }
            newIDQuery.Close();
            //pick up unused trade_history_ids and add them to the trade_histoy pool pool
            newIDQuery = new SqlQuery(commandProcessor.m_worldDB, "select trade_history_id from trade_history where trade_history_id>" + commandProcessor.m_maxTradeHistoryID + " and character1_id=-1 and character2_id=-1", true);
            while (newIDQuery.Read())
            {
                int newID = newIDQuery.GetInt32("trade_history_id");
                commandProcessor.m_maxTradeHistoryID = newID;
                if (Database.debug_database)
                {
                    Program.DisplayDelayed("enqueued new tradeHistory id " + newID);
                }
                lock (commandProcessor.m_tradeHistoryPool)
                {
                    commandProcessor.m_tradeHistoryPool.Enqueue(newID);
                }
            }
            newIDQuery.Close();

            //pick up unused mail_ids and add them to the mail_id pool
            newIDQuery = new SqlQuery(commandProcessor.m_worldDB, "select mail_id from mail where recipient_id=-1 and sender_id=-1 order by mail_id", true);
            while (newIDQuery.Read())
            {
                int newID = newIDQuery.GetInt32("mail_id");
                if (Database.debug_database)
                {
                    Program.DisplayDelayed("enqueued new mail id " + newID);
                }
                lock (commandProcessor.objLock)
                {
                    if (!commandProcessor.m_mailPool.Contains(newID))
                    {
                        commandProcessor.m_mailPool.Enqueue(newID);
                    }
                }
            }
            newIDQuery.Close();
        }

        private void LoadParams()
        {
            SqlQuery query = new SqlQuery(commandProcessor.m_dataDB, "select * from params");
            while (query.Read())
            {
                switch (query.GetString("param_name"))
                {
                    case "new character pos x":
                        {
                            Character.m_NewCharPosX = float.Parse(query.GetString("param_value"));
                            break;
                        }
                    case "new character pos y":
                        {
                            Character.m_NewCharPosY = float.Parse(query.GetString("param_value"));
                            break;
                        }
                    case "new character pos z":
                        {
                            Character.m_NewCharPosZ = float.Parse(query.GetString("param_value"));
                            break;
                        }
                    case "new character zone":
                        {
                            Character.m_NewCharZone = Int32.Parse(query.GetString("param_value"));
                            break;
                        }
                    case "new character teleport list":
                        {
                            Character.m_NewCharTeleportList = query.GetString("param_value");
                            break;
                        }
                    case "new character y angle":
                        {
                            Character.m_NewCharStartAngle = float.Parse(query.GetString("param_value"));
                            break;
                        }
                    case "pvp melee mult":
                        {
                            Character.m_pvpMeleeMult = float.Parse(query.GetString("param_value"));
                            break;
                        }
                    case "global exp rate":
                        {
                            commandProcessor.m_globalEXPMod = float.Parse(query.GetString("param_value"));
                            break;
                        }
                    case "global gold rate":
                        {
                            commandProcessor.m_globalGoldMod = float.Parse(query.GetString("param_value"));
                            break;
                        }
                    case "current patch checksum":
                        {
                            commandProcessor.m_verificationHash = query.GetString("param_value");
                            break;
                        }
                    case "health for lock":
                        {
                            ServerControlledEntity.MIN_HEALTH_FOR_LOCK = float.Parse(query.GetString("param_value"));
                            break;
                        }
                    case "MOB_MIN_STAT_REMAINS":
                        {
                            ServerControlledEntity.MOB_MIN_STAT_REMAINS = float.Parse(query.GetString("param_value"));
                            break;
                        }
                    case "LOCK_UPPER_LVL_LIMIT":
                        {
                            ServerControlledEntity.LOCK_UPPER_LVL_LIMIT = int.Parse(query.GetString("param_value"));
                            break;
                        }
                    case "LOCK_UPPER_DEGRADE_START":
                        {
                            ServerControlledEntity.LOCK_UPPER_DEGRADE_START = int.Parse(query.GetString("param_value"));
                            break;
                        }
                    case "LOCK_LOWER_LVL_LIMIT":
                        {
                            ServerControlledEntity.LOCK_LOWER_LVL_LIMIT = int.Parse(query.GetString("param_value"));
                            break;
                        }
                    case "LOCK_LOWER_DEGRADE_START":
                        {
                            ServerControlledEntity.LOCK_LOWER_DEGRADE_START = int.Parse(query.GetString("param_value"));
                            break;
                        }
                    case "DAMAGE_TAIL_OFF_LVL_BELOW_END":
                        {
                            DamageCalculator.DAMAGE_TAIL_OFF_LVL_BELOW_END = int.Parse(query.GetString("param_value"));
                            break;
                        }
                    case "DAMAGE_TAIL_OFF_LVL_BELOW_START":
                        {
                            DamageCalculator.DAMAGE_TAIL_OFF_LVL_BELOW_START = int.Parse(query.GetString("param_value"));
                            break;
                        }
                    case "DAMAGE_TAIL_OFF_MIN_VAL":
                        {
                            DamageCalculator.DAMAGE_TAIL_OFF_MIN_VAL = float.Parse(query.GetString("param_value"));
                            break;
                        }
                    case "DAMAGE_TAIL_OFF_MOB_START_LVL":
                        {
                            DamageCalculator.DAMAGE_TAIL_OFF_MOB_START_LVL = int.Parse(query.GetString("param_value"));
                            break;
                        }
                    case "DAMAGE_TAIL_OFF_MOB_END_LVL":
                        {
                            DamageCalculator.DAMAGE_TAIL_OFF_MOB_END_LVL = int.Parse(query.GetString("param_value"));
                            break;
                        }
                    case "AGGRO_DEGRADE_START_LVL_ABOVE":
                        {
                            ServerControlledEntity.AGGRO_DEGRADE_START_LVL_ABOVE = int.Parse(query.GetString("param_value"));
                            break;
                        }
                    case "AGGRO_DEGRADE_END_LVL_ABOVE":
                        {
                            ServerControlledEntity.AGGRO_DEGRADE_END_LVL_ABOVE = int.Parse(query.GetString("param_value"));
                            break;
                        }
                    case "AGGRO_INCREASE_START_LVL_BELOW":
                        {
                            ServerControlledEntity.AGGRO_INCREASE_START_LVL_BELOW = int.Parse(query.GetString("param_value"));
                            break;
                        }
                    case "AGGRO_INCREASE_INCREMENT_PER_LEVEL":
                        {
                            ServerControlledEntity.AGGRO_INCREASE_INCREMENT_PER_LEVEL = float.Parse(query.GetString("param_value"));
                            break;
                        }
                    case "MIN_AGGRO_ADDED":
                        {
                            ServerControlledEntity.MIN_AGGRO_ADDED = float.Parse(query.GetString("param_value"));
                            break;
                        }
                    case "FULL_HEAL_LOGOUT_TIME":
                        {
                            DamageCalculator.FULL_HEAL_LOGOUT_TIME = float.Parse(query.GetString("param_value"));
                            break;
                        }
                    case "melee crit base ability":
                        {
                            commandProcessor.m_abilityVariables.CriticalStrikeBaseAbility = float.Parse(query.GetString("param_value"));
                            break;
                        }
                    case "melee crit max chance":
                        {
                            commandProcessor.m_abilityVariables.CriticalStrikeMaxChance = float.Parse(query.GetString("param_value"));
                            break;
                        }
                    case "melee crit multiplier":
                        {
                            commandProcessor.m_abilityVariables.CriticalStrikeMultiplier = float.Parse(query.GetString("param_value"));
                            break;
                        }
                    case "skill crit base ability":
                        {
                            commandProcessor.m_abilityVariables.CriticalSkillBaseAbility = float.Parse(query.GetString("param_value"));
                            break;
                        }
                    case "skill crit max chance":
                        {
                            commandProcessor.m_abilityVariables.CriticalSkillMaxChance = float.Parse(query.GetString("param_value"));
                            break;
                        }
                    case "skill crit multiplier":
                        {
                            commandProcessor.m_abilityVariables.CriticalSkillMultiplier = float.Parse(query.GetString("param_value"));
                            break;
                        }
                    case "gold crit base ability":
                        {
                            commandProcessor.m_abilityVariables.LuckyGoldBaseAbility = float.Parse(query.GetString("param_value"));
                            break;
                        }
                    case "gold crit max chance":
                        {
                            commandProcessor.m_abilityVariables.LuckyGoldMaxChance = float.Parse(query.GetString("param_value"));
                            break;
                        }
                    case "gold crit multiplier":
                        {
                            commandProcessor.m_abilityVariables.LuckyGoldMultiplier = float.Parse(query.GetString("param_value"));
                            break;
                        }
                    case "xp crit base ability":
                        {
                            commandProcessor.m_abilityVariables.LuckyXpBaseAbility = float.Parse(query.GetString("param_value"));
                            break;
                        }
                    case "xp crit max chance":
                        {
                            commandProcessor.m_abilityVariables.LuckyXpMaxChance = float.Parse(query.GetString("param_value"));
                            break;
                        }
                    case "xp crit multiplier":
                        {
                            commandProcessor.m_abilityVariables.LuckyXpMultiplier = float.Parse(query.GetString("param_value"));
                            break;
                        }

                    default:
                        {
                            break;
                        }
                }
            }
            query.Close();
        }

        public void LoadWorldParams(Database db)
        {
            SqlQuery query = new SqlQuery(db, "select * from world_params");
            if (query.HasRows == true)
            {
                while (query.Read())
                {
                    string param = query.GetString("param_name");

                    switch (param)
                    {
                        case "last quest clear time":
                            {

                                commandProcessor.m_lastQuestClearDown = query.GetDateTime("param_value");
                                break;
                            }

                        // PDH - Parameters for Server Bounty Manager
                        case "bounty_last_clear_time":
                        case "bounty_clear_time":
                        case "bounty_max_free_bounties":
                        case "bounty_max_paid_bounties":
                        case "bounty_paid_token":
                        case "bounty_max_total_bounties":
                        case "bounty_max_concurrent_bounties":
                        case "bounty_minimum_level_for_bounties":
                            {
                                ServerBountyManager.ReadWorldParam(param, query.GetString("param_value"));
                                break;
                            }

                        //param_name "last quest clear time"
                        default:
                            {
                                break;
                            }
                    }
                }
            }
            query.Close();
        }

        private List<int> LoadImmobileMobIDsFile()
        {
            List<int> immovableMobIDs = new List<int>(4);

            string configFilepath = Path.GetDirectoryName(Application.ExecutablePath) + "\\server_immovableMobIDs.cfg";

            if (File.Exists(configFilepath) == false)
            {
                Program.Display("+++ Expected to find an immovable mob id config at " + configFilepath);
                return immovableMobIDs;
            }

            using (StreamReader file = new StreamReader(configFilepath))
            {
                while (file.EndOfStream == false)
                {
                    string lineStr = file.ReadLine();
                    int indexOfCommentStart = lineStr.IndexOf(' ');

                    if (indexOfCommentStart == -1)
                        indexOfCommentStart = lineStr.IndexOf('#');

                    if (indexOfCommentStart != -1)
                        lineStr = lineStr.Substring(0, indexOfCommentStart);

                    int parsedID;
                    if (int.TryParse(lineStr, out parsedID))
                    {
                        Program.Display("+++ PARSED IMMOBILE MOB ID " + parsedID);
                        immovableMobIDs.Add(parsedID);
                    }
                    else
                    {
                        Program.Display("+++ INVALID IMMOBILE MOB ID " + lineStr);
                    }
                }
            }

            return immovableMobIDs;
        }

        private void LoadAndSetupZones()
        {
            Program.Display("Loading Server Configs");
            SqlQuery configQuery = new SqlQuery(commandProcessor.m_dataDB, "select * from server_configs");
            while (configQuery.Read())
            {
                int serverConfigID = configQuery.GetInt32("server_config_id");
                string ipaddress = configQuery.GetString("ipaddress");
                int portno = configQuery.GetInt32("portno");
            }
            configQuery.Close();

            // we are querying for all player accessible zones, 93 is the castle, everything else above 89 is test stuff
            SqlQuery zoneQuery = new SqlQuery(commandProcessor.m_dataDB, "select * from zones where zone_id>0 and (zone_id < 90 or zone_id = 93)");

            if (zoneQuery.HasRows)
            {
                while (zoneQuery.Read())
                {
                    startTime = NetTime.Now;
                    Program.Display("Loading Zone " + zoneQuery.GetString("zone_name"));
                    Zone newZone = new Zone(commandProcessor.m_dataDB, zoneQuery);
                    commandProcessor.m_zones.Add(newZone);
                    Program.MainForm.AddZoneToComboBox(zoneQuery.GetInt32("zone_id").ToString().PadLeft(2) + " " + zoneQuery.GetString("zone_name"));
                }
            }
            Program.MainForm.SelectZoneComboBox( 0);
            zoneQuery.Close();
        }

        private void LoadClansFromDatabase()
        {
            SqlQuery query = new SqlQuery(commandProcessor.m_worldDB, "select * from clan_details");
            while (query.Read())
            {

                Clan newClan = new Clan(query);
                if (Program.m_LogSysClan)
                    Program.Display("loaded clan: " + newClan.ClanName);
                commandProcessor.m_clanList.Add(newClan);
            }
            query.Close();

            LoadClanMembersFromDatabase();

            for (int i = 0; i < commandProcessor.m_clanList.Count; i++)
            {
                Clan currentClan = commandProcessor.m_clanList[i];
                currentClan.checkHasLeader(commandProcessor.m_worldDB);
                if (currentClan.Leader == null)
                {
                    commandProcessor.m_clanList.RemoveAt(i);
                    i--;
                }
            }
        }

        private void LoadClanMembersFromDatabase()
        {
            int clanCounter = 0;

            SqlQuery leadersQuery = new SqlQuery(Program.processor.m_worldDB, "select * from clan_members where clan_rank=" + (int)Clan.CLAN_RANKS.LEADER + " order by clan_id,character_id");
            while (leadersQuery.Read())
            {
                int clan_id = leadersQuery.GetInt32("clan_id");
                int character_id = leadersQuery.GetInt32("character_id");
                while (clanCounter < commandProcessor.m_clanList.Count && commandProcessor.m_clanList[clanCounter].ClanID < clan_id)
                {
                    clanCounter++;
                }
                if (clanCounter < commandProcessor.m_clanList.Count && commandProcessor.m_clanList[clanCounter].ClanID == clan_id)
                {
                    Clan clan = commandProcessor.m_clanList[clanCounter];
                    clan.AddLeaderFromDatabase(character_id);
                }
            }
            leadersQuery.Close();

            clanCounter = 0;
            SqlQuery membersQuery = new SqlQuery(Program.processor.m_worldDB, "select * from clan_members cl join character_details ch on (cl.character_id=ch.character_id) where clan_rank<" + (int)Clan.CLAN_RANKS.LEADER + "  and deleted=0 and last_logged_in>'" + DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd HH:mm:ss") + "' order by cl.clan_id,cl.character_id");
            while (membersQuery.Read())
            {
                int clan_id = membersQuery.GetInt32("clan_id");
                while (clanCounter < commandProcessor.m_clanList.Count && commandProcessor.m_clanList[clanCounter].ClanID < clan_id)
                {
                    clanCounter++;
                }
                if (clanCounter < commandProcessor.m_clanList.Count && commandProcessor.m_clanList[clanCounter].ClanID == clan_id)
                {
                    Clan clan = commandProcessor.m_clanList[clanCounter];
                    clan.AddClanMemberFromDatabase(membersQuery);

                }
            }
            membersQuery.Close();
        }

        private void LoadAchievementTargets()
        {
            SqlQuery achievementTemplatesQuery = new SqlQuery(commandProcessor.m_dataDB, "select * from achievements");
            while (achievementTemplatesQuery.Read())
            {
                int achievement_type = achievementTemplatesQuery.GetInt32("achievement_id");
                string code = achievementTemplatesQuery.GetString("achievement_code");
                string description = achievementTemplatesQuery.GetString("description");
                double target = achievementTemplatesQuery.GetDouble("target");
                bool rare = achievementTemplatesQuery.GetBoolean("rare");
                AchievementTemplate template = new AchievementTemplate((AchievementsManager.ACHIEVEMENT_TYPE)achievement_type, code, description, target, rare);
                commandProcessor.m_achievement_templates.Add(template);
            }
            achievementTemplatesQuery.Close();
        }

        private void LoadInRandomStrings()
        {
            SqlQuery query = new SqlQuery(commandProcessor.m_dataDB, "select * from word_list");
            if (query.HasRows)
            {
                while ((query.Read()))
                {
                    string currentString = query.GetString("word");
                    commandProcessor.m_randomStrings.Add(currentString);
                }

            }
            query.Close();

        }

        private void LoadEquipmentSets(Database db)
        {
            SqlQuery query = new SqlQuery(db, "select * from equipment_sets");
            if (query.HasRows == true)
            {
                while (query.Read())
                {
                    int equipmentSetID = query.GetInt32("equipment_set_id");
                    string equipmentSetName = query.GetString("equipment_set_name");
                    EquipmentSet newSet = new EquipmentSet(db, equipmentSetID, equipmentSetName);
                    commandProcessor.m_equipmentSets.Add(newSet);
                }
            }
            query.Close();
        }

        private void HaltServerImmediately()
        {
            Program.m_abortGracefully = false;
            commandProcessor.m_backgroundThreadFinished = true;
            Application.Exit();
        }
    }
}
