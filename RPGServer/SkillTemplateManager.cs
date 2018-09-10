using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MainServer.Localise;

namespace MainServer
{

    static class SkillTemplateManager
    {
		// #localisation
		static int textDBIndex = 0;

		static List<SkillTemplate> m_skills = new List<SkillTemplate>();

        static SkillTemplateManager()
        {

        }
        static public void FillTemplate(Database db)
        {

            SqlQuery query = new SqlQuery(db, "select * from skill_templates order by skill_id");
            if (query.HasRows)
            {
                while (query.Read())
                {

                    m_skills.Add(new SkillTemplate(db, query));
                }
            }

            query.Close();
            ReadSkillLevels(db);
            ReadLootSets(db);

			textDBIndex = Localiser.GetTextDBIndex("skill_templates");
		}

        static private void ReadSkillLevels(Database db)
        {
            int currentSkill = 0;
            SqlQuery subQuery = new SqlQuery(db, "select * from skill_template_levels order by skill_template_id,skill_level,PVP");
            while (subQuery.Read())
            {
                int skill_id = subQuery.GetInt32("skill_template_id");
                SkillTemplateLevel newSkillLevel = new SkillTemplateLevel(db, subQuery);
                while (currentSkill < m_skills.Count && (int)m_skills[currentSkill].m_skillID < skill_id)
                {
                    currentSkill++;
                }
                if (currentSkill < m_skills.Count && (int)m_skills[currentSkill].m_skillID == skill_id)
                {
                    if (subQuery.GetBoolean("PVP"))
                    {
                        m_skills[currentSkill].m_PVPTemplateLevels.Add(newSkillLevel);
                    }
                    else
                    {
                        m_skills[currentSkill].m_templateLevels.Add(newSkillLevel);
                    }
                }
            }
            subQuery.Close();
        }

        static void ReadLootSets(Database db)
        {
            int skillCounter = 0;
            int skillLevelCounter = 0;
            int pvpSkillLevelCounter = 0;

            SqlQuery lootQuery = new SqlQuery(db, "select * from skill_loot_sets order by skill_template_id,skill_level,pvp");
            while (lootQuery.Read())
            {
                int skill_template_id = lootQuery.GetInt32("skill_template_id");
                int skill_level = lootQuery.GetInt32("skill_level");
                int pvp = lootQuery.GetInt32("pvp");
                int lootSetID = lootQuery.GetInt32("loot_set_id");
                int lootSetDrops = lootQuery.GetInt32("num_drops");

                LootSet newLootSet = LootSetManager.getLootSet(lootSetID);
                if (newLootSet != null)
                {
                    LootSetHolder newSetHolder = new LootSetHolder(newLootSet, lootSetDrops);
                    while (skillCounter < m_skills.Count && (int)m_skills[skillCounter].m_skillID < skill_template_id)
                    {
                        skillCounter++;
                        skillLevelCounter = 0;
                        pvpSkillLevelCounter = 0;
                    }
                    if (skillCounter < m_skills.Count && (int)m_skills[skillCounter].m_skillID == skill_template_id)
                    {
                        SkillTemplate skillTemplate = m_skills[skillCounter];
                        if (pvp == 0)
                        {
                            while (skillLevelCounter < skillTemplate.m_templateLevels.Count && skillTemplate.m_templateLevels[skillLevelCounter].m_skill_level < skill_level)
                            {
                                skillLevelCounter++;
                            }
                            if (skillLevelCounter < skillTemplate.m_templateLevels.Count && skillTemplate.m_templateLevels[skillLevelCounter].m_skill_level == skill_level)
                            {
                                skillTemplate.m_templateLevels[skillLevelCounter].m_lootSets.Add(newSetHolder);
                            }
                        }
                        else
                        {
                            while (pvpSkillLevelCounter < skillTemplate.m_templateLevels.Count && skillTemplate.m_templateLevels[pvpSkillLevelCounter].m_skill_level < skill_level)
                            {
                                pvpSkillLevelCounter++;
                            }
                            if (pvpSkillLevelCounter < skillTemplate.m_PVPTemplateLevels.Count && skillTemplate.m_PVPTemplateLevels[pvpSkillLevelCounter].m_skill_level == skill_level)
                            {
                                skillTemplate.m_PVPTemplateLevels[pvpSkillLevelCounter].m_lootSets.Add(newSetHolder);
                            }
                        }
                    }
                }
            }
            lootQuery.Close();
        }

        static public SkillTemplate GetItemForID(SKILL_TYPE ID)
        {
            if (m_skills == null)
            {
                return null;
            }
            for (int currentTemplate = 0; currentTemplate < m_skills.Count; currentTemplate++)
            {
                if (m_skills[currentTemplate].SkillID == ID)
                {
                    return m_skills[currentTemplate];
                }
            }
            return null;
        }


		static internal string GetLocaliseSkillName(Player player, SKILL_TYPE skill_id)
		{
			return Localiser.GetString(textDBIndex, player, (int)skill_id);
		}
	}
}
