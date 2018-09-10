using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Lidgren.Network;

namespace MainServer
{
    enum MODERATOR_LEVEL
    {
        STANDARD_PLAYER=0,
        GM_PLAYER=1,
        ADMINISTRATOR=2
    }
    class LogInFailure
    {
        
        static int MaxFailedAttempts = 5;
        internal static float LockOutTimeMinuits = 4;
        string m_username;
        List<DateTime> m_logInAttempts = new List<DateTime>();
        bool m_lockedOut = false;
        internal bool LockedOut
        {
            get { return m_lockedOut; }
        }

        internal LogInFailure(string username)
        {
            m_username = username;
        }
        internal bool Empty
        {
            get { return (m_logInAttempts.Count == 0); }
        }
        internal DateTime MostRecentTime
        {
            get 
            {
                if (m_logInAttempts.Count > 0)
                {
                    return m_logInAttempts.Last();
                }
                return Program.m_referenceDate;
            }
        }
        internal void AddLogInAttemptTime(DateTime latestAttempt)
        {
            m_logInAttempts.Add(latestAttempt);
            if (m_logInAttempts.Count >= LogInFailure.MaxFailedAttempts)
            {
                m_lockedOut = true;
            }
        }
        internal void ClearDownOldAttempts(DateTime timeOutVal)
        {
            for (int i = (m_logInAttempts.Count - 1); i >= 0; i--)
            {
                DateTime currentAttempt = m_logInAttempts[i];
                if (currentAttempt < timeOutVal)
                {
                    m_logInAttempts.Remove(currentAttempt);
                }
            }
            if (m_logInAttempts.Count == 0)
            {
                m_lockedOut = false;
            }
        }
        static internal LogInFailure GetDataForUsername(List<LogInFailure> failureList, string username)
        {
            LogInFailure dataForUser = null;

            for (int i = 0; i < failureList.Count && dataForUser==null; i++)
            {
                LogInFailure currentData = failureList[i];
                if (currentData.m_username == username)
                {
                    dataForUser = currentData;
                }
            }
            return dataForUser;

        }


    }
    class Player
    {
        internal enum Registration_Type
        {
            Normal = 0,
            Guest = 1,
            Facebook = 2,

        }
        internal enum SettingTypes
        {
            deviceNotifications =1,
            emailNotifications =2,
            registerRequired = 3,

        }

        internal enum RateUs_Type
        {
            NotAskedYet = 0,
            NeverAskAgain = 1,
            AskLater = 2,
            Rating = 3
        
        }

        public UInt32 m_sessionID;
        public long m_account_id;
        public String m_UserName;
        public String m_email = "";
        public string m_hashedPass = "";
		public int m_languageIndex = 0;
		public Character m_activeCharacter;
        public NetConnection connection;
        public int m_lastSelectedCharacter = -1;
        public int m_platinum;
        public int m_totalCharacterSlots = 0;
        public DateTime m_loggedInTime;
        public RankingsManager m_AccountRankings;
        public AchievementsManager m_AccountAchievements;
        public bool m_likedOnFacebook = false;
        public bool m_followedOnTwitter = false;
        public bool m_markedForDeletion = false;
        public byte m_testAccount = 0;

        public int m_highestFacebookFriendsRewarded = 0;
        public int m_highestTwitterFollowersRewarded = 0;
        public int m_currentRewardsForFacebookPosts = 0;
        public int m_currentRewardsForTwitterTweets = 0;
        public DateTime m_silencedUntil = DateTime.MinValue;
        public int m_plat_purchased=0;
        public double m_pounds_spent=0;
        public MODERATOR_LEVEL m_moderatorLevel = MODERATOR_LEVEL.STANDARD_PLAYER;

        public List<XML_Popup> m_openPopups = new List<XML_Popup>();
        /// <summary>
        /// popups added by a thread since the last popup update
        /// </summary>
        public List<XML_Popup> m_newPopups = new List<XML_Popup>();
        public string m_notificationDevice = "";
        public string m_notificationToken = null;
        public int m_notificationType = 0;
        public string m_savedNotificationDevice = "";
        public string m_savedNotificationToken = null;
        public int m_savedNotificationType = 0;
        public bool m_deviceNotificationsOn = false;
        public bool m_emailNotificationsOn = false;

        // Targeted Special Offer Additions //
        public DateTime? m_accountAge   = null;
        public DateTime? m_lastLogin    = null;
        public int?      m_playTime     = null;
        public int       m_platRewarded = 0;

        internal double m_timeOfLastOfferWall = 0;

        internal Registration_Type m_registrationType = 0;
        internal RateUs_Type m_rateUsType = 0;

        List<PlatinumRewards> m_rewardsList=null;

        public DateTime m_lastPerformanceUpdate = DateTime.MinValue;

        public Player(Database db, int account_id)
        {
            m_account_id = account_id;
            if (Program.m_SendAchievements==UPDATE_ACHIEVEMENTS.ALL)
            {
                m_AccountRankings = new RankingsManager(db, RankingsManager.RANKING_MANAGER_TYPE.ACCOUNT_RANKINGS, "account_rankings", "account_id", account_id);
                m_AccountAchievements = new AchievementsManager(db, AchievementsManager.ACHIEVEMENT_MANAGER_TYPE.ACCOUNT_ACHIEVEMENTS, "account_achievements", "account_id", account_id);
            }
        }
        internal void saveAchievementsAndLeaderBoards()
        {
            if (Program.m_SendAchievements==UPDATE_ACHIEVEMENTS.ALL)
            {
                m_AccountAchievements.saveAchievements();
                m_AccountRankings.saveRankings();
            }
        }

        internal void SavePlatinum(int costPlat,double costPounds)
        {
            m_plat_purchased += costPlat;
            m_pounds_spent += costPounds;
            Program.processor.m_universalHubDB.runCommandSync("update account_details set platinum=" + m_platinum + ",plat_purchased=plat_purchased+"+costPlat+",pounds_spent=pounds_spent+"+costPounds+" where account_id=" + m_account_id);
        }
        internal void SaveCharacterSlots()
        {
            Program.processor.m_universalHubDB.runCommandSync("update account_details set max_character_slots=" + m_totalCharacterSlots + " where account_id=" + m_account_id);
        }
        internal void AddToRewardsList(PlatinumRewards newReward)
        {
            m_rewardsList.Add(newReward);
        }

        internal void PoputateRewardsList()
        {
            m_rewardsList = new List<PlatinumRewards>();
            PlatinumRewards.GetRewardsForAccount(m_account_id,ref m_rewardsList);


            //sort the rewards to get data

            for (int i = 0; i < m_rewardsList.Count; i++)
            {
                PlatinumRewards currentReward = m_rewardsList[i];
                switch (currentReward.RewardType)
                {
                    case PlatinumRewards.REWARD_TYPES.FACEBOOK_POSTS:
                        {
                            //update the total for rewards of this type
                            m_currentRewardsForFacebookPosts += currentReward.PlatinumAwarded;
                            //check the number of friends
                            if (currentReward.RewardValue > m_highestFacebookFriendsRewarded)
                            {
                                m_highestFacebookFriendsRewarded = currentReward.RewardValue;
                            }
                            break;
                        }
                    case PlatinumRewards.REWARD_TYPES.TWITTER_TWEET:
                        {
                            //update the total for rewards of this type
                            m_currentRewardsForTwitterTweets += currentReward.PlatinumAwarded;
                            //check the number of friends
                            if (currentReward.RewardValue > m_highestTwitterFollowersRewarded)
                            {
                                m_highestTwitterFollowersRewarded = currentReward.RewardValue;
                            }
                            break;
                        }
                    default:
                        break;
                }

            }
        }
        internal void CheckForNotificationsChange()
        {
            if (m_deviceNotificationsOn == false)
            {
                m_notificationToken = m_savedNotificationToken;
                m_notificationDevice = m_savedNotificationDevice;
                return;
            }
            //if the notification token has changed
            if (m_savedNotificationToken != m_notificationToken)
            {
                if (m_notificationDevice != "")
                {

                    //if it's the same device then save as it is the new token
                    if (m_savedNotificationDevice == m_notificationDevice)
                    {
                        SaveNewNotificationData();
                    }
                    else if (m_notificationToken != "")
                    {
                        bool popupOpen = false;
                        for (int i = 0; i < m_openPopups.Count && popupOpen == false; i++)
                        {
                            XML_Popup currentPopup = m_openPopups[i];
                            if (currentPopup.PopupType == XML_Popup.Popup_Type.NotificationChanging && currentPopup.PopupID == (int)XML_Popup.Set_Popup_IDs.SPI_NewNotifications)
                            {
                                popupOpen = true;
                            }
                        }
                        if (popupOpen == false)
                        {
                            Program.processor.SendXMLPopupMessage(false, this, (int)XML_Popup.Set_Popup_IDs.SPI_NewNotifications, XML_Popup.Popup_Type.NotificationChanging, "new_notify_popup.txt", null, false);
                        }
                    }
                }
            }
                //if the device has changed but not the token
                // it is probably a new install on the same machine
            else if (m_savedNotificationDevice != m_notificationDevice)
            {
                SaveNewNotificationData();
            }
            
        }
        internal void SaveNewNotificationData()
        {
            string savedNotificationTokenString = "";
            string savedNotificationDeviceString = "";
            if (m_notificationToken != null)
            {
                savedNotificationTokenString = m_notificationToken;
                //if there is no token then there is no device
                savedNotificationDeviceString = m_notificationDevice;
            }
            // Legacy code: update not required here.
            //Program.processor.m_universalHubDB.runCommandSync("update account_details set notification_device_id='" + savedNotificationDeviceString + "',notification_token='" + savedNotificationTokenString + "',notification_types=" + m_notificationType + " where account_id=" + m_account_id);
            m_savedNotificationDevice = savedNotificationDeviceString;
            m_savedNotificationToken = m_notificationToken;
        }
        internal void SetDeviceNotificationsOn(bool newVal)
        {
            
            if (m_deviceNotificationsOn != newVal)
            {
                int valInt = 0;
                if (newVal == true)
                {
                    valInt = 1;
                }

                Program.processor.m_universalHubDB.runCommandSync("update account_details set device_notifications=" + valInt + " where account_id=" + m_account_id);
            }
            m_deviceNotificationsOn = newVal;
          
        }
        internal void SetEmailNotificationsOn(bool newVal)
        {
            if (m_emailNotificationsOn != newVal)
            {
                int valInt = 0;
                if (newVal == true)
                {
                    valInt = 1;
                }

                Program.processor.m_universalHubDB.runCommandSync("update account_details set email_notifications=" + valInt + " where account_id=" + m_account_id);

            }
            m_emailNotificationsOn = newVal;
        }
        internal string GetIDString()
        {
            string idString = "(" + m_UserName + "," + m_account_id + ")";

            return idString;
        }

        internal void updateRateUsType(int rate_type)
        {
            m_rateUsType = (Player.RateUs_Type)rate_type;
            Program.processor.m_universalHubDB.runCommandSync("update account_details set rate_type=" + rate_type + " where account_id=" + m_account_id);
        }

        public void CloseMagicBoxPopups()
        {
            lock (m_openPopups)
            { 
                for (int i = 0; i < m_openPopups.Count; i++)
                {
                    var openPopup = m_openPopups[i];

                    switch (openPopup.PopupID)
                    {
                        case (int)XML_Popup.Set_Popup_IDs.SPI_MAG_BOX_OPEN:

                            break;
                        case (int) XML_Popup.Set_Popup_IDs.SPI_MAG_BOX_NEXT:
                            Program.processor.SendCloseXMLPopup(this, (int) XML_Popup.Set_Popup_IDs.SPI_MAG_BOX_NEXT);
                            m_openPopups.Remove(openPopup);
                            i--;
                            break;
                        case (int) XML_Popup.Set_Popup_IDs.SPI_MAG_BOX_CLOSE:
                            if (openPopup.PopupData != null)
                            {
                                if (openPopup.PopupData.m_postString != "")
                                {
                                    Program.processor.sendSystemMessage(openPopup.PopupData.m_postString, this, true,
                                        SYSTEM_MESSAGE_TYPE.ITEM_USE);
                                }
                            }
                            Program.processor.SendCloseXMLPopup(this, (int) XML_Popup.Set_Popup_IDs.SPI_MAG_BOX_CLOSE);
                            m_openPopups.Remove(openPopup);
                            i--;
                            break;
                    }
                }
            }
        }

		
	};
}
