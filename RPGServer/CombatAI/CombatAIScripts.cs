using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MainServer.Combat;
using XnaGeometry;
using MainServer.Localise;

namespace MainServer
{
    /// <summary>
    /// Combat AI Scripts
    /// A class that allows for unique behavior based on conditions
    /// Can be used with a AI template or mob template
    /// </summary>
    class CAI_Script
    {
        
        enum CONDITION_TYPES
        {
            /// <summary>
            /// what will cause this script to be carried out
            /// </summary>
            TRIGGER = 1,
            /// <summary>
            /// What conditions will cause the effect to fail
            /// meeting any fail condition will prevent completion
            /// </summary>
            FAIL = 2,
            /// <summary>
            /// what actions are to be performed once the script is triggered
            /// </summary>
            ACTION = 3,
            /// <summary>
            /// Affects how the data is cleared when the ai template is changed
            /// </summary>
            REPEAT = 4,
        }

        //an individual script ID
        int m_scriptID = -1;
        //what priority is this script
        int m_priority = -1;
        //what AI Type does it work with
        int m_templateID = -1;
        //what are the trigger conditions
        List<CAI_ScriptFragment> m_triggers = new List<CAI_ScriptFragment>();
        //what are the fail conditions
        List<CAI_ScriptFragment> m_failConditions = new List<CAI_ScriptFragment>();
        //what are the actions
        List<CAI_ScriptFragment> m_actions = new List<CAI_ScriptFragment>();
        //what are the repeat conditions
        List<CAI_ScriptFragment> m_repeatConditions = new List<CAI_ScriptFragment>();

        //a local system message to be sent when the script is completed
        string m_activationString = "";

        /// <summary>
        /// what are the trigger conditions
        /// </summary>
        internal List<CAI_ScriptFragment> Triggers
        {
            get { return m_triggers; }
        }
        /// <summary>
        /// what are the fail conditions
        /// </summary>
        internal List<CAI_ScriptFragment> FailConditions
        {
            get { return m_failConditions; }
        }
        /// <summary>
        /// what are the actions
        /// </summary>
        internal List<CAI_ScriptFragment> Actions
        {
            get { return m_actions; }
        }
        /// <summary>
        /// what are the repeat conditions
        /// </summary>
        internal List<CAI_ScriptFragment> RepeatConditions
        {
            get { return m_repeatConditions; }
        }
        internal int TemplateID
        {
            get { return m_templateID; }

        }
        internal int ScriptID
        {
            get { return m_scriptID; }

        }
        internal string ActivationString
        {
            get { return m_activationString; }
        }
        internal int Priority
        {
            get
            {
                return m_priority;
            }
        }
        internal CAI_Script(int scriptID, int aiTemplateID, string scriptString, string activationString, int priority)
        {
            m_scriptID = scriptID;
            m_templateID = aiTemplateID;
            m_activationString = activationString;
            m_priority = priority;
            //type|fragmentType^fragmentVal|fragmentType^fragmentVal;
            //break up the string
            string[] conditionSplit = scriptString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int currentConditionIndex = 0; currentConditionIndex < conditionSplit.Length; currentConditionIndex++)
            {

               
                string currentConditionString = conditionSplit[currentConditionIndex];
                //split it into fragments
                string[] fragmentArray = currentConditionString.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                if (fragmentArray.Length > 0)
                {
                    CONDITION_TYPES type = (CONDITION_TYPES)Int32.Parse(fragmentArray[0]);
                    List<CAI_ScriptFragment> activeList = GetListForType(type);
                    for (int currentFragmentIndex = 1; currentFragmentIndex < fragmentArray.Length && activeList != null; currentFragmentIndex++)
                    {
                        string currentFragment = fragmentArray[currentFragmentIndex];
                        string[] fragmentSplit = currentFragment.Split(new char[] { '^' }, StringSplitOptions.RemoveEmptyEntries);
                        //if (fragmentSplit.Length > 1)
                        {
                            CAI_ScriptFragment.Fragment_Type fragmentType = (CAI_ScriptFragment.Fragment_Type)Int32.Parse(fragmentSplit[0]);
                           
                            List<float> values = new List<float>();
                            for (int i = 1; i < fragmentSplit.Length; i++)
                            {
                                float value = float.Parse(fragmentSplit[i]);
                                values.Add(value);
                            }
                            CAI_ScriptFragment newFragment = new CAI_ScriptFragment(fragmentType, values);
                            activeList.Add(newFragment);
                        }
                        /*else if (fragmentSplit.Length > 0)
                        {
                            CAI_ScriptFragment.Fragment_Type fragmentType = (CAI_ScriptFragment.Fragment_Type)Int32.Parse(fragmentSplit[0]);
                            List<float> values = new List<float>();
                            CAI_ScriptFragment newFragment = new CAI_ScriptFragment(fragmentType, values);
                            activeList.Add(newFragment);
                        }*/

                    }
                }

            }
        }
        List<CAI_ScriptFragment> GetListForType(CONDITION_TYPES type)
        {
            switch (type)
            {
                case CONDITION_TYPES.TRIGGER:
                    {
                        return m_triggers;
                    }
                case CONDITION_TYPES.FAIL:
                    {
                        return m_failConditions;
                    }
                case CONDITION_TYPES.ACTION:
                    {
                        return m_actions;
                    }
                case CONDITION_TYPES.REPEAT:
                    {
                        return m_repeatConditions;
                    }
                default:
                    {
                        break;
                    }
            }
            return null;

        }
    };

    class CAI_ScriptFragment
    {
        internal enum Fragment_Type
        {
            /// <summary>
            /// percentage Health
            /// </summary>
            HEALTH_BELOW = 1,
            /// <summary>
            /// percentage Health
            /// </summary>
            HEALTH_ABOVE = 2,
            /// <summary>
            /// time in seconds 
            /// </summary>
            TIME_SINCE_LAST_CAST = 3,
            /// <summary>
            /// time in seconds 
            /// </summary>
            TIME_SINCE_BATTLE_START = 4,
            /// <summary>
            /// other script ID ^ count
            /// </summary>
            OTHER_SCRIPT_COUNT_ABOVE = 5,
            /// <summary>
            /// other script ID ^ time in seconds 
            /// </summary>
            OTHER_SCRIPT_TIME_SINCE_LAST_CAST = 6,
            /// <summary>
            /// other script ID ^ count
            /// </summary>
            OTHER_SCRIPT_COUNT_BELOW = 7,
            /// <summary>
            /// other script ID ^ time in seconds 
            /// </summary>
            OTHER_SCRIPT_TIME_SINCE_LAST_CAST_BELOW = 8,
            /// <summary>
            /// attack type(ID's in CombatManager.ATTACK_TYPE)(-1 if not specific)^
            /// skill ID (-1 if not specific)^
            /// amount of damage
            /// </summary>
            DAMAGE_OF_TYPE_GREATER_THAN = 9,
            /// <summary>
            /// attack type(ID's in CombatManager.ATTACK_TYPE)(-1 if not specific)^
            /// skill ID (-1 if not specific)^
            /// amount of damage
            /// </summary>
            DAMAGE_OF_TYPE_LESS_THAN = 10,
            /// <summary>
            ///  val 0 = Time in seconds
            /// </summary>
            TIME_SINCE_AI_TEMPLATE_CHANGED = 11,
            /// <summary>
            ///  val 0 =status effect ID
            /// </summary>
            STATUS_EFFECT_PRESENT_ON_SELF = 12,
            /// <summary>
            ///  val 0 =status effect ID
            /// </summary>
            STATUS_EFFECT_NOT_PRESENT_ON_SELF = 13,
            /// <summary>
            /// val 0 = skill ID
            /// </summary>
            SKILL_READY_FOR_USE = 14,
            //actions
            /// <summary>
            /// Template ID to change to
            /// </summary>
            CHANGE_ACTIVE_TEMPLATE = 101,
            /// <summary>
            /// skill ID
            /// </summary>
            USE_SKILL = 102,
            /// <summary>
            /// float 0-1 precent reduction
            /// </summary>
            AGGRO_WIPE=103,
            /// <summary>
            /// none
            /// </summary>
            CALL_FOR_HELP = 104,
            /// <summary>
            /// none
            /// </summary>
            STOP_ATTACKING_TARGET = 105,
            /// <summary>
            /// Will attemt to Cast a skill on the nth highest aggro in the aggro list
            /// val 0 = skill ID
            /// val 1 = n
            /// </summary>
            USE_SKILL_ON_Nth_HIGHEST_AGGRO = 106,
            /// <summary>
            /// Will attemt to Cast a skill on the nth closest entity form the main target
            /// val 0 = skill ID
            /// val 1 = n
            /// val 2 = max range from target
            /// </summary>
            USE_SKILL_ON_Nth_CLOSEST_TO_TARGET = 107,
            /// <summary>
            /// Will attemt to Cast a skill on the nth closest entity form the calling Mob
            /// val 0 = skill ID
            /// val 1 = n
            /// val 2 = max range from target
            /// </summary>
            USE_SKILL_ON_Nth_CLOSEST_TO_SELF= 108,
            /// <summary>
            /// Will attemt to Cast a skill on the nth closest mob of templateID to the mob
            /// val 0 = skill ID
            /// val 1 = n
            /// val 2 = templateID
            /// </summary>
            USE_SKILL_ON_Nth_CLOSEST_TEMPLATE_ID_TO_SELF = 109,
            /// <summary>
            /// val 0 = Status effect ID
            /// val 1 = Status effect Level
            /// val 2 = caster level
            /// </summary>
            INFLICT_STATUS_EFFECT_ON_SELF = 110,
            /// <summary>
            /// val 0 = Status effect ID
            /// </summary>
            REMOVE_STATUS_EFFECT_ON_SELF = 111,
            /// <summary>
            /// Casts a Skill On The mob reguardless of type
            /// val 0 = skill ID
            /// </summary>
            USE_SKILL_ON_SELF = 112,
            /// <summary>
            /// Will attemt to Cast a skill on the nth random entity form the calling Mob
            /// val 0 = skill ID
            /// val 1 = n (min, max)            
            /// </summary>
            USE_SKILL_ON_Nth_RANDOM_TEMPLATE_ID_TO_SELF = 113,

            /// <summary>
            /// Will attemt to Cast a skill on the nth highest aggro in the aggro list
            /// val 0 = skill ID
            /// val 1 = n
            /// </summary>
            USE_SKILL_ON_Nth_RANDOM_AGGRO = 114,
            /// <summary>
            /// Will attemt to Cast a skill on the nth random entity form the main target
            /// val 0 = skill ID
            /// val 1 = n
            /// val 2 = max range from target
            /// </summary>
            USE_SKILL_ON_Nth_RANDOM_TO_TARGET = 115,

            /// <summary>
            /// Will attemt to Cast a skill on the nth random entity form the calling Mob
            /// val 0 = skill ID
            /// val 1 = n
            /// val 2 = max range from target
            /// </summary>
            USE_SKILL_ON_Nth_RANDOM_TO_SELF = 116,
            
            /// <summary>
            /// Check if N mobs of a certaion type have spawned near us
            /// val 0 = mob ID
            /// val 1 = n count
            /// val 2 = max range from ourselves
            /// </summary>            
            MOB_COUNT_GREATER_THAN = 117,

            /// <summary>
            /// Find the Nth aggro to us, then check if a certain status effect is active
            /// val 0 = status effect id
            /// val 1 = n
            /// val 2 = range
            /// </summary>
            STATUS_EFFECT_PRESENT_ON_NTH_AGGRO_TO_TARGET = 118,
            
            //fail Condidtions
            /// <summary>
            /// count
            /// </summary>
            COUNT_GREATER_THAN = 201,
            //Repeat Conditions
            /// <summary>
            /// none
            /// </summary>
            DOES_NOT_RESET = 301

        }

        /// <summary>
        /// what type of condition/action is it
        /// </summary>
        Fragment_Type m_type;
        /// <summary>
        /// what value(if any) is associated with it
        /// </summary>
        List<float> m_values;

        /// <summary>
        /// what type of condition/action is it
        /// </summary>
        internal Fragment_Type Type
        {
            get { return m_type; }
        }
        /// <summary>
        /// what value(if any) is associated with it
        /// </summary>
        internal List<float> Value
        {
            get { return m_values; }
        }
        
        internal CAI_ScriptFragment(Fragment_Type type, List<float> values)
        {
            m_type = type;
            m_values = values;

        }
    }

    class CAI_ScriptContainer
    {
		// #localisation
		public class CAI_ScriptContainerTextDB : TextEnumDB
		{
			public CAI_ScriptContainerTextDB() : base(nameof(CAI_ScriptContainer), typeof(TextID)) { }

			public enum TextID
			{
				NO_ONE_IN_PARTICULAR,   //"no one in particular"
			}
		}
		public static CAI_ScriptContainerTextDB textDB = new CAI_ScriptContainerTextDB();

		//the contained script
		CAI_Script m_containedScript = null;
        
        //number of times the script has triggered
        int m_triggerCount = 0;
        //time the script was last triggered
        double m_lastTriggerTime = 0;

        internal CAI_Script Script
        {
            get { return m_containedScript; }

        }

        internal CAI_ScriptContainer(CAI_Script m_script)
        {
            m_containedScript = m_script;
        }

        bool TriggerFragmentPassesCondition(CAI_ScriptFragment currentFragment, ServerControlledEntity theMob, double currentTime)
        {
            bool passesFragmentConditions = true;
            if (currentFragment != null)
            {
                switch (currentFragment.Type)
                {
                    case CAI_ScriptFragment.Fragment_Type.HEALTH_BELOW:
                        {
                            if (theMob.PercentHealth >= currentFragment.Value[0])
                            {
                                passesFragmentConditions = false;
                            }
                            break;
                        }
                    case CAI_ScriptFragment.Fragment_Type.HEALTH_ABOVE:
                        {
                            if (theMob.PercentHealth <= currentFragment.Value[0])
                            {
                                passesFragmentConditions = false;
                            }
                            break;
                        }
                    case CAI_ScriptFragment.Fragment_Type.TIME_SINCE_LAST_CAST:
                        {
                            if (currentTime < m_lastTriggerTime + currentFragment.Value[0])
                            {
                                passesFragmentConditions = false;
                            }
                            break;
                        }

                    case CAI_ScriptFragment.Fragment_Type.TIME_SINCE_BATTLE_START:
                        {
                            if (currentTime < theMob.MobCombatAI.BattleStartTime + currentFragment.Value[0])
                            {
                                passesFragmentConditions = false;
                            }
                            break;
                        }
                    case CAI_ScriptFragment.Fragment_Type.TIME_SINCE_AI_TEMPLATE_CHANGED:
                        {
                            if (currentTime < theMob.MobCombatAI.AITemplateLastChangedTime + currentFragment.Value[0])
                            {
                                passesFragmentConditions = false;
                            }
                            break;
                        }
                    case CAI_ScriptFragment.Fragment_Type.OTHER_SCRIPT_COUNT_ABOVE:
                        {
                            #region OTHER_SCRIPT_COUNT_ABOVE

                            //this condition needs 2 values
                            if (currentFragment.Value.Count() >= 2)
                            {
                                int otherScriptID = (int)currentFragment.Value[0];
                                int count = (int)currentFragment.Value[1];
                                //try to find the other script
                                CAI_ScriptContainer otherScript = theMob.MobCombatAI.GetScript(otherScriptID);

                                if (otherScript == null)
                                {
                                    passesFragmentConditions = false;
                                }
                                else
                                {
                                    if (otherScript.m_triggerCount <= count)
                                    {
                                        passesFragmentConditions = false;
                                    }
                                }
                            }
                            break;

                            #endregion
                        }
                    case CAI_ScriptFragment.Fragment_Type.OTHER_SCRIPT_COUNT_BELOW:
                        {
                            #region OTHER_SCRIPT_COUNT_BELOW

                            //this condition needs 2 values
                            if (currentFragment.Value.Count() >= 2)
                            {
                                int otherScriptID = (int)currentFragment.Value[0];
                                int count = (int)currentFragment.Value[1];
                                //try to find the other script
                                CAI_ScriptContainer otherScript = theMob.MobCombatAI.GetScript(otherScriptID);

                                if (otherScript == null)
                                {
                                    passesFragmentConditions = false;
                                }
                                else
                                {
                                    if (otherScript.m_triggerCount >= count)
                                    {
                                        passesFragmentConditions = false;
                                    }
                                }
                            }
                            break;
                            #endregion
                        }
                    case CAI_ScriptFragment.Fragment_Type.OTHER_SCRIPT_TIME_SINCE_LAST_CAST:
                        {
                            #region OTHER_SCRIPT_TIME_SINCE_LAST_CAST
                            //this condition needs 2 values
                            if (currentFragment.Value.Count() >= 2)
                            {
                                int otherScriptID = (int)currentFragment.Value[0];
                                double time = (double)currentFragment.Value[1];
                                //try to find the other script
                                CAI_ScriptContainer otherScript = theMob.MobCombatAI.GetScript(otherScriptID);

                                if (otherScript == null)
                                {
                                    passesFragmentConditions = false;
                                }
                                else
                                {
                                    if (currentTime < otherScript.m_lastTriggerTime + time)
                                    {
                                        passesFragmentConditions = false;
                                    }
                                }
                            }
                            break;
                            #endregion
                        }
                    case CAI_ScriptFragment.Fragment_Type.OTHER_SCRIPT_TIME_SINCE_LAST_CAST_BELOW:
                        {
                            #region OTHER_SCRIPT_TIME_SINCE_LAST_CAST_BELOW

                            //this condition needs 2 values
                            if (currentFragment.Value.Count() >= 2)
                            {
                                int otherScriptID = (int)currentFragment.Value[0];
                                double time = (double)currentFragment.Value[1];
                                //try to find the other script
                                CAI_ScriptContainer otherScript = theMob.MobCombatAI.GetScript(otherScriptID);

                                if (otherScript == null)
                                {
                                    passesFragmentConditions = false;
                                }
                                else
                                {
                                    if (currentTime >= otherScript.m_lastTriggerTime + time)
                                    {
                                        passesFragmentConditions = false;
                                    }
                                }
                            }
                            else
                            {
                                passesFragmentConditions = false;
                            }
                            break;
                            #endregion
                        }
                    case CAI_ScriptFragment.Fragment_Type.DAMAGE_OF_TYPE_GREATER_THAN:
                        {
                            #region DAMAGE_OF_TYPE_GREATER_THAN
                            //this condition needs 3 values
                            if (currentFragment.Value.Count() >= 3)
                            {
                                int attackTypeID = (int)currentFragment.Value[0];
                                int skillID = (int)currentFragment.Value[1];
                                int damage = (int)currentFragment.Value[2];
                                bool foundPassingDamage = false;
                                for (int i = 0; i < theMob.RecentDamages.Count; i++)
                                {
                                    CombatDamageMessageData damageData = theMob.RecentDamages[i];
                                    //check it's the right type
                                    if (damageData == null)
                                    {
                                        continue;
                                    }
                                    if ((attackTypeID < 0 || attackTypeID == damageData.AttackType) &&
                                        (skillID < 0 || skillID == damageData.SkillID) &&
                                        (damageData.DamageTaken > damage))
                                    {
                                        foundPassingDamage = true;
                                    }
                                }
                                passesFragmentConditions = foundPassingDamage;

                            }
                            else
                            {
                                passesFragmentConditions = false;
                            }
                            break;
                            #endregion
                        }
                    case CAI_ScriptFragment.Fragment_Type.DAMAGE_OF_TYPE_LESS_THAN:
                        {
                            #region DAMAGE_OF_TYPE_LESS_THAN
                            //this condition needs 3 values
                            if (currentFragment.Value.Count() >= 3)
                            {
                                int attackTypeID = (int)currentFragment.Value[0];
                                int skillID = (int)currentFragment.Value[1];
                                int damage = (int)currentFragment.Value[2];
                                bool foundPassingDamage = false;
                                for (int i = 0; i < theMob.RecentDamages.Count; i++)
                                {
                                    CombatDamageMessageData damageData = theMob.RecentDamages[i];
                                    //check it's the right type
                                    if (damageData == null)
                                    {
                                        continue;
                                    }
                                    if ((attackTypeID < 0 || attackTypeID == damageData.AttackType) &&
                                          (skillID < 0 || skillID == damageData.SkillID) &&
                                          (damageData.DamageTaken > damage))
                                    {
                                        foundPassingDamage = true;
                                    }
                                }
                                passesFragmentConditions = foundPassingDamage;

                            }
                            else
                            {
                                passesFragmentConditions = false;
                            }
                            break;
                            #endregion
                        }
                    case CAI_ScriptFragment.Fragment_Type.COUNT_GREATER_THAN:
                        {
                            if (m_triggerCount <= (int)currentFragment.Value[0])
                            {
                                passesFragmentConditions = false;
                            }
                            break;
                        }
                    case CAI_ScriptFragment.Fragment_Type.STATUS_EFFECT_PRESENT_ON_SELF:
                        {
                            int statusEffectID = (int)currentFragment.Value[0];
                            if (theMob.GetStatusEffectForID((EFFECT_ID)statusEffectID) == null)
                            {
                                passesFragmentConditions = false;
                            }
                            break;
                        }
                    case CAI_ScriptFragment.Fragment_Type.STATUS_EFFECT_NOT_PRESENT_ON_SELF:
                        {
                            int statusEffectID = (int)currentFragment.Value[0];
                            if (theMob.GetStatusEffectForID((EFFECT_ID)statusEffectID) != null)
                            {
                                passesFragmentConditions = false;
                            }
                            break;
                        }
                    case CAI_ScriptFragment.Fragment_Type.SKILL_READY_FOR_USE:
                        {
                            #region SKILL_READY_FOR_USE
                            int skillID = (int)currentFragment.Value[0];

                            EntitySkill theSkill = theMob.GetEnitySkillForID((SKILL_TYPE)skillID, false);
                            if (theSkill == null)
                            {
                                passesFragmentConditions = false;
                            }
                            else
                            {
                                //get the time since this was last cast
                                double timeSinceLastCast = Program.MainUpdateLoopStartTime() - theSkill.TimeLastCast;
                                SkillTemplateLevel skillLevel = theSkill.getSkillTemplateLevel(false);
                                double rechargeTime = 0;

                                if (skillLevel != null)
                                {
                                    rechargeTime = skillLevel.GetRechargeTime(theSkill, false);

                                }
                                if (skillLevel == null || rechargeTime > timeSinceLastCast)
                                {
                                    passesFragmentConditions = false;
                                }
                            }


                            break;
                            #endregion
                        }
                    case CAI_ScriptFragment.Fragment_Type.MOB_COUNT_GREATER_THAN:
                        {
                            #region MOB_COUNT_GREATER_THAN

                            //check paramaters
                            if (currentFragment.Value.Count != 3)
                            {
                                Program.Display("CombatAiScrip.MOB_COUNT_GREATER_THAN.incorrect values");
                                return false;
                            }

                            // mob id, count, range
                            int mobTemplateID = (int)currentFragment.Value[0];
                            int count = (int)currentFragment.Value[1];
                            float range = currentFragment.Value[2];

                            if (mobTemplateID < 0)
                                return false;

                            //first get everyone in range
                            List<ServerControlledEntity> mobs = new List<ServerControlledEntity>();
                            if (theMob.CurrentPartition != null)
                            {
                                theMob.CurrentPartition.AddMobsInRangeToList(theMob, theMob.CurrentPosition.m_position, range, mobs,
                                    MainServer.partitioning.ZonePartition.ENTITY_TYPE.ET_MOB, theMob);
                            }

                            // cound how many match our template id, and return true if equal or above
                            int found = mobs.FindAll(x => x.Template.m_templateID == mobTemplateID).Count;
                            if (found >= count)
                            {
                                passesFragmentConditions = true;
                                break;
                            }

                            //default to false
                            passesFragmentConditions = false;
                            break;
                            #endregion
                        }
                    case CAI_ScriptFragment.Fragment_Type.STATUS_EFFECT_PRESENT_ON_NTH_AGGRO_TO_TARGET:
                        {
                            #region STATUS_EFFECT_PRESENT_ON_NTH_AGGRO_TO_TARGET

                            // 0 stattus effect id
                            // 1 nth
                            // 2 range

                            //check paramaters
                            if (currentFragment.Value.Count != 3)
                            {
                                Program.Display("CombatAiScrip.STATUS_EFFECT_PRESENT_ON_NTH_AGGRO_TO_TARGET.incorrect values");
                                return false;
                            }

                            // mob id, count, range
                            int statusEffectId = (int)currentFragment.Value[0];
                            int n = (int)currentFragment.Value[1];
                            float range = currentFragment.Value[2];

                            // move n into index range, i.e. 1 should be [0]
                            n -= 1;

                            if (statusEffectId < 0)
                                return false;

                            //try to find the target                            
                            CombatEntity target = theMob.GetNthHighestAggro(n, theMob.CurrentPosition, range);

                            // fail it no target in range
                            if (target == null)
                            {
                                passesFragmentConditions = false;
                                break;
                            }

                            // check status effect on target                            
                            if (target.GetStatusEffectForID((EFFECT_ID)statusEffectId) != null)
                            {
                                passesFragmentConditions = true;
                            }
                            else
                            {
                                passesFragmentConditions = false;
                            }

                            break;
                            #endregion
                        }
                    default:
                        {
                            passesFragmentConditions = false;
                            break;
                        }
                }
            }

            //return
            return passesFragmentConditions;
        }

        internal bool PassesTriggerConditions(ServerControlledEntity theMob, double currentTime)
        {
            bool passesTriggerConditions = true;

            for (int currentTriggerIndex = 0; passesTriggerConditions == true && currentTriggerIndex < m_containedScript.Triggers.Count; currentTriggerIndex++)
            {
                CAI_ScriptFragment currentTrigger = m_containedScript.Triggers[currentTriggerIndex];
                if (currentTrigger != null)
                {
                    passesTriggerConditions = TriggerFragmentPassesCondition(currentTrigger, theMob, currentTime);                   
                }
            }

            return passesTriggerConditions;
        }

        internal bool PassesFailConditions(ServerControlledEntity theMob, double currentTime)
        {
            bool passesConditions = true;
            for (int currentIndex = 0; currentIndex < m_containedScript.FailConditions.Count && passesConditions == true; currentIndex++)
            {
                CAI_ScriptFragment currentFragment = m_containedScript.FailConditions[currentIndex];

                if (currentFragment != null)
                {
                    // for the script to continue no fail trigger condition can be met
                    passesConditions = !TriggerFragmentPassesCondition(currentFragment, theMob, currentTime);
                   /* switch (currentFragment.Type)
                    {
                        case CAI_ScriptFragment.Fragment_Type.HEALTH_ABOVE:
                            {
                                if (theMob.PercentHealth > currentFragment.Value[0])
                                {
                                    passesConditions = false;
                                }
                                break;
                            }
                        case CAI_ScriptFragment.Fragment_Type.HEALTH_BELOW:
                            {
                                if (theMob.PercentHealth < currentFragment.Value[0])
                                {
                                    passesConditions = false;
                                }
                                break;
                            }
                        case CAI_ScriptFragment.Fragment_Type.COUNT_GREATER_THAN:
                            {
                                if (m_triggerCount >= (int)currentFragment.Value[0])
                                {
                                    passesConditions = false;
                                }
                                break;
                            }
                        case CAI_ScriptFragment.Fragment_Type.TIME_SINCE_LAST_CAST:
                            {
                                if (m_triggerCount>0 && currentTime > m_lastTriggerTime + currentFragment.Value[0])
                                {
                                    passesConditions = false;
                                }
                                break;
                            }
                        case CAI_ScriptFragment.Fragment_Type.TIME_SINCE_BATTLE_START:
                            {
                                if (currentTime > theMob.MobCombatAI.BattleStartTime + currentFragment.Value[0])
                                {
                                    passesConditions = false;
                                }
                                break;
                            }
                        case CAI_ScriptFragment.Fragment_Type.OTHER_SCRIPT_COUNT_ABOVE:
                            {
                                //this condition needs 2 values
                                if (currentFragment.Value.Count() >= 2)
                                {
                                    int otherScriptID = (int)currentFragment.Value[0];
                                    int count = (int)currentFragment.Value[1];
                                    //try to find the other script
                                    CAI_ScriptContainer otherScript = theMob.MobCombatAI.GetScript(otherScriptID);

                                    if (otherScript != null)
                                    {
                                        if (otherScript.m_triggerCount > count)
                                        {
                                            passesConditions = false;
                                        }
                                    }
                                }
                                break;
                            }
                        case CAI_ScriptFragment.Fragment_Type.OTHER_SCRIPT_COUNT_BELOW:
                            {
                                //this condition needs 2 values
                                if (currentFragment.Value.Count() >= 2)
                                {
                                    int otherScriptID = (int)currentFragment.Value[0];
                                    int count = (int)currentFragment.Value[1];
                                    //try to find the other script
                                    CAI_ScriptContainer otherScript = theMob.MobCombatAI.GetScript(otherScriptID);

                                    if (otherScript != null)
                                    {
                                        if (otherScript.m_triggerCount < count)
                                        {
                                            passesConditions = false;
                                        }
                                    }
                                }
                                break;
                            }
                        case CAI_ScriptFragment.Fragment_Type.OTHER_SCRIPT_TIME_SINCE_LAST_CAST:
                            {
                                //this condition needs 2 values
                                if (currentFragment.Value.Count() >= 2)
                                {
                                    int otherScriptID = (int)currentFragment.Value[0];
                                    double time = (double)currentFragment.Value[1];
                                    //try to find the other script
                                    CAI_ScriptContainer otherScript = theMob.MobCombatAI.GetScript(otherScriptID);

                                    if (otherScript != null)
                                    {
                                        if (currentTime > otherScript.m_lastTriggerTime + time)
                                        {
                                            passesConditions = false;
                                        }
                                    }
                                }
                                break;
                            }
                        case CAI_ScriptFragment.Fragment_Type.OTHER_SCRIPT_TIME_SINCE_LAST_CAST_BELOW:
                            {
                                //this condition needs 2 values
                                if (currentFragment.Value.Count() >= 2)
                                {
                                    int otherScriptID = (int)currentFragment.Value[0];
                                    double time = (double)currentFragment.Value[1];
                                    //try to find the other script
                                    CAI_ScriptContainer otherScript = theMob.MobCombatAI.GetScript(otherScriptID);

                                    if (otherScript != null)
                                    {
                                        if (currentTime < otherScript.m_lastTriggerTime + time)
                                        {
                                            passesConditions = false;
                                        }
                                    }
                                }
                                break;
                            }
                        default:
                            {
                                break;
                            }

                    }*/

                }

            }
            return passesConditions;
        }

        void SortEntitiesFromPosition(Vector3 position, List<CombatEntity> oldList, List<CombatEntity>newList, float maxRangeSqr)
        {

            //go through the old List
            for (int oldListIndex = 0; oldListIndex < oldList.Count; oldListIndex++)
            {
                CombatEntity oldListEntity = oldList[oldListIndex];
                if (oldListEntity == null)
                {
                    continue;
                }
                //find the distance
                double oldListDistSQR = Utilities.Difference2DSquared(oldListEntity.CurrentPosition.m_position, position); 

                //if it's not too far away then add it to the new list
                if (maxRangeSqr > oldListDistSQR)
                {
                    bool added = false;
                    //for all the items in the new list (or until it has been added)
                    for (int newListIndex = 0; newListIndex < newList.Count&& added == false; newListIndex++)
                    {
                        CombatEntity newListEntity = newList[newListIndex];
                        if (newListEntity == null)
                        {
                            continue;
                        }
                        //check the distance
                        double newListDistSQR = Utilities.Difference2DSquared(newListEntity.CurrentPosition.m_position, position); 

                        // if the old list distance is lower than the newList distance
                        if (newListDistSQR > oldListDistSQR)
                        {
                            //insert the old List value here
                            newList.Insert(newListIndex, oldListEntity);
                            added = true;
                        }
                    }
                    if (added == false)
                    {
                        newList.Add(oldListEntity);
                    }
                }
            }

        }

        /// <summary>
        /// Go through the mobs ai_action scripts and apply them sequentially. 
        /// </summary>
        /// <param name="theMob"></param>
        internal void TakeAction(ServerControlledEntity theMob)
        {
            //carry out all actions
            for (int currentActionIndex = 0; currentActionIndex < m_containedScript.Actions.Count; currentActionIndex++)
            {
                CAI_ScriptFragment currentAction = m_containedScript.Actions[currentActionIndex];
                if (currentAction != null)
                {
                    //what sort of action is it
                    switch (currentAction.Type)
                    {
                        //Change ai type
                        case CAI_ScriptFragment.Fragment_Type.CHANGE_ACTIVE_TEMPLATE:
                            {
                                #region CHANGE_ACTIVE_TEMPLATE
                                //what type is it to change to
                                int templateID = (int)currentAction.Value[0];
                                //try to find the correct type
                                CombatAITemplate theTemplate = CombatAITemplateManager.GetItemForID(templateID);
                                //if you found it then change ai type
                                if (theTemplate != null)
                                {
                                    theMob.ChangeAITemplate(theTemplate);
                                }
                                break;
                                #endregion
                            }
                        case CAI_ScriptFragment.Fragment_Type.USE_SKILL:
                            {
                                #region USE_SKILL
                                //what skill is it
                                SKILL_TYPE skillID = (SKILL_TYPE)currentAction.Value[0];
                                //try to find the skill
                                MobSkill theSkill = theMob.SkillTable.GetSkillForID(skillID);
                                CombatEntity target = theMob.MobCombatAI.GetMainTarget(theMob);
                               //if the skill exists try to cast the skill
                                if (theSkill != null&&target!=null)
                                {
                                    int targetError = SkillTemplate.CheckSkillForUseAgainst(target, theMob, theSkill.TheSkill.Template.CastTargetGroup);

                                    if (targetError != (int)SkillTemplate.SkillTargetError.NoError)
                                    {
                                        target = theMob;
                                        targetError = SkillTemplate.CheckSkillForUseAgainst(target, theMob, theSkill.TheSkill.Template.CastTargetGroup);

                                    }
                                   
                                    if (targetError == (int)SkillTemplate.SkillTargetError.NoError)
                                    {
                                        theMob.UseSkill(theSkill, target);
                                        if (theMob.NextSkill != null)
                                        {
                                            theMob.MobCombatAI.DecisionMade();
                                        }
                                    }
                                }
                                break;
                                #endregion
                            }
                        case CAI_ScriptFragment.Fragment_Type.USE_SKILL_ON_SELF:
                            {
                                #region USE_SKILL_ON_SELF
                                //what skill is it
                                SKILL_TYPE skillID = (SKILL_TYPE)currentAction.Value[0];
                                //try to find the skill
                                MobSkill theSkill = theMob.SkillTable.GetSkillForID(skillID);
                                CombatEntity target = theMob;
                                //if the skill exists try to cast the skill
                                if (theSkill != null && target != null)
                                {
                                    theMob.UseSkill(theSkill, target);
                                    if (theMob.NextSkill != null)
                                    {
                                        theMob.MobCombatAI.DecisionMade();
                                    }
                                    
                                }
                                break;
                                #endregion
                            }
                        case (CAI_ScriptFragment.Fragment_Type.USE_SKILL_ON_Nth_HIGHEST_AGGRO):
                        case (CAI_ScriptFragment.Fragment_Type.USE_SKILL_ON_Nth_RANDOM_AGGRO):
                            {
                                #region USE_SKILL_ON_Nth_highest/random_AGGRO
                                SKILL_TYPE skillID = (SKILL_TYPE)currentAction.Value[0];
                                int numberInAggro = 0;
                                if (currentAction.Value.Count > 1)
                                {
                                    numberInAggro = (int)currentAction.Value[1];
                                }
                                //n will be one for closest, change so 0 is closest
                                numberInAggro--;
                                if (numberInAggro < 0)
                                {
                                    return;
                                }
                                float rangeFromTarget = 0;

                                if (currentAction.Value.Count > 2)
                                {
                                    rangeFromTarget = currentAction.Value[2];
                                }
                                //try to find the skill
                                MobSkill theSkill = theMob.SkillTable.GetSkillForID(skillID);
                                CombatEntity target = null;

                                // get Nth highest or random here
                                if (currentAction.Type == CAI_ScriptFragment.Fragment_Type.USE_SKILL_ON_Nth_HIGHEST_AGGRO)
                                    target = theMob.GetNthHighestAggro(numberInAggro, theMob.CurrentPosition, rangeFromTarget);
                                if (currentAction.Type == CAI_ScriptFragment.Fragment_Type.USE_SKILL_ON_Nth_HIGHEST_AGGRO)
                                    target = theMob.GetNthRandomAggro(numberInAggro, theMob.CurrentPosition, rangeFromTarget);

                                //if the skill exists try to cast the skill
                                if (theSkill != null && target != null)
                                {
                                    int targetError = SkillTemplate.CheckSkillForUseAgainst(target, theMob, theSkill.TheSkill.Template.CastTargetGroup);

                                    if (targetError != (int)SkillTemplate.SkillTargetError.NoError)
                                    {
                                        target = theMob;
                                        targetError = SkillTemplate.CheckSkillForUseAgainst(target, theMob, theSkill.TheSkill.Template.CastTargetGroup);

                                    }

                                    if (targetError == (int)SkillTemplate.SkillTargetError.NoError)
                                    {
                                        theMob.UseSkill(theSkill, target);
                                        if (theMob.NextSkill != null)
                                        {
                                            theMob.MobCombatAI.DecisionMade();
                                        }
                                    }
                                }

                                break;
                                #endregion
                            }
                        case (CAI_ScriptFragment.Fragment_Type.USE_SKILL_ON_Nth_CLOSEST_TO_TARGET):
                        case (CAI_ScriptFragment.Fragment_Type.USE_SKILL_ON_Nth_CLOSEST_TO_SELF):
                        case (CAI_ScriptFragment.Fragment_Type.USE_SKILL_ON_Nth_RANDOM_TO_TARGET):
                        case (CAI_ScriptFragment.Fragment_Type.USE_SKILL_ON_Nth_RANDOM_TO_SELF):
                            {
                                #region USE_SKILL_ON_Nth_closest/random_TO_target/self
                                //what skill is it
                                SKILL_TYPE skillID = (SKILL_TYPE)currentAction.Value[0];
                                int numberFromTarget = 0;
                                if (currentAction.Value.Count > 1)
                                {
                                    numberFromTarget = (int)currentAction.Value[1];
                                }
                                //n will be one for closest, change so 0 is closest
                                numberFromTarget--;
                                if (numberFromTarget < 0)
                                {
                                    break;
                                }
                                float rangeFromTarget = 0;

                                if (currentAction.Value.Count > 2)
                                {
                                    rangeFromTarget = currentAction.Value[2];
                                }
                                //try to find the skill
                                MobSkill theSkill = theMob.SkillTable.GetSkillForID(skillID);
                                CombatEntity target = theMob.MobCombatAI.GetMainTarget(theMob);
                                //if the skill exists try to cast the skill
                                if (theSkill != null && target != null)
                                {

                                    //How far from the caster can the target be
                                    float range = theSkill.TheSkill.Template.Range;
                                    //check if it's a friendly skill

                                    int targetError = SkillTemplate.CheckSkillForUseAgainst(theMob, theMob, theSkill.TheSkill.Template.CastTargetGroup);
                                    bool friendlySkill = (targetError == (int)SkillTemplate.SkillTargetError.NoError);

                                    //get everyone in range
                                    List<CombatEntity> nearbyEntities = new List<CombatEntity>();
                                    if (friendlySkill == true)
                                    {
                                        if (theMob.CurrentPartition != null)
                                        {
                                            theMob.CurrentPartition.AddEntitiesInRangeToList(theMob, theMob.CurrentPosition.m_position, range, nearbyEntities, MainServer.partitioning.ZonePartition.ENTITY_TYPE.ET_NOT_ENEMY, theMob);
                                        }
                                    }
                                    else
                                    {
                                        if (theMob.CurrentPartition != null)
                                        {
                                            theMob.CurrentPartition.AddEntitiesInRangeToList(theMob, theMob.CurrentPosition.m_position, range, nearbyEntities, MainServer.partitioning.ZonePartition.ENTITY_TYPE.ET_ENEMY, theMob);
                                        }
                                    }

                                    //if we're dealing with target use it's position otherwise use our own
                                    Vector3 position = target.CurrentPosition.m_position;
                                    if (currentAction.Type == CAI_ScriptFragment.Fragment_Type.USE_SKILL_ON_Nth_CLOSEST_TO_SELF ||
                                        currentAction.Type == CAI_ScriptFragment.Fragment_Type.USE_SKILL_ON_Nth_RANDOM_TO_SELF)
                                    {
                                        position = theMob.CurrentPosition.m_position;
                                    }

                                    List<CombatEntity> closestEntities = new List<CombatEntity>();
                                    SortEntitiesFromPosition(position, nearbyEntities, closestEntities, rangeFromTarget * rangeFromTarget);

                                    // are we looking for the Nth closest
                                    if (currentAction.Type == CAI_ScriptFragment.Fragment_Type.USE_SKILL_ON_Nth_CLOSEST_TO_SELF ||
                                        currentAction.Type == CAI_ScriptFragment.Fragment_Type.USE_SKILL_ON_Nth_CLOSEST_TO_TARGET)
                                    {
                                        if (closestEntities.Count > 0)
                                        {
                                            // if we don't have enough, just get the last
                                            CombatEntity skillTarget = closestEntities[numberFromTarget % closestEntities.Count];
                                            theMob.UseSkill(theSkill, skillTarget);
                                            if (theMob.NextSkill != null)
                                            {
                                                theMob.MobCombatAI.DecisionMade();
                                            }
                                        }
                                    }


                                    // getting a random selection
                                    if (currentAction.Type == CAI_ScriptFragment.Fragment_Type.USE_SKILL_ON_Nth_RANDOM_TO_SELF ||
                                        currentAction.Type == CAI_ScriptFragment.Fragment_Type.USE_SKILL_ON_Nth_RANDOM_TO_TARGET)
                                    {
                                        if (closestEntities.Count > 0)
                                        {
                                            CombatEntity skillTarget = closestEntities[Program.m_rand.Next(numberFromTarget % closestEntities.Count)];
                                            theMob.UseSkill(theSkill, skillTarget);
                                            if (theMob.NextSkill != null)
                                            {
                                                theMob.MobCombatAI.DecisionMade();
                                            }
                                        }
                                    }

                                }
                                break;
                                #endregion
                            }
                        
                        // very similar cases, only difference is near the end when we pick either a closes or a random value
                        case ( CAI_ScriptFragment.Fragment_Type.USE_SKILL_ON_Nth_CLOSEST_TEMPLATE_ID_TO_SELF):
                        case (CAI_ScriptFragment.Fragment_Type.USE_SKILL_ON_Nth_RANDOM_TEMPLATE_ID_TO_SELF):
                            {
                                #region USE_SKILL_ON_Nth_Cclosest/random_TEMPLATE_ID_TO_SELF

                                if (currentAction.Value.Count < 3)
                                {
                                    break;
                                }
                                //get the skill
                                //what skill is it
                                SKILL_TYPE skillID = (SKILL_TYPE)currentAction.Value[0];
                                int n = (int)currentAction.Value[1];

                                int templateID = (int)currentAction.Value[2];
                                //n will be one for closest
                                n--;
                                if (n < 0)
                                {
                                    break;
                                }
                                //try to find the skill
                                MobSkill theSkill = theMob.SkillTable.GetSkillForID(skillID);
                                CombatEntity target = theMob.MobCombatAI.GetMainTarget(theMob);
                                //if the skill exists try to cast the skill
                                if (theSkill != null && target != null)
                                {
                                    //How far from the caster can the target be
                                    float range = theSkill.TheSkill.Template.Range;
                                    //get all mobs in range
                                    List<ServerControlledEntity> mobs = new List<ServerControlledEntity>();
                                    if (theMob.CurrentPartition != null)
                                    {
                                        theMob.CurrentPartition.AddMobsInRangeToList(theMob, theMob.CurrentPosition.m_position, range, mobs, MainServer.partitioning.ZonePartition.ENTITY_TYPE.ET_MOB, theMob);
                                    }
                                    //filter out mobs that are not the correct type
                                    List<CombatEntity> unsortedList = new List<CombatEntity>();
                                    for (int currentMobIndex = mobs.Count - 1; currentMobIndex >= 0; currentMobIndex--)
                                    {
                                        ServerControlledEntity currentMob = mobs[currentMobIndex];
                                        if(currentMob!=null&&currentMob.Template.m_templateID==templateID)
                                        {
                                            unsortedList.Add(currentMob);
                                        }
                                    }
                                    //cast the skill on the correct target
                                    List<CombatEntity> sortedList = new List<CombatEntity>();
                                    SortEntitiesFromPosition(theMob.CurrentPosition.m_position, unsortedList, sortedList, range * range);
                                    target = null;

                                    //are we picking a random or the nth closest
                                    if (currentAction.Type == CAI_ScriptFragment.Fragment_Type.USE_SKILL_ON_Nth_CLOSEST_TEMPLATE_ID_TO_SELF)
                                    {
                                        int targetCount = sortedList.Count;
                                        if (targetCount > 0)
                                        {
                                            // this modulor operator effectively keeps the index in range
                                            int targetIndex = n % targetCount;
                                            if (targetIndex < targetCount)
                                            {
                                                target = sortedList[targetIndex];
                                            }
                                        }
                                    }

                                    //if we're picking at RANDOM
                                    if (currentAction.Type == CAI_ScriptFragment.Fragment_Type.USE_SKILL_ON_Nth_RANDOM_TEMPLATE_ID_TO_SELF)
                                    {
                                        int targetCount = sortedList.Count;
                                        if (targetCount > 0)
                                        {                                            
                                            // here pick randomly - use the modulo to keep in range 
                                            int targetIndex = Program.m_rand.Next(n % targetCount);                                            
                                            if (targetIndex < targetCount)
                                            {
                                                target = sortedList[targetIndex];
                                            }
                                        }
                                    }

                                    if (target != null)
                                    {

                                        theMob.UseSkill(theSkill, target);
                                        if (theMob.NextSkill != null)
                                        {
                                            theMob.MobCombatAI.DecisionMade();
                                        }
                                    }
                                }
                                break;

                                #endregion
                            } 
                            
                        case CAI_ScriptFragment.Fragment_Type.AGGRO_WIPE:
                            {
                                theMob.AggroReduction(currentAction.Value[0]);
                                break;
                            }
                        case CAI_ScriptFragment.Fragment_Type.CALL_FOR_HELP:
                            {
                                #region CALL_FOR_HELP

                                if (currentAction.Value.Count < 1)
                                {
                                    break;
                                }
                                
                                float range = currentAction.Value[0];
                                CombatEntity target = theMob.MobCombatAI.GetMainTarget(theMob);
                                if(target!=null)
                                {
                                    theMob.CurrentZone.RequestAssistance(theMob, target, range);
                                }
                                break;

                                #endregion
                            }
                            //stop attacking the target
                        case CAI_ScriptFragment.Fragment_Type.STOP_ATTACKING_TARGET:
                            {
                                CombatEntity target = theMob.AttackTarget;
                                if (target != null)
                                {
                                    theMob.TheCombatManager.StopAttacking(theMob);
                                }
                                break;
                            }
                        case CAI_ScriptFragment.Fragment_Type.INFLICT_STATUS_EFFECT_ON_SELF:
                            {
                                #region INFLICT_STATUS_EFFECT_ON_SELF

                                if (currentAction.Value.Count < 3)
                                {
                                    break;
                                }
                                int statusEffectID = (int)currentAction.Value[0];
                                int statusEffectLevel = (int)currentAction.Value[1];
                                int statusEffectCastAsLevel = (int)currentAction.Value[2];

                                CharacterEffectParams param = new CharacterEffectParams();
                                param.charEffectId = (EFFECT_ID)statusEffectID;
                                param.caster = null;
                                param.level = statusEffectLevel;
                                param.aggressive = false;
                                param.PVP = false;
                                param.statModifier = 0;
                                CharacterEffectManager.InflictNewCharacterEffect(param, theMob);

                                // Test if the status effect went off
                                CharacterEffect newEffect = param.QueryStatusEffect(param.charEffectId);
                                if (newEffect != null && newEffect.StatusEffect != null)
                                {
                                    newEffect.StatusEffect.CasterLevel = statusEffectCastAsLevel;                                    
                                }

                                break;
                                #endregion
                            }
                        case CAI_ScriptFragment.Fragment_Type.REMOVE_STATUS_EFFECT_ON_SELF:
                            {
                                #region REMOVE_STATUS_EFFECT_ON_SELF

                                if (currentAction.Value.Count < 1)
                                {
                                    break;
                                }
                                int statusEffectID = (int)currentAction.Value[0];
                                StatusEffect effectToRemove = theMob.GetStatusEffectForID((EFFECT_ID)statusEffectID);
                                if (effectToRemove != null)
                                {
                                    effectToRemove.Complete();
                                }

                                break;
                                #endregion

                            }
                        default:
                            {
                                break;
                            }
                    }
                }
            }
            if (m_containedScript.ActivationString.Length > 0)
            {
                PlayActivationString(theMob);
            }
            ScriptCarriedOut();
        }

        internal void PlayActivationString(ServerControlledEntity  theMob)
        {
            if (m_containedScript.ActivationString.Length > 0)
            {
				string mobName = "";
				string targetName = "";
				string stringToSend = "";

				CombatEntity mainTarget = theMob.MobCombatAI.GetMainTarget(theMob);

				if (mainTarget != null)
				{
					targetName = mainTarget.Name;
				}

				List<Player> nearbyPlayers = new List<Player>();
				theMob.CurrentZone.AddPlayersInRangeToList(theMob.CurrentPosition.m_position, Zone.LOCAL_MESSAGE_RANGE, nearbyPlayers);
				foreach (Player player in nearbyPlayers)
				{
					if (player != null && player.connection != null)
					{
						mobName = MobTemplateManager.GetLocaliseMobName(player, theMob.Template.m_templateID);

						if (mainTarget == null)
						{
							targetName = Localiser.GetString(textDB, player, (int)CAI_ScriptContainerTextDB.TextID.NO_ONE_IN_PARTICULAR);
						}

						stringToSend = CombatAITemplateManager.GetLocaliseActivationString(player, m_containedScript.ScriptID);
						stringToSend = stringToSend.Replace("<MN>", mobName);
						stringToSend = stringToSend.Replace("<TN>", targetName);

						Program.processor.sendSystemMessage("^o" + stringToSend + "^0", player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
					}
				}
			}
		}

        internal bool RemovesOnStateTransition()
        {
            for (int i = 0; i < m_containedScript.RepeatConditions.Count;i++ )
            {
                   CAI_ScriptFragment currentFragment = m_containedScript.RepeatConditions[i];
                   if (currentFragment != null)
                   {
                       switch (currentFragment.Type)
                       {
                           case CAI_ScriptFragment.Fragment_Type.DOES_NOT_RESET:
                               {
                                   return false;
                               }
                           default:
                               {
                                   break;
                               }
                       }
                   }

            }
            return true;
        }

        void ScriptCarriedOut()
        {
            m_triggerCount++;
            m_lastTriggerTime = Program.MainUpdateLoopStartTime();
        }

    }

}
