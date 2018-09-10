using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MainServer
{
    class MobSkillWeight
    {
        int m_weight=0;
        SKILL_TYPE m_skillID = SKILL_TYPE.NONE;
        MobSkill m_skill = null;

        internal int Weight
        {
            get { return m_weight; }
        }

        internal MobSkill Skill
        {
            get { return m_skill; }
        }

        internal MobSkillWeight(MobSkill skill, SKILL_TYPE skillID, int weight)
        {
            m_skill = skill;
            m_weight = weight;
            m_skillID = skillID;
        }

    }

    class MobSkillSet
    {
        MobSkillSetTemplate m_skillSetTemplate = null;
        int m_skillSetAIID = -1;
        List<MobSkillWeight> m_setSkills = new List<MobSkillWeight>();

        internal int SkillSetAIID
        {
            get { return m_skillSetAIID; }
        }
        internal List<MobSkillWeight> SetSkills
        {
            get { return m_setSkills; }
        }
        internal MobSkillSetTemplate SkillSetTemplate
        {
            get { return m_skillSetTemplate; }
        }

        internal MobSkillSet(MobSkillSetTemplate skillSetTemplate,int skillSetID)
        {
            m_skillSetTemplate = skillSetTemplate;
            m_skillSetAIID = skillSetID;
        }
        internal static MobSkillSet GetSetFromList(int skillSetID, List<MobSkillSet> theList)
        {
            for (int i = 0; i < theList.Count; i++)
            {
                if(theList[i].SkillSetAIID == skillSetID)
                {
                    return theList[i];
                }
            }
                return null;
        }
    }
}
