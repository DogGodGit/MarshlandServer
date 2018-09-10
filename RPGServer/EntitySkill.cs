using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MainServer
{
    public class EntitySkill
    {

        double m_timeLastCast = 0;
        SkillTemplate m_template;
        int m_skillLevel = 0;
        int m_maxLevel = 0;
        int m_modifiedLevel = 0;
        bool m_fromItem = false;
        bool m_isProc = false;
        int m_timesCastSinceLog = 0;
        int m_timesCastBeforeLog = 0;
        List<SkillAugment> m_skillAugments = new List<SkillAugment>();
        public virtual double TimeLastCast
        {
            set { m_timeLastCast = value; }
            get { return m_timeLastCast; }
        }
        internal SKILL_TYPE SkillID
        {
            get { return m_template.SkillID; }
        }
        internal int TimesCastSinceLog
        {
            get { return m_timesCastSinceLog; }
            set { m_timesCastSinceLog = value; }
        }
        internal int TimesCastBeforeLog
        {
            get { return m_timesCastBeforeLog; }
            set { m_timesCastBeforeLog = value; }
        }
       
        public int SkillLevel
        {
            get { return m_skillLevel; }
            set { m_skillLevel = value; m_modifiedLevel = value; }// fudge for no resetskillmodifiers
        }
        public int MaxLevel
        {
            get { return m_maxLevel; }
            set { m_maxLevel = value; }
        }
        public SkillTemplate Template
        {
            get { return m_template; }
        }
        internal List<SkillAugment> SkillAugments
        {
            get { return m_skillAugments; }
        }
        public int ModifiedLevel
        {
            get { return m_modifiedLevel; }
            set
            {
                //check the value is not to high or to low then clamp it
                int newLevel = value;

                if (newLevel < 0)
                {
                    newLevel = 0;
                }
                else if (newLevel > m_template.GetMaxLevel())
                {

                    newLevel = m_template.GetMaxLevel();
                }
                m_modifiedLevel = newLevel;

            }
        }
        /// <summary>
        /// true if this skill is from using an item and does not belong to the entity
        /// </summary>
        internal bool FromItem
        {
            get { return m_fromItem; }
            set
            {
                m_fromItem = value;
            }
        }
        internal bool IsProc
        {
            get { return m_isProc; }
            set
            {
                m_isProc = value;
            }
        }
        internal void SetModifiedLevel(int newLevel, int entityLevel)
        {
            //check the value is not to high or to low then clamp it

            int maxLvl = m_template.GetMaxLevel();//m_template.GetMaxSkillLevelForPlayerLevel(entityLevel);
            if (newLevel < 0)
            {
                newLevel = 0;
            }
                
            else if (newLevel > maxLvl)
            {

                newLevel = maxLvl;
            }
            m_modifiedLevel = newLevel;


        }
        public SkillTemplateLevel getSkillTemplateLevel(bool pvp)
        {
            return m_template.getSkillTemplateLevel(m_modifiedLevel,pvp);
        }
        public EntitySkill(SkillTemplate template)
        {
            m_template = template;
        }

        internal virtual void SkillCast(double currentTime){
            TimeLastCast = currentTime;
        }
        internal double GetBaseValForAugment(CombatModifiers.Modifier_Type modType, bool inPvp)
        {
            double modVal = 0;
            switch (modType)
            {
                case CombatModifiers.Modifier_Type.AddedSkillDamage:
                    {
                        SkillTemplateLevel skillLevel = m_template.getSkillTemplateLevel(ModifiedLevel,inPvp);
                        if(skillLevel!=null){
                            modVal = skillLevel.baseDamage;
                        }
                        break;
                    }
                case CombatModifiers.Modifier_Type.ChangesCastingTime:
                    {
                        SkillTemplateLevel skillLevel = m_template.getSkillTemplateLevel(ModifiedLevel, inPvp);
                        if (skillLevel != null)
                        {
                            modVal = skillLevel.CastingTime;
                        }
                        break;
                    }
                case CombatModifiers.Modifier_Type.ChangesRecastTime:
                    {
                        SkillTemplateLevel skillLevel = m_template.getSkillTemplateLevel(ModifiedLevel, inPvp);
                        if (skillLevel != null)
                        {
                            modVal = skillLevel.RechargeTime;
                        }
                        break;
                    }
            }
            return modVal;
        }
        internal SkillAugment GetAugmentForType(CombatModifiers.Modifier_Type modType)
        {
            SkillAugment augmentForType = null;
            for (int i = 0; i < m_skillAugments.Count && augmentForType == null; i++)
            {
                SkillAugment currentAugment = m_skillAugments[i];
                if (currentAugment.ModType == modType)
                {
                    augmentForType = currentAugment;
                }
            }
            return augmentForType;
        }
    }
}
