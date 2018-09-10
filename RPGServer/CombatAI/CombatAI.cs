using System.Collections.Generic;

namespace MainServer
{
    internal class CombatDecisions
    {
        private readonly DECISION_TYPES m_decisionType = DECISION_TYPES.DT_NONE;
        private readonly int m_probabilityChance;

        internal CombatDecisions(DECISION_TYPES decisionType, int probability)
        {
            m_decisionType = decisionType;
            m_probabilityChance = probability;
        }

        internal int ProbabilityValue
        {
            get { return m_probabilityChance; }
        }

        internal DECISION_TYPES DecisionType
        {
            get { return m_decisionType; }
        }

        internal enum DECISION_TYPES
        {
            DT_NONE = 0,
            DT_ATTACK = 1,
            DT_ATTACK_SKILL = 2,
            DT_DEBUFF_SKILL = 3,
            DT_HEAL_SELF = 4,
            DT_BUFF_SELF = 5,
            DT_HEAL_OTHER = 6, //currently friendly other skills are not supported
            DT_BUFF_OTHER = 7 //once a way to find a target for these has been created they will be available
        }
    };

    internal class CombatAI
    {
        internal static double MOVING_INTO_RANGE_RECHECK_TIME = 0.5;
        internal static double WAITING_FOR_ACTION_RECHECK_TIME = 30.0;
        private readonly List<MobSkillSet> m_currentlyAvailableSkills = new List<MobSkillSet>();

        /// <summary>
        ///     A List of scripts that have restrictions on use but are not active in the current ai type
        /// </summary>
        private readonly List<CAI_ScriptContainer> m_oldScriptContainer = new List<CAI_ScriptContainer>();

        /// <summary>
        ///     A List of scrips that can be used in the current ai state
        /// </summary>
        private readonly List<CAI_ScriptContainer> m_scriptContainers = new List<CAI_ScriptContainer>();

        private double m_aiTemplateLastChangedTime;

        private List<CombatDecisions> m_availableDecisions;
        private double m_battleStartTime;
        private CombatAITemplate m_combatAITemplate;
        private bool m_currentActionComplete = true;
        private double m_lastCombatAIReport;
        private double m_lastDecisionCheck;
        private string m_lastDecisionString = "";
        private double m_lastDecisionTime;
        private CombatEntity m_mainTarget;
        private int m_minEnergyAttackSpell = -1;
        private float m_preferredCombatRange;
        private double m_timeAtLastAggroCheck;
        private bool m_waitTillInRange;

        internal CombatAI(ServerControlledEntity theMob)
        {
            SetUp(theMob);
        }

        internal double BattleStartTime
        {
            get { return m_battleStartTime; }
        }

        internal double AITemplateLastChangedTime
        {
            get { return m_aiTemplateLastChangedTime; }
        }

        internal CombatAITemplate AITemplate
        {
            get { return m_combatAITemplate; }
        }

        internal CombatEntity MainTarget
        {
            get { return m_mainTarget; }
            set { m_mainTarget = value; }
        }

        internal float PreferredCombatRange
        {
            get { return m_preferredCombatRange; }
        }

        internal CombatEntity GetMainTarget(ServerControlledEntity theMob)
        {
            CombatEntity mainTarget = null;
            CombatEntity targetToCheck = m_mainTarget;
            if (targetToCheck != null)
            {
                //if target isn't dead, an we aren't either, and in range. 
                if ((targetToCheck.Destroyed == false) && targetToCheck.IsEnemyOf(theMob) && (!targetToCheck.Dead) &&
                    (targetToCheck.TheCombatManager == theMob.TheCombatManager) &&
                    (theMob.OtherEntityCannotBeTargettedBecauseOFStatusEffect(targetToCheck) == false) &&
                    (Utilities.Difference2DSquared(targetToCheck.CurrentPosition.m_position,
                        theMob.ChaseStart.m_position) < theMob.FollowRange*theMob.FollowRange))
                {
                    mainTarget = targetToCheck;
                }
            }

            return mainTarget;
        }

        private static int CompareScriptContainers(CAI_ScriptContainer first, CAI_ScriptContainer second)
        {
            if (first == null)
            {
                if (second == null)
                {
                    return 0;
                }

                return -1;
            }
            if (second == null)
            {
                return 1;
            }

            if (first.Script.Priority > second.Script.Priority)
            {
                return -1;
            }
            if (first.Script.Priority < second.Script.Priority)
            {
                return 1;
            }

            return 0;
        }

        private bool AttemptToMakeScriptedDecision(ServerControlledEntity theMob)
        {
            bool decisionMade = false;
            double currentTime = Program.MainUpdateLoopStartTime();
            var passingScripts = new List<CAI_ScriptContainer>();
            //check each script to see if they can take action
            for (int activeScriptIndex = m_scriptContainers.Count - 1;
                activeScriptIndex >= 0 && decisionMade == false;
                activeScriptIndex--)
            {
                CAI_ScriptContainer currentActiveScript = m_scriptContainers[activeScriptIndex];
                bool triggerConditionPassed = currentActiveScript.PassesTriggerConditions(theMob, currentTime);
                if (triggerConditionPassed)
                {
                    bool passesAllConditions = currentActiveScript.PassesFailConditions(theMob, currentTime);
                    if (passesAllConditions)
                    {
                        passingScripts.Add(currentActiveScript);
                        //currentActiveScript.TakeAction(theMob);
                        // decisionMade = true;
                    }
                }
            }
            if (passingScripts.Count > 0)
            {
                passingScripts.Sort(CompareScriptContainers);
                CAI_ScriptContainer scriptToUse = passingScripts[0];
                if (scriptToUse != null)
                {
                    scriptToUse.TakeAction(theMob);
                    decisionMade = true;
                }
            }
            // if no action was taken then clear down any data that was being held
            //to aid in decision making
            // if (decisionMade == false)
            {
                theMob.RecentDamages.Clear();
            }

            return decisionMade;
        }

        internal CAI_ScriptContainer GetScript(int scriptID)
        {
            for (int currentScriptIndex = 0; currentScriptIndex < m_scriptContainers.Count; currentScriptIndex++)
            {
                CAI_ScriptContainer currentContainer = m_scriptContainers[currentScriptIndex];

                if (currentContainer != null && currentContainer.Script != null)
                {
                    if (currentContainer.Script.ScriptID == scriptID)
                    {
                        return currentContainer;
                    }
                }
            }
            for (int currentScriptIndex = 0; currentScriptIndex < m_oldScriptContainer.Count; currentScriptIndex++)
            {
                CAI_ScriptContainer currentContainer = m_oldScriptContainer[currentScriptIndex];

                if (currentContainer != null && currentContainer.Script != null)
                {
                    if (currentContainer.Script.ScriptID == scriptID)
                    {
                        return currentContainer;
                    }
                }
            }
            return null;
        }

        private bool MakeADecision(ServerControlledEntity theMob)
        {
            bool decisionMade = false;
            var tempList = new List<CombatDecisions>(m_availableDecisions);
            //the sum of the probabilities for all actions
            int totalDecisionProbability = 0;
            //work out the starting random Chance
            for (int i = 0; i < m_availableDecisions.Count; i++)
            {
                CombatDecisions currentDecision = m_availableDecisions[i];
                if (currentDecision != null)
                {
                    totalDecisionProbability += currentDecision.ProbabilityValue;
                }
            }
            while (totalDecisionProbability > 0 && decisionMade == false && tempList.Count > 0)
            {
                int decisionValue = Program.getRandomNumber(totalDecisionProbability);
                bool decisionFound = false;
                int currentValue = 0;
                //find the choice made
                for (int i = 0; i < tempList.Count && decisionFound == false; i++)
                {
                    CombatDecisions currentDecision = m_availableDecisions[i];
                    if (currentDecision != null)
                    {
                        currentValue += currentDecision.ProbabilityValue;
                    }
                    if (currentValue >= decisionValue)
                    {
                        decisionFound = true;
                        //check if it is valid
                        bool isValid = false;
                        CombatEntity skillTarget = null;
                        MobSkill skillToUse = null;
                        isValid = GetValidAction(ref skillToUse, ref skillTarget, theMob, currentDecision.DecisionType);
                        if (isValid)
                        {
                            //commit the decision to the mob
                            decisionMade = true;
                            if (skillToUse != null && skillTarget != null)
                            {
                                theMob.UseSkill(skillToUse, skillTarget);
                            }
                            DecisionMade();
                            //make a string record
                            if (skillToUse != null && skillTarget != null)
                            {
                                m_lastDecisionString = "Decision made, using skill type " + currentDecision.DecisionType +
                                                       " " + skillToUse.TheSkill.Template.SkillName + " on " +
                                                       skillTarget.Name + ".";
                            }
                            else if (skillToUse != null)
                            {
                                m_lastDecisionString = "Decision made, using skill type " + currentDecision.DecisionType +
                                                       " " + skillToUse.TheSkill.Template.SkillName + " on Null target.";
                            }
                            else if (skillTarget != null)
                            {
                                m_lastDecisionString = "Decision made, type " + currentDecision.DecisionType + " on " +
                                                       skillTarget.Name + ".";
                            }
                            else
                            {
                                m_lastDecisionString = "oops, something went wrong";
                            }
                            if (Program.m_LogAIDebug)
                            {
                                Program.Display("lastDecisionString" + m_lastDecisionString);
                                string mobString = theMob.GetCombatDebugString();
                                Program.Display(theMob.Name + " " + theMob.ServerID + " Combat Data = " + mobString +
                                                ".");
                                if (theMob.NextSkill == null && skillToUse != null)
                                {
                                    Program.Display("theMob.NextSkill!=skillToUse");
                                }
                            }
                        }
                        else
                        {
                            // remove the option
                            //remove it from the choices
                            tempList.Remove(currentDecision);
                            //remove from the total probability value
                            totalDecisionProbability -= currentDecision.ProbabilityValue;
                        }
                    }
                }
            }

            if (decisionMade == false)
            {
                //you must be out of range
                if (m_waitTillInRange == false)
                {
                    //m_lastDecisionString += " Now waiting till in range";
                }
                m_waitTillInRange = true;
            }
            return decisionMade;
        }

        internal void ResetCombatAI(ServerControlledEntity theMob)
        {
            ActionComplete();
            m_preferredCombatRange = CalculateDesiredCombatRange(theMob);
            m_waitTillInRange = false;
            m_lastDecisionString = "";
            m_lastDecisionTime = 0;
            m_lastCombatAIReport = 0;
        }

        /// <summary>
        /// Called if there has been a change to the aggro to require it to be checked sooner than expected
        /// </summary>
        internal void AggroNeedsRechecked(double currentTime, ServerControlledEntity theMob)
        {
            double timeToRemove = ServerControlledEntity.AGGRO_RECHECK_TIME -
                                  ServerControlledEntity.AGGRO_MODIFIED_RECHECK_TIME;
            if (currentTime - m_timeAtLastAggroCheck < timeToRemove)
            {
                m_timeAtLastAggroCheck = currentTime - timeToRemove;
            }
        }

        internal CombatEntity GetRunToTarget()
        {
            return MainTarget;
        }

        private bool GetValidAction(ref MobSkill skillToUse, ref CombatEntity skillTarget, ServerControlledEntity theMob,
            CombatDecisions.DECISION_TYPES decisionType)
        {
            CombatEntity mainTarget = MainTarget;
            bool decisionValid = false;

            switch (decisionType)
            {
                case CombatDecisions.DECISION_TYPES.DT_ATTACK:
                {
                    //doas it have a target 
                    if (mainTarget == null)
                    {
                        break;
                    }
                    //do a range check
                    //get the distance between attack target and the mob
                    //float distToMob = (theMob.CurrentPosition.m_position - theMob.AttackTarget.CurrentPosition.m_position).Length();
                    float distToMob = Utilities.Difference2D(theMob.CurrentPosition.m_position,
                        mainTarget.CurrentPosition.m_position);
                    //remove the radius
                    distToMob = distToMob - (theMob.Radius + mainTarget.Radius);
                    //is this less than attack range
                    if ((theMob.StatusPreventsActions.Move ||
                         (distToMob >= 0 || theMob.BacktrackDist >= theMob.MaxBacktrackDist)) &&
                        distToMob <= theMob.CompiledStats.MaxAttackRange)
                    {
                        skillTarget = mainTarget;
                        decisionValid = true;
                        if (mainTarget != theMob.AttackTarget)
                        {
                            theMob.AttackNewTarget(theMob.InCombat, mainTarget);
                        }
                    }

                    break;
                }
                case CombatDecisions.DECISION_TYPES.DT_ATTACK_SKILL:
                {
                    //doas it have a target 
                    CombatEntity target = mainTarget; //theMob.AttackTarget;
                    if (target == null)
                    {
                        break;
                    }

                    skillToUse = GetAvailableSkillOfType(m_currentlyAvailableSkills,
                        MobSkillTable.Mob_Skill_Category.MSC_ATTACK, theMob, target);
                    if (skillToUse != null)
                    {
                        decisionValid = true;
                        skillTarget = target;
                    }

                    break;
                }
                case CombatDecisions.DECISION_TYPES.DT_DEBUFF_SKILL:
                {
                    //doas it have a target 
                    CombatEntity target = mainTarget; //theMob.AttackTarget;
                    if (target == null)
                    {
                        break;
                    }

                    skillToUse = GetAvailableSkillOfType(m_currentlyAvailableSkills,
                        MobSkillTable.Mob_Skill_Category.MSC_DEBUFF, theMob, target);
                    if (skillToUse != null)
                    {
                        decisionValid = true;
                        skillTarget = target;
                    }

                    break;
                }
                case CombatDecisions.DECISION_TYPES.DT_HEAL_SELF:
                {
                    //doas it have a target 
                    CombatEntity target = theMob;
                    if (target == null)
                    {
                        break;
                    }
                    //is it hurt enough
                    if ((target.PercentHealth*100) > m_combatAITemplate.HealingThreshold)
                    {
                        break;
                    }
                    //check for available skills in range
                    skillToUse = GetAvailableSkillOfType(m_currentlyAvailableSkills,
                        MobSkillTable.Mob_Skill_Category.MSC_HEALING, theMob, target);
                    if (skillToUse != null)
                    {
                        decisionValid = true;
                        skillTarget = target;
                    }

                    break;
                }
                case CombatDecisions.DECISION_TYPES.DT_HEAL_OTHER:
                {
                    //doas it have a target 
                    CombatEntity target = null;
                    if (target == null)
                    {
                        break;
                    }

                    skillToUse = GetAvailableSkillOfType(m_currentlyAvailableSkills,
                        MobSkillTable.Mob_Skill_Category.MSC_HEALING, theMob, target);
                    if (skillToUse != null)
                    {
                        decisionValid = true;
                        skillTarget = target;
                    }

                    break;
                }
                case CombatDecisions.DECISION_TYPES.DT_BUFF_SELF:
                {
                    //doas it have a target 
                    CombatEntity target = theMob;
                    if (target == null)
                    {
                        break;
                    }
                    skillToUse = GetAvailableSkillOfType(m_currentlyAvailableSkills,
                        MobSkillTable.Mob_Skill_Category.MSC_BUFF, theMob, target);
                    if (skillToUse != null)
                    {
                        decisionValid = true;
                        skillTarget = target;
                    }

                    break;
                }
                case CombatDecisions.DECISION_TYPES.DT_BUFF_OTHER:
                {
                    //doas it have a target 
                    CombatEntity target = null;
                    if (target == null)
                    {
                        break;
                    }

                    skillToUse = GetAvailableSkillOfType(m_currentlyAvailableSkills,
                        MobSkillTable.Mob_Skill_Category.MSC_BUFF, theMob, target);
                    if (skillToUse != null)
                    {
                        decisionValid = true;
                        skillTarget = target;
                    }

                    break;
                }
                default:
                    break;
            }


            return decisionValid;
        }

        internal void ReachedDesiredPosition()
        {
            m_waitTillInRange = false;
        }

        private void SetUp(ServerControlledEntity theMob)
        {
            SetNewAITemplate(theMob.Template.m_combatAITemplate, theMob);
        }

        internal void SetNewAITemplate(CombatAITemplate aiTemplate, ServerControlledEntity theMob)
        {
            m_aiTemplateLastChangedTime = Program.MainUpdateLoopStartTime();
            m_combatAITemplate = aiTemplate;
            //copy the decisions, vet them all for availability
            m_availableDecisions = new List<CombatDecisions>();
            List<CombatDecisions> baseDecisions = m_combatAITemplate.CombatDecisionsList;
            for (int i = 0; i < baseDecisions.Count; i++)
            {
                CombatDecisions currentDecision = baseDecisions[i];
                if (currentDecision != null)
                {
                    bool isAvailable = checkDecisionAvailableForMob(currentDecision.DecisionType, theMob);
                    ;

                    if (isAvailable)
                    {
                        m_availableDecisions.Add(currentDecision);
                    }
                }
            }

            ClearDownInvalidScripts();
            AddScriptsFromList(m_combatAITemplate.Scripts);
            AddScriptsFromList(theMob.Template.m_scripts);

            theMob.SkillTable.ResetWeights();
            m_currentlyAvailableSkills.Clear();
            MobSkillSet defaultSet = MobSkillSet.GetSetFromList(-1, theMob.SkillTable.SkillSets);
            if (defaultSet != null)
            {
                m_currentlyAvailableSkills.Add(defaultSet);
                //theMob.SkillTable.SetProbabilitiesForSet(defaultSet);
            }
            for (int i = 0; i < m_combatAITemplate.SkillSetIDs.Count; i++)
            {
                MobSkillSet currentSet = MobSkillSet.GetSetFromList(m_combatAITemplate.SkillSetIDs[i],
                    theMob.SkillTable.SkillSets);
                if (currentSet != null)
                {
                    m_currentlyAvailableSkills.Add(currentSet);
                    //theMob.SkillTable.SetProbabilitiesForSet(currentSet);
                }
            }

            m_minEnergyAttackSpell = CalculateMinCostAttack(theMob);
            m_preferredCombatRange = CalculateDesiredCombatRange(theMob);
        }

        internal void ClearDownInvalidScripts()
        {
            for (int i = m_scriptContainers.Count - 1; i >= 0; i--)
            {
                CAI_ScriptContainer currentScriptContainer = m_scriptContainers[i];
                CAI_Script containedScript = currentScriptContainer.Script;
                if (containedScript.TemplateID >= 0 && containedScript.TemplateID != m_combatAITemplate.CombatAIID)
                {
                    if (currentScriptContainer.RemovesOnStateTransition() == false)
                    {
                        m_oldScriptContainer.Add(currentScriptContainer);
                    }

                    m_scriptContainers.RemoveAt(i);
                }
            }
        }

        internal void AddScriptsFromList(List<CAI_Script> allScripts)
        {
            var scriptsToAdd = new List<CAI_Script>();

            for (int i = allScripts.Count - 1; i >= 0; i--)
            {
                bool scriptActive = false;
                CAI_Script currentScript = allScripts[i];
                //check the script is for this template
                if (currentScript == null ||
                    (currentScript.TemplateID >= 0 && currentScript.TemplateID != m_combatAITemplate.CombatAIID))
                {
                    continue;
                }
                //look in the currently active scripts
                for (int activeScriptIndex = m_scriptContainers.Count - 1;
                    activeScriptIndex >= 0 && scriptActive == false;
                    activeScriptIndex--)
                {
                    //check the base script of each
                    CAI_ScriptContainer activeScript = m_scriptContainers[activeScriptIndex];
                    if (activeScript.Script == currentScript)
                    {
                        //nothing needs to be done as the script is already present
                        scriptActive = true;
                    }
                }
                //look through the ones that are on hold
                for (int oldScriptIndex = m_oldScriptContainer.Count - 1;
                    oldScriptIndex >= 0 && scriptActive == false;
                    oldScriptIndex--)
                {
                    //check the base script of each
                    CAI_ScriptContainer oldScript = m_oldScriptContainer[oldScriptIndex];
                    if (oldScript.Script == currentScript)
                    {
                        m_oldScriptContainer.RemoveAt(oldScriptIndex);
                        m_scriptContainers.Add(oldScript);
                        //nothing needs to be done as the script is already present
                        scriptActive = true;
                    }
                }
                //if the script is not already active a container will need to be made
                if (scriptActive == false)
                {
                    var newContainer = new CAI_ScriptContainer(currentScript);
                    m_scriptContainers.Add(newContainer);
                    scriptActive = true;
                }
            }
        }

        private int CalculateMinCostAttack(ServerControlledEntity theMob)
        {
            var availableAttackSkills = new List<MobSkillWeight>();
            //find all attack skills
            for (int i = 0; i < m_availableDecisions.Count; i++)
            {
                CombatDecisions currentDecision = m_availableDecisions[i];
                if (currentDecision != null)
                {
                    if (currentDecision.DecisionType == CombatDecisions.DECISION_TYPES.DT_ATTACK_SKILL)
                    {
                        for (int setIndex = 0; setIndex < m_currentlyAvailableSkills.Count; setIndex++)
                        {
                            MobSkillSet currentSet = m_currentlyAvailableSkills[setIndex];
                            theMob.SkillTable.AddSkillsToListOfTypeFromSet(availableAttackSkills, currentSet,
                                MobSkillTable.Mob_Skill_Category.MSC_ATTACK, -1, -1, false, null);
                        }
                    }
                    else if (currentDecision.DecisionType == CombatDecisions.DECISION_TYPES.DT_DEBUFF_SKILL)
                    {
                        for (int setIndex = 0; setIndex < m_currentlyAvailableSkills.Count; setIndex++)
                        {
                            MobSkillSet currentSet = m_currentlyAvailableSkills[setIndex];
                            theMob.SkillTable.AddSkillsToListOfTypeFromSet(availableAttackSkills, currentSet,
                                MobSkillTable.Mob_Skill_Category.MSC_DEBUFF, -1, -1, false, null);
                        }
                    }
                }
            }
            int minEnergyAttackSpell = -1;
            //find the attack skill with the shortest range
            for (int i = 0; i < availableAttackSkills.Count; i++)
            {
                MobSkillWeight currentWeight = availableAttackSkills[i];
                MobSkill currentSkill = currentWeight.Skill;

                if (currentWeight.Weight > 0 && currentSkill != null && currentSkill.TheSkill != null &&
                    currentSkill.TheSkill.Template != null && currentSkill.TheSkill.getSkillTemplateLevel(false) != null)
                {
                    if (currentSkill.TheSkill.getSkillTemplateLevel(false).EnergyCost < minEnergyAttackSpell ||
                        minEnergyAttackSpell < 0)
                    {
                        minEnergyAttackSpell = currentSkill.TheSkill.getSkillTemplateLevel(false).EnergyCost;
                    }
                }
            }
            return minEnergyAttackSpell;
        }

        private float CalculateDesiredCombatRange(ServerControlledEntity theMob)
        {
            float preferredRange = -1;
            switch (m_combatAITemplate.AiType)
            {
                case CombatAITemplate.COMBAT_AI_TYPES.AGGRESSIVE:
                {
                    preferredRange = theMob.CompiledStats.MaxAttackRange;
                    break;
                }
                case CombatAITemplate.COMBAT_AI_TYPES.MAGE_TYPE:
                {
                    var availableAttackSkills = new List<MobSkillWeight>();
                    //find all attack skills
                    for (int i = 0; i < m_availableDecisions.Count; i++)
                    {
                        CombatDecisions currentDecision = m_availableDecisions[i];
                        if (currentDecision != null)
                        {
                            if (currentDecision.DecisionType == CombatDecisions.DECISION_TYPES.DT_ATTACK_SKILL)
                            {
                                for (int setIndex = 0; setIndex < m_currentlyAvailableSkills.Count; setIndex++)
                                {
                                    MobSkillSet currentSet = m_currentlyAvailableSkills[setIndex];
                                    theMob.SkillTable.AddSkillsToListOfTypeFromSet(availableAttackSkills, currentSet,
                                        MobSkillTable.Mob_Skill_Category.MSC_ATTACK, -1, -1, false, null);
                                }
                            }
                            else if (currentDecision.DecisionType == CombatDecisions.DECISION_TYPES.DT_DEBUFF_SKILL)
                            {
                                for (int setIndex = 0; setIndex < m_currentlyAvailableSkills.Count; setIndex++)
                                {
                                    MobSkillSet currentSet = m_currentlyAvailableSkills[setIndex];
                                    theMob.SkillTable.AddSkillsToListOfTypeFromSet(availableAttackSkills, currentSet,
                                        MobSkillTable.Mob_Skill_Category.MSC_DEBUFF, -1, -1, false, null);
                                }
                            }
                        }
                    }

                    //find the attack skill with the shortest range
                    for (int i = 0; i < availableAttackSkills.Count; i++)
                    {
                        MobSkillWeight currentWeight = availableAttackSkills[i];
                        MobSkill currentSkill = currentWeight.Skill;
                        if (currentWeight.Weight > 0 && currentSkill != null && currentSkill.TheSkill != null &&
                            currentSkill.TheSkill.Template != null &&
                            currentSkill.TheSkill.getSkillTemplateLevel(false) != null)
                        {
                            if (currentSkill.TheSkill.Template.Range < preferredRange || preferredRange < 0)
                            {
                                preferredRange = currentSkill.TheSkill.Template.Range;
                            }
                        }
                    }
                    //if there were no skills to set it
                    //use attack range instead
                    if (preferredRange < 0)
                    {
                        preferredRange = theMob.CompiledStats.MaxAttackRange;
                    }
                    break;
                }
                default:
                    preferredRange = theMob.CompiledStats.MaxAttackRange;
                    break;
            }
            return preferredRange;
        }

        private bool checkDecisionAvailableForMob(CombatDecisions.DECISION_TYPES decisionType,
            ServerControlledEntity theMob)
        {
            bool isAvailable = false;

            switch (decisionType)
            {
                case CombatDecisions.DECISION_TYPES.DT_ATTACK:
                {
                    isAvailable = true;
                    break;
                }

                case CombatDecisions.DECISION_TYPES.DT_ATTACK_SKILL:
                {
                    isAvailable = theMob.SkillTable.HasSkillOfTypeAvailable(MobSkillTable.Mob_Skill_Category.MSC_ATTACK);
                    break;
                }
                case CombatDecisions.DECISION_TYPES.DT_DEBUFF_SKILL:
                {
                    isAvailable = theMob.SkillTable.HasSkillOfTypeAvailable(MobSkillTable.Mob_Skill_Category.MSC_DEBUFF);
                    break;
                }
                case CombatDecisions.DECISION_TYPES.DT_BUFF_SELF:
                case CombatDecisions.DECISION_TYPES.DT_BUFF_OTHER:
                {
                    isAvailable = theMob.SkillTable.HasSkillOfTypeAvailable(MobSkillTable.Mob_Skill_Category.MSC_BUFF);
                    break;
                }
                case CombatDecisions.DECISION_TYPES.DT_HEAL_OTHER:
                case CombatDecisions.DECISION_TYPES.DT_HEAL_SELF:
                {
                    isAvailable = theMob.SkillTable.HasSkillOfTypeAvailable(MobSkillTable.Mob_Skill_Category.MSC_HEALING);
                    break;
                }
                default:
                    break;
            }


            return isAvailable;
        }

        internal bool MoveDueToCombat(ServerControlledEntity theMob, double timeSinceLastUpdate)
        {			
            if (theMob == null)
            {
                return false;
            }
            if (m_combatAITemplate.AiType == CombatAITemplate.COMBAT_AI_TYPES.INANIMATE)
            {
                bool destChanged = theMob.MoveAlongSetRoute(timeSinceLastUpdate);
              	
				// snik - this was removed as design use inanimate as a way of preventing auto attacks
				// however auraxis can teleport and leaving combat early could cause his spawnpoint to be incorrectly set
				// apparently no mob actually uses this functionality 
				//  theMob.SetChaseStart(theMob.CurrentPosition);
                return destChanged;
            }
            return theMob.MoveTowardsAttackTarget(timeSinceLastUpdate);
        }

        internal void ChangeMainTarget(ServerControlledEntity theMob, CombatEntity newTarget)
        {
            if (m_mainTarget != newTarget)
            {
                if (theMob.AttackTarget == m_mainTarget && theMob.TheCombatManager != null)
                {
                    theMob.TheCombatManager.StopAttacking(theMob);
                }
                m_mainTarget = newTarget;
            }
        }

        internal void Update(ServerControlledEntity theMob, double timeSinceLastUpdate, double currentTime)
        {
            if (theMob == null)
            {
                return;
            }
			
            theMob.ConductedHotileAction();
            theMob.CheckOwner();
            //theMob.InCombat = true;
            bool canTakePartInCombat = ((theMob.MovementAI !=
                                         ServerControlledEntity.NPC_MOVEMENT_AI.NPC_MOVEMENT_AI_RETURNING) &&
                                        (!theMob.Dead));

            //the mob cannot take part in combat if it is returning or is dead


            if (canTakePartInCombat)
            {
                //if there is a target
                //if enough time has passed
                //check for new highest aggro target  

                if ((theMob.AggroList.Count > 0) &&
                    (currentTime > ServerControlledEntity.AGGRO_RECHECK_TIME + m_timeAtLastAggroCheck))
                {
                    CombatEntity newTarget = theMob.GetValidTarget(true);
                    if (newTarget != null)
                    {
                        ChangeMainTarget(theMob, newTarget);
                    }
                    m_timeAtLastAggroCheck = currentTime;
                }
                CombatEntity mainTarget = GetMainTarget(theMob);
                //if the pathing failed
                //forget the innitial target and go to the next
                if (theMob.CheckForFailedPathToEnt(mainTarget))
                {
                    theMob.RemoveFromAggroLists(mainTarget);
                    mainTarget = null;
                }


                //if there is no valid main target look for a new one
                if (mainTarget == null)
                {
                    //choose a new Target
                    CombatEntity newTarget = theMob.GetValidTarget(true);
                    mainTarget = newTarget;
                }
                if (m_mainTarget != mainTarget)
                {
                    ChangeMainTarget(theMob, mainTarget);
                }

                if (m_combatAITemplate.AiType == CombatAITemplate.COMBAT_AI_TYPES.INANIMATE)
                {
                    AttemptToTakeAction(theMob, timeSinceLastUpdate, currentTime, true);
                }
                else
                {
                    AttemptToTakeAction(theMob, timeSinceLastUpdate, currentTime, false);
                }
            }
        }

        private void AttemptToTakeAction(ServerControlledEntity theMob, double timeSinceLastUpdate, double currentTime,
            bool onlyUseScripts)
        {
            int maxInactiveTime = 10;
            CheckAttackRange(theMob);
            //is it time for the mob to make a new decision
            bool attemptDecision = (m_currentActionComplete && m_waitTillInRange == false);
            if (attemptDecision == false)
            {
                // have they spent long enough trying to get into range
                //if the mob is moving into position then check every so often
                if (m_waitTillInRange && currentTime - m_lastDecisionCheck > MOVING_INTO_RANGE_RECHECK_TIME)
                {
                    attemptDecision = true;
                }
                    //have they spent a long time waiting for the action to complete
                else if (m_currentActionComplete == false &&
                         currentTime - m_lastDecisionCheck > WAITING_FOR_ACTION_RECHECK_TIME)
                {
                    attemptDecision = true;
                    Program.Display(theMob.Name + " " + theMob.ServerID +
                                    " waiting for completion Timer caused Recheck");
                    string mobString = theMob.GetCombatDebugString();
                    Program.Display(theMob.Name + " " + theMob.ServerID + " Combat Data = " + mobString + ".");
                    ResetCombatAI(theMob);
                    mobString = theMob.GetCombatDebugString();
                    Program.Display(theMob.Name + " " + theMob.ServerID + " Was Reset Combat Data = " + mobString +
                                    ".");
                }
            }

            //is it time for the mob to make a new decision
            if (attemptDecision)
            {
                bool decisionMade = false;
                decisionMade = AttemptToMakeScriptedDecision(theMob);
                if (decisionMade == false && onlyUseScripts == false)
                {
                    decisionMade = MakeADecision(theMob);
                }
                m_lastDecisionCheck = currentTime;
                //if a decision was made and the decission was previously logging data then make a log
                //long wait times are expected if only scripts are being used
                if (decisionMade && onlyUseScripts == false)
                {
                    if (Program.m_LogAIDebug && (currentTime - m_lastDecisionTime) > maxInactiveTime)
                    {
                        double timeTakenSoFar = (currentTime - m_lastDecisionTime);
                        string logString = "decision made at " + currentTime + " for " + theMob.Name + " " +
                                           theMob.ServerID + " has taken " + timeTakenSoFar +
                                           "s, currect action complete = " + m_currentActionComplete +
                                           ", waiting Till In Range = " + m_waitTillInRange + " Last " +
                                           m_lastDecisionString + ".";
                        string mobString = theMob.GetCombatDebugString();
                        Program.Display(logString);
                        Program.Display(theMob.Name + " " + theMob.ServerID + " Combat Data = " + mobString + ".");
                        //remember when the last report was given
                        m_lastCombatAIReport = currentTime;
                    }
                    //remember the last time a decision was made
                    m_lastDecisionTime = currentTime;
                }
                else if (m_lastDecisionTime == 0)
                {
                    m_lastDecisionTime = currentTime;
                }
            }

            //check if any decision has been made recently, if not then report Data
            //long wait times are expected if only scripts are being used
            if (Program.m_LogAIDebug &&
                (currentTime - m_lastDecisionTime) > maxInactiveTime &&
                (currentTime - m_lastCombatAIReport) > maxInactiveTime &&
                m_lastDecisionTime > 0 && onlyUseScripts == false)
            {
                double timeTakenSoFar = (currentTime - m_lastDecisionTime);

                string logString = "At " + currentTime + " decision for " + theMob.Name + " " + theMob.ServerID +
                                   " has taken " + timeTakenSoFar +
                                   "s and not made a decision, currect action complete = " + m_currentActionComplete +
                                   ", waiting Till In Range = " + m_waitTillInRange + " Last " + m_lastDecisionString +
                                   ".";
                string mobString = theMob.GetCombatDebugString();
                Program.Display(logString);
                Program.Display(theMob.Name + " " + theMob.ServerID + " Combat Data = " + mobString + ".");

                //remember when the last report was given
                m_lastCombatAIReport = currentTime;
            }
        }

        private void CheckAttackRange(ServerControlledEntity theMob)
        {
            if (theMob.CurrentEnergy < m_minEnergyAttackSpell &&
                m_preferredCombatRange > theMob.CompiledStats.MaxAttackRange)
            {
                m_preferredCombatRange = theMob.CompiledStats.MaxAttackRange;
            }
        }
        
        private MobSkill GetAvailableSkillOfType(List<MobSkillSet> availableSets,
            MobSkillTable.Mob_Skill_Category theCategory, ServerControlledEntity theCaster, CombatEntity target)
        {
            //make a new copy of the list
            var validSets = new List<MobSkillSet>(availableSets);
            //until a decision has been made or the valid sets are exhausted
            while (validSets.Count > 0)
            {
                int totalWeight = 0;
                //calculate the total weight of the valid sets combined
                for (int currentSetIndex = 0; currentSetIndex < validSets.Count; currentSetIndex++)
                {
                    MobSkillSet currentSet = validSets[currentSetIndex];
                    totalWeight += currentSet.SkillSetTemplate.Weight;
                }

                //pick a value up to the total weight
                int randVal = Program.getRandomNumber(totalWeight);
                MobSkillSet chosenSet = null;
                //find the set
                int currentWeight = 0;
                for (int currentSetIndex = 0; currentSetIndex < validSets.Count && chosenSet == null; currentSetIndex++)
                {
                    MobSkillSet currentSet = validSets[currentSetIndex];
                    currentWeight += currentSet.SkillSetTemplate.Weight;
                    if (currentWeight >= randVal)
                    {
                        chosenSet = currentSet;
                    }
                }
                if (chosenSet != null)
                {
                    //get the skills available to the set of this type
                    var availableSkills = new List<MobSkillWeight>();

                    float distToMob = Utilities.Difference2D(theCaster.CurrentPosition.m_position,
                        target.CurrentPosition.m_position);
                    //remove the radius
                    distToMob = distToMob - (theCaster.Radius + target.Radius);
                    //if the target is the caster the distance is 0
                    if (theCaster == target)
                    {
                        distToMob = 0;
                    }

                    //pass this in to the skill selection
                    if ((theCaster.StatusPreventsActions.Move ||
                         (distToMob >= 0 || theCaster.BacktrackDist >= theCaster.MaxBacktrackDist)))
                    {
                        theCaster.SkillTable.AddSkillsToListOfTypeFromSet(availableSkills, chosenSet, theCategory,
                            distToMob, theCaster.CurrentEnergy, true, target);
                    }
                    //try to pick a skill
                    MobSkill skillToUse = MobSkillTable.GetSkillFromList(availableSkills);
                    //if there was no skill
                    if (skillToUse == null)
                    {
                        validSets.Remove(chosenSet);
                    }
                    else
                    {
                        return skillToUse;
                    }
                }
            }
            return null;
        }
        
        internal void DecisionMade()
        {
            if (Program.m_LogAIDebug)
            {
                string logString = "DecisionMade";
                Program.Display(logString);
            }
            m_currentActionComplete = false;
        }

        internal void ActionComplete()
        {
            if (Program.m_LogAIDebug)
            {
                string logString = "ActionComplete";
                Program.Display(logString);
            }
            m_currentActionComplete = true;
            m_waitTillInRange = false; //james
        }

        internal void BattleStarted(ServerControlledEntity theMob)
        {
            m_battleStartTime = Program.MainUpdateLoopStartTime();
        }

        public void BattleEnded(ServerControlledEntity theMob)
        {			
            m_oldScriptContainer.Clear();
            m_scriptContainers.Clear();
            SetNewAITemplate(theMob.Template.m_combatAITemplate, theMob);
        }
    }
}