using System.Collections.Generic;
using MainServer.Localise;

namespace MainServer
{
    class CombatAITemplate
    {
        internal enum COMBAT_AI_TYPES
        {
            NONE = 0, //default- should not be used
            AGGRESSIVE = 1, // moves into attack range
            MAGE_TYPE = 2, //moves into skill range, will hit players if they come within attack range
            INANIMATE=3//this is an acting as an object so must not react to damage or make decisions

        }

        COMBAT_AI_TYPES m_aiType = COMBAT_AI_TYPES.NONE;
        /// <summary>
        /// percent Health A mob must reach before being considered as needing healing
        /// 0-100%
        /// </summary>
        int m_healingThreshold = 0;

        int m_otherHealingThreshold = 0;

        int m_templateID = -1;

        List<CombatDecisions> m_combatDecisions= new List<CombatDecisions>();
        List<CAI_Script> m_scripts = new List<CAI_Script>();
        List<int> m_skillSetIDs = new List<int>();

        internal List<CAI_Script> Scripts
        {
            get { return m_scripts; }
        }
        internal List<int> SkillSetIDs
        {
            get { return m_skillSetIDs; }
        }
        internal COMBAT_AI_TYPES AiType
        {
            get { return m_aiType; }
        }
        /// <summary>
        /// percent Health A mob must reach before being considered as needing healing
        /// 0-100%
        /// </summary>
        internal int HealingThreshold
        {
            get { return m_healingThreshold; }
        }

        internal int CombatAIID
        {
            get { return m_templateID; }
        }
        internal List<CombatDecisions> CombatDecisionsList
        {
            get { return m_combatDecisions; }
        }
        /// <summary>
        /// Query from the combat ai manager, one for each template
        /// </summary>
        /// <param name="query"></param>
        /// <param name="db"></param>
        internal CombatAITemplate(Database db, SqlQuery query)
        {
           /* m_templateID = -1;
            m_otherHealingThreshold = 50;

            m_healingThreshold = 30;
            m_aiType = COMBAT_AI_TYPES.AGGRESSIVE;

            m_combatDecisions.Add(new CombatDecisions(CombatDecisions.DECISION_TYPES.DT_ATTACK, 500));
            m_combatDecisions.Add(new CombatDecisions(CombatDecisions.DECISION_TYPES.DT_HEAL_SELF, 500));
            m_combatDecisions.Add(new CombatDecisions(CombatDecisions.DECISION_TYPES.DT_ATTACK_SKILL, 500));*/
            m_templateID = query.GetInt32("ai_template_id");
            m_otherHealingThreshold = query.GetInt32("other_healing_threshold");
            
            m_healingThreshold = query.GetInt32("self_healing_threshold");
            m_aiType = (COMBAT_AI_TYPES)query.GetInt32("ai_type");

            SqlQuery decisionQuery = new SqlQuery(db, "select * from combat_decisions where ai_template_id=" + m_templateID);
            while (decisionQuery.Read())
            {
                int decisionType = decisionQuery.GetInt32("decision_type");
                int probability = decisionQuery.GetInt32("probability");
                m_combatDecisions.Add(new CombatDecisions( (CombatDecisions.DECISION_TYPES)decisionType, probability));
            }
            decisionQuery.Close();

            SqlQuery skillSetQuery = new SqlQuery(db, "select * from combat_ai_skill_sets where ai_template_id=" + m_templateID);
            while (skillSetQuery.Read())
            {
                int skillSetID = skillSetQuery.GetInt32("ai_skill_set_id");
                
                m_skillSetIDs.Add(skillSetID);
            }
            skillSetQuery.Close();
            //combat_ai_scripts

            SqlQuery scriptListQuery = new SqlQuery(db, "select * from combat_ai_scripts where ai_template_id=" + m_templateID);
            while (scriptListQuery.Read())
            {
                int scriptID = scriptListQuery.GetInt32("ai_script_id");
                SqlQuery scriptQuery = new SqlQuery(db, "select * from ai_scripts where ai_script_id=" + scriptID);
                while (scriptQuery.Read())
                {
                    string scriptString = scriptQuery.GetString("ai_script_string");
                    string activationString = scriptQuery.GetString("activation_string");
                    int priority = scriptQuery.GetInt32("priority");

                    CAI_Script newScript = new CAI_Script(scriptID, m_templateID, scriptString, activationString, priority);
                    m_scripts.Add(newScript);
                }
                scriptQuery.Close();
            }
            scriptListQuery.Close();
        }

    }

    static class CombatAITemplateManager
    {
        static CombatAITemplate[] m_combatAITemplates;
		
		// #localisation
		static int textDBIndex = 0;

		static public void FillTemplate(Database db)
        {
            uint numberOfTemplates = 0;

            
            //get how many items are in the database
            SqlQuery query = new SqlQuery(db, "select count(*) as numtemplates from combat_ai_templates");
            if (query.HasRows)
            {
                query.Read();
                numberOfTemplates = query.GetUInt32("numtemplates");
            }
            query.Close();

            //create the array of items
            m_combatAITemplates = new CombatAITemplate[numberOfTemplates];
            //copy each item into the array

            int currentTemplate = 0;

            query = new SqlQuery(db, "select * from combat_ai_templates");
            if (query.HasRows)
            {
                while ((query.Read()) && (currentTemplate < numberOfTemplates))
                {

                    m_combatAITemplates[currentTemplate] = new CombatAITemplate(db, query);
                    currentTemplate++;
                }
            }

            query.Close();

			// Get textNameDB index.
			textDBIndex = Localiser.GetTextDBIndex("ai_scripts");
		}

		static internal string GetLocaliseActivationString(Player player, int scriptID)
		{
			return Localiser.GetString(textDBIndex, player, scriptID);
		}

		static public CombatAITemplate GetItemForID(int ID)
        {
            if (m_combatAITemplates == null)
            {
                return null;
            }
            for (int currentTemplate = 0; currentTemplate < m_combatAITemplates.Length; currentTemplate++)
            {
                if (m_combatAITemplates[currentTemplate].CombatAIID == ID)
                {
                    return m_combatAITemplates[currentTemplate];
                }
            }
            return null;
        }
    }
}
