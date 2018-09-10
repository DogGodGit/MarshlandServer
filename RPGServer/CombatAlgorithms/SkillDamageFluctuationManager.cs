using System.Collections.Generic;

namespace MainServer.CombatAlgorithms
{
    interface ISkillDamageFluctuationManager
    {
        void SetUpSkillDamageFlucations();
        float GetSkillDamageFlucationForClass(CLASS_TYPE classType);
    }

    class SkillDamageFluctuationManager : ISkillDamageFluctuationManager
    {
        private IDictionary<CLASS_TYPE, float> m_skillDamageFlucations = new Dictionary<CLASS_TYPE, float>(); 
        private readonly ISkillDamageFluctuationDatabase m_skillDamageFluctuationDatabase;

        public SkillDamageFluctuationManager(ISkillDamageFluctuationDatabase skillDamageFluctuationDatabase)
        {
            m_skillDamageFluctuationDatabase = skillDamageFluctuationDatabase;
            SetUpSkillDamageFlucations();
        }

        public void SetUpSkillDamageFlucations()
        {
            m_skillDamageFlucations = m_skillDamageFluctuationDatabase.SetUpSkillDamageFluctuations();
        }

        public float GetSkillDamageFlucationForClass(CLASS_TYPE classType)
        {
            var skillDamageFluctuation = m_skillDamageFlucations.ContainsKey(classType) ? m_skillDamageFlucations[classType] : 0.5f;
            if (Program.m_LogDamage)
            {
                Program.Display("Getting Skill damage fluctuation for class type: " + classType + " returning: " + skillDamageFluctuation);
            }
            return skillDamageFluctuation;
        }
    }
}
