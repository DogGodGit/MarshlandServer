using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MainServer
{
    enum RACE_TYPE
    {
        UNDEFINED = 0,
        HIGHLANDER = 1
    };
    class RaceTemplate
    {
        public RaceTemplate(SqlQuery query)
        {

            m_raceType = (RACE_TYPE)query.GetInt32("race_id");
            m_strength_modifier = query.GetFloat("strength_multiplier");
            m_dexterity_modifier = query.GetFloat("dexterity_multiplier");
            m_focus_modifier = query.GetFloat("focus_multiplier");
            m_vitality_modifier = query.GetFloat("vitality_multiplier");
            m_starting_strength = query.GetInt32("starting_strength");
            m_starting_dexterity = query.GetInt32("starting_dexterity");
            m_starting_focus = query.GetInt32("starting_focus");
            m_starting_vitality = query.GetInt32("starting_vitality");

        }
        public RACE_TYPE m_raceType;
        public float m_strength_modifier;
        public float m_dexterity_modifier;
        public float m_focus_modifier;
        public float m_vitality_modifier;
        public int m_starting_strength;
        public int m_starting_dexterity;
        public int m_starting_focus;
        public int m_starting_vitality;
    }
    static class RaceTemplateManager
    {
        static List<RaceTemplate> m_RaceTemplates = new List<RaceTemplate>();

        internal static void Setup(Database m_db)
        {
            SqlQuery query = new SqlQuery(m_db, "select * from race order by race_id");
            while (query.Read())
            {
                RaceTemplate newTemplate = new RaceTemplate(query);
                m_RaceTemplates.Add(newTemplate);
            }
            query.Close();

        }
        internal static RaceTemplate getRaceTemplate(RACE_TYPE race_Type)
        {
            for (int i = 0; i < m_RaceTemplates.Count; i++)
            {
                if (race_Type == m_RaceTemplates[i].m_raceType)
                {
                    return m_RaceTemplates[i];
                }
            }
            return null;
        }
    }
}
