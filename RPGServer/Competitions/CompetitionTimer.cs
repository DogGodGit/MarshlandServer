using System;
using System.Timers;

namespace MainServer.Competitions
{
    public enum NextAnnouncementMinutes { START = 0, THRITY = 30, FIFTEEN = 15, FIVE = 5, FOUR = 4, THREE = 3, TWO = 2, ONE = 1 };

    public class CompetitionTimer
    {
        #region Variables

        private const string ANNOUNCEMENT_MESSAGE = "The Competition: {0} will end in {1} minute{2}!";

        private TimeSpan THRITY_MINUTES  = TimeSpan.FromMinutes(30);
        private TimeSpan FIFTEEN_MINUTES = TimeSpan.FromMinutes(15);
        private TimeSpan TEN_MINUTES     = TimeSpan.FromMinutes(10);
        private TimeSpan FIVE_MINUTES    = TimeSpan.FromMinutes(5);
        private TimeSpan FOUR_MINUTES    = TimeSpan.FromMinutes(4);
        private TimeSpan THREE_MINUTES   = TimeSpan.FromMinutes(3);
        private TimeSpan TWO_MINUTES     = TimeSpan.FromMinutes(2);
        private TimeSpan ONE_MINUTE      = TimeSpan.FromMinutes(1);

        private CompetitionManager m_competitionManager;

        private Competition m_competition;

        private NextAnnouncementMinutes m_nextAnnouncement;

        private Timer m_timer;

        #endregion

        public CompetitionTimer(Competition competition, CompetitionManager competitioManager)
        {
            m_competition        = competition;
            m_competitionManager = competitioManager;
        }

        public void YetToStart(TimeSpan timeTillStart)
        {
            m_timer             = new Timer(timeTillStart.TotalMilliseconds);
            m_nextAnnouncement  = NextAnnouncementMinutes.START;
            m_timer.Elapsed    += TimerEvent;
            m_timer.AutoReset   = false;
            m_timer.Enabled     = true;
        }

        public void Start(TimeSpan timeTillEnd)
        {
            TimeSpan diff      = GetTimerDiff(timeTillEnd);
            m_timer            = new Timer(diff.TotalMilliseconds);
            m_timer.Elapsed   += TimerEvent;
            m_timer.AutoReset  = false;
            m_timer.Enabled    = true;
        }

        /// <summary>
        /// Timer event delegate which tells the Competition Manager to end this competition
        /// </summary>
        private void EndEvent(Object source, ElapsedEventArgs e)
        {
            m_timer.Elapsed -= EndEvent;
            m_competitionManager.EndCompetition(m_competition);
        }

        /// <summary>
        /// Timer event delegate which sends the correctly formatted server message and sets up the timer for the next required message
        /// </summary>
        private void TimerEvent(Object source, ElapsedEventArgs e)
        {
            string message     = string.Empty;
            bool   sendMessage = true;

            switch (m_nextAnnouncement)
            {
                case NextAnnouncementMinutes.START:
                {
                    m_competitionManager.StartCompetition(m_competition);
                    sendMessage = false;
                    break;
                }
                case NextAnnouncementMinutes.THRITY:
                {
                    message            = SetupNextAnnouncement(FIFTEEN_MINUTES);
                    m_nextAnnouncement = NextAnnouncementMinutes.FIFTEEN;
                    break;
                }
                case NextAnnouncementMinutes.FIFTEEN:
                {
                    message            = SetupNextAnnouncement(TEN_MINUTES);
                    m_nextAnnouncement = NextAnnouncementMinutes.FIVE;
                    break;
                }
                case NextAnnouncementMinutes.FIVE:
                {
                    message            = SetupNextAnnouncement(ONE_MINUTE);
                    m_nextAnnouncement = NextAnnouncementMinutes.FOUR;
                    break;
                }
                case NextAnnouncementMinutes.FOUR:
                {
                    message            = SetupNextAnnouncement(ONE_MINUTE);
                    m_nextAnnouncement = NextAnnouncementMinutes.THREE;
                    break;
                }
                case NextAnnouncementMinutes.THREE:
                {
                    message            = SetupNextAnnouncement(ONE_MINUTE);
                    m_nextAnnouncement = NextAnnouncementMinutes.TWO;
                    break;
                }
                case NextAnnouncementMinutes.TWO:
                {
                    message            = SetupNextAnnouncement(ONE_MINUTE);
                    m_nextAnnouncement = NextAnnouncementMinutes.ONE;
                    break;
                }
                case NextAnnouncementMinutes.ONE:
                {
                    message = SetupNextAnnouncement(ONE_MINUTE);
                    break;
                }
            }

            if (sendMessage)
            {
                Program.sendSystemMessage(message, false, false);
            }
        }

        private string SetupNextAnnouncement(TimeSpan interval)
        {
            string message = String.Format(ANNOUNCEMENT_MESSAGE, m_competition.Name, ((int)m_nextAnnouncement).ToString(), m_nextAnnouncement == NextAnnouncementMinutes.ONE ? String.Empty : "s");

            if (m_nextAnnouncement == NextAnnouncementMinutes.ONE)
            {
                m_timer.Elapsed -= TimerEvent;
                m_timer.Elapsed += EndEvent;
            }

            m_timer.Interval = interval.TotalMilliseconds;
            m_timer.Start();

            return message;
        }

        /// <summary>
        /// Calculates what the next required server message is, sets the next announcement state and returns the time till that event
        /// </summary>
        /// <param name="timeTillEnd"> Time that the competition ends </param>
        /// <returns> The time till the next server message event </returns>
        private TimeSpan GetTimerDiff(TimeSpan timeTillEnd)
        {
            if (timeTillEnd > THRITY_MINUTES)
            {
                m_nextAnnouncement = NextAnnouncementMinutes.THRITY;
                return timeTillEnd - THRITY_MINUTES;
            }
            else if (timeTillEnd > FIFTEEN_MINUTES)
            {
                m_nextAnnouncement = NextAnnouncementMinutes.FIFTEEN;
                return timeTillEnd - FIFTEEN_MINUTES;
            }
            else if (timeTillEnd > FIVE_MINUTES)
            {
                m_nextAnnouncement = NextAnnouncementMinutes.FIVE;
                return timeTillEnd - FIVE_MINUTES;
            }
            else if (timeTillEnd > FOUR_MINUTES)
            {
                m_nextAnnouncement = NextAnnouncementMinutes.FOUR;
                return timeTillEnd - FOUR_MINUTES;
            }
            else if (timeTillEnd > THREE_MINUTES)
            {
                m_nextAnnouncement = NextAnnouncementMinutes.THREE;
                return timeTillEnd - THREE_MINUTES;
            }
            else if (timeTillEnd > TWO_MINUTES)
            {
                m_nextAnnouncement = NextAnnouncementMinutes.TWO;
                return timeTillEnd - TWO_MINUTES;
            }
            else
            {
                m_nextAnnouncement = NextAnnouncementMinutes.ONE;
                return timeTillEnd - ONE_MINUTE;
            }
        }
    }
}
