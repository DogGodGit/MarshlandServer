# region Includes

// Includes //
using System;

#endregion 

namespace MainServer
{
    #region Details
    // AbilityVariables                                                                  //
    // Container Class - for access defined variables controlling the four new abilities //
    // Critical Strike, Critical Skill, Scholar, Treasure Hunter                         //
    // Each has:                                                                         //
    // base ability - base value used to define final chance vs target level             //
    // max chance - max possible chance                                                  //
    // multiplier - multipier used against the abilities modified value                  //

    #endregion

    // AbilityVariables //
    public class AbilityVariables
    {
        #region Variables
        // Variables //
        private float m_meleeCritBaseAbility;
        private float m_meleeCritMaxChance;
        private float m_meleeCritMultiplier;
        private float m_skillCritBaseAbility;
        private float m_skillCritMaxChance;
        private float m_skillCritMultiplier;
        private float m_goldCritBaseAbility;
        private float m_goldCritMaxChance;
        private float m_goldCritMultiplier;
        private float m_xpCritBaseAbility;
        private float m_xpCritMaxChance;
        private float m_xpCritMultiplier;

        #endregion 

        #region Getters & Setters
        // Getters & Setters //
        internal float CriticalStrikeBaseAbility { get { return m_meleeCritBaseAbility; } set { m_meleeCritBaseAbility = value; } }
        internal float CriticalStrikeMaxChance   { get { return m_meleeCritMaxChance;   } set { m_meleeCritMaxChance   = value; } }
        internal float CriticalStrikeMultiplier  { get { return m_meleeCritMultiplier;  } set { m_meleeCritMultiplier  = value; } }
        internal float CriticalSkillBaseAbility  { get { return m_skillCritBaseAbility; } set { m_skillCritBaseAbility = value; } }
        internal float CriticalSkillMaxChance    { get { return m_skillCritMaxChance;   } set { m_skillCritMaxChance   = value; } }
        internal float CriticalSkillMultiplier   { get { return m_skillCritMultiplier;  } set { m_skillCritMultiplier  = value; } }
        internal float LuckyGoldBaseAbility      { get { return m_goldCritBaseAbility;  } set { m_goldCritBaseAbility  = value; } }
        internal float LuckyGoldMaxChance        { get { return m_goldCritMaxChance;    } set { m_goldCritMaxChance    = value; } }
        internal float LuckyGoldMultiplier       { get { return m_goldCritMultiplier;   } set { m_goldCritMultiplier   = value; } }
        internal float LuckyXpBaseAbility        { get { return m_xpCritBaseAbility;    } set { m_xpCritBaseAbility    = value; } }
        internal float LuckyXpMaxChance          { get { return m_xpCritMaxChance;      } set { m_xpCritMaxChance      = value; } }
        internal float LuckyXpMultiplier         { get { return m_xpCritMultiplier;     } set { m_xpCritMultiplier     = value; } }

        #endregion
    }
}
