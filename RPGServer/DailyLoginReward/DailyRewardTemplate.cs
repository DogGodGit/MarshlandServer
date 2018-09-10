using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainServer.DailyLoginReward
{
    class DailyRewardTemplate
    {

        internal int StepID { get; private set; }
        /// <summary>
        /// the type of item this offer contains
        /// </summary>
        internal int ItemTemplateID { get; private set; }
        /// <summary>
        /// The number of items this offer contains
        /// </summary>
        internal int Quantity { get; private set; }
        /// <summary>
        /// Is this step a high value reward
        /// </summary>
        internal bool IsPriorityReward { get; private set; }

        internal DailyRewardTemplate(SqlQuery query)
        {
            StepID = query.GetInt32("step_id");
            ItemTemplateID = query.GetInt32("item_template_id");
            Quantity = query.GetInt32("quantity");
            IsPriorityReward = bool.Parse(query.GetString("is_priority_reward"));
        }
    }

    static class DailyRewardTemplateManager
    {
        static List<DailyRewardTemplate> m_dailyRewardTemplates = new List<DailyRewardTemplate>();
        public static int MAX_STEPS { get; private set; } // Should be set when steps are read in from DB
        private const string GetDailyRewardsQuery = "select * from daily_reward_templates order by step_id asc";

        public static bool m_RewardsLoaded = false;

        internal static void LoadDailyRewards(Database db)
        {
       
            ClearDailyRewardTemplates();

            SqlQuery query = new SqlQuery(db, GetDailyRewardsQuery);
            int tempMaxStep = 0;
            if (query.HasRows)
            {
                while (query.Read())
                {
                    DailyRewardTemplate newReward = new DailyRewardTemplate(query);
                    m_dailyRewardTemplates.Add(newReward);

                    tempMaxStep = newReward.StepID > tempMaxStep ? newReward.StepID : tempMaxStep;
                }
            }
            query.Close();

            MAX_STEPS = tempMaxStep;
            m_RewardsLoaded = true;
        }

        static public List<DailyRewardTemplate> GetDailyRewardList()
        {
            return m_RewardsLoaded ? m_dailyRewardTemplates : null;
        }

        //Get the reward described in the loaded itinerary for the provided step
        static public DailyRewardTemplate GetRewardForStep(int step)
        {
            if (m_dailyRewardTemplates == null)
            {
                return null;
            }

            for (int cnt = 0; cnt < m_dailyRewardTemplates.Count; cnt++)
            {
                if (m_dailyRewardTemplates[cnt].StepID == step)
                {
                    return m_dailyRewardTemplates[cnt];
                }
            }

            return null;
        }

        internal static void ClearDailyRewardTemplates()
        {
            m_dailyRewardTemplates.Clear();
            m_RewardsLoaded = false;
        }

    }
}
