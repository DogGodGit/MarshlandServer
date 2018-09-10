namespace MainServer.Combat
{
    class AOETarget
    {
        CombatEntity m_target = null;
        CombatDamageMessageData m_damage = null;
        internal CombatEntity Target
        {
            get { return m_target; }
        }
        internal CombatDamageMessageData Damage
        {
            get { return m_damage; }
        }
        internal AOETarget(CombatEntity target, CombatDamageMessageData damage)
        {
            m_target = target;
            m_damage = damage;
        }
    }
}