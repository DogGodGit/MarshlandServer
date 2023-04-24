
//#define SHOW_DAMAGE


using System;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;

using Lidgren.Network;
using MainServer.DailyLoginReward;
using SamplesCommon;
using System.IO;
using log4net;
using log4net.Config;
using System.Collections.Generic;
using MainServer.player_offers;
using System.Reflection;
using MainServer.Localise;

namespace MainServer
{
    enum UPDATE_ACHIEVEMENTS
    {
        NONE = 0,
        ALL = 1,
        CHARACTER_ONLY = 2
    }


    static class Program
    {
        public enum SERVER_ROLE
        {
            WORLD_SERVER = 0,
            ZONE_SERVER = 1
        }

        private static float k_updateLastHeardFromIntervalSecs = 5f;

        public static SERVER_ROLE m_serverRole = SERVER_ROLE.WORLD_SERVER;
        public static int m_serverID;
        public static int m_worldID;
        public static string m_ServerName = "";
        public static Form1 MainForm;
        public static NetServer Server;
        public static NetPeerSettingsWindow SettingsWindow;
        public static CommandProcessor processor;

        //public static Timer updateLoopTimer;
        private static Thread m_updateLoopThread;
        internal static bool m_exiting = false;

        public static Random m_rand = new Random();
        public static readonly ILog comprehensiveLog = LogManager.GetLogger("Comprehensive");
        public static readonly ILog databaseExceptionsLog = LogManager.GetLogger("DatabaseExceptions");

        public static bool m_showAllMsgs = false;
        public static bool m_showText = true;
        public static bool m_AutoScrollText = true;
        public static int m_max_users = 0;
        public static double m_worldUpdatePeriod = 50;
        public static DateTime m_lastWorldUpdateTime = DateTime.Now;

        public static double m_lastStatsUpdate = NetTime.Now;
        public static double m_lastHeardFromLastUpdate = NetTime.Now;
        public static double m_messageTime = 0;
        public static double m_maxMessageTime = 0;
        private static string m_maxMessageType = "";
        private static double m_individualMessageMaxTime = 0;
        public static double m_waitMessageTime = 0;
        public static double m_idleStart = NetTime.Now;
        public static double m_idleTime = 0;
        public static double m_updateTime = 0;
        public static double m_updateStartTime = 0;
        public static double m_maxUpdateTime = 0;
        public static double m_statsTime = 0;
        public static double m_combatUpdateTime = 0;
        public static double m_playerUpdateTime = 0;
        public static double m_mobUpdateTime = 0;
        public static double m_otherUpdateTime = 0;

        public static double m_bootTime = 0;
        public static string m_windowTitle;
        public static int m_last_MessaageInCount = 0;
        public static int m_last_MessageOutCount = 0;
        public static int m_maxMessagesDequeue = 1000;
        public static int m_maxMessagesDequeued = 0;
        public const int MAX_PVE_LEVEL = 500;
        public const int MAX_PROFESSION_LEVEL = 500;
        public static Int64[] m_levelRequirements = new Int64[MAX_PVE_LEVEL + 2];
        public static Int64[] m_professionLevelRequirements = new Int64[MAX_PROFESSION_LEVEL + 2];

        public static double m_AHUpdateTime = 0.0f;
        public static double m_AHListingUpdateTime = 0.0f;
        public static double m_AHExpiringListings = 0.0f;
        public static double m_AHActiveListings = 0.0f;

        public static Int64[] m_pvpLevelRequirements = new Int64[33];
        internal static DateTime m_referenceDate = new DateTime(2011, 1, 1);
        public static int m_processID = -1;

        public static bool m_initialised = false;

        public static string m_hubConStr = "";
        public static string m_worldConStr = "";
        public static string m_dataConStr = "";
        public static int m_updateLoops;
        public static int m_statsUpdateCounter = 0;
        public static bool m_usingThreads = false;
        public static string m_mailHandlerIP = "";
        static SMTPHandler m_mailHandler = null;
        public static bool m_StopOnError = true;
        /// <summary>
        /// test servers will react differently from live servers
        /// 0 // take no action
        /// 1 // debug actions
        /// 2 // full client kicking action
        /// </summary>
        public static int m_kickChecksumFailures = 0;
        /// <summary>
        /// test servers will react differently from live servers
        /// 0 // take no action
        /// 1 // debug actions
        /// 2 // release actions
        /// </summary>
        public static int m_usesNotifications = 0;
        /// <summary>
        /// 0 trialPay is fully disabled
        /// 1 trialpay receipts will be checked but will not be offered
        /// 2 trialpay receipts will be checked and offered
        /// </summary>
        public static int m_trialpayActive = 0;
        /// <summary>
        /// 0 w3i is fully disabled
        /// 1 w3i receipts will be checked but will not be offered
        /// 2 w3i receipts will be checked and offered
        /// </summary>
        public static int m_w3iActive = 2;

        /// <summary>
        /// 0 w3i is fully disabled
        /// 1 w3i receipts will be checked but will not be offered
        /// 2 w3i receipts will be checked and offered
        /// </summary>
        public static int m_superSonicActive = 2;

        // 0 disabled, 1 receipts checked, 2 receipts checked and offered
        public static int m_fyberActive = 2;
        public static bool m_fyberVideoActive = false;

        // 0 disabled, 1 safe mode, 2 online
        public static int m_auctionHouseActive = 2;
        public static bool m_resetAHDurations = true;

        public static bool m_signpostingOn = false;
        public static bool m_offerPopupActive = false;
        public static bool m_LogInactivityUpdates = false;
        public static int m_inactivity_timeout = 600;
        public static bool m_LogPartitionUpdates = false;
        public static bool m_LogInterestLists = false;
        public static bool m_LogDamage = false;
        public static bool m_LogPathingErrors = false;
        public static bool m_LogAIDebug = false;

        public static bool m_LogSysSkills = false;
        public static bool m_LogSysFriends = false;
        public static bool m_LogSysClan = false;
        public static bool m_LogSysBattle = false;
        public static bool m_LogSysParty = false;
        public static bool m_LogSysBlock = false;
        public static UPDATE_ACHIEVEMENTS m_SendAchievements = UPDATE_ACHIEVEMENTS.NONE;
        public static bool m_Aggro_debugging = false;
        public static bool m_A_Star_Debugging = false;
        public static bool m_LogNonSpawns = false;
        public static bool m_LogSpawns = false;
        public static bool m_LogRanking = false;
        public static bool m_LogQuests = false;
        public static bool m_abortGracefully = true;
        public static bool m_LogAnalytics = false;
        public static bool m_RemoveAllMobs = false;
        public const float m_defaultRangeMultiplier = 1.0f;
        public static float m_aggroRangeMultiplier = m_defaultRangeMultiplier;
        public static string m_ServerEmail = "support@onethumbmobile.com";
        public static string m_testAccountToken = "";
        public static float m_longMessageThreshold = 0.050f;
        public static float m_longMobUpdateThreshold = 0.050f;
        public static float m_longZoneUpdateThreshold = 0.050f;
        public static bool m_AIMapEnabled = true;
        public static bool m_CollisionsEnabled = true;

        private static double s_diagnosticRemainingTimeTilTaskNameChange = 0f;
        private static double s_lastDiagnosticTaskLabelUpdate = 0;

        private const float k_diagnosticBGTaskChangeMinTimeSecs = 0.5f;

		private static double m_lastLogDisplay = 0;

		static internal SMTPHandler MailHandler
        {
            get { return m_mailHandler; }
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            MainForm = new Form1();

            if (m_StopOnError)
            {
                init();

            }
            else
            {
                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

                try
                {
                    init();
                }
                catch (Exception ex)
                {
                    Display("Error in app init : " + ex.Message + " : " + ex.StackTrace);
                }
            }
        }

        public static void init()
        {
            Display("Initializing");

            // Init localiser before anything
            // In case that some error happen, app will exit
            Localiser.InitTextDB();


            Process currentProcess = Process.GetCurrentProcess();
            int processID = currentProcess.Id;
            m_processID = processID;
            string configstr = "";
            NetPeerConfiguration config = new NetPeerConfiguration("Chat");
            config.MaximumConnections = 512;
            using (
                StreamReader file = new StreamReader(Path.GetDirectoryName(Application.ExecutablePath) + "\\serverconfig.cfg"))
            {
                while (file.EndOfStream == false)
                {
                    string lineStr = file.ReadLine();
                    if (lineStr.Length > 1 && lineStr[0] == '/' && lineStr[1] == '/')
                        continue;
                    string[] lineSplit = lineStr.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                    if (lineSplit.Length > 0)
                    {
                        string paramName = lineSplit[0].Trim();
                        string paramValue = lineSplit[1].Trim();
                        switch (paramName)
                        {
                            case "SERVER_ROLE":
                                {
                                    m_serverRole = (SERVER_ROLE)Int32.Parse(paramValue);
                                    break;
                                }
                            case "SERVER_ID":
                                {
                                    m_serverID = Int32.Parse(paramValue);
                                    break;
                                }
                            case "WORLD_ID":
                                {
                                    m_worldID = Int32.Parse(paramValue);
                                    DisplayDelayed("WorldID: " + m_worldID);
                                    break;
                                }
                            case "PORT":
                                {
                                    config.Port = Int32.Parse(paramValue);
                                    DisplayDelayed("Port: " + config.Port);
                                    break;
                                }
                            case "HUB_CONN_STR":
                                {
                                    m_hubConStr = paramValue;
                                    DisplayDelayed("Hub Connection: " + paramValue);
                                    break;
                                }
                            case "WORLD_CONN_STR":
                                {
                                    m_worldConStr = paramValue;
                                    break;
                                }
                            case "MAX_USERS":
                                {
                                    m_max_users = Int32.Parse(paramValue);
                                    break;
                                }
                            case "SERVER_NAME":
                                {
                                    m_ServerName = paramValue;
                                    break;
                                }
                            case "DATA_CONN_STR":
                                {
                                    m_dataConStr = paramValue;
                                    break;
                                }
                            case "USES_THREADS":
                                {
                                    if (Int32.Parse(paramValue) == 1)
                                    {
                                        m_usingThreads = true;
                                    }
                                    break;
                                }
                            case "MAIL_HANDLER":
                                {
                                    m_mailHandlerIP = paramValue;
                                    break;
                                }
                            case "STOP_ON_ERROR":
                                {
                                    if (Int32.Parse(paramValue) == 0)
                                    {
                                        m_StopOnError = false;
                                    }
                                    break;
                                }
                            case "LOG_INACTIVITY":
                                {
                                    if (Int32.Parse(paramValue) == 1)
                                    {
                                        m_LogInactivityUpdates = true;
                                    }
                                    break;
                                }
                            case "INACTIVITY":
                                {
                                    m_inactivity_timeout = Int32.Parse(paramValue);
                                    break;
                                }
                            case "LOG_PARTITION_UPD":
                                {
                                    if (Int32.Parse(paramValue) == 1)
                                    {
                                        m_LogPartitionUpdates = true;
                                    }
                                    break;
                                }
                            case "LOG_INTEREST_LIST":
                                {
                                    if (Int32.Parse(paramValue) == 1)
                                    {
                                        m_LogInterestLists = true;
                                    }
                                    break;
                                }
                            case "LOG_PATHING_ERRS":
                                {
                                    if (Int32.Parse(paramValue) == 1)
                                    {
                                        m_LogPathingErrors = true;
                                    }
                                    break;
                                }
                            case "LOG_AI_DEBUG":
                                {
                                    if (Int32.Parse(paramValue) == 1)
                                    {
                                        m_LogAIDebug = true;
                                    }
                                    break;
                                }

                            case "LOG_SYS_SKILLS":
                                {
                                    if (Int32.Parse(paramValue) == 1)
                                    {
                                        m_LogSysSkills = true;
                                    }
                                    break;
                                }
                            case "LOG_SYS_FRIENDS":
                                {
                                    if (Int32.Parse(paramValue) == 1)
                                    {
                                        m_LogSysFriends = true;
                                    }
                                    break;
                                }
                            case "LOG_SYS_CLAN":
                                {
                                    if (Int32.Parse(paramValue) == 1)
                                    {
                                        m_LogSysClan = true;
                                    }
                                    break;
                                }
                            case "LOG_SYS_PARTY":
                                {
                                    if (Int32.Parse(paramValue) == 1)
                                    {
                                        m_LogSysParty = true;
                                    }
                                    break;
                                }
                            case "LOG_SYS_BLOCK":
                                {
                                    if (Int32.Parse(paramValue) == 1)
                                    {
                                        m_LogSysBlock = true;
                                    }
                                    break;
                                }
                            case "LOG_AGGRO_DEBUG":
                                {
                                    if (Int32.Parse(paramValue) == 1)
                                    {
                                        m_Aggro_debugging = true;
                                    }
                                    break;
                                }
                            case "LOG_A_STAR_DEBUG":
                                {
                                    if (Int32.Parse(paramValue) == 1)
                                    {
                                        m_A_Star_Debugging = true;
                                    }
                                    break;
                                }
                            case "SEND_ACHIEVEMENTS":
                                {
                                    m_SendAchievements = (UPDATE_ACHIEVEMENTS)Int32.Parse(paramValue);

                                    break;
                                }
                            case "LOG_NON_SPAWNS":
                                {
                                    if (Int32.Parse(paramValue) == 1)
                                    {
                                        m_LogNonSpawns = true;
                                    }
                                    break;
                                }
                            case "LOG_SPAWNS":
                                {
                                    if (Int32.Parse(paramValue) == 1)
                                    {
                                        m_LogSpawns = true;
                                    }
                                    break;
                                }
                            case "LOG_RANKS":
                                {
                                    if (Int32.Parse(paramValue) == 1)
                                    {
                                        m_LogRanking = true;
                                    }
                                    break;
                                }
                            case "LOG_QUESTS":
                                {
                                    if (Int32.Parse(paramValue) == 1)
                                    {
                                        m_LogQuests = true;
                                    }
                                    break;
                                }
                            case "DEBUG_DATABASE":
                                {
                                    if (Int32.Parse(paramValue) == 1)
                                    {
                                        Database.debug_database = true;
                                    }
                                    break;
                                }
                            case "EMAIL_ADDRESS":
                                {
                                    m_ServerEmail = paramValue;
                                    break;
                                }
                            case "KICK_CHECKSUM_FAILURES":
                                {
                                    m_kickChecksumFailures = Int32.Parse(paramValue);
                                    /* if (Int32.Parse(paramValue) == 1)
                                     {
                                         m_kickChecksumFailures = true;
                                     }
                                     else
                                     {
                                         m_kickChecksumFailures = false;
                                     }*/
                                    break;
                                }
                            case "DEVICE_NOTIFICATIONS":
                                {
                                    m_usesNotifications = Int32.Parse(paramValue);
                                    break;
                                }
                            case "NOTIFICATIONS_P12":
                                {
                                    break;
                                }
                            case "LOG_ANALYTICS":
                                {
                                    if (Int32.Parse(paramValue) == 1)
                                    {
                                        m_LogAnalytics = true;
                                    }
                                    else
                                    {
                                        m_LogAnalytics = false;
                                    }
                                    break;
                                }
                            case "TEST_ACCOUNT_TOKEN":
                                {
                                    m_testAccountToken = paramValue;
                                    break;
                                }
                            case "REMOVE_ALL_MOBS":
                                {
                                    if (Int32.Parse(paramValue) == 1)
                                    {
                                        m_RemoveAllMobs = true;
                                    }
                                    else
                                    {
                                        m_RemoveAllMobs = false;
                                    }
                                    break;
                                }
                            case "TRIALPAY_ACTIVE":
                                {
                                    //m_trialpayActive = Int32.Parse(paramValue);
                                    break;
                                }
                            case "W3I_ACTIVE":
                                {
                                    m_w3iActive = Int32.Parse(paramValue);
                                    break;
                                }
                            case "SUPERSONIC_ACTIVE":
                                {
                                    m_superSonicActive = Int32.Parse(paramValue);
                                    break;
                                }
                            case "FYBER_ACTIVE":
                                {
                                    m_fyberActive = Int32.Parse(paramValue);
                                    break;
                                }
                            case "FYBER_VIDEO_ACTIVE":
                                {
                                    if (Int32.Parse(paramValue) == 1)
                                        m_fyberVideoActive = true;
                                    break;
                                }
                            case "SIGNPOSTS_ACTIVE":
                                {
                                    m_signpostingOn = (Int32.Parse(paramValue) == 1);
                                    break;
                                }
                            case "SPECIAL_OFFERS_ACTIVE":
                                {
                                    SpecialOfferTemplate.SPECIAL_OFFERS_ACTIVE = (Int32.Parse(paramValue) == 1);
                                    break;
                                }
                            case "OFFER_POPUP_ACTIVE":
                                {
                                    m_offerPopupActive = (Int32.Parse(paramValue) == 1);

                                    break;
                                }
                            case "TINY_AGGRO_RADIUS":
                                {
                                    if (Int32.Parse(paramValue) == 1)
                                    {
                                        m_aggroRangeMultiplier = 0.01f;
                                    }
                                    else
                                    {
                                        m_aggroRangeMultiplier = m_defaultRangeMultiplier;
                                    }
                                    break;
                                }
                            case "LONG_MESSAGE_THRESHOLD":
                                {
                                    m_longMessageThreshold = Single.Parse(paramValue);
                                    break;
                                }
                            case "LONG_MOB_UPDATE_THRESHOLD":
                                {
                                    m_longMobUpdateThreshold = Single.Parse(paramValue);
                                    break;
                                }
                            case "LONG_ZONE_UPDATE_THRESHOLD":
                                {
                                    m_longZoneUpdateThreshold = Single.Parse(paramValue);
                                    break;
                                }
                            case "AUCTION_HOUSE_ACTIVE":
                                {
                                    m_auctionHouseActive = Int32.Parse(paramValue);
                                    break;
                                }
                            case "RESET_AUCTION_HOUSE_DURATIONS":
                                {
                                    m_resetAHDurations = (Int32.Parse(paramValue) == 1);
                                    break;
                                }
                            case "DAILY_REWARDS_ACTIVE":
                                {
                                    DailyRewardManager.DAILY_REWARDS_ACTIVE = (Int32.Parse(paramValue) == 1);
                                    break;
                                }
                        }
                    }
                }
                configstr = file.ReadToEnd();
            }

            // create a configuration

            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);

            if (!m_ServerName.Equals(""))
            {
                m_windowTitle = string.Format("{0} : {1}, PID {2}, Version: {3}, Uptime: 0", m_ServerName, config.Port, m_processID, fvi.FileVersion);
            }
            else
            {
                m_windowTitle = "DM " + config.Port + " PID " + m_processID + " Uptime: ";
            }

            MainForm.Text = m_windowTitle;
            m_bootTime = NetTime.Now;

            //set our icon depending on server name
            MainForm.SetIcon(m_ServerName);

            MainForm.tbMaxUsers.Text = m_max_users.ToString();
            if (!m_StopOnError)
            {
                MainForm.btnCreatePlayers.Visible = false;
                MainForm.chkRemoveAllMobs.Visible = false;
            }
            XmlConfigurator.Configure(new FileInfo("log4netconfig.xml"));

            //set up the mail
            m_mailHandler = new SMTPHandler(m_mailHandlerIP);
            Server = new NetServer(config);

            m_updateLoopThread = new Thread(new ThreadStart(AppLoop));
            m_updateLoopThread.Name = "MainUpdateLoop";
            m_updateLoopThread.Start();

            ApplicationEventHandlerClass AppEvents = new ApplicationEventHandlerClass();

            Application.ApplicationExit += new EventHandler(AppEvents.OnApplicationExit);

            Application.Run(MainForm);

        }

        public static void ReinitialiseThirdPartyOptions()
        {
            using (StreamReader file = new StreamReader(Path.GetDirectoryName(Application.ExecutablePath) + "\\serverconfig.cfg"))
            {
                while (file.EndOfStream == false)
                {
                    string lineStr = file.ReadLine();
                    string[] lineSplit = lineStr.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                    if (lineSplit.Length > 0)
                    {
                        string paramName = lineSplit[0].Trim();
                        string paramValue = lineSplit[1].Trim();
                        switch (paramName)
                        {
                            case "W3I_ACTIVE":
                                {
                                    m_w3iActive = Int32.Parse(paramValue);
                                    break;
                                }
                            case "SUPERSONIC_ACTIVE":
                                {
                                    m_superSonicActive = Int32.Parse(paramValue);
                                    break;
                                }
                        }
                    }
                }
            }
            processor.ServerControlledClientManager.RecheckSettings();
        }

        public static void Display(string text)
        {
            comprehensiveLog.Info(text);
			if (MainForm != null)
				MainForm.Display(text);
		}

        public static void DisplayDelayed(string text)
        {
			Display(text);
        }

        public static void LogDatabaseException(string text)
        {
            comprehensiveLog.Info(text);
            databaseExceptionsLog.Info("Database Exception:" + text);
            if (m_showText)
            {
                Program.Display("Database Exception:" + text);
            }
        }

        public static int getRandomNumber(int max)
        {
            if (max < 0)
            {
                max = 0;
            }

            return m_rand.Next(max);
        }

        public static double getRandomDouble()
        {
            return m_rand.NextDouble();
        }

        public static int getRandomNumberFromZero(int maxRange)
        {
            int multiplier = 1;

            if (maxRange < 0)
            {
                multiplier = -1;
                maxRange = maxRange * -1;
            }

            int randomValue = m_rand.Next(maxRange);
            randomValue = randomValue * multiplier;
            return randomValue;
        }

        #region main update loops

        /// <summary>
        /// Main update loop that's called every tick
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void AppLoop()
        {
            double lastTitleUpdate = NetTime.Now;

            while (m_exiting == false)
            {
                double timeNow = NetTime.Now;

                // update title with server uptime every second
                if (timeNow - lastTitleUpdate > 2d)
                {
                    MainForm.UpdateTitle(string.Format("{0}{1}s, Users: {2}", m_windowTitle, (int)(timeNow - m_bootTime), (processor != null && processor.m_players != null ? processor.m_players.Count : 0)));
                    lastTitleUpdate = timeNow;
                }

                if (!m_initialised)
                {
                    initializeLoop();
                    Thread.Sleep(500);
                }
                else
                {
                    if (m_StopOnError)
                    {
                        mainLoop();
                    }
                    else
                    {
                        try
                        {
                            mainLoop();
                        }
                        catch (Exception ex)
                        {
                            Display("Error in outer mainLoop : " + ex.Message + " : " + ex.StackTrace);
                        }
                    }

                    // prob wanna float so that idle time doesn't go above 4500 per 10000.
                    Thread.Sleep(20);

					if (timeNow - m_lastLogDisplay > 0.5)
					{
						if (MainForm != null)
						{
							//MainForm.DisplayText(m_lastLogDisplay.ToString() + "\n");

							string str = MainForm.GetPendingText();
							if (str.Length > 0)
							{
								MainForm.RemoveOldText();
								MainForm.DisplayText(str);
							}
						}
                        m_lastLogDisplay = timeNow;
					}

                   

                }
			}
        }

        
        /// <summary>
        /// Before the server runs as normal allowing players to connect, 
        /// need to finish initializing a numer of loads from the datadb and 
        /// the worlddb.  This loop handles this.
        /// </summary>
        static void initializeLoop()
        {
            // generate folder structure if missing
            Directory.CreateDirectory("transactions");
            Directory.CreateDirectory("log");

            // haven't yet started the load, so create the command processor
            // ready to initialize it and start loading
            if (processor == null)
            {
                processor = new CommandProcessor(Server, m_hubConStr, m_worldConStr, m_dataConStr);
				
                ProfanityFilter.ReadProfanityList(processor.m_dataDB);
                return;
            }

            // processor is created but not yet initializd, call this thread to 
            // load everything
            if (processor.CurrentLoadingState == LoadingState.NotInitialized)
                processor.InitializeLoadingThread();


            // processor and finished all our loading
            // set up for accepting players
            if (processor.CurrentLoadingState == LoadingState.LoadComplete)
            {
                try
                {
                    Server.Start();
                }
                catch (System.Net.Sockets.SocketException esock)
                {
                    string errorMessage = string.Format("Attempted to open server when socket was already taken. ({0}) {1}", Server.Port, esock.ToString());

                    Display(errorMessage);
                    DialogResult result = MessageBox.Show(errorMessage);

                    if (result == DialogResult.OK)
                    {
                        m_exiting = true;
                        processor.shutDown();
                        Application.Exit();
                        return;
                    }
                }

                MainForm.StartPlayerRefreshTimer();

                Int64 cumTotal = 0;
                for (int i = 1; i < MAX_PVE_LEVEL; i++)
                {
                    //NEW XP FORUMLA - CHANGED 03/02/17
                    cumTotal += (Int64)Math.Round((i + 4) * (60 + 20 * i) * (1 + Math.Pow(i, (i / 223f)) / 12f));

                    ////OLD XP FORMULA.
                    //if (i < 100)
                    //{
                    //    cumTotal += (Int64)Math.Round((i + 4) * (80 + i * 20) * (i / 80.0f + 1));
                    //}
                    //else
                    //{
                    //    cumTotal += (Int64)Math.Round((i + 4) * (80 + i * 20) * (i / 80.0f + (i - 99) / 10.0f + 1));
                    //}

                    m_levelRequirements[i] = cumTotal;
                }
                cumTotal = 0;
                for (int i = 1; i < MAX_PROFESSION_LEVEL; i++)
                {
                    cumTotal += (Int64)Math.Round((i + 4) * (30 + i * 10) * (1 + Math.Pow(i, (i / 140.0f)) / 50.0f));
                    m_professionLevelRequirements[i] = cumTotal;
                }
                cumTotal = 0;
                int seed = 500;
                for (int i = 1; i < 33; i++)
                {
                    cumTotal += seed;
                    seed = (int)(seed * 1.4);
                    m_pvpLevelRequirements[i] = cumTotal;
                }

                //we are now correctly initialised!
                m_initialised = true;

                //Set the initial season profile to the one stored in app.config
                List<string> seasonProfile = MainForm.seasonDictionary[System.Configuration.ConfigurationManager.AppSettings["currentSeason"]];
                processor.SetSeasonTweak(seasonProfile);

                Display("[Server initialised and running]");
            }
        }

        internal static void UpdateDiagnosticBackgroundTaskName(double in_elapsedTimeSecs)
        {
            // active background task name update
            s_diagnosticRemainingTimeTilTaskNameChange -= in_elapsedTimeSecs;
            
            if (s_diagnosticRemainingTimeTilTaskNameChange <= 0f)
            {
                string currentBGTask = processor.GetCurrentTaskName();

                if (currentBGTask != string.Empty)
                {
                    int numPending = processor.GetNumRemainingBGTaskNames();
                    MainForm.labelActiveTask.Text = string.Format("CurrTask({0}): {1}", numPending, currentBGTask);
                }

                s_diagnosticRemainingTimeTilTaskNameChange = k_diagnosticBGTaskChangeMinTimeSecs;
            }
        }

        /// <summary>
        /// Once everything is iniatialized (e.g. loaded all templates) 
        /// run this loop to handle the main game 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void mainLoop()
        {
            double timestart = NetTime.Now;
            m_idleTime += (timestart - m_idleStart) * 1000;
            NetIncomingMessage msg = Server.WaitMessage(50);
            double diff = (NetTime.Now - timestart) * 1000;

            m_waitMessageTime += diff;
            timestart = NetTime.Now;

            // update the current bg task label
            double timeSinceLastDiag = timestart - s_lastDiagnosticTaskLabelUpdate;

            MainForm.UpdateDiagnosticBackgroundTaskName(timeSinceLastDiag);

            s_lastDiagnosticTaskLabelUpdate = timestart;

            int numberProcessed = 0;
            double nextMessageEndProcessTime = NetTime.Now;

            while (msg != null)
            {
                double nextMessageStartTime = NetTime.Now;
                string messageType = "";
                // try
                {

                    // Display(msg.ToString());
                    switch (msg.MessageType)
                    {
                        case NetIncomingMessageType.VerboseDebugMessage:
                        case NetIncomingMessageType.DebugMessage:
                        case NetIncomingMessageType.ErrorMessage:
                        case NetIncomingMessageType.WarningMessage:
                            // print any library message
                            Display(msg.ReadString());
                            messageType = "logging";
                            break;

                        case NetIncomingMessageType.StatusChanged:
                            // print changes in connection(s) status
                            NetConnectionStatus status = (NetConnectionStatus)msg.ReadByte();
                            string reason = msg.ReadString();
                            Display(msg.SenderConnection + " status: " + status + " (" + reason + ")");
                            if (status == NetConnectionStatus.Disconnected)
                            {
                                bool laggedOut = reason.Contains("Timed out after ");
                                string dcReason = "";
                                if (laggedOut)
                                {
                                    dcReason = "LaggedOut";
                                }
                                processor.disconnect(msg.SenderConnection, false, dcReason);
                            }
                            messageType = "status";
                            break;

                        case NetIncomingMessageType.Data:
                            if (m_StopOnError)
                            {
                                NetOutgoingMessage om = processor.processMessage(msg, ref messageType);
                            }
                            else
                            {
                                // Forward all data to all clients (including sender for debugging purposes)
                                try
                                {
                                    NetOutgoingMessage om = processor.processMessage(msg, ref messageType);
                                }
                                catch (Exception ex)
                                {
                                    String outstr = "";
                                    byte[] buff = msg.PeekDataBuffer();
                                    for (int i = 0; i < msg.LengthBytes; i++)
                                    {
                                        outstr += "," + buff[i].ToString("X2");
                                    }
                                    if (outstr.Length > 0)
                                        outstr = outstr.Substring(1);
                                    Display("Error in process message " + outstr + " : " + ex.Message + " : " + ex.StackTrace);
                                }
                                //Display(msg.ToString());
                            }
                            break;
                        case NetIncomingMessageType.DiscoveryRequest:
                            try
                            {
                                NetOutgoingMessage outmsg = Server.CreateMessage();
                                byte available = 1;
                                byte availability = 1;
                                if (processor.m_players.Count >= m_max_users)
                                {
                                    available = 0;
                                    availability = 2;
                                }
                                outmsg.Write(available);
                                outmsg.Write(availability);
                                Server.SendDiscoveryResponse(outmsg, msg.SenderEndPoint);
                            }
                            catch (Exception ex)
                            {
                                Display("Error in Discovery process " + ex.Message);
                            }
                            break;
                    }
                }

                nextMessageEndProcessTime = NetTime.Now;

                // log the type of the longest-to-process message
                double currentMessageTime = nextMessageEndProcessTime - nextMessageStartTime;

                if (currentMessageTime > m_individualMessageMaxTime)
                {
                    m_maxMessageType = string.Format("{0} {1}ms", messageType, (long)(currentMessageTime * 1000));
                    m_individualMessageMaxTime = currentMessageTime;
                }

                numberProcessed++;
                Server.Recycle(msg);
                msg = null;
                if (numberProcessed <= m_maxMessagesDequeue && (nextMessageEndProcessTime - timestart) < 1.0f)
                {
                    msg = Server.ReadMessage();
                }
            }

            if (numberProcessed > m_maxMessagesDequeued)
            {
                m_maxMessagesDequeued = numberProcessed;
            }
                        

            double totalMessageTime = (nextMessageEndProcessTime - timestart) * 1000;
            if (totalMessageTime > m_maxMessageTime)
            {
                m_maxMessageTime = totalMessageTime;

            }

            m_messageTime += totalMessageTime;

            m_updateStartTime = SecondsFromReferenceDate();

            double localUpdateStartTime = NetTime.Now;
            processor.Update();

            double endLoopTime = NetTime.Now;

            double updateTime = (endLoopTime - localUpdateStartTime) * 1000;
            //    Program.Display("update time=" + updateTime);
            m_updateTime += updateTime;
            if (updateTime > m_maxUpdateTime)
            {
                m_maxUpdateTime = updateTime;
            }

            const int k_statusDetailsUpdateSeconds = 5;
            
            if ((endLoopTime - m_lastStatsUpdate) > k_statusDetailsUpdateSeconds)
            {
                MainForm.UpdateStatusDetails();
            }
            m_idleStart = endLoopTime;
        }

        internal static void UpdateStatusDetails()
        {
            //Program.Display("update time=" + ((int)m_updateTime) + " for " + m_updateLoops + " loops");
            int incomingMessages = Server.Statistics.ReceivedPackets - m_last_MessaageInCount;
            int ic = processor.m_dataDB.m_syncStatements.Count + processor.m_universalHubDB.m_syncStatements.Count + processor.m_worldDB.m_syncStatements.Count;

            double averageUpdateLoopTime = (m_updateTime / m_updateLoops);

            MainForm.lblMessageUpdateTime.Text = "MessageProcessing: " + ((int)m_messageTime);
            MainForm.labelServerMsgWaitTime.Text = "WaitingForServerMsg: " + ((int)m_waitMessageTime);
            MainForm.labelMainUpdateTime.Text = "Updating: " + ((int)m_updateTime);
            MainForm.labelNonUpdating.Text = "Non-Updating: " + ((int)m_idleTime);
            MainForm.labelPlayerInfoRefresh.Text = "PlayerInfoRefresh: " + m_statsTime;
            MainForm.labelReceivedPackets.Text = "ReceivedPackets: " + incomingMessages;
            MainForm.labelOutgoingPackets.Text = "OutgoingPackets: " + (Server.Statistics.SentPackets - m_last_MessageOutCount);
            MainForm.labelOutstandingIncMessages.Text = "OutstandingIncMessages: " + Server.OutstandingIncomingMessages();
            MainForm.labelProcessedMessages.Text = "ProcessedMsgs: " + m_maxMessagesDequeued;
            MainForm.labelPendingBGTasks.Text = "PendingBGTasks: " + processor.m_backgroundTasks.Count;
            MainForm.labelInventoryPoolCount.Text = "InvPoolCount: " + processor.m_inventoryPool.Count;
            MainForm.labelPendingSyncStatements.Text = "PendingSyncStatements: " + ic;
            MainForm.labelUpdateLoops.Text = "UpdateLoops: " + m_updateLoops;
            MainForm.labelAvrUpdateLoop.Text = "AvrUpdateLoop: " + averageUpdateLoopTime.ToString("F2");
            MainForm.labelAvrMessageProcess.Text = "AvrMessageProcessTime: " + (incomingMessages > 0 ? m_messageTime / incomingMessages : 0).ToString("F2");
            MainForm.labelMaxMessageProcess.Text = "MaxMessageProcessTime: " + m_maxMessageTime.ToString("F2");
            MainForm.labelMaxUpdateTime.Text = "MaxUpdateTime: " + m_maxUpdateTime.ToString("F2");
            MainForm.labelAHUpdateTime.Text = "AHUpdateTime: " + m_AHUpdateTime.ToString("F2");
            MainForm.labelAHListingUpdateTime.Text = "AHListingUpdateTime: " + m_AHListingUpdateTime.ToString("F2");
            MainForm.labelAHExpiringListings.Text = "AHExpiringListings: " + m_AHExpiringListings.ToString("F0");
            MainForm.labelAHActiveListings.Text = "AHActiveListings: " + m_AHActiveListings.ToString("F0");

            MainForm.lblLongestMessageProcess.Text = "Expensive: " + m_maxMessageType;

            // colour handling
            SetColourForRange(MainForm.lblMessageUpdateTime, m_messageTime, 180.0, 300.0);
            SetColourForRange(MainForm.labelMainUpdateTime, m_updateTime, 2750.0, 3500.0);

            SetColourForRangeInt(MainForm.labelPendingBGTasks, processor.m_backgroundTasks.Count, 20, 40);
            SetColourForRangeInt(MainForm.labelPendingSyncStatements, ic, 4, 8);

            SetColourForRange(MainForm.labelAvrUpdateLoop, averageUpdateLoopTime, 22.0, 40.0);
            SetColourForRange(MainForm.labelMaxUpdateTime, m_maxUpdateTime, 45.0, 60.0);


            appendToFile("log\\times" + DateTime.Now.ToString("yyyyMMdd") + ".csv", DateTime.Now.ToString("HH:mm:ss") + "," + ((int)m_messageTime) + "," + ((int)m_waitMessageTime) + "," + ((int)m_updateTime) + "," + (Server.Statistics.ReceivedPackets - m_last_MessaageInCount) + "," + (Server.Statistics.SentPackets - m_last_MessageOutCount) + "," + Server.OutstandingIncomingMessages() + "," + m_maxMessagesDequeued + "," + processor.m_players.Count + "," + ((int)m_combatUpdateTime) + "," + ((int)m_playerUpdateTime) + "," + ((int)m_mobUpdateTime) + "," + ((int)m_otherUpdateTime));

            m_messageTime = 0;
            m_maxMessageTime = 0;
            m_waitMessageTime = 0;
            m_idleTime = 0;
            m_statsTime = 0;
            m_updateTime = 0;
            m_maxUpdateTime = 0;
            m_combatUpdateTime = 0;
            m_playerUpdateTime = 0;
            m_mobUpdateTime = 0;
            m_otherUpdateTime = 0;
            m_updateLoops = 0;
            m_lastStatsUpdate = NetTime.Now;
            m_last_MessaageInCount = Server.Statistics.ReceivedPackets;
            m_last_MessageOutCount = Server.Statistics.SentPackets;
            m_maxMessagesDequeued = 0;

            if (m_lastStatsUpdate - m_lastHeardFromLastUpdate > k_updateLastHeardFromIntervalSecs) // opti to avoid additional nettime.now call
            {
                processor.updateLastHeardFrom();
                m_lastHeardFromLastUpdate = m_lastStatsUpdate; // opti to avoid additional nettime.now call
            }

            ++m_statsUpdateCounter;

            m_individualMessageMaxTime = 0; // reset max message tracking so we can get the highest per update window
        }


        private static void SetColourForRangeInt(Label in_label, int in_value, int in_yellowVal, int in_redVal)
        {
            // colour handling
            if (in_value > in_redVal)
                in_label.ForeColor = Color.Red;
            else if (in_value > in_yellowVal)
                in_label.ForeColor = Color.Orange;
            else
                in_label.ForeColor = Color.Black;
        }

        private static void SetColourForRange(Label in_label, double in_value, double in_yellowVal, double in_redVal)
        {
            // colour handling
            if (in_value > in_redVal)
                in_label.ForeColor = Color.Red;
            else if (in_value > in_yellowVal)
                in_label.ForeColor = Color.Orange;
            else
                in_label.ForeColor = Color.Black;
        }

        #endregion

        public static void appendToFile(string filename, string text)
        {
            if (!File.Exists(filename))
            {
                // Create a file to write to, create directory if needed.
                if (!Directory.Exists(Path.GetDirectoryName(filename)))
                    Directory.CreateDirectory(Path.GetDirectoryName(filename));

                using (StreamWriter sw = File.CreateText(filename))
                {
                    sw.WriteLine(text);
                }
            }

            // This text is always added, making the file longer over time
            // if it is not deleted.
            using (StreamWriter sw = File.AppendText(filename))
            {
                sw.WriteLine(text);
            }

        }
        public static void refreshPlayersTimer_Tick(object sender, EventArgs eArgs)
        {
            if (m_StopOnError)
            {
                refreshPlayers();
            }
            else
            {
                try
                {
                    refreshPlayers();
                }
                catch (Exception ex)
                {
                    Display("Error in refreshPlayers : " + ex.Message + " : " + ex.StackTrace);
                }

            }

        }
        public static void refreshPlayers()
        {
            DateTime timestart = DateTime.Now;

            //  MainForm.lvPlayerList.BeginUpdate();
            MainForm.lvPlayerList.Visible = false;
            bool hasFocus = MainForm.lvPlayerList.Focused;
            int topItemIndex = -1;
            Player topPlayer = null;
            try
            {
                if (MainForm.lvPlayerList.TopItem != null)
                {
                    MainForm.lvPlayerList.Items.IndexOf(MainForm.lvPlayerList.TopItem);
                    if (MainForm.lvPlayerList.TopItem.Tag != null)
                        topPlayer = (Player)MainForm.lvPlayerList.TopItem.Tag;
                }
            }
            catch (Exception)
            {
            }
            List<Player> oldSelected = new List<Player>();
            for (int i = 0; i < MainForm.lvPlayerList.Items.Count; i++)
            {
                if (MainForm.lvPlayerList.Items[i].Tag != null && MainForm.lvPlayerList.Items[i].Selected)
                {
                    oldSelected.Add((Player)MainForm.lvPlayerList.Items[i].Tag);

                }
            }
            MainForm.lvPlayerList.Items.Clear();
            bool topPlayerFound = false;
            for (int i = 0; i < processor.m_players.Count; i++)
            {
                ListViewItem item = new ListViewItem();
                Player player = processor.m_players[i];
                item.Text = player.m_account_id.ToString();
                item.SubItems.Add(player.m_UserName);
                item.SubItems.Add(Server.Connections.IndexOf(player.connection).ToString());
                item.Tag = player;
                if (oldSelected.IndexOf(player) > -1)
                {
                    item.Selected = true;
                }

                if (player.m_activeCharacter != null)
                {
                    item.SubItems.Add(player.m_activeCharacter.m_character_id.ToString());
                    item.SubItems.Add(player.m_activeCharacter.m_name);
                    int zone_id = (int)player.m_activeCharacter.m_zone.m_zone_id;
                    item.SubItems.Add(processor.getZone(zone_id).m_zone_name);
                    item.SubItems.Add(player.m_activeCharacter.Level.ToString());
                    item.SubItems.Add(player.m_activeCharacter.m_CharacterPosition.m_position.X.ToString("F2"));
                    item.SubItems.Add(player.m_activeCharacter.m_CharacterPosition.m_position.Z.ToString("F2"));
                }
                else
                {
                    item.SubItems.Add("");
                    item.SubItems.Add("");
                    item.SubItems.Add("");
                    item.SubItems.Add("");
                    item.SubItems.Add("");
                    item.SubItems.Add("");
                }
                string pingtime = "0";
                if (player.connection != null)
                {
                    pingtime = ((int)(player.connection.AverageRoundtripTime * 1000.0f)).ToString();
                }
                item.SubItems.Add(pingtime);

                MainForm.lvPlayerList.Items.Add(item);

            }

            //    MainForm.lvPlayerList.EndUpdate();
            try
            {

                if (MainForm.lvPlayerList.Items.Count > 0 && (topPlayer != null || topItemIndex > -1))
                {


                    MainForm.lvPlayerList.TopItem = MainForm.lvPlayerList.Items[MainForm.lvPlayerList.Items.Count - 1];

                    if (topPlayer != null)
                    {
                        for (int i = 0; i < MainForm.lvPlayerList.Items.Count; i++)
                        {
                            if (topPlayer == MainForm.lvPlayerList.Items[i].Tag)
                            {
                                topPlayerFound = true;
                                MainForm.lvPlayerList.TopItem = MainForm.lvPlayerList.Items[i];
                                break;
                            }

                        }
                    }

                    if (topPlayerFound == false && topItemIndex > -1 && topItemIndex < MainForm.lvPlayerList.Items.Count)
                    {
                        MainForm.lvPlayerList.TopItem = MainForm.lvPlayerList.Items[topItemIndex];
                    }
                }
            }
            catch (Exception)
            {
            }
            MainForm.lvPlayerList.Visible = true;
            if (hasFocus)
                MainForm.lvPlayerList.Focus();
            m_statsTime += (DateTime.Now - timestart).TotalMilliseconds;

        }
        
        static public string DecodeFrom64(string encodedData)
        {
            byte[] encodedDataAsBytes
                = Convert.FromBase64String(encodedData);
            string returnValue =
               Encoding.ASCII.GetString(encodedDataAsBytes);
            return returnValue;
        }
        
        static public double MainUpdateLoopStartTime()
        {
            /* double currentTime = 0;
             TimeSpan timeSinceReferanceDate = DateTime.Now - m_referenceDate;
             currentTime = timeSinceReferanceDate.TotalSeconds;
             */
            return m_updateStartTime;
        }

        static public double SecondsFromReferenceDate()
        {
            double currentTime = 0;
            TimeSpan timeSinceReferanceDate = DateTime.Now - m_referenceDate;
            currentTime = timeSinceReferanceDate.TotalSeconds;

            return currentTime;
        }

        static public void sendSystemMessage(String message, bool popup, bool selectedOnly)
        {
            NetConnection[] connections;
            if (selectedOnly)
            {
                List<NetConnection> connectionsList = new List<NetConnection>();
                for (int i = 0; i < MainForm.lvPlayerList.Items.Count; i++)
                {

                    if (MainForm.lvPlayerList.Items[i].Selected && MainForm.lvPlayerList.Items[i].Tag != null)
                    {
                        connectionsList.Add(((Player)MainForm.lvPlayerList.Items[i].Tag).connection);
                    }

                }
                connections = connectionsList.ToArray();
            }
            else
            {
                connections = Server.Connections.ToArray();
            }
            SYSTEM_MESSAGE_TYPE smt = SYSTEM_MESSAGE_TYPE.NONE;
            if (popup)
            {
                smt = SYSTEM_MESSAGE_TYPE.POPUP;
            }
            if (connections.Length > 0)
                processor.sendSystemMessage(message, connections, false, smt);
        }


        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                Exception ex = (Exception)e.ExceptionObject;
                
                comprehensiveLog.Error("unhandled exception information:\n\n" + ex.Message + " " + ex.StackTrace);
            }
            finally
            {

            }
        }
    }

    public class ApplicationEventHandlerClass
    {
        public void OnApplicationExit(object sender, EventArgs e)
        {
            try
            {
                Program.m_exiting = true;
                Program.processor.shutDown();
            }
            catch (NotSupportedException)
            {
            }
        }
    }
}