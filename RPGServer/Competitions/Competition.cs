using System;

namespace MainServer.Competitions
{
    public enum CompetitionType
    {
        MOB_KILLS,
        LOOT_ITEM,
        QUESTS_COMPLETED,
        COOKED_ITEM,
        PICKUP_ITEM
    }

    public class Competition
    {
        public int             ID            { get; private set; }
        public string          Name          { get; private set; }
        public DateTime        StartDate     { get; private set; }
        public DateTime        EndDate       { get; private set; }
        public CompetitionType Type          { get; private set; }
        public int             TemplateID    { get; private set; }
        public int             LeaderboardID { get; private set; }

        public CompetitionTimer CompetitionTimer { get; set; }

        public Competition(SqlQuery query)
        {
            ID            = query.GetInt32("id");
            Name          = query.GetString("name");
            StartDate     = query.GetDateTime("start_date");
            EndDate       = query.GetDateTime("end_date");
            Type          = (CompetitionType)query.GetInt32("type");
            TemplateID    = query.GetInt32("template_id");
            LeaderboardID = query.GetInt32("leaderboard_id");
        }
    }
}