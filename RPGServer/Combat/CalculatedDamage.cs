namespace MainServer.Combat
{
    class CalculatedDamage
    {
        internal int m_preLvlReductionDamage = 0;
        internal int m_calculatedDamage = 0;
        public CalculatedDamage(int calculatedDamage, int preLvlReductionDamage)
        {
            m_preLvlReductionDamage = preLvlReductionDamage;
            m_calculatedDamage = calculatedDamage;
        }
        internal int GetAmendedOriginalDamage(int newCalcDamage)
        {
            int amendedDamage = newCalcDamage;
            if (m_calculatedDamage != m_preLvlReductionDamage && m_calculatedDamage != 0)
            {
                amendedDamage =( newCalcDamage * m_preLvlReductionDamage)/m_calculatedDamage;
            }

            return amendedDamage;
        }
    }
}