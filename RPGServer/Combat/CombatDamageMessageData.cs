namespace MainServer.Combat
{
    internal class CombatDamageMessageData
    {

        CombatManager.DamageMessageType m_messageType;
        int m_damageTaken = 0;
        int m_estimatedDamage = 0;
        int m_sentDamage = 0;
        int m_reaction = -1;
        int m_casterID = -1;
        int m_targetID = -1;
        int m_targetHealth = 0;
        int m_skillID = 0;
        int m_attackType = 0;
        bool m_aggressive = false;
        int m_damageID;
        CombatEntity.LevelType m_gathering = CombatEntity.LevelType.none;
        CombatEntity m_targetLink = null;
        CombatEntity m_casterLink = null;
        double m_applyTime = 0;
        double m_aggroModifier = 1;
        bool m_damageApplied = false;
        int m_critical = 0; 
        /// <summary>
        /// The Time at which the entity is no longer needed for damage to occur
        /// e.g. the arrow has been fired
        /// </summary>
        double m_actionCompleteTime = 0;

        public CombatManager.DamageMessageType MessageType
        {
            set { m_messageType = value; }
            get { return m_messageType; }
        }
        public int SentDamage
        {
            set { m_sentDamage = value; }
            get { return m_sentDamage; }
        }
        public int EstimatedDamage
        {
            set { m_estimatedDamage = value; }
            get { return m_estimatedDamage; }
        }
        public int DamageTaken
        {
            set { m_damageTaken = value; }
            get { return m_damageTaken; }
        }
        public int Reaction
        {
            set { m_reaction = value; }
            get { return m_reaction; }
        }
        public int CasterID
        {
            set { m_casterID = value; }
            get { return m_casterID; }
        }
        public int TargetID
        {
            set { m_targetID = value; }
            get { return m_targetID; }
        }

        public CombatEntity.LevelType Gathering
        {
            set { m_gathering = value; }
            get { return m_gathering; }
        }

        public int TargetHealth
        {
            set { m_targetHealth = value; }
            get { return m_targetHealth; }
        }
        public int SkillID
        {
            set { m_skillID = value; }
            get { return m_skillID; }
        }
        internal int AttackType
        {
            set { m_attackType = value; }
            get { return m_attackType; }
        }
        internal CombatEntity TargetLink
        {
            set { m_targetLink = value; }
            get { return m_targetLink; }
        }
        internal CombatEntity CasterLink
        {
            set { m_casterLink = value; }
            get { return m_casterLink; }
        }

        internal double ApplyTime
        {
            set { m_applyTime = value; }
            get { return m_applyTime; }
        }
        internal double ActionCompleteTime
        {
            set { m_actionCompleteTime = value; }
            get { return m_actionCompleteTime; }
        }
        internal bool Aggressive
        {
            set { m_aggressive = value; }
            get { return m_aggressive; }
        }
        internal int DamageID
        {
            set { m_damageID = value; }
            get { return m_damageID; }
        }
        internal double AggroModifier
        {
            set { m_aggroModifier = value; }
            get { return m_aggroModifier; }
        }
        /// <summary>
        /// Has the damage been applied to the target
        /// used for end effects like attack procs
        /// </summary>
        internal bool DamageApplied
        {
            get { return m_damageApplied; }
            set { m_damageApplied = value; }
        }
        internal int Critical
        {
            get { return m_critical; }
            set { m_critical = value; }
        }

        internal CombatDamageMessageData()
        {
        }
        internal CombatDamageMessageData(CombatDamageMessageData dataToCopy)
        {
            m_messageType = dataToCopy.MessageType;
            m_damageTaken = dataToCopy.DamageTaken;
            m_reaction = dataToCopy.Reaction;
            m_casterID = dataToCopy.CasterID;
            m_targetID = dataToCopy.TargetID;
            m_targetHealth = dataToCopy.TargetHealth;
            m_skillID = dataToCopy.SkillID;
            m_attackType = dataToCopy.AttackType;
            m_aggressive = dataToCopy.Aggressive;
            m_targetLink = dataToCopy.TargetLink;
            m_casterLink = dataToCopy.CasterLink;
            m_applyTime = dataToCopy.ApplyTime;
            m_sentDamage = dataToCopy.SentDamage;
            m_estimatedDamage = dataToCopy.EstimatedDamage;
            //m_gathering = dataToCopy.Gathering;
            m_critical = dataToCopy.Critical;
            m_actionCompleteTime = dataToCopy.ActionCompleteTime;
        }
    }
}