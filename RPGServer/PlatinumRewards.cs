using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MainServer
{
    class PlatinumRewards
    {
        internal enum REWARD_TYPES
        {
            NONE=0,
            FACEBOOK_POSTS=1,
            TWITTER_TWEET = 2
        }
        /// <summary>
        /// what the reward was given for
        /// </summary>
        REWARD_TYPES m_rewardType = REWARD_TYPES.NONE;
        /// <summary>
        /// An amount specific to the type of reward
        /// eg. number of friends
        /// </summary>
        int m_rewardValue=0;
        /// <summary>
        /// the amount of platinum awarded
        /// </summary>
        int m_platinumAwarded = 0;


        /// <summary>
        /// what the reward was given for
        /// </summary>
        internal REWARD_TYPES RewardType
        {
            get { return m_rewardType; }
        }
        /// <summary>
        /// An amount specific to the type of reward
        /// eg. number of friends
        /// </summary>
        internal int RewardValue
        {
            get { return m_rewardValue; }
        }
        /// <summary>
        /// the amount of platinum awarded
        /// </summary>
        internal int PlatinumAwarded
        {
            get { return m_platinumAwarded; }
        }
        public PlatinumRewards(int rewardID,REWARD_TYPES rewardType, int rewardValue, int platinumAwarded, long accountID, uint characterID, string description,DateTime timeAwarded)
        {
            m_rewardType = rewardType;
            m_rewardValue = rewardValue;
            m_platinumAwarded = platinumAwarded;
        
        }
        ~PlatinumRewards()
        {

        }

        /// <summary>
        /// Creates a new record of the reward in the database and then returns a copy to be held in code
        /// </summary>
        /// <returns></returns>
        static internal PlatinumRewards SaveReward(REWARD_TYPES rewardType, int rewardValue, int platinumAwarded, long accountID, uint characterID, string description, string rewardString)
        {
            //return value
            PlatinumRewards newReward = null;

            //get the time in string form
            DateTime timeAwarded = DateTime.Now;
            string dateString = timeAwarded.ToString("yyyy-MM-dd HH:mm:ss");

			//add to the database
			/*Program.processor.m_universalHubDB.runCommand("insert into platinum_rewards (account_id,character_id,reward_type,reward_value,platinum_awarded,time_awarded,description,reward_string) values ("
                + accountID + "," + characterID + "," + (int)rewardType + "," + rewardValue + "," + platinumAwarded + ",'" + dateString + "','" + description + "','" + rewardString+"')");*/

			List<MySqlParameter> sqlParams = new List<MySqlParameter>();
			sqlParams.Add(new MySqlParameter("@account_id", accountID));
			sqlParams.Add(new MySqlParameter("@character_id", characterID));
			sqlParams.Add(new MySqlParameter("@reward_type", (int)rewardType));
			sqlParams.Add(new MySqlParameter("@reward_value", rewardValue));
			sqlParams.Add(new MySqlParameter("@platinum_awarded", platinumAwarded));
			sqlParams.Add(new MySqlParameter("@time_awarded", dateString));
			sqlParams.Add(new MySqlParameter("@description", description));
			sqlParams.Add(new MySqlParameter("@reward_string", rewardString));

			Program.processor.m_universalHubDB.runCommandWithParams("insert into platinum_rewards (account_id,character_id,reward_type,reward_value,platinum_awarded,time_awarded,description,reward_string) " +
				"values (@account_id, @character_id, @reward_type, @platinum_awarded, @time_awarded, @description, @reward_string)", sqlParams.ToArray());

			//get the ID of the record
			int rewardID = -1;
			//SqlQuery query = new SqlQuery(Program.processor.m_universalHubDB, "select reward_id from platinum_rewards where account_id=" + accountID + " and time_awarded='" + dateString + "' and description='" + description + "'");

			sqlParams.Clear();
			sqlParams.Add(new MySqlParameter("@account_id", accountID));
			sqlParams.Add(new MySqlParameter("@time_awarded", dateString));
			sqlParams.Add(new MySqlParameter("@description", description));

			SqlQuery query = new SqlQuery(Program.processor.m_universalHubDB, "select reward_id from platinum_rewards where account_id=@account_id and time_awarded=@time_awarded and description=@description", sqlParams.ToArray());

			if (query.HasRows)
            {
                query.Read();
                rewardID = query.GetInt32("reward_id");
            }

            query.Close();


            //now all the data is ready, create and return the new code record
            newReward = new PlatinumRewards(rewardID, rewardType, rewardValue, platinumAwarded, accountID, characterID, description, timeAwarded);
            return newReward;

        }
        static internal void GetRewardsForAccount(long accountID, ref List<PlatinumRewards> listToAddTo)
        {
          //  Program.processor.m_universalHubDB.runCommand("insert into platinum_rewards (account_id,character_id,reward_type,reward_value,platinum_awarded,time_awarded,description) values ("
          //      + accountID + "," + characterID + "," + (int)rewardType + "," + rewardValue + "," + platinumAwarded + ",'" + timeAwarded.ToString("yyyy-MM-dd HH:mm:ss") + "','" + description + "')");


            SqlQuery query = new SqlQuery(Program.processor.m_universalHubDB, "select * from platinum_rewards where account_id =" + accountID);
            //did it find anything
            if (query.HasRows)
            {
                //for each record found
                while ((query.Read()))
                {
                    REWARD_TYPES rewardType = (REWARD_TYPES)query.GetInt32("reward_type");
                    int rewardID = query.GetInt32("reward_id");
                    int rewardValue = query.GetInt32("reward_value");
                    int platinumAwarded = query.GetInt32("platinum_awarded");
                    uint characterID = query.GetUInt32("character_id");
                    string description = query.GetString("description");
                    DateTime timeAwarded = query.GetDateTime("time_awarded");

                    PlatinumRewards newReward = new PlatinumRewards(rewardID, rewardType, rewardValue, platinumAwarded, accountID, characterID, description, timeAwarded);
                    listToAddTo.Add(newReward);
                }
            }
            query.Close();

        }
    
    }

}
