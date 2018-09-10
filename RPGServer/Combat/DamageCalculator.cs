using System;

namespace MainServer.Combat
{
    static class DamageCalculator
    {
        /// <summary>
        /// number of levels below the player that the mob has to be for the damage to start to be reduced
        /// </summary>
        internal static int DAMAGE_TAIL_OFF_LVL_BELOW_START = 30;
        /// <summary>
        /// number of levels below the player that the mob has to be for the damage to be fully reduced
        /// </summary>
        internal static int DAMAGE_TAIL_OFF_LVL_BELOW_END = 60;
        /// <summary>
        /// Minimum remaining percent of damage after lvl taken into account
        /// </summary>
        internal static float DAMAGE_TAIL_OFF_MIN_VAL = 0.3f;
        /// <summary>
        /// The level a mob has to be before the damage tail based on lvl off starts to be applied 
        /// </summary>
        internal static int DAMAGE_TAIL_OFF_MOB_START_LVL = 30;
        /// <summary>
        /// The level of a mob at which the damage tail off is in full effect
        /// </summary>
        internal static int DAMAGE_TAIL_OFF_MOB_END_LVL = 60;
        /// <summary>
        /// The amount of time in minutes that a logged out player will fully recover health and energy
        /// </summary>
        internal static float FULL_HEAL_LOGOUT_TIME = 60;


        public static float calcLevelReductionFactor(int casterLevel, int targetLvl)
        {
            const float LevelBasedReductionFactor = 0.04f;
            const float MinLevelBasedReduction = 0.1f;

            float levelReductionFactor = 1.0f;


            if (targetLvl > casterLevel)
            {
                levelReductionFactor = (1.0f - LevelBasedReductionFactor * (targetLvl - casterLevel));
                if (levelReductionFactor < MinLevelBasedReduction)
                {
                    levelReductionFactor = MinLevelBasedReduction;
                }
            }
                //if the mobs lvl is lower than the players lvl by enough to start reducing the damage
                //if the mobs lvl is high enough to be reduced
            else if ((targetLvl - DAMAGE_TAIL_OFF_LVL_BELOW_START) < casterLevel && targetLvl > DAMAGE_TAIL_OFF_MOB_START_LVL)
            {

                //the difference in lvl between attacker and target
                int lvlDiff = casterLevel - targetLvl;
                //how much of the damage reduction should be used if the mobs lvl is not high enough 
                float mobLvlTailOff = 1;
                //how much will the damage  be reduced
                float levelBasedReduction = 0;
                //if the mob is a low enough level to not use full damage tail off 
                //work out how much of the tail off will be3 in effect 
                if (targetLvl < DAMAGE_TAIL_OFF_MOB_END_LVL)
                {
                    float moblvlIntoTailOff = targetLvl - DAMAGE_TAIL_OFF_MOB_START_LVL;
                    float lvlRange = DAMAGE_TAIL_OFF_MOB_END_LVL - DAMAGE_TAIL_OFF_MOB_START_LVL;
                    mobLvlTailOff = moblvlIntoTailOff / lvlRange;
                }
                //should it use the maximum tail off value
                if (lvlDiff >= DAMAGE_TAIL_OFF_LVL_BELOW_END)
                {
                    levelBasedReduction = 1 - DAMAGE_TAIL_OFF_MIN_VAL;
                }
                    //otherwise work out how much of a tail off there should be
                else
                {
                    levelBasedReduction = (1 - DAMAGE_TAIL_OFF_MIN_VAL) * (lvlDiff - DAMAGE_TAIL_OFF_LVL_BELOW_START) / (DAMAGE_TAIL_OFF_LVL_BELOW_END - DAMAGE_TAIL_OFF_LVL_BELOW_START);
                }
                //take this away from 1 to get the actual reduction value
                levelReductionFactor = 1 - (levelBasedReduction * mobLvlTailOff);
                //just incase something has gone wronge clamp the vals
                if (levelReductionFactor > 1)
                {
                    levelReductionFactor = 1;
                }
                else if (levelReductionFactor < DAMAGE_TAIL_OFF_MIN_VAL)
                {
                    levelReductionFactor = DAMAGE_TAIL_OFF_MIN_VAL;
                }
                if (Program.m_LogDamage)
                {
                    Program.Display("damage reduced due to lvl diff moblvl:" + targetLvl + " attackerLVL:" + casterLevel + " levelReductionFactor:" + levelReductionFactor);
                }
            }
            return levelReductionFactor;
        }

        /// <summary>
        /// Part 1 of the Calculation, this goes on to call Part 2
        /// </summary>
        public static CalculatedDamage CalculateDamage(int maxDamage, int maxDefence, int casterLevel, CombatEntity target)
        {
            float levelReductionFactor = 1.0f;
            if (target.Type == CombatEntity.EntityType.Mob)
            {
                levelReductionFactor = calcLevelReductionFactor(casterLevel, target.Level);

            }
            float reducedDamage = maxDamage * levelReductionFactor;
            float randDamage = (reducedDamage + Program.getRandomNumberFromZero((int)reducedDamage + 1)) / 2.0f;

            return CalculateDamage(maxDamage, maxDefence, casterLevel, target, randDamage, levelReductionFactor);
        }

        /// <summary>
        /// Part 1 of the Calculation, this goes on to call the fluctuation part that calculates the random damage
        /// </summary>
        public static CalculatedDamage CalculateDamage(bool isSkill, bool isMelee, int maxDamage, int maxDefence, CombatEntity attacker, CombatEntity target)
        {
            if (attacker as Character == null)
                return CalculateDamage(maxDamage, maxDefence, attacker.GetRelevantLevel(target), target);  // standard case for server controlled entities


            float fluctuationRollChance = 0.5f; // standard case - they get 50% chance of high, 50% chance of low
            if (isSkill)
            {
                fluctuationRollChance = Program.processor.SkillDamageFluctuationManager
                    .GetSkillDamageFlucationForClass(((Character) attacker).m_class.m_classType);
            }
            else if (isMelee)
            {
                fluctuationRollChance = Program.processor.MeleeDamageFluctuationManager
                    .GetMeleeDamageFlucationForClass(((Character)attacker).m_class.m_classType);
            }

            return CalculateRandomDamageFromFlunctuation(maxDamage, maxDefence, attacker.GetRelevantLevel(target), target, fluctuationRollChance);
        }

        /// <summary>
        /// Part 2 of the Calculation, this does the bulk work of the calculation and returns the final damage values
        /// </summary>   
        private static CalculatedDamage CalculateDamage(int maxDamage, int maxDefence, int casterLevel, CombatEntity target, float randDamage, float levelReductionFactor)
        {
            const float N1 = 6.0f;
            const float N2 = 3.0f;

            float reducedDamage = maxDamage * levelReductionFactor;
            float estOriginalDamage = randDamage;
            if (levelReductionFactor < 1 && levelReductionFactor != 0)
            {
                estOriginalDamage = randDamage / levelReductionFactor;
            }

            float mitigation = maxDefence / (maxDefence + N1 + N2 * casterLevel);
            float tempDamage = randDamage * (1 - mitigation);
            float tempOriginalDamage = estOriginalDamage * (1 - mitigation);
            if (tempDamage < 0)
            {
                tempDamage = 0;
            }
            if (tempOriginalDamage < 0)
            {
                tempOriginalDamage = 0;
            }

            // tempDamage += (float)maxDamage / 4.0f;
            int damage = (int)Math.Round(tempDamage);
            int originalDamage = (int)Math.Round(tempOriginalDamage);
            if (Program.m_LogDamage)
            {
                Program.Display("maxDamage=" + maxDamage + ",reducedDamage=" + Math.Round(reducedDamage) + ",randDamage=" + Math.Round(randDamage) + ",maxDefence=" + maxDefence + ",casterLevel=" + casterLevel + ",mitigation=" + mitigation.ToString("F2") + ",damage=" + damage + ", originalDamage=" + originalDamage);
            }
            if (target.Level > casterLevel)
            {
                originalDamage = damage;
            }
            var newDamage = new CalculatedDamage(damage, originalDamage);
            return newDamage;
        }

        /// <summary>
        /// This calculates the random damage based on the fluctuation passed in
        /// </summary>
        private static CalculatedDamage CalculateRandomDamageFromFlunctuation(int maxDamage, int maxDefence, int casterLevel, CombatEntity target, float fluctuationRollChance)
        {
            float levelReductionFactor = 1.0f;
            if (target.Type == CombatEntity.EntityType.Mob)
            {
                levelReductionFactor = calcLevelReductionFactor(casterLevel, target.Level);

            }
            float reducedDamage = maxDamage * levelReductionFactor;

            float randDamage;
            int randomRollChance = Program.getRandomNumber(100);
            fluctuationRollChance = fluctuationRollChance*100.0f; // to make it a % out of 100
            if (randomRollChance < fluctuationRollChance) // high damage roll
            {
                randDamage = (reducedDamage*0.8f + (Program.getRandomNumberFromZero((int)reducedDamage + 1)) * 0.2f);
            }
            else
            {
                 randDamage = (reducedDamage*0.6f + (Program.getRandomNumberFromZero((int)reducedDamage + 1))* 0.2f);
            }

            return CalculateDamage(maxDamage, maxDefence, casterLevel, target, randDamage, levelReductionFactor);
        }
        
    }
}