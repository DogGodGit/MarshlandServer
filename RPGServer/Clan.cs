using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using System.Text.RegularExpressions;
using MainServer.Localise;
using Analytics.Social;
using MySql.Data.MySqlClient;

namespace MainServer
{

    class ClanMember : FriendTemplate
    {
        Clan.CLAN_RANKS m_position = Clan.CLAN_RANKS.NONE;
       
        internal ClanMember()
            : base()
        {

        }

        internal ClanMember(Character baseCharacter)
            : base(baseCharacter)
        {

        }
        
        public Clan.CLAN_RANKS Position
        {
            set { m_position = value; }
            get { return m_position; }
        }
        static internal void WriteClanMembersToMessage(List<ClanMember> memberList, NetOutgoingMessage msg)
        {
            int numberOfFriends = memberList.Count;
            //number of friends
            msg.WriteVariableInt32(numberOfFriends);

            for (int currentFriendIndex = 0; currentFriendIndex < memberList.Count; currentFriendIndex++)
            {
                ClanMember currentFriend = memberList[currentFriendIndex];
                currentFriend.WriteSelfToMessageIncludingRank(msg);
            }


        }
        internal void SaveOutRank(int clanID)
        {
            Program.processor.m_worldDB.runCommandSync("update clan_members set clan_rank=" + (int)Position + " where clan_id=" + clanID + " and character_id=" + CharacterID);
        }
        internal void WriteSelfToMessageIncludingRank(NetOutgoingMessage msg)
        {
            
            msg.Write(CharacterName);
            msg.WriteVariableInt32(CharacterID);
            
            //online          
            msg.Write((byte)(Online ? 1 : 0));           
            //location
            msg.WriteVariableInt32(Zone);
            //level
            msg.WriteVariableInt32(Level);
            //class
            msg.WriteVariableInt32(Class);
            //race
            msg.WriteVariableInt32(Race);
            //position
            msg.WriteVariableInt32((int)Position);
        }
        static internal ClanMember AddFriend(int character_id, List<ClanMember> friendList, Clan.CLAN_RANKS rank, bool checkLogInDate)
        {

            //string friendsListString = WriteFriendsToString();
            string sqlstr = "select * from character_details where character_id=" + character_id;
            if (rank < Clan.CLAN_RANKS.LEADER && checkLogInDate == true)
                sqlstr += " and deleted=0 and last_logged_in>'" + DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd HH:mm:ss") + "'";
            //Character character;

            ClanMember newFriend = null;
            SqlQuery query = new SqlQuery(Program.processor.m_worldDB, sqlstr);
            if (!query.HasRows)
            {
                //send error report, no character of this name
                query.Close();
                return null;
            }
            while ((query.Read()))
            {
                //character = null;
                //bool currentlyOnline;
                //name
                string name = query.GetString("name");
                //location
                int zone = query.GetInt32("zone");
                //level
                int level = query.GetInt32("level");
                //class
                int characterClass = query.GetInt32("class_id");
                //race
                int characterRace = query.GetInt32("race_id");
                //account
                uint accountID = (uint)query.GetInt32("account_id");

                newFriend = new ClanMember();
                {
                    newFriend.CharacterID = character_id;

                    newFriend.CharacterName = name;
                    newFriend.Level = level;
                    newFriend.Online = false;
                    newFriend.Zone = zone;
                    newFriend.Class = characterClass;
                    newFriend.Race = characterRace;
                    newFriend.AccountID = accountID;
                    newFriend.Position = rank;
                    friendList.Add(newFriend);
                }

            }
            query.Close();

            return newFriend;
        }


        static internal ClanMember AddFriend(List<ClanMember> friendList, SqlQuery query)
        {



                //name
                string name = query.GetString("name");
                //location
                int zone = query.GetInt32("zone");
                //level
                int level = query.GetInt32("level");
                //class
                int characterClass = query.GetInt32("class_id");
                //race
                int characterRace = query.GetInt32("race_id");
                //account
                uint accountID = (uint)query.GetInt32("account_id");

                ClanMember newFriend = new ClanMember();
                
                    newFriend.CharacterID = query.GetInt32("character_id");

                    newFriend.CharacterName = name;
                    newFriend.Level = level;
                    newFriend.Online = false;
                    newFriend.Zone = zone;
                    newFriend.Class = characterClass;
                    newFriend.Race = characterRace;
                    newFriend.AccountID = accountID;
                    newFriend.Position = (Clan.CLAN_RANKS)query.GetInt32("clan_rank");
                    friendList.Add(newFriend);
                
              return newFriend;
            
        }

        static internal ClanMember ContainsTemplateForID(List<ClanMember> theList, int theTemplateID)
        {
            ClanMember theTemplate = null;
            for (int i = 0; i < theList.Count; i++)
            {
                ClanMember currentTemplate = theList[i];
                if (currentTemplate != null)
                {
                    if (currentTemplate.CharacterID == theTemplateID)
                    {
                        theTemplate = currentTemplate;
                        return theTemplate;
                    }
                }
            }
            return theTemplate;
        }
    }
    
    class Clan
    {
		// #localisation
		public class ClanTextDB : TextEnumDB
		{
			public ClanTextDB() : base(nameof(Clan), typeof(TextID)) { }

			public enum TextID
			{
				OTHER_PROMOTED,				// "{name0} has been promoted to {positionName1} by {promoterName2}"
				PLAYER_PROMOTED,			// "You have been promoted to {potisionName0} by {promoterName1}"
				INCORRECT_RANK_PROMOTE,		// "You are not the correct rank to promote to {positionName0}"
				LEFT_THE_CLAN,				// "{name0} has left the clan."
				OTHER_KICKED_FROM_CLAN,		// "{name0} has been kicked from the clan by {demoterName1}"
				PLAYER_KICKED_FROM_CLAN,	// "You have been kicked from the clan by {demoterName0}"
				PLAYER_LEFT_THE_CLAN,		// "You have left the clan"
				OTHER_DEMOTED,				// "{name0} has been demoted to {positionName1} by {demoterName2}"
				DEMOTED_HIMSELF,			// "{name0} has demoted himself to {positionName1}"
				DEMOTED_HERSELF,			// "{name0} has demoted herself to {positionName1}"
				PLAYER_DEMOTED_BY,			// "You have been demoted to {positionName0} by {demoterName1}"
				PLAYER_DEMOTED_SELF,		// "You have demoted yourself to {positionName0}"
				PLAYER_LEAVE_CLAN,			// "To leave you must appoint a new {positionName0} or disband the clan"
				INCORRECT_RANK_DEMOTE,		// "You are not the correct rank to demote from {positionName0}"
				JOINED_CLAN,				// "{name0} joined the clan"
				LOGGED_IN,					// "clan member {name0} logged in"
				LOGGED_OUT,					// "clan member {name0} logged out"
				RECRUIT,					// "recruit"
				CLANSMAN,					// "clansman"
				GUARDIAN,					// "guardian"
				GENERAL,					// "general"
				CHIEFTAIN,					// "chieftain"
				EDITED_BY,					// "\n\nEdited by {name0}"
			}
		}
		public static ClanTextDB textDB = new ClanTextDB();

        #region enum & fields

        public enum CLAN_RANKS
        {
            NONE = -1,
            RECRUIT = 0,
            MEMBER = 1,
            NOBLE = 2,
            GENERAL = 3,
            LEADER = 4
        };

		//public static string[] PositionNames = { "recruit", "clansman", "guardian", "general", "chieftain" };
		//public static string[] PositionNamesPlural = { "recruits", "clansmen", "guardians", "generals", "chieftains" };

		internal static int[] PositionNameIDs = new int[]
		{
			(int)ClanTextDB.TextID.RECRUIT,
			(int)ClanTextDB.TextID.CLANSMAN,
			(int)ClanTextDB.TextID.GUARDIAN,
			(int)ClanTextDB.TextID.GENERAL,
			(int)ClanTextDB.TextID.CHIEFTAIN
		};

		internal string GetPositionName(int rank, Player player)
		{
			if (rank < 0 || rank > PositionNameIDs.Length)
				return "";

			return Localiser.GetString(textDB, player, PositionNameIDs[rank]);
		}

        internal static double TIME_BETWEEN_CLAN_ACTIONS = 5.0;

		//internal static string LEADER_NAME = "chieftain";
		//internal static string GENERAL_NAME = "general";
		//internal static string NOBLE_NAME = "guardian";
		//internal static string MEMBER_NAME = "clansman";

		//internal static string LEADER_NAME_PLURAL = "chieftains";
		//internal static string GENERAL_NAME_PLURAL = "generals";
		//internal static string NOBLE_NAME_PLURAL = "guardians";
		//internal static string MEMBER_NAME_PLURAL = "clansmen";
        static int HW_NO_CLAN_ID = -1;
        public static int HW_CLAN_START_UP_COST = 200;
        byte[] m_clanListMessage = null;
        int m_clanListMessageLength = 0;
        internal int m_leaderID = 0;

        
        string m_clanName = "";
        string m_clanMessage = "";
        int m_clanID = -1;

        internal string ClanName
        {
            get { return m_clanName; }

        }
        internal int ClanID
        {
            get { return m_clanID; }
        }
       
        internal List<ClanMember> ClanMembers { get; private set; }

        internal ClanMember Leader { get; private set; }

        #endregion

        #region constructors

        internal Clan()
        {
            ClanMembers = new List<ClanMember>();
        }

       
        internal Clan(SqlQuery query)
        {
            m_clanID = query.GetInt32("clan_id");
            m_clanName = query.GetString("clan_name");
            m_leaderID = query.GetInt32("clan_leader");
            m_clanMessage = query.GetString("clan_message");
            ClanMembers = new List<ClanMember>();
        }

        #endregion

        internal void CreateClan(string clanName, Character clanLeader)
        {
            m_clanName = clanName;
			/*Program.processor.m_worldDB.runCommand("insert into clan_details " +
                            "(clan_name,clan_leader,clan_nobles,clan_members) values ('" +
                            m_clanName + "'," + clanLeader.m_character_id + "," + "'','')");*/

			List<MySqlParameter> sqlParams = new List<MySqlParameter>();
			sqlParams.Add(new MySqlParameter("@clan_name", m_clanName));
			sqlParams.Add(new MySqlParameter("@clan_leader", clanLeader.m_character_id));
			sqlParams.Add(new MySqlParameter("@clan_nobles", ""));
			sqlParams.Add(new MySqlParameter("@clan_members", ""));

			Program.processor.m_worldDB.runCommandWithParams("insert into clan_details (clan_name,clan_leader,clan_nobles,clan_members) values (@clan_name, @clan_leader, @clan_nobles, @clan_members)", sqlParams.ToArray());

			//create an empty clan
			Leader = new ClanMember(clanLeader);
            m_leaderID = Leader.CharacterID;
            clanLeader.increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.CLANSMAN, 1);
            clanLeader.increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.CHIEFTAIN, 1);
            ClanMembers.Add(Leader);
            clanLeader.CharactersClan = this;
            Leader.Position = CLAN_RANKS.LEADER;
			//SqlQuery query = new SqlQuery(Program.processor.m_worldDB, "select max(clan_id)as max_clan_id from clan_details where clan_name='" + m_clanName + "'");

			sqlParams.Clear();
			sqlParams.Add(new MySqlParameter("@clan_name", m_clanName));
			SqlQuery query = new SqlQuery(Program.processor.m_worldDB, "select max(clan_id)as max_clan_id from clan_details where clan_name=@clan_name", sqlParams.ToArray());

			if (query.HasRows)
            {
                query.Read();
                m_clanID = query.GetInt32("max_clan_id");
            }
            query.Close();
            if (clanLeader.m_zone != null)
            {
                clanLeader.m_zone.SendUpdatedCharacerToAllPlayers(Program.Server, clanLeader);
            }
            Program.processor.m_worldDB.runCommandSync("update character_details set clan_id=" + m_clanID + " where character_id=" + clanLeader.m_character_id);
            Program.processor.m_worldDB.runCommandSync("insert into clan_members (clan_id,character_id,clan_rank) values (" + m_clanID + "," + m_leaderID + "," + (int)CLAN_RANKS.LEADER + ")");

        }
        
        internal void AddReturningMember(Character returningCharacter, uint characterID)
        {
            //check the member does not exist already
            ClanMember existingMember = GetMember((int)characterID);
            //there is no need to load them up again
            if (existingMember != null)
            {
                return;
            }
            SqlQuery membersQuery = new SqlQuery(Program.processor.m_worldDB, "select * from clan_members where clan_id=" + m_clanID + " and character_id =" + characterID);

            if (membersQuery.HasRows == true)
            {
                membersQuery.Read();
                int character_id = membersQuery.GetInt32("character_id");
                CLAN_RANKS rank = (CLAN_RANKS)membersQuery.GetInt32("clan_rank");
                ClanMember newMember = ClanMember.AddFriend(character_id, ClanMembers, rank, false);
                if (rank == CLAN_RANKS.LEADER)
                {
                    //this should not be possible at this stage
                    if (Leader == null)
                    {
                        Leader = newMember;
                    }
                    else
                    {
                        //how can there be two leaders, report error

                        Program.Display("Clan Error: Additional Leader found Current Leader " + Leader.GetIDString() + " additional Leader " + newMember.GetIDString());
                    }
                }

            }
            membersQuery.Close();
        }

        internal bool PromoteMember(int memberID, CLAN_RANKS oldRank, int promoterID)
        {
            bool promotionSuceeded = false;
            ClanMember promoter = null;
            if (Leader != null && Leader.CharacterID == promoterID)
            {
                promoter = Leader;
            }
            //find the promoter
            if (promoter == null)
            {
                promoter = ClanMember.ContainsTemplateForID(ClanMembers, promoterID);
            }
            //find the member to promote
            ClanMember member = ClanMember.ContainsTemplateForID(ClanMembers, memberID);

            
            if (promoter != null && member != null)
            {
                //are they at the rank the promoter expects
                if (member.Position == oldRank)
                {
                    CLAN_RANKS promoterRank = promoter.Position;
                    CLAN_RANKS memberRank = member.Position;
                    //does the promoter have the correct rights to promote this person
                    bool canPromote = CanPromoteMember(memberRank, promoterRank);
                    CLAN_RANKS newRank = memberRank + 1;
                    //if so carry out the promotion
                    if (canPromote == true)
                    {

                        //if it's a promotion to leader then the promoter must be demoted to General
                        if (newRank == CLAN_RANKS.LEADER)
                        {
                            if (promoter == Leader)
                            {
                                Leader = member;
                                
                                member.Position = newRank;
                                promoter.Position = CLAN_RANKS.GENERAL;
                                //remove member from the members list in db
                                //add the old leader to the members list in db
                                m_leaderID = Leader.CharacterID;

                                Program.processor.m_worldDB.runCommandSync("update clan_details set clan_leader=" + memberID + " where clan_id=" + m_clanID);
                                
                                member.SaveOutRank(ClanID);
                                promoter.SaveOutRank(ClanID);
                                promotionSuceeded = true;
                            }

                        }
                        else
                        {

                            member.Position = newRank;
                            member.SaveOutRank(ClanID);
                            promotionSuceeded = true;

                        }

                        if ((int)newRank < PositionNameIDs.Length)
                        {
							List<Player> playerList = new List<Player>();
							AddPlayersToList(playerList, -1, member.CharacterID);
							foreach (Player player in playerList)
							{
								string locText = Localiser.GetString(textDB, player, (int)ClanTextDB.TextID.OTHER_PROMOTED);
								locText = string.Format(locText, member.CharacterName, GetPositionName((int)newRank, player), promoter.CharacterName);
								Program.processor.sendSystemMessage(locText, player, true, SYSTEM_MESSAGE_TYPE.CLAN);
							}

                            //send a specific message to the player involved
                            if (member.Character != null && member.Character.m_player != null)
                            {
								string locText = Localiser.GetString(textDB, member.Character.m_player, (int)ClanTextDB.TextID.PLAYER_PROMOTED);
								locText = string.Format(locText, GetPositionName((int)newRank, member.Character.m_player), promoter.CharacterName);
								Program.processor.sendSystemMessage(locText, member.Character.m_player, false, SYSTEM_MESSAGE_TYPE.CLAN);
                            }
                        }
                    }
                    else
                    {
                        if ((int)newRank < PositionNameIDs.Length)
                        {
                            //this person does not have the right to carry out this promotion
                            if (promoter.Character != null && promoter.Character.m_player != null)
                            {
								string locText = Localiser.GetString(textDB, promoter.Character.m_player, (int)ClanTextDB.TextID.INCORRECT_RANK_PROMOTE);
								locText = string.Format(locText, GetPositionName((int)newRank, promoter.Character.m_player));
								Program.processor.sendSystemMessage(locText, promoter.Character.m_player, false, SYSTEM_MESSAGE_TYPE.CLAN);
                            }
                        }

                    }



                }
                else
                {
                    //this is likely a duplicate from the client, do not process
                }

            }
            if (promotionSuceeded == true)
            {
                ClanDataChanged();
            }

            return promotionSuceeded;

        }
        
        internal bool DemoteMember(int memberID, CLAN_RANKS oldRank, int demoterID)
        {

            bool demoteSucessful = false;

            ClanMember demoter = null;
            if (Leader != null && Leader.CharacterID == demoterID)
            {
                demoter = Leader;
            }
            //find the demoter
            if (demoter == null)
            {
                demoter = ClanMember.ContainsTemplateForID(ClanMembers, demoterID);
            }
            ClanMember member = null;
            if (demoterID == memberID)
            {
                member = demoter;
            }
            //find the member to demote
            if (member == null)
            {
                member = ClanMember.ContainsTemplateForID(ClanMembers, memberID);
            }

            if (demoter != null && member != null)
            {
                //are they at the rank the promoter expects
                if (member.Position == oldRank)
                {
                    CLAN_RANKS demoterRank = demoter.Position;
                    CLAN_RANKS memberRank = member.Position;
                    //does the promoter have the correct rights to promote this person
                    bool canDemote = CanDemoteMember(memberRank, demoterRank);

                    //you can demote yourself unless you are the leader
                    if (canDemote == false)
                    {
                        canDemote = (demoterID == memberID && demoter != Leader);
                    }

                    //if so carry out the promotion
                    if (canDemote == true)
                    {
                        CLAN_RANKS newRank = memberRank - 1;
                        //recruits get fully removed from the clan
                        if (memberRank == CLAN_RANKS.RECRUIT)
                        {
                            RemoveMember(member);
                            demoteSucessful = true;
                            if (member.Character != null)
                            {
                                member.Character.PVPListNeedsChecked();
                            }

							int textID = (int)ClanTextDB.TextID.LEFT_THE_CLAN;
                            //send a specific message to the player involved
                            if (member.Character != null && member.Character.m_player != null)
                            {
                                if (demoter != member)
                                {
									textID = (int)ClanTextDB.TextID.OTHER_KICKED_FROM_CLAN;
									string locText = Localiser.GetString(textDB, member.Character.m_player, (int)ClanTextDB.TextID.PLAYER_KICKED_FROM_CLAN);
									locText = string.Format(locText, demoter.CharacterName);
									Program.processor.sendSystemMessage(locText, member.Character.m_player, false, SYSTEM_MESSAGE_TYPE.CLAN);
                                }
                                else
                                {
									string locText = Localiser.GetString(textDB, member.Character.m_player, (int)ClanTextDB.TextID.PLAYER_LEFT_THE_CLAN);
									Program.processor.sendSystemMessage(locText, member.Character.m_player, false, SYSTEM_MESSAGE_TYPE.CLAN);
                                }
                            }

							List<Player> playerList = new List<Player>();
							AddPlayersToList(playerList, -1, member.CharacterID);
							foreach (Player player in playerList)
							{
								string locText = Localiser.GetString(textDB, player, textID);
								locText = string.Format(locText, member.CharacterName, demoter.CharacterName);
								Program.processor.sendSystemMessage(locText, player, true, SYSTEM_MESSAGE_TYPE.CLAN);
							}

                            if (member.Character != null)
                            {
                                Clan.sendNoClanMessage(member.Character);
                                if (demoter == member)
                                {
                                    if (Program.m_LogAnalytics)
                                    {
                                        AnalyticsMain logAnalytics = new AnalyticsMain(false);
                                        logAnalytics.LogGuildEvent(member.Character.m_player, "", "", m_clanName, Analytics.Social.GuildAction.LEFT.ToString());
                                  
                                    }
                                }
                            }
                            else
                            {
                                if (demoter != member && demoter != null && demoter.Character != null)
                                {
                                    if (Program.m_LogAnalytics)
                                    {
                                        AnalyticsMain logAnalytics = new AnalyticsMain(false);
                                        logAnalytics.LogGuildEvent(demoter.Character.m_player, member.CharacterID.ToString(), member.CharacterName, m_clanName, Analytics.Social.GuildAction.KICKED.ToString());
                                    }
                                }
                            }

                        }
                        else
                        {
                            member.Position = newRank;
                            member.SaveOutRank(ClanID);
                            demoteSucessful = true;

                            if ((int)newRank >= 0 && (int)newRank < PositionNameIDs.Length)
                            {
								int textID = (int)ClanTextDB.TextID.OTHER_DEMOTED;
                                if (demoter == member)
                                {
                                    Character memberChar = member.Character;
									textID = (int)ClanTextDB.TextID.DEMOTED_HIMSELF;
                                    if (memberChar != null && memberChar.m_gender == GENDER.FEMALE)
                                    {
										textID = (int)ClanTextDB.TextID.DEMOTED_HERSELF;
                                    }
                                }
								List<Player> playerList = new List<Player>();
								AddPlayersToList(playerList, -1, member.CharacterID);
								foreach (Player player in playerList)
								{
									string locText = Localiser.GetString(textDB, player, textID);
									locText = string.Format(locText, member.CharacterName, GetPositionName((int)newRank, player), demoter.CharacterName);
									Program.processor.sendSystemMessage(locText, player, true, SYSTEM_MESSAGE_TYPE.CLAN);
								}

                                //send a specific message to the player involved
                                if (member.Character != null && member.Character.m_player != null)
                                {
									textID = (int)ClanTextDB.TextID.PLAYER_DEMOTED_BY;
                                    if (demoter == member)
                                    {
										textID = (int)ClanTextDB.TextID.PLAYER_DEMOTED_SELF;
                                    }
									string locText = Localiser.GetString(textDB, member.Character.m_player, textID);
									locText = string.Format(locText, GetPositionName((int)newRank, member.Character.m_player), demoter.CharacterName);
									Program.processor.sendSystemMessage(locText, member.Character.m_player, false, SYSTEM_MESSAGE_TYPE.CLAN);
                                }
                            }

                        }

                    }
                    else
                    {
                        //they are not the correct rank to demote

                        //check you can send them a message
                        if (demoter.Character != null && demoter.Character.m_player != null && (int)memberRank < PositionNameIDs.Count())
                        {

                            //are they trying to demote themselves but they are the leader
                            if (demoter == member && member == Leader)
                            {
                                //otherwise there rank must not be high enough
								string locText = Localiser.GetString(textDB, demoter.Character.m_player, (int)ClanTextDB.TextID.PLAYER_LEAVE_CLAN);
								locText = string.Format(locText, GetPositionName((int)CLAN_RANKS.LEADER, demoter.Character.m_player));
								Program.processor.sendSystemMessage(locText, demoter.Character.m_player, false, SYSTEM_MESSAGE_TYPE.CLAN);
                            }
                            else
                            {

                                //otherwise there rank must not be high enough
								string locText = Localiser.GetString(textDB, demoter.Character.m_player, (int)ClanTextDB.TextID.INCORRECT_RANK_DEMOTE);
								locText = string.Format(locText, GetPositionName((int)memberRank, demoter.Character.m_player));
								Program.processor.sendSystemMessage(locText, demoter.Character.m_player, false, SYSTEM_MESSAGE_TYPE.CLAN);
                            }

                        }
                    }
                }
                else
                {
                    //the member is not the rank they expected, this is probably a duplicate 
                }

            }
            if (demoteSucessful == true)
            {
                ClanDataChanged();
            }
            return demoteSucessful;
        }
        
        void RemoveMember(ClanMember memberToRemove)
        {
            Player removedPlayer = Program.processor.getPlayerFromActiveCharacterId(memberToRemove.CharacterID);
            if (removedPlayer != null)
            {
                removedPlayer.m_activeCharacter.CharactersClan = null;
                if (removedPlayer.m_activeCharacter.m_zone != null)
                {
                    removedPlayer.m_activeCharacter.m_zone.SendUpdatedCharacerToAllPlayers(Program.Server, removedPlayer.m_activeCharacter);
                }
            }
            ClanMembers.Remove(memberToRemove);
            Program.processor.m_worldDB.runCommandSync("update character_details set clan_id=" + HW_NO_CLAN_ID + " where character_id=" + memberToRemove.CharacterID);
            Program.processor.m_worldDB.runCommandSync("delete from clan_members where clan_id=" + m_clanID + " and character_id=" + memberToRemove.CharacterID);
        }
        
        internal void AddMember(Character newMember)
        {
            ClanMember memberTemplate = new ClanMember(newMember);

            if (memberTemplate != null)
            {
                if (newMember != null)
                {
                    newMember.PVPListNeedsChecked();
                }
                memberTemplate.Position = CLAN_RANKS.RECRUIT;
				List<Player> playerList = new List<Player>();
				AddPlayersToList(playerList, -1, -1);
				//List<NetConnection> clanConnections = new List<NetConnection>();
				//AddConnectionsToList(clanConnections, -1, -1);

                ClanMembers.Add(memberTemplate);
                newMember.CharactersClan = this;
                Program.processor.m_worldDB.runCommandSync("update character_details set clan_id=" + m_clanID + " where character_id=" + newMember.m_character_id);
                Program.processor.m_worldDB.runCommandSync("insert into clan_members (clan_id,character_id,clan_rank) values (" + m_clanID + "," + newMember.m_character_id + ",0)");
                newMember.increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.CLANSMAN, 1);
				foreach (Player player in playerList)
				{
					string locText = Localiser.GetString(textDB, player, (int)ClanTextDB.TextID.JOINED_CLAN);
					locText = string.Format(locText, newMember.m_name);
					Program.processor.sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.CLAN);
				}

                ClanDataChanged();
            }
            if (newMember.m_zone != null)
            {
                newMember.m_zone.SendUpdatedCharacerToAllPlayers(Program.Server, newMember);
            }

        }
        
        bool CanPromoteMember(CLAN_RANKS memberRank, CLAN_RANKS promoterRank)
        {
            bool canPromote = false;


            /*CLAN_RANKS promoterRank = promoter.Position;
            CLAN_RANKS memberRank = member.Position;*/
            CLAN_RANKS newRank = memberRank + 1;

            if ((promoterRank >= CLAN_RANKS.NOBLE && promoterRank > newRank) || (promoterRank == CLAN_RANKS.LEADER && memberRank != CLAN_RANKS.LEADER))
            {
                canPromote = true;
            }


            return canPromote;
        }

        bool CanDemoteMember(CLAN_RANKS memberRank, CLAN_RANKS demoterRank)
        {
            bool canDemote = false;

            CLAN_RANKS newRank = memberRank - 1;

            if (demoterRank >= CLAN_RANKS.NOBLE && demoterRank > memberRank && memberRank != CLAN_RANKS.LEADER)
            {
                canDemote = true;
            }


            return canDemote;
        }
        
        /// <summary>
        /// Make current member the leader of the clan
        /// </summary>
        /// <param name="memberID"></param>
        internal void MakeLeader(int memberID)
        {
            // find the new leader from the member id
            ClanMember newLeader = ClanMember.ContainsTemplateForID(ClanMembers, memberID);            
            if (newLeader == null)            
                return;

            //note who old leader was.
            ClanMember oldLeader = Leader;

            //perform rank swap
            oldLeader.Position = CLAN_RANKS.GENERAL;
            newLeader.Position = CLAN_RANKS.LEADER;

            //note new Leader
            Leader = newLeader;            
            m_leaderID = newLeader.CharacterID;
                                    
            Program.processor.m_worldDB.runCommandSync("update clan_details set clan_leader=" + memberID + " where clan_id=" + m_clanID);
            newLeader.SaveOutRank(ClanID);
            oldLeader.SaveOutRank(ClanID);

            ClanDataChanged();

            #region handle achievements

            //if the new leader is online then update the character
            if (Leader.Character != null)
            {
                Leader.Character.increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.CHIEFTAIN, 1);
            }
            else
            {
                //otherwise teporarily Load up the character and award the achievement
                Player tempPlayer = new Player(Program.processor.m_universalHubDB, (int)Leader.AccountID);
                Character tempCharacter = new Character(Program.processor.m_worldDB, tempPlayer);

                if (tempCharacter.SetUpCharacterWithDetails(Program.processor.m_worldDB, (long)Leader.AccountID, (uint)Leader.CharacterID, Leader.CharacterName) == false)
                {
                    Program.Display("Failed to load character " + Leader.CharacterID + " " + Leader.CharacterName + " to award leadership achievement, account ID = " + Leader.AccountID);
                }
                else
                {
                    tempCharacter.increaseAchievement(AchievementsManager.ACHIEVEMENT_TYPE.CHIEFTAIN, 1);
                }
            }

            #endregion

        }
       
        internal void DisbandClan()
        {
            //remove Clan ID For All members
            Program.Display("Disbanding Clan (" + m_clanID + "," + m_clanName + ")");

            Program.processor.m_worldDB.runCommandSync("update character_details set clan_id=" + HW_NO_CLAN_ID + " where clan_id=" + m_clanID);

            //remove the file from the database
            Program.processor.m_worldDB.runCommandSync("delete from clan_details where clan_id=" + m_clanID);
            Program.processor.m_worldDB.runCommandSync("delete from clan_members where clan_id=" + m_clanID);
            if (Leader != null && Leader.Character != null)
            {
                Leader.Character.CharactersClan = null;
                if (Leader.Character.m_zone != null)
                {
                    Leader.Character.m_zone.SendUpdatedCharacerToAllPlayers(Program.Server, Leader.Character);
                }
            }
            Leader = null;
        }

        /// <summary>
        /// Removes a character reguardless of their position
        /// </summary>
        /// <param name="characterID"></param>
        internal void RemoveCharacter(int characterID)
        {
            ClanMember characterToRemove = null;
            bool actionTaken = false;

            if (Leader != null && Leader.CharacterID == characterID)
            {
                AppointNewLeader(characterID);
                actionTaken = true;
            }
            characterToRemove = ClanMember.ContainsTemplateForID(ClanMembers, characterID);
            if (characterToRemove != null)
            {
                RemoveMember(characterToRemove);
                actionTaken = true;
            }            

            //if the leader is null the clan should have been disbanded
            if (actionTaken == true && Leader != null)
            {
                SendClanListToAllMembers();
            }

        }
       
        internal void WriteClanToMessage(NetOutgoingMessage msg)
        {
            msg.WriteVariableInt32(m_clanID);
            msg.Write(m_clanName);            
            ClanMember.WriteClanMembersToMessage(ClanMembers, msg);            
            msg.Write(m_clanMessage);
        }

        /// <summary>
        /// A character is being deleted, we want to promote someone else from within to be the new leader
        /// </summary>
        /// <param name="notThisCharacterID">characterId being deleted, so exclude them from the ranks</param>
        void AppointNewLeader(int notThisCharacterID)
        {            
            //start from the top rank of general and promote
            for (CLAN_RANKS currentRank = CLAN_RANKS.GENERAL; currentRank > CLAN_RANKS.NONE; currentRank--)
            {
                for (int i = 0; i < ClanMembers.Count; i++)
                {
                    ClanMember currentMember = ClanMembers[i];
                    if (currentMember != null && currentMember.Position >= currentRank && currentMember.CharacterID != notThisCharacterID)
                    {
                        Program.Display("AppointNewLeader." + currentMember.CharacterID + " " + currentMember.CharacterName);
                        MakeLeader(currentMember.CharacterID);
                        return;
                    }
                }


            }

            //otherwise disband
            DisbandClan();
        }        

        internal NetOutgoingMessage BuildClanList()
        {

            NetOutgoingMessage clanmsg = null;

            if (m_clanListMessage == null)
            {
                clanmsg = Program.Server.CreateMessage();
                clanmsg.WriteVariableUInt32((uint)NetworkCommandType.ClanMessage);
                clanmsg.WriteVariableInt32((int)HW_CLAN_MESSAGE_TYPE.HW_CLAN_MESSAGE_TYPE_CLAN_LIST);
                WriteClanToMessage(clanmsg);
                
                m_clanListMessage = clanmsg.PeekDataBuffer();
                m_clanListMessageLength = (int)clanmsg.LengthBytes;
            }
            else
            {
                clanmsg = Program.Server.CreateMessage();

                clanmsg.Write(m_clanListMessage, 0, m_clanListMessageLength);

            }

            return clanmsg;
        }
        internal void SendClanListToAllMembers()
        {
            List<NetConnection> connectionList = new List<NetConnection>();
            AddConnectionsToList(connectionList, -1, -1);            
            NetOutgoingMessage outmsg = BuildClanList();
            if (m_clanListMessage != null)
            {
                Program.processor.SendMessage(outmsg, connectionList, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.ClanMessage);
            }
        }
        internal void ClanDataChanged()
        {
            m_clanListMessage = null;
        }
        internal void SendClanListToPlayer(Player thePlayer)
        {            
            NetOutgoingMessage outmsg = BuildClanList();
            if (m_clanListMessage != null)
            {
                Program.processor.SendMessage(outmsg, thePlayer.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.ClanMessage);
            }
        }
        static internal void sendNoClanMessage(Character theCharacter)
        {
            NetOutgoingMessage outmsg = Program.Server.CreateMessage();
            outmsg.WriteVariableUInt32((uint)NetworkCommandType.ClanMessage);
            outmsg.WriteVariableInt32((int)HW_CLAN_MESSAGE_TYPE.HW_CLAN_MESSAGE_TYPE_CLAN_LIST);
            outmsg.WriteVariableInt32(-1);
            Program.processor.SendMessage(outmsg, theCharacter.m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.ClanMessage);
        }
        internal void AddConnectionsToList(List<NetConnection> connectionList, int senderID, int characterToExclude)
        {           
            for (int i = 0; i < ClanMembers.Count; i++)
            {
                ClanMember currentMember = ClanMembers[i];
                if (currentMember.Character != null)
                {
                    if ((senderID < 0) || (currentMember.Character.HasBlockedCharacter(senderID) == false))
                    {
                        if (currentMember != null && currentMember.Character != null && currentMember.Character.m_player != null 
                            && currentMember.Character.m_player.connection != null && currentMember.CharacterID != characterToExclude)
                        {
                            connectionList.Add(currentMember.Character.m_player.connection);
                        }
                    }
                }
            }           
        }
		// same logic as AddConnectionsToList but add players instead
		internal void AddPlayersToList(List<Player> playerList, int senderID, int characterToExclude)
		{
			for (int i = 0; i < ClanMembers.Count; i++)
			{
				ClanMember currentMember = ClanMembers[i];
				if (currentMember.Character != null)
				{
					if ((senderID < 0) || (currentMember.Character.HasBlockedCharacter(senderID) == false))
					{
						if (currentMember != null && currentMember.Character != null && currentMember.Character.m_player != null
							&& currentMember.Character.m_player.connection != null && currentMember.CharacterID != characterToExclude)
						{
							playerList.Add(currentMember.Character.m_player);
						}
					}
				}
			}
		}
        /// <summary>
        /// Returns true if thes character is a mamber of the guild
        /// </summary>
        /// <param name="theMember"></param>
        /// <returns></returns>
        internal bool MemberUpdate(Character theMember)
        {
            FriendTemplate memberTemplate = GetMember((int)theMember.m_character_id);
            if (memberTemplate != null)
            {
                if (memberTemplate.Online == false)
                {
					List<Player> playerList = new List<Player>();
					AddPlayersToList(playerList, -1, -1);
					foreach (Player player in playerList)
					{
						string locText = Localiser.GetString(textDB, player, (int)ClanTextDB.TextID.LOGGED_IN);
						locText = string.Format(locText, memberTemplate.CharacterName);
						Program.processor.sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.CLAN);
                }
                }
                memberTemplate.UpdateWithDetails(theMember);
                memberTemplate.Online = true;
                ClanDataChanged();
                return true;
            }

            return false;
        }
        internal void MemberLogout(Character theMember)
        {
            FriendTemplate memberTemplate = GetMember((int)theMember.m_character_id);
            if (memberTemplate != null)
            {
                memberTemplate.UpdateWithDetails(theMember);
                memberTemplate.Online = false;
                memberTemplate.Character = null;

				List<Player> playerList = new List<Player>();
				AddPlayersToList(playerList, -1, -1);
				foreach (Player player in playerList)
				{
					string locText = Localiser.GetString(textDB, player, (int)ClanTextDB.TextID.LOGGED_OUT);
					locText = string.Format(locText, memberTemplate.CharacterName);
					Program.processor.sendSystemMessage(locText, player, false, SYSTEM_MESSAGE_TYPE.CLAN);
				}

                ClanDataChanged();
            }
        }
        internal ClanMember GetMember(int characterID)
        {
            ClanMember theMember = null;
            if (m_leaderID == characterID)
            {
                theMember = Leader;

            }
            for (int i = 0; (i < ClanMembers.Count) && (theMember == null); i++)
            {
                ClanMember currentMember = ClanMembers[i];
                if (currentMember.CharacterID == characterID)
                {
                    theMember = currentMember;
                }
            }

            return theMember;
        }

        internal bool HasLeaderRights(int characterID)
        {
            if (characterID == Leader.CharacterID)
            {
                return true;
            }

            return false;
        }
        internal bool HasEditClanMessageRights(int characterID)
        {
            if (characterID == Leader.CharacterID)
            {
                return true;
            }
            ClanMember clanMember = GetMember(characterID);
            if (clanMember.Position >= CLAN_RANKS.GENERAL)
            {
                return true;
            }
            return false;

        }
        internal bool HasInviteRights(int characterID)
        {           
            ClanMember clanMember = GetMember(characterID);
            if (clanMember.Position >= CLAN_RANKS.NOBLE)
            {
                return true;
            }

            return false;
        }

        internal void SendClanChatMessege(string chatMessage, Player sendingPlayer)
        {
            List<NetConnection> connectionList = new List<NetConnection>();
            int sendersID = (int)sendingPlayer.m_activeCharacter.m_character_id;


            AddConnectionsToList(connectionList, sendersID, -1);
            NetOutgoingMessage outmsg = Program.Server.CreateMessage();

            outmsg.WriteVariableUInt32((uint)NetworkCommandType.GeneralChat);
            outmsg.WriteVariableInt32((int)HW_CHAT_BOX_CHANNEL.HW_CHAT_BOX_CHANNEL_GUILD);
            outmsg.Write(sendingPlayer.m_activeCharacter.m_name);
            outmsg.Write(chatMessage);
            outmsg.WriteVariableInt32((int)sendingPlayer.m_activeCharacter.m_character_id);
            Program.Display("got clan chat message from " + sendingPlayer.m_activeCharacter.m_name + " : " + chatMessage);
            Program.processor.SendMessage(outmsg, connectionList, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Chat, NetworkCommandType.GeneralChat);
        }

        internal void SetClanMessage(string newMessage, int editingCharacterID)
        {
            // make it clean 1st
            newMessage = Regex.Replace(newMessage, Localiser.TextSymbolNewLineFilter, "");
            newMessage = ProfanityFilter.replaceOffendingStrings(newMessage);

            //messages should be no longer than 300 characters - so say we all
            if (newMessage.Length > 300)
                newMessage = newMessage.Substring(0, 300);
            
            // find out who changed it
            ClanMember editingCharacter = GetMember(editingCharacterID);
            //if you could find them then add their name and the date/time to the message
            if (editingCharacter != null)
            {
				// should we localise this text?
				string locText    = Localiser.GetString(textDB, editingCharacter.Character.m_player, (int)ClanTextDB.TextID.EDITED_BY);
                string appendText = String.Format(locText, editingCharacter.CharacterName);
                newMessage += appendText;
            }

            m_clanMessage = newMessage;
			//Program.processor.m_worldDB.runCommandSync("update clan_details set clan_message=\"" + m_clanMessage + "\" where clan_id=" + m_clanID);

			List<MySqlParameter> sqlParams = new List<MySqlParameter>();
			sqlParams.Add(new MySqlParameter("@clan_message", m_clanMessage));
			sqlParams.Add(new MySqlParameter("@clan_id", m_clanID));

			Program.processor.m_worldDB.runCommandSyncWithParams("update clan_details set clan_message=@clan_message where clan_id=@clan_id", sqlParams.ToArray());

			ClanDataChanged();
        }
        
        internal void AddClanMemberFromDatabase(SqlQuery membersQuery)
        {
            ClanMember newMember = ClanMember.AddFriend(ClanMembers, membersQuery);
        }

        internal void AddLeaderFromDatabase(int character_id)
        {
            bool onlyLoadRecent = true;

            if (character_id == m_leaderID)
            {
                onlyLoadRecent = false;
            }
            ClanMember newMember = ClanMember.AddFriend(character_id, ClanMembers, Clan.CLAN_RANKS.LEADER, onlyLoadRecent);


            if (Leader == null)
            {
                Leader = newMember;
            }
            else
            {
                //how can there be two leaders, report error

                Program.Display("Clan Error: Additional Leader found Current Leader " + Leader.GetIDString() + " additional Leader " + newMember.GetIDString());
            }

        }

        //first try to find the leader in the current clan list
        internal void checkHasLeader(Database db)
        {
            if (Leader == null)
            {
                ClanMember lostLeader = GetMember(m_leaderID);

                if (lostLeader != null)
                {
                    lostLeader.Position = Clan.CLAN_RANKS.LEADER;
                    lostLeader.SaveOutRank(m_clanID);
                    Leader = lostLeader;
                }
            }
            if (Leader == null)
            {
                //find the leaders data

                string sqlstr = "select * from character_details where character_id =" + m_leaderID;
                //Character character;

                SqlQuery query = new SqlQuery(Program.processor.m_worldDB, sqlstr);
                if (query.HasRows)
                {
                    query.Read();

                    int character_id = m_leaderID; //query.GetInt32("character_id");
                    //name
                    string name = query.GetString("name");
                    //location
                    int zone = query.GetInt32("zone");
                    //level
                    int level = query.GetInt32("level");
                    //class
                    int characterClass = query.GetInt32("class_id");
                    int characterRace = query.GetInt32("race_id");

                    ClanMember newFriend = new ClanMember();

                    newFriend.CharacterID = character_id;

                    newFriend.CharacterName = name;
                    newFriend.Level = level;
                    newFriend.Online = false;
                    newFriend.Zone = zone;
                    newFriend.Class = characterClass;
                    newFriend.Race = characterRace;
                    newFriend.Position = Clan.CLAN_RANKS.LEADER;
                    Leader = newFriend;

                    //Save the leader out to be with the other members
                    Program.processor.m_worldDB.runCommandSync("replace into clan_members (clan_id,character_id,clan_rank) values (" + m_clanID + "," + character_id + "," + (int)Clan.CLAN_RANKS.LEADER + ")");

                    ClanMembers.Insert(0, Leader);
                }
                else
                {
                    //the clan leader has been lost
                    //assign new leader or disband
                    AppointNewLeader(-1);

                }
                query.Close();
            }
        }
    }
}
