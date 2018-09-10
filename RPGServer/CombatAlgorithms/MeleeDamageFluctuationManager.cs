using System;
using System.Collections.Generic;

namespace MainServer.CombatAlgorithms
{
    interface IMeleeDamageFluctuationManager
    {
        void SetUpMeleeDamageFlucations();
        float GetMeleeDamageFlucationForClass(CLASS_TYPE classType);
    }

    class MeleeDamageFluctuationManager : IMeleeDamageFluctuationManager
    {
        private IDictionary<CLASS_TYPE, float> m_meleeDamageFlucations = new Dictionary<CLASS_TYPE, float>();
        private readonly IMeleeDamageFluctuationDatabase m_meleeDamageFlucationsDatabase;

        public MeleeDamageFluctuationManager(IMeleeDamageFluctuationDatabase meleeDamageFlucationsDatabase)
        {
            m_meleeDamageFlucationsDatabase = meleeDamageFlucationsDatabase;
            SetUpMeleeDamageFlucations();
        }

        public void SetUpMeleeDamageFlucations()
        {
            m_meleeDamageFlucations = m_meleeDamageFlucationsDatabase.SetUpMeleeDamageFluctuation();
        }

        public float GetMeleeDamageFlucationForClass(CLASS_TYPE classType)
        {           
            float meleeDamageFluctuation = m_meleeDamageFlucations.ContainsKey(classType) ? m_meleeDamageFlucations[classType] : 0.5f;
            if (Program.m_LogDamage)
            {
                Program.Display("Getting Melee damage fluctuation for class type: " + classType + " returning: " + meleeDamageFluctuation);
            }
            return meleeDamageFluctuation;
        }
    }
}
