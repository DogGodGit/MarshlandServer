using System;
using System.Collections.Generic;
using System.Linq;
using Lidgren.Network;

namespace MainServer
{
    class Ranking
    {
        public RankingsManager.RANKING_TYPE m_ranking_type;
        public double m_value;
        public bool m_confirmed;
        public DateTime m_updateSent=DateTime.MinValue;
        public bool m_updated;

        public Ranking(RankingsManager.RANKING_TYPE rankingType, double value, bool confirmed)
        {
            m_ranking_type = rankingType;
            m_value = value;
            m_confirmed = confirmed;
            m_updated = false;
        }
    }

    class RankingsManager
    {
        public enum RANKING_MANAGER_TYPE
        {
            CHARACTER_RANKINGS=0,
            ACCOUNT_RANKINGS=1
        }

        public enum RANKING_TYPE
        {
            PLAYER_LEVEL=0,
            LARGEST_MELEE_HIT=1,
            LARGEST_SKILLS_HIT=2,
            LARGEST_HEAL=3,
            QUEST_COMPLETED=4,
            ENEMIES_KILLED=5,
            NUMBER_OF_DEATHS=6,
            STAR_1=8,
            STAR_2=9,
            STAR_3=10,
            STAR_4=11,
            STAR_5=12,
            STAR_6=13,
            PVP_KILLS=14,
            PVP_DEATHS=15,
            PVP_RANKING=16,
            PVE_KILLS_V_DEATHS=17,
            PVP_KILLS_V_DEATHS=18,
            PLAYER_LEVEL_FISHING=19,
            PLAYER_LEVEL_COOKING=20,
            COMPETITION=21
        }

        #region REMOVED

        /*readonly string[] leaderboardNames =
        {
            "rpg.playerlevel",//PLAYER_LEVEL=0,
            "rpg.meleehit",//LARGEST_MELEE_HIT=1,
            "rpg.skillhit",//LARGEST_SKILLS_HIT=2,
            "rpg.heal",//LARGEST_HEAL=3,
            "rpg.questscomplete",//QUEST_COMPLETED=4,
            "rpg.numberofkills",//ENEMIES_KILLED=5,
            "rpg.numberofdeaths",//NUMBER_OF_DEATHS=6,
            "",
            "rpg.star1",//STAR_1=8,
            "rpg.star2",//STAR_2=9,
            "rpg.star3",//STAR_3=10,
            "rpg.star4",//STAR_4=11,
            "rpg.star5",//STAR_5=12,
            "rpg.star6",//STAR_6=13,
            "rpg.pvpkills",//PVP_KILLS=14
            "rpg.pvpdeaths",//PVP_DEATHS=15
            "rpg.pvprank",//PVP_RANKING=16
            "",//PVE_KILLS_V_DEATHS
            "",//PVP_KILLS_V_DEATHS
            "",//PLAYER_LEVEL_FISHING
            "",//PLAYER_LEVEL_COOKING
            "",//COMPETITION
        };*/

        #endregion

        List<Ranking> m_rankings = new List<Ranking>();
        Database m_db;
        string m_tablename;
        string m_keyfield;
        int m_keyvalue;
        RANKING_MANAGER_TYPE m_managerType;

        public RankingsManager(Database db,RANKING_MANAGER_TYPE managerType,string tablename,string keyfield,int keyvalue)
        {
            m_db=db;
            m_tablename=tablename;
            m_keyfield = keyfield;
            m_keyvalue = keyvalue;
            m_managerType = managerType;
            SqlQuery query = new SqlQuery(db,"select * from " + tablename + " where " + keyfield+"="+keyvalue);
            while (query.Read())
            {
                int ranking_id=query.GetInt32("ranking_id");
                double ranking_value=query.GetDouble("ranking_value");
                bool confirmed = query.GetBoolean("confirmed");
                m_rankings.Add(new Ranking((RANKING_TYPE)ranking_id,ranking_value,confirmed));
            }
        }

        public double increaseStat(RANKING_TYPE rankingType, double increment)
        {
            Ranking ranking = getRanking(rankingType);
            ranking.m_value += increment;
            writeToDatabase(rankingType);
            updateRanking(rankingType);
            return ranking.m_value;
        }

        public bool setStat(RANKING_TYPE rankingType, double value,bool allowDecrease)
        {
            Ranking ranking = getRanking(rankingType);
            if (ranking.m_value < value || allowDecrease)
            {
                ranking.m_value = value;
                writeToDatabase(rankingType);
                updateRanking(rankingType);
                return true;
            }

            return false;
        }

        public double getStat(RANKING_TYPE rankingType)
        {
            return getRanking(rankingType).m_value;
        }

        void writeToDatabase(RANKING_TYPE rankingType)
        {
            Ranking ranking = getRanking(rankingType);
            ranking.m_confirmed = true;
            if (m_managerType == RANKING_MANAGER_TYPE.ACCOUNT_RANKINGS)
            {
                ranking.m_updateSent = DateTime.Now;
                ranking.m_confirmed = false;
            }

            ranking.m_updated = true;
        }

        public void confirmed(RANKING_TYPE rankingType)
        {
            Ranking ranking = getRanking(rankingType);
            ranking.m_confirmed = true;
            ranking.m_updated = true;
        }

        #region REMOVED

        /*public  void sendLeaderBoardUpdate(Character character, RANKING_TYPE rankingType)
        {
            if (leaderboardNames[(int)rankingType].Length > 0)
            {
                NetOutgoingMessage msg = Program.Server.CreateMessage();
                Ranking ranking = getRanking(rankingType);
                msg.WriteVariableUInt32((uint)NetworkCommandType.LeaderBoardUpdate);
                msg.WriteVariableInt32((int)character.m_character_id);
                msg.WriteVariableInt32((int)rankingType);
                msg.Write(leaderboardNames[(int)rankingType]);
                msg.Write((float)ranking.m_value);
                Program.processor.SendMessage(msg, character.m_player.connection, NetDeliveryMethod.ReliableOrdered, NetMessageChannel.NMC_Normal, NetworkCommandType.LeaderBoardUpdate);
            }
        }*/

        #endregion

        public Ranking getRanking(RANKING_TYPE rankingType)
        {
            Ranking ranking = null;
            for (int i = 0; i < m_rankings.Count; i++)
            {
                if (m_rankings[i].m_ranking_type == rankingType)
                {
                    ranking = m_rankings[i];
                    break;
                }
            }
            if (ranking == null)
            {
                ranking = new Ranking(rankingType, 0, true);
                m_rankings.Add(ranking);
            }

            return ranking;
        }

        public void update(Character character)
        {
            for (int i = 0; i < m_rankings.Count; i++)
            {
                Ranking ranking = m_rankings[i];
                if (ranking.m_confirmed == false && (DateTime.Now - ranking.m_updateSent).TotalMinutes > 60)
                {
                    writeToDatabase(ranking.m_ranking_type);
                    updateRanking(ranking.m_ranking_type);
                    //sendLeaderBoardUpdate(character, ranking.m_ranking_type);
                }
            }
        }

        internal void saveRankings()
        {
            string updateString = "";
            for (int i = 0; i < m_rankings.Count(); i++)
            {
                Ranking ranking = m_rankings[i];
                if (ranking.m_updated == true)
                {
                    string confirmed = "0";
                    if (ranking.m_confirmed == true)
                    {
                        confirmed = "1";
                    }

                    updateString = updateString + ",("+m_keyvalue+"," + ((int)ranking.m_ranking_type).ToString() + "," + ranking.m_value + ","+confirmed+")";
                    ranking.m_updated = false;
                }
            }

            if (updateString.Length > 0)
            {
                m_db.runCommandSync("replace into " + m_tablename + " (" + m_keyfield + ",ranking_id,ranking_value,confirmed) values " + updateString.Substring(1));
            }  
        }

        internal void updateRanking(RANKING_TYPE rankingType)
        {
            Ranking ranking = getRanking(rankingType);

            if (ranking != null && ranking.m_updated == true)
            {
                string confirmed = ranking.m_confirmed ? "1" : "0";
                string updateString = string.Format("replace into {0} ({1}, ranking_id, ranking_value, confirmed) values ({2}, {3}, {4}, {5})", m_tablename, m_keyfield, m_keyvalue, (int)ranking.m_ranking_type, ranking.m_value, confirmed);
                m_db.runCommandSync(updateString);
                ranking.m_updated = false;
            }
        }
    }
}