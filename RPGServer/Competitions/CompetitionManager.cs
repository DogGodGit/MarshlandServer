using System;
using System.Collections.Generic;
using System.Timers;

namespace MainServer.Competitions
{
    public class CompetitionManager
    {
        private Database          m_datadb;
        private List<Competition> m_activeCompetitions;
        private List<Competition> m_pendingCompetitions;

        private const string SELECT_ACTIVE_COMPETITIONS    = "select * from competitions where '{0}' between start_date and end_date";
        private const string SELECT_POTENTIAL_COMPETITIONS = "select * from competitions where '{0}' < start_date";

        private const string COMPETITION_STARTED = "The Competition: {0} has begun!";
        private const string COMPETITION_ENDED   = "The Competition: {0} has finished!";

        private Object m_activeCompetitionsLock = new Object();

        /// <summary>
        /// Reads from 'competitions' table on hubglobaldb to determine if any competitions are active
        /// Competitions will use Leaderboard RankingType 21, 22 & 23 - called 'LeaderboardID' (***only 21 just now***)
        /// If a second competition uses the same LeaderboardID it will be rejected and not added
        /// Sets up a competition timer to manage the competitions start/ending and end messages
        /// </summary>
        /// <param name="datadb"> The hubglobaldb </param>
        public CompetitionManager(Database datadb)
        {
            m_datadb              = datadb;
            m_activeCompetitions  = new List<Competition>();
            m_pendingCompetitions = new List<Competition>();

            SqlQuery query = new SqlQuery(m_datadb, String.Format(SELECT_ACTIVE_COMPETITIONS, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff")));
            while (query.Read())
            {
                Competition newCompetition = new Competition(query);

                if (newCompetition != null)
                    TryAddActiveCompetition(newCompetition);
            }
            query.Close();

            query = new SqlQuery(m_datadb, String.Format(SELECT_POTENTIAL_COMPETITIONS, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff")));
            while (query.Read())
            {
                Competition pendingCompetition = new Competition(query);

                if (pendingCompetition == null)
                    continue;

                if (CheckCompetitionIds(m_pendingCompetitions, pendingCompetition.ID))
                    continue;

                pendingCompetition.CompetitionTimer = new CompetitionTimer(pendingCompetition, this);
                pendingCompetition.CompetitionTimer.YetToStart(pendingCompetition.StartDate - DateTime.UtcNow);
                m_pendingCompetitions.Add(pendingCompetition);
                Program.Display(String.Format("Competition ID: {0} has yet to start!", pendingCompetition.ID));
            }
            query.Close();
        }

        #region Public Functions

        public void StartCompetition(Competition competitionToStart)
        {
            TryAddActiveCompetition(competitionToStart);
            m_pendingCompetitions.Remove(competitionToStart);
        }

        public void EndCompetition(Competition competitionToEnd)
        {
            Program.sendSystemMessage(String.Format(COMPETITION_ENDED, competitionToEnd.Name), true, false);
            Program.Display(String.Format("Competition ID: {0} has ENDED!", competitionToEnd.ID));
            lock (m_activeCompetitionsLock)
                m_activeCompetitions.Remove(competitionToEnd);
        }

        #endregion

        #region Private Functions

        private void TryAddActiveCompetition(Competition competitionToStart)
        {
            if (CheckCompetitionIds(m_activeCompetitions, competitionToStart.ID))
                return;

            if (ClashingLeaderboardIDs(m_activeCompetitions, competitionToStart.LeaderboardID) == false)
            {
                TimeSpan timeRemaining = competitionToStart.EndDate - DateTime.UtcNow;
                competitionToStart.CompetitionTimer = new CompetitionTimer(competitionToStart, this);
                competitionToStart.CompetitionTimer.Start(timeRemaining);

                lock (m_activeCompetitionsLock)
                    m_activeCompetitions.Add(competitionToStart);

                Program.sendSystemMessage(String.Format(COMPETITION_STARTED, competitionToStart.Name), true, false);
                Program.Display(String.Format("Competition ID: {0} has STARTED!", competitionToStart.ID));
            }
            else
            {
                Program.Display(String.Format("Competition Error! - Active Competition ID: {0} - leaderboard id is already in use! - This competition will NOT be added!", competitionToStart.ID));
            }
        }

        private bool CheckCompetitionIds(List<Competition> list, int id)
        {
            foreach (Competition competition in list)
            {
                if (competition.ID == id)
                    return true;
            }

            return false;
        }

        private bool ClashingLeaderboardIDs(List<Competition> list, int leaderboardID)
        {
            foreach (Competition competition in list)
            {
                if (competition.LeaderboardID == leaderboardID)
                    return true;
            }

            return false;
        }
        
        #endregion

        /// <summary>
        /// Call to check if a incoming event should update an existing competition
        /// We can bail if there are no competitions running or the passed character is null
        /// Then we use both the CompetitionType and targetId to determine if a increaseRanking call is required
        /// </summary>
        /// <param name="character"> Character ref to allow access to increaseRanking() </param>
        /// <param name="type"> CompetitionType clears the general action - a mob kill, item looted or quest completed etc </param>
        /// <param name="targetId"> targetId then clears that specific mob, item or quest id is correct </param>
        internal void UpdateCompetition(Character character, CompetitionType type, int targetId)
        {
            if (m_activeCompetitions.Count < 1)
                return;

            if (character == null)
                return;

            foreach (Competition competition in m_activeCompetitions)
            {
                if (competition.Type == type && competition.TemplateID == targetId)
                {
                    Program.Display(String.Format("Added for competition type: {0}, target id: {1}", competition.Type.ToString(), targetId));
                    character.increaseRanking((RankingsManager.RANKING_TYPE)competition.LeaderboardID, 1, false);
                }
            }
        }
    }
}
