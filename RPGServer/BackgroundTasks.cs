using System;
using System.Collections.Generic;
using System.Linq;
using Lidgren.Network;
using MainServer.DailyLoginReward;
using MainServer.player_offers;
using System.Configuration;
using MainServer.Localise;
using MySql.Data.MySqlClient;

namespace MainServer
{
    abstract class BaseTask
    {
        public enum TaskType
        {
            CreateAccount,
            CreateCharacter,
            DeleteCharacter,
            Login,
            RequestCharList,
            StartGame,
            RequestMailSendInfo,
            LoadNewMail,
            DeleteOldMail,
            DeleteCompleteRepeatableQuests,
            RegisterEmail,
            SendEmail,
            LoadOfferData,
            ClearGuestAccount,
            ResetBounties,
            AHSendPlayerMail,
            LinkAccountToFacebook,
            ClearExpiredLogins,
            ReadFyberOffers,
            ReadNativeXOffers,
            ReadSupersonicOffers,
            AH_SendMail,
            SignpostMail
        }
        public TaskType m_TaskType;

		public override string ToString()
		{
			return m_TaskType.ToString() + (Tag != null && Tag.Length > 0 ? (" " + Tag) : "");
		}
				
		internal string Tag{ get; set; }

        internal abstract void TakeAction(CommandProcessor processor);
    }

    class CreateAccountTask : BaseTask
    {
		// #localisation
		public class CreateAccountTaskTextDB : TextEnumDB
		{
			public CreateAccountTaskTextDB() : base(nameof(CreateAccountTask), typeof(TextID)) { }

			public enum TextID
			{
				ACCOUNT_HAS_BEEN_DISABLED,	// "This account has been disabled. Please contact customer support at support@onethumbmobile.com"
				USERNAME_INVALID			// "Username invalid. Please try again"
			}
		}
		public static CreateAccountTaskTextDB textDB = new CreateAccountTaskTextDB();

		public NetConnection m_sender;
        public string m_firstName;
        public string m_lastName;
        public string m_userName;
        public string m_password;
        public string m_analyticsStr;
        public string m_uuidString;
        public int m_inappVersion=0;

        public CreateAccountTask(NetConnection sender, string firstName, string lastName, string userName, string password, string analyticsStr, string uuidString, string deviceToken)
        {
            m_TaskType = TaskType.CreateAccount;
            m_sender = sender;
            m_firstName = firstName;
            m_lastName = lastName;
            m_userName = userName;
            m_password = password;
            m_analyticsStr = analyticsStr;
            if (uuidString.IndexOf(',') > -1)
            {
                string[] uuidsplit = uuidString.Split(new char[] { ',' });
                uuidString = uuidsplit[0];
                m_inappVersion = Int32.Parse(uuidsplit[1]);
            }
            m_uuidString = uuidString;
            
            Tag = userName;
        }
        internal override void TakeAction(CommandProcessor processor)
        {
            Player curplayer = null;
            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.CreateAccountReply);
            string[] analyticsStuff = m_analyticsStr.Split(new [] { ',' });
            string deviceModel = String.Empty;
            string deviceGen = String.Empty;
            string deviceIOS = String.Empty;
            string deviceStr = String.Empty;
            try
            {
                deviceModel = analyticsStuff[0];
                deviceGen = analyticsStuff[1];
                deviceIOS = analyticsStuff[2];
                deviceStr = analyticsStuff[3];
            }
            catch (Exception)
            {
                deviceGen = "0";
            }
            //SqlQuery uuidQuery = new SqlQuery(Program.processor.m_universalHubDB, "select account_id from account_details where uuid='" + m_uuidString + "' and disabled=true");

			List<MySqlParameter> sqlParams = new List<MySqlParameter>();
			sqlParams.Add(new MySqlParameter("@uuid", m_uuidString));

			SqlQuery uuidQuery = new SqlQuery(Program.processor.m_universalHubDB, "select account_id from account_details where uuid=@uuid and disabled=true", sqlParams.ToArray());

			if (uuidQuery.Read())
            {
                outmsg.Write((byte)0);
				string locText = Localiser.GetStringByUsername(textDB, m_userName, (int)CreateAccountTaskTextDB.TextID.ACCOUNT_HAS_BEEN_DISABLED);
				outmsg.Write(locText);
				Program.DisplayDelayed("failed to create account for " + m_userName + " device disabled");
            }
            else
            {
				//Program.processor.m_universalHubDB.runCommand("insert into account_details (user_name,hashed_pwd,firstname,lastname,logged_in_world,last_login,device_model,device_gen,device_ios_version,device_str,num_sessions,account_created,uuid,in_app_version, last_logged_in_world_id) values ('" + m_userName + "','" + m_password + "','" + m_firstName + "','" + m_lastName + "'," + Program.m_worldID + ",'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + deviceModel + "'," + deviceGen + ",'" + deviceIOS + "','" + deviceStr + "',1,'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + m_uuidString + "'," + m_inappVersion + "," + Program.m_worldID + ")", true);

				int deviceGenInt = 0;
				Int32.TryParse(deviceGen, out deviceGenInt);

				sqlParams.Clear();
				sqlParams.Add(new MySqlParameter("@user_name", m_userName));
				sqlParams.Add(new MySqlParameter("@hashed_pwd", m_password));
				sqlParams.Add(new MySqlParameter("@firstname", m_firstName));
				sqlParams.Add(new MySqlParameter("@lastname", m_lastName));
				sqlParams.Add(new MySqlParameter("@logged_in_world", Program.m_worldID));
				sqlParams.Add(new MySqlParameter("@last_login", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
				sqlParams.Add(new MySqlParameter("@device_model", deviceModel));
				sqlParams.Add(new MySqlParameter("@device_gen", deviceGenInt));
				sqlParams.Add(new MySqlParameter("@device_ios_version", deviceIOS));
				sqlParams.Add(new MySqlParameter("@device_str", deviceStr));
				sqlParams.Add(new MySqlParameter("@account_created", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
				sqlParams.Add(new MySqlParameter("@uuid", m_uuidString));
				sqlParams.Add(new MySqlParameter("@in_app_version", m_inappVersion));
				sqlParams.Add(new MySqlParameter("@last_logged_in_world_id", Program.m_worldID));

				Program.processor.m_universalHubDB.runCommandWithParams("insert into account_details (user_name,hashed_pwd,firstname,lastname,logged_in_world,last_login,device_model,device_gen,device_ios_version,device_str,num_sessions,account_created,uuid,in_app_version, last_logged_in_world_id) " + 
					"values(@user_name, @hashed_pwd, @firstname, @lastname, @logged_in_world, @last_login, @device_model, @device_gen, @device_ios_version, @device_str, 1, @account_created, @uuid, @in_app_version, @last_logged_in_world_id)", sqlParams.ToArray(), true);

				int account_id = -1;
                //SqlQuery query = new SqlQuery(Program.processor.m_universalHubDB, "select account_id from account_details where user_name='" + m_userName + "'", true);

				sqlParams.Clear();
				sqlParams.Add(new MySqlParameter("@user_name", m_userName));

				SqlQuery query = new SqlQuery(Program.processor.m_universalHubDB, "select account_id from account_details where user_name=@user_name", sqlParams.ToArray(), true);

				if (query.HasRows)
                {
                    query.Read();
                    account_id = query.GetInt32("account_id");
                }

                query.Close();
                if (account_id > -1)
                {
                    curplayer = new Player(Program.processor.m_universalHubDB, account_id);

                    curplayer.connection = m_sender;
                    curplayer.m_sessionID = (uint)Program.m_rand.Next();
                    curplayer.m_account_id = account_id;
                    curplayer.m_hashedPass = m_password;
                    curplayer.m_UserName = m_userName;
                    if (Program.m_testAccountToken != "" && m_userName.StartsWith(Program.m_testAccountToken))
                    {
                        curplayer.m_testAccount = 1;
                    }

                    curplayer.m_loggedInTime = DateTime.Now;
                    outmsg.Write((byte)1);
                    outmsg.Write(curplayer.m_sessionID);


                    Program.DisplayDelayed("Account Created " + account_id + " " + m_userName);

                }
                else
                {
                    outmsg.Write((byte)0);
					string locText = Localiser.GetStringByUsername(textDB, m_userName, (int)CreateAccountTaskTextDB.TextID.USERNAME_INVALID);
					outmsg.Write(locText);
					Program.DisplayDelayed("failed to create account for " + m_userName);
                }
            }
            uuidQuery.Close();
            if (Program.m_LogAnalytics)
            {
                AnalyticsMain logAnalytics = new AnalyticsMain(false);
                logAnalytics.newAccount(curplayer);
                logAnalytics.clientDevice(curplayer, deviceGen, deviceIOS, deviceStr);
            }

            DelayedMessageDescriptor msgDesc = new DelayedMessageDescriptor(outmsg, m_sender, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Login, NetworkCommandType.CreateAccountReply, curplayer);
            lock (CommandProcessor.m_delayedMessages)
            {
                CommandProcessor.m_delayedMessages.Enqueue(msgDesc);
            }
        }
    }

    class CreateCharacterTask : BaseTask
    {
        public Player m_player;
        public int m_account_id;
        public string m_name;
        public int m_race_id;
        public int m_face_id;
        public int m_skin;
        public int m_skincol;
        public int m_hair_id;
        public int m_hair_col;
        public int m_face_acc;
        public int m_face_acc_col;
        public float m_scale;
        public int m_class_id;
        public GENDER m_gender;


        public CreateCharacterTask(Player player, int account_id, string name, int race_id, int face_id, int skin_id, int skincol, int hair_id, int hair_col, int face_acc, int face_acc_col, float scale, int class_id, GENDER gender)
        {
            m_TaskType = TaskType.CreateCharacter;
            m_player = player;
            m_account_id = account_id;
            m_name = name;
            m_race_id = race_id;
            m_face_id = face_id;
            m_skin = skin_id;
            m_skincol = skincol;
            m_hair_id = hair_id;
            m_hair_col = hair_col;
            m_face_acc = face_acc;
            m_face_acc_col = face_acc_col;
            m_scale = scale;
            m_class_id = class_id;
            m_gender = gender;
            
            if (player != null)
                Tag = player.m_UserName;
        }

        internal override void TakeAction(CommandProcessor processor)
        {
            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.CreateCharacterReply);
            outmsg.Write((byte)1);
            Character character = new Character(processor.m_worldDB, m_player, m_account_id, m_name, m_race_id, m_face_id, m_skin, m_skincol, m_hair_id, m_hair_col, m_face_acc, m_face_acc_col, m_scale, m_class_id, m_gender);
            processor.m_universalHubDB.runCommand("update account_details set last_selected_character=" + character.m_character_id + " where account_id=" + m_player.m_account_id, true);
            m_player.m_lastSelectedCharacter = (int)character.m_character_id;
            processor.updateWorldCharacterTotal(m_player.m_account_id);
            
            if (Program.m_LogAnalytics)
            {
                AnalyticsMain logAnalytics = new AnalyticsMain(false);
                logAnalytics.newCharacter(character, m_player);
            }
            outmsg.WriteVariableUInt32(character.m_character_id) ;
            outmsg.Write(character.m_name);
            DelayedMessageDescriptor msgDesc = new DelayedMessageDescriptor(outmsg, m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Login, NetworkCommandType.CreateCharacterReply, null);
            lock (CommandProcessor.m_delayedMessages)
            {
                CommandProcessor.m_delayedMessages.Enqueue(msgDesc);
            }
        }
    }

    class DeleteCharacterTask : BaseTask
    {
		// #localisation
		public class DeleteCharacterTaskTextDB : TextEnumDB
		{
			public DeleteCharacterTaskTextDB() : base(nameof(DeleteCharacterTask), typeof(TextID)) { }

			public enum TextID
			{
				INVALID_CHARACTER_DELETION	// "Invalid character deletion"
			}
		}
		public static DeleteCharacterTaskTextDB textDB = new DeleteCharacterTaskTextDB();

		public Player m_player;
        public uint m_character_id;
        public string m_characterName;
        public DeleteCharacterTask(Player player, uint character_id, string characterName)
        {
            m_TaskType = TaskType.DeleteCharacter;
            m_player = player;
            m_character_id = character_id;
            m_characterName = characterName;

			DataValidator.JustCheckCharacterName(m_characterName);

            if (player != null)
                Tag = player.m_UserName;
        }
        internal override void TakeAction(CommandProcessor processor)
        {
            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            string datestr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
			//bool processed = processor.m_worldDB.runCommand("update character_details set deleted=true, deleted_date='" + datestr + "' where character_id=" + m_character_id + " and account_id=" + m_player.m_account_id + " and name='" + m_characterName + "'", true);

			List<MySqlParameter> sqlParams = new List<MySqlParameter>();
			sqlParams.Add(new MySqlParameter("@deleted_date", datestr));
			sqlParams.Add(new MySqlParameter("@character_id", m_character_id));
			sqlParams.Add(new MySqlParameter("@account_id", m_player.m_account_id));
			sqlParams.Add(new MySqlParameter("@name", m_characterName));

			bool processed = processor.m_worldDB.runCommandWithParams("update character_details set deleted=true, deleted_date=@deleted_date where character_id=@character_id and account_id=@account_id and name=@name", sqlParams.ToArray(), true);

			outmsg.WriteVariableUInt32((uint)NetworkCommandType.DeleteCharacterReply);
            uint numactiveslots = processor.getNumberActiveSlots(m_player.m_account_id);
            if (!processed)
            {
                outmsg.Write((byte)0);
				string locText = Localiser.GetString(textDB, m_player, (int)DeleteCharacterTaskTextDB.TextID.INVALID_CHARACTER_DELETION);
				outmsg.Write(locText);
            }
            else
            {
                outmsg.Write((byte)1);
                processor.updateWorldCharacterTotal(m_player.m_account_id);

            }
            DelayedMessageDescriptor msgDesc = new DelayedMessageDescriptor(outmsg, m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.DeleteCharacterReply, null);
            lock (CommandProcessor.m_delayedMessages)
            {
                CommandProcessor.m_delayedMessages.Enqueue(msgDesc);
            }
        }
    }

    class LoginTask : BaseTask
    {
		// #localisation
		public class LoginTaskTextDB : TextEnumDB
		{
			public LoginTaskTextDB() : base(nameof(LoginTask), typeof(TextID)) { }

			public enum TextID
			{
				EXCEEDED_THE_LIMIT_ON_FAILED_LOGIN,	// "You have exceeded the limit on failed logins, please try again in a few minutes. If you have forgotten your password you can reset it from the support menu."
				PROBLEM_RECOVERING_LONG_TERM,		// "There was a problem recovering your account from long term storage. Please contact support."
				USER_NAME_OR_PASSWORD_INCORRECT,	// "Username or Password incorrect"
				ACCOUNT_ALREADY_LOGGED_IN,			// "This account is already logged in"
				ACCOUNT_DISABLED,					// "Account disabled.\n\n"
				DEVICE_DISABLED,					// "Device disabled.\n\n"
				BANNED_REASON,						// "Reason: {bannedReason}\n""
				BANNED_UNTIL_FUTHER_NOTICE,			// "Banned until further notice."
				BANNED_UNTIL_DATE,					// "Banned until:  {formattedDate}\n \n"
				HAVE_ANY_FUTHER_QUESTIONS			// "If you have any further questions please email us at appeals@onethumbmobile.com"
			}
		}
		public static LoginTaskTextDB textDB = new LoginTaskTextDB();

		public string m_userName;
        public string m_password;
        public string m_analyticsStr;
        public string m_deviceID = String.Empty;
        public string m_deviceToken = String.Empty;
        public Player m_player;
        public NetConnection m_sender;
        public int m_inappVersion=0;
        internal Player.Registration_Type m_registrationType = 0;
		public string m_languageString;

		public LoginTask(Player player, NetConnection sender, string userName, string password, byte allowPrivate, string analyticsStr, string deviceID, string deviceToken, string languageString, Player.Registration_Type registrationType)
        {
            m_TaskType = TaskType.Login;
            m_player = player;

            m_userName = userName;
            m_password = password;
            m_analyticsStr = analyticsStr;
            m_sender = sender;
            m_registrationType = registrationType;
            if (deviceID.IndexOf(',') > -1)
            {
                string[] deviceidSplit = deviceID.Split(new char[] { ',' });
                deviceID = deviceidSplit[0];
                m_inappVersion = Int32.Parse(deviceidSplit[1]);
            }
            
            m_deviceID = deviceID;
            m_deviceToken = deviceToken;
			m_languageString = languageString;

			DataValidator.JustCheckUserName(m_userName);
		}

        internal override void TakeAction(CommandProcessor processor)
        {
			double startTime = NetTime.Now;
			
            if (!LoginInBackground(processor))
			{
				Program.DisplayDelayed("[logintask] LoginInBackground failed, enqueuing another logintask: " + m_userName);
                lock (processor.m_backgroundTasks)
                {
                    processor.m_backgroundTasks.Enqueue(this);
                }
            }
			Program.DisplayDelayed("[logintask] LoginTask end " + (NetTime.Now - startTime).ToString("N3") + " " + m_userName);
        }
        
        bool LoginInBackground(CommandProcessor processor)
        {
            //is the player already logged in on this server
            Player foundPlayer = null;
            try
            {
                if (processor.m_players.Count > 0)
                {
                    for (int i = processor.m_players.Count - 1; i > -1; i--)
                    {
                        //remove them 1st
                        Player curPlayer = processor.m_players[i];
                        if (curPlayer.m_UserName != null && curPlayer.m_UserName.Equals(m_userName) && curPlayer.m_registrationType == m_registrationType)
                        {
                            foundPlayer = curPlayer;
                            if (foundPlayer.m_markedForDeletion)
                            {
								Program.DisplayDelayed("[logintask] LoginTask foundPlayer.m_markedForDeletion, will be readding a login task " + m_userName);
                                return false;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Program.DisplayDelayed("[logintask] dupcheck exception " + e.ToString());
                return false;
            }

            LogInFailure loginFailure = null;
            //get how often the usename has tried to log in 
            lock (processor.m_logInFailures)
            {
                loginFailure = LogInFailure.GetDataForUsername(processor.m_logInFailures, m_userName);
            }
            bool accountLocked = false;
            //if there are too many accounts the do not try again untill the old attempts expire
            if (loginFailure != null)
            {
                DateTime failureTimeOut = DateTime.Now - TimeSpan.FromMinutes(LogInFailure.LockOutTimeMinuits);
                bool clearFailures = false;
                lock (loginFailure)
                {
                   
                    loginFailure.ClearDownOldAttempts(failureTimeOut);
                    accountLocked = loginFailure.LockedOut;
                    if (accountLocked == false)
                    {
                        if (loginFailure.Empty==true)
                        {
                            lock (processor.m_logInFailures)
                            {
                                processor.m_logInFailures.Remove(loginFailure);
                                clearFailures = true;
                            }
                        }
                    }
                }
                if (clearFailures == true)
                {
                    loginFailure = null;
                }
            }

            
            double startTimeDetails = NetTime.Now;
            
            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.LoginReply);
            bool accountFound = false;

			//SqlQuery query = new SqlQuery(processor.m_universalHubDB, "select * from account_details where user_name='" + m_userName + "' and registration_type = "+ (int)m_registrationType, true);

			List<MySqlParameter> sqlParams = new List<MySqlParameter>();
			sqlParams.Add(new MySqlParameter("@user_name", m_userName));
			sqlParams.Add(new MySqlParameter("@registration_type", (int)m_registrationType));

			SqlQuery query = new SqlQuery(processor.m_universalHubDB, "select * from account_details where user_name=@user_name and registration_type = @registration_type", sqlParams.ToArray(), true);

			if (accountLocked)
            {
                query.Close();
                outmsg.Write((byte)0);
				string locText = Localiser.GetStringByUsername(textDB, m_userName, (int)LoginTaskTextDB.TextID.EXCEEDED_THE_LIMIT_ON_FAILED_LOGIN);
				outmsg.Write(locText);
				outmsg.Write((byte)0); //stay on main menu
                Program.DisplayDelayed("incorrect login: username not recognised: " + m_userName);
            }
            //check if the player has been archived
            else if (!query.HasRows) //no such player
            {
				Program.DisplayDelayed(string.Format("[logintask] LoginTask no such active player: {0}, or registration type mismatch: expected {1}", m_userName, (int)m_registrationType));

                query.Close();
                
                if (loginFailure != null)
                {
                    lock (loginFailure)
                    {
                        loginFailure.AddLogInAttemptTime(DateTime.Now);
                    }
                }
                else
                {
                    LogInFailure newLoginFailure = new LogInFailure(m_userName);
                    newLoginFailure.AddLogInAttemptTime(DateTime.Now);
                    lock (processor.m_logInFailures)
                    {
                        processor.m_logInFailures.Add(newLoginFailure);
                    }
                }
                outmsg.Write((byte)0);

				string locText = Localiser.GetStringByUsername(textDB, m_userName, (int)LoginTaskTextDB.TextID.USER_NAME_OR_PASSWORD_INCORRECT);
				outmsg.Write(locText);
				outmsg.Write((byte)0); //stay on main menu
				Program.DisplayDelayed("[logintask] incorrect login: username not recognised: " + m_userName);                
            }
            else
            {
                accountFound = true;
            }
            
            double startTimeReadDetails = NetTime.Now;

            // if the account was found then read the account data
            if (accountFound)
            {
                query.Read();
                int account_id = query.GetInt32("account_id");
                string hashedPassword = query.GetString("hashed_pwd");
                string userName = query.GetString("user_name");
                int loggedInWorld = query.GetInt32("logged_in_world");
                int lastSelectedCharacter = query.GetInt32("last_selected_character");
                bool likedOnFacebook = query.GetBoolean("liked_on_facebook");
                bool likedOnTwitter = query.GetBoolean("followed_on_twitter");
                int platinum = query.GetInt32("platinum");
                int maxCharSlots = query.GetInt32("max_character_slots");
                int platPurchased = query.GetInt32("plat_purchased");
                double poundsSpent = query.GetDouble("pounds_spent");
                string savedNotificationDeviceString = query.GetString("notification_device_id");
                string savedNotificationTokenString = query.GetString("notification_token");
                int savedNotificationTypes = query.GetInt32("notification_types");
                bool deviceNotifications = query.GetBoolean("device_notifications");
                bool emailNotifications = query.GetBoolean("email_notifications");
                int rateType = query.GetInt32("rate_type");
                string uuid = query.GetString("uuid");
                string device_IOS_string = query.GetString("device_str");
				int langID = Localiser.GetLanguageIndexOfUsername(userName);

                // Targeted Special Offer & Offer Wall Additions //
                DateTime accountAge = DateTime.Now;
                if (query.isNull("account_created") == false)
                    accountAge = query.GetDateTime("account_created");
                DateTime lastLogin = DateTime.Now;
                if (query.isNull("last_login") == false)
                    lastLogin = query.GetDateTime("last_login");
                int playTime = 0;
                if (query.isNull("total_play_time") == false)
                    playTime = query.GetInt32("total_play_time");
                int platRewarded = 0;
                if (query.isNull("plat_rewarded") == false)
                    platRewarded = query.GetInt32("plat_rewarded");

                MODERATOR_LEVEL moderatorLevel = (MODERATOR_LEVEL)query.GetInt32("moderator_level");
                DateTime silencedUntil = DateTime.MinValue;
                if (!query.isNull("silenced_until"))
                {
                    silencedUntil = query.GetDateTime("silenced_until");
                }
                string email = query.GetString("email");
                // bool disabled = query.GetBoolean("disabled");
                query.Close();

                //create sessiondID
                uint sessionID = (uint)Program.m_rand.Next();
                
                double updateNotificationTokenStartTime = NetTime.Now;

                // Update device token as necessary.
                if (!String.IsNullOrEmpty(m_deviceToken))
                {
                    if (savedNotificationTokenString != m_deviceToken)
                    {
                        savedNotificationTokenString = m_deviceToken;
                        processor.m_universalHubDB.runCommand("update account_details set notification_token='" + savedNotificationTokenString + "' where account_id=" + account_id);

                        if (Program.m_LogAnalytics) // log the change in push notification id
                        {
                            //Analytics Insertion
                            AnalyticsMain logAnalytics = new AnalyticsMain(false);
                            logAnalytics.NotificationServices(account_id, sessionID, savedNotificationTokenString, device_IOS_string);
                        }
                    }
                }
                
                // Check for banned accounts.
                bool accountDisabled = false;
                bool deviceDisabled  = false;
                int banReasonID = -1;
                DateTime BannedUntil = new DateTime();

                double checkForBansStartTime = NetTime.Now;
                
                
                SqlQuery accountBanQuery = new SqlQuery(processor.m_universalHubDB, "SELECT * FROM banned_accounts WHERE account_id=" + account_id + " AND active_ban=true;");

                if (accountBanQuery.Read())
                {
                    banReasonID = accountBanQuery.GetInt32("ban_reason_id");
                    BannedUntil = accountBanQuery.GetDateTime("ban_end");
                    accountDisabled = true;
                    Program.DisplayDelayed("[logintask] account banned " + m_userName);
                }
                
                accountBanQuery.Close();

                // Account status has now been determined - either disabled or not.
                // If not disabled, now check whether device is disabled or not.
                if (accountDisabled == false)
                {
                    // Check for banned devices.                   
                    SqlQuery deviceBanQuery = new SqlQuery(processor.m_universalHubDB, "SELECT * FROM banned_devices WHERE uuid='" + uuid + "' AND active_ban=true;");

                    if (deviceBanQuery.Read())
                    {
                        banReasonID = deviceBanQuery.GetInt32("ban_reason_id");
                        BannedUntil = deviceBanQuery.GetDateTime("ban_end");
                        deviceDisabled = true;
                        Program.DisplayDelayed("[logintask] device banned " + userName + " : " + uuid);
                    }

                    deviceBanQuery.Close();
                }

                bool passwordCorrect = false;
                
                string baseusername = userName.ToLower();
                if (baseusername.Contains('^'))
                {
                    string[] usernameSplit = baseusername.Split(new char[] { '^' }, StringSplitOptions.RemoveEmptyEntries);
                    if (usernameSplit.Length > 1)
                    {
                        baseusername = usernameSplit[1];
                    }
                }

                passwordCorrect = Utilities.hashString(baseusername + hashedPassword).Equals(m_password);


                // used to replace the commented out section above. Nasty I know but I don't want the flow of the program to change.
                if (accountDisabled || deviceDisabled)
                {
                    //report to the client that the login failed
                    outmsg.Write((byte)0);

                    string Message = String.Empty;

                    if (accountDisabled)
                    {
						Message += Localiser.GetStringByLanguageIndex(textDB, langID, (int)LoginTaskTextDB.TextID.ACCOUNT_DISABLED);
                    }
                    else
                    {
						Message += Localiser.GetStringByLanguageIndex(textDB, langID, (int)LoginTaskTextDB.TextID.DEVICE_DISABLED);
					}

					string locText = Localiser.GetStringByLanguageIndex(textDB, langID, (int)LoginTaskTextDB.TextID.BANNED_REASON);
					locText = String.Format(locText, GetBanReason(processor, banReasonID));
					Message += locText;

                    string FormattedDateTime = String.Empty;

                    if (BannedUntil.Year == DateTime.MaxValue.Year)
                    {
						FormattedDateTime = Localiser.GetStringByLanguageIndex(textDB, langID, (int)LoginTaskTextDB.TextID.BANNED_UNTIL_FUTHER_NOTICE);
					}
                    else
                    {
                        FormattedDateTime = Utilities.GetFormatedDateTimeString(BannedUntil);
                    }

					locText = Localiser.GetStringByLanguageIndex(textDB, langID, (int)LoginTaskTextDB.TextID.BANNED_UNTIL_DATE);
					locText = String.Format(locText, FormattedDateTime);
					Message += locText;

					Message += Localiser.GetStringByLanguageIndex(textDB, langID, (int)LoginTaskTextDB.TextID.HAVE_ANY_FUTHER_QUESTIONS);

					outmsg.Write(Message);
                    outmsg.Write((byte)0); //stay on main menu
					Program.DisplayDelayed("[logintask] incorrect login: account disabled: " + account_id + " " + userName);

                }
                //is the password Correct
                else if (!passwordCorrect) // password incorrect
                {
                    if (loginFailure != null)
                    {
                        lock (loginFailure)
                        {
                            loginFailure.AddLogInAttemptTime(DateTime.Now);
                        }
                    }
                    else
                    {
                        LogInFailure newLoginFailure = new LogInFailure(m_userName);
                        newLoginFailure.AddLogInAttemptTime(DateTime.Now);
                        lock (processor.m_logInFailures)
                        {
                            processor.m_logInFailures.Add(newLoginFailure);
                        }
                    }
                    //report to the client that the login failed
                    outmsg.Write((byte)0);
					string locText = Localiser.GetStringByLanguageIndex(textDB, langID, (int)LoginTaskTextDB.TextID.USER_NAME_OR_PASSWORD_INCORRECT);
					outmsg.Write(locText);
					outmsg.Write((byte)0); //stay on main menu
					Program.DisplayDelayed("[logintask] incorrect login: password incorrect: " + account_id + " " + userName);
                }
                //is the player already logged in on another server
                else if (loggedInWorld != 0 && loggedInWorld != Program.m_worldID)
                {
                    //report to the client that the login failed
					Program.DisplayDelayed("[logintask] Already logged in " + userName);
                    outmsg.Write((byte)0);
                    string locText = Localiser.GetStringByLanguageIndex(textDB, langID, (int)LoginTaskTextDB.TextID.ACCOUNT_ALREADY_LOGGED_IN);
					outmsg.Write(locText);
                    outmsg.Write((byte)0); //stay on main menu
                }
                else
                {
                    //is the player already logged in on this server
                    try
                    {
                        if (foundPlayer != null)
                        {
							Program.DisplayDelayed("[logintask] marking dup player for deletion " + foundPlayer.m_UserName);
                            foundPlayer.m_markedForDeletion = true;

                            // force logout now, delaying this would result in the new player being disconnected later
                            Program.processor.disconnect(foundPlayer, true, String.Empty);
                        }
                    }
                    catch (Exception e)
                    {
						Program.DisplayDelayed("[logintask] Exception during login " + userName + " " + e.ToString());

                        return false;
                    }

					// this check is carried out at m_delayedMessages loginmessage send time also, but check here for moot logins
					if (Program.Server.Connections.IndexOf(m_sender) == -1)
					{
						Program.Display("[logintask] connection not found in lidgren list, exiting logintask: " + userName);
						return true;
					}

                    //read the account data
                    m_player = new Player(processor.m_universalHubDB, account_id);
                    m_player.m_lastSelectedCharacter = lastSelectedCharacter;
                    m_player.connection = m_sender;

                    m_player.m_sessionID = (uint)Program.m_rand.Next();
                    //liked_on_facebook` 
                    m_player.m_likedOnFacebook = likedOnFacebook;
                    //followed on twitter
                    m_player.m_followedOnTwitter = likedOnTwitter;
                    m_player.m_UserName = userName;
                    m_player.m_email = email;
                    m_player.m_hashedPass = hashedPassword;
                    m_player.m_languageIndex = Localiser.GetLanguageIndexOfLangString(m_languageString);
                    m_player.m_account_id = account_id;
                    m_player.m_platinum = platinum;
                    m_player.m_totalCharacterSlots = maxCharSlots;
                    m_player.m_moderatorLevel = moderatorLevel;
                    m_player.m_silencedUntil = silencedUntil;
                    m_player.m_plat_purchased = platPurchased;
                    m_player.m_pounds_spent = poundsSpent;
                    m_player.m_savedNotificationToken = savedNotificationTokenString;
                    m_player.m_savedNotificationDevice = savedNotificationDeviceString;
                    m_player.m_savedNotificationType = savedNotificationTypes;
                    m_player.m_deviceNotificationsOn = deviceNotifications;
                    m_player.m_emailNotificationsOn = emailNotifications;
                    //get the rewards 
                    m_player.PoputateRewardsList();
                    m_player.m_registrationType = m_registrationType;
                    m_player.m_rateUsType = (Player.RateUs_Type)rateType;

                    // Targeted Special Offer & Offer Wall Additions //
                    m_player.m_accountAge = accountAge;
                    m_player.m_lastLogin = lastLogin;
                    m_player.m_playTime = playTime;
                    m_player.m_platRewarded = platRewarded;

                    //flag test account
                    if (Program.m_testAccountToken != "" && m_userName.StartsWith(Program.m_testAccountToken))
                    {
                        m_player.m_testAccount = 1;
                    }
                    //these must be done after the platinum has been set
                    if (Program.m_trialpayActive >= 1)
                    {
                        UpdateTrialpayOffers(m_player, processor);
                    }
                    if (Program.m_w3iActive >= 1)
                    {
                        UpdateW3iOffers(m_player, processor);
                    }
                    if(Program.m_fyberActive >= 1)
                    {
                        UpdateFyberOffers(m_player, processor);
                    }

                    //report to the client that the login was a success
                    outmsg.Write((byte)1);
                    outmsg.Write(m_player.m_sessionID);
                    //add them to the current player
                    //   m_players.Add(task.m_player);

                    if (m_player.m_email != "")
                    {
                        outmsg.Write((byte)1);
                    }
                    else
                    {
                        outmsg.Write((byte)0);
                    }

                    string[] analyticsStuff = m_analyticsStr.Split(new char[] { ',' });
                    string deviceModel = "";
                    string deviceGen = "";
                    string deviceIOS = "";
                    string deviceStr = "";
                    try
                    {
                        deviceModel = analyticsStuff[0];
                        deviceGen = analyticsStuff[1];
                        deviceIOS = analyticsStuff[2];
                        deviceStr = analyticsStuff[3];

                        // handle maximum sizes for model and version data as these strings vary quite a lot
                        deviceGen = deviceGen.Substring(0, Math.Min(deviceGen.Length, 45));
                        deviceIOS = deviceIOS.Substring(0, Math.Min(deviceIOS.Length, 45));
                        deviceStr = deviceStr.Substring(0, Math.Min(deviceStr.Length, 45));
                    }
                    catch (Exception e)
                    {
                        Program.DisplayDelayed("[logintask] exception getting analytics " + e.ToString());
                    }
                    m_player.m_notificationDevice = deviceStr;
					//record that they are logged in
					//processor.m_universalHubDB.runCommandSync("update account_details set session_id=" + m_player.m_sessionID + ",logged_in_world=" + Program.m_worldID + ",last_login='" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',last_logged_in_world_id=" + Program.m_worldID + ",device_model='" + deviceModel + "',device_gen=" + deviceGen + ",device_ios_version='" + deviceIOS + "',device_str='" + deviceStr + "',uuid ='" + m_deviceID + "',lang_id ='" + m_player.m_languageIndex + "',num_sessions=num_sessions+1 , in_app_version = " + m_inappVersion + " where account_id=" + m_player.m_account_id);

					int deviceGenInt = 0;
					Int32.TryParse(deviceGen, out deviceGenInt);

					sqlParams.Clear();
					sqlParams.Add(new MySqlParameter("@session_id", m_player.m_sessionID));
					sqlParams.Add(new MySqlParameter("@logged_in_world", Program.m_worldID));
					sqlParams.Add(new MySqlParameter("@last_login", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
					sqlParams.Add(new MySqlParameter("@last_logged_in_world_id", Program.m_worldID));
					sqlParams.Add(new MySqlParameter("@device_model", deviceModel));
					sqlParams.Add(new MySqlParameter("@device_gen", deviceGenInt));
					sqlParams.Add(new MySqlParameter("@device_ios_version", deviceIOS));
					sqlParams.Add(new MySqlParameter("@device_str", deviceStr));
					sqlParams.Add(new MySqlParameter("@uuid", m_deviceID));
					sqlParams.Add(new MySqlParameter("@lang_id", m_player.m_languageIndex));
					sqlParams.Add(new MySqlParameter("@in_app_version", m_inappVersion));
					sqlParams.Add(new MySqlParameter("@account_id", m_player.m_account_id));

					processor.m_universalHubDB.runCommandSyncWithParams("update account_details set session_id=@session_id, logged_in_world=@logged_in_world, last_login=@last_login, last_logged_in_world_id=@last_logged_in_world_id, device_model=@device_model, device_gen=@device_gen, device_ios_version=@device_ios_version, device_str=@device_str, uuid=@uuid, lang_id=@lang_id, num_sessions=num_sessions+1, in_app_version=@in_app_version where account_id=@account_id", sqlParams.ToArray());

					m_player.m_loggedInTime = DateTime.Now;
					Program.DisplayDelayed("[logintask] Player Logged in:" + account_id + " " + userName + " sessionID=" + m_player.m_sessionID + " deviceID=" + m_deviceID);
                }
            }
            else
            {
                Program.DisplayDelayed("[logintask] account wasn't found " + m_userName);
            }
            
            
            DelayedMessageDescriptor msgDesc = new DelayedMessageDescriptor(outmsg, m_sender, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Login, NetworkCommandType.LoginReply, m_player);
            
            lock (CommandProcessor.m_delayedMessages)
            {
                CommandProcessor.m_delayedMessages.Enqueue(msgDesc);
            }
            
            return true;
        }

        private string GetBanReason(CommandProcessor processor, int id)
        {
            SqlQuery query = new SqlQuery(processor.m_universalHubDB, "SELECT * FROM ban_reasons where ban_reason_id=" + id + ";");

            if (query.Read())
            {
                return query.GetString("ban_reason");
            }

            query.Close();

            return "ERROR: Ban Reason not found";
        }
        
        void UpdateTrialpayOffers(Player player, CommandProcessor processor)
        {
            SqlQuery query = new SqlQuery(processor.m_universalHubDB, "select * from trialpay_orders where account_id=" + player.m_account_id + " and rewarded = 0", true);
            int platReward = 0;
            List<string> rewardedOffers = new List<string>();
             
            if (query.HasRows)
            {
                  while (query.Read())
                {
                    string receiptID = query.GetString("order_id");
                    int rewardAmount = query.GetInt32("award_amount");
                    if (rewardAmount > 0)
                    {
                        rewardedOffers.Add(receiptID);
                        platReward += rewardAmount;
                    }

                }

            }
            query.Close();
            if (rewardedOffers.Count > 0&& platReward>0)
            {
                //player.SavePlatinum(platReward, 0);
                string deleteString = "";
                for (int i = 0; i < rewardedOffers.Count; i++)
                {
                    if (deleteString.Length > 0)
                    {
                        deleteString += ",";
                    }
                    deleteString += "\"" + rewardedOffers[i] + "\"";
                }
                List<string> tansactionList = new List<string>();
                tansactionList.Add("update trialpay_orders set world_id=0, rewarded = 1 where account_id = " + player.m_account_id + " and order_id in (" + deleteString + ")");
                player.m_platinum += platReward;
                tansactionList.Add("update account_details set platinum=" + player.m_platinum + " where account_id=" + player.m_account_id);

                processor.m_universalHubDB.runCommandsInTransaction(tansactionList);
                for (int i = 0; i < tansactionList.Count; i++)
                {
                    Program.DisplayDelayed("tansactionList "+i+" : "+tansactionList[i]);
                }
                string modString = "Received " + platReward + " Platinum from Trialpay";
                Program.processor.SendXMLPopupMessage(true, player, (int)XML_Popup.Set_Popup_IDs.SPI_BasicPopup, XML_Popup.Popup_Type.None, "popup_template.txt", new List<string> { modString, " modal = \"true\" active = \"connected\"", "", "" }, false);
                          
            }
        }
        void UpdateW3iOffers(Player player, CommandProcessor processor)
        {
            SqlQuery query = new SqlQuery(processor.m_universalHubDB, "select * from w3i_offer_callbacks where account_id=" + player.m_account_id + " and rewarded = 0", true);
            int platReward = 0;
            List<string> rewardedOffers = new List<string>();

            if (query.HasRows)
            {
                while (query.Read())
                {
                    string receiptID = query.GetString("offer_callback_id");
                    int rewardAmount = query.GetInt32("reward_amount");
                    if (rewardAmount > 0)
                    {
                        rewardedOffers.Add(receiptID);
                        platReward += rewardAmount;
                    }

                }

            }
            query.Close();
            if (rewardedOffers.Count > 0 && platReward > 0)
            {
                //player.SavePlatinum(platReward, 0);
                string deleteString = "";
                for (int i = 0; i < rewardedOffers.Count; i++)
                {
                    if (deleteString.Length > 0)
                    {
                        deleteString += ",";
                    }
                    deleteString +=  rewardedOffers[i];
                }
                List<string> tansactionList = new List<string>();
                tansactionList.Add("update w3i_offer_callbacks set world_id=0, rewarded = 1 where account_id = " + player.m_account_id + " and offer_callback_id in (" + deleteString + ")");
                player.m_platinum += platReward;
                player.m_platRewarded += platReward;
                tansactionList.Add("update account_details set platinum=" + player.m_platinum + ", plat_rewarded=" + player.m_platRewarded + " where account_id=" + player.m_account_id);

                processor.m_universalHubDB.runCommandsInTransaction(tansactionList);
                for (int i = 0; i < tansactionList.Count; i++)
                {
                    Program.DisplayDelayed("tansactionList " + i + " : " + tansactionList[i]);
                }
                string modString = "Received " + platReward + " Platinum from NativeX";
                Program.processor.SendXMLPopupMessage(true, player, (int)XML_Popup.Set_Popup_IDs.SPI_BasicPopup, XML_Popup.Popup_Type.None, "popup_template.txt", new List<string> { modString, " modal = \"true\" active = \"connected\"", "", "" }, false);
                
            }
        }

        /// <summary>
        /// Handles login-time redemption of pending Fyber rewards for passed player
        /// </summary>
        /// <param name="player"></param>
        /// <param name="processor"></param>
        void UpdateFyberOffers(Player player, CommandProcessor processor)
        {
            SqlQuery queryUnredeemedReceipts = new SqlQuery(processor.m_universalHubDB, "select * from " + ReadFyberOrdersTask.k_callbacksTableName + " where account_id=" + player.m_account_id + " and rewarded = 0", true);
            int platReward = 0;
            List<string> rewardedOfferIds = new List<string>();

            if (queryUnredeemedReceipts.HasRows)
            {
                while (queryUnredeemedReceipts.Read())
                {
                    string receiptID = queryUnredeemedReceipts.GetString(ReadFyberOrdersTask.k_transactionColumnName);
                    int rewardAmount = queryUnredeemedReceipts.GetInt32("reward_amount");
                    if (rewardAmount > 0)
                    {
                        rewardedOfferIds.Add(receiptID);
                        platReward += rewardAmount;
                    }
                }
            }

            queryUnredeemedReceipts.Close();

            if (rewardedOfferIds.Count > 0 && platReward > 0)
            {
                string redeemedList = "";
                for (int i = 0; i < rewardedOfferIds.Count; i++)
                {
                    if (redeemedList.Length > 0)
                    {
                        redeemedList += ",";
                    }
                    redeemedList += "'" + rewardedOfferIds[i] + "'";
                }

                // flag all rewarded recepits as actioned and set world id to zero so they're not picked up again
                List<string> tansactionList = new List<string>();
                tansactionList.Add("update " + ReadFyberOrdersTask.k_callbacksTableName + " set world_id=0, rewarded = 1 where account_id = " + player.m_account_id + " and " + ReadFyberOrdersTask.k_transactionColumnName + " in (" + redeemedList + ")");
                
                player.m_platinum += platReward;
                player.m_platRewarded += platReward;
                tansactionList.Add("update account_details set platinum=" + player.m_platinum + ", plat_rewarded=" + player.m_platRewarded + " where account_id=" + player.m_account_id);

                processor.m_universalHubDB.runCommandsInTransaction(tansactionList);

                for (int i = 0; i < tansactionList.Count; i++)
                {
                    Program.DisplayDelayed("Fyber tansactionList " + i + " : " + tansactionList[i]);
                }
                string modString = "Received " + platReward + " Platinum from Fyber";
                Program.processor.SendXMLPopupMessage(true, player, (int)XML_Popup.Set_Popup_IDs.SPI_BasicPopup, XML_Popup.Popup_Type.None, "popup_template.txt", new List<string> { modString, " modal = \"true\" active = \"connected\"", "", "" }, false);
            }
        }
    }

    class RequestCharListTask : BaseTask
    {
		// #localisation
		public class RequestCharListTaskTextDB : TextEnumDB
		{
			public RequestCharListTaskTextDB() : base(nameof(RequestCharListTask), typeof(TextID)) { }

			public enum TextID
			{
				NO_MORE_CHARACTER_SLOT, // "You have no more character slots available, you can get more from the item shop.\n\nTo play the game now, go to a world on which you have created a character.\n\nNote: each character can only be played on the world on which it was created."
			}
		}
		public static RequestCharListTaskTextDB textDB = new RequestCharListTaskTextDB();

		public Player m_player;
        public RequestCharListTask(Player player)
        {
            m_TaskType = TaskType.RequestCharList;
            m_player = player;

            if(player!= null)
                Tag = player.m_UserName;
        }
        internal override void TakeAction(CommandProcessor processor)
        {
            if (!RequestCharacterListInBackground(processor))
            {
                lock (processor.m_backgroundTasks)
                {
                    processor.m_backgroundTasks.Enqueue(this);
                }
            }

        }
        bool RequestCharacterListInBackground(CommandProcessor processor)
        {
            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.RequestCharacterListReply);
            uint numactiveslots = processor.getNumberActiveSlots(m_player.m_account_id);
            if (numactiveslots == 0)
            {
                outmsg.Write((byte)0);
				string locText = Localiser.GetString(textDB, m_player, (int)RequestCharListTaskTextDB.TextID.NO_MORE_CHARACTER_SLOT);
			}
            else
            {
                outmsg.Write((byte)1);
                outmsg.WriteVariableUInt32(numactiveslots);
                uint charcount = processor.getNumberActiveCharacters(m_player.m_account_id);
                outmsg.WriteVariableUInt32(charcount);

                //player.m_lastSelectedCharacter = lastSelected
                outmsg.WriteVariableInt32(m_player.m_lastSelectedCharacter);
                
                SqlQuery query = new SqlQuery(processor.m_worldDB, "select * from character_details where account_id=" + m_player.m_account_id + " and deleted=false");
                if (query.HasRows == true)
                {
                    while (query.Read())
                    {
                        Character character = new Character(processor.m_worldDB, m_player);

                        character.readBasicfromDb(processor.m_worldDB, query);


                        character.writeBasicCharacterInfoToMsg(outmsg);

                    }
                }
                query.Close();
            }
            DelayedMessageDescriptor desc = new DelayedMessageDescriptor(outmsg, m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.RequestCharacterListReply, null);
            lock (CommandProcessor.m_delayedMessages)
            {
                CommandProcessor.m_delayedMessages.Enqueue(desc);
            }
            return true;
        }
    }

    class StartGameTask : BaseTask
    {
		// #localisation
		public class StartGameTaskTextDB : TextEnumDB
		{
			public StartGameTaskTextDB() : base(nameof(StartGameTask), typeof(TextID)) { }

			public enum TextID
			{
				INVALID_CHARACTER_OPERATION,        // "Invalid character List operation"
				USER_AND_PASSWORD_IDENTICAL,        // "It has come to our attention that your username and password are currently identical.\nPlease can you update your password from the character select page, as this is a security risk"
			}
		}
		public static StartGameTaskTextDB textDB = new StartGameTaskTextDB();

		public Player m_player;
        public uint m_character_id;
        public string m_characterName;
        public StartGameTask(Player player, uint character_id, string characterName)
        {
            m_TaskType = TaskType.StartGame;
            m_player = player;
            m_character_id = character_id;
            m_characterName = characterName;
            
            if (player != null)
                Tag = player.m_UserName;
        }

        internal override void TakeAction(CommandProcessor processor)
        {
            //has the character been successfully logged in and created
            bool loggedIn = false;
            NetOutgoingMessage outmsg = Program.Server.CreateMessage();

            outmsg.WriteVariableUInt32((uint)NetworkCommandType.StartGameReply);
            string modString = "";
            Program.Display("starting game for: " + m_player.m_UserName + " [" + m_player.m_account_id + "] with " + m_characterName + " [" + m_character_id + "]");
            
            m_player.m_activeCharacter = Character.loadCharacter(processor.m_worldDB, m_player, m_player.m_account_id, m_character_id, m_characterName);
            if (m_player.m_activeCharacter == null)
            {
                outmsg.Write((byte)0);
				string locText = Localiser.GetString(textDB, m_player, (int)StartGameTaskTextDB.TextID.INVALID_CHARACTER_OPERATION);
				outmsg.Write(locText);
            }
            else
            {
                if (m_player.m_lastSelectedCharacter != m_character_id)
                {
                    processor.m_universalHubDB.runCommandSync("update account_details set last_selected_character=" + m_character_id + " where account_id=" + m_player.m_account_id);
                    m_player.m_lastSelectedCharacter = (int)m_character_id;
                }
                processor.m_worldDB.runCommandSync("update character_details set last_logged_in='" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "' where character_id=" + m_character_id);
                m_player.m_activeCharacter.HealForLoggedOutTime();
                    
                outmsg.Write((byte)1);
                outmsg.WriteVariableInt32(m_player.m_platinum);

                //send highest level of characters on account
                int maxLevel = -1;
	            try
	            {
		            SqlQuery maxLevelQuery = new SqlQuery(processor.m_worldDB,
			            "select max(level) as level from character_details where deleted = 0 and account_id=" +
			            m_player.m_account_id);
		            if (maxLevelQuery.Read())
		            {
			            maxLevel = maxLevelQuery.GetInt32("level");
		            }
		            maxLevelQuery.Close();
	            }
	            catch
	            {
		            maxLevel = -1;
	            }
	            outmsg.WriteVariableInt32(maxLevel);
                //DailyRewardManager.ProcessPlayerLogin(m_player, DailyRewardManager.RewardChainType.Incremental);
                m_player.m_activeCharacter.writeFullCharacterInfoToMsg(outmsg);
                m_player.m_activeCharacter.writePositionInfo(outmsg);
                m_player.m_activeCharacter.m_QuestManager.WriteQuestListToMessage(outmsg);
                m_player.m_activeCharacter.WriteDiscoveredTeleportLocationsToMsg(outmsg);
                if (m_player.m_activeCharacter.CharactersClan != null)
                {
                    m_player.m_activeCharacter.CharactersClan.WriteClanToMessage(outmsg);
                }
                else
                {//no clan
                    outmsg.WriteVariableInt32(-1);
                }

                m_player.m_activeCharacter.WriteDiscoveredZonesToMsg(outmsg);
                
                loggedIn = true;
                SqlQuery mtpQuery = new SqlQuery(processor.m_universalHubDB, "select message_to_player_id,message_text from message_to_player where account_id=" + m_player.m_account_id);
                if (mtpQuery.Read())
                {
                    modString = mtpQuery.GetString("message_text");
                    processor.m_universalHubDB.runCommand("delete from message_to_player where message_to_player_id=" + mtpQuery.GetInt32("message_to_player_id"));

                }

                mtpQuery.Close();
                if (modString.Length == 0)
                {

                    string modQueryStr = "select * from mod_list where expired=0 and expires>now()";
                    SqlQuery query = new SqlQuery(processor.m_universalHubDB, "select mod_read from account_details where account_id=" + m_player.m_account_id);
                    string modFilter = "";
                    if (query.Read())
                    {
                        modFilter = query.GetString("mod_read");
                        if (modFilter.Length > 0)
                        {
                            modQueryStr += " and mod_id not in (" + modFilter + ")";
                        }
                    }
                    query.Close();
                    SqlQuery modQuery = new SqlQuery(processor.m_universalHubDB, modQueryStr);
                    if (modQuery.Read())
                    {
                        int modID = modQuery.GetInt32("mod_id");
                        modString = modQuery.GetString("mod_text");
                        if (modFilter.Length > 0)
                            modFilter += ",";
                        modFilter += modID.ToString();
                        processor.m_universalHubDB.runCommandSync("update account_details set mod_read='" + modFilter + "' where account_id=" + m_player.m_account_id);
                    }
                    modQuery.Close();
                                        
                }
                if (m_player.m_activeCharacter.CharacterMail.UnreadMail == true)
                {
                    Mailbox.SendNewMailMessageToPlayer(true,m_player);
                }
                processor.SendSettings(m_player, true);

                if (m_player.m_activeCharacter.HasUsedSkill == false)
                {
                    processor.StartTutorialMessage(m_player, 4,true);
                }

                // set admin for visiblity
                m_player.m_activeCharacter.AdminCloakedCharacter = processor.IsCloakedAdminAccount((int)m_player.m_account_id);
            }
            if (modString.Length == 0)
            {
                if (m_player.m_registrationType== Player.Registration_Type.Normal&&  m_player.m_hashedPass == Utilities.hashString(m_player.m_UserName))
                {
					modString = Localiser.GetString(textDB, m_player, (int)StartGameTaskTextDB.TextID.USER_AND_PASSWORD_IDENTICAL);
				}
            }

            //tack on fyber video flag            
            outmsg.WriteVariableInt32(Program.m_fyberVideoActive ? 1 : 0);

           // if (loggedIn)
            //{
                /* TEST CODE */
            //    DailyRewardManager.ProcessPlayerLogin(m_player, DailyRewardManager.RewardChainType.Incremental);
            //}

            DelayedMessageDescriptor desc = new DelayedMessageDescriptor(outmsg, m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.StartGameReply, null);
            lock (CommandProcessor.m_delayedMessages)
            {
                CommandProcessor.m_delayedMessages.Enqueue(desc);
            }

            if (modString.Length > 0)
            {
                Program.processor.SendXMLPopupMessage(true, m_player, (int)XML_Popup.Set_Popup_IDs.SPI_BasicPopup, XML_Popup.Popup_Type.None, "popup_template.txt",new List<string>{modString," modal = \"true\" active = \"connected\"","",""}, false);
            }
            if (loggedIn == true)
            {
                processor.sendActiveCharactersBlockedList(m_player, true);
                processor.SendGameDataMessage(m_player);
            }
            if (Program.m_LogAnalytics)
            {
                //Analytics Insertion
                AnalyticsMain logAnalytics = new AnalyticsMain(false);
                logAnalytics.startedPlaying(m_player);
            }

            //send the factions
            Program.processor.FactionNetworkManager.SendFactionsCurrent(m_player,m_player.m_activeCharacter);
            
            m_player = null;
            m_characterName = null;
        }
    }

    class RequestMailSendInfoTask : BaseTask
    {
        public Player m_player = null;
        public string m_recipientName = "";
        public int m_messageLen = 0;
        public int m_goldAdded = 0;
        public int m_numItems = 0;

        internal RequestMailSendInfoTask(Player player, string recipientName, int messageLen, int goldAdded, int numItems)
        {
            m_player = player;
            m_recipientName = recipientName;
            m_messageLen = messageLen;
            m_goldAdded = goldAdded;
            m_numItems = numItems;
            m_TaskType = TaskType.RequestMailSendInfo;

            if (player != null)
                Tag = player.m_UserName;
        }
        internal override void TakeAction(CommandProcessor processor)
        {

            string newRecipientName = m_recipientName.Replace("'", "");

            bool searchPassed = (m_recipientName == newRecipientName);
            //m_recipientName = newRecipientName;

            if (newRecipientName == "")
            {
                searchPassed = false;
            }

            FriendTemplate recipient = null;
            if (searchPassed)
            {
                recipient = Character.LoadCharacterStub(processor.m_worldDB, m_recipientName);
            }

            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.MailMessage);
            outmsg.WriteVariableInt32((int)MailMessageType.MMT_SendInfo);

            //write the basic info about the message in the same order it came
            outmsg.Write(m_recipientName);
            outmsg.WriteVariableInt32(m_messageLen);
            outmsg.WriteVariableInt32(m_goldAdded);
            outmsg.WriteVariableInt32(m_numItems);



            //if the character was not found then send a fail
            if (recipient == null || searchPassed==false)
            {
                outmsg.Write((byte)0);
            }
            //if the recipient was found then get then send down the info 
            else
            {
                outmsg.Write((byte)1);
                recipient.WriteSelfToMessage(outmsg);
                int cost = Mailbox.GetCostForMessage(m_messageLen, m_goldAdded, m_numItems);
                outmsg.WriteVariableInt32(cost);
            }



            DelayedMessageDescriptor desc = new DelayedMessageDescriptor(outmsg, m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.MailMessage, null);
            lock (CommandProcessor.m_delayedMessages)
            {
                CommandProcessor.m_delayedMessages.Enqueue(desc);
            }
           
        }
    }
    class LoadNewMailTask : BaseTask
    {
        public uint m_character_id;
        public Mailbox m_characterMailbox = null;

        List<FriendTemplate> m_blockedCopy = null;
        public LoadNewMailTask(Player player, uint character_id, Mailbox characterMailbox, DateTime endTime, List<FriendTemplate> blockedList)
        {
            m_TaskType = TaskType.LoadNewMail;
            m_character_id = character_id;
            m_characterMailbox = characterMailbox;

            m_blockedCopy = new List<FriendTemplate>(blockedList);
        }
        internal override void TakeAction(CommandProcessor processor)
        {
            //Mailbox.LoadNewMailFromDatabase(processor.m_worldDB, m_character_id, m_startTime, m_endTime, m_characterMailbox, m_player);
            m_characterMailbox.LoadMailFromDatabase(processor.m_worldDB, m_character_id, m_blockedCopy);
        }
    }
    class DeleteOldMailTask : BaseTask
    {
        internal DeleteOldMailTask()
        {
            m_TaskType = TaskType.DeleteOldMail;
        }
        internal override void TakeAction(CommandProcessor processor)
        {
            string nowStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            List<PlayerMail> oldMail = new List<PlayerMail>();
            SqlQuery query = new SqlQuery(processor.m_worldDB, "select * from mail where deleted = 0 and expiry_time < '" + nowStr + "'");
            if (query.HasRows)
            {
                while (query.Read())
                {
                    PlayerMail newMail = new PlayerMail(query, processor.m_worldDB);
                    oldMail.Add(newMail);
                }
            }
            query.Close();

            for (int i = 0; i < oldMail.Count; i++)
            {
                PlayerMail currentMail = oldMail[i];
                bool online = true;
                try
                {
                    Player thePlayer = processor.getPlayerFromActiveCharacterId(currentMail.RecipientID);
                    if (thePlayer == null)
                    {
                        online = false;
                    }
                }
                catch
                {

                }

                if (online == false)
                {
                    //return the mail if it was not sent by the current owner
                    //and it has some attachments
                    if (currentMail.SenderID != currentMail.RecipientID &&
                        (currentMail.AttachedGold > 0 ||
                        (currentMail.AttachedItems != null && currentMail.AttachedItems.Count > 0)))
                    {
                        //return the mail
                        currentMail.ReturnSelf((uint)currentMail.RecipientID);
                    }
                    else
                    {
                        //delete the mail
                        currentMail.DeleteSelf();
                    }
                }
            }
         
        }
    };
    class DeleteCompleteRepeatableQuestsTask : BaseTask
    {
        internal QuestTemplate.Repeatability m_repeatability = QuestTemplate.Repeatability.instant_repeat;
        internal DeleteCompleteRepeatableQuestsTask(QuestTemplate.Repeatability repeatability)
        {
            m_TaskType = TaskType.DeleteCompleteRepeatableQuests;
            m_repeatability = repeatability;
        }
        internal override void TakeAction(CommandProcessor processor)
        {
            processor.m_QuestTemplateManager.DeleteCompleteRepeatableQuests(m_repeatability, processor.m_worldDB);

            if (m_repeatability == QuestTemplate.Repeatability.day_repeat)
            {
                //processor.m_worldDB.runCommand("delete from quest_stage where (character_id,quest_id) in (select character_id,quest_id from quest where completed=1)");
                string timeStr = (DateTime.Now - TimeSpan.FromHours(3.0)).ToString("yyyy-MM-dd HH:mm:ss");
               processor.m_universalHubDB.runCommand("delete from pending_email_changes where request_date <\"" + timeStr + "\"");
            }
        }
    };

    class LinkAccountToFacebookTask : BaseTask
    {
        #region Localization

        public class LinkAccountToFacebookTaskTextDB : TextEnumDB
        {
            public LinkAccountToFacebookTaskTextDB() : base(nameof(LinkAccountToFacebookTask), typeof(TextID)) { }

            public enum TextID
            {
                LINKING_SUCCESSFUL, // "Your Celtic Heroes and Facebook accounts have been successfully linked!"
            }
        }

        public static LinkAccountToFacebookTaskTextDB textDB = new LinkAccountToFacebookTaskTextDB();

        #endregion

        public string m_newUserName       = String.Empty;
        public string m_newHashedPassword = String.Empty;
        public Player m_player            = null;

        internal LinkAccountToFacebookTask(string newUserName, string newHashedPassword, Player player)
        {
            m_newUserName       = newUserName;
            m_newHashedPassword = newHashedPassword;
            m_player            = player;
            
            m_TaskType = TaskType.LinkAccountToFacebook;
        }

        internal override void TakeAction(CommandProcessor processor)
        {
            // Save the current user name and hashed password as were about to replace them
            string currentUserName       = m_player.m_UserName;
            string currentHashedPassword = m_player.m_hashedPass;

			// Replace user_name and hashed_pwd with the new facebook login details
			/*string sqlString = String.Format("update account_details set user_name = '{0}', hashed_pwd = '{1}', registration_type = {2} where account_id = {3}", 
                                             m_newUserName,
                                             m_newHashedPassword,
                                             (int)Player.Registration_Type.Facebook,
                                             m_player.m_account_id);
            processor.m_universalHubDB.runCommand(sqlString);*/

			List<MySqlParameter> sqlParams = new List<MySqlParameter>();
			sqlParams.Add(new MySqlParameter("@user_name", m_newUserName));
			sqlParams.Add(new MySqlParameter("@hashed_pwd", m_newHashedPassword));
			sqlParams.Add(new MySqlParameter("@registration_type", (int)Player.Registration_Type.Facebook));
			sqlParams.Add(new MySqlParameter("@account_id", m_player.m_account_id));

			string sqlString = "update account_details set user_name = @user_name, hashed_pwd = @hashed_pwd, registration_type = @registration_type where account_id = @account_id";
			processor.m_universalHubDB.runCommandWithParams(sqlString, sqlParams.ToArray());



			// Update player references to user name and hashed password, also change account registration type
			m_player.m_UserName         = m_newUserName;
            m_player.m_hashedPass       = m_newHashedPassword;
            m_player.m_registrationType = Player.Registration_Type.Facebook;

			// Save the old login details for support / possible un-linking
			/*sqlString = String.Format("insert into linked_account_details (account_id, previous_user_name, previous_hashed_password) values ({0}, '{1}', '{2}')",
                                      m_player.m_account_id,
                                      currentUserName,
                                      currentHashedPassword);
            processor.m_universalHubDB.runCommand(sqlString);*/

			sqlParams.Clear();
			sqlParams.Add(new MySqlParameter("@account_id", m_player.m_account_id));
			sqlParams.Add(new MySqlParameter("@previous_user_name", currentUserName));
			sqlParams.Add(new MySqlParameter("@previous_hashed_password", currentHashedPassword));

			sqlString = "insert into linked_account_details (account_id, previous_user_name, previous_hashed_password) values (@account_id, @previous_user_name, @previous_hashed_password)";

			processor.m_universalHubDB.runCommandWithParams(sqlString, sqlParams.ToArray());

			// Send confirmation of the account change
			string replyString = Localiser.GetString(textDB, m_player, (int)LinkAccountToFacebookTaskTextDB.TextID.LINKING_SUCCESSFUL);
            processor.SendAccountOptionChangeConnectedReplyDelayed(true, replyString, m_player, AccountOptionsAction.AOA_LinkAccountToFacebook);
        }
    };

    class RegisterEmailTask : BaseTask
    {
		// #localisation
		public class RegisterEmailTaskTextDB : TextEnumDB
		{
			public RegisterEmailTaskTextDB() : base(nameof(RegisterEmailTask), typeof(TextID)) { }

			public enum TextID
			{
				FOLLOW_THE_INSTRUCTIONS,			// "Thanks for registering\nFollow the instructions we have emailed you to complete registration."
				THANKS_YOU_FOR_REGISTERING,			// "Thank You for Registering"
				CONFIRM_EMAIL_ADDRESS,				// "Thank you for registering your email address with Celtic Heroes. <br>This will be used to reset your password should you forget it.<br>The link below will change you registered email address to {baseEmail0}<br><br>Click the following link to confirm the change <a href="{verificationLink1}">{verificationLink2}</a>"
				EMAIL_ADDRESS_WAS_NOT_CHANGED,		// "The email address was not changed, please try again"
			}
		}
		public static RegisterEmailTaskTextDB textDB = new RegisterEmailTaskTextDB();

		public Player m_player = null;
        public string m_newEmail = "";
        internal RegisterEmailTask(string newEmail, Player player)
        {
            m_TaskType = TaskType.RegisterEmail;
            m_newEmail = newEmail;
            m_player = player;
            
            if (player != null)
                Tag = player.m_UserName;
        }
        internal override void TakeAction(CommandProcessor processor)
        {
            string baseEmail = m_newEmail;
            Player player = m_player;
            string hashSecret = "BunnyOfGl00m";
            //it's time to make the hash
            string requestDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string prehashStr = player.m_account_id + baseEmail + requestDate + hashSecret;
            string secureHash = Utilities.hashString(prehashStr);
			
            /*Program.processor.m_universalHubDB.runCommandSync("replace into pending_email_changes (account_id,email,hash,request_date) values (" +
                player.m_account_id + ",\"" + baseEmail + "\",\"" + secureHash +
                "\",\"" + requestDate + "\")");*/

			List<MySqlParameter> sqlParams = new List<MySqlParameter>();
			sqlParams.Add(new MySqlParameter("@account_id", player.m_account_id));
			sqlParams.Add(new MySqlParameter("@email", baseEmail));
			sqlParams.Add(new MySqlParameter("@hash", secureHash));
			sqlParams.Add(new MySqlParameter("@request_date", requestDate));

			Program.processor.m_universalHubDB.runCommandSyncWithParams("replace into pending_email_changes (account_id,email,hash,request_date) values (@account_id, @email, @hash, @request_date)", sqlParams.ToArray());

			string successString = "";//"A verification email has been sent.\nPlease follow instructions in this email to complete registration";
            string emailToSentTo = baseEmail;

            if (player.m_email != "")
            {
               // successString = "A verification email has been sent to your old email address.";
                emailToSentTo = player.m_email;
            }

			successString = Localiser.GetString(textDB, player, (int)RegisterEmailTaskTextDB.TextID.FOLLOW_THE_INSTRUCTIONS);
			string verificationLink = ConfigurationManager.AppSettings["PatchserverAddress"] + "emailVerification.aspx?email=" + baseEmail + "&hash=" + secureHash;
			string locSubjectText = Localiser.GetString(textDB, player, (int)RegisterEmailTaskTextDB.TextID.THANKS_YOU_FOR_REGISTERING);
			string locMessageText = Localiser.GetString(textDB, player, (int)RegisterEmailTaskTextDB.TextID.CONFIRM_EMAIL_ADDRESS);
			locMessageText = String.Format(locMessageText, baseEmail, verificationLink, verificationLink);

            // burn delimiters left in the string (beware, visualstudio will show delimiters behind doublequotes, hit magnifying glass to show pretty-value)
            locMessageText = locMessageText.Replace(@"\", @"");

            //try to send an email to it to comfirm
            bool sendSucessfull = Program.MailHandler.sendMail(emailToSentTo, Program.m_ServerEmail, "", "", locSubjectText, locMessageText, "");

            if (sendSucessfull)
            {
                processor.SendAccountOptionChangeConnectedReplyDelayed(true, successString , player);

				/*
                string logMsg = "Email change request from '" + player.m_email + "' to '" + baseEmail + "'";
                Program.processor.m_universalHubDB.runCommandSync("insert into account_journal (account_id,journal_date,user,details) values (" + player.m_account_id + ",'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',' Server : " + Program.m_ServerName + "',\"" + logMsg + "\")");
				*/

				
				string logMsg = "Email change request from '" + player.m_email + "' to '" + baseEmail + "'";

				sqlParams.Clear();
				sqlParams.Add(new MySqlParameter("@account_id", player.m_account_id));
				sqlParams.Add(new MySqlParameter("@journal_date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
				sqlParams.Add(new MySqlParameter("@user", Program.m_ServerName));
				sqlParams.Add(new MySqlParameter("@details", logMsg));

				Program.processor.m_universalHubDB.runCommandSyncWithParams("insert into account_journal (account_id,journal_date,user,details) values (@account_id, @journal_date, @user, @details)", sqlParams.ToArray());


				/*Program.processor.m_universalHubDB.runCommandSync("update account_details set email = '" + baseEmail + "' where account_id=" + account_id);
                Program.Display("Email changed from '" + player.m_email + "' to '" + baseEmail + "'");
                player.m_email = baseEmail;*/
			}
            else
            {
				string locText = Localiser.GetString(textDB, player, (int)RegisterEmailTaskTextDB.TextID.EMAIL_ADDRESS_WAS_NOT_CHANGED);
				processor.SendAccountOptionChangeConnectedReplyDelayed(false, locText, player);
			}

        }
    }
    class SendEmailTask : BaseTask
    {
        string m_toStr="";
        string m_fromStr = "";
        string m_ccStr = "";
        string m_bccStr = "";
        string m_msgSubject = "";
        string m_msgBody = "";
        string m_msgAttachments = "";

        internal SendEmailTask(string toStr, string fromStr, string ccStr, string bccStr, string msgSubject, string msgBody, string msgAttachments)
        {
            m_TaskType = TaskType.SendEmail;
            m_toStr = toStr;
            m_fromStr = fromStr;
            m_ccStr = ccStr;
            m_bccStr = bccStr;
            m_msgSubject = msgSubject;
            m_msgBody = msgBody;
            m_msgAttachments = msgAttachments;

            Tag = fromStr;
        }
        internal override void TakeAction(CommandProcessor processor)
        {
            Program.MailHandler.sendMail(m_toStr, m_fromStr, m_ccStr, m_bccStr, m_msgSubject, m_msgBody, m_msgAttachments);
        }
    }
    class SendMailNotification : BaseTask
    {
       // string m_message = "";
        int m_characterID = -1;
        int m_senderCharacterID = -1;
        string m_senderName = "";
        string m_subject = "";
        string m_worldName = "";
        
        internal SendMailNotification(string worldName,int senderCharacterID,string senderName, int characterID, string subject, int badge, string sound)
        {
            m_characterID = characterID;
            m_subject = subject;
            m_senderCharacterID = senderCharacterID;
            m_senderName = senderName;
            m_worldName = worldName;
        }
        internal override void TakeAction(CommandProcessor processor)
        {
            //find the characters details and account id
            string sqlString = "SELECT account_id,name from character_details where character_id = " + m_characterID;
            int accountID = -1;
            string name = "";
            SqlQuery query = new SqlQuery(processor.m_worldDB, sqlString, true);
            if (query.HasRows)
            {
                query.Read();
                accountID = query.GetInt32("account_id");
                name = query.GetString("name");
            }
            query.Close();
            //check the sender is not blocked 
            sqlString = "SELECT * from block_list where character_id = " + m_characterID + " and other_character_id = " + m_senderCharacterID;
            bool blocked = false;
            query = new SqlQuery(processor.m_worldDB, sqlString, true);
            if (query.HasRows)
            {
                blocked = true;
            }
            query.Close();

            string message = name + "(" + m_worldName + ") got mail from " + m_senderName + " : ";
            string shortSubject = m_subject;
            int maxNotSize = 100;
            if ((message + shortSubject).Length > maxNotSize)
            {
                int remainingSize = maxNotSize - 3 - message.Length;
                shortSubject = m_subject.Substring(0, remainingSize);

            }

            //set up the task to sent the notification
            if (blocked == false && accountID >= 0)
            {
                string emailSubject = name + "(" + m_worldName + ") has new mail";
                string emailMessage = "<h2>Celtic Heroes</h2><p>"+message+m_subject+"</p>";
                SendEmailNotificationTask emailTask = new SendEmailNotificationTask(accountID, emailSubject,emailMessage);
                lock (processor.m_backgroundTasks)
                {
                    processor.m_backgroundTasks.Enqueue(emailTask);
                }
            }

        }
    }

    class ClearExpiredLoginFailures : BaseTask
    {

        internal ClearExpiredLoginFailures()
        {
            m_TaskType = TaskType.ClearExpiredLogins;
        }

        internal override void TakeAction(CommandProcessor processor)
        {
            //int numFailures = 0;
            LogInFailure[] falureArray=null;
            lock (processor.m_logInFailures)
            {
                falureArray = processor.m_logInFailures.ToArray();
               // numFailures = processor.m_logInFailures.Count;
            }
            if (falureArray == null)
            {
                return;
            }
            DateTime failureTimeout = DateTime.Now - TimeSpan.FromMinutes(LogInFailure.LockOutTimeMinuits);
            List<LogInFailure> itemsToRemove = new List<LogInFailure>();
            for (int i = falureArray.Length - 1; i >= 0; i--)
            {
                LogInFailure currentFailure = null;
                if (i < falureArray.Length)
                {
                    currentFailure = falureArray[i];
                }
                /*lock (processor.m_logInFailures)
                {
                    if (i < processor.m_logInFailures.Count)
                    {
                        currentFailure = processor.m_logInFailures[i];
                    }
                }*/
                bool removecurrent=false;
                if (currentFailure != null)
                {
                    lock (currentFailure)
                    {
                        if (currentFailure.Empty == true || currentFailure.MostRecentTime < failureTimeout)
                        {
                            removecurrent = true;
                        }
                    }
                    if (removecurrent == true)
                    {
                        itemsToRemove.Add(currentFailure);
                        /*lock (processor.m_logInFailures)
                        {
                            processor.m_logInFailures.Remove(currentFailure); 
                           
                        }*/
                    }
                }

            }
            if (itemsToRemove.Count > 0)
            {
                lock (processor.m_logInFailures)
                {
                    for (int i = itemsToRemove.Count - 1; i >= 0; i--)
                    {
                        processor.m_logInFailures.Remove(itemsToRemove[i]); 
                    }
                }
            }
        }
    }
    class SendEmailNotificationTask : BaseTask
    {

        int m_accountID = 0;
        string m_message = "";
        string m_subject = "";

        internal SendEmailNotificationTask(int accountID,string subject, string message)
        {
            m_accountID = accountID;
            m_message = message;
            m_subject = subject;
 
        }
        internal override void TakeAction(CommandProcessor processor)
        {
            string sqlString = "SELECT email, email_notifications from account_details where account_id = " + m_accountID;
            SqlQuery query = new SqlQuery(processor.m_universalHubDB, sqlString, true);
            string savedEmailAddress = "";
            bool emailNotificationsOn = false;
            if (query.HasRows)
            {
                query.Read();
                savedEmailAddress = query.GetString("email");
                emailNotificationsOn = query.GetBoolean("email_notifications");
            }
            query.Close();

            if (savedEmailAddress != "" && emailNotificationsOn==true)
            {
                SendEmailTask task = new SendEmailTask(savedEmailAddress,Program.m_ServerEmail,"","",m_subject, m_message,"");
                lock (processor.m_backgroundTasks)
                {
                    processor.m_backgroundTasks.Enqueue(task);
                }
               /* NotificationPayload payload1 = new NotificationPayload(savedEmailAddress, m_message, m_badge, m_sound);
                //payload1.AddCustom("RegionID", "IDQ10150");

                List<NotificationPayload> p = new List<NotificationPayload> { payload1 };

                PushNotification push = new PushNotification(true, "apn_developer_identity.p12", "otm123");
                List<string> rejected = push.SendToApple(p);
                //List<Feedback> pushFeedback = push.GetFeedBack();
                foreach (string item in rejected)
                {
                    Console.WriteLine(item);
                }*/
			}
        }
    }
    class ReadTrialPayOrdersTask : BaseTask
    {

        int m_worldID=-1;

        internal ReadTrialPayOrdersTask(int worldID)
        {
            m_worldID = worldID;
 

        }
        internal override void TakeAction(CommandProcessor processor)
        {

            SqlQuery query = new SqlQuery(processor.m_universalHubDB, "select * from trialpay_orders where world_id = " + m_worldID +" and rewarded = 0", true);
            List<TrialpayReceipt> receipts = new List<TrialpayReceipt>();
            if (query.HasRows)
            {
                while (query.Read())
                {
                    int userID = query.GetInt32("account_id");
                    int rewardAmount = query.GetInt32("award_amount"); 
                    string order_id = query.GetString("order_id");
                    TrialpayReceipt newReceipt = new TrialpayReceipt(order_id, userID, rewardAmount);
                    receipts.Add(newReceipt);
                }
            }
            lock (processor.m_trialpayController.m_pendingReceipts)
            {
                processor.m_trialpayController.m_pendingReceipts.AddRange(receipts);
                processor.m_trialpayController.m_searchActive = false;
            }
            query.Close();
        }
    }

    // make generic so we can take a generic orderid
    internal class ReadFyberOrdersTask : BaseTask
    {
        public const string k_callbacksTableName = "fyber_offer_callbacks";
        public const string k_transactionColumnName = "transact_id";

        private string m_offerTable;
        private int m_worldID = -1;

        internal ReadFyberOrdersTask(int worldID, string in_offerTableName) // w3i_offer_callbacks
        {
            m_worldID = worldID;
            m_offerTable = in_offerTableName;

            m_TaskType = TaskType.ReadFyberOffers;
        }

        internal override void TakeAction(CommandProcessor processor)
        {
            SqlQuery query = new SqlQuery(processor.m_universalHubDB, "select * from " + m_offerTable + " where world_id = " + m_worldID + " and rewarded = 0", true);
            List<FyberReceipt> receipts = new List<FyberReceipt>();

            if (query.HasRows)
            {
                while (query.Read())
                {
                    string orderID = query.GetString(k_transactionColumnName);
                    int userID = query.GetInt32("account_id");
                    int rewardAmount = query.GetInt32("reward_amount");

                    FyberReceipt newReceipt = new FyberReceipt(orderID, userID, rewardAmount);
                    receipts.Add(newReceipt);
                }
            }

            lock (processor.m_fyberController.m_pendingReceipts)
            {
                processor.m_fyberController.m_pendingReceipts.AddRange(receipts);
                processor.m_fyberController.m_searchActive = false;
            }
            query.Close();
        }
    }

    class ReadW3iOrdersTask : BaseTask
    {

        int m_worldID = -1;

        internal ReadW3iOrdersTask(int worldID)
        {
            m_worldID = worldID;
            m_TaskType = TaskType.ReadNativeXOffers;
        }

        internal override void TakeAction(CommandProcessor processor)
        {

            SqlQuery query = new SqlQuery(processor.m_universalHubDB, "select * from w3i_offer_callbacks where world_id = " + m_worldID + " and rewarded = 0", true);
            List<W3iReceipt> receipts = new List<W3iReceipt>();
            if (query.HasRows)
            {
                while (query.Read())
                {
                    int orderID = query.GetInt32("offer_callback_id");
                    int userID = query.GetInt32("account_id");
                    int rewardAmount = query.GetInt32("reward_amount");

                    W3iReceipt newReceipt = new W3iReceipt(orderID,userID, rewardAmount);
                    receipts.Add(newReceipt);
                }
            }
            lock (processor.m_w3iController.m_pendingReceipts)
            {
                processor.m_w3iController.m_pendingReceipts.AddRange(receipts);
                processor.m_w3iController.m_searchActive = false;
            }
            query.Close();
        }
    }

    class ReadSuperSonicOrdersTask : BaseTask
    {
         int m_worldID = -1;

        internal ReadSuperSonicOrdersTask(int worldID)
        {
            m_worldID = worldID;
            m_TaskType = TaskType.ReadSupersonicOffers;
        }

        internal override void TakeAction(CommandProcessor processor)
        {

            SqlQuery query = new SqlQuery(processor.m_universalHubDB, "select * from supersonic_offer_callbacks where world_id = " + m_worldID + " and rewarded = 0", true);
            List<SuperSonicReceipt> receipts = new List<SuperSonicReceipt>();
            if (query.HasRows)
            {
                while (query.Read())
                {
                    string orderID = query.GetString("event_id");
                    int userID = query.GetInt32("account_id");
                    int rewardAmount = query.GetInt32("reward_amount");

                    SuperSonicReceipt newReceipt = new SuperSonicReceipt(orderID, userID, rewardAmount);
                    receipts.Add(newReceipt);
                }
            }
            lock (processor.m_supersonicController.m_pendingReceipts)
            {
                processor.m_supersonicController.m_pendingReceipts.AddRange(receipts);
                processor.m_supersonicController.m_searchActive = false;
            }
            query.Close();
        }
    }


    internal struct CharacterAndQuestCombo
    {
        internal int CharacterId { private set; get; }
        internal int QuestId { private set; get; }

        internal CharacterAndQuestCombo(int characterId, int questId) : this()
        {
            CharacterId = characterId;
            QuestId = questId;
        }
    }

    class ResetBountyBoardTask : BaseTask
    {
        private readonly List<Player> mListOfOnlinePlayers;

        internal ResetBountyBoardTask(List<Player> listOfOnlinePlayers)
        {
            m_TaskType = TaskType.ResetBounties;
            mListOfOnlinePlayers = listOfOnlinePlayers;
        }

        internal override void TakeAction(CommandProcessor processor)
        {
            try
            {
                var characterAndQuestComboForDeletion = new List<CharacterAndQuestCombo>();

                var query = new SqlQuery(Program.processor.m_worldDB, "select * from bounties where bounties.status = " + (int) Bounty.tStatus.HandedIn);
                if (query.HasRows)
                {
                    while (query.Read())
                    {
                        int characterId = query.GetInt32("character_id");
                        int questId = query.GetInt32("quest_id");
                        
                        characterAndQuestComboForDeletion.Add(new CharacterAndQuestCombo(characterId, questId));
                    }
                }
                
                var deletionCommands = new List<string>();
                foreach (CharacterAndQuestCombo combo in characterAndQuestComboForDeletion)
                {
                    string whereClause = String.Format(" WHERE character_id={0} AND quest_id={1}", combo.CharacterId, combo.QuestId);
                    deletionCommands.Add("DELETE FROM quest" + whereClause);
                    deletionCommands.Add("DELETE FROM quest_stage" + whereClause);
                    deletionCommands.Add("DELETE FROM bounties" + whereClause);
                }

                foreach (string deletionCommand in deletionCommands)
                {
                    Program.processor.m_worldDB.runCommand(deletionCommand, true);
                }

                //clear out dropped bounties
                Program.processor.m_worldDB.runCommand("DELETE FROM bounties WHERE bounties.status =" + (int)Bounty.tStatus.Dropped, true);

                // set the world clear time to this time
                string updateTime = "UPDATE world_params SET param_value = '" + DateTime.Now +
                                    "' WHERE param_name='bounty_last_clear_time'";
                Program.processor.m_worldDB.runCommandSync(updateTime);

                processor.commandProcessorLoading.LoadWorldParams(processor.m_worldDB); // recheck the clear time incase its changed or any of the variables has changed

                // refresh all players that are online so they're notified the bounty board has changed
                if (mListOfOnlinePlayers.Count > 0)
                {
                    for (int i = mListOfOnlinePlayers.Count - 1; i > -1; i--)
                    {
                        if (mListOfOnlinePlayers[i] == null) continue;
                        Player curPlayer = mListOfOnlinePlayers[i];
                        if (curPlayer.m_activeCharacter == null) continue;

                        curPlayer.m_activeCharacter.CharacterBountyManager.RefreshBounties();
                            // shouldn't need to reset anything quest related because we are only deleting completed bounties                    
                    }
                }
            }
            catch (Exception ex)
            {
                string error = String.Format("Reset bounty board: {0} ", ex.ToString());
                Program.LogDatabaseException(error);
            }
        }
    }
}
