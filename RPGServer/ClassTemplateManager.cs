using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MainServer
{
    public enum CLASS_TYPE
    {
        UNDEFINED = 0,
        WARRIOR = 1,
        DRUID = 2,
        MAGE = 3,
        RANGER = 4,
        ROGUE = 5
    };
    class ClassTemplate
    {
        public ClassTemplate(CLASS_TYPE classType, string ability_list, string starting_equipment,string starting_skills)
        {
            m_classType = classType;
            m_ability_list = ability_list;
            m_starting_equipment = starting_equipment;
            m_starting_Skills = starting_skills;
        }
        public CLASS_TYPE m_classType;
        public string m_ability_list;
        public string m_starting_equipment;
        public string m_starting_Skills;
    }
    static class ClassTemplateManager
    {
        static List<ClassTemplate> m_ClassTemplates=new List<ClassTemplate>();

        internal static void Setup(Database m_db)
        {
            SqlQuery query = new SqlQuery(m_db, "select * from class order by class_id");
            while (query.Read())
            {
                int class_id = query.GetInt32("class_id");
                string ability_list = query.GetString("starting_abilities");
                string equipment_list = query.GetString("starting_equipment");
                string starting_skills = query.GetString("starting_skills");
                ClassTemplate newTemplate = new ClassTemplate((CLASS_TYPE)class_id,ability_list,equipment_list,starting_skills);
                m_ClassTemplates.Add(newTemplate);
            }
            query.Close();
        }
        internal static ClassTemplate getClassTemplate(CLASS_TYPE class_Type)
        {
            for (int i = 0; i < m_ClassTemplates.Count; i++)
            {
                if (class_Type == m_ClassTemplates[i].m_classType)
                {
                    return m_ClassTemplates[i];
                }
            }
            return null;
        }
    }
}
