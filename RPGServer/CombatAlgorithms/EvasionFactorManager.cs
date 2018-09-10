using System.Collections.Generic;

namespace MainServer.CombatAlgorithms
{
    interface IEvasionFactorManager
    {
        void SetUpEvasionFactors();
        float GetEvasionFactorForClass(CLASS_TYPE classType);
    }

    class EvasionFactorManager : IEvasionFactorManager
    {
        private IDictionary<CLASS_TYPE, float> m_evasionFactors = new Dictionary<CLASS_TYPE, float>();
        private readonly IEvasionFactorDatabase m_evasionFactorDatabase;

        public EvasionFactorManager(IEvasionFactorDatabase evasionFactorDatabase)
        {
            m_evasionFactorDatabase = evasionFactorDatabase;
            SetUpEvasionFactors();
        }

        public void SetUpEvasionFactors()
        {
            m_evasionFactors = m_evasionFactorDatabase.SetUpEvasionFactors();
        }

        public float GetEvasionFactorForClass(CLASS_TYPE classType)
        {
            return m_evasionFactors.ContainsKey(classType) ? m_evasionFactors[classType] : 1.0f;
        }
    }
}