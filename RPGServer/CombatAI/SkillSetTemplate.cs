using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MainServer
{
    /// <summary>
    /// A mob Specific Skill Set that holds the sort of ai set it belongs to
    /// and its weight
    /// </summary>
    class MobSkillSetTemplate
    {
        /// <summary>
        /// The Template containing all the weights for this skill set
        /// </summary>
        SkillSetTemplate m_skillSet=null;

        /// <summary>
        /// The AI Skill Set ID That this will behave as
        /// </summary>
        int m_aiSkillSetID=-1;
        /// <summary>
        /// The Weight of this skill Set
        /// </summary>
        int m_weight=0;

        /// <summary>
        /// The Template containing all the weights for this skill set
        /// </summary>
        internal SkillSetTemplate SkillSet
        {
            get { return m_skillSet; }
        }
        /// <summary>
        /// The AI Skill Set ID That this will behave as
        /// </summary>
        internal int AISkillSetID
        {
            get { return m_aiSkillSetID; }
        }
        /// <summary>
        /// The Weight of this skill Set
        /// </summary>
        internal int Weight
        {
            get { return m_weight; }
        }

        internal MobSkillSetTemplate(SkillSetTemplate skillSet, int aiSkillSetID,int weight)
        {
            m_skillSet = skillSet;
            m_aiSkillSetID = aiSkillSetID;
            m_weight = weight;
        }

    }
    
    /// <summary>
    /// The Generic Skill Set that can be shared between all the mobs
    /// </summary>
    class SkillSetTemplate
    {
        int m_skillSetID=-1;
        string m_skillSetName = "";
        List<SkillSetTemplateWeight> m_skillWeights = new List<SkillSetTemplateWeight>();

        internal int SkillSetID
        {
            get { return m_skillSetID; }
        }
        internal List<SkillSetTemplateWeight> SkillWeights
        {
            get { return m_skillWeights; }
        }

        internal SkillSetTemplate(int skillSetID, string skillSetName, Database db)
        {
            m_skillSetID = skillSetID;
            m_skillSetName = skillSetName;

            SqlQuery  query = new SqlQuery(db, "select * from skill_set_weights where skill_set_id = "+skillSetID);
            if (query.HasRows)
            {
                while (query.Read())
                {
                    SKILL_TYPE skillID = (SKILL_TYPE)query.GetInt32("skill_id");
                    int weight = query.GetInt32("weight");
                    SkillSetTemplateWeight newWeight = new SkillSetTemplateWeight(skillID, weight);

                    m_skillWeights.Add(newWeight);
                }
            }
            query.Close();
        }

    }

    class SkillSetTemplateWeight
    {
        SKILL_TYPE m_skillID = SKILL_TYPE.NONE;
        int m_weight = 0;

        internal SKILL_TYPE SkillID
        {
            get { return m_skillID; }
        }
        internal int Weight
        {
            get { return m_weight; }
        }
        internal SkillSetTemplateWeight(SKILL_TYPE skillID, int weight)
        {
            m_skillID = skillID;
            m_weight = weight;
        }
    };

    static class SkillSetTemplateManager
    {
        static SkillSetTemplate[] m_skillSets=null;

        static SkillSetTemplateManager()
        {

        }
        static public void FillTemplate(Database db)
        {
            uint numberOfTemplates = 0;
            //get how many items are in the database
            SqlQuery query = new SqlQuery(db, "select count(*) as num_skill_sets from skill_sets");
            if (query.HasRows)
            {
                query.Read();
                numberOfTemplates = query.GetUInt32("num_skill_sets");
            }
            query.Close();

            //create the array of items
            m_skillSets = new SkillSetTemplate[numberOfTemplates];
            //copy each item into the array

            /* for (int currentTemplate = 0; currentTemplate < numberOfTemplates; currentTemplate++)
             {

             }*/
            int currentTemplate = 0;

            query = new SqlQuery(db, "select * from skill_sets");
            if (query.HasRows)
            {
                while ((query.Read()) && (currentTemplate < numberOfTemplates))
                {
                    //get the id
                    int skillSetID = query.GetInt32("skill_set_id");
                    //get the name
                    string skillSetName = query.GetString("skill_set_name");
                    //create the new skill set that will then load itself from the database
                    SkillSetTemplate skillSetTemplate = new SkillSetTemplate(skillSetID, skillSetName, db);
                    m_skillSets[currentTemplate] = skillSetTemplate;
                    currentTemplate++;
                }
            }

            query.Close();
        }

        static public SkillSetTemplate GetSkillSetForID(int ID)
        {
            if (m_skillSets == null)
            {
                return null;
            }
            for (int currentTemplate = 0; currentTemplate < m_skillSets.Length; currentTemplate++)
            {
                if (m_skillSets[currentTemplate].SkillSetID == ID)
                {
                    return m_skillSets[currentTemplate];
                }
            }
            return null;
        }

    }
}
