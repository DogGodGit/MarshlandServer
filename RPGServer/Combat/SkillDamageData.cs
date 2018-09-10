using System.Collections.Generic;

namespace MainServer.Combat
{
    class SkillDamageData
    {
        CombatDamageMessageData m_targetDamage = null;
        List<CombatDamageMessageData> m_aoeDamages = null;

        EntitySkill m_theSkill = null;
        internal CombatDamageMessageData TargetDamage
        {
            get { return m_targetDamage; }
        }
        internal List<CombatDamageMessageData> AOEDamages
        {
            get { return m_aoeDamages; }
        }
        /// <summary>
        /// The Skill that did the damage
        /// </summary>
        internal EntitySkill TheSkill
        {
            get { return m_theSkill; }
        }
        internal SkillDamageData(EntitySkill theSkill, CombatDamageMessageData targetDamage, List<CombatDamageMessageData> aoeDamage)
        {
            m_theSkill = theSkill;
            m_targetDamage = targetDamage;
            m_aoeDamages = aoeDamage;
        }
        internal void CancelSkillDamage(CombatManager theCombatManager)
        {
            if (m_targetDamage != null)
            {
                theCombatManager.RemoveDamageFromList(m_targetDamage, true);
            }
            for (int damageIndex = 0; damageIndex < m_aoeDamages.Count; damageIndex++)
            {
                theCombatManager.RemoveDamageFromList(m_aoeDamages[damageIndex], true);
            }
        }
    }
}