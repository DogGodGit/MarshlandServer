using System;
using System.Collections.Generic;
using Lidgren.Network;
using XnaGeometry;

namespace MainServer.Signposting
{
    /// <summary>
    /// Describes a game/player situation that can be described as true or false,
    /// describes type of check and variables to judge whether the check is true eg. type QuestComplete: var 109,
    /// Used to decide if the active signpost should be changed,
    /// condition checks can have different failure/pass levels to allow rechecks to be optimised to only recheck conditions that are likely to change,
    /// </summary>
    class SignpostCondition
    {
        #region enums
        /// <summary>
        /// How did the condition fail or pass
        /// </summary>
        internal enum PassType
        {
            /// <summary>
            /// The condition will always pass 
            /// </summary>
            CompletePass,
            /// <summary>
            /// The condition has passed and can not fail before a complete recheck
            /// </summary>
            PassesCondition,
            /// <summary>
            /// The condition currently passes but may fail due to movement or time
            /// </summary>
            CurrentPass,
            /// <summary>
            /// The condition currently fails but may pass due to movement or time
            /// </summary>
            CurrentFail,
            /// <summary>
            /// The condition has failed and can never pass before a complete recheck
            /// </summary>
            FailsCondition,
            /// <summary>
            /// This condition can never pass ()
            /// </summary>
            CompleteFail
        }
        /// <summary>
        /// How a condition should be judged as true
        /// all conditions must contain parsing and parameters structure in comments
        /// parsing structure to be added after database code has been done
        /// </summary>
        internal enum ConditionType
        {
            /// <summary>
            /// The condition has failed to be set
            /// </summary>
            None=0,
            /// <summary>
            /// Parameters: List of condition ID's that must all pass their condition
            /// InternalConditions:Links to each condition
            /// parsing structure: conditionID1;conditionID2;conditionID1
            /// </summary>
            And=1,
            /// <summary>
            /// Parameters: List of condition ID's that one must pass their condition
            /// InternalConditions:Links to each condition 
            /// </summary>
            Or=2,
            /// <summary>
            /// Parameters:zoneID
            /// </summary>
            InZone=3,
            /// <summary>
            /// Parameters:QuestID
            /// </summary>
            QuestAvailable=4,
            /// <summary>
            /// Parameters:QuestID,StageID
            /// </summary>
            OnQuestStage=5,
            /// <summary>
            /// Parameters:QuestID
            /// </summary>
            QuestComplete=6,
            /// <summary>
            /// Parameters:Level
            /// </summary>
            LevelGreaterThan=7,
            /// <summary>
            /// Parameters:TimeInSeconds
            /// </summary>
            TimeLoggedIn=8,
            /// <summary>
            /// Parameters:QuestID,StageID,TimeInSeconds
            /// </summary>
            TimeSinceQuestStageComplete=9,
            /// <summary>
            /// Parameters:QuestID,TimeInSeconds
            /// </summary>
            TimeSinceQuestComplete=10,
            /// <summary>
            /// Parameters:Range,PositionX,PositionY,PositionZ
            /// </summary>
            InRangeOfPosition=11,
            /// <summary>
            /// Parameters:numAttributePoints
            /// </summary>
            AttributePointsGreaterThan =12,
            /// <summary>
            /// Parameters:numAttributePoints
            /// </summary> 
            SkillPointsGreaterThan=13,
            /// <summary>
            /// Date in format YYYY/MM/DD HH:mm:ss
            /// </summary>
            DateGreaterThan = 14,
            /// <summary>
            /// type_check;number
            /// </summary>
            AccountNo = 15,
            /// <summary>
            /// no params
            /// </summary>
            CharacterIsDead = 16,
            /// <summary>
            /// numberDeaths, TimeInSeconds
            /// </summary>
            CharacterDeaths = 17,
            /// <summary>
            /// tutorial ID
            /// </summary>
            TutorialComplete = 18,

            FirstTimeComplete = 40,
        }
        enum AccountConditionsOptions
        {
            Even =1,
            GreaterThan=2
        }
        #endregion enums
        #region variables
        protected int m_conditionID = -1;
        /// <summary>
        /// The condition fails if the condition is true
        /// </summary>
        protected bool m_inverted = false;
        protected DateTime m_paramDate;

        /// <summary>
        /// The type of condition
        /// </summary>
        protected ConditionType m_conditionType = ConditionType.None;
        /// <summary>
        /// A list of float parameters
        /// depending on the contition type these values will be used in different ways
        /// </summary>
        protected List<float> m_parameters = null;
#endregion //variables
        #region properties

        /// <summary>
        /// The type of condition
        /// </summary>
        internal ConditionType Type
        {
            get { return m_conditionType; }
        }
        /// <summary>
        /// A list of float parameters
        /// depending on the contition type these values will be used in different ways
        /// </summary>
        internal List<float> Parameters
        {
            get { return m_parameters; }
        }
        internal int ConditionID
        {
            get { return m_conditionID; }
        }
        internal DateTime ParamDate
        {
            set { m_paramDate = value; }
        }
        #endregion //properties
        internal SignpostCondition(int conditionID,ConditionType conditionType,bool inverted,List<float> parameters)
        {
            m_conditionType = conditionType;
            m_inverted = inverted;
            m_parameters = parameters;
            m_conditionID=conditionID;
        }

        virtual internal PassType PassesConditions(Character playerCharacter, SignpostContainer resultContainer)
        {
            PassType result =  PassType.FailsCondition;

            switch (m_conditionType)
            {
                case ConditionType.QuestAvailable:
                    {
                        if (m_parameters.Count > 0)
                        {
                            int questID = (int)m_parameters[0];
                            if (playerCharacter.m_QuestManager.IsAvailable(questID))
                            {
                                result = PassType.PassesCondition;
                            }
                        }
                        else
                        {
                            return result;
                        }
                        break;
                    }
                case ConditionType.QuestComplete:
                    {
                        if (m_parameters.Count > 0)
                        {
                            int questID = (int)m_parameters[0];
                            if (playerCharacter.m_QuestManager.IsQuestComplete(questID))
                            {
                                result = PassType.CompletePass;
                            }
                        }
                        else
                        {
                            return result;
                        }
                        break;
                    }
                case ConditionType.OnQuestStage:
                    {
                        if (m_parameters.Count > 1)
                        {
                            int questID = (int)m_parameters[0];
                            int stageID = (int)m_parameters[1];
                            Quest currentQuest = playerCharacter.m_QuestManager.GetCurrentQuest(questID);
                            if (currentQuest !=null && currentQuest.m_QuestStages.Count>stageID)
                            {
                                QuestStage currentStage = currentQuest.m_QuestStages[stageID];
                                if (currentStage.m_completed == false && currentStage.IsAvailable() == true)
                                {
                                    result = PassType.PassesCondition;
                                }
                            }
                        }
                        else
                        {
                            return result;
                        }
                        break;
                    }
                case ConditionType.LevelGreaterThan:
                    {
                        if (m_parameters.Count > 0)
                        {
                            int level = (int)m_parameters[0];
                            if (playerCharacter.Level > level)
                            {
                                result = PassType.CompletePass;
                            }
                        }
                        else
                        {
                            return result;
                        }
                        break;
                    }
                case ConditionType.InZone:
                    {
                        if (m_parameters.Count > 0)
                        {
                            int zoneID = (int)m_parameters[0];
                            if (playerCharacter.CurrentZone != null && playerCharacter.CurrentZone.m_zone_id == zoneID)
                            {
                                result = PassType.PassesCondition;
                            }
                        }
                        else
                        {
                            return result;
                        }
                        break;
                    }
                case ConditionType.InRangeOfPosition:
                    {
                        if (m_parameters.Count > 3)
                        {
                            float range = m_parameters[0];
                            float positionX = m_parameters[1];
                            float positionY = m_parameters[2];
                            float positionZ = m_parameters[3];
                            Vector3 position = new Vector3(positionX, positionY, positionZ);
                            float rangeSqr = range*range;
                            double lengthSqr = Utilities.Difference2DSquared(playerCharacter.CurrentPosition.m_position, position);
                            if (rangeSqr > lengthSqr)
                            {
                                result = PassType.CurrentPass;
                            }
                            else
                            {
                                result = PassType.CurrentFail;
                            }
                        }
                        else
                        {
                            return result;
                        }
                        break;
                    }
                case ConditionType.TimeLoggedIn:
                    {
                        if (m_parameters.Count > 0)
                        {
                            double currentNetTime = NetTime.Now;
                            float timeInSeconds = m_parameters[0];
                            if ((playerCharacter.m_timeCharacterLoggedIn + timeInSeconds) < currentNetTime)
                            {
                                result = PassType.CompletePass;
                            }
                            else
                            {
                                result = PassType.CurrentFail;
                            }
                        }
                        else
                        {
                            return result;
                        }
                        break;
                    }
                case ConditionType.TimeSinceQuestComplete:
                    {
                        if (m_parameters.Count > 1)
                        {
                            double currentNetTime = NetTime.Now;
                            int questID = (int)m_parameters[0];
                            float timeInSeconds = m_parameters[1];

                            CharacterEvent questEvent = CharacterEvent.GetQuestComplete(playerCharacter.RecentEvents, questID);
                            if (questEvent != null)
                            {

                                if ((questEvent.ActionedNetTime + timeInSeconds) < currentNetTime)
                                {
                                    result = PassType.CompletePass;
                                }
                                else
                                {
                                    result = PassType.CurrentFail;
                                }
                            }
                            else
                            {
                                result = PassType.FailsCondition ;
                            }
                        }
                        else
                        {
                            return result;
                        }
                        break;
                    }
                case ConditionType.TimeSinceQuestStageComplete:
                    {
                        if (m_parameters.Count > 2)
                        {
                            double currentNetTime = NetTime.Now;
                            int questID = (int)m_parameters[0];
                            int stageID = (int)m_parameters[1];
                            float timeInSeconds = m_parameters[2];

                            CharacterEvent questEvent = CharacterEvent.GetQuestStageComplete(playerCharacter.RecentEvents, questID,stageID);
                            if (questEvent != null)
                            {

                                if ((questEvent.ActionedNetTime + timeInSeconds) < currentNetTime)
                                {
                                    result = PassType.CompletePass;
                                }
                                else
                                {
                                    result = PassType.CurrentFail;
                                }
                            }
                            else
                            {
                                result = PassType.FailsCondition;
                            }
                        }
                        else
                        {
                            return result;
                        }
                        break;
                    }
                case ConditionType.AttributePointsGreaterThan:
                    {
                        if (m_parameters.Count > 0)
                        {
                            int attributePoints = (int)m_parameters[0];
                            if (playerCharacter.CompiledStats.AttributePoints >= attributePoints)
                            {
                                result = PassType.PassesCondition;
                            }

                        }
                        else
                        {
                            return result;
                        }
                        break;
                    }
                case ConditionType.SkillPointsGreaterThan:
                    {
                        if (m_parameters.Count > 0)
                        {
                            int skillPoints = (int)m_parameters[0];
                            if (playerCharacter.CompiledStats.SkillPoints >= skillPoints)
                            {
                                result = PassType.PassesCondition;
                            }

                        }
                        else
                        {
                            return result;
                        }
                        break;
                    }
                case ConditionType.DateGreaterThan:
                    {
                        if (DateTime.Now > m_paramDate)
                        {
                            result = PassType.CompletePass;
                        }
                        else
                        {
                            result = PassType.CurrentFail;
                        }
                        
                        break;
                    }
                case ConditionType.AccountNo:
                    {
                        if (m_parameters.Count > 0)
                        {
                            AccountConditionsOptions checkType = (AccountConditionsOptions)m_parameters[0];
                           
                            switch(checkType){
                                case AccountConditionsOptions.Even:
                                    {
                                        if (playerCharacter.m_player.m_account_id % 2 == 0)
                                        {
                                            result = PassType.CompletePass;
                                        }
                                        else
                                        {
                                            result = PassType.CompleteFail;
                                        }
                                        break;
                                    }

                                case AccountConditionsOptions.GreaterThan:
                                    {
                                        if (m_parameters.Count > 1)
                                        {
                                            int param = (int)m_parameters[1];
                                            if (playerCharacter.m_player.m_account_id >param)
                                            {
                                                result = PassType.CompletePass;
                                            }
                                            else
                                            {
                                                result = PassType.CompleteFail;
                                            }

                                        }
                                        else
                                        {
                                            return result;
                                        }
                                        break;
                                    }
                            }
                        }
                        else
                        {
                            return result;
                        }
                        break;
                    }
                case ConditionType.CharacterIsDead:
                    {
                        if (playerCharacter.Dead == true)
                        {
                            result = PassType.CurrentPass;
                        }
                        else
                        {
                            result = PassType.CurrentFail;
                        }
                        break;
                    }
                case ConditionType.CharacterDeaths:
                    {
                        if (m_parameters.Count > 1)
                        {
                            int numberOfDeathsReq = (int)m_parameters[0];
                            float timeInSeconds = m_parameters[1];
                            int numberOfDeaths = CharacterEvent.GetNumDeathsForTime(playerCharacter.RecentEvents, timeInSeconds);

                            if (numberOfDeaths >= numberOfDeathsReq)
                            {
                                result = PassType.CurrentPass;
                            }
                            else
                            {
                                result = PassType.CurrentFail;
                            }

                        }
                        else
                        {
                            result = PassType.CurrentFail;
                        }
                        break;
                    }

                case ConditionType.TutorialComplete:
                    {
                        if (m_parameters.Count > 0)
                        {
                            int tutorialID= (int)m_parameters[0];
                            if (playerCharacter.TutorialComplete(tutorialID))
                            {
                                result = PassType.CompletePass;
                            }
                            else
                            {
                                result = PassType.FailsCondition;
                            }
                        }
                        break;
                    }
                case ConditionType.FirstTimeComplete:
                    {
						if (playerCharacter.Level > 19)
						{
							result = PassType.CompleteFail;
							break;
						}

						if (m_parameters.Count > 0)
                        {
                            int firstTimeID = (int)m_parameters[0];
                            if (playerCharacter.FirstTimeComplete(firstTimeID))
                            {
                                result = PassType.CompletePass;
                            }
                            else
                            {
                                result = PassType.FailsCondition;
                            }
                        }
                        break;
                    }
                default:
                    {
                        return result;
                       // break;
                    }

            }

            if (m_inverted == true)
            {
                result = InvertPassType(result);
            }

            return result;
        }
        /// <summary>
        /// Returns the inverse Passtype for the entered value
        /// Eg. CurrentPass to CurrentFail
        /// </summary>
        /// <param name="passType"></param>
        /// <returns></returns>
        internal PassType InvertPassType(PassType passType)
        {
            PassType result = PassType.FailsCondition;
            switch (passType)
            {
                case PassType.CompleteFail:
                    {
                        result = PassType.CompletePass;
                        break;
                    }
                case PassType.CurrentFail:
                    {
                        result = PassType.CurrentPass;
                        break;
                    }
                case PassType.CurrentPass:
                    {
                        result = PassType.CurrentFail;
                        break;
                    }
                case PassType.FailsCondition:
                    {
                        result = PassType.PassesCondition;
                        break;
                    }
                case PassType.PassesCondition:
                    {
                        result = PassType.FailsCondition;
                        break;
                    }
                case PassType.CompletePass:
                    {
                        result = PassType.CompleteFail;
                        break;
                    }
                default:
                    {
                        result = PassType.FailsCondition;
                        break;
                    }
            }
            return result;
        }
    }
    /// <summary>
    /// A condition that passes or fails based on the results of other conditions
    /// And/Or
    /// </summary>
    class SignpostLogicCondition : SignpostCondition
    {
        #region variables
        List<SignpostCondition> m_internalConditions = new List<SignpostCondition>();
        #endregion //variables
        #region properties
        internal List<SignpostCondition> InternalConditions
        {
            get { return m_internalConditions; }
        }
        #endregion //properties
        internal SignpostLogicCondition(int conditionID, ConditionType conditionType, bool inverted, List<float> parameters)
            : base(conditionID,conditionType, inverted, parameters)
        {

        }
        /// <summary>
        /// Returns whether the condition is passes or fails,
        /// result contains information on whether the condition result may change
        /// </summary>
        /// <param name="playerCharacter"></param>
        /// <param name="resultContainer"></param>
        /// <returns></returns>
        override internal PassType PassesConditions(Character playerCharacter, SignpostContainer resultContainer)
        {
            PassType result =  PassType.CompleteFail;
            List<SignpostCondition> currentFails = new List<SignpostCondition>();
            List<SignpostCondition> currentPasses = new List<SignpostCondition>();
            switch(m_conditionType){
                    // or Type 
                    //if any condition passes it is a pass
                case ConditionType.Or:
                    {
                        result = PassType.FailsCondition;
                        bool checkComplete = false;
                        //for each condition until one passes 
                        for (int i = 0; i < m_internalConditions.Count && checkComplete == false; i++)
                        {
                            SignpostCondition currentCondition = m_internalConditions[i];
                            PassType currentPass = currentCondition.PassesConditions(playerCharacter, resultContainer);
                            
                            switch (currentPass)
                            {
                                case PassType.CurrentFail:
                                    {
                                        if (result > PassType.CurrentFail)
                                        {
                                            result = currentPass;
                                        }
                                        //this may pass in the future
                                        currentFails.Add(currentCondition);
                                        break;
                                    }
                                case PassType.FailsCondition:
                                    {
                                        if (result > PassType.FailsCondition)
                                        {
                                            result = currentPass;
                                        }
                                        break;
                                    }
                                case PassType.CompleteFail:
                                    {
                                        break;
                                    }
                                case PassType.CurrentPass:
                                    {
                                        result = currentPass;
                                        //or condition passed, finish looking
                                        checkComplete = true;
                                        //this may fail in the future
                                        currentPasses.Add(currentCondition);
                                        //if one thing passes the other conditions nolonger matter
                                        currentFails.Clear();
                                        break;
                                    }
                                
                                case PassType.PassesCondition:
                                case PassType.CompletePass:
                                    {
                                        result = currentPass;
                                        //or condition passed, finish looking
                                        //if one thing passes the other conditions nolonger matter
                                        currentFails.Clear();
                                        checkComplete = true;
                                        break;
                                    }
                                
                                   
                            }

                        }
                        break;
                        
                    }
                    //and condition
                    //if any condition fails it is a fail
                case ConditionType.And:
                    {
                        result = PassType.CompletePass;
                        bool checkFailed = false;
                        for (int i = 0; i < m_internalConditions.Count && checkFailed == false; i++)
                        {
                            SignpostCondition currentCondition = m_internalConditions[i];
                            PassType currentPass = currentCondition.PassesConditions(playerCharacter, resultContainer);

                            switch (currentPass)
                            {
                                case PassType.CurrentFail:
                                    {
                                        //this may pass later
                                        currentFails.Add(currentCondition);
                                        //there is no way for these to pass if there are still fails
                                        currentPasses.Clear();
                                        checkFailed = true;
                                        result=currentPass;
                                        break;
                                    }
                                
                                case PassType.FailsCondition:
                                case PassType.CompleteFail:
                                    {
                                        //there is no way for these to pass if there are still fails
                                        currentPasses.Clear();
                                        checkFailed = true;
                                        result=currentPass;
                                        break;
                                    }
                                case PassType.CurrentPass:
                                    {
                                        result = PassType.CurrentPass;
                                        currentPasses.Add(currentCondition);
                                        break;
                                    }
                                
                                case PassType.PassesCondition:
                                case PassType.CompletePass:
                                    {
                                        if (result < currentPass)
                                        {
                                            result = currentPass;
                                        }
                                        break;
                                    }
                            }
                           
                        }
                        break;
                    }

            }
            resultContainer.AddConditions(currentFails, false);
            resultContainer.AddConditions(currentPasses, true);
            return result;
        }
    }
    class CharacterEvent
    {
        #region enums
        internal enum EventType
        {
            None,
            QuestComplete,
            QuestStageComplete,
            CharacterDied,

        }
        #endregion //enums
        #region variables
        /// <summary>
        /// The time the event occurred
        /// </summary>
        double m_actionedTime = NetTime.Now;
        EventType m_eventType = EventType.None;
        /// <summary>
        /// A list of float parameters
        /// depending on the event type these values will be used in different ways
        /// </summary>
        List<float> m_parameters = null;
    
        #endregion //variables
        #region properties
        /// <summary>
        /// The time the event occurred
        /// </summary>
        internal double ActionedNetTime
        {
            get { return m_actionedTime; }
        }
        internal EventType Type
        {
            get { return m_eventType; }
        }

        #endregion //properties

        internal CharacterEvent(EventType eventType,List<float> parameters)
        {
            m_parameters = parameters;
            m_eventType = eventType;
        }
        internal static CharacterEvent GetQuestComplete(List<CharacterEvent> eventList,int questID)
        {
            CharacterEvent relatedEvent = null;
            for (int i = 0; i<eventList.Count&& relatedEvent == null; i++)
            {
                CharacterEvent currentEvent = eventList[i];

                if (currentEvent.m_eventType == EventType.QuestComplete)
                {
                    if ((currentEvent.m_parameters.Count > 0) && ((int)currentEvent.m_parameters[0] == questID))
                    {
                        relatedEvent = currentEvent;
                    }
                }
            }

            return relatedEvent;

        }
        internal static CharacterEvent GetQuestStageComplete(List<CharacterEvent> eventList, int questID, int stageID)
        {
            CharacterEvent relatedEvent = null;
            for (int i = 0; i < eventList.Count && relatedEvent == null; i++)
            {
                CharacterEvent currentEvent = eventList[i];

                if (currentEvent.m_eventType == EventType.QuestStageComplete)
                {
                    if ((currentEvent.m_parameters.Count > 1) && ((int)currentEvent.m_parameters[0] == questID) && ((int)currentEvent.m_parameters[1] == stageID))
                    {
                        relatedEvent = currentEvent;
                    }
                }
            }

            return relatedEvent;
        }
        internal static int GetNumDeathsForTime(List<CharacterEvent> eventList, float timeInSeconds)
        {
            bool searchComplete = false;
            double searchTime = NetTime.Now - timeInSeconds;
            int deathCount =0;
            for (int i = 0; i < eventList.Count && searchComplete == false; i++)
            {
                CharacterEvent currentEvent = eventList[i];
                if (currentEvent.m_actionedTime > searchTime)
                {
                    if (currentEvent.m_eventType == EventType.CharacterDied)
                    {
                        deathCount++;
                    }
                }
                else
                {
                    searchComplete = true;
                }
            }

            return deathCount;
        }
    }
}
