using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MainServer
{
    class MobSkill
    {
        EntitySkill m_theSkill=null;
        int m_probability = 0;

        internal EntitySkill TheSkill
        {
            get { return m_theSkill; }
        }
        internal int Probability
        {
            set { m_probability = value; }
        }
        internal MobSkill(MobSkillTemplate template)
        {
            m_theSkill = new EntitySkill(template.TheTemplate);
            m_theSkill.SkillLevel = template.SkillLevel;
           // m_probability = template.ProbabilityFactor;
        }
        internal bool IsOfType(MobSkillTable.Mob_Skill_Category category)
        {
            bool passesTypeCheck = false;

            switch (category)
            {
                case MobSkillTable.Mob_Skill_Category.MSC_ALL:
                    {
                        passesTypeCheck = true;
                        break;
                    }
                case MobSkillTable.Mob_Skill_Category.MSC_ATTACK:
                    {
                        if (TheSkill.Template.CastTargetGroup == SkillTemplate.CAST_TARGET.ENEMY && TheSkill.getSkillTemplateLevel(false).getUnModifiedAmount(TheSkill,false) > 0)
                        {
                            passesTypeCheck = true;
                        }
                        break;
                    }
                case MobSkillTable.Mob_Skill_Category.MSC_HEALING:
                    {
                        if (TheSkill.Template.CastTargetGroup != SkillTemplate.CAST_TARGET.ENEMY && TheSkill.getSkillTemplateLevel(false).getUnModifiedAmount(TheSkill, false) < 0)
                        {
                            passesTypeCheck = true;
                        }
                        break;
                    }
                case MobSkillTable.Mob_Skill_Category.MSC_BUFF:
                    {
                        if (TheSkill.Template.CastTargetGroup != SkillTemplate.CAST_TARGET.ENEMY && TheSkill.Template.StatusEffectID>0)
                        {
                            passesTypeCheck = true;
                        }
                        break;
                    }
                case MobSkillTable.Mob_Skill_Category.MSC_DEBUFF:
                    {
                        if (TheSkill.Template.CastTargetGroup == SkillTemplate.CAST_TARGET.ENEMY && TheSkill.Template.StatusEffectID >= 0)
                        {
                            passesTypeCheck = true;
                        }
                        break;
                    }
            }

            return passesTypeCheck;
        }

    }
    class MobSkillTable
    {
        internal enum Mob_Skill_Category
        {
            MSC_ALL = 0,
            MSC_ATTACK = 1,
            MSC_HEALING = 2,
            MSC_BUFF=3,
            MSC_DEBUFF=4
        };

        List<MobSkill> m_skillList = new List<MobSkill>();
        List<MobSkillSet> m_skillSets = new List<MobSkillSet>();

        internal List<MobSkillSet> SkillSets
        {
            get { return m_skillSets; }
        }


        internal MobSkillTable(List<MobSkillTemplate> skillList,List<MobSkillSetTemplate>skillSetTemplates)
        {
            for (int i = 0; i < skillList.Count; i++)
            {
                MobSkillTemplate currentMobSkill = skillList[i];
                AddSkill(currentMobSkill);
            }

            //for all skill sets
            for (int i = 0; i < skillSetTemplates.Count; i++)
            {
                MobSkillSetTemplate currentTemplate = skillSetTemplates[i];
                //create a unique set
                MobSkillSet newSet = new MobSkillSet(currentTemplate, currentTemplate.AISkillSetID);
                //for all weights
                List<SkillSetTemplateWeight> weights = currentTemplate.SkillSet.SkillWeights;
                for (int currentWeightIndex = 0; currentWeightIndex < weights.Count; currentWeightIndex++)
                {
                    SkillSetTemplateWeight currentWeight = weights[currentWeightIndex];
                    //check the mob has the skill
                    MobSkill theSkill = GetSkillForID(currentWeight.SkillID);
                    if (theSkill != null)
                    {
                        //Create a new Weight
                        //Link the Weight to the Skill 
                        MobSkillWeight skillWeight = new MobSkillWeight(theSkill, currentWeight.SkillID, currentWeight.Weight);
                        
                        //add the weight to the List of weights
                        newSet.SetSkills.Add(skillWeight);
                    }
                }
                m_skillSets.Add(newSet);
            }
        }
        void AddSkill(MobSkillTemplate skillToAdd)
        {
            //if the skill doesn't exist then don't add it
            if (skillToAdd == null || skillToAdd.TheTemplate == null)
            {
                return;
            }
            //create the skill
            MobSkill newSkill = new MobSkill(skillToAdd);

            //do some verification

            //add it to the list
            m_skillList.Add(newSkill);


        }

        static internal MobSkill GetSkillFromList(List<MobSkillWeight> skillList)
        {
            MobSkill theSkill = null;

            int totalSum = 0;
            for (int i = 0; i < skillList.Count; i++)
            {
                MobSkillWeight currentMobSkill = skillList[i];

                if (currentMobSkill == null || currentMobSkill.Skill==null||currentMobSkill.Skill.TheSkill == null || currentMobSkill.Skill.TheSkill.getSkillTemplateLevel(false) == null)
                {
                    continue;
                }
                totalSum += currentMobSkill.Weight;
            }

            int result = Program.getRandomNumber(totalSum);

            int currentCount = 0;
            for (int i = 0; i < skillList.Count && theSkill == null; i++)
            {
                MobSkillWeight currentMobSkill = skillList[i];

                if (currentMobSkill == null || currentMobSkill.Skill == null || currentMobSkill.Skill.TheSkill == null || currentMobSkill.Skill.TheSkill.getSkillTemplateLevel(false) == null)
                {
                    continue;
                }
                currentCount += currentMobSkill.Weight;
                if (currentCount > result)
                {
                    theSkill = currentMobSkill.Skill;
                }
            }

            return theSkill;

        }

        internal void AddSkillsToListOfTypeFromSet(List<MobSkillWeight> listToAddTo, MobSkillSet availableSet, Mob_Skill_Category skillType, float distanceFromTarget, int availableEnergy, bool checkRecharge, CombatEntity target)
        {
            double currentTime = Program.MainUpdateLoopStartTime();
            for (int i = 0; i < availableSet.SetSkills.Count; i++)
            {
                //get the current Skill
                MobSkillWeight currentWeight = availableSet.SetSkills[i];
                MobSkill currentMobSkill = currentWeight.Skill;
                //does the skill exist
                //if any part is not definedit is not valid
                if (currentMobSkill == null || currentMobSkill.TheSkill == null
                    || currentMobSkill.TheSkill.Template == null || currentMobSkill.TheSkill.getSkillTemplateLevel(false) == null)
                {
                    continue;
                }
                //get the skill info
                SkillTemplate skillTemplate = currentMobSkill.TheSkill.Template;
                SkillTemplateLevel templateLevel = currentMobSkill.TheSkill.getSkillTemplateLevel(false);
                //check the energy is valid
                if (availableEnergy >= 0 && templateLevel.EnergyCost > availableEnergy)
                {
                    continue;
                }
                //is the skill the correntType
                bool addToList = currentMobSkill.IsOfType(skillType);
                if (addToList == true && target != null && (skillType == Mob_Skill_Category.MSC_BUFF || skillType == Mob_Skill_Category.MSC_DEBUFF))
                {
                    if (target.GetStatusEffectForID((EFFECT_ID)skillTemplate.StatusEffectID) != null)
                    {

                        addToList = false;
                    }
                }
                //has it finnished recharging
                //is it in range
                //is it aready in the list
                if (addToList == true)
                {
                    double timeSinceLastCast = currentTime - currentMobSkill.TheSkill.TimeLastCast;
                    double rechargeTime = 0;
                    if (templateLevel != null)
                    {
                        rechargeTime = templateLevel.GetRechargeTime(currentMobSkill.TheSkill, false);
                    }
                    if ((timeSinceLastCast < rechargeTime && checkRecharge == true) || (distanceFromTarget > skillTemplate.Range && distanceFromTarget >= 0))
                    {
                        addToList = false;
                    }
                    //dont add a skill that is already there
                    else if (listToAddTo.Contains(currentWeight) == true)
                    {
                        addToList = false;
                    }

                }
                //if everything passed add it to the list
                if (addToList == true)
                {
                    listToAddTo.Add(currentWeight);
                }

            }
        }
        internal bool HasSkillOfTypeAvailable(MobSkillTable.Mob_Skill_Category skillType)
        {
            bool hasAvailableSkill = false;
            for (int i = 0; i < m_skillList.Count && hasAvailableSkill==false; i++)
            {
                //get the current Skill
                MobSkill currentMobSkill = m_skillList[i];
                //does the skill exist
                if (currentMobSkill == null || currentMobSkill.TheSkill == null
                    || currentMobSkill.TheSkill.Template == null || currentMobSkill.TheSkill.getSkillTemplateLevel(false) == null)
                {
                    continue;
                }
                //is the skill the correntType
                hasAvailableSkill = currentMobSkill.IsOfType(skillType);

            }
            return hasAvailableSkill;
        }
        internal MobSkill GetSkillForID(SKILL_TYPE skillID)
        {
            MobSkill theSkill = null;

            
            for (int i = 0; i < m_skillList.Count && theSkill==null; i++)
            {
                MobSkill currentMobSkill = m_skillList[i];

                if (currentMobSkill == null || currentMobSkill.TheSkill == null || currentMobSkill.TheSkill.getSkillTemplateLevel(false) == null ||
                   currentMobSkill.TheSkill.SkillID != skillID)
                {
                    continue;
                }
                theSkill = currentMobSkill;
            }

            return theSkill;
        }
        internal void ResetWeights()
        {
            for (int i = 0; i < m_skillList.Count; i++)
            {
                MobSkill currentMobSkill = m_skillList[i];
                if (currentMobSkill != null)
                {
                    currentMobSkill.Probability = 0;
                }
            }
        }
    }
}
